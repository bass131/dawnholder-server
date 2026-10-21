#nullable enable
using System;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Main-thread presentation source shared by the two windows. The supplier replaces confirmed values;
    /// windows subscribe and read them. Null means unsynchronized, distinct from a confirmed zero.
    /// </summary>
    public sealed class GuildDisplayModel
    {
        public GuildStatusDisplay? Guild { get; private set; }
        public BlacksmithDisplay? Blacksmith { get; private set; }
        public event Action? Changed;

        public void SetGuild(GuildStatusDisplay? display)
        {
            Guild = display;
            Changed?.Invoke();
        }

        public void SetBlacksmith(BlacksmithDisplay? display)
        {
            Blacksmith = display;
            Changed?.Invoke();
        }
    }
}
