using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 씬 전환 시 검은 페이드 인/아웃. Singleton + DontDestroyOnLoad.
    /// 모든 씬 전환 호출은 SceneTransition.Instance.LoadScene(...)로 일원화.
    ///
    /// **헌법 #1 (Server Authority)**: 페이드는 *본인 클라 시각* 효과만.
    /// 서버 권위 타임라인엔 영향 X (멀티게임 시 다른 플레이어는 보지 못함).
    ///
    /// **timeScale=0 안전망**: PauseMenuController에서 timeScale=0인 채 호출될 위험을
    /// 대비해 Fade Coroutine은 Time.unscaledDeltaTime을 사용. PauseMenuController가
    /// timeScale=1 복원 먼저 하지만 안전망 이중.
    /// </summary>
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

        bool isTransitioning;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // PersistentServices 프리팹의 *자식*(FadeCanvas)으로 들어가면 영속화는 루트가 담당
            // (PersistentServicesBootstrap이 루트를 DontDestroyOnLoad). 자식에 DDOL 호출은
            // "only works for root GameObjects" 경고만 내고 무의미하므로 루트일 때만 호출.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = 0f;
                _fadeGroup.blocksRaycasts = false;
            }
        }

        public void LoadScene(string sceneName)
        {
            if (isTransitioning) return;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        IEnumerator LoadSceneRoutine(string sceneName)
        {
            // 방어: _fadeGroup Inspector 슬롯이 비었으면 페이드 스킵 + 즉시 로드 (검은 화면 멈춤 방지)
            if (_fadeGroup == null)
            {
                Debug.LogError("[SceneTransition] _fadeGroup is NULL — Inspector slot empty. Skipping fade.");
                SceneManager.LoadScene(sceneName);
                isTransitioning = false;
                yield break;
            }

            isTransitioning = true;
            _fadeGroup.blocksRaycasts = true;

            yield return Fade(0f, 1f, _fadeDuration);

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                // 잘못된 씬 이름 / Build Settings 미등록 — 검은 화면 멈춤 방지 위해 페이드 인으로 복구
                Debug.LogError($"[SceneTransition] LoadSceneAsync returned null — '{sceneName}' not in Build Settings?");
            }
            else
            {
                while (!op.isDone)
                    yield return null;
            }

            yield return Fade(1f, 0f, _fadeDuration);

            _fadeGroup.blocksRaycasts = false;
            isTransitioning = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _fadeGroup.alpha = to;
        }
    }
}
