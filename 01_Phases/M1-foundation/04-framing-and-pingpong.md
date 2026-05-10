# Phase 04: 서버 Listener wire-up + 첫 connect 스모크

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 2~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트 + 사용자 (Unity 시연)
> **재작성**: 2026-05-10 (옛 plan = "framing + ping/pong + CLI 클라"는 폐기 — 너무 큰 단위 + Phase 03 가정 outdated + ADR-002 변경(MessagePack → 자체 PDL) 미반영)
> **근거 ADR**: ADR-012 (Y2 분리 모델) + ADR-010 (DLL + Embedded PDB)

---

## 🎯 목표

서버에서 **처음으로 포트를 열고**, Unity 클라가 **connect**까지 가서 양쪽 로그가 뜨는 것까지. 패킷 송수신은 다음 Phase. 이번엔 *살아있는 connection 1개*가 양쪽에 인지되는 모습 시연.

**왜 이 범위로 잘랐나** (3시간 가이드):
- 서버 wire-up + 클라 wire-up + Unity main thread queue 첫 도입까지가 한 호흡
- framing/직렬화/ping-pong까지 넣으면 4~5시간 + 디버깅 폭발 위험
- Unity main thread queue는 *첫 패턴 도입*이라 학습 가치가 큼 → 이걸 깔끔히 박는 데 집중

---

## ⏪ 사전 조건

- [x] Phase 02 완료 (서버측 `02_Server/Network/` 정착)
- [x] Phase 03 완료 (`04_ClientNet/` 신작 + Unity F12 검증)
- [x] ADR-012 박힘 (Y2 분리 모델)
- [ ] 헌법 + ADR-001/010/012 + Phase 03 -DONE 통독
- [ ] **이번 Phase의 핵심 통찰 인지**: Unity main thread 제약. socket 콜백은 워커 스레드에서 호출됨 → GameObject·Transform 등 Unity API 직접 접근 시 `UnityException`. 해결 = main thread queue.

---

## 📝 작업 내용

### 1단계: 서버 측 ServerSession 신작

서버는 클라가 connect하면 새 세션을 만들어 Session을 상속한 *클래스 인스턴스*를 띄움. 이번 Phase에선 **로그만 찍는 가벼운 구현**.

- [ ] 새 파일: `02_Server/GameServer/Network/GameSession.cs`
  ```csharp
  using System.Net;
  using Dawnholder.Server.Network;

  namespace Dawnholder.Server.GameSessions;

  // Session(98_Shared의 Session과는 다름 — 서버측 02_Server/Network/Session.cs)을 상속.
  // Phase 04에선 패킷 처리 X. connect/disconnect 로그만.
  public class GameSession : Session
  {
      public override void OnConnected(EndPoint endPoint)
          => Console.WriteLine($"[GameSession] OnConnected from {endPoint}");

      public override void OnDisconnected(EndPoint endPoint)
          => Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

      public override int OnRecv(ArraySegment<byte> buffer)
      {
          // Phase 04: 받은 바이트 수만 로그. 패킷 해석은 Phase 05 framing 도입 후.
          Console.WriteLine($"[GameSession] OnRecv {buffer.Count} bytes");
          return buffer.Count; // 모두 처리한 것으로 간주
      }

      public override void OnSend(int numOfBytes)
          => Console.WriteLine($"[GameSession] OnSend {numOfBytes} bytes");
  }
  ```
- [ ] namespace는 `Dawnholder.Server.GameSessions` (또는 `Dawnholder.Server.Sessions`). `Network`와 분리한 이유: Network는 *프로토콜 인프라*, GameSession은 *게임 도메인*.

### 2단계: 서버 Program.cs에 Listener 띄우기

- [ ] `02_Server/GameServer/Program.cs` 교체:
  ```csharp
  using System.Net;
  using Dawnholder.Server.Network;
  using Dawnholder.Server.GameSessions;
  using Shared.GameData;

  Console.WriteLine("=== Dawnholder Server ===");
  Console.WriteLine($"Tick rate: {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms)");

  // 0.0.0.0:7777 listen. 0.0.0.0 = 모든 인터페이스 (loopback + LAN).
  IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7777);

  Listener listener = new Listener();
  listener.Init(endPoint, () => new GameSession());

  Console.WriteLine($"Listening on {endPoint}. Press Enter to stop.");
  Console.ReadLine();
  ```
- [ ] `Listener.Init` 시그니처 확인 — `02_Server/Network/Listener.cs` 읽고 정확한 메서드명·파라미터 맞춤. (이름이 `Init`이 아니라 `Listen`/`Start`일 수도 있음. 실제 코드에 맞춰 조정.)

### 3단계: Unity 클라 측 — MainThreadDispatcher

socket 콜백은 워커 스레드. Unity API는 main thread 전용. 그 *스레드 경계 넘기*를 담당하는 작은 헬퍼.

- [ ] 새 폴더: `03_Client/Assets/Scripts/Network/`
- [ ] 새 파일: `03_Client/Assets/Scripts/Network/MainThreadDispatcher.cs`
  ```csharp
  using System;
  using System.Collections.Concurrent;
  using UnityEngine;

  namespace Dawnholder.Client.Network
  {
      /// <summary>
      /// 워커 스레드에서 발생한 작업을 Unity main thread에서 실행하기 위한 큐.
      ///
      /// 사용법:
      ///   - 워커 스레드: MainThreadDispatcher.Enqueue(() => Debug.Log("hi"));
      ///   - main thread: 이 컴포넌트의 Update()가 자동으로 큐를 drain.
      ///
      /// **왜 필요한가**: Unity의 GameObject/Transform/MonoBehaviour API는
      /// main thread에서만 접근 가능. socket 콜백은 .NET 스레드풀의 워커 스레드에서
      /// 호출되어 UnityException 발생.
      /// </summary>
      public class MainThreadDispatcher : MonoBehaviour
      {
          static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

          /// <summary>워커 스레드 안전. 다음 main thread 프레임에 실행됨.</summary>
          public static void Enqueue(Action action)
          {
              if (action != null) _queue.Enqueue(action);
          }

          void Update()
          {
              // 한 프레임에 누적된 모든 작업을 drain.
              while (_queue.TryDequeue(out Action action))
              {
                  try { action(); }
                  catch (Exception ex) { Debug.LogException(ex); }
              }
          }
      }
  }
  ```
- [ ] **함정 가드**: `_queue`를 *static*으로 둔 이유 = MonoBehaviour 인스턴스가 여럿 있어도 단일 큐. 단점은 씬 전환 시 누수 가능 — 본 Phase에선 신경 X (Phase 04는 단일 씬 시연).

### 4단계: Unity 클라 측 — UnityClientSession

ClientNet의 `ClientSession`을 상속해서 *콜백을 main thread queue로 푸시*하는 wrapper.

- [ ] 새 파일: `03_Client/Assets/Scripts/Network/UnityClientSession.cs`
  ```csharp
  using System;
  using System.Net;
  using Dawnholder.Client.Net;
  using UnityEngine;

  namespace Dawnholder.Client.Network
  {
      /// <summary>
      /// ClientNet의 ClientSession을 Unity 컨텍스트로 wrap.
      ///
      /// 콜백(OnConnected/OnRecv/OnSend/OnDisconnected)은 socket 워커 스레드에서
      /// 호출되므로, 모든 처리를 MainThreadDispatcher에 enqueue 한 뒤
      /// Unity의 Update()에서 실행되게 함.
      /// </summary>
      public class UnityClientSession : ClientSession
      {
          public override void OnConnected(EndPoint endPoint)
              => MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnConnected to {endPoint}"));

          public override void OnDisconnected(EndPoint endPoint)
              => MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnDisconnected from {endPoint}"));

          public override int OnRecv(ArraySegment<byte> buffer)
          {
              int count = buffer.Count;
              MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnRecv {count} bytes"));
              return count; // 모두 소비한 것으로
          }

          public override void OnSend(int numOfBytes)
              => MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnSend {numOfBytes} bytes"));
      }
  }
  ```
- [ ] **주의**: `OnRecv`의 반환은 *워커 스레드에서 즉시* 결정 (몇 바이트 소비됐는지). 그러므로 `count` 변수를 *클로저 캡처 전에* 로컬에 박아둠 (closure 안 가두려는 안전망).

### 5단계: Unity 클라 측 — NetworkBootstrap (시연 트리거)

- [ ] 새 파일: `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs`
  ```csharp
  using System.Net;
  using Dawnholder.Client.Net;
  using UnityEngine;

  namespace Dawnholder.Client.Network
  {
      /// <summary>
      /// 씬에 빈 GameObject 하나 두고 이 컴포넌트 + MainThreadDispatcher를 같이 붙여서
      /// Play 누르면 자동으로 서버에 connect 시도.
      ///
      /// Phase 04 시연용. Phase 05+에서는 UI 버튼이나 게임 시작 흐름과 통합 예정.
      /// </summary>
      public class NetworkBootstrap : MonoBehaviour
      {
          [SerializeField] string serverHost = "127.0.0.1";
          [SerializeField] int serverPort = 7777;

          void Start()
          {
              IPAddress ip = IPAddress.Parse(serverHost);
              IPEndPoint endPoint = new IPEndPoint(ip, serverPort);

              Connector connector = new Connector();
              connector.Connect(endPoint, () => new UnityClientSession());

              Debug.Log($"[Unity] Connect 시도 → {endPoint}");
          }
      }
  }
  ```
- [ ] **시연 절차** (사용자가 Unity 에디터에서 직접):
  1. 빈 씬에 빈 GameObject(`NetworkBootstrap`) 생성
  2. 컴포넌트로 `MainThreadDispatcher` + `NetworkBootstrap` 추가
  3. Play → Unity Console 로그: `Connect 시도` → `OnConnected to 127.0.0.1:7777`
  4. 동시에 서버 콘솔: `[GameSession] OnConnected from 127.0.0.1:NNNNN`

### 6단계: 빌드 + 양쪽 시연

- [ ] `dotnet build Dawnholder.slnx` — 5개 프로젝트 경고 0 / 오류 0
- [ ] 새 DLL이 Plugins/ClientNet/에 자동 복사됐는지 확인
- [ ] 서버 실행: `dotnet run --project 02_Server/GameServer`
  - 로그 "Listening on 0.0.0.0:7777" 확인
- [ ] Unity 에디터: Play
  - Unity Console: `Connect 시도` + `OnConnected to ...`
  - 서버 콘솔: `OnConnected from ...`
- [ ] Stop → `OnDisconnected` 양쪽 로그 (Unity가 socket 닫을 때)

### 7단계: 커밋

- [ ] `feat(connect): 서버 Listener + Unity 첫 connect 스모크 — main thread queue 도입`

---

## ✅ 완료 조건

- [ ] `dotnet build Dawnholder.slnx` — 경고 0 / 오류 0
- [ ] 서버에 `02_Server/GameServer/Network/GameSession.cs` 신규
- [ ] 서버 `Program.cs` Listener wire-up 됨, 7777 listen 로그 표시
- [ ] Unity 클라에 3개 .cs 신규 (`MainThreadDispatcher`, `UnityClientSession`, `NetworkBootstrap`)
- [ ] **End-to-end 시연**: Unity Play → 양쪽 콘솔에 OnConnected 로그
- [ ] Unity Stop → 양쪽에 OnDisconnected 로그 (clean shutdown)
- [ ] Unity Console에 `UnityException: ... main thread` 같은 에러 없음 (main thread queue 작동 증명)

---

## 🧪 테스트

**자동 테스트**: 이번 Phase는 **신설 안 함**. 이번엔 *wiring + 살아있는 connection 시연*이 본질. 단위 테스트 가치 낮음 (Listener·Connector 자체는 Phase 02·03에서 검증).

**수동 테스트**:
1. 서버 단독 실행 → "Listening on 0.0.0.0:7777" 로그 표시
2. Unity Play → 양쪽 OnConnected 로그
3. Unity Stop → 양쪽 OnDisconnected 로그
4. (옵션) Unity Play 두 번 — 같은 클라가 재접속해도 서버에 새 GameSession이 새로 만들어지는지 확인
5. (옵션) 서버 안 켠 상태에서 Unity Play → `OnConnectCompleted Error : ConnectionRefused` 로그

---

## 📚 학습 포인트

### 1. Unity main thread 제약 (Phase 04 핵심)
- **문제**: Unity의 `GameObject` / `Transform` / `Rigidbody2D` / `Time.deltaTime` 등은 *main thread에서만* 안전.
- **socket 콜백 위치**: .NET 스레드풀의 워커 스레드 (Unity main과 별개).
- **해결 패턴**: `ConcurrentQueue<Action>` + main thread의 `Update()`에서 drain.
- **대안**: `UnitySynchronizationContext` 활용 — 더 정교하지만 Phase 04엔 과함.

### 2. 0.0.0.0 vs 127.0.0.1
- 서버: `IPAddress.Any` (= 0.0.0.0) → 모든 네트워크 인터페이스에서 listen (loopback + LAN).
- 클라: `127.0.0.1` (loopback)으로 connect → 같은 머신 안 통신.
- LAN 테스트(같은 와이파이의 다른 머신에서 접속) 시 클라가 서버 LAN IP를 입력하면 됨.

### 3. Y2 갈래의 *분업* 본격 시연
- **ClientNet.dll** = Unity 무지. socket 패턴만.
- **Unity 측 wrapper** (`UnityClientSession` / `MainThreadDispatcher`) = ClientNet의 콜백을 Unity 컨텍스트로 변환.
- 이 분업이 *진짜 작동*함을 시연. 같은 ClientNet.dll을 (미래에) `99_Tools/headless-bot`에서도 그대로 쓸 수 있음 (그땐 main thread queue 불필요).

### 4. Listener.Init의 SessionFactory 패턴
- 서버는 connect 받을 때마다 *새 Session 인스턴스*가 필요. Listener는 *어떤 Session을 만들지* 모르고, 호출자가 `Func<Session>` factory로 알려줌.
- 이 패턴 = "프레임워크가 제어, 사용자가 *행동*만 주입" (의존성 역전).

### 5. ConcurrentQueue vs lock-Queue
- ConcurrentQueue<T> = lock-free 자료구조 (CAS 기반). 멀티 producer / 단일 consumer 시나리오에 최적.
- 단순 `Queue<T>` + lock도 가능하지만, *워커 스레드의 Enqueue가 빈번*하면 lock contention 발생.
- Phase 04엔 connect/recv 빈도 낮아 어느 쪽이든 OK. 그러나 *학습 차원*에서 ConcurrentQueue를 선택.

---

## ⚠️ 함정 / 주의사항

- **`Listener.Init` 시그니처 mismatch**: ServerCore 코드의 정확한 메서드명·파라미터 모양이 `Listener` / `Init` / `Listen` 중 무엇인지 본 작업 시작 시 `02_Server/Network/Listener.cs` 직접 읽기. 이름 다르면 Program.cs 호출도 맞춰 변경.
- **방화벽**: Windows 방화벽이 첫 7777 listen 시 팝업. "허용" 클릭. 안 그러면 LAN 접근 X (loopback은 OK).
- **동시 두 Unity 에디터 Play**: 한 번에 하나만. 두 Play는 같은 7777 socket을 두 번 connect 시도 → 두 GameSession 정상 (서버는 각각 별도 처리).
- **ConnectAsync 즉시 완료 케이스**: loopback connect는 가끔 동기 완료. ClientNet의 Connector는 이미 처리 (`pending == false` → 직접 OnConnectCompleted 호출). 별도 신경 X.
- **Unity Plugin 인식 안 됨**: Plugins/ClientNet/에 새 .dll 들어가도 Unity가 못 잡으면 Refresh(Ctrl+R) 또는 에디터 재시작.
- **Unity Stop 후 서버 콘솔에 OnDisconnected 안 뜸**: TCP RST를 못 받았을 수 있음. 서버측 OnRecv가 0바이트 받으면 정상 종료 처리하는지 ServerCore 코드 확인.
- **MainThreadDispatcher 컴포넌트 안 붙임**: Console에 connect 로그가 안 뜸 (큐에는 쌓이지만 drain 안 됨). 항상 NetworkBootstrap 옆에 함께.

---

## ➡️ 다음 Phase

**Phase 05: Length-prefixed framing + 첫 패킷 (Ping/Pong)**
- `[size(2)][packetId(2)][payload...]` framing 도입 (PacketSession 활용)
- `Ping` / `Pong` 첫 패킷 정의 (자체 PDL 또는 단순 BitConverter — 결정은 Phase 05 진입 시)
- Unity Update() 안 1초마다 Ping 송신 → 서버가 Pong 응답 → 클라 RTT 출력
- 처음으로 *살아있는 양방향 통신* 시연

> 이 시점에서 **M1 마일스톤(Foundation) 완료** 가까움. 두 머신 시연 영상도 가능.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.
> Phase 끝나면 이 내용을 `04-framing-and-pingpong-DONE.md`로 박제.
> ⚠️ Phase 04 파일명을 그대로 유지(이미 박힌 패턴) — 내용은 framing이 아닌 connect 스모크지만, 이름 변경은 git history 비용이 커서 생략.
