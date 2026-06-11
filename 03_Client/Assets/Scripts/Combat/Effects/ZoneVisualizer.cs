#nullable enable
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 3-zone 시각 분할. *비주얼만* — 실제 zone 로직(서버 검사)은 별도.
    //   - 좌 마을: 푸른 배경 + "VILLAGE" 표지판
    //   - 중 전투: 갈색 배경 + "COMBAT" 표지판 (enemy spawn x=10 포함)
    //   - 우 보스: 어두운 자주 + "BOSS" 표지판 (boss spawn x=30 포함)
    //
    // 각 zone = SpriteRenderer(white square × tint color × scale) + TMP_Text 표지판.
    //   sortingOrder = -10 (Player/Enemy(1~4)보다 아래, 진짜 *배경* 레이어).
    [DisallowMultipleComponent]
    public class ZoneVisualizer : MonoBehaviour
    {
        // 공유 가능한 1x1 white sprite — Fantasy Forest sprite load 실패 시 fallback.
        Sprite? _whiteSquare;
        Sprite GetWhiteSquare()
        {
            if (_whiteSquare != null) return _whiteSquare;
            Texture2D tex = new Texture2D(2, 2);
            Color[] pixels = new Color[4];
            for (int i = 0; i < 4; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return _whiteSquare;
        }

        // Sprite는 Editor only (AssetDatabase), Font는 Resources.Load (런타임 동작).
        // Sprite null이면 흰 사각형 + tint fallback.
        Sprite? _villageBg;
        Sprite? _combatBg;
        Sprite? _bossBg;
        TMP_FontAsset? _fontAsset;

        void Awake()
        {
            LoadAssets();
            // 표지판은 영문 — LiberationSans SDF에 한글 글리프 없음 (Pretendard SDF 생성은 M4 후속).
            BuildZone("Zone_Village", centerX: -2.5f, width: 15f, height: 20f, bgSprite: _villageBg,
                      tint: new Color(0.7f, 0.85f, 1f, 1f), label: "VILLAGE", labelY: 5f);
            BuildZone("Zone_Combat", centerX: 12.5f, width: 15f, height: 20f, bgSprite: _combatBg,
                      tint: new Color(1f, 0.9f, 0.7f, 1f), label: "COMBAT", labelY: 5f);
            BuildZone("Zone_Boss", centerX: 30f, width: 20f, height: 20f, bgSprite: _bossBg,
                      tint: new Color(0.9f, 0.7f, 1f, 1f), label: "BOSS", labelY: 5f);
        }

        void LoadAssets()
        {
            // Font asset — Resources.Load fallback 실패 시 AssetDatabase (Editor only).
            _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (_fontAsset == null)
            {
                _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
            _villageBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Environment/FREE_Fantasy Forest/Backgrounds/Sky.png");
            _combatBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Environment/FREE_Fantasy Forest/Backgrounds/Grass Mountains.png");
            _bossBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Environment/FREE_Fantasy Forest/Backgrounds/Rock Mountains.png");
#endif
        }

        void BuildZone(string name, float centerX, float width, float height, Sprite? bgSprite, Color tint, string label, float labelY)
        {
            // 컨테이너 (scale=1, position만) + 자식 SpriteRenderer/Sign 구조 — sprite native size 따라 자식 scale 정규화.
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(transform, worldPositionStays: false);
            zone.transform.position = new Vector3(centerX, 0f, 0f);

            // 배경 자식
            GameObject bgGo = new GameObject($"{name}_Bg");
            bgGo.transform.SetParent(zone.transform, worldPositionStays: false);
            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            if (bgSprite != null)
            {
                sr.sprite = bgSprite;
                sr.color = tint;
                Vector2 spriteSize = bgSprite.bounds.size;
                if (spriteSize.x > 0.01f && spriteSize.y > 0.01f)
                {
                    bgGo.transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
                }
                else
                {
                    bgGo.transform.localScale = new Vector3(width, height, 1f);
                }
            }
            else
            {
                sr.sprite = GetWhiteSquare();
                sr.color = new Color(tint.r * 0.6f, tint.g * 0.6f, tint.b * 0.6f, 0.55f);
                bgGo.transform.localScale = new Vector3(width, height, 1f);
            }
            sr.sortingOrder = -10;

            // 표지판 자식 (zone scale=1이라 직접 좌표)
            GameObject sign = new GameObject($"{name}_Sign");
            sign.transform.SetParent(zone.transform, worldPositionStays: false);
            sign.transform.localPosition = new Vector3(0f, labelY, 0f);

            TextMeshPro tmp = sign.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.95f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingOrder = -9;
            if (_fontAsset != null) tmp.font = _fontAsset;

            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(10f, 3f);
        }
    }
}
