#nullable enable
using Dawnholder.Client.State;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    // 퀘스트 완료 감지 → "퀘스트 완료!" 임팩트 연출(QuestAlert) 1회 재생.
    // CombatBootstrap이 사냥 구역에서 설치. QuestState.OnQuestUpdated 구독해 완료 전이 감지.
    //
    // **세션 1회**: _shown static — 완료 연출은 세션당 한 번만.
    [DisallowMultipleComponent]
    public class QuestCompleteWatcher : MonoBehaviour
    {
        static bool _shown;

        Transform _alertParent = null!;
        bool _subscribed;

        public static void Install(Transform parent)
        {
            if (_shown) return; // 이미 완료 연출 봤으면 설치 불필요
            GameObject go = new GameObject("_QuestCompleteWatcher");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<QuestCompleteWatcher>()._alertParent = parent;
        }

        void OnEnable() => TryBind();
        void Start()    => TryBind();
        void Update()   { if (!_subscribed) TryBind(); }

        void TryBind()
        {
            if (_subscribed || QuestState.Instance == null) return;
            QuestState.Instance.OnQuestUpdated += OnQuestUpdated;
            _subscribed = true;
            OnQuestUpdated(); // 진입 시 이미 완료 상태면 즉시 처리
        }

        void OnDisable()
        {
            if (_subscribed && QuestState.Instance != null)
                QuestState.Instance.OnQuestUpdated -= OnQuestUpdated;
            _subscribed = false;
        }

        void OnQuestUpdated()
        {
            if (_shown) return;
            QuestState qs = QuestState.Instance;
            if (qs == null || qs.TargetCount <= 0) return;
            if (qs.CurrentCount < qs.TargetCount) return;

            _shown = true;
            QuestAlert alert = QuestAlert.BuildRuntime(_alertParent, QuestAlertKind.Clear);
            alert.PlayThenCallback(() => { if (alert != null) Destroy(alert.gameObject); });
        }
    }
}
