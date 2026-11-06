#nullable enable
using System.Linq;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // Packs one character into a contiguous sortingOrder range. The caller orders characters, not their parts.
    internal static class CharacterRenderOrder
    {
        // Player-layer effects start at 4. All body ranges grow downwards from here; layers and z stay authored.
        internal const int HighestBodyOrder = 3;

        internal static int Apply(Transform root, int highestOrder)
        {
            // OrderBy is stable: equal authored orders use hierarchy order, including on the first Attach.
            // Inactive old Visuals awaiting Destroy and disabled/empty renderers are not bodies.
            SpriteRenderer[] bodies = root.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => renderer.gameObject.activeInHierarchy && renderer.enabled && renderer.sprite != null)
                .OrderBy(renderer => renderer.sortingOrder).ToArray();
            int lowestOrder = highestOrder - bodies.Length + 1;
            for (int index = 0; index < bodies.Length; index++)
                bodies[index].sortingOrder = lowestOrder + index;
            return lowestOrder - 1;
        }
    }
}
