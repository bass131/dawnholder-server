#nullable enable
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
    //   Gameplay.unity 안에 컴포넌트 박을 필요 없음 → 씬 YAML 편집 0건.
    //   Gameplay 씬 식별: 활성 씬 이름이 "Gameplay" 또는 "GameplayTest"일 때만 발동.
    //
    // **셋업 순서** (Awake — 다른 컴포넌트 진입 전 보장):
    //   1. ZoneVisualizer — 3-zone 배경.
    //   2. EnemyRegistry — enemy/boss spawn 받기 위한 싱글톤.
    //   3. StageClearUI — S_StageClear 도착 시 표시할 Canvas.
    //
    // **SceneBootstrap과의 관계**: SceneBootstrap이 UI 씬 Additive 로드 담당.
    //   본 컴포넌트는 *Gameplay 씬*에 박혀서 combat 인프라만. 책임 분리.
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

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Gameplay 또는 GameplayTest 씬에서만 박힘. MainMenu / UI 씬에선 skip.
            if (scene.name != "Gameplay" && scene.name != "GameplayTest") return;
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
    }
}
