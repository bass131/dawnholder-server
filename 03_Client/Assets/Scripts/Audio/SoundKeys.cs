namespace Dawnholder.Client.Audio
{
    // M7 사운드 키 카탈로그. 콜사이트 매직스트링 방지 + 컴파일 타임 안전.
    // 키는 Resources/Audio/<key를 '/'로 치환> 경로의 클립으로 해석됨 (AudioManager.ResourcePath).
    public static class SoundKeys
    {
        // ── BGM ──
        public const string BgmMainMenu = "bgm.mainmenu";
        public const string BgmTown = "bgm.town";
        public const string BgmHunting = "bgm.hunting";
        public const string BgmBoss = "bgm.boss";
        public const string BgmEnding = "bgm.ending";

        // ── SFX: Combat ──
        public const string MeleeSwing = "sfx.combat.melee_swing";
        public const string MagicCast = "sfx.combat.magic_cast";
        public const string ProjectileLaunch = "sfx.combat.projectile_launch";
        public const string Dash = "sfx.combat.dash";
        public const string TeleportDepart = "sfx.combat.teleport_depart";
        public const string TeleportArrive = "sfx.combat.teleport_arrive";
        public const string HitEnemy = "sfx.combat.hit_enemy";
        public const string HitLightning = "sfx.combat.hit_lightning";
        public const string HitPlayer = "sfx.combat.hit_player";
        public const string EnemyAttack = "sfx.combat.enemy_attack";
        public const string EnemyDie = "sfx.combat.enemy_die";    // Normal(슬라임)
        public const string GolemDie = "sfx.combat.golem_die";
        public const string BossDie = "sfx.combat.boss_die";      // Boss(뱀파이어)
        public const string StageClear = "sfx.combat.stage_clear";

        // ── SFX: Movement ──
        public const string JumpStart = "sfx.movement.jump_start";
        public const string JumpLand = "sfx.movement.jump_land";
        public const string Footstep = "sfx.movement.footstep";

        // ── UI ── (콜사이트에서 'ui.' 접두 → AudioManager가 전용 UI 소스로 재생)
        public const string ButtonClick = "ui.button_click";
        public const string PanelOpen = "ui.panel_open";
        public const string Toast = "ui.toast";
        public const string QuestComplete = "ui.quest_complete";
        public const string PartyInvite = "ui.party_invite";

        // destMapId → BGM 키 (SceneRouter와 병렬). 0=Town,1=Hunting,2=Boss,3=Ending.
        public static string? BgmKeyForMap(byte mapId) => mapId switch
        {
            0 => BgmTown,
            1 => BgmHunting,
            2 => BgmBoss,
            3 => BgmEnding,
            _ => null,
        };
    }
}
