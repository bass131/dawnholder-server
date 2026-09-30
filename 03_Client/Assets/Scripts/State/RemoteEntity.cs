#nullable enable
using System;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 원격 플레이어·적의 Unity 적용과 텔레포트 도착 callback을 소유한다.
    [DisallowMultipleComponent]
    public class RemoteEntity : MonoBehaviour
    {
        readonly RemoteInterpolationState _interpolation = new();
        Action? _teleportArriveCallback;

        public int EntityId { get; private set; }

        // 첫 프레임 위치는 즉시 적용하고 다음 snapshot에서 렌더 시계를 초기화한다.
        public void Initialize(int entityId, float initialX, float initialY)
        {
            EntityId = entityId;
            transform.position = new Vector3(initialX, initialY, 0f);
            _interpolation.Reset();
        }

        public void EnqueueSnapshot(int serverTick, float x, float y)
        {
            _interpolation.EnqueueSnapshot(serverTick, x, y);

            // 적재 직후 1회 호출한다. 다음 Update의 Transform 적용을 기다리지 않는다.
            if (_teleportArriveCallback != null)
            {
                Action cb = _teleportArriveCallback;
                _teleportArriveCallback = null;
                cb();
            }
        }

        // buffer만 비운다. 렌더 시계와 callback은 유지한다.
        public void ClearBuffer() => _interpolation.ClearBuffer();

        // 다음 snapshot까지 현재 Transform을 유지하고 buffer/clock만 초기화한다.
        public void SnapInterpolation() => _interpolation.Reset();

        // 다음 시전은 기존 callback을 덮어쓴다.
        public void SetTeleportArriveCallback(Action? callback)
        {
            _teleportArriveCallback = callback;
        }

        void Update()
        {
            if (_interpolation.TryAdvance(Time.deltaTime, out Vector2 position))
            {
                transform.position = new Vector3(position.x, position.y, 0f);
            }
        }
    }
}
