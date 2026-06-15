#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 헌팅그라운드 진입 시 1회성 퀘스트 부여 연출 팝업.
    // BuildRuntime으로 생성 → PlayThenCallback으로 fade in/hold/fade out 재생 → onDone 콜백.
    //
    // **순수 연출**: 서버 상태를 변경하지 않음. 텍스트는 QuestProgressHud와 동일 콘텐츠 상수.
    [DisallowMultipleComponent]
    public class QuestGrantedPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup? _group;

        // QuestIntroSequencer가 PlayThenCallback 종료 후 Destroy 호출.
        public Coroutine PlayThenCallback(Action onDone)
        {
            return StartCoroutine(FadeRoutine(onDone));
        }

        IEnumerator FadeRoutine(Action onDone)
        {
            if (_group == null) { onDone(); yield break; }

            const float fadeIn  = 0.3f;
            const float hold    = 2.0f;
            const float fadeOut = 0.4f;

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(1f - t / fadeOut);
                yield return null;
            }
            _group.alpha = 0f;

            onDone();
        }

        // ============================================================
        // 런타임 빌드 (QuestIntroSequencer가 호출). ToastUI 패턴 동형.
        // sortingOrder 1150 — ToastUI(1100)보다 위, PartyInvitePopup(1200)보다 아래.
        // ============================================================
        public static QuestGrantedPopup BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("QuestGrantedPopup");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1150;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha          = 0f;
            group.interactable   = false;
            group.blocksRaycasts = false;

            // 화면 중앙 반투명 패널.
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRt.pivot            = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta        = new Vector2(520f, 180f);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);

            // 제목.
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, worldPositionStays: false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin        = new Vector2(0f, 0.55f);
            titleRt.anchorMax        = new Vector2(1f, 1f);
            titleRt.offsetMin        = new Vector2(16f, 0f);
            titleRt.offsetMax        = new Vector2(-16f, 0f);

            TMP_Text title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text      = "퀘스트 부여";
            title.fontSize  = 28f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color     = new Color(1f, 0.85f, 0.2f, 1f);

            // 본문.
            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panel.transform, worldPositionStays: false);
            RectTransform bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin  = new Vector2(0f, 0f);
            bodyRt.anchorMax  = new Vector2(1f, 0.55f);
            bodyRt.offsetMin  = new Vector2(16f, 0f);
            bodyRt.offsetMax  = new Vector2(-16f, 0f);

            TMP_Text body = bodyGo.AddComponent<TextMeshProUGUI>();
            // QuestProgressHud의 콘텐츠 상수와 동일 — 표시 연출용이므로 헌법 §1 위반 아님.
            body.text      = $"{QuestProgressHud.QuestName}\n{QuestProgressHud.QuestObjective}";
            body.fontSize  = 22f;
            body.alignment = TextAlignmentOptions.Center;
            body.color     = Color.white;

            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
            if (font != null) { title.font = font; body.font = font; }

            QuestGrantedPopup popup = root.AddComponent<QuestGrantedPopup>();

            var type  = typeof(QuestGrantedPopup);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            type.GetField("_group", flags)!.SetValue(popup, group);

            return popup;
        }
    }
}
