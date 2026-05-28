---
owner: youngho
milestone: M4.2
phase: 04
title: 클라 4 scene dispatch + portal UX
status: done
grade: 복잡
summary: 서버 S_MapTransition 수신 시 Unity scene 전환 + portal UX 완성. 3맵(Town/HuntingGround/BossRoom) 외관(본인 직접) + Portal prefab 통일(AI MCP) + Ending=게임끝 UI 처리. Play 검증에서 발견한 (1)카메라 미추적 (2)맵 전환 직후 spawn snap 튐을 코드 봉합. reviewer Tier 2-A 통과(🔴0/🟡2). 평상시 reconcile drift는 M4.3 이월. commit a59cb1b(코드)+cc532d5(외관+봉합).
---

# Phase 04 — 클라 4 scene dispatch + portal UX (완료 박제)

**마감 일자**: 2026-05-28
**등급**: 복잡 (client + unity-asset 위험 깃발)
**담당**: 본인 직접(Unity scene/portal 외관) + AI(네트워크 wiring + MCP 구조물 + Play 검증 봉합)

---

## TL;DR

서버 권위 맵 전환(S_MapTransition)을 클라가 scene 전환으로 렌더링하고, portal 트리거로 C_EnterPortal 의도를 보내는 흐름을 완성했다(헌법 #1 — 클라는 "도착 통보"를 받고서야 전환, 자체 판정 X). 3개 게임플레이 맵 외관을 입히고 portal을 prefab으로 통일했으며, Ending은 지형 없는 "게임 끝" UI로 처리했다. **Play 검증이 코드 단위 테스트로는 안 잡히는 두 결함을 드러냈다**: (1) 동적 spawn된 LocalPlayer를 씬 카메라가 못 따라감(CameraFollow.target 미연결), (2) 맵 전환 직후 옛 LocalPlayer가 페이드 동안 살아서 서버 좌표로 reconcile snap → 2 unit 튐. 둘 다 "생명주기/연결 시점" 문제로 봉합했다.

---

## AC 검증 결과

Phase 04 완료조건(정의 파일 `## ✅ 완료 조건`) 대조 — Unity Play 모드 수동 검증 + MCP 콘솔 확인:

| 완료조건 | 결과 | 검증 방법/증거 |
|---|---|---|
| portal 밟으면 scene 전환 + spawn 좌표 배치 | ✅ | Play 모드: Town(x=20)→HuntingGround(x=25)→BossRoom(x=35)→Ending 전환 확인. 서버 PortalTable 좌표 정합. |
| 맵 이동 후 enemy가 해당 맵에서 보임 | ✅ | Play 모드 육안 — 사냥터/보스방 enemy spawn 확인 (본인 검증). ⚠️ 단 GameSession/GameMap이 spawn 기능을 자체 보유 = 종속성 과다 → 개선 backlog (아래 학습 키워드). |
| 맵 전환 후 prediction/reconcile 정상 (떨림/순간이동 없음) | ✅(맵 전환 한정) | 맵 전환 직후 spawn snap 튐(d=2)을 ResetPredictionForMapTransition 봉합으로 제거. MCP ReadConsole [Reconcile] 로그로 d=2 ack=0 소멸 확인 흐름. 단 *평상시 이동 중* reconcile drift(d≈±1.5)는 클라 가변 dt vs 서버 고정 tick 비대칭 = 별 축 → M4.3 prediction 튜닝 이월. |
| Unity 콘솔 에러 0 (전환 흐름) | ✅ | MCP `Unity_ReadConsole` Error 0건. Warning 9건은 전부 StageClearUI의 LiberationSans SDF 폰트 미할당(맵 전환과 무관 별개 이슈). |
| scene/prefab 편집 전 백업 | ✅ | `.backups/20260525-pre-mcp-scene/` (Phase 08 BackGround prefab 사고 학습 정합). |

**컴파일**: MCP RunCommand 타입 참조 검증 — CameraFollow/LocalPlayerSpawner/LocalPlayerController/UnityClientSession 0 컴파일 에러.
**reviewer**: Tier 2-A 통과 (🔴0 / 🟡2 — 주석 stale 4곳=처리 완료 / Spawner의 Rendering 의존=관찰 포인트).

---

## 결정 흐름

1. **동적 spawn 채택 (B안)** — LocalPlayer를 씬에 pre-place하지 않고 RemoteEntity처럼 sceneLoaded에서 Instantiate(LocalPlayerSpawner). 사유: 맵 4개 × 씬 YAML 편집 폭탄 회피 + RemoteEntity 패턴 일관성. ADR-027(DontDestroyOnLoad 땜질 제거) 정합. 비용(Instantiate 1개/전환)은 무시 가능.
2. **카메라 연결을 spawn 후 셋업으로** — CameraFollow.target이 [SerializeField]라 동적 spawn 대상을 미리 못 가리킴 → Spawner가 spawn 직후 SetTarget으로 꽂음 + 즉시 snap(첫 LateUpdate lerp 끌림 방지). Rendering→Input 단방향 의존, 현 규모 OK(reviewer 관찰 포인트).
3. **맵 전환 spawn 튐 = 위치 조작이 아니라 생명주기 문제로 재정의** — 옛 ResetPredictionForMapTransition은 predictor를 (0,0)으로 리셋(상태 변경)했는데, 진짜 원인은 "곧 파괴될 옛 LocalPlayer가 페이드 동안 살아서 snapshot을 계속 받는다". 해법을 위치 리셋 → `Instance=null + enabled=false`(이벤트 수신·Update 차단)로 전환. HandleSnapshot의 `Instance != null` 가드가 "입력 받을 자격 창"을 단일 지점에서 강제. + PendingSpawn 소비를 Start→Awake로 옮겨 첫 snapshot race 차단.
4. **Ending = UI 결정** — 지형/플레이어 없이 EndingController "게임 끝" UI만. LocalPlayerSpawner.GameplayScenes에서 Ending 제외. BossRoom 포탈 진입 시 서버는 플레이어를 Ending 맵에 두지만 클라는 UI만 표시 → "메인으로" 클릭 시 Disconnect+MainMenu. 옛 Town↔Ending 루프백 폐기.
5. **reconcile drift는 scope 분리** — 평상시 이동 중 d≈1.5 snap은 prediction 본질(가변 dt vs 고정 tick)이라 M4.2(맵 전환) scope 밖 → M4.3 이월.

---

## 학습 일지 후보 키워드

- **dynamic-spawn-camera-setup**: 런타임 spawn 객체는 씬 Inspector로 참조 연결 불가 → "생성 후 셋업"(Spawner가 SetTarget)이 정석. 즉시 snap으로 첫 프레임 lerp 끌림 방지.
- **lifecycle-not-position-for-transition-jump**: 게임 클라 "튐/점프" 버그는 값을 더 보정하기보다 *그 객체가 아직 입력/이벤트를 받을 자격이 있는가*(authority/ownership 창)를 먼저 의심. 옛 객체는 위치 리셋 말고 이벤트·Update에서 분리(Instance 해제 + enabled=false)가 깔끔.
- **client-server-time-model-asymmetry**: 클라 가변 dt(렌더 Hz) vs 서버 고정 20 TPS tick 누적 오차 → SnapThreshold 근처 reconcile. tickrate "차이"가 아니라 시간 진행 모델 비대칭. M4.3 튜닝 후보.
- **gamesession-gamemap-spawn-coupling** (backlog): GameSession/GameMap이 enemy spawn 기능을 자체 보유 = 종속성 과다. spawn 책임을 별 모듈로 분리 검토 (M4.3 또는 서버 리팩터 ADR 후보).
- **map-editor-data-serialization** (memory `future-map-editor-data-driven-milestone`): portal/spawn 좌표를 서버 코드 + 클라 씬 양쪽 수동 맞춤 = 번거로움 실측. 충돌 타일맵과 한 묶음 미래 마일스톤.

---

## 다음 Phase

- **Phase 05** — 통합 검증 + 봇 맵 이동 시나리오 + smoke 2건 복구 + 마일스톤 마감(+ `_milestone-DONE.md`) + reviewer Tier 2-A 통합 점검 → M4.2 PR.
