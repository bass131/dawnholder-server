using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PacketManager
{
    #region SINGLETON
#nullable enable
    static PacketManager? _instance = new PacketManager();
#nullable disable

    public static PacketManager Instance
    {
        get { return _instance; }
        private set { }
    }
    #endregion
    
    PacketManager()
    {
        Register();
    }

    // < 프로토콜 ID, 패킷 처리 콜백>
    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> m_MakeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession,IPacket>> m_Handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();

    public void Register()
    {
        
        m_MakeFunc.Add((ushort)PacketID.S_Pong, MakePacket<S_Pong>);
        m_Handler.Add((ushort)PacketID.S_Pong, PacketHandler.S_PongHandler);

    }
        
    public void OnRecvPacket(PacketSession session,
        ArraySegment<byte> buffer,
        Action<PacketSession, IPacket> onRecvCallBack = null)
    {
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;

        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if(m_MakeFunc.TryGetValue(id, out func))
        {
            IPacket packet = func.Invoke(session, buffer);

            if (onRecvCallBack != null)
                onRecvCallBack.Invoke(session, packet);
            else
                HandlePacket(session, packet);
        }

        // System.Console.WriteLine($"Recv PacketId: {id}, Size: {size}");
    }

    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer)
        where T : IPacket, new()
    {
        T packet = new T();
        packet.Read(buffer);

        return packet;
    }

    public void HandlePacket(PacketSession _session, IPacket _packet)
    {
        Action<PacketSession, IPacket> action = null;
        if (m_Handler.TryGetValue(_packet.Protocol, out action))
            action.Invoke(_session, _packet);
    }
}