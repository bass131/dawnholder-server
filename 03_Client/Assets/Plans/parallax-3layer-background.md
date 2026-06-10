# Project Overview
- **Game Title**: Dawnholder (Client) — HuntingGround 스테이지
- **High-Level Concept**: 횡스크롤 2D 액션 platformer. 플레이어가 사냥터(HuntingGround)를 이동하며 전투/탐험. 본 플랜은 해당 스테이지에 **저녁 노을 분위기의 3-Layer 무한 패럴랙스 배경**을 신규 생성·세팅하는 작업.
- **Players**: Single player (LocalPlayer 런타임 spawn)
- **Inspiration / Reference**: 클래식 2D 플랫포머 패럴랙스 (Sky / Mountains / Forest 깊이감)
- **Tone / Art Direction**: 저녁 노을(따뜻한 주황·보라 그라데이션), 원경은 밝은 하늘, 중·근경으로 갈수록 어두운 실루엣
- **Target Platform**: StandaloneWindows64
- **Screen Orientation / Resolution**: Landscape 16:9 (카메라 Orthographic size 3.5 → 표시 영역 약 12.44 × 7 월드 유닛)
- **Render Pipeline**: URP (2D)

# Game Mechanics
## Core Gameplay Loop
횡스크롤 이동 + 전투. 카메라(`Dawnholder.Client.Rendering.CameraFollow`)가 플레이어를 LateUpdate에서 부드럽게(Lerp) 추적. 배경은 게임플레이에 영향을 주지 않는 **순수 시각 깊이감(parallax)** 요소.

## Controls and Input Methods
신규 Input System 사용. 본 작업은 입력 변경 없음 (배경은 카메라 이동에 반응할 뿐 입력과 무관).

# 현재 씬 분석 (사실 기반)
- **활성 배경**: `BackGround_Evening_0` — `Assets/Art/Environment/BackGround/BackGround_Evening.png` (2048×1144, PPU 100, Multiple). **Main Camera 자식**으로 붙어 화면에 고정된 정적 스카이박스. Sorting Layer = Background, order 0.
- **비활성 그룹**: 루트 `BackGround` (자식: `Bg_Sky` ×8, `Clouds` ×4, `BG_Rock` ×2, `Bg_Grass` ×2). 현재 **비활성(Find로 안 잡힘)**. → 본 플랜에서 **제거/정리** 대상.
- **카메라**: Main Camera, Orthographic, size 3.5, pos (3, 1.55, -10). `CameraFollow`로 플레이어 추적.
- **Sorting Layers (기존)**: `Background`(-1) / `Default`(0) / `Foreground`(1) / `UI`(2). → 신규 정렬 레이어 추가 불필요.
- **패럴랙스 스크립트**: 프로젝트에 **없음** → 신규 작성 필요.
- **스크립트 네임스페이스 규칙**: `Dawnholder.Client.*` (예: `Dawnholder.Client.Rendering`). 한국어 주석으로 의도 설명하는 스타일.

# 결정된 방향 (사용자 확정)
1. 기존 배경 **전부 교체** (정적 노을 + 비활성 그룹 정리)
2. 3레이어 = **하늘(원경) / 산맥(중경) / 숲(근경)**
3. **무한 반복 스크롤** (가로 타일링)
4. **AI 생성** — 가로 seamless, 원경 불투명 / 중·근경 투명 PNG

# UI
배경 작업이므로 UI/HUD 변경 없음. (게임플레이 요소는 모두 `Default` 정렬 레이어 이상에서 렌더 → 3레이어는 모두 `Background`에 배치하여 항상 뒤에 그려짐.)

# Key Asset & Context

## 생성할 아트 에셋 (AI Generate, 저장 위치: `Assets/Art/Environment/BackGround/Parallax/`)
모두 **가로 이음새 없는(seamless horizontal tiling)** 으로 생성. PPU 100 기준.

| 레이어 | 파일명(예) | 권장 해상도 | 알파 | 내용 / 프롬프트 방향 |
|---|---|---|---|---|
| Far (원경) | `BG_Far_Sky.png` | 2048×1024 | 불투명 | 저녁 노을 하늘, 부드러운 구름, 원경 대기 그라데이션 (주황→보라). 가로 타일 가능. |
| Mid (중경) | `BG_Mid_Mountains.png` | 2048×768 | 투명 PNG | 겹친 산맥 능선 실루엣, 황혼 톤(어두운 보라/남색), 바닥 정렬. 가로 타일 가능. |
| Near (근경) | `BG_Near_Forest.png` | 2048×512 | 투명 PNG | 어두운 나무·수풀 실루엣 전경, 거의 검정에 가까운 실루엣, 바닥 정렬. 가로 타일 가능. |

### 생성 프롬프트 초안 (생성 단계에서 사용)
- **Far**: "2D side-scroller background, evening sunset sky, soft warm gradient orange to purple, gentle distant clouds, seamless horizontal tiling, flat painterly style, no characters"
- **Mid**: "2D parallax mountain silhouette layer, layered dusk mountain ridges, dark purple to blue, transparent background, bottom aligned, seamless horizontal tiling, flat style"
- **Near**: "2D parallax foreground silhouette, dark forest trees and bushes silhouette, near-black, transparent background, bottom aligned, seamless horizontal tiling"

### Seamless 처리 정책
AI 생성물이 좌우 이음새가 완벽하지 않을 수 있음. 우선순위:
1. 생성 시 seamless/tileable 옵션 활용.
2. 부족하면 가장자리 in-painting refine로 좌우 이음새 보정.
3. 그래도 부정확하면 **미러 타일링**(짝수 타일 X축 반전) 폴백 — 단 산/숲 실루엣은 미러가 부자연스러울 수 있어 1·2 우선.

### Import 설정 (생성 후 적용)
- Sprite Mode: Single, PPU 100, Filter Bilinear, Compression 적절히, Wrap Mode = Repeat(타일드 드로우용), Mesh Type = Full Rect.
- Mid/Near는 알파 보존(투명 PNG).

## 생성할 스크립트
- `Assets/Scripts/Rendering/ParallaxLayer.cs` (네임스페이스 `Dawnholder.Client.Rendering`)
  - 자기 완결형(per-layer) 무한 패럴랙스. SpriteRenderer(`drawMode = Tiled`)로 가로 반복.
  - 직렬화 필드:
    - `[SerializeField] Transform _cameraTransform` (없으면 `Camera.main` 자동)
    - `[SerializeField, Range(0f,1f)] float _parallaxFactor` (0=거의 정지=원경, 1=카메라와 동일)
    - `[SerializeField] bool _followVerticalFull` (세로도 카메라 따라갈지)
  - 핵심 로직(LateUpdate, 카메라 이동 후):
    ```csharp
    Vector3 cam = _cameraTransform.position;
    float deltaX = cam.x - _prevCamX;
    // 레이어는 카메라 X 이동량의 (1 - factor)만큼 뒤처져 보임
    transform.position += new Vector3(deltaX * (1f - _parallaxFactor), 0f, 0f);
    // 세로: 원경은 카메라를 부분/완전 추적해 항상 화면을 덮음
    // 무한 반복: 타일 1칸(_tileWorldWidth) 넘게 어긋나면 그만큼 보정
    float relX = cam.x - transform.position.x;
    if (Mathf.Abs(relX) >= _tileWorldWidth)
        transform.position += new Vector3(Mathf.Sign(relX) * _tileWorldWidth, 0f, 0f);
    _prevCamX = cam.x;
    ```
  - `_tileWorldWidth` = 스프라이트 1장 월드 폭(=texture.width / PPU). `drawMode=Tiled`의 `size.x`는 화면 폭의 약 3배(예: 40 유닛)로 잡아 리포지셔닝 중에도 빈틈이 안 보이게 함.
- (선택) `Assets/Scripts/Rendering/ParallaxBackground.cs` — 3개 `ParallaxLayer`를 한 부모에서 관리(카메라 참조 일괄 주입). 단순화를 위해 per-layer 자기완결형이면 생략 가능.

## 권장 파라미터
- 정렬: 모두 Sorting Layer = `Background`. order → Far=0, Mid=10, Near=20.
- parallaxFactor: Far ≈ 0.1, Mid ≈ 0.4, Near ≈ 0.7.
- 세로 추적: Far `_followVerticalFull=true`(항상 하늘로 덮음), Mid/Near는 바닥 고정 또는 부분 추적.
- 배치: 월드 공간(카메라 자식 아님). z는 정렬 레이어로 제어하므로 0 유지(또는 Far=10/Mid=8/Near=6로 정리).

# Implementation Steps

### Step 1 — 배경 아트 3종 AI 생성
- **Description**: `Assets/Art/Environment/BackGround/Parallax/`에 Far/Mid/Near 3장 생성. 위 프롬프트/해상도/알파 규격 준수. seamless 우선, 부족 시 refine.
- **Assigned role**: developer (asset generation)
- **Dependencies**: None
- **Parallelizable**: Yes (3장 동시 생성 가능)

### Step 2 — 생성 에셋 Import 설정
- **Description**: 3장 모두 Sprite(Single), PPU 100, Wrap=Repeat, FullRect, Mid/Near 알파 보존으로 importer 설정.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3 — ParallaxLayer 스크립트 작성
- **Description**: `Assets/Scripts/Rendering/ParallaxLayer.cs` 작성 (위 설계). `drawMode=Tiled` 기반 무한 반복 + parallaxFactor + 세로 추적 옵션. 컴파일 에러 0 확인.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes (Step 1과 동시 가능)

### Step 4 — 기존 배경 정리
- **Description**: HuntingGround 씬에서 Main Camera 자식 `BackGround_Evening_0` 제거(또는 비활성), 비활성 루트 `BackGround` 그룹 제거. (씬 백업/되돌리기 가능하도록 비활성 우선 후 확인되면 삭제)
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 5 — 3레이어 GameObject 구성 및 와이어링
- **Description**: 빈 루트 `Parallax_Background` 아래 `Far_Sky`/`Mid_Mountains`/`Near_Forest` 3개 생성. 각자 SpriteRenderer(스프라이트 할당, Sorting Layer=Background, order 0/10/20, drawMode=Tiled, size.x≈40, size.y=각 레이어 월드 높이)와 `ParallaxLayer`(factor 0.1/0.4/0.7, 카메라/세로옵션) 부착. 카메라 위치 기준 정렬.
- **Assigned role**: developer
- **Dependencies**: Step 2, Step 3, Step 4
- **Parallelizable**: No

### Step 6 — 검증 및 튜닝
- **Description**: Play Mode 진입 → 플레이어 좌우 이동 시 3레이어가 서로 다른 속도로 스크롤, 빈틈/이음새 없는지 확인. factor·size 미세 조정.
- **Assigned role**: developer
- **Dependencies**: Step 5
- **Parallelizable**: No

# Verification & Testing
- **컴파일**: ParallaxLayer 추가 후 Console 에러 0.
- **무한 스크롤**: Play Mode에서 플레이어를 한쪽 끝까지 이동해도 모든 레이어가 끊김 없이 화면을 덮는지(타일 리포지셔닝 정상) 확인.
- **깊이감**: 카메라 이동 시 Far < Mid < Near 순으로 빠르게 움직이는지 시각 확인.
- **이음새**: 정지/이동 중 좌우 타일 경계 seam 없는지 확인. seam 보이면 Step 1 refine 또는 미러 타일링 폴백.
- **세로 커버**: 카메라 Y 변동 시 하늘 레이어가 위/아래로 빈 영역을 만들지 않는지 확인(`_followVerticalFull` 또는 size.y 확대로 보정).
- **정렬**: 모든 배경이 게임플레이(Default/Foreground) 뒤에 그려지는지 확인.
- **회귀**: 기존 `BackGround_Evening_0` 제거 후 다른 시스템(SceneBootstrap, 포탈 등)에 깨짐 없는지 확인.
