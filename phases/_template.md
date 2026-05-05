# Phase NN: [Phase 제목]

> **상태**: pending | in-progress | done | blocked
> **마일스톤**: M1 / M2 / ...
> **예상 소요**: 1~3시간
> **담당 에이전트**: netcode | gameplay | client | content | persistence | qa-sim

---

## 🎯 목표

> 이 Phase가 끝나면 무엇이 동작해야 하는가? 한두 문장으로.

_(예: 서버가 TCP 포트 7777에서 클라이언트 연결을 받아 Session 객체로 관리할 수 있다.)_

---

## ⏪ 사전 조건

> 이 Phase 시작 전에 끝나있어야 할 것들.

- [ ] _(예: Phase 01 — 서버 프로젝트 부트스트랩 완료)_
- [ ] _(예: Shared 라이브러리 csproj 참조 가능)_

---

## 📝 작업 내용

> 구체적인 체크리스트. 너무 잘게 쪼개지 말고, 의미 있는 단위로.

- [ ] _(예: TcpListener를 server/Network/에 구현)_
- [ ] _(예: Session 클래스 정의 (ID, 소켓, 연결 시각))_
- [ ] _(예: Program.cs에서 Listener 시작/종료)_
- [ ] _(예: 단위 테스트 추가)_

---

## ✅ 완료 조건

> 어떻게 "끝났다"를 객관적으로 판단할지.

- [ ] _(예: `dotnet run` 으로 서버 시작 시 "Listening on :7777" 로그 확인)_
- [ ] _(예: telnet 또는 nc로 5번 연속 연결/해제 시 서버가 안 죽음)_
- [ ] _(예: 단위 테스트 N개 모두 통과)_

---

## 🧪 테스트

> 검증 방법 (자동/수동 둘 다).

**자동 테스트:**
- _(예: SessionManagerTests — 동시 100개 연결 처리)_

**수동 테스트:**
- _(예: 서버 켜고, 별도 터미널에서 `nc localhost 7777` 5회 반복)_

---

## 📚 학습 포인트

> 이번 Phase에서 새로 만나는 개념. AI가 보고에서 다뤄줘야 할 키워드들.

- _(예: TcpListener vs Socket — 추상화 레벨)_
- _(예: async/await가 왜 네트워크 코드에 적합한가)_
- _(예: IDisposable 패턴과 using statement)_

---

## ⚠️ 함정 / 주의사항

> 이 Phase에서 흔히 빠지는 함정.

- _(예: AcceptTcpClientAsync를 await하지 않으면 한 명만 받고 멈춤)_
- _(예: Session 종료 시 NetworkStream을 닫아야 메모리 누수 없음)_

---

## ➡️ 다음 Phase

> 이 Phase가 끝나면 자연스럽게 이어지는 다음 작업.

- _(예: Phase 03 — Packet Framing: 들어온 바이트를 패킷 단위로 자르기)_

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
