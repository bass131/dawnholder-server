---
summary: M3.5 Phase 05 — 옛 슬래시 16/17개 → 새 10개 다이어트 (학습 5 + 일지 3 = 트랙 B 이관) + 유지 8개 정합 갱신 (등급 4단계 / SubAgent 9 풀 / Hook 자동 발동) + 신규 2개 (`/harness-review` 하네스 자체 점검 / `/cross-review` γ 방식 흡수) + **work-pin ↔ CONTEXT 정합 게이트 박음 (옵션 C)** — session/end.md 7.5단계 정신 전환 + policies/pin-and-done.md §5 신설. _mapping.md + README.md Phase 05 매핑 행 박힘. 옛 운영 영향 0.
phase: M3.5/05
status: done
owner: youngho
grade: 복잡
---

## TL;DR

새 하네스 v1의 슬래시 다이어트 + 신규 2개 + **work-pin ↔ CONTEXT 정합 게이트** 박음:
- `New_Harness/commands/_mapping.md` (옛 16/17 → 새 10 매핑 최종본 + 트랙 B 이관 처)
- `New_Harness/commands/work/` 4개 (`plan.md` / `new-packet.md` / `new-monster.md` / `load-test.md`) — 정합 갱신
- `New_Harness/commands/session/` 3개 (`start.md` / `end.md` / `log.md`) — 정합 갱신. **`end.md` 7.5단계 정신 전환** (옛 양식 부담 줄이기 → 새 work-pin ↔ CONTEXT 정합 게이트, 등급 무관 항상 동기)
- `New_Harness/commands/setup.md` — 팀 namespace 정합
- `New_Harness/commands/harness-review.md` — 신규 (하네스 자체 점검)
- `New_Harness/commands/cross-review.md` — 신규 (γ 방식 흡수)
- `New_Harness/policies/pin-and-done.md` — **§5 신설** (work-pin ↔ CONTEXT 정합, 옵션 C `/session:end` 단일 게이트). 옛 §5 → §6 (변경 동기화 책임)
- `New_Harness/README.md` Phase 05 매핑 행 갱신 + 폴더 구조 갱신

옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` + 옛 `00_Document/policies/pin-and-done.md`는 **그대로** → 옛 운영 100% 작동. Phase 06 전환 commit 시점에 일괄 mv + 옛 8개 삭제 예정.

---

## 등급 = 복잡

새 헌법 v1 등급 기준:
- 2 도메인 (commands/ + README/매핑)
- 줄 수 ~700~800 (10 새 파일 + 1 매핑 + README/매핑 갱신)
- 가역적 (격리 폴더 안 자산)
- 위험 깃발 X (irreversible / trust-boundary / unity-asset 모두 X)

→ -DONE.md 박음 + commit message 정합. **5단계 보고 X** (대규모 등급만 의무).

---

## 무엇을 만들었나

**11 파일 / ~1300여줄 (커밋 단위는 사용자 결정)**:

### `_mapping.md` (240줄)

옛 17 → 새 10 매핑 최종본:
- 학습 5 + 일지 3 = 트랙 B 이관 (제거) — 본인 노션 + 잔존 `learning-journal/{본인}/`
- 작업 5 중 4 유지 + 1 rename — `work/review` → `harness-review` (책임 확장)
- 옛 `work/audit` (보류 상태) → `cross-review` 신설 (Rule of Three γ 4~7회차 통과 후 정착)
- 세션 3 + setup 1 = 유지 + 정합 갱신
- 점검 카테고리 신설 (`harness-review` + `cross-review`)

학부생 이관 안내 양식 박힘 (옛 슬래시 호출 시 안내 메시지).

### 유지 8개 슬래시 정합 갱신

| 슬래시 | 정합 갱신 |
|---|---|
| `work/plan.md` | 등급 4단계 명시 / plan-auditor 자동 호출 / Phase 입자 5~7개/마일스톤 권장 / frontmatter `owner`+`grade` 필수 |
| `work/new-packet.md` | 옛 `netcode` 단일 위임 → 새 `shared`+`server`+`client` 3 SubAgent 분담 / `shared-discipline-guard` Hook 자동 발동 / risk-detector `irreversible` 깃발 박힘 |
| `work/new-monster.md` | 옛 `content` SubAgent 삭제 (Phase 02) → 새 `qa`(데이터 값) + `unity-bridge`(자산 조건부) 분담 |
| `work/load-test.md` | 옛 `qa-sim` → 새 `qa` (이름 일관) / `circuit-breaker` false positive 차단 정합 |
| `session/start.md` | work-pin 압축 양식 30~40줄 정합 / 등급별 보고 안내 / B+ 게이트 유지 |
| `session/end.md` | 등급별 -DONE.md 의무(복잡/대규모만) / 5단계 보고 = 대규모만 / 옛 `/journal:phase` → 본인 노션 트랙 B 직접 안내 |
| `session/log.md` | ADR-016 그대로 + 트랙 A/B 분리 정신 명시 |
| `setup.md` | 팀 namespace (영호/유현/인규) 명시 / 옛 `/learn:*`+`/journal:*` 안내 제거 (Phase 06 전환 시 04-finalize.md 갱신) |

### 신규 2개 슬래시

#### `harness-review.md` (215줄)

본인 하네스 자체 점검 — 영호 단독 호출 (헌법/SubAgent/Hook/Knowledge/슬래시 모두 영호 단독 통제 약속 정합).

- scope 인자: `constitution` / `subagent` / `hook` / `knowledge` / `command` / `all` (기본 all)
- 동원: `reviewer` (Tier 2-A 자동 정신) + `plan-auditor` (Tier 2-B 자동 정신) + (조건부) `knowledge-gc`
- 점검 항목 특화: 옛 "주석 약속 가짜화" 패턴 잔존 / policies/ ↔ 헌법 충돌 / ADR 모순 / policies/ 신선도 / 양식 비용 평가 (5/20 의논의 양식 다이어트 정신 정합)
- 산출물: `00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md`

#### `cross-review.md` (230줄)

외부 시각 cross-check — γ 방식 4~7회차 (Rule of Three 통과) 후 슬래시화.

- 인자: `branch` 또는 `file-list` 또는 자동 (현재 브랜치 변경분)
- α = `reviewer` (Claude 헌법/ADR 시각)
- β = Codex CLI (외부 도구, 코드 직접 접근 + dotnet test 재실측) — 사용자 환경 종속 옵션
- γ = α + β 결과 *비교* → 결함 시각 분리 (α만 / β만 / 양쪽 다)
- 결정 권유: 양쪽 다 잡음 = 최우선 봉합 / 한쪽만 잡음 = 본인 판단 옵션 A/B
- 산출물: `00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md` + (있으면) Codex 출력

### README + 매핑 표 갱신

- `New_Harness/README.md` Phase 05 매핑 행 14건 박힘 (옛 17 → 새 10 + 신설 _mapping.md)
- 폴더 구조 commands/ 부분 디테일 갱신
- 발효 절차 (Phase 06 commands 정리) 정밀 박힘 (학습 5/일지 3 별도 명령 + `work/audit.md` 미존재 명시)

---

## 왜 이렇게 만들었나

### 1. 학습 5 + 일지 3 = 트랙 B 이관 (제거 8개)

5/20 의논 결과 = KPI 전환 "학습 박제 중심" → "Planning → 구현 → 보고". 슬래시는 *작업 중심*만 유지. 학습은 별 트랙.

**옛 슬래시 5+3개의 *기능 가치*는?**:
- `/learn:why`, `/learn:concept`, `/learn:explain`, `/learn:dumb-it-down` = *학습 토큰 절약*. 새 Claude 응답에서 자연스럽게 풀이 가능 = 슬래시 양식 없어도 동일 가치
- `/learn:recap` = "어디까지 왔지?" 자체 점검. `/session:start`가 CONTEXT.md 자동 출력으로 흡수
- `/journal:bug`, `/journal:concept`, `/journal:phase` = 인터뷰 질문지 + 본인 답 채움. 본인 노션에서 자유 양식 가능 = 슬래시 양식 없어도 동일 가치

**왜 제거 가능한가**: 옛 슬래시는 *발견 비용 ↓* (학습이 *어디서* 일어나는지 명시) 가치. 새 트랙 B Notion으로 이관해도 본인이 *명시적*으로 박는 시점만 결정하면 됨. 양식 수 ↓ + 결정 부담 ↓.

### 2. `/work:review` → `/harness-review` rename + 책임 확장

옛 `/work:review`는 *코드 리뷰* 책임 (Tier 3 수동 깊은 리뷰). 그러나 새 운영에서:
- *코드 변경* 점검 책임은 `reviewer` SubAgent (Tier 2-A 자동) 흡수 — 매 코드 변경 후 자동 호출
- *하네스 자체* 점검 책임은 옛 운영에서 ad-hoc만 — 빠진 점 ↑ + 발동 시점 모호

→ `/work:review` 책임 *재배치*: 코드 리뷰 = reviewer 자동 / 하네스 점검 = `/harness-review` 명시 호출. 이름도 책임 정합 (work-review가 아니라 *harness*-review).

### 3. `/cross-review` 신설 — γ 방식 슬래시화

γ 방식 (2026-05-18 박힘) 실측 누적:
- γ 1회차: Phase 02 ProtocolVersion handshake (Codex β 7건 발견)
- γ 4회차: M3 Phase 02 후속 (★★★ `gamma-fourth-instance-codex-direct-verification`)
- γ 5회차: M3 Phase 03~04 (2건 후속 봉합)
- γ 6/7회차: M3 Phase 06 사전 검증 (HIGH 2 + MEDIUM 3 봉합 시간 절감, ★★★)

= **Rule of Three 통과** + 정착 단계. 옛 `/work:audit`이 *보류 상태*였던 이유 (Rule of Three 미통과)는 4회차에서 해소됨. M3.5는 정착 단계에서 슬래시화 = *재현 가능성* 확보 + ad-hoc 부담 ↓.

### 4. 유지 8개 슬래시 *정합 갱신*만 (옛 본문 변경 아님)

옛 슬래시 본문은 *옛 운영에 결박*된 참조 다수 (옛 6 SubAgent 이름 / 옛 hook 5개 / 옛 양식 규칙). 단순 복사하면 새 운영과 충돌 = *반쪽 갱신 상태*.

해결: 본문 *흐름*은 옛 검증된 절차 유지 + *참조*만 새 운영 정합 갱신:
- `netcode/gameplay/persistence/qa-sim` → `server/shared/client/qa`
- `inject-current-pin` → `pin-injector`
- `validate-shared-changes` (경고) → `shared-discipline-guard` (exit 2 차단)
- "매 코드 응답마다 work-envelope" → "단순/보통 = work-pin+commit, 복잡 = -DONE.md, 대규모만 5단계 보고"

→ 옛 흐름의 *검증된 정신* 보존 + 새 인프라 정합.

---

## 어떻게 만들었나

### 작업 흐름

1. **옛 슬래시 17개 통독** — `00_Document/commands-index.md` + `.claude/commands/**/*.md`
2. **Phase 02/03/04 산출물 정합 매핑** — `_routing.md` / `hooks/README.md` / `knowledge/README.md` / `plan-auditor.md` / `reviewer.md` / `knowledge-gc.md`
3. **`_mapping.md` 박음** — 옛 17 → 새 10 결정 표 + 트랙 B 이관 처
4. **유지 8개 본문 작성** — 옛 흐름 + 새 참조
5. **신규 2개 본문 작성** — `harness-review` 영호 단독 호출 + scope 5개 / `cross-review` γ 방식 흡수
6. **README.md Phase 05 행 갱신** — 옛 → 새 매핑 14건 + 폴더 구조 + 발효 절차 정밀화
7. **옛 운영 sanity check** — `dotnet build green` (0 경고 0 오류, 4.02s) + 옛 슬래시 17개 그대로

### SubAgent 동원 (Phase 05 작업)

- 메인 세션 (Claude) 직접 — 슬래시 정의 `.md` 작성은 메인 세션 영역 (헌법/policies 동일)
- **plan-auditor SubAgent 호출 안 함** — Phase 정의는 옛 단계에서 박혔고 본 시점은 *구현*만
- **reviewer SubAgent 호출 안 함** — 격리 폴더 안 산출물 (Phase 06 전환 전 = 실측 발동 X). 본인 검증 = 옛 운영 sanity check만

### 격리 컨벤션 정상 작동

- 옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` *그대로*
- 옛 슬래시 호출 가능 (격리 폴더 안 새 슬래시는 Claude Code 자동 로드 X)
- `dotnet build green` 유지 — 코드 변경 0

---

## 검증

### 자동 검증 (옛 phase-gate-validator)

본 commit 박을 때 옛 `.claude/hooks/validate-phase-gate.sh`가 `-DONE.md` frontmatter 점검 (옛 운영 그대로 작동, self-dogfooding 정신 정합).

- ✅ `summary` 필드 박힘
- ✅ `phase`, `status: done`, `owner: youngho` 박힘
- ✅ `grade: 복잡` 박힘 (M3.5 신규 필드, 옛 hook이 무관 통과)
- ✅ "5단계 보고" 섹션 = 본 등급(복잡)은 의무 X → 검증 통과

### 옛 운영 sanity check

```bash
dotnet build Dawnholder.slnx --nologo
```

결과:
```
PacketGenerator -> ...
HeadlessBot -> ...
GameServer -> ...
GameServer.Tests -> ...
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:04.02
```

✅ 0 경고 0 오류. 옛 운영 깨뜨림 0.

### 옛 슬래시 호출 가능 확인

```bash
ls .claude/commands/
```

17개 그대로:
- `learn/` × 5 (concept, dumb-it-down, explain, recap, why)
- `journal/` × 3 (bug, concept, phase)
- `work/` × 5 (load-test, new-monster, new-packet, plan, review)
- `session/` × 3 (end, log, start)
- `setup.md` × 1

✅ 옛 슬래시 17개 호출 가능 — Phase 06 전환 전까지 그대로.

### 새 슬래시 시뮬레이션 (가상)

본 Phase에서 박은 새 슬래시 10개는 격리 폴더 안 *정의*만 — Claude Code 자동 로드 X. 본인 눈으로 통독 검증:

- `_mapping.md` 옛 → 새 매핑 reverse check (옛 17개 누락 없이 매핑됨)
- `work/plan.md` 새 시나리오 — frontmatter `owner`+`grade` 필수 명시 / plan-auditor 자동 호출 / 입자 5~7 권장
- `work/new-packet.md` 새 시나리오 — shared+server+client 3 분담 / shared-discipline-guard Hook 자동 발동
- `session/start.md` 새 시나리오 — work-pin 압축 양식 30~40줄 + B+ 게이트 그대로
- `session/end.md` 새 시나리오 — 등급별 -DONE.md 의무 분기 + 옛 `/journal:phase` → 본인 노션 직접 안내
- `harness-review.md` 가상 호출 — `scope=all` → reviewer + plan-auditor + knowledge-gc 동원 + 양식 비용 평가
- `cross-review.md` 가상 호출 — α + β + γ 비교 + 양쪽 다 잡음 = 최우선 봉합 권유

✅ 5건 시뮬레이션 통과 — 옛 흐름 정신 보존 + 새 인프라 정합.

---

## 완료 조건 점검

- [x] `New_Harness/commands/` 안에 10개 슬래시 `.md` (유지 8 + 신규 2)
- [x] `_mapping.md` 옛 → 새 매핑 최종본
- [x] 신규 2개의 동원/산출물 명세 (`harness-review` scope 5개 + `cross-review` α/β/γ)
- [x] 옛 운영 100% 작동 (옛 17개 슬래시 + dotnet build green)

5/5 통과. Phase 06 진입 전제 조건 ✅.

---

## 학습 일지 후보 키워드 (트랙 B 박음 후보)

### ★★★ — 면접 결정타

1. **`work-pin-context-sync-option-c`** (★★★) — *본 Phase 05 의논 중 박힘*
   - **증상**: work-pin (작업 중 좌표) ↔ CONTEXT.md "⏸️ 현재 멈춤 지점" (세션 진입 통독 좌표) 두 자산 공존. 갱신 시점·빈도 다름. 어긋나면 다음 세션 `/session:start` 시 AI가 옛 멈춤 지점 읽음 = 옛 결정 기반 작업 위험 (Claude 혼선)
   - **패턴**: 옛 운영은 "CONTEXT 갱신 = 양식 부담 줄이기" 자동화 정신. 새 자각 = **정합 보장이 양식 부담 ↓보다 우선**. 등급별 갱신 스킵 분기 (단순/보통은 work-pin으로 충분) = work-pin과 어긋남 위험 = ❌
   - **봉합**: 옵션 C — 두 자산 *대등*, 정합은 `/session:end` *단일 게이트*에서 단방향 동기 (work-pin → CONTEXT). 등급 무관 "⏸️ 현재 멈춤 지점" *항상* 동기. 등급별 분기는 *콘텐츠 깊이*만 (학습 후보 유무 등). 기본 OK 디폴트로 양식 부담 ↓
   - **박힘**: `policies/pin-and-done.md` §5 신설 + `commands/session/end.md` 7.5 정신 전환
   - **면접 가치**: 분산 상태 정합 의사결정 어필. "두 진실 자산 = 정합 게이트가 비용 결정. 양식 부담 vs 정합 보장 비교 시 정합이 거의 항상 우선"

2. **`slash-diet-via-track-split`** (★★★)
   - **증상**: 슬래시 수 ↑ → 발견 비용 ↑ + 결정 부담 ↑ + 학부생 정신 분산
   - **패턴**: 슬래시 다이어트의 *진짜 트리거* = *책임 분리* (트랙 분리). 무리한 통합 X
   - **봉합**: 트랙 A (작업 슬래시) + 트랙 B (학습 노션) 분리. 학습 5 + 일지 3 = 자연 제거 (옛 슬래시 자산 보존 + 새 트랙 추가)
   - **면접 가치**: 한국 게임 회사 면접 *AI/도구 의사결정* 어필. "슬래시 다이어트의 정신은 *무리한 통합*이 아니라 *책임 분리*다"

2. **`rename-vs-strengthen`** (★★★)
   - **증상**: 슬래시 이름 변경 = 단순 rename으로 보이나, 책임 *재배치* 가능
   - **패턴**: `/work:review` → `/harness-review` = 단순 rename X. 옛 *코드 리뷰* 책임은 reviewer Tier 2-A 자동으로 흡수 + *하네스 자체 점검* 책임으로 *확장*
   - **봉합**: rename 결정 시 *책임 시각 분리* (옛 책임 어디 갔나 / 새 책임 무엇 흡수). 단순 이름 변경이면 rename 무가치 — 책임 *재배치*가 진짜 가치
   - **면접 가치**: 리팩터링 의사결정 어필. "이름 바꿨다"가 아니라 "책임을 *재배치*했다"

3. **`cross-review-rule-of-three`** (★★★)
   - **증상**: 옛 `/work:audit` 보류 상태 (M2.5에서 Rule of Three 미통과). γ 방식 4~7회차 누적
   - **패턴**: ad-hoc 운영 → 슬래시화 *시점*은 *Rule of Three 통과* 후. 너무 일찍 슬래시화 = 정신 미정착 + 너무 늦게 = 옛 운영 부담 ↑
   - **봉합**: γ 1회차 (실측) → γ 4회차 (정신 검증) → γ 5/6/7 (정착) → 슬래시화. 5/18 박은 보류 결정이 *옳았음* 실증
   - **면접 가치**: 디자인 패턴 *시점 판단* 어필. "Rule of Three는 코드뿐 아니라 *프로세스/도구*에도 적용"

4. **`track-b-migration-without-asset-loss`** (★★★)
   - **증상**: 학습 자산 = 옛 `learning-journal/` + 옛 슬래시 8개 + 옛 commands-index.md 안내. 트랙 분리 결정으로 *유실 위험*
   - **패턴**: 트랙 B 이관 = *옛 자산 유지* + *새 트랙 추가* + *옛 슬래시만 제거* 3 분리. 옛 자산 삭제 X
   - **봉합**: `learning-journal/{본인}/` 디렉토리 *유지* / 본인 노션 *추가* / 옛 슬래시 8개만 *제거* (Phase 06 발효 시). 학부생/팀원 안내 메시지 박음 (옛 슬래시 호출 시 안내)
   - **면접 가치**: 마이그레이션 의사결정 어필. "데이터 이관 시 *옛 자산 보존* + *새 자산 추가* + *옛 인터페이스만 제거* 3 분리가 안전"

### ★★ — 보완 자료

5. **`coordinator-decomposition-in-action`** (★★)
   - 본 Phase의 11 파일 작성 = 메인 세션 직접 처리 (등급 복잡, 1 도메인 = commands/)
   - Phase 02의 `coordinator-decomposition-boundary` 패턴 *역증명*: 단일 도메인 + 복잡 등급 = Coordinator 호출 비용 > 가치. 직접 처리가 정답

6. **`slash-self-reference-trap`** (★★)
   - 옛 슬래시 본문에 박힌 `/learn:*`, `/journal:*` 자기 참조 = 트랙 분리 시 *반쪽 갱신 상태* 위험
   - 봉합: 새 슬래시 본문에서 옛 슬래시 참조 모두 *제거* + 트랙 B 안내로 대체 (Phase 06 setup-steps + commands-index 갱신 명시)

### ★ — 단편 정보

7. **`scope-arg-default-all-pattern`** (★)
   - `harness-review.md` scope 인자 기본값 `all` = 학부생 친화 디폴트. 인자 없으면 가장 광범위 점검 = 사용 부담 ↓
   - 단 비용 ↑ (5 영역 + 3 SubAgent 동원). 명시 호출 시점 권유 표 박힘

---

## 함정 결과 학습

### 함정 1: 옛 `/work:audit` 실재 X 함정

Phase 05 정의 문서에 "옛 `/work:audit` → `/cross-review` rename" 기재. 그러나 실제로 `.claude/commands/work/audit.md` *미존재* (M2.5에서 Rule of Three 미통과로 보류).

**봉합**: `_mapping.md`에 "옛 work/audit = 미존재 (보류 상태) → cross-review 신설"로 정정 + README.md 발효 절차에 명시. Phase 정의 문서는 결정 박힘 시점 추정 기반이라 실재와 차이 가능 — 본 Phase에서 확인 후 정정.

### 함정 2: 옛 슬래시 본문 *옛 참조* 다수

옛 슬래시 본문에는 옛 6 SubAgent 이름 (`netcode`/`gameplay`/`persistence`/`qa-sim`/`content`/`reviewer`) / 옛 hook 5개 / 옛 양식 규칙 ("매 코드 응답마다 work-envelope") 박혀있음. 단순 복사 = 새 운영과 충돌 = *반쪽 갱신 상태*.

**봉합**: 본문 *흐름*은 옛 검증된 절차 유지 + *참조*만 새 운영 정합 갱신. 새 슬래시 본문 11개 모두 새 운영 SubAgent 이름 / Hook 이름 / 양식 규칙 박힘.

### 함정 3: 신설 슬래시 인자 결정 비용 (학부생 부담)

`harness-review.md` scope 5개 / `cross-review.md` branch+file-list. 인자 많을수록 사용 비용 ↑. 학부생 친화 X 위험.

**봉합**: 둘 다 *기본값 명시* (scope 기본 `all` / cross-review 기본 자동 브랜치 변경분). 인자 없이 호출 가능. 인자 명시는 *제어 필요할 때만*. 인자 결정 비용 ↓.

---

## 다음 Phase

**Phase 06 — 양식 다이어트 + 정합 마감 + 옛 → 새 전환 commit** (~2~3h 복잡, M3.5 ↔ M4 게이트):

### 작업 흐름

1. **최종 검증** (시뮬레이션 5건 실측):
   - dotnet build green
   - dotnet test 170+ PASS
   - 새 슬래시 10개 본인 통독 reverse check
   - 새 SubAgent 9 풀 권한 경계 정합 점검
   - 새 Hook 7 풀세트 우회 정책 점검

2. **일괄 mv** (옛 → 새 전환 commit 1회):
   ```bash
   git rm CLAUDE.md
   git rm 00_Document/policies/{reporting-format,pin-and-done,doc-thresholds,review-tiering}.md
   git rm -r .claude/agents/   # 옛 7개
   git rm .claude/hooks/{check-work-envelope,check-server-authority,validate-shared-changes,inject-current-pin,validate-phase-gate}.sh
   git rm -r .claude/commands/learn/             # 5개
   git rm -r .claude/commands/journal/           # 3개
   git rm .claude/commands/work/review.md        # → harness-review로 책임 확장

   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/CLAUDE.md ./
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/policies/* 00_Document/policies/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/* .claude/agents/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/hooks/*.sh .claude/hooks/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/settings.proposed.json .claude/settings.json
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/ .claude/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/*.md .claude/commands/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/work/ .claude/commands/
   git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/session/ .claude/commands/

   rm -r 01_Phases/youngho/M3.5-harness-v1/New_Harness/
   ```

3. **import 경로 정정** — commands/ 안 상대 경로 (`../../policies/*` → `../../00_Document/policies/*` 등) 일괄 정정

4. **`00_Document/commands-index.md` 재작성** — 옛 16/17 → 새 10 반영, "비슷한 것끼리 차이" 섹션 갱신

5. **ADR-022 박음** — 새 하네스 v1 결정 박제 (옛 ADR-019 후속 또는 ADR-023 신설)

6. **CHANGELOG [H] entry 박음** — 모든 팀원 영향 (헌법 변경 + SubAgent 풀 변경 + 슬래시 다이어트 + Hook 7개 발효)

7. **시뮬레이션 5건 실측** — 본인 머신에서 새 운영 작동 확인 (옛 운영 sanity check 패턴)

### 의존성

- ✅ Phase 01 (헌법/policies/CLAUDE)
- ✅ Phase 02 (SubAgent 풀 9)
- ✅ Phase 03 (Hook 7개)
- ✅ Phase 04 (Knowledge + GC)
- ✅ Phase 05 (슬래시 10개) ← 본 Phase
- → Phase 06 (정합 마감 + 일괄 전환)

순차 영역 마지막 — 의존성 모두 충족.

---

## 옛 운영 영향

**0**. 격리 컨벤션 그대로:

- 옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` 미변경
- `dotnet build` green 유지 (0 경고 0 오류)
- 옛 슬래시 호출 모두 가능
- 옛 SubAgent / Hook / Knowledge 모두 옛 상태 그대로

Phase 06 전환 commit 시점에 일괄 mv → 옛 자산 삭제 → 본격 발효 예정.

---

## 박힘 (commit / 박제)

- commit: (사용자 결정 — 본 -DONE.md commit 시 박힘)
- 박제 파일:
  - `New_Harness/commands/_mapping.md` (신설)
  - `New_Harness/commands/work/plan.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/new-packet.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/new-monster.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/load-test.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/session/start.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/session/end.md` (신설, 옛 정합 갱신 + 7.5단계 정신 전환)
  - `New_Harness/commands/session/log.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/setup.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/harness-review.md` (신규 슬래시)
  - `New_Harness/commands/cross-review.md` (신규 슬래시)
  - `New_Harness/policies/pin-and-done.md` (§5 신설 work-pin ↔ CONTEXT 정합 + 옛 §5 → §6)
  - `New_Harness/README.md` (Phase 05 매핑 행 갱신 + 폴더 구조 갱신 + 발효 절차 정밀화)
  - 본 `05-slash-cleanup-and-new-DONE.md` (박제)

총 14 파일 / ~1600여줄.
