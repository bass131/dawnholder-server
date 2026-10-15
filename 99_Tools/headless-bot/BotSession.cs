using System.Net;
using System.Buffers.Binary;
using Dawnholder.Client.Net;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot;

// **상속 의도** (ADR-012 Y2 정합): 04_ClientNet의 PacketSession을 그대로 받음 →
// 프레이밍·SendBuffer·RecvBuffer는 클라와 100% 동일 코드. 봇은 게임 로직만 다름.
public class BotSession : PacketSession
{
    public Action<EndPoint>? OnConnectedCallback;
    public Action<EndPoint>? OnDisconnectedCallback;
    public Action<ArraySegment<byte>>? OnPacketCallback;
    readonly object _loadGate = new();
    (CharacterLoadStatus Status, CharacterClass? LoadedClass)? _characterLoad;
    public (CharacterLoadStatus Status, CharacterClass? LoadedClass)? CharacterLoad
    {
        get { lock (_loadGate) return _characterLoad; }
    }

    public override void OnConnected(EndPoint endPoint)
    {
        OnConnectedCallback?.Invoke(endPoint);
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        OnDisconnectedCallback?.Invoke(endPoint);
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array is not null && buffer.Count == 6)
        {
            ReadOnlySpan<byte> bytes = buffer.AsSpan();
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) == 6 &&
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2)) == (ushort)PacketID.S_CharacterLoadResult &&
                bytes[4] <= (byte)CharacterLoadStatus.Unavailable &&
                (bytes[4] == (byte)CharacterLoadStatus.Success ? bytes[5] <= (byte)CharacterClass.Mage : bytes[5] == 255))
            {
                lock (_loadGate)
                    _characterLoad ??= ((CharacterLoadStatus)bytes[4],
                        bytes[4] == (byte)CharacterLoadStatus.Success ? (CharacterClass?)bytes[5] : null);
            }
        }
        OnPacketCallback?.Invoke(buffer);
    }

    public override void OnSend(int numOfBytes)
    {
    }
}
