#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // M3 Phase 05: 타인 entity placeholder. 본인 entity는 LocalPlayerController + PlayerPredictor가 담당.
    //
    // **시그니처 불변 약속** (정유현 Phase 08a 비주얼 교체 시 보존 — 영역 분리 핵심):
    //   - public int EntityId { get; }
    //   - public void Initialize(int entityId, float x, float y)
    //   - public void EnqueueSnapshot(float x, float y)
    //   - public void ClearBuffer()
    //   비주얼 컴포넌트(SpriteRenderer/Animator/Sprite swap)는 prefab Inspector에서
    //   *추가/교체 자유* — 본 컴포넌트는 시그니처만 보존하면 됨.
    //
    // **보간 알고리즘** (응급 모드 — 200ms 지연 보간):
    //   1. now = Time.realtimeSinceStartup (main thread)
    //   2. target = now - InterpolationDelay (= jitter 흡수 윈도우)
    //   3. buffer에서 target을 *둘러싼* 2개 snapshot 찾기 → 선형 보간
    //   4. target이 buffer 최신보다 미래 → last-known 유지 (extrapolation 안 함 — 응급 약속)
    //   5. target이 buffer 최古보다 과거 → 최古 snapshot 위치 (rare — buffer 비어가는 중)
    //
    // **timesource = Time.realtimeSinceStartup** (Phase 05 결정):
    //   서버 tick 기반 정밀 lag-comp는 M4+. 응급 모드는 클라 수신 시각 기반 jitter 흡수만.
    //   장점: 단순. 단점: RTT 변동에 영향 (jitter 흡수엔 충분, 정밀 lag-comp X).
    [DisallowMultipleComponent]
    public class RemoteEntity : MonoBehaviour
    {
        // 200ms 지연 — packet jitter 흡수 윈도우. 서버 SnapshotTickInterval(=5 × 50ms = 250ms)보다 짧게
        // 잡아 buffer 빔 빈도 낮춤 + 시각적 lag 체감 최소화. 정밀 값은 Phase 09 리허설에서 튜닝.
        const float InterpolationDelay = 0.2f;

        // 메모리 위생: receivedAt - BufferRetention 이전 항목 제거. 보간 윈도우(0.2s) + 여유(0.8s).
        const float BufferRetention = 1.0f;

        public int EntityId { get; private set; }

        readonly List<Snapshot> _buffer = new(capacity: 16);

        // Registry.Spawn에서 1회 호출. transform 즉시 박아 첫 frame 깜빡임 방지.
        public void Initialize(int entityId, float initialX, float initialY)
        {
            EntityId = entityId;
            transform.position = new Vector3(initialX, initialY, 0f);
            _buffer.Clear();
        }

        // Registry.UpdateSnapshot에서 매 S_Snapshot 도착 시 호출 (main thread 큐 dispatch 안).
        // 내부에서 Time.realtimeSinceStartup 박음 — caller 부담 X, 응급 모드 단순화.
        public void EnqueueSnapshot(float x, float y)
        {
            float now = Time.realtimeSinceStartup;
            _buffer.Add(new Snapshot(now, x, y));

            float cutoff = now - BufferRetention;
            int removeCount = 0;
            while (removeCount < _buffer.Count && _buffer[removeCount].Time < cutoff)
                removeCount++;
            if (removeCount > 0) _buffer.RemoveRange(0, removeCount);
        }

        // Registry.Despawn/Clear에서 호출 — 메모리 누수 차단 (Phase 정의 함정 #4).
        public void ClearBuffer() => _buffer.Clear();

        void Update()
        {
            if (_buffer.Count == 0) return; // last-known 유지 (transform 그대로)

            float target = Time.realtimeSinceStartup - InterpolationDelay;

            // target이 최古보다 과거 (또는 buffer 1개뿐) — 최古 위치 유지.
            if (_buffer.Count == 1 || target <= _buffer[0].Time)
            {
                Snapshot s = _buffer[0];
                transform.position = new Vector3(s.X, s.Y, 0f);
                return;
            }

            // target이 최新보다 미래 — extrapolation 안 함 (응급 모드 약속), last-known 유지.
            int last = _buffer.Count - 1;
            if (target >= _buffer[last].Time)
            {
                Snapshot s = _buffer[last];
                transform.position = new Vector3(s.X, s.Y, 0f);
                return;
            }

            // target을 둘러싼 2개 찾기 → 선형 보간. N≤4 정도라 O(N) 스캔 OK.
            for (int i = 0; i < last; i++)
            {
                Snapshot a = _buffer[i];
                Snapshot b = _buffer[i + 1];
                if (target >= a.Time && target <= b.Time)
                {
                    float span = b.Time - a.Time;
                    float t = span > 0.0001f ? (target - a.Time) / span : 0f;
                    float x = Mathf.Lerp(a.X, b.X, t);
                    float y = Mathf.Lerp(a.Y, b.Y, t);
                    transform.position = new Vector3(x, y, 0f);
                    return;
                }
            }
        }

        readonly struct Snapshot
        {
            public readonly float Time;
            public readonly float X;
            public readonly float Y;
            public Snapshot(float time, float x, float y)
            {
                Time = time;
                X = x;
                Y = y;
            }
        }
    }
}
