#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // M3 Phase 08c: Enemy/Boss 전담 registry. Player(RemoteEntityRegistry)와 영역 분리.
    //
    // **분리 이유** (응급 결정 — 5/19):
    //   1. lookup 분리 — Player entityId와 enemy entityId가 서버 같은 풀이라
    //      RemoteEntityRegistry에 섞으면 type 분기 위해 매 frame switch 필요.
    //   2. 컴포넌트 타입 다름 — RemoteEntity (보간 buffer) vs RemoteEnemy (HP+kind).
    //   3. 정유현 영역 격리 — RemoteEntityRegistry는 정유현이 Prefab variant 박는 영역.
    //
    // **prefab 파일 안 쓰는 이유** (응급 placeholder 약속):
    //   디자인 0 + 정유현 prefab 영역 보존 + 씬 YAML 편집 회피 위해 *런타임 코드 생성*.
    //   spawn 시점에 GameObject+SpriteRenderer+HpBar를 코드로 build.
    //   미래 정유현 prefab 박으면 _normalPrefab/_bossPrefab SerializeField로 교체.
    //
    // **시그니처 약속** (UnityClientSession 호출):
    //   - Spawn(int entityId, byte entityKind, float x, float y, int currentHp, int maxHp)
    //   - ApplyHit(int targetEntityId, int currentHp, int maxHp)
    //   - Despawn(int entityId)
    //   - TryGetNearest(Vector3 origin, float maxRangeSq, out int targetEntityId)
    //   - Clear()
    [DisallowMultipleComponent]
    public class EnemyRegistry : MonoBehaviour
    {
        public static EnemyRegistry? Instance { get; private set; }

        readonly Dictionary<int, RemoteEnemy> _enemies = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[EnemyRegistry] 중복 박힘 — 씬에 여러 인스턴스. 본인 셋업 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }

        // S_EntitySpawn 핸들러에서 호출.
        public void Spawn(int entityId, byte entityKind, float x, float y, int currentHp, int maxHp)
        {
            if (_enemies.ContainsKey(entityId))
            {
                Debug.LogWarning($"[EnemyRegistry] entity {entityId} 이미 spawn — 중복 패킷 drop.");
                return;
            }

            RemoteEnemy.EnemyKind kind = (RemoteEnemy.EnemyKind)entityKind;
            GameObject go = BuildPlaceholder(entityId, kind, x, y, out RemoteEnemy comp,
                                              out Transform hpFill, out float fullWidth);
            comp.Initialize(entityId, kind, currentHp, maxHp);
            comp.SetHpBar(hpFill, fullWidth);
            _enemies[entityId] = comp;
            Debug.Log($"[EnemyRegistry] Spawned {kind} entity {entityId} at ({x:F2}, {y:F2}) hp={currentHp}/{maxHp}");
        }

        // S_HitResult 핸들러에서 호출.
        public void ApplyHit(int targetEntityId, int currentHp, int maxHp)
        {
            if (!_enemies.TryGetValue(targetEntityId, out RemoteEnemy? enemy))
            {
                // Death packet이 먼저 도착했거나 spawn 전 race — 응급 단순: silent drop.
                return;
            }
            enemy.ApplyHpUpdate(currentHp, maxHp);
        }

        // S_EntityDeath 핸들러에서 호출.
        public void Despawn(int entityId)
        {
            if (!_enemies.TryGetValue(entityId, out RemoteEnemy? enemy)) return;
            _enemies.Remove(entityId);
            if (enemy != null) Destroy(enemy.gameObject);
            Debug.Log($"[EnemyRegistry] Despawned entity {entityId}");
        }

        // LocalPlayerController 공격 입력에서 호출.
        // origin 기준 maxRangeSq 안 가장 가까운 enemy/boss entityId 반환. 없으면 false.
        // 헌법 #1 — *클라는 target 추천*만, 서버가 최종 검증 (range/cooldown/dead 모두 서버 재검사).
        public bool TryGetNearest(Vector3 origin, float maxRangeSq, out int targetEntityId)
        {
            targetEntityId = 0;
            float bestSq = maxRangeSq;
            bool found = false;
            foreach (KeyValuePair<int, RemoteEnemy> kv in _enemies)
            {
                RemoteEnemy enemy = kv.Value;
                if (enemy == null) continue;
                Vector3 diff = enemy.transform.position - origin;
                float dSq = diff.x * diff.x + diff.y * diff.y; // 2D — z 무시
                if (dSq <= bestSq)
                {
                    bestSq = dSq;
                    targetEntityId = kv.Key;
                    found = true;
                }
            }
            return found;
        }

        public void Clear()
        {
            foreach (RemoteEnemy enemy in _enemies.Values)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            _enemies.Clear();
        }

        // 런타임 placeholder 생성. Normal=Mushroom sprite, Boss=ToxicFrog sprite + HP bar 자식.
        // M3 Phase 08b/08c hardening (5/20): 응급 시연 시각 강화 — placeholder 색 박스 → 실제 sprite asset.
        // Sprite는 Editor에서 AssetDatabase로 load (Editor only — 응급 시연 영역).
        // M4에서 Resources/Addressables로 마이그레이션 (런타임 빌드 지원).
        // Sprite asset 없으면 옛 placeholder(흰 사각형 + tint)로 fallback (안전).
        static Sprite? _whiteSquare;
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
        static Sprite? _mushroomSprite;
        static Sprite? _toxicFrogSprite;
        static bool _spritesLoaded;
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

        static GameObject BuildPlaceholder(int entityId, RemoteEnemy.EnemyKind kind,
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

            // M3 hardening 5/20: 발바닥 pivot 정합 — 서버 spawn (x, y)는 *발바닥* 기준 가정.
            // sprite pivot=center라 transform.y를 sprite 절반 높이만큼 위로 보정해야 발바닥이 spawn y에 맞음.
            float halfHeight = bodySprite != null ? bodySprite.bounds.size.y * size / 2f : 0.5f * size;
            go.transform.position = new Vector3(x, y + halfHeight, 0f);

            comp = go.AddComponent<RemoteEnemy>();

            // HP bar 부모 (offset) + 배경 + fill. localScale.x 깎는 단순 패턴.
            GameObject hpBarRoot = new GameObject("HpBar");
            hpBarRoot.transform.SetParent(go.transform, worldPositionStays: false);
            // 부모 scale이 size배 적용되니, 자식 localPosition.y는 1/size로 보정해야 시각 일정.
            // Boss(size=2)면 박스 윗변 = +1 (local 0.5) → +0.7 = local 0.35; Normal(size=1)면 +0.6 = local 0.6.
            float hpBarLocalY = isBoss ? 0.35f : 0.6f;
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
