namespace Dawnholder.Client.Network
{
    // 서버 02_Server/GameServer/Handlers/IPacketHandler.cs 미러 (§3.2).
    //
    // **책임**: 수신 버퍼 하나를 디코딩 + Unity 컨텍스트 반영.
    //   파싱은 socket 워커 스레드 안에서 완료, Unity API 접근은 반드시
    //   MainThreadDispatcher.Enqueue 경유.
    //
    // **Handle 진입 시점**: OnRecvPacket 호출 = socket 워커 스레드.
    //   → Unity API 직접 호출 금지. MainThreadDispatcher 큐 경유 의무.
    internal interface IClientPacketHandler
    {
        void Handle(UnityClientSession session, System.ArraySegment<byte> buffer);
    }
}
