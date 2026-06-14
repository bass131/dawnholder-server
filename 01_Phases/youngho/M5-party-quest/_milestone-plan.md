---
owner: youngho
milestone: M5
title: 파티 시스템 + 40킬 퀘스트 + 보스 포탈 잠금 + 양방향 포탈 + 콘텐츠 결선
status: planned
grade: 대규모
estimated: 25~40h (총합, 24 Phase · 6 트랙)
domains: [shared, server, client, qa]
risk: irreversible | trust-boundary
---

# M5 — 파티·퀘스트·보스 포탈 잠금 + 콘텐츠 결선

> **상태**: planned — 2026-06-14 영호 의논(plan mode) + 메인 세션 + Explore×3 + Plan 에이전트 실측 기반 작성
> **시작**: 2026-06-14 (M4.15 마감 + PR #110 머지 후, `feature/m5-party-quest`)
> **목표 마감**: 미정 — 일정 = 영호 컨트롤 (마감 경고 프레이밍 금지)
> **선행 근거 문서**: `C:\Users\bass1\.claude\plans\velvety-kindling-quill.md` (영호 plan-mode 4회 반복 승인 설계)

---

## 🎯 마일스톤 목표

영호 Play-test 동선에서 빠진 콘텐츠 7종을 한 마일스톤으로 묶음. 핵심은 **프로젝트 첫 플레이어 간 협동 시스템**(파티 + 공유 퀘스트 + 보스 게이트)이며, 신규 프로토콜(비가역 `ProtocolVersion` v14→v15 bump)이 걸림. 나머지(이펙트/NPC/StageClear)는 대부분 wiring/visual.

**목표 = 야간 자율 최대화**: 서버 권위 핵심은 헤드리스(xUnit+봇) 검증으로 무인 완주, Unity 외관은 MCP 시도(백업의무)+아침 육안.

### 확정 결정 (영호 plan-mode 승인 2026-06-14)

1. **NPC** = 대화 NPC만 (배치+E키 대사, 상점/제작 X — 별도 마일스톤)
2. **파티** = 초대/수락, 정원 **2명 고정** (`C_PartyInvite`/`C_PartyRespond`)
3. **퀘스트** = 파티 공유 40킬 + **보스 포탈 잠금**(미달 거부)
4. **포탈 진입** = 충돌 자동 → "겹침 + 위 방향키" + **양방향 포탈**
5. **야간 경계** = Unity 외관도 MCP 야간 시도(백업의무+Phase08 학습), 육안은 아침
6. **에셋 swap-ready** = 에셋 없으면 placeholder, 단 진짜 animator/sprite를 같은 슬롯에 꽂으면 코드 0변경 동작(바인딩 분리)
7. **v15 bump 사전승인** = 야간 브랜치 commit 자율(push/PR/디스코드는 아침 영호 게이트)
8. **Phase 다수 허용** = 평소 5~7개/마일스톤 룰 예외 waive (24 Phase, 잘게)

### 핵심 워크스트림 (6 트랙)

- **(A) 파티 서버** — PartyRegistry actor(cross-map), 초대/수락 프로토콜, 신뢰경계
- **(Q) 퀘스트 + 게이트** — 공유 40킬 카운트, 보스 포탈 잠금
- **(B) 포탈 메커니즘** — 충돌→겹침+위키, 양방향
- **(P) 클라 파티/퀘스트 표현** — 초대 팝업, 멤버 HUD, 퀘스트 카운터, 잠금 토스트
- **(C) 독립 콘텐츠** — 일반몹 공격, 피격·찌르기 이펙트, NPC, StageClear 애니
- **(R) 회귀 + 마감** — 봇 시나리오 2종, 전체 회귀, -DONE

### ⚠️ 등급 = 대규모 사유

- **4 도메인**(shared PDL + server + client + qa — §grade-and-risk "3+ 도메인")
- **irreversible 깃발**(`ProtocolVersion` v14→v15 bump — 8패킷 append)
- **trust-boundary 깃발**(파티 핸들러 A3/A4 `Handlers/`, 보스 게이트 Q3 `MapMigration` 검증)
- **300줄+**(신규 PartyRegistry/핸들러/퀘스트/일반몹 공격 + 클라 UI 다수)

### 핵심 아키텍처 (Plan 에이전트 설계 — 비가역 부분)

- **PartyRegistry = GameWorld 소유 별도 actor**(JobQueue+매 틱 드레인). 파티는 cross-map(마을↔헌팅 유지)이라 맵/세션에 못 둠. 헌법 "actor, lock 금지" 정합.
- **멤버 식별 = entityId**(session 참조 X — disconnect race 회피, ADR-026 전역 entityId).
- **cross-map 1:1 송신 = `GameWorld.SendToEntity(entityId, payload)`** — 멤버 현재 맵 찾아 그 맵 `EnqueueJob` 안 `session.Send`. **다른 맵 tick thread 직접 호출 금지**.
- **킬카운트 = `PartyState.KillCount`(공유) + `SoloProgress` dict(솔로)**. 리셋=파티 해산/StageClear(맵 재진입 시 X).
- **보스 게이트 = `MapMigration.Execute` 검증 단계(transfer 전, RemovePlayer 전 필수 — ghost 방지)**.
- **PDL 가변 list 미지원** → 정원 2 고정 `member0/member1` 2슬롯(빈=0).

### Plan 에이전트 실측 정정 (drift 봉합)

- **보스 이펙트는 이미 결선됨**: `EnemyAttackHandler.cs:74`→`BossAttackEffectSpawner.Spawn(attackPattern)`. 일반몹이 기존 **`S_EnemyAttack`(ID20) 재사용**하면 이펙트 자동 흐름 → C 트랙 신규 패킷 0.
- **위 방향키 3중 충돌**: Teleport `verticalDir`(↑+E조합)+NPC `E`키+점프. **핵심 리스크 = "↑가 점프 바인딩인지"** → B2 전 `*.inputactions` 확인 선행.
- **StageClear = 현재 TMP 텍스트 동작 중**(`StageClearUI.cs:54`). 애니 교체.
- **UI 에셋 보유**: `Quest_Panel.png`/`Status_Frame.png`/`Dialog_Frame_Temporary.png` placeholder 재사용.

---

## 📋 Phase 분해 (24개 — 6 트랙)

> 전역 번호 01~24 = 트랙 그룹 순. 실제 실행 순서는 아래 의존성 그래프 + 야간 우선순위 블록 참조.

| # | Phase | 트랙 | 등급 | 도메인 | risk | 검증 |
|---|---|---|---|---|---|---|
| 01 | A0 PDL 8패킷 일괄 + v15 bump | A | 보통 | shared | irreversible | ✅빌드 |
| 02 | A1 PartyState + PartyRegistry actor 코어 | A | 복잡 | server | — | ✅xUnit |
| 03 | A2 GameWorld 통합 + cross-map 송신 | A | 복잡 | server | — | ✅xUnit |
| 04 | A3 파티 핸들러 happy path | A | 복잡 | server | trust-boundary | ✅xUnit |
| 05 | A4 파티 신뢰경계 + disconnect 정리 | A | 복잡 | server | trust-boundary | ✅xUnit |
| 06 | Q1 HandleEnemyDeath killer 전파 | Q | 보통 | server | — | ✅xUnit |
| 07 | Q2 킬카운트 + S_QuestUpdate | Q | 보통 | server | — | ✅xUnit |
| 08 | Q3 보스 포탈 잠금 게이트 | Q | 보통 | server | trust-boundary | ✅xUnit/봇 |
| 09 | B1 양방향 포탈 데이터 | B | 단순 | server | — | ✅봇 |
| 10 | B2 포탈 진입(겹침+위키) | B | 보통 | client | — | 🔧→⚠️ |
| 11 | B3 역방향 포탈 씬 배치 | B | 단순 | client | unity-asset | ⚠️육안 |
| 12 | P1 클라 파티 패킷 핸들러 + 상태 | P | 보통 | client | — | 🔧컴파일 |
| 13 | P2 파티 초대 송신 + 수락 팝업 UI | P | 보통 | client | unity-asset | ⚠️육안 |
| 14 | P3 파티 멤버 HUD | P | 단순 | client | unity-asset | ⚠️육안 |
| 15 | P4 퀘스트 진행 HUD | P | 단순 | client | unity-asset | ⚠️육안 |
| 16 | P5 포탈 잠금 피드백 토스트 | P | 단순 | client | — | ⚠️육안 |
| 17 | C1 일반몹(Normal/Golem) 공격 로직 | C | 복잡 | server | — | ✅xUnit/봇 |
| 18 | C2 일반몹 피격 이펙트 wiring | C | 단순 | client | unity-asset | ⚠️육안 |
| 19 | C3 보스 찌르기 이펙트 wiring | C | 단순 | client | unity-asset | ⚠️육안 |
| 20 | C4 NPC 배치 + 대사 | C | 단순 | client | unity-asset | ⚠️육안 |
| 21 | C5 StageClear 폰트→애니 스프라이트 | C | 단순 | client | unity-asset | ⚠️육안 |
| 22 | R1 봇 PartyQuestSmoke | R | 보통 | qa | — | ✅봇 |
| 23 | R2 봇 BossGateSmoke | R | 보통 | qa | — | ✅봇 |
| 24 | R3 전체 WSL2 회귀 + 마일스톤 마감 | R | 복잡 | qa | irreversible | ✅+영호 |

**복잡+ Phase**(02·03·04·05·17·24)는 각 `-DONE.md`(또는 마일스톤 종합 흡수). 마일스톤 마감 = `_milestone-DONE.md` + HTML(ADR-031).

**모델 라우팅**(subagent-routing §5.5): 구현 Worker 기본 Sonnet. **A3/A4(`복잡+trust-boundary`=Handlers/)는 Opus Worker**(선택적 Opus B). Q3(`보통+TB`)는 Sonnet 구현 + Opus 리뷰. 메인 file:line 실측 게이트 모델 무관. **Worker는 진입 시 재실측**(브랜치 전환 시 옛 좌표 stale).

---

## 🔗 의존성 그래프

```
01 A0(v15 bump, 최우선 단독) ─┬─→ 02 A1 → 03 A2 → 04 A3 → 05 A4 ──────┐
                              │                                          │
                              ├─→ 06 Q1(killer 전파, 파티 의존 0)────────┤  [06은 트랙 A와 병렬]
                              │                                          ↓
                              │                            07 Q2 → 08 Q3 ──┐  (07 = 05+06 의존)
                              │                                             ├→ 22 R1, 23 R2
17 C1(일반몹, S_EnemyAttack 재사용) ──────────────────────────────────────┘
09 B1(양방향 데이터) ──[08 Q3과 다른 파일·게이트 무관=병렬 안전]───────────┘
                                                                            ↓
12 P1(클라 파티 핸들러) → 13 P2 / 14 P3 / 15 P4    [04·05·07 서버계약 의존, 클라]   24 R3(마감)
16 P5(잠금 토스트) ──[08 Q3 계약 의존]
10 B2(포탈 진입) ──[*.inputactions 점프 바인딩 확인 선행]→ 11 B3(역방향 씬)
18 C2 / 19 C3 / 20 C4 / 21 C5  (완전 독립, Unity 외관)
```

- **01 A0은 전 트랙 선행** — PDL/Shared.dll 단독 commit+빌드 통과 후 진행.
- **02→03→04→05 직렬** — PartyRegistry API 순차 확장.
- **06 Q1은 트랙 A와 병렬** (auditor 봉합) — `GameMap` 시그니처 전파라 파티 코드 의존 0. 01만 선행. 실제 파티 결합은 07(OnKill 소비)에서. → 06을 야간 1순위에서 17 C1·09 B1처럼 트랙 A와 동시 진행 가능.
- **GameSession.cs 충돌 최소화**: A3/A4(파티)→Q3(S_PortalLocked) 순서.
- **08 Q3 ↔ 09 B1 병렬 안전**: Q3=`MapMigration.cs`, B1=`PortalTable.cs` 다른 파일. Q3 게이트는 `Dest==BossRoom`만 보고 양방향 테이블 추가 무영향. **단 B1을 Q3 앞 배치 권장**(양방향 테이블 보고 게이트 작성).
- **17 C1 → 18 C2**: C1이 S_EnemyAttack.attackPattern 값 정의 후 C2가 slime/golem 분기.
- **호출처 실측 정정 (auditor)**: `HandleEnemyDeath` 실제 호출처 = `MeleeAction.cs:107`/`DashAction.cs:62`/`DeferredDamageSystem.cs:79` (옛 plan "CombatSystem/SkillSystem"은 stale — Phase 06 반영).

### 야간 자율 우선순위 (헤드리스 우선)

- **1순위 (헤드리스 무인 완주):** `01→02→03→04→05 → 06→07→08 → 22,23` + `17 C1` + `09 B1`. 전부 xUnit+봇.
- **2순위 (클라 스크립트, MCP 컴파일 검증):** `12 P1`, `10 B2`, `18/19/21`(C2/C3/C5 스크립트). Unity 컴파일 0err. 기능 검증 아침.
- **3순위 (아침 Unity 육안, MCP 시도+백업의무):** `13/14/15/16`(P2~P5), `11 B3`, `20 C4`, C2~C5 시각 + 영호 Play-test.

---

## ✅ 마일스톤 완료 조건

- [ ] **(A) 파티**: 초대→수락→S_PartyUpdate 양 멤버 동기. 거절4(자기/이미파티/정원/만료)+응답race 차단. disconnect 시 해산. cross-map 유지(마을↔헌팅). 정원 2 invariant.
- [ ] **(Q) 퀘스트**: 파티 2명 합산 Normal 40킬 → S_QuestUpdate count=40. 솔로 추적. StageClear 시 리셋.
- [ ] **(Q) 보스 게이트**: killCount<40 시 BossRoom 진입 거부 + S_PortalLocked. ≥40 통과. ghost 미발생(RemovePlayer 전 차단).
- [ ] **(B) 포탈**: 포탈 위 정지=자동진입X, 위 방향키 down-edge만 진입. 4맵 양방향 이동.
- [ ] **(C) 일반몹 공격**: Normal/Golem 사거리 내 플레이어 데미지 + 기존 S_EnemyAttack 재사용 broadcast. 무적게이트(Dash) 정합.
- [ ] **(C) 이펙트/NPC/StageClear**: slime/golem 피격 이펙트, 보스 찌르기, 마을 NPC 2종 대사, StageClear 애니 스프라이트.
- [ ] **(P) 클라 표현**: 초대 팝업·멤버 HUD·퀘스트 카운터·잠금 토스트.
- [ ] **wire**: `C_PartyInvite/Respond/Leave` + `S_PartyInviteRecv/Update/Error/QuestUpdate/PortalLocked` 8패킷 append + `ProtocolVersion.Current == 15`. 기존 필드 순서 불변.
- [ ] **회귀 0**: WSL2 `dotnet build` 0/0 + `dotnet test` green(신규 테스트 반영) + 봇 시나리오 회귀 0(`PartyQuestSmoke`/`BossGateSmoke` 신규 그린) + Unity 컴파일 0err.
- [ ] **swap-ready**: placeholder Phase(B3/C5/P2~P5)는 에셋 슬롯 분리 + swap 지점 -DONE 노트 박제.
- [ ] reviewer 헌법 hard 위반 0 (trust-boundary Phase 04/05/08 reviewer 자동).
- [ ] (복잡+) Phase별 -DONE + 마일스톤 -DONE.md + HTML.

---

## 🚫 이번에 명시적으로 뺀 것 (사유 박음)

- **상점/제작 기능**: NPC는 대화만. 아이템 매매/제작은 서버 인벤토리·골드 프로토콜 = 별도 대규모 마일스톤 (확정 결정 1).
- **가변 정원 파티**: 정원 2 고정. 3명+ 확장은 PDL 가변 list 미지원 + 동기화 복잡도 → 후속 (확정 결정 2).
- **XP/보상/드랍**: 퀘스트 완료 보상 시스템은 스코프 밖. 게이트 해제만.
- **퀘스트 데이터 테이블/다중 퀘스트**: 40킬 단일 하드코딩 퀘스트. 데이터 주도 퀘스트는 미래 맵 에디터 마일스톤과 묶음.
- **신규 아트 제작**: placeholder + swap-ready만. 진짜 sprite/animator = 영호/유현 영역 (확정 결정 6).
- **클라 prediction**: 파티/퀘스트는 순수 서버 권위+표시(현 snapshot 방식, reconciliation 없음).

---

## 갱신 이력

- 2026-06-14 — **사전 작성** (메인 세션 plan-mode). 영호 의논(7기능 묶음) + Explore×3(전투/맵·파티·퀘스트/NPC·에셋 실측) + Plan 에이전트(파티/퀘스트/포탈 프로토콜·상태모델 설계). 영호 plan-mode 4회 반복(① 4결정 AskUserQuestion ② Phase 다수 waive ③ swap-ready 원칙 ④ 오토모드 무중단 + v15 사전승인) 승인. 승인 플랜 = `velvety-kindling-quill.md`. **카운트 정정**: 플랜 헤더 "22 Phase" → 트랙 합산 실제 24(실측). 24 Phase로 정식화.
