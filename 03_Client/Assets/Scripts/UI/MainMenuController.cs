using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 메인 메뉴 버튼 핸들러. MainMenu 씬의 "시작" / "종료" 버튼 onClick에 연결.
    ///
    /// **헌법 #1 (Server Authority)**: 시작 버튼은 *씬 로드*만 트리거. 캐릭터/인벤토리/연결
    /// 같은 권위 상태는 건드리지 않음. 서버 연결 흐름은 후속 마일스톤(M3+)에서 별도 도입.
    ///
    /// **SceneTransition Singleton 경유** (페이드 폴리시, 정유현 Phase 05 박음). 직접 SceneManager 호출 X.
    ///
    /// **M3.8 Phase 02 — Demo flow 재정렬**: 옛 시작 버튼 = Gameplay 직접 로드 (캐릭터 선택 건너뜀).
    /// 새 흐름 = MainMenu → CharacterSelect → Gameplay → Ending (캡스톤 1 발표 데모 flow).
    /// Phase 03 박힌 후 placeholder Debug.Log 제거 + LoadScene 호출 활성화.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public void OnStartClicked()
        {
            // M3.8 Phase 04 활성화 — Phase 03 박힘 후 CharacterSelect Scene 실재 박힘.
            // Demo flow = MainMenu → CharacterSelect → Gameplay → Ending.
            SceneTransition.Instance.LoadScene("CharacterSelect");
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
