---
owner: youngho
milestone: M4.4
phase: 04
title: 직업 이동 분리 — Physics 파라미터 주입 + LocalPlayerController 4분할
status: pending
grade: 복잡
estimated: 3~4h
domain: shared+server+client
summary: 직업별 MoveSpeed/JumpVel을 98_Shared 단일 출처로 양쪽 주입 + God class LocalPlayerController를 책임별 분리
---

# Phase 04: 직업 이동 분리

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 복잡 (3 도메인 터치이나 shared/server 측은 ~20줄 연결 수준 — 본질은 클라 구조 분리. 대규모 상향 여부는 plan-auditor 판단 위임)
> **담당**: shared SubAgent (PlayerStats/Physics) + server SubAgent (GameMap 연결) + client SubAgent (LPC 분할)

---

## 🎯 목표

전사(MoveSpeed 4)와 원거리(6)가 **서버 권위로 다르게 움직이고** 클라 prediction이 일치한다. `LocalPlayerController`(206줄, 4책임 God class)가 입력/이동/공격전략/서버응답으로 분리되어 직업 분기를 받을 구조가 된다. **프로토콜 변경 0.**

---

## ⏪ 사전 조건

- [ ] Phase 03 — 지형 통합 머지 (같은 `Physics.Step` 파일이라 직렬 — 충돌 회피)

---

## 📝 작업 내용

**shared (~15줄)**
- [ ] `PlayerStats`에 `JumpVel` 추가 (Warrior/Ranger factory에 직업값 — 단일 출처)
- [ ] `Physics.Step` 이동 파라미터 주입 (`Constants.MoveSpeed`/`JumpSpeed` 하드코딩 → 파라미터). ⚠️ **본 Phase는 회귀가 아니라 *미연결 값의 실연결*** (plan-auditor 🔴 봉합): 현재 전 직업이 `Constants.MoveSpeed=5.0`으로 움직이고 `PlayerStats.MoveSpeed`(Warrior 4/Ranger 6)는 죽은 값(β10) — 실연결 순간 **5.0 → 4/6 체감 변화는 의도된 정정**. 5.0 기준 기존 테스트는 직업값 파라미터로 갱신

**server (~10줄)**
- [ ] `GameMap` player tick이 `p.Stats.MoveSpeed`/`JumpVel`을 Step에 주입 (현재 미사용 값 실연결 — β10 발본)

**client (본체)**
- [ ] `LocalPlayerController` 분할: `LocalPlayerInput`(입력 콜백+비트필드) / `LocalPlayerMovement`(predict+50ms 송신+reconcile — 서버응답 흡수) / 공격은 `IAttackStrategy` 인터페이스로 위임(구현체는 Phase 05)
- [ ] `PlayerPredictor`에 직업 이동값 주입 경로 (값 출처는 Phase 05 ClassConfig — 본 Phase는 파라미터 통로만 + 임시 Warrior 고정)
- [ ] prefab 컴포넌트 교체 (LocalPlayer.prefab — 본인 확인 동반)

---

## ✅ 완료 조건

- [ ] `dotnet build` + `dotnet test` green (회귀 0)
- [ ] 서버 실측: 같은 입력으로 Warrior 4 vs Ranger 6 이동 거리 차이 (봇 or 2클라)
- [ ] mispredict 미증가 (reconcile snap 로그 기준 — 지형 위에서)
- [ ] 분할 후 각 클래스 책임 1개 (조작 코드에 직업 if-분기 0 — 구조 검증)
- [ ] ProtocolVersion 8 불변

---

## 🧪 테스트

**자동**: Physics 직업 파라미터 단위 테스트 (4 vs 6 거리/점프 높이) + 기존 prediction 테스트 회귀
**수동**: Play — 이동/점프/공격(임시 전략) 기존 체감 유지

---

## 📚 학습 포인트

- **God class 분리의 실전 기준** (CODE_CONVENTION §2.2) — "2+ 도메인 책임"을 컨테이너/시스템으로 나누는 과정
- **파라미터 주입 vs 전역 상수** — 같은 공식을 클래스별 값으로 재사용 (서버·클라 대칭 유지)
- **전략 패턴의 입구** — 인터페이스 먼저, 구현은 다음 Phase (구조와 내용 분리)

---

## ⚠️ 함정 / 주의사항

- 클라가 주입하는 직업값과 서버 `PlayerStats`가 **다르면 영구 드리프트** — 둘 다 98_Shared factory 단일 출처 강제 (클라 로컬 하드코딩 금지, 헌법 #4)
- 분할 중 `PlayerInput`(Unity Input System) 콜백 연결 끊김 주의 — prefab의 이벤트 바인딩 재확인
- `LocalPlayerController.Instance` 싱글톤 참조처 일괄 추적 (PendingSpawn race 처리 등)

---

## ➡️ 다음 Phase

- Phase 05 — 직업 장착 구조 (ClassConfig SO + 전략 구현 + Animator 교체)

---

## 📋 박제 (완료 후)

- **복잡 등급** — -DONE.md 박음

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
