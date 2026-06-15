#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Rendering
{
    // 미니맵 dot 마커 오버레이 — MinimapView RawImage에 AddComponent해서 사용.
    //
    // **헌법 §1**: 위치는 서버 미러(RemoteEntityRegistry 보간값) 또는 로컬 prediction 값을
    //   *읽기만* 한다. 절대 변경 X. 순수 표시물.
    //
    // **좌표 변환 흐름**:
    //   월드좌표 → MinimapCamera.WorldToViewportPoint(0..1) → RectTransform 로컬좌표 매핑.
    //   viewport 0..1 밖이면 해당 dot을 숨긴다(클리핑).
    //
    // **dot 풀**: 매 프레임 Instantiate/Destroy는 GC 폭탄 — 풀에서 대여 후 반납.
    //   풀 크기는 필요 시 동적 확장(shrink는 하지 않음 — 소규모 씬에서 문제 없음).
    //
    // **배선**: 메인 세션이 MCP로 MinimapView에 AddComponent<MinimapMarkers> 호출만으로 동작.
    //   _container null 시 자신의 RectTransform을 컨테이너로 사용.
    [DisallowMultipleComponent]
    public class MinimapMarkers : MonoBehaviour
    {
        // ── 튜닝 상수 (영호 조정 지점) ───────────────────────────────────────────────
        static readonly Color ColorSelf      = Color.green;
        static readonly Color ColorParty     = new Color(0.6f, 0.2f, 0.9f, 1f);   // 보라
        static readonly Color ColorEnemy     = Color.red;
        static readonly Color ColorOtherPlayer = new Color(1f, 1f, 1f, 0.5f);     // 흐린 흰

        const float DotSizeSelf   = 8f;   // 픽셀 단위 dot 크기 (영호 조정)
        const float DotSizeOther  = 6f;
        const float DotSizeEnemy  = 6f;
        // ─────────────────────────────────────────────────────────────────────────────

        // dot을 RawImage 위에 올릴 컨테이너. null이면 Awake에서 자기 RectTransform으로 폴백.
        [SerializeField] RectTransform? _container;

        RectTransform _containerRect = null!;

        // dot 풀 — GC 폭탄 방지. 대여(Rent) 후 반납(Return) 패턴.
        readonly List<Image> _pool = new();
        int _nextDotIndex;  // 이번 프레임에서 풀에서 대여한 수

        void Awake()
        {
            _containerRect = _container != null ? _container : (RectTransform)transform;
        }

        void LateUpdate()
        {
            if (MinimapCamera.Instance == null) return;

            BeginFrame();

            // 본인 dot
            LocalPlayerMovement? local = LocalPlayerMovement.Instance;
            if (local != null)
                PlaceDot(local.transform.position, ColorSelf, DotSizeSelf);

            // 원격 플레이어 dot — 파티원이면 보라, 그 외 흐린 흰
            RemoteEntityRegistry? remoteReg = RemoteEntityRegistry.Instance;
            if (remoteReg != null)
            {
                foreach (State.RemoteEntity entity in remoteReg.Entities)
                {
                    if (entity == null) continue;
                    Color color = IsPartyMember(entity.EntityId) ? ColorParty : ColorOtherPlayer;
                    PlaceDot(entity.transform.position, color, DotSizeOther);
                }
            }

            // 적 dot
            EnemyRegistry? enemyReg = EnemyRegistry.Instance;
            if (enemyReg != null)
            {
                foreach ((int _, Transform t) in enemyReg.EnemyTransforms)
                    PlaceDot(t.position, ColorEnemy, DotSizeEnemy);
            }

            EndFrame();
        }

        // 이번 프레임 시작 — 풀 커서를 0으로 되감고, 지난 프레임 dot들을 일단 전부 숨긴다.
        // EndFrame에서 실제 사용된 dot만 표시하므로 이중 숨김이 돼도 무방.
        void BeginFrame()
        {
            _nextDotIndex = 0;
            foreach (Image dot in _pool)
                dot.gameObject.SetActive(false);
        }

        // 이번 프레임 끝 — 사용하지 않은 나머지 풀 dot은 이미 BeginFrame에서 숨긴 상태.
        void EndFrame() { }

        // 월드 좌표 → viewport → RectTransform 로컬 좌표 변환 후 dot 배치.
        // viewport가 0..1 밖이면 숨김(미니맵 영역 클리핑).
        void PlaceDot(Vector3 worldPos, Color color, float size)
        {
            Camera cam = MinimapCamera.Instance!.Cam;
            Vector3 vp = cam.WorldToViewportPoint(worldPos);

            // viewport z < 0 = 카메라 뒤(2D에서는 발생 거의 없지만 방어적 체크)
            if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                return;

            Image dot = RentDot();
            dot.color = color;

            RectTransform rt = (RectTransform)dot.transform;
            rt.sizeDelta = new Vector2(size, size);

            // viewport (0..1) → 컨테이너 로컬좌표.
            // anchorMin/Max = (0.5, 0.5)으로 중앙 기준 → (vp - 0.5) * rectSize
            Rect rect = _containerRect.rect;
            float lx = (vp.x - 0.5f) * rect.width;
            float ly = (vp.y - 0.5f) * rect.height;
            rt.anchoredPosition = new Vector2(lx, ly);

            dot.gameObject.SetActive(true);
        }

        // 풀에서 dot Image를 대여. 풀이 부족하면 새로 생성해 확장.
        Image RentDot()
        {
            if (_nextDotIndex >= _pool.Count)
                _pool.Add(CreateDot());

            return _pool[_nextDotIndex++];
        }

        // dot GameObject 최초 생성 — 기본 흰 사각형 Image(원형 sprite 없이도 색으로 구분 가능).
        // Unity 내장 Knob sprite를 원형 대용으로 쓰려면 sprite 할당을 추가하면 됨.
        Image CreateDot()
        {
            GameObject go = new GameObject("MinimapDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_containerRect, worldPositionStays: false);

            RectTransform rt = (RectTransform)go.transform;
            // 중앙 앵커 — anchoredPosition이 중심 기준 오프셋으로 작동하도록
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(DotSizeOther, DotSizeOther);

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false; // 미니맵 dot이 UI 클릭 이벤트 막지 않게

            go.SetActive(false);
            return img;
        }

        // entityId가 현재 파티 슬롯(Member0 / Member1) 중 하나인지 확인.
        // 빈 슬롯은 entityId == 0이므로 자연스럽게 제외됨.
        // PartyState.Instance는 null! 선언이지만 씬 로드 타이밍에 따라 null일 수 있어 방어 체크.
        static bool IsPartyMember(int entityId)
        {
            PartyState ps = PartyState.Instance;
            if (ps == null || !ps.InParty) return false;  // 씬 초기화 전 null 방어
            return entityId == ps.Member0EntityId || entityId == ps.Member1EntityId;
        }
    }
}
