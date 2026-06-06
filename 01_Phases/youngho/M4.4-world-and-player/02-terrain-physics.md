---
owner: youngho
milestone: M4.4
phase: 02
title: 지형 물리 — Physics.Step 솔리드 AABB + one-way 플랫폼
status: pending
grade: 복잡
estimated: 3~5h
domain: shared
summary: 평지(GroundY=0) 물리를 지형 충돌(솔리드 AABB + one-way 플랫폼)로 교체 — 서버·클라 prediction 단일 출처
---

# Phase 02: 지형 물리

> **상태**: pending
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

- [ ] 지형 조회 타입 정의 (`MapTerrain` — 솔리드 AABB 목록 + one-way 세그먼트, 맵별)
- [ ] `Physics.Step` 시그니처 확장 — 지형 파라미터 추가 (기존 호출부 호환: 지형 null/empty = 평지 fallback)
- [ ] 수평 이동: 진행 방향 솔리드 변과 교차 시 벽에 스냅 (vx=0)
- [ ] 수직 하강: 솔리드 윗면 or one-way 윗면(시작 Y가 면 위 + vy≤0일 때만) 착지 → `onGround=true`
- [ ] 수직 상승: 솔리드 아랫면 머리 충돌 → vy=0. one-way는 통과
- [ ] tunneling 검토: 틱당 최대 이동량(이동·점프·낙하 최대 속도 × dt) vs 최소 지형 두께 — 초과 가능 조합이면 스윕(이동 경로 분할) 추가
- [ ] 단위 테스트 8~12건: 평지 회귀(fallback) / 단차 착지 / 벽 차단 / 머리 충돌 / one-way 상향 통과 / one-way 착지 / 모서리(epsilon) / 낙하 고속 tunneling 경계
- [ ] 기존 물리 테스트 전부 green (시그니처 변경 호출부 일괄 수정 — 02_Server·클라·봇·테스트)

---

## ✅ 완료 조건

- [ ] 신규 지형 테스트 8건+ green + 기존 테스트 회귀 0
- [ ] **이동속도/점프 상수 불변** — `Constants.MoveSpeed`(5.0)/`JumpSpeed` 파라미터화는 Phase 04 소관, 본 Phase는 지형만 (plan-auditor 경계 명시)
- [ ] `dotnet build Dawnholder.slnx` green (서버·봇·테스트 호출부 포함)
- [ ] 지형 미주입 경로 = 기존 평지와 바이트 단위 동일 거동 (fallback 검증 테스트)
- [ ] ProtocolVersion 8 불변 (프로토콜 무관 확인)

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
