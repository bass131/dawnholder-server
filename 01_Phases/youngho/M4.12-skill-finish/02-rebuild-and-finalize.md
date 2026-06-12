---
owner: youngho
milestone: M4.12
phase: 02-rebuild-and-finalize
title: 발표 재빌드 + 전체 회귀 + M4.9·M4.12 마감 박제
status: done
grade: 복잡
slug: 02-rebuild-and-finalize
created: 2026-06-12
completed: 2026-06-12
domains: [qa]
risk_flags: [irreversible]
prior_phases: [01-skill-cooldown-hud]
depends_on: [01-skill-cooldown-hud]
---

# M4.12 Phase 02 — 발표 재빌드 + 전체 회귀 + 마감 박제

> 마일스톤 계획서 = `_milestone-plan.md` P2. **마일스톤의 마지막 게이트** — 앞 Phase(쿨다운 HUD)가 green이어야 재빌드가 의미. 발표 나갈 클라를 박고, 오래 미뤄둔 **M4.9 마감 박제**(정의 7개 / -DONE 0개)를 회수한다.

---

## 🎯 목표

M4.10/11/12 변경이 **전부 포함된 발표용 클라 빌드**를 `C:\Dev\Build`에 박고, 전체 회귀가 **M4.11 baseline 대비 비감소**로 green임을 확인한 뒤, **M4.9·M4.12를 정식 마감 박제**한다(Teleport 실상태 정직 기록 포함).

---

## ⏪ 사전 조건

- [ ] Phase 01(쿨다운 HUD) 완료 — 빌드에 포함될 마지막 기능.
- [ ] Unity 에디터 컴파일 0 error (빌드 = 에디터 컴파일 0 전제).
- [ ] WSL2 서버/테스트 환경(ADR-029) 가동 가능.

---

## 📝 작업 내용

**전체 회귀 (선 — 빌드 전 green 확인)**
- [ ] WSL2 서버 테스트 (rsync → build → test) — **≥561 passed**.
- [ ] Unity EditMode (TestRunnerApi 콜백 + 콘솔 폴링) — **≥119 passed 0 failed**.
- [ ] 봇 전 시나리오 (`run_bot_regression.sh` + `run_bot_fresh_recheck.sh`) — **16 PASS**.
- [ ] Unity 콘솔 0 error (scriptCompilationFailed=False).

**발표 재빌드**
- [ ] `BuildPlayer` 7씬 → `C:\Dev\Build`. **Succeeded errors=0**.
- [ ] **DLL mtime 신선도** 확인 — Managed DLL mtime > 마지막 소스 수정 시각(stale 빌드 방지).
- [ ] dry-run(발표 리허설) — 빌드 클라 실행 + 2클라 발표 시나리오 1회.

**마감 박제**
- [ ] **M4.9 마감 박제** — 정의 7개(`M4.9-skill-completion/01~07`)를 -DONE로 회수. **박제 입자(7장 개별 vs 마일스톤 -DONE 1장 흡수)는 착수 시 영호와 확정**(plan-auditor 🟡). **Teleport 실상태(서버·클라 코드 ✅ / VFX 프리팹 미배치) 정직 기록.**
- [ ] **M4.12 마감 박제** — `_milestone-DONE.md`(쿨다운 HUD + 재빌드 사실 박제).

---

## ✅ 완료 조건 (정량)

- [ ] **BuildPlayer Succeeded, errors=0, 7씬.**
- [ ] **회귀 비감소**: WSL2 ≥561 / EditMode ≥119 / 봇 16 PASS / Unity 콘솔 0err — *전부 M4.11 baseline 대비 같거나 위*.
- [ ] **DLL mtime > 소스 mtime** (신선도 증명).
- [ ] **M4.9 -DONE 박제 완료** — `phase-gate-validator.sh` Hook 통과(frontmatter + 의무 섹션).
- [ ] **M4.12 `_milestone-DONE.md` 박제 완료** — Hook 통과.
- [ ] **wire 무변경 v12** — PDL.xml / `ProtocolVersion` diff 0.

---

## 🧪 테스트

**자동**:
- 전체 회귀 스위트(위 4종) — baseline 비감소가 단일 판정 기준.

**수동**:
- 빌드 클라 실행 → 캐릭터 선택 → Town 진입 → 스킬 시전(쿨다운 HUD fill 확인) → 발표 동선 dry-run.
- 2클라(머신 다중 인스턴스 — PlayerPrefs 오염 주의) 동시 시나리오 1회.

---

## 📚 학습 포인트

- **빌드 신선도 = Managed DLL mtime**: 빌드가 "성공"해도 stale DLL이면 옛 코드. mtime 비교가 신선도 증명.
- **회귀 baseline 비감소 판정**: "green"만으론 신규 케이스 누락을 못 잡음 — *직전 숫자 대비*로 판정.
- **소급 박제**: 미박제 마일스톤(M4.9)을 사후 회수. 사실 박제는 *지금 시점의 실상태*를 정직하게(Teleport VFX 미배치까지) — 가짜 약속 방지.

---

## ⚠️ 함정 / 주의사항

- **빌드 = 에디터 컴파일 0 전제** — 컴파일 에러 있으면 빌드가 옛 DLL로 성공한 것처럼 보일 수 있음.
- **PlayerPrefs = 머신 전역 공유** — 한 PC 다중 클라 시 캐릭터 선택 덮어쓰기 + Player.log 섞임(`ClassLoadout` process-local 캐시로 완화하나 주의).
- **봇 Freeze 동류 한계** — 연속 실행 시 몬스터 상태 누적으로 후반 시나리오 흔들릴 수 있음. **fresh 단독 PASS = 회귀 아님**(M4.11 P5 관측).
- **★irreversible** — 발표 빌드 후 PR/머지는 **영호 명시 GO** 필수(외부 publication + main history). admin 머지 시 `CLAUDE_ADMIN_BYPASS_REASON` + 영호 GO. PR body에 보안 키워드 literal 금지.

---

## ➡️ 다음

- M4.12 마감 → 발표 또는 M4.13(임펄스 동작 클래스 재설계) 착수(순서 영호 결정).

---

## 📋 박제 (완료 후)

복잡 등급 → `02-rebuild-and-finalize-DONE.md` + **마일스톤 마감**(`_milestone-DONE.md`). 마일스톤 등급(M4.12 = 복잡)에 따라 5단계 보고 MD/HTML은 영호 판단(경량 마일스톤이라 -DONE 중심도 가능). `phase-gate-validator.sh` Hook 검사.
