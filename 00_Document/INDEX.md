# 00_Document — 문서 지도 (마스터 INDEX)

> 이 폴더의 모든 문서로 가는 **단일 진입점**. "어디에 뭐가 있지?"를 한 번에 해결합니다.
> 새 카테고리·핵심 문서 추가 시 이 표도 갱신하세요 (ADR/·policies/ INDEX와 같은 규율).

## ⚖️ 충돌 우선순위

문서끼리 말이 다르면 위에 있는 게 이깁니다:

**[`CLAUDE.md`](../CLAUDE.md)(헌법) > [`ADR/`](ADR/INDEX.md)(결정) > [`policies/`](policies/INDEX.md)(운영) > [`ARCHITECTURE.md`](ARCHITECTURE.md)(구조) > [`PRD.md`](PRD.md)(요구사항)**

---

## 📄 핵심 문서 (루트)

| 파일 | 무엇 |
|---|---|
| [`PRD.md`](PRD.md) | 무엇을 만들지 — 그리고 **안 만들** 것 |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | 시스템 구조의 큰 그림 |
| [`REVIEW_CHECKLIST.md`](REVIEW_CHECKLIST.md) | 리뷰 5축 점검 항목 (reviewer SubAgent 기준) |
| [`commands-index.md`](commands-index.md) | 슬래시 커맨드 카탈로그 |
| [`ADR.md`](ADR.md) | ADR 카탈로그 랜딩 (얇은 입구 → [`ADR/INDEX.md`](ADR/INDEX.md)) |
| [`ADR_History.md`](ADR_History.md) | ADR 변경 이력 |

> 위 6개는 헌법(`CLAUDE.md`)이 직접 가리키는 **앵커**라 루트에 고정됩니다 (옮기면 헌법 본문 수정 필요).

---

## 📂 카테고리

| 폴더 | 무엇 | 인덱스 |
|---|---|---|
| [`ADR/`](ADR/INDEX.md) | 아키텍처 결정 기록 (왜 이 선택을 했는지). tech-stack·gameplay·harness 3분류. **append-only(동결)** | ✅ |
| [`policies/`](policies/INDEX.md) | 헌법의 운영 가이드 (등급·보고·라우팅·루프 등) | ✅ |
| [`conventions/`](conventions/INDEX.md) | 코드 컨벤션 + 진입점 + 참고서적(`refs/`) | ✅ |
| [`ledgers/`](ledgers/INDEX.md) | 루프 운영 원장 — `pending-art`·`pending-comprehension`·`pending-knowledge` (ADR-032) | ✅ |
| [`reviews/`](reviews/INDEX.md) | 과거 리뷰 기록 (α/β/γ cross-review 스냅샷, M3~M4.5) | ✅ |
| [`reports/`](reports/) | Phase 캡스톤 HTML 시각화 (`reporting-format.md` 정규 출력 위치) | — |
| [`case-studies/`](case-studies/) | 심화 사례 연구 (예: AI 오케스트레이터 동시성) | — |
| [`archive/`](archive/README.md) | 은퇴·오래된 보관 문서 ([README](archive/README.md) 참조) | ✅ |

---

## 🧭 빠른 길찾기

- **"왜 이렇게 만들었지?"** → [`ADR/INDEX.md`](ADR/INDEX.md)
- **"이 작업 어떻게 굴려?"** (등급·보고·루프) → [`policies/INDEX.md`](policies/INDEX.md)
- **"코드 어떻게 짜?"** → [`conventions/INDEX.md`](conventions/INDEX.md)
- **"지금 밀린 거 뭐 있지?"** (아트·이해·knowledge) → [`ledgers/INDEX.md`](ledgers/INDEX.md)
- **"과거에 뭘 리뷰했지?"** → [`reviews/INDEX.md`](reviews/INDEX.md)
- **작업 단위(Phase)** 는 여기 아닌 [`../01_Phases/`](../01_Phases/) (사람별 namespace)
