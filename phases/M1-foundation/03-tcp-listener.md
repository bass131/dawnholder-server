# Phase 03: TCP 리스너 + Session 객체

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 2~3시간
> **담당 에이전트**: `netcode`

---

## 🎯 목표

서버가 TCP 포트 7777에서 클라이언트 연결을 받아들이고, 각 연결을
`Session` 객체로 래핑해서 관리. telnet으로 연결/해제 5번 반복해도
서버가 안 죽고 로그가 깔끔하게 찍히는 상태.

**이 Phase가 중요한 이유**: 이게 진짜 "MMO 서버의 시작"이에요. 여기서
배우는 패턴(비동기 accept 루프, 연결당 객체)이 모든 후속 네트워크
코드의 토대가 돼요.

---

## ⏪ 사전 조건

- [x] Phase 02 완료 (shared 라이브러리 + 상수)

---

## 📝 작업 내용

### 디렉토리 구조 만들기
- [ ] `server/GameServer/Network/` 폴더 생성
- [ ] 그 안에 `TcpServer.cs`, `Session.cs`, `SessionManager.cs` 생성 예정

### Session 클래스 (`server/GameServer/Network/Session.cs`)
- [ ] 필드: 고유 ID(GUID), TcpClient, 연결 시각, 마지막 활동 시각
- [ ] 생성자: TcpClient 받아서 ID 발급, 시각 기록
- [ ] `Task RunAsync(CancellationToken ct)` — 이 세션의 read 루프
  - 일단 read 루프는 비워둠 (Phase 04에서 채울 것)
  - "연결 시작" / "연결 종료" 로그만 찍기
- [ ] `void Disconnect()` — 안전한 종료. 스트림 닫기, TcpClient.Close().
- [ ] `IDisposable` 패턴 구현

### SessionManager (`server/GameServer/Network/SessionManager.cs`)
- [ ] 활성 세션 목록을 thread-safe하게 관리 (`ConcurrentDictionary<Guid, Session>`)
- [ ] `Add(Session)`, `Remove(Guid)`, `Count` 프로퍼티
- [ ] `DisconnectAll()` — 종료 시 호출

### TcpServer (`server/GameServer/Network/TcpServer.cs`)
- [ ] 생성자: 포트 받기, SessionManager 주입
- [ ] `async Task StartAsync(CancellationToken ct)`
  - `TcpListener.Start()`
  - 무한 루프에서 `AcceptTcpClientAsync(ct)`
  - 새 연결마다 `Session` 생성, SessionManager에 추가, `Session.RunAsync` 시작
  - ct가 취소되면 깔끔하게 빠져나옴
- [ ] `void Stop()` — Listener.Stop() + 모든 세션 disconnect

### Program.cs 업데이트
- [ ] CTRL+C 핸들링: `CancellationTokenSource` 만들고 `Console.CancelKeyPress`에 연결
- [ ] TcpServer 인스턴스화 후 `await StartAsync(cts.Token)`
- [ ] 종료 시 통계 로그 ("총 N개 연결을 처리했습니다")

### 단위 테스트 (`server/GameServer.Tests/Network/`)
- [ ] `SessionManagerTests`:
  - 빈 상태에서 Count == 0
  - Add 후 Count == 1
  - Remove 후 Count == 0
  - 같은 ID로 두 번 Add 시도 시 동작 확인 (override? throw?)
- [ ] 통합 테스트는 Phase 04에서 (실제 패킷 주고받을 때)

---

## ✅ 완료 조건

- [ ] `dotnet build` + `dotnet test` 통과
- [ ] `dotnet run` 실행 시 "Listening on :7777" 로그 출력
- [ ] **수동 테스트 시나리오 통과**:
  1. 서버 켜기
  2. 다른 터미널에서 `nc localhost 7777` (또는 telnet)
  3. 서버 로그에 "Session {guid} connected" 표시
  4. nc 종료 (Ctrl+C)
  5. 서버 로그에 "Session {guid} disconnected" 표시
  6. **이걸 5번 반복해도 서버가 안 죽음**
- [ ] 동시에 5개 연결 가능 (5개 터미널에서 동시 nc)
- [ ] 서버 Ctrl+C 시 모든 세션이 깔끔하게 닫힘 + 통계 출력

---

## 🧪 테스트

**자동 테스트:**
- `SessionManagerTests.Count_StartsAtZero`
- `SessionManagerTests.Add_IncrementsCount`
- `SessionManagerTests.Remove_DecrementsCount`
- `SessionManagerTests.Add_AndRemove_HandlesUnknownId`

**수동 테스트 절차:**
```bash
# 터미널 1
dotnet run --project server/GameServer

# 터미널 2 (반복 5회)
nc localhost 7777
# Ctrl+C로 종료
```

서버 로그가 깔끔한지, 메모리/CPU가 비정상 안 올라가는지 (작업 관리자
혹은 `dotnet-counters monitor`) 관찰.

---

## 📚 학습 포인트

이번 Phase는 학습 포인트가 많아요. 각 항목은 `/concept` 커맨드로 더
깊이 파볼 수 있어요.

### 1. async/await 와 비동기 I/O
- `AcceptTcpClientAsync`를 `await`하면 OS 커널이 연결을 가져올 때까지
  현재 스레드를 양보. CPU 안 씀.
- 동기 `AcceptTcpClient()`는 스레드를 점유하면서 기다림. 100명 동접 시
  100개 스레드 필요. 비효율.
- async I/O는 적은 수의 스레드로 많은 연결 처리. MMO 서버의 기본.

### 2. CancellationToken 패턴
- 비동기 작업을 우아하게 취소하는 표준 메커니즘.
- 호출 체인을 따라 ct를 전달. 어디서든 ct.IsCancellationRequested
  체크 가능.
- 우리 패턴: 메인의 cts → TcpServer → 각 Session까지 같은 ct 전달.
- Ctrl+C 핸들러에서 cts.Cancel() 호출 → 줄줄이 깔끔하게 종료.

### 3. ConcurrentDictionary
- 일반 Dictionary는 thread-safe 아님. 동시 접근 시 깨짐.
- ConcurrentDictionary는 lock 내부적으로 처리. TryAdd, TryRemove 등 제공.
- SessionManager는 여러 스레드에서 접근될 가능성 (accept 루프 + 각 세션
  종료 시) → ConcurrentDictionary 적절.

### 4. IDisposable 패턴
- 비관리 리소스(소켓, 파일 핸들 등) 정리용 인터페이스.
- `using (var s = new Session(...))` 또는 `using var s = ...` 패턴.
- TcpClient는 IDisposable. Session도 그걸 감싸니 IDisposable.

### 5. 스레드풀
- async I/O 완료 시 콜백은 스레드풀의 워커 스레드에서 실행.
- 우리가 직접 스레드 생성 안 함. 런타임이 알아서 풀링.
- 스레드풀 고갈은 별도 주제 (성능 튜닝 시 다룸).

---

## ⚠️ 함정 / 주의사항

- **AcceptTcpClientAsync를 await 안 하면**: 한 명만 받고 멈춤. 또는
  무한 루프에 들어가서 CPU 폭주. **반드시 await + while loop**.
- **Session.RunAsync를 await 하면**: 첫 세션이 끝날 때까지 다음 accept
  안 됨. **fire-and-forget 패턴 사용** (`_ = session.RunAsync(ct);`).
  단 fire-and-forget은 예외 추적이 어려우니 try/catch + 로깅 필수.
- **TcpClient 미닫힘**: NetworkStream을 안 닫으면 소켓이 TIME_WAIT에
  쌓여서 메모리 누수. 항상 IDisposable로 정리.
- **동기 호출 섞기**: 한 메서드 안에서 동기/비동기 섞으면 데드락 가능.
  async 메서드 안에서는 시종일관 await 사용.
- **Listener.Stop() 후 AcceptTcpClientAsync는 예외 던짐**: 정상.
  ObjectDisposedException 또는 OperationCanceledException을 catch해서
  깔끔하게 빠져나가게.

---

## ➡️ 다음 Phase

**Phase 04: 길이 프리픽스 프레이밍 + 첫 ping/pong**
- 이번 Phase에서 만든 read 루프를 진짜로 채움
- TCP 스트림에서 패킷 단위로 자르는 framing 구현
- 첫 패킷(C2S_Ping → S2C_Pong) 정의 + 핸들러
- nc 대신 간단한 .NET CLI 클라이언트 만들어서 ping 보내기

---

## 작업 로그
