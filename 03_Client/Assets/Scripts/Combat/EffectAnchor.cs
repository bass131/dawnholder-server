#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 이펙트/투사체 생성 위치 컨벤션 — 엔티티 계층 안의 "EffectAnchor" 자식.
    //
    // 엔티티 pivot은 발바닥(bottom) 기준이라 entity position에 이펙트를 박으면 발밑에서 터짐.
    // 앵커 배치(몸 중앙 등)는 prefab 저작 = 본인 직접 영역.
    //   - 플레이어(v2): 직업 비주얼 prefab 안 (root>Visual>EffectAnchor)
    //   - 적/보스: prefab 직속 자식
    // 앵커 없으면 root 위치 폴백 (fail-soft — 앵커 미저작 prefab도 동작).
    //
    // 위치 *조회만* 제공 — 이펙트는 부모 없이 독립 Transform으로 Instantiate
    // (자식 생성 시 부모 sprite flip/scale이 이펙트에 전파되는 부작용 차단).
    public static class EffectAnchor
    {
        public const string ChildName = "EffectAnchor";

        // 앵커는 prefab 저작 pose(비반전, 우향) 기준으로 배치됨.
        // 이 프로젝트 flip은 SpriteRenderer.flipX 방식(AnimatorDriver) — 그림만 거울상이고
        // 자식 transform은 안 움직임 → flipX가 켜져 있으면 코드로 거울상 보정.
        //
        // **world 기준 root.x 거울상** (plan-auditor 🔴 봉합): anchor.localPosition은
        // *직속 부모(Visual 등) 기준*이라 root.TransformPoint로 곱하면 중첩 깊이에서 깨짐.
        // anchor.position(world)을 직접 읽고 root.x 축으로 반사 — 깊이 무관.
        // ⚠️ 불변식: 반사 중심 = root.x — SR(거울상 중심)이 root와 x 정렬이어야 정확.
        //   ClassVisualMount가 Visual을 localPosition=zero로 장착해 보장. Visual에 x offset을
        //   박으면 silent drift (reviewer 🟡 박제).
        //
        // facing 부호 대신 flipX를 기준 삼는 이유: SpriteDefaultFacesLeft 스프라이트는
        // facing(+1)에서 flipX=true라 "facing<0 = 거울상"이 항상 참이 아님. 화면 진실 = flipX.
        public static Vector3 ResolvePosition(Transform entityRoot)
        {
            Transform? anchor = FindRecursive(entityRoot, ChildName);
            if (anchor == null) return entityRoot.position;

            Vector3 world = anchor.position;
            SpriteRenderer? sr = entityRoot.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.flipX)
                world.x = 2f * entityRoot.position.x - world.x;
            return world;
        }

        static Transform? FindRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child;
                Transform? found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
