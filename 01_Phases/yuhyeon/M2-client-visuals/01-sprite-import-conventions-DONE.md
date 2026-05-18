---
summary: Knight_player_1.4 폴더 36개 PNG 모두 픽셀아트 학습 컨벤션(PPU 64 / Filter Point / Compression None / Sprite Multiple) 통일. Idle_KG_1.png 4 frame slice + GameplayTest 씬 Player GameObject 배치 + Scene View 캡처로 또렷 렌더 시각 검증. drift 5건 발견-자동 일괄 fix 패턴 박힘.
phase: 01-sprite-import-conventions
work-id: yuhyeon-m2-phase01-sprite-import-conventions
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 01 — Sprite Import 컨벤션 + 캐릭터 spritesheet 완료 박제

**소요 시간**: ~1.5시간 (본인 손작업 50분 + drift 발견 자동 fix 20분 + 검증 박제 20분)

## TL;DR

본인이 Knight_player_1.4(@Jump_Button) 자원에서 Idle_KG_1.png을 *학습 컨벤션*(PPU 64 / Filter Point / Compression None / Sprite Mode Multiple)으로 import + Sprite Editor에서 4 frame slice + Player GameObject 신설해 GameplayTest 씬에 정적 배치까지 진행. 검증 중 *나머지 35 PNG는 default 상태(drift)*임을 MCP RunCommand로 발견 — "단위 정상성 ≠ end-to-end" 패턴(M1 Phase 04 디버깅 사건 재발견). 자동 일괄 fix(RunCommand 한 번에 36 PNG 컨벤션 통일) + 재검증 36/36 ok로 마감. Phase 02·03에서 Walking·Run sprite를 일관 PPU/Filter로 쓸 수 있는 토대 확보.

## 5단계 보고

- **무엇을 만들었나** — Knight_player_1.4 폴더 36 PNG 모두 학습 컨벤션 일치(PPU 64 / Filter Mode Point / Compression Uncompressed / Sprite Mode 보존 / Wrap Clamp / sRGB True / Mipmap off / Alpha Is Transparency on). Idle_KG_1.png 4개 sub-sprite(knight14_idle_00~03, 100×64 each) slice. GameplayTest 씬에 Player GameObject(SpriteRenderer + LocalPlayerController + PlayerInput) 배치 + Sorting Layer Default Order 1.
- **왜 필요한가** — Phase 02~04(애니/이동)에서 Walking·Attack·Hurt 등 다른 동작 PNG들을 자원으로 쓸 때 PPU/Filter가 PNG마다 다르면 *캐릭터 크기 비례 깨짐* + *프레임 흐림*. 마일스톤 시작에 *전수 일관*을 박아 후속 Phase의 시각 불일치 사고 예방. M1 Phase 04 *fake-null + worldPositionStays* 사건의 교훈(데이터 일점에서 검증 ≠ end-to-end 보장) 재적용.
- **어떻게 만들었나** — 결정 1: 자원 = Knight_player_1.4 (@Jump_Button), 학습 임시용 / 출시본 = 팀원 제작 중. 라이선스 = 개인+상용 OK + 크레딧 + AI 학습 X. 결정 2: PPU = 64 (frame 64px / 캐릭터 1유닛, OrthoSize 5와 조화). 결정 3: drift 발견 후 *본인 multi-select 일괄* vs *Claude MCP 자동 일괄* → 학습 1회로 충분 + 면담 1일 압박 = MCP 자동. RunCommand로 *Directory.GetFiles + TextureImporter.SaveAndReimport* 36개 일괄(35 changed + 1 unchanged). 새 개념 = TextureImporter API + spriteImportMode 보존 + AssetDatabase.Refresh 흐름.
- **테스트 결과** — MCP 검증 3축:
  - Console 에러/경고: 0건
  - Idle_KG_1.png 실측: PPU=64, spriteMode=Multiple, filter=Point, compression=Uncompressed, wrap=Clamp, sRGB=True (4 sub-sprites)
  - Scene View 카메라 캡처: 캐릭터 sprite *또렷한 픽셀*로 렌더, Filter Point 효과 시각 확인 (블러 X)
  - drift 재검증: 36/36 ok, 0 drift
  - Player position (0,0,0), Camera Orthographic size 5 pos (0,0,-10) 정상
- **다음 스텝** — Phase 02 진입 (Idle AnimationClip 생성 + Animator Controller wiring). knight14_idle_00~03 4 frame이 이미 slice 완료라 *드래그→GameObject* 워크플로우 즉시 가능. Phase 02 시작 전 핀 갱신.

## AC 검증 결과

```bash
# 1. 폴더 git 추적 — 본인 확인
$ ls 03_Client/Assets/Art/Characters/Knight_player_1.4/Knight_player/*.png | wc -l
   36

# 2-3. MCP RunCommand 1: Idle_KG_1.png import + Player setup 검증
Active scene: GameplayTest (Assets/Scenes/GameplayTest.unity)
Player at (0.00, 0.00, 0.00)
Sprite: knight14_idle_00 (PPU=64, rect=100x64)
Sorting Layer: Default, Order: 1, Enabled: True
Idle_KG_1.png: type=Sprite, spriteMode=Multiple, ppu=64, filter=Point,
              compression=Uncompressed, wrap=Clamp, sRGB=True
sub-sprite[0]: knight14_idle_00 (100x64)
sub-sprite[1]: knight14_idle_01 (100x64)
sub-sprite[2]: knight14_idle_02 (100x64)
sub-sprite[3]: knight14_idle_03 (100x64)
Sub-sprites count: 4
Main Camera: ortho=True, size=5, pos=(0.00, 0.00, -10.00)

# 2. MCP RunCommand 2: 다른 PNG drift 검사 (학습 컨벤션 위반 발견)
DRIFT: Walking_KG_1.png — spriteMode=Multiple, ppu=100, filter=Bilinear, compression=Compressed (sub-sprites: 7)
DRIFT: Attack_KG_1.png — ppu=100, filter=Bilinear, compression=Compressed (sub-sprites: 6)
DRIFT: Crouching_KG_1.png — ppu=100, filter=Bilinear, compression=Compressed (sub-sprites: 3)
DRIFT: Hurt_KG_1.png — ppu=100, filter=Bilinear, compression=Compressed (sub-sprites: 4)
DRIFT: Idle_KG_2.png — ppu=100, filter=Bilinear, compression=Compressed (sub-sprites: 4)
Summary: consistent=0, drifted=5 (of 5 checked)

# 3. MCP RunCommand 3: 자동 일괄 fix (36 PNG)
Updated: Attack_KG_1.png ... Wallside_KG_1.png (총 35건)
Summary: changed=35, unchanged=1 (Idle_KG_1), skipped=0 (total=36)

# 4. MCP RunCommand 4: 재검증
Final verification: ok=36, drift=0, total=36

# 5. Scene View 캡처 (Unity_Camera_Capture)
이미지 시각 확인: 캐릭터 또렷한 픽셀, Filter Point 효과 정상, 블러 0
```

## 결정 흐름

- 자원 선택: Kenney CC0 vs Knight_player_1.4 → **Knight 선택** (frame 풍부 4/8 + 본인 친숙 + 학습 임시용 + 출시본 팀원 제작 중). 라이선스 검토 후 진행.
- PPU 결정: 32 vs 64 → **64** (frame 64px → 캐릭터 1 유닛, OrthoSize 5와 화면 균형 적절). 본인 처음 32 박았다 64로 정정.
- 씬 작업 위치: Gameplay.unity(공유) vs GameplayTest.unity(sandbox) → **GameplayTest** 신설 (ad-hoc Phase로 분리, Phase 01 scope 보존).
- drift 발견 후 fix 방식: 본인 multi-select vs MCP 자동 → **MCP 자동** (학습 1회로 충분 + 면담 1일 압박).

## 막혔던 지점

- **drift 5건 발견** — Idle_KG_1.png만 본인이 컨벤션 박았고, 나머지 35 PNG는 Unity default(PPU 100 / Bilinear / Compressed) 상태. 본인이 한 PNG 학습 후 *나머지는 됐겠지* 가정 → end-to-end 검증 누락. **M1 Phase 04 worldPositionStays 사건과 같은 패턴**(데이터 일점 검증 ≠ 전체 일관). 해결: MCP RunCommand로 36 PNG 일괄 fix + 재검증 0 drift.
- **Unity MCP 연결 revoke** — 세션 시작 시점 MCP가 revoke 상태(Unity 재시작 후 권한 reset 추정). 본인이 Unity Editor > Project Settings > AI > Unity MCP Server에서 Accept 후 재연결 성공. 본인 셋업 가이드 line 210-213에 박힌 "Free 체험 종료 함정"은 다행히 아님.

## 학습 일지 후보 키워드

- `unit-correctness-vs-end-to-end-revisited` — ★★★ 면접 가치. M1 Phase 04 fake-null + worldPositionStays 사건의 *재발견*. 한 PNG 학습 후 나머지 검증 누락 = 같은 패턴. *Rule of Two* 성립 → `/journal:concept` 펼침 후보.
- `texture-importer-batch-api` — TextureImporter + SaveAndReimport + AssetDatabase.Refresh 자동화 패턴. 학습 1회 후 반복은 자동화 가성비 ★.
- `ppu-vs-orthosize-balance` — PPU 64 + OrthoSize 5 = 화면 세로 ~10 유닛, 캐릭터 1 유닛 키 비례 학습. Camera·Sprite 두 축이 짝.
- `mcp-runtime-verification` — Claude MCP RunCommand로 *시각 결과 + 데이터*를 한 번에 회수. 본인 육안 검증보다 *정확성* 강점. M1 Phase 04 디버깅 사건 자동화 도구의 *왜* 확장.
- `learning-convention-vs-default` — "Unity default가 픽셀아트엔 함정"(Bilinear/Compressed). 첫 PNG부터 *컨벤션 명시*가 후속 모든 PNG의 토대.
