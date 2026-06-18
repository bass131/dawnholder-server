---
description: 리뷰용(pull) 세션 시작 — 구현이 아니라 *깊은 학습·점검*. pending-comprehension 원장 열고 "이거 어떻게/왜 구현했어?" 깊은 설명 모드. loop-driven 세션 2종 중 리뷰 축.
---

[`/session:start`](start.md)(구현 세션)와 **짝** 커맨드. 본 슬래시는 **리뷰용(pull) 세션**입니다. 루프가 구현을 빠르게 처리하는 동안 *미뤄둔 깊은 학습·점검*을 여기서 몰아 봅니다 (ADR-032 §D: 구현과 학습 분리 / [`loop-driver.md`](../../../00_Document/policies/loop-driver.md) §6).

> **왜 분리?** 구현 세션은 "흐름 안 끊김"이 목표라 깊은 학습을 끼우면 처리량이 깎입니다. 그렇다고 학습을 버리면 button-pusher가 됨(영호는 engineer로 남아야 함). 그래서 *별도 pull 세션*으로 분리 — 영호가 *원할 때* 깊게 판다. (ADR-025가 죽인 "트랙 B 쌓이기만" 함정의 회피 = pull-분리 + 가시 원장.)

---

### 0. git 안전 (가벼운 점검)

리뷰 세션은 보통 코드를 안 바꿉니다. `git status -sb`로 **현재 브랜치 + dirty 여부만 알림**(작업 풀게이트 아님). 리뷰 중 실제 수정이 생기면 그때 [`/session:start`](start.md) 작업 흐름으로 전환.

### 1. pending-comprehension 원장 열기

[`00_Document/ledgers/pending-comprehension.md`](../../../00_Document/ledgers/pending-comprehension.md) 통독 → **"아직 깊게 안 판 항목" 목록**을 영호에게 제시:

```
📚 깊게 안 본 항목 (pending-comprehension):
  1. [M7.5] /engine:goal done 심판 루프 — 어떻게 외부 게이트로 판정하나
  2. [M4.8] DeferredDamageSystem 틱 카운트다운 — 왜 논블로킹인가
  ...
어느 걸 깊게 볼까요? (번호 / "전부" / "오늘은 패스")
```

### 2. 깊은 설명 모드 (학부생 멘토링)

영호가 항목을 고르면:
- **file:line 실측 먼저** (추측 X) → "어떻게 / 왜 이렇게 구현했나" + 트레이드오프 + 안 고른 대안.
- 구현 세션의 *흐름 안 끊김*과 **반대** — 여기선 충분히 멈춰서 깊게. 확인 질문으로 이해 점검.
- 이해 끝난 항목은 **원장에서 체크/제거**(사람 확인 후).

### 3. 톤

멘토링·학습 집중. 구현 재촉 X. "이해했어" 답엔 핵심 개념 확인 질문 (헌법 사용자 컨텍스트).

---

### 작업 세션과 차이

| | `/session:start` | `/session:review` (본 커맨드) |
|---|---|---|
| 목적 | 구현 (루프 구동) | 깊은 학습·점검 (pull) |
| git | 작업 안전 풀게이트 | 가벼운 상태 알림 |
| pending-comprehension | *적재* (미뤄둠) | *소비* (깊게 봄) |
| 호흡 | 흐름 안 끊김 | 충분히 멈춤 |

### 관련

- 세션 2종 개념 → [`loop-driver.md`](../../../00_Document/policies/loop-driver.md) §6
- pending-comprehension 원장 → [`00_Document/ledgers/pending-comprehension.md`](../../../00_Document/ledgers/pending-comprehension.md)
- 구현 세션 → [`/session:start`](start.md)
