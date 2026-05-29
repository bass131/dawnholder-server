---
title: 게임 네트워크 엔진 프라우드넷
source: 게임 서버 프로그래밍 교과서
category: 6장 — 상용 네트워크 엔진 사례 (콘텐츠/엔진 분리 관점)
---
# [CH06] 게임 네트워크 엔진 프라우드넷 (Game Network Engine ProudNet)
> 상용 네트워크 엔진이 소켓 계층을 어떻게 추상화하는지 — 콘텐츠 코드와 엔진 코드의 경계 설계 사례

## 언제 참조하나 (트리거)
- ServerCore / GameServer 계층 분리 설계 또는 리팩터링 시 ("엔진 vs 콘텐츠" 경계를 어디에 그을지 고민될 때)
- 자체 PDL 코드 생성기(RMI 유사 패턴) 동작 방식을 설명하거나 확장할 때
- 스레드 모델(단일 스레드 Actor vs 멀티 스레드 워커) 선택 근거가 필요할 때
- P2P 아키텍처 또는 모바일 연결 복구 기능 추가를 검토할 때

## 핵심 내용

### 6.1 네트워크 엔진이 필요한 이유 (접근 성공)
- 운영체제마다 소켓 API 차이(윈도 Overlapped I/O vs 리눅스 epoll)를 직접 추상화해야 함.
- 소켓 API만으로는 제공되지 않는 기능 — 망 전환(Wi-Fi↔셀룰러), 암호화, rate-limit, 압축 — 을 직접 구현해야 한다.
- 네트워크 엔진은 이 복잡성을 캡슐화해 콘텐츠 개발자가 게임 로직에만 집중하게 해 준다.

### 6.2 기본 모듈 구조 (접근 성공)
- 프라우드넷은 **NetServer / NetClient** 두 클래스가 핵심.
  - `NetServer`: 클라이언트 연결 수락 + 메시지 송수신 + 네트워크 상태 모니터링.
  - `NetClient`: 서버 연결 + 메시지 송수신 + 클라이언트 간 P2P 통신.
- C++ / C# 이중 지원 — 게임 엔진(Unity 등) 통합이 목표.

### 6.3 연결 수립 패턴 (접근 성공)
- 서버: `CNetServer.Create()` → `CNetServer.Start(프로토콜버전, 포트)`.
- 클라이언트: `CNetClient.Create()` → `CNetClient.Connect(서버주소, 포트, 프로토콜버전)`.
- **프로토콜 버전**: 문자열 UUID 등 임의 값 — 버전 불일치 시 접속 거부. 우리 PDL의 `Protocol.Version` 상수와 동일한 역할.
- 연결 이벤트는 콜백(이벤트 핸들러) 방식으로 전달됨 — "연결됨/끊김" 통지를 게임 로직이 구독.

### 6.4 메시지 송수신 — SendUserMessage (접근 성공)
- 저수준 바이너리 전송 API: `SendUserMessage(수신자HostID, RmiContext, 바이너리배열)`.
- **수신자**: 단일 HostID 또는 HostID 배열(멀티캐스트).
- **RmiContext(전송 방식)**:
  - `Reliable` — TCP 보장 전달(중요 이벤트, 스킬 결과 등).
  - `Unreliable` — 즉시 전송 우선(위치 동기화 등 손실 허용).
  - 옵션 추가: 암호화, 압축, "이미 다음 패킷이 왔으면 이전 것 버림(Supersede)".

### 6.5 Wi-Fi↔셀룰러 연결 핸드오버 (접근 성공)
- 일반 TCP 소켓은 망 전환 시 소켓 FD가 무효화 → 게임 세션 끊김.
- 프라우드넷 해법: 클라이언트 Connect 파라미터에 `autoConnectionRecovery = true` 설정 → 엔진이 재연결 + 세션 복구를 자동 처리.
- 모바일 MMO에서 중요 — 지하철/이동 중 플레이 시 재접속 로직을 콘텐츠 코드가 직접 관리할 필요 없음.

### 6.6 원격 메서드 호출 — RMI (접근 성공)
- RMI(Remote Method Invocation): 함수 시그니처를 정의 파일에 선언하면 코드 생성기가 직렬화/역직렬화 코드를 자동 생성.
- 송신 측은 평범한 함수 호출 형태(`Knight_Move(pos, velocity)`) — 생성 코드가 메시지 ID + 파라미터를 직렬화해 전송.
- 수신 측은 디스패처가 메시지 ID로 라우팅 → 역직렬화 후 원래 함수 실행.
- 장점: 통신 보일러플레이트 제거, 파라미터 타입 안전성.
- 단점: 코드 생성기 의존 — 생성 코드 디버깅이 어려울 수 있음. 생성 코드와 정의 파일 불일치 시 런타임 에러.

### 6.7 클라이언트 간 P2P 통신 (접근 성공)
- 서버 중계 없이 클라이언트끼리 직접 통신 — 레이턴시 감소, 서버 대역폭 절약.
- **보안 구조**: P2P 연결은 반드시 서버가 승인해야 성립. 클라이언트 단독으로 임의 P2P 연결 불가 → 해킹 클라이언트의 무단 접근 차단.
- **P2P 그룹**: 메신저 채팅방과 유사 — 그룹 단위로 멤버 관리 + 멀티캐스트.
- 한계: P2P 경로가 막힌 환경(방화벽/NAT 엄격)에서는 서버 릴레이로 자동 폴백.

### 6.8 채팅 처리 예시 — RMI 활용 패턴 (접근 성공)
- 3단계 흐름: `클라이언트.Chat(메시지) → 서버 수신 → 서버.ShowChat(메시지) 멀티캐스트`.
- **멀티캐스트**: 특정 HostID 목록에 동시 전송 (브로드캐스트와 달리 수신자 명시).
- 이 패턴이 우리 GameMap의 `Broadcast()` + `S_Chat` 패킷 흐름과 동일한 구조.

### 6.9 스레드 모델 선택 기준 (접근 성공)
- 프라우드넷 NetServer 기본: CPU 수만큼 워커 스레드(멀티 스레드 모델).
- **단일 스레드 모델(여러 프로세스 분산)이 더 나은 경우** — 세 조건이 모두 맞을 때:
  1. 서버 내부 데이터를 단일 뮤텍스로만 보호하는 구조 (lock 경합 병목).
  2. DB/파일 I/O가 없거나 비동기 큐로 분리된 순수 인메모리 로직.
  3. 여러 프로세스로 수평 확장하는 분산 서버 구조.
- 이 조건이 맞으면 멀티 스레드는 lock 경합만 늘리고 이득이 없다.

### 6.10 더 읽을거리 (접근 성공)
- 공식 가이드: `guide.nettention.com` — API 레퍼런스 + 튜토리얼.
- Chat 샘플(`ProudNet/Samples/Chat`), CharacterMove 리포지토리 — RMI 기반 이동 동기화 예제.
- SynchWorld 샘플: 추측항법(Dead Reckoning) + 가시 영역 필터링(AOI) 혼용 — 대규모 MMO 최적화 패턴.

## 우리 프로젝트 적용

| 프라우드넷 개념 | Dawnholder 대응 | 상태 |
|---|---|---|
| NetServer/NetClient 분리 | `02_Server/Network/`(`Session.cs`/`Listener.cs`) + `03_Client/Assets/Scripts/Network/UnityClientSession.cs` | 이미 사용 중 |
| RMI / 코드 생성기 | `98_Shared/Protocol/` PDL XML + PacketGenerator (`99_Tools/`) | 이미 사용 중 (동일 패턴) |
| Reliable/Unreliable RmiContext | `S_Move` = Unreliable 후보, `S_EnterGame`/`S_Chat` = Reliable | 채택 후보 — 현재 TCP 고정 |
| autoConnectionRecovery | 모바일 클라이언트 재연결 핸들러 | 현재 무관 (데스크톱 MVP) |
| P2P 그룹 | 없음 — 서버 중계만 사용 | 현재 무관 |
| 단일 스레드 모델 권고 | GameMap Actor (맵별 단일 스레드 20TPS tick, lock 없음) | 이미 사용 중 — 동일 결론 |
| 멀티캐스트 | `GameMap.Broadcast()` — `S_Move`, `S_Chat` 등 | 이미 사용 중 |
| 프로토콜 버전 | `98_Shared/Protocol/Protocol.cs`의 `Protocol.Version` 상수 | 이미 사용 중 |

핵심 관찰: Dawnholder의 `02_Server/Network/` + PDL 설계는 프라우드넷이 제공하는 추상화 계층을 자체 구현한 것과 구조적으로 동일하다. 프라우드넷을 쓴다면 교체될 레이어 = `98_Shared/Protocol/` + `02_Server/Network/`.

## 함정 / 과용 경계

- **RMI 과신**: 코드 생성기 출력물은 직접 수정하면 안 됨 — 재생성 시 덮어씀. PDL XML이 단일 진실 공급원. (우리도 동일 — PacketGenerator 출력물 직접 편집 금지)
- **P2P를 기본으로 쓰는 함정**: 서버 권위 원칙(헌법 #1)과 충돌. P2P는 레이턴시 최적화 수단이지 권위 이전이 아님. 게임 상태 확정은 서버 경유 필수.
- **멀티 스레드 기본값 맹신**: CPU 수만큼 스레드가 기본이지만, 단일 뮤텍스 보호 + 인메모리 로직이면 오히려 lock 경합 병목. 우리처럼 Map=단일스레드가 맞는 이유.
- **autoConnectionRecovery 없이 모바일 출시**: 모바일 타겟 추가 시 필수 — 없으면 지하철 타면 세션 끊김.
- **프로토콜 버전 무시**: 버전 불일치 클라이언트가 접속을 시도하면 데이터 파싱 오류. 헌법 #2 — PDL 변경 시 `Protocol.Version` 반드시 bump.

## 관련
- `98_Shared/Protocol/` + `99_Tools/` PacketGenerator — 우리 자체 코드 생성기 (RMI와 동일한 역할)
- `02_Server/GameServer/Maps/GameMap.cs` 단일 스레드 Actor 패턴 — 6.9 결론과 동일
- 헌법 절대원칙 #1 (Server Authority) — P2P 적용 시 권위 경계 검토 기준
