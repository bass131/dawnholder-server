#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Lays the MainMenu scene out as the login screen: the title banner above, a bright login window below. It moves the
    /// scene's own fields, buttons, error text and banner, so their serialized wiring and onClick calls stay as they are.
    /// No login decision lives here: Enter in either field asks for the same start the start button calls. The view
    /// lives as long as the scene and owns only the frame of its last Enter.
    /// </summary>
    internal sealed class MainMenuLoginView
    {
        // The character select window's colours (CharacterMenuView): dark ink on parchment, white on wood and the title band.
        static readonly Color s_ink = new(0.26f, 0.17f, 0.09f);
        static readonly Color s_secondaryText = new(0.45f, 0.33f, 0.2f);
        static readonly Color s_resultText = new(0.6f, 0.25f, 0.08f);
        static readonly Vector2 s_topLeft = new(0f, 1f);
        readonly TMP_InputField _account;
        readonly TMP_InputField _host;
        readonly Action _startRequested;
        int _lastEnterFrame = -1;

        internal MainMenuLoginView(TMP_InputField account, TMP_InputField host, Button start, TMP_Text error,
            Action startRequested)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (error == null) throw new ArgumentNullException(nameof(error));
            _account = account;
            _host = host;
            _startRequested = startRequested ?? throw new ArgumentNullException(nameof(startRequested));
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) throw new InvalidOperationException("Missing project menu font.");
            UiWindowSkin skin = UiWindowAppearance.BrightSkin;

            // The scene canvas already draws the full background, so it becomes the login screen instead of a new canvas.
            // Its scaler follows the screen height at the character select's 800×450 reference: the banner and the window
            // are stacked by height, so they stay apart at any width.
            var sceneCanvas = start.GetComponentInParent<Canvas>();
            if (sceneCanvas == null) throw new InvalidOperationException("The MainMenu start button is not on a canvas.");
            Transform screen = sceneCanvas.rootCanvas.transform;
            var scaler = screen.GetComponent<CanvasScaler>();
            if (scaler == null) throw new InvalidOperationException($"MainMenu canvas {screen.name} has no CanvasScaler.");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 450f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // The banner keeps its Image and sprite animation; only its place changes, at its frames' 1024×424 shape.
            Place((RectTransform)SceneChild(screen, "TITLE"), new Vector2(0.5f, 1f), new Vector2(444f, 184f),
                new Vector2(0f, -12f));
            RectTransform window = UiWindowAppearance.CreatePanel("LoginWindow", screen, raycastTarget: true, skin: skin)
                .rectTransform;
            Place(window, new Vector2(0.5f, 0f), new Vector2(420f, 230f), new Vector2(0f, 16f));

            // Heights reserve the Korean fallback font's line box (about 1.9 em): under Ellipsis TMP drops a line that
            // does not fit instead of clipping it.
            Place(UiWindowAppearance.CreateHeader("Header", window, skin).rectTransform, s_topLeft,
                new Vector2(396f, 44f), new Vector2(12f, -8f));
            TMP_Text title = Label("Title", window, new Vector2(396f, 44f), new Vector2(12f, -8f), font);
            title.text = "로그인";
            title.fontSize = 20f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Midline;

            TMP_Text accountLabel = Label("AccountLabel", window, new Vector2(100f, 34f), new Vector2(24f, -60f), font);
            accountLabel.text = "계정 이름";
            accountLabel.fontSize = 16f;
            accountLabel.color = s_ink;
            MoveField(account, window, new Vector2(268f, 34f), new Vector2(128f, -60f), 16f, skin);
            // The server address is the minor row under the account name: smaller words and a lower field.
            TMP_Text hostLabel = Label("HostLabel", window, new Vector2(100f, 30f), new Vector2(24f, -100f), font);
            hostLabel.text = "서버 주소";
            hostLabel.fontSize = 14f;
            hostLabel.color = s_secondaryText;
            MoveField(host, window, new Vector2(268f, 30f), new Vector2(128f, -100f), 14f, skin);

            // The error text keeps the scene's font and overflow: MainMenuNotice sentences are written for that font's
            // static atlas and every character must stay drawn. 14 pt keeps the longest sentence on one line.
            Move(error.rectTransform, window, new Vector2(380f, 32f), new Vector2(20f, -136f));
            error.fontSize = 14f;
            error.color = s_resultText;
            // The dark box the error text sat in is left in the scene, switched off.
            SceneChild(screen, "ErrorMessage").gameObject.SetActive(false);

            var quit = SceneChild(screen, "QuitButton").GetComponent<Button>();
            if (quit == null) throw new InvalidOperationException("MainMenu QuitButton has no Button.");
            MoveButton(quit, window, new Vector2(120f, 40f), new Vector2(24f, -176f), "종료", font, skin, accept: false);
            MoveButton(start, window, new Vector2(144f, 40f), new Vector2(252f, -176f), "게임 시작", font, skin,
                accept: true);

            // Only the fields submit to the view. A selected start button still takes Enter as its own single click.
            account.onSubmit.AddListener(_ => OnEnter());
            host.onSubmit.AddListener(_ => OnEnter());
        }

        /// <summary>Call once per frame. Tab in the account field selects the server address field.</summary>
        internal void HandleKeys()
        {
            // The scene's UI input actions have no Tab move, so the keyboard is read here.
            Keyboard keyboard = Keyboard.current;
            EventSystem events = EventSystem.current;
            if (keyboard == null || events == null || !keyboard.tabKey.wasPressedThisFrame) return;
            if (events.currentSelectedGameObject == _account.gameObject) events.SetSelectedGameObject(_host.gameObject);
        }

        // A focused field can submit twice for one Enter in a player: its own key event and the UI module's Submit, in
        // the same frame. The second one is the same key press, not another start.
        void OnEnter()
        {
            if (Time.frameCount == _lastEnterFrame) return;
            _lastEnterFrame = Time.frameCount;
            _startRequested();
        }

        static Transform SceneChild(Transform screen, string name)
        {
            Transform child = screen.Find(name);
            if (child == null) throw new InvalidOperationException($"MainMenu has no {name} under {screen.name}.");
            return child;
        }

        static void MoveField(TMP_InputField field, Transform window, Vector2 size, Vector2 position, float fontSize,
            UiWindowSkin skin)
        {
            Move((RectTransform)field.transform, window, size, position);
            // The field's own Image is its background and target graphic, as on the character name input.
            var background = field.GetComponent<Image>();
            if (background == null) throw new InvalidOperationException($"MainMenu field {field.name} has no Image.");
            background.sprite = skin.InputField;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1f;
            background.color = Color.white;
            // Typed text stays clear of the frame; the scene's own font, which has the account and address characters, stays.
            field.textViewport.offsetMin = new Vector2(10f, 0f);
            field.textViewport.offsetMax = new Vector2(-10f, 0f);
            field.textComponent.enableAutoSizing = false;
            field.pointSize = fontSize;
            field.textComponent.color = s_ink;
            field.textComponent.alignment = TextAlignmentOptions.Left;
        }

        // UiWindowAppearance.CreateButton makes new buttons; these are the scene's own, so the kit's pieces go on them here.
        static void MoveButton(Button button, Transform window, Vector2 size, Vector2 position, string caption,
            TMP_FontAsset font, UiWindowSkin skin, bool accept)
        {
            Move((RectTransform)button.transform, window, size, position);
            Button template = skin.ButtonPrefab;
            var templateImage = (Image)template.targetGraphic;
            var image = button.targetGraphic as Image;
            if (image == null) throw new InvalidOperationException($"MainMenu {button.name} has no Image to draw it.");
            image.sprite = accept ? skin.AcceptNormal : templateImage.sprite;
            image.type = templateImage.type;
            image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
            image.preserveAspect = false;
            image.color = Color.white;
            button.transition = template.transition;
            button.spriteState = accept ? skin.AcceptStates : template.spriteState;
            TMP_Text label = Label("Label", button.transform, size, Vector2.zero, font);
            label.text = caption;
            label.fontSize = 16f;
            // Midline centres the drawn glyphs; Center would centre the fallback's tall line box and sit Korean low.
            label.alignment = TextAlignmentOptions.Midline;
        }

        static void Move(RectTransform rect, Transform window, Vector2 size, Vector2 position)
        {
            rect.SetParent(window, false);
            Place(rect, s_topLeft, size, position);
        }

        // Anchors and pivot on one point, so the position is that point's offset: the window's top-left for its contents.
        static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, s_topLeft, size, position);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 18f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }
    }
}
