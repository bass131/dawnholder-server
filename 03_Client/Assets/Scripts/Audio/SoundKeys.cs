#nullable enable
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
        // 피격(플레이어→적) — 적 종류별 분리. Lightning은 별도.
        public const string HitSlime = "sfx.combat.hit_slime";
        public const string HitGolem = "sfx.combat.hit_golem";
        public const string HitGeneric = "sfx.combat.hit_generic";
        public const string HitLightning = "sfx.combat.hit_lightning";
        // 피격(적→플레이어) — 직업별 분리.
        public const string HitKnight = "sfx.combat.hit_knight";
        public const string HitMage = "sfx.combat.hit_mage";
        // 적 공격 모션 — 종류별 분리 + 울음(공격에 레이어).
        public const string AttackSlime = "sfx.combat.attack_slime";
        public const string AttackGolem = "sfx.combat.attack_golem";
        public const string CrySlime = "sfx.combat.cry_slime";
        public const string CryGolem = "sfx.combat.cry_golem";
        // 보스(뱀파이어) — 준비자세(텔레그래프) + 찌르기.
        public const string BossTelegraph = "sfx.combat.boss_telegraph";
        public const string BossStab = "sfx.combat.boss_stab";
        public const string EnemyDie = "sfx.combat.enemy_die";    // Normal(슬라임)
        public const string GolemDie = "sfx.combat.golem_die";
        public const string BossDie = "sfx.combat.boss_die";      // Boss(뱀파이어)
        public const string StageClear = "sfx.combat.stage_clear";

        // ── SFX: Movement ──
        public const string JumpStart = "sfx.movement.jump_start";
        public const string JumpLand = "sfx.movement.jump_land";
        public const string Footstep = "sfx.movement.footstep";

        // ── SFX: Player 생명주기 ── (직업별 사망 분리)
        public const string DeathKnight = "sfx.player.death_knight";
        public const string DeathMage = "sfx.player.death_mage";
        public const string PlayerRespawn = "sfx.player.respawn";

        // ── SFX: Zone/포탈 ──
        public const string PortalEnter = "sfx.zone.portal_enter";

        // ── UI ── (콜사이트에서 'ui.' 접두 → AudioManager가 전용 UI 소스로 재생)
        public const string ButtonClick = "ui.button_click";
        public const string PanelOpen = "ui.panel_open";
        public const string Toast = "ui.toast";
        public const string QuestComplete = "ui.quest_complete";
        public const string PartyInvite = "ui.party_invite";
        public const string PanelClose = "ui.panel_close";        // M7 확장 — 전용 닫기음
        public const string UiError = "ui.error";                 // 거부/에러 (PortalLocked 등)
        public const string PartyFormed = "ui.party_formed";      // 파티 결성
        public const string PartyDisbanded = "ui.party_disbanded";// 파티 해산

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
