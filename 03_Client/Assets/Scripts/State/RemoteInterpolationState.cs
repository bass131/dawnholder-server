#nullable enable
using System.Collections.Generic;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // snapshot buffer와 서버 시간축의 렌더 시계는 이 객체가 단독 소유한다.
    internal sealed class RemoteInterpolationState
    {
        // 150ms 지연 — packet jitter 흡수 윈도우. 서버 broadcast 간격(100ms)보다 살짝 길게
        // 잡아 buffer 1~2개 항상 풍부 + 보간 자연. 너무 짧으면 buffer 매번 빔 → 정지 패턴 결함.
        const float InterpolationDelay = 0.15f;

        // 메모리 위생: 서버 시각 기준 BufferRetention 이전 항목 제거.
        const float BufferRetention = 1.0f;

        // freeze 후 _renderTime과 targetRender 갭이 이 값 이상이면 즉시 snap 재동기.
        // InterpolationDelay(0.15)의 ~3배 — 창 드래그·일시 정지 복귀 감지 임계.
        const float ResyncThreshold = 0.5f;

        // 평상시 드리프트를 프레임당 이 비율만큼 흡수 (rubber-band). 0.1 = 10%/frame.
        const float CatchupRate = 0.1f;

        readonly List<Snapshot> _buffer = new(capacity: 16);
        float _renderTime;
        bool _renderTimeInit;

        internal void Reset()
        {
            _buffer.Clear();
            _renderTimeInit = false;
        }

        internal void ClearBuffer() => _buffer.Clear();

        internal void EnqueueSnapshot(int serverTick, float x, float y)
        {
            // freeze 후 한 프레임에 몰린 snapshot도 각자의 serverTick으로 시간축에 펼친다.
            // 수신 벽시계로 다시 찍으면 서로 다른 시점이 뭉쳐 보간 간격을 잃는다.
            float serverTime = serverTick * Constants.TickDuration;

            _buffer.Add(new Snapshot(serverTime, x, y));

            float cutoff = serverTime - BufferRetention;
            int removeCount = 0;
            while (removeCount < _buffer.Count && _buffer[removeCount].Time < cutoff)
                removeCount++;
            if (removeCount > 0) _buffer.RemoveRange(0, removeCount);

        }

        internal bool TryAdvance(float deltaTime, out Vector2 position)
        {
            position = default;
            if (_buffer.Count == 0) return false; // last-known 유지 (transform 그대로)

            float latestServerTime = _buffer[_buffer.Count - 1].Time;
            float targetRender = latestServerTime - InterpolationDelay;

            if (!_renderTimeInit)
            {
                _renderTime = targetRender;
                _renderTimeInit = true;
            }
            else
            {
                _renderTime += deltaTime; // 연속 전진 — snapshot 기준점 리셋 없음 → stutter 제거

                float drift = targetRender - _renderTime;
                if (Mathf.Abs(drift) > ResyncThreshold)
                    _renderTime = targetRender;          // freeze 복귀 등 큰 갭 → 즉시 snap
                else
                    _renderTime += drift * CatchupRate;  // 평상시 드리프트 부드럽게 흡수
            }

            float target = _renderTime;

            // target이 최古보다 과거 (또는 buffer 1개뿐) — 최古 위치 유지.
            if (_buffer.Count == 1 || target <= _buffer[0].Time)
            {
                Snapshot s = _buffer[0];
                position = new Vector2(s.X, s.Y);
                return true;
            }

            // target이 최新보다 미래 — extrapolation 안 함, last-known 유지.
            int last = _buffer.Count - 1;
            if (target >= _buffer[last].Time)
            {
                Snapshot s = _buffer[last];
                position = new Vector2(s.X, s.Y);
                return true;
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
                    position = new Vector2(x, y);
                    return true;
                }
            }
            return false;
        }

        readonly struct Snapshot
        {
            // Time = serverTick * Constants.TickDuration (서버 시간축 — 벽시계 아님).
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
