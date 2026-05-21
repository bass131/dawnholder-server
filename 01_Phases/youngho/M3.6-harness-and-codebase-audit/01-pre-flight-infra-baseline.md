---
owner: youngho
milestone: M3.6
phase: 01
title: Pre-flight — 인프라 작동 baseline
status: pending
grade: 보통
estimated: 1.5~2h
domain: cross
---

# Phase 01: Pre-flight — 인프라 작동 baseline

> **상태**: pending
> **마일스톤**: M3.6
> **등급**: 보통 (1 도메인 cross — 영호 단독, 검증 위주, ≤50줄 변경)
> **담당**: 메인 세션 직접 (영호)

---

## 🎯 목표

M3.5 atomic 전환(2026-05-21) 후 **새 하네스 v1 인프라 4종(빌드 + Hook 7 + SubAgent 9 + Knowledge 5)이 실제로 작동하는지 baseline 확정**. 이후 Phase 02~06의 점검 결과 신뢰성을 보장하는 사전 게이트.

*점검 결과가 "정합 OK"라고 박혔는데 사실 Hook이 안 돌고 있었으면* false negative 사고. 본 Phase가 그 위험 차단.

---

## ⏪ 사전 조건

- [x] M3.5 Phase 06 atomic 전환 완료 (PR #42 머지 `fc983ea`)
- [x] M3.5 후속 봉합 PR #43 main 머지 (`fb5ae36`)
- [x] 본 세션 `youngho/harness-and-codebase-audit` 브랜치 분기 완료
- [x] work-pin M3.6 진입 좌표로 박힘

---

## 📝 작업 내용

### 빌드 baseline
- [ ] `dotnet build Dawnholder.slnx` green 확인 (경고 0 / 오류 0)
- [ ] 빌드 시간 측정 (M3.5 박힘 시점 3.49~3.82s 정합)
- [ ] Shared.dll → 03_Client/Assets/Plugins/Shared/Shared.dll 자동 복사 확인 (ADR-010)
- [ ] Dawnholder.Client.Net.dll → 03_Client/Assets/Plugins/ClientNet/ 자동 복사 확인

### Hook 7 시뮬 발동
- [ ] `pin-injector.sh` — UserPromptSubmit 시 work-pin 자동 주입 확인 (본 세션 매 턴 박혀있음)
- [ ] `phase-gate-validator.sh` — `-DONE.md` Write 시 frontmatter 검사 (`grade` / `owner` / `summary` / `phase` / `status` 5필드)
- [ ] `risk-detector.sh` — trust-boundary / irreversible / unity-asset 깃발 자동 검출 (stderr 알림 + `.claude/state/risk-flags.txt` 누적)
- [ ] `circuit-breaker.sh` — Worker 무한 재시도 차단 (Bash 제외 + 등급별 임계 차등)
- [ ] `shared-discipline-guard.sh` — 98_Shared/ 변경 시 양쪽 빌드 검사 (옛 경고 → 새 exit 2 차단)
- [ ] `tdd-guard.sh` — 새 핸들러/공식 추가 시 테스트 페어 누락 경고 (차단 X, 학부생 학습 호흡)
- [ ] `dangerous-cmd-guard.sh` — curl/wget/secrets read + `--admin` literal 차단 (PR #43 박힘)

### SubAgent 9 호출 가능 검증

> **필터 명세** (M3.6 cross-review α2 봉합): `.claude/agents/` 디렉토리에는 9 SubAgent + `_routing.md` + `_escalation.md` 메타 2 파일 = **총 11 파일**. SubAgent 9개 식별은 *frontmatter `name:` 필드 박힘 9개* 기준 (메타 2 파일은 `name:` 없음). `ls` 후 카운트 시 11이 정상, *9개 식별*은 `grep "^name:"`로.

- [ ] `reviewer` 자동 호출 — 도메인 Worker 코드 변경 후 트리거 확인
- [ ] `plan-auditor` 자동 호출 — 본 마일스톤 Phase 정의 박은 직후 자동 발동 확인 (본 `/work:plan` 호출의 일부)
- [ ] `coordinator` 분해 1회 시뮬 — Phase 04 진입 시 발동 예상 (본 Phase에서는 *호출 가능성 검증만*)
- [ ] `server` / `shared` / `client` / `qa` / `unity-bridge` — 정의 파일 존재 + frontmatter `name:` 박힘 + 정합 확인
- [ ] `knowledge-gc` — 정의 파일 존재 + 자동 호출 X 정합 확인

### Knowledge 5 도메인 baseline
- [ ] `.claude/knowledge/cross-cutting/_index.md` 존재 + 시드 항목 확인
- [ ] `.claude/knowledge/server/_index.md` / `shared/_index.md` / `client/_index.md` / `qa/_index.md` 동일
- [ ] `.claude/knowledge/_usage.md` 박힘 확인 (AI 자율 박제 차단 게이트 명세)

### work-pin ↔ CONTEXT 정합 게이트
- [ ] `git check-ignore CONTEXT.md CONTEXT_History.md` 통과 (본인 자산, ignored)
- [ ] work-pin "⏸️ 현재 멈춤 지점" vs CONTEXT.md 본문 정합 확인 (본 세션 시작 시 work-pin 갱신 완료)

### Phase 06 선행 게이트 — 외부 리뷰 원본 위치 사전 점검 (β4 봉합)
- [ ] `00_Document/reviews/` 디렉토리 listing — `Dawnholder-harness-review-2026-05-19.md` 또는 `harness-review-followup-{2,3,4,5}of5.md` 잔여 확인
- [ ] 잔여 4건 위치 추정 — 옛 commit `986a042` (1of5 도입 commit) blame 및 git history grep으로 시리즈 흔적 추적
- [ ] **확보 결정 박음**: (a) repo 반입 / (b) 자료 요약만 / (c) 확보 불가 — Phase 06 진입 전 결정 의무. Phase 01 시점에 결정 박으면 Phase 06 막판 입력 결손 위험 ↓

---

## ✅ 완료 조건

- [ ] 빌드 green + 자동 복사 2종 정합 (1차 객관 baseline)
- [ ] Hook 7개 시뮬 발동 표 작성 (작동 6+ / 미작동 1- 허용, 사유 박음)
- [ ] SubAgent 9개 정의 파일 존재 + frontmatter 정합 (호출 실측 X — 정의 확인까지만)
- [ ] Knowledge 5 도메인 + `_usage.md` 존재 확인
- [ ] work-pin ↔ CONTEXT 정합 게이트 통과
- [ ] Phase 02~03 진입 정합 박힘 (false negative 차단 baseline)

---

## 🧪 테스트

**자동**:
- `dotnet build Dawnholder.slnx` (Bash, 본 머신 SAC On 영향 X)
- Hook 시뮬: 본 Phase 안에서 안전한 임시 Edit 1건으로 phase-gate-validator 실측 (예: `_milestone-plan.md` 갱신 이력 한 줄)

**수동**:
- SubAgent 정의 파일 9개 Glob 확인
- Knowledge 5 도메인 Glob 확인

---

## 📚 학습 포인트

- **인프라 작동 검증 = 점검의 점검** — 점검 자체가 의미 있으려면 점검 도구가 먼저 작동해야 함. *false negative 차단 baseline*
- **Hook 시뮬 vs 실측** — Hook 정의 파일 존재 ≠ 실제 발동. 본 Phase는 일부 시뮬, 실측은 Phase 04~05 코드 변경 시 자연 발동
- **Knowledge `_usage.md` 게이트** — AI 자율 박제 차단 = 사용자 확인 의무. 본 Phase에서 게이트 명세 확인 = M3.6 안에서 새 학습 박을 때 정합 보장

---

## ⚠️ 함정 / 주의사항

- **Hook 시뮬은 *비파괴적*만** — 본 Phase에서 임시 Edit으로 시뮬 시 commit X. 시뮬 후 즉시 원복 (`git checkout`)
- **SubAgent 호출 비용** — 본 Phase는 정의 확인까지만. 실제 호출은 후속 Phase에서 자연 발동 (비용 절감)
- **빌드 baseline 비교 데이터** — M3.5 박힘 시점(3.49~3.82s) 대비 현재가 *큰 차이* 나면 (예: +50%) 환경 변화 의심 (의존성 변경 / Shared.dll 캐시 누락 등)

---

## ➡️ 다음 Phase

- Phase 02 (헌법 + ADR + policies 정합 감사) + Phase 03 (하네스 v1 실측 1주차 재조정) — **병렬 진입 가능**

---

## 📋 박제 (완료 후)

- 등급 보통 → **`-DONE.md` 박지 않음** (work-pin + commit message로 충분)
- 단 본 Phase는 *baseline 데이터* 박제 가치가 있어 `_baseline-2026-05-22.md` 별 파일에 Hook 시뮬 결과 표 박는 옵션 검토 (Phase 진행 중 사용자 GO 시)
