---
owner: youngho
milestone: M4.5
phase: 04
title: 보스 프로토콜 + 서버 행동 — S_EnemyAttack + class append + 8→9 bump + BossBehavior
status: done
grade: 대규모
risk: irreversible
estimated: 4~6h
domain: shared+server
---

# Phase 04: 보스 프로토콜 + 서버 행동

> **상태**: done (2026-06-07 — 박제 = `04-boss-protocol-and-server-DONE.md` + `.html`)
> **마일스톤**: M4.5
> **등급**: 대규모 (복잡 기본 + irreversible[8→9 bump] + trust-boundary[적→플레이어 데미지] 깃발 2개 상향)
> **담당**: shared (PDL) + server (behavior) SubAgent + Coordinator 분해 + 메인 검수

---

## 🎯 목표

옛 M4.3-09 설계를 계승해 **맞기만 하던 보스를 스스로 공격하는 보스**로 만든다: 페이즈 1/2(HP 50% 임계) 패턴 FSM + tick 카운터 쿨다운 + 범위 내 플레이어 서버 권위 데미지 + 사망 정책(스폰 리스폰 + HP full). 프로토콜은 **본 마일스톤 유일 bump(8→9)** 한 묶음: `S_EnemyAttack` 신설 + `S_PlayerJoin` characterClass append. 끝나면 봇이 보스방에서 맞고 HP가 깎인다 (클라 연출은 Phase 05).

---

## ⏪ 사전 조건

- [x] M4.3-07/08a enemy AI 인프라 + animState 채널 (보스 attack = animState로 송신)
- [x] 옛 M4.3-09 정의 (사망 정책 plan-auditor 봉합 완료 상태로 이월)
- [ ] Phase 02 완료 권장 (EnemyKind 중복 정의 결정이 먼저 정리되면 보스 코드가 그 위에)

---

## 📝 작업 내용

### 공유 (shared) — 한 묶음 bump
- [ ] PDL append-only: `S_EnemyAttack` 신설 — `attackerId, targetId, damage, targetCurrentHp, attackPattern(byte)` (클라 이펙트 분기 힌트). **PDL 경로 = `99_Tools/PacketGenerator/PDL.xml`** (98_Shared가 아님 — PacketGenerator가 `98_Shared/Protocol/Generated/`로 생성, plan-auditor 🟡 경로 혼선 봉합)
- [ ] PDL append-only: `S_PlayerJoin`에 `characterClass(byte)` append (원격 플레이어 직업 표시용 — Phase 05 소비)
- [ ] **ProtocolVersion 8→9 bump** (본 마일스톤 유일 — 이후 Phase bump 0)
- [ ] **옛 박제 주석 정정 의무** (plan-auditor 🟡 — false-promise 봉합): `ProtocolVersion.cs:29` + `PDL.xml:232`의 "Phase 09 S_EnemyAttack은 v8에 포함, 추가 bump 없음" 주석을 "v9 신설 (옛 09가 v8 묶음에서 이월되며 깨진 약속)"으로 *같은 commit에서* 정정 — 코드 주석과 진실의 모순 제거
- [ ] PacketGenerator 재생성 + Shared.dll/ClientNet.dll 동반 commit + 봇 재빌드

### 서버 (server)
- [ ] `BossBehaviorSystem` — `EnemyKind.Boss`일 때 patrol/chase 대신 패턴 FSM (EnemyAISystem의 "Boss는 Idle 고정" 분기 교체)
  - 페이즈 1: 쿨다운 기반 공격 1~2종 (tick 카운터 — 헌법 #5, ms/`Task.Delay` 금지)
  - 페이즈 2: HP ≤ 50% 전환 — 공격 속도/패턴 강화 + 전환 1회성 보장
  - 공격 전 telegraph(예고 모션 틱) — animState 채널로 송신, 클라 이펙트는 Phase 05. **telegraph 선행 틱 수는 구현 시 결정 후 정량 박제** (plan-auditor 🟡 — Phase 05가 "예고 짧아 못 피함" 봉합을 떠안지 않게)
  - ※ Coordinator 분해 시 4구획 체크포인트 권장 (plan-auditor 🟡): ① shared bump 묶음 → ② BossBehavior FSM → ③ 사망/리스폰 → ④ 테스트
- [ ] 데미지 = `Formulas.cs` 재사용 + `EnemyStats`에 Attack 스탯 추가 (Golem/Normal 기본값 동반 정의)
- [ ] 적→플레이어 데미지 판정: 보스 공격 범위 ∩ 플레이어 *권위 위치* (헌법 #1) — 범위 밖 데미지 0
- [ ] 플레이어 HP 0 → **해당 맵 spawn 재배치 + HP full** (kill-plane 재배치 경로 재사용 검토) + snapshot/S_EnemyAttack으로 통지
- [ ] S_PlayerJoin characterClass: `GameSession` 입장 시 선택 직업을 broadcast에 포함 (신뢰 경계 — 클라 신고값 범위 검증, 유효 직업 외 fail-closed)

### 테스트 (서버 측)
- [ ] `BossBehaviorTests` — 페이즈 전환 임계(50%/1회성), 쿨다운 tick 정확성, 범위 내/밖 데미지, 플레이어 사망→리스폰 HP full
- [ ] 봇 `BossFightSmoke` 신설 (BossStageClearSmoke 확장) — 보스에게 맞아 HP 감소 관측 → 처치 → StageClear
- [ ] 기존 BossStageClearSmoke/StageClear 1회성 회귀 0

---

## ✅ 완료 조건

- [ ] `ProtocolVersion == 9` + 두 패킷 변경이 한 commit 묶음 + 은퇴 ID 재사용 0 (헌법 #2)
- [ ] 보스 페이즈 1→2 전환 + 쿨다운 패턴 공격 단위 테스트 green
- [ ] 범위 안 플레이어만 데미지 / 범위 밖 0 / 데미지 값 서버 계산 (클라 입력 무관) 테스트 green
- [ ] 플레이어 HP 0 → 스폰 리스폰 + HP full 테스트 green (데모 무중단)
- [ ] characterClass 비유효값 fail-closed 테스트 green (헌법 #3)
- [ ] `dotnet test` 전체 green + 봇 BossFightSmoke PASS (WSL2 경로)
- [ ] tick 메트릭 회귀 0 (보스 패턴이 50ms 예산 침범 X)

---

## 🧪 테스트

**자동**: BossBehaviorTests 신설 + 기존 전투/AI/StageClear 회귀 + 봇 BossFightSmoke
**수동**: 서버 로그로 페이즈 전환/공격 틱 관측 (Play 연출 검증은 Phase 05)

---

## 📚 학습 포인트

- **양방향 권위 전투** — 지금까지 플레이어→적 단방향. 적→플레이어도 서버가 판정하면 "누가 누굴 언제 때렸나"의 진실이 완전히 서버에 모임
- **tick 카운터 패턴** — `await Task.Delay` 없이 틱 수로 쿨다운/페이즈 타이밍 (헌법 #5). 게임 서버 AI의 표준
- **bump 묶음 전략** — 프로토콜 변경 2건을 한 버전에 싣는 이유: 팀원 재빌드 비용이 bump 횟수에 비례 (M4.2 학습)
- **보스 공격 비대칭** — 보스→플레이어는 권위 위치 판정이라 prediction으로 피한 플레이어가 맞을 수 있음 → telegraph가 체감 공정성을 보완

---

## ⚠️ 함정 / 주의사항

- **PDL append-only + 은퇴 ID 재사용 금지** (헌법 #2). 두 패킷 변경 후 *반드시* 한 PR 묶음 — 쪼개지면 bump 2회
- **Shared.dll 동반 commit 의무** (헌법 #4) — 클라/봇/CI 세 소비자 모두 재빌드 확인
- **characterClass도 untrusted** (헌법 #3) — 클라 신고값 범위 검증, switch default = fail-closed
- **StageClear 1회성 회귀** — 보스가 공격 로직을 가져도 `_stageCleared` flag 불변
- **tick 폭주** — 페이즈 2 강화가 무할당 원칙(헌법 #5) 침범하지 않게 (사전 할당 패턴)
- WSL2 sync→build→run 한 묶음 (stale 함정 — ADR-029 부록 A)

---

## ➡️ 다음 Phase

- Phase 05 — 보스 클라 연출 + 원격 직업 표시 (본 Phase 패킷 소비)

---

## 📋 박제 (완료 후)

- **대규모 등급** — `04-boss-protocol-and-server-DONE.md` + **5단계 보고 MD/HTML** + ⚠️ `/cross-review` 권장 (옛 09 plan-auditor 🟡 계승 — 첫 양방향 권위 전투 + bump + 신뢰경계 동시)

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18 — 옛 M4.3-09 설계 계승 + S_PlayerJoin append 묶음 + 대규모 상향[깃발 2])
- 2026-06-07: 완료 (세션21 — Coordinator 4구획 분해 + 메인 검수 정정 1건. v9 bump 한 묶음 + BossBehaviorSystem + 리스폰 HP full. test 417/0 + 봇 BossFightSmoke PASS + reviewer 🔴0. telegraph 정량 = P1 16틱/P2 10틱 박제)
