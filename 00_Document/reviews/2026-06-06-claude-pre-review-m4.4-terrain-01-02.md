# Pre-Review for Codex β — 2026-06-06 — M4.4 Phase 01+02 (지형 bake + 지형 물리)

## 변경 범위

- 브랜치: `feature/m4.4-01-terrain-baker` vs `main` (base `954e028`)
- 등급: 복잡 ×2 Phase 묶음 (위험 깃발: **unity-asset** 씬 3 + **shared** 물리 코어 — 머지 전 /cross-review 권장 = Phase 02 정의 명시)
- 변경 파일 18 (+17,243/−1,941 — 대부분 씬 YAML 타일 데이터):
  - `98_Shared/GameData/Physics.cs` — **물리 코어** (Step 오버로드 + StepFlat 보존 + StepWithTerrain 신설)
  - `98_Shared/GameData/Terrain.cs` — TerrainAabb/TerrainPlatform struct + MapTerrain 조회 타입
  - `98_Shared/GameData/Generated/MapTerrainData.cs` — bake 생성물 (솔리드 AABB 24개, 손편집 금지)
  - `03_Client/Assets/Editor/TerrainBaker.cs` — 에디터 bake 도구 (씬 3개 → 생성 .cs)
  - `02_Server/GameServer.Tests/MapTerrainDataTests.cs`(13) + `TerrainPhysicsTests.cs`(19)
  - 씬 3 (Tilemap→Tilemap_Solid rename + 빈 Tilemap_Platform + 본인 레벨 디자인) + Shared.dll + Phase 문서 5

## diff 요약 (자연어)

1. Unity 씬 타일맵을 에디터 메뉴로 추출해 98_Shared 생성 C#(MapTerrainData)으로 박는 bake 파이프라인 (PacketGenerator와 같은 generated-artifact 패턴, 2회 bake diff 0 idempotent).
2. Physics.Step에 지형 경로 신설 — 점 모델, 축 분리(X 스윕 → newX로 Y 스윕), 교차(crossing) 판정이라 스윕 분할 없이 tunneling 방지. one-way 발판은 상승 검사 제외 + "시작 y가 면 위" 착지 조건으로 플래그 없이 강제.
3. 기존 2-인자 Step은 3-인자(null) 위임 → 호출부(서버 GameMap/클라 PlayerPredictor/봇) 무변경. StepFlat = 기존 본문 그대로 보존, fallback float 완전동일을 테스트(F-1/F-2)로 고정.
4. 지형 모드는 GroundY clamp 없음(구멍=무한 낙하) — kill-plane/지형 주입 배선은 Phase 03 이월 명시.
5. 검수 봉합 1건: 상승 머리충돌 `y <= faceY` → `y < faceY` (벽 모서리(y==벽.MinY==바닥 윗면)에 붙어 점프 시 바닥에 묻힌 벽 아랫면이 가짜 천장 → 점프 불가였음). W-3 회귀 고정.

## α (Claude reviewer) 결과 요약 — Tier 2-A ×2 (Phase별, 같은 세션)

- **Phase 01 (bake 도구)**: 🔴 0 / 🟡 2 — ① mapId↔서버 MapId enum 정합이 주석 약속으로만 존재 (Phase 02 테스트 M-1/M-2로 봉합 완료) ② enum-키 정합 어서션 부재 (동일하게 봉합 완료). "클라→Shared 쓰기 금지"는 빌드타임 코드 생성이라 비위반 판정 (PacketGenerator 동형).
- **Phase 02 (지형 물리)**: 🔴 0 / 🟡 3 — ① 대각 동시충돌 테스트 부재 (→ 즉시 채택, D-1/D-2 추가됨) ② dt Theory 확대 (보류 — Phase 03 실측과 함께) ③ Physics.cs 구조 분리 보류 판정 ("지금은 인라인이 정답", Phase 04 Rule of Three). 헌법 #5 할당 0 정밀 점검 통과 (new/LINQ/closure 0).
- 검증 실측: 클린빌드 경고0/오류0 + 전체 테스트 **381 통과 / 0 실패 / 4 skip** (기존 362 + 신규 32 − 13 기존… 정확히는 362+19) + bake idempotent diff 0 + Town 경계 스팟 8/8.

## Codex β 점검 가닥 (본인 직접 호출 시 참고)

- **StepWithTerrain 수치 정합** — 축 분리 순서(X 먼저)·교차 판정 경계(등호 포함/제외)의 엣지: 모서리 대각 진입 / 두 면 동시 후보 / eps 누적. α가 논리 시각이라 β의 정량 시각이 보완.
- **fallback 바이트 동일 주장 검증** — StepFlat이 정말 옛 본문과 동일한지 (diff로 직접 대조 가능).
- **결정론** — MathF 외 플랫폼 의존 / 부동소수 연산 순서 변경 여부.
- **TerrainBaker 좌표 변환** — CellToWorld + (x1+1, y1+1) 경계 산식 off-by-one (α는 Unity 실측 스팟 8곳으로 검증했지만 코드 시각 재확인 가치).
- **헌법 §1/§5** — 클라 권위 변경 없음 / 틱 루프 할당·블로킹 없음.
- **옛 사고 패턴** — 생성기-산출물 drift (생성 .cs + Shared.dll 동반 commit 여부), false-promise (주석 약속 vs 코드 실행).

## 본인 호출 명령어 (별 세션 터미널)

```bash
codex review --base main
```

결과는 raw 출력 또는 요약으로 Claude 세션에 전달 → γ 비교 진행.
