# Phase NN: [Phase 제목] — 학습 일지

> **작성일**: YYYY-MM-DD
> **work-id**: phase{NN}-{slug}   ← ADR-018 합류 ID. 봉투/-DONE.md와 동일하게 박아 grep 회수 가능.
> **Phase 파일**: `phases/M{N}-{slug}/{NN}-{phase-name}.md`
> **소요 시간**: 예상 ?h / 실제 ?h
> **상태**: 작성 중 / 완료 / 추가 학습 필요

---

## 🎯 한 줄 요약

> 면접관이 "이 Phase에서 뭐 했어요?" 물으면 30초 안에 답할 한 문장.

_(예: TCP 리스너를 비동기로 구현하고 Session 객체로 연결을 관리하는 구조를 만들었어요.)_

---

## 📦 결과물

> 무엇을 만들었나? 코드 차원이 아니라 **기능/구조 차원**으로.

- _(예: server/Network/TcpServer.cs — 비동기 accept 루프)_
- _(예: server/Network/Session.cs — 연결당 객체)_
- _(예: server/Network/SessionManager.cs — 활성 세션 관리)_

---

## 🧠 새로 배운 것

> 이 Phase 전에는 몰랐거나 어렴풋했던 것들. **본인 말로** 적기.
> 모르는 건 모른다고 적기. "X는 아직 잘 모르겠음" 같은 표시도 OK.

### 개념 차원
- _(예: async/await가 스레드를 점유하지 않고 양보한다는 게 처음으로 진짜 이해됨.
       이전엔 "그냥 비동기겠지" 했는데, 100명 동접에 100스레드 안 쓰는 이유가
       이거였구나 깨달음.)_

### 구현 차원
- _(예: TcpListener.AcceptTcpClientAsync를 await하지 않으면 한 명만 받고 멈춘다는
       것을 시행착오로 배움. 처음엔 "왜 두 번째 클라가 연결 안 되지?"로 30분 헤맴.)_

### 도구 차원
- _(예: dotnet-counters로 실시간 메모리/스레드 카운트 보는 법 배움. 작업 관리자
       대신 이거 쓰면 .NET 내부 메트릭까지 보임.)_

---

## 🤔 결정 포인트

> 이 Phase에서 내린 결정과 그 이유. ADR로 격상할 만한 건 표시.

- **결정**: _(예: SessionManager는 ConcurrentDictionary로)_
  - **고려한 대안**: lock + Dictionary, Channel 기반 actor 모델
  - **선택 이유**: 가장 단순하고, MVP 동접 규모에 충분함
  - **트레이드오프**: 1만+ 동접 가면 lock 경합 가능 (그땐 다른 패턴)
  - **ADR 격상?**: 아직 (다음 Phase에서 패턴 굳어지면 ADR 작성)

---

## 🐛 막혔던 지점

> 이 Phase에서 진짜로 막혔던 것. 하나하나가 트러블슈팅 일지로 발전 가능.

- **증상**: _(예: 두 번째 클라이언트가 연결되지 않음)_
- **원인**: _(예: AcceptTcpClientAsync를 await하지 않고 바로 다음 줄로 넘어감)_
- **해결**: _(예: while 루프 + await 패턴으로 변경)_
- **소요 시간**: _(예: 30분)_
- **별도 일지**: `troubleshoots/2025-XX-XX-XXX.md` (작성했다면 링크)

---

## 💡 다시 한다면

> 지금 다시 시작한다면 무엇을 다르게 할까. **이게 진짜 학습의 증거**.

- _(예: 처음부터 ILogger를 DI로 받는 패턴으로 갔을 것. 나중에 갈아끼우는 게 더 큰 일.)_
- _(예: Session.cs에 너무 많은 책임 — 다음 Phase에서 SendQueue 분리 예정)_

---

## ❓ 아직 모르는 것 / 다음에 배울 것

> 이 Phase에서 등장했지만 깊이 못 판 것. 미래의 학습 큐.

- _(예: TaskScheduler와 ThreadPool의 차이는 아직 표면적 이해)_
- _(예: TCP의 Nagle algorithm 영향 — Phase 04에서 더 봐야 함)_

---

## 🎤 면접 시뮬레이션

> 면접관이 이 Phase에 대해 물을 만한 질문 + 본인 답변 미리 적어보기.
> **이 답을 못 적는다면 학습 부족.**

**Q: "이 Phase에서 가장 어려웠던 건 뭐예요?"**
A: _(본인 답)_

**Q: "왜 ConcurrentDictionary를 골랐어요?"**
A: _(본인 답)_

**Q: "비동기 accept 루프가 어떻게 100명 동접을 한 스레드로 처리해요?"**
A: _(본인 답)_

---

## 🔗 관련 링크

- Phase 파일: `phases/M{N}-{slug}/{NN}-{name}.md`
- 관련 ADR: ADR-XXX
- 관련 개념 일지: `concepts/async-await.md`, ...
- 관련 트러블슈팅: `troubleshoots/...`

---

## 작성 메모

> 일지 자체에 대한 메모. 미완성이면 무엇이 빠졌는지.

- [ ] 면접 시뮬레이션 답변 미작성
- [ ] "다시 한다면" 부분 더 깊이 생각 필요
- [ ] 관련 개념 일지 1~2개 추가 작성 예정
