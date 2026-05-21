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
├── agents/                         새 SubAgent 풀 9 (Phase 02 + Phase 04 knowledge-gc 확장)
│   ├── server.md
│   ├── shared.md
│   ├── client.md
│   ├── qa.md
│   ├── reviewer.md
│   ├── plan-auditor.md
│   ├── unity-bridge.md
│   ├── coordinator.md
│   ├── knowledge-gc.md             (Phase 04 신설, Specialist 3번째)
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
├── knowledge/                      새 Knowledge 시스템 (Phase 04 ✅)
│   ├── README.md                   진입점 + 트랙 A/B 분리 + 박는 양식
│   ├── _usage.md                   SubAgent/사용자 입출력 가이드 (통독 매핑 등)
│   ├── server/_index.md            (시드 1건: lifecycle-race-broadcast-skip)
│   ├── shared/_index.md            (시드 1건: false-promise-pattern)
│   ├── client/_index.md            (시드 2건: prefab-overwrite-untracked-disaster / unity-version-hash-pinning)
│   ├── qa/_index.md                (시드 0건 — M4 진입 후 자연 누적)
│   └── cross-cutting/_index.md     (시드 4건: sac-dotnet-test-block / projectsettings-cloud-ping-pong / gamma-pre-validation-pattern / riot-vanguard-spawn-unknown)
└── commands/                       새 슬래시 10개 (Phase 05 ✅)
    ├── _mapping.md                 옛 16/17 → 새 10 매핑 최종본 + 트랙 B 이관 처
    ├── work/
    │   ├── plan.md                 등급 4단계 + plan-auditor 자동 + 입자 5~7
    │   ├── new-packet.md           shared+server+client 3 SubAgent 분담
    │   ├── new-monster.md          qa(데이터) + unity-bridge(자산) 분담
    │   └── load-test.md            qa SubAgent (옛 qa-sim rename)
    ├── session/
    │   ├── start.md                work-pin 압축 + 등급별 보고
    │   ├── end.md                  등급별 -DONE.md 의무 + 트랙 B 안내
    │   └── log.md                  ADR-016 + 트랙 A/B 분리 정신
    ├── setup.md                    팀 namespace (영호/유현/인규)
    ├── harness-review.md           (신규) 하네스 자체 점검 (헌법/SubAgent/Hook/Knowledge/슬래시) + 양식 비용 평가
    └── cross-review.md             (신규) γ 방식 흡수 (α reviewer + β Codex 옵션 + γ 비교)
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

### Phase 02 산출물 (완료 — commits `5fec7ec` + `c723195` + Phase 02 -DONE.md)

| 옛 `.claude/agents/` | 새 `New_Harness/agents/` | 변경 |
|---|---|---|
| `netcode.md` + `gameplay.md` + `persistence.md` + `qa-sim.md`의 server-side | `server.md` | 4→1 통합 (경계 코드 직접 처리, 옛 다중 위임 부담 해소) |
| (옛 묵시적, 메인 세션 처리) | `shared.md` | 신설 (98_Shared/ 단독 게이트 — 헌법 §2 보호 SubAgent) |
| `client.md` | `client.md` | 갱신 (Y2 04_ClientNet/ 통합 + unity-bridge 분리 + 새 등급 정합 + Knowledge 캐시 통독) |
| `qa-sim.md` + `content.md` 일부 (데이터 값 영역) | `qa.md` | 책임 명확화 (헤드리스 봇 + 부하 + 퍼징 + 콘텐츠 데이터 값) |
| `reviewer.md` | `reviewer.md` | 흡수 + 새 등급 정합 + Knowledge 캐시 통독 + Tier 2-A 자동 호출 트리거 명시 |
| (옛 없음, Codex γ 외부 의존) | `plan-auditor.md` | 신설 (γ 사전 검증 패턴 내부 흡수, Tier 2-B 자동) |
| (옛 없음, Unity MCP 도구만) | `unity-bridge.md` | 신설 (asset/scene/prefab 전담 + Phase 08 prefab 사고 학습 정합) |
| (옛 없음, 메인 세션 직접 분해) | `coordinator.md` | 신설 (복잡/대규모 Phase 분해 + Worker 위임 + 결과 통합, Coordinator → Worker 1단계만) |
| `content.md` | (삭제, server/qa/unity-bridge로 흡수) | 삭제 |
| (옛 없음, 헌법 본문 표 박힘) | `_routing.md` | 신설 (도메인 → SubAgent + 등급 → 처리 패턴 + 자동 호출 트리거 + 권한 경계 통합) |
| (옛 없음, 헌법 본문 + 사용자 직접 처리) | `_escalation.md` | 신설 (Sonnet 2회 → Opus → 사용자 + Reviewer 위반 재위임 + Plan-auditor 옵션 A/B + 권한 위반 + 재귀 차단 + 우회 흐름) |

### Phase 03 산출물 (완료 — commits `e2faae9` + `ef4682c` + (3/3) 본 commit)

| 옛 `.claude/hooks/` | 새 `New_Harness/hooks/` | 변경 |
|---|---|---|
| `check-work-envelope.sh` | (삭제) | work-envelope 죽임 (5/20 의논) |
| `check-server-authority.sh` | (삭제) | false positive 많음, 코드 리뷰가 잡음 (Reviewer SubAgent) |
| `validate-shared-changes.sh` | `shared-discipline-guard.sh` | 강화 — 경고만 → exit 2 차단 + PDL 의무 3종 자동 점검 (GenPackets stale / Shared.dll commit / ProtocolVersion bump) |
| `inject-current-pin.sh` | `pin-injector.sh` | rename + 정책 경로 정합 (ADR-018 → policies/pin-and-done.md) |
| `validate-phase-gate.sh` | `phase-gate-validator.sh` | rename + `grade`/`owner` frontmatter 필수 + 5단계 보고 = 대규모만 의무 + MD/HTML 이중 박음 경고 |
| (옛 없음) | `dangerous-cmd-guard.sh` | 신설 (7 파괴 패턴 차단: rm -rf / git reset --hard / force push / git clean -fd / main force push / gh pr merge --admin / git checkout --force) |
| (옛 없음) | `tdd-guard.sh` | 신설 (TDD 영역 4 경고만 + 누적 로그) |
| (옛 없음) | `circuit-breaker.sh` | 신설 (같은 도구 N회 반복 알림, Bash 제외, 등급별 임계 5/10/15/20, 윈도우 5분) |
| (옛 없음) | `risk-detector.sh` | 신설 (trust-boundary/irreversible/unity-asset 3 깃발 검출 + 누적 + stderr 알림) |
| (옛 없음) | `settings.proposed.json` | 신설 (hooks 7 매핑 + Auto Mode permissions + deny 확장 `.env.*`/`appsettings.Secrets.json`) |

### Phase 04 산출물 (완료 — commits `a42dbdc` + `c6b1402` + (3/3) 본 commit)

| 옛 | 새 `New_Harness/knowledge/` + `agents/knowledge-gc.md` | 변경 |
|---|---|---|
| (옛 없음, AI 캐시 영역 X) | `server/_index.md` + `shared/_index.md` + `client/_index.md` + `qa/_index.md` + `cross-cutting/_index.md` | 신설 (5 도메인 골격 + 시드 8건) |
| (옛 없음) | `knowledge/README.md` + `_usage.md` | 신설 (진입점 + AI/사용자 입출력 가이드) |
| (옛 없음, AI 캐시 GC 영역 X) | `agents/knowledge-gc.md` | 신설 (Specialist 3번째, 수동 트리거 — `/harness-review` 또는 `/session:end`) |
| 옛 `~/.claude/memory/{sac-dotnet-test-block, unity-version-hash-pinning, riot-vanguard-spawn-unknown}.md` | `cross-cutting/_index.md` + `client/_index.md` 시드 흡수 | 마이그 (옛 memory는 *유지* — 개인 영역, 새 캐시는 *팀 영역* git 박힘) |
| 옛 CHANGELOG [M] (5/18 SAC / 5/19 cloud ping-pong) | `cross-cutting/_index.md` 시드 흡수 | 마이그 (CHANGELOG는 *유지* — 시간순 이력) |
| 옛 학습 일지 ★★★ (`false-promise-pattern`, `gamma-pre-validation-pattern`, `prefab-overwrite-untracked-disaster`, `lifecycle-race-broadcast-skip`) | `shared/_index.md` + `cross-cutting/_index.md` + `client/_index.md` + `server/_index.md` 시드 흡수 | AI 가독성으로 *재작성* (회고체 X, 구조화 패턴) |
| 옛 `learning-journal/` (트랙 B) | 그대로 유지 | 변경 X (트랙 B 분리 정신) |

### Phase 05 산출물 (완료 — 본 commit)

| 옛 `.claude/commands/` | 새 `New_Harness/commands/` | 변경 |
|---|---|---|
| `learn/*.md` (5개: concept/dumb-it-down/explain/recap/why) | (삭제, 트랙 B Notion 이관) | 학습은 별 트랙. `_mapping.md`에 이관 처 명시 (본인 노션 + 잔존 learning-journal/) |
| `journal/*.md` (3개: bug/concept/phase) | (삭제, 트랙 B Notion 이관) | 학습은 별 트랙. 인터뷰 양식은 사용자 요청 시 Claude가 자연스럽게 던짐 |
| `work/plan.md` | `commands/work/plan.md` | 등급 4단계 명시 + plan-auditor SubAgent 자동 호출 + Phase 입자 5~7개/마일스톤 권장 + frontmatter `owner`/`grade` 필수 |
| `work/review.md` | `commands/harness-review.md` | **rename + 책임 확장** — 옛 코드 리뷰(Tier 3) → 새 하네스 자체 점검 (헌법/SubAgent/Hook/Knowledge/슬래시 + 양식 비용 평가). 옛 코드 리뷰 책임은 reviewer Tier 2-A로 흡수 |
| (옛 `work/audit.md` = 보류 상태) | `commands/cross-review.md` | **신설** — γ 방식 4~7회차 Rule of Three 통과 후 슬래시화. α(reviewer) + β(Codex 옵션) + γ 비교 + 외부 시각 cross-check + 옵션 A/B 결정 권유 |
| `work/new-packet.md` | `commands/work/new-packet.md` | 정합 갱신 — 옛 `netcode` 단일 SubAgent → 새 `shared`+`server`+`client` 분담 + `shared-discipline-guard` Hook 자동 발동 |
| `work/new-monster.md` | `commands/work/new-monster.md` | 정합 갱신 — 옛 `content` SubAgent 삭제(Phase 02) → 새 `qa`(데이터 값) + `unity-bridge`(자산 조건부) 분담 |
| `work/load-test.md` | `commands/work/load-test.md` | 정합 갱신 — 옛 `qa-sim` → 새 `qa` (이름 일관, 책임 동일) |
| `session/start.md` | `commands/session/start.md` | 정합 갱신 — work-pin 압축 양식 30~40줄 / 등급별 보고 안내 / B+ 게이트 유지 |
| `session/end.md` | `commands/session/end.md` | 정합 갱신 — 등급별 -DONE.md 의무(복잡/대규모만) + 5단계 보고 = 대규모만 / 옛 `/journal:phase` 안내 → 본인 노션 트랙 B 안내 |
| `session/log.md` | `commands/session/log.md` | 정합 갱신 — ADR-016 그대로 + 트랙 A/B 분리 정신 명시 |
| `setup.md` | `commands/setup.md` | 정합 갱신 — 팀 namespace (영호/유현/인규) 명시 + 옛 `/learn:*`/`/journal:*` 안내 제거 |
| (옛 없음) | `commands/_mapping.md` | **신설** — 옛 16/17 → 새 10 매핑 최종본 + 제거 8개 트랙 B 이관 처 명시 + 변환 트리거 절차 박힘 |

**총합**: 옛 16/17개 → 새 10개 (작업 4 + 세션 3 + 점검 2 + 셋업 1). 38% 다이어트. 자세히 → [`commands/_mapping.md`](commands/_mapping.md)

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
git rm .claude/hooks/{check-work-envelope,check-server-authority,validate-shared-changes,inject-current-pin,validate-phase-gate}.sh
git rm -r .claude/commands/learn/             # 5개 (concept/dumb-it-down/explain/recap/why)
git rm -r .claude/commands/journal/           # 3개 (bug/concept/phase)
git rm .claude/commands/work/review.md        # → harness-review.md로 책임 확장 (이동)
# 옛 work/audit.md = 미존재 (M2.5 Rule of Three 미통과로 보류 — cross-review.md로 신설)

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
