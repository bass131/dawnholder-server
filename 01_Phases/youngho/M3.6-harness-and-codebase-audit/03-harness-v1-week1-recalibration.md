---
owner: youngho
milestone: M3.6
phase: 03
title: 하네스 v1 실측 1주차 재조정
status: done
grade: 대규모
risk: irreversible+harness
note: 옛 등급 복잡 = 옛 추측 (4-1만 가정). 실제 5건 Hook 변경 + ADR-020 + setup-steps + CHANGELOG [H] = 대규모 정합. 4-4 자기 참조 깃발 발동으로 자동 상향 (M3.6 Phase 03-B 4-4 진행 중 인지 박힘, 2026-05-22).
estimated: 2~3h
domain: cross
---

# Phase 03: 하네스 v1 실측 1주차 재조정

> **상태**: pending
> **마일스톤**: M3.6
> **등급**: 복잡 (cross 2 도메인 — Hook + policies, ~100~200줄, policies 갱신 = irreversible 부분)
> **담당**: 영호 단독 (새 하네스 v1 통제 영역)

---

## 🎯 목표

M3.5에서 박힌 `grade-and-risk.md` + `subagent-routing.md` 양쪽 본문에 명시된 **"M4 진입 후 첫 1주 안에 재조정 예정"** 항목을 M3.6에서 실측 후 정책 갱신. 추측 정책 → 실측 정책 진화.

별 시점 work-pin에 박힌 **#4 dangerous-cmd-guard word-boundary false positive 본질 봉합** (Python shlex.split 기반)도 본 Phase에 흡수.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (인프라 작동 baseline)

---

## 📝 작업 내용

### grade-and-risk.md 실측 8개 항목
- [ ] 줄 수 임계 적정성 (50줄/200줄/300줄) — M3.5 박힘 시점 누적 (Phase 06 99 files / +1906 / -5262 → 대규모 정합 확인)
- [ ] 위험 깃발 false positive 빈도 측정 — `02_Server/`에 박혔지만 로깅만인 케이스 등 (실제 발동 카운트 vs 의도 발동)
- [ ] 위험 깃발 누락 후보 식별 — `98_Shared/` 변경 자체 / `.claude/agents/` 변경 / `.claude/hooks/` 변경
- [ ] 등급 상향 사용자 마찰 — 본인이 단순 인식했는데 자동 상향된 빈도
- [ ] 처리 패턴 효율 — 보통에 Worker 위임 vs 메인 직접 속도 비교

### subagent-routing.md 실측 5개 항목
- [ ] 위임 false hit — 메인 직접이 더 빠른데 SubAgent 위임된 빈도 (현재까지 0건일 가능성, 본 마일스톤에서 자연 누적)
- [ ] 재귀 차단 마찰 — Worker가 다른 도메인 작업 필요 발견 시 coordinator 거치는 비용
- [ ] 에스컬레이션 빈도 (Sonnet 2회 실패 → Opus) 주간 카운트
- [ ] plan-auditor 가치 — 본 `/work:plan` 호출 시 발견 결함 수 vs 후속 사고 가설치 비율
- [ ] unity-bridge 단독 영역 효과 — prefab 사고 0건 유지 검증 (M3.6은 prefab 변경 X 예상)

### Hook hardening bundle (M3.6 cross-review γ 8회차 봉합 — "점검 도구의 점검")

> **묶음 정신** (β 재문의 권유): β2 lexer 매트릭스 + β1 must-pass/advisory + α3 HTML 페어 검사 + 신규 깃발 누락 후보 = **한 묶음으로 hook 안전망 본질 봉합**. 옛 PR #43 응급 봉합 → 본 본질 봉합.

#### 4-1 dangerous-cmd-guard 본질 봉합 (β2 매트릭스 + 옛 PR #43 후속)
- [ ] 현재 bash regex word-boundary 한계 분석 (`hook-false-positive-quoted-context` 학습 정합)
- [ ] Python shlex.split 기반 lexer 봉합 패치 작성
- [ ] **β2 매트릭스 시뮬** — 다음 5 명령 각각에 대해 "실행이면 차단 / commit message·PR body 안 literal 텍스트면 통과" 매트릭스 박음:
  - `rm -rf` (실행 차단 / "docs about: rm -rf" 통과)
  - `git reset --hard` (실행 차단 / heredoc 안 통과)
  - `git clean -fd` (실행 차단 / heredoc 안 통과)
  - `--admin` (gh pr merge --admin 실행 차단 / "PR #42 admin merge" body literal 통과)
  - `--force` (실행 차단 / body literal 통과)
- [ ] 시뮬 10건+ PASS — 본 매트릭스 + 일반 git status/build 통과 케이스
- [ ] `.claude/hooks/dangerous-cmd-guard.sh` 갱신 또는 `.py` 신설 (Hook 환경 의존성 ADR-020 정합)

#### 4-2 Hook must-pass / advisory 분리 (β1)
- [ ] Phase 01 "Hook 7개 작동 6+ / 미작동 1- 허용" 패턴이 *false negative 차단 목적과 충돌* — 안전망 일부 죽어도 "정합 OK" 잘못 통과 위험
- [ ] **must-pass** (실패 시 Phase 02 진입 금지): dangerous-cmd-guard / risk-detector / phase-gate-validator / shared-discipline-guard
- [ ] **advisory** (실패 시 사유 박고 통과 가능): tdd-guard / circuit-breaker / pin-injector
- [ ] Phase 01 정의 본문에 must-pass/advisory 표 박음 (역방향 갱신) + 본 Phase 03 후속 검증

#### 4-3 phase-gate-validator HTML 페어 검사 (α3)
- [ ] 현재 Hook은 frontmatter 5필드 + 대규모 등급 5 라벨까지만 검사. *대규모 등급 `-DONE.md`의 HTML 페어 박힘 의무*는 강제 안 됨
- [ ] Phase 06 같은 대규모 마감 시 *MD+HTML 이중 박음* 약속이 Hook으로 미검사 = 가짜 약속 패턴 신호
- [ ] `.claude/hooks/phase-gate-validator.sh`에 "대규모 등급 `-DONE.md` 박을 때 동명 `.html` 페어 존재 검사" 추가
- [ ] 시뮬: 대규모 등급 MD only → exit 2 차단 / MD+HTML 통과

#### 4-4 위험 깃발 누락 후보 박음 (β 재문의 신규 발견)
- [ ] `.claude/hooks/` 변경 시 `risk-detector.sh`가 깃발 발동 X (β 재문의 발견) — 본 Phase 작업 자체가 Hook 변경 = 그 자체 검증 fixture
- [ ] **결정 박음**: `risk-detector.sh` 깃발 패턴에 `.claude/{hooks,agents,commands}/**` 추가할지 / 옛 정합 유지하고 *별 영역 관찰*만 박을지
- [ ] 추가 권장: 본 Phase 작업 진행 자체로 깃발 누락 패턴 측정 (변경 직후 위험 깃발 누적 0 vs 1+)

### ask 매처 false positive/negative 측정 (PR #43 박은 후)
- [ ] settings.json `ask` 매처 3개 발동 빈도 측정
- [ ] false positive (의도된 작업이 ask로 막힌 경우) 카운트
- [ ] false negative (위험 작업이 ask 통과한 경우) 카운트

### 정책 갱신
- [ ] `grade-and-risk.md` §8 "실측 후 재조정 항목" 체크박스 갱신 + 본문 정정 (필요 시)
- [ ] `subagent-routing.md` §9 동일
- [ ] `.claude/CHANGELOG.md` [M] entry 박음

---

## ✅ 완료 조건

- [ ] grade-and-risk 8개 + subagent-routing 5개 항목 × 실측 데이터 박힘 (정량)
- [ ] dangerous-cmd-guard 본질 봉합 patch 작성 + 시뮬 7~10건 PASS
- [ ] policies 2개 §8/§9 체크박스 ≥80% 봉합
- [ ] CHANGELOG [M] entry 박음
- [ ] `-DONE.md` 박음 (복잡 등급)

---

## 🧪 테스트

**자동**:
- Hook 시뮬: dangerous-cmd-guard 새 패치 × 7~10 시나리오 (admin / quoted "admin" context / 일반 git push 등)
- `dotnet build` green 유지

**수동**:
- M3.5~M3.6 본 세션 누적 Hook 발동 로그 grep (`.claude/state/risk-flags.txt`)
- SubAgent 위임 카운트 (본 세션 turn별)

---

## 📚 학습 포인트

- **추측 정책 → 실측 정책 진화** — 명시적으로 "실측 후 재조정 예정" 박은 정책이 *실측 시점*에 봉합되는 *정책 생명주기*. 한국 게임 회사 백엔드 면접 *AI 자동화 의사결정* 어필
- **lexer 봉합 정신** — bash regex의 word-boundary 한계를 Python shlex.split로 본질 봉합 = *공구의 한계 인식 → 적절한 공구 교체*. 옛 PR #43 응급 봉합 → 본 Phase 본질 봉합 정신
- **false positive vs false negative 균형** — 안전망 동시 작동 시 어느 쪽이 더 큰 비용인지 측정. M3.5 박힌 정신(`circuit-breaker-false-positive-prevention`)의 1주차 실측

---

## ⚠️ 함정 / 주의사항

- **Hook 변경은 `.claude/CHANGELOG.md` [M]/[H] 의무** — 모든 팀원 매번 영향
- **본 Phase 자체가 *새 하네스 v1 실측 데이터 생성*** — 본 Phase 진행이 추가 데이터 누적 (자기 참조). 본 Phase 진행 *전*의 누적 데이터로 1차 실측, *후*의 변동은 Phase 06 종합 보고에 박음
- **policies 갱신은 헌법 변경 X** — policies는 ADR 하위. ADR 정정이 필요한 경우 (예: ADR-022 본문 정정) Phase 02로 위임

---

## ➡️ 다음 Phase

- Phase 04 (서버 코드 전수조사) + Phase 05 (클라 코드 전수조사) — 본 Phase + Phase 02 둘 다 끝나야 진입

---

## 📋 박제 (완료 후)

- 등급 복잡 → **`-DONE.md` 박음**
- 학습 키워드 후보:
  - `dangerous-cmd-guard-lexer-essence-fix` (Python shlex 본질 봉합)
  - `policy-week-1-recalibration-cycle` (추측 → 실측 진화 사이클)
  - `ask-matcher-false-positive-negative-balance` (PR #43 후속)
