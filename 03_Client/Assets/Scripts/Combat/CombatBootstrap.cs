#nullable enable
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Combat
{
    // 씬 진입 시 combat 인프라 자동 셋업.
    //
    // **자동 진입** — `RuntimeInitializeOnLoadMethod`로 씬 로드 직후 GameObject 자동 생성.
    //   맵 씬 안에 컴포넌트 박을 필요 없음 → 씬 YAML 편집 0건.
    //   발동 대상: 전투 맵(Town/HuntingGround/BossRoom + GameplayTest) — OnSceneLoaded의 CombatScenes 참조.
    //
    // **셋업 순서** (Awake — 다른 컴포넌트 진입 전 보장):
    //   1. EnemyRegistry — enemy/boss spawn 받기 위한 싱글톤.
    //   2. StageClearUI — S_StageClear 도착 시 표시할 Canvas.
    //   3. RemoteEntityRegistry — S_PlayerJoin/S_Snapshot 타인 분기 수신.
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

        // 전투 인프라가 필요한 게임플레이 맵에서만 발동. MainMenu/CharacterSelect/UI/Ending(결과화면)은 skip.
        // GameplayTest는 테스트 씬용.
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
            // ZoneVisualizer 자동 생성 *비활성*: 사용자가 BackGround.prefab을 씬에 직접 박아
            // 코드 빌드와 중복되면 두 배경 박힘. ZoneVisualizer 클래스는 fallback 용으로 보존.
            // BuildZoneVisualizer();
            BuildEnemyRegistry();
            BuildStageClearUI();
            BuildToastUI();
            BuildRemoteEntityRegistry();
            BuildPartyState();
            BuildQuestState();
            // 파티/퀘스트 HUD는 UI.unity 씬에 배치된 패널(PartyMemberHud/QuestProgressHud 컴포넌트)이
            // PartyState/QuestState를 직접 구독 — 런타임 빌드 폐지(M6 Phase 05, 영호 UI.unity 정식 채택).
            BuildPartyInvitePopup();
            BuildNpcDialogPanel();
            BuildMinimapCamera();
            BuildQuestIntro();
        }

        // 퀘스트 부여 연출 — 사냥 구역(HuntingGround/BossRoom) 진입 시에만.
        // QuestIntroSequencer가 세션 1회 팝업(Fade in/out) → 퀘스트 HUD 출현 순서를 조율.
        // 마을(Town)에선 퀘스트 HUD를 띄우지 않음 — 퀘스트 목표가 사냥터 한정이라.
        void BuildQuestIntro()
        {
            string sceneName = gameObject.scene.name;
            if (sceneName != "HuntingGround" && sceneName != "BossRoom") return;
            QuestIntroSequencer.Run(transform);
        }

        // 미니맵 카메라 — Resources/MinimapRT에 줌아웃 사이드뷰 렌더. UI.unity RawImage가 표시.
        void BuildMinimapCamera()
        {
            RenderTexture? rt = Resources.Load<RenderTexture>("MinimapRT");
            if (rt == null)
            {
                Debug.LogWarning("[CombatBootstrap] Resources/MinimapRT.renderTexture 없음 — 미니맵 비활성.");
                return;
            }
            MinimapCamera.BuildRuntime(parent: transform, rt: rt);
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

        void BuildToastUI()
        {
            if (ToastUI.Instance != null) return;
            ToastUI.BuildRuntime(parent: transform);
        }

        // **prefab 참조 전략 (Resources.Load)**:
        // RemoteEntityRegistry는 [SerializeField] _remotePlayerPrefab이 필요한데,
        // 코드 주도 패턴에서 Inspector 드래그가 없으므로 Resources.Load("RemotePlayer")로 주입.
        // ⚠️ 의무: Assets/Resources/RemotePlayer.prefab 이 존재해야 함.
        //   없으면 아래 경고가 박히고 registry 자체는 생성되나 spawn 시 또 에러.
        // 파티/퀘스트 미러 State = DontDestroyOnLoad 싱글톤. 핸들러가 갱신, HUD가 구독·렌더.
        // HUD 빌드보다 먼저 생성해야 HUD가 OnXxxUpdated 구독 성공(없으면 null guard로 미구독).
        // root 레벨 생성(SetParent 금지) — 자식이면 DontDestroyOnLoad 안 먹고 씬과 함께 파괴.
        void BuildPartyState()
        {
            if (PartyState.Instance != null) return;
            new GameObject("_PartyState").AddComponent<PartyState>();
        }

        void BuildQuestState()
        {
            if (QuestState.Instance != null) return;
            new GameObject("_QuestState").AddComponent<QuestState>();
        }

        void BuildPartyInvitePopup()
        {
            if (PartyInvitePopup.Instance != null) return;
            PartyInvitePopup.BuildRuntime(parent: transform);
        }

        void BuildNpcDialogPanel()
        {
            if (NpcDialogPanel.Instance != null) return;
            NpcDialogPanel.BuildRuntime(parent: transform);
        }

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
