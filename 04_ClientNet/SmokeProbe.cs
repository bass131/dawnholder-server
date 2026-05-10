namespace Dawnholder.Client.Net;

/// <summary>
/// Phase 03 검증용 빈 셸. 본 사용은 Phase 04부터 (Connector + ClientSession).
///
/// **검증 절차** (Phase 03 5단계):
/// 1. <c>dotnet build</c> 후 <c>03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll</c>
///    이 자동 생성됐는지 확인.
/// 2. Unity 6.4 LTS 에디터 → <c>Assets &gt; Refresh</c> (Ctrl+R).
/// 3. 임시 MonoBehaviour에서 <c>using Dawnholder.Client.Net;</c> + <c>SmokeProbe.Marker</c>
///    가 IntelliSense에 잡히면 통과.
/// 4. <c>SmokeProbe</c> 위에서 <b>F12</b>를 눌렀을 때, 디컴파일된 코드가 아니라
///    이 원본 .cs 파일 + <b>이 한국어 주석</b>이 ReadOnly로 보이면 ADR-010 패턴
///    (Embedded PDB + EmbedAllSources) 두 번째 인스턴스 검증 통과.
///
/// 이 클래스가 존재하는 이유는 오직 위 4단계 검증을 위해서. Phase 04 진입 시
/// 삭제하지는 말 것 — 향후 라이브러리 업그레이드/툴체인 변경 시 재검증 도구로 유용.
/// </summary>
public static class SmokeProbe
{
    /// <summary>Unity F12 시 이 한국어 문장이 그대로 보여야 함 (ADR-010 동작 확인).</summary>
    public const string Marker = "ClientNet 라이브러리가 Unity에서 정상 인식됨";
}
