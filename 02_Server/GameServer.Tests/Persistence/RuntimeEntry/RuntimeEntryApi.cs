using System.Reflection;
using System.Runtime.ExceptionServices;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// The API v2 seams that writer1 and writer2 add for the runtime entry goal (api-contract v2 「선행시험과 두 writer가
/// 공유할 최소 타입 접점(v2)」), reached by name. A direct reference to a member that does not exist yet would stop the
/// whole test assembly from compiling, so a missing seam fails only its own test with a message starting with
/// <see cref="Missing"/>. That message marks an API-absence RED; every other assertion failure is a behaviour RED.
/// API v2 fixes type and member names but not namespaces, so server types are matched by their unique simple name.
/// </summary>
static class RuntimeEntryApi
{
    public const string Missing = "[API 부재] ";

    const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly Assembly ServerAssembly = typeof(GameWorld).Assembly;
    static readonly Assembly SharedAssembly = typeof(PacketID).Assembly;

    public static Type ServerType(string name)
    {
        Type[] matches = ServerAssembly.GetTypes().Where(type => type.Name == name).ToArray();
        Assert.True(matches.Length != 0, $"{Missing}{name} does not exist in the GameServer assembly yet");
        Assert.True(matches.Length == 1, $"API v2 names one {name}; the GameServer assembly has {matches.Length}");
        return matches[0];
    }

    public static Type SharedType(string fullName)
    {
        Type? type = SharedAssembly.GetType(fullName);
        Assert.True(type is not null, $"{Missing}{fullName} does not exist in the Shared assembly yet");
        return type!;
    }

    /// <summary>writer1: <c>PersistenceAccountSelector(string configuredName, Guid accountId)</c>.</summary>
    public static object NewSelector(string? configuredName, Guid accountId)
        => Construct(ServerType("PersistenceAccountSelector"), new[] { typeof(string), typeof(Guid) }, configuredName, accountId);

    public static string NormalizedAccountName(object selector)
        => (string)GetProperty(selector, "NormalizedAccountName", typeof(string))!;

    public static Guid SelectorAccountId(object selector)
        => (Guid)GetProperty(selector, "AccountId", typeof(Guid))!;

    public static bool IsPersistent(object selector, Guid accountId)
        => (bool)Call(selector, "IsPersistent", typeof(bool), accountId)!;

    /// <summary>writer2: <c>CharacterAdmissionServices(selector, identity, repository, timeProvider, int queueCapacity = 4)</c>.</summary>
    public static object NewServices(object selector, PersistenceIdentity identity, ICharacterRepository repository, TimeProvider timeProvider)
    {
        Type[] parameters = { selector.GetType(), typeof(PersistenceIdentity), typeof(ICharacterRepository), typeof(TimeProvider), typeof(int) };
        // The default queue capacity of API v2 is passed explicitly; reflection does not apply optional values.
        return Construct(ServerType("CharacterAdmissionServices"), parameters, selector, identity, repository, timeProvider, 4);
    }

    // Current Stored host: the actual Core facade owns ID selection; repositories remain the test boundary.
    public static CharacterAdmissionServices NewCatalogServices(ICharacterCatalogRepository catalog,
        ICharacterRepository characters, TimeProvider clock)
        => new(new CharacterCatalogService(catalog, characters, PersistenceTestValues.Defaults(), clock), clock);

    /// <summary>writer2: <c>GameWorld.AttachPersistence(CharacterAdmissionServices)</c>, once and before Start.</summary>
    public static void AttachPersistence(GameWorld world, object services)
        => Call(world, "AttachPersistence", typeof(void), services);

    /// <summary>writer2: <c>Task GameWorld.FlushPersistenceClosuresAsync(PersistenceDeadline)</c>, called off the tick.</summary>
    public static Task FlushPersistenceClosuresAsync(GameWorld world, PersistenceDeadline deadline)
        => (Task)Call(world, "FlushPersistenceClosuresAsync", typeof(Task), deadline)!;

    /// <summary>
    /// writer2: <c>internal CharacterAdmissionState GameSession.AdmissionState</c>. API v2 keeps the enum's numbers out of
    /// the contract, so only the state name (Selecting, Loading, Active, Rejected, Closed, ...) is returned.
    /// </summary>
    public static string AdmissionState(GameSession session)
    {
        PropertyInfo? property = typeof(GameSession).GetProperty("AdmissionState", Members);
        Assert.True(property is not null, $"{Missing}GameSession.AdmissionState does not exist yet");
        Assert.Equal("CharacterAdmissionState", property!.PropertyType.Name);
        return Read(property, session)!.ToString()!;
    }

    /// <summary>writer2: immutable <c>CharacterClass? PersistenceWorkResult.LoadedClass</c>.</summary>
    public static CharacterClass? LoadedClass(PersistenceWorkResult result)
        => (CharacterClass?)GetProperty(result, "LoadedClass", typeof(CharacterClass?));

    public static PropertyInfo Property(Type type, string name, Type propertyType)
    {
        PropertyInfo? property = type.GetProperty(name, Members);
        Assert.True(property is not null, $"{Missing}{type.Name}.{name} does not exist yet");
        Assert.Equal(propertyType, property!.PropertyType);
        return property;
    }

    static object? GetProperty(object target, string name, Type propertyType)
        => Read(Property(target.GetType(), name, propertyType), target);

    static object Construct(Type type, Type[] parameters, params object?[] arguments)
    {
        ConstructorInfo? constructor = type.GetConstructor(Members, parameters);
        string signature = string.Join(", ", parameters.Select(parameter => parameter.Name));
        Assert.True(constructor is not null, $"{Missing}{type.Name}({signature}) does not exist yet");
        return Unwrap(() => constructor!.Invoke(arguments))!;
    }

    static object? Call(object target, string name, Type returnType, params object[] arguments)
    {
        Type[] parameters = arguments.Select(argument => argument.GetType()).ToArray();
        MethodInfo? method = target.GetType().GetMethod(name, Members, parameters);
        string signature = string.Join(", ", parameters.Select(parameter => parameter.Name));
        Assert.True(method is not null, $"{Missing}{target.GetType().Name}.{name}({signature}) does not exist yet");
        Assert.Equal(returnType, method!.ReturnType);
        return Unwrap(() => method.Invoke(target, arguments));
    }

    static object? Read(PropertyInfo property, object target) => Unwrap(() => property.GetValue(target));

    // A product exception thrown through reflection keeps its own type, so tests can assert it as written.
    static object? Unwrap(Func<object?> invoke)
    {
        try
        {
            return invoke();
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(wrapped.InnerException).Throw();
            throw;
        }
    }
}
