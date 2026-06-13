---
owner: youngho
milestone: M4.13
phase: milestone-closeout
summary: 임펄스 동작 클래스(대쉬·넉백·임펄스공격) 시스템 통일 마일스톤 마감. P1 행동 입력 게이트 → P2 서버 단일 임펄스 모델 → P3 대쉬 거동 재설계 → P4 공유 공식 추출 → P5 클라 하이브리드 예측 + forceAdopt 크러치 제거(Protocol v13) → P6 통일 서사 완성(넉백 forceAdopt 영구 채택, 안 B). 회귀 WSL2 569/0 + EditMode 122/0.
title: 임펄스 동작 클래스 재설계 — 행동 입력 게이트 + 대쉬 거동 + 서버 모델 통일 + 클라 예측 통일
status: done
grade: 대규모
created: 2026-06-12
completed: 2026-06-14
domains: [shared, server, client, qa]
---

# M4.13 — 임펄스 동작 클래스 재설계 — ✅ 마일스톤 마감

**마감 일자**: 2026-06-14 · **Phase**: 6/6 완료 · **브랜치**: `feature/m4.13-shared-extract` (P1~P3+하네스 main `#107` 위, P4~P6 7 commit)

---

## TL;DR

"클라가 예측 못 하는 서버 임펄스 동작(대쉬·넉백·임펄스공격)"이 매 스냅샷 `forceAdopt`로 끌려와 50ms 시각 스터터를 내던 **클래스 공통 문제**를, 하나의 시스템으로 통일했다. 행동 입력 게이트(P1) → 서버 임펄스 단일 모델(P2) → 대쉬 거동 재설계(P3) → 공유 공식 추출(P4) → 클라 하이브리드 예측 + 크러치 제거(P5) → 통일 서사 완성(P6). **대쉬·lunge는 클라가 직접 예측**(forceAdopt 499/500 → 0, 손맛 "엄청 좋다"), **넉백은 server-reactive라 forceAdopt 채택이 표준**으로 원리를 닫았다. 회귀 WSL2 569/0 + EditMode 122/0 + reviewer 6축 0위반.

---

## 5단계 보고

### 1. 무엇을 만들었나

| Phase | 등급 | 핵심 산출물 | commit |
|---|---|---|---|
| P1 행동 입력 게이트 | 대규모·trust-boundary | `ActionKind`+`IGameAction`+`ActionGate` 단일 입구(상태 허용·쿨다운·클래스·rewind 한 곳), `ActorState.AcceptsAction` 상태 정책, 상태가 임펄스 데이터 소유(`0.85` 잔류 사고 봉합) | `11b5baf` 외 |
| P2 서버 임펄스 모델 | 복잡·trust-boundary | 단일 `ExternalImpulseVx`+`DecayImpulse()` 경로, 대쉬 고정거리 등속 D=4.0(`DashTravelTicks`) | `cf0f739`+`15ea8b8` |
| P3 대쉬 거동 | 복잡·trust-boundary | 적 밀침(허딩 — `KnockbackVx` 채널 재사용, Boss 면역) + 완전 무적(`InvulnUntilTick` 데미지 전 게이트) | `860f5a2`+`f35814c` |
| P4 공유 추출 | 보통 | `DashSpeed`/`AttackLungeInitialVx`→`Constants`, 감쇠→`Physics.DecayImpulse` 순수함수(복붙 silent drift 차단 = P5 안전망) | `3403f38` |
| P5 클라 예측 통일 | 대규모·핵심 리스크 | 하이브리드 임펄스 예측(live=계산/replay=저장값) + forceAdopt 크러치 제거 + 대쉬 방향 정렬(`C_SkillUse.facing` v13) + 원격 스킬 모션 | 5 commit (~`fc50711`) |
| P6 클래스 통합 | 복잡 | 통일 서사 완성 — 넉백 forceAdopt 영구 채택(안 B), 명문화 주석 3곳(거동 0), 전체 회귀 마감 | 명문화 1 commit |

### 2. 왜 필요한가

M4.11 백로그 SnapThreshold Play 실측 중 대쉬에서 **20Hz 뚝뚝 끊김**(시각 스터터)이 발견됐다. 근본 원인은 대쉬 하나가 아니라 **클래스 공통** — 대쉬·넉백·임펄스공격이 모두 *클라가 예측 못 하는 서버 임펄스*라, 매 스냅샷 `forceAdopt`로 끌려오고 reconcile 보간 버퍼 리셋이 그걸 매번 지워 50ms 간격 스냅이 됐다(`[Reconcile]` 500 로그: 단순 이동 오차 0.2칸 vs 대쉬 forceAdopt 499/500). 동시에 "동작 State가 끝나야 입력을 받는 예외 처리가 스킬마다 재발"하는 구조 부재(`LungeDecayPerTick 0.85` 잔류 사고)도 같은 뿌리 — 상태가 자기 행동·데이터를 소유하지 못해서다.

### 3. 어떻게 만들었나

- **게이트 토대(P1)**: 상태가 `AcceptsAction(kind)`으로 입장 정책을 소유, 서버 `ActionGate`가 단일 입구에서 ①상태 허용 ②쿨다운 ③클래스 ④rewind 검사. 호출자가 상태 내부 파라미터를 찔러 넣던 구조 제거(상태 Enter/Exit가 임펄스 데이터 소유).
- **서버 단일 모델(P2·P4)**: `EnterAttackState`(대쉬/lunge)·`EnterHitState`(넉백)가 모두 `ExternalImpulseVx`+`ImpulseDecayPerTick`을 세팅하고, `AttackState.Tick`·`HitState.Tick`이 **같은 `player.DecayImpulse()` 헬퍼**(=`Physics.DecayImpulse` 공유 공식)로 감쇠 → 세 임펄스가 서버에서 한 기계.
- **클라 하이브리드 예측(P5)**: live는 `Physics.DecayImpulse`로 매 틱 전진(서버 `AttackState.Tick` 거울), replay는 `InputRecord.ExternalVelX` 저장값 재생(재계산 금지). `forceAdopt` Attack 분기 제거로 보간 복원(스터터 소멸). 방향전환 대쉬 클러스터는 `C_SkillUse.facing`(Protocol v13)으로, 원격 썬더볼트 모션은 Channeling latch로 봉합.
- **통일 서사 완성(P6)**: 클라는 *시작점을 아는* 임펄스만 예측한다 — 대쉬/lunge는 self-initiated라 시전 틱·방향·지속을 알아 `StartImpulse`로 직접 예측. 넉백은 server-reactive(피격 신호 RTT 후 도착 + 방향 추론 + hitstun 서버 전용)라 예측 근거가 없어 `forceAdopt` 채택. "예측이냐 채택이냐"는 우연이 아니라 **클라가 시작점을 아느냐의 원리**.

### 4. 테스트 결과

| 게이트 | 결과 |
|---|---|
| WSL2 서버 회귀 | **569/0** |
| EditMode (클라 예측 결정성) | **122/0** (state=Passed) |
| Unity 컴파일 / 콘솔 error | 0 / 0 |
| reviewer Tier 2-A | 6축 0위반 |
| Play 검증 (영호, P5 2클라) | forceAdopt 499/500→0 + 손맛 "엄청 좋다" + 방향전환 대쉬 정상 + 원격 썬더볼트·대쉬 모션 정상 |

### 5. 다음 스텝

- **마일스톤 전체 PR**(영호 명시 GO 게이트): P4~P6 한 브랜치(`feature/m4.13-shared-extract`) push + `gh pr create` = irreversible. `Shared.dll`+`ClientNet.dll` commit 포함 → 03_Client CODEOWNERS(정유현) co-review 트리거 → admin bypass(사유 박기 + GO).
- **후속 제안**: 임펄스 방향 정보가 필요한 미래 기능(방향 가변 넉백 등)이 생기면 `S_EnemyAttack` 방향 필드 추가를 별도 마일스톤으로 — 단 현재는 클라 추론으로 충분해 보류.

---

## AC 검증 결과

| 마일스톤 완료 조건 | 결과 |
|---|---|
| 임펄스 클래스(대쉬·넉백·임펄스공격) 시스템 통일 | ✅ 서버 단일 경로 + 클라 예측/채택 원리 |
| 대쉬 거동 재설계(고정거리 등속·적 밀침·완전 무적, 영호 6결정) | ✅ P2·P3 |
| forceAdopt 크러치 제거(대쉬/lunge) — 빈도 급감 | ✅ 499/500 → 0 (실측) |
| 공유 공식 단일 출처(§4) — 클라/서버 동일 | ✅ P4 `Physics.DecayImpulse` |
| wire 호환(§2) — Protocol | ✅ v13(P5 facing append, append-only) |
| 전체 회귀 green | ✅ WSL2 569/0 + EditMode 122/0 + Unity 0 |

검증 명령: WSL2 `dotnet test Dawnholder.slnx --no-build` → `Failed: 0, Passed: 569, Skipped: 4, Total: 573`. EditMode = TestRunnerApi `[EDITMODE-DONE] passed=122 failed=0 state=Passed`. Unity ReadConsole `error CS` 필터 = 0건.

---

## 결정 흐름

1. **마일스톤 재구성(2026-06-12)**: 행동 입력 게이트를 옛 M4.12에서 본 마일스톤 P1으로 합류 → M4.12 ⟂ M4.13 독립, 자족적 마일스톤.
2. **P3 "신규 충돌 시스템" 오판 정정**: 적 밀침이 신규인 줄 알았으나 적 변위 채널(`KnockbackVx`)이 이미 존재 → 신규 시스템·wire bump 0.
3. **P5 핵심 리스크 = 크러치 제거**: forceAdopt 제거 = 안전망 제거. replay가 서버 임펄스 궤적을 비트단위 재현 못하면 영구 offset 누적(M4.11 P2 ε silent break 동류) → P4 공유 공식 + EditMode 결정성 테스트가 안전망.
4. **P5 잔여 클러스터 진단 2단계 진화**: 큐 깊이 가설 ❌기각(queueDepth 0~1, offset 로그는 서버틱 vs 클라 카운터 도메인 혼동) → 영호 단서("방향 틀고 대쉬하면 반대로")로 ②facing 불일치 확정 → `C_SkillUse.facing` v13.
5. **P6 넉백 안 A vs 안 B(영호 결정 B)**: 넉백을 예측까지 통일하려 했으나(안 A, 패킷 추가 없이 클라 추론 가능함도 확인), server-reactive 특성상 이득이 작고 방향추론 시각버그 위험. forceAdopt는 서버 권위 100%·위험 0 → **넉백 forceAdopt 영구 채택**. reviewer 🟡 "넉백 예측 재검토" = 별도 Phase 불필요로 해소.

---

## 학습 일지 후보 키워드

- **`impulse-class-prediction-boundary`** — 임펄스 예측 가능성의 경계 = 클라가 시작점(틱·방향·지속)을 아느냐. self-initiated(대쉬/lunge)=예측, server-reactive(넉백)=채택(forceAdopt). 서버 단일 경로 위에서 클라만 갈린다.
- **`dash-facing-client-authority`** — "§1은 적용·판정 권위를 서버에 두라는 것이지 입력 출처를 서버가 만들라는 게 아니다." 방향/조준 같은 클라 입력 파생 값은 클라가 보내고 서버가 정규화·적용.
- **진단 도메인 혼동 경계** — prediction 로그에서 서로 다른 tick 도메인(서버틱 vs 클라 로컬 카운터)을 섞어 빼면 무의미. 측정 설계 자체를 의심하는 게 1차.
- **크러치 제거 = 비트단위 재현 계약** — forceAdopt 같은 안전망을 떼려면 클라 replay가 서버 궤적을 비트단위 재현해야 성립. 공유 공식 단일 출처가 그 전제.
- **착수 후 실측이 Phase 범위를 재정의** — 정의서 골격이 선행 Phase 진행으로 이미 충족될 수 있음. plan은 현재 코드 실측 먼저.

---

> M4.13 마감 — 임펄스 동작 클래스 통일 완성. 대쉬는 첫 적용 사례였고, 같은 기계가 lunge·넉백까지 원리적으로 닫혔다. 다음 = 마일스톤 전체 PR(영호 GO).
