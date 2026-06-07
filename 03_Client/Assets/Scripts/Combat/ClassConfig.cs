#nullable enable
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 직업별 장착 데이터 — variant prefab 참조 + 공격 전략 공급원.
    //
    // 이동값(MoveSpeed/JumpVel)은 여기에 두지 않음:
    //   PlayerStats.ForClass()가 단일 출처 — SO에 중복 보유 시 서버와 영구 mispredict drift 발생.
    //
    // 아트 컨셉(Knight/Mage) ↔ 프로토콜 직업(Warrior/Ranger) 매핑은 에셋의 Class 필드가 연결.
    //
    // **Prefab Variant 구조 (M4.5 Phase 05)**:
    //   Controller/EffectAnchorOffset 은퇴 — variant prefab이 controller와 EffectAnchor 자식을 직접 보유.
    //   Inspector에서 LocalPlayerPrefab/RemotePlayerPrefab에 각 variant prefab을 드래그 연결.
    //   미연결 시 Spawner가 base prefab 폴백 + 경고 1회 (fail-soft).
    public abstract class ClassConfig : ScriptableObject
    {
        public CharacterClass Class;

        // 직업별 LocalPlayer variant prefab — Inspector에서 드래그 연결.
        public GameObject? LocalPlayerPrefab;

        // 직업별 RemotePlayer variant prefab — Inspector에서 드래그 연결.
        public GameObject? RemotePlayerPrefab;

        public abstract IAttackStrategy CreateStrategy();
    }
}
