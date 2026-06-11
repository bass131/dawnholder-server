#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // Enemy/Boss 시각 미러 + 서버 권위 HP 표시.
    //
    // **헌법 #1 (Server Authority)** — 표시만 합니다. HP/사망 판정 X.
    //   - HP 갱신: 서버 S_HitResult 경로 → ApplyHpUpdate(currentHp, maxHp) 호출.
    //   - 사망: 서버 S_EntityDeath 경로 → EnemyRegistry.Despawn → GO Destroy.
    //
    // **prefab 방식**: visualFootOffset / HP바 참조를 prefab에서 직렬화.
    //   EnemyViewFactory가 Instantiate 후 Initialize만 호출 — 런타임 조립 없음.
    //
    // **EnemyKind** = 98_Shared 단일 정의 (M4.5-02 이사).
    [DisallowMultipleComponent]
    public class RemoteEnemy : MonoBehaviour
    {
        public int EntityId { get; private set; }
        public EnemyKind Kind { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        // 서버 좌표 → 화면 좌표 변환 y 오프셋.
        // sprite bottom pivot 기준으로 sprite 내부 발 위치까지의 world 단위 보정값.
        // RemoteEntity.EnqueueSnapshot/Initialize 호출 전에 y에 더해야 sprite 바닥이 타일과 정합.
        [SerializeField] float _visualFootOffset;
        public float VisualFootOffset => _visualFootOffset;

        // HP bar Fill Transform. localScale.x를 [0, fullWidth] 범위로 줄여 HP 표시.
        [SerializeField] Transform? _hpBarFill;

        // Fill Transform이 HP 100%일 때의 localScale.x.
        // prefab 저작 시 fill.localScale.x = 원하는 최대 폭으로 세팅하면 여기서 읽어 기준값으로 씀.
        [SerializeField] float _hpBarFullWidth = 1f;

        public void Initialize(int entityId, EnemyKind kind, int currentHp, int maxHp)
        {
            EntityId = entityId;
            Kind = kind;
            CurrentHp = currentHp;
            MaxHp = maxHp > 0 ? maxHp : 1;
            RefreshHpBar();
        }

        // 서버 S_HitResult 경로 — 서버 권위 HP 그대로 미러 (헌법 #1).
        public void ApplyHpUpdate(int currentHp, int maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp > 0 ? maxHp : 1;
            RefreshHpBar();
        }

        void RefreshHpBar()
        {
            if (_hpBarFill == null) return;
            float ratio = Mathf.Clamp01((float)CurrentHp / MaxHp);
            Vector3 s = _hpBarFill.localScale;
            s.x = _hpBarFullWidth * ratio;
            _hpBarFill.localScale = s;
        }
    }
}
