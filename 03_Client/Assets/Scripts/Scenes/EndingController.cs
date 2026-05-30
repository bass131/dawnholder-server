using UnityEngine;
using UnityEngine.SceneManagement;
using Dawnholder.Client.UI;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 엔딩 화면 컨트롤러. 보스 처치 후 종료점 — "메인으로" 버튼 클릭 시 MainMenu Scene 로드.
    ///
    /// 헌법 #1 (Server Authority): 본 Scene은 단순 UI 흐름, 네트워크 X.
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        public void OnMainClicked()
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("MainMenu");
            }
            else
            {
                // Fallback: Ending Scene 단독 Editor Play 시 SceneTransition Singleton 미박힘 가능
                Debug.LogWarning("[Ending] SceneTransition.Instance is null — direct LoadScene fallback");
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
