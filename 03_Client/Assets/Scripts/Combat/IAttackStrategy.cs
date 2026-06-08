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
        // 반환: C_Attack을 실제로 송신했으면 true (타겟 잡힘) — 호출자가 로컬 commit window
        //   예측 게이트 시작 신호로 사용. 헛스윙(타겟 없음)은 false → 이동 잠금 안 함.
        bool TryAttack(Vector3 origin);
    }
}
