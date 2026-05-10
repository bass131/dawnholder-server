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
- **Notion 분업 정합**. Codex가 이 문서를 베이스로 Notion용 회고/면접 자료를 8단 구조(TL;DR → 배경 → 결정 → 구현 → 검증 → 막힘 → 학습/면접 → 다음)로 재편집함. Claude는 *사실*만 정확히 박고, 면접 답변/문장 다듬기는 Codex 영역. 자세한 분업 원칙은 `CONTEXT.md` "Notion 협업 히스토리 문서 분업" 섹션 참조.

## 참고 사례

- `01_Phases/M1-foundation/01-solution-bootstrap-DONE.md`
- `01_Phases/M1-foundation/02-server-network-DONE.md`
- `01_Phases/M1-foundation/03-tcp-listener-DONE.md`
