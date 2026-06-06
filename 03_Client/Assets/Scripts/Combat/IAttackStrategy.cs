#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 공격 전략 인터페이스 — 직업별 공격 패턴을 교환 가능하게 분리.
    //
    // **헌법 #1**: 구현체는 target 추천 + intent 송신만. 데미지/판정은 서버 전용.
    // **직업별 구현체**: KnightMeleeAttack / MageRangedAttack. ClassConfig SO가 주입.
    public interface IAttackStrategy
    {
        // origin: 공격자 월드 위치. 타게팅 힌트 계산 기준점.
        void TryAttack(Vector3 origin);
    }
}
