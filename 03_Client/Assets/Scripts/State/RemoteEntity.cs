#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 타인 entity placeholder. 본인 entity는 LocalPlayerMovement + PlayerPredictor가 담당.
    //
    // **보간 알고리즘** (지연 보간):
    //   1. now = Time.realtimeSinceStartup (main thread)
    //   2. target = now - InterpolationDelay (= jitter 흡수 윈도우)
    //   3. buffer에서 target을 *둘러싼* 2개 snapshot 찾기 → 선형 보간
    //   4. target이 buffer 최신보다 미래 → last-known 유지 (extrapolation 안 함)
    //   5. target이 buffer 최古보다 과거 → 최古 snapshot 위치 (rare — buffer 비어가는 중)
    //
    // **timesource = Time.realtimeSinceStartup**: 서버 tick 기반 정밀 lag-comp가 아니라
    //   클라 수신 시각 기반 jitter 흡수. 단순하지만 RTT 변동에 영향 (jitter 흡수엔 충분).
    [DisallowMultipleComponent]
    public class RemoteEntity : MonoBehaviour
    {
        // 150ms 지연 — packet jitter 흡수 윈도우. 서버 broadcast 간격(100ms)보다 살짝 길게
        // 잡아 buffer 1~2개 항상 풍부 + 보간 자연. 너무 짧으면 buffer 매번 빔 → 정지 패턴 결함.
        const float InterpolationDelay = 0.15f;

        // 메모리 위생: receivedAt - BufferRetention 이전 항목 제거. 보간 윈도우 + 여유.
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
        // 내부에서 Time.realtimeSinceStartup 박음 — caller 부담 X.
        public void EnqueueSnapshot(float x, float y)
        {
            float now = Time.realtimeSinceStartup;
            _buffer.Add(new Snapshot(now, x, y));

            float cutoff = now - BufferRetention;
            int removeCount = 0;
            while (removeCount < _buffer.Count && _buffer[removeCount].Time < cutoff)
                removeCount++;
            if (removeCount > 0) _buffer.RemoveRange(0, removeCount);

            // 텔레포트 도착 이펙트 — SnapInterpolation 후 첫 새 위치 snapshot이 여기서 확정.
            if (_teleportArriveCallback != null)
            {
                Action cb = _teleportArriveCallback;
                _teleportArriveCallback = null;
                cb();
            }
        }

        // Registry.Despawn/Clear에서 호출 — 메모리 누수 차단.
        public void ClearBuffer() => _buffer.Clear();

        // Teleport 보간 끊기 — S_SkillCast(Teleport) 수신 시 호출.
        // 이전 스냅샷(구 위치)을 모두 버리고, 다음 S_Snapshot(새 위치)을 기다린다.
        // 버리지 않으면 Update가 구 위치→새 위치를 보간으로 미끄러뜨려 순간이동이 슬라이드로 뭉개진다.
        public void SnapInterpolation()
        {
            _buffer.Clear();
            // transform은 현재 위치 유지(마지막 렌더 위치) — 다음 snapshot 도착 전 teleport 진행 중 상태.
        }

        // 텔레포트 도착 이펙트 콜백 — SnapInterpolation 후 첫 EnqueueSnapshot(새 위치 확정) 시 1회 발동.
        // 다음 시전 시 덮어쓰기 — 스냅샷 미도착/despawn race 시 영구 잔류해도 무해.
        Action? _teleportArriveCallback;

        // S_SkillCast(Teleport) 수신 시 SnapInterpolation과 함께 호출 — 도착 이펙트 등록.
        public void SetTeleportArriveCallback(Action? callback)
        {
            _teleportArriveCallback = callback;
        }

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

            // target이 최新보다 미래 — extrapolation 안 함, last-known 유지.
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
