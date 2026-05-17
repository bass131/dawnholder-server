using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 메인 메뉴 버튼 핸들러. Phase 02 — MainMenu 씬의 "시작" / "종료" 버튼 onClick에 연결.
    ///
    /// **헌법 #1 (Server Authority)**: 시작 버튼은 *씬 로드*만 트리거. 캐릭터/인벤토리/연결
    /// 같은 권위 상태는 건드리지 않음. 서버 연결 흐름은 후속 마일스톤(M3+)에서 별도 도입.
    ///
    /// **Phase 05 — SceneTransition Singleton 경유로 교체됨** (페이드 폴리시). 직접 SceneManager 호출 X.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string gameplaySceneName = "Gameplay";

        public void OnStartClicked()
        {
            SceneTransition.Instance.LoadScene(gameplaySceneName);
        }

        public void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
