# Policies — 헌법 외부화 가이드 카탈로그

> 헌법(`CLAUDE.md`)은 **AI가 매 응답마다 떠올려야 할 절대 규칙**만 둡니다.
> *해당 작업 시점에만 참조하면 되는 정책·양식·운영 가이드*는 본 폴더로 분리합니다.
>
> **분리 원칙** (2026-05-15 박힘):
> - 헌법 = "*무엇을 절대 어기지 않는가*"
> - policies/ = "*그것을 어떻게 운영하는가*"
>
> 헌법과 본 폴더의 내용이 충돌하면 **헌법이 이깁니다** (단일 진실 공급원 룰).

---

## 정책 목록

| 파일 | 한 줄 요약 | 헌법에서 참조하는 위치 |
|---|---|---|
| [`reporting-format.md`](reporting-format.md) | 5단계 보고 양식 + work-envelope 봉투 양식·훅 동작 | "응대 원칙" 섹션 |
| [`pin-and-done.md`](pin-and-done.md) | current-pin 시스템 + -DONE.md 박제 + Phase 완료 시 두 액션 권유 | "응대 원칙" 섹션 |
| [`doc-thresholds.md`](doc-thresholds.md) | 220줄·350줄 문서 세분화 정책 (Level 1→2→3 재귀) | "문서 운영" 섹션 |
| [`review-tiering.md`](review-tiering.md) | ADR-019 3-Tier 리뷰 구조 + Tier 2 자동 호출 트리거 | "Agent Routing" 섹션 |

---

## 추가 정책 발생 시

- 본 폴더에 `{topic}.md` 추가
- 본 `INDEX.md` 표에 한 줄 추가
- 헌법에서 참조하는 위치 명시

## 폐기 시

- 파일 자체는 `git history`로 보존, INDEX에서 제거
- 헌법에서 해당 링크 제거

---

## 갱신 이력

- 2026-05-15 — 헌법(354줄, 21KB) 응축 작업 일환으로 정책 4개 외부화 (Action 1)
