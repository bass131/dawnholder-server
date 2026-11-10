#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Input
{
    /// <summary>
    /// Whether the pause menu has stopped game time, so new gameplay input (move, jump, attack, skill, portal, invite)
    /// must not create packets or predictions. <see cref="Dawnholder.Client.UI.PauseMenuController"/> is the only
    /// writer of <see cref="Time.timeScale"/>, so it is the pause signal and no second pause state exists.
    /// InventoryPanel and GuildUiController still test <c>Time.timeScale &gt; 0f</c> inline; a new time control
    /// (hit-stop, slow motion) must change them together with this one.
    /// </summary>
    internal static class GameplayPause
    {
        internal static bool IsPaused => Time.timeScale <= 0f;
    }
}
