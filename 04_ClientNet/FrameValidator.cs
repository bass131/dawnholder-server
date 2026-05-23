// 02_Server/Network/Session.cs(PacketSession 상수)와 동기화 약속 —
// 두 파일(또는 두 상수 블록) 동시 변경 의무 (헌법 #4 정합, ServerCore 재사용성 보존).
// 양쪽 같은 상수 + 같은 시그니처 의무.
//
// 서버 측 카운터파트: 02_Server/Network/Session.cs PacketSession 클래스 내
//   public const int MinFrameSize = 4
//   public const int MaxFrameSize = 4096
//   분기: if (dataSize < MinFrameSize || dataSize > MaxFrameSize) → Disconnect()

namespace Dawnholder.Client.Net;

/// <summary>
/// length-prefix 기반 framing 검증 helper.
///
/// <para>
/// <b>왜 별도 파일인가?</b> — 서버 측(ServerCore)은 의도적으로 Shared 참조 X (재사용성 보존
/// — 옵션 B 변형, ADR-012). 따라서 클라/서버 각자 validator를 보유하되 *같은 시그니처 +
/// 같은 상수*를 유지하는 동기화 약속 방식으로 헌법 #4 정신(복사-붙여넣기 금지 = 동기화 깨짐
/// 방지)을 지킵니다.
/// </para>
///
/// <para>
/// <b>결함 3종 봉합 (M4.1 Phase 03)</b>:
/// <list type="bullet">
/// <item>dataSize = 0 → buffer.Count 검사를 통과해 zero-size 루프 무한 반복</item>
/// <item>dataSize &lt; 4 (헤더 미만) → packetId 슬롯까지 읽으려다 깨진 frame 해석</item>
/// <item>dataSize &gt; 4096 → 정상 데이터 도착 불가능 frame이 disconnect 안 되고 wait 잠김</item>
/// </list>
/// </para>
/// </summary>
public static class FrameValidator
{
    /// <summary>
    /// 유효한 frame의 최소 크기(바이트). [size:2][packetId:2] = 4.
    /// 서버 측 PacketSession.MinFrameSize 와 반드시 동일 값 유지.
    /// </summary>
    public const int MinFrameSize = 4;

    /// <summary>
    /// 유효한 frame의 최대 크기(바이트). 초과 시 공격 또는 버그 frame으로 판단.
    /// 서버 측 PacketSession.MaxFrameSize 와 반드시 동일 값 유지.
    /// 또한 98_Shared/GameData/Constants.cs MaxPacketSize 와 동기화 약속.
    /// </summary>
    public const int MaxFrameSize = 4096;

    /// <summary>
    /// frame 헤더의 dataSize 값이 유효한 범위인지 검증합니다.
    /// </summary>
    /// <param name="dataSize">패킷 헤더에서 읽은 2바이트 크기 값.</param>
    /// <param name="reason">
    /// 검증 실패 시 사람이 읽을 수 있는 이유 문자열. 성공 시 <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> = 유효한 frame. <see langword="false"/> = invalid, 즉시 disconnect 필요.
    /// </returns>
    public static bool TryValidateFrameHeader(ushort dataSize, out string? reason)
    {
        if (dataSize < MinFrameSize)
        {
            reason = "too small";
            return false;
        }

        if (dataSize > MaxFrameSize)
        {
            reason = "too large";
            return false;
        }

        reason = null;
        return true;
    }
}
