namespace Dawnholder.Client.Network
{
    // destMapId(byte) → Unity 씬 이름 / HUD 표시명 매핑.
    // 서버 MapId enum 값과 정합 (Town=0/HuntingGround=1/BossRoom=2/Ending=3).
    // 씬 이름은 Build Settings의 파일명 기준 (폴더 경로 무관).
    // MapId drift를 한 파일로 봉인 — 씬명/표시명 모두 여기서 관리.
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

        /// <summary>
        /// 서버 MapId → HUD 표시명.
        /// 알 수 없는 mapId는 <c>string.Empty</c> 반환 — 호출처에서 null/empty 검사 의무.
        /// </summary>
        public static string MapIdToDisplayName(byte mapId) => mapId switch
        {
            0 => "Town",
            1 => "Hunting Ground",
            2 => "Boss Room",
            3 => "Ending",
            _ => string.Empty
        };
    }
}
