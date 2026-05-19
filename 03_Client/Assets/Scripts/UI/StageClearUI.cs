#nullable enable
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // M3 Phase 08b: Stage Clear UI. S_StageClear 수신 → 화면 중앙 "Stage Clear!" 표시.
    //
    // **헌법 #1 (Server Authority)**: 본 UI는 *서버 신호를 표시만* 합니다. 보스 사망 판정 / 진행도
    // 산정 *클라가 절대 하지 않음*. UnityClientSession.HandleStageClear가 본 컴포넌트 Show 호출.
    //
    // **런타임 자동 생성**:
    //   응급 약속에 따라 prefab/씬 YAML 편집 없이 코드로 Canvas+Text 만듦. 정유현 UI 영역
    //   (UI.unity 씬, FadeCanvas.prefab 등)과 격리 — 씬 conflict 차단.
    //   미래 UI 정리 단계에서 UI.unity 씬 안 prefab으로 마이그레이션 권장.
    //
    // **싱글톤 사용 이유**: UnityClientSession이 정적 접근으로 Show 호출. Instance가 없으면
    // (씬 진입 race) 큐 없이 silent drop — 응급 단순화. 정상 흐름엔 CombatBootstrap이 씬 진입
    // 직후 박아서 race window 거의 0.
    [DisallowMultipleComponent]
    public class StageClearUI : MonoBehaviour
    {
        public static StageClearUI? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text? _text;

        Coroutine? _activeFade;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[StageClearUI] 중복 박힘 — 씬에 여러 인스턴스. 본인 셋업 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_group != null) _group.alpha = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // UnityClientSession.HandleStageClear 경로 (main thread).
        // bossEntityId는 로깅용 — UI에는 단순히 텍스트만 표시.
        public void Show(int bossEntityId)
        {
            Debug.Log($"[StageClearUI] Stage Clear! (boss entity {bossEntityId})");
            if (_group == null || _text == null)
            {
                Debug.LogWarning("[StageClearUI] _group/_text null — Canvas/Text wire 누락.");
                return;
            }
            _text.text = "🎉 Stage Clear!";
            if (_activeFade != null) StopCoroutine(_activeFade);
            _activeFade = StartCoroutine(FadeRoutine());
        }

        // 응급 페이드: 0 → 1 (0.4s) → stay 5s → 1 (manual close — 면담 종료 시 Stop)
        // Phase 정의 함정 "alpha 1로 박힌 채 다음 stage 가면 데모 깨짐"은 *데모 시연 중엔* 의도된 동작
        // (clear가 마지막 비주얼). 다음 Stage는 M4+에서 reset 절차 박을 때 다룸.
        IEnumerator FadeRoutine()
        {
            if (_group == null) yield break;
            const float fadeIn = 0.4f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;
        }

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출).
        // Canvas (Screen Space - Overlay) + 중앙 TMP_Text.
        // ============================================================
        public static StageClearUI BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("StageClearUI");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 다른 UI보다 위

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 200f);

            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Stage Clear!";
            tmp.fontSize = 96f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.92f, 0.2f, 1f);
            tmp.fontStyle = FontStyles.Bold;

            StageClearUI ui = root.AddComponent<StageClearUI>();
            // SerializeField 직접 접근 — 런타임 셋업이라 안전 (private이라 외부 X).
            typeof(StageClearUI).GetField("_group",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                !.SetValue(ui, group);
            typeof(StageClearUI).GetField("_text",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                !.SetValue(ui, tmp);

            return ui;
        }
    }
}
