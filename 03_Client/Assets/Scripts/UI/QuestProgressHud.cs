#nullable enable
using System.Collections;
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
    //
    // **자기-비활성 함정 회피**: SetActive 금지. CanvasGroup.alpha로만 표시/숨김.
    //   GameObject는 항상 활성 → Update(재바인딩) + 이벤트 구독이 끊기지 않음.
    [DisallowMultipleComponent]
    public class QuestProgressHud : MonoBehaviour
    {
        public static QuestProgressHud? Instance { get; private set; }

        // 영호 Phase 05 조정 지점 — 문구.
        internal const string QuestName      = "마을의 위협";
        internal const string QuestObjective = "사냥터의 몬스터 처치";

        [SerializeField] TMP_Text? _titleText;     // Quest_Hud/Title
        [SerializeField] TMP_Text? _objectiveText; // Quest_Hud/Explain
        [SerializeField] TMP_Text? _progressText;  // Quest_Hud/Prograss

        CanvasGroup? _group;
        bool _subscribed;
        Coroutine? _revealRoutine;

        void Awake()
        {
            Instance = this;
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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

        // alpha와 무관하게 항상 텍스트 갱신 — 서버값 반영(헌법 §1).
        void Refresh()
        {
            if (_titleText != null)     _titleText.text     = QuestName;
            if (_objectiveText != null) _objectiveText.text = QuestObjective;
            if (_progressText != null && QuestState.Instance != null)
                _progressText.text = $"{QuestState.Instance.CurrentCount} / {QuestState.Instance.TargetCount}";
        }

        // QuestIntroSequencer가 팝업 완료 후 호출 — HUD를 부드럽게 출현시킴.
        // 이미 보이는 상태면 noop.
        public void Reveal()
        {
            if (_group == null) return;
            if (_group.alpha >= 1f) return;
            if (_revealRoutine != null) StopCoroutine(_revealRoutine);
            _revealRoutine = StartCoroutine(RevealRoutine());
        }

        IEnumerator RevealRoutine()
        {
            if (_group == null) yield break;

            const float fadeIn = 0.3f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha          = 1f;
            _group.interactable   = true;
            _group.blocksRaycasts = true;
            _revealRoutine = null;
        }
    }
}
