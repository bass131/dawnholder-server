#nullable enable
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 직업별 장착 데이터 — Animator controller + 공격 전략 공급원.
    //
    // 이동값(MoveSpeed/JumpVel)은 여기에 두지 않음:
    //   PlayerStats.ForClass()가 단일 출처 — SO에 중복 보유 시 서버와 영구 mispredict drift 발생.
    //
    // 아트 컨셉(Knight/Mage) ↔ 프로토콜 직업(Warrior/Ranger) 매핑은 에셋의 Class 필드가 연결.
    public abstract class ClassConfig : ScriptableObject
    {
        public CharacterClass Class;
        public RuntimeAnimatorController? Controller;

        public abstract IAttackStrategy CreateStrategy();
    }
}
