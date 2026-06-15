---
owner: youngho
milestone: M7
type: night-progress (AI 무인 박제)
date: 2026-06-16
branch: feature/m7-sound
status: AI 1패스 완료 — 영호 청음 + GO 대기
---

# M7 사운드 — 야간 무인(AutoMode) 진행 박제

> 영호가 자는 사이(2026-06-16 새벽~) AI가 무인 1패스 완주. **push/PR/청음은 영호 GO 대기.** 최종 -DONE.md + HTML은 청음 후 Phase 05 클로즈아웃에서.

## 🎯 무엇을 했나

게임 사운드 0 → **사운드 시스템 + 32개 사운드 적용** (1패스). 7 commit ahead of origin/main (M6 아트 1 + M7 6).

- **AudioManager 인프라**: 자기-부트스트랩(프리팹 편집 0) 싱글톤. `PlaySfx(key,vol,throttle)` / `PlayBgm(key,fade)` / `StopBgm` / 볼륨(Master/BGM/SFX, PlayerPrefs). SFX pool 12 + UI 전용 소스 + BGM 2채널 크로스페이드. **누락 클립 no-op**(미생성 키 wiring돼도 NRE 0).
- **콜사이트 wiring 33곳** (18+7 파일): 전투(공격/스킬/피격/사망/적사망 kind분기)·이동(점프/발소리 코드티커)·UI(버튼/패널/토스트/퀘스트/파티)·BGM 존전환.
- **에셋**: 기존 17개 GUID보존 이관(BGM 4 + die 3 재사용 / 칩튠 의심 8 → `_replaced/` / 잉여 2 → `_unused/`) + **AI 생성 25개**(ElevenLabs SFX 24 + Lyria 엔딩 BGM 1).

## 🤔 왜 이렇게

- **생성 = AI 단독(ElevenLabs `elevenlabs-sound-effects-v2`)**, BgmComposer 칩튠 전면 배제(영호 "저품질 배제"). 프롬프트 = 공식 권장 수준(묘사어 3~4 + 품질 태그, 모던 판타지·dry). 실패=재시도≤3→MISSING(무한 생성 금지) — 실제 MISSING 0건.
- **발소리 = 코드 티커**(.anim 편집 회피, 무인 견고).
- **die 매핑**: Normal→Slime, Golem→Golem, **Boss→Vampire**(영호 확정). 잉여=Frog/Mushroom(스폰 로스터 없음).
- **자기-부트스트랩**: PersistentServices 프리팹에 MCP로 붙이는 대신 코드 RuntimeInitialize — 프리팹 편집 위험 제거.

## 🛠️ 어떻게 (커밋)

```
0153fd3 Phase D — AI 사운드 25개 생성
d8bc29b Phase C — 기존 17 에셋 Resources/Audio 이관 + import
492dfcd 확장 wiring — 미커버 이벤트 5키
454e55b Phase B — 콜사이트 wiring (27키 기초)
4f49dd6 Phase A — AudioManager 인프라
4c378a4 docs — 플랜/Phase 착수 갱신
75ccebb (M6 마무리 아트 — 동반)
```

## 🧪 테스트 (4중 검증 통과)

- Unity 컴파일 **0 err** (콘솔의 GenerateAsset 에러 3건은 생성 툴 일시 실패 로그, 컴파일 무관).
- 클립 해석 스모크 **32/33 RESOLVED** (`Resources.Load` 전 키 순회).
- 헌법 #1: origin/main 대비 **클라+문서만**, 02_Server/98_Shared/ProtocolVersion **무변경**.
- WSL2 회귀 **645 passed / 0 failed / 5 skipped** (baseline 일치).

## ➡️ 다음 (영호가 일어나서)

1. **청음** — 실제 Play로 32개 사운드 들어보기. 톤/볼륨 밸런스 체크. 마음에 안 드는 키는 알려주면 그 키만 재생성(프롬프트 조정).
2. **MISSING 1개**: `sfx.player.respawn` — 깔끔한 "리스폰 순간" 트리거가 없어 미wiring/미생성. 리스폰 신호 정의 방식 결정 필요.
3. **미적용 컨텐츠(아트/prefab 측, 사운드 무관)**: 투사체 prefab + Slime/Golem DamageEffect prefab 미배치(코드 TODO 경고). 사운드는 prefab 없어도 재생됨.
4. **확장 후보(미적용, 선택)**: party_disbanded / cooldown_ready / boss 패턴별 타격음 / 존 앰비언트 / 버튼 호버 — 발굴됐으나 스코프 가드로 보류.
5. **클로즈아웃**: 청음 OK → 볼륨 밸런싱 → 최종 `-DONE.md` + HTML → **영호 GO 시** push → PR(코드=admin 예외) → main 머지. (비가역 = GO 의무.)

## 📋 사운드 키 32개 (RESOLVED) + 1 MISSING

- **BGM(5)**: mainmenu, town, hunting, boss, ending
- **Combat(14)**: melee_swing, magic_cast, projectile_launch, dash, teleport_depart, teleport_arrive, hit_enemy, hit_lightning, hit_player, enemy_attack, enemy_die, golem_die, boss_die, stage_clear
- **Movement(3)**: jump_start, jump_land, footstep
- **Player(1)**: death  · *(respawn = MISSING)*
- **Zone(1)**: portal_enter
- **UI(8)**: button_click, panel_open, toast, quest_complete, party_invite, panel_close, error, party_formed
