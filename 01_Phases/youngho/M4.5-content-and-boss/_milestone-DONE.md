---
owner: youngho
milestone: M4.5
phase: milestone-closeout
title: Content & Boss — 콘텐츠 + 보스 전투 마일스톤 마감
status: done
completed: 2026-06-07
grade: 대규모
summary: M4.5 완전 마감 (6 Phase 풀세트, PR #68~#72 + 마감 PR). 맞기만 하던 적이 양방향 전투가 됐다 — 보스 FSM(쿨다운→telegraph 예고→권위 AABB 판정→사망/리스폰, 페이즈2)이 서버 권위로 돌고, 클라는 telegraph 모션·임팩트 이펙트·HP 실감소·리스폰 페이드로 연출만 한다(헌법 #1). ProtocolVersion 8→9 한 묶음 bump(S_EnemyAttack ID 20 + S_PlayerJoin.characterClass — M4.5 유일, 팀원 pull 후 Shared.dll 재빌드 의무). 콘텐츠 축 = 적 prefab 구조 전환(placeholder 113파일 은퇴) + 골렘 + HUD 실연결(mock HP 은퇴) + 직업 로직/비주얼 분리(ClassVisuals 단일 출처). 최종 회귀 = 클린빌드 0/0 + test 419/0/4 + 봇 7종 PASS + cross-review γ 3라운드 수렴 GO + 발표 데모 풀 루프 2클라 이상 무. 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.5 — Content & Boss 마일스톤 박제

**마감 일자**: 2026-06-07 (세션23, Phase 06 회귀 + 마감)
**Phase 수**: 6/6 완료 (01~05 개별 PR 머지 + 06 본 마감 PR)
**등급**: 대규모 (shared/server/client/qa 4도메인 관통 + irreversible v9 bump + unity-asset 깃발)
**WORK-ID**: m4.5-content-and-boss
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — ① 보스 양방향 전투: 서버 권위 FSM(쿨다운 40/24틱 → telegraph 16/10틱 예고 → AABB 판정 → 사망/리스폰, 페이즈2 HP50%)이 돌고 클라는 준비→발동 모션·패턴 이펙트·HP 실감소·리스폰 페이드로 연출. ② 콘텐츠/구조: 적 prefab 데이터 전환(새 적 = prefab 1 + 테이블 1행) + 골렘 + HUD 실연결 + 직업 로직/비주얼 분리(직업 모습 수정 = ClassVisuals 한 파일). 합쳐서 발표 데모 풀 루프(메인→마을→사냥터→보스방 StageClear) 완성.
- 🤔 **왜 필요한가** — M4.4까지 적은 맞기만 했고(전투 절반), HUD는 mock 값이었고, 적 외관은 런타임 조립 placeholder였다. 발표 데모에 "전투다운 전투"가 필요했고, 보스 = 서버 권위 적 행동 FSM의 첫 사례라 M5(영속화) 전에 전투 계약(공격 이벤트 wire 모양)을 확정해야 했다.
- 🛠️ **어떻게 만들었나** — 6 Phase 직렬: 적 prefab 구조(01) → 골렘+EnemyKind 이사(02) → HUD 게이트(03) → 보스 프로토콜+서버, v9 유일 bump(04) → 보스 클라 연출+직업 비주얼 분리(05) → 회귀+마감(06). 핵심 선택은 본문 "결정 흐름" 6건 — 특히 telegraph 전원 broadcast vs 임팩트 targeted의 2채널 분리, v9 한 묶음 bump, Animator 속도를 서버 틱 상수에서 역산.
- 🧪 **테스트 결과** — 최종 회귀(세션23): 클린빌드 0 경고/0 오류 + `dotnet test` **419 통과/0 실패/4 skip**(WSL2 = ADR-029 표준) + 헤드리스 봇 **7종 전부 PASS**(M2 desync 0.00, BossFight/BossStageClear 보스 처치+StageClear) + `ProtocolVersion == 9`(04 유일 bump 약속 이행, 본 브랜치 98_Shared diff 0) + **cross-review γ 3라운드 수렴 GO**(β 결함 6건+신규 1건 전부 봉합) + **발표 데모 풀 루프 Play 2클라 이상 무**(사망 HP 0 표시→암전→부활 full 포함).
- ➡️ **다음 스텝** — M5 Persistence 정식 분해(`/work:plan`) — 선행 결정 = LocalDB Linux 부재(ADR-029 트레이드오프 ④). 구조급 이월 = 플레이어 HP 동기화 + 공격 이벤트 패킷(v10 후보 한 묶음). 상세는 본문 "이월 명시".

---

## TL;DR (🎯 무엇 / 🤔 왜)

M4.5는 게임의 **전투를 양방향으로** 만들고 콘텐츠 추가를 **데이터 작업**으로 바꾼 마일스톤이다.

**보스 전투**: 옛 적은 플레이어가 때리면 맞기만 했다. 이제 보스는 서버 FSM이 쿨다운을 세고, telegraph(예고)를 전원에게 broadcast하고(`S_EntityState animState=Attack`), 16/10틱 뒤 보스 중심 AABB와 플레이어 *서버 권위 위치*를 교차 판정해서 히트 시에만 `S_EnemyAttack`(ID 20)을 보낸다. 클라는 그 두 패킷을 준비자세→발동 모션과 임팩트 이펙트로 연출할 뿐 판정 0줄(헌법 #1). 플레이어가 죽으면 서버가 스폰 재배치 + HP full, 클라는 "HP 0 표시 → 암전 → 부활 full" 페이드.

**콘텐츠 구조**: 적 외관은 `EnemyVisualTable` SO + prefab(149줄 런타임 조립 은퇴), 직업 모습은 `ClassConfig.VisualPrefab` → `ClassVisuals/*.prefab` 단일 출처(런타임 "Visual" 자식 장착). 새 적 = prefab 1 + 테이블 1행, 직업 외형 수정 = 파일 1개. `EnemyKind`는 서버/클라 중복 정의를 98_Shared 단일 정의로 이사(헌법 #4 봉합).

**프로토콜 규율**: v9 bump는 M4.5 전체에서 Phase 04 단 한 번 — `S_EnemyAttack` 신설 + `S_PlayerJoin.characterClass` append를 한 묶음으로. 나머지 5개 Phase는 bump 0 약속 이행 (본 마감 브랜치 98_Shared diff 0 검증).

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 머지 |
|---|---|---|---|
| 01 | 적 시각 prefab 전환 | `EnemyViewFactory.BuildPlaceholder`(149줄) 은퇴 → `EnemyVisualTable` SO + prefab. 본인 제작 Slime/Boss_Vampire로 통일, 외부 placeholder 113파일 삭제(참조 0 전수). `InternalsVisibleTo` 첫 도입 | PR #68 |
| 02 | 골렘 + EnemyKind 이사 | 골렘(HP60/방어5/속도1.2) + HG 재bake + `EnemyKind` 98_Shared 단일 정의(헌법 #4) + 화석 분기 3곳(`!= Normal` → `== Boss`) 정정 | PR #69 |
| 03 | HUD UI 연결 | `HudController.UpdateMP` + MP 슬라이더 / `MapNameDisplay`(static mapId — UI 씬 재로드 생존) / `SceneRouter.MapIdToDisplayName` 한 파일 봉인 | PR #70 |
| 04 | 보스 프로토콜+서버 | `BossBehaviorSystem` FSM + `EnemyStats.Attack`(5/8/12) + **ProtocolVersion 8→9 한 묶음**(S_EnemyAttack ID 20 + characterClass append) + BossFightSmoke 봇 | PR #71 |
| 05 ★대규모 | 보스 클라 연출 + 직업 비주얼 분리 | `EnemyAttackHandler`(HP바 실연결 **mock HP 은퇴** + DamageFlash + 패턴 이펙트 + 리스폰 페이드) + 원격 직업 표시 + Mage 투사체(시각 전용) + **로직/비주얼 분리**(ClassVisuals 단일 출처, PlayerBase 수술, EffectAnchor flipX 거울상) | PR #72 |
| 06 | 회귀 + 마감 | 보스 모션 Start→End 봉합 + cross-review β 봉합(사망 HUD 0 고착) + 회귀 풀세트 + 데모 풀 루프 + 본 박제 | 본 마감 PR |

**Phase 06 포함분 (세션23)**:
- **보스 모션 Start→End 미전이 봉합** (`897a90d`) — 원인 실측: Start 클립 1.333s > 서버 AttackLatch 1.2s(16+8틱) → Any State→Idle 전이가 Start를 90% 지점에서 끊어 End(발동) 영원히 미도달. 봉합 = Animator 속도를 서버 틱 상수에서 역산(Start 1.6667→0.8s=telegraph / End 3.3333→0.4s=latch) + `ComputeBossAnimState` 우선순위 Death > **Attack > Hit**(보스 한정 — telegraph 중 피격이 Start 리셋시키는 공범 차단, 피격 피드백은 DamageFlash 유지). P2(0.5s)는 telegraph 상수 98_Shared 이동 + 동적 배율로 이월.
- **cross-review γ 봉합** (`2271025`) — β(Codex)가 잡은 진짜 결함: 사망 후 S_Snapshot에 HP가 없어 **HUD 0 고착**(리스폰 복구 경로 부재 — α/reviewer 2회 통과를 뚫고 잠복). 봉합 = `PlayRespawnFade(onCovered)` 암전 시점 콜백으로 HUD를 `PlayerStats.ForClass.MaxHp` 복구(서버 리스폰 규칙의 98_Shared 표시 미러, v9 불변 유지). β 2차가 봉합 1차의 신규 결함(같은 프레임 0→full 덮어쓰기 = 사망 피드백 소실)을 또 잡음 — 재실측 의무 두 마일스톤 연속 실증. β 3차 수렴.

---

## 결정 흐름 (🛠️ 어떻게 — 회고 참고용)

1. **telegraph/임팩트 2채널 broadcast 분리** — 예고(`S_EntityState animState=Attack`)는 전원 broadcast(회피 판단은 히트 여부와 무관하게 모두에게 필요), 임팩트(`S_EnemyAttack`)는 피격자 있을 때만(HP reconcile 목적 = targeted가 대역폭 정합). 한 패킷에 합치면 "빗나간 공격도 임팩트 패킷" 또는 "범위 밖은 예고 못 봄" 중 하나를 강요당한다.
2. **v9 한 묶음 bump** — S_EnemyAttack 신설 + characterClass append 모두 같은 PR의 wire 변경 → 단일 bump. 흩어 bump하면 팀원 재빌드 의무가 2회로 늘 뿐 이득 0.
3. **Animator 속도 = 서버 상수 역산** — 클립을 늘려 자연 종료를 기다리는 게 아니라 서버 진실(telegraph 16틱 + latch 8틱)을 단일 출처로 두고 클라 재생 속도를 역산. "클라 애니 길이 > 서버 latch = 시각 상태 영원히 미도달"이 server-authority 특유 함정. P2 분기는 상수가 갈리므로 동적 배율(98_Shared 상수 이동)로 이월 — 그게 되면 P1/P2 자동 정합.
4. **HP 복구 = 표시 미러 (패킷 신설 기각)** — 리스폰 HP 동기화의 정석은 전용 패킷이지만 그건 v10 bump = "M4.5 bump는 04 유일" 약속 위반. 서버 리스폰 규칙(`Stats.MaxHp`)과 같은 98_Shared 값을 쓰는 클라 표시 미러로 임시 봉합 + v10 구조급 이월 명시. 복구 시점은 페이드 암전 콜백(같은 프레임 복구는 사망 0 표시를 지움 — β 2차 발견).
5. **보스 한정 Attack > Hit** — 일반 적/플레이어는 Hit 우선 유지(피격 반응이 주 정보), 보스만 Attack 우선(telegraph 예고가 회피 정보라 더 중요, 피격 피드백은 DamageFlash 별도 채널). 전역 변경 대신 `BossBehaviorSystem` 내부 static에 격리.
6. **로직/비주얼 분리 (Prefab Variant)** — controller swap 코드 은퇴, 직업 모습 = ClassVisuals prefab 단일 출처. PlayerBase root SR/Animator 제거 수술로 Local/Remote variant 자동 전파 — 에디터에서 모습 없는 게 정상(런타임 장착). 모든 직업 비주얼 작업자에게 영향(CHANGELOG [M] 박힘).

---

## AC 검증 결과

Phase 06 완료 조건 대조 (2026-06-07 세션23, WSL2 = ADR-029 표준 경로):

- [x] 보스 공격 모션 Start→End 전이가 이펙트 타이밍과 정합 — Play 실측 통과 (준비 0.8s → 발동+임팩트 동시 → Idle 복귀, 때리면서도 telegraph 안정)
- [x] `dotnet test` 전부 green — 클린빌드 0/0 + **419/0/4 skip** (기존 417 + 신규 3: 우선순위 2 + 경계값 1)
- [x] 봇 전 시나리오 PASS — **7종**(smoke/M2 desync 0.00/MultiRoster/EmergencyCombat/BossStageClear/BossFight/EnemyAi). 캐비앗 박제: 보스 무리스폰 설계라 보스 시나리오는 서버당 1회(fresh 재기동 필요)
- [x] 발표 데모 풀 루프 무사고 — 2클라: 메인→캐릭터 선택→마을(미니맵/맵 이름)→사냥터(슬라임+골렘 HP 실감소)→보스방(telegraph→양방향 전투→사망 HP 0 표시→암전→부활 full→처치→StageClear) + 원격 직업 상호 확인
- [x] `ProtocolVersion.Current == 9` — 04 유일 bump 약속 이행 (본 마감 브랜치 98_Shared diff 0)
- [x] /cross-review — α 🔴0/🟡1 + β 3라운드 수렴 GO (산출물 = `00_Document/reviews/2026-06-07-cross-review-m4.5-phase04-combat-v9.md`)
- [x] CHANGELOG entry [M] — v9 bump 팀원 재빌드 의무 명시
- [ ] PR 생성·머지 — **사용자 명시 GO 게이트** (본 박제 commit 후 진행)
- [x] work-pin 갱신 — 본 마감 흐름에서 지속 갱신

---

## 이월 명시 (➡️ 다음)

- **M5 Persistence 정식 분해 트리거**: 본 마감 PR 머지 → LocalDB Linux 부재 결정(ADR-029 트레이드오프 ④) → `/work:plan`
- **구조급 (v10 후보 한 묶음)**: 플레이어 HP 동기화 전용 패킷(현 표시 미러는 임시) + 원격 공격 이벤트 패킷 부재(원격 Ranger 투사체 표시 불가의 뿌리)
- **보스 P2 애니 정합**: telegraph 상수 98_Shared 이동 + 클라 동적 속도 배율 — P1/P2 자동 정합
- **qa**: BossFightSmoke 리스폰 단언 보강(서버측 position/후속 동작 — 봇은 HUD 관측 불가) / EffectAnchor 거울상 PlayMode 통합 테스트 / MapNameDisplay flaky(03)
- **부하 단계(M5+)**: GenPackets Write 64KB 할당 풀링(생성기 전역 패턴 — α 발견)
- **하네스/환경**: CI actions v5 bump(Node20 deprecated, 6/16 임박) / reviewer 🟡 누적(04 3건+05 2건+봇 2건) / 옛 브랜치 정리 / /session:log 문서 Codex 분기 은퇴 갱신
- **게임플레이 후보**: 적 중력 부재 / 공유 코드 정리 논의 / 유현 BgmComposer 합류분

---

## 학습 일지 후보 키워드

telegraph 전원 broadcast vs 임팩트 targeted 2채널 분리 / 클라 애니 길이 vs 서버 latch 정합(시각 상태 영원히 미도달 함정) / 서버 상수 단일 진실 + 클라 속도 역산 / 봉합이 새 결함 도입(β 재실측 의무 두 마일스톤 연속 실증) / 표시 미러 vs 패킷 신설의 bump 비용 트레이드오프 / 보스 한정 우선순위 격리(전역 변경 회피) / Codex 프롬프트 가드 — 읽기 허용 명시 의무(전면 셸 금지 = NO-GO 오판) / 보스 무리스폰과 시나리오 간 상태 오염
