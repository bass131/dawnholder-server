#nullable enable
using Dawnholder.Client.State;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    // 퀘스트 진행 HUD — UI.unity 씬의 Quest_Hud 패널에 부착(씬 연결).
    // 서버 QuestState 미러를 구독해 이름/목표/진행도 TMP를 갱신.
    //
    // **헌법 §1**: 진행 카운트는 서버 QuestState 값만. 이름·목표는 표시용 클라 로컬 콘텐츠(단일 퀘스트).
    //
    // **UI 씬 재로드 대응**: UI.unity는 맵 전환마다 내려갔다 재로드 → 이 컴포넌트도 매번 새로 Awake.
    //   QuestState는 DontDestroyOnLoad라 지속 → Start/Update에서 (재)구독 + 즉시 Refresh로 현재값 복원.
    [DisallowMultipleComponent]
    public class QuestProgressHud : MonoBehaviour
    {
        // 영호 Phase 05 조정 지점 — 문구.
        const string QuestName      = "마을의 위협";
        const string QuestObjective = "사냥터의 몬스터 처치";

        [SerializeField] TMP_Text? _titleText;     // Quest_Hud/Title
        [SerializeField] TMP_Text? _objectiveText; // Quest_Hud/Explain
        [SerializeField] TMP_Text? _progressText;  // Quest_Hud/Prograss

        bool _subscribed;

        void OnEnable() => TryBind();
        void Start()    => TryBind();

        // QuestState가 아직 없으면(씬 비동기 로드 레이스) 준비될 때까지 재시도.
        void Update()
        {
            if (!_subscribed) TryBind();
        }

        void TryBind()
        {
            if (_subscribed || QuestState.Instance == null) return;
            QuestState.Instance.OnQuestUpdated += Refresh;
            _subscribed = true;
            Refresh();
        }

        void OnDisable()
        {
            if (_subscribed && QuestState.Instance != null)
                QuestState.Instance.OnQuestUpdated -= Refresh;
            _subscribed = false;
        }

        void Refresh()
        {
            if (_titleText != null)     _titleText.text     = QuestName;
            if (_objectiveText != null) _objectiveText.text = QuestObjective;
            if (_progressText != null && QuestState.Instance != null)
                _progressText.text = $"{QuestState.Instance.CurrentCount} / {QuestState.Instance.TargetCount}";
        }
    }
}
