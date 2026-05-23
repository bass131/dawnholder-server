# Pre-Review for Codex β — 2026-05-23 — M4.1 Phase 01 (Pre-M4 Hardcoding Audit)

> **본 파일의 역할**: M4.1 Phase 01 1단계 Claude α 자체 점검 결과 + Codex β (본인 별 세션 직접 호출)에게 던질 입력 자료. 본인이 Codex 호출 시 *이 파일을 첨부* 또는 *내용 prompt에 박음*.
>
> **분담** (2026-05-23 봉합 정합):
> - **Claude (본 세션)** = 본 파일 + 다음 Step "본인이 던질 Codex prompt" + γ 비교
> - **본인 (별 세션)** = `codex exec --sandbox read-only --cd "C:\Dev\ClaudeDev" "@..."` 직접 호출 + 결과 박음

---

## 1. 변경 범위

- **마일스톤**: M4.1 Combat Precision (캡스톤 1 발표 6/10 전반 흡수)
- **본 Phase 정신**: M3 응급 코드 전체 *예상 못한 하드코딩* 색출 + plan 갱신 필요성 판정. ARCHITECTURE.md "M4 사전 과제 8건"은 *이미 인지* — 그 *외* 발견 중심.
- **점검 대상 영역**: `02_Server/GameServer/Combat/` + `Maps/` + `Handlers/` + `Network/` / `03_Client/Assets/Scripts/` 전영역 / `98_Shared/GameData/` + `Protocol/` / `99_Tools/headless-bot/` + `PacketGenerator/`
- **등급**: 보통 (qa+cross / 비가역 X / 보고서 박제 위주) — 8건+ 발견 시 *복잡 자동 상향* 트리거 박힘

---

## 2. α (Claude 자체 점검) 결과 — 22건 발견

### 2.1. 서버 측 (7건)

| # | 위치 | 박힌 magic/hardcoded | 추정 분류 |
|---|---|---|---|
| S1 | `02_Server/GameServer/Maps/GameMap.cs:40~50` | 3-zone 좌표: `NormalEnemySpawnX=10f` / `NormalEnemySpawnY=0f` / `NormalEnemyMaxHp=30` / `BossSpawnX=30f` / `BossSpawnY=0f` / `BossMaxHp=100` | **M4.2 이관 후보** (맵 분리·Constants 이관 영역) |
| S2 | `02_Server/GameServer/Maps/PlayerEntity.cs:39~40` | 플레이어 기본 HP `Hp=100`, `MaxHp=100` | **M4.1 Phase 02 흡수 후보** (PlayerStats 흡수 시 정합) |
| S3 | `02_Server/GameServer/Combat/PlayerStats.cs:31~35` | 전사 스탯 `Hp=150` / `MaxHp=150` / `Attack=15` / `Defense=5` / `MoveSpeed=4f` | **M4.1 Phase 02 영역** (Formulas.cs 흡수 박정) |
| S4 | `02_Server/GameServer/Combat/PlayerStats.cs:42~46` | 원거리 스탯 `Hp=80` / `MaxHp=80` / `Attack=12` / `Defense=2` / `MoveSpeed=6f` | **M4.1 Phase 02 영역** (Formulas.cs 흡수 박정) |
| S5 | `02_Server/GameServer/Combat/CombatConstants.cs:23~26,30` | `AttackRange=3.0f` / `AttackRangeSquared=9.0f` / `BaseDamage=10` / `AttackCooldownMs=500` | **M4.1 Phase 02·03 영역** (BaseDamage = Formulas 입력, AttackRange = hitbox 박정) |
| S6 | `02_Server/GameServer/Network/GameSession.cs:78` | 의도 rate-limit `IntentRateLimitPerSecond=500` | **M5+ 이관 후보** (Serilog + runtime config) |
| S7 | `02_Server/GameServer/Maps/GameMap.cs:26` | 엔티티 ID 초기값 `_nextEntityId=1` | **수정 불필요** (player/enemy 공유 풀 안전 박힘) |

### 2.2. 클라 측 (12건)

| # | 위치 | 박힌 magic/hardcoded | 추정 분류 |
|---|---|---|---|
| C1 | `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs:22~23` | 서버 접속 `serverHost="127.0.0.1"` / `serverPort=7777` | **host 봉합 완료 / port 잔여** (PlayerPrefs는 host만, port는 Inspector/default — β 보정 2026-05-23). M4.2 이관 후보 |
| C2 | `03_Client/Assets/Scripts/UI/MainMenuController.cs:29~31` | 메인메뉴 서버 설정 `serverPort=7777` / `connectTimeoutMs=3000` / `defaultHost="127.0.0.1"` | **host 봉합 완료 / port·timeout 잔여** (성공 시 host만 저장, port/timeout default — β 보정 2026-05-23). M4.2 이관 후보 |
| C3 | `03_Client/Assets/Scripts/Network/ConnectionProbe.cs:33` | probe 타임아웃 `timeoutMs=3000` | **M4.2 이관 후보** (config 화) |
| C4 | `03_Client/Assets/Scripts/Combat/EnemyRegistry.cs:172,181,210~211,222~223` | 적 시각화 스케일·오프셋·HP 바 크기 5건 (Boss/일반 분기) | **M4.3 이관 후보** (Enemy 시각화 asset 자동화) |
| C5 | `03_Client/Assets/Scripts/Combat/EnemyRegistry.cs:220,231` | HP 바 색상 2건 `Color(0.1f, 0.1f, 0.1f, 0.8f)` 등 | **M4.3 이관 후보** (UI 색상 테마화) |
| C6 | `03_Client/Assets/Scripts/Combat/EnemyRegistry.cs:197,221,232` | `sortingOrder = 2/3/4` 3건 | **M4.2 이관 후보** (SortingLayer enum/registry) |
| C7 | `03_Client/Assets/Scripts/Combat/ZoneVisualizer.cs:48~53` | 3-zone 시각 좌표 (centerX/너비/높이/labelY) | **M4.2 이관 후보** (GameMap 맵 스트럭처 sync 의무) |
| C8 | `03_Client/Assets/Scripts/Combat/ZoneVisualizer.cs:49~53` | 3-zone 색상 3건 | **M4.3 이관 후보** (UI 테마·환경 Asset) |
| C9 | `03_Client/Assets/Scripts/Combat/ZoneVisualizer.cs:106,119` | 배경 `sortingOrder = -10 / -9` | **M4.2 이관 후보** (SortingLayer 정의) |
| C10 | `03_Client/Assets/Scripts/Combat/ZoneVisualizer.cs:115,123` | 표지판 `fontSize=6f`, `sizeDelta=Vector2(10f, 3f)` | **M4.3 이관 후보** (UI 폰트 스케일 정책) |
| C11 | `03_Client/Assets/Scripts/UI/StageClearUI.cs:92,109,113,115` | StageClear UI 4건 `sortingOrder=1000` / `sizeDelta=(900,200)` / `fontSize=96f` / `color=(1f,0.92f,0.2f,1f)` | **M4.3 이관 후보** (UI 레이아웃·스타일 통합) |
| C12 | `03_Client/Assets/Prefabs/Characters/LocalPlayer.prefab` (M3.8 박힘) | hardcoded BoxCollider/Rigidbody2D 값 | **서버 hitbox와 별개** (서버 권위 hitbox 신뢰 X, Phase 03 서버 측 AABB 박음 — β 보정 2026-05-23). Phase 03 정합 |

### 2.3. 공유 측 (1건)

| # | 위치 | 박힌 값 | 추정 분류 |
|---|---|---|---|
| Sh1 | `98_Shared/GameData/Constants.cs:13~36` | 게임 전역 상수 6건 (`ServerTickRate=20` / `TickIntervalMs=50` / `TickDuration=0.05f` / `MoveSpeed=5.0f` / `SnapshotTickInterval=2` / `MaxPacketSize=4096`) | **설계 의도 박힘** (Phase 04/05 명시 주석, ARCHITECTURE.md 정합) — 수정 불필요. tuning/table 후보 (runtime config X — β 보정 2026-05-23) |
| Sh2 | `98_Shared/GameData/Physics.cs` (β 추가 발견) | Gravity / JumpSpeed / GroundY 등 shared gameplay constants | **설계 의도 박힘** (Sh1과 같은 계열). tuning/table 후보 |

### 2.4. 도구 측 (2건)

| # | 위치 | 박힌 값 | 추정 분류 |
|---|---|---|---|
| T1 | `99_Tools/headless-bot/Program.cs:15~16` | 봇 default `host="127.0.0.1"` / `port=7777` | **수정 불필요** (CLI 인자 우선, default fallback 안전) |
| T2 | `99_Tools/headless-bot/Program.cs:113,120` | 봇 타임아웃 `TimeSpan.FromSeconds(5/2)` | **M4+ 이관 후보** (봇 설정 외화) |

---

## 3. α 종합 (Codex β 입력 자료)

- **총 발견**: 22건
- **M4.1 Phase 02·03 영역 (즉시 봉합 후보)**: 4건 (S2/S3/S4/S5 일부)
- **M3.8 봉합 완료 (재발견)**: 2건 (C1/C2 — PlayerPrefs fallback 박힘)
- **M4.2 이관 후보**: 6건 (S1/C3/C6/C7/C9 + S5 일부)
- **M4.3 이관 후보**: 6건 (C4/C5/C8/C10/C11)
- **M5+ 이관 후보**: 2건 (S6/T2)
- **수정 불필요 (설계 의도 박힘)**: 3건 (S7/Sh1/C12/T1)

**plan 갱신 트리거 판정** (Phase 01 정의 임계):
- 0~3건 = plan 변경 X
- 4~7건 = M4.1 Phase 0X 추가 검토
- 8건+ = plan 재구성 필요

**Claude α 판정**: 22건 = 8건+ 임계 초과지만 **대부분 plan 이미 박힌 영역** (M4.2/M4.3 = PRD 표 박힘 + Phase 02 PlayerStats 흡수 = plan 박힘). 진짜 *예상 못한* 발견 = 0건. **plan 변경 트리거 X 권유** (M4.1 plan 그대로 진행 + 발견 결과 M4.2/M4.3에 backlog 박음).

---

## 4. Codex β 자문 가닥 (본인 호출 시 prompt에 박음)

### 4.1. 핵심 자문 질문

1. **본 α 발견 22건 중 누락된 *예상 못한* 하드코딩 있나?** (특히 ARCHITECTURE.md M4 사전 과제 8건 *외* 영역)
2. **α 분류가 정합한가?** 특히:
   - M3.8 봉합 완료 표기 (C1/C2)가 진짜 봉합 완료인가, 잔재 있나?
   - 설계 의도 박힘 (Sh1)이 진짜 *설계 의도*인가, hardcoded 변종인가?
3. **M4.1 plan 변경 트리거 판정**: Claude α는 "X 권유"라 박았는데 외부 시각으로도 정합인가?
4. **Phase 03 hitbox AABB vs capsule trade-off 의견** — α는 "응급 우선 AABB 추천" 박혔음. capsule 권장 사유 박을 가닥 있나? 점프 정합 영역 비용 ↑이라 *M4.3 backlog 권장*과 일치하나?

### 4.2. 본인이 별 세션에 던질 Codex 호출 명령어

**옵션 A — `codex exec` (자유 prompt, sandbox 옵션 박음)**:

```bash
codex exec --sandbox read-only --cd "C:\Dev\ClaudeDev" "@00_Document/reviews/2026-05-23-pre-m4-cross-review-claude.md 보고 M4.1 Phase 01 Codex β 크로스 리뷰 수행. 본 파일이 Claude α 결과 + 자문 가닥. 다음 4건 점검해줘: (1) α 발견 22건 중 누락된 *예상 못한* 하드코딩 있나 (ARCHITECTURE.md M4 사전 과제 8건 *외* 영역), (2) α 분류 정합 (특히 M3.8 봉합 완료 표기 + 설계 의도 박힘이 진짜인가), (3) plan 변경 트리거 판정 (α는 'X' 권유, 외부 시각도 정합인가), (4) Phase 03 hitbox AABB vs capsule trade-off 의견. 결과를 00_Document/reviews/2026-05-23-pre-m4-cross-review-codex.md 형식으로 박아줘. 발견 4 분류 표 (즉시 봉합 / M4.2 이관 / M4.3 이관 / M5+ 또는 별 시점) 박는 게 의무."
```

**옵션 B — `codex review --base main` (PR 머지 전 main 대비 변경분 검토)**:

```bash
# 본 옵션은 본 작업 정신과 *불일치* (본 작업 = 전수조사이지 변경분 검토 X)
# main 대비 본 브랜치 변경분 = M4.1 plan 갱신 2 commit만 (Codex β 검토 가치 X)
# → 옵션 A 권장
```

### 4.3. 결과 박을 형식 (`codex.md` 권장 구조)

```markdown
# Cross-Review by Codex β — 2026-05-23 — M4.1 Phase 01

## 1. α 발견 22건 점검
- 정합 N건 / 보완 N건 / 누락 N건

## 2. β만 잡은 *예상 못한* 발견 (있으면 박음)
- 위치 | 박힌 magic/hardcoded | 분류

## 3. plan 변경 트리거 판정 (β 시각)
- 동의 (plan 변경 X) / 이의 (plan 변경 필요, 사유)

## 4. Phase 03 hitbox AABB vs capsule trade-off 의견
- AABB 권장 / capsule 권장 / 사유 + 비용 평가

## 5. 종합 4 분류 표 (α + β)
- 즉시 봉합 (M4.1 Phase 02·03 흡수): N건
- M4.2 이관: N건
- M4.3 이관: N건
- M5+ 또는 별 시점: N건
```

---

## 5. 다음 액션 (본 파일 박힌 후)

1. **본인** = 위 옵션 A 명령어 별 세션 터미널 호출 → Codex 결과 `codex.md` 박음
2. **Claude (본 세션 복귀)** = γ 비교 (α vs β 교집합/차집합) + 4 분류 표 통합 박음 + plan 갱신 결정 → Phase 02 진입 또는 별 Phase 신설 결정

---

## 갱신 이력

- 2026-05-23: M4.1 Phase 01 1단계 Claude α 자체 점검 결과 박힘 (22건 발견, plan 변경 트리거 X 권유). Codex β 입력 자료 + 자문 가닥 + 본인 호출 명령어 박힘.
- 2026-05-23: β 결과 받은 후 α 분류 보정 4건 흡수 — C1/C2 (host 봉합 완료 / port·timeout 잔여 M4.2 이관) + Sh1 (tuning/table 후보) + Sh2 (Physics.cs 추가 발견) + C12 (서버 hitbox와 별개 표현 정정). β 추가 발견 B1/B2/B3/B4는 γ 비교 산출물 `2026-05-23-cross-review-m4.1-phase01.md` 참조.
