#nullable enable
using System;

namespace Dawnholder.Client.Demo
{
    public enum DemoRole
    {
        Knight,
        Mage,
    }

    // Command-line switch for the scripted demo run (user decision 21A, 10-28: record a two-client play
    // without human input). A player launched without -demo-role behaves exactly like the shipped build.
    //   Dawnholder.exe -demo-role knight -demo-account demo_knight
    // -demo-mute silences one of the two players so the recording has a single copy of the game sound;
    // -demo-no-bgm keeps only effects on the recorded player.
    public sealed class DemoLaunchOptions
    {
        public DemoRole Role { get; }
        public string Account { get; }
        public bool Muted { get; }
        public bool NoMusic { get; }

        DemoLaunchOptions(DemoRole role, string account, bool muted, bool noMusic)
        {
            Role = role;
            Account = account;
            Muted = muted;
            NoMusic = noMusic;
        }

        internal static DemoLaunchOptions? Parse(string[] args)
        {
            string? role = ValueAfter(args, "-demo-role");
            if (role == null) return null;

            DemoRole parsed;
            if (string.Equals(role, "knight", StringComparison.OrdinalIgnoreCase)) parsed = DemoRole.Knight;
            else if (string.Equals(role, "mage", StringComparison.OrdinalIgnoreCase)) parsed = DemoRole.Mage;
            else return null;

            string account = ValueAfter(args, "-demo-account") ?? (parsed == DemoRole.Knight ? "demo_knight" : "demo_mage");
            return new DemoLaunchOptions(parsed, account, HasFlag(args, "-demo-mute"), HasFlag(args, "-demo-no-bgm"));
        }

        static bool HasFlag(string[] args, string name) =>
            Array.Exists(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

        static string? ValueAfter(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }
}
