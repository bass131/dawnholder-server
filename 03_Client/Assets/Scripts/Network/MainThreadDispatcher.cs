using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// 워커 스레드에서 발생한 작업을 Unity main thread에서 실행하기 위한 큐.
    ///
    /// **사용법**
    /// <code>
    /// // 워커 스레드 (예: socket 콜백):
    /// MainThreadDispatcher.Enqueue(() => Debug.Log("hi"));
    ///
    /// // main thread: 이 컴포넌트의 Update()가 자동으로 큐를 drain.
    /// </code>
    ///
    /// **왜 필요한가**
    /// Unity의 GameObject / Transform / MonoBehaviour API는 main thread 전용.
    /// socket 콜백은 .NET 스레드풀의 워커 스레드에서 호출되므로 직접 접근 시
    /// <c>UnityException: ... can only be called from the main thread</c>.
    ///
    /// **설계 메모**
    /// - <c>_queue</c>는 static — MonoBehaviour 인스턴스가 여럿이어도 단일 큐.
    ///   단점은 씬 전환 시 잔존 작업 누수 가능. Phase 04는 단일 씬 시연이라 OK.
    /// - <c>ConcurrentQueue&lt;T&gt;</c> = lock-free (CAS 기반). 다수 producer
    ///   (워커 스레드들) + 단일 consumer (Update) 시나리오에 최적.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        /// <summary>워커 스레드 안전. 다음 main thread 프레임에 실행됨.</summary>
        public static void Enqueue(Action action)
        {
            if (action != null)
                _queue.Enqueue(action);
        }

        void Update()
        {
            // 한 프레임에 누적된 모든 작업을 drain.
            // 한 작업이 던져도 다음 작업 계속 진행 (try/catch로 격리).
            while (_queue.TryDequeue(out Action action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
