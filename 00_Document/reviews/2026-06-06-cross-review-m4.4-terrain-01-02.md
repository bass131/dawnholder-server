# Cross-Review — 2026-06-06 — M4.4 Phase 01+02 (지형 bake + 지형 물리)

## 변경 범위

- 브랜치 `feature/m4.4-01-terrain-baker` vs `main`(`954e028`) — 18파일 (+17,243/−1,941, 대부분 씬 YAML)
- 핵심: Physics.cs 지형 경로 + Terrain.cs(MapTerrain) + 생성 MapTerrainData.cs + TerrainBaker.cs + 테스트 32건(13+19) + 씬 3 + Shared.dll
- 등급: 복잡 ×2 묶음 (위험 깃발: unity-asset + shared 물리 코어 → Phase 02 정의가 머지 전 /cross-review 명시)

## α — Claude reviewer 결과 (Tier 2-A ×2, 같은 세션 — 본 diff 전체 커버라 재사용)

- Phase 01: 🔴 0 / 🟡 2 — mapId↔enum 정합 주석 약속 + 정합 어서션 부재 → **둘 다 Phase 02 테스트 M-1/M-2로 봉합 완료**
- Phase 02: 🔴 0 / 🟡 3 — ① 대각 동시충돌 테스트 부재 → **즉시 채택 (D-1/D-2)** ② dt Theory 확대(보류 — Phase 03 실측과 함께) ③ 구조 분리 보류 판정(인라인 유지, Phase 04 Rule of Three)
- 헌법 #5 할당 0 정밀 통과 / fallback float 완전동일 테스트 고정 / 검증: 클린빌드 0/0 + 전체 381/0/4skip

## β — Codex 결과 (본인 직접 호출, `codex review --base main`)

- **P0/P1/P2 블로커 없음** — 머지 차단급 결함 미발견
- P3-1: ClientNet.dll 변경됐는데 04_ClientNet 소스 diff 없음 (근거 없는 binary churn 의심)
- P3-2: MapTerrainData public static 배열이 mutable reference로 노출 — 현재 읽기 전용 사용이라 즉시 버그 아님, Phase 03+ 호출자가 수정하면 전역 충돌 데이터 오염 가능. IReadOnlyList/ReadOnlyMemory 후속 고려
- 추가 확인(전부 통과): Shared.dll에 새 타입 실재 / MapId 0·1·2 ↔ MapTerrainData ↔ SceneRouter 3자 정합 / GameMap·PlayerPredictor 2-인자 유지 = Phase 03 의도 정합 / 씬 3 Tilemap_Solid+Platform 실재 / ToneGrade_Test 미참조
- 테스트 재실측 X (read-only sandbox — 사전 문서의 381 pass는 참고만)

## γ 비교 분석

- **양쪽 다 잡음: 0건**
- **α만 잡음: 2건** (🟡 dt Theory 확대 / 구조 분리 주시 — 둘 다 보류 판정 박힘, Phase 03·04 좌표 명시)
- **β만 잡음: 2건**
  - P3-1 churn → **메인 세션 실측 정정: 브랜치 diff(main...HEAD)에 ClientNet.dll 미포함.** 워킹트리 미커밋 churn(클린빌드 산물, 04_ClientNet 소스 무변경)이고 commit 위생에서 이미 제외 운영 중. β가 워킹트리 기준으로 본 것 — *사실 정확, PR 비포함이라 조치 불요*
  - P3-2 mutable 배열 노출 → **유효한 지적 (α는 zero-alloc 시각으로 칭찬만, β가 변조 위험 시각 보완 = γ 가치 실증).** 본인 판단 = **Phase 03 이월** (사유: 현재 호출자 0 + 생성 파일이라 bake마다 재생성 + Phase 03에서 첫 소비자 배선할 때 ReadOnlySpan/관례 중 결정이 자연스러움. work-pin 박음)
- **양쪽 다 통과**: 헌법 #1(런타임 권위 무변경)/#4(빌드 양쪽 green)/#5(할당 0) / 결정론 / fallback 동일성 / MapId 3자 정합 / bake off-by-one / 생성물-DLL 동반 commit
- 메인 세션 추가 실측: 런타임 스크립트의 "Tilemap" 이름 참조 0건 — 씬 rename 런타임 안전

## Step 4-A (봉합 후 재실측) / 4-B (실측 검증)

- 4-A: β 발견 2건 모두 코드 봉합 없음 (1건 사실 정정, 1건 명시 이월) → 재실측 비대상
- 4-B: 본 PR은 런타임 거동 불변 (지형 경로 미배선 — fallback float 완전동일 + 381 테스트 + M4.3 Phase 12 봇 6/6). **지형 경로의 Play 실측은 Phase 03 완료 조건에 이미 박혀 있음** (언덕/단차/발판/맵전환 + 봇 회귀) — 실측 의무는 Phase 03으로 명시 이월

## 결정 권유

- 🔴 양쪽 다 잡음 0개 + 단독 발견은 정정·이월 처리 완료 → **GO (01+02 묶음 PR 머지 권장)**

## 옛 학습 정합

- 생성기-산출물 drift: 생성 .cs + Shared.dll 동반 commit 확인 (β 추가 확인 통과)
- false-promise: mapId 정합이 주석 약속 → 테스트 어서션으로 코드화 (M-1/M-2)
- binary churn 위생: 의도 churn은 commit 제외 + pin 명시 운영이 β 시각에서도 재확인됨
