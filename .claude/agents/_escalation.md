# Agents Escalation — 실패 시 모델 상향 + 사용자 escalate

> 본 문서는 SubAgent 작업 실패 시 *에스컬레이션 절차*. WHY는 [`../policies/subagent-routing.md`](../policies/subagent-routing.md) "에스컬레이션 룰" 절.

---

## 1. Worker 작업 실패 (Sonnet 2회 → Opus → 사용자)

> **선택적 Opus 상호작용 (2026-06-13 영구)**: `복잡+trust-boundary` / `대규모` Phase는 구현 Worker가 *처음부터 Opus*(routing §5.5). 따라서 아래 'Sonnet 2회 → Opus' 흐름은 그 미만(단순/보통/복잡-non-tb)에만 적용된다.

```
[1차 시도 — Sonnet, Worker A]
   │
   ├─ 성공 → 결과 반환
   │
   └─ 실패 (빌드 깨짐 / 테스트 미달 / 명세 미달)
       │
       ├─ work-pin에 사유 박힘:
       │   "에스컬레이션: <worker-name> 1차 실패 — <사유 한 줄>"
       │
       ▼
[2차 시도 — Sonnet, 같은 Worker A]
       │
       ├─ 성공 → work-pin에 "에스컬레이션: Sonnet 2회" 박힘 + 결과 반환
       │
       └─ 실패
           │
           ├─ work-pin에 사유 박힘 (2차)
           │
           ▼
[3차 시도 — Opus 재호출 또는 다른 Worker 위임 (coordinator 판단)]
           │
           ├─ Opus 재호출: 같은 Worker, 모델만 Opus
           ├─ 다른 Worker: 분해 잘못 추정 시 (예: server → coordinator로 분해 요청)
           │
           ├─ 성공 → work-pin에 "에스컬레이션: Opus" 박힘 + 결과 반환
           │
           └─ 실패 → 사용자 escalate
```

### 사용자 escalate 양식

```
⚠️ Worker 에스컬레이션 — 3차 시도 후에도 실패

SubAgent: <name>
작업: <한 줄>
실패 사유: <마지막 시도 에러 한 줄>

옵션:
  1) 본인이 직접 코드 들어가기
  2) 다른 SubAgent에 재위임 (예: server → coordinator로 분해 요청)
  3) Phase 분해 재검토 (작업 단위가 너무 컸을 가능성)
```

### 박힘 정신 (work-pin 가시화)

에스컬레이션 매번 work-pin에 박힘 — *Opus 호출 비용 인식* + *무한 호출 사고 차단*.

M4 진입 후 *Opus 발동 주간 카운트* 추적 → 잦으면 등급 체계 재조정 (처음부터 Opus 등급 추가 후보).

---

## 2. Reviewer 위반 발견 (Tier 2-A)

```
[Worker 작업 완료 → reviewer 자동 호출]
   │
   ├─ 🟢 위반 0개 → 통과 + 메인 세션 반환
   │
   ├─ 🟡 개선 제안만 → 통과 + 사용자 노출 (수정 강제 X)
   │
   └─ 🔴 위반 있음
       │
       ├─ 사용자 확인: "고칠까요?"
       │
       ├─ 사용자 "고치자" → 같은 도메인 Worker 재위임 (1회만)
       │   │
       │   ├─ 성공 → 통과
       │   │
       │   └─ 실패 → 사용자 escalate (위 Worker 에스컬레이션 흐름)
       │
       └─ 사용자 "패스" → work-pin에 박힘:
           "리뷰 패스 사유: <한 줄>"
           ↓
           통과 (단 사유 흔적 영구 잔존)
```

**재위임은 1회**. 같은 위반이 2회 째 발견되면 *분해 잘못 추정 신호* → coordinator escalate.

---

## 3. Plan-auditor 결함 발견 (Tier 2-B)

```
[plan 또는 Phase 정의 Write → plan-auditor 자동 호출]
   │
   ├─ 🟢 결함 0개 → Phase GO
   │
   ├─ 🟡 개선 제안만 → 사용자 결정 (반영 vs 진행)
   │
   └─ 🔴 결함 있음
       │
       ├─ 사용자에게 옵션 제시:
       │
       ├─ 옵션 A (즉시 봉합 권장 — 특히 irreversible 위험 시):
       │   ├─ plan / Phase 정의 갱신
       │   └─ plan-auditor 재호출 → 통과 후 Phase GO
       │
       └─ 옵션 B (현 상태 진행):
           ├─ work-pin에 박힘:
           │   "plan 결함 잔존: <한 줄> — 별 Phase 봉합 예정"
           └─ Phase GO (위험 인지 + 잔존 결함 후속)
```

옵션 A/B 권유는 *균형*. 단 *비가역* (Protocol.Version bump / DB migration / main 푸시) 위험 시 옵션 A *강력 권유*.

---

## 4. 권한 위반 시 (Worker 자기 영역 외 작업 요청)

```
[Worker가 권한 범위 외 파일 수정 시도 발견]
   │
   ├─ 즉시 거부 (Edit/Write 실패)
   │
   ├─ coordinator에게 보고:
   │   "권한 외 작업 필요 — <도메인>: <파일> — <Worker명> 위임 요청"
   │
   └─ coordinator가 적절 Worker 재위임 또는 분해 재검토
```

권한 경계 = [`_routing.md`](_routing.md) "권한 경계" 절.

---

## 5. 경계 코드 정합 충돌 (Worker A 결과 vs Worker B 결과)

```
[coordinator가 결과 통합 검증]
   │
   ├─ 정합 OK → 통합 보고 반환
   │
   └─ 충돌 발견 (예: client측 PacketID == 5 사용, shared측 정의는 PacketID == 6)
       │
       ├─ 충돌하는 Worker에 *재위임 1회* — 정정 요청
       │
       ├─ 성공 → 정합 재검증 → 통과
       │
       └─ 실패 → 사용자 escalate + 분해 재검토 권유
```

---

## 6. 재귀 호출 시도 (절대 차단)

```
[Worker가 다른 Worker 직접 호출 시도]
   │
   └─ 구조적으로 차단 (Hook 강제 아님):
       Worker는 위임 권한(Agent/Task tool) 없음 + coordinator만 단독 위임자.
       circuit-breaker.sh는 *반복 도구 사용 알림* advisory일 뿐 —
       Worker→Worker 재귀 판정 로직은 미실재. 차단은 구조/규율로 강제.
       ↓
       coordinator에게 분해 요청으로 escalate
```

```
[Coordinator가 다른 Coordinator 호출 시도]
   │
   └─ 차단 — 분해 너무 깊으면 Phase 자체 잘못 추정 신호
       ↓
       사용자 escalate + Phase 분해 재검토
```

---

## 7. 사용자 우회 (S-1: 사유 명시 후 허용)

사용자가 *"리뷰 스킵"* / *"plan 점검 스킵"* / *"본인이 직접 분해"* 명시 시:

1. 자동 호출 강제 *해제*
2. work-pin에 박힘:
   ```
   <자동화> 스킵 사유: <사용자가 제공한 한 줄>
   ```

이 흔적은 `grep "스킵 사유"`로 한 방에 회수 — *우회 습관화* 감지 가능. 주간 3회 초과 시 트리거 조건 재설계 신호.

---

## 8. 무인 루프 분기 (loop-driven, M7.5)

루프 엔진이 작업을 자율 구동할 때의 에스컬레이션:

- **v1 (attended)**: 모든 escalate(Worker 3차 실패 / reviewer 🔴 / 권한 위반)는 *즉시 사람에게 도달* — 사람이 그 자리에 있어 GO/판단. v1은 본질적으로 attended라 §1~§7 흐름 그대로.
- **비가역(버킷 c) 동반 실패 = 루프 정지**: push/PR/merge/`Protocol.Version`/trust-boundary 작업이 실패하면 자율 재시도 X → 즉시 사람 게이트 정지 ([`../policies/work-judge.md`](../policies/work-judge.md) 버킷 c 졸업 불가).
- **circuit halt (v2 선결)**: 무인 토큰/시간 폭주는 `circuit-breaker.sh`가 halt 신호 파일 기록 → 드라이버가 폴링해 멈춤 (P05). hook은 루프를 직접 못 죽임. v2(무인)는 이 폴링이 선결이라 defer.

엔진·정지 게이트 → [`../policies/loop-driver.md`](../policies/loop-driver.md) §3·§5.

---

## 변경 시 동기화 책임

본 문서 수정 시 *반드시* 함께 갱신:
- [`../policies/subagent-routing.md`](../policies/subagent-routing.md) (에스컬레이션 룰 원칙)
- [`../policies/loop-driver.md`](../policies/loop-driver.md) · [`../policies/work-judge.md`](../policies/work-judge.md) (무인 루프 분기 + 비가역 버킷 c 정지)
- [`coordinator.md`](coordinator.md) (에스컬레이션 절차 카탈로그)
- [`../hooks/circuit-breaker.sh`](../hooks/circuit-breaker.sh) (Phase 03 산출물 — *반복 도구 사용 알림* advisory. 재귀 차단은 hook 아닌 구조/규율 강제)
- 각 SubAgent의 "에스컬레이션 룰" 절

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 02 (3/3) 신설. Sonnet 2회 → Opus → 사용자 흐름 + Reviewer 위반 재위임 1회 + Plan-auditor A/B 옵션 + 권한 위반 + 경계 코드 충돌 + 재귀 차단 + 사용자 우회 통합.
- 2026-05-24 — `/harness-review all` 봉합. §6 "circuit-breaker.sh가 재귀 차단" false claim 정정 — 재귀 차단은 구조/규율 강제(Worker 무위임 + coordinator 단독), circuit-breaker는 반복 도구 알림 advisory. line 193 참조 동반 정정.
