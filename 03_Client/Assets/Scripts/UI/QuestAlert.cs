#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 어떤 배너를 띄울지 — 호출부 가독성/타입 안전용.
    public enum QuestAlertKind { Available, Clear }

    // 퀘스트 임팩트 연출 — 중앙 배너가 오버슈트 확대로 쾅! 등장하며 스프라이트 시트 애니(반짝이/샤인) 재생 → 페이드.
    // "퀘스트 발생"(QuestIntroSequencer) / "퀘스트 완료"(QuestCompleteWatcher) 양쪽이 같은 연출로 재사용.
    //
    // 프레임 = Resources/UI/Quest{Available,Clear}.png (4×4=16, 영호 AI Generator 시트 → 크로마키 후 슬라이스).
    //
    // **순수 연출**: 서버 상태를 변경하지 않음. Time.deltaTime 기반 — 연출이라 tick 무관.
    // **안전**: 프레임 로드 실패해도 TMP 텍스트 폴백으로 강등 — 코루틴이 죽지 않아 후속(HUD reveal) 경로 보존.
    [DisallowMultipleComponent]
    public class QuestAlert : MonoBehaviour
    {
        CanvasGroup   _group  = null!;
        RectTransform _rt     = null!; // 배너(또는 폴백 텍스트)의 RectTransform
        Image?        _flash;
        Image?        _banner; // 폴백이면 null
        Sprite[]?     _frames; // 폴백이면 null

        public Coroutine PlayThenCallback(Action onDone) => StartCoroutine(PlayRoutine(onDone));

        IEnumerator PlayRoutine(Action onDone)
        {
            const float scaleIn = 0.28f; // 오버슈트 확대 등장
            const float hold    = 0.95f; // 시트 한 바퀴 이상 보이도록
            const float fadeOut = 0.34f;
            const float fps     = 14f;   // 시트 재생 속도

            _group.alpha = 1f;
            float clock = 0f;

            float t = 0f;
            while (t < scaleIn)
            {
                float dt = Time.deltaTime; t += dt; clock += dt;
                float k = Mathf.Clamp01(t / scaleIn);
                _rt.localScale = Vector3.one * EaseOutBack(k);
                if (_flash != null) _flash.color = new Color(1f, 0.9f, 0.45f, 0.30f * (1f - k)); // 골드 플래시 빠르게 소멸
                Frame(clock, fps);
                yield return null;
            }
            _rt.localScale = Vector3.one;
            if (_flash != null) _flash.color = new Color(1f, 0.9f, 0.45f, 0f);

            float h = 0f;
            while (h < hold)
            {
                float dt = Time.deltaTime; h += dt; clock += dt;
                Frame(clock, fps);
                yield return null;
            }

            t = 0f;
            while (t < fadeOut)
            {
                float dt = Time.deltaTime; t += dt; clock += dt;
                float k = Mathf.Clamp01(t / fadeOut);
                _group.alpha   = 1f - k;
                _rt.localScale = Vector3.one * (1f + 0.10f * k); // 살짝 더 커지며 사라짐
                Frame(clock, fps);
                yield return null;
            }
            _group.alpha = 0f;
            onDone();
        }

        // 현재 클록에 해당하는 시트 프레임 표시 (폴백/미로드 시 무시).
        void Frame(float clock, float fps)
        {
            if (_banner == null || _frames == null || _frames.Length == 0) return;
            _banner.sprite = _frames[(int)(clock * fps) % _frames.Length];
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
        public static QuestAlert BuildRuntime(Transform parent, QuestAlertKind kind)
        {
            string resKey      = kind == QuestAlertKind.Clear ? "UI/QuestClear" : "UI/QuestAvailable";
            string fallbackMsg = kind == QuestAlertKind.Clear ? "퀘스트 완료!"   : "퀘스트 발생!";

            Sprite[] frames = Resources.LoadAll<Sprite>(resKey);
            if (frames != null && frames.Length > 1)
                Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name)); // _00.._15 순서

            GameObject root = new GameObject("QuestAlert");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1180;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha          = 0f;
            group.interactable   = false;
            group.blocksRaycasts = false; // 연출이 클릭 막지 않게

            // 전체화면 골드 플래시 (배너보다 뒤 = 먼저 추가).
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

            RectTransform rt;
            Image? banner = null;

            if (frames != null && frames.Length > 0)
            {
                // ── 배너 Image (스프라이트 시트 프레임) ──
                GameObject bannerGo = new GameObject("Banner");
                bannerGo.transform.SetParent(root.transform, worldPositionStays: false);
                rt = bannerGo.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 80f); // 화면 중앙보다 살짝 위 (튜닝 지점)
                rt.sizeDelta        = new Vector2(760f, 380f); // 시트 2:1 비율, preserveAspect로 왜곡 방지
                rt.localScale       = Vector3.zero;

                banner = bannerGo.AddComponent<Image>();
                banner.sprite         = frames[0];
                banner.preserveAspect = true;
                banner.raycastTarget  = false;
            }
            else
            {
                // ── 폴백: TMP 텍스트 (프레임 미로드 시) ──
                Debug.LogWarning($"[QuestAlert] frames '{resKey}' 로드 실패 — 텍스트 폴백.");
                GameObject textGo = new GameObject("AlertText");
                textGo.transform.SetParent(root.transform, worldPositionStays: false);
                rt = textGo.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 120f);
                rt.sizeDelta        = new Vector2(900f, 160f);
                rt.localScale       = Vector3.zero;

                TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
                text.text      = fallbackMsg;
                text.fontSize  = 80f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color     = new Color(1f, 0.88f, 0.25f, 1f);

                // ⚠️ 폰트(머티리얼)를 outline보다 *먼저* 할당 (없으면 ArgumentNullException으로 코루틴 사망).
                TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
                if (font == null)
                    font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
#endif
                if (font != null) text.font = font;
                if (text.fontSharedMaterial != null)
                {
                    text.outlineWidth = 0.22f;
                    text.outlineColor = new Color32(40, 20, 0, 255);
                }
            }

            QuestAlert alert = root.AddComponent<QuestAlert>();
            alert._group  = group;
            alert._rt     = rt;
            alert._flash  = flash;
            alert._banner = banner;
            alert._frames = banner != null ? frames : null;
            return alert;
        }
    }
}
