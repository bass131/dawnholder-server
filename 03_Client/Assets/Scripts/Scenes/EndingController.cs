#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Dawnholder.Client.UI;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 엔딩 화면 컨트롤러. 보스 처치 후 종료점.
    ///
    /// 흐름: 진입 후 <see cref="_armDelay"/>초간 입력 잠금(연출 감상) → 이후 아무 키/마우스/패드
    /// 입력 시 MainMenu Scene 로드. 기존 "메인으로" 버튼도 호환(<see cref="OnMainClicked"/>).
    ///
    /// 헌법 #1 (Server Authority): 본 Scene은 단순 UI 흐름, 네트워크 X.
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] float _armDelay = 1f;   // 입력 받기 전 잠금 시간
        [SerializeField] Image? _background;      // 비우면 런타임 "BackGround" 탐색
        [SerializeField] float _bgFps = 10f;      // 배경 시트 재생 속도

        // 슬라이스된 엔딩 배경 시트(없으면 정적 배경 유지).
        const string BackgroundFramesKey = "UI/Ending";

        bool  _armed;
        bool  _exiting;
        float _t;
        float _hintClock;
        CanvasGroup? _hintGroup;

        void Awake() => TrySetupBackground();

        // 엔딩 배경을 스프라이트 시트 애니로 구동 — 시트 미준비 시 정적 배경 유지(graceful).
        // 씬 편집 회피: BackGround Image를 런타임 탐색해 사이클러를 부착.
        void TrySetupBackground()
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(BackgroundFramesKey);
            if (frames == null || frames.Length <= 1) return; // 시트 미준비 → 정적 유지
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));

            Image? bg = _background;
            if (bg == null)
            {
                GameObject? bgGo = GameObject.Find("BackGround");
                if (bgGo != null) bg = bgGo.GetComponent<Image>();
            }
            if (bg == null) { Debug.LogWarning("[Ending] BackGround Image 못 찾음 — 배경 애니 생략."); return; }

            var anim = bg.gameObject.GetComponent<EndingBackgroundAnimator>();
            if (anim == null) anim = bg.gameObject.AddComponent<EndingBackgroundAnimator>();
            anim.Play(bg, frames, _bgFps);
        }

        void Update()
        {
            if (_exiting) return;

            if (!_armed)
            {
                _t += Time.deltaTime;
                if (_t >= _armDelay) { _armed = true; BuildHint(); }
                return;
            }

            // "Press any key" 힌트 숨쉬기(부드러운 페이드 펄스).
            if (_hintGroup != null)
            {
                _hintClock += Time.deltaTime;
                _hintGroup.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_hintClock * 2.2f));
            }

            if (AnyInputDown()) Exit();
        }

        // 새 Input System — 키보드/마우스/패드 down-edge. Keyboard.current null 가드(키보드 미연결 환경).
        static bool AnyInputDown()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
            return false;
        }

        // 기존 버튼 호환 진입점.
        public void OnMainClicked() => Exit();

        void Exit()
        {
            if (_exiting) return;
            _exiting = true;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("MainMenu");
            }
            else
            {
                // Fallback: Ending Scene 단독 Editor Play 시 SceneTransition Singleton 미박힘 가능.
                Debug.LogWarning("[Ending] SceneTransition.Instance is null — direct LoadScene fallback");
                SceneManager.LoadScene("MainMenu");
            }
        }

        // "Press any key to continue" 힌트 — 런타임 생성(씬 편집 불필요).
        // 영문 사용: 엔딩 이미지가 영문 톤 + LiberationSans SDF에 한글 글리프 부재.
        void BuildHint()
        {
            GameObject root = new GameObject("EndingHint");
            root.transform.SetParent(transform, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            _hintGroup = root.AddComponent<CanvasGroup>();
            _hintGroup.alpha          = 0f;
            _hintGroup.interactable   = false;
            _hintGroup.blocksRaycasts = false;

            GameObject textGo = new GameObject("HintText");
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 70f); // 하단 중앙
            rt.sizeDelta        = new Vector2(720f, 80f);

            TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
            text.text      = "Press any key to continue";
            text.fontSize  = 36f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color     = new Color(1f, 1f, 1f, 1f);

            // ⚠️ 폰트(머티리얼)를 outline보다 *먼저* 할당 (QuestAlert 6R 교훈: 머티리얼 null이면 예외).
            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
#endif
            if (font != null) text.font = font;
            if (text.fontSharedMaterial != null)
            {
                text.outlineWidth = 0.2f;
                text.outlineColor = new Color32(0, 0, 0, 255);
            }
        }
    }
}
