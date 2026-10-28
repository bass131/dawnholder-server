using Shared.GameData;
using Shared.Protocol;
using System.Net;

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

    /// <summary>Observe one existing Stored character; the operator owns the intervening server restart.</summary>
    public static async Task<PersistenceReconnectScenario.Result> RunStored(string host, int port, string accountName,
        string characterIdD, string phase, CancellationToken ct = default)
    {
        if (!AccountNameRule.TryNormalize(accountName, out string normalized) || phase is not ("before" or "after") ||
            !Guid.TryParseExact(characterIdD, "D", out Guid characterId) || characterId == Guid.Empty ||
            !IPAddress.TryParse(host, out _) || port is < 1 or > 65535)
        {
            return new(false, "Stored mode requires an explicit account, non-empty Guid D character ID, " +
                "phase before/after, IP address and port 1..65535.");
        }
        string canonicalId = characterId.ToString("D");
        PersistenceCharacterProbe probe = new(normalized, canonicalId, phase);
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            string? failure = await probe.ObserveStoredAsync(host, port, deadline.Token);
            return failure is null
                ? new(true, $"Stored {phase}: listed and selected {canonicalId}; observed matching stored class and map entry.")
                : new(false, failure);
        }
        catch (OperationCanceledException)
        {
            return new(false, ct.IsCancellationRequested ? "Stored observation cancelled." : "Stored observation exceeded 30s.");
        }
        finally
        {
            probe.Disconnect();
            Console.WriteLine($"[Persistence Stored] utc={DateTime.UtcNow:O} phase={phase} account={normalized} " +
                $"characterId={canonicalId} event=close-requested");
        }
    }
}
