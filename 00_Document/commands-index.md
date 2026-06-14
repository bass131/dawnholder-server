# 슬래시 커맨드 빠른 참조

총 **11개**. 3 카테고리 폴더(`work/` `session/`) + 점검 슬래시 3개 + 단독 진입점(`setup.md`) (2026-06-12 `/refactor-sweep` 신설 — 첫 무인 리팩토링).

호출 형식: `/<카테고리>:<이름>` (예: `/work:plan`, `/session:end`) 또는 단독 진입점 (`/setup`, `/harness-review`, `/cross-review`, `/refactor-sweep`).

> **옛 학습 5 (`/learn:*`) + 일지 3 (`/journal:*`) 슬래시 = M3.5에서 제거** (5/20 의논 — KPI 전환 "학습 박제 중심 → Planning + 구현 + 보고"). 그 슬래시들이 떠받치던 학습 추적 트랙 B 자체도 **ADR-025로 은퇴** — 회고는 대화/노션 자유 양식으로 자율. 잔존 `00_Document/learning-journal/{본인}/` 디렉토리는 *각자 작업물*이라 보존 (신규 박제 안 강제).

---

## 🛠️ 작업용 (4) — 실제 코드·구조 변경

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/work:plan`](../.claude/commands/work/plan.md) | 큰 목표를 학습 가능한 Phase들(5~7개/마일스톤 권장)로 쪼개고 싶을 때. `plan-auditor` SubAgent 자동 호출 (사전 검증) | `<목표>` |
| [`/work:new-packet`](../.claude/commands/work/new-packet.md) | 새 패킷을 클라/서버 양쪽 wiring까지 한 번에 추가. `shared` + `server` SubAgent 분담 + `shared-discipline-guard` Hook 자동 발동 | `<C2S\|S2C> <name>` |
| [`/work:new-monster`](../.claude/commands/work/new-monster.md) | 새 몬스터 데이터 추가 (엔진 코드 변경 없음). `qa`(데이터 값) + `shared`(스키마) 분담 (옛 `content` SubAgent 흡수) | `<name> <level> <map>` |
| [`/work:load-test`](../.claude/commands/work/load-test.md) | 헤드리스 봇 부하 테스트 시나리오 실행 + 리포트. `qa` SubAgent (옛 `qa-sim` rename) | `<scenario> <bots> [duration]` |

---

## 📌 세션 관리 (3) — 시작·마감·박제

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/session:start`](../.claude/commands/session/start.md) | 새 세션 시작 시 첫 입력. git 게이트 (B+) 정책 + work-pin(`current-pin.txt`) 좌표 인지(현재 작업·다음 액션) + CHANGELOG 최근 변경 확인. | (인풋 없음) |
| [`/session:end`](../.claude/commands/session/end.md) | Phase 완료 마감 절차. -DONE.md 박제 후 호출 → commit + PR + `/session:log`(선택) + **work-pin 갱신** (마감 상태 반영, ADR-025 — 단일 핸드오프) + 다음 액션 결정. 등급별 마감 분기 (단순/보통 = work-pin + commit message / 복잡 이상 = work-pin + -DONE.md + HTML 시각화, ADR-031). | (인풋 없음) |
| [`/session:log`](../.claude/commands/session/log.md) | 노션 박제 트리거. 보통 `/session:end`가 자동 호출. 실행자 분기: Codex 있으면 Codex가 박음 (본인 유영호 흐름), Codex 없으면 Claude가 mcp__notion 직접 호출 (인규/유현 fallback). | (인풋 없음) |

---

## 🔍 점검 (3) — 하네스 정합 + 외부 cross-check + 무인 리팩토링

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/harness-review`](../.claude/commands/harness-review.md) | **Tier 3** 수동 깊은 리뷰 — 본인 하네스 자체 점검 (헌법 / SubAgent / Hook / Knowledge / 슬래시 정합 + 옛 약속 가짜화 여부 + 양식 비용 평가). 옛 `/work:review` rename + 책임 확장 (코드 리뷰는 Tier 2 reviewer SubAgent가 자동 흡수). `reviewer` + `plan-auditor` + (옵션) `knowledge-gc` 동원. **읽기 전용** | `[scope]` (기본 all) |
| [`/cross-review`](../.claude/commands/cross-review.md) | 외부 시각 cross-check — 본인 작업 결과를 Codex β 또는 외부 도구로 재검증 (큰 PR 머지 전, 비가역 변경 전 권유). Rule of Three 통과 후 슬래시화 (5/18 pre-m3 감사 + γ 방식 4~7회차 실측 누적). **읽기 전용** | `<scope>` (예: PR # 또는 branch) |
| [`/refactor-sweep`](../.claude/commands/refactor-sweep.md) | **자기 전 무인 자동 리팩토링** — production(server/shared/clientnet) CODE_CONVENTION/SOLID 진단 후 안전 범위(저위험 ✅ + 고위험 🔶) 자동 수정 → WSL2 회귀 게이트 통과분만 **전용 브랜치 atomic commit**. ⛔ trust-boundary(보안)·📋 03_Client(Unity 검증 불가) 제안만. **push/PR 없음**(아침 영호 GO). reviewer×N 병렬 진단 → Worker 직렬 리팩 → reviewer 재검증. *코드 수정+commit하는 유일한 슬래시* | `[--dry-run] [--max=N] [--domains=…]` |

---

## 🚀 협업 셋업 — 단독 진입점

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/setup`](../.claude/commands/setup.md) | 팀원 첫 합류 시 호출. 자기소개 → 환경 검증 → 역할별 셋업 → 자산 초기화 → 첫 작업 안내까지 차근차근. 한 번에 한 단계씩 떠먹임. | (인풋 없음) |

(내부 단계 파일은 `.claude/setup-steps/`에 박혀있으나 직접 호출 안 함. `/setup`이 흐름 제어.)

---

## 비슷한 것끼리 차이 (헷갈리기 쉬운 것)

### `/harness-review` vs Tier 2 자동 리뷰 (reviewer SubAgent) — ADR-019 + ADR-022
- **`/harness-review` (Tier 3)** — 사용자가 *명시 호출*. Phase 완료 시점 또는 큰 단위 재점검. *하네스 자체 정합* 점검까지 확장 (헌법/SubAgent/Hook/Knowledge/슬래시 + 옛 약속 가짜화 여부).
- **Tier 2 reviewer SubAgent** — 메인 세션이 *자동 호출* (도메인 Worker 코드 변경 후 트리거 조건 충족 시). 요약만 출력. 사용자 명시 조작 불필요.

→ 둘은 **상호 보완**. 코드 점검 기준은 둘 다 동일하게 [`REVIEW_CHECKLIST.md`](REVIEW_CHECKLIST.md). 하네스 정합 점검은 `/harness-review` 전용.

### `/harness-review` vs `/cross-review`
- **`/harness-review`** — *본인 머리 + 본인 자산*만으로 하네스 자체 정합 점검. Claude 단독.
- **`/cross-review`** — *외부 시각* 도입 (보통 Codex β, 옵션). 본인 사각 발견 + 큰 PR 머지 전 안전망.

### `/refactor-sweep` vs `/harness-review`·`/cross-review` (코드 수정 여부)
- **`/harness-review`·`/cross-review`** — **읽기 전용**. 발견·제안만, 코드 미수정.
- **`/refactor-sweep`** — **점검 + 코드 수정 + commit**. production을 무인 자동 리팩토링 → 전용 브랜치(`refactor/auto-YYYYMMDD`)에 atomic commit까지. 셋 중 *유일하게 코드를 바꾸는* 슬래시. 안전 = 전용 브랜치 + WSL2 회귀 게이트(통과분만 commit) + 아침 선별 revert + push/PR 금지(영호 GO). trust-boundary(보안)·03_Client(Unity 검증 불가)는 제안만.

### `/work:plan` vs `plan-auditor` SubAgent
- **`/work:plan <목표>`** — *새* Phase 묶음 생성. 사용자 명시 호출.
- **`plan-auditor` SubAgent** — `_milestone-plan.md` 또는 Phase 정의 `.md` Write 직후 *자동 호출* (사전 검증 = γ 방식 흡수, Codex 외부 의존 → 내부 자산 전환).

### `/session:start` vs `/session:end` vs `/session:log`
- **`/session:start`** — 세션 **시작**. git 안전 게이트 + work-pin 좌표 인지 + CHANGELOG 최근 변경 확인.
- **`/session:end`** — **Phase 완료** 마감 절차. commit + PR + 박제 + work-pin 갱신 + 다음 액션.
- **`/session:log`** — 노션 **박제만**. 보통 `/session:end`가 호출. 본인이 직접 호출하는 경우는 Phase 외 큰 결정 박을 때.

### `/work:plan` vs Phase 파일
- **`/work:plan <목표>`** — 새 Phase 묶음(`01_Phases/<owner>/M{N}-{slug}/`)을 생성. **만들기**.
- **Phase 파일들** — 이미 만들어진 작업 단위. **실행**.

### `/setup` vs `/session:start`
- **`/setup`** — 팀원 **첫 합류**. 환경 검증 + 자산 초기화. **단 한 번** 호출.
- **`/session:start`** — 매 세션 **시작**. 인지 확인. **매번** 호출.

---

## 제거된 8 슬래시 안내 (옛 학습 5 + 일지 3)

옛 `/learn:*` (5) + `/journal:*` (3) = M3.5 새 하네스 v1에서 *제거* (ADR-022, 5/20 의논). 그 슬래시들이 떠받치던 학습 추적 트랙 B 자체도 **ADR-025로 은퇴** (가치보다 비용 ↑ — 회고 자산이 거의 안 쌓임).

**대체**:
- 학습 풀이 → 대화 안에서 "이거 왜 이래?" / "이 코드 한 줄씩 설명해줘" / "이 ADR 왜 박혔어?" 같이 *자연어로* 물어보세요. Claude가 학부생 멘토링 톤으로 풀어줌
- 회고 박음 → 본인 노션에 *자유 양식* (학부생 회고체 + 면접 무기 누적). AI 인터뷰 형식은 그대로 가능 — 사용자가 "노션 회고 박을 거 인터뷰 도와줘" 요청 시 Claude가 질문 던지면 됨 (자율, 권유 강제 X)
- 옛 `learning-journal/{본인}/` 디렉토리는 *그대로 유지* (각자 작업물 보존, 새 박제도 본인 자유)

상세 매핑은 [`.claude/commands/_mapping.md`](../.claude/commands/_mapping.md) 참조 (트랙 B 이관 내용은 ADR-025 이후 *역사 기록*).

---

## 보통 흐름 (참고)

### 첫 합류 (단 한 번)
```
clone + Claude Code 설치 후 첫 호출
  └─ /setup                    (자기소개 → 환경 검증 → 역할별 → 자산 초기화 → 첫 작업 안내)
```

### 일상 작업 흐름
```
새 세션 시작
  └─ /session:start            (git 안전 게이트 + work-pin 인지 + CHANGELOG 최근 변경 확인)
        └─ 큰 작업이면 /work:plan <목표>   (plan-auditor 자동 호출)
              └─ Phase 작업 진행
                    └─ 막히면 Claude한테 자연어로 질문
                    └─ 코드 변경 시 Tier 2 reviewer SubAgent 자동 호출 + 새 hooks 자동 검사
                    └─ Phase 끝: 등급별 박제 (흐름 안 끊고 자동 진행, ADR-031)
                          ├─ 단순/보통 = work-pin + commit message
                          ├─ 복잡    = work-pin + -DONE.md + HTML 시각화
                          └─ 대규모   = + 마일스톤 종합 (5단계 보고 구조 = 문서 내장)
                          └─ 큰 PR 머지 전: /cross-review 권유 (옵션)
                          └─ (선택) 학습 회고: 본인 노션 자유 양식 (트랙 B 은퇴, ADR-025)
                          └─ 마감: /session:end  (commit + PR + /session:log 선택 + work-pin 갱신)
                          └─ (가끔) /harness-review : 하네스 자체 정합 재점검
```

---

## 추가 정보

- 헌법(`CLAUDE.md`)의 "사용자 컨텍스트" 섹션에 작업용/세션/점검/셋업 짧은 안내 있음
- 세션 간 작업 좌표(현재 작업·다음 액션)는 work-pin(`.claude/state/current-pin.txt`)이 단일 보유 (ADR-025, 매 턴 자동 주입)
- 새 슬래시 추가 시: (1) 알맞은 카테고리 폴더(`work/` `session/`) 또는 단독 진입점에 `.md` 생성, (2) 이 인덱스의 표에 추가, (3) 헌법(`CLAUDE.md`) 짧은 안내 갱신, (4) `.claude/CHANGELOG.md` 한 줄 박기, (5) `.claude/commands/_mapping.md` 옛 → 새 매핑 갱신 (필요 시)

---

## 갱신 이력

- **2026-05-24** — ADR-025 정합 sweep. `/session:start` CONTEXT 통독 → work-pin 인지 / `/session:end` CONTEXT 자동 갱신 → work-pin 갱신 (CONTEXT 3종 은퇴). 트랙 B "이관 안내" → "제거된 8 슬래시 안내" (트랙 B 자체 은퇴 반영).
- **2026-05-21** — M3.5 Phase 06 통째 재작성. 옛 16 카탈로그 → 새 10 카탈로그. 학습 5 + 일지 3 = 트랙 B 이관 안내 섹션 신설. 점검 카테고리 신설 (`/harness-review` + `/cross-review`). 비슷한 것끼리 차이 갱신 (SubAgent 자동 호출 vs 슬래시 수동 호출 + 하네스 정합 vs 코드 리뷰 분리 명시). ADR-022 정합.
- 2026-05-14 — 협업 셋업 후속 갱신 (옛 15 → 16, `/session:end` 신설).
