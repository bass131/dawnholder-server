using UnityEngine;
using UnityEngine.SceneManagement;
using Dawnholder.Client.UI;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 엔딩 화면 컨트롤러 (M3.8 Phase 02).
    /// 보스 처치 후 시연 종료점 — "메인으로" 버튼 클릭 시 MainMenu Scene 로드.
    ///
    /// 헌법 #1 (Server Authority): 본 Scene은 단순 UI 흐름, 네트워크 X.
    /// 보스 처치 트리거 (S_StageClear 패킷 수신 → 본 Scene 전환)는 Phase 03/04 박힌 후
    /// GameplayController 또는 RemoteEntityRegistry에서 dispatch.
    ///
    /// SceneTransition Singleton 활용 (정유현 Phase 05 박은 fade 패턴).
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
