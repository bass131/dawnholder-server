---
description: 외부 시각 cross-check — 본인 작업 결과를 Codex β 또는 외부 도구로 재검증 (큰 PR 머지 전 권유)
argument-hint: [branch 또는 file-list] - 선택. 없으면 현재 브랜치 변경분 자동
---

**$ARGUMENTS** 작업 결과의 *외부 시각* cross-check 슬래시. M3.5 새 하네스 v1 신규.

---

### 이 커맨드의 역할

γ 방식 (2026-05-18 박힘, 4~7회 실측 = Rule of Three 통과) 정신 정합:

- **α** = Claude reviewer SubAgent (헌법/ADR 점검) — 자동 호출 (Tier 2-A)
- **β** = 외부 도구 (Codex CLI 등) — *코드 직접 접근* + dotnet test 재실측
- **γ** = α + β 결과 비교 → 사용자 의사결정

본 슬래시는 **β 호출 + γ 비교** 흐름을 슬래시화한 것. 옛 운영은 ad-hoc 메인 세션에서 진행 → 일관성 X + 발동 시점 모호. 새 운영 = *명시 호출* + 일관 산출물.

**분담 정신 (2026-05-23 봉합)** — 본 슬래시는 *Claude가 Codex CLI를 Bash로 직접 호출 X*. 분담:
- **Claude (α + γ 조율)** = (a) Claude reviewer SubAgent 호출 + (b) 본인 입력용 점검 자료 박음 (`00_Document/reviews/YYYY-MM-DD-claude-pre-review-{slug}.md`) + (c) 본인이 Codex 세션에 던질 prompt 박음 + (d) 본인이 가져온 Codex 결과 받아 γ 비교 + 산출물 박음
- **본인 (β 직접 호출)** = 별 세션 터미널에서 `codex review --base main` (또는 `--uncommitted`) 직접 호출 + 결과 검토 후 Claude한테 요약 또는 raw 출력 전달
- **사유**: (1) Claude Bash → Codex 호출 시 *Codex 출력이 Claude 컨텍스트 채움* = 토큰 비용 ↑ / (2) 본인이 Codex 결과 *직접 검토*하면 학부생 학습 호흡 ↑ / (3) sandbox/명령어 옵션 결함 본인 환경에서 즉시 조정 가능 / (4) Codex CLI는 본인 계정 사용 → Claude를 거쳐도 과금은 같음. 이 분담은 [`memory unity-visual-work-user-owned`](../../../Users/bass1/.claude/projects/C--Dev-ClaudeDev/memory/unity-visual-work-user-owned.md) "본인 직접 정신"의 외부 도구 호출 확장.

**언제 호출**:
- 큰 PR 머지 *전* (대규모 등급 + irreversible 깃발)
- 본인 reviewer 결과만으로 자신감 부족 시
- 옛 사고 패턴이 본 작업에 잠복할 의심 시
- 5/20 의논의 *cross-check* 정신 적용 시점

**언제 호출 X**:
- 단순/보통 등급 (위임 비용 > 가치)
- Codex 없는 환경 (옵션, 단 reviewer 단독으로도 작동)
- 헌법/ADR 자체 점검 — 그건 [`harness-review.md`](harness-review.md)

---

### 인자 처리

**`branch` 형식** (예: `feature/m3-phase05`):
- 해당 브랜치와 main 간 diff 점검 대상

**`file-list` 형식** (예: `02_Server/.../MoveHandler.cs,98_Shared/Protocol/PDL.xml`):
- 콤마 구분 파일 목록 점검 대상

**인자 없음**:
- 현재 브랜치 변경분 자동 (`git diff main...HEAD`)

---

### 작업 흐름

#### Step 1. 변경 범위 결정

```bash
# 인자 없음 케이스
git diff --name-only main...HEAD

# branch 케이스
git diff --name-only main...{branch}

# file-list 케이스
echo "{콤마 구분 파일}" | tr ',' '\n'
```

변경 파일 목록을 변수 `files`에 박음.

#### Step 2. reviewer SubAgent 호출 (α)

[`../agents/reviewer.md`](../agents/reviewer.md) 호출 — Tier 2-A 5축 점검 (이미 작업 중 자동 호출됐을 수 있으니 *재호출* 정당화):

- 본 slash가 *명시 cross-check* 호출 = reviewer는 *추가 시각* 제공
- 결과를 변수 `alpha_review` 박음

#### Step 3. Codex β 호출 자료 박음 (본인이 직접 호출)

**Claude는 Codex 직접 호출 X**. 분담 정신 (2026-05-23 봉합) — Claude는 *본인이 별 세션에서 던질 자료*만 박음:

3-A. **점검 자료 박음** = `00_Document/reviews/YYYY-MM-DD-claude-pre-review-{slug}.md` Write:

```markdown
# Pre-Review for Codex β — {YYYY-MM-DD} — {brief}

## 변경 범위
- 브랜치 / 인자: <branch 또는 files>
- 변경 파일 목록: <files>
- 등급: <단순/보통/복잡/대규모> (위험 깃발: <flag>)
- main 대비 diff 요약: <자연어 ~5줄>

## α (Claude reviewer) 결과 요약
<reviewer 출력 핵심만, 본인이 Codex 입력 시 참조 자료>

## Codex β 점검 가닥 (본인 직접 호출 시 참고)
- 헌법 §1~§5 위반 여부 (특히 trust-boundary)
- M3 응급 하드코딩 잔존 패턴
- PDL/ProtocolVersion 정합
- 옛 사고 패턴 잠복 의심 (false-promise 변종)
```

3-B. **본인 Codex 호출 명령어 박음** = 본인이 별 세션에서 복사해 던질 형식:

```bash
# 권장 (PR 머지 전 main 대비 변경분 검토)
codex review --base main

# 또는 (commit 박기 전 staged + unstaged 변경분)
codex review --uncommitted

# 또는 (특정 commit 검토)
codex review --commit <SHA>

# 자료 입력은 본인이 직접 — 위 `pre-review` MD 본인이 첨부 또는 prompt에 박음
```

3-C. **본인 응답 대기** = Claude는 본인이 Codex 결과를 가져올 때까지 대기. 본인 응답 형식:
- **(A) "Codex 결과 첨부"** = raw 출력 또는 요약 던짐 → Claude γ 비교 진행
- **(B) "β 스킵"** = Codex 환경 없음 또는 본인 시간 부족 → α 단독 진행 (산출물에 *β 미발동* 명시)
- **(C) "Codex가 봉합 박음"** = 본인이 Codex 직접 봉합 박은 경우, diff 보여주면 Claude γ 비교 + 후속 처리

#### Step 4. γ 비교 분석

α + β 결과를 *비교* (둘 다 있을 때만):

| 차원 | α (Claude reviewer) | β (Codex) | γ 비교 |
|---|---|---|---|
| 헌법 §1~§5 점검 | ✅ | ✅ | 일치/불일치 |
| 코드 직접 접근 | R only | R only | 둘 다 |
| dotnet test 실측 | ❌ | ✅ | β 우위 |
| 토큰 비용 | 메인 세션 ↑ | 외부 도구 | 분담 |
| 시각 | 헌법 우선 | 정량 검증 우선 | 상호 보완 |

**핵심 신호** (어느 한쪽에만 잡힌 결함 = 진짜 위험):
- α만 잡음 = 헌법 위반인데 동작은 함 (코드 시각)
- β만 잡음 = 동작은 깨졌는데 헌법 정합 (검증 시각)
- 양쪽 다 잡음 = 명확한 위반 (최우선 봉합)

#### Step 5. 산출물 생성

`00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md` Write:

```markdown
# Cross-Review — {YYYY-MM-DD} — {brief}

## 변경 범위
- 변경 파일: <files 요약>
- 등급: <단순/보통/복잡/대규모> (위험 깃발: <flag>)

## α — Claude reviewer 결과
[reviewer 출력 그대로]

## β — Codex 결과 (β 호출 시만)
[Codex 출력 그대로]

## γ 비교 분석
- α만 잡음: <목록>
- β만 잡음: <목록>
- 양쪽 다 잡음: <목록> (최우선)
- 양쪽 다 통과: <축 목록>

## 결정 권유
- 🔴 양쪽 다 잡음 → 즉시 봉합 (옵션 A)
- 🟡 한쪽만 잡음 → 본인 판단 (옵션 B)
- 🟢 양쪽 통과 → 안심하고 진행

## 옛 학습 정합
- (해당 시) 옛 사고 패턴 잠복 여부: <패턴 키워드>
```

#### Step 6. 사용자 보고

```
─────────────────────────────────────────
🔬 Cross-Review 완료
─────────────────────────────────────────

변경 범위: {files 요약}
등급: {grade} (위험 깃발: {flag})

α — Claude reviewer: 🔴 N개 / 🟡 N개 / 🟢 통과
β — Codex (호출 시): 🔴 N개 / 🟡 N개 / 🟢 통과
β 미발동: <"환경 X" 또는 "사용자 스킵">

γ 비교:
  - 양쪽 다 잡음 (최우선): N개
  - α만 잡음: N개
  - β만 잡음: N개

산출물: 00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md

➡️ 다음 액션:
  - 🔴 양쪽 다 잡음 0개 + α/β 단독 ≤2개 = GO (PR 머지 권장)
  - 🔴 양쪽 다 잡음 ≥1개 = 봉합 후 재실행 권장 (옵션 A)
  - 🟡 한쪽만 잡음 = 본인 결정 (옵션 B 진행 시 work-pin에 사유 박음)
```

---

### Hard rules

1. **Codex CLI 직접 호출 = 본인 분담** (2026-05-23 봉합) — Claude는 Bash로 `codex` 직접 호출 X. 본인이 별 세션 터미널에서 호출. Claude는 *자료 박음 + γ 비교*만. 사유 = 토큰 비용 ↓ + 본인 학습 호흡 ↑ + sandbox 옵션 결함 본인 환경 즉시 조정 + Codex CLI 본인 계정 종속. 옛 ad-hoc Claude 직접 호출 패턴 = 컨텍스트 채움 사고 + sandbox 차단 8회+ 누적 발본 학습 정합.
2. **외부 도구 호출 = 사용자 환경 종속** — Codex 안 깔린 환경에서 β 스킵 가능 (옵션). reviewer 단독으로도 작동
3. **읽기 전용** — Step 5 산출물 외 코드/헌법 *수정 X*. 결함 발견해도 *제안*만
4. **γ 비교는 정량** — "α/β 일치/불일치 어림짐작" X. 각 결함을 *축 번호 + 위치*로 매핑 후 비교
5. **양쪽 다 잡음 = 최우선** — 의사결정 *기본값*은 즉시 봉합. 별 마일스톤 빼두는 건 사용자 명시 결정 + work-pin 사유 박힘
6. **scope 인자 명시** — 인자 없으면 현재 브랜치 *전체* 변경분. 큰 작업이면 시간 ↑. 미리 시간 짐작

---

### 함정

- **β만 호출 = γ 효과 X** — α 없이 β만 = 외부 cross-check 정신 X. 항상 α + β 비교
- **β 신뢰 맹목** — Codex도 false positive 가능. "Codex가 말하면 무조건 맞다" X. γ 비교 의무
- **양쪽 통과 = 안심** ❌ — α/β 둘 다 *체크리스트 기반*. 체크리스트에 없는 새 패턴은 둘 다 놓침. *완벽 보장 X*
- **호출 빈도 ↑↑** — γ 비용 = ~5~10분 + 외부 도구 호출. 매 PR마다 X. 큰 PR 또는 대규모 등급만

---

### 옛 슬래시와 차이

- **옛 `/work:audit`**: M2.5에서 Rule of Three 미통과로 *보류 상태*. 옛 ad-hoc γ 방식 (5/18 pre-m3) 실측 후 정착 시 슬래시화 결정 박혀있었음
- **새 `/cross-review`**: γ 방식 4/5/6/7회차 = Rule of Three 통과 후 정착 → 슬래시화. 옛 `/work:audit` 책임 흡수 + 인자 / 산출물 / 실행자 분기 명시화

---

### 발동 시점 권유

| 시점 | 권유 |
|---|---|
| 큰 PR 머지 *전* | ✅ 권장 (irreversible 깃발 = 자동 권유) |
| 대규모 등급 -DONE.md 박은 직후 | ✅ 권장 |
| `/harness-review` 결함 발견 후 봉합 직후 | ✅ 권장 (재검증) |
| 단순/보통 등급 작업 | ❌ 비용 > 가치 |
| 헌법/ADR 자체 점검 | ❌ 그건 [`harness-review.md`](harness-review.md) |
| 매 PR | ❌ 빈도 ↑↑ — 토큰/시간 비용 누적 |

---

### γ 방식 학습 정합 (배경)

옛 운영 γ 방식 실측 (2026-05-18 박힘):
- γ 1회차: Phase 02 ProtocolVersion handshake (Codex β 7건 발견)
- γ 4회차: M3 Phase 02 후속 (Codex β 7건 = `false-promise-second-instance` ★★★)
- γ 5회차: M3 Phase 03~04 Codex β 1차 검증 (2건 후속 봉합)
- γ 6/7회차: M3 Phase 06 사전 검증 (HIGH 2 + MEDIUM 3 봉합 시간 절감 = ★★★)

본 슬래시 = γ 정착 후 슬래시화. 새 운영에서 외부 의존 ↓ (plan-auditor가 *사전 검증* 책임 흡수) + 본 슬래시가 *사후 cross-check* 책임 흡수. 옛 ad-hoc 운영 → 새 슬래시화 = *재현 가능성* 확보.
