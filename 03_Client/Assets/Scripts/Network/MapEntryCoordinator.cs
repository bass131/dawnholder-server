using System;

namespace Dawnholder.Client.Network
{
    public enum MapEntryState { Waiting, Entering, Ready, Failed, Closed }

    /// <summary>Main-thread map entry state. It knows neither Unity scenes nor sockets.</summary>
    public sealed class MapEntryCoordinator
    {
        readonly Action<MapEntryCoordinator> _commit;
        readonly Action<int, int> _applyHp;
        readonly Action<Exception> _failed;
        readonly Action<MapEntryCoordinator> _finishBindings;
        bool _sceneReady;
        bool _playerReady;
        bool _registriesReady;
        bool _committing;

        public MapEntryCoordinator(Action<MapEntryCoordinator> commit, Action<int, int> applyHp,
            Action<Exception> failed, Action<MapEntryCoordinator> finishBindings = null)
        {
            _commit = commit ?? throw new ArgumentNullException(nameof(commit));
            _applyHp = applyHp ?? throw new ArgumentNullException(nameof(applyHp));
            _failed = failed ?? throw new ArgumentNullException(nameof(failed));
            _finishBindings = finishBindings;
        }

        public MapEntryState State { get; private set; }
        public long Epoch { get; private set; }
        public byte MapId { get; private set; }
        public float SpawnX { get; private set; }
        public float SpawnY { get; private set; }
        public ulong SceneHandle { get; private set; }
        public bool RequiresPlayer { get; private set; }
        public bool HasHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool SpawnApplied { get; private set; }
        public bool SceneReady => _sceneReady;
        public bool IsGameplayReady => State == MapEntryState.Ready && RequiresPlayer;
        public Exception LastError { get; private set; }

        public long Begin(byte mapId, float spawnX, float spawnY, bool requiresPlayer)
        {
            if (State == MapEntryState.Closed) throw new ObjectDisposedException(nameof(MapEntryCoordinator));
            Epoch++;
            MapId = mapId;
            SpawnX = spawnX;
            SpawnY = spawnY;
            RequiresPlayer = requiresPlayer;
            SceneHandle = 0;
            HasHp = SpawnApplied = _sceneReady = _playerReady = _registriesReady = false;
            CurrentHp = MaxHp = 0;
            LastError = null;
            State = MapEntryState.Entering;
            return Epoch;
        }

        public bool IsCurrent(long epoch) => epoch == Epoch &&
            (State == MapEntryState.Entering || State == MapEntryState.Ready);

        public void BindScene(long epoch, ulong sceneHandle)
        {
            if (!IsCurrent(epoch)) return;
            SceneHandle = sceneHandle;
            _sceneReady = true;
            TryCommit();
        }

        // The adapter calls this only after the player belongs to this scene and terrain is loaded.
        public void BindPlayer(long epoch)
        {
            if (!IsCurrent(epoch)) return;
            _playerReady = true;
            TryCommit();
        }

        public void BindRegistries(long epoch)
        {
            if (!IsCurrent(epoch)) return;
            _registriesReady = true;
            TryCommit();
        }

        public void ReceiveHp(long epoch, int current, int max)
        {
            if (!IsCurrent(epoch)) return;
            CurrentHp = current;
            MaxHp = max;
            HasHp = true;
            if (State == MapEntryState.Ready) _applyHp(current, max);
            else TryCommit();
        }

        void TryCommit()
        {
            if (State != MapEntryState.Entering || _committing || !_sceneReady) return;
            if (RequiresPlayer && (!_playerReady || !_registriesReady || !HasHp)) return;
            _committing = true;
            try
            {
                // A callback may begin and fully bind a new entry. Reevaluate it without recursion.
                while (State == MapEntryState.Entering && _sceneReady &&
                    (!RequiresPlayer || (_playerReady && _registriesReady && HasHp)))
                {
                    long epoch = Epoch;
                    try
                    {
                        _commit(this);
                        if (!IsCurrent(epoch)) continue;
                        SpawnApplied = RequiresPlayer;
                        if (HasHp) _applyHp(CurrentHp, MaxHp);
                        if (IsCurrent(epoch)) _finishBindings?.Invoke(this);
                        if (IsCurrent(epoch)) State = MapEntryState.Ready;
                    }
                    catch (Exception error) { Fail(epoch, error); }
                    if (Epoch == epoch) break;
                }
            }
            finally { _committing = false; }
        }

        public void Fail(long epoch, Exception error)
        {
            if (!IsCurrent(epoch)) return;
            State = MapEntryState.Failed;
            LastError = error;
            _failed(error);
        }

        public void Close()
        {
            if (State == MapEntryState.Closed) return;
            Epoch++;
            State = MapEntryState.Closed;
            HasHp = SpawnApplied = _sceneReady = _playerReady = _registriesReady = false;
            CurrentHp = MaxHp = 0;
            SceneHandle = 0;
            SpawnX = SpawnY = 0;
        }
    }
}
