#nullable enable
using System;
using System.Collections.Generic;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 타인 entity placeholder. 본인 entity는 LocalPlayerMovement + PlayerPredictor가 담당.
    //
    // **보간 알고리즘** (clock smoothing + 지연 보간):
    //   목표: (A) 평상시 부드러움  (B) freeze(창 드래그) 봉합 — 두 속성 동시 달성.
    //
    //   [버퍼 적재] Snapshot.Time = serverTick * Constants.TickDuration (서버 시간축).
    //     freeze 후 N개 snapshot이 한 프레임에 몰려도 각자 고유 serverTick으로 펼쳐져 적재.
    //     → 이것이 freeze 봉합의 핵심. 벽시계 재도장 뭉침 제거.
    //
    //   [렌더 시계 _renderTime] Update마다 Time.deltaTime으로 연속 전진 → target이 프레임마다
    //     부드럽게 이동 (snapshot 도착 기준점 리셋 없음 → stutter 제거).
    //     targetRender = latestServerTime - InterpolationDelay 를 이상값으로 삼아:
    //       · 평상시 드리프트 → CatchupRate 비율로 부드럽게 흡수 (rubber-band).
    //       · freeze 후 큰 갭(> ResyncThreshold) → 즉시 snap 재동기.
    //
    //   [보간] _renderTime 위치를 buffer에서 둘러싼 두 snapshot 사이 선형 보간.
    //     · target이 최新보다 미래 → last-known 유지 (extrapolation 금지).
    //     · target이 最古보다 과거 → 最古 위치.
    [DisallowMultipleComponent]
    public class RemoteEntity : MonoBehaviour
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

        public int EntityId { get; private set; }

        readonly List<Snapshot> _buffer = new(capacity: 16);

        // 연속 렌더 시계 — Update마다 deltaTime으로 전진. snapshot 도착 기준점 리셋 없음.
        float _renderTime;
        bool _renderTimeInit;

        // Registry.Spawn에서 1회 호출. transform 즉시 박아 첫 frame 깜빡임 방지.
        public void Initialize(int entityId, float initialX, float initialY)
        {
            EntityId = entityId;
            transform.position = new Vector3(initialX, initialY, 0f);
            _buffer.Clear();
            _renderTimeInit = false;
        }

        // Registry.UpdateSnapshot에서 매 S_Snapshot 도착 시 호출 (main thread 큐 dispatch 안).
        // serverTick → 서버 시간축으로 버퍼에 적재 (벽시계 재도장 제거).
        public void EnqueueSnapshot(int serverTick, float x, float y)
        {
            float serverTime = serverTick * Constants.TickDuration;

            _buffer.Add(new Snapshot(serverTime, x, y));

            float cutoff = serverTime - BufferRetention;
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
            _renderTimeInit = false; // 다음 첫 snapshot에서 _renderTime 재동기.
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

            float latestServerTime = _buffer[_buffer.Count - 1].Time;
            float targetRender = latestServerTime - InterpolationDelay;

            if (!_renderTimeInit)
            {
                _renderTime = targetRender;
                _renderTimeInit = true;
            }
            else
            {
                _renderTime += Time.deltaTime; // 연속 전진 — snapshot 기준점 리셋 없음 → stutter 제거

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
