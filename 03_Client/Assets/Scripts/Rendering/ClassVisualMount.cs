#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 로직 껍데기(LocalPlayer/RemotePlayer root)에 직업 비주얼 prefab을 "Visual" 자식으로 장착/교체.
    //
    // **순서 불변식** (plan-auditor 🔴): 옛 자식 비활성→파괴 → 새 자식 장착 → AnimatorDriver.Rebind().
    //   - 비활성 먼저: Destroy는 프레임 끝 지연이라, 비활성 없이 Rebind하면
    //     GetComponentInChildren이 죽어가는 옛 SR/Animator를 다시 잡음.
    //   - Rebind 마지막: AnimatorDriver.Awake 캐시는 비주얼 장착 *전* 상태라 항상 stale.
    public static class ClassVisualMount
    {
        public const string ChildName = "Visual";

        public static void Attach(Transform root, GameObject? visualPrefab)
        {
            if (visualPrefab == null)
            {
                Debug.LogWarning($"[ClassVisualMount] '{root.name}' — VisualPrefab null. 비주얼 미장착 (ClassConfig 연결 확인).");
                return;
            }

            Transform old = root.Find(ChildName);
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }

            GameObject visual = Object.Instantiate(visualPrefab, root);
            visual.name = ChildName;
            visual.transform.localPosition = Vector3.zero;

            root.GetComponent<AnimatorDriver>()?.Rebind();
        }
    }
}
