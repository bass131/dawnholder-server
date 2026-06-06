---
owner: youngho
milestone: M4.4
phase: 02
title: 지형 물리 — Physics.Step 솔리드 AABB + one-way 플랫폼
status: done
grade: 복잡
estimated: 3~5h
domain: shared
summary: 평지(GroundY=0) 물리를 지형 충돌(솔리드 AABB + one-way 플랫폼)로 교체 — 서버·클라 prediction 단일 출처
---

# Phase 02: 지형 물리

> **상태**: done (2026-06-06 — -DONE.md 참조)
> **마일스톤**: M4.4
> **등급**: 복잡 (1 도메인이지만 물리 코어 교체 — 전 entity 이동 영향. **마일스톤 내 기술 난도 최고, 머지 전 /cross-review 후보**)
> **담당**: shared SubAgent (98_Shared 단독) + qa (단위 테스트)

---

## 🎯 목표

`Physics.Step`이 Phase 01 생성 지형 데이터를 받아 솔리드 AABB(수평 차단/착지/머리 충돌)와 one-way 플랫폼(아래서 통과, 위에서 착지)을 처리한다. 지형 데이터가 없으면 기존 평지(GroundY=0)로 동작(fallback — 기존 테스트 회귀 0).

---

## ⏪ 사전 조건

- [ ] Phase 01 — 생성 지형 데이터 (`MapTerrainData`) 박힘

---

## 📝 작업 내용

- [x] 지형 조회 타입 정의 (`MapTerrain` — Terrain.cs, ForMap(int) 1회 생성·틱 루프 배열 순회만)
- [x] `Physics.Step` 시그니처 확장 — **오버로드** 방식 (2-인자 유지 → 3-인자 위임, 호출부 12파일 무변경. null/빈 지형 = StepFlat fallback)
- [x] 수평 이동: 교차 판정 + 벽 스냅 (vx=0). 측면 차단 y∈[MinY,MaxY) — 바닥 윗면 보행 간섭 방지
- [x] 수직 하강: 솔리드 윗면+발판 면 중 최고 면 착지 (시작 y≥faceY−eps + newY≤faceY)
- [x] 수직 상승: 솔리드 아랫면 충돌 (**y<faceY 등호 제외 — 벽 모서리 점프 결함 봉합, 메인 세션 검수 발견**). 발판 통과
- [x] tunneling 검토: 점 모델 + 교차(crossing) 판정이라 스윕 불요 — 빠른 이동도 면 통과 시점을 잡음 (E-2 vy=-40 테스트 고정)
- [x] 단위 테스트 **19건** (계획 8~12 초과): fallback float 완전동일 2 / 착지·서기 / 벽 2 + 벽점프 회귀 / 천장 / one-way 3 / eps·tunneling·ledge / **대각 2 (reviewer 🟡 봉합)** / MapId 정합 2 (Phase 01 reviewer 🟡 봉합)
- [x] 기존 물리 테스트 전부 green — 오버로드라 호출부 수정 0 (전체 suite 381 통과 / 0 실패 / 4 skip)

---

## ✅ 완료 조건

- [x] 신규 지형 테스트 19건 green + 기존 테스트 회귀 0 (suite 362→381)
- [x] **이동속도/점프 상수 불변** — Constants/JumpSpeed 무변경 (Phase 04 소관 유지)
- [x] `dotnet build Dawnholder.slnx` green (경고 0/오류 0 — 호출부 무변경)
- [x] 지형 미주입 경로 = 기존 평지와 바이트 단위 동일 거동 (F-1/F-2 — float == 정확 비교 20틱)
- [x] ProtocolVersion 8 불변 (Protocol/ 무변경)

---

## 🧪 테스트

**자동**: `TerrainPhysicsTests` — 위 8~12 케이스. 서버 fixed dt(0.05)와 클라 가변 dt(예: 0.016) 양쪽 파라미터화
**수동**: 없음 (통합 실측은 Phase 03)

---

## 📚 학습 포인트

- **AABB 충돌 해소의 축 분리** — X 먼저/Y 먼저 처리 순서가 만드는 차이 (모서리 케이스)
- **one-way 플랫폼 판정** — "시작 위치가 면 위였는가 + 하강 중인가"가 핵심 (현재 위치만 보면 뚫고 올라가다 끼임)
- **tunneling** — 이산 시뮬레이션에서 빠른 물체가 얇은 벽을 건너뛰는 고전 문제와 스윕 해법
- **fallback 설계** — 코어 교체 시 기존 거동을 보존하는 안전망 (회귀 테스트가 그대로 살아있는 가치)

---

## ⚠️ 함정 / 주의사항

- **서버·클라 dt 불일치** — 같은 코드라도 dt가 다르면 충돌 시점이 미세하게 어긋남 → mispredict는 기존 reconcile(SnapThreshold 1.5)이 흡수하는지 Phase 03에서 실측. 여기선 양 dt 테스트만
- **epsilon 처리** — 기존 `GroundEpsilon` 패턴 유지. 면 위 판정에 등호 경계 명확히
- **헌법 #5** — 지형 조회는 배열 순회(할당 0). 틱 루프에서 LINQ/할당 금지
- 시그니처 변경은 **한 commit에 전 호출부 일괄** — 중간 상태로 빌드 깨진 commit 금지

---

## ➡️ 다음 Phase

- Phase 03 — 지형 통합 실측 (서버 GameMap + 클라 PlayerPredictor 연결)

---

## 📋 박제 (완료 후)

- **복잡 등급** — -DONE.md 박음. 머지 전 `/cross-review` 권장 (물리 코어 — 전 entity 영향)

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
- 2026-06-06 (세션12, 구현): shared SubAgent 구현(오버로드+StepFlat 보존+StepWithTerrain) → 메인 세션 검수에서 **벽 모서리 점프 불가 결함 발견·봉합**(상승 등호 제외 y<faceY) → qa SubAgent 테스트 17건 → reviewer 🔴0/🟡3(대각 테스트 권고 채택→2건 추가, dt Theory 확대·구조 분리는 보류 판정대로) → 최종 19건 + 전체 381/0/4skip. 점 모델 채택(캐릭터 폭 무시 — 발 위치 점), GroundY clamp 제거(구멍 낙하 = Phase 03 kill-plane 소관).
