using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios.Persistence;

public static class PersistenceReconnectScenario
{
    public sealed record Result(bool Success, string Reason);

    public static async Task<Result> Run(string host, int port, string accountName, CancellationToken ct = default)
    {
        if (!AccountNameRule.TryNormalize(accountName, out string normalized))
            return new(false, "An explicit configured persistence account is required.");
        PersistenceCharacterProbe first = new(normalized, CharacterClass.Mage);
        PersistenceCharacterProbe reconnect = new(normalized, CharacterClass.Knight);
        try
        {
            string? firstFailure = await first.ObserveMageAsync(host, port, ct);
            if (firstFailure is not null) return new(false, firstFailure);
            first.Disconnect();
            // Allow the normal close budget to settle. The subsequent Success is the observation,
            // not this delay; Busy/RecoveryRequired/Unavailable is reported without retries.
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            string? restoredFailure = await reconnect.ObserveMageAsync(host, port, ct);
            return restoredFailure is null
                ? new(true, "Same-process reconnect selected Knight and observed Mage after normal close.")
                : new(false, restoredFailure);
        }
        finally
        {
            first.Disconnect();
            reconnect.Disconnect();
        }
    }
}
