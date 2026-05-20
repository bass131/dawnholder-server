# Policies (New_Harness v1) — 헌법 외부화 가이드 카탈로그

> 새 헌법 v1(`../CLAUDE.md`)은 **AI가 매 응답마다 떠올려야 할 절대 규칙**만 둡니다.
> *해당 작업 시점에만 참조하면 되는 정책·양식·운영 가이드*는 본 폴더로 분리합니다.
>
> **분리 원칙**:
> - 헌법 = "*무엇을 절대 어기지 않는가*"
> - policies/ = "*그것을 어떻게 운영하는가*"
>
> 헌법과 본 폴더의 내용이 충돌하면 **헌법이 이깁니다** (단일 진실 공급원 룰).
>
> **본 폴더 발효**: M3.5 Phase 06 전환 commit 시점에 `00_Document/policies/`로 일괄 mv. 그 전엔 옛 `00_Document/policies/` 4개가 정식 운영본.

---

## 정책 목록 (7개 — 옛 4 + 신규 3)

| 파일 | 한 줄 요약 | 헌법에서 참조하는 위치 | 상태 |
|---|---|---|---|
| [`reporting-format.md`](reporting-format.md) | 5단계 보고 양식 (대규모 등급 한정) + MD/HTML 이중 박음 | "응대 원칙 / 작업 보고" | 옛 갱신 (work-envelope 죽임) |
| [`pin-and-done.md`](pin-and-done.md) | work-pin 압축본(5+1 필드) + -DONE.md 박제(복잡/대규모) + 두 액션 권유 | "작업 좌표 + Phase 완료 박제" | 옛 갱신 (work-pin 압축) |
| [`doc-thresholds.md`](doc-thresholds.md) | 220줄·350줄 문서 세분화 정책 + 단위 작업 비대 시 등급 재산정 | "문서 운영 / 문서 세분화" | 옛 미세 정합 |
| [`review-tiering.md`](review-tiering.md) | 3-Tier 리뷰 + Tier 2 = reviewer + plan-auditor 두 SubAgent | "SubAgent 풀 / 자동 호출 트리거" | 옛 재작성 (Tier 2-B 신설) |
| [`grade-and-risk.md`](grade-and-risk.md) | 정량 4등급(단순/보통/복잡/대규모) + 위험 깃발 자동 상향 | "📊 작업 등급" | **신규** (M3.5) |
| [`subagent-routing.md`](subagent-routing.md) | SubAgent 풀 8 라우팅 + 자동 호출 + 에스컬레이션 | "🤖 SubAgent 풀" | **신규** (M3.5) |
| [`knowledge-system.md`](knowledge-system.md) | AI 캐시 도메인별 _index.md + GC + 트랙 A/B 분리 | "📚 Knowledge 시스템" | **신규** (M3.5, Phase 04 reference) |

---

## 옛 → 새 매핑 표 (Phase 06 전환 시점 적용)

| 옛 (`00_Document/policies/`) | 새 (본 폴더) | 변경 형태 |
|---|---|---|
| `reporting-format.md` | `reporting-format.md` | 응축 (work-envelope 절 통째 삭제 / 5단계 보고 조건부화 / MD+HTML) |
| `pin-and-done.md` | `pin-and-done.md` | 응축 (work-pin 8→5+1 필드 / -DONE.md 박제 등급 한정 / 트랙 A/B 분리) |
| `doc-thresholds.md` | `doc-thresholds.md` | 미세 정합 (단위 작업 비대 시 등급 재산정 한 줄 추가) |
| `review-tiering.md` | `review-tiering.md` | 재작성 (Tier 2 = reviewer + plan-auditor 두 SubAgent 정합) |
| (옛 없음) | `grade-and-risk.md` | **신설** |
| (옛 없음) | `subagent-routing.md` | **신설** (옛 헌법 본문 라우팅 표 외부화) |
| (옛 없음) | `knowledge-system.md` | **신설** (Phase 04 산출물 reference) |

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

- 2026-05-20 — M3.5 Phase 01 (2/2) 신설. 옛 4 (응축 갱신) + 신규 3 = 총 7 정책 카탈로그. Phase 06 전환 시점에 옛 `00_Document/policies/INDEX.md` 자리에 mv 예정.
