// 04_ClientNet/FrameValidator.cs와 동기화 약속 — 두 파일 동시 변경 의무
// (헌법 #4 정합, ServerCore 재사용성 보존: ServerCore는 의도적으로 Shared 참조 X).
// 상수/시그니처를 바꿀 때는 반드시 클라 측 FrameValidator.cs도 함께 수정.

namespace Dawnholder.Server.Network
{
    /// <summary>
    /// length-prefixed frame 헤더 검증 — fail-closed 정책.
    /// PacketSession.OnRecv에서 inline이던 검증 분기를 추출해 단위 테스트 가능하도록 분리.
    /// </summary>
    public static class FrameValidator
    {
        /// <summary>
        /// frame 최소 크기. [size:2][id:2] = 4. 미만은 invalid.
        /// = HeaderSize(2) + PacketIdSize(2).
        /// </summary>
        public const int MinFrameSize = 4;

        /// <summary>
        /// frame 최대 크기. 초과 시 fail-closed disconnect.
        /// 98_Shared/GameData/Constants.MaxPacketSize와 동기화 약속
        /// (ServerCore 재사용성 보존 — Shared 직접 참조 X, 두 파일 동시 변경 컨벤션으로 처리).
        /// </summary>
        public const int MaxFrameSize = 4096;

        /// <summary>
        /// frame 헤더(dataSize)가 유효한지 검증.
        /// </summary>
        /// <param name="dataSize">수신 버퍼에서 읽은 frame 크기 (LittleEndian ushort).</param>
        /// <param name="reason">거부 이유 (수락 시 null).</param>
        /// <returns>유효하면 true, 유효하지 않으면 false.</returns>
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
}
