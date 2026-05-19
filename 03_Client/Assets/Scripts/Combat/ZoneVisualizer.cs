#nullable enable
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // M3 Phase 08b: 3-zone 시각 분할. *비주얼만* — 실제 zone 로직(서버 검사)은 M4+.
    //
    // **응급 단순화** (정의 약속):
    //   - 좌 마을 (x ≈ -10 ~ 5): 푸른 배경 + "마을" 표지판
    //   - 중 전투 (x ≈ 5 ~ 20): 갈색 배경 + "전투구역" 표지판 (enemy spawn x=10 포함)
    //   - 우 보스 (x ≈ 20 ~ 40): 어두운 자주 + "보스방" 표지판 (boss spawn x=30 포함)
    //
    // **런타임 코드 생성** (씬 YAML 편집 회피, 정유현 씬 영역 격리):
    //   각 zone = SpriteRenderer(white square × tint color × scale) + TMP_Text 표지판.
    //   sortingOrder = -10 (Player/Enemy(1~4)보다 아래, 진짜 *배경* 레이어).
    //
    // **면담 어필 포인트**: 단일 맵 안 zone 트릭 → 4맵 분리 인프라 부담 회피. M4에서 진짜 4맵.
    [DisallowMultipleComponent]
    public class ZoneVisualizer : MonoBehaviour
    {
        // 공유 가능한 1x1 white sprite — EnemyRegistry에도 같은 게 있지만 어셈블리 의존 피하려고 자체 보유.
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

        void Awake()
        {
            BuildZone("Zone_Village", centerX: -2.5f, width: 15f, height: 20f,
                      tint: new Color(0.35f, 0.55f, 0.7f, 0.55f), label: "마을", labelY: 5f);
            BuildZone("Zone_Combat", centerX: 12.5f, width: 15f, height: 20f,
                      tint: new Color(0.6f, 0.45f, 0.25f, 0.55f), label: "전투구역", labelY: 5f);
            BuildZone("Zone_Boss", centerX: 30f, width: 20f, height: 20f,
                      tint: new Color(0.45f, 0.15f, 0.4f, 0.6f), label: "보스방", labelY: 5f);
        }

        void BuildZone(string name, float centerX, float width, float height, Color tint, string label, float labelY)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(transform, worldPositionStays: false);
            zone.transform.position = new Vector3(centerX, 0f, 0f);
            zone.transform.localScale = new Vector3(width, height, 1f);

            SpriteRenderer sr = zone.AddComponent<SpriteRenderer>();
            sr.sprite = GetWhiteSquare();
            sr.color = tint;
            sr.sortingOrder = -10; // 진짜 배경 — Player(1) / Enemy(2) / HpBar(3~4) 모두 아래.

            // 표지판 (World Space TextMeshPro). zone 자식이지만 scale 영향 X 위해 localScale 보정.
            GameObject sign = new GameObject($"{name}_Sign");
            sign.transform.SetParent(zone.transform, worldPositionStays: false);
            // zone 자체 scale=(w,h,1)이라 자식 localPosition (0, labelY/h, 0)이면 world y=labelY.
            sign.transform.localPosition = new Vector3(0f, labelY / height, 0f);
            sign.transform.localScale = new Vector3(1f / width, 1f / height, 1f);

            TextMeshPro tmp = sign.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.95f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingOrder = -9; // 배경(-10)보다 위, 캐릭터(1+) 아래.

            // RectTransform sizing — TMP가 world space일 땐 sizeDelta로 텍스트 영역.
            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(10f, 3f);
        }
    }
}
