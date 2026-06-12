---
owner: youngho
milestone: M4.11
phase: 05-rebuild-and-full-regression
title: serverTick + 고정스텝 반영 클라 재빌드 + 전체 회귀
status: planned
grade: 보통
slug: 05-rebuild-and-full-regression
created: 2026-06-12
domains: [client, qa]
prior_phases: [01-remote-interp-servertick, 02-force-adopt-decouple, 03-regression-safety-net, 04-fixedstep-prediction]
depends_on: [04-fixedstep-prediction]
---

# M4.11 Phase 05 — serverTick + 고정스텝 반영 클라 재빌드 + 전체 회귀

> 마일스톤 계획서 = `_milestone-plan.md` P5 행(표 #5) — 위험 오름차순 게이트식의 **마지막 매듭**. P1~P4가 친 코드를 빌드 클라에 반영하고 마일스톤 전체를 회귀한다.

---

## Context (왜)

P1(원격 보간 serverTick + clock smoothing, ProtocolVersion 12)부터 P4(로컬 예측 고정스텝 + 시각 보간)까지 **소스는 전부 commit됐다.** 하지만 **빌드 클라(exe)는 P3 시점 구코드**다 — P4를 빌드에 반영한 적이 없다. 그래서 지금 Editor(신코드)와 빌드 클라(구코드)가 어긋나 있다.

마지막 매듭 = **재빌드로 Editor와 빌드 클라를 P4 코드로 통일**한 뒤, 마일스톤 전체를 한 번에 회귀(테스트 + 봇 + 2클라 + 콘솔)해서 도미노가 끝까지 봉합됐음을 증명하는 것이다. **게임 코드 변경 0 예정** — 순수 빌드 + 검증 Phase다. 그래서 grade는 **보통**이고, `-DONE.md`는 생략한다([grade-and-risk: 단순/보통은 work-pin + commit으로 충분]). 본 Phase의 결과는 마일스톤 마감 시 `_milestone-DONE.md`(대규모 의무)에 흡수된다.

---

## 작업 항목 (전부 실측 확정 — 새 주장 금지)

1. **BuildPlayer** (Unity MCP, 메인 세션) — 출력 `C:/Dev/Build/Client/03_Client.exe`, 7씬. P1-DONE 전례 = BUILD result=Succeeded errors=0. 빌드 신선도 = Managed `Dawnholder.Client.dll` mtime이 빌드 시각으로 갱신됐는지 확인.

2. **봇 16 시나리오 회귀** (WSL2) — M4.10 전례 패턴 = **단일 fresh 서버에 연속 16개**. 단 HpSync/BossFight 연속실행 한계(보스 상태 누적)는 fresh 서버 단독으로 재검. 등록명(`Program.cs` 실측): MultiRosterSmoke / EmergencyCombatSmoke / BossStageClearSmoke / BossFightSmoke / HpSyncSmoke / RemoteAttackSmoke / WhiffSwingSmoke / RangedHitSmoke / FreezeSmoke / ThunderboltAoeSmoke / RangedWhiffSmoke / DashSmoke / TeleportSmoke / EnemyAiSmoke / MapTransition / M2BasicMovement.

3. **테스트 풀세트 재확인** — WSL2 561 passed + EditMode 119 passed. P4 직후 이 세션에서 이미 green이었고 **코드 무변경**이므로, WSL2만 재실행해 airtight 확인(EditMode는 P4 green 인정).

4. **Unity 콘솔 error 0** (빌드 후).

5. **영호 2클라 실측** — 이번엔 **양쪽 다 P4 신코드**(Editor + 새 빌드 exe). 핵심 = 백로그 #5(창 드래그 desync) 봉합 유지 + `_p4-2client-checklist.md` 6항목 거동 재확인(간단 패스).

---

## 완료 조건 / 게이트 (정량)

- [ ] **BuildPlayer Succeeded errors=0** — `C:/Dev/Build/Client/03_Client.exe` 산출 + `Dawnholder.Client.dll` mtime 갱신 확인.
- [ ] **봇 16 시나리오 PASS** — 연속실행 한계 2건(HpSync/BossFight 보스 상태 누적)은 **fresh 서버 단독 PASS를 인정**(M4.10 전례).
- [ ] **WSL2 서버 테스트 561 passed** 유지.
- [ ] **Unity 콘솔 error 0** (빌드 후).
- [ ] **영호 2클라 실측 이상 무** — 백로그 #5 봉합 유지 + 체크리스트 6항목 거동 패스.
- [ ] 전부 green → **마일스톤 마감 절차** 착수: `_milestone-DONE.md` + `.html` 5단계 보고(대규모 의무) → PR(영호 GO 게이트).

---

## 위험 / 금지

- **게임 코드 · 98_Shared · PDL 무변경.** 검증 중 결함을 발견하면 = **STOP → 영호 의논**. P5 안에서 자율 수정 금지 — **심장부(P4) 직후라 한 줄도 자율로 안 고친다.**
- **ProtocolVersion 12 유지** — wire 손대지 않음.
- **봇 실행 pkill bracket 패턴** `GameServer\.[d]ll`.
- **서버 fresh 기동 의무** — 직전 수동 테스트로 보스 상태가 누적됐을 수 있으므로, 봇 회귀 전 fresh 서버로 기동.

---

> 전부 green이면 M4.11 마일스톤 마감(`_milestone-DONE.md` + `.html` 5단계 보고 → PR, 영호 GO 게이트). 결함 발견 시 STOP → 영호 의논.
