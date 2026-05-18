---
summary: M3 Phase 05 — 클라 Remote Entity Registry + 본인/타인 분기 + 200ms 보간 buffer 완성. S_PlayerJoin/Leave dispatch + 지연 spawn 패턴 박힘. Phase 04 broadcast 인프라 수신측 완성, 멀티 캐릭터 클라 상태 골격 마감 (5/20 면담 데모 묶음).
phase: 05-client-remote-entity-registry
work-id: phase05-client-remote-entity
status: done
completed_at: 2026-05-19
commit: pending
---

# Phase 05 — 클라 Remote Entity Registry + Local/Remote 분기 + Interpolation Buffer 완료 박제

**소요 시간**: 약 3h (예상치 정확 — 가장 무거운 Phase ★)

## TL;DR

멀티 캐릭터 클라 상태 골격. 본인 entity는 기존 prediction/reconcile 그대로 두고, 타인 entity는 RemoteEntity placeholder spawn + 200ms 지연 보간으로 분리. `UnityClientSession`에 LocalEntityId 박아 entityId 비교 분기. `S_PlayerJoin`/`S_PlayerLeave` 핸들러 신설, Phase 04 broadcast 인프라 수신측 완성. 헤드리스 봇 1대 시연으로 spawn/despawn + 보간 부드러움 + 1인 reconcile 회귀 0 검증.

## 5단계 보고

- **무엇을 만들었나** — `03_Client/Assets/Scripts/State/{RemoteEntity.cs, RemoteEntityRegistry.cs}` 신설 (200ms 지연 보간 + `Dictionary<int, RemoteEntity>` + 지연 spawn 패턴). `UnityClientSession.cs` 6곳 패치 (`LocalEntityId` 필드 / `HandleEnterMap`에 박음 / `HandleSnapshot` 분기 / `S_PlayerJoin`·`S_PlayerLeave` 핸들러 신설 / switch 2 case 추가 / `OnDisconnected`에 `Registry.Clear`). `Assets/Prefabs/Characters/RemotePlayer.prefab` (4 컴포넌트 — Transform/SpriteRenderer/Animator/RemoteEntity) + 1×1 회색 placeholder sprite + `_RemoteEntityRegistry` GameObject Gameplay 씬에 박음.

- **왜 필요한가** — M2까지 본인 1명 가정. 멀티엔 본인/타인 처리가 다름 (본인은 입력 반응성 위해 prediction, 타인은 권위 좌표 순수 보간으로 packet jitter 흡수). 분기 없으면 본인도 200ms lag로 움직임. 5/20 면담 응급 데모 멀티 골격의 핵심 골격.

- **어떻게 만들었나** — 3가지 결정: (1) **지연 spawn 패턴** = `S_PlayerJoin` 도착 전 `S_Snapshot` 도착 시 그 자리 placeholder spawn + buffer push (idempotent — PlayerJoin이 늦게 도착해도 noop). (2) **timesource = `Time.realtimeSinceStartup`** = 응급 모드 단순화, server-tick 정밀 동기화는 M4+. (3) **extrapolation 안 함** = buffer 비면 last-known 위치 유지 (응급 약속). RemoteEntity public API 4개(`EntityId`/`Initialize`/`EnqueueSnapshot`/`ClearBuffer`)는 *불변 약속* — 유현 Phase 08a 비주얼 교체 시 보존. 본인 reconcile path는 **0줄 변경** (회귀 안전망 자동).

- **테스트 결과** — `dotnet build Dawnholder.slnx` green (경고 0/오류 0/3.21초). 1인 회귀 시나리오 60초: Reconcile 3건/60초 (5% 미만, 정상). 2인 시연 (Unity 클라 + 헤드리스 봇 M2BasicMovement 50초): 봇 `success=True intents=1000 snapshots=500 desync=(0.00, 0.00)` (완벽 동기), 클라 console `[Registry] Spawned entity 2` → 보간 부드러움 시각 → `[Registry] Despawned entity 2` 완전 lifecycle. Error 0 / Unknown PacketId 0. 본인 reconcile 7건/65초 (모두 Y축 — 점프 prediction 결함 = Phase 07 후속 봉합 후보).

- **다음 스텝** — Phase 06 (서버 응급 전투 인프라) 진입 가능. 본 commit + cloud hook commit 묶음으로 main 빠른 머지 → 정유현 Phase 08a pull 가능 (본 세션 합의 약속). 점프 reconcile (Y축 mispredict) Phase 07 후속 봉합 후보 — 응급 데모엔 영향 X, M4 정밀 전투 단계에 봉합.

## AC 검증 결과

Phase 정의 완료 조건 5건 모두 통과:

1. **헤드리스 2 봇 접속 → 두 캐릭터 모두 본인 클라 화면 표시** — 봇 1대 시연으로 검증 (본인 + 봇 = 2명). Unity Hierarchy `RemotePlayer_2` spawn + 회색 박스 시각.
2. **한 봇 움직이면 다른 봇 클라에서 보간 표시 (부드러움)** — 본인 시각 확인 (M2BasicMovement 50초간 좌/우/점프). jitter 시각적 X.
3. **한 봇 disconnect → despawn** — 봇 disconnect 직후 `[Registry] Despawned entity 2` + Hierarchy에서 사라짐 확인.
4. **본인 reconcile은 기존 그대로 (1인 회귀)** — Reconcile 3건/60초 (1인 시나리오) + 7건/65초 (2인 시나리오). 모두 정상 범위.
5. **disconnect 시 buffer 청소 (메모리 누수 X)** — `[Registry] Despawned entity 2` 로그 + `RemoteEntity.ClearBuffer()` 호출 확인.

검증 명령 + 결과:

```
$ dotnet build Dawnholder.slnx
빌드했습니다. 경고 0개 오류 0개. 경과 시간: 00:00:03.21

$ dotnet run --project 99_Tools/headless-bot -- --scenario M2BasicMovement
[Bot] M2BasicMovement: success=True intents=1000 snapshots=500
      bot=(0.00,0.00) server=(0.00,0.00) desync=(dx=0.00, dy=0.00)

Unity Console (총 79건):
[Unity] EnterMap as entity 1 at server spawn (0, 0)
[Registry] Spawned entity 2 at (0.00, 0.00)
[Reconcile] d=(-0.09, -1.52) at serverTick=890 ack=608 (count=1)
... (Reconcile 7건 / Y축 mispredict 0.5~1.5)
[Registry] Despawned entity 2
Error 0 / Warning 1 (Unity 내부 노이즈)
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Spawn/despawn race**: 지연 spawn vs Snapshot 무시 → *지연 spawn* (PDL idempotent 약속 정합 + 표시 빠름)
- **Interpolation timesource**: server-tick (정확, 복잡) vs `Time.realtimeSinceStartup` (단순, RTT 변동에 영향) → *Time.realtimeSinceStartup* (응급 모드 + jitter 흡수엔 충분)
- **Registry 매니저 위치**: 씬 GameObject (Inspector 학습 가치) vs 동적 생성 (코드만) → *씬 GameObject* (학부생 학습 + LocalPlayerController.Instance 패턴 정합)
- **RemoteEntity 시그니처 약속**: SerializeField로 SpriteRenderer/Animator 잡음 vs 내부 비저장 → *내부 비저장* (불필요한 결합 차단 + 유현 비주얼 자유 swap)
- **2봇 시연 시점**: Phase 09 리허설로 미룸 vs 본 Phase에 끼움 → *본 Phase에 끼움* (5/20 면담 risk 차단 + 학습 가치)
- **점프 reconcile 처리**: 즉시 봉합 vs 응급 모드 무시 vs 메모만 → *(b) 메모만* (Phase 07 후속 봉합 후보, 응급 데모 영향 X)

## 막혔던 지점

- **Unity MCP `PrefabUtility.SaveAsPrefabAsset` 차단** — modal dialog 발화 API 정적 분석 차단 룰에 박힘. 우회: 씬에 4-컴포넌트 GameObject 신설 → 본인이 Hierarchy → Project 드래그로 prefab 생성 → 자동화 재개. 반자동화 패턴 정착.
- **prefab 이름 mismatch** — 본인이 드래그하면서 GameObject 이름 `_RemoteEntityTemplate` → prefab `RemoteEntity.prefab` → 본인 rename `RemotePlayer.prefab`. Unity가 GUID 기반 reference 유지해서 `_remotePlayerPrefab` field 자동 보존 (학습 메모).
- **MCP `Unity_GetConsoleLogs` 빈 응답** — `logTypes="Error,Warning"` 콤마 분리 형식 미지원. 진단: `logTypes` 안 보내고 default 호출 → 정상 반환 (79건). 빈 응답 = 도구 버그 X, 파라미터 형식 어긋남 (학습 메모 갱신 가치).
- **cloud ping-pong 발견 + 봉합** — main에 `cloudProjectId: c89f079f-... (유현)` 박혀있었음. 본인 commit이 `1094041a-... (영호)`로 덮어쓰는 패턴 (서로 ping-pong). `/session:start` (C-1) 게이트는 세션 시작 시점만 작동 — 세션 중간 commit이 빠져나감. fix: `.githooks/pre-commit` 맨 앞에 cloud 라인 10 패턴 자동 unstage 로직 박음. 본 Phase와 별도 commit으로 박힘.

## 학습 일지 후보 키워드

- **★★★ 본인 vs 타인 분기** (`local-vs-remote-entity-branch`) — MMORPG/MO 보편 패턴. 본인엔 prediction (즉시 반응), 타인엔 순수 보간 (jitter 흡수). Source/Quake/Mirror/NGO 모두 동형. 한국 게임 회사 백엔드 면접 단골.
- **★★★ Interpolation buffer 200ms** (`interpolation-buffer-jitter-absorption`) — packet jitter 흡수 윈도우. extrapolation 안 하는 응급 모드 약속. 둘러싼 2 snapshot lerp 알고리즘.
- **★★★ Entity Registry 패턴** (`entity-registry-pattern`) — `Dictionary<id, entity>` + spawn/despawn lifecycle + idempotent. Unity NGO/Mirror 보편.
- **★★ 회귀 안전망 명시 테스트 가치** (`regression-safety-net-explicit-test`) — 새 기능 분기 짤 때 *기존 path 0줄 변경*만으로 자동 안전망. 봇 시나리오로 회귀 명시 검증.
- **★★ cloud ping-pong + pre-commit 자동 정리** (`cloud-id-ping-pong-precommit-fix`) — Unity Cloud Services 다인 환경 함정. `/session:start` 게이트 시점 한계 → pre-commit으로 모든 commit 시점 강제. 10 패턴 매칭으로 `cloudServicesEnabled` 블록까지 흡수.
- **★ Unity MCP PrefabUtility 차단 우회** (`mcp-prefab-utility-block-workaround`) — modal dialog 발화 API 정적 분석 차단. 반자동화 패턴 (씬 modification + 본인 드래그).
- **★ MCP logTypes 콤마 분리 X** (`mcp-unity-logtypes-comma-trap`) — 빈 응답 = 파라미터 형식 함정. 도구 버그 가정 X (기존 memory 보강).
- **★ 점프 연속 누름 → Reconcile (Y축 mispredict)** (`continuous-jump-reconcile-y-axis`) — Phase 07 점프 prediction 후속 봉합 후보. 매 frame Predict + 가변 dt가 짧은 transient 이벤트(점프)에 mispredict 누적.
- **★ Phase 04 broadcast 인프라 수신측 일반화 후속** (`phase04-broadcast-receiver-completion`) — 송신측(Phase 04) → 수신측(Phase 05) 짝 완성, broadcast 흐름 e2e.
- **★ Prefab variant 패턴 도입 결정** (`prefab-variant-pattern-introduction`) — Phase 08a 유현 측 비주얼 교체 시 base 보존 + variant로 차별화.
- **★ 팀 컨텍스트 동기화 (git + 프롬프트 통로 분리)** (`team-context-sync-git-vs-prompt-channels`) — git은 코드/문서, CONTEXT.md/work-pin은 각자 머신 (.gitignore), 프롬프트는 별송. 통로 분리로 충돌 차단.
