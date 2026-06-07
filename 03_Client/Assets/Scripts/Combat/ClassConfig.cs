#nullable enable
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 직업별 장착 데이터 — 직업 비주얼 prefab 참조 + 공격 전략 공급원.
    //
    // 이동값(MoveSpeed/JumpVel)은 여기에 두지 않음:
    //   PlayerStats.ForClass()가 단일 출처 — SO에 중복 보유 시 서버와 영구 mispredict drift 발생.
    //
    // 아트 컨셉(Knight/Mage) ↔ 프로토콜 직업(Warrior/Ranger) 매핑은 에셋의 Class 필드가 연결.
    //
    // **로직/비주얼 분리 (M4.5 Phase 05 v2)**:
    //   LocalPlayer/RemotePlayer = 로직 껍데기. 직업 시각 정체성(스프라이트/Animator/EffectAnchor)은
    //   VisualPrefab *한 곳*에만 저작 — 본인 화면과 타인 화면이 같은 비주얼을 공유 (drift 차단).
    //   런타임에 ClassVisualMount가 "Visual" 자식으로 장착.
    public abstract class ClassConfig : ScriptableObject
    {
        public CharacterClass Class;

        // 직업 비주얼 prefab (직업당 1개) — Inspector에서 드래그 연결.
        public GameObject? VisualPrefab;

        public abstract IAttackStrategy CreateStrategy();
    }
}
