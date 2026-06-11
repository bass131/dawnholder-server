// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ 본 코드는 02_Server/Network/SendBuffer.cs와 거의 동일 (의도된 두 벌).
//
// "왜 합치지 않았나" — Y2 분리 갈래(ADR-012):
//   - 클라: .NET Standard 2.1, Unity Mono/IL2CPP 제약
//   - 서버: .NET 10 LTS, GC 최적화 자유
//   환경별 최적화 자유 + 한쪽 변경이 반대편 빌드 안 깸 + 한국 MMO 백엔드
//   현업 패턴(Rookiss/NCSoft/Nexon — 전용 서버 + 클라 socket layer 분리).
//
// "동기는 어떻게 보장하나" — 차이가 알고리즘 자체에 생기면 *그것이 신호*.
//   현재는 같은 알고리즘 우연히 가능. 패킷 정의는 PDL.xml + 코드 생성기가
//   자동 동기화 (98_Shared/Protocol/Generated/).
//
// 책임 단위 분리/통합 표는 ADR-012 본문 참조.
// ─────────────────────────────────────────────────────────────────────────────
namespace Dawnholder.Client.Net;

/// <summary>
/// 전송 버퍼의 ThreadLocal 풀.
///
/// **클라 컨텍스트 메모**:
/// - 클라는 보내는 측 스레드가 보통 *둘*: ① Unity main thread (사용자 입력 → Send 호출)
///   ② socket 콜백 스레드 (응답 수신 직후 ack 전송 등). ThreadLocal로 두 스레드가
///   각자 자기 chunk를 갖게 해서 lock 없이 동시 Open/Close 가능.
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
/// 단순 chunk 버퍼: 앞에서부터 _usedSize 만큼 채워나가는 방식.
/// Open()으로 자리 예약 → 직렬화 → Close()로 실제 사용량 커밋.
/// </summary>
public class SendBuffer
{
    readonly byte[] _buffer;
    int _usedSize;

    public SendBuffer(int chunkSize)
    {
        _buffer = new byte[chunkSize];
    }

    public int FreeSize => _buffer.Length - _usedSize;

    /// <summary>reserveSize 만큼의 자리를 *예약만* (커서는 안 움직임).</summary>
    public ArraySegment<byte> Open(int reserveSize)
    {
        if (reserveSize > FreeSize)
            return default;

        return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
    }

    /// <summary>예약한 영역 중 실제 usedSize 만큼 사용 확정 → 커서 전진.</summary>
    public ArraySegment<byte> Close(int usedSize)
    {
        var segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
        _usedSize += usedSize;
        return segment;
    }
}
