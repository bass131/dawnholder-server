---
owner: youngho
milestone: M3.8
phase: 05
title: Hamachi 셋업 검증 + M3.8 마감 의례
status: pending
grade: 보통
risk: irreversible
estimated: 1~2h
domain: qa+meta
summary: 본인 + 정유현 환경 Hamachi 셋업 검증 (백업 = 본인 단독 로컬 데모) + M3.8-마감 별 -DONE.md (복잡 등급 의무) + ADR-024 cadence false-promise 점검 + CHANGELOG [M] entry
---

# Phase 05: Hamachi 셋업 검증 + M3.8 마감 의례

> **상태**: pending
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 보통 (위험 깃발 `irreversible` — 마일스톤 마감 + PR 머지 게이트)
> **담당**: qa SubAgent (Hamachi 검증) + 메인 직접 (마감 의례)

---

## 🎯 목표

캡스톤 1 발표 영상 환경(Hamachi 가상 LAN) 본인 + 정유현 머신에서 *실제 작동* 검증 + 백업 시나리오 박음 + M3.8 마일스톤 전체 마감 의례 (-DONE.md + ADR-024 cadence false-promise 점검 + CHANGELOG entry + CONTEXT 갱신 + PR 게이트).

본 Phase 끝나면 = *완성된 캡스톤 1 데모 인프라 풀세트* (메인 → 캐릭터 선택 → 마을 NPC → 전투 → 보스 → 엔딩 흐름 끊김 없음, M3 broadcast + M3.8 인프라 정합) + Hamachi로 *정유현 머신과 같이 데모 가능* + M3.8 PR 머지 + M4.1 진입 대기 상태.

---

## ⏪ 사전 조건

- [ ] Phase 01 (PRD 갱신) + Phase 02 (메인 + 엔딩) + Phase 03 (캐릭터 선택) + Phase 04 (NPC 대화) 모두 마감 박힘
- [ ] dotnet test green (회귀 0)
- [ ] Unity batchmode compile green (Phase 03 + 04 후)
- [ ] 정유현과 Hamachi 셋업 시간 일정 조율 박힘 (별 채널 = 디스코드/카카오 — 본 Phase 진입 *전* 시간 약속)

---

## 📝 작업 내용

### 5-A. Hamachi 셋업 검증 (qa SubAgent)

- [ ] 본인 머신 Hamachi 클라이언트 설치 + 네트워크 생성 (네트워크 ID/비번 박음)
- [ ] 정유현 머신 Hamachi 클라이언트 설치 + 본인 네트워크 가입 (별 채널로 ID/비번 전달)
- [ ] 본인 머신에서 `Dawnholder.Server` 부팅 + Hamachi 가상 IP로 listen 검증 (`netstat -an | findstr 7777`)
- [ ] 정유현 머신에서 Unity 클라 부팅 + Hamachi 가상 IP로 connect 검증
- [ ] *2인 같은 맵 broadcast* 실측 (M3 Phase 08c 결과 정합) — 본인 + 정유현 캐릭터 화면에 같이 표시
- [ ] *Hamachi 안 될 때 백업 시나리오 박음* (본인 단독 로컬 데모 — `localhost:7777`)
  - 사전 녹화 영상 = Hamachi 환경 우선, 안 되면 *본인 머신 2개 Unity 인스턴스* 백업
- [ ] Hamachi 셋업 검증 결과 박제 (`01_Phases/youngho/M3.8-capstone-1-demo-infrastructure/_hamachi-verification-2026-MM-DD.md` 단순 노트)

### 5-B. M3.8 마감 의례 (메인 직접)

- [ ] M3.8 시연 흐름 풀세트 dry-run (메인 → 캐릭터 선택 → 마을 NPC → 전투 → 보스 → 엔딩) — *끊김 없이* 작동 검증 의무
- [ ] M3.8 마감 별 -DONE.md 박음 (`05-hamachi-setup-and-milestone-closeout-DONE.md` 또는 `_milestone-DONE.md`):
  - 5단계 구조 (단순/보통 → 단순 사실 박제, 복잡 → 5단계 보고 구조)
  - **ADR-024 cadence false-promise 점검 결과 섹션 의무** — M3.8 진행 중 박힌 약속들 (`_milestone-plan.md` + 5 Phase 정의) 실제 박혀있는지 점검 + 누적 0건 박음 또는 발견 시 봉합 분기 박음
- [ ] M3.8-마감 5단계 보고는 *마일스톤 자체가 복잡 등급*이라 -DONE.md만 의무 (대규모 아님 = 5단계 MD/HTML X). 단 *캡스톤 1 평가 자산*으로 5단계 MD/HTML 박을지 본인 결정 가능 (옵션 박음)
- [ ] `.claude/CHANGELOG.md` 최상단에 [M] entry 박음:
  - 한 줄 요약 = "M3.8 Capstone-1 Demo Infrastructure 마일스톤 마감 (5 Phase 풀세트, 복잡 등급)" + 본 마일스톤 산출물 4 영역 (메인+엔딩/캐릭터 선택/NPC/Hamachi) + PDL ProtocolVersion 3→4 bump + PRD 갱신 동반 ([H]는 Phase 01 박힘) + false-promise 누적 0건 (또는 발견 N건 봉합) 박음
- [ ] `CONTEXT.md` "⏸️ 현재 멈춤 지점" 갱신:
  - 옛 = "M3.8 진입 대기"
  - 새 = "M3.8 ✅ 완전 마감. 다음 = M4.1 Phase 01 진입 (Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본). M4.1 plan은 M3.8 박힌 PlayerStats 흡수 반영 갱신 의무."
- [ ] `CONTEXT_History.md` 한 줄 추가 (2026-MM-DD M3.8 마감)
- [ ] work-pin 최종 갱신 ("M3.8 ✅ 완전 마감, 다음 액션 = M4.1 plan 재조정 + Phase 01 진입")
- [ ] commit 박힘 + push 박힘 + PR 생성 (사용자 명시 GO 후) + 사용자 명시 GO 후 머지

---

## ✅ 완료 조건

- [ ] Hamachi 셋업 검증 박힘 (본인 + 정유현, 2인 같은 맵 broadcast 실측)
- [ ] 백업 시나리오 박힘 (본인 단독 로컬)
- [ ] M3.8 시연 흐름 풀세트 dry-run 통과 (끊김 없음)
- [ ] M3.8-마감 -DONE.md 박힘 (복잡 등급 의무, ADR-013 페어 박제 정합)
- [ ] -DONE.md에 *false-promise 점검 결과* 섹션 박힘 (ADR-024 cadence 의무)
- [ ] CHANGELOG [M] entry 박힘
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점" 갱신 박힘
- [ ] CONTEXT_History.md 한 줄 박힘
- [ ] work-pin 최종 갱신 박힘
- [ ] PR 생성 + 사용자 명시 GO + 머지 박힘
- [ ] origin/main sync 박힘
- [ ] 본 feature 브랜치 삭제 박힘 (**사용자 명시 GO 후**, `--delete-branch` 옵션 정합 — plan-auditor 개선 제안 봉합)

---

## 🧪 테스트

**자동**:
- `dotnet test` 최종 green (회귀 0)
- Unity batchmode compile 최종 green
- `.githooks/pre-commit` 통과 (cloud 라인 자동 unstage + -DONE.md 형식 강제)
- `phase-gate-validator.sh` Hook 통과 (-DONE.md frontmatter + 등급별 의무 섹션 검사)

**수동**:
- Hamachi 환경 본인 + 정유현 2인 broadcast 실측 (시각 검증)
- 백업 시나리오 = 본인 머신 2개 Unity 인스턴스로 2인 broadcast 실측
- M3.8 시연 흐름 dry-run (메인 → 엔딩까지 끊김 없음)

---

## 📚 학습 포인트

- **Hamachi 가상 LAN 기본 동작** — Hamachi = 가상 LAN over Internet. 클라이언트끼리 *마치 같은 LAN*처럼 보임 → TCP 7777 listen + connect 자연 작동. 본 마감(11/19) 클라우드 서버 전환 시 *진짜 외부 IP + 방화벽 + NAT* 학습 트리거 (M5+ 영역).
- **마감 의례 = 사실 박제 + 회고 분리** (ADR-013 + ADR-024) — -DONE.md = 사실 (`Phase 진행 결과 / 산출물 / 회귀 결과`), 본인 회고 = 별 일지 (`learning-journal/youngho/`). 가짜 학습 누적 방지.
- **false-promise 점검 cadence** (ADR-024) — 마일스톤 마감 시점에 *진행 중 박힌 약속들이 실제 박혔는지* 점검 의무. 누적 12건+ 학습 정합 (M3 5건 + M3.6 7건). 본 마일스톤에서 0건 박음 또는 발견 시 즉시 봉합 분기.
- **`/session:end` 자동화 검토** — 본 Phase 마감 의례 = 옛 `/session:end` 슬래시 일부 흡수. 본 마일스톤 후 *자동화 도구* 신설 검토 (ADR-024 별 시점).
- **dry-run 의무** — 시연 환경 실측 의무. 학부생 함정 = *각 Phase 박은 후 통합 테스트 빠뜨림* → 발표 직전 사고 발견. M3.8 시점에 풀세트 dry-run 박음 + M4.1 마감 시점에 재 dry-run + 6/3 시점 슬라이드 박을 때 *시연 영상 1차 녹화* (백업).

---

## ⚠️ 함정 / 주의사항

- **Hamachi 셋업 시간 빠듯** — 정유현과 시간 약속 안 잡으면 본 Phase 블로킹. *Phase 04 마감 시점에 미리 일정 조율* 의무.
- **Hamachi 방화벽 차단** — Windows 방화벽이 Hamachi 가상 IP 차단 가능 → *예외 추가* 의무 (TCP 7777). 정유현 머신도 동일 인지.
- **본 마감 시점 클라우드 전환** — 본 Phase = Hamachi 환경 한정. M5(영속화) + M8(부하 테스트) 시점에 *진짜 클라우드 서버* 전환 검토. 본 Phase에서 *Hamachi 의존 코드 박지 X* (단순 TCP listen + connect만, 환경 무관).
- **-DONE.md 형식 강제 함정** — `phase-gate-validator.sh` Hook이 frontmatter + 등급별 의무 섹션 검사. 누락 시 commit 차단. 학부생 함정 1순위 = 5단계 보고 섹션 빠뜨림 (대규모 등급 시) — 본 마일스톤은 복잡이라 -DONE.md만 의무, 5단계 MD/HTML 옵션.
- **PR 머지 = 사용자 명시 GO 의무** (헌법 §"확신이 없을 때" + `pr-and-merge-gate.md`). AI 자율 머지 X. 본 Phase 마감 시점에 PR 생성 + 사용자 GO 대기.
- **work-pin drift 위험** — 본 Phase 진행 중 work-pin 박힌 단계와 실제 git 상태 어긋날 수 있음 (commit/push/PR 생성/머지 4단계 stale). *다음 세션 진입 시 `/session:start` 0-부수 단계가 drift 잡음* (M3.7 박힘) — 본 마감 시 *현재 세션*에서 work-pin 동기 의무.
- **M4.1 plan 갱신 의무** — 본 Phase 마감 후 *M4.1 plan은 PlayerStats 흡수 반영 갱신 의무*. 본 Phase에서 미리 박지 X (별 작업으로 분리, 다음 세션 또는 본 세션 마감 시점에).

---

## ➡️ 다음 마일스톤

- **M4.1 Combat Precision** — Phase 01 진입 = Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본
- **M4.1 plan 재조정** = 본 마일스톤 마감 시점 또는 *직후 별 작업*으로 박음:
  - Phase 02 `Formulas.cs` 시그니처 = `ComputeDamage(attackerStats, targetStats)` (M3.8 Phase 03 박힌 `PlayerStats` 흡수)
  - Phase 03 lag compensation rewind = `PlayerStats` 인식 없음 (단순 position만), 변경 X
  - M4.1 의존성 그래프 갱신 = "사전 조건: M3.8 Phase 03 마감 (PlayerStats 박힘)"

---

## 📋 박제

본 Phase = 보통 등급이지만 *마일스톤 마감 의례*라 -DONE.md 박음 의무 (M3.8 마일스톤 복잡 등급 정합).

ADR-013 페어 박제:
- AI=사실 박제 = 본 -DONE.md (Phase 산출물 + 회귀 + ADR-024 점검 결과)
- 본인=회고 = `learning-journal/youngho/M3.8-회고.md` 또는 Notion 트랙 B (별 박제)

work-pin 최종 갱신 = "M3.8 ✅ 완전 마감. 다음 = M4.1 plan 재조정 + Phase 01 진입"

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
