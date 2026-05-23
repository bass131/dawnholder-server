using UnityEngine;
using Dawnholder.Client.UI;
using Shared.Protocol;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 캐릭터 선택 화면 컨트롤러 (M3.8 Phase 03 → M4.1 Phase 02 개정).
    ///
    /// **M4.1 Phase 02 변경 (5-A)**: 패킷 즉시 송신 제거. 선택값을 PlayerPrefs에만 저장 후
    /// Gameplay Scene 로드. 실제 `C_CharacterSelect` 송신은 Gameplay Scene의
    /// NetworkBootstrap이 S_HandshakeResult(ok=true) 수신 후 event 기반으로 처리.
    ///
    /// **왜 변경했나**: CharacterSelect 씬에서는 TCP 연결(NetworkBootstrap)이 아직 시작
    /// 되지 않아 UnityClientSession.Instance가 null임. 연결 전 패킷 송신 시도 = race 위험.
    /// event 기반(S_HandshakeResult 수신 후 송신) = 헌법 #3 정합 (handshake 완료 전 입력 = untrusted).
    ///
    /// **헌법 #1 (Server Authority)**: 클라는 *선택 의도만* PlayerPrefs 저장.
    /// 서버가 PlayerStats 박음. 클라가 stats(HP/Attack) 직접 박지 않음.
    ///
    /// **PlayerPrefs key**: "SelectedCharacterClass" (byte, 0=Warrior / 1=Ranger).
    /// 미박힘(key 없음) 또는 값 invalid(0/1 외) 시 NetworkBootstrap이 MainMenu로 돌려보냄.
    ///
    /// **SceneTransition Singleton 활용** (정유현 Phase 05 박은 fade 패턴).
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        // M4.1 Phase 02 5-A: PlayerPrefs key. NetworkBootstrap + CharacterSelectController 동일 상수.
        // NetworkBootstrap에서도 동일 key 읽으므로 상수명 일치 필수.
        public const string SelectedClassPrefsKey = "SelectedCharacterClass";

        public void OnWarriorClicked()
        {
            SaveSelectAndLoad(CharacterClass.Warrior);
        }

        public void OnRangerClicked()
        {
            SaveSelectAndLoad(CharacterClass.Ranger);
        }

        void SaveSelectAndLoad(CharacterClass characterClass)
        {
            // M4.1 Phase 02 5-A: PlayerPrefs에 선택값 저장. 실제 패킷 송신은 Gameplay 씬에서.
            // byte 0=Warrior / 1=Ranger. CharacterClass enum cast 박힘.
            PlayerPrefs.SetInt(SelectedClassPrefsKey, (int)(byte)characterClass);
            PlayerPrefs.Save();

            Debug.Log($"[CharacterSelect] Saved SelectedCharacterClass={characterClass} ({(byte)characterClass}) to PlayerPrefs → loading Gameplay Scene");

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
