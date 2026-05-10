namespace Dawnholder.Client.Net;

/// <summary>
/// 전송 버퍼의 ThreadLocal 풀.
///
/// **클라 컨텍스트 메모**:
/// - 클라는 보내는 측 스레드가 보통 *둘*: ① Unity main thread (사용자 입력 → Send 호출)
///   ② socket 콜백 스레드 (응답 수신 직후 ack 전송 등). ThreadLocal로 두 스레드가
///   각자 자기 chunk를 갖게 해서 lock 없이 동시 Open/Close 가능.
/// - 서버측 SendBufferHelper와 동일 코드. 클라에 단순화 안 한 이유: 같은 패턴을
///   양쪽에서 *왜 쓰는지* 이해하는 게 학습 가치 (Y2 갈래의 본질).
/// </summary>
public class SendBufferHelper
{
    public static ThreadLocal<SendBuffer?> CurrentBuffer { get; } =
        new ThreadLocal<SendBuffer?>(() => null);

    public static int ChunkSize { get; set; } = 65535 * 100;

    /// <summary>현재 스레드의 chunk에서 reserveSize 만큼 자리 예약. chunk가 없거나 부족하면 새 chunk.</summary>
    public static ArraySegment<byte> Open(int reserveSize)
    {
        if (CurrentBuffer.Value == null)
            CurrentBuffer.Value = new SendBuffer(ChunkSize);

        if (CurrentBuffer.Value!.FreeSize < reserveSize)
            CurrentBuffer.Value = new SendBuffer(ChunkSize);

        return CurrentBuffer.Value!.Open(reserveSize);
    }

    /// <summary>실제로 사용한 바이트 수만큼 커밋. 직렬화 직후 패킷 크기를 알게 됐을 때 호출.</summary>
    public static ArraySegment<byte> Close(int usedSize) =>
        CurrentBuffer.Value!.Close(usedSize);
}

/// <summary>
/// 단순 chunk 버퍼: 앞에서부터 m_UsedSize 만큼 채워나가는 방식.
/// Open()으로 자리 예약 → 직렬화 → Close()로 실제 사용량 커밋.
/// </summary>
public class SendBuffer
{
    readonly byte[] m_Buffer;
    int m_UsedSize;

    public int FreeSize => m_Buffer.Length - m_UsedSize;

    public SendBuffer(int chunkSize)
    {
        m_Buffer = new byte[chunkSize];
    }

    /// <summary>reserveSize 만큼의 자리를 *예약만* (커서는 안 움직임).</summary>
    public ArraySegment<byte> Open(int reserveSize)
    {
        if (reserveSize > FreeSize)
            return default;

        return new ArraySegment<byte>(m_Buffer, m_UsedSize, reserveSize);
    }

    /// <summary>예약한 영역 중 실제 usedSize 만큼 사용 확정 → 커서 전진.</summary>
    public ArraySegment<byte> Close(int usedSize)
    {
        var segment = new ArraySegment<byte>(m_Buffer, m_UsedSize, usedSize);
        m_UsedSize += usedSize;
        return segment;
    }
}
