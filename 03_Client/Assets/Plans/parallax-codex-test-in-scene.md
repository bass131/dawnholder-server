# Project Overview
- **Game Title**: Dawnholder (Client)
- **High-Level Concept**: Codex와 함께 만든 5-Layer 패럴랙스 배경(저녁 노을 컨셉, 2D 횡스크롤 MMORPG용)을 **현재 열려 있는 씬에서 한 번 빠르게 시각 테스트**한다. 프로덕션 통합이 아니라 "동작 확인"이 목적.
- **Players**: Single player (이번 테스트는 플레이어 불필요)
- **Inspiration / Reference**: 클래식 2D 패럴랙스 (Sky/Mountains/Landmarks/Midground/Foreground 깊이감)
- **Tone / Art Direction**: 저녁 노을(주황·보라), 원경 밝고 근경 어두운 실루엣
- **Target Platform**: StandaloneWindows64
- **Screen Orientation / Resolution**: Landscape 16:9 (현재 Main Camera Orthographic size 3.86 → 표시 영역 약 13.7 × 7.7 월드 유닛)
- **Render Pipeline**: URP (2D)

# 현재 상태 분석 (사실 기반, 도구로 확인됨)
- **활성 씬**: `Assets/Art/Environment/Others/Cainos/Pixel Art Platformer - Village Props/Scene/SC Demo Scene - Village Props.unity`
  - 루트 오브젝트 3개: `SCENE`, `Main Camera`, `Global Volume - ToneTest`
  - **플레이어 없음** (`Player` 태그 0개) → 카메라를 움직일 주체가 없음.
- **Main Camera**: Orthographic, size 3.86, pos ≈ (-64.67, 8.61, -3), aspect ≈ 1.778 (view 반폭 ≈ 6.86).
  - 컴포넌트에 `Dawnholder.Client.Rendering.CameraFollow` 존재. `_target`은 런타임 spawn으로 채워지는 구조이며 **이 씬에선 null** → `CameraFollow.LateUpdate()`가 즉시 return하므로 카메라를 직접 움직여도 충돌 없음.
- **패럴랙스 스크립트(이미 존재)**: `Assets/Scripts/Rendering/ParallaxLayer.cs` (ns `Dawnholder.Client.Rendering`)
  - LateUpdate에서 카메라 X 이동량 `dx`에 대해 레이어를 `dx * (1 - factor)` 만큼 이동.
  - **factor 의미: 0 = 원경(화면상 거의 정지) ~ 1 = 근경(월드 고정=완전 스크롤)** → README 비율과 동일하게 그대로 대입.
  - 직렬화 private 필드: `_cameraTransform`, `_parallaxFactor`(Range 0~1), `_followVertical`. → 에디터에서 `SerializedObject`로 주입 필요.
  - 타일 1칸 이상 어긋나면 타일 폭만큼 스냅(무한 반복은 SpriteRenderer `drawMode = Tiled` 전제).
- **레이어 에셋(5장, PPU 100)**: `Assets/Art/Environment/BackGround/Parallax/Parallax_background_layers_With_Codex/`
  - 전부 Sprite, **Multiple(단일 스프라이트 포함)**, **Wrap = Clamp**, Mesh = Tight, 월드 크기 ≈ **16.72 × 9.41**.
  - **Pivot = (0,0) 좌하단** → `transform.position`은 스프라이트의 좌하단 모서리. 화면 중앙 정렬하려면 (-8.36, -4.705) 오프셋 필요.
  - README 권장 순서/비율:
    | 파일 | 역할 | 권장 factor |
    |---|---|---|
    | 01_parallax_sky_sunset | 최원경 하늘 | 0.05~0.10 |
    | 02_parallax_far_mountains | 원경 산맥 | 0.15~0.25 |
    | 03_parallax_castle_valley_landmarks | 성/계곡 | 0.30~0.45 |
    | 04_parallax_midground_fields_ruins | 중경 들판/폐허 | 0.55~0.75 |
    | 05_parallax_foreground_village_platform | 근경 마을/플랫폼 | 1.00 |

# 결정된 방향 (사용자 확정)
1. **카메라 이동 = 키보드 수동 조작**(A/D, New Input System) — 임시 테스트 스크립트로 처리.
2. **단순 파노라마** — 임포트 설정 변경 없음(타일링 안 함). 한 장씩 배치하고 제한된 범위에서 패럴랙스 차등 스크롤만 확인.
3. 본 작업은 **현재 Cainos 데모 씬에서의 일회성 테스트**. 기존 씬 콘텐츠는 변경/삭제하지 않고, 테스트 오브젝트만 추가하며 확인 후 정리 가능하도록 구성.

# Game Mechanics
## Core Gameplay Loop
이번 테스트에 게임플레이 루프는 없음. 카메라를 좌우로 패닝했을 때 5개 배경 레이어가 **서로 다른 속도로 스크롤되어 깊이감**이 생기는지를 확인하는 순수 시각 검증.

## Controls and Input Methods
- New Input System 직접 폴링 방식(`UnityEngine.InputSystem.Keyboard.current`).
  - `A` / `LeftArrow`: 카메라 왼쪽 이동
  - `D` / `RightArrow`: 카메라 오른쪽 이동
  - (선택) `W`/`S`: 상하 이동(세로 followVertical 확인용)
- 액션 에셋/리바인딩 불필요(테스트 전용 최소 구현).

# UI
UI 변경 없음. 화면 좌상단에 안내 텍스트가 필요하면 `OnGUI`로 임시 표시(선택, 기본은 생략).

# Key Asset & Context

## 신규 생성 (임시 테스트 전용)
- `Assets/Scripts/Dev/ParallaxCameraTester.cs` (ns `Dawnholder.Client.Dev`)
  - 목적: 플레이어가 없는 씬에서 카메라를 키보드로 직접 패닝해 패럴랙스를 눈으로 확인.
  - 동작(개요):
    ```csharp
    using UnityEngine;
    using UnityEngine.InputSystem;

    namespace Dawnholder.Client.Dev
    {
        // [임시/테스트용] 패럴랙스 확인을 위한 카메라 수동 패닝.
        // CameraFollow._target == null 이면 CameraFollow가 동작 안 하므로 충돌 없음.
        public class ParallaxCameraTester : MonoBehaviour
        {
            [SerializeField] float _speed = 8f;
            void Update()
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                float x = 0f, y = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.wKey.isPressed) y += 1f;
                if (kb.sKey.isPressed) y -= 1f;
                transform.position += new Vector3(x, y, 0f) * (_speed * Time.deltaTime);
            }
        }
    }
    ```

## 씬에 추가할 오브젝트 (테스트 루트)
- 빈 루트 `ParallaxTest (TEMP)` 생성.
- 하위 5개 GameObject(각각 `SpriteRenderer` + `ParallaxLayer`):
  | 자식 이름 | 스프라이트(GUID) | factor | followVertical | 권장 sortingOrder | Z |
  |---|---|---|---|---|---|
  | L0_Sky | 9b0450cc4de22424e99857cd99834817 | 0.08 | true | (backmost) | 50 |
  | L1_FarMountains | 10f03d22f6a3c7a4a8b32b52ab4cff19 | 0.20 | false | +1 | 40 |
  | L2_CastleValley | b76440fda8f32f944b6adb8c87870e42 | 0.38 | false | +2 | 30 |
  | L3_Midground | 22fa4ca74f1117143a4825b8b30b9cb5 | 0.65 | false | +3 | 20 |
  | L4_Foreground | fed3f780dc902a44c84568e91569f3f4 | 1.00 | false | +4 | 10 |
  - **SpriteRenderer.drawMode = Simple** (단순 파노라마). 5장 모두 동일 Sorting Layer 사용, order만 위 순서로.
  - **정렬 보장**: 배포 단계 전에 씬의 기존 스프라이트들이 쓰는 Sorting Layer/order 범위를 먼저 조회해, 패럴랙스 5장을 **가장 뒤쪽 Sorting Layer(없으면 Default)에서 충분히 낮은 order(예: -200부터 -196)** 로 배치해 모든 데모 프롭 뒤에 그려지도록 한다. (Cainos 데모는 자체 Sorting Layer를 쓸 수 있으므로 실측 후 결정)
  - **배치 좌표**: pivot이 좌하단이므로 각 자식 `position = (camX - 8.36, camY - 4.705, Z)` 로 두어 현재 카메라 화면 중앙에 오게 한다(camX≈-64.67, camY≈8.61, 단 배포 시점의 실제 카메라 위치를 읽어 적용).
  - `ParallaxLayer._cameraTransform` = Main Camera transform 주입(`SerializedObject`).

## 알려진 제약 (단순 파노라마 선택의 결과)
- 한 장 폭 16.72, 카메라 view 폭 ≈ 13.7. 근경(factor 1.0)은 카메라 이동 시 화면상 가장 빨리 어긋나, **약 ±1.5 월드 유닛** 패닝 후 한쪽 가장자리가 보일 수 있음. 차등 스크롤 확인엔 충분하나, **무한 스크롤은 이번 테스트 범위 밖**(원하면 추후 Wrap=Repeat + FullRect + drawMode=Tiled로 전환).

# Implementation Steps

### Step 1 — 테스트 카메라 패닝 스크립트 작성
- **Description**: `Assets/Scripts/Dev/ParallaxCameraTester.cs` 생성(위 코드). 컴파일 에러 0 확인.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2 — 씬 정렬 컨텍스트 실측
- **Description**: 현재 활성 씬의 기존 `SpriteRenderer`들이 사용하는 Sorting Layer 이름과 order 범위를 읽어, 패럴랙스 5장이 항상 뒤에 그려질 Sorting Layer/order 기준값을 결정.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3 — ParallaxTest 루트 + 5레이어 구성/와이어링
- **Description**: `ParallaxTest (TEMP)` 빈 루트 생성. 하위 5개 GameObject에 각 스프라이트로 `SpriteRenderer`(drawMode=Simple, Step 2 결과의 Sorting Layer/order) 추가. 각 오브젝트에 `ParallaxLayer` 추가하고 `SerializedObject`로 `_parallaxFactor`, `_followVertical`, `_cameraTransform`(Main Camera) 주입. pivot(좌하단) 보정해 카메라 화면 중앙 정렬, Z는 표의 값.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4 — 카메라에 테스터 부착
- **Description**: `Main Camera`에 `ParallaxCameraTester` 컴포넌트 추가(speed 기본 8). `CameraFollow._target`이 null인지 재확인(충돌 방지).
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 5 — Play Mode 검증
- **Description**: Play 진입 → A/D로 카메라 패닝. 5레이어가 Sky(가장 느림) → Foreground(가장 빠름) 순으로 차등 스크롤되는지, 모두 데모 프롭 뒤에 그려지는지, Console 에러 0인지 확인. 필요 시 factor/정렬/위치 미세 조정.
- **Assigned role**: developer
- **Dependencies**: Step 3, Step 4
- **Parallelizable**: No

### Step 6 — 정리(선택)
- **Description**: 확인 후 `ParallaxTest (TEMP)` 오브젝트와 `Main Camera`의 `ParallaxCameraTester`를 제거할지, 추가 작업(무한 타일링/HuntingGround 통합)으로 이어갈지 사용자에게 확인. 테스트 스크립트(`ParallaxCameraTester.cs`)는 Dev 폴더에 유지 또는 삭제.
- **Assigned role**: developer
- **Dependencies**: Step 5
- **Parallelizable**: No

# Verification & Testing
- **컴파일**: `ParallaxCameraTester` 추가 후 Console 에러 0.
- **차등 스크롤**: Play Mode에서 A/D 패닝 시 화면상 이동 속도가 Sky < FarMountains < CastleValley < Midground < Foreground 순으로 빨라지는지 시각 확인.
- **정렬**: 5개 배경이 모두 Cainos 데모 프롭/플랫폼 뒤에 그려지는지 확인(앞에 보이면 Sorting Layer/order 하향).
- **세로 커버(선택)**: W/S로 상하 이동 시 Sky 레이어(`_followVertical=true`)가 따라와 상·하단 빈 영역을 만들지 않는지 확인.
- **제약 확인**: 근경 기준 ±1.5 유닛 초과 패닝 시 가장자리 노출은 "단순 파노라마"의 예상된 동작임(무한 반복 아님).
- **무결성**: 테스트 오브젝트/컴포넌트는 기존 씬 콘텐츠를 수정하지 않으며 제거 시 원상복구됨.
