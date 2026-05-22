using UnityEngine;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using Shared.Protocol;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 캐릭터 선택 화면 컨트롤러 (M3.8 Phase 03).
    /// 전사/원거리 버튼 클릭 시 `C_CharacterSelect` 패킷 전송 + Gameplay Scene 로드.
    ///
    /// **헌법 #1 (Server Authority)**: 클라는 *선택 의도만* 전송. 서버가 PlayerStats 박음.
    /// 클라가 stats(HP/Attack) 직접 보낼 경로 없음.
    ///
    /// **SceneTransition Singleton 활용** (정유현 Phase 05 박은 fade 패턴).
    ///
    /// **Phase 04 진입 트리거**: Gameplay Scene 로드 후 마을 NPC 대화 자연 흐름 (Phase 04 박힌 후).
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        public void OnWarriorClicked()
        {
            SendSelect(CharacterClass.Warrior);
        }

        public void OnRangerClicked()
        {
            SendSelect(CharacterClass.Ranger);
        }

        void SendSelect(CharacterClass characterClass)
        {
            if (UnityClientSession.Instance == null)
            {
                Debug.LogWarning("[CharacterSelect] UnityClientSession.Instance is null — server connection 미박힘. Scene 단독 Editor Play 가능성.");
                LoadGameplayScene();
                return;
            }

            // 헌법 #1 정합: characterClass byte cast만 전송. stats 직접 박지 X.
            var packet = new C_CharacterSelect { characterClass = (byte)characterClass };
            UnityClientSession.Instance.Send(packet.Write());
            Debug.Log($"[CharacterSelect] Sent C_CharacterSelect (class={characterClass})");

            LoadGameplayScene();
        }

        void LoadGameplayScene()
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("Gameplay");
            }
            else
            {
                Debug.LogWarning("[CharacterSelect] SceneTransition.Instance is null — direct LoadScene fallback");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
            }
        }
    }
}
