# New_Harness/ — 새 하네스 v1 산출물 격리 폴더

> **상태**: M3.5 진행 중 (Phase 01~06 산출물 누적)
> **목적**: 옛 운영(`CLAUDE.md` / `.claude/` / `00_Document/policies/`)을 깨뜨리지 않고 새 하네스 v1을 *별 파일*로 박는 격리 영역
> **발효 시점**: Phase 06 정합 마감 — 본 폴더 안 산출물 → 옛 영역으로 *일괄 mv* + 옛 자산 삭제/응축 = 전환 commit 1회
> **수명**: Phase 06 전환 commit 직후 본 폴더 *삭제* (격리 사명 완료)

---

## 격리 정신

옛 `CLAUDE.md` + `.claude/agents/` + `.claude/hooks/` + `.claude/commands/` + `00_Document/policies/`는 Claude Code가 **매 세션 자동 로드**합니다. 작업 중에 직접 수정하면:

- 반쪽 갱신 상태로 다음 세션 진입 → 깨진 하네스 자기 발등 찍기
- 옛 운영 sanity check 불가 (옛 vs 새 동시 비교 X)
- 롤백 비용 ↑ (어느 commit이 옛이고 어느 게 새인지 추적 어려움)

**해결**: 새 하네스 v1 디테일은 본 `New_Harness/` 폴더 안에만 박음. Claude Code 자동 로드 경로 *아님* → 옛 운영 100% 작동 유지.

---

## 폴더 구조 (Phase 진행하며 누적)

```
New_Harness/
├── README.md                       이 파일 (옛 → 새 매핑 표)
├── CLAUDE.md                       새 헌법 초안 (Phase 01)
├── policies/                       새 정책 묶음 (Phase 01)
│   ├── reporting-format.md
│   ├── pin-and-done.md
│   ├── doc-thresholds.md
│   ├── review-tiering.md
│   ├── grade-and-risk.md           (신규)
│   ├── subagent-routing.md         (신규)
│   └── knowledge-system.md         (신규, Phase 04 reference)
├── agents/                         새 SubAgent 풀 8 (Phase 02)
│   ├── server.md
│   ├── shared.md
│   ├── client.md
│   ├── qa.md
│   ├── reviewer.md
│   ├── plan-auditor.md
│   ├── unity-bridge.md
│   ├── coordinator.md
│   ├── _routing.md
│   └── _escalation.md
├── hooks/                          새 Hook 7 (Phase 03)
│   ├── dangerous-cmd-guard.sh
│   ├── tdd-guard.sh
│   ├── circuit-breaker.sh
│   ├── risk-detector.sh
│   ├── shared-discipline-guard.sh
│   ├── pin-injector.sh
│   └── phase-gate-validator.sh
├── settings.proposed.json          (Phase 03)
├── knowledge/                      새 Knowledge 시스템 (Phase 04)
│   ├── server/_index.md
│   ├── shared/_index.md
│   ├── client/_index.md
│   ├── qa/_index.md
│   ├── cross-cutting/_index.md
│   └── _usage.md
└── commands/                       새 슬래시 10개 (Phase 05)
    ├── _mapping.md
    ├── work/
    │   ├── plan.md
    │   ├── new-packet.md
    │   ├── new-monster.md
    │   └── load-test.md
    ├── session/
    │   ├── start.md
    │   ├── end.md
    │   └── log.md
    ├── setup.md
    ├── harness-review.md           (신규)
    └── cross-review.md             (신규)
```

---

## 옛 → 새 매핑 표 (Phase 진행하며 행 추가)

### Phase 01 산출물

| 옛 | 새 | 변경 | 사유 |
|---|---|---|---|
| `CLAUDE.md` (200줄) | `New_Harness/CLAUDE.md` (목표 ~150줄) | 응축 + 절 신설 | work-envelope 절 제거 / Agent Routing 6→8 / 등급+모델 분담+Knowledge 절 신설 |
| `00_Document/policies/reporting-format.md` | `New_Harness/policies/reporting-format.md` | 갱신 | work-envelope 양식 절 제거 + 5단계 보고 조건부화 |
| `00_Document/policies/pin-and-done.md` | `New_Harness/policies/pin-and-done.md` | 갱신 | work-pin 압축 양식 (목표 30~40줄) |
| `00_Document/policies/doc-thresholds.md` | `New_Harness/policies/doc-thresholds.md` | 미세 정정 | 새 등급 체계 정합 |
| `00_Document/policies/review-tiering.md` | `New_Harness/policies/review-tiering.md` | 재작성 | 새 reviewer + plan-auditor SubAgent 정합 |
| (옛 없음) | `New_Harness/policies/grade-and-risk.md` | 신설 | 정량 4등급 + 위험 Hook 자동 상향 |
| (옛 없음) | `New_Harness/policies/subagent-routing.md` | 신설 | SubAgent 풀 8 라우팅 (옛 헌법 본문에 박혔던 표 외부화) |
| (옛 없음) | `New_Harness/policies/knowledge-system.md` | 신설 | Knowledge 시스템 입출력 패턴 (Phase 04 reference) |

### Phase 02 산출물 (예정)

| 옛 `.claude/agents/` | 새 `New_Harness/agents/` | 변경 |
|---|---|---|
| `netcode.md` + `gameplay.md` + `persistence.md` + `qa-sim.md`의 server-side | `server.md` | 4→1 통합 |
| (옛 묵시적) | `shared.md` | 신설 (98_Shared/ 단독) |
| `client.md` | `client.md` | 그대로 |
| `qa-sim.md` + `content.md` 일부 | `qa.md` | 책임 명확화 |
| `reviewer.md` | `reviewer.md` | 그대로 |
| (옛 없음, Codex γ 외부 의존) | `plan-auditor.md` | 신설 (γ 흡수) |
| (옛 없음, Unity MCP 도구만) | `unity-bridge.md` | 신설 |
| (옛 없음, 메인 세션 직접 분해) | `coordinator.md` | 신설 |
| `content.md` | (삭제, server/client/qa로 흡수) | 삭제 |

### Phase 03 산출물 (예정)

| 옛 `.claude/hooks/` | 새 `New_Harness/hooks/` | 변경 |
|---|---|---|
| `check-work-envelope.sh` | (삭제) | work-envelope 죽임 |
| `check-authority.sh` | (삭제) | false positive 많음, 코드 리뷰가 잡음 |
| `validate-shared.sh` | `shared-discipline-guard.sh` | 강화 (PDL 의무 3종 자동 점검) |
| `inject-current-pin.sh` | `pin-injector.sh` | rename + 새 압축 양식 정합 |
| `validate-phase-gate.sh` | `phase-gate-validator.sh` | rename + 새 등급 frontmatter 점검 |
| (옛 없음) | `dangerous-cmd-guard.sh` | 신설 (rm -rf / git reset --hard / force push 차단) |
| (옛 없음) | `tdd-guard.sh` | 신설 (TDD 강제 영역 경고) |
| (옛 없음) | `circuit-breaker.sh` | 신설 (Worker 무한 재시도 차단) |
| (옛 없음) | `risk-detector.sh` | 신설 (trust-boundary/irreversible/unity-asset 자동 등급 상향) |

### Phase 04 산출물 (예정)

| 옛 | 새 `New_Harness/knowledge/` | 변경 |
|---|---|---|
| (옛 없음, AI 캐시 영역 X) | `server/_index.md` + `shared/_index.md` + `client/_index.md` + `qa/_index.md` + `cross-cutting/_index.md` | 신설 |
| 옛 CHANGELOG / `~/.claude/memory/` 항목들 | `cross-cutting/` 흡수 후보 | 마이그 |
| 옛 `learning-journal/` (트랙 B) | 그대로 유지 | 변경 X (트랙 B 분리) |

### Phase 05 산출물 (예정)

| 옛 `.claude/commands/` | 새 `New_Harness/commands/` | 변경 |
|---|---|---|
| `learn/*.md` (5개) | (삭제, 트랙 B Notion 이관) | 학습은 별 트랙 |
| `journal/*.md` (3개) | (삭제, 트랙 B Notion 이관) | 학습은 별 트랙 |
| `work/plan.md` | `work/plan.md` | 등급 4단계 + plan-auditor 자동 호출 정합 |
| `work/review.md` | `harness-review.md` | rename + 강화 (하네스 자체 점검 슬래시) |
| `work/audit.md` | `cross-review.md` | rename + γ 방식 정합 |
| `work/new-packet.md` + `work/new-monster.md` + `work/load-test.md` | 그대로 | 정합 갱신 |
| `session/*.md` (3개) | 그대로 | work-pin 압축 양식 정합 |
| `setup.md` | 그대로 | 팀 namespace 갱신 |

### Phase 06 산출물 (예정)

- 본 폴더 일괄 mv → 옛 영역
- 옛 자산 삭제 (옛 hook 2개, 옛 agent 1개, 옛 슬래시 8개, 옛 policies/ 일부)
- ADR-022 박음 (새 하네스 v1 결정 박제)
- CHANGELOG [H] entry 박음 (모든 팀원 영향)
- 본 `New_Harness/` 폴더 삭제 (격리 사명 완료)

---

## 발효 절차 (Phase 06 전환 commit)

```bash
# 1. 최종 검증
dotnet build Dawnholder.slnx --nologo   # green
dotnet test Dawnholder.slnx --nologo    # 170+ PASS

# 2. 옛 자산 삭제
git rm CLAUDE.md
git rm 00_Document/policies/{reporting-format,pin-and-done,doc-thresholds,review-tiering}.md
git rm -r .claude/agents/   # 옛 7개
git rm .claude/hooks/{check-work-envelope,check-authority,validate-shared,inject-current-pin,validate-phase-gate}.sh
git rm -r .claude/commands/{learn,journal}/   # 8개 + work/review.md + work/audit.md

# 3. 새 자산 mv (일괄)
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/CLAUDE.md ./
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/policies/* 00_Document/policies/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/* .claude/agents/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/hooks/* .claude/hooks/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/settings.proposed.json .claude/settings.json  # 또는 diff 적용
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/ .claude/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/* .claude/commands/

# 4. import 경로 정정 + ADR-022 박음 + CHANGELOG [H] entry

# 5. 본 폴더 삭제
rm -r 01_Phases/youngho/M3.5-harness-v1/New_Harness/

# 6. 전환 commit + push + PR + 영호 셀프 머지
git add -A
git commit -m "M3.5 새 하네스 v1 발효: 옛 → 새 일괄 전환 (ADR-022)"
```

---

## 본 폴더 안 파일 사용 주의

- **AI가 자동 로드 X**: `New_Harness/CLAUDE.md`는 옛 `CLAUDE.md`와 *충돌 안 함* — Claude Code는 옛 것만 자동 로드
- **새 SubAgent 호출 불가**: Phase 06 전환 전엔 `New_Harness/agents/` 정의가 *문서*일 뿐. 옛 agents/만 동원 가능
- **새 슬래시 호출 불가**: 마찬가지로 옛 슬래시만 작동
- **새 Hook 발동 X**: settings.json에 박혀야 작동. 본 폴더 안은 *정의*만
- **검증 = 본인 눈 통독**: Phase 02~05 진행하면서 매핑 표 reverse check + 시나리오 시뮬레이션
