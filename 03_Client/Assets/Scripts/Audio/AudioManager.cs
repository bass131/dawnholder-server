#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.Audio
{
    // 키 기반 사운드 재생 인프라 (M7). 순수 표현 — 게임 로직/서버 무관 (헌법 #1).
    //
    // 자기-부트스트랩: PersistentServicesBootstrap과 동일한 RuntimeInitializeOnLoadMethod 패턴으로
    // 프리팹 편집 없이 코드만 1회 생성 + DontDestroyOnLoad → 씬 전환 가로질러 BGM 연속.
    //
    // 클립은 Resources/Audio/<key를 '/'로> 에서 로드. 클립이 없으면 경고 1회 후 no-op —
    // 미생성 키에 wiring돼도 예외 안 남 (M7 무인 생성 실패 안전장치).
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager? Instance { get; private set; }

        const int SfxPoolSize = 12;

        readonly Dictionary<string, AudioClip?> _clipCache = new();
        readonly Dictionary<string, float> _lastPlay = new();

        AudioSource[] _sfxPool = System.Array.Empty<AudioSource>();
        int _sfxIndex;
        AudioSource? _uiSource;
        AudioSource? _bgmA;
        AudioSource? _bgmB;
        bool _bgmBActive;           // 현재 들리는 BGM 소스 (false=A 들림, true=B 들림)
        string? _currentBgmKey;
        Coroutine? _bgmFade;

        float _masterVol = 0.8f;
        float _bgmVol = 0.7f;
        float _sfxVol = 0.9f;

        const string PrefMaster = "audio.vol.master";
        const string PrefBgm = "audio.vol.bgm";
        const string PrefSfx = "audio.vol.sfx";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<AudioManager>() != null) return;
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
            Object.DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _masterVol = PlayerPrefs.GetFloat(PrefMaster, _masterVol);
            _bgmVol = PlayerPrefs.GetFloat(PrefBgm, _bgmVol);
            _sfxVol = PlayerPrefs.GetFloat(PrefSfx, _sfxVol);

            _bgmA = NewSource("BGM_A", loop: true);
            _bgmB = NewSource("BGM_B", loop: true);
            _uiSource = NewSource("UI", loop: false);
            _sfxPool = new AudioSource[SfxPoolSize];
            for (int i = 0; i < SfxPoolSize; i++)
                _sfxPool[i] = NewSource($"SFX_{i}", loop: false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource NewSource(string label, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, worldPositionStays: false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;  // 2D
            return src;
        }

        // ── SFX ──────────────────────────────────────────────
        // minInterval>0 이면 키별 연타 스로틀 (피격/도트 폭주 방지).
        public void PlaySfx(string key, float volumeScale = 1f, float minInterval = 0f)
        {
            var clip = ResolveClip(key);
            if (clip == null) return;

            if (minInterval > 0f)
            {
                float now = Time.unscaledTime;
                if (_lastPlay.TryGetValue(key, out float last) && now - last < minInterval) return;
                _lastPlay[key] = now;
            }

            float vol = _sfxVol * _masterVol * volumeScale;
            AudioSource? src = key.StartsWith("ui.") ? _uiSource : NextSfxSource();
            src?.PlayOneShot(clip, vol);
        }

        AudioSource NextSfxSource()
        {
            var src = _sfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
            return src;
        }

        // ── BGM ──────────────────────────────────────────────
        public void PlayBgm(string key, float fadeSeconds = 1.0f)
        {
            if (key == _currentBgmKey) return;
            var clip = ResolveClip(key);
            if (clip == null) return;
            _currentBgmKey = key;

            AudioSource? next = _bgmBActive ? _bgmA : _bgmB;
            AudioSource? prev = _bgmBActive ? _bgmB : _bgmA;
            _bgmBActive = !_bgmBActive;
            if (next == null) return;

            next.clip = clip;
            next.volume = 0f;
            next.Play();

            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = StartCoroutine(CrossfadeBgm(prev, next, fadeSeconds));
        }

        public void StopBgm(float fadeSeconds = 0.5f)
        {
            _currentBgmKey = null;
            AudioSource? active = _bgmBActive ? _bgmB : _bgmA;
            if (_bgmFade != null) StopCoroutine(_bgmFade);
            if (active != null) _bgmFade = StartCoroutine(FadeOutStop(active, fadeSeconds));
        }

        IEnumerator CrossfadeBgm(AudioSource? prev, AudioSource next, float dur)
        {
            float target = BgmTargetVolume();
            float prevStart = prev != null ? prev.volume : 0f;
            if (dur <= 0f)
            {
                next.volume = target;
                if (prev != null) { prev.Stop(); prev.volume = 0f; }
                _bgmFade = null;
                yield break;
            }
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                next.volume = target * k;
                if (prev != null) prev.volume = prevStart * (1f - k);
                yield return null;
            }
            next.volume = target;
            if (prev != null) { prev.Stop(); prev.volume = 0f; }
            _bgmFade = null;
        }

        IEnumerator FadeOutStop(AudioSource src, float dur)
        {
            float start = src.volume;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                src.volume = start * (1f - Mathf.Clamp01(t / dur));
                yield return null;
            }
            src.Stop();
            src.volume = 0f;
            _bgmFade = null;
        }

        // ── Volume ───────────────────────────────────────────
        public float MasterVolume => _masterVol;
        public float BgmVolume => _bgmVol;
        public float SfxVolume => _sfxVol;

        public void SetMasterVolume(float v) { _masterVol = Mathf.Clamp01(v); Persist(PrefMaster, _masterVol); ApplyBgmVolume(); }
        public void SetBgmVolume(float v) { _bgmVol = Mathf.Clamp01(v); Persist(PrefBgm, _bgmVol); ApplyBgmVolume(); }
        public void SetSfxVolume(float v) { _sfxVol = Mathf.Clamp01(v); Persist(PrefSfx, _sfxVol); }

        void ApplyBgmVolume()
        {
            if (_bgmFade != null) return;  // 페이드 중이면 코루틴이 target 반영
            AudioSource? active = _bgmBActive ? _bgmB : _bgmA;
            if (active != null && active.isPlaying) active.volume = BgmTargetVolume();
        }

        // BGM 최종 재생 볼륨 = 마스터·BGM 슬라이더 × 키별 보정, [0,1] 클램프.
        float BgmTargetVolume() => Mathf.Min(1f, _bgmVol * _masterVol * BgmGain(_currentBgmKey));

        // 키별 음량 보정. 원본 OGG 4곡이 ending보다 RMS 15~22dB 낮아 소스 자체를 -1dBFS로 증폭(WAV 교체).
        // 이제 추가 부스트 불필요(1.0). hunting만 0.75로 한 단계 작게(영호 지시 4:3).
        static float BgmGain(string? key) => key switch
        {
            SoundKeys.BgmHunting => 0.75f,
            _ => 1f,
        };

        static void Persist(string pref, float v) { PlayerPrefs.SetFloat(pref, v); PlayerPrefs.Save(); }

        // ── Clip 해석 (Resources/Audio/<key>) ────────────────
        public AudioClip? ResolveClip(string key)
        {
            if (_clipCache.TryGetValue(key, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>(ResourcePath(key));
            if (clip == null)
                Debug.LogWarning($"[AudioManager] 사운드 클립 없음: '{key}' (Resources/{ResourcePath(key)}) — no-op. (M7 미생성/MISSING)");
            _clipCache[key] = clip;
            return clip;
        }

        public static string ResourcePath(string key) => "Audio/" + key.Replace('.', '/');
    }
}
