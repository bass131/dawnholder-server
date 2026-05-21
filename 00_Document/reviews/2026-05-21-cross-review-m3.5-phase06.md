# Cross-Review — 2026-05-21 — M3.5 Phase 06 atomic 발효 + 후속 봉합

**γ 방식 8회차** — 옛 ad-hoc 4~7회차 후 정착, `/cross-review` 슬래시화 후 첫 호출. 본 산출물 = α (Claude reviewer SubAgent, Tier 2-A) + β (Codex CLI 0.130.0, gpt-5.5 xhigh) 결과 풀세트 + γ 비교 분석.

## 변경 범위

- **브랜치**: `youngho/harness-v1` (HEAD = `48bb8c9`, main 기준 4 commit ahead)
- **commits**:
  - `b1eed04` Phase 05 -DONE.md (이전 세션)
  - `226f34c` M3.5 Phase 06 atomic 발효 [ADR-022]
  - `578511f` 후속 — .githooks/pre-commit reference 정합
  - `48bb8c9` 후속 — 가짜 약속 잔존 4건 봉합 (봉합 4회째, constitution-partial-update-trap 학습)
- **변경 파일**: ~108 (67 삭제 + 29 갱신 + 26 신규)
- **등급**: 복잡 (정의 frontmatter) — 위험 깃발: irreversible
- **본질**: 옛 하네스 운영 → 새 하네스 v1 atomic 전환. 코드 변경 X (.claude/ + 00_Document/ + 1줄 98_Shared/CLAUDE.md 봉합)

## α — Claude reviewer 결과

### 🔴 4건 (이미 봉합 완료 — commit 48bb8c9)

1. `98_Shared/CLAUDE.md:74` 옛 `validate-shared-changes.sh` → 새 `shared-discipline-guard.sh`
2. `.claude/templates/` 3 파일 옛 `/journal:*` + hook 이름 → 트랙 B 안내 + 새 이름 (7 reference 정정)
3. `ADR-018` 부분 superseded 표기 한 줄 박음
4. `ADR-019` 부분 갱신 표기 한 줄 박음

### 🟡 제안 4건 (별 시점)
- `policies/INDEX.md:17` 카운트 방식 차이 (옛 4+신규 3 vs ADR-022 옛 5+신규 3)
- 옛 ADR *부분 superseded* 표기 규칙 미정
- plan-auditor/reviewer 자동 호출 *실재 메커니즘* 미박힘 (Soft 트리거만)
- 새 hook 7개 *실측 0건* (M4 첫 1주 발동 로그 권장)

### 🟢 잘 된 점
- 헌법 절대 원칙 5개 글자 그대로 100% 보존 (라인 단위 diff 검증)
- 헌법 #4 신규 약속 *실재* (settings.json PreToolUse Edit|Write matcher + shared-discipline-guard.sh 6KB + 차단 동작)
- ADR-022 본문 10개 핵심 변경 모두 commit 박힘
- CHANGELOG [H] entry 풀세트 + 합류자 안내 + 5/21 일자 정합
- `01_Phases/inkyu/` 합류 대비 폴더 신설 + frontmatter `owner:` 의무

### 🎓 학습 ★★★
α reviewer가 본 transition을 *constitution-partial-update-trap 학습의 메타 적용*으로 박음 — 본 transition이 *명시한 학습*을 *바로 그 패턴으로* 어겼다는 정확한 자가 발견.

## β — Codex 결과 (Codex CLI 0.130.0, gpt-5.5 xhigh)

### 🔴 P1 — 시급 (α가 못 본 더 깊은 패턴, 봉합 5회째 후보)

**1. Hook payload stdin 파싱 누락** — `.claude/hooks/dangerous-cmd-guard.sh:19` + risk/tdd/shared/phase/circuit 5 hook 모두 동일
- Claude Code hook payload = **stdin JSON** (`{tool_input: {command, file_path}, tool_name}`)
- 새 7 hook 모두 `CLAUDE_TOOL_INPUT_*` 환경변수/argv만 읽음 → COMMAND 빈 상태로 `exit 0`
- **결과**: rm -rf / git reset --hard / force push 차단 **전부 무력화** → 가짜 약속 5회째
- work-pin 박힌 학습 정합: "Phase 03 함정 — Hook 본문 PreToolUse Bash 환경변수 명세 미확정 (`CLAUDE_TOOL_INPUT_COMMAND` 추정). Phase 06 전환 후 실측" → **β가 그 실측 결과 정확히 발견**
- 수정 방향: 각 hook 본문에 stdin JSON 파싱 박음 (jq 또는 native bash JSON 파싱). 모든 7 hook 적용.

**2. risk-detector가 work-pin 실제로 안 갱신** — `.claude/hooks/risk-detector.sh:102`
- 약속 (헌법 + `grade-and-risk.md`): "등급 자동 상향 + work-pin 갱신"
- 실재: `.claude/state/risk-flags.txt`에만 누적 + stderr 알림. work-pin **미수정**
- **결과**: trust-boundary/irreversible/unity-asset 편집 후도 SubAgent 라우팅 낮은 등급으로 진행 위험
- 수정 방향: (A) work-pin 실제 갱신 박음 (sed 또는 별 함수) OR (B) 헌법/policies/ADR 약속을 *알림 전용*으로 정정 (사용자가 수동 work-pin 갱신)

### 🟡 P2 — 중간 (별 시점 봉합 가능)

**3. `subagent-routing.md`** = 옛 8풀 기준, `knowledge-gc` 누락
- CLAUDE.md / ADR-022 / `.claude/agents/_routing.md`는 풀 9개 정의 — 본 정책 파일만 옛 8풀
- 수정 방향: subagent-routing.md 갱신 (9풀 + knowledge-gc 권한·트리거·동기화 책임)

**4. `hooks/README.md:5`** = "격리 폴더 안 *제안*이라 실행 안 됨"
- Phase 06 전환 후 settings.json이 hooks 실제 등록 = 활성 상태
- 수정 방향: README 갱신 (active 표기)

**5. `CLAUDE.md:144`** = §4 본문에 `shared-discipline-guard.sh` 명시 = ADR-022 "100% 보존" 약속과 모순
- 옛 main = generic `.claude/hooks/` 참조 / 새 = hook 이름 명시
- 수정 방향: hook 이름은 policy/ADR로 옮기고 본문은 원문 그대로 OR ADR 약속 수정

### ⏸️ dotnet test 재실측 = 실패
- Codex 환경에서도 **CreateProcessAsUserW 1312 에러** (sandbox 권한 한계, SAC 패턴과 동일)
- 별 환경 필요: cloud Codex 또는 외부 머신
- 본 transition은 코드 변경 X라 회귀 0건 *예상*이지만 실측 0건은 큰 부담 그대로

## γ 비교 분석 — 합 9건

| 차원 | α (Claude reviewer) | β (Codex) | γ |
|---|---|---|---|
| 헌법 §1~§5 100% 보존 검증 | ✅ 통과 | ⚠️ §4에 hook 이름 명시 박힘 = 부분 모순 | β 추가 |
| 가짜 약속 잔존 점검 | 🔴 4건 (이미 봉합) | 🔴 5건 추가 (Hook 무력화 시리즈) | 양쪽 다 패턴 발견, 다른 자리 |
| 코드 직접 접근 | R only (Read tool) | R + W + 실행 가능 (sandbox) | β 우위 |
| dotnet test 실측 | ❌ 본 머신 SAC | ⏸️ Codex 환경도 sandbox 실패 | 양쪽 미수행 |
| 토큰 비용 | 메인 세션 ↑ | 외부 도구 (본인 Codex 한도) | 분담 |
| 시각 | 헌법 우선 + 메타 패턴 | 정량 검증 + Hook payload 인터페이스 깊이 | 상호 보완 |
| 봉합 자가 점검 | α 4건 봉합 commit 48bb8c9 자체 점검 X | 48bb8c9 박힌 후도 5건 추가 발견 | β가 더 깊음 |

**양쪽 다 잡음** = 0건 (γ 비교 정신상 *최우선*인데 0건 = 양쪽 시각 완벽 분리됨, 상호 보완 ★★★)

**α만 잡음** = 4건 (가짜 약속 잔존 — 이미 봉합)

**β만 잡음** = 5건 (Hook payload + risk-detector + subagent-routing + hooks/README + 헌법 §4)

**양쪽 통과** = 헌법 절대 원칙 5개 본질 / ADR-022 본문 10개 결정 commit 박힘 / CHANGELOG [H] 풀세트 / 격리 폴더 atomic 정리 / inkyu/ 신설

## 결정 권유

### 🔴 P1 봉합 (β 발견 2건)

**옵션 A** = 즉시 봉합 (별 commit 1회)
- Hook 7개 본문 stdin JSON 파싱으로 정정 (~30분 작업)
- risk-detector.sh work-pin 갱신 로직 박음 또는 약속 *알림 전용*으로 정정
- 학습 ★★★ 새 항목: `hook-payload-stdin-vs-env-vars` (옛 추측 vs 새 실측)

**옵션 B** = 별 마일스톤 (M4 진입 후 첫 실측 1회 후 봉합)
- 본 transition 자체가 *학습 박제 자산* — β 발견이 work-pin "Phase 03 함정 후 실측" 약속 *정확 이행*
- 봉합 시점은 *실제 hook 발동* 첫 실측 후 (M4 첫 1주)

**옵션 C** = 헌법/policies *deprecation* 명시 + 별 commit (1줄씩)
- "본 hook 인터페이스는 추측 기반 명세 — Phase 06 후 실측 결과 stdin JSON. 봉합 commit 별도 후속" 한 줄씩 박음

### 🟡 P2 봉합 (β 3건 + α 4건 = 7건)
- 별 마일스톤 OK (시급 X, M4 진입 후 별 시점)
- 또는 본 commit과 묶어 후속 정합 commit 1회

### 헌법 §4 모순 (β #5) = 의사결정 분기
- 옵션 1: hook 이름을 §4에서 제거 + policy/ADR로 이동 → ADR-022 "100% 보존" 약속 유지
- 옵션 2: ADR-022 본문 정정 → "절대 원칙 5개의 *본질* 100% 보존, 단 hook 이름 정합 위해 §4 본문 한 줄 정정" 명시
- 권장: **옵션 1** (constitution-partial-update-trap 학습 정합)

## 옛 학습 정합

본 transition의 핵심 학습 `constitution-partial-update-trap`이 *재귀 발견*:
- **1차** = ADR-022 본문에 명시 "절대 원칙 5개 글자 그대로 100% 보존" 약속
- **2차** = α reviewer 4건 발견 → commit 48bb8c9 봉합
- **3차** = β Codex 5건 추가 발견 (Hook 무력화 + 헌법 §4 모순 + 3 docs 정합)

**큰 마이그 직후 *연쇄 정합 게이트*가 박힘이 본질** — 다음 마이그(M4+)에서는 *grep 패턴 + Codex β cross-check* 모두 *마이그 정의*에 박는 게 표준.

## 학습 키워드 후보 (트랙 B 신규)

- **`hook-payload-stdin-vs-env-vars`** ★★★ — Phase 03 박힘 시점 추측 (`CLAUDE_TOOL_INPUT_*`) vs Phase 06 실측 (stdin JSON). 옛 운영의 *명세 미확정* 함정이 새 운영에서 *5회째 가짜 약속*으로 표면화. AI 자동화 인터페이스 학습 결정타
- **`gamma-recursive-discovery`** ★★★ — α + β 시각 분리도 ↑ → *상호 보완 ★★★*. 양쪽 다 잡음 0건 = 시각 완벽 분리 = γ 8회차 정착 가치. 면접 *외부 시각 가치 + 본 시각 부족 인지* 어필
- **`atomic-transition-recursive-trap`** ★★ — *큰 마이그 + 학습 명시 + α 봉합 + β 발견 + 또 봉합*의 재귀 구조. constitution-partial-update-trap이 무한 재귀 가능성

## 다음 액션

1. **P1 봉합 결정** (옵션 A/B/C 사용자 선택)
2. P2 봉합 (별 시점)
3. PR + 셀프 머지 (P1 옵션에 따라)
4. M4 진입 (별 시점)
