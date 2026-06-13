---
owner: youngho
milestone: M4.13
phase: 05-client-prediction-b
summary: 클라 임펄스 예측(하이브리드) + forceAdopt 크러치 제거 + 대쉬 방향 정렬(Protocol v13) + 원격 스킬 모션 전파
title: 클라 예측 통일 (방식 B 하이브리드)
status: done
grade: 대규모
slug: 05-client-prediction-b
created: 2026-06-13
completed: 2026-06-14
prior_phases: [01-action-input-gate, 02-server-impulse-model, 03-dash-behavior, 04-shared-extract]
---

# M4.13 Phase 05 — 클라 예측 통일 (방식 B 하이브리드) — ✅ DONE

> 대규모 등급 5단계 보고. 브랜치 `feature/m4.13-shared-extract`, P4 위 **5 commit**.
> 착수 범위(임펄스 예측 + 크러치 제거)에서 **Play 실측으로 2건 추가 봉합**(대쉬 방향 클러스터, 원격 스킬 모션) — 진단 과정 자체가 이 Phase의 핵심 학습 자산.

---

## TL;DR

대쉬·평타 lunge의 **클라 임펄스 예측**을 도입해 `forceAdopt` 크러치(매 snapshot 서버 위치 강제 채택)를 제거하고 50ms 시각 스터터를 없앴다. 그 과정에서 드러난 **방향전환 직후 대쉬 reconcile 클러스터**(facing 불일치)를 `C_SkillUse.facing` 추가(Protocol v12→v13)로, **원격 플레이어 썬더볼트 모션 누락**을 Channeling latch로 봉합했다. forceAdopt 499/500 → 0, 손맛 "엄청 좋다", 회귀 EditMode 122/0 + WSL2 569/0 + reviewer 6축 0위반.

---

## 5단계 보고

### 1. 무엇을 만들었나

| # | commit | 내용 |
|---|---|---|
| 5a | `2e1b85e` | 하이브리드 임펄스 예측 (live=계산 / replay=저장값) |
| 5b | `dcf3b12` | `ShouldForceAdopt` Attack 분기 제거 → 보간 복원 |
| 이펙트 | `3c825e2` | 대쉬 이펙트를 캐스터에 parenting (follow + flip) |
| facing v13 | `52e5042` | 대쉬 방향을 `C_SkillUse.facing`으로 정렬 (Protocol v12→v13) |
| 원격 모션 | `98134bf` | 원격 썬더볼트 캐스팅 모션 표시 (Channeling latch) |

### 2. 왜 필요한가

- **forceAdopt 스터터**: 대쉬/lunge는 클라가 예측 못 하는 서버 임펄스라 매 snapshot 끌려오고, reconcile 보간 버퍼 리셋이 그걸 매번 지워 50ms 간격 시각 스터터(대쉬 forceAdopt 499/500).
- **핵심 계약 리스크**: 크러치 제거 = 안전망 제거. replay가 서버 임펄스 궤적을 비트단위 재현 못하면 영구 offset 누적(M4.11 P2 ε silent break 동류) → P4 공유 공식이 안전망.
- **(Play 발견) 방향전환 대쉬 클러스터**: 방향 틀고 대쉬하면 의도 반대로 "빠바박" + 뚝뚝. 서버 `FacingDir`이 `C_MoveIntent` 입력 큐 지연으로 옛 방향.
- **(Play 발견) 원격 썬더볼트 모션 누락**: 타인 시점에서 마법사 캐스팅 모션 미표시.

### 3. 어떻게 만들었나

- **5a 하이브리드**: live는 `Physics.DecayImpulse`로 매 틱 전진(서버 `AttackState.Tick` 거울), replay는 `InputRecord.ExternalVelX` 저장값 재생(`LastAppliedImpulseVx` 단일 경로 = 재계산 금지). 결정성 뿌리 = "클라 live도 서버와 같은 P4 공식".
- **5b 크러치 제거**: `ShouldForceAdopt`에서 `Attack && |vx|≥ε` 분기 + `serverVx` 인자 제거 → `teleportSnap` + `Hit`만 잔류. 진짜 mispredict는 `SnapThreshold`가 잡음(§1 유지).
- **facing v13**: 서버 `FacingDir`은 `C_MoveIntent.inputX`(입력 큐)로 파생되는데, 대쉬(`C_SkillUse`)는 잡으로 즉시 적용돼 방향 입력을 추월 → 옛 방향. 해법: `C_SkillUse.facing` append(v13) → 서버 `ActionGate`가 Dash일 때만 `FacingDir`=클라 facing 갱신(Validate 통과 후, 거부 시 부작용 0). **§1 위반 아님**: 방향은 원래도 클라 입력 파생 — 적용 권위만 서버가 행사.
- **원격 모션**: 썬더볼트 Channeling은 클라 로컬 선예측(`NotifyChannel`) 전용이라 서버 animState에 미포함(`ThunderboltAction`이 AttackState 미진입) → `RemotePlayerMotion._channelingRemaining` latch로 S_SkillCast 수신 시 캐스팅 지속 동안 Channeling 오버라이드(Hit/Death 우선). wire 무변경.

### 4. 테스트 결과

| 게이트 | 결과 |
|---|---|
| EditMode (클라 예측 결정성) | **122/0** — 임펄스 궤적 live==replay==서버시뮬 비트 정확 + 시작틱 1틱 오프셋 검출 |
| WSL2 서버 회귀 | **569/0** — facing 거동 박제 테스트(`Dash_Knight_ClientFacing_OverridesFacingDir`) + `ProtocolVersion_Is13` |
| reviewer Tier 2-A (facing) | **6축 0위반** — §2 append-only, §3 정규화, §1 미위반(방향=클라입력 파생), 무적/hitbox 영향 0 |
| Play 검증 (영호) | forceAdopt **완전 소멸**(499/500→0) + 손맛 "엄청 좋다" + 방향전환 대쉬 정상 + 원격 썬더볼트/대쉬 모션 정상 |
| Unity 컴파일 / 빌드 | 0 에러 / 빌드 성공(`C:\Dev\Build\Client`, 7씬, 551MB) |

### 5. 다음 스텝

- **PR**: 영호 명시 GO 게이트 (push + `gh pr create`). Shared.dll + ClientNet.dll commit 포함 → 03_Client CODEOWNERS(정유현) co-review 트리거 → admin bypass(사유 박기 + GO).
- **P6** (클래스 통합 + 전체 회귀): 넉백(피격 시점)/임펄스 공격(공격 시점)을 같은 기계로 통합 + 마일스톤 마감.
- **추적 약속 해소**: "넉백 예측 시 Hit 분기 재검토"(reviewer 🟡) — 넉백 방향 정보가 `S_EnemyAttack`에 없어 패킷 변경 필요 → 별도 Phase 후보로 이월.

---

## AC 검증 결과

| 완료 조건 (정의서 대조) | 결과 |
|---|---|
| (5a) `InputRecord.ExternalVelX` + replay 4-arg + `StartImpulse` + live 전진, 저장값=live 적용값 | ✅ |
| (5a) replay 결정성 — 오차 0, offset 누적 0 | ✅ EditMode |
| (5a) 임펄스 시작 틱 정렬 + 1틱 오프셋 검출 테스트 | ✅ |
| (5b) forceAdopt 빈도 대쉬당 ≤2 | ✅ 실측 **0** |
| (5b) 보간 복원 — 스터터 소멸, 손맛 실측 | ✅ |
| `PlayerPredictorTests`/`MovementGateTests` 확장·green | ✅ |
| 회귀 green: WSL2 + EditMode + Unity 0 + reviewer | ✅ |
| **(추가)** 방향전환 대쉬 클러스터 봉합 (facing v13) | ✅ |
| **(추가)** 원격 스킬 모션 전파 | ✅ |

검증 명령: `dotnet test Dawnholder.slnx --no-build` → `Passed! Failed: 0, Passed: 569, Skipped: 4, Total: 573`. EditMode = TestRunnerApi 122/0. reviewer 6축 0위반(facing 통합 리뷰). Play 2클라 실측(영호) — forceAdopt 로그 0, 방향전환 대쉬 정상, 원격 썬더볼트/대쉬 모션 정상.

---

## 결정 흐름

잔여 대쉬 클러스터의 **진단이 두 번 진화**했다 (이 Phase의 핵심 학습):

1. **큐 깊이 가설 → 기각**: 서버 `DashAction`에 진단 로그를 박고 측정하니 `queueDepth`가 항상 0~1 (스파이크 없음). 처음 만든 `offset` 로그는 `C_SkillUse.attackerClientTick`(서버틱 도메인, lag-comp용)과 `C_MoveIntent.clientTick`(클라 로컬 카운터, reconcile용)을 뺀 **무의미한 비교**였음 — 도메인 혼동(내 설계 실수).
2. **영호 단서로 핫스팟 확정**: "방향 틀고 대쉬하면 반대로 간다". 클라 `[Reconcile]` 로그에 컨텍스트(`impulseVx`/`anim`/`facing`)를 박아 재측정하니 서버 대쉬 `facingDir=1`(오른쪽) vs 클라 `impulseVx=-10`(왼쪽) — 방향 반대 명확.
3. **근본**: `C_SkillUse`(잡 즉시)가 `C_MoveIntent`(입력 큐) 방향 입력을 추월 → 서버가 옛 방향으로 대쉬. 위치(처음 의심)가 아니라 facing에서 터진 동일 비대칭.
4. **해법 A vs B**: A(wire v13, 클라 방향 권위) vs B(서버 큐 정렬). B는 대쉬 적용 시점을 옮겨 무적(InvulnUntilTick)/hitbox 타이밍을 흔드는 trust-boundary 리스크 → A 채택(방향은 원래 클라 입력, 신뢰 경계 불변).

---

## 학습 일지 후보 키워드

- **`dash-facing-client-authority`** (방향성 스킬 3번째 등장 시 박제): "§1은 *적용·판정 권위*를 서버에 두라는 것이지 *입력 출처*를 서버가 만들라는 게 아니다." 방향/조준 같은 클라 입력 파생 값은 클라가 보내고 서버가 정규화·적용하는 게 정석.
- **진단 도메인 혼동 경계**: prediction 로그를 만들 때 서로 다른 tick 도메인(서버틱 vs 클라 로컬 카운터)을 섞어 빼면 무의미. 측정 설계 자체를 의심하는 게 1차.
- **reconcile 스터터 = 다른 입력 출처로 같은 동작 계산**: jump-buffer-ack-vs-apply-split과 같은 뿌리. 이번엔 "방향"이 그 출처.
