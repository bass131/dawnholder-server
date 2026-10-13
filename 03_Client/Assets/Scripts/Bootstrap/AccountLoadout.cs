#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Bootstrap
{
    public static class AccountLoadout
    {
        public const string PrefsKey = "AccountName";

        // A running client's selection is process-local; shared PlayerPrefs only prefill the menu.
        public static string? SessionAccountName { get; set; }

        public static string GetMenuName() => SessionAccountName ?? PlayerPrefs.GetString(PrefsKey, "");

        public static void Select(string normalizedName)
        {
            SessionAccountName = normalizedName;
            PlayerPrefs.SetString(PrefsKey, normalizedName);
        }
    }
}
