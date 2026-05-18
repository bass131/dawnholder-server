# Phase 01: Sprite Import 컨벤션 + 캐릭터 spritesheet 임포트

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1~1.5시간
> **담당 에이전트**: client

---

## 🎯 목표

Kenney.nl에서 캐릭터 spritesheet 1개를 다운받아 Unity 프로젝트에 임포트하고, 픽셀아트에 맞는 Import 설정(Pixels Per Unit, Filter Mode, Compression)을 박제한 뒤, Sprite Editor에서 grid slice로 프레임을 분리해 Gameplay 씬에 정적으로 배치한다.

**끝나면 데모 가능한 것**: Gameplay 씬에 캐릭터 sprite 1장이 또렷한 픽셀로 표시됨 (애니메이션은 다음 Phase).

---

## ⏪ 사전 조건

- [ ] M1-client-foundations 마감 (Gameplay 씬 + SceneBootstrap + UI 씬 셋업 완료)
- [ ] URP 2D Renderer 이미 활성 상태 확인 (`Assets/Settings/URP-2DRenderer.asset` 또는 동등)
- [ ] Kenney.nl 접속 가능 (https://kenney.nl/assets — 회원가입 불필요, 즉시 다운로드)

---

## 📝 작업 내용

- [ ] **자원 선정**: Kenney.nl/assets에서 2D 캐릭터 spritesheet 1개 선정. 권장:
  - "Tiny Town" / "Toon Characters 1" / "Platformer Art Deluxe" 중 1개. Idle/Run 프레임이 명확히 보이는 것
  - 라이선스 CC0 확인 (Kenney는 전부 CC0지만 페이지에 명시되어 있는지 한 번 보고 학습)
- [ ] **폴더 신설**: `03_Client/Assets/Art/Characters/` (Art 폴더가 없으면 같이 생성)
- [ ] **임포트**: 다운받은 spritesheet PNG를 위 폴더에 드래그
- [ ] **Import 설정 박제** (Inspector → Texture Type: Sprite (2D and UI)):
  - Sprite Mode = **Multiple**
  - Pixels Per Unit (PPU) = **16** (또는 32, 자원에 맞춰 결정 — 한 캐릭터가 화면에서 작을수록 PPU 낮게)
  - Filter Mode = **Point (no filter)**
  - Compression = **None**
  - Apply 클릭
- [ ] **Sprite Editor에서 slice**: Sprite Editor 버튼 → Slice 메뉴 → Type = "Grid by Cell Size" → Pixel Size에 spritesheet 한 칸 크기 입력 (예: 16×16, 32×32) → Slice → Apply
- [ ] **Gameplay 씬에 배치**: Hierarchy에 빈 GameObject 신설 (`Player` 이름) → Add Component → Sprite Renderer → Sprite 슬롯에 임포트된 sprite 첫 프레임 할당
- [ ] **Scene 뷰 확인**: 캐릭터가 픽셀 또렷하게 보이는지 (Bilinear 흐림 X)

---

## ✅ 완료 조건

- [ ] `Assets/Art/Characters/{선정한자원}.png` + `.meta` 가 git 추적됨
- [ ] Inspector에서 PPU/Filter/Compression 위 설정 그대로 박혀있음
- [ ] Sprite Editor에서 frame 4개 이상 slice 되어 있음 (Multiple sprite 자식들로 보임)
- [ ] Play 모드 진입 시 Gameplay 씬에 캐릭터 sprite 한 장이 또렷하게 보임 (검은 사각형 X, 흐릿함 X)

---

## 🧪 테스트

**수동 테스트:**
1. Project 뷰에서 spritesheet 파일 펼치기 → 자식 sprite 여러 개 보이는지
2. Scene 뷰에서 카메라 zoom in → 픽셀 경계가 또렷한지 (흐리면 Filter Mode 잘못)
3. Play 진입 → MainMenu → 시작 → Gameplay 씬에서 캐릭터 보이는지

**자동 테스트:** 없음 (Editor 워크플로우 학습 Phase)

---

## 📚 학습 포인트

- **Pixels Per Unit (PPU)**: Unity 1 단위(1m 가정) 당 픽셀 수. 픽셀아트 게임은 보통 16/32/64 중 선택. 카메라 Orthographic Size와 같이 결정 — 화면 세로 = `OrthoSize × 2` 유닛 = `OrthoSize × 2 × PPU` 픽셀. PPU 16 + OrthoSize 5 = 화면 세로 160 픽셀 (캐릭터 크게 보임).
- **Filter Mode**: Point = 가장 가까운 픽셀 그대로 (픽셀아트 정답). Bilinear = 주변 픽셀 평균 (3D 텍스처에 좋지만 픽셀아트는 흐림). Trilinear = mipmap 보간.
- **Compression**: 텍스처 메모리 절약용 압축. None = 원본 보존 (학습용 작은 프로젝트 OK). DXT/ASTC = 손실 압축 (배포 시).
- **Sprite Mode Single vs Multiple**: 한 PNG에 sprite 1개면 Single, 여러 프레임 들었으면 Multiple → Sprite Editor로 slice.
- **폴더 컨벤션**: 자산(Art/Audio/Fonts)과 코드(Scripts) 분리. 검색·정렬·CODEOWNERS 분리 모두 깔끔.
- **CC0 라이선스**: Public Domain 수준. 출처 표기 불필요, 상용 사용 가능. Kenney는 전부 CC0라 학습/포트폴리오 안전.

---

## ⚠️ 함정 / 주의사항

- **Filter Mode Bilinear 기본값**: import 시 자동으로 Bilinear가 잡힘 → 픽셀아트가 흐림. 명시적으로 Point로 바꿔야 함.
- **Compression Crunched가 작은 픽셀에 손실**: 32×32 같은 작은 sprite는 압축 손실이 눈에 보임 → None.
- **URP 2D Renderer 아닌 경우 검은 사각형**: Built-in pipeline 잔재 또는 Universal 3D Renderer면 2D Sprite가 검게 나옴 → Project Settings → Graphics → Default Render Pipeline 확인.
- **Grid by Cell Count vs Cell Size**: 정확한 픽셀 크기 알면 Cell Size, 모르면 Cell Count(가로/세로 칸 개수). spritesheet 위키 페이지에 명시되어 있음.
- **PPU 결정의 영향**: PPU 바꾸면 게임 내 캐릭터 크기 통째 바뀜 → 후속 Phase의 배경·UI 크기와 일관성. 한 번 정하면 마일스톤 내내 유지.
- **PNG 외 형식**: Kenney가 .ase / .pyxel / .ai 동봉 가능 → PNG만 Assets/에 넣기 (Unity가 다른 포맷 인식 X).

---

## ➡️ 다음 Phase

- **Phase 02 — Idle 애니메이션 클립 + Animator**: slice된 프레임으로 AnimationClip 만들고 Animator Controller wiring.

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
