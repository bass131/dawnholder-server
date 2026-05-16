# 개념 사실 메모: tick 카운터에는 왜 `uint`인가

> **작성일**: 2026-05-16
> **작성 주체**: Claude (AI 사실 박제 — 본인이 본격 일지 작성 시 회고/자가평가만 채우면 됨)
> **등장 Phase**: M2 Phase 06 — input replay 기반 reconcile
> **트리거**: Phase 04에서 `int clientTick` / `int lastAckedClientTick`으로 자리잡아 둔 필드를 Phase 06 reconcile 들어가기 *직전*에 `uint`로 정합 정정할지 결정하는 자리에서 본인이 "왜 uint?"라고 물음.
> **상태**: 사실 카탈로그만 박제. 회고/자가평가 빈칸 — 본인이 본격 일지(`/journal:concept`)로 발전시킬 때 채움.

---

## 🎯 한 줄 정의

### `int` (signed 32비트 정수)
부호 있음. 범위 -2,147,483,648 ~ +2,147,483,647 (대략 ±21억). 맨 앞 비트가 부호 비트.

### `uint` (unsigned 32비트 정수)
부호 없음. 범위 0 ~ 4,294,967,295 (대략 0~42억). 맨 앞 비트도 그냥 큰 자리 숫자.

### tick 카운터
서버가 게임 시간을 자르는 단위. Dawnholder는 20 TPS = 50ms마다 +1. 서버 시작 = 0. **본질적으로 비음수인 단조 증가 카운터**.

---

## 🌱 사실 1 — 왜 `int`보다 `uint`가 정직한가

tick은 음수가 *무의미*한 값. 의미상 카운터.

- `int`: "음수도 가능" 이라고 *거짓말*하는 타입 선언
- `uint`: "0 이상" 이 타입 그 자체로 *진실*

도메인 의미가 타입에 박혀있으면 주석 없이도 코드 의도 드러남. 헌법 #2(Protocol is Sacred)의 "필드 의미 보존" 정신과 합치.

---

## 🌱 사실 2 — `int`로 두면 *실제로* 어떤 사고 패턴이 생기나

Phase 06 reconcile 의사 코드:

```csharp
foreach (var input in InputHistory)
    if (input.tick > snapshot.lastAckedClientTick)  // 미-ack 입력 골라내기
        position += input.inputX * MoveSpeed * TickDuration;
```

여기서 `int`라면 — 어떤 버그로 음수 tick이 흘러들어왔다고 가정:

```
input.tick = -5  (초기화 누락 등)
snapshot.lastAckedClientTick = 0
-5 > 0  →  false  →  replay에서 빠짐  →  "입력이 사라지는" 디버깅 지옥
```

`uint`로 박혀있으면 *컴파일러가* "여기 음수 못 들어와요" 거절 → 사고 원천 차단.

---

## 🌱 사실 3 — `uint`의 함정 (정직하게 짚어야 할 단점)

### (a) 음수 빼기 wrap

```csharp
uint a = 5;
uint b = 10;
uint diff = a - b;   // 기대: -5 / 실제: 4,294,967,291 (42억!)
```

음수 결과가 거대한 양수로 wrap. C 시절부터 unsigned 산술의 유명한 함정.

- **위험한 곳**: `clientTick - lastAckedTick` 같은 *차이* 계산.
- **Phase 06에서는?**: 대소 비교(`>`)만 쓰고 차이 계산 안 함 (replay는 큐 순회). **안전.**
- **만약 차이 필요해지면**: `(int)(a - b)` 캐스트로 의도 명시, 또는 `long`으로 비교.

### (b) C# BCL 관습은 `int`

.NET 표준 라이브러리가 거의 다 `int`(`List.Count`, `Array.Length` 등). interop 시 캐스트 한 번씩 발생.

- 우리는 *내부 프로토콜* 코드라 BCL 인터페이스 충돌 없음.
- 외부 라이브러리/JSON 직렬화에서 `uint` 처리 가끔 불완전 — 우리 PDL은 자체 직렬화라 영향 X.

### (c) wrap-around 자체는 `uint`도 막지 못함

- 42억 tick 이후 0으로 wrap.
- **계산**: 50ms tick × 42억 = 약 **6.8년 연속 실행**.
- 실질 영향 0. 진짜 sequence number wrap 처리(TCP·RTP의 *circular comparison*)는 지금 단계 불필요.

---

## 📊 비교 표

| 항목 | `int` 유지 | **`uint` 변경** |
|------|---------|----------------|
| 도메인 의미 정합 | ❌ 거짓말 | ✅ 정직 |
| 음수 사고 차단 | ❌ 음수 흘러도 통과 | ✅ 컴파일 거절 |
| 차이 계산 wrap | ✅ 자연스러움 | ⚠️ 캐스트 필요 (Phase 06 미사용) |
| BCL 호환 | ✅ 매끄럽 | ⚠️ 가끔 캐스트 |
| Phase 06 영향 | 사용처마다 캐스트 산재 | PDL 1줄 + 직렬화 영향 |

---

## 🔗 등장 맥락 (이 메모가 생긴 흐름)

1. Phase 04에서 `C_MoveIntent.clientTick` + `S_Snapshot.lastAckedClientTick`을 `int`로 미리 자리잡음 ("Phase 06 replay 대비, 지금은 미사용 0" 주석).
2. Phase 06 진입 직전 정의 파일 통독: 정의 파일은 *`uint`*로 박혀있음.
3. 작업핀에 "uint 캐스트 정합 정정"이 Phase 06 흡수 항목으로 등록됨.
4. Step 1(PDL 수정)에서 (A) `int` 유지 + 사용처 캐스트 vs (B) `uint`로 변경 결정 갈림.
5. **(B) 채택** — tick의 본질이 비음수 카운터라는 도메인 의미가 BCL 관습보다 우선.

---

## ✅ 후속 액션 (Phase 06 본 작업에서 자연 흡수)

- [ ] Step 1: PDL `int clientTick` / `int lastAckedClientTick` → `uint`로 변경, PacketGenerator 재실행
- [ ] Step 1: `Protocol.Version` bump (헌법 #2 — 필드 타입 폭 같지만 의미 변경)
- [ ] Step 2~5: 서버/클라 사용처가 `uint`로 자연 컴파일되는지 확인 (대부분 그냥 통과)
- [ ] Step 5: reconcile 큐 순회 시 차이 계산 *안 들어가는지* 코드 리뷰 (`uint` 함정 (a) 회피 확인)

---

## ✍️ 본인 회고 (빈칸 — 본격 일지 쓸 때 채울 자리)

> **AI 메모**: 아래는 본인이 직접 채울 영역. Claude가 채우면 가짜 학습.

- [ ] 본인이 옛 코드(DX9 또는 다른 프로젝트)에서 `int` vs `uint` 헷갈렸던 경험이 있었나?
- [ ] 이번 결정에서 어떤 부분이 *직관*과 다르게 다가왔나? (예: "C# BCL이 int 쓰는데 우리는 uint?")
- [ ] 면접 답으로 발전시킨다면 한 줄 답은? ("왜 uint?" → "...")
- [ ] 이해도 자가평가 (🔴 표면 / 🟡 대략 / 🟢 설명 가능)
