#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 퀘스트 임팩트 연출 — 중앙 텍스트가 오버슈트 확대로 쾅! 등장 + 화면 플래시 → 페이드.
    // "퀘스트 발생!"(QuestIntroSequencer) / "퀘스트 완료!"(QuestCompleteWatcher) 양쪽이 같은 연출로 재사용.
    //
    // **순수 연출**: 서버 상태를 변경하지 않음. Time.deltaTime 기반 — 연출이라 tick 무관.
    [DisallowMultipleComponent]
    public class QuestAlert : MonoBehaviour
    {
        CanvasGroup   _group  = null!;
        RectTransform _textRt = null!;
        Image         _flash  = null!;

        public Coroutine PlayThenCallback(Action onDone) => StartCoroutine(PlayRoutine(onDone));

        IEnumerator PlayRoutine(Action onDone)
        {
            const float scaleIn = 0.28f; // 오버슈트 확대 등장
            const float hold    = 0.55f;
            const float fadeOut = 0.32f;

            _group.alpha = 1f;

            float t = 0f;
            while (t < scaleIn)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / scaleIn);
                _textRt.localScale = Vector3.one * EaseOutBack(k);
                _flash.color = new Color(1f, 0.9f, 0.45f, 0.38f * (1f - k)); // 골드 플래시 빠르게 소멸
                yield return null;
            }
            _textRt.localScale = Vector3.one;
            _flash.color = new Color(1f, 0.9f, 0.45f, 0f);

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeOut);
                _group.alpha = 1f - k;
                _textRt.localScale = Vector3.one * (1f + 0.12f * k); // 살짝 더 커지며 사라짐
                yield return null;
            }
            _group.alpha = 0f;
            onDone();
        }

        // 끝에서 1.0을 살짝 넘었다 수렴 — "통! 튀는" 임팩트. c1↑일수록 오버슈트 강함.
        static float EaseOutBack(float x)
        {
            const float c1 = 2.4f;
            const float c3 = c1 + 1f;
            float xm1 = x - 1f;
            return 1f + c3 * xm1 * xm1 * xm1 + c1 * xm1 * xm1;
        }

        // ============================================================
        // 런타임 빌드. sortingOrder 1180 — 상세 팝업(1150)보다 위(시간상 겹치진 않음).
        // ============================================================
        public static QuestAlert BuildRuntime(Transform parent, string message)
        {
            GameObject root = new GameObject("QuestAlert");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1180;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha          = 0f;
            group.interactable   = false;
            group.blocksRaycasts = false; // 연출이 클릭 막지 않게

            // 전체화면 골드 플래시 (텍스트보다 뒤 = 먼저 추가).
            GameObject flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform flashRt = flashGo.AddComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            Image flash = flashGo.AddComponent<Image>();
            flash.color = new Color(1f, 0.9f, 0.45f, 0f);
            flash.raycastTarget = false;

            // 중앙 임팩트 텍스트.
            GameObject textGo = new GameObject("AlertText");
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin        = new Vector2(0.5f, 0.5f);
            textRt.anchorMax        = new Vector2(0.5f, 0.5f);
            textRt.pivot            = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = new Vector2(0f, 120f); // 상세 팝업보다 살짝 위
            textRt.sizeDelta        = new Vector2(900f, 160f);
            textRt.localScale       = Vector3.zero;

            TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
            text.text      = message;
            text.fontSize  = 80f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color     = new Color(1f, 0.88f, 0.25f, 1f);

            // ⚠️ 폰트(머티리얼)를 outline보다 *먼저* 할당. outlineWidth는 폰트 머티리얼 인스턴스를
            //    만드는데, 머티리얼이 없으면 ArgumentNullException으로 빌드 코루틴이 통째로 죽는다.
            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
#endif
            if (font != null) text.font = font;

            // 머티리얼 준비된 경우에만 외곽선(가독성). 없으면 안전 생략.
            if (text.fontSharedMaterial != null)
            {
                text.outlineWidth = 0.22f;
                text.outlineColor = new Color32(40, 20, 0, 255);
            }

            QuestAlert alert = root.AddComponent<QuestAlert>();
            alert._group  = group;
            alert._textRt = textRt;
            alert._flash  = flash;
            return alert;
        }
    }
}
