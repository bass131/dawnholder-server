#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>Vertical offset of the chosen character's standing art, as a function of the time since choosing.</summary>
    internal static class CharacterArtBobMotion
    {
        // UI units at the menu's 800×450 reference: the art rises up to 2.5 units and back over one slow cycle.
        internal const float Height = 2.5f;
        internal const float Period = 2.4f;

        // Starts at rest (offset 0) so choosing a slot does not make the art jump.
        internal static float OffsetAt(float elapsed) =>
            Height * 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * elapsed / Period));
    }
}
