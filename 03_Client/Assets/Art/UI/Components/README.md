# 공통 UI 조각 사용 안내

승인된 브라운 B1 조각 33개다. 시작점은 [import-spec.json](import-spec.json)의 sprites이며 각 조각의 크기·mode·border·본문 여백·PNG SHA256을 함께 찾을 수 있다. 다음 단계는 이 조각으로 창을 조합하는 작업이다. 이 폴더에는 창 코드·씬·프리팹·미리보기·생성기가 없다.

## 사용 순서와 단위

1. 같은 폴더의 PNG를 uGUI Image의 Source Image에 연결한다. meta는 Sprite(2D/UI), Single, FullRect, 중앙 pivot, PPU 200이다.
2. referencePixelsPerUnit 100, 배율 1을 기준으로 RectTransform 크기는 명세 uiSize를 사용한다. PNG는 2배 밀도이므로 PNG 512×384는 UI 256×192다. 다른 Canvas 배율을 적용할 때 화면상 크기는 별도로 확인한다.
3. 일반 Sliced 22개와 Divider 1개는 Image.type=Sliced로 쓴다(총 23개). Divider는 가로만 늘리고 높이는 **6 UI**로 고정한다. Simple 계열 10개는 Image.type=Simple, preserveAspect=true로 비율을 유지한다. Ornament_Corner는 MirrorXY, Ornament_HeaderEnd는 MirrorX를 명세대로 적용할 수 있다.
4. borderLRTB는 PNG px 그대로 Unity Vector4의 **(L,B,R,T)**에 대응한다. 밀도 2로 나누어 meta에 저장하지 않는다. contentInsetLRTB는 별도 본문 여백이며 UI 배치에서 2로 나눈다. 이 값을 sprite border로 넣지 않는다.

| 종류 | 수 | L/R/T/B (PNG px) | UI 사용 |
|---|---:|---|---|
| Panel | 3 | 24/24/24/24 | Sliced |
| Inset | 3 | 12/12/12/12 | Sliced |
| Header·Button·ItemSlot | 1+12+3 | 16/16/16/16 | Sliced |
| Divider_Horizontal | 1 | 16/16/0/0 | Sliced, 높이 6 UI |
| IconBase·CircleButton·NumberBadge·PortraitFrame·Ornament | 10 | 0/0/0/0 | Simple, 비율 유지 |

Sliced의 최소 합법 크기는 (L+R)/2 × (T+B)/2 UI다. 그 아래로 줄인 화면을 정상 사용으로 취급하지 않는다. Image.type·preserveAspect·RectTransform·Canvas 설정은 importer 저장 항목이 아니므로 자동 적용된다고 가정할 수 없다. **이 네 항목은 Image/레이아웃에서 별도로 설정한다.**

## 가져오기 설정과 보존

mipmap Off, sRGB/alpha transparency On, Bilinear, Clamp, NPOT 크기 유지, 기본 max 2048, 무압축, Read/Write Off다. 크기·압축을 바꾸는 플랫폼 override는 없다. 기존 HUD Panel/Slot의 PPU100·Multiple 설정을 복사하지 않는다. 고유 GUID는 함께 제공된 meta로 유지한다.

최종 PNG를 바이트 그대로 옮긴다. 원본 금 고리·전체 alpha·수락 녹색/거절 적색 각 4상태·D5 별 중앙 남색 장식은 그대로다. B1의 월넛/가죽풍 표면과 IconBase 고리 재합성은 승인 원천에서 끝난 작업이다. 이 반입에서는 새 생성·재색칠·리사이즈·alpha 청소를 하지 않았다.

## 승인 원천과 출처

저장소 루트 기준 원천: `.backups/verification/2026-10-13-demo-video-art/ui-kit-b1-ring-fix/package/kit`.
원천 명세 `kit-spec.json` SHA256: `5986771c0a75c9cefaeeedc097a06462a92ddc036596914a055f3d5ac5f18d91`.
`sprites[].file`은 원천의 상대경로를 그대로 보존하며, 이 폴더의 PNG를 찾을 때는 `assetFile`을 사용한다. PPU·밀도·mode·border·inset·크기는 원천의 계약값이며 새 Unity 실측치가 아니다.

역사 기록은 원천의 `production-history.json`, `raw/provenance.json`, `raw/original-provenance.json` 및 저장소 근거 폴더의 `ui-kit-production/raw/provenance.json`, `ui-kit-v2/raw/provenance.json`으로 추적한다. 그 hash와 기계 판독 출처표는 import-spec에 있다. 아래 경로·생성 시각은 당시 기록이고 원본/복사 hash와 크기는 이번 준비에서 다시 확인했다. 생성 도구가 이미지 모델 이름을 노출하지 않아 unknown을 유지한다. 작업자 모델과 이미지 모델은 다르다. 실제 생성 참고 그림은 승인 A 한 장이며 기존 HUD는 참고하지 않았다.

### Approved style A reference

| 도구 | 이미지 모델 | 생성 시작 → 종료 (UTC 원문) | 크기(px) |
|---|---|---|---|
| Codex built-in image_gen.imagegen | unknown (미노출) | Tue Oct 13 06:11:39 UTC 2026 → Tue Oct 13 06:12:00 UTC 2026 | 1536×1024 |

- 원본: `C:/Users/bass1/.codex/generated_images/01a133d4-ec6a-74d3-9e33-492058b8a84c/exec-0b6c49c5-26f6-468e-bdbd-608cdcd9c7cc.png`
- 원본/키트 복사 SHA256: `beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69` / `beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69`
- 키트 위치: `gen/selected-A-reference.png`; 참고 그림: []

### surface

| 도구 | 이미지 모델 | 생성 시작 → 종료 (UTC 원문) | 크기(px) |
|---|---|---|---|
| built-in image_gen.imagegen only | unknown (미노출) | 2026-10-13T13:54:51Z → 2026-10-13T13:56:16Z | 1254×1254 |

- 원본: `C:/Users/bass1/.codex/generated_images/01a13584-869a-70a2-9d50-af56a5086ca0/exec-869b8242-0c33-4351-8fc0-779da4485e81.png`
- 원본/키트 복사 SHA256: `89494cb83812d46becaddeae88e5d756f06d968a091ad494c7ade20ebb5f9751` / `89494cb83812d46becaddeae88e5d756f06d968a091ad494c7ade20ebb5f9751`
- 키트 위치: `gen/surface-source.png`; 참고 그림: [{"file":"gen/selected-A-reference.png","role":"approved A source, style and material only","sha256":"beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69"}]

### corner

| 도구 | 이미지 모델 | 생성 시작 → 종료 (UTC 원문) | 크기(px) |
|---|---|---|---|
| built-in image_gen.imagegen | unknown (미노출) | 2026-10-13T13:56:16Z → 2026-10-13T13:57:33Z | 1302×1208 |

- 원본: `C:/Users/bass1/.codex/generated_images/01a13584-869a-70a2-9d50-af56a5086ca0/exec-6838634d-2d5f-4a4e-be7c-9826a134f469.png`
- 원본/키트 복사 SHA256: `ccc1841adc4340926088ddd8e1562fcd83d5f9da5f85880eee10c2f0c09f9122` / `ccc1841adc4340926088ddd8e1562fcd83d5f9da5f85880eee10c2f0c09f9122`
- 키트 위치: `gen/corner-source.png`; 참고 그림: [{"file":"gen/selected-A-reference.png","role":"approved A style reference / edit source","sha256":"beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69"}]

### ring

| 도구 | 이미지 모델 | 생성 시작 → 종료 (UTC 원문) | 크기(px) |
|---|---|---|---|
| built-in image_gen.imagegen | unknown (미노출) | 2026-10-13T13:57:33Z → 2026-10-13T13:59:00Z | 1254×1254 |

- 원본: `C:/Users/bass1/.codex/generated_images/01a13584-869a-70a2-9d50-af56a5086ca0/exec-244b411b-f14d-4f9c-84b5-4da0577a2747.png`
- 원본/키트 복사 SHA256: `905801e1daa7f4a06affd7d542f98ca42eb76c726d4521d22b0fb71a59d4121c` / `905801e1daa7f4a06affd7d542f98ca42eb76c726d4521d22b0fb71a59d4121c`
- 키트 위치: `gen/ring-source.png`; 참고 그림: [{"file":"gen/selected-A-reference.png","role":"approved A style reference / edit source","sha256":"beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69"}]

### header-end

| 도구 | 이미지 모델 | 생성 시작 → 종료 (UTC 원문) | 크기(px) |
|---|---|---|---|
| built-in image_gen.imagegen | unknown (미노출) | 2026-10-13T13:59:01Z → 2026-10-13T14:00:11Z | 2172×724 |

- 원본: `C:/Users/bass1/.codex/generated_images/01a13584-869a-70a2-9d50-af56a5086ca0/exec-0ba8337c-a2de-478d-bfe0-63ff1acdd090.png`
- 원본/키트 복사 SHA256: `d615a62b320c30907fe5376a9965359da941151c48259c7e4f0eb6afdc9dd153` / `d615a62b320c30907fe5376a9965359da941151c48259c7e4f0eb6afdc9dd153`
- 키트 위치: `gen/header-end-source.png`; 참고 그림: [{"file":"gen/selected-A-reference.png","role":"approved A style reference / edit source","sha256":"beeb3c79232ffafc6a1d7e04aa537b09891b22a5c71e2357857f245321ca9a69"}]


## 최종 조각 목록

| PNG | PNG px | 기본 UI 크기 | 최종 PNG SHA256 |
|---|---|---|---|
| Panel_Default.png | 512×384 | 256×192 | 0e91be5fc7d15467fc9cf1b2c98fe621a4dacc255fc6f107cd81f96158a3e167 |
| Panel_Emphasis.png | 512×384 | 256×192 | 42f83c6196753ca98e9827a4c3bbb7702f4f5305cafea65882cdb598ed2faeb2 |
| Panel_Dialog.png | 512×384 | 256×192 | 4fd7d75d21370e5cde20af3ab5bbc0c172ac414f7b83969afff9218c7aa36a5b |
| Inset_Default.png | 192×128 | 96×64 | e6c0507eb16efd633011b73eb10ef743b100f27630e47998f3e040686ad02220 |
| Inset_Selected.png | 192×128 | 96×64 | 1a662ed0208c587c5cbfcdf8fbb8f50b5ef3710a6893d17290eff2133091d086 |
| Inset_Disabled.png | 192×128 | 96×64 | 483b0e0110b8cc6c85b575a7a01e10612ae957564c10284dd95a88a1f69d3422 |
| Header.png | 384×80 | 192×40 | b102ddcb312d5471b7117a9dab26ab74a5e10091332ba04f16578192a9894cb8 |
| Button_Default_Normal.png | 256×96 | 128×48 | 7d8ae7d641ad793d54bc5f0774151311b02cb979024920d7f9f24b3c26ccc49e |
| Button_Default_Highlighted.png | 256×96 | 128×48 | f8a4cd66fc34106ac6bbf0965f189fe3411dae622fb10a6d9b707397cb5e8b9d |
| Button_Default_Pressed.png | 256×96 | 128×48 | b44e54008bb82e5902835d9c47fecc1fc1390c30e48a0280e226ff96350daceb |
| Button_Default_Disabled.png | 256×96 | 128×48 | 950daa52e17f70bcf360093950f1a1478e4c425db48fc2b8dfac704c3cd334e5 |
| Button_Accept_Normal.png | 256×96 | 128×48 | 337b52d28762a586832a08a7dbcf907f8a50bdf8aa23a89bbe730c1544258de8 |
| Button_Accept_Highlighted.png | 256×96 | 128×48 | c504ce98c7558038b2e0d3f1bd0dc5e7c27c8427a63e4e52687ef80fd4d393c9 |
| Button_Accept_Pressed.png | 256×96 | 128×48 | 6c62b138f3b631754425d9e68b81b0f8e07b8c22b4b8d4fe1ed227c59e994cf3 |
| Button_Accept_Disabled.png | 256×96 | 128×48 | dac87078c460d1165cc20d15a978ab76671821946e4e69af12339197b0848f3d |
| Button_Decline_Normal.png | 256×96 | 128×48 | 2e5fcc9a904249bfadda2f44601b547c2c5ce19d09b2b3952cbea42b087b51e1 |
| Button_Decline_Highlighted.png | 256×96 | 128×48 | 0b749782b7a1648125a6b7533aec2fe19fd2a8d8cba8b2371fffe7a1e6a66a14 |
| Button_Decline_Pressed.png | 256×96 | 128×48 | 9af23d9fa9709bf3a3f01041efc0f02a2b2174119cd773fb75460f6c125f9f2e |
| Button_Decline_Disabled.png | 256×96 | 128×48 | 9d5976a53705d414659c2e54a645d2d30f996d8197a2e550dfbeaa52efd98e2c |
| ItemSlot_Empty.png | 128×128 | 64×64 | 55de5d67d500ab866e63ee71fe2616818f2293c75e626ffa53b7cc61ffec3a93 |
| ItemSlot_Filled.png | 128×128 | 64×64 | 7576eb3e85ecec6739a3910350050ce8f86b4c55dab0333d360e59baba59c131 |
| ItemSlot_Selected.png | 128×128 | 64×64 | 20e4fca9f37f43029d827e31ff1737600e23cc30a7bcb8f6e85eb582f87269b5 |
| IconBase_64.png | 128×128 | 64×64 | c915019f32255a40be3c2f1f505d72f51877250085d8e5182a3811a0867e7796 |
| IconBase_96.png | 192×192 | 96×96 | 4371a658716d7cb91553ed01416ed1119ef1b1a4f8c1d9ab94a638db29003c04 |
| CircleButton_Normal.png | 128×128 | 64×64 | 8a4e5bdea51d9773b16298a97f18ae5a4388f51d037d8b6cc017d9ddac42ce1d |
| CircleButton_Highlighted.png | 128×128 | 64×64 | 97aa2d7026557821216a8f36a7a4d75fbcf29f56a4a4afa448cec73485b2545f |
| CircleButton_Pressed.png | 128×128 | 64×64 | 41e458ffe6bf9aedb5f2a246614597a43c50b1b39165f9bdc8bf892b5638d8ec |
| CircleButton_Disabled.png | 128×128 | 64×64 | 7199ba862ebaa451f29748ae5124f38fbc49bbe4791470b717f06a5449ae4d9c |
| NumberBadge.png | 96×96 | 48×48 | fc01fb893835e3a9d6bb17efd246e24d7f75e035be4f637fe4315e549b201a5e |
| PortraitFrame_128.png | 256×256 | 128×128 | 9b60f36a348cfd88a62508bcfa0f426a39591b2e3c23ecb4d2bf1582db23870a |
| Divider_Horizontal.png | 256×12 | 128×6 | c81aed6fd72542080814a87c2189df9d39523ef4b59b67b5fc466a0e6a11d8b9 |
| Ornament_Corner.png | 96×96 | 48×48 | 1ec4f967578031ff41ed3faffc52987ab494db32f613a40ca94abb8984598ec8 |
| Ornament_HeaderEnd.png | 192×64 | 96×32 | c7814044846e55506f45308ef63ee50fa7dd488600fd9900c0b9b1734f841ebc |

## 다음 단계와 확인 한계

각 창에서 Image·여백·장식 반전·상태 연결을 적용하는 작업은 별도다. 12장의 소개 미리보기와 생성 이력 전체는 Assets에 넣지 않았다. 이 문서와 meta의 정적 대조는 실제 Sprite 로드·uGUI 확대 화면·독립 검증·게임 화면 판정을 대신하지 않는다. 후속 독립 검증은 실제 Sprite와 임시 uGUI fixture에서 원본 크기/1.5배/2배, Divider, Simple 비율 및 잘못된 임시 객체 대조군을 확인한다.
