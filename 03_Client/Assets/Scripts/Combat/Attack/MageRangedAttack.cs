#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 마법사 원거리 공격 전략 — 서버 intent 송신만 담당 (헌법 #1).
    //
    // 투사체 스폰은 서버 확정(S_ProjectileLaunch) 수신 후 ProjectileLaunchHandler가 담당.
    // 클라 선예측 스폰 금지 — 서버 miss 시 "그림만 나가고 데미지 0" 위험 제거 (M4.8 기둥1).
    public sealed class MageRangedAttack : IAttackStrategy
    {
        // 8 units 사거리 — 서버 MageAttackHalfExtent 기준 여유분 포함.
        public float TargetingRangeSquared => 64.0f;

        public bool TryAttack(Vector3 origin)
        {
            // C_Attack 송신 — 서버가 명중 판정 후 S_ProjectileLaunch로 발사 연출 통보.
            // 투사체 시각은 해당 패킷 수신 시 생성 (로컬/원격 동일 경로).
            return AttackIntent.TrySend(origin, TargetingRangeSquared, out _);
        }
    }
}
