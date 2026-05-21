---
summary: M3.5 Phase 06 마감 — 옛 → 새 atomic 전환 commit으로 새 하네스 v1 발효. 옛 운영 100% → 새 운영 100% 한 시점 박음 (격리 폴더 패턴, DB dual-write phase 정신). 122 파일 변경.
phase: 06
status: done
grade: 복잡
owner: youngho
milestone: M3.5
risk: irreversible
date: 2026-05-21
---

# Phase 06: 양식 다이어트 + 정합 마감 + 옛 → 새 전환 commit — *마감*

> **상태**: ✅ done (2026-05-21)
> **마일스톤**: M3.5 — 새 하네스 v1 문서화 (**마감** — 6/6 = 100%)
> **등급**: 복잡 (정량 4등급 중 3단계)
> **담당**: 영호 단독
> **소요**: ~3h
> **commit**: (본 commit 박힘 직후 확정 — `git log -1`)

---

## 🎯 무엇 — Phase 06에서 한 일

본 commit으로 **옛 하네스 운영 100%** → **새 하네스 v1 운영 100%** *atomic 발효*. 격리 폴더 `01_Phases/youngho/M3.5-harness-v1/New_Harness/` 안에 Phase 01~05에서 누적 박은 새 하네스 v1 산출물 풀세트를 옛 영역으로 *일괄 mv* + 옛 자산 *삭제/갱신* + 양식 다이어트 정합 검증. 한 commit으로 *반쪽 발효 차단* + 롤백 비용 명확.

**6 핵심 변경** (ADR-022 정합):

1. **KPI 전환** — "학습 박제 중심" → "Planning → 구현 → 보고" (학습은 트랙 B 분리)
2. **정량 4등급 + 위험 깃발 자동 상향** — 단순/보통/복잡/대규모 + trust-boundary/irreversible/unity-asset (Hook 강제)
3. **SubAgent 풀 9** (옛 7 → 새 9) — Worker 4 (server/shared/client/qa) + Reviewer 2 (reviewer/plan-auditor) + Specialist 3 (unity-bridge/coordinator/knowledge-gc)
4. **Hook 7 풀세트** (옛 5 → 새 7) — rename 3 + 신설 4 + 삭제 2 (server-authority Reviewer 흡수 / work-envelope 양식 죽임)
5. **Knowledge 시스템 신설** — 5 도메인 _index.md + GC Collector (수동 트리거만)
6. **슬래시 17 → 10 + 양식 다이어트** — 학습 5 + 일지 3 = 트랙 B 이관 / work-envelope 절 통째 죽임 / 5단계 보고 = 대규모만

**변경 수치**: 67 삭제 + 29 갱신 + 26 신규 = **122 파일** 변경.

---

## 🤔 왜 — 결정 흐름 (5/20 의논 결과 + 옛 운영 4 모순 표면화)

- **(a) SubAgent 옛 6개 도메인 모호** — 98_Shared/ 누가? Unity asset 누가? 메인 세션 분해 부담은? → 새 9개 카테고리화 (Worker/Reviewer/Specialist) + 3축 정의(도메인+권한+모델) 강제
- **(b) Codex γ 외부 의존** — 3~7회차 누적 실측 → `plan-auditor` SubAgent 내부 자산화
- **(c) "주석으로 박힌 약속은 가짜다" 3회 봉합 시리즈** — M3 Phase 02 (헌법 #2 ProtocolVersion handshake) + Phase 03 (헌법 #4 Handlers/ 폴더) + 5/17 정유현 Shared.dll 사고. 정책이 *물리적 강제*되지 않으면 가짜화 → 새 7 Hook으로 정책의 물리적 강제
- **(d) 옛 학습 5 + 일지 3 슬래시 vs KPI 전환** — *학습 토큰 절약 슬래시* 본질이 KPI 전환 (Planning→구현→보고)과 어긋남 → 트랙 B로 이관 (옛 자산 보존, 새 트랙 추가, 옛 슬래시만 제거 = `track-b-migration-without-asset-loss`)

**왜 한 commit 인가**:
- 반쪽 발효 차단 (옛/새 섞이면 다음 세션이 어느 양식 따라야 할지 혼동)
- 롤백 비용 명확 (revert로 옛 100% 복구)
- 포트폴리오 가치 (새 하네스 v1 도입을 한 git 시점으로 박음 — 캡스톤 평가 자산)

**왜 옛 양식으로 -DONE.md 박는가** (Phase 05 학습 정합):
- 본 Phase 자체가 *전환 commit* — 양식 죽이는 commit이 옛 양식으로 박혀야 *논리적 정합*
- 새 phase-gate-validator는 frontmatter (summary/phase/status/grade/owner) 검사 + 등급별 의무 — 본 -DONE은 양쪽 다 만족

---

## 🛠️ 어떻게 — 5 Step 단계 분할 (사용자 명시 확인 게이트 정신)

Auto Mode classifier가 *agent-controlling config 통째 교체*를 차단 → 사용자가 "단계 분할 + 각 단계 사용자 확인" 정책 선택 → 5 Step으로 분할 (각 Step 사용자 GO 받음).

| Step | 대상 | 변경 metric |
|------|------|------|
| 1 | `CLAUDE.md` (헌법 본체) | 옛 200줄 → 새 239줄 (+39, 절대 원칙 5개 100% 보존 + 새 모델 본체 추가) |
| 2 | `.claude/settings.json` (권한 + hooks 절) | 옛 5 hook 호출 → 새 8 hook 호출. deny 절 보존+강화 (.env.* + appsettings.Secrets.json 추가). ask 절 비움 (PreToolUse hook 차단으로 강화) |
| 3 | `.claude/hooks/` (5 → 7) | rename 3 (inject-current-pin→pin-injector / validate-phase-gate→phase-gate-validator 강화 / validate-shared-changes→shared-discipline-guard 차단) + 삭제 2 + 신설 4 |
| 4 | `.claude/commands/` (17 → 10 + _mapping) | 학습 5 + 일지 3 = 트랙 B 이관 / work/review.md → harness-review.md rename·강화 / cross-review.md 신설 |
| 5 | `.claude/agents/` (7 → 11 = 9 SubAgent + _routing + _escalation) + `00_Document/policies/` (5 → 8) + `.claude/knowledge/` 신설 | 도메인 통합 + Specialist 신설 + Knowledge 시스템 5 도메인 + GC |

각 Step 직후 `git status` 검증. 본 응답 단위로 Auto Mode classifier 통과 + 사용자 명시 GO.

**그 다음 후속 정합**:
- import 경로 정정 (commands-index.md 통째 재작성 / README 5건 / REVIEW_CHECKLIST 4건 / reporting-format 시제)
- Phase 폴더 namespace (inkyu/ 신설 + _template owner frontmatter 박음 + setup-steps 4건 정정)
- team-guide.html v2.0 갱신 (14건 큰 Edit — L1~L5 표 + 슬래시 카탈로그 통째 + 학습 일지 섹션 트랙 B 안내 + 막혔을 때 표 + footer)
- ADR-022 신설 + INDEX/History 박음 + CHANGELOG [H] entry

---

## 🧪 테스트 — 옛 운영 깨뜨림 검증

**자동**:
- ✅ **`dotnet build Dawnholder.slnx --nologo` green** — 경고 0개 / 오류 0개 / 3.82s
- ⏸️ **`dotnet test`** — 본 머신 SAC On 차단 (5/18 박힌 사실, MEMORY.md `smart-app-control-dotnet-test-block.md` 정합). 응급 모드 = Codex 환경 위탁 별 시점

**수동**:
- ✅ **새 hook 작동 실측** — work-pin reference "ADR-018" → "policies/pin-and-done.md" 자동 갱신 확인 (Step 2 후) = `pin-injector.sh` 정상 작동
- ✅ **새 슬래시 카탈로그 시스템 자동 등록** — Step 4 후 시스템 리마인더에 새 10 슬래시 떴음 (`harness-review`, `cross-review`, `work:*`, `session:*`, `setup`)
- ✅ **Edit 도구 통과** — Step 5 후 README/REVIEW_CHECKLIST/policies/team-guide 등 30+ 건 Edit 모두 새 hooks (tdd-guard / risk-detector / shared-discipline-guard) 검사 통과 (비-TDD 영역 + 비-trust-boundary + 비-PDL)
- ⚠️ **work-pin 줄 수 86** — 목표 30~40보다 ↑. 별 시점 압축 (.gitignore라 commit 무관)
- ⏸️ **reviewer SubAgent 자동 호출** — 본 commit 후 다음 turn 첫 작업에서 자연 발동 예상 (트리거 = ≥10줄 변경 + agents/policies 변경)
- ⏸️ **plan-auditor SubAgent 호출** — Phase 정의 .md Write 자동 호출, 본 Phase는 *기존 정의 마감*이라 발동 X. M4 첫 `/work:plan` 호출 시 실측

---

## ➡️ 다음 — M3.5 마감 → M4 진입 게이트

### M3.5 마일스톤 — 완전 마감 (6/6 = 100%)

- ✅ Phase 01 헌법/docs 다이어트
- ✅ Phase 02 SubAgent 풀 9 정의
- ✅ Phase 03 Hook 인프라 7
- ✅ Phase 04 Knowledge 시스템 + GC
- ✅ Phase 05 슬래시 다이어트 + work-pin↔CONTEXT 정합 게이트
- ✅ Phase 06 양식 다이어트 + 정합 마감 + atomic 전환 (본 commit)

### M4 진입 준비

- [ ] CONTEXT.md "현재 멈춤 지점" → "M3.5 마감 + 새 하네스 v1 발효. M4 진입 준비 완료" (본 commit 동반)
- [ ] work-pin → M4 새 WORK-ID로 갱신 (M4 첫 호출 시점)
- [ ] `/work:plan M4` 호출 — *새 운영 첫 실측* — plan-auditor 자동 호출 흐름 검증
- [ ] M4 Phase 1 진입 — 새 SubAgent + Hook + Knowledge 풀세트 첫 작업

### 옵션 후속 (별 시점)

- 디스코드/슬랙 [H] 공지 — 옛 `/learn:*` `/journal:*` 호출 시 "트랙 B 이관됨" 안내
- work-pin 압축 (86줄 → 30~40줄)
- 외부 리뷰 mini-Phase 4건 (`Dawnholder-harness-review-2026-05-19.md`)
- M3 + M3.5 학습 일지 트랙 B 박음 (★★★ 누적 18건+)
- Notion 박제 `/session:log` (별 시점)

---

## 📋 박제 — 옛 → 새 일괄 mv 결과

### 자산 변화 (사실 확정)

| 영역 | 옛 | 새 | 변화 |
|---|---|---|---|
| `CLAUDE.md` | 200줄 | 239줄 (+39) | 절대 원칙 5개 100% 보존, 새 모델 본체 추가 |
| `.claude/agents/` | 7 | 11 (9 SubAgent + _routing + _escalation) | 도메인 통합 + Specialist 신설 |
| `.claude/hooks/` | 5 | 7 | rename 3 + 신설 4 + 삭제 2 |
| `.claude/commands/` | 17 (학습 5 + 일지 3 + 작업 5 + 세션 3 + setup 1) | 10 (작업 4 + 세션 3 + 점검 2 + setup 1) + _mapping 메타 | 학습/일지 트랙 B 이관 + 점검 카테고리 신설 |
| `00_Document/policies/` | 5 | 8 | 갱신 5 + 신설 3 (grade-and-risk / subagent-routing / knowledge-system) |
| `.claude/knowledge/` | 없음 | 5 도메인 _index.md + _usage.md + README.md | 신설 (트랙 A) |
| `.claude/settings.json` | hooks 5 호출 | hooks 8 호출 (hook 7개, risk-detector 2 매처) | deny 보존+강화 / ask 비움 (Hook 차단) / allow 12→19 |

### 양식 다이어트 metric

- **work-envelope 양식**: 통째 죽임 (옛 매 코드 응답 봉투 → 새 = 등급별 분기)
- **5단계 보고**: 옛 매 코드 응답 의무 → 새 *대규모 등급 Phase 완료 시만* + MD+HTML 이중 박음 (캡스톤 평가 자산)
- **work-pin**: 옛 60~70줄 목표 → 새 30~40줄 목표 (현 86줄, 별 시점 압축)
- **슬래시 수**: 옛 17 → 새 10 (-41%)
- **하네스 .md 파일**: 옛 ~26 (agents 7 + hooks 5 markdown 0 + commands 17 + setup-steps 5 + policies 5) → 새 ~37 (agents 11 + hooks README 1 + commands 11 + setup-steps 5 + policies 8 + knowledge 7) — 증가지만 *기능 단위 책임 분리* 결과

### 검증 결과

| 검증 | 결과 |
|---|---|
| dotnet build | ✅ green (0 경고 0 오류 / 3.82s) |
| dotnet test | ⏸️ SAC 차단 — Codex 환경 위탁 (응급 모드) |
| 새 hooks 작동 | ✅ pin-injector / phase-gate-validator (본 -DONE.md 박을 때 PostToolUse 검사 통과 예상) / tdd-guard / risk-detector / shared-discipline-guard |
| 새 슬래시 등록 | ✅ 시스템 카탈로그 자동 등록 (Step 4 후) |
| 옛 약속 가짜화 점검 | ✅ check-server-authority Reviewer 흡수 / check-work-envelope 양식 죽임 / Handlers/ 폴더 = 옛 운영에서 *실재* 박힘 (M3 Phase 03 봉합 commit `4065616`) |

### 학습 키워드 후보 (트랙 B — 본인 노션 별 시점)

본 Phase 06 신규 ★★★ 학습:

- **`atomic-transition-commit`** ★★★ — 큰 마이그를 *한 git 시점*으로 박음. 격리 폴더 누적 + 일괄 mv 전환 = DB dual-write phase 정신. 옛 100% → 새 100% atomic 발효. 반쪽 발효 차단 + 롤백 비용 명확 = 한국 게임 회사 백엔드 마이그 의사결정 어필 결정타
- **`self-modification-classifier-guard`** ★★★ — Auto Mode classifier가 *agent-controlling config 통째 교체*를 자동 차단. Claude(나)의 운영 규칙을 *나 자신이* 본 적도 없는 상태에서 통째 교체 = 안전망 정당. *단계 분할 + 사용자 명시 확인 게이트*로 우회. 분산 시스템에서 *self-modification 안전 패턴* 학습
- **`step-by-step-go-protocol`** ★★★ — 5 Step 분할 + 매 Step 사용자 명시 GO. Auto Mode bias (진행)와 *self-modification 안전*의 균형점. 사용자 부담 ↑ but 안전망 가치 큼. 큰 변경의 인간-루프-인 의사결정 패턴
- **`format-diet-via-grade-conditional`** ★★ — 양식 죽이는 X, *조건부화*. 5단계 보고 = 대규모만 / work-envelope = 통째 죽임. 양식 가치 vs 비용 평가 결정의 두 가지 결과 모두 활용
- **`reference-historical-vs-stale`** ★★ — 새 policies 본문에 옛 슬래시 reference 박혀있어도 *역사 컨텍스트*면 정합 / *살아있는 안내*면 갱신. 같은 문자열도 의도가 다름. 학부생 멘토링 = 의도 명시 가치

(나머지 누적 ★★★ M3.5 18건 — Phase 01~05 -DONE.md 참조)

### ADR + CHANGELOG reference

- **ADR-022** — `00_Document/ADR/harness/ADR-022-new-harness-v1.md` (신규)
- **ADR INDEX** — `00_Document/ADR/INDEX.md` 한 줄 추가
- **ADR_History** — `00_Document/ADR_History.md` 한 줄 추가 (5/21)
- **CHANGELOG [H]** — `.claude/CHANGELOG.md` 최상단 (5/21) — 모든 팀원 영향 + 슬랙/디스코드 동반 안내 의무

### M4 진입 게이트

- [x] M3.5 6/6 마감
- [ ] CONTEXT.md "현재 멈춤 지점" → M3.5 마감 + M4 진입 준비 (본 commit 동반)
- [ ] M4 첫 `/work:plan` = 새 운영 plan-auditor 자동 호출 첫 실측
- [ ] 외부 리뷰 mini-Phase 4건은 별 시점

---

## ⚠️ 잔존 함정

- **work-pin 86줄** — 목표 30~40 미달 (별 시점 압축)
- **dotnet test 실측 X** — 본 머신 SAC On 차단. Codex 환경 위탁 별 시점 (M3 Phase 04 5/18 박힌 응급 모드 정합)
- **새 hook 본문 false positive/negative 빈도** — M3.5 박힘 시점 실측 0건. M4 진입 첫 1주 안 관찰 → 명세 재조정 (특히 `risk-detector` 등급 상향 마찰 / `circuit-breaker` 정당 반복 알림 / `tdd-guard` 학습 호흡 영향)
- **새 SubAgent 풀세트 트리거 조건** — 명세 추측 기반 (ADR-019 정신 정합). 합류 후 1~2회 발동 관찰 → 트리거 조건 재조정
- **본 Phase 후 reviewer/plan-auditor 자동 호출 첫 실측** — 본 commit *후* 다음 turn 첫 작업에서 자연 발동 예상. 명세 정합 검증 X

---

## 갱신 이력

- 2026-05-21 — Phase 06 본 commit 박힘 직후 -DONE.md 박제 (옛 양식 정합 + 새 frontmatter 정합)
