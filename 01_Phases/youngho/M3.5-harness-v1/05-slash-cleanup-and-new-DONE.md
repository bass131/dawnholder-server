---
summary: M3.5 Phase 05 — 옛 슬래시 16/17개 → 새 10개 다이어트 (학습 5 + 일지 3 = 트랙 B 이관) + 유지 8개 정합 갱신 (등급 4단계 / SubAgent 9 풀 / Hook 자동 발동) + 신규 2개 (`/harness-review` 하네스 자체 점검 / `/cross-review` γ 방식 흡수) + work-pin ↔ CONTEXT 정합 게이트 박음 (옵션 C) — session/end.md 7.5 정신 전환 + policies/pin-and-done.md §5 신설. _mapping.md + README.md Phase 05 매핑 행 박힘. 옛 운영 영향 0.
phase: M3.5/05
status: done
owner: youngho
grade: 복잡
---

## TL;DR

새 하네스 v1의 슬래시 다이어트 + 신규 2개 + work-pin ↔ CONTEXT 정합 게이트 박음:

- `New_Harness/commands/_mapping.md` 신설 — 옛 16/17 → 새 10 매핑 최종본 + 트랙 B 이관 처
- `New_Harness/commands/work/` 4개 (`plan` / `new-packet` / `new-monster` / `load-test`) — 정합 갱신
- `New_Harness/commands/session/` 3개 (`start` / `end` / `log`) — 정합 갱신. **`end.md` 7.5단계 정신 전환** (옛 양식 부담 줄이기 → 새 정합 게이트)
- `New_Harness/commands/setup.md` — 팀 namespace 정합
- `New_Harness/commands/harness-review.md` 신규 — 하네스 자체 점검 (scope 5개)
- `New_Harness/commands/cross-review.md` 신규 — γ 방식 흡수
- `New_Harness/policies/pin-and-done.md` §5 신설 — work-pin ↔ CONTEXT 정합 옵션 C. 옛 §5 → §6
- `New_Harness/README.md` Phase 05 매핑 행 14건 + 폴더 구조 + 발효 절차 갱신

옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` + 옛 `00_Document/policies/pin-and-done.md`는 **그대로** → 옛 운영 100% 작동. Phase 06 전환 commit 시점에 일괄 mv + 옛 8개 삭제 예정.

---

## 5단계 보고

> Phase 05 등급 = *복잡* — 새 헌법 v1 기준 5단계 보고 X (대규모만 의무). 그러나 옛 운영 pre-commit hook은 5단계 보고 의무 → Phase 06 전환 전엔 옛 양식 정합 박음 (옛 자산 보존 정신).

### 무엇을 만들었나

14 파일 / ~1600여줄 박음.

**11 슬래시 파일** (`New_Harness/commands/`):

- `_mapping.md` (240줄) — 옛 17 → 새 10 매핑 최종본. 학습 5 + 일지 3 트랙 B 이관 처 명시. 변환 트리거 절차
- `work/plan.md` (140줄) — 등급 4단계 + plan-auditor 자동 호출 + Phase 입자 5~7 권장 + frontmatter owner/grade 필수
- `work/new-packet.md` (115줄) — 옛 `netcode` 단일 → 새 `shared`+`server`+`client` 분담 + `shared-discipline-guard` Hook 자동
- `work/new-monster.md` (95줄) — 옛 `content` 삭제 → 새 `qa`(데이터) + `unity-bridge`(자산 조건부)
- `work/load-test.md` (80줄) — 옛 `qa-sim` → 새 `qa` 이름 일관
- `session/start.md` (165줄) — work-pin 압축 양식 30~40줄 정합 / B+ 게이트 유지 / CHANGELOG 자동 확인
- `session/end.md` (235줄) — 등급별 -DONE.md 의무 분기 + **7.5단계 정합 게이트 정신 전환**
- `session/log.md` (255줄) — ADR-016 그대로 + 트랙 A/B 분리 정신 명시
- `setup.md` (75줄) — 팀 namespace (영호/유현/인규) + 옛 `/learn:*`+`/journal:*` 안내 제거
- `harness-review.md` (215줄) 신규 — 영호 단독 호출, scope 5개, reviewer + plan-auditor + knowledge-gc 동원
- `cross-review.md` (230줄) 신규 — α(reviewer) + β(Codex 옵션) + γ 비교 + 옵션 A/B 결정 권유

**정합 게이트** (`New_Harness/policies/` + `commands/session/`):

- `policies/pin-and-done.md` §5 신설 (~50줄) — work-pin ↔ CONTEXT 정합 옵션 C. 등급 무관 "현재 멈춤 지점" 항상 동기. 학습 후보는 콘텐츠 분기. 옛 §5 → §6
- `commands/session/end.md` 7.5단계 (~40줄) — 옛 "CONTEXT 자동 갱신 = 양식 부담 줄이기" → 새 "work-pin ↔ CONTEXT 정합 게이트". 기본 OK 디폴트

**박제**:

- `New_Harness/README.md` Phase 05 매핑 행 14건 + 폴더 구조 commands/ 부분 + 발효 절차 정밀화
- `05-slash-cleanup-and-new-DONE.md` (본 파일)

### 왜 필요한가

**1. 슬래시 다이어트 (5/20 의논 결과 KPI 전환)**

옛 KPI = "학습 박제 중심" → 새 KPI = "Planning → 구현 → 보고". 슬래시 = *작업 중심*만 유지. 학습 = 트랙 B (Notion).

- 학습 5 + 일지 3 = *학습 토큰 절약 슬래시*. Claude 응답 안에서 자연스럽게 풀이 가능 = 슬래시 양식 없어도 동일 가치
- 발견 비용 ↓ + 결정 부담 ↓ — 학부생이 *언제 어느 슬래시* 결정 부담 줄어듦
- 옛 자산 *보존* (learning-journal/ 잔존, 본인 노션 누적) — 자산 유실 X

**2. `/work:review` → `/harness-review` rename + 책임 *재배치***

옛 `/work:review` = Tier 3 수동 코드 리뷰. 새 운영에서:
- 코드 변경 점검 = reviewer SubAgent (Tier 2-A 자동) 흡수
- 하네스 자체 점검 = 옛 ad-hoc만 = 발동 시점 모호 → 슬래시화 = 재현 가능성 ↑

단순 rename X. 옛 책임 어디 갔는지 + 새 책임 무엇 흡수했는지 명확.

**3. `/cross-review` 신설 — γ 방식 Rule of Three 통과 후 정착**

γ 방식 4~7회차 실측 누적 (5/18~5/20) → Rule of Three 통과 → 정착 단계 = 슬래시화 *시점*. 옛 `/work:audit` 보류 상태 해소 → ad-hoc 부담 ↓ + 재현 가능성 확보.

**4. work-pin ↔ CONTEXT 정합 게이트 (옵션 C, 본 의논 중 박힘)**

옛 운영의 사각지대: work-pin (작업 중 좌표) ↔ CONTEXT.md "⏸️ 현재 멈춤 지점" (세션 진입 통독 좌표) 두 자산 공존. 갱신 시점·빈도 다름. 어긋나면 다음 세션 `/session:start` 시 AI가 옛 멈춤 지점 읽음 = 옛 결정 기반 작업 위험 (Claude 혼선).

내가 처음 제안한 "등급별 갱신 스킵 분기" (단순/보통 = CONTEXT 갱신 X) = work-pin 정합 깨질 위험 = ❌. 본인 자각 = **정합 보장이 양식 부담 ↓보다 우선**. 옵션 C 박힘: `/session:end` 단일 게이트에서 단방향 동기 (work-pin → CONTEXT). 등급 무관 "현재 멈춤 지점" 항상 동기.

### 어떻게 만들었나

**작업 흐름**:

1. 옛 슬래시 17개 통독 (`00_Document/commands-index.md` + `.claude/commands/**/*.md`)
2. Phase 02/03/04 산출물 정합 매핑 확인 (`_routing.md` / `hooks/README.md` / `knowledge/README.md` / `plan-auditor.md` / `reviewer.md` / `knowledge-gc.md`)
3. `_mapping.md` 박음 — 옛 17 → 새 10 결정 표
4. 유지 8개 본문 작성 — 옛 흐름 + 새 참조
5. 신규 2개 본문 작성 — `harness-review` + `cross-review`
6. README.md Phase 05 행 갱신 + 폴더 구조 + 발효 절차
7. **의논 중 자각 박음** — work-pin ↔ CONTEXT 정합 게이트
8. `policies/pin-and-done.md` §5 신설 + `commands/session/end.md` 7.5단계 정신 전환
9. -DONE.md 통합 박제 + 옛 운영 sanity check

**SubAgent 동원**: 메인 세션 (Claude) 직접. Phase 05 = 슬래시 정의 `.md` 작성 = 헌법/policies와 동일 영역 = 메인 직접. plan-auditor / reviewer 호출 X (격리 폴더 안 산출물 = 실측 발동 X).

**격리 컨벤션 정상 작동**: 옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` + 옛 `00_Document/policies/pin-and-done.md` *그대로*. 옛 슬래시 호출 가능. `dotnet build green` 유지.

### 테스트 결과

```bash
$ bash .claude/hooks/validate-phase-gate.sh 01_Phases/youngho/M3.5-harness-v1/05-slash-cleanup-and-new-DONE.md
exit=0
```
✅ 옛 phase-gate-validator self-dogfooding PASS (Phase 03 마감 시점과 동일 정신, 옛 hook이 새 산출물 검증해도 통과)

```bash
$ dotnet build Dawnholder.slnx --nologo
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:01.43
```
✅ dotnet build green — 0 경고 0 오류

```bash
$ ls .claude/commands/
journal  learn  session  setup.md  work
```
✅ 옛 슬래시 17개 (learn 5 + journal 3 + work 5 + session 3 + setup 1) 그대로 — 옛 운영 100% 작동

**시뮬레이션 7건** (가상, 새 슬래시 본인 통독 reverse check):

1. `_mapping.md` 옛 → 새 매핑 reverse check — 옛 17개 누락 없이 매핑됨 ✅
2. `work/plan.md` 새 시나리오 — frontmatter `owner`+`grade` 필수 + plan-auditor 자동 호출 + 입자 5~7 권장 ✅
3. `work/new-packet.md` 새 시나리오 — shared+server+client 3 분담 + `shared-discipline-guard` Hook 자동 ✅
4. `session/start.md` 새 시나리오 — work-pin 압축 양식 30~40줄 + B+ 게이트 그대로 ✅
5. `session/end.md` 새 시나리오 — 등급별 -DONE.md 의무 분기 + 7.5 정합 게이트 (work-pin ↔ CONTEXT 단방향 동기) ✅
6. `harness-review.md` 가상 호출 (scope=all) — reviewer + plan-auditor + knowledge-gc 동원 + 양식 비용 평가 ✅
7. `cross-review.md` 가상 호출 — α + β + γ 비교 + 양쪽 다 잡음 = 최우선 봉합 권유 ✅

### 다음 스텝

**Phase 06 — 양식 다이어트 + 정합 마감 + 옛 → 새 전환 commit** (~2~3h 복잡, M3.5 ↔ M4 게이트):

1. 최종 검증 — `dotnet test` 170+ PASS / build green / 시뮬레이션 실측
2. 옛 자산 삭제 + 새 자산 일괄 mv (격리 폴더 → 옛 영역)
3. import 경로 정정 + `00_Document/commands-index.md` 재작성
4. ADR-022 박음 + CHANGELOG [H] entry
5. `New_Harness/` 폴더 삭제 (격리 사명 완료)

**(별 시점 옵션)** M3 + M3.5 학습 일지 — 누적 ★★★ 14건 (M3 누적 + M3.5 신규 4건 추가) → 본인 노션 트랙 B 박음.

---

## AC 검증 결과

Phase 05 완료 조건 4개 점검:

### 1. `New_Harness/commands/` 안에 10개 슬래시 `.md` (유지 8 + 신규 2)

```bash
$ find 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands -name "*.md" -not -name "_mapping.md"
work/plan.md / work/new-packet.md / work/new-monster.md / work/load-test.md
session/start.md / session/end.md / session/log.md
setup.md / harness-review.md / cross-review.md
```
✅ 10개 박힘 (작업 4 + 세션 3 + 점검 2 + 셋업 1)

### 2. `_mapping.md` 옛 → 새 매핑 최종본

```bash
$ ls 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/_mapping.md
```
✅ 박힘 — 옛 17 → 새 10 매핑 + 학습 5/일지 3 트랙 B 이관 처 + 변환 트리거 절차

### 3. 신규 2개의 동원/산출물 명세

- `harness-review.md`: scope 5 옵션 (constitution/subagent/hook/knowledge/command/all) + 동원 (reviewer + plan-auditor + 조건부 knowledge-gc) + 산출물 (`00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md`) ✅
- `cross-review.md`: 인자 (branch/file-list/자동) + α(reviewer) + β(Codex 옵션) + γ 비교 + 산출물 (`00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md`) ✅

### 4. 옛 운영 100% 작동

- 옛 17 슬래시 그대로 ✅
- 옛 `commands-index.md` 미변경 ✅
- 옛 `policies/pin-and-done.md` 미변경 ✅
- `dotnet build green` 0 경고 0 오류 ✅
- 옛 `phase-gate-validator.sh` 본 -DONE.md 검증 PASS (self-dogfooding ✨) ✅

**4/4 통과**. Phase 06 진입 전제 조건 충족.

---

## 결정 흐름

본 Phase 작업 중 박힌 결정 흐름:

### 결정 1: 학습 5 + 일지 3 = 트랙 B 이관 (제거)

- **상황**: 옛 16/17개 슬래시 중 학습 5 + 일지 3 = 학습 트랙 결박. 5/20 KPI 전환 결과 = 작업 중심.
- **옵션 A**: 옛 슬래시 8개 *유지* (학습 자산 보존 정신)
- **옵션 B**: 옛 슬래시 8개 *제거* + 트랙 B (Notion) 이관 (KPI 정합)
- **채택**: 옵션 B. 이유 = (1) Claude 응답 자연스럽게 학습 풀이 가능 = 슬래시 양식 없어도 동일 가치, (2) 발견 비용 ↓, (3) 옛 자산은 `learning-journal/` 잔존 = 자산 보존
- **단점**: 학부생 옛 슬래시 호출 시 안내 메시지 필요 = Phase 06 디스코드 알림

### 결정 2: `/work:review` → `/harness-review` rename + 책임 재배치

- **상황**: 옛 `/work:review` = Tier 3 수동 코드 리뷰. 새 운영에 reviewer Tier 2-A 자동 흡수.
- **옵션 A**: 단순 rename — 이름만 변경, 책임 동일
- **옵션 B**: rename + 책임 재배치 — 코드 리뷰는 reviewer 흡수, 하네스 자체 점검으로 확장
- **채택**: 옵션 B. 이유 = 단순 rename은 무가치, 책임 재배치가 진짜 가치. 옛 ad-hoc 운영 → 슬래시화 = 재현 가능성 ↑
- **단점**: 슬래시 발견 비용 ↑ (학부생이 이름만 보고 책임 추정 어려움) — `_mapping.md`에 명시

### 결정 3: `/cross-review` 신설 (Rule of Three 통과 후 정착)

- **상황**: 옛 `/work:audit` M2.5에서 Rule of Three 미통과 = 보류 상태. γ 방식 4~7회차 누적 (5/18~5/20).
- **옵션 A**: 옛 보류 그대로, ad-hoc 운영 유지
- **옵션 B**: 슬래시화 = 재현 가능성 + ad-hoc 부담 ↓
- **채택**: 옵션 B. 이유 = γ 4회차(★★★ `gamma-fourth-instance`) → 5회차 → 6/7회차(M3 Phase 06 ★★★) = Rule of Three 명확 통과. 슬래시화 시점 정합
- **단점**: Codex 외부 도구 의존 = 사용자 환경 종속 → 옵션화 (Codex 없으면 reviewer 단독)

### 결정 4 (본 의논 중 박힘): work-pin ↔ CONTEXT 정합 게이트 (옵션 C)

- **상황**: 본 Phase 진행 중 사용자 자각 = "단순/보통 마감에서도 CONTEXT 갱신 필요 — work-pin과 어긋나면 Claude 혼선"
- **옵션 A**: 등급별 갱신 스킵 분기 (내가 처음 제안) — 단순/보통 = CONTEXT 갱신 X, 복잡/대규모만 갱신
- **옵션 B**: 매 마감 자동 (현재 옛 운영 그대로)
- **옵션 C**: 두 자산 대등, `/session:end` 단일 게이트에서 단방향 동기 (work-pin → CONTEXT). 등급 무관 "현재 멈춤 지점" 항상 동기. 학습 후보는 콘텐츠 분기
- **채택**: 옵션 C. 이유 = 정합 보장 비용 > 양식 부담 ↓. work-pin과 어긋나면 다음 세션 `/session:start` 시 옛 결정 기반 작업 위험 (Claude 혼선)
- **단점**: 단순/보통 마감에서도 미리보기 + 컨펌 부담 → 봉합 = 기본 OK 디폴트 (사용자 "스킵"/"수정" 명시 시만 분기)
- **박힌 곳**: `policies/pin-and-done.md` §5 신설 + `commands/session/end.md` 7.5 정신 전환

---

## 학습 일지 후보 키워드

본 Phase에서 박힌 학습 (트랙 B 노션 박을 후보):

### ★★★ — 면접 결정타

1. **`work-pin-context-sync-option-c`** (★★★) — *본 Phase 의논 중 박힘*
   - **증상**: work-pin ↔ CONTEXT 두 좌표 자산 갱신 시점·빈도 다름. 어긋나면 다음 세션 Claude 혼선
   - **패턴**: 옛 운영은 "CONTEXT 갱신 = 양식 부담 줄이기" 자동화 정신. 새 자각 = **정합 보장이 양식 부담 ↓보다 우선**. 등급별 갱신 스킵 분기 = work-pin과 어긋남 위험 ❌
   - **봉합**: 옵션 C — 두 자산 *대등*, 정합은 `/session:end` 단일 게이트 단방향 동기. 등급 무관 "현재 멈춤 지점" 항상 동기. 등급 분기는 콘텐츠 깊이만
   - **박힘**: `policies/pin-and-done.md` §5 + `commands/session/end.md` 7.5
   - **면접 가치**: 분산 상태 정합 의사결정. "두 진실 자산 = 정합 게이트 비용 결정. 양식 부담 vs 정합 보장 시 정합 우선"

2. **`slash-diet-via-track-split`** (★★★)
   - **증상**: 슬래시 수 ↑ → 발견 비용 ↑ + 결정 부담 ↑
   - **패턴**: 슬래시 다이어트 *진짜 트리거* = *책임 분리* (트랙 분리). 무리한 통합 X
   - **봉합**: 트랙 A (작업) + 트랙 B (학습 노션) 분리. 학습 5 + 일지 3 = 자연 제거
   - **면접 가치**: AI/도구 의사결정. "다이어트 정신은 *통합*이 아니라 *책임 분리*"

3. **`rename-vs-strengthen`** (★★★)
   - **증상**: 슬래시 이름 변경 = 단순 rename으로 보임. 사실 책임 *재배치* 가능
   - **패턴**: `/work:review` → `/harness-review` = 옛 *코드 리뷰* 책임은 reviewer Tier 2-A 자동 흡수 + *하네스 자체 점검* 책임 확장
   - **봉합**: rename 결정 시 *책임 시각 분리* (옛 책임 어디 / 새 책임 무엇)
   - **면접 가치**: 리팩터링 의사결정. "이름 바꿨다"가 아니라 "책임을 재배치했다"

4. **`cross-review-rule-of-three`** (★★★)
   - **증상**: 옛 `/work:audit` 보류 상태 (Rule of Three 미통과)
   - **패턴**: ad-hoc → 슬래시화 *시점*은 Rule of Three 통과 후
   - **봉합**: γ 1회차 → γ 4회차 → 5/6/7 → 슬래시화. 5/18 보류 결정이 *옳았음* 실증
   - **면접 가치**: "Rule of Three는 코드뿐 아니라 *프로세스/도구*에도 적용"

5. **`track-b-migration-without-asset-loss`** (★★★)
   - **증상**: 학습 자산 = 옛 `learning-journal/` + 옛 슬래시 8개. 트랙 분리 시 유실 위험
   - **패턴**: *옛 자산 유지* + *새 트랙 추가* + *옛 슬래시만 제거* 3 분리
   - **봉합**: 잔존분 유지 / 노션 추가 / 슬래시 8개만 제거
   - **면접 가치**: 마이그레이션 의사결정. "데이터 이관 시 3 분리가 안전"

### ★★ — 보완 자료

6. **`coordinator-decomposition-in-action`** (★★) — 본 Phase 11 파일 = 메인 세션 직접 처리. Phase 02의 `coordinator-decomposition-boundary` 패턴 *역증명*: 단일 도메인 + 복잡 등급 = Coordinator 호출 비용 > 가치

7. **`slash-self-reference-trap`** (★★) — 옛 슬래시 본문에 박힌 `/learn:*`/`/journal:*` 자기 참조 = 트랙 분리 시 반쪽 갱신 위험. 봉합 = 새 슬래시 본문에서 옛 참조 모두 제거 + 트랙 B 안내로 대체

### ★ — 단편 정보

8. **`scope-arg-default-all-pattern`** (★) — `harness-review.md` scope 기본값 `all` = 학부생 친화 디폴트

---

## 함정 결과 학습

### 함정 1: 옛 `/work:audit` 실재 X

Phase 05 정의 문서에 "옛 `/work:audit` → `/cross-review` rename" 기재. 실제 `.claude/commands/work/audit.md` *미존재* (M2.5에서 Rule of Three 미통과로 보류).

**봉합**: `_mapping.md`에 "옛 work/audit = 미존재 (보류 상태) → cross-review 신설"로 정정 + README.md 발효 절차에 명시.

### 함정 2: 옛 슬래시 본문 *옛 참조* 다수

옛 본문에 옛 6 SubAgent 이름 / 옛 hook 5개 / 옛 양식 규칙 박혀있음. 단순 복사 = 새 운영과 충돌.

**봉합**: 본문 *흐름*은 옛 검증된 절차 유지 + *참조*만 새 운영 정합 갱신.

### 함정 3: 등급별 CONTEXT 갱신 스킵 분기 함정 (의논 중 발견)

내가 처음 제안한 "단순/보통 = CONTEXT 갱신 X" = work-pin과 어긋남 위험. 사용자 자각으로 옵션 C 박힘.

**봉합**: `policies/pin-and-done.md` §5 + `commands/session/end.md` 7.5 박음. 등급 무관 "현재 멈춤 지점" 항상 동기.

### 함정 4: 옛 양식 vs 새 양식 commit 게이트

본 Phase 마감 시 옛 pre-commit hook이 새 양식 -DONE.md (5단계 보고 X) 차단. Phase 06 전환 전엔 옛 양식 정합 필수.

**봉합**: 본 -DONE.md를 옛 양식대로 재구성 (5단계 보고 + AC 검증 결과 + 결정 흐름 박힘). Phase 06 발효 후 새 양식 발동.

---

## Phase 06 전환 시 추가 작업

본 Phase 산출물이 Phase 06에서 옛 영역으로 이동될 때 처리:

1. **옛 8개 슬래시 삭제**:
   ```bash
   git rm -r .claude/commands/learn/             # 5개
   git rm -r .claude/commands/journal/           # 3개
   git rm .claude/commands/work/review.md        # → harness-review로 책임 확장
   ```

2. **새 10개 슬래시 mv**:
   ```bash
   git mv New_Harness/commands/_mapping.md .claude/commands/
   git mv New_Harness/commands/work/*.md .claude/commands/work/
   git mv New_Harness/commands/session/*.md .claude/commands/session/
   git mv New_Harness/commands/setup.md .claude/commands/
   git mv New_Harness/commands/harness-review.md .claude/commands/
   git mv New_Harness/commands/cross-review.md .claude/commands/
   ```

3. **import 경로 정정** — 새 commands/ 안 상대 경로 (`../../policies/*` → `../../00_Document/policies/*` 등)

4. **`00_Document/commands-index.md` 재작성** — 옛 16/17 → 새 10. "비슷한 것끼리 차이" 섹션 갱신

5. **CHANGELOG [H] entry 박음** — 모든 팀원 슬래시 동작 변경

6. **`policies/pin-and-done.md` §5 발효** — 옛 자산 (`00_Document/policies/pin-and-done.md`) 삭제 + 새 자산 mv

---

## 옛 운영 영향

**0**. 격리 컨벤션 그대로:

- 옛 `.claude/commands/` 17개 + `00_Document/commands-index.md` 미변경
- 옛 `00_Document/policies/pin-and-done.md` 미변경
- `dotnet build` green 유지
- 옛 슬래시 호출 모두 가능
- 옛 SubAgent / Hook / Knowledge 모두 옛 상태 그대로

Phase 06 전환 commit 시점에 일괄 mv → 옛 자산 삭제 → 본격 발효.

---

## 참조

- commit: (본 -DONE.md commit 시점에 박힘)
- 박제 파일 14개:
  - `New_Harness/commands/_mapping.md` (신설)
  - `New_Harness/commands/work/plan.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/new-packet.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/new-monster.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/work/load-test.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/session/start.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/session/end.md` (신설, 옛 정합 갱신 + 7.5 정신 전환)
  - `New_Harness/commands/session/log.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/setup.md` (신설, 옛 정합 갱신)
  - `New_Harness/commands/harness-review.md` (신규 슬래시)
  - `New_Harness/commands/cross-review.md` (신규 슬래시)
  - `New_Harness/policies/pin-and-done.md` (§5 신설 정합 게이트)
  - `New_Harness/README.md` (Phase 05 매핑 + 폴더 구조 + 발효 절차)
  - 본 `05-slash-cleanup-and-new-DONE.md` (박제)

총 14 파일 / ~1600여줄.
