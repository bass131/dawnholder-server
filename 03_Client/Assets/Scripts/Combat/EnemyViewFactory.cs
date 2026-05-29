#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // M4.3R Phase 05 (rank 3): EnemyRegistry에서 추출한 GameObject 빌더 담당 정적 클래스.
    //
    // **추출 이유 (§3.1)**:
    //   EnemyRegistry는 dict 관리(Spawn/Despawn/ApplyHit/TryGetNearest)가 책임이고,
    //   GameObject·SpriteRenderer·HP bar를 코드로 조립하는 것은 별개의 "뷰 빌더" 관심사.
    //   dict만 보고 싶은 사람이 76줄 빌더를 읽지 않아도 되게 분리.
    //
    // **미래 prefab 교체 약속 보존 (Phase 05 스펙)**:
    //   EnemyRegistry의 _normalPrefab/_bossPrefab SerializeField 주석이 가리키는
    //   '코드 빌더 → prefab 교체' 경로를 막지 않도록 정적 public 메서드 시그니처 유지.
    //   prefab 교체 시 EnemyRegistry.Spawn에서 BuildPlaceholder 대신 Instantiate 분기로 전환.
    //
    // **런타임 placeholder 약속 (M3 Phase 08b/08c hardening — 5/20)**:
    //   Normal=Mushroom sprite, Boss=ToxicFrog sprite + HP bar 자식.
    //   Sprite asset 없으면 흰 사각형 + tint fallback.
    //   M4에서 Resources/Addressables 마이그레이션 예정.
    public static class EnemyViewFactory
    {
        // === static sprite 캐시 ===

        static Sprite? _whiteSquare;
        static Sprite? _mushroomSprite;
        static Sprite? _toxicFrogSprite;
        static bool _spritesLoaded;

        static Sprite GetWhiteSquare()
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

        // Editor 전용 sprite asset 로더. Multiple-mode sliced sprite면 첫 sub-sprite 반환.
        // Build 빌드에선 null 반환 → 호출 측에서 _whiteSquare로 fallback (헌법 #1 영향 X, 시각만).
        static void TryLoadEnemySprites()
        {
            if (_spritesLoaded) return;
            _spritesLoaded = true;
#if UNITY_EDITOR
            _mushroomSprite = LoadFirstSpriteAt("Assets/Art/Enemy/Forest_Monsters_FREE/Mushroom/Mushroom without VFX/Mushroom-Idle.png");
            // BlueBlue 색만 큰 sprite (1.5x1.5) — 나머지 색은 0.23×0.18로 매우 작음. M3 hardening 5/20.
            _toxicFrogSprite = LoadFirstSpriteAt("Assets/Art/Enemy/ToxicFrog/BlueBlue/ToxicFrogBlueBlue_Idle.png");
#endif
        }

#if UNITY_EDITOR
        static Sprite? LoadFirstSpriteAt(string path)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets) if (a is Sprite sp) return sp;
            return null;
        }
#endif

        // === 공개 팩토리 메서드 ===

        // 런타임 placeholder GameObject 생성. Normal=Mushroom sprite, Boss=ToxicFrog sprite + HP bar 자식.
        // M3 Phase 08b/08c hardening (5/20): 응급 시연 시각 강화 — placeholder 색 박스 → 실제 sprite asset.
        // Sprite는 Editor에서 AssetDatabase로 load (Editor only — 응급 시연 영역).
        // Sprite asset 없으면 옛 placeholder(흰 사각형 + tint)로 fallback (안전).
        public static GameObject BuildPlaceholder(int entityId, RemoteEnemy.EnemyKind kind,
                                                   float x, float y,
                                                   out RemoteEnemy comp,
                                                   out Transform hpFill,
                                                   out float fullWidth)
        {
            bool isBoss = kind == RemoteEnemy.EnemyKind.Boss;
            // M3 Phase 08b/08c hardening (5/20): sprite scale 정합.
            // Mushroom (2.5x2.0) → size=1 (world 2.5x2.0), Boss ToxicFrogBlueBlue (1.5x1.5) → size=2.5 (world 3.75x3.75).
            float size = isBoss ? 2.5f : 1.0f;
            Color bodyColor = isBoss ? new Color(0.85f, 0.15f, 0.15f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);

            string name = isBoss ? $"Boss_{entityId}" : $"Enemy_{entityId}";
            GameObject go = new GameObject(name);
            go.transform.localScale = new Vector3(size, size, 1f);
            // M3 hardening 5/20: sprite bottom pivot 기준 + sprite 내부 발 위치 보정.
            // Mushroom: 발이 sprite bottom과 일치 → offset 0.
            // ToxicFrogBlueBlue: sprite 안 발 아래 투명 여백 ~약 0.4 unit (size 2.5 적용 시 1.0 world) → offset -1.0.
            float visualFootOffset = isBoss ? -1.0f : 0f;
            go.transform.position = new Vector3(x, y + visualFootOffset, 0f);

            TryLoadEnemySprites();
            Sprite? bodySprite = isBoss ? _toxicFrogSprite : _mushroomSprite;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (bodySprite != null)
            {
                sr.sprite = bodySprite;
                sr.color = Color.white; // 원본 sprite 색 유지
            }
            else
            {
                sr.sprite = GetWhiteSquare();
                sr.color = bodyColor; // fallback placeholder
            }
            sr.sortingOrder = 2; // Player(1)보다 위

            comp = go.AddComponent<RemoteEnemy>();

            // HP bar 부모 (offset) + 배경 + fill. localScale.x 깎는 단순 패턴.
            // M3 hardening 5/20: sprite *그림 안 실제 머리 위치* hardcode (sprite asset 마다 그림 영역 다름).
            // visualFootOffset과 정합한 *visualHeadLocalY* — local 단위 (transform.localScale로 size 적용).
            //   Mushroom: 그림 발 = sprite bottom (offset 0), 그림 머리 ≈ 1.7 (sprite 안 1.5~1.8 그림 영역)
            //   Boss (ToxicFrogBlueBlue): visualFootOffset=-1 (world) → -0.4 (local) 그림 발 박힘
            //                              그림 머리 = visual 발 + 그림 visual height ≈ -0.4 + 1.5 = 1.1 (local)
            //                              여유 +0.2 = 1.3 (local) → world = -1 + 1.3*2.5 = 2.25
            GameObject hpBarRoot = new GameObject("HpBar");
            hpBarRoot.transform.SetParent(go.transform, worldPositionStays: false);
            float hpBarLocalY = isBoss ? 1.3f : 1.4f; // 그림 머리 추정치 + 여유 (Mushroom 그림 작아 1.4)
            hpBarRoot.transform.localPosition = new Vector3(0f, hpBarLocalY, 0f);
            // 부모 scale로 늘어나는 효과 차단 — 자식 localScale을 1/size로 보정.
            hpBarRoot.transform.localScale = new Vector3(1f / size, 1f / size, 1f);

            // 배경 (어두운 색)
            GameObject hpBg = new GameObject("Bg");
            hpBg.transform.SetParent(hpBarRoot.transform, worldPositionStays: false);
            SpriteRenderer bgSr = hpBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = GetWhiteSquare();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            bgSr.sortingOrder = 3;
            float barWidth = isBoss ? 2.0f : 1.0f;
            float barHeight = isBoss ? 0.3f : 0.15f;
            hpBg.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            // Fill (녹색)
            GameObject hpFillGo = new GameObject("Fill");
            hpFillGo.transform.SetParent(hpBarRoot.transform, worldPositionStays: false);
            SpriteRenderer fillSr = hpFillGo.AddComponent<SpriteRenderer>();
            fillSr.sprite = GetWhiteSquare();
            fillSr.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            fillSr.sortingOrder = 4;
            hpFillGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            hpFill = hpFillGo.transform;
            fullWidth = barWidth;
            return go;
        }
    }
}
