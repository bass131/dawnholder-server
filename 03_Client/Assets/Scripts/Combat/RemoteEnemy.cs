#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // M3 Phase 08c: Enemy/Boss placeholder visual + 서버 권위 HP 미러.
    //
    // **헌법 #1 (Server Authority)** — 본 컴포넌트는 *표시만* 합니다. HP/사망 자체 판정 X.
    //   - HP 갱신: UnityClientSession.HandleHitResult가 ApplyHpUpdate(currentHp, maxHp) 호출.
    //   - 사망: UnityClientSession.HandleEntityDeath가 EnemyRegistry.Despawn 호출 → 본 GO Destroy.
    //
    // **응급 placeholder visual** (Phase 정의 약속 — 디자인 0):
    //   - Normal: 회색 1x1 박스 + 위쪽 HP bar (가로 1.0 × 세로 0.15)
    //   - Boss:   빨간 2x2 박스 + 큰 HP bar (가로 2.0 × 세로 0.3)
    //   - prefab 파일 만들지 않음 — EnemyRegistry가 코드로 런타임 build.
    //     (정유현 영역 Prefabs/Characters/ 격리 + 씬 YAML 편집 회피 동시 달성)
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

        // HP bar 자식 — Initialize에서 Registry가 SetHpBar로 wire.
        // 응급 단순화: 별도 컴포넌트 X. 본 컴포넌트가 SpriteRenderer scale.x 줄이는 방식.
        Transform? _hpBarFill;
        float _hpBarFullWidth;

        public void Initialize(int entityId, EnemyKind kind, int currentHp, int maxHp)
        {
            EntityId = entityId;
            Kind = kind;
            CurrentHp = currentHp;
            MaxHp = maxHp > 0 ? maxHp : 1; // div-by-zero 차단
        }

        // Registry가 HP bar 자식 만들고 wire. fullWidth = scale.x 기준 100%.
        public void SetHpBar(Transform hpBarFill, float fullWidth)
        {
            _hpBarFill = hpBarFill;
            _hpBarFullWidth = fullWidth;
            RefreshHpBar();
        }

        // UnityClientSession.HandleHitResult 경로 — 서버 권위 HP 그대로 미러 (헌법 #1).
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

            // pivot 좌측 정렬 효과 — fill의 부모 기준 localPosition.x를 -(fullWidth-ratio*fullWidth)/2로
            // 보정해 왼쪽 고정. 응급 단순: 부모 pivot 기준 중심 정렬 (양쪽 깎임) 그대로 둠.
            // Phase 09 리허설에서 이상하면 좌측 정렬로 fix.
        }
    }
}
