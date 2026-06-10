using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 한 장의 배경 레이어를 카메라 이동에 맞춰 패럴랙스 스크롤시킨다.
    //
    // 동작 개요:
    //   - 카메라가 X로 dx만큼 이동하면, 이 레이어는 dx * (1 - parallaxFactor)만큼만 따라간다.
    //     → factor가 0에 가까울수록(원경) 거의 안 움직이고, 1에 가까울수록(근경) 카메라와 함께 움직인다.
    //   - SpriteRenderer를 Tiled 드로우 모드로 두고 가로로 충분히 넓게(size.x) 깔아두면,
    //     레이어가 카메라보다 뒤처져도 화면 양쪽이 항상 타일로 채워진다.
    //   - 누적 오차/부동소수 문제를 막기 위해, 레이어가 카메라에서 타일 1칸 이상 벗어나면
    //     정확히 타일 폭만큼 스냅한다. 타일 폭의 정수배 이동은 텍스처 위상이 동일하므로
    //     화면상으로는 끊김 없이 무한 반복된다.
    //
    // LateUpdate 이유:
    //   CameraFollow가 LateUpdate에서 카메라를 움직이므로, 그 뒤에 읽어야 한 프레임 밀림이 없다.
    //   (실행 순서가 보장되지 않을 수 있어, 카메라의 "이번 프레임 위치"를 매 프레임 직접 읽는다.)
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Tooltip("따라갈 카메라. 비우면 Camera.main을 자동 사용")]
        Transform _cameraTransform;

        [SerializeField, Range(0f, 1.5f), Tooltip("0=원경(거의 정지) ~ 1=지면(월드 고정) ~ >1=플레이어보다 앞(더 빠름)")]
        float _parallaxFactor = 0.3f;

        [SerializeField, Tooltip("세로도 카메라를 따라가 항상 화면을 덮는다(하늘 등 원경에 권장).\n끄면 월드에 고정되어 지면에 정렬된 채로 유지된다(중/근경).")]
        bool _followVertical = false;

        [SerializeField, Tooltip("가로도 카메라 중심에 고정 → 캐릭터 기준 쏠림 없음(먼 배경/스카이박스용).\n켜면 가로 패럴랙스는 사라지고 전경 스크롤이 깊이감을 담당한다.")]
        bool _followHorizontal = false;

        [SerializeField, Tooltip("타일러블(좌우 경계가 맞물리는) 텍스처에서만 켠다. 카메라가 타일 1칸 벗어나면 폭만큼 스냅해 무한 반복.\n타일러블이 아닌 단일 일러스트면 끈다(켜면 경계에서 툭 끊긴다).")]
        bool _infiniteWrap = false;

        [SerializeField, Tooltip("플레이어 spawn 직후 배경을 카메라(=플레이어) X에 재정렬 → 합성 구도를 플레이어 중심에 맞춤.\nLocalPlayerSpawner가 카메라 SetTarget 직후 AnchorToCameraX()를 호출한다. spawn 위치는 서버 권위라 씬 authored 위치와 달라서 필요.")]
        bool _anchorToCameraOnSpawn = true;

        SpriteRenderer _renderer;
        float _tileWorldWidth;   // 스프라이트 한 장의 월드 폭
        float _prevCamX;
        float _yOffset;          // followVertical일 때 카메라 기준 세로 오프셋

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_renderer != null && _renderer.sprite != null)
                _tileWorldWidth = _renderer.sprite.bounds.size.x * transform.lossyScale.x;

            if (_cameraTransform != null)
            {
                _prevCamX = _cameraTransform.position.x;
                _yOffset = transform.position.y - _cameraTransform.position.y;
            }
        }

        // 런타임에 카메라가 동적으로 생성/연결되는 경우를 위해 외부 주입 허용.
        public void SetCamera(Transform cam)
        {
            _cameraTransform = cam;
            if (cam != null)
            {
                _prevCamX = cam.position.x;
                _yOffset = transform.position.y - cam.position.y;
            }
        }

        // 플레이어 spawn 직후 LocalPlayerSpawner가 카메라 SetTarget 직후 호출.
        //   배경을 카메라(=플레이어) 현재 X에 재정렬 → 합성 구도가 플레이어 중심에 온다.
        //   Start의 앵커는 authored X(씬 배치값)라 서버 권위 spawn 위치를 모름 → spawn 시점에 다시 잡는다.
        //   followHorizontal(항상 화면 중앙) 모드는 이미 중앙이라 불필요.
        public void AnchorToCameraX()
        {
            if (!_anchorToCameraOnSpawn || _followHorizontal) return;
            if (_cameraTransform == null)
            {
                if (Camera.main == null) return;
                _cameraTransform = Camera.main.transform;
            }
            float cx = _cameraTransform.position.x;
            transform.position = new Vector3(cx, transform.position.y, transform.position.z);
            _prevCamX = cx;
        }

        void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                if (Camera.main == null) return;
                SetCamera(Camera.main.transform);
            }

            Vector3 cam = _cameraTransform.position;
            float dx = cam.x - _prevCamX;

            // followHorizontal: 스프라이트 중심을 카메라에 맞춰 항상 화면 중앙(가로 패럴랙스 없음).
            float newX = _followHorizontal
                ? cam.x
                : transform.position.x + dx * (1f - _parallaxFactor);
            float newY = _followVertical ? cam.y + _yOffset : transform.position.y;

            // 무한 반복: 카메라에서 타일 1칸 이상 벗어나면 정확히 타일 폭만큼 스냅. (타일러블 + 가로 패럴랙스 모드 한정)
            if (_infiniteWrap && !_followHorizontal && _tileWorldWidth > 0.0001f)
            {
                float rel = cam.x - newX;
                if (rel > _tileWorldWidth) newX += _tileWorldWidth;
                else if (rel < -_tileWorldWidth) newX -= _tileWorldWidth;
            }

            transform.position = new Vector3(newX, newY, transform.position.z);
            _prevCamX = cam.x;
        }
    }
}
