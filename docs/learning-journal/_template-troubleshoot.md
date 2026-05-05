# 트러블슈팅: [버그 한 줄 요약]

> **작성일**: YYYY-MM-DD
> **발생 Phase**: Phase NN
> **소요 시간**: ?h
> **재발 위험**: 🔴 높음 / 🟡 중간 / 🟢 낮음 (가드레일 추가했는지)

---

## 🚨 증상

> 무엇이 잘못 보였나. 사실만. 아직 원인 추측 금지.

_(예: 두 번째 클라이언트가 서버에 연결되지 않음. 첫 클라는 정상.)_

**재현 절차:**
1. _(예: dotnet run으로 서버 시작)_
2. _(예: nc localhost 7777로 클라1 연결 — 정상)_
3. _(예: 다른 터미널에서 nc localhost 7777로 클라2 연결 — 멈춤)_

**관찰된 것:**
- _(예: 서버 콘솔에 "Session ... connected" 한 번만 찍힘)_
- _(예: nc 클라2는 응답 없이 대기)_
- _(예: CPU 사용률 0% — 무한 루프는 아님)_

**기대했던 것:**
- _(예: 두 번째 연결도 똑같이 "Session ... connected" 찍히고, SessionManager.Count == 2)_

---

## 🔍 가설과 시도

> 머릿속에 떠오른 원인 후보들. 시간순으로.

### 가설 1: 포트가 한 명만 받는다
- **근거**: TCP는 처음 들어보는 분야. 그럴 수도 있겠다 싶었음.
- **시도**: 검색 → 그건 아님. TcpListener는 큐 사이즈만큼 동시 받음.
- **결과**: 기각. 다른 가설로.

### 가설 2: SessionManager 추가가 잘못됐다
- **근거**: 동시성 문제일 수도?
- **시도**: SessionManager.Add 직후에 Console.WriteLine 추가
- **결과**: **이 줄이 안 찍힘.** 즉 Add까지 도달도 못 함.
  → SessionManager는 무관. 더 위쪽(Accept)에서 막힘.

### 가설 3: AcceptTcpClientAsync가 다음 호출 안 됨
- **근거**: 첫 연결 후 두 번째 Accept가 안 일어나는 것 같음.
- **시도**: AcceptTcpClientAsync 줄에 breakpoint 찍고 디버그
- **결과**: **빙고.** Accept는 한 번만 호출되고, Session.RunAsync로 들어가서
  거기서 멈춤.

---

## 🎯 진짜 원인

> 정확히 무엇이 문제였나. 학습 포인트가 여기에 있음.

```csharp
// 잘못된 코드:
var tcpClient = await listener.AcceptTcpClientAsync(ct);
var session = new Session(tcpClient);
await session.RunAsync(ct);   // ← 이거! await하면 첫 세션 끝날 때까지 멈춤.
```

**메커니즘**: `await session.RunAsync(ct)`는 그 세션의 read 루프가 끝날
때까지 (= 클라가 disconnect 할 때까지) 다음 줄로 안 넘어감. 그래서
다음 `AcceptTcpClientAsync`가 호출되지 않음. → 두 번째 클라는 OS 레벨
backlog에서 대기 중이지만 application이 가져가질 않음.

**왜 헷갈렸나**: async 코드라서 "비동기니까 알아서 처리되겠지" 생각.
하지만 await는 "이 작업이 완료될 때까지 기다림"이라는 동기적 호출의
시맨틱을 갖고 있음. 비동기는 "스레드 점유 안 함"일 뿐.

---

## ✅ 해결

```csharp
// 수정된 코드:
var tcpClient = await listener.AcceptTcpClientAsync(ct);
var session = new Session(tcpClient);
_ = session.RunAsync(ct);   // ← fire-and-forget
// while 루프 다음 iteration으로 즉시 진행
```

**fire-and-forget 패턴**: 작업을 시작만 하고 결과를 기다리지 않음.
`_ =` 는 "이 Task를 의도적으로 await 안 한다"는 표시.

**주의**: fire-and-forget은 예외가 무시될 수 있음. 그래서 `Session.RunAsync`
내부에 try/catch + 로깅 필수.

---

## 🛡️ 재발 방지

> 같은 실수가 구조적으로 못 일어나게 하는 장치. **마구를 고치는 곳.**

- [x] **헌법(server/CLAUDE.md)에 명시**: "Accept 루프 안에서 Session.RunAsync는
      fire-and-forget. 절대 await 금지."
- [ ] **코드 주석**: TcpServer.cs의 `_ = session.RunAsync(ct);` 라인 위에
      주석 "// fire-and-forget. await하면 한 명만 받게 됨 (트러블슈팅 일지 참조)"
- [ ] **테스트**: 동시 5개 연결 통합 테스트. 한 번에 한 명만 받는 회귀를
      자동 검출.
- [ ] **(검토) Hook**: client/server 코드에서 `await.*RunAsync` 패턴이 Accept
      루프 안에 있으면 경고 — 너무 정교해서 보류.

---

## 💡 배운 것

> 한 줄짜리 정수.

- async ≠ "알아서 동시성 처리". `await`은 여전히 "다음 줄로 넘어가기 전에
  기다림"이라는 동기적 시맨틱.
- 동시성을 원하면 의도적으로 `_ =` (fire-and-forget) 또는 `Task.WhenAll`.
- 디버깅 1순위: "내가 기대한 곳에 진짜로 도달하나?" — 작은 print/breakpoint
  하나로 가설을 빠르게 검증.

---

## 🎤 면접 답변용 정리

> "기억나는 어려웠던 버그 있어요?" 질문에 90초로 답할 버전.

_(서사 형식으로. 본인 말로.)_

**예시 답:**
"두 번째 클라가 서버에 연결 안 되는 버그가 있었어요. async/await 처음 써보던
시기라, AcceptTcpClientAsync 후에 자연스럽게 await session.RunAsync()를
호출했거든요. 알고 보니 RunAsync는 클라가 끊길 때까지 안 끝나는 무한 루프라,
첫 세션이 끝날 때까지 다음 Accept가 안 일어나고 있었던 거였어요. fire-and-forget
패턴으로 바꿔서 해결했고, 그 일을 계기로 'await가 비동기지만 시맨틱은 동기적'
이라는 것을 진짜 이해하게 됐어요. 같은 실수 막으려고 헌법에 명시 + 통합
테스트도 추가했어요."

---

## 🔗 관련

- 관련 Phase: `phases/M1-foundation/03-tcp-listener.md`
- 관련 개념: `concepts/async-await.md`, `concepts/fire-and-forget.md`
- 관련 ADR: 없음 (코드 패턴 차원, ADR까지 갈 사안 아님)
