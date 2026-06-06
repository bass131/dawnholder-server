---
owner: youngho
milestone: M4.4
phase: 02
title: 지형 물리 — Physics.Step 솔리드 AABB + one-way 발판
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.4 Phase 02 완료 (commit c5e615e). Physics.Step에 지형 경로 신설 — MapTerrain 조회 타입 + StepWithTerrain(점 모델, 축 분리 X→Y, 교차 판정 = tunneling 방지). 오버로드 방식이라 기존 호출부 12파일 무변경, StepFlat(기존 본문 보존) fallback은 float 완전동일 테스트로 고정. 메인 세션 검수가 벽 모서리 점프 불가 결함(상승 등호 케이스)을 구현 직후 발견·봉합. 테스트 19건(벽점프 회귀/one-way 3종/tunneling/대각 2) + 전체 381/0/4skip + reviewer 🔴0/🟡3(대각 권고 채택). 서버·클라 지형 주입은 Phase 03.
---

# Phase 02 박제: 지형 물리

**소요**: 세션12 후반 (Phase 01 직후 연속 — shared/qa SubAgent 분담 + 메인 검수)

## TL;DR

평지(GroundY=0)뿐이던 `Physics.Step`이 Phase 01 생성 지형(솔리드 AABB + one-way 발판)을 소비할 수 있게 됐다. 설계 갈래 셋이 핵심: ① **오버로드** — 시그니처를 깨는 대신 2-인자 Step을 3-인자(null)로 위임시켜 호출부 12파일을 건드리지 않았고, 지형 주입은 Phase 03에서 명시적으로 배선. ② **교차(crossing) 판정** — "면을 사이에 두고 건너갔는가"를 보므로 점 모델에서는 고속 낙하도 면을 못 뚫는다(스윕 분할 불요 — vy=-40 테스트 고정). ③ **one-way를 플래그 없이** — 상승 검사에서 발판을 아예 안 보고, 하강 착지는 "이번 틱 시작 y가 면 위"일 때만 후보로 잡아 아래→위 통과·위→아래 착지가 조건 하나로 강제된다.

## 박제 사실

- `98_Shared/GameData/Terrain.cs` — `MapTerrain` sealed class (Solids/Platforms readonly 배열, `ForMap(int)` — 맵 로드 1회 생성, 틱 루프는 배열 순회만)
- `98_Shared/GameData/Physics.cs` — `Step(state, input)` → `Step(state, input, null)` 위임 / `StepFlat`(기존 본문 그대로 이동) / `StepWithTerrain`(지형 경로 ~150줄)
- `02_Server/GameServer.Tests/TerrainPhysicsTests.cs` — 19건 (qa 17 + 메인 세션 대각 2)
- commit `c5e615e` (Shared.dll 동반). 분담 = shared SubAgent 구현 / 메인 세션 검수·봉합 / qa SubAgent 테스트 / reviewer 통합 점검

## AC 검증 결과

- `dotnet build Dawnholder.slnx --no-incremental` → 경고 0 / 오류 0 (호출부 무변경 — 서버 GameMap·클라 PlayerPredictor·봇·기존 테스트 전부 2-인자 그대로)
- `dotnet test --no-build` → **381 통과 / 0 실패 / 4 skip** (기존 362 + 신규 19, 기존 PhysicsTests 회귀 0)
- fallback 바이트 동일: F-1/F-2 — 20틱 시퀀스(정지·이동·점프·낙하)에서 2-인자 vs 3-인자(null/빈 지형) 결과 float `==` 정확 비교 통과
- 핵심 회귀 고정: 벽-점프(W-3) / one-way 통과·착지·아래-비차단(P-1~3) / 고속 낙하 tunneling vy=-40(E-2) / 대각 모서리 진입 2건(D-1/D-2) / MapId enum 정합 + ForMap ReferenceEquals(M-1/M-2 — Phase 01 reviewer 🟡 2건 봉합)
- reviewer Tier 2-A: **🔴 0 / 🟡 3** — ① 대각 테스트 부재(→ 즉시 채택, D-1/D-2 추가) ② dt Theory 확대(보류 — Phase 03 실측과 함께) ③ 구조 분리 보류 판정("지금은 인라인이 정답", Phase 04에서 세 번째 유사 순회 등장 시 Rule of Three)
- ProtocolVersion 8 불변 / 이동·점프 상수 불변 (Phase 04 경계 준수)

## 결정 흐름 (회고 참고용)

- **오버로드 vs 시그니처 일괄 변경** — 일괄 변경은 "모든 호출자가 지형을 의식"하게 강제하지만 12파일 동시 수정 + 중간 깨진 commit 위험. 오버로드는 Phase 02를 98_Shared 단독으로 닫고 주입을 Phase 03의 명시적 배선으로 미룸. 단점 = 2-인자 호출이 남아있는 동안 "지형 모름" 상태가 조용히 허용됨 → Phase 03 완료 조건에서 흡수.
- **점 모델 (캐릭터 폭 무시)** — 현 Step 의미론(발 위치 점 + GroundY clamp)과 일관. 폭 있는 캡슐은 비용 대비 캡스톤 범위 초과. 시각 폭과 판정 차이는 Phase 03 실측에서 관찰.
- **교차 판정이 스윕을 대체** — 점 모델에서는 "구간이 면을 건넜는가"가 곧 연속 충돌 검사라 이동 경로 분할이 불필요. 폭 모델로 가면 재검토.
- **상승 충돌 등호 제외 (y < faceY)** — 착지·스냅이 face 값에 *정확히* 박히는 구조라 등호 케이스(벽 모서리에 붙어 섬)가 일상 도달 상태. 등호 포함이면 바닥에 묻힌 벽 아랫면이 가짜 천장이 돼 점프 불가. AI 구현 직후 메인 세션 검수가 발견 — SubAgent 산출물 검수의 가치 실증.
- **GroundY clamp 제거 (지형 모드)** — 지형 구멍 = 무한 낙하 허용. kill-plane/스폰 보정은 Phase 03 소관으로 명시 이월.

## 막혔던 지점 / 이월

- **Phase 03 이월**: ① 서버 GameMap + 클라 PlayerPredictor + 봇에 지형 주입(`MapTerrain.ForMap((int)MapId)` 캐스팅 경유 — 정합 코드화) ② kill-plane/스폰 보정 ③ dt 불일치 mispredict가 reconcile(SnapThreshold 1.5)로 흡수되는지 실측 ④ dt Theory 확대(reviewer 🟡②)
- **머지 전 /cross-review 권장** (Phase 정의 — 물리 코어 전 entity 영향): 본인 별 세션 codex 호출 분담. PR 전략(01+02 묶음 vs Phase별)은 사용자 결정 대기.
- qa SubAgent가 work-pin을 임의 갱신하며 브랜치명을 잘못 기록(`feature/m4.3-...`) — 메인 세션이 실측 정정. SubAgent의 pin 갱신은 메인 세션 소관으로 유지해야 함을 재확인.

## 학습 일지 후보 키워드

축 분리 충돌 해소(X 먼저, newX로 Y 평가) / 교차 판정 vs 오버랩 판정(tunneling의 뿌리) / one-way 발판 = "시작 상태 기준" 판정(구간으로 보기) / 등호 경계가 일상 도달 상태일 때의 함정 / 오버로드 fallback 설계(기존 거동 보존 + float 완전동일 테스트) / SubAgent 산출물 메인 검수 가치

## 다음 Phase

- **Phase 03 — 지형 통합 실측** (서버·클라 지형 주입 + 스폰 조정 + 봇 회귀 + 상호작용 지형 범위 확정 — 유현 씬 실물 확인 선행)
