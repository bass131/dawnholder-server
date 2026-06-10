using UnityEngine;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.UI;
using Shared.Protocol;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 캐릭터 선택 화면 컨트롤러. 선택값을 PlayerPrefs에만 저장 후 Town Scene 로드.
    /// 실제 `C_CharacterSelect` 송신은 Town 씬의 NetworkService가 S_HandshakeResult(ok=true)
    /// 수신 후 event 기반으로 처리.
    ///
    /// **왜 PlayerPrefs 저장만 하나**: CharacterSelect 씬에서는 TCP 연결(NetworkService)이 아직 시작
    /// 되지 않아 UnityClientSession.Instance가 null임. 연결 전 패킷 송신 시도 = race 위험.
    /// event 기반(S_HandshakeResult 수신 후 송신) = 헌법 #3 정합 (handshake 완료 전 입력 = untrusted).
    ///
    /// **헌법 #1 (Server Authority)**: 클라는 *선택 의도만* PlayerPrefs 저장.
    /// 서버가 PlayerStats 박음. 클라가 stats(HP/Attack) 직접 박지 않음.
    ///
    /// **PlayerPrefs key**: "SelectedCharacterClass" (byte, 0=Knight / 1=Mage).
    /// 미박힘(key 없음) 또는 값 invalid(0/1 외) 시 NetworkService가 MainMenu로 돌려보냄.
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        // NetworkService에서도 동일 key 읽으므로 상수명 일치 필수.
        public const string SelectedClassPrefsKey = "SelectedCharacterClass";

        public void OnKnightClicked()
        {
            SaveSelectAndLoad(CharacterClass.Knight);
        }

        public void OnMageClicked()
        {
            SaveSelectAndLoad(CharacterClass.Mage);
        }

        void SaveSelectAndLoad(CharacterClass characterClass)
        {
            // 프로세스 로컬 캐시가 1순위 진실 — PlayerPrefs는 같은 PC 다중 인스턴스 간
            // 공유돼 덮어쓰기 오염 가능 (ClassLoadout.GetSelectedClassValue 주석 참조).
            ClassLoadout.SessionSelectedClass = characterClass;

            // PlayerPrefs에 선택값 저장. 실제 패킷 송신은 Town 씬에서. byte 0=Knight / 1=Mage.
            PlayerPrefs.SetInt(SelectedClassPrefsKey, (int)(byte)characterClass);
            PlayerPrefs.Save();

            Debug.Log($"[CharacterSelect] Saved SelectedCharacterClass={characterClass} ({(byte)characterClass}) to PlayerPrefs → loading Town Scene");

            LoadGameplayScene();
        }

        void LoadGameplayScene()
        {
            // 진입 맵은 "Town".
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("Town");
            }
            else
            {
                Debug.LogWarning("[CharacterSelect] SceneTransition.Instance is null — direct LoadScene fallback");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
            }
        }
    }
}
