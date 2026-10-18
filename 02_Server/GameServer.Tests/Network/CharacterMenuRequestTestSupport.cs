using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// server-request-contract-v1.md §1: only transport Send/Disconnect are observed here.
// LoginTestWorld retains ownership of the real world, account processing and ticks.
internal sealed class CharacterMenuRequestWorld : IDisposable
{
    readonly LoginTestWorld _login = new();

    internal GameWorld World => _login.World;

    public void Dispose() => _login.Dispose();

    internal void Tick() => _login.Tick();

    internal CharacterMenuRequestSession Connected()
    {
        CharacterMenuRequestSession session = new(World);
        session.OnConnected(LoginTestWorld.Endpoint);
        return session;
    }

    internal CharacterMenuRequestSession Handshaken()
    {
        Assert.Equal((ushort)22, ProtocolVersion.Current);
        CharacterMenuRequestSession session = Connected();
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        Assert.Single(session.Packets.Where(packet => CharacterMenuRequestWire.Id(packet) == 8));
        Assert.False(session.IsClosing);
        return session;
    }

    internal CharacterMenuRequestSession LoggedIn(string name = "menu_account")
    {
        CharacterMenuRequestSession session = Handshaken();
        session.OnRecvPacket(new C_Login { accountName = name }.Write());
        Tick();
        Assert.True(session.IsLoggedIn, "fixture: real C_Login must be accepted");
        Assert.Equal(new byte[] { 5, 0, 42, 0, 0 },
            Assert.Single(session.Packets.Where(packet => CharacterMenuRequestWire.Id(packet) == 42)));
        session.Clear();
        return session;
    }

    internal PlayerEntity? PlayerOf(CharacterMenuRequestSession session)
        => World.AllLiveMaps.SelectMany(map => map.Players)
            .SingleOrDefault(player => ReferenceEquals(player.Owner, session));

    internal void AssertNoAdmission(CharacterMenuRequestSession session, int onlineAccounts)
    {
        Assert.Null(PlayerOf(session));
        Assert.Equal(-1, session.EntityId);
        Assert.False(session.Selected);
        Assert.Equal(onlineAccounts, World.Accounts.OnlineCount);
        Assert.Empty(Registrations(World.Inventory, "_registeredSessions"));
        Assert.Empty(Registrations(World.Inventory, "_states"));
        Assert.Empty(Registrations(World.Guild, "_members"));
        object state = Field(World.Guild, "_state");
        Assert.Equal(0, (int)state.GetType().GetProperty("Treasury",
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(state)!);
        Assert.DoesNotContain(session.Packets, packet => CharacterMenuRequestWire.Id(packet) is 3 or 44 or 48);
    }

    internal static IDictionary Registrations(object owner, string name) => (IDictionary)Field(owner, name);

    static object Field(object owner, string name)
        => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}

internal sealed class CharacterMenuRequestSession(GameWorld world) : GameSession(world)
{
    readonly List<byte[]> _packets = new();

    internal IReadOnlyList<byte[]> Packets => _packets;
    internal bool Selected => HasSelectedClass;
    internal int DisconnectCalls { get; private set; }

    public override void Send(ArraySegment<byte> packet) => _packets.Add(packet.ToArray());

    public override void Disconnect()
    {
        DisconnectCalls++;
        OnDisconnected(LoginTestWorld.Endpoint);
    }

    internal void Clear() => _packets.Clear();
}

// Independent byte oracle: packet-contract-v1.md lengths/LE fields and fixed request fixtures.
// No generated menu writer/reader, handler or lifetime algorithm is used to form expectations.
internal static class CharacterMenuRequestWire
{
    internal static void RequireRegistered(ushort id)
    {
        bool registered = HandlerRegistry.TryGet((PacketID)id, out IPacketHandler handler);
        Assert.True(registered, $"server-request-contract-v1 §1: menu handler {id} must be registered");
        Assert.False(handler.RequiresSelectedClass);
    }

    internal static byte[] Request(ushort id, uint requestId)
    {
        string label = id switch
        {
            49 => "list.request",
            51 => "create.request",
            53 => "select.request",
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };
        byte[] frame = CharacterMenuPacketFixtures.Frames.Single(item => item.Label == label).Bytes.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4, 4), requestId);
        return frame;
    }

    internal static byte[] Failure(ushort requestPacketId, uint requestId, byte result)
    {
        byte[] frame = Convert.FromHexString(requestPacketId switch
        {
            49 => "1A00320000000000000000000000000000000000000000000000",
            51 => "10003400000000000000000000000000",
            53 => "0C0036000000000000000000",
            _ => throw new ArgumentOutOfRangeException(nameof(requestPacketId)),
        });
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4, 4), requestId);
        frame[8] = result;
        return frame;
    }

    internal static ushort Id(byte[] frame) => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2, 2));

    internal static ArraySegment<byte> AtOffset(byte[] frame)
    {
        byte[] storage = Enumerable.Repeat((byte)0xCC, frame.Length + 14).ToArray();
        frame.CopyTo(storage, 7);
        return new(storage, 7, frame.Length);
    }

    internal static byte[] WithSize(byte[] frame)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0, 2), checked((ushort)frame.Length));
        return frame;
    }
}

// server-request-contract-v1.md §2: reflection is only a driver for the real product API.
// Missing types/signatures assert; there is no fallback state machine or shadow Core type.
internal sealed class CharacterMenuLifetimeProbe
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    readonly object _lifetime;
    readonly MethodInfo _begin;
    readonly MethodInfo _isCurrent;
    readonly MethodInfo _complete;
    readonly MethodInfo _close;

    internal CharacterMenuLifetimeProbe()
    {
        Assembly assembly = typeof(GameSession).Assembly;
        Type lifetime = RequireType(assembly, "CharacterMenuRequestLifetime");
        Type result = RequireType(assembly, "CharacterMenuRequestBeginResult");
        Type ticket = RequireType(assembly, "CharacterMenuRequestTicket");
        ConstructorInfo? constructor = lifetime.GetConstructor(Instance, null, Type.EmptyTypes, null);
        Assert.NotNull(constructor);
        _begin = RequireMethod(lifetime, "Begin", result, typeof(uint), typeof(ushort), typeof(PersistenceDeadline));
        _isCurrent = RequireMethod(lifetime, "IsCurrent", typeof(bool), ticket);
        _complete = RequireMethod(lifetime, "Complete", typeof(bool), ticket);
        _close = RequireMethod(lifetime, "Close", typeof(void));
        PropertyInfo status = RequireProperty(result, "Status", null);
        Assert.True(status.PropertyType.IsEnum, "Begin Status must name the four contract states");
        Assert.Equal(new[] { "Closed", "Ignored", "InProgress", "Started" },
            Enum.GetNames(status.PropertyType).OrderBy(name => name).ToArray());
        RequireProperty(result, "Ticket", ticket);
        RequireProperty(ticket, "RequestId", typeof(uint));
        RequireProperty(ticket, "RequestPacketId", typeof(ushort));
        RequireProperty(ticket, "Generation", typeof(long));
        RequireProperty(ticket, "Deadline", typeof(PersistenceDeadline));
        Assert.Empty(ticket.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        _lifetime = constructor!.Invoke(null);
    }

    internal BeginObservation Begin(uint requestId, ushort id, PersistenceDeadline? deadline)
    {
        object result = Invoke(_begin, requestId, id, deadline)!;
        return new(Read<object>(result, "Status").ToString()!, ReadNullable(result, "Ticket"));
    }

    internal bool IsCurrent(object ticket) => (bool)Invoke(_isCurrent, ticket)!;
    internal bool Complete(object ticket) => (bool)Invoke(_complete, ticket)!;
    internal void Close() => Invoke(_close);

    internal static T Read<T>(object owner, string name) => (T)ReadNullable(owner, name)!;

    object? Invoke(MethodInfo method, params object?[] arguments)
    {
        try
        {
            return method.Invoke(_lifetime, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType("Dawnholder.Server.GameServer.Sessions." + name);
        Assert.True(type is not null, $"server-request-contract-v1 §2: product type {name} is absent; later semantics not reached");
        return type!;
    }

    static MethodInfo RequireMethod(Type owner, string name, Type result, params Type[] arguments)
    {
        MethodInfo? method = owner.GetMethod(name, Instance, null, arguments, null);
        Assert.True(method is not null, $"{owner.Name}.{name} signature is required by server-request-contract-v1 §2");
        Assert.Equal(result, method!.ReturnType);
        return method;
    }

    static PropertyInfo RequireProperty(Type owner, string name, Type? expectedType)
    {
        PropertyInfo? property = owner.GetProperty(name, Instance);
        Assert.NotNull(property);
        Assert.NotNull(property!.GetMethod);
        Assert.Null(property.SetMethod);
        if (expectedType is not null) Assert.Equal(expectedType, property.PropertyType);
        return property;
    }

    static object? ReadNullable(object owner, string name) => owner.GetType().GetProperty(name, Instance)!.GetValue(owner);

    internal sealed record BeginObservation(string Status, object? Ticket);
}
