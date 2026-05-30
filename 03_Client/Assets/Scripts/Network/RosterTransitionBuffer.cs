using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Network
{
    // 맵 전환 중 roster 패킷 버퍼링.
    //
    // **문제**: HandleMapTransition은 SceneTransition.LoadScene(페이드 코루틴) 시작 후
    //   즉시 return. 서버는 S_MapTransition 직후 즉시 S_PlayerJoin/S_EntitySpawn/S_Snapshot
    //   전송. 그 사이 클라는 *옛 씬*에 있어 roster 패킷이 옛 씬 레지스트리에 박힘 →
    //   LoadScene(Single)로 옛 씬 destroy → 새 씬에 enemy/remote player 없음.
    //
    // **해결**: 전환 중 roster 패킷을 버퍼에 캐싱 → 새 씬 sceneLoaded 콜백에서 drain.
    //   - BeginTransition에서 _pendingMapTransition = true + 목적 씬 이름 보관.
    //   - TryBuffer로 진입 직후 공통 overflow 가드 1곳에서 검사.
    //   - sceneLoaded 콜백에서 씬 이름 매치 시 _pendingMapTransition = false + drain.
    //   - main thread에서만 접근하므로 lock 불필요.
    internal sealed class RosterTransitionBuffer
    {
        // overflow 가드 상한 — 비정상 상황(서버가 전환 완료 전 수백 패킷 폭주) 방어.
        const int MaxSize = 100;

        // 맵 전환 진행 중 플래그 (main thread 전용).
        bool _pendingMapTransition;

        // 전환 목적 씬 이름 — sceneLoaded에서 매치 기준.
        string _pendingDestSceneName = string.Empty;

        // 전환 중 도착한 roster 패킷의 재실행 Action 목록.
        // Action 패턴: 패킷 파싱은 socket 워커에서 이미 완료 → main thread에서 registry에 적용만.
        readonly List<Action> _buffer = new();

        /// <summary>sceneLoaded 콜백 등록. 생성자 호출 측(UnityClientSession)이 1회 new.</summary>
        public RosterTransitionBuffer()
        {
            SceneManager.sceneLoaded += OnSceneLoadedForRosterDrain;
        }

        /// <summary>맵 전환 시작. 이 시점부터 TryBuffer가 패킷을 캐싱.</summary>
        /// <param name="destSceneName">전환 목적 씬 이름 (Build Settings 파일명).</param>
        public void BeginTransition(string destSceneName)
        {
            if (_pendingMapTransition)
            {
                Debug.LogWarning($"[Unity] 이전 맵 전환 roster buffer 미drain 상태에서 새 MapTransition 도착 — buffer 초기화 후 재시작.");
                _buffer.Clear();
            }
            _pendingMapTransition = true;
            _pendingDestSceneName = destSceneName;
        }

        /// <summary>현재 전환 중인지 여부.</summary>
        public bool IsPending => _pendingMapTransition;

        /// <summary>
        /// roster 패킷 Action을 버퍼에 추가 시도 (진입 직후 공통 overflow 가드 1곳).
        /// <para>전환 중 + 여유 있으면 버퍼에 추가하고 <c>true</c> 반환.</para>
        /// <para>전환 중 아니거나 overflow이면 <c>false</c> 반환 → 호출처가 즉시 처리.</para>
        /// </summary>
        /// <param name="packetLabel">overflow 경고 로그용 패킷 설명 (예: "S_Snapshot entity=5").</param>
        /// <param name="action">새 씬에서 실행할 registry 적용 Action.</param>
        public bool TryBuffer(string packetLabel, Action action)
        {
            if (!_pendingMapTransition) return false;

            if (_buffer.Count >= MaxSize)
            {
                Debug.LogWarning($"[Unity] RosterBuffer overflow (>{MaxSize}) — {packetLabel} dropped.");
                return true; // overflow지만 "전환 중"이므로 drop(즉시 처리 X)
            }

            _buffer.Add(action);
            return true;
        }

        /// <summary>
        /// disconnect/teardown 시 sceneLoaded 구독 해제.
        /// UnityClientSession.OnDisconnected(main thread dispatch 안)에서 호출.
        /// 재연결 시 new RosterTransitionBuffer()로 재생성 — 재구독 필요 없음.
        /// </summary>
        public void Teardown()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForRosterDrain;
            _pendingMapTransition = false;
            _buffer.Clear();
        }

        // 새 씬 로드 완료 시 호출 (main thread — Unity 보장).
        // 목적 씬 이름 매치 시 buffer drain 후 플래그 해제.
        void OnSceneLoadedForRosterDrain(Scene scene, LoadSceneMode mode)
        {
            if (!_pendingMapTransition) return;
            if (scene.name != _pendingDestSceneName) return;

            _pendingMapTransition = false;
            _pendingDestSceneName = string.Empty;

            Debug.Log($"[Unity] RosterBuffer drain: {_buffer.Count}개 패킷 재실행 (씬='{scene.name}')");
            foreach (Action action in _buffer)
                action();
            _buffer.Clear();
        }
    }
}
