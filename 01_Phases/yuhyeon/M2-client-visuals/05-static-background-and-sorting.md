# Phase 05: 정적 배경 1장 + Sorting Layer 정리

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1시간
> **담당 에이전트**: client

---

## 🎯 목표

Kenney.nl에서 정적 배경 1장을 받아 Gameplay 씬에 배치하고, Sorting Layer 4개(Background / Default / Foreground / UI)를 박아 캐릭터가 배경 위에 정상 표시되게 한다.

**끝나면 데모 가능한 것**: Gameplay 씬에 배경(예: 산/숲) + 그 위에 움직이는 캐릭터. 캐릭터가 절대 배경 뒤로 숨지 않음.

---

## ⏪ 사전 조건

- [ ] Phase 01~04 완료 (캐릭터 sprite + 이동 + 애니메이션 wiring)
- [ ] Kenney.nl에서 2D 배경 sprite 1장 다운 (예: "Background Elements Redux" 등)

---

## 📝 작업 내용

- [ ] **폴더 신설**: `03_Client/Assets/Art/Environment/`
- [ ] **배경 sprite 임포트**:
  - PNG를 위 폴더에 드래그
  - Texture Type = Sprite (2D and UI)
  - Sprite Mode = Single (한 장이라)
  - PPU = Phase 01과 동일 (16 또는 32 — 캐릭터와 비례 맞춤)
  - Filter Mode = Point
- [ ] **Sorting Layer 박제**: Edit → Project Settings → Tags and Layers → Sorting Layers
  - 위에서부터 박는 순서대로 *뒤*에서 *앞*: `Background` / `Default`(기본 존재) / `Foreground` / `UI`
  - `+` 버튼으로 Background를 Default 위에 추가, Foreground를 Default 아래 추가, UI를 최하단 추가
- [ ] **배경 GameObject 신설**: Hierarchy에 빈 GameObject(`Background_Mountain`) + SpriteRenderer
  - Sprite 슬롯에 임포트된 배경 할당
  - Sorting Layer = **Background**
  - Order in Layer = 0
  - Transform.Position = (0, 0, 0) — z는 sorting과 무관 (2D는 layer로 깊이 표현)
- [ ] **캐릭터 Sorting 확인**: Player GameObject의 SpriteRenderer
  - Sorting Layer = **Default**
  - Order in Layer = 0
- [ ] **카메라 background 정리**: Main Camera → Inspector → Camera → Environment → Background Type = Solid Color, 색은 검정 또는 어두운 회색 (배경 sprite가 화면 채우지 않을 때 보일 영역)
- [ ] **배경 크기 조정**: 배경이 카메라 시야 채우도록 Transform.Scale 또는 카메라 OrthoSize 조정 (배경 작으면 검은 띠 보임)
- [ ] **시각 검증**: Play → 배경 + 캐릭터 동시 보임, 캐릭터가 배경 *앞*에 있음

---

## ✅ 완료 조건

- [ ] `Assets/Art/Environment/{배경}.png` + `.meta` git 추적
- [ ] Project Settings → Sorting Layers에 4개 박힘 (Background / Default / Foreground / UI)
- [ ] Play 진입 시 배경 + 캐릭터 둘 다 보임, 캐릭터 z-order가 배경 *위*
- [ ] 캐릭터가 좌우 이동해도 배경 뒤로 절대 숨지 않음
- [ ] 검은 띠 안 보임 (배경이 화면 채움)

---

## 🧪 테스트

**수동 테스트:**
1. Play → 배경 + 캐릭터 동시 가시 확인
2. A/D 이동 → 캐릭터가 배경 위에서 움직임 (절대 뒤로 안 숨음)
3. Scene 뷰에서 배경 GameObject 잠시 비활성 → 캐릭터만 보이는지(다른 sprite 가린 게 아닌지)
4. 카메라 zoom 변경 시 검은 띠 안 보이게 OrthoSize 미세조정

**자동 테스트:** 없음

---

## 📚 학습 포인트

- **2D의 깊이 표현 = Sorting Layer + Order in Layer** (3D는 카메라 거리 z).
  - Sorting Layer = *큰 단위* 그룹 (배경/캐릭터/UI 같은)
  - Order in Layer = *같은 Layer 안* 미세 조정 (같은 Default 안에서도 어떤 게 앞)
- **Layer (Tags and Layers의 Layer)** vs **Sorting Layer** — 둘 다 "Layer"라 헷갈리지만 다른 개념:
  - Layer = Physics 충돌·Camera Culling용. Tag와 비슷한 분류.
  - Sorting Layer = 2D 렌더 순서 *전용*.
- **z축 비활용**: 2D에선 z를 따로 안 쓰는 게 컨벤션. 모든 z=0. 깊이는 Sorting으로.
- **Camera Background Type**: Solid Color(2D 표준) / Skybox(3D) / Don't Clear(post-process trick).
- **Camera Orthographic Size**: 2D는 Orthographic 카메라. OrthoSize = 화면 세로 / 2 (유닛). PPU와 곱하면 화면 세로 픽셀 수.

---

## ⚠️ 함정 / 주의사항

- **Sorting Layer 등록 안 한 채 사용**: 코드에서 `sortingLayerName = "Background"` 같이 쓰는데 Project Settings에 미등록이면 "Default"로 fallback + 경고 X → 의도와 다른 렌더. 항상 등록 먼저.
- **배경 크기 < 화면**: 검은 띠 보임 → Transform.Scale 키우거나 OrthoSize 줄임. Scale은 픽셀 비례 깨질 수 있음(Filter Mode Point면 블록처럼 보임) → 보통 OrthoSize 조정 권장.
- **z 값 활용 유혹**: "캐릭터를 z=-1로 두면 앞으로 오겠지" — 작동은 하지만 2D 컨벤션 깨짐. 다른 팀원이 보고 혼란. Sorting으로 통일.
- **Order in Layer 음수**: 가능. -1 = Order 0보다 뒤. 박스 정렬 미세조정에 자주 씀.
- **Sorting Layer 추가 순서 = 렌더 순서**: 리스트 위쪽이 *뒤*, 아래쪽이 *앞*. Background 맨 위, UI 맨 아래.
- **여러 배경 레이어(parallax) 욕심**: 면담 데모는 *정적 1장*이 정답. 패럴랙스(여러 배경이 다른 속도로 스크롤)는 M2 범위 밖.

---

## ➡️ 다음 Phase

- **Phase 06 — TMP 한글 Font Asset 생성 + Fallback**: 한글 폰트 도입 (메뉴 한글화 준비).

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
