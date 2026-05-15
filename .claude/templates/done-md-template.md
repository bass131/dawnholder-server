# -DONE.md 템플릿

> Phase 완료 시 AI가 작성하는 사실 박제 페어 ([`00_Document/policies/pin-and-done.md`](../../00_Document/policies/pin-and-done.md) 2번 절 참조).
>
> 5단계 보고를 출력한 직후 같은 응답 안에서 AI가 작성·commit.
> 작성 위치: `01_Phases/M{N}-{slug}/{NN}-{phase-name}-DONE.md`

---

## 템플릿 본문 (이 아래를 그대로 가져다 채움)

> **⚠️ Post-flight 게이트 적용 중**. 아래 frontmatter 필드 + 5개 H2 섹션 + 5단계 보고 5개 항목이 모두 채워져야 훅(`validate-phase-gate.sh`)을 통과해 commit 가능.

```markdown
---
summary: <1줄. 다음 Phase가 인용할 표준 입력. "무엇을 했고 무엇이 가능해졌는지" 압축>
phase: {NN}-{phase-name}
work-id: phase{NN}-{slug}   # ADR-018 합류 지점 — 봉투·핀·학습 일지와 동일 ID. grep으로 산출물 회수.
status: done
completed_at: {YYYY-MM-DD}
commit: {short hash}
---

# Phase {NN} — {제목} 완료 박제

**소요 시간**: {대략}

## TL;DR
(2~4문장. 무엇을 / 왜 / 결과를 압축. Codex가 Notion 첫 단락으로 그대로 가져갈 수 있게 *사실*만. 본인 회고/면접 답변 X.)

## 5단계 보고
(아래 5개 항목 라벨을 그대로 유지하며 채움. 훅이 라벨 존재를 검사함.)

- **무엇을 만들었나** —
- **왜 필요한가** —
- **어떻게 만들었나** —
- **테스트 결과** —
- **다음 스텝** —

## AC 검증 결과
(Phase 파일의 완료조건(Acceptance Criteria)을 **실제로 실행한** 명령어와 결과를 박는다. 추측·요약 X. 실패하면 이 Phase는 아직 done이 아님.)

예시:
```bash
$ dotnet test 02_Server/Tests/
  Passed: 12, Failed: 0
$ ./scripts/headless-bot --connect 1 --duration 30s
  Handshake OK, 30s 동안 disconnect 0회
```

## 결정 흐름 (학습 일지 쓸 때 참고용)
- 갈래/대안 → 채택안 → 이유 (한두 줄씩)

## 막혔던 지점 (있다면)
- 증상 → 원인 → 해결 (각 한두 줄)

## 학습 일지 후보 키워드
- /journal:concept 로 펼칠 만한 키워드들
```

---

## 작성 원칙

- **사실 박제**, 본인 회고 X. 회고는 `learning-journal/`에서 본인이 쓰는 영역.
- **잊히기 전에**. 5단계 보고 직후 같은 응답에서 작성.
- **간결하게**. 학습 일지의 *베이스*이지 학습 일지 자체가 아님.
- **검색 가능하게**. "학습 일지 후보 키워드"는 미래의 본인이 `/journal:concept`로 펼칠 단서.
- **Notion 분업 정합**. 아래 "Notion 협업 분업 원칙" 섹션 참조.

---

## Notion 협업 분업 원칙

> 2026-05-11 정합. ClaudeDev 원본(`-DONE.md`/`CONTEXT*`/`ADR.md`)과 Notion "Dawnholder 협업 히스토리" DB 사이의 역할 분담.

### 역할

- **Claude**: `-DONE.md` / `CONTEXT*` / `ADR.md`에 **사실·결정·트레이드오프·테스트·막힘**을 정확히 박제.
- **Codex**: 그 원본을 **Notion용 회고·면접 자료로 재편집**. 문장 다듬기, 면접 답변 박스, 시각 구조화 담당.
- **본인**: `learning-journal/`에서 회고·교훈·면접 답변 작성. AI는 인터뷰만.

### Notion 출력 형식 — STAR (메인) + 8단 (사고 체크리스트)

**최종 출력 = STAR 4섹션 + 배운 것 3가지** — 기존 "Dawnholder 협업 히스토리" DB에 정착된 패턴. 분량 30~50줄. 자세한 STAR 명세·DB 스키마·페이지 생성 API는 [`.claude/commands/session:log.md`](../commands/session:log.md) 참조.

**8단 = STAR 작성 시 빠뜨리면 안 될 사고 체크리스트** — Codex가 Phase 07 Before/After HTML에서 도출, 사용자가 "사람이 읽기 더 편하다" 판단한 항목 셋. STAR을 *형식*으로, 8단을 *항목 빠짐 체크*로 활용.

**8단 → STAR 매핑**:

| 8단 항목          | STAR 어디로            |
|------------------|------------------------|
| TL;DR            | 제목 + 상황 첫 문장    |
| 배경/용어 맥락    | 상황                   |
| 핵심 결정        | 정해야 했던 것         |
| 구현 변경        | 한 행동                |
| 검증 결과        | 결과 (앞부분 bullet)   |
| 막힌 지점        | 한 행동 (녹임)         |
| 학습/면접 포인트  | 결과 → 배운 것 3가지   |
| 다음 액션        | italics 한 줄 (말미)   |

Codex는 STAR 4섹션 박을 때 위 8 항목이 다 들어갔나 점검 후 박음. `-DONE.md` 본문도 동일 매핑을 사실 베이스로 제공 (TL;DR / 5단계 보고 / 결정 흐름 / 막혔던 지점 / 학습 키워드).

### 용어 처리

내부 용어(`Y2`, `PDL`, `ADR`, `M1 Foundation`, `AI sliding` 등)는 처음 보는 사람도 이해할 수 있게 **첫 등장 시 한 번 풀어쓴다**. Codex가 Notion 페이지 만들 때 이 풀이를 추가. Claude는 `-DONE.md` 본문에선 풀이 없어도 OK (개발자 본인 + Codex가 베이스로 보는 문서).

### 원칙

- **사실 박제가 1순위**. Claude는 Notion용 문장을 과하게 다듬지 않아도 된다.
- **면접 답변/회고는 Codex/본인 영역**. Claude는 침범 X.
- **막힘·실패·AI sliding 같은 흔들림도 보존**. Codex가 의사결정 품질 사건으로 살림.

### 핸드오프 절차 (Claude → Codex)

**Claude의 종료 지점 (Phase 완료 ritual)**: `-DONE.md` 박제 + git commit/push까지. **Claude는 Notion 페이지를 직접 생성하지 않는다.** (이전 `/session:log`의 Claude 직접 박기 흐름은 deprecated.)

**Notion 박기 트리거**: 사용자가 명시 요청 (예: "노션 박아줘", `/session:log` 등). Phase 완료마다 자동 X — 사용자가 박을 가치 있다고 판단할 때만.

**Codex 호출 방식**: **Claude가 Bash 도구로 Codex CLI 호출** (사용자가 호출하는 게 아님). Codex 세션은 ClaudeDev에 **readonly** 접근 (쓰기 권한 X — ClaudeDev 원본은 Claude만 변경). 정확한 cmd 형식은 아래 "Codex CLI cmd 형식" 참조.

**Codex CLI cmd 형식** (2026-05-11 첫 e2e 검증):

| 케이스 | cmd | 사유 |
|---|---|---|
| **새 페이지 생성** (Notion DB에 신규 row) | `codex exec -s workspace-write -C "<ClaudeDev 절대 경로>" "<prompt>" < /dev/null 2>&1` | `notion-create-pages` MCP는 기본 sandbox로 OK |
| **기존 페이지 수정** (update-page) | `codex exec --dangerously-bypass-approvals-and-sandbox -C "<ClaudeDev 절대 경로>" "<prompt>" < /dev/null 2>&1` | `notion-update-page` MCP는 비대화형 모드에서 confirmation 채널 없음 → 자동 cancel → bypass 필수 |

**필수 옵션 공통**:
- `< /dev/null` — stdin 차단. 안 하면 codex가 stdin 대기로 hang (2026-05-11 첫 시도 함정).
- `-C <dir>` — Codex 작업 root. ClaudeDev 절대 경로.
- `run_in_background: true` 권장 — Codex 호출은 1~10분 걸림.

**Codex가 받을 input 셋** (Claude가 호출 시 path/내용 전달):
- `01_Phases/M{N}/{NN}-*-DONE.md` ← 해당 Phase, 1순위 베이스
- `CONTEXT.md` + `CONTEXT_History.md` ← 세션 맥락
- `00_Document/ADR.md` ← 관련 ADR-NNN 절
- 본 템플릿 (`.claude/templates/done-md-template.md`) ← 분업 원칙·8단 구조 사양
- `.claude/commands/session:log.md` ← Notion DB 스키마·페이지 생성 API 명세

**Codex의 책임**:
- readonly로 위 파일 읽기 → 8단 구조로 Notion 페이지 본문 작성
- 내부 용어(`Y2`, `PDL`, `M1 Foundation`, `AI sliding` 등) 첫 등장 시 풀이 추가
- Notion API/MCP로 "Dawnholder 협업 히스토리" DB에 페이지 생성
- 분량 가이드(30~50줄) 준수

**Claude의 책임**:
- 사용자 트리거 받으면 Bash로 Codex CLI 호출
- input 셋 path를 cmd 인자로 정확히 전달
- Codex 출력(생성된 Notion 페이지 URL 등)을 사용자에게 보고

**사용자의 책임**:
- 트리거 신호 ("노션 박아줘" 등)
- Codex가 만든 Notion 페이지 검토 (사실 정합·분량·톤)
- 정정 필요 시 Claude에 재호출 요청 또는 본인이 직접 수정

**트리거 흐름 요약**:
```
Phase 완료 → Claude: 5단계 보고 → -DONE.md 박제 → commit/push (Phase ritual 끝)
        ↓ (사용자: "노션 박아줘")
Claude: Bash로 Codex CLI 호출 (readonly + input paths) → Codex: ClaudeDev 읽고 Notion 페이지 작성
        → Claude: 결과 URL 사용자에게 보고 → 사용자: 검토
```

### Fallback — Codex sandbox 실패 시 (Windows 회귀)

**증상**: `codex exec` 호출이 `CreateProcessAsUserW failed: 5` (Windows API ERROR_ACCESS_DENIED) 또는 sandbox 관련 hard fail로 노션 박제 중단. 2026-05-15 Action 1 노션 박제 시도에서 1회 발생.

**환경 가설**: Codex CLI가 Windows에서 sandboxed worker 생성 시 권한/정책 실패 (정확한 뿌리 불명 — UAC·Smart App Control·그룹 정책 의심). ADR-020(훅 PATH 의존성)과 결이 비슷하지만 *주체가 Codex 외부 CLI*라 별 카테고리.

**Fallback 절차**: Claude가 **`mcp__notion-*` MCP 도구를 직접 호출**해 노션 페이지 생성. Codex CLI 거치지 않음. 사용 도구: `notion-create-pages` (신규), `notion-update-page` (수정), `notion-search` (DB 조회).

**트레이드오프**: Notion 분업 원칙 일시 위반 (Claude=사실 박제 / Codex=재편집). Fallback 사용 시 노션 페이지 톤이 *사실 톤*에 가까워지고, 면접 답변 박스·시각 구조화 같은 *Codex 재편집 가치*는 빠짐. 사용자가 별도 세션에서 Codex 재호출하거나 본인이 직접 다듬는 식으로 후처리.

**재발 시 판단**: 1회성으로 보이면 본 Fallback 그대로. 누적되면 (3회+) 별도 ADR 박제하고 *기본 흐름을 mcp 직접 호출로 역전* 검토.

---

## 참고 사례

- `01_Phases/M1-foundation/01-solution-bootstrap-DONE.md`
- `01_Phases/M1-foundation/02-server-network-DONE.md`
- `01_Phases/M1-foundation/03-tcp-listener-DONE.md`
- `01_Phases/M1-foundation/07-pdl-integration-DONE.md` ← TL;DR + 분업 원칙 박힌 첫 사례 (기준점)
