# Phase 04 — 서버 Listener wire-up + 첫 connect 스모크 완료 박제

**완료일**: 2026-05-10
**커밋**: `a798479` (feat(connect): 서버 Listener + Unity 첫 connect 스모크 + main thread queue 도입)
**소요 시간**: 약 1.5시간 (Phase 04 파일 재작성 포함)

> 파일명은 `04-framing-and-pingpong-DONE.md` 그대로 유지(원본 짝꿍 패턴).
> 내용은 *connect 스모크* 기준 — framing은 Phase 05로 분리.

---

## 5단계 보고

### 🎯 무엇을 만들었나
서버에서 처음으로 7777 포트를 열고, Unity 클라이언트가 connect까지 가서 **양쪽 콘솔에 OnConnected 로그가 동시에 뜨는** 살아있는 connection 1개를 시연. Unity main thread queue 패턴(`MainThreadDispatcher`)을 첫 도입해서 socket 워커 스레드 콜백을 main thread로 안전하게 marshalling.

### 🤔 왜 필요한가
지금까지(Phase 01~03)는 *코드만 박혀있는 상태* — Listener는 있어도 Program.cs가 안 부르고, ClientNet은 라이브러리만 있고 Unity 측 wrapper가 없었음. **이번 Phase는 그 라인들을 *전기로 연결*하는 첫 시연**. Y2 분리 갈래(ADR-012)의 분업이 *진짜로 작동*함을 증명 — ClientNet은 Unity 무지, Unity측 wrapper가 main thread 책임.

### 🛠️ 어떻게 만들었나
- **서버 wire-up** (2개 파일): `GameSession.cs` (Session 상속, 단순 로그) + `Program.cs` (Hello World → Listener.Init 호출).
- **Unity main thread queue** (3개 파일): `MainThreadDispatcher` (ConcurrentQueue + Update drain) + `UnityClientSession` (콜백을 dispatcher로 push) + `NetworkBootstrap` (Connector 호출 + OnApplicationQuit 정리).
- **고려했지만 안 고른 대안**: ① `UnitySynchronizationContext` 활용 → 더 정교하지만 Phase 04엔 과함. ② `Queue<T>` + lock → 단순하지만 워커 스레드의 빈번한 Enqueue 시 lock contention 위험. ConcurrentQueue가 multi-producer/single-consumer 시나리오에 최적.
- **신규 개념**: ConcurrentQueue (CAS 기반 lock-free), closure 캡처 가드 (콜백 인자를 로컬 변수에 박은 뒤 람다 캡처), SessionFactory 패턴(Listener와 Connector 양쪽이 같은 발상의 데칼코마니).

### 🧪 테스트 결과
- `dotnet build Dawnholder.slnx`: **5개 프로젝트 경고 0 / 오류 0**
- ClientNet DLL이 `Plugins/ClientNet/`에 자동 복사 ✅
- Unity Play → Unity Console `OnConnected to 127.0.0.1:7777` ✅
- 동시에 서버 콘솔 `[GameSession] OnConnected from 127.0.0.1:NNNNN` ✅
- **`UnityException: ... main thread` 없음** ✅ (MainThreadDispatcher 작동 입증)
- Unity Stop → 양쪽 OnDisconnected 로그 ✅ (clean shutdown)

### ➡️ 다음 스텝
- **Phase 05**: Length-prefixed framing + 첫 패킷(Ping/Pong). `[size(2)][packetId(2)][payload]` 도입. Unity Update에서 1초마다 Ping → 서버 Pong → 클라 RTT 출력. 직렬화는 자체 PDL vs 단순 BitConverter — Phase 05 진입 시 결정.
- 알아두면 좋을 후속: `99_Tools/headless-bot`에서 같은 ClientNet을 *main thread queue 없이* 그대로 재사용 가능 (콘솔 환경이라 Unity 제약 무관). Y2 갈래의 진짜 가치가 부하 테스트 시점에 드러남.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Phase 04 범위 축소** — 옛 plan(framing + MessagePack + ping/pong + CLI 클라)이 4~5시간 + Phase 03 가정·ADR-002 변경(MessagePack→자체 PDL) 미반영. *connect 스모크*만으로 한 Phase 잘라냄. framing은 Phase 05.
- **MainThreadDispatcher 위치** — Unity 측(`03_Client/Assets/Scripts/Network/`). ClientNet 라이브러리(.NET Std 2.1)는 Unity 무지여야 하므로 *main thread 책임은 Unity 쪽 wrapper에 둠*. Y2 분업의 본질.
- **ConcurrentQueue vs Queue+lock** — 워커 스레드 다수 + Update 단일 consumer. CAS 기반 ConcurrentQueue가 lock contention 없음. Phase 04 빈도엔 어느 쪽이든 OK지만 *학습 차원*에서 ConcurrentQueue.
- **closure 캡처 가드** — 람다가 콜백 인자(`endPoint`, `buffer.Count`, `numOfBytes`)를 직접 캡처하면 워커 스레드의 *변경 가능 상태*를 가둘 위험. 안전책으로 로컬 변수에 박은 뒤 캡처.
- **`OnApplicationQuit`에서 Disconnect** — Unity Stop / 빌드 종료 시 socket 정상 종료. 안 하면 서버측 OnDisconnected가 뒤늦게 또는 안 뜸.
- **씬에 Network GameObject 박음** — 다음 세션도 시연 재현 가능하도록 SampleScene.unity를 commit에 포함. 다음 세션이 Unity 열고 Play만 해도 자동 connect.
- **GameSession namespace** — `Dawnholder.Server.GameServer.Sessions`. ServerCore의 `Dawnholder.Server.Network` 와 분리 — *infrastructure (Network)* vs *game domain (Sessions)* 구분.
- **Phase 04 파일명 유지** — `04-framing-and-pingpong.md`라는 옛 이름이 framing 가정을 박았지만, *git history 비용*이 커서 rename 안 함. 파일 본문 통째 재작성으로 정합.

---

## 막혔던 지점

이번 Phase는 큰 막힘 없음. 시연 1회 통과.

소소한 발견:
- **plan mode 중간 활성화** — Program.cs 작성 직후 plan mode 켜졌으나, 이미 박힌 1·2단계 + 명확한 Phase 04 청사진 덕에 즉시 종료 후 진행 재개. 흐름 단절 최소.
- **Unity가 Scripts.meta + Network.meta 자동 생성** — 폴더 단위 .meta는 git에 들어가야 함 (.gitignore가 Plugins 하위 .meta만 무시). 명시적 add로 함께 commit.

---

## 학습 일지 후보 키워드

`/journal:concept <키워드>` 로 펼칠 만한 것들:

- **unity-main-thread-queue** — `UnitySynchronizationContext` vs ConcurrentQueue 패턴, Update() drain 흐름, 씬 전환 시 누수, async/await + UnityWebRequest 같은 다른 Unity-friendly 비동기 도구와의 관계.
- **concurrent-queue-internals** — CAS(compare-and-swap) 기반 lock-free의 의미, ABA 문제, MPSC vs MPMC 시나리오 적합도, `BlockingCollection<T>` / `Channel<T>`와의 대비.
- **session-factory-pattern** — Listener와 Connector가 둘 다 `Func<Session>`을 받는 *대칭 설계*의 본질. 의존성 역전 / IoC 첫 인스턴스. ASP.NET DI / Unity의 Activator.CreateInstance와 비교.
- **clean-shutdown-tcp** — TCP 4-way handshake (FIN/ACK), `Shutdown(SocketShutdown.Both)` vs `Close()`, half-close 시나리오, 서버측 OnRecv가 0 byte 받으면 정상 종료로 인지하는 패턴.
- **closure-capture-of-mutables** — C# 람다의 클로저 캡처 의미, *값 vs 참조* 캡처, foreach 변수 캡처 함정(C# 5+에서 변경됨), 멀티스레드에서 *변경 가능 변수*를 람다에 직접 캡처할 때의 race.

---

## 메모 (다음 세션을 위한)

- Phase 04 시연용 GameObject(`Network`)가 SampleScene.unity에 박혀있어, 다음 세션엔 Unity 열고 서버 띄운 뒤 Play만 누르면 즉시 시연 재현.
- Phase 05 진입 시 Phase 05 파일은 *없음* (Phase 04 옛 이름이 그 자리를 차지). `/work:plan` 또는 직접 `01_Phases/M1-foundation/05-*.md` 신설부터.
- `99_Tools/headless-bot`은 `99_Tools/headless-bot/` 폴더만 있고 코드 없음. Phase 06+(부하 테스트 진입 시) 시작점.
- Phase 05에서 직렬화 결정 필요: ① 자체 PDL(ADR-002 채택, 4월 코드 재활용 + 코드 생성기) ② 단순 `BitConverter` 직접(첫 ping/pong은 필드 2개라 PDL 없이도 됨). PDL 인프라 셋업 비용 vs 학습 임팩트 trade-off.
- 헌법 응축본이 다음 세션에서 *진짜 톤·동작*에 영향 주는지 자체 검증 가능 — 이번 세션 톤이 응축 전과 비슷하게 작동했음을 첫 신호로 박아둠.
