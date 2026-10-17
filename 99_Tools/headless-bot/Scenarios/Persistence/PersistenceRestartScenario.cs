using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios.Persistence;

/// <summary>Run seed, restart the authorized server/process externally, then run restore with the same account.</summary>
public static class PersistenceRestartScenario
{
    public static async Task<PersistenceReconnectScenario.Result> Run(string host, int port, string accountName,
        string phase, CancellationToken ct = default)
    {
        if (!AccountNameRule.TryNormalize(accountName, out string normalized) || phase is not ("seed" or "restore"))
            return new(false, "Explicit account and phase seed/restore are required.");
        PersistenceCharacterProbe probe = new(normalized,
            phase == "seed" ? CharacterClass.Mage : CharacterClass.Knight);
        try
        {
            string? failure = await probe.ObserveMageAsync(host, port, ct);
            return failure is null
                ? new(true, phase == "seed"
                    ? "Mage observed; normal close requested. Restart must be performed by the authorized operator."
                    : "After externally performed restart, Knight intent returned stored Mage.")
                : new(false, failure);
        }
        finally { probe.Disconnect(); }
    }
}
