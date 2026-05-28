# Pre-Review for Codex β — 2026-05-28 — M4.2 Map Transition 마일스톤 마감

본 문서는 본인(유영호)이 별 세션 터미널에서 Codex CLI β 호출 시 던질 점검 자료. Claude는 *자료 박음*만, Codex 직접 호출 X(2026-05-23 분담 봉합 정합).

---

## 변경 범위

- **브랜치**: `feature/m4.2-map-transition` (main `d751332`에서 분기)
- **commit 수**: 7건 (Phase 01~04) + 본 Phase 05 마감 commit 미커밋
- **diff 통계**: **104 파일 / +7378 / -651** (main 대비)
- **등급**: **대규모** (3+ 도메인 = server + shared + client + qa, 300줄+ 비가역)
- **위험 깃발**: `irreversible` (ProtocolVersion 5→6 bump @ Phase 02) + `trust-boundary` (Phase 02·03) + `unity-asset` (Phase 04)

## Commit 목록 (origin/main 대비)

```
dad760b feat(server): M4.2 Phase 01 맵 레지스트리 + MapId enum + Spawn 모듈화
8363425 feat(server,shared): M4.2 Phase 02 portal 패킷 + entity id 전역 풀 (ADR-026)
41f224d feat(server): M4.2 Phase 03 맵 간 player migration + 동시성 보강
8c11e91 docs: AI 조율자 케이스 스터디 — 동시성 설계 결정 과정 (M4.2 Phase 03)
a59cb1b wip(client): M4.2 Phase 04 맵 전환 + ADR-027 Bootstrap 구조 (Play 검증 전 체크포인트)
cc532d5 feat(client): M4.2 Phase 04 맵 외관 + 전환 Play 검증 봉합
8ccedd8 docs(phase): M4.2 Phase 04 -DONE.md 박제
(미커밋) Phase 05 마감 + ARCHITECTURE 갱신 + _milestone-DONE.md + CHANGELOG
```

## main 대비 diff 요약 (자연어)

1. **server (02_Server/)**: GameWorld 단일 맵 → `Dictionary<MapId, GameMap>` 레지스트리, MapId enum 4맵, MapSpawnTable/PortalTable 단일 진실 공급원, `SubmitEnterPortal` 4단 trust-boundary 검증 + 맵 간 player migration(`_migrating` Volatile, race window 4종 결정론 봉합).
2. **shared (98_Shared/)**: PDL `C_EnterPortal`(ID 17) + `S_MapTransition`(ID 18) append-only, ProtocolVersion 5→6 bump, GenPackets.cs 재생성, Shared.dll 동반 commit.
3. **client (03_Client/)**: ADR-027 PersistentServicesBootstrap + NetworkService + GameEntryPoint + LocalPlayerSpawner(동적 spawn) + PortalTrigger + 4 scene 외관(Town/HG/BossRoom/Ending). Play 검증 봉합 3건(카메라/spawn 튐/첫 snapshot race).
4. **qa (99_Tools/headless-bot, 02_Server/GameServer.Tests/)**: 봇 portal 이동 흐름(CombatProbe/BossProbe), 새 MapTransitionScenario, 신규 통합 테스트(MapTransitionIntegrationTests), 옛 Skip smoke 2건 복구, 단위 테스트 79건 추가(M4.1 baseline 221 → 300).
5. **docs (00_Document/)**: ADR-026 entity id 전역 풀 + ADR-027 client bootstrap + AI 조율자 동시성 케이스 스터디 HTML + ARCHITECTURE.md M4.2 결과 종합 절 + CHANGELOG [M] entry.

## α (Claude reviewer SubAgent Tier 2-A) 결과 요약

본 마감 박제 직전 호출 결과 (2026-05-28):

- 🔴 위반 **0건**
- 🟡 개선 제안 **3건**:
  - 🟡 1: `00_Document/ARCHITECTURE.md` L212 ProtocolVersion 본문 v5 stale (실제 v6) — *역방향* false-promise 재발 패턴(M4.1 동일). **→ 본 마감 commit 동반 봉합 완료**
  - 🟡 2: `ARCHITECTURE.md`에 `## M4.2 결과 종합` 절 누락 (M3 절 패턴 정합 미박힘). **→ 본 마감 commit 동반 봉합 완료**(M4.2 결과 종합 절 신설)
  - 🟡 3: 봇 portal 좌표 const 3 파일 중복 (`MapTransitionScenario` / `EmergencyCombatSmoke` / `BossStageClearSmoke`의 `TownPortalX=20f` 등) — 서버 `PortalTable.cs`와 drift 위험 3 곳으로 확장. **→ M4.3 backlog** (즉시 사고 없음, 동기화 의무 주석 박음)
- 🟢 5축 PASS:
  - 헌법 §1 (Server Authority): portal 목적지/spawn 100% 서버 권위, `C_EnterPortal`에 portalId만
  - 헌법 §2 (Protocol Sacred): PDL append-only, PacketID 17/18 stable, Shared.dll 동반 commit
  - 헌법 §3 (Trust Boundary): `SubmitEnterPortal` 4단 검증(handshake/class/portalId 범위/portal 근접 ≤ 2 unit), invalid silent drop
  - 헌법 §4 (Shared Code Discipline): 양쪽 빌드 통과(dotnet test 300 PASS), Shared.dll PostBuild 자동 복사
  - 헌법 §5 (No Blocking): migration 로직 EnqueueJob 람다 안 동기 코드, await/Task.Delay/Thread.Sleep 0건
  - ADR-026/027 정합 + PDL bump 후속 의무 3종(PacketGenerator 재생성 + Shared.dll + 동반 commit)
  - 테스트 커버리지: happy/invalid/auth 일관 (EnterPortalHandlerTests 5패턴)
  - race window 4종 결정론 봉합 (TransientDrop / DisconnectDuringMigration / GetDestMap null / broadcast skip)

- 머지 게이트: **조건부 GO → 본 마감 commit으로 GO 격상** (🟡 1·2 동반 봉합 완료, 🟡 3 backlog)

## Codex β 점검 가닥 (본인이 별 세션에서 검토 시 참고)

본인이 직접 던질 prompt 박을 때 *어디를 봐야 하나* 가이드. α가 놓칠 가능성 있는 영역 우선:

### 1. 헌법 §1~§5 위반 (특히 trust-boundary)
- `02_Server/GameServer/Network/GameSession.cs`의 `SubmitEnterPortal` 4단 검증 — 범위 검증 누락된 portalId 케이스가 있나? (예: portalId 음수 / int overflow / 같은 맵 내 unique지만 다른 맵 portalId와 충돌 시점)
- `02_Server/GameServer/Maps/GameMap.cs`의 RemovePlayer/AddPlayerWithId — entity id 재사용 가능성 / 같은 맵에 두 번 add되는 race?
- `02_Server/GameServer/Handlers/EnterPortalHandler.cs` 게이트 — handshake/class 선결 검증 누락 케이스?

### 2. M3 응급 하드코딩 잔존 패턴
- MapSpawnTable/PortalTable 신설로 흩어진 const는 통합됐는데, GameMap 어딘가 남은 const 좌표는?
- 4 scene 외관 코드에 직접 박힌 portal 좌표 / spawn 좌표가 있나?

### 3. PDL/ProtocolVersion 정합
- PDL.xml의 C_EnterPortal(17) + S_MapTransition(18) ID가 옛 패킷과 충돌 X?
- ProtocolVersion 본문이 v6로 정합 + 변경 이력 정확?
- Generated/GenPackets.cs가 PDL과 정합?

### 4. 옛 사고 패턴 잠복 (false-promise 변종)
- ARCHITECTURE.md M3 결과 종합 절은 이미 박혔는데, 본 마감 commit으로 추가된 M4.2 결과 종합 절이 코드 실재와 정확히 정합?
- 02_Server/CLAUDE.md, 04_ClientNet/CLAUDE.md 같은 도메인 가이드에 M4.2 변화 미반영 stale은? (옛 M4.1 패턴 재발 후보)
- ADR-026/027의 약속 vs 코드 실재 정합?

### 5. 동시성 / race window 봉합 정합
- `_migrating` Volatile 단일 writer 가정 — 실제 single writer인가? 두 곳 이상에서 set되는 경로 있나?
- 케이스 스터디 HTML (`00_Document/case-studies/2026-05-25-ai-orchestrator-concurrency.html`)이 실제 코드와 정합?
- `MapMigrationTests` 13건이 race window 4종을 정확히 커버하나?

### 6. 클라 측 ADR-027 정합
- `PersistentServicesBootstrap` 단일 spawn invariant — `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`가 두 번 호출되는 케이스?
- `LocalPlayerSpawner` GameplayScenes에서 Ending 제외 — 다른 씬 추가 시 깨질 패턴?
- `LocalPlayerController.ResetPredictionForMapTransition`의 enabled=false 봉합 — 새 LocalPlayer가 활성화될 때 race 가능?

### 7. 테스트 회귀
- 300통과/0실패/4Skip — Skip 4건이 모두 정당한 LongRunning 사유? 가짜 Skip은 없나?
- M4.1 baseline 221 → +79 신규 — 신규가 의미 있는 invariant 검증인가, 표면 검증만인가?

---

## 본인 Codex 호출 명령어 (별 세션 터미널)

권장 가닥:

```bash
# (A) 권장 — main 대비 전체 마일스톤 변경분 (104 파일 / +7378 / -651)
#     Codex가 commit 7건 + 본 마감 stage된 변경 함께 검토.
#     소요 시간 ↑↑ (커밋 후 호출 권장)
codex review --base main
```

```bash
# (B) 미커밋 본 마감 변경만 (10 파일, ARCHITECTURE 정정 + _milestone-DONE.md + 봇/테스트)
#     본 마감 commit 박기 *전* Codex β 점검 시 권장
codex review --uncommitted
```

```bash
# (C) 특정 Phase commit만 (예: Phase 03 동시성 핵심부분)
codex review --commit 41f224d
codex review --commit 8363425   # Phase 02 PDL bump
```

**입력 자료**: 본 pre-review MD 본인이 첨부 또는 prompt 본문에 박음 (Codex가 점검 가닥을 알도록).

**sandbox 옵션 결함 학습 정합** (memory `codex-sandbox-permission-current-dir`): exec 모드는 sandbox 옵션 박지만, **review 모드는 sandbox 옵션 없음** (false-promise 23번째 변종 발본 학습). 위 명령어 그대로 OK.

---

## 본인 응답 형식 (Claude 대기)

본인이 Codex 호출 후 응답 시 다음 셋 중 하나:

- **(A) "Codex 결과 첨부"** — raw 출력 또는 요약 던지면 → Claude γ 비교 진행 + `2026-05-28-cross-review-m4.2-map-transition.md` 산출물 박음
- **(B) "β 스킵"** — Codex 환경 부재 또는 시간 부족 → α 단독 진행 + 산출물에 *β 미발동* 명시
- **(C) "Codex가 봉합 박음"** — 본인이 Codex 직접 봉합한 경우, diff 보여주면 Claude γ 비교 + 후속 처리

---

## γ 비교 시 핵심 신호 (Claude 후속 처리 기준)

| 시그널 | 해석 | 권장 액션 |
|---|---|---|
| α만 잡음 | 헌법 위반인데 동작은 함 (코드 시각) | reviewer 신뢰 — 봉합 검토 |
| β만 잡음 | 동작은 깨졌는데 헌법 정합 (검증 시각) | Codex 신뢰 — 봉합 검토 |
| **양쪽 다 잡음** | **명확한 위반** | **최우선 봉합** |
| 양쪽 다 통과 | 안심 신호 (단 체크리스트 한계 명심) | 진행 GO |

---

## 옛 학습 정합

- **γ 방식 정착 (5/18 ~ 5/23)**: pre-M3 / Phase 02 / Phase 03~04 / M3.6 plan / M4.1 plan 등 8회차 누적. M4.1 마감 GO 격상 사례(γ 8회차) 학습 정합.
- **본 M4.2 = 9회차 γ** (정착 후 첫 *대규모 마일스톤 마감* 적용).
- **옛 사고 패턴 잠복 의심**: M4.1 마감 직후 ARCHITECTURE stale false-promise 발견 → M4.2 마감 시점에 reviewer 사전 봉합으로 *예방적* 작동. Codex β 시각이 추가 stale을 잡는지 확인 가치.
