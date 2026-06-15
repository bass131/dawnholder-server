namespace Dawnholder.Client.Rendering
{
    // SortingLayer 이름 상수 단일 출처 (M6 Phase 02).
    // TagManager.asset의 SortingLayer 이름과 1:1 일치해야 함 — 코드/에셋 양쪽 오타 방지.
    //
    // 렌더 순서(뒤 → 앞): Background → TownTerrain → DetailTerrain → NPC → Enemy → Player → UI.
    //   Enemy = Player 바로 아래 전용 레이어 (로컬 플레이어가 항상 적보다 앞 = 전투 가독성).
    //   영호 Phase 05 조정 지점: 순서·Enemy 배치는 Play-test 후 변경 가능.
    public static class RenderLayers
    {
        public const string Background    = "Background";
        public const string TownTerrain   = "TownTerrain";
        public const string DetailTerrain = "DetailTerrain";
        public const string Npc           = "NPC";
        public const string Enemy         = "Enemy";
        public const string Player        = "Player";
        public const string Ui            = "UI";
    }
}
