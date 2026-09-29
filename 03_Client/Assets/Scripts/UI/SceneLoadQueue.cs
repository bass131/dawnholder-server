using System;

namespace Dawnholder.Client.UI
{
    public enum SceneLoadStatus { Completed, Failed, Superseded, Canceled }

    public sealed class SceneLoadResult
    {
        public SceneLoadResult(long id, SceneLoadStatus status, ulong sceneHandle = 0, Exception error = null)
        { RequestId = id; Status = status; SceneHandle = sceneHandle; Error = error; }
        public long RequestId { get; }
        public SceneLoadStatus Status { get; }
        public ulong SceneHandle { get; }
        public Exception Error { get; }
    }

    public sealed class SceneLoadRequest
    {
        readonly Action<Exception> _reportObserverError;
        internal SceneLoadRequest(long id, string sceneName, Action<SceneLoadResult> completed, Action<Exception> reportObserverError)
        { Id = id; SceneName = sceneName; Callback = completed; _reportObserverError = reportObserverError; }
        internal Action<SceneLoadResult> Callback { get; private set; }
        SceneLoadResult _result;
        public long Id { get; }
        public string SceneName { get; }
        public bool IsFinished { get; internal set; }

        public void ObserveCompletion(Action<SceneLoadResult> callback)
        {
            if (_result != null) Notify(callback, _result);
            else Callback += callback;
        }

        internal Action PrepareFinish(SceneLoadResult result)
        {
            _result = result;
            var callback = Callback;
            Callback = null;
            return () => Notify(callback, result);
        }

        void Notify(Action<SceneLoadResult> callbacks, SceneLoadResult result)
        {
            if (callbacks == null) return;
            foreach (Action<SceneLoadResult> callback in callbacks.GetInvocationList())
            {
                try { callback(result); }
                catch (Exception error) { _reportObserverError(error); }
            }
        }
    }

    /// <summary>One physical operation and one latest pending request. Main-thread use only.</summary>
    public sealed class SceneLoadQueue
    {
        long _nextId;
        public SceneLoadRequest Active { get; private set; }
        public SceneLoadRequest Pending { get; private set; }
        public Exception LastObserverError { get; private set; }
        public event Action<Exception> ObserverFailed;

        public SceneLoadRequest Request(string sceneName, Action<SceneLoadResult> completed = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) throw new ArgumentException("Scene name is required.", nameof(sceneName));
            var request = new SceneLoadRequest(++_nextId, sceneName, completed, ReportObserverError);
            var previousPending = Pending;
            var previousActive = Active;
            Pending = request;
            // Finalize both old requests before observers can start/finish another physical slot.
            Action notifyPending = PrepareFinish(previousPending, SceneLoadStatus.Superseded);
            Action notifyActive = PrepareFinish(previousActive, SceneLoadStatus.Superseded);
            notifyPending?.Invoke();
            notifyActive?.Invoke();
            return request;
        }

        public bool TryStartNext(out SceneLoadRequest request)
        {
            request = null;
            if (Active != null || Pending == null) return false;
            request = Pending;
            Pending = null;
            Active = request;
            return true;
        }

        public bool Cancel(long requestId)
        {
            if (Pending != null && Pending.Id == requestId)
            {
                var request = Pending;
                Pending = null;
                PrepareFinish(request, SceneLoadStatus.Canceled)?.Invoke();
                return true;
            }
            if (Active != null && Active.Id == requestId)
            {
                PrepareFinish(Active, SceneLoadStatus.Canceled)?.Invoke();
                return true; // Keep the physical slot until its operation has actually completed.
            }
            return false;
        }

        public bool Complete(long requestId, SceneLoadStatus status, ulong sceneHandle = 0, Exception error = null)
        {
            if (status != SceneLoadStatus.Completed && status != SceneLoadStatus.Failed)
                throw new ArgumentException("Physical completion must be Completed or Failed.", nameof(status));
            if (Active == null || Active.Id != requestId) return false;
            var request = Active;
            Active = null;
            PrepareFinish(request, status, sceneHandle, error)?.Invoke();
            return true;
        }

        public void CancelAll()
        {
            var pending = Pending;
            var active = Active;
            Pending = null;
            Action notifyPending = PrepareFinish(pending, SceneLoadStatus.Canceled);
            Action notifyActive = PrepareFinish(active, SceneLoadStatus.Canceled);
            notifyPending?.Invoke();
            notifyActive?.Invoke();
        }

        static Action PrepareFinish(SceneLoadRequest request, SceneLoadStatus status, ulong handle = 0, Exception error = null)
        {
            if (request == null || request.IsFinished) return null;
            request.IsFinished = true;
            return request.PrepareFinish(new SceneLoadResult(request.Id, status, handle, error));
        }

        void ReportObserverError(Exception error)
        {
            LastObserverError = error;
            if (ObserverFailed == null) return;
            foreach (Action<Exception> report in ObserverFailed.GetInvocationList())
            {
                try { report(error); }
                catch (Exception observerError) { LastObserverError = new AggregateException(error, observerError); }
            }
        }
    }
}
