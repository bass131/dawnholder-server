# Phase 05: 슬래시 정리 + 신규 2개 (`/harness-review` `/cross-review`)

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화
> **등급**: 복잡 (정량 4등급 중 3단계)
> **도메인**: `.claude/commands/` (슬래시 다이어트 + 신규)
> **담당**: 영호 단독
> **예상 소요**: 3~4h
> **산출물 위치**: `01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/`

---

## 🎯 목표

옛 16개 슬래시 (학습 5 + 일지 3 + 작업 5 + 세션 3) 중 *옛 운영에 결박*된 것들 다이어트 + 신규 2개(`/harness-review` `/cross-review`) 박음. 학습 일지 트랙은 *트랙 B 분리* 결정에 따라 옛 운영에서 *이관*.

---

## 🤔 옛 16 → 새 N개 (다이어트 + 신규)

### 옛 슬래시 목록 (`00_Document/commands-index.md` 기준)

**학습 5**:
- `/learn:concept` / `/learn:dumb-it-down` / `/learn:explain` / `/learn:recap` / `/learn:why`

**일지 3**:
- `/journal:bug` / `/journal:concept` / `/journal:phase`

**작업 5**:
- `/work:audit` / `/work:load-test` / `/work:new-monster` / `/work:new-packet` / `/work:plan` / `/work:review`

**세션 3**:
- `/session:end` / `/session:log` / `/session:start`

**기타**: `/setup` / `/update-config` / `/keybindings-help` / `/simplify` / `/fewer-permission-prompts` / `/loop` / `/schedule` / `/claude-api` / `/init` / `/review` / `/security-review` / `meetingnote`

### 새 슬래시 결정 (옵션, 본 Phase 작업에서 최종)

| 옛 | 새 | 변경 |
|---|---|---|
| /learn:* (5개) | 학습 트랙 B 별 도구 (Notion 또는 별 markdown) | *제거* — 학습은 트랙 B 분리 결정 |
| /journal:* (3개) | 학습 트랙 B로 이관 | *제거* (또는 트랙 B 슬래시로 rename) |
| /work:plan | /work:plan | 유지 (등급 4단계 정합 갱신) |
| /work:review | /harness-review (rename + 강화) | rename |
| /work:audit | /cross-review (rename + 강화) | rename — γ 방식 정합 |
| /work:new-packet | /work:new-packet | 유지 (shared-discipline-guard 정합) |
| /work:new-monster | /work:new-monster | 유지 |
| /work:load-test | /work:load-test | 유지 |
| /session:start | /session:start | 유지 (work-pin 압축 양식 정합) |
| /session:end | /session:end | 유지 (Phase 완료 권유 정합) |
| /session:log | /session:log | 유지 (Notion 박제) |
| /setup | /setup | 유지 (팀 합류 흐름) |
| 기타 외부 (`/init`, `/review`, `/security-review`, `meetingnote` 등) | 그대로 | Claude Code 내장 또는 별 출처 |

**최종 추정**: 옛 16개 → 새 10개 (학습 5 + 일지 3 = 제거, /work:review → /harness-review, /work:audit → /cross-review)

### 신규 2개

#### `/harness-review`

- 본인 하네스 자체를 점검 (CLAUDE.md / .claude/ / policies/ / ADR)
- 수동 트리거 (마일스톤 끝 또는 본인 깜빡 의심 시)
- reviewer + plan-auditor SubAgent 동원
- 점검 영역: 절대 원칙 5개 정합 / 헌법-ADR-policies 충돌 / 옛 약속 가짜화 여부 / 양식 비용 평가

#### `/cross-review`

- 옛 `/work:audit` 패턴 흡수 — 메인 세션 작업 결과를 Codex β 또는 외부 시각으로 cross-check
- 수동 트리거 (큰 PR 박기 전, M3 5/18 `pre-m3` 감사 패턴)
- reviewer SubAgent 호출 + Codex 출력 결과 import (외부 도구 의존이지만 옵션)

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (헌법에 새 슬래시 카탈로그 한 줄 박힘)
- [ ] Phase 02 완료 (SubAgent 정의 — 슬래시가 위임할 대상)
- [ ] Phase 03 완료 (Hook — 슬래시 동작 강제)
- [ ] Phase 04 완료 (Knowledge — 슬래시가 조회/박을 대상)

---

## 📝 작업 내용

### 1. 옛 → 새 슬래시 매핑 결정 최종 (`New_Harness/commands/_mapping.md`)

- [ ] 위 표 최종본 박음 — 어느 슬래시가 *제거/유지/rename/신규*인지 명확
- [ ] 제거 슬래시의 *기능 이관 처*: 학습 5 + 일지 3 = 트랙 B Notion 또는 별 도구 (학습은 본인 트랙)

### 2. 유지 슬래시 8개 정합 갱신 (`New_Harness/commands/`)

옛 슬래시 `.md`를 `New_Harness/commands/`로 복사 + 새 등급/SubAgent/Knowledge 정합:

- [ ] `work/plan.md` — 등급 4단계 명시 + plan-auditor SubAgent 자동 호출 트리거 추가
- [ ] `work/new-packet.md` — shared-discipline-guard Hook 정합
- [ ] `work/new-monster.md` — content SubAgent 흡수 결과 반영 (server/client 분담)
- [ ] `work/load-test.md` — qa SubAgent 정합
- [ ] `session/start.md` — work-pin 압축 양식 정합 + CHANGELOG 확인 절차 유지
- [ ] `session/end.md` — Phase 완료 권유 정합 + 자동 CONTEXT.md 갱신 흐름 유지 (5/16 [M] 정합)
- [ ] `session/log.md` — Notion 박제 양식 정합 (ADR-016 그대로)
- [ ] `setup.md` — 팀 합류 흐름 갱신 (영호/유현/인규 namespace 정합)

### 3. 신규 2개 박기 (`New_Harness/commands/`)

- [ ] `harness-review.md` — 자체 하네스 점검 슬래시
  - 인자: `[scope]` (헌법/SubAgent/Hook/Knowledge/슬래시/all)
  - 동원: reviewer + plan-auditor
  - 산출물: `00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md`
- [ ] `cross-review.md` — 외부 시각 cross-check
  - 인자: `[branch]` 또는 `[file-list]`
  - 동원: reviewer + (옵션) Codex β 호출 안내
  - 산출물: `00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md` + Codex 박힌 결과 (있으면)

### 4. 옛 → 새 매핑 표 갱신 (`New_Harness/README.md`)

- [ ] 옛 16 → 새 10 슬래시 매핑 행 추가
- [ ] 제거된 8개 슬래시의 *기능 이관 처* 명시 (트랙 B Notion 안내)

---

## ✅ 완료 조건

- [ ] `New_Harness/commands/` 안에 10개 슬래시 `.md` (유지 8 + 신규 2)
- [ ] `_mapping.md` 옛 → 새 매핑 최종본
- [ ] 신규 2개의 동원/산출물 명세
- [ ] 옛 운영 100% 작동 (옛 16개 슬래시 그대로)

---

## 🧪 테스트

**자동**: 옛 운영 sanity check
- 옛 슬래시 16개 그대로 호출 가능 (Claude Code 자동 로드 그대로)

**수동**:
- 새 10개 슬래시 본인 눈으로 통독 (인자/동원/산출물 명세 합리적인지)
- *가상 시나리오*: M3.5 마감 후 `/harness-review all` 호출 시 흐름 시뮬레이션
- 제거된 8개 슬래시의 *학습 트랙 B 이관*이 본인 부담 안 키우는지 검토

---

## 📚 학습 포인트

- **슬래시 다이어트의 정신**: 양식 수가 많을수록 *발견 비용 ↑* + *유지 비용 ↑*. 학습은 트랙 B 분리 결정에 따라 슬래시 8개 제거 = 1/2 감소
- **`/harness-review` 신설 이유**: 옛 운영은 헌법 검토를 *ad-hoc 메인 세션 안에서* → 일관성 X + 빠진 점 ↑. 슬래시화 = *재현 가능한 점검*
- **`/cross-review` 흡수**: 옛 `/work:audit` 패턴 + 5/18 pre-m3 감사 실측 → 슬래시화 = Rule of Three 통과
- **이관 결정의 trade-off**: 학습 5 + 일지 3 = 본인 학부생 시기에 *학습 박제*에 큰 기여. 그러나 5/20 의논 결과 = *작업 KPI 전환* → 학습은 별 트랙. 옛 가치 인정 + 새 모델 정합

---

## ⚠️ 함정 / 주의사항

- **제거 슬래시 8개 = 학습 자산 유실 X**: 옛 `00_Document/learning-journal/` 그대로 유지. 슬래시만 제거 — 본인이 직접 .md 박는 흐름은 가능 (또는 트랙 B Notion)
- **신규 슬래시 인자 명세 = 사용자 학부생 친화**: 인자 너무 많으면 사용 비용 ↑. `/harness-review all` 같은 디폴트 박는 게 정석
- **`/cross-review`의 Codex 의존**: 외부 도구 = 본인이 안 쓰는 환경에서는 무용. 옵션화 (Codex 없으면 reviewer SubAgent 단독으로도 작동)
- **`/work:audit` 옛 사용자가 즐겨 쓰던 패턴이면 rename 시 혼란**: `cross-review`로 rename + 옛 `/work:audit`은 redirect 또는 deprecate 안내 (Phase 06 전환 시점에 처리)

---

## ➡️ 다음 Phase

- **Phase 06 — 양식 다이어트 + 정합 마감 + 옛 → 새 전환 commit**
- 의존성: 본 Phase 05의 슬래시가 옛 운영과 새 운영 정합 최종 검증

---

## 📋 박제 (완료 후 -DONE.md)

- 옛 16 → 새 10 슬래시 매핑 표 최종본
- 제거 8개의 트랙 B 이관 처
- 신규 2개의 동원/산출물 명세
- 학습 키워드 후보 (슬래시 다이어트 / harness-review 슬래시화 / cross-review 흡수 etc)
