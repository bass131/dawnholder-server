# Hook 인프라 (M3.5 새 하네스 v1)

> **상태**: **active** — M3.5 Phase 06 atomic 전환 (PR #42 main `fc983ea`, 2026-05-21) 후 `.claude/settings.json` PreToolUse/PostToolUse/UserPromptSubmit 등록 완료. 매 도구 호출마다 발동.
>
> **입력 인터페이스**: Claude Code Hook payload = **stdin JSON** (공식 명세). 공통 헬퍼 `hook-common.sh`가 파싱 → `$TOOL_NAME` / `$TOOL_INPUT_FILE` / `$TOOL_INPUT_COMMAND` 세팅. 옛 추측 env vars(`$CLAUDE_TOOL_INPUT_*`)는 fallback만 유지.
>
> **봉합 이력**: 2026-05-22 M3.5 후속 봉합 — Phase 03 박힘 시점 추측 명세 (`CLAUDE_TOOL_INPUT_*` env vars)가 hook 무력화 (β cross-review #1 발견 + 본 세션 `gh pr merge --admin` 차단 누락 현장 실증) → `hook-common.sh` 신설 + 6 hook 본문 정정.

---

## Hook 7개 풀세트

| # | Hook | 단계 | 매처 | 동작 | 진입 |
|---|------|------|------|------|------|
| 1 | `dangerous-cmd-guard.sh` | PreToolUse | Bash | rm -rf / git reset --hard / force push 등 7 패턴 차단 (exit 2) | ✅ (2/3) |
| 2 | `tdd-guard.sh` | PreToolUse | Edit/Write | TDD 영역 4 (Handlers / GameSession / Protocol/Packets / GameData) 변경 시 테스트 부재 *경고만* (exit 0) + 누적 로그 | ✅ (2/3) |
| 3 | `circuit-breaker.sh` | PostToolUse | (all) | 같은 도구 N회 반복 시 *알림* (Bash 제외, 등급별 임계 5/10/15/20, 윈도우 5분) | ✅ (2/3) |
| 4 | `risk-detector.sh` | PreToolUse | Bash/Edit/Write | trust-boundary / irreversible / unity-asset 자동 검출 → stderr 알림 + .claude/state/risk-flags.txt 누적 | ✅ (2/3) |
| 5 | `shared-discipline-guard.sh` | PreToolUse | Edit/Write | PDL.xml 변경 시 GenPackets stale 검사 + Shared.dll commit 동반 검사 (exit 2) + ProtocolVersion bump 경고 | ✅ (2/3) |
| 6 | `pin-injector.sh` | UserPromptSubmit | (all) | 매 사용자 입력 직전 work-pin + 미commit -DONE.md 경고 주입 | ✅ (1/3) |
| 7 | `phase-gate-validator.sh` | PostToolUse | Edit/Write | -DONE.md 박제 frontmatter (summary/phase/status/grade/owner) + 등급별 의무 섹션 점검 (exit 2) | ✅ (1/3) |

---

## 옛 5 → 새 7 매핑

| 옛 Hook | 새 Hook | 변경 |
|---|---|---|
| `inject-current-pin.sh` | `pin-injector.sh` | 이름 정정 + 정책 경로 정합 (`ADR-018` → `policies/pin-and-done.md`). 본문 로직 동일 |
| `validate-phase-gate.sh` | `phase-gate-validator.sh` | 이름 정정 + `grade`/`owner` frontmatter 필드 추가 + 5단계 보고 섹션 = *대규모 등급만* 의무 (단순/보통은 -DONE.md 자체 X) |
| `validate-shared-changes.sh` | `shared-discipline-guard.sh` | 강화 — *경고만* → exit 2 차단. PDL 의무 3종(PacketGenerator 재생성 + Shared.dll commit + Protocol.Version bump 자동 점검) |
| `check-server-authority.sh` | (삭제) | false positive 많음. 서버 권위 점검은 코드 리뷰가 더 정확 (Reviewer SubAgent) |
| `check-work-envelope.sh` | (삭제) | 5/20 의논 결과 — work-envelope 양식 자체 죽임. 5단계 보고는 *대규모만* 박음 |
| (옛 없음) | `dangerous-cmd-guard.sh` | 신설 — settings.json deny 룰 보강. PreToolUse는 우회 불가 |
| (옛 없음) | `tdd-guard.sh` | 신설 — TDD 영역 점검. 경고만 (학부생 학습 호흡) |
| (옛 없음) | `circuit-breaker.sh` | 신설 — 무한 재시도 차단 (토큰/시간 보호) |
| (옛 없음) | `risk-detector.sh` | 신설 — 위험 깃발 3종 자동 검출 + 등급 상향 |

---

## 우회·차단 정책

| Hook | 우회 가능? | 사유 |
|------|------------|------|
| `dangerous-cmd-guard` | ❌ PreToolUse 차단 | 파괴 명령 보호 |
| `shared-discipline-guard` | ❌ PreToolUse 차단 | "주석 약속은 가짜다" 3회 봉합 사고 원인 — 강제 |
| `phase-gate-validator` | ❌ PostToolUse 차단 | -DONE.md 형식 강제 (`.githooks/pre-commit`이 commit 시점 이중 안전망) |
| `risk-detector` | ⚠️ work-pin 갱신만, 차단 X | 등급 상향 통보 — 본인이 인지하면 진행 가능 |
| `tdd-guard` | ⚠️ 경고만 | 학부생 학습 호흡 (테스트 강제 부담 ↓) |
| `circuit-breaker` | ⚠️ 알림만 | false positive 위험 (정당한 반복) — 사용자 판단 |
| `pin-injector` | ⚠️ 출력만 | UserPromptSubmit, 차단 X |

---

## 설정 매핑

본 폴더의 Hook 7개는 `New_Harness/settings.proposed.json`의 `hooks` 절에서 등록됨 (Phase 06 전환 후 `.claude/settings.json`으로 일괄 이동).

상세 매핑·우회 정책 변경 책임 → 본 README + `settings.proposed.json` 동시 갱신 의무.

---

## 신선도 주의

본 Hook 풀세트는 *M3.5 박힘 시점(2026-05-20) 실측 0건*. M4 진입 후 첫 1주 안에 다음 관찰 → 명세 갱신:

- [ ] **`dangerous-cmd-guard` false positive** — 정당한 `rm` (build artifact 정리) 차단 빈도
- [ ] **`risk-detector` 등급 상향 마찰** — 본인이 단순 작업이라 인식했는데 자동 상향 시 불만 빈도
- [ ] **`circuit-breaker` false positive** — 정당한 반복 (테스트 fuzz, batch 처리) 알림 빈도
- [ ] **`tdd-guard` 학습 호흡 영향** — 경고가 학부생 작업 끊는지 vs 도움 되는지
- [ ] **`shared-discipline-guard` PDL 의무 3종 누락 검출 정확성** — false negative(빠뜨림) 빈도

재조정 결과는 본 README + 해당 `.sh` 본문 직접 수정.
