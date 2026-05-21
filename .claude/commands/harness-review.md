---
description: 본인 하네스 자체 점검 — 헌법/SubAgent/Hook/Knowledge/슬래시 정합 + 옛 약속 가짜화 여부 + 양식 비용 평가
argument-hint: [scope] - 선택. 기본 all. 옵션: constitution | subagent | hook | knowledge | command | all
---

본인 하네스(영호 단독 통제 영역)의 *자체 점검* 슬래시. M3.5 새 하네스 v1 신규.

점검 범위: **$ARGUMENTS** (없으면 `all`)

---

### 이 커맨드의 역할

옛 운영은 헌법/하네스 검토를 *ad-hoc 메인 세션 안에서* 진행 → 일관성 X + 빠진 점 ↑. 새 운영은 슬래시화 = *재현 가능한 점검* + reviewer + plan-auditor 자동 동원.

**언제 호출**:
- 마일스톤 끝 (M3.5 Phase 06 같은 게이트 마감 직전)
- 본인 깜빡 의심 시 ("하네스 어딘가 옛 약속이 가짜화됐나?")
- 새 SubAgent / Hook / 슬래시 박은 직후 정합 점검
- 분기별 정기 점검 (옵션)

**언제 호출 X**:
- 코드 변경 점검 — 그건 reviewer (Tier 2-A 자동) 또는 [`cross-review.md`](cross-review.md) (외부 시각)
- Phase 정의 점검 — 그건 plan-auditor (Tier 2-B 자동)

---

### Scope 옵션

| Scope | 점검 대상 | 동원 |
|---|---|---|
| `constitution` | `CLAUDE.md` + `00_Document/ADR/` + `00_Document/policies/` | reviewer (5축 점검) |
| `subagent` | `.claude/agents/*.md` (풀 9 + _routing + _escalation) | reviewer + plan-auditor (역할 분담 정합) |
| `hook` | `.claude/hooks/*.sh` + `.claude/settings.json` | 본인 + reviewer (실행 우회 가능성 점검) |
| `knowledge` | `.claude/knowledge/**/*.md` | knowledge-gc (정리 후보 추출) + reviewer (트랙 A/B 분리 정신 정합) |
| `command` | `.claude/commands/**/*.md` + `00_Document/commands-index.md` | reviewer + plan-auditor (옛/새 매핑 정합) |
| `all` | 위 5개 통합 | reviewer + plan-auditor + knowledge-gc |

---

### 작업 흐름

#### Step 1. 컨텍스트 수집

scope에 따른 점검 대상 파일 목록 박음:

- `constitution` → `CLAUDE.md` + `00_Document/ADR/INDEX.md` + 모든 ADR `.md` + `00_Document/policies/*.md`
- `subagent` → `.claude/agents/*.md`
- `hook` → `.claude/hooks/*.sh` + `.claude/settings.json`
- `knowledge` → `.claude/knowledge/**/*.md`
- `command` → `.claude/commands/**/*.md` + `00_Document/commands-index.md`
- `all` → 위 모두

#### Step 2. reviewer SubAgent 호출 (Tier 2-A 자동 정신 정합)

[`../agents/reviewer.md`](../agents/reviewer.md) 호출 — *하네스 자체*가 헌법/ADR/도메인 패턴을 잘 따르는지 5축 점검:

- 축 1: 헌법 절대 원칙 5개 정합 (옛 약속이 코드/문서에 살아있나)
- 축 2: ADR 정합 (옛 ADR이 현재 운영과 일치하나)
- 축 3: 구조 정합 (`00_Document/policies/` 우선순위 표 정합)
- 축 4: 테스트 커버리지 (Hook 실측 횟수, SubAgent 실측 횟수)
- 축 5: 도메인 적합 패턴 (knowledge _index 정합)

특화 점검 항목 (헌법 자체 점검 용):

- **옛 "주석 약속 가짜화" 패턴 잔존**: 헌법/CLAUDE.md에 "박혀있어야 함" 약속이 *코드에 실재*하는지 (헌법 #2 ProtocolVersion handshake / #4 Shared Code Discipline / Handlers/ 폴더 같은 3회 봉합 시리즈 정신)
- **policies/ ↔ 헌법 충돌**: 정책 파일이 헌법 우선순위 위반하나
- **ADR 모순**: 새 ADR이 옛 ADR 뒤집은 거면 옛 ADR에 *deprecated* 표기 있나
- **policies/ 신선도**: 마지막 갱신 6개월 넘은 정책 있나 (옛 운영과 어긋남 위험)

#### Step 3. plan-auditor SubAgent 호출 (Tier 2-B 자동 정신 정합)

[`../agents/plan-auditor.md`](../agents/plan-auditor.md) 호출 — *설계 시각*에서 6축 점검:

- 축 1: SubAgent 풀 분해 적정성 (9개 적정인가, 옛 6 → 9 확장 비용 정당화)
- 축 2: 의존성 그래프 (Coordinator → Worker 재귀 차단 정합)
- 축 3: 완료 조건 정량성 (각 SubAgent 입력 약속 명확한가)
- 축 4: 등급 산정 적정성 (4등급 매핑 옛/새 일관)
- 축 5: 헌법 절대 원칙 위반 위험 (SubAgent 권한 경계 위반 없나)
- 축 6: 시나리오 명세 명확성 (Hook 발동 시나리오 명시)

#### Step 4. (조건부) knowledge-gc SubAgent 호출

scope 중 `knowledge` 또는 `all`이면 [`../agents/knowledge-gc.md`](../agents/knowledge-gc.md) 호출:

- 비활성화 후보 (3개월 무참조)
- 응축 후보 (중복 패턴)
- 결함 정정 후보 (후속 실측 false 판명)
- 승격 후보 (Rule of Three 통과 + 사용 빈도 ↑↑)

⚠️ **사용자 확인 게이트** — 자동 정리 X. 제안만 박힘.

#### Step 5. 양식 비용 평가 (M3.5 신규)

5/20 의논의 *양식 다이어트* 정신 정합 점검:

- work-pin 평균 줄 수 (목표 30~40줄)
- -DONE.md 평균 줄 수 (등급 *복잡/대규모만* 박음)
- 5단계 보고 발동 빈도 (목표: 대규모만)
- 양식 비용 vs 가치 = 발견 비용 / 학습 가치 비율

만약 *양식이 가치보다 비용 ↑* 의심되면 짚기 (옛 work-envelope 죽인 정신 정합).

#### Step 6. 산출물 생성

`00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md` Write:

```markdown
# 하네스 자체 점검 — {YYYY-MM-DD} — scope={scope}

## TL;DR
- 🔴 결함 N개 / 🟡 제안 N개 / 🟢 정합 N개

## reviewer (Tier 2-A) 결과
[reviewer 출력 그대로]

## plan-auditor (Tier 2-B) 결과
[plan-auditor 출력 그대로]

## knowledge-gc 결과 (knowledge scope만)
[knowledge-gc 출력 그대로]

## 양식 비용 평가
- work-pin 평균: <N>줄 (목표 30~40)
- -DONE.md 발동: <N>건 (등급 분포)
- 5단계 보고 발동: <N>건 (대규모 등급만 의도)

## 결정 권유 (옵션)
- 🔴 즉시 봉합 권유: ...
- 🟡 별 마일스톤 봉합: ...
- 🟢 그대로 진행: ...
```

#### Step 7. 사용자 보고

```
─────────────────────────────────────────
🔬 하네스 자체 점검 완료
─────────────────────────────────────────

scope: {scope}
산출물: 00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md

🔴 결함: N개
  - <축X> <한 줄 설명>
  ...
🟡 제안: N개
🟢 정합: N개

knowledge-gc (knowledge scope 시): 정리 후보 N개

양식 비용 평가:
  - work-pin: <평균 줄 수> / 목표
  - -DONE.md: <발동 분포>

➡️ 다음 액션:
  - 🔴 0개 = GO (하네스 정합 통과)
  - 🔴 N개 = 본인 결정 (즉시 봉합 / 별 마일스톤 / 그대로)
```

---

### Hard rules

1. **이 슬래시는 영호 단독 호출** — 옛 운영 약속 (헌법/SubAgent/Hook 모두 영호 단독 통제)
2. **읽기 전용** — Step 6 산출물 외 코드/헌법 *수정 X*. 결함 발견해도 *제안*만
3. **scope 디폴트 = all** — 인자 없으면 5개 영역 모두 점검 (시간 ~10~15분)
4. **knowledge-gc 결과 = 제안만** — 자동 정리 X. 사용자 확인 게이트 통과 후만 실제 정리
5. **양식 비용 평가 정량** — "양식이 많아 보임" 같은 모호 표현 X. 줄 수 / 발동 빈도 / 비율 명시

---

### 함정

- **scope=all이 비용 큼** — 5개 영역 동시 점검 + 3 SubAgent 동원 = ~10~15분 + Opus 2개 토큰. 잦은 호출 X. 마일스톤 끝 또는 큰 의심 시만
- **reviewer false positive** — *임의 판정* 가능성. 모호하면 🟡로 표시. 🔴는 *명확한 헌법/ADR 위반*만
- **plan-auditor가 코드 점검 못 함** — plan-auditor는 *설계* 영역. 코드 위반은 reviewer 영역
- **양식 비용 평가는 정량적** — 본인 *느낌*에 휘둘리지 마. 실측 평균 줄 수로 판단

---

### 옛 슬래시와 차이

- **옛 `/work:review`**: Phase 단위 코드 리뷰 — 헌법/ADR 점검 (Tier 3 수동)
- **새 `/harness-review`**: *하네스 자체* 점검 — 헌법/SubAgent/Hook/Knowledge/슬래시 정합 + 양식 비용 평가. *코드 변경 점검 X* (그건 reviewer Tier 2-A 자동)

옛 `/work:review`의 *코드 리뷰* 책임은 reviewer SubAgent로 흡수 (Tier 2-A 자동). 본 슬래시는 *하네스 메타 점검* 책임만.

---

### 발동 시점 권유

| 시점 | scope 권유 |
|---|---|
| 마일스톤 끝 (M3.5 Phase 06 같은 게이트) | `all` |
| 새 SubAgent 박은 직후 | `subagent` |
| 새 Hook 박은 직후 | `hook` |
| 큰 PR 머지 직후 | `all` |
| 분기별 정기 점검 | `all` |
| Knowledge 비대 의심 | `knowledge` |
| 슬래시 정리 마감 직후 | `command` |
