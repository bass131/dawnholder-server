# CastleValley_Sunset Parallax Background Pack

2D Side Scroll MMORPG용 원근 배경 레이어 패키지입니다.  
기준 해상도: `2172 x 724`, 비율: `3:1`.

## Layer 구성

| 파일명 | 역할 | Unity 권장 Sorting Order | 권장 Parallax Speed |
|---|---|---:|---:|
| `00_Composite_Preview.png` | 전체 합성 컨셉 미리보기 | - | - |
| `01_Sky_SunGlow.png` | 하늘 그라데이션 + 태양광. 가장 뒤 배경 | 0 | 0.00 |
| `02_Far_Clouds.png` | 원거리 구름 | 10 | 0.02 |
| `03_Distant_Mountains.png` | 가장 먼 산맥 | 20 | 0.05 |
| `04_Mid_Mountains_Hills.png` | 중거리 산 / 언덕 | 30 | 0.09 |
| `05_Castle_City.png` | 성곽 도시 / 원거리 건축물 | 40 | 0.14 |
| `06_Valley_Fields_Ruins.png` | 초원 / 밭 / 폐허 / 농가 | 50 | 0.22 |
| `07_Foreground_Foliage.png` | 근경 나무 / 숲 / 울타리 장식 | 60 | 0.35 |

실제 플레이 지형과 플레이어는 배경보다 앞에 두는 것이 안전합니다.

```text
Background_01_Sky              Order 0
Background_02_Clouds           Order 10
Background_03_DistantMountain  Order 20
Background_04_MidMountain      Order 30
Background_05_CastleCity       Order 40
Background_06_ValleyRuins      Order 50
Background_07_ForegroundTrees  Order 60
Gameplay_Terrain               Order 100
Player                         Order 200
```

## Unity Import 권장값

각 PNG 선택 후 Inspector에서 다음처럼 설정합니다.

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 프로젝트 타일 기준에 맞춤. 예: 100 또는 16/32
Filter Mode: Point (no filter) 또는 Bilinear
Compression: None
Generate Mip Maps: Off
```

픽셀 아트 느낌을 강하게 유지하려면 `Point (no filter)`가 맞습니다.  
다만 현재 배경은 부드러운 원경 느낌이 있으므로, 화면 확대/축소가 심하면 `Bilinear`가 더 자연스러울 수 있습니다.

## 배치 기준

- `01_Sky_SunGlow.png`는 화면 전체를 덮는 고정 배경으로 사용합니다.
- `02`~`07`은 카메라 이동량에 따라 서로 다른 속도로 이동시킵니다.
- 실제 플랫폼 타일/충돌 지형은 이 배경 패키지와 분리해서 `Gameplay_Terrain` 레이어에 둡니다.
- `07_Foreground_Foliage.png`는 플레이어보다 앞에 두면 캐릭터를 가릴 수 있으므로, 처음에는 플레이어 뒤쪽 배경으로 두는 것을 권장합니다.

## 구성 폴더

```text
Assets/Art/Backgrounds/CastleValley_Sunset/
Source_Raw_Generated/
README.md
parallax_config.json
```

`Assets/Art/Backgrounds/CastleValley_Sunset/` 안의 파일이 Unity에 바로 넣을 정리본입니다.  
`Source_Raw_Generated/`는 원본 생성 파일을 이름만 정리해서 보관한 폴더입니다.

## 주의

일부 레이어는 원본 생성 과정에서 투명 영역이 체크보드 배경으로 렌더링된 상태였기 때문에, 정리본에서는 밝은 무채색 체크보드 영역을 알파로 제거했습니다.  
실사용 전 Unity에서 가장자리 halo가 보이면 해당 레이어의 가장자리만 수동 리터칭하는 것이 좋습니다.
