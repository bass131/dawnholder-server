using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Bootstrap
{
    /// <summary>
    /// 게임플레이 씬 진입 시 UI 씬을 Additive 로드. ad-hoc UI Scene 분리 결정의 런타임 트리거.
    ///
    /// **헌법 #1 (Server Authority)**: 씬 *로드*만 트리거. 게임 권위 상태 변경 0.
    ///
    /// **멀티 씬 편집 안전망**: Editor에서 UI 씬이 이미 열려있으면 중복 로드 회피 (가드).
    /// 빌드 런타임에선 UI 씬이 *없는* 상태로 진입하므로 항상 로드됨. 같은 게임오브젝트가
    /// MainMenu→Gameplay 씬 전환(LoadSceneMode.Single)으로 새로 시작될 때마다 Awake가
    /// 다시 불려 UI 씬을 다시 로드 — Single 모드가 기존 씬을 unload하므로 의도된 동작.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] string uiSceneName = "UI";

        void Awake()
        {
            var existing = SceneManager.GetSceneByName(uiSceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                return;
            }
            SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        }
    }
}
