using System;
using System.Collections.Generic;
using System.Net;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// PersistentServices의 연결 facade. GameEntryPoint가 명시적으로 접속하고,
    /// 맵 이동 중 연결은 유지하며 메뉴 복귀/종료 때 현재 수명을 정리한다.
    /// 연결 상태와 generation은 ClientConnectionLifetime 한 곳에서 소유한다.
    /// </summary>
    public class NetworkService : MonoBehaviour
    {
        public static NetworkService Instance { get; private set; }

        [FormerlySerializedAs("serverHost")]
        [SerializeField] string _serverHost = "127.0.0.1";
        [FormerlySerializedAs("serverPort")]
        [SerializeField] int _serverPort = 7777;
        [FormerlySerializedAs("pingIntervalSeconds")]
        [SerializeField] float _pingIntervalSeconds = 1.0f;

        const string ServerHostPrefsKey = "ServerHost";
        const int ClassPrefsInvalid = -1;
        ClientConnectionLifetime _lifetime;
        float _accumSec;
        Exception _reportedError;

        // TCP/session 활성 상태이며, 씬 준비 완료를 의미하지 않는다.
        public bool IsConnected => _lifetime != null && _lifetime.IsConnected;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[NetworkService] 중복 서비스 파괴.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            var connector = new Connector();
            _lifetime = new ClientConnectionLifetime(connector.BeginConnect,
                MainThreadDispatcher.Enqueue, current => new UnityClientSession(current), ResetSessionMirrors);
        }

        /// <summary>현재 시도가 없을 때 endpoint/class를 고정하고 연결을 시작한다.</summary>
        public void Connect(string hostOverride = null, int characterClassOverride = ClassPrefsInvalid)
        {
            if (_lifetime == null || _lifetime.State != ClientConnectionState.Disconnected) return;

            int classValue = characterClassOverride != ClassPrefsInvalid
                ? characterClassOverride
                : ClassLoadout.GetSelectedClassValue(ClassPrefsInvalid);
            if (!IsValidClassValue(classValue))
            {
                Debug.LogWarning("[NetworkService] 캐릭터 선택값이 없어 MainMenu로 복귀합니다.");
                ReturnToMainMenu();
                return;
            }

            string host = hostOverride ?? PlayerPrefs.GetString(ServerHostPrefsKey, _serverHost);
            if (string.IsNullOrWhiteSpace(host)) host = _serverHost;
            if (!IPAddress.TryParse(host, out IPAddress ip) || _serverPort < 1 || _serverPort > 65535)
            {
                Debug.LogWarning($"[NetworkService] 잘못된 서버 주소 '{host}:{_serverPort}' — MainMenu 복귀.");
                ReturnToMainMenu();
                return;
            }

            var endPoint = new IPEndPoint(ip, _serverPort);
            _reportedError = null;
            _accumSec = 0f;
            if (_lifetime.Connect(endPoint, (byte)classValue))
                Debug.Log($"[NetworkService] Connect 시도 → {endPoint} (class={classValue})");
        }

        public void Disconnect() => _lifetime?.Disconnect();

        void ResetSessionMirrors()
        {
            _accumSec = 0f;
            ResetGlobalSessionMirrors();
        }

        // 서버의 상태는 변경하지 않는다. 끝난 연결의 client mirror만 비운다.
        public static void ResetGlobalSessionMirrors()
        {
            var errors = new List<Exception>();
            void Reset(Action reset)
            {
                try { reset(); }
                catch (Exception error) { errors.Add(error); }
            }
            var party = PartyState.Instance;
            var quest = QuestState.Instance;
            Reset(() => LocalPlayerMovement.Instance?.SuspendForMapEntry());
            Reset(() => party?.ResetSessionValues());
            Reset(() => quest?.ResetSessionValues());
            Reset(() => RemoteEntityRegistry.Instance?.Clear());
            Reset(() => EnemyRegistry.Instance?.Clear());
            // Every mirror value is cleared before any Party/Quest subscriber is notified.
            Reset(() => party?.NotifySessionReset());
            Reset(() => quest?.NotifySessionReset());
            if (errors.Count != 0) throw new AggregateException("Session mirror cleanup failed.", errors);
        }

        static bool IsValidClassValue(int classValue) =>
            classValue == (int)CharacterClass.Knight || classValue == (int)CharacterClass.Mage;

        void ReturnToMainMenu()
        {
            Disconnect();
            if (SceneTransition.Instance != null) SceneTransition.Instance.LoadScene("MainMenu");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        void Update()
        {
            if (_lifetime == null) return;
            if (_lifetime.LastError != null && !ReferenceEquals(_reportedError, _lifetime.LastError))
            {
                _reportedError = _lifetime.LastError;
                Debug.LogWarning($"[NetworkService] 연결 종료 오류: {_reportedError.Message}");
            }
            if (!IsConnected || !(_lifetime.CurrentSession is UnityClientSession session)) return;
            _accumSec += Time.deltaTime;
            if (_accumSec < _pingIntervalSeconds) return;
            _accumSec = 0f;
            session.Send(new C_Ping { clientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }.Write());
        }

        void OnApplicationQuit() => _lifetime?.Dispose();

        void OnDestroy()
        {
            _lifetime?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
