---
owner: youngho
milestone: M5
phase: milestone-closeout
title: 파티·퀘스트·보스 포탈 잠금 + 콘텐츠 결선 — 마일스톤 마감
status: done
grade: 대규모
summary: 파티 시스템 + 40킬 공유 퀘스트 + 보스 포탈 잠금 게이트(서버 권위) + 양방향 포탈 + 일반몹 공격 + 이펙트/StageClear 결선. ProtocolVersion v14→v15. 무인 완주(영호 취침 중 자율, 멈춤 0). 코드 Phase 21개 구현·검증·커밋(WSL2 643/0/5, reviewer 🔴0, Unity 컴파일 0err). 순수 씬 배치 2개(B3·C4) = 아침 육안. 다음 = push/PR/디스코드 v15공지(영호 GO) + Play-test.
---

# M5 마일스톤 마감 — 파티·퀘스트·보스 포탈 + 콘텐츠 결선

**마감 일자**: 2026-06-15 (무인 완주 — 영호 취침 중 자율)
**Phase 수**: 24 (코드 21 완주 + 순수 씬배치 2[B3·C4] 아침육안 + R3 마감)
**등급**: 대규모 (3+ 도메인 + 비가역 v15 bump + 300줄+)
**브랜치**: `feature/m5-party-quest` (origin/main +20 커밋, 104 파일, +8741/-126)

---

## TL;DR

플레이 동선에서 빠졌던 7종을 한 마일스톤으로 묶어 구현: **파티(초대/수락, 정원2, cross-map) + 40킬 공유 퀘스트 + 보스 포탈 잠금 게이트(서버 권위, ghost 방지, fail-closed) + 양방향 포탈(겹침+위키) + 일반몹 공격 + 피격/찌르기 이펙트 결선 + StageClear 애니**. 프로젝트 첫 *플레이어 간 협동 시스템*. 비가역 `ProtocolVersion v14→v15`(8패킷 일괄). 코드 Phase 21개 전부 구현·검증·커밋(멈춤 0). 순수 Unity 씬 배치 2개(B3 역방향포탈·C4 NPC)는 씬 손상 위험 + 영호 시각영역이라 아침 육안. **push/PR/디스코드 v15공지 = 영호 GO 게이트.**

---

## AC 검증 결과

- **WSL2 회귀(ADR-029)**: `~/.dotnet/dotnet build Dawnholder.slnx` → **0 error / 0 warning**. `dotnet test --no-build` → **Passed! Failed: 0, Passed: 643, Skipped: 5, Total: 648** (M4.15 baseline 612 → +31 신규).
- **신규 테스트(서버/봇)**: HandleEnemyDeathKillerTests(4) / QuestKillCountTests(7) / BossPortalGateTests(7) / EnemyAttackTests(8) / PortalTableTests(+2 역방향) / Party 단위(A1~A4) / BossGateSmokeTests(2) / PartyQuestSmokeTests(1).
- **reviewer(Opus) Tier 2-A**: trust-boundary Phase(A3·A4·Q3) + 데미지 §1 민감(C1) 전부 **🔴 0**. Q3 fail-closed(`?? 0`) 칭찬. C1 보스 회귀 0(헬퍼 추출 byte-identical, 보스 테스트 가드).
- **Unity 컴파일(클라 9 Phase)**: P1·P2·P3·P4·P5·B2·C2/C3·C5 전부 MCP `ReadConsole "error CS"` = **0 error**.
- **flaky 1건**: 통합테스트 컬렉션(공유 ServerFixture) 간헐 1실패 관측 → 재실행 643/0/5 green. 봇 e2e 격리 통과 확인(코드버그 아님).

---

## 결정 흐름

1. **PartyRegistry = GameWorld 소유 actor**(맵/세션 아님): 파티는 cross-map 유지라 특정 맵에 못 둠. EnqueueJob+Tick 드레인, lock 금지. 멤버=entityId(session 참조 X — disconnect race 회피, ADR-026). vs 맵 소유(❌ cross-map 깨짐) / lock dict(❌ 헌법 위반).
2. **보스 게이트 = MapMigration 검증단계(transfer 전)**: RemovePlayer 전 차단 → ghost 0. killCount=서버 권위, entityId=서버 _entityId, fail-closed(null→거부). vs transfer 후 검증(❌ ghost).
3. **QuestConstants=40 서버측 배치**(98_Shared 아님): targetCount를 wire로 보내 진짜 SSOT=패킷, 클라 하드코딩 0, co-review 회피로 야간 헤드리스 유지. vs 98_Shared(❌ 불필요 Shared.dll co-review).
4. **C1 ApplyMeleeDamage 헬퍼**: 보스/일반몹 공통 데미지 경로 추출(byte-identical). EnemyStats.Attack(5/8) 기존 재사용 → 98_Shared 무변경, 순수 서버, S_EnemyAttack 재사용(신규패킷 0). vs 복붙(❌) / 새 패킷(❌ v15 충돌).
5. **봇 e2e = in-process 시드 하이브리드**: 40킬 그라인드는 리스폰 5초로 60~90s 느림/flaky. 게이트/공유전달 e2e는 ServerFixture 시드(killCount 충족), 킬→카운트 wiring은 Q2 xUnit. vs 실 40킬 그라인드(❌ flaky/타임아웃). 봇 근접전투 finicky → R1 시드 재설계.
6. **swap-ready 배선**: 이펙트/StageClear/HUD 배경 = Resources 경로/SerializeField 슬롯 분리 → 아침 에셋 드롭 = 코드 0. 미배치 시 graceful 폴백(무연출 회귀 0).
7. **B3·C4 야간 미실시**: 순수 씬 YAML 편집은 취침 중 손상 시 대참사 + 시각 배치는 영호 영역(메모리) + Unity 육안=아침 게이트. 코드/데이터/로직은 완비 — 배치만 아침.

---

## 5단계 보고

### 🎯 무엇을 만들었나

파티 시스템(A0~A4) / 40킬 공유 퀘스트 + S_QuestUpdate(Q1~Q2) / 보스 포탈 잠금 게이트(Q3) / 양방향 포탈 데이터+진입(B1·B2) / 일반몹 공격(C1) / 이펙트 kind 라우팅·StageClear 애니(C2/C3·C5) / 클라 파티·퀘스트 UI(P1~P5) / 회귀 봇(R1·R2). 신규 8패킷 + v15.

### 🤔 왜 필요한가

영호 Play-test 동선에서 빠진 콘텐츠 7종. 핵심은 **파티+퀘스트+게이트** — 프로젝트 첫 *플레이어 간 협동 시스템*. 신규 프로토콜(비가역 v15)이 걸려 cross-map thread 안전 + 신뢰경계(게이트) 설계가 마일스톤의 무게중심. 나머지(이펙트/NPC/StageClear)는 wiring/visual.

### 🛠️ 어떻게 만들었나

트랙 A(파티 서버 actor) → Q(퀘스트+게이트, killer 전파 seam → 공유 카운트 → transfer-전 게이트) → B(포탈 데이터+클라 입력) → C(일반몹 공격 헬퍼 추출+이펙트 라우팅) → P(클라 미러+HUD+팝업+토스트) → R(봇 e2e). 무인 파이프: Worker 위임 → 메인 file:line 실측 → Plugins drift 복원 → WSL2 게이트 → (TB) reviewer → commit. 클라는 Unity MCP 컴파일체크.

### 🧪 테스트 결과

WSL2 643/0/5(build 0err/0warn). reviewer TB+민감 Phase 🔴0. Unity 컴파일 클라 9 Phase 0err. 신규 +31 테스트. flaky 1건(공유 ServerFixture)은 재실행 green.

### ➡️ 다음 스텝

**영호 GO 게이트**: push / PR(정유현 co-review + Shared.dll v15) / 디스코드 v15 wire-break 공지. **아침 육안 씬배치**: B3(역방향 포탈 GameObject) / C4(NPC 배치+대사). **swap-ready 에셋 드롭**: slime/golem 이펙트 prefab, 보스 stab, StageClear anim, HUD 배경. **Play-test**: 파티/퀘스트/게이트/포탈/공격/이펙트/NPC 전수 + P2 초대타겟(근접최단)·Q2 카운트전환(결성0/해산소멸) 야간기본 확인.

---

## 학습 일지 후보 키워드

cross-map actor(PartyRegistry) / SendToEntity EnqueueJob 마샬링 / transfer-전 게이트 ghost 방지 / fail-closed 신뢰경계 / SSOT=wire(targetCount) / Extract Method 회귀봉인(보스 헬퍼) / in-process 시드 vs 봇 그라인드 / swap-ready Resources 바인딩 / Unity MCP 컴파일체크 파이프 / 무인 완주 파이프(Worker→실측→WSL2→reviewer→commit).

---

## 후속 작업 — B3·C4 씬 배치 완료 (2026-06-15, 영호 깸 후 MCP)

야간엔 취침 중 씬 손상 catastrophe 위험 + Unity 육안=영호 영역이라 B3·C4를 아침으로 연기했음. **2026-06-15 영호가 깨어 옆에서 즉시 육안 확인 가능한 상태**가 되어 Unity MCP `RunCommand`로 프로그래밍 배치 진행(손 YAML 편집 회피 = Unity가 직렬화 안전 처리). 백업 = git clean tree(편집 전 전부 커밋, 손상 시 `git checkout`으로 복원). 각 편집 후 git diff(삭제 0/헤더 정상) + `Capture2DScene` 육안으로 손상 0 검증.

### B3 — 역방향 포탈 씬 배치 (서버 좌표 = 씬 좌표 1:1 확정)

- **HuntingGround.unity**: `Portal_Reverse_ToTown` @ (5, 0, 0), portalId=2 → Town (서버 `PortalTable` HG 역방향 `Position(5,0)/DestSpawn(17,0)` 정합).
- **BossRoom.unity**: `Portal_Reverse_ToHG` @ (18, 0, 0), portalId=2 → HuntingGround (서버 `Position(18,0)/DestSpawn(22,0)` 정합).
- 둘 다 `Portal.prefab` 인스턴스 → SpriteRenderer(placeholder 시안 sprite) + BoxCollider2D(isTrigger) + PortalTrigger(portalId override) + **swap-ready Animator 슬롯(controller 비어있음)** 부착.
- Town은 역방향 불필요(정방향 x=20만, Ending→Town은 정방향 루프).

### C4 — 마을 NPC 배치 + 대사

- **Town.unity**: `Npc_BlackSmith` @ (13.5, 0, 0), `Npc_Glocery` @ (16.5, 0, 0) — 석상(x=13)과 포탈(x=20) 사이.
- 각 NPC = SpriteRenderer(BlackSmith_Idle_0 / Glocery_Idle_0, **bottom-pivot이라 y=0=발이 잔디에 정확히 닿음**) + BoxCollider2D(isTrigger, 2.2×2.4 offset y=1.2) + Animator(swap-ready) + NpcInteractable(대사 하드코딩, 헌법 §1 = 상태 변경 0).
- 대사: BlackSmith="무기 손질 필요하면 들러…", Glocery="물약·식료품 곧 들어와요…" (상점 기능 0, 대화만 = 확정 결정 1).

### swap 지점 (영호가 진짜 에셋 꽂는 곳 — 코드 변경 0)

| GameObject | 슬롯 | 현재(placeholder) | 진짜 에셋 드롭 |
|---|---|---|---|
| Portal_Reverse_ToTown / _ToHG | SpriteRenderer.Sprite | 시안 반투명 포탈(Portal.prefab 공용) | 진짜 포탈 sprite |
| Portal_Reverse_* | Animator.Controller | 비어있음 | 포탈 빛남 AnimatorController |
| Npc_BlackSmith / Npc_Glocery | SpriteRenderer.Sprite | Idle_0 정지 프레임 | (선택) 다른 sprite |
| Npc_* | Animator.Controller | 비어있음 | Idle_0..5 순환 AnimatorController |
| Npc_* | NpcInteractable.DialogText | 임시 대사 | Inspector에서 편집 |

### C2·C3 이펙트 에셋 결선 — swap-ready 슬롯에 진짜 아트 (2026-06-15)

야간엔 "placeholder는 throwaway"라 미배치했으나, 영호가 `Assets/Art/damage_effect`(slime/golem 애니) + `Boss_Vampire/Skills/Stabbing/Effect`(stab 애니) **진짜 아트 보유**를 알려줘 결선. 코드(C2/C3 swap-ready)는 이미 커밋됨 — Resources 슬롯만 채움.

- **C2 일반몹 이펙트** (`be985e1`): `Resources/Effects/SlimeDamageEffect.prefab`(Normal/슬라임) + `GolemDamageEffect.prefab`(골렘). 각 = SpriteRenderer + Animator(damage 컨트롤러) + EffectLifetime(slime 1.0s/golem 1.25s, 1회 재생 후 파괴). 렌더 확인=파란 splash/회색 충격.
- **C3 보스 stab** (`ce55022`): `Resources/Effects/BossAttackPattern0·1.prefab`(주황 knob placeholder→교체). Boss_Vampire `Stabbing_Effect` 컨트롤러+스프라이트, EffectLifetime 1.33s, sortingOrder 100. **meta guid 보존**(SaveAsPrefabAsset 덮어쓰기)→참조 깨짐 0. 패턴 0(페이즈1)·1(페이즈2) 둘 다. 렌더 확인=빨간 찌르기.
- 전부 MCP RunCommand 제작, mid-frame Capture 렌더 확인, 콘솔 0err, 씬 무변경. **애니 풀재생은 Play 모드에서 확인**(에디터 정지 상태는 정지 프레임).
- **C5 StageClear**: 스프라이트 시트 아트 부재 → TMP "Stage Clear!" 텍스트 폴백 유지(없는 아트로 throwaway 안 만듦). knight/base damage 아트는 해당 적 종류 없어 미사용.

### 남은 영호 육안/Play-test (실제 동작 확인)

- 양방향 포탈 왕복(HG↔Town, Boss↔HG) — 포탈 위 ↑키 진입.
- 마을 NPC 2종 E키 대사 (단, **플레이어에 "Player" 태그** 박혀있어야 `NpcInteractable` 트리거 작동 — 기존 플레이어 prefab 태그 확인 필요).
- NPC/포탈 위치 미세 조정은 Inspector에서 자유(좌표 박제값은 swap-ready라 코드 무관).
