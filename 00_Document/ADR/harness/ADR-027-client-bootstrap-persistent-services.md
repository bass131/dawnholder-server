### ADR-027: 클라이언트 Bootstrap 씬 + Persistent Services + 연결 생명주기

**날짜**: 2026-05-25
**상태**: 채택됨

**결정**: 멀티 맵 scene 전환(M4.2)을 지원하기 위해 클라이언트에 **코드 주도 부트스트래퍼**를 도입한다. `Resources/`의 `PersistentServices.prefab` 하나에 cross-scene 서비스 3종 — `MainThreadDispatcher`(네트워크 콜백 큐 drain), `NetworkService`(소켓 + 세션 보유, `Connect()`/`Disconnect()` 노출, **자동 연결 안 함**), 페이드(`SceneTransition` + CanvasGroup) — 를 담고, **`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`** 정적 메서드가 *첫 씬 로드 전에* 이 프리팹을 1회 Instantiate + `DontDestroyOnLoad` 한다(이미 있으면 no-op). 별도 `_Bootstrap` 씬·빌드 순서 관리는 불필요 — 어느 씬에서 Play하든(에디터/빌드 동일) 서비스가 코드로 보장된다. (초안의 전용 `_Bootstrap` 씬안은 런타임 어트리뷰트와 역할이 중복되어 *코드 방식으로 통합* — 씬 관리 제거가 더 단순, 현업 부트스트래퍼 정석.)

**연결 생명주기 = A안 (게임플레이 진입 시 연결)**: 소켓 연결은 *첫 게임플레이 맵(Town) 진입 시점*에 `GameEntryPoint`가 `NetworkService.Connect(host, class)`를 **명시적 1회** 호출(`IsConnected` 가드로 포탈 루프백 재진입은 no-op). MainMenu는 throwaway `ConnectionProbe`(reachability 확인)만, CharacterSelect는 클래스를 PlayerPrefs로 운반. MainMenu 복귀 시 `NetworkService.Disconnect()`(오브젝트 파괴 X, 소켓만 닫음). 포탈 맵 이동(Town↔사냥터↔보스↔종료)에는 연결이 그대로 유지된다.

본 결정은 M4.2 Phase 04 진행 중 NetworkBootstrap을 맵 전환에 살아남게 만드는 과정에서, 초기 채택안(① 서비스마다 개별 `DontDestroyOnLoad` + 중복가드 + 씬 감지 teardown, WIP·미커밋)이 *서비스마다 persistence 가드를 흩뿌리는 주먹구구*로 번지는 것을 사용자가 자각 → 전용 bootstrap 패턴으로 선회한 것이다. ADR-021(UI Additive Scene)의 scene-lifecycle 모델을 *cross-scene 서비스 계층*으로 확장한다.

**B안(서버 주도 로그인/인증 + 온라인 캐릭터 선택 + 서버가 초기 씬 로드 주도)은 M5 Persistence로 이월** — 서버 권위 캐릭터 *목록*은 DB 영속화(M5)가 전제라 둘이 한 묶음. PRD 마일스톤 표는 변경 없음(M5에 흡수).

**이유**: 단일 Gameplay 씬 전제로 만들어진 기존 구조(`NetworkBootstrap.Start()`가 씬 로드 시 자동 connect, `MainThreadDispatcher` 주석 "단일 씬이라 OK")가 *진짜 맵 전환*에서 깨진다. (1) 소켓이 맵 전환마다 파괴·재생성되면 재handshake → 서버가 migration이 아닌 신규 접속으로 오인 → Phase 03 player migration(entity id 유지, ADR-026) 붕괴. (2) persistent한 네트워크가 받은 콜백을 항상 비울 persistent `MainThreadDispatcher`가 필요(네트워크 persistent ⟺ dispatcher persistent는 한 쌍). ①(서비스별 DontDestroyOnLoad)은 서비스 3개를 넘어가면 "어느 씬에 두나 / 루프백 중복 / 메뉴 teardown" 가드가 흩어지는 DontDestroyOnLoad 싱글톤 안티패턴으로 수렴 → bootstrap 씬은 *서비스를 부팅 때 단 한 번 생성*해 그 가드 전부를 제거한다(중복 불가 → 가드 불필요, 연결은 `Connect()`/`Disconnect()`로 명시 제어). 연결을 `Start()`에서 떼어 명시적 `Connect()`로 만든 것은 "접속 시점이 씬 로드에 암묵적으로 묶여 불분명"하던 문제의 해소다. `[RuntimeInitializeOnLoadMethod]`는 ADR-021이 트레이드오프로 남긴 "Editor Play-from-scene이 빌드와 경로가 다름" 문제를 *코드로 일원화*해 닫는다(어느 씬에서 Play하든 동일). A안 채택은 M4.2 scope(맵 전환) 정합 + 변경 최소이며, MMO 정통인 B안은 서버 상태머신·인증을 건드리므로 영속화(M5)와 함께 제대로 설계하는 것이 옳다.

**트레이드오프**: `_Bootstrap` 씬 + build order 관리라는 인프라가 한 겹 늘어난다(학부 학습 가치로 상쇄 — 현업 MMO/게임 클라 정석 패턴, 면접 자산). `[RuntimeInitializeOnLoadMethod]`는 편하지만 *씬 진입 전에 코드가 먼저 돈다*는 점에서 디버깅 시 "이 오브젝트 언제 생겼지?"가 덜 직관적(주석으로 보강). PersistentServices가 전역 상태라 *테스트 격리*가 어려움(MonoBehaviour wiring 영역이라 자동 단위테스트 밖, Play 모드 실측 의존 — ADR-021과 동일 한계). A안은 캐릭터 선택이 오프라인(PlayerPrefs 운반)이라 *서버 권위가 게임 월드 입장 시점에야 작동* → MMO 정통(B)과 거리가 있으나 M5에서 교정 예정으로 *의도된 임시*. 본 ADR이 M4.2 Phase 04에서 client SubAgent가 이미 박은 ① 코드(NetworkBootstrap DontDestroyOnLoad/중복가드/teardown)를 일부 supersede → 해당 WIP 코드를 bootstrap 패턴으로 리팩터(미커밋이라 되돌릴 것 없음). CHANGELOG는 팀장 영역이라 본 ADR 박힌 후 [M] 한 줄 추가 권유.

**관련**: [ADR-021](ADR-021-client-ui-additive-scene.md)(UI Additive Scene — scene-lifecycle 모델 확장 원본), [ADR-026](../tech-stack/ADR-026-entity-id-global-pool.md)(entity id 유지 — 연결 persistent 필요성의 서버측 근거).
