#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 이펙트/투사체 생성 위치 컨벤션 — 엔티티 prefab의 직속 자식 "EffectAnchor".
    //
    // 엔티티 pivot은 발바닥(bottom) 기준이라 entity position에 이펙트를 박으면 발밑에서 터짐.
    // 앵커 자식 배치(몸 중앙 등)는 prefab 저작 = 본인 직접 영역.
    // 앵커 없으면 root 위치 폴백 (fail-soft — 앵커 미저작 prefab도 동작).
    //
    // 위치 *조회만* 제공 — 이펙트는 부모 없이 독립 Transform으로 Instantiate
    // (자식 생성 시 부모 sprite flip/scale이 이펙트에 전파되는 부작용 차단).
    public static class EffectAnchor
    {
        public const string ChildName = "EffectAnchor";

        // 앵커는 prefab 저작 pose(비반전) 기준으로 배치됨.
        // 이 프로젝트 flip은 SpriteRenderer.flipX 방식(AnimatorDriver) — 그림만 거울상이고
        // 자식 transform은 안 움직임 → 현재 flipX를 읽어 앵커 local x를 코드로 반전.
        // facing 부호 대신 flipX를 기준 삼는 이유: SpriteDefaultFacesLeft 스프라이트는
        // facing(+1)에서 flipX=true라 "facing<0 = 거울상"이 항상 참이 아님. 화면 진실 = flipX.
        public static Vector3 ResolvePosition(Transform entityRoot)
        {
            Transform? anchor = entityRoot.Find(ChildName);
            if (anchor == null) return entityRoot.position;
            return MirrorByFlip(entityRoot, anchor.localPosition);
        }

        static Vector3 MirrorByFlip(Transform entityRoot, Vector3 localOffset)
        {
            SpriteRenderer? sr = entityRoot.GetComponent<SpriteRenderer>();
            if (sr == null) sr = entityRoot.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.flipX)
                localOffset.x = -localOffset.x;
            return entityRoot.TransformPoint(localOffset);
        }
    }
}
