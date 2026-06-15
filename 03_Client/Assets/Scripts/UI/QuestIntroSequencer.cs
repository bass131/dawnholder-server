#nullable enable
using System.Collections;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    // 헌팅그라운드 진입 시 퀘스트 연출 오케스트레이션.
    // 팝업 → HUD 출현 순서를 관리. CombatBootstrap이 HuntingGround 씬에서만 생성.
    //
    // **세션 1회 보장**: _introShown은 static — 씬 재로드 후 재진입해도 팝업을 다시 띄우지 않음.
    //   재진입 시 HUD만 즉시 reveal (팝업 생략).
    //
    // **한 개념 = 한 MonoBehaviour**: 팝업과 HUD를 별도 클래스로 두고 이 클래스가 순서만 조율.
    [DisallowMultipleComponent]
    public class QuestIntroSequencer : MonoBehaviour
    {
        static bool _introShown;

        // CombatBootstrap이 호출하는 진입점.
        // parent: CombatBootstrap의 transform (PopupGO를 씬에 붙임).
        public static void Run(Transform parent)
        {
            GameObject go = new GameObject("_QuestIntroSequencer");
            go.transform.SetParent(parent, worldPositionStays: false);
            QuestIntroSequencer seq = go.AddComponent<QuestIntroSequencer>();
            seq._parent = parent;
            seq.StartCoroutine(seq.RunSequence());
        }

        Transform? _parent;

        IEnumerator RunSequence()
        {
            // UI.unity additive 로드 레이스 — HUD가 준비될 때까지 대기.
            // 타임아웃 ~5s: 과도한 대기는 연출 생략으로 처리.
            float waited = 0f;
            while (QuestProgressHud.Instance == null)
            {
                waited += Time.deltaTime;
                if (waited > 5f)
                {
                    Debug.LogWarning("[QuestIntroSequencer] QuestProgressHud.Instance 5s 내 미등장 — 연출 생략.");
                    Destroy(gameObject);
                    yield break;
                }
                yield return null;
            }

            if (!_introShown)
            {
                _introShown = true;

                QuestGrantedPopup popup = QuestGrantedPopup.BuildRuntime(_parent!);
                bool done = false;
                popup.PlayThenCallback(() => done = true);

                while (!done) yield return null;

                Destroy(popup.gameObject);
            }

            // 팝업 완료(또는 재진입 생략) 후 HUD 출현.
            QuestProgressHud.Instance?.Reveal();
            Destroy(gameObject);
        }
    }
}
