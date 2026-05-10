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

## 참고 사례

- `01_Phases/M1-foundation/01-solution-bootstrap-DONE.md`
- `01_Phases/M1-foundation/02-server-network-DONE.md`
- `01_Phases/M1-foundation/03-tcp-listener-DONE.md`
