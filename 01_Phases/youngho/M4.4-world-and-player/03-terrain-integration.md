---
owner: youngho
milestone: M4.4
phase: 03
title: 지형 통합 실측 — 서버+prediction 연결 + 스폰 조정 + 봇 회귀
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~4h
domain: server+client
summary: 생성 지형을 서버 GameMap과 클라 PlayerPredictor에 연결하고 세 씬 Play 실측 + 봇 회귀로 검증
---

# Phase 03: 지형 통합 실측

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 복잡 (2 도메인 + 씬 스폰 조정 unity-asset)
> **담당**: server SubAgent (GameMap 연결) + client SubAgent (predictor 연결) + 본인 (스폰 위치·씬 검수·Play 실측)

---

## 🎯 목표

서버와 클라 prediction이 같은 생성 지형 위에서 동작한다 — 세 씬의 언덕·단차·공중 지형 위에서 이동/점프/착지가 Play로 정상이고, reconcile drift 봇 회귀가 통과한다. **상호작용 지형의 범위가 확정**된다.

---

## ⏪ 사전 조건

- [ ] Phase 02 — 지형 물리 + 단위 테스트 green
- [ ] **상호작용 지형 실물 확인 선행** (유현 씬에서 어떤 오브젝트인지 — 미확인 상태로 진입하면 범위 확정 항목이 본 Phase를 블록. 포탈이면 기존 C_EnterPortal 재사용으로 즉결)

---

## 📝 작업 내용

- [ ] 서버: `GameMap`이 자기 MapId의 `MapTerrain`을 보유하고 player tick의 `Physics.Step` 호출에 주입
- [ ] 서버: enemy 이동(EnemyAISystem)의 Y 처리 점검 — 적은 평탄 구간 스폰 전제(지형 추적 X, scope 컷). 스폰 좌표가 새 지형의 지면과 맞는지 `MapSpawnTable` 수치 조정
- [ ] 클라: `PlayerPredictor`에 현재 맵 지형 주입 (맵 전환 시 갱신 — S_MapTransition 흐름에 연결)
- [ ] 본인: 세 씬에서 플레이어/적 스폰 위치를 새 지형 위로 조정 + 씬 저장 (스폰이 지면 아래/공중에 박혀있으면 즉사 모양)
- [ ] 본인+유현: **상호작용 지형 실물 확인 → 범위 확정** (포탈이면 기존 C_EnterPortal 재사용 / 그 외면 M4.5+ 이월 명시 — 결정 박제)
- [ ] 봇 회귀: 기존 이동 시나리오가 새 지형 맵에서 PASS (봇도 같은 Physics라 자체 시뮬 일치 검증 = reconcile drift 검증)
- [ ] Play 실측: 언덕 오르내리기 / 단차 점프 착지 / 공중 플랫폼 아래서 점프 통과 + 위 착지 / 맵 전환 후 지형 정상

---

## ✅ 완료 조건

- [ ] 세 씬 Play 실측 체크리스트 전 항목 통과 (멈춤/벽끼임/공중부양 0)
- [ ] 봇 이동 시나리오 PASS (드리프트 snap 미발생 — 로그 기준)
- [ ] 서버 단독 진실 확인: 클라 prediction을 죽여도(스냅만) 서버 위치가 지형 위 정상
- [ ] 상호작용 지형 범위 결정이 본 문서 작업 로그에 박힘
- [ ] `dotnet test` 회귀 0

---

## 🧪 테스트

**자동**: 봇 이동 시나리오 (지형 맵), 서버 통합 테스트 (스폰 → 이동 → 단차 착지 좌표 검증 1건+)
**수동**: Play 실측 체크리스트 (위 작업 내용 마지막 항목)

---

## 📚 학습 포인트

- **prediction-서버 대칭의 가치** — 같은 코드+같은 데이터면 지형이 복잡해져도 reconcile이 평지 때와 동일하게 동작
- **스폰 데이터와 지형의 결합** — 지형이 바뀌면 스폰 좌표도 자산(데이터)이라 같이 움직여야 함 (미래 맵 에디터 마일스톤의 복선)

---

## ⚠️ 함정 / 주의사항

- **씬 수정 = unity-asset 깃발** — 스폰 조정 전 씬 백업 (Phase 08 BackGround 사고 학습)
- 맵 전환 시 클라 지형 갱신 누락 → 이전 맵 지형으로 예측해 드리프트 폭증 (S_MapTransition 핸들러 경로 확인)
- 적 스폰이 공중/지면 아래면 AI가 이상 동작 — 서버 로그로 적 Y 확인
- **콘솔 경고는 타임스탬프 먼저** (세션8 학습 — stale 로그 헛다리)

---

## ➡️ 다음 Phase

- Phase 04 — 직업 이동 분리 (같은 Physics.Step 위 작업이라 본 Phase 머지 후 착수)

---

## 📋 박제 (완료 후)

- **복잡 등급** — -DONE.md 박음

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
