---
owner: youngho
milestone: M7.5
title: Loop-driven 하네스 격상 (ADR-032 구현 sweep)
status: done
grade: 대규모
risk: trust-boundary
estimated: 12~18h (총합, 6 Phase)
domains: [cross]
---

# M7.5 — Loop-driven 하네스 격상 (Loop Engineering)

> **상태**: planned — 2026-06-18 ADR-032 accepted + ultracode 전면 파악 triage 위에 작성
> **시작**: 미정 (영호 GO 후)
> **목표 마감**: 미정 — 일정은 영호 컨트롤 (발표 종료, 점검 모드)
> **선행 근거 문서**: [`../../../00_Document/ADR/harness/ADR-032-loop-driven-operation.md`](../../../00_Document/ADR/harness/ADR-032-loop-driven-operation.md) (결정 전문 + sweep 목록 + 미결)

---

## 🎯 마일스톤 목표

ADR-032(accepted)의 **loop-driven 운영 모드**를 실제 문서·하네스에 반영한다. 핵심: *사람이 매 스텝 프롬프트 → 시스템이 대신 구동, 사람은 방향+판단만.* **v1(attended)만** — v2 무인은 defer(별도 ADR).

**이 마일스톤은 코드 거동을 한 줄도 안 바꾼다.** 게임/기술/conventions = 손 0. 변경 표면 = `CLAUDE.md` + `00_Document/policies/` + `00_Document/ADR/` + `.claude/{agents,commands,hooks,state,knowledge}/` + `.claude/settings*.json` (문서·하네스 only). ultracode triage 결론 = **"teardown 아니라 격상"**.

### ⚠️ 등급 = 대규모 사유

- **300줄+ + 광역**: 헌법 6절 + policies 9개+ + agents 6 + commands 6 + hooks + settings + 신규 파일 ~8 = grade-and-risk §대규모(300줄+/광역) 충족.
- **harness 깃발** (전 Phase): `.claude/{hooks,agents,commands}/**` 변경 = risk-detector harness 깃발 자동.
- **trust-boundary 인접** (Phase 05): `settings.json` 권한 승격 — 무인 commit 권한은 **defer(v1)**, 단 ask(pr merge/create) 게이트는 *절대 보존* (헌법 §3 / pr-and-merge-gate).
- **비-irreversible**: push/PR/merge는 본 sweep 범위 밖(마일스톤 마감 시 별도 영호 GO). 문서·config는 가역.

### 핵심 원칙 (헌법 + ADR-032)

- **append-only**: ADR 파일 본문 rewrite 0. 옛 ADR(022/019/016/023)은 상태줄 "(부분 superseded — ADR-032)" 한 줄만. ADR-031은 *확장*(supersede 아님).
- **거동·wire 무변경**: 게임 코드 0 변경 → **WSL2 게임 회귀 게이트는 불요**. done 판사 = ① dangling 참조 0 ② hook 정합(기존 발동 유지 + 신규 가드 smoke) ③ reviewer 🔴 0.
- **헌법 §1(서버권위)·§2(프로토콜) = N/A**: 게임 코드 미접촉이라 변경 표면 밖(거동 git diff 0이 보증). §3(trust-boundary)만 P05 settings 권한으로 인접 → reviewer 필수. (plan-auditor 축5)
- **정책 5개 강결합 atomic**: reporting-format↔pin-and-done↔review-tiering↔subagent-routing↔grade-and-risk가 §동기화 책임으로 순환 참조 → **반드시 한 Phase(02)에서 atomic**. 하나만 고치면 drift.
- **사람=방향+판단 보존**: 모든 Phase는 영호 게이트(비가역/설계분기/육안) 정의를 *강화*만, 약화 X. ask(pr) GO 게이트 불변.
- **학습 분리**: 이 sweep 자체가 "깊은 학습은 pull 세션으로" 원칙의 첫 적용 — 영호는 결과를 *나중에 별도로* 파면 됨.

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk | 내용 |
|---|---|---|---|---|---|---|
| 01 | **신규 정책 3종 토대** + 미결 #2·#6 결정 | 복잡 | cross(meta) | 2~3h | — | `loop-driver`/`work-judge`/`review-throughput` (독립 vs 흡수 결정). REVISE가 가리킬 단일 진실 |
| 02 | **헌법 + policies 5개 강결합 atomic** + pr-and-merge-gate | 복잡 | cross(meta) | 3~4h | — | CLAUDE.md 6절 + 5정책 동시 REVISE (운영모드 키스톤) |
| 03 | **agents 6 REVISE** | 복잡 | cross(meta) | 2~3h | — | coordinator·reviewer·plan-auditor·knowledge-gc·_routing·_escalation |
| 04 | **드라이버 슬래시 + commands** + 미결 #3 | 복잡 | cross(meta) | 2~3h | — | 신규 loop/goal + refactor-sweep 일반화 + session/start·end + work:plan |
| 05 | **hooks + settings + 원장 3종** + 미결 #4·#5 | 복잡 | cross(meta) | 2~3h | **trust-boundary** | circuit halt + 신규 가드 + 권한 승격(ask(pr) 보존) + pending-art/comprehension/knowledge |
| 06 | **카탈로그 + 상태줄 + drift 마감** + reviewer 통합 | 보통 | cross(meta) | 1~2h | — | 카탈로그 3종 atomic + ADR 등록/상태줄 + stale drift + 통합 점검 + -DONE.md/HTML |

**총 등급 = 대규모**. Phase 05(trust-boundary, settings 권한)는 reviewer 필수. 거동 무변경이라 구현 Worker는 Sonnet 유지(코드 trust-boundary 로직 변경 0 — settings는 config). 메인 세션 직접 편집 위주(문서 sweep은 도메인 Worker보다 메인이 일관성 유지 유리), Phase별 reviewer 통합.

---

## 🔗 의존성 그래프

```
P01 (신규 정책 3종 = 단일 진실 토대)
  │
  └─→ P02 (헌법 + policies 5 atomic + pr-and-merge-gate)   ← 키스톤
          │
          ├─→ P03 (agents 6 REVISE)            ─┐
          │                                      ├─ P03 ∥ P04 (도메인 무관, 둘 다 P02 후)
          └─→ P04 (드라이버 슬래시 + commands)  ─┘
                       │
                       └─→ P05 (hooks + settings + 원장 3)   ← 드라이버가 원장 소비 + 권한
                                  │
                                  └─→ P06 (카탈로그 + 상태줄 + drift + 통합 마감)   ← 전부 후
```

- **P01 → 전부**: REVISE 편집들이 새 정책을 *포인터로 참조* → 정책이 먼저 존재해야 dangling 0.
- **P02 = 키스톤**: 정책 5개 강결합 atomic. 단일 Phase 안에서 동시 수정 + §동기화 책임 표 정합.
- **P03 ∥ P04**: agents(정의) ↔ commands(슬래시) 도메인 다름 — 둘 다 P02만 의존, 서로 무관 → 병렬 가능.
- **P05 → P06**: 권한·hook·원장 정착 후, 마지막에 카탈로그/상태줄/drift를 *atomic 등록*(내용이 다 있는 뒤 한 번에).
- **권장 순서**: 01 → 02 → (03 ∥ 04) → 05 → 06.

---

## ✅ 마일스톤 완료 조건

- [ ] **P01 — 신규 정책**: `loop-driver.md` 작성(엔진 /goal+Workflow / v1 기동 attended+RemoteControl / PC-on·WSL게이트 done판사 / 버킷별 SubAgent 구동). `work-judge`·`review-throughput`은 독립 파일 vs 기존 흡수 *결정 박음*(#2). 신뢰졸업 N 초안(#6). 220줄 임계 준수. policies/INDEX 등록.
- [ ] **P02 — 헌법+5정책 atomic**: CLAUDE.md 6절(작업보고/작업좌표/Phase진행/작업등급/SubAgent풀/Knowledge) + 5정책 동시 REVISE. §동기화 책임 표 상호 정합(순환 참조 깨짐 0). pr-and-merge-gate가 권한 승격과 정합. **사람 게이트(ask(pr)) 약화 0** 명시 검증.
- [ ] **P03 — agents 6**: 라우팅 진입 주체에 "루프 드라이버" 병기, SubAgent=Worker/checker 인지, 에스컬레이션 무인 분기(단 v1은 attended라 사람 즉시). knowledge-gc "무인 자율실행 X" 명시.
- [ ] **P04 — 드라이버+commands**: 신규 `loop`/`goal` 슬래시(통합 여부 #3 결정). refactor-sweep가 "드라이버 refactor 프리셋"으로 재정의(Step0~5 골격 추출 명시). session/start·end·work:plan 루프 정합.
- [ ] **P05 — hooks+settings+원장**: circuit-breaker halt 신호 기록 + 신규 가드 hook(또는 risk-detector 확장). settings 권한 승격 범위 결정(#4) — **`ask(gh pr merge/create)` 매처 보존 git diff로 검증**. pending-art/comprehension/knowledge 원장 신설(위치 #5). hook 정합 smoke(기존 발동 유지 + 신규 exit 코드 정상).
- [ ] **P06 — 카탈로그+drift+마감**: 카탈로그 3종 atomic(policies/INDEX·ADR/INDEX·commands-index — 새 정책/슬래시 반영, 10 vs 11 drift 봉합). ADR-032 등록(INDEX harness 행 + ADR.md 후보표/카운트 :18 tech-stack 026·:20 harness 17 번호나열 + ADR_History 한 줄). 옛 ADR 상태줄 supersede 표기(022/019/016/023). templates(done-md/pin)·setup-steps/04-finalize stale 정정.
- [ ] **거동 무변경 증명**: 게임 코드 `git diff` 0 (02_Server/03_Client/98_Shared/04_ClientNet 소스 무변경). **dangling 참조 0** 전수 grep. hook 정합 smoke green.
- [ ] **reviewer 통합 (P06)**: 헌법/ADR/도메인 패턴 🔴 0. 정책 5개 동기화 정합 확인.
- [ ] **박제**: 대규모 → `_milestone-DONE.md` + HTML 시각화 (5단계 보고 구조 박제, ADR-031).
- [ ] **PR/머지**: 영호 명시 GO (비가역 게이트 — 본 sweep 완료 후 별도).

---

## 🚫 이번에 명시적으로 뺀 것 (사유 박음)

- **v2(무인 Desktop scheduled)**: defer. 권한 승격·circuit halt 폴링·trust-boundary 자율침범 3위험 동시 유입 → v1 검증 후 별도 ADR (ADR-032 미결 #1 결정).
- **게임 코드/conventions/PRD/ARCHITECTURE**: 손 0 — loop 운영과 직교(ultracode triage 결론).
- **무인 가드 hook 풀세트**: v1은 attended라 사람이 게이트 → 무인 전용 가드(자동 적재·halt 폴링)는 v2 ADR로. v1은 circuit halt *신호 기록*까지만.
- **자율 commit 권한 전면 승격**: settings 권한은 v1 필요 최소만. 무인 commit allow 승격은 v2.

---

## 갱신 이력

- 2026-06-18 — **사전 작성** (메인 세션). ADR-032 accepted + ultracode 전면 파악 triage(harness ADR 재실행 포함) 위에 6 Phase 분해. v1만 adopt.
- 2026-06-18 — **plan-auditor GO (🔴 0)** + 🟡 3건 봉합: ① knowledge/{README,_usage} REVISE를 P05에 추가(누락 봉합) ② P05 convention-size-guard "추가"→"등재" 문구 정정(이미 기존) ③ 헌법 §1/§2 N/A 한 줄 명시. 설계 변경 0 — 완료조건·범위 정밀화만.
- 2026-06-18 — **마일스톤 마감 (6/6 done)**. commits: P01 `70533b7` / P02 `6fcb466`(reviewer🟢) / P03 `7c94e12` / P04 `a2e094d` / P05 `cea1e4f`(reviewer🟢 trust-boundary) / P06(본 closeout, reviewer🟢). 미결 #2~#6 전부 결정(영호 게이트). 게임 코드 0 변경 + dangling 0 + reviewer 🟢×3. 박제 = `_milestone-DONE.{md,html}`. 실행 중 플랜 보강: 세션 2종(/session:review 신설) + 팀 유지 안 됨=영호 단독 컨텍스트 전환. **PR/머지 = 영호 명시 GO 대기**(비가역).
