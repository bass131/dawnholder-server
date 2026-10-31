using Dawnholder.Client.Audio;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 캐릭터 선택 화면 컨트롤러. 두 경로의 Unity 수명만 잇고 응답을 해석하지 않는다.
    ///
    /// **메뉴 경로(기본)**: NetworkService가 있으면 씬 시작 때 메뉴 접속을 열고 CharacterMenuView와
    /// CharacterMenuPresenter를 만든다. character ID와 응답 해석은 presenter가 가진다. 선택이 승인되면
    /// 기존 48 → 맵 입장 흐름이 Town을 열며, 이 컨트롤러는 씬을 열지 않는다.
    ///
    /// **Legacy 경로**: 서버가 Legacy 전용(16)을 돌려주거나 NetworkService가 없으면(에디터 씬 단독 Play)
    /// Knight/Mage 버튼이 선택값을 PlayerPrefs에만 저장 후 Town Scene 로드. 실제 `C_CharacterSelect` 송신은
    /// Town 씬의 NetworkService가 S_HandshakeResult(ok=true) 수신 후 event 기반으로 처리.
    ///
    /// **왜 PlayerPrefs 저장만 하나**: Legacy 경로에서는 메뉴 연결을 이미 끊었거나 처음부터 없어서
    /// UnityClientSession.Instance가 null임. 연결 전 패킷 송신 시도 = race 위험.
    /// event 기반(S_HandshakeResult 수신 후 송신) = 헌법 #3 정합 (handshake 완료 전 입력 = untrusted).
    ///
    /// **헌법 #1 (Server Authority)**: 클라는 *선택 의도만* 보낸다.
    /// 서버가 PlayerStats 박음. 클라가 stats(HP/Attack) 직접 박지 않음.
    ///
    /// **PlayerPrefs key**: "SelectedCharacterClass" (byte, 0=Knight / 1=Mage).
    /// 미박힘(key 없음) 또는 값 invalid(0/1 외) 시 NetworkService가 MainMenu로 돌려보냄.
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        // NetworkService에서도 동일 key 읽으므로 상수명 일치 필수.
        public const string SelectedClassPrefsKey = "SelectedCharacterClass";

        // 메뉴 접속 동안만 있다. Legacy 전환과 씬 파괴 때 CloseMenu가 정리한다.
        CharacterMenuPresenter _menu;
        CharacterMenuView _menuView;

        // Last display projection only; the presenter continues to own character IDs and replies.
        #nullable enable
        internal CharacterMenuViewState? MenuState { get; private set; }
        #nullable restore

        void Start()
        {
            NetworkService network = NetworkService.Instance;
            // PersistentServices가 없으면(에디터 씬 단독 Play) 기존 버튼 동작만 남는다.
            if (network == null) return;
            _menu = new CharacterMenuPresenter(state =>
            {
                MenuState = state;
                _menuView?.Render(state);
            }, network.Disconnect, OnMenuExit);
            _menuView = new CharacterMenuView(gameObject, _menu.Select, _menu.Create, _menu.Refresh, _menu.Back);
            // 주소를 새로 정하지 않는다: MainMenu가 probe 성공 뒤 저장한 ServerHost와 NetworkService의 포트를 쓴다.
            network.ConnectCharacterMenu();
        }

        void Update()
        {
            if (_menu == null) return;
            NetworkService network = NetworkService.Instance;
            // 매 프레임 현재 연결의 client를 넘긴다. 연결이 바뀌거나 끊기면 presenter가 옛 구독을 푼다.
            _menu.Observe(network != null ? network.CharacterMenu : null, network != null && network.IsConnected);
        }

        // 씬 파괴는 연결을 끊지 않는다. 선택 승인 뒤 Town 로드가 이 씬을 내려도 같은 연결로 입장한다.
        void OnDestroy() => CloseMenu();

        public void OnKnightClicked()
        {
            // 메뉴가 열린 동안에는 키보드 이동으로 메뉴 뒤 버튼을 골라도 메뉴 연결로 Town에 가지 않는다.
            if (_menuView != null) return;
            AudioManager.Instance?.PlaySfx(SoundKeys.ButtonClick);
            SaveSelectAndLoad(CharacterClass.Knight);
        }

        public void OnMageClicked()
        {
            if (_menuView != null) return;
            AudioManager.Instance?.PlaySfx(SoundKeys.ButtonClick);
            SaveSelectAndLoad(CharacterClass.Mage);
        }

        void OnMenuExit(CharacterMenuPresenter.Exit exit)
        {
            // presenter가 메뉴 연결을 이미 끊었다.
            if (exit == CharacterMenuPresenter.Exit.MainMenu)
            {
                // 화면은 뒤로 가기에서 잠겼고, 씬이 내려갈 때 OnDestroy가 정리한다.
                LoadScene("MainMenu");
                return;
            }
            // Legacy 전용: 메뉴를 닫아 이 씬의 Knight/Mage 버튼으로 넘긴다.
            CloseMenu();
        }

        void CloseMenu()
        {
            _menu?.Dispose();
            _menu = null;
            _menuView?.Dispose();
            _menuView = null;
            MenuState = null;
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

            // 진입 맵은 "Town".
            LoadScene("Town");
        }

        static void LoadScene(string sceneName)
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[CharacterSelect] SceneTransition.Instance is null — direct LoadScene fallback");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
    }
}
