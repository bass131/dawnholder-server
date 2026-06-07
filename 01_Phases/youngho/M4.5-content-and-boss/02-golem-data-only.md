---
owner: youngho
milestone: M4.5
phase: 02
title: 골렘 추가 — EnemyKind=2, 데이터만 (엔진 변경 0 검증)
status: in-progress
grade: 복잡
risk: unity-asset
estimated: 1.5~2h
domain: shared+server+client
---

# Phase 02: 골렘 추가 — 데이터만으로 새 적

> **상태**: pending
> **마일스톤**: M4.5
> **등급**: 복잡 (다도메인 — 단 각 도메인 ~20줄 데이터 수준)
> **담당**: server + shared SubAgent (데이터) + unity-bridge (씬 마커/prefab 행) + 메인 검수

---

## 🎯 목표

세 번째 적 종류 **골렘**(느리고 단단한 탱커)을 추가하되, **AI/전투/스폰 엔진 코드 변경 0**으로 해낸다. M4.4 바이너리 파이프라인 + Phase 01 시각 테이블이 "콘텐츠 추가 = 데이터 추가"라는 약속을 지키는지 검증하는 Phase. 끝나면 HG에서 골렘이 순찰/추격한다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (`EnemyVisualTable` — 골렘 행을 추가할 테이블)
- [x] M4.4-03 content.bin 파이프라인 (씬 마커 → bake → 서버 스폰)

---

## 📝 작업 내용

- [ ] `EnemyKind.Golem = 2` append (`02_Server/GameServer/Combat/EnemyKind.cs:10` — append-only 주석 약속 이행)
- [ ] **결정 포인트 — EnemyKind 중복 정의 봉합**: 현재 서버(`EnemyKind.cs`)와 클라(`RemoteEnemy.cs:16~20`)에 같은 enum이 중복 정의됨(헌법 #4 위반 소지 — 프로토콜로는 byte cast만 오감). 골렘 추가로 양쪽 수정이 강제되는 지금이 98_Shared 이사 적기인지 검토 → 이사 시 Shared.dll 동반 commit, 보류 시 사유 박음 (작업 로그). **이사 채택 시 Phase 04 bump 묶음으로 합류 옵션 우선 검토** (plan-auditor 🟡 — 02에서 이사하면 Shared.dll이 02·04 두 번 흔들려 클라/봇/CI 재빌드 비용 ×2, 04 한 묶음이면 1회)
- [ ] `EnemyStats.GolemDefault()` factory (`98_Shared/GameData/Formulas.cs` — NormalDefault 패턴): 느림(MoveSpeed < 2.0) + 단단함(MaxHp/Defense ↑) + 좁은 aggro — 구체 수치는 구현 시 결정 후 박제
- [ ] `EnemyDefaultHp.ByKind` 테이블 골렘 행 (`GameMap.cs:16`)
- [ ] HG 씬 `Spawn_Enemy_Golem` 마커 저작(평탄 구간 — M4.4 결정: 적은 평탄 스폰) + 재bake + bin 두벌·씬 동반 commit (Town/BR 무변경 idempotent 확인)
- [ ] `EnemyVisualTable` 골렘 행 + `Enemy_Golem` prefab (M4.3 Phase 11 본인 제작 Golem 클립 5종 연결)
- [ ] 봇/테스트: 기존 EnemyAiSmoke가 골렘 포함 배치에서도 PASS (적응형 선정 — M4.4-03 학습 활용), 골렘 스탯 단위 테스트

---

## ✅ 완료 조건

- [ ] `git diff`에 AI/전투/스폰 *엔진* 코드(.cs 로직) 변경 0 — enum append + 데이터 factory + 테이블 행만 (목표 위반 시 사유 박제)
- [ ] HG에서 골렘 순찰/추격/사망 Play 실측 (Golem 애니 5종 구동)
- [ ] bake idempotent (Town/BR bin 무변경) + bin 두벌 identical
- [ ] `dotnet test` green (골렘 스탯 신규 테스트 포함) + 봇 EnemyAiSmoke PASS
- [ ] **ProtocolVersion == 8 유지** (bump 0 — kind는 byte cast라 프로토콜 무변경)

---

## 🧪 테스트

**자동**: GolemDefault 스탯 단언 + 기존 enemy AI/전투 회귀 + 봇 EnemyAiSmoke
**수동**: Play — HG 골렘 조우 → 추격 → 처치 풀 루프

---

## 📚 학습 포인트

- **"콘텐츠 추가 비용"이 아키텍처의 성적표** — 골렘 하나에 엔진 코드를 고쳐야 하면 M4.4~M4.5-01 구조가 거짓이었다는 뜻. 데이터 4곳(enum/스탯/씬/시각)만 만지는 게 목표
- **enum 중복 정의의 냄새** — 서버/클라가 같은 enum을 따로 들고 있으면 한쪽만 고치는 사고가 시간문제. 공유 코드(98_Shared)로 올리는 비용 vs 방치 위험의 trade-off

---

## ⚠️ 함정 / 주의사항

- **EnemyKind는 append-only** (stability 약속) — 기존 0/1 값 변경 절대 금지
- **씬 수정 → 재bake → bin 두벌+씬 동반 commit** (M4.4 의무 — idempotent 확인 4회째)
- 골렘 스폰을 경사/공중에 놓으면 M4.4 결정(적 = 평탄 스폰) 위반 — 마커 face 확인
- 클라 RemoteEnemy enum에 골렘 누락 시 미등록 kind 폴백(Phase 01) 발동 — 폴백 로그가 뜨면 누락 신호

---

## ➡️ 다음 Phase

- Phase 03 — UI 연결 (본 Phase와 의존성 0, 병렬 가능)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `02-golem-data-only-DONE.md` 박음

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18)
