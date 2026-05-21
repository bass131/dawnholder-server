# Cross-Review — 2026-05-22 — M3.6 plan + Phase 01 baseline

> **`/cross-review` 슬래시 흐름** (γ 방식 정착 후 슬래시화 첫 정식 실측). γ 1~7회차 ad-hoc → 본 8회차부터 슬래시화 산출물.

---

## 변경 범위

- 변경 파일 (untracked):
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/_milestone-plan.md`
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/_milestone-plan.html`
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/01-pre-flight-infra-baseline.md`
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/02-constitution-adr-policies-audit.md` (P2 #1 봉합 후)
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/03-harness-v1-week1-recalibration.md`
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/04-server-codebase-audit.md` (P2 #2 봉합 후)
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/05-client-codebase-audit.md`
  - `01_Phases/youngho/M3.6-harness-and-codebase-audit/06-external-review-absorption-and-final-report.md`
- 변경 파일 (working dir modified): `03_Client/Assets/Plugins/Shared/Shared.dll` (dotnet build 자동 산출물, ADR-010 정합)
- 등급: **대규모** (위험 깃발: `irreversible` — Phase 06 PR/머지 + 일부 정책 갱신)
- Phase 01 baseline 진행 결과 포함 (검증 완료, build green / Hook 8 / SubAgent 9 / Knowledge 5 / git check-ignore 통과)

## 사전 점검 (plan-auditor Tier 2-B, 본 cross-review 이전)

- P0 = 0건 / P1 = 1건 (옵션 B 채택) / P2 = 2건 (모두 봉합 완료) / 합격 6건
- plan-auditor 시각 = *plan 내 정합* (분해/의존성/완료 조건/등급/헌법/시나리오)

---

## α — Claude reviewer 결과

**시각**: 헌법 우선 메타 점검 (점검 마일스톤의 점검). 약속 vs 실재 정합.

🔴 **P0 = 0건**

🔴 **P1 = 3건** (모두 *문서 약속 박힘 vs 실재 박힘 정합 깨짐* — 헌법 #4 Shared Discipline 메타 적용)

| # | 결함 | 위치 | 옵션 |
|---|---|---|---|
| α1 | **policies 카운트 정합 깨짐** — plan/헌법 "8개" vs `policies/INDEX.md` 본문 표 "7개" vs 실제 파일 9개 (INDEX + 8 정책). PR #43 박힌 `pr-and-merge-gate.md`가 INDEX 표에 *누락*. 가짜 약속 4번째 발본 후보 | `00_Document/policies/INDEX.md` | A: 즉시 봉합 (한 commit, 5~10줄) |
| α2 | **SubAgent 9개 필터 명세 부재** — `.claude/agents/` 실제 11개 (9 + `_routing` + `_escalation`). Phase 01 본문 "9개 정의 + frontmatter 정합"이 ls 후 카운트 시 false positive 위험. 점검 결과 신뢰 baseline 흔들림 | `01-pre-flight-infra-baseline.md` | A: 한 줄 ("`frontmatter `name:` 박힘 9개`") |
| α3 | **HTML 페어 Hook 미검사** — Phase 06 "MD+HTML 이중 박음" 약속이 `phase-gate-validator.sh`로 강제 안 됨. 가짜 약속 패턴 신호 | `phase-gate-validator.sh` | B: Phase 03 흡수 또는 별 Phase |

🟡 **P2 = 5건**:
1. Phase 04 dotnet test Cloud Codex 위탁 결과 *반영* 시점 완료 조건 명시 부재 (β 결과에서 봉합됨 — 아래 참조)
2. **risk-detector.sh false positive 본질 봉합** Phase 03 흡수 — dangerous-cmd-guard와 *동형 결함*이라 한 lexer 봉합으로 묶기 (★ β에서도 동형 잡힘)
3. Phase 02 헌법 5원칙 시연 증거 보존 위치 명시 (별 `_constitution-evidence-*.md` 패턴)
4. Phase 05 UI.unity "commit history만" 빈약 → "git log + diff stat까지" 명시
5. Phase 03 ask 매처 측정 데이터 부족 → M4 1주차 후속 측정 결정 박음

🎓 학습: *점검 마일스톤의 순환 신뢰 함정* — plan-auditor의 *plan 내 정합* 시각이 *plan ↔ 외부 자산 정합* 영역을 사각지대로 둠.

---

## β — Codex CLI 결과 (코드 직접 접근 + dotnet test 재실측)

**시각**: 정량 검증 + 외부 자산 실재 + Unity scope. 본 머신 SAC On 차단 회피 가치 입증.

🔴 **P0 = 0건** (α와 일치)

🟢 **dotnet test Codex 환경 실측**: **170 passed / 0 failed / 1 skipped / 46초** — 본 머신 차단 회피 가치 ★★★ 입증 (M3 baseline 160 → +10 정합)

🔴 **P1 = 4건** (β 시각):

| # | 결함 | 위치 | 옵션 |
|---|---|---|---|
| β1 | **Hook baseline 통과 조건 너무 느슨** — Phase 01 "Hook 7개 중 작동 6+ / 미작동 1- 허용"이 *false negative 차단 목적과 충돌*. dangerous-cmd-guard / risk-detector / phase-gate-validator / shared-discipline-guard 중 하나 죽어도 "정합 OK" 잘못 통과 | `01-pre-flight-infra-baseline.md:76` | A: must-pass / advisory 표 분리, must-pass 실패 시 Phase 02 진입 금지 |
| β2 | **Phase 03 lexer 봉합 테스트가 실제 false positive 미겨냥** — 사고는 commit/PR body 안 literal `rm -rf` 인데 계획 테스트는 admin / quoted "admin" / 일반 git push 중심. shlex.split로 바꿔도 *실제 사고 재발 케이스 누락 위험* | `03-harness-v1-week1-recalibration.md:52, :83` + `dangerous-cmd-guard.sh:36` | A: rm -rf / git reset --hard / git clean -fd / --admin / --force 각각 "실행이면 차단 / commit message면 통과" 매트릭스 추가 |
| β3 | **Client audit dotnet build로 안 덮임** — `03_Client/Assets/Tests/EditMode/Dawnholder.Client.Tests.EditMode.asmdef` 실재. Unity 컴파일/데모가 "옵션"이라 Unity 스크립트 컴파일 / asmdef 참조 / EditMode 테스트 실패 누락 위험 | `05-client-codebase-audit.md:88-90` | A: Unity batchmode compile 또는 EditMode test required, 미실행 시 Phase 06 "미검증 리스크" 박음 |
| β4 | **외부 리뷰 입력 파일 repo에 없음** — `Dawnholder-harness-review-2026-05-19.md` 기준 파일 repo 안 X. 실재 = `00_Document/reviews/2026-05-19-harness-review-followup-1of5.md`만. Phase 06 막판 입력 결손 | `06-external-review-absorption-and-final-report.md:24, :41` | A: Phase 01/02에 "외부 리뷰 원본 위치 확정 또는 repo 반입" 선행 게이트 추가 |

🟡 **P2 = 1건**:
- `98_Shared/CLAUDE.md:19` ProtocolVersion.Current=2 박혀있지만 실제 코드 = 3. `PacketRoundTripTests.cs:481, :540`도 v2 흔적 잔존. *가짜 약속 계열*. Phase 02/04에 stale sweep 한 줄 추가 권장

🎓 합격: ADR 22 / policies 8 / SubAgent 9 / Knowledge 5 / Hook 7+helper *실제 파일 대체로 정합*. **위 P1/P2만 봉합하면 M4 진입 전 점검 마일스톤 GO 가능**.

---

## γ 비교 분석

### 양쪽 다 잡음 (최우선) — 1건

| 영역 | α 박음 | β 박음 |
|---|---|---|
| **dangerous-cmd-guard quoted context lexer 봉합** | P2 #2 — risk-detector 동형 결함 흡수 권유 | P1 #2 — 실제 사고 재발 케이스 (commit/PR body literal) 테스트 매트릭스 추가 |

→ **양쪽 시각 합치면 Phase 03 봉합 = lexer 본질 봉합 + 실제 사고 매트릭스 + risk-detector 동형 결함 묶음 세 가지 한 commit**. 옛 학습 정합 (PR #43 응급 봉합 → 본질 봉합).

### α만 잡음 (헌법 우선 시각) — 3건

| # | α 결함 | β 미언급 사유 |
|---|---|---|
| α1 | policies 카운트 정합 (`INDEX.md` 표 누락) | β는 "policies 8개 실재 정합"이라 합격 박음 — β 시각이 *실제 파일 카운트* 정합이라 *문서 본문 카운트* 사각지대 |
| α2 | SubAgent 9개 필터 명세 부재 | β도 "SubAgent 9개 실재 정합"이라 합격 — *검증 방법* 시각 결손 |
| α3 | HTML 페어 Hook 미검사 | Codex 미언급 — β의 정량 시각이 *약속 vs 검증 인프라* 매핑 사각지대 |

**α 고유 가치 검증**: 모두 *약속 vs 약속 검증 인프라* 매핑 영역. **헌법 #4 메타 적용** 시각.

### β만 잡음 (정량 + 외부 자산 + 코드 직접 접근) — 4건 + dotnet test 170 passed 추가

| # | β 결함 | α 미언급 사유 |
|---|---|---|
| β1 | Hook baseline 통과 조건 (must-pass/advisory) | α는 "Hook 7+ 작동 1- 허용"을 *유연성*으로 봄. β의 *false negative 차단 목적 정합* 시각 보완 |
| β3 | Unity batchmode/EditMode test (Phase 05 dotnet build 안 덮음) | α는 04_ClientNet/ Y2 정합까지만 봄 — *03_Client/Assets/Tests/EditMode/* 디렉토리 실재 검증 β 우위 |
| β4 | 외부 리뷰 원본 파일 repo 외부 (Phase 06 입력 결손) | α는 work-pin 박힘만 신뢰 — β의 *실재 파일 검증* 가치 입증 |
| β P2 | ProtocolVersion stale v2 흔적 (`98_Shared/CLAUDE.md:19` + `PacketRoundTripTests.cs`) | α 미언급 — β의 *코드 직접 접근* 가치 |
| (가외) | **Codex 환경 dotnet test 170 passed / 46초** | α 환경 시뮬 X — β의 *별 환경 실측* 가치 (본 머신 SAC On 차단 회피) |

**β 고유 가치 검증**: 모두 *코드 직접 접근* + *외부 자산 실재 검증* + *정량 측정* 영역. **본 머신 SAC On 차단 회피 = β만 가능**.

### 양쪽 통과 (합격 축)

- 헌법 5대 절대 원칙 정면 위반 0건
- ADR 22 / policies 8 (실제 파일 카운트) / SubAgent 9 / Knowledge 5 / Hook 7+helper *실재 정합*
- "발견→분기→봉합" 일관 / Coordinator+Worker 시나리오 명세 방향 / Rule of Three 흡수 패턴
- Phase 04 시나리오 명세 의무 (plan-auditor P2 #2 봉합 후) 정합

---

## 결정 권유

### 🔴 양쪽 다 잡음 (최우선) → 옵션 A 즉시 봉합 1건

- **dangerous-cmd-guard 본질 봉합 = lexer + 매트릭스 + risk-detector 묶음** → Phase 03 정의 강화 (즉시 plan 갱신, Phase 03 진행 시 한 commit)

### 🟡 한쪽만 잡음 → 본인 결정 권유

**옵션 A 즉시 봉합** (5분 내, Phase 02/03/05 진입 전 plan 갱신 필요):

- **α1** policies INDEX 카운트 정합 — 코드+문서 동시 commit 정신, *지금 즉시* 봉합 권장 (가짜 약속 발본은 발견 시점 즉시 봉합이 옛 학습 정합)
- **α2** SubAgent 9개 필터 명세 — Phase 01 정의 한 줄
- **β4** 외부 리뷰 원본 위치 확정 — 사용자 확인 필요 (`1of5.md`만 박혀있는데 *5건 중 1건*인가, *4건 + 1*인가). Phase 01/02 선행 게이트 한 줄
- **β P2** ProtocolVersion stale sweep — `98_Shared/CLAUDE.md:19` + `PacketRoundTripTests.cs` 한 commit (Phase 02 흡수 또는 즉시)

**옵션 B 흡수 plan 갱신** (관련 Phase 정의 강화):

- **β1** Hook must-pass/advisory 분리 → Phase 01 정의 갱신 또는 Phase 03 흡수
- **β3** Unity batchmode/EditMode test → Phase 05 정의 강화 (required 승격)
- **α3** HTML 페어 Hook 검사 → Phase 03 흡수 (Hook 봉합 항목에 한 줄)

### 🟢 양쪽 통과 → 안심하고 진행

- 헌법 5원칙 정합 / 카운트 정합 / 분해 정합 — M3.6 진입 정합

---

## 옛 학습 정합

**잠복 패턴 확인**:

- ✅ **가짜 약속 시리즈 4번째 발본** — α1 (policies 카운트) + α3 (HTML 페어 Hook) = 옛 학습 `false-promise-{first,second,third}-instance` Rule of Three 통과 후 *4번째 사례*. 본 점검 마일스톤이 정확히 이런 패턴 잡기 위한 목적이었음 — **목적 적중 = M3.6 첫 가치 증명**
- ✅ **safety-net-trio-without-legal-bypass-path** 학습 정합 (β1 Hook must-pass/advisory) — 안전망 동시 작동 시 *어느 게 진짜 차단* 정합 약속 빈약
- ✅ **hook-false-positive-quoted-context** 학습 정합 (양쪽 다 잡음 #1) — PR #43 후속 봉합이 응급 봉합이었고, 본질 봉합 + 매트릭스 박음이 β로 다시 확인
- 🆕 **γ-8th-instance-slash-formalization** (★★★ 후보) — γ 방식 ad-hoc 7회 → 본 8회차 슬래시화 첫 실측. α + β 시각 보완 *현장 실증* (α 헌법 우선 / β 정량 우선 / 보완성 7건 분리 정합)
- 🆕 **plan-auditor-vs-cross-review-blind-spot-complementarity** (★★ 후보) — plan-auditor의 *plan 내 정합* 시각 + cross-review의 *plan ↔ 외부 자산 정합* 시각 보완. 둘 다 의무 (Tier 2-B + 슬래시 cross-review) 정신 검증
- 🆕 **codex-cloud-test-environment-value-confirmation** (★★ 후보) — Codex 환경 dotnet test 170 passed / 46초 = 본 머신 SAC On 차단 회피 가치 *측정 정량 실증*

---

## 본 cross-review 자체 가치 평가

- **시간 비용**: α background ~2.4분 + β 사용자 실행 + γ 비교 산출물 박음
- **발견 가치**: P0=0 / P1=7건 (양쪽 다 1 + α만 3 + β만 3) / P2=6건 / dotnet test 실측 추가 가치
- **학습 박힘**: ★★★ 1 + ★★ 2 = 3건 후보 (트랙 B Notion 박제 시점)
- **β만 잡은 영역 비율**: 4/8 결함 = 50% — γ 방식 정신 검증 ("β만 잡음 = β 호출 가치 입증")
- **결론**: cross-review = 가치 ↑↑ (특히 점검 마일스톤의 *순환 신뢰 함정* 차단). 큰 PR 머지 *전* 권유 정신 정합 검증

---

## 갱신 이력

- 2026-05-22 — `/cross-review` 슬래시 첫 정식 실측 (γ 방식 ad-hoc 7회차 → 본 8회차 슬래시화). M3.6 plan + Phase 01 baseline 점검. P1 7건 + P2 6건 발견. α + β γ 비교 보완성 검증.
