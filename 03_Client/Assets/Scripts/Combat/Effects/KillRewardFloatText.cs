#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Shared.GameData;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 몬스터 처치 때 사망 자리 위로 보상 줄(「+금화 N」, 「+<아이템 이름> N」)을 띄웠다가 흐리게 지우는 월드 공간 글.
    //
    // 규칙(증가분·클리어 몫·짝짓기 창·가장 가까운 사망)은 KillRewardPairing 한 곳에 있다. 여기는 구독, Unity 객체,
    // 시간에 따른 움직임만 맡는다. 값은 서버가 확정한 스냅숏 차이뿐이고 보상을 판정하지 않는다(헌법 #1).
    //
    // **수명**:
    //   - 씬 소유(CombatBootstrap). 맵 이동은 새 씬이라 남은 글·사망 기록이 씬과 함께 사라지고,
    //     새 씬의 인스턴스는 보존된 인벤토리 미러의 현재 값을 기준으로 잡는다.
    //   - 연결 종료는 InventoryState 리셋 통지(HasSnapshot == false)로 글·기록·기준을 비운다.
    //   - 외곽선 material 복사본은 이 컴포넌트가 만들고 파괴한다. font asset은 바꾸지 않는다.
    // **입력**: 글은 TextMeshPro(3D)뿐이다. Canvas·GraphicRaycaster·collider가 없어 포인터·공격 입력을 가리지 않는다.
    [DisallowMultipleComponent]
    public sealed class KillRewardFloatText : MonoBehaviour
    {
        // 사용자 원문 「위로 천천히 떠올랐다가 사라지는」의 「천천히」. 메인 처분 msg_22555c3ffe7a: 1.5초 이상이고,
        // 캡처와 사용자의 마지막 플레이에서 이 값 하나로 맞춘다.
        const float LifetimeSeconds = 1.8f;
        // 수명 끝의 이 구간에서 alpha가 1 → 0으로 흐려진다.
        const float FadeSeconds = 0.5f;
        const float RiseDistance = 0.9f;
        // 사망 자리는 적 transform(발 기준)이다. 몸통을 비켜 그 위에서 시작한다.
        const float StartHeight = 1.6f;
        const float LineHeight = 0.5f;
        const float LineWidth = 5f;
        const float FontSize = 4f;
        const float OutlineWidth = 0.22f;
        // 같은 월드 TMP인 DungeonEntranceLabel과 같은 값 — 적·플레이어 위에 그린다.
        const int SortingOrder = 100;

        // Art 리드 답 msg_ae88de6f6d11: 금화 #FFEB33(인벤토리 제목 색), 아이템 #FFF0BA(길드 성장 레벨 글자), 외곽선 #281400.
        static readonly Color GoldColor = new Color(1f, 0.92f, 0.2f);
        static readonly Color ItemColor = new Color(1f, 0.94f, 0.73f);
        static readonly Color32 OutlineColor = new Color32(40, 20, 0, 255);

        readonly KillRewardPairing _pairing = new();
        readonly InventorySlot[] _slots = new InventorySlot[InventoryLimits.MaxSlots];
        readonly List<Shown> _shown = new();
        InventoryState? _inventory;
        EnemyRegistry? _enemies;
        DungeonClearRewardState? _clearResult;
        TMP_FontAsset? _font;
        Material? _outline;

        public static void BuildRuntime(Transform parent)
        {
            if (parent.GetComponentInChildren<KillRewardFloatText>(includeInactive: true) != null) return;
            var go = new GameObject("_KillRewardFloatText");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<KillRewardFloatText>();
        }

        void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_font == null || _font.material == null) return;
            // GuildGrowthEffectPresentation과 같은 방식: 외곽선 복사본을 여기서 소유해 공유 font asset을 바꾸지 않는다.
            _outline = new Material(_font.material) { hideFlags = HideFlags.HideAndDontSave };
            _outline.EnableKeyword(ShaderUtilities.Keyword_Outline);
            _outline.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);
            _outline.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
        }

        void OnEnable() => BindSources();

        void OnDisable()
        {
            Unbind();
            // 비활성화 콜백(씬 unload 포함) 안에서는 자식의 SetActive가 막히므로 파괴만 예약한다.
            ClearShown(hideNow: false);
            _pairing.Reset();
        }

        void OnDestroy()
        {
            if (_outline != null) Destroy(_outline);
            _outline = null;
        }

        void Update()
        {
            BindSources();
            float now = Time.realtimeSinceStartup;
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                Shown shown = _shown[i];
                float age = now - shown.BornAt;
                if (shown.Root == null || age >= LifetimeSeconds)
                {
                    Discard(shown.Root, hideNow: true);
                    _shown.RemoveAt(i);
                    continue;
                }
                // 처음엔 조금 빠르게, 끝으로 갈수록 멈추듯 천천히 오른다(ease-out).
                float t = age / LifetimeSeconds;
                shown.Root.transform.position = shown.Start + Vector3.up * (RiseDistance * (1f - (1f - t) * (1f - t)));
                float alpha = Mathf.Clamp01((LifetimeSeconds - age) / FadeSeconds);
                foreach (TextMeshPro line in shown.Lines) line.alpha = alpha;
            }
        }

        static Vector3? LocalPlayerSpot()
        {
            LocalPlayerMovement? player = LocalPlayerMovement.Instance;
            if (player == null) return null;
            return player.transform.position;
        }

        static void Discard(GameObject? root, bool hideNow)
        {
            if (root == null) return;
            // 파괴는 frame 끝이라 먼저 꺼서 같은 frame에도 보이지 않게 한다.
            if (hideNow) root.SetActive(false);
            Destroy(root);
        }

        // 미러·registry가 늦게 생기거나 바뀌면 다시 묶는다(HudController.BindInventory 관례).
        void BindSources()
        {
            InventoryState? inventory = InventoryState.Instance;
            if (!ReferenceEquals(_inventory, inventory))
            {
                UnbindInventory();
                _inventory = inventory;
                if (inventory != null) inventory.OnInventoryChanged += OnInventoryChanged;
                // 이미 들고 있던 값은 이번 처치의 증가가 아니라 기준이다. 맵 이동 뒤 새 씬도 여기서 기준을 잡는다.
                _pairing.Reset();
                if (inventory != null && inventory.HasSnapshot) _pairing.SetBaseline(inventory.Currency, ReadSlots(inventory));
            }

            EnemyRegistry? enemies = EnemyRegistry.Instance;
            if (!ReferenceEquals(_enemies, enemies))
            {
                UnbindEnemies();
                _enemies = enemies;
                if (enemies != null) enemies.EnemyDied += OnEnemyDied;
            }

            DungeonClearRewardState? clearResult = DungeonClearRewardState.Instance;
            if (!ReferenceEquals(_clearResult, clearResult))
            {
                UnbindClearResult();
                _clearResult = clearResult;
                if (clearResult != null) clearResult.Changed += OnClearResultChanged;
            }
        }

        void Unbind()
        {
            UnbindInventory();
            UnbindEnemies();
            UnbindClearResult();
        }

        // 파괴된 Unity 객체도 우리가 단 managed 구독은 들고 있으므로 ReferenceEquals로 푼다.
        void UnbindInventory()
        {
            if (!ReferenceEquals(_inventory, null)) _inventory!.OnInventoryChanged -= OnInventoryChanged;
            _inventory = null;
        }

        void UnbindEnemies()
        {
            if (!ReferenceEquals(_enemies, null)) _enemies!.EnemyDied -= OnEnemyDied;
            _enemies = null;
        }

        void UnbindClearResult()
        {
            if (!ReferenceEquals(_clearResult, null)) _clearResult!.Changed -= OnClearResultChanged;
            _clearResult = null;
        }

        void OnEnemyDied(Vector3 spot) => _pairing.RecordDeath(spot, Time.realtimeSinceStartup);

        void OnClearResultChanged()
        {
            // Changed는 새 결과 적용과 연결 종료 정리 때 온다. 정리 뒤에는 이 연결의 pending 결과가 없다.
            DungeonClearRewardState? result = _clearResult;
            bool newSuccess = result != null && result.Result == InventoryResult.Success && result.HasPendingFor(UnityClientSession.Instance);
            if (!newSuccess) return;
            // Success만 결과 바로 뒤에 스냅숏이 온다(서버 InventoryRegistry.ApplyDungeonClear). 보상 창이 보여 준 몫이다.
            _pairing.ExpectDungeonClearShare(result.Currency, result.Items);
        }

        void OnInventoryChanged()
        {
            InventoryState? inventory = _inventory;
            if (inventory == null || !inventory.HasSnapshot)
            {
                // 연결 종료·세션 리셋: 다음 연결의 첫 스냅숏은 기준만 잡는다.
                _pairing.Reset();
                ClearShown(hideNow: true);
                return;
            }
            if (_pairing.TryTakeReward(inventory.Currency, ReadSlots(inventory), Time.realtimeSinceStartup, LocalPlayerSpot(),
                    out KillRewardPairing.Reward reward))
            {
                Show(reward);
            }
        }

        InventorySlot[] ReadSlots(InventoryState inventory)
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = inventory.GetSlot(i);
            return _slots;
        }

        void Show(KillRewardPairing.Reward reward)
        {
            var root = new GameObject("KillRewardText");
            root.transform.SetParent(transform, worldPositionStays: false);
            Vector3 start = reward.Spot + Vector3.up * StartHeight;
            root.transform.position = start;

            var lines = new List<TextMeshPro>();
            if (reward.Gold > 0) lines.Add(AddLine(root.transform, lines.Count, KillRewardPairing.GoldLine(reward.Gold), GoldColor));
            foreach (InventorySlot item in reward.Items)
            {
                lines.Add(AddLine(root.transform, lines.Count, KillRewardPairing.ItemLine(item), ItemColor));
            }
            _shown.Add(new Shown(root, lines.ToArray(), start, Time.realtimeSinceStartup));
        }

        TextMeshPro AddLine(Transform root, int index, string line, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(root, worldPositionStays: false);
            // 첫 줄(금화)이 위, 아이템 줄이 그 아래로 쌓인다.
            go.transform.localPosition = new Vector3(0f, -index * LineHeight, 0f);
            ((RectTransform)go.transform).sizeDelta = new Vector2(LineWidth, LineHeight);
            var text = go.AddComponent<TextMeshPro>();
            // 글꼴을 먼저 넣는다 — font를 바꾸면 TMP가 material을 그 font의 기본값으로 되돌린다.
            if (_font != null) text.font = _font;
            if (_outline != null) text.fontSharedMaterial = _outline;
            text.text = line;
            text.fontSize = FontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = color;
            text.raycastTarget = false;
            text.GetComponent<Renderer>().sortingOrder = SortingOrder;
            return text;
        }

        void ClearShown(bool hideNow)
        {
            foreach (Shown shown in _shown) Discard(shown.Root, hideNow);
            _shown.Clear();
        }

        readonly struct Shown
        {
            public readonly GameObject Root;
            public readonly TextMeshPro[] Lines;
            public readonly Vector3 Start;
            public readonly float BornAt;

            public Shown(GameObject root, TextMeshPro[] lines, Vector3 start, float bornAt)
            {
                Root = root;
                Lines = lines;
                Start = start;
                BornAt = bornAt;
            }
        }
    }
}
