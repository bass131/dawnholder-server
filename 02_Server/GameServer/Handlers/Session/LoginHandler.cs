using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

internal sealed class LoginHandler : IPacketHandler
{
    public bool RequiresSelectedClass => false;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!session.TryBeginLogin())
        {
            Console.WriteLine("[Trust] Duplicate C_Login — dropped");
            return;
        }

        // Generated string reads trust the prefix and ignore trailing bytes. Validate the exact
        // UTF-16 frame before decoding, including odd lengths and prefixes smaller than the payload.
        ReadOnlySpan<byte> frame = buffer.AsSpan();
        int nameBytes = frame.Length >= 6 ? BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(4, 2)) : -1;
        if (frame.Length < 6 || BinaryPrimitives.ReadUInt16LittleEndian(frame) != frame.Length ||
            nameBytes != frame.Length - 6 || nameBytes % 2 != 0 ||
            nameBytes < AccountNameRule.MinLength * 2 || nameBytes > AccountNameRule.MaxLength * 2)
        {
            RejectName(session, nameBytes);
            return;
        }

        C_Login packet = new();
        packet.Read(buffer);
        if (!AccountNameRule.TryNormalize(packet.accountName, out string normalized))
        {
            RejectName(session, nameBytes);
            return;
        }

        session.SubmitLogin(normalized);
    }

    static void RejectName(GameSession session, int nameBytes)
    {
        Console.WriteLine($"[Login] rejected nameBytes={nameBytes} result=InvalidName");
        session.RejectLogin(LoginResult.InvalidName);
    }
}
