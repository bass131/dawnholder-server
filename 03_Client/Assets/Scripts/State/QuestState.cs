#nullable enable
using System;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 클라 퀘스트 미러 — 서버 S_QuestUpdate 통보를 거울처럼 저장.
    // 클라이언트가 임의로 변경하지 않음 (헌법 §1). QuestProgressHud가 이벤트를 구독해 렌더.
    //
    // **싱글톤 패턴**: PartyState와 동형.
    //   CombatBootstrap이 씬 진입 직후 코드 주도로 생성.
    //   씬 간 이동 시 퀘스트 상태는 세션 지속 동안 유효 → DontDestroyOnLoad.
    [DisallowMultipleComponent]
    public class QuestState : MonoBehaviour
    {
        public static QuestState Instance { get; private set; } = null!;

        public int CurrentCount { get; private set; }
        public int TargetCount  { get; private set; }

        // UI 구독용 이벤트 — S_QuestUpdate 수신 → 발화.
        public event Action? OnQuestUpdated;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null!;
        }

        // QuestUpdateHandler가 MainThreadDispatcher 경유 메인 스레드에서 호출.
        public void ApplyUpdate(int currentCount, int targetCount)
        {
            CurrentCount = currentCount;
            TargetCount  = targetCount;

            OnQuestUpdated?.Invoke();
        }
    }
}
