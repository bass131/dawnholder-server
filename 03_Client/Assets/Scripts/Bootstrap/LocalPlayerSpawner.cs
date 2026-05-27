#nullable enable
using Dawnholder.Client.Input;
using Dawnholder.Client.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Bootstrap
{
    // M4.2 Phase 04: 로컬 플레이어 동적 spawn 전담 컴포넌트.
    //
    // **왜 동적 spawn이 필요한가**:
    //   RemoteEntity는 이미 동적 spawn (RemoteEntityRegistry.Spawn → Instantiate).
    //   몇 명이 들어올지 모르는 타인과 달리 로컬은 "항상 1명"이지만,
    //   맵이 여러 개(Town/HuntingGround/BossRoom/Ending)이고 맵 전환 시마다 씬이 교체됨.
    //   씬에 미리 배치(pre-placed)하면 맵 수만큼 prefab 박기 + 씬 YAML 편집 의무 — 유지보수 폭탄.
    //   동적 spawn은 "어느 맵에서든 서버가 필요하다 할 때 1회 Instantiate"라 일관된 단일 경로.
    //
    // **설계 결정 — trade-off**:
    //   A) 미리 배치 (pre-placed):  씬마다 LocalPlayer.prefab을 드래그해 둠.
    //      장점: 씬 열면 즉시 보임 (Editor 작업 편함).
    //      단점: 맵 4개 × 씬 편집 + YAML 충돌 위험 + 맵 추가 시마다 반복.
    //   B) 동적 spawn (채택):  sceneLoaded에서 Instantiate. RemoteEntityRegistry 패턴과 동일.
    //      장점: 씬 YAML 편집 0건. 단일 PersistentServices 컴포넌트가 모든 맵에서 작동.
    //      단점: S_EnterMap 도착 전 Instance가 null인 짧은 창 존재 (아래 Race 섹션 참고).
    //
    // **배치 위치**: PersistentServices.prefab에 컴포넌트로 부착 (DontDestroyOnLoad).
    //   PersistentServicesBootstrap이 첫 씬 전에 Instantiate + DontDestroyOnLoad.
    //   이후 모든 씬 로드 이벤트를 이 컴포넌트가 받음.
    //
    // **spawn 책임 범위 (헌법 §1 Server Authority)**:
    //   Spawner = GameObject 생성만.
    //   위치/entityId/스탯 = 서버 권위 경로가 처리:
    //     - 초기 진입: HandleEnterMap → LocalPlayerController.Instance.SetServerPosition()
    //     - 맵 전환:   HandleMapTransition → PendingSpawn 세팅 → Awake()에서 소비
    //
    // **초기 진입 race (봉합됨)**:
    //   sceneLoaded에서 Instantiate → LocalPlayerController.Awake() → Instance 등록.
    //   S_EnterMap이 Instance 등록보다 *먼저* 처리되는 경우, HandleEnterMap이 Instance null을 보고
    //   좌표를 PendingSpawn에 보관(경고 X) → 곧 spawn될 LocalPlayerController.Awake()가 소비.
    //   맵 전환(HandleMapTransition)과 동일한 PendingSpawn 메커니즘으로 대칭 봉합 (M4.2).
    [DisallowMultipleComponent]
    public class LocalPlayerSpawner : MonoBehaviour
    {
        // Inspector에서 LocalPlayer.prefab 드래그 연결 (경로: Assets/Prefabs/Characters/LocalPlayer.prefab).
        // RemoteEntityRegistry._remotePlayerPrefab 패턴과 동일.
        [SerializeField] private GameObject? _localPlayerPrefab;

        // 게임플레이 맵 목록 — 이 씬에서만 LocalPlayer를 spawn.
        // Ending 제외 (2026-05-28 결정): Ending은 게임플레이 맵이 아니라 "게임 끝" UI 화면
        //   (EndingController + 메인으로 버튼)이므로 지형/플레이어 spawn을 하지 않음.
        //   BossRoom 포탈로 진입하면 서버는 플레이어를 Ending 맵에 두지만, 클라는 순수 UI만 표시 →
        //   "메인으로" 클릭 시 NetworkService.Disconnect + MainMenu (옛 Town↔Ending 루프백 설계 폐기).
        // MainMenu / CharacterSelect / UI 씬도 플레이어 GameObject 불필요 → 처음부터 제외.
        static readonly string[] GameplayScenes = { "Town", "HuntingGround", "BossRoom" };

        // sceneLoaded 구독: CombatBootstrap의 정적 InstallAutoBoot 패턴과 달리
        // 여기선 인스턴스 컴포넌트이므로 OnEnable/OnDisable이 적합.
        // PersistentServices는 DontDestroyOnLoad라 OnEnable = 앱 시작 시 1회만 발동.
        // OnDisable = 앱 종료 시 정리 (메모리 누수 차단).
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 게임플레이 맵이 아니면 skip (MainMenu/CharacterSelect/UI 등).
            if (System.Array.IndexOf(GameplayScenes, scene.name) < 0) return;

            // 이미 살아있는 Instance가 있으면 중복 spawn 차단.
            // DontDestroyOnLoad 씬에서 Play 중 같은 씬을 Reload해도 안전.
            if (LocalPlayerController.Instance != null)
            {
                Debug.Log($"[LocalPlayerSpawner] '{scene.name}': LocalPlayerController.Instance 이미 존재 — spawn 생략 (중복 방지).");
                return;
            }

            if (_localPlayerPrefab == null)
            {
                Debug.LogError("[LocalPlayerSpawner] _localPlayerPrefab이 null입니다. " +
                               "Inspector에서 PersistentServices → LocalPlayerSpawner 컴포넌트에 " +
                               "LocalPlayer.prefab을 연결하세요 (Assets/Prefabs/Characters/LocalPlayer.prefab).");
                return;
            }

            // Instantiate: 위치는 origin (0,0,0). 서버 권위 좌표 적용은 이후 흐름에 위임.
            // - 초기 진입: HandleEnterMap → SetServerPosition()
            // - 맵 전환:   LocalPlayerController.Awake() → PendingSpawn 소비 → SetServerPosition()
            // Spawner는 GameObject 생성만 — 헌법 §1 Server Authority 정합.
            GameObject go = Instantiate(_localPlayerPrefab, Vector3.zero, Quaternion.identity);
            go.name = "LocalPlayer"; // "LocalPlayer(Clone)" → "LocalPlayer" (Hierarchy 가독성)

            // 로드된 게임플레이 씬에 소속시킴 (DontDestroyOnLoad 씬에 박히지 않게).
            // 씬 전환 시 SceneManager.LoadScene(Single)이 이 GameObject를 자동 파괴 →
            // LocalPlayerController.OnDestroy가 Instance 정리 → 다음 씬 로드 시 재spawn.
            SceneManager.MoveGameObjectToScene(go, scene);

            // M4.2 Phase 04: 카메라 연결 ("생성 후 셋업").
            //   CameraFollow.target은 [SerializeField]라 보통 씬에서 Inspector로 연결하지만,
            //   LocalPlayer는 런타임 동적 spawn이라 씬 카메라가 미리 가리킬 수 없음 → target이 비어
            //   LateUpdate가 return → 카메라가 안 따라감. spawn 직후 여기서 꽂아줌.
            //   SetTarget이 즉시 snap도 하므로 맵 전환 직후 카메라 점프("위치 렉")도 방지.
            CameraFollow camera = FindAnyObjectByType<CameraFollow>();
            if (camera != null)
                camera.SetTarget(go.transform);
            else
                Debug.LogWarning($"[LocalPlayerSpawner] '{scene.name}': CameraFollow를 찾지 못했습니다 — " +
                                 "카메라가 플레이어를 따라가지 못합니다. 씬에 CameraFollow가 붙은 Main Camera가 있는지 확인하세요.");

            Debug.Log($"[LocalPlayerSpawner] '{scene.name}': LocalPlayer Instantiate 완료. " +
                      "위치는 서버 권위 경로(HandleEnterMap / PendingSpawn)가 설정합니다.");
        }
    }
}
