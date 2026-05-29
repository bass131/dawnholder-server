namespace Dawnholder.Client.Network
{
    // M4.2 Phase 04: destMapId(byte) → Unity 씬 이름 매핑.
    // 서버 MapId enum 값과 정합 (Town=0/HuntingGround=1/BossRoom=2/Ending=3).
    // 씬 이름은 Build Settings의 파일명 기준 (폴더 경로 무관).
    // 매핑이 클라 표현(렌더링 책임)이라 헌법 #1 위반 아님.
    //
    // **분리 이유 (§2.2)**: 씬 이름 매핑은 "클라 표현 라우팅" 단독 책임.
    //   UnityClientSession(framing·dispatch 컨테이너)과 도메인이 다름.
    //   정적 헬퍼이므로 MonoBehaviour X, 새 맵 추가 시 한 곳만 수정.
    internal static class SceneRouter
    {
        /// <summary>
        /// 서버 MapId → Unity Build Settings 씬 이름.
        /// 알 수 없는 mapId는 <c>string.Empty</c> 반환 — 호출처에서 null/empty 검사 의무.
        /// </summary>
        public static string MapIdToSceneName(byte mapId) => mapId switch
        {
            0 => "Town",
            1 => "HuntingGround",
            2 => "BossRoom",
            3 => "Ending",
            _ => string.Empty
        };
    }
}
