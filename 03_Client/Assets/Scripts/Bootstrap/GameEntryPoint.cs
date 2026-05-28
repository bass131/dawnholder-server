using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using UnityEngine;

namespace Dawnholder.Client.Bootstrap
{
    /// <summary>
    /// 게임플레이 진입(첫 Town) 시 연결을 명시적으로 트리거하는 컴포넌트 (ADR-027 A안).
    ///
    /// **씬 배치 (사용자 작업 필요)**:
    ///   Town 씬의 아무 GameObject에나 붙이세요.
    ///   예: "GameManager" 빈 GameObject 신규 생성 후 AddComponent.
    ///   이 컴포넌트는 Start()에서 1회 NetworkService.Connect()를 시도한 뒤
    ///   역할이 끝납니다 (이후 프레임에서 아무 일도 하지 않음).
    ///
    /// **포탈 루프백 안전망**:
    ///   Ending→Town 재진입처럼 이미 연결된 상태에서 씬이 다시 로드돼도
    ///   NetworkService.IsConnected 가드로 인해 재연결이 일어나지 않습니다.
    ///   연결이 맵 전환 내내 유지되는 ADR-027 A안의 핵심 동작입니다.
    ///
    /// **PlayerPrefs 의존**:
    ///   host  = PlayerPrefs "ServerHost"            (MainMenuController probe 성공 시 박음)
    ///   class = PlayerPrefs "SelectedCharacterClass" (CharacterSelectController 박음)
    ///   두 값이 없거나 invalid면 NetworkService.Connect() 내부에서 MainMenu로 돌려보냄.
    ///
    /// **헌법 #1 (Server Authority)**:
    ///   연결 시점을 명시화할 뿐 — 권위 상태 변경 없음. 서버 응답(S_EnterMap, S_Snapshot)이
    ///   도착해야 비로소 게임 세계가 확정됩니다.
    /// </summary>
    public class GameEntryPoint : MonoBehaviour
    {
        void Start()
        {
            NetworkService service = NetworkService.Instance;

            if (service == null)
            {
                // PersistentServices 프리팹이 없거나 PersistentServicesBootstrap가 실패한 경우.
                // 에디터에서 Town 씬 단독 Play 시 발생할 수 있음.
                Debug.LogError(
                    "[GameEntryPoint] NetworkService.Instance null — PersistentServices 프리팹이 없거나 " +
                    "PersistentServicesBootstrap 초기화에 실패했습니다.\n" +
                    "확인 사항:\n" +
                    "  1. Assets/Resources/PersistentServices.prefab 존재 여부\n" +
                    "  2. PersistentServicesBootstrap.cs가 Assets/Scripts/Bootstrap/ 에 있는지\n" +
                    "  3. 프리팹에 NetworkService 컴포넌트가 붙어 있는지");

                // SceneTransition도 같은 프리팹 소속이라 없을 가능성이 높으므로 직접 fallback.
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                return;
            }

            if (service.IsConnected)
            {
                // 포탈 루프백(Ending→Town) 재진입 — 이미 연결됨, 아무것도 하지 않음.
                Debug.Log("[GameEntryPoint] 이미 연결된 상태 — Connect() 생략 (포탈 루프백).");
                return;
            }

            // 첫 게임플레이 진입: PlayerPrefs에서 host/class를 읽어 Connect.
            // 파라미터 없이 호출하면 NetworkService 내부에서 PlayerPrefs fallback.
            service.Connect();
        }
    }
}
