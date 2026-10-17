using Shared.Protocol;
using Shared.GameData;

namespace Dawnholder.Tools.HeadlessBot.Scenarios.Persistence;

/// <summary>Observes public admission and packet order only; never logs store authority or credentials.</summary>
sealed class PersistenceCharacterProbe : ProbeBase
{
    readonly string _name;
    readonly CharacterClass _intent;
    bool _enteredWithoutAuthority;

    internal PersistenceCharacterProbe(string accountName, CharacterClass intent)
    {
        _name = accountName;
        _intent = intent;
    }

    protected override string AccountName => _name;
    protected override CharacterClass SelectedClass => _intent;

    internal async Task<string?> ObserveMageAsync(string host, int port, CancellationToken cancellationToken)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);
        Connect(host, port);
        if (!WaitConnected(timeout) || !WaitHandshake(timeout) || !HandshakeOk)
            return "Connection or handshake did not complete.";
        if (!WaitLogin(timeout) || LoginResult != Shared.GameData.LoginResult.Accepted)
            return "Account login was not accepted.";
        if (!await WaitUntil(() => Session?.CharacterLoad.HasValue == true, timeout, cancellationToken))
            return "No valid character load result was observed.";
        var load = Session!.CharacterLoad!.Value;
        if (load.Status != CharacterLoadStatus.Success)
            return $"Admission refused: {load.Status}.";
        if (load.LoadedClass != CharacterClass.Mage)
            return "The server did not return the stored Mage class.";
        if (!WaitEnterMap(timeout) || _enteredWithoutAuthority)
            return "EnterMap did not follow a valid authority result.";
        return null;
    }

    protected override void OnEnterMap(S_EnterMap packet)
    {
        var load = Session?.CharacterLoad;
        _enteredWithoutAuthority = load is null || load.Value.Status != CharacterLoadStatus.Success;
    }
}
