#nullable enable
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Combat
{
    // M3 Phase 08b/08c: 씬 진입 시 combat 인프라 자동 셋업.
    //
    // **이 패턴의 이유** (응급 결정 5/19):
    //   - 정유현 씬 YAML 영역 격리 — Gameplay.unity 직접 편집 회피.
    //   - 씬 conflict 차단 — git 머지 시 YAML diff 충돌 0.
    //   - 박힌 후 누락 검출 단순 — Inspector 사진 비교 X, *코드*가 진실.
    //
    // **자동 진입** — `RuntimeInitializeOnLoadMethod`로 씬 로드 직후 GameObject 자동 생성.
    //   맵 씬 안에 컴포넌트 박을 필요 없음 → 씬 YAML 편집 0건.
    //   발동 대상: 전투 맵(Town/HuntingGround/BossRoom + GameplayTest) — OnSceneLoaded의 CombatScenes 참조. (M4.2 ADR-027)
    //
    // **셋업 순서** (Awake — 다른 컴포넌트 진입 전 보장):
    //   1. ZoneVisualizer — 3-zone 배경.
    //   2. EnemyRegistry — enemy/boss spawn 받기 위한 싱글톤.
    //   3. StageClearUI — S_StageClear 도착 시 표시할 Canvas.
    //   4. RemoteEntityRegistry — S_PlayerJoin/S_Snapshot 타인 분기 수신 (P2 봉합, 2026-05-28).
    //
    // **SceneBootstrap과의 관계**: SceneBootstrap이 UI 씬 Additive 로드 담당.
    //   본 컴포넌트는 *전투 맵 씬*에 박혀서 combat 인프라만. 책임 분리.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // 다른 MonoBehaviour보다 먼저 Awake (UnityClientSession Dispatch 도착 전 ready)
    public class CombatBootstrap : MonoBehaviour
    {
        // 씬 로드 직후 (Awake 전) 자동 발동. SceneManager.sceneLoaded subscribe 후 한 번만.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallAutoBoot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // M4.2 (ADR-027): 옛 단일 "Gameplay" 씬이 Town/HuntingGround/BossRoom로 분리됨.
        // 전투 인프라가 필요한 게임플레이 맵에서만 발동. MainMenu/CharacterSelect/UI/Ending(결과화면)은 skip.
        // GameplayTest는 테스트 씬용으로 유지.
        static readonly string[] CombatScenes = { "Town", "HuntingGround", "BossRoom", "GameplayTest" };

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (System.Array.IndexOf(CombatScenes, scene.name) < 0) return;
            // 이미 있으면 noop (씬에 수동 박힘 + auto 동시 케이스 안전).
            if (FindAnyObjectByType<CombatBootstrap>() != null) return;

            GameObject root = new GameObject("_CombatBootstrap");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<CombatBootstrap>();
        }

        void Awake()
        {
            // M3 hardening 5/20: ZoneVisualizer 자동 생성 *비활성*.
            // 사용자가 BackGround.prefab을 Gameplay 씬에 직접 박았기 때문 — 코드 빌드와 중복되면 두 배경 박힘.
            // ZoneVisualizer 클래스는 fallback 용으로 보존 (수동 박을 수 있게). GameplayTest 씬은 자체 Bg GameObject들 박혀있음.
            // BuildZoneVisualizer();
            BuildEnemyRegistry();
            BuildStageClearUI();
            BuildRemoteEntityRegistry(); // P2 봉합 (2026-05-28): 4 씬 분리 후 누락 — 멀티플레이 타인 표시 불가 봉합.
        }

        void BuildZoneVisualizer()
        {
            if (FindAnyObjectByType<ZoneVisualizer>() != null) return;
            GameObject go = new GameObject("_ZoneVisualizer");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.AddComponent<ZoneVisualizer>();
        }

        void BuildEnemyRegistry()
        {
            if (EnemyRegistry.Instance != null) return;
            GameObject go = new GameObject("_EnemyRegistry");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.AddComponent<EnemyRegistry>();
        }

        void BuildStageClearUI()
        {
            if (StageClearUI.Instance != null) return;
            // 독립 root — Canvas는 자식보다 root로 두는 게 일반적.
            StageClearUI.BuildRuntime(parent: transform);
        }

        // P2 봉합 (2026-05-28 β cross-review):
        // 옛 Gameplay.unity에 _RemoteEntityRegistry GameObject가 수동으로 박혀있었는데
        // M4.2 Phase 04에서 Town/HuntingGround/BossRoom 4 씬으로 분리될 때 누락됨.
        // 2 client 접속 시 RemoteEntityRegistry.Instance == null → S_PlayerJoin/S_Snapshot 타인 분기 silent drop.
        //
        // **prefab 참조 전략 (Resources.Load)**:
        // RemoteEntityRegistry는 [SerializeField] _remotePlayerPrefab이 필요하다.
        // 코드 주도 패턴에서 Inspector 드래그가 없으므로 Resources.Load("RemotePlayer")로 주입.
        // ⚠️ 의무: Assets/Resources/RemotePlayer.prefab 이 존재해야 함.
        //   없으면 아래 경고가 박히고 registry 자체는 생성되나 spawn 시 또 에러 (RemoteEntityRegistry.Spawn 참조).
        //   메인 세션 MCP 작업으로 prefab 확인/이동 필요 시 안내 박힘.
        void BuildRemoteEntityRegistry()
        {
            if (RemoteEntityRegistry.Instance != null) return;

            GameObject go = new GameObject("_RemoteEntityRegistry");
            go.transform.SetParent(transform, worldPositionStays: false);
            RemoteEntityRegistry registry = go.AddComponent<RemoteEntityRegistry>();

            // Resources.Load로 RemotePlayer prefab 주입.
            // RemoteEntityRegistry._remotePlayerPrefab은 [SerializeField] private — 직접 필드 접근 X.
            // SetRemotePlayerPrefab 공개 메서드로 주입 (아래 참조).
            GameObject? prefab = Resources.Load<GameObject>("RemotePlayer");
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[CombatBootstrap] Resources/RemotePlayer.prefab 을 찾을 수 없음. " +
                    "멀티플레이 시 타인 캐릭터가 보이지 않습니다.\n" +
                    "조치: Assets/Resources/ 폴더에 RemotePlayer.prefab 을 복사/이동하거나 " +
                    "메인 세션 MCP 작업으로 처리하세요.");
                // registry는 생성됨 — prefab 없어도 registry 자체는 작동 (Spawn 시 에러 박힘).
            }
            else
            {
                registry.SetRemotePlayerPrefab(prefab);
            }
        }
    }
}
