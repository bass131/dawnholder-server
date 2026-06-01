#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // Enemy/Boss placeholder visual + 서버 권위 HP 미러.
    //
    // **헌법 #1 (Server Authority)** — 본 컴포넌트는 *표시만* 합니다. HP/사망 자체 판정 X.
    //   - HP 갱신: 서버 S_HitResult 경로가 ApplyHpUpdate(currentHp, maxHp) 호출.
    //   - 사망: 서버 S_EntityDeath 경로가 EnemyRegistry.Despawn 호출 → 본 GO Destroy.
    //
    // **EntityKind**: 0=Normal, 1=Boss (서버 enum 정합). byte로 받음 → C# enum cast.
    [DisallowMultipleComponent]
    public class RemoteEnemy : MonoBehaviour
    {
        public enum EnemyKind : byte
        {
            Normal = 0,
            Boss = 1,
        }

        public int EntityId { get; private set; }
        public EnemyKind Kind { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        // 서버 좌표 → 화면 좌표 변환에 쓰이는 y 오프셋.
        // RemoteEntity가 transform.position을 서버 좌표로 덮어쓰므로
        // EnqueueSnapshot/Initialize 호출 전에 offset을 더해야 sprite가 바닥 정합.
        public float VisualFootOffset { get; private set; }

        // HP bar 자식 — Registry가 SetHpBar로 wire. 본 컴포넌트가 SpriteRenderer scale.x 줄이는 방식.
        Transform? _hpBarFill;
        float _hpBarFullWidth;

        public void Initialize(int entityId, EnemyKind kind, int currentHp, int maxHp, float visualFootOffset = 0f)
        {
            EntityId = entityId;
            Kind = kind;
            CurrentHp = currentHp;
            MaxHp = maxHp > 0 ? maxHp : 1; // div-by-zero 차단
            VisualFootOffset = visualFootOffset;
        }

        // Registry가 HP bar 자식 만들고 wire. fullWidth = scale.x 기준 100%.
        public void SetHpBar(Transform hpBarFill, float fullWidth)
        {
            _hpBarFill = hpBarFill;
            _hpBarFullWidth = fullWidth;
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
