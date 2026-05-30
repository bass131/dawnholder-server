namespace Dawnholder.Client.Net;

/// <summary>
/// ADR-010 패턴(Embedded PDB + EmbedAllSources) 동작을 Unity에서 재검증하는 도구.
/// 미사용처럼 보여도 삭제 금지 — 라이브러리 업그레이드/툴체인 변경 시 재검증 도구로 유용.
/// (Unity에서 이 타입 위 F12 → 디컴파일이 아니라 이 원본 .cs + 한국어 주석이 ReadOnly로
///  보이면 ADR-010 패턴 정상.)
/// </summary>
public static class SmokeProbe
{
    /// <summary>Unity F12 시 이 한국어 문장이 그대로 보여야 함 (ADR-010 동작 확인).</summary>
    public const string Marker = "ClientNet 라이브러리가 Unity에서 정상 인식됨";
}
