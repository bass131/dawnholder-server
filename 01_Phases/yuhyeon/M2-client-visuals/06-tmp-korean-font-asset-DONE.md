---
summary: Pretendard Std OTF + TMP_FontAsset.CreateFontAsset API로 SDF Atlas(2048×2048, Dynamic mode, SDFAA) 생성 → TMP Settings Default Font Asset + Fallback에 박음. MCP 자동화 한 번에 끝남(~10분). 한글 음절 11,172자가 런타임 첫 만남 시 Atlas에 추가되는 패턴.
phase: 06-tmp-korean-font-asset
work-id: yuhyeon-m2-phase06-tmp-korean-font-asset
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 06 — TMP 한글 Font Asset 완료 박제

**소요 시간**: ~15분 (Phase 05의 7층 함정 대비 *완벽 짧음*)

## TL;DR

Pretendard Std OTF(SIL OFL, 7MB 가벼움) 1개 → `TMP_FontAsset.CreateFontAsset` API + Dynamic Atlas(2048×2048, SDFAA)로 SDF Font Asset 자동 생성. TMP Settings의 Default Font Asset + Fallback 둘 다 박아 *모든 새 TMP_Text가 한글 자동 렌더*. Atlas Population Mode Dynamic이라 *런타임 첫 만남 시 글리프 추가* — 정적 11,172자 미리 베이크 안 함, 메모리 작음.

## 5단계 보고

- **무엇을 만들었나** — `Assets/Fonts/PretendardStd-Regular.otf` (본인 박음, SIL OFL) + `Assets/Fonts/PretendardStd-Regular SDF.asset` (TMP_FontAsset, SDFAA Atlas 2048×2048 Dynamic) + TMP_Settings.defaultFontAsset = Pretendard + TMP_Settings.fallbackFontAssets에 추가.
- **왜 필요한가** — 면담 1일 압박 + 한국 면담관 *즉시 친숙도 ↑↑*가 Phase 04+05 시각 핵심 통과 후 *마지막 임팩트 레버*. 폰트 인프라가 Phase 07(메뉴/HUD 한글화) 토대. SDF + Dynamic Atlas는 *크기 자유 + 메모리 최적*의 표준 패턴.
- **어떻게 만들었나** — 자원 선택: PretendardStd-1.3.9.zip (7MB, 한글+영문만, 모던 깔끔). GOV/JP는 과함, Pretendard 일반(45MB)도 과함. API: `TMP_FontAsset.CreateFontAsset(font, 90, 9, SDFAA, 2048, 2048, Dynamic, true)`. TMP_Settings 박기: SerializedObject `m_defaultFontAsset` + `m_fallbackFontAssets` 두 곳 동시 → 이중 방어. MCP 자동화 한 번에 끝남(Phase 05 시각 디버깅 7층 대비 극단 대조).
- **테스트 결과** — MCP 검증: Font 로드 / FontAsset 생성 / TMP_Settings Default + Fallback 박힘 확인 (4건 로그 정상). 본인 시각 검증(임의 TMP_Text "안녕하세요")은 Phase 07 진입 시 자연스럽게 검증됨 — 기존 메뉴/HUD TMP_Text의 Font Asset 교체 + 한글 텍스트 변경하면 즉시 결과 보임.
- **다음 스텝** — Phase 07 즉시 진입 (MainMenu/PauseMenu/HUD TMP_Text Font Asset 교체 + 한글 텍스트). MCP 자동화 가능. M2 마감 시 LICENSE.txt 동봉(SIL OFL 의무) — 본인이 zip에서 안 가져온 듯, 면담 후 정리.

## AC 검증 결과

```bash
# 1. Font OTF 로드
Loaded font: PretendardStd-Regular

# 2. SDF Font Asset 생성
Created Font Asset: Assets/Fonts/PretendardStd-Regular SDF.asset
  Atlas: 2048x2048
  Atlas Population Mode: Dynamic (런타임 글리프 추가)
  Render Mode: SDFAA (SDF + Anti-Aliasing)
  Sampling Point Size: 90
  Padding: 9

# 3. TMP Settings 박기
Default Font Asset 박힘: PretendardStd-Regular SDF
Fallback에도 추가됨 (이중 방어)

# 4. 본인 시각 검증
Phase 07에서 자연스럽게 검증 (메뉴/HUD 한글 텍스트 변경 후 시각 결과)
```

## 결정 흐름

- 폰트 선택: PretendardStd (7MB) vs Pretendard 일반(45MB) vs GOV(107MB) vs JP(101MB) → **Std** (한글+영문 음절만, 게임 메뉴 충분, 빌드 크기 ↓).
- Atlas: Static vs Dynamic → **Dynamic** (런타임 글리프 추가 → 메모리 작음, 11,172자 정적 베이크 회피).
- 렌더: SDFAA vs SDF Mono → **SDFAA** (Anti-Aliasing — 부드러움, Unity TMP 표준).
- TMP_Settings: Default만 vs Default + Fallback → **둘 다** (이중 방어 — Default 못 찾는 케이스라도 Fallback 작용).

## 막혔던 지점

- **TMP_FontAsset.CreateFontAsset 파라미터 이름** — `sampling` 키워드 인자 X (CS1739) → positional 인자로 호출. Unity 6.4 TMP API 시그니처: `(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport)`. *API 시그니처 정확 외우기 어려움 → positional이 안전 패턴*.
- **LICENSE.txt 누락** — SIL OFL 임베드 의무. 본인이 zip 다운 시 root LICENSE 안 가져옴. M2 마감 시 추가 commit.

## 학습 일지 후보 키워드

- `tmp-fontasset-create-api` — `TMP_FontAsset.CreateFontAsset` API + 8 positional args 의미. SDF + Dynamic Atlas 표준 패턴.
- `tmp-settings-default-vs-fallback` — Default Font Asset 못 찾는 케이스 fallback chain. 다국어 처리에 핵심.
- `dynamic-vs-static-atlas-tradeoff` — 한글 11,172자 정적 베이크 vs 런타임 추가. 메모리·시작 성능 trade-off.
- `sil-ofl-license-discipline` — SIL Open Font License 임베드 시 *LICENSE 동봉 의무*. M2 마감 시 정리 메모.
- `phase05-vs-phase06-difficulty-contrast` — ★ 시각 시스템(P05) 7층 함정 vs 폰트 API(P06) 10분 끝. *Unity 시스템마다 함정 깊이 다름* 메타 학습.
