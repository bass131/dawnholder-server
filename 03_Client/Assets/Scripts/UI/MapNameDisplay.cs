#nullable enable
using Dawnholder.Client.Network;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    // map_name TMP 전용 컨트롤러 (SRP: HudController는 자원 표시, 맵 이름은 씬 전환 생존 책임이 다름).
    //
    // 씬 전환 시 UI 씬이 내려갔다 재로드 → TMP 인스턴스가 매번 새로 생김.
    // static 저장 + 인스턴스 Start()에서 표시하는 패턴으로 재로드 후 표시 복원.
    public class MapNameDisplay : MonoBehaviour
    {
        static byte s_currentMapId;         // 기본 0 = Town (S_EnterMap은 mapId 미포함 — ClientPacketHandlers.cs 약속)
        static MapNameDisplay? s_instance;

        [SerializeField] TMP_Text? _label;  // 씬에서 연결. null이면 GetComponent 폴백.

        public static void SetMapId(byte mapId)
        {
            s_currentMapId = mapId;
            s_instance?.Refresh();
        }

        internal static void ResetForTest()
        {
            s_currentMapId = 0;
            s_instance = null;
        }

        void Start()
        {
            s_instance = this;
            if (_label == null)
                _label = GetComponent<TMP_Text>();
            Refresh();
        }

        void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        void Refresh()
        {
            if (_label == null) return;
            string name = SceneRouter.MapIdToDisplayName(s_currentMapId);
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError($"[MapNameDisplay] 알 수 없는 mapId={s_currentMapId} — SceneRouter 매핑 누락.");
                return;
            }
            _label.text = name;
        }
    }
}
