---
summary: FREE_Fantasy Forest 4 layered 배경(Sky/Clouds/Rock Mountains/Grass Mountains) GameplayTest 씬 배치 + Sorting Layer 4개(Background/Default/Foreground/UI) 등록 + Camera Background 하늘색. 시각 디버깅 7층 함정(Light/Material/중복/parent transform/Sprite-Lit→Unlit/Pivot/Camera 시야) 통과해 합격선 시각 도달.
phase: 05-static-background-and-sorting
work-id: yuhyeon-m2-phase05-static-background-and-sorting
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 05 — 정적 배경 + Sorting Layer 완료 박제

**소요 시간**: ~2시간 (계획 1h → 시각 디버깅 누적)

## TL;DR

FREE_Fantasy Forest(개인+상용 OK 라이선스) 4 배경 PNG(Sky/Clouds/Rock Mountains/Grass Mountains, 320×320 PPU 32)를 *layered parallax-ready*로 배치. Sorting Layer 4개(Background/Default/Foreground/UI) TagManager SerializedObject로 등록. **시각 디버깅 7층 함정** 통과: URP 2D Sprite-Lit material이 Light Target Sorting Layers에 Background 미포함 → 검정 / spritesheet 단순 박기로 중복 sub-sprite / Bg_Mountains 부모 transform (-4.6,-4.5) scale 0.5 잔재 / sprite 그림 영역이 sprite의 하단 1/3에 한정. *Sprites-Default(unlit) material + Camera Background 하늘색* 이중 방어로 합격선 시각 도달. Mountains는 작가 자원 색이 Clouds와 비슷해 분리 약함 — 면담 후 정리 예정.

## 5단계 보고

- **무엇을 만들었나** — `Assets/Art/Environment/FREE_Fantasy Forest/Backgrounds/` 4 PNG import 통일(Sprite/PPU 32/Filter Point/Compression Uncompressed/Pivot Center/Sprites-Default material) + Sorting Layers TagManager 등록(Background→Default→Foreground→UI 순서) + Camera Background Color 하늘색 + Camera ClearFlags SolidColor + GameplayTest 씬에 4 Bg_* GameObject(pos(0,0) scale 2 Background layer order 0/1/2/3).
- **왜 필요한가** — Phase 04 캐릭터 + 이동 + 애니 + flipX 위에 *배경 레이어*를 얹어 면담 데모 시각 임팩트 ↑. Sorting Layer 4개 박제는 M3+ 멀티플레이어 + 컨텐츠 진입 토대(Background → Default(캐릭터·몹) → Foreground(파티클·앞쪽 사물) → UI). Light Target Sorting Layers 함정 봉합 = URP 2D *시각 학습 깊이*.
- **어떻게 만들었나** — 결정 1: 자원 = 본인 보유 FREE_Fantasy Forest. 결정 2: 4 layer parallax-ready 배치 (Phase 05 scope "1장"보다 풍부, Sorting Layer 학습 진짜 활용). 결정 3: URP 2D Sprite-Lit이 검정 렌더(Light Target Layers 미포함) → **Sprites-Default(unlit) material로 교체** + 이중 방어로 Camera Background 하늘색. 결정 4: Pivot Center vs Bottom 시도 → **Pivot Center + pos(0,0) scale 2** 단순 합격선 채택. 새 개념 = TagManager SerializedObject 통한 Sorting Layer 동적 등록 + AssetDatabase.GetBuiltinExtraResource로 built-in material 로드 + SetParent 함정 부모 transform 잔재 패턴.
- **테스트 결과** — MCP 다축 + 본인 시각 회귀:
  - 4 Bg_* GameObject 생성 (Sky/Clouds/Rock/Grass, Background layer order 0/1/2/3) ✓
  - 4 PNG Sprites-Default material + Pivot Center ✓
  - Sorting Layers 4개 TagManager 등록 + 순서 [Background, Default, Foreground, UI] ✓
  - Camera Background Color 하늘색 + ClearFlags SolidColor ✓
  - 본인 Play 회귀: "정상 작동해 다음 작업 진행하자" — 시각 합격선 통과
- **다음 스텝** — Phase 06 진입 (TMP 한글 Font Asset 도입). 면담 1일 압박 + 메뉴 한글화가 한국 면담관 *즉시 친숙* → 면담 임팩트 ↑↑. Phase 06 + Phase 07(메뉴/HUD 한글화) 묶음 처리 검토. M2 마감 시 Mountains 시각 정리(작가 자원 색 분리 어려움 — 면담 후) + MainMenuController TODO 복원.

## AC 검증 결과

```bash
# 1. Sorting Layers 등록 (TagManager SerializedObject)
Existing: [Default]
Added: Background, Foreground, UI
Final order: [Background, Default, Foreground, UI]

# 2. 4 PNG import 통일
Sky.png, Clouds.png, Rock Mountains.png, Grass Mountains.png
  → Sprite/PPU 32/Filter Point/Compression Uncompressed/Pivot Center/sRGB True

# 3. 4 Bg_* GameObject 신설
Bg_Sky:            pos(0,0,0) scale(2,2,1) sortingLayer=Background order=0 material=Sprites-Default
Bg_Clouds:         pos(0,0,0) scale(2,2,1) sortingLayer=Background order=1 material=Sprites-Default
Bg_RockMountains:  pos(0,0,0) scale(2,2,1) sortingLayer=Background order=2 material=Sprites-Default
Bg_GrassMountains: pos(0,0,0) scale(2,2,1) sortingLayer=Background order=3 material=Sprites-Default

# 4. Camera 설정
pos=(0,0,-10) orthoSize=5 bgColor=(0.42,0.78,0.96) clearFlags=SolidColor

# 5. Player/Square 정렬
Player: sortingLayer=Default order=1 (배경 위)
Square: sortingLayer=Default order=0 (배경 위)

# 6. 본인 Play 회귀
"정상 작동해 다음 작업 진행하자" — 시각 합격선 통과
```

## 결정 흐름

- 자원 = 본인 보유 FREE_Fantasy Forest (라이선스 개인+상용 OK, 재판매 X, NFT X, 크레딧 권장).
- 4 layer parallax vs 1장 → **4 layer** (Sorting Layer 학습 진짜 활용 + 면담 임팩트).
- Sprite-Lit vs Sprites-Default → **Sprites-Default(unlit)** (Light Target Layers Background 미포함 함정 우회 + 결과 보장).
- Pivot Center vs Bottom Center → **Center + pos(0,0)** (단순 + 검증된 시각).
- Mountains 시각 더 fix vs 합격선 마감 → **합격선 마감** (면담 1일 압박, 작가 자원 색 분리 어려움).
- Camera Background Color = 하늘색 (Sky sprite 보완 이중 방어).

## 막혔던 지점 — ★★★ 시각 디버깅 7층 함정 (면접 결정타)

1. **URP 2D Sprite-Lit material + Light Target Sorting Layers 미포함** — Player(Default Layer)는 Light 작용해 보이는데 Background Layer는 Light 작용 X → 검정 렌더. Sprites-Default(unlit) material로 우회. *Player와 같은 material 사용했는데 다른 결과* = "같은 코드/설정 ≠ 같은 결과" Light 시스템 함정.
2. **importer.spritesheet 단순 박기 → 중복 sub-sprite 9개** (Phase 03 Walking과 같은 패턴 — Single 모드 flush 트릭 안 거치면 잔재).
3. **`Bg_Mountains` 부모 GameObject transform 잔재** — pos(-4.6,-4.5) scale 0.5 박혀 자식 world 좌표 왜곡. Hierarchy 구조 디버깅 시 *부모 transform 의심* 마인드셋.
4. **Bg_RockMountains (1), Bg_GrassMountains (1) 중복 자식** — root scan만으론 못 잡음. 재귀 순회로 자식까지 검사.
5. **sprite 그림 영역이 sprite center가 아닌 sprite 하단 1/3** — Mountains의 산 silhouette이 sprite 중앙 X. Pivot/위치 계산 시 *sprite 그림 영역*과 *sprite mesh 영역* 구분 의식.
6. **Camera 시야 vs sprite 영역 mismatch** — OrthoSize 5 = 시야 x[-8.9,+8.9] y[-5,+5], sprite scale 2 = world 20×20. sprite center 영역만 보이고 *sprite 가장자리 그림*은 시야 밖. Scale/Position 계산 = 항상 *Camera 시야 좌표*와 비교.
7. **Scene View 캡처 ≠ Game 카메라** — MCP Camera_Capture는 Scene View 시점. *진짜 검증*은 Play 모드 + Game 카메라.

## 학습 일지 후보 키워드

- **`urp-2d-light-target-sorting-layers`** — ★★★ URP 2D Light2D의 *Target Sorting Layers* 명시 필요. Sprite-Lit material 의존하면 *어떤 sortingLayer*에 작용할지 결정. Default Layer만 박혀있으면 Background sprite 검정. M2 학습 절정.
- **`sprite-lit-vs-default-material-tradeoff`** — Light 의존(톤 조절·night mode 가능) vs Light 무관(보장된 색). 배경은 unlit이 *컨벤션*.
- **`tagmanager-serializedobject-sortinglayers`** — Sorting Layer 동적 등록 패턴. TagManager.asset SerializedObject 통한 m_SortingLayers 배열 갱신. Unity API 직접 노출 X라 SerializedObject 우회.
- **`scene-view-vs-game-camera-trap`** — ★ MCP Camera_Capture가 Scene View라 Game 시야와 다름. *진짜 검증*은 Play 모드. Phase 05 디버깅 시간 절반의 원인.
- **`sprite-image-area-vs-mesh-area`** — sprite의 *그림 픽셀 영역*과 *mesh 영역*은 다름. 작가가 sprite 한구석에만 그림 그려두면 *mesh center*에 그림 없음. Pivot/위치 계산 의식.
- **`parent-transform-leftover-trap`** — Bg_Mountains 부모 transform이 자식 world 좌표 왜곡. *Hierarchy 디버깅 시 부모부터 검증* 마인드셋.
- **`unit-correctness-vs-end-to-end-revisited`** — Phase 01~04에서 박은 키워드, Phase 05의 *7층 함정 누적*으로 **Rule of Six 도달**. M2 마감 시 `/journal:concept`로 펼침 = *6 사례 통합 면접 답변* 압도적 강도.
