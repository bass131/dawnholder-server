#nullable enable
using System.Collections;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Prediction;
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

                // 플레이어가 실제 월드에 등장 + 화면 안정화까지 대기 — 콜드 로드 중 연출이 묻히던 문제 방지.
                float pw = 0f;
                while (LocalPlayerMovement.Instance == null && pw < 3f) { pw += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.4f);

                // ① "퀘스트 발생" 임팩트 — 배너 시트 애니 쾅! 등장 + 플래시
                AudioManager.Instance?.PlaySfx(SoundKeys.Toast);
                QuestAlert alert = QuestAlert.BuildRuntime(_parent!, QuestAlertKind.Available);
                bool alertDone = false;
                alert.PlayThenCallback(() => alertDone = true);
                while (!alertDone) yield return null;
                Destroy(alert.gameObject);

                // ② 상세 부여 팝업 (Fade in/out)
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
