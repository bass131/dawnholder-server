# -DONE.md 템플릿

> Phase 완료 시 AI가 작성하는 사실 박제 페어 (헌법: "Phase 완료 시 -DONE.md 박제" 섹션).
>
> 5단계 보고를 출력한 직후 같은 응답 안에서 AI가 작성·commit.
> 작성 위치: `01_Phases/M{N}-{slug}/{NN}-{phase-name}-DONE.md`

---

## 템플릿 본문 (이 아래를 그대로 가져다 채움)

```markdown
# Phase {NN} — {제목} 완료 박제

**완료일**: {YYYY-MM-DD}
**커밋**: {short hash}
**소요 시간**: {대략}

## TL;DR
(2~4문장. 무엇을 / 왜 / 결과를 압축. Codex가 Notion 첫 단락으로 그대로 가져갈 수 있게 *사실*만. 본인 회고/면접 답변 X.)

## 5단계 보고
(방금 출력한 5단계 보고를 그대로 복붙)

## 결정 흐름 (학습 일지 쓸 때 참고용)
- 갈래/대안 → 채택안 → 이유 (한두 줄씩)

## 막혔던 지점 (있다면)
- 증상 → 원인 → 해결 (각 한두 줄)

## 학습 일지 후보 키워드
- /journal-concept 로 펼칠 만한 키워드들
```

---

## 작성 원칙

- **사실 박제**, 본인 회고 X. 회고는 `learning-journal/`에서 본인이 쓰는 영역.
- **잊히기 전에**. 5단계 보고 직후 같은 응답에서 작성.
- **간결하게**. 학습 일지의 *베이스*이지 학습 일지 자체가 아님.
- **검색 가능하게**. "학습 일지 후보 키워드"는 미래의 본인이 `/journal-concept`로 펼칠 단서.
- **Notion 분업 정합**. 아래 "Notion 협업 분업 원칙" 섹션 참조.

---

## Notion 협업 분업 원칙

> 2026-05-11 정합. ClaudeDev 원본(`-DONE.md`/`CONTEXT*`/`ADR.md`)과 Notion "Dawnholder 협업 히스토리" DB 사이의 역할 분담.

### 역할

- **Claude**: `-DONE.md` / `CONTEXT*` / `ADR.md`에 **사실·결정·트레이드오프·테스트·막힘**을 정확히 박제.
- **Codex**: 그 원본을 **Notion용 회고·면접 자료로 재편집**. 문장 다듬기, 면접 답변 박스, 시각 구조화 담당.
- **본인**: `learning-journal/`에서 회고·교훈·면접 답변 작성. AI는 인터뷰만.

### Notion 글 구조 (8단)

```
TL;DR
→ 배경/용어 맥락
→ 핵심 결정
→ 구현 변경
→ 검증 결과
→ 막힌 지점
→ 학습/면접 포인트
→ 다음 액션
```

`-DONE.md` 본문이 위 8단을 *순서대로* 지원하도록 작성. 위 템플릿 본문(TL;DR / 5단계 보고 / 결정 흐름 / 막혔던 지점 / 학습 키워드)이 8단 매핑 베이스.

### 용어 처리

내부 용어(`Y2`, `PDL`, `ADR`, `M1 Foundation`, `AI sliding` 등)는 처음 보는 사람도 이해할 수 있게 **첫 등장 시 한 번 풀어쓴다**. Codex가 Notion 페이지 만들 때 이 풀이를 추가. Claude는 `-DONE.md` 본문에선 풀이 없어도 OK (개발자 본인 + Codex가 베이스로 보는 문서).

### 원칙

- **사실 박제가 1순위**. Claude는 Notion용 문장을 과하게 다듬지 않아도 된다.
- **면접 답변/회고는 Codex/본인 영역**. Claude는 침범 X.
- **막힘·실패·AI sliding 같은 흔들림도 보존**. Codex가 의사결정 품질 사건으로 살림.

---

## 참고 사례

- `01_Phases/M1-foundation/01-solution-bootstrap-DONE.md`
- `01_Phases/M1-foundation/02-server-network-DONE.md`
- `01_Phases/M1-foundation/03-tcp-listener-DONE.md`
- `01_Phases/M1-foundation/07-pdl-integration-DONE.md` ← TL;DR + 분업 원칙 박힌 첫 사례 (기준점)
