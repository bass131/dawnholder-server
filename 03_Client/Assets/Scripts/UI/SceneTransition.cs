using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Dawnholder.Client.UI
{
    /// <summary>Unity fade/load adapter. SceneLoadQueue owns request ordering and completion.</summary>
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [Header("Fade")]
        [Tooltip("화면 검은 천 — CanvasGroup α 0↔1로 토글.")]
        [FormerlySerializedAs("fadeGroup")]
        [SerializeField] CanvasGroup _fadeGroup;
        [Tooltip("페이드 한 방향 시간 (초). 0.3~0.5 권장.")]
        [FormerlySerializedAs("fadeDuration")]
        [SerializeField] float _fadeDuration = 0.5f;

        readonly SceneLoadQueue _loads = new SceneLoadQueue();
        bool _running;
        Coroutine _respawnFade;
        public bool IsTransitioning => _running;
        public SceneLoadRequest ActiveRequest => _loads.Active;

        public static SceneTransition EnsureInstance()
        {
            if (Instance == null)
            {
                Debug.LogWarning("[SceneTransition] Missing service; using a runtime loader without fade.");
                new GameObject("SceneTransitionFallback").AddComponent<SceneTransition>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _loads.ObserverFailed += error => Debug.LogException(error);
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            RestoreOverlay();
        }

        public void LoadScene(string sceneName) => RequestScene(sceneName);

        public SceneLoadRequest RequestScene(string sceneName, Action<SceneLoadResult> completed = null)
        {
            var request = _loads.Request(sceneName, result =>
            {
                if (result.Status == SceneLoadStatus.Failed)
                    Debug.LogError($"[SceneTransition] '{sceneName}' failed: {result.Error}");
                try { completed?.Invoke(result); }
                catch (Exception error) { Debug.LogException(error); }
            });
            if (_respawnFade != null)
            {
                StopCoroutine(_respawnFade);
                _respawnFade = null;
            }
            if (!_running)
            {
                _running = true;
                StartCoroutine(LoadScenes());
            }
            return request;
        }

        public bool CancelRequest(long requestId) => _loads.Cancel(requestId);

        IEnumerator LoadScenes()
        {
            try
            {
                while (_loads.TryStartNext(out SceneLoadRequest request))
                {
                    if (_fadeGroup != null) _fadeGroup.blocksRaycasts = true;
                    yield return Fade(_fadeGroup != null ? _fadeGroup.alpha : 0f, 1f, _fadeDuration);
                    AsyncOperation operation = null;
                    Exception failure = null;
                    ulong sceneHandle = 0;
                    if (!request.IsFinished)
                    {
                        try
                        {
                            operation = SceneManager.LoadSceneAsync(request.SceneName, LoadSceneMode.Single);
                            if (operation == null) failure = new InvalidOperationException("LoadSceneAsync returned null.");
                        }
                        catch (Exception error) { failure = error; }
                    }
                    // Cancellation suppresses application; it cannot roll back a started Unity load.
                    if (operation != null)
                    {
                        while (!operation.isDone) yield return null;
                        Scene scene = SceneManager.GetSceneByName(request.SceneName);
                        if (scene.IsValid() && scene.isLoaded) sceneHandle = scene.handle.GetRawData();
                        else failure = new InvalidOperationException("Loaded scene was not available for binding.");
                    }
                    // Bind the loaded scene while covered, before revealing its initial state.
                    _loads.Complete(request.Id, failure == null ? SceneLoadStatus.Completed : SceneLoadStatus.Failed,
                        sceneHandle, failure);
                    if (_loads.Pending == null)
                    {
                        yield return Fade(_fadeGroup != null ? _fadeGroup.alpha : 1f, 0f, _fadeDuration);
                        RestoreOverlay();
                    }
                }
            }
            finally
            {
                _running = false;
                RestoreOverlay();
            }
        }

        // Respawn remains a visual-only fade; an authoritative scene request takes precedence.
        public bool PlayRespawnFade(Action onCovered = null)
        {
            if (_running || _respawnFade != null || _fadeGroup == null) return false;
            _respawnFade = StartCoroutine(RespawnFadeRoutine(onCovered));
            return true;
        }

        IEnumerator RespawnFadeRoutine(Action onCovered)
        {
            try
            {
                _fadeGroup.blocksRaycasts = true;
                yield return Fade(0f, 1f, _fadeDuration);
                onCovered?.Invoke();
                yield return Fade(1f, 0f, _fadeDuration);
            }
            finally { RestoreOverlay(); _respawnFade = null; }
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            if (_fadeGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration && _fadeGroup != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (_fadeGroup != null) _fadeGroup.alpha = to;
        }

        void RestoreOverlay()
        {
            if (_fadeGroup == null) return;
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _loads.CancelAll();
            RestoreOverlay();
        }
    }
}
