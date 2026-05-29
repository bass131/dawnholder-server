---
title: 03장 소켓 프로그래밍
source: 게임 서버 프로그래밍 교과서
category: 3장 — 소켓 프로그래밍
---
# [GSP-03] 소켓 프로그래밍 (Socket Programming)
> 블로킹 소켓 → 논블록 소켓 → Overlapped I/O → epoll → IOCP 순으로 한계를 극복해 온 과정. ServerCore의 IOCP 구현이 왜 그 형태인지 맥락.

---

## 언제 참조하나 (트리거)
- ServerCore의 IOCP/RecvBuffer/SendBuffer 코드를 읽거나 수정할 때
- "왜 accept 스레드가 따로 있나", "왜 send가 즉시 리턴하나" 질문이 나올 때
- 새 플랫폼(Linux, macOS) 포팅을 고려할 때 — epoll vs kqueue vs IOCP 비교 근거
- 블로킹/논블록 소켓 기초를 모르는 팀원에게 설명할 때

---

## 핵심 내용

### 3.1 블로킹 소켓 (0172 — 무료 접근)
- 소켓은 파일 핸들과 같다: 읽기/쓰기 인터페이스, 커널이 실제 I/O 처리.
- **블로킹**: I/O 요청 후 완료될 때까지 스레드가 waitable 상태로 잠든다. CPU 사용 0%.
- 상대방이 데이터를 보내지 않으면 `recv()`는 **무한정** 블로킹될 수 있다 — 게임 서버에서 방치하면 스레드 고갈.
- 블로킹 자체는 나쁜 것이 아님; 1:1 단순 연결이라면 이해하기 쉽고 코드가 직선적.

### 3.2 네트워크 연결 및 송신 (0173 — 무료 접근)
- TCP = 연결 지향, 1:1 통신. 하나의 소켓은 하나의 원격 엔드포인트(IP:Port)에만 연결.
- 클라이언트 흐름: `socket()` → `bind(any_port)` → `connect(server)` → `send()` → `close()`.
- 엔드포인트: "IP주소 + 포트" 쌍. `55.66.77.88:5959` 형태로 표기.
- `connect()`는 블로킹이고, 서버가 `accept()` 할 때까지 대기한다.

### 3.3 블로킹과 소켓 버퍼 (0175 — 무료 접근)
- `send(data)` 호출은 **즉시 리턴**한다 — 상대방에게 도달한 게 아니라 **송신 버퍼에 복사**되었을 뿐.
- 각 소켓은 커널이 관리하는 **송신 버퍼(send buffer)** + **수신 버퍼(recv buffer)** 두 개를 가진다.
- 송신 버퍼가 꽉 차면 그때서야 `send()`가 블로킹된다 (버퍼에 공간이 생길 때까지 대기).
- 커널이 버퍼에서 꺼내 NIC(네트워크 카드)로 보내는 것은 애플리케이션과 비동기적으로 일어난다.
- 수신 버퍼: 상대방이 보낸 데이터를 커널이 먼저 받아 쌓아두고, 애플리케이션이 `recv()`로 꺼냄.

### 3.4 네트워크 연결받기 및 수신 (0182 — 무료 접근)
- 서버 흐름: `socket()` → `bind(port)` → `listen()` → `accept()` → `recv()` 루프 → `close()`.
- `accept()`는 클라이언트가 올 때까지 블로킹. 반환값은 **클라이언트 전용 소켓** (listen 소켓과 별개).
- `recv()`는 수신 버퍼에 **1바이트라도** 있으면 즉시 반환; 비어 있으면 데이터 도착까지 블로킹.
- 반환된 바이트 수가 0이면 상대방이 연결을 닫은 것 (정상 종료 감지 방법).

### 3.5 수신 버퍼가 가득 차면 (0185 — 무료 접근)
- 앱이 `recv()`를 너무 늦게 호출 → 수신 버퍼가 꽉 참 → TCP 흐름 제어가 발동 → 송신측 `send()`가 블로킹.
- `recv()`를 아예 안 하면 송신측은 영원히 블로킹. **연결은 끊기지 않고 느린 쪽 속도에 맞춰진다.**
- 이것이 TCP의 "신뢰성"의 일부: 데이터 유실 없이 배압(back-pressure) 전파.
- 게임 서버 영향: 틱 루프(20 TPS)가 recv를 제때 못 비우면 상대방 send가 막힘 → 체감 지연 급증.

### 3.6 논블록 소켓 (0188 — 무료 접근)
- **문제**: 블로킹 소켓 + 다중 클라 = 클라 1명당 스레드 1개. 스레드 1개 = 스택 ~1 MB. 1000명 = ~1 GB.
  컨텍스트 스위칭 폭발 → CPU 낭비.
- **해법**: 소켓을 논블록 모드로 전환. I/O가 즉시 처리 불가면 에러 코드(`EAGAIN`/`WSAEWOULDBLOCK`) 반환.
- 하나의 스레드가 여러 소켓을 폴링(polling)하는 패턴 가능해짐.
- **단점 3가지**:
  1. 보낼 공간 없으면 retry 루프 — CPU 스핀(바쁜 대기, busy-wait). 낭비.
  2. `send()`/`recv()` 호출마다 유저-커널 공간 데이터 복사 발생.
  3. `connect()`는 논블록에서 API 동작이 달라 별도 처리 필요 — API 일관성 없음.

### 3.7 Overlapped I/O / 비동기 I/O (0202 — 무료 접근)
- 논블록의 retry 낭비를 없애는 방향: **"완료되면 알려줘"** 패러다임.
- I/O 요청을 걸어두고 스레드는 다른 일 진행 → 커널이 완료 이벤트 통보.
- 논블록 대비 장점:
  1. retry 루프 제거 — CPU 낭비 없음.
  2. (IOCP 조합 시) 유저 버퍼를 커널이 직접 쓰는 zero-copy 가능.
  3. 취소(cancellation) 지원.
- 이 자체로 완결이 아니고, "완료 통보 수집기"가 필요 → Windows: IOCP, Linux: epoll + `io_uring`.

### 3.8 epoll (0213 — 무료 접근)
- Linux 전용. 소켓이 **I/O 가능 상태**가 되면 감지 + 사용자에게 알림.
- 내부적으로 이벤트 큐를 가짐: I/O 가능해진 소켓을 큐에 넣음 → 앱이 `epoll_wait()`으로 한 번에 수거.
- 핵심 강점: 소켓이 10,000개라도 **이 중 실제로 I/O 가능한 것만** 반환 — 폴링 루프 불필요.
- 등장: 2002년경 Linux 커널 2.5.44.
- iOS/macOS 등 FreeBSD 계열은 `kqueue`가 동일 역할.

### 3.9 IOCP (I/O Completion Port) (0219 — 무료 접근)
- Windows 전용. **Overlapped I/O의 완료**를 감지하는 메커니즘.
- 동작: Overlapped I/O 완료 시 커널이 IOCP 내부 큐에 완료 패킷(Completion Packet) 삽입 → 워커 스레드가 `GetQueuedCompletionStatus()`로 수거.
- 등장: 1993년 (Windows NT 3.5) — epoll보다 약 9년 먼저.
- epoll이 "I/O 가능 통보" (레벨/에지 트리거)라면, IOCP는 "I/O 완료 통보" — 더 높은 추상 레벨.
- 워커 스레드 수를 CPU 코어 수에 맞추면 컨텍스트 스위칭 최소화.
- 저자가 3장 마지막에 배치한 이유: 앞 개념(블로킹→논블록→Overlapped→epoll)을 모두 이해해야 IOCP가 왜 이 형태인지 납득됨.

---

## 우리 프로젝트 적용

| 개념 | 상태 | 위치 |
|------|------|------|
| IOCP 완료 포트 (SocketAsyncEventArgs) | **이미 사용 중** | `02_Server/Network/Session.cs`, `02_Server/Network/Listener.cs`, `02_Server/Network/Connector.cs` |
| Overlapped I/O (WSARecv/WSASend) | **이미 사용 중** | `02_Server/Network/RecvBuffer.cs`, `02_Server/Network/SendBuffer.cs` |
| 송신 버퍼 (커널 버퍼 추상화) | **이미 사용 중** | `02_Server/Network/SendBuffer.cs` — 청크 풀 + 세그먼트 관리 |
| 수신 버퍼 (경계 보존 링버퍼) | **이미 사용 중** | `02_Server/Network/RecvBuffer.cs` — 불완전 패킷 축적 |
| 논블록 폴링 / select / epoll | **사용 안 함** | 서버가 Windows IOCP만 사용; Linux 이식 필요 없음 |
| 블로킹 I/O | **사용 안 함** | 틱 루프 안에서 블로킹 호출 금지(헌법 #5) |

### 구체적 연결 포인트
- `02_Server/GameServer/Network/GameSession.cs`의 `OnRecv()` 콜백: IOCP 완료 이벤트가 워커 스레드에서 쏴주는 진입점. 3.9의 "완료 패킷 수거" 흐름.
- `02_Server/Network/SendBuffer.cs` 청크 풀: 커널이 Overlapped 송신 동안 버퍼를 유지해야 하므로 `ArraySegment`를 풀링해 관리. 3.3의 "송신 버퍼 복사" 비용을 줄이는 설계.
- `02_Server/Network/RecvBuffer.cs` 커서(읽기/쓰기/처리 포인터): 3.4의 "recv 반환 바이트가 한 패킷 미만일 수 있다"는 특성 처리.
- `02_Server/GameServer/Maps/GameMap.cs`의 틱 루프: 3.5의 "recv를 빠르게 비워야 상대방 send가 안 막힌다" — 20 TPS 처리로 수신 큐 적체 방지.

---

## 함정 / 과용 경계
- **블로킹 소켓 + 멀티스레드 혼용**: 스레드 당 클라 패턴은 학습용에만. 실 서버에 쓰면 1000명에 1 GB 스택.
- **논블록 폴링 루프**: `EAGAIN`이 올 때까지 무한 재시도하면 CPU 100% 스핀. select/epoll로 대기해야 함.
- **send() 즉시 리턴 착각**: "send 성공 = 상대방 수신 완료"가 아님. 송신 버퍼에 들어갔을 뿐. 소켓 닫고 곧바로 recv 기대하면 데이터 유실 가능.
- **수신 버퍼 over-read**: `recv()` 한 번에 패킷 정확히 1개 온다고 가정 금지. 반드시 RecvBuffer 길이 체크 + 누적 처리.
- **IOCP 워커 스레드 과다**: CPU 코어 수 ×2 이상으로 늘려도 컨텍스트 스위칭만 늘어남. 통상 코어 수 또는 ×1.5.
- **틱 루프 안 동기 DB**: 3.5 배압 전파 + 헌법 #5. DB 블로킹 호출이 recv 처리를 막으면 모든 클라이언트 send가 줄줄이 멈춤.

---

## 관련
- [[01-multithreading]] — 워커 스레드 동기화 (IOCP 수거 스레드 풀과 연결)
- [[04-server-and-client]] — PDL + length-prefixed framing (RecvBuffer 위에서 동작)
- [[game-programming-patterns/15-service-locator]] — IOCP 소켓 레이어를 서비스로 교체하는 패턴 (이식 시 참고)
