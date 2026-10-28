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
        if (!TryStoredInput(host, port, accountName, characterIdD, phase,
            out string normalized, out string canonicalId))
        {
            return new(false, "Stored mode requires an explicit account, non-empty Guid D character ID, " +
                "phase before/after, IP address and port 1..65535.");
        }
        PersistenceCharacterProbe probe = new(normalized, canonicalId, phase);
        string? failure = await ObserveStoredSessionAsync(probe, host, port, ct);
        return failure is null
            ? new(true, $"Stored {phase}: listed and selected {canonicalId}; " +
                "observed matching stored class and map entry.")
            : new(false, failure);
    }

    /// <summary>
    /// Before: session A lists the existing character, creates the new one in the lowest empty slot and enters with
    /// the existing ID; session B lists and enters with the created ID. After the operator's restart: the same two
    /// sessions without creating, given the ID that before printed as createdCharacterId. The first failure stops.
    /// </summary>
    public static async Task<PersistenceReconnectScenario.Result> RunStoredCreate(string host, int port,
        string accountName, string characterIdD, string newName, string newClass, string phase,
        string newCharacterIdD, CancellationToken ct = default)
    {
        bool before = phase == "before";
        string? newId = null;
        bool existingInput = TryStoredInput(host, port, accountName, characterIdD, phase,
            out string normalized, out string existingId);
        if (!existingInput || newName.Length is < 1 or > 36 || !TryClass(newClass, out CharacterClass newClassValue) ||
            (before ? newCharacterIdD.Length != 0 : !TryCanonicalId(newCharacterIdD, out newId) || newId == existingId))
        {
            return new(false, "Stored create mode requires an explicit account, non-empty Guid D character ID, " +
                "phase before/after, a new name of 1..36 UTF-16 units, new class Knight or Mage, IP address, " +
                "port 1..65535, and a different new character ID only in phase after.");
        }
        PersistenceCharacterProbe.NewCharacter newCharacter = new(newName, newClassValue);
        PersistenceCharacterProbe first = before
            ? new(normalized, existingId, phase, "A", [], newCharacter)
            : new(normalized, existingId, phase, "A", [new(newId!, newName, newClassValue)], create: null);
        string? failure = await ObserveStoredSessionAsync(first, host, port, ct);
        if (failure is not null) return new(false, $"Session A: {failure}");
        string createdId = before ? first.CreatedCharacterIdD! : newId!;

        // Same settle as PersistenceReconnectScenario: the server releases session A's account on a later tick.
        // A remaining AlreadyOnline is reported as session B's failure, never retried.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
        catch (OperationCanceledException)
        {
            return new(false, "Stored observation cancelled.");
        }
        PersistenceCharacterProbe second = new(normalized, createdId, phase, "B",
            [new(createdId, newName, newClassValue)], create: null);
        failure = await ObserveStoredSessionAsync(second, host, port, ct);
        if (failure is not null) return new(false, $"Session B: {failure}");
        return new(true, before
            ? $"Stored before: created {createdId} ({newClassValue}) beside {existingId}; both selected and entered."
            : $"Stored after: {existingId} and {createdId} ({newClassValue}) listed, selected and entered again.");
    }

    // One connection per probe: a 30s bound, the first failure wins, and the close is always requested and logged.
    static async Task<string?> ObserveStoredSessionAsync(PersistenceCharacterProbe probe, string host, int port,
        CancellationToken ct)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            return await probe.ObserveStoredAsync(host, port, deadline.Token);
        }
        catch (OperationCanceledException)
        {
            return ct.IsCancellationRequested ? "Stored observation cancelled." : "Stored observation exceeded 30s.";
        }
        finally
        {
            probe.CloseStored();
        }
    }

    static bool TryStoredInput(string host, int port, string accountName, string characterIdD, string phase,
        out string normalized, out string canonicalId)
    {
        canonicalId = "";
        return AccountNameRule.TryNormalize(accountName, out normalized) && phase is ("before" or "after") &&
            TryCanonicalId(characterIdD, out canonicalId) && IPAddress.TryParse(host, out _) &&
            port is >= 1 and <= 65535;
    }

    static bool TryCanonicalId(string text, out string canonicalId)
    {
        canonicalId = Guid.TryParseExact(text, "D", out Guid id) && id != Guid.Empty ? id.ToString("D") : "";
        return canonicalId.Length != 0;
    }

    // Class names only; numeric text such as "0" is not taken as a class.
    static bool TryClass(string text, out CharacterClass characterClass)
    {
        foreach (CharacterClass candidate in Enum.GetValues<CharacterClass>())
        {
            if (string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                characterClass = candidate;
                return true;
            }
        }
        characterClass = default;
        return false;
    }
}
