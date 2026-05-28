// ADR-027 (M4.2 Phase 04): NetworkBootstrap → NetworkService 로 재정의됨.
//
// 이 파일은 빈 전달 안내 파일입니다. 실제 구현은 NetworkService.cs 에 있습니다.
//
// **변경 요약**:
//   - NetworkBootstrap 클래스 제거 (씬 배치 미커밋 WIP이라 기존 씬 인스턴스 없음)
//   - NetworkService.cs 로 대체 (같은 폴더: Network/)
//   - 씬/prefab에서 연결하던 참조가 있다면 NetworkService 로 교체 필요
//
// **제거된 ① 땜질 코드**:
//   1. DontDestroyOnLoad(gameObject) in Awake    → PersistentServicesBootstrap.cs 로 일원화
//   2. Instance != null 중복가드 (_isDuplicate)  → PersistentServicesBootstrap 1회 생성 보장으로 불필요
//   3. SceneManager.sceneLoaded += OnSceneLoaded → 씬 감지 자동 teardown 제거
//   4. OnSceneLoaded() (MainMenu/CharacterSelect 감지) → 제거
//   5. MenuSceneNames 상수 + foreach 씬 이름 비교 → 제거
//   6. Start()의 auto-connect 흐름             → GameEntryPoint.cs 로 이전
//
// **대체 흐름 (ADR-027)**:
//   PersistentServicesBootstrap(BeforeSceneLoad) → PersistentServices 프리팹 spawn
//     └─ NetworkService(Start 없음, 자동연결 없음)
//   GameEntryPoint(Town 씬 Start) → NetworkService.Connect() 명시 호출
//   MainMenuController(Awake)    → NetworkService.Disconnect() 명시 호출
