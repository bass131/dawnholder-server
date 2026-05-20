---
milestone: M3.5
title: 새 하네스 v1 문서화
status: planned
owner: youngho
target_branch: youngho/harness-v1
prerequisite: M3 마감 (PR youngho/m3-wrap -> main 머지 완료)
target_completion: 2026-05 말 ~ 2026-06 초
---

# M3.5 — 새 하네스 v1 문서화 (Milestone Plan)

## 배경

5/20 면담 후 의논 결과 박힌 *새 하네스 v1 모델*을 *Repo 구조에 박는* 별도 마일스톤. M3와 M4 사이 *게이트* 역할. 작업 KPI 전환을 위한 인프라 마이그.

### 핵심 변경 (5/20 의논 결과)

- **KPI 전환**: "학습 박제 중심" → "Planning → 구현 → 보고" (학습은 트랙 B로 분리)
- **팀 합류**: 영호(server/shared) + 유현(client) + 인규(client + ComfyUI 2D 자산). Repo 모델 (B) = 단일 Repo + 공통/개인 namespace 분리, main 보호 (영호만 머지)
- **정량 4등급**: 단순 / 보통 / 복잡 / 대규모 (정량) + 위험 Hook 자동 상향 (trust-boundary / irreversible / unity-asset)
- **SubAgent 풀 8개**: server / shared / client / qa + reviewer / plan-auditor / unity-bridge / coordinator
- **Knowledge 시스템 풀세트** + GC Collector (PDF NDREAM 패턴 참조)
- **양식 다이어트**: work-envelope X, 5단계 보고 = 대규모 Phase 완료 시만, work-pin 유지(압축)
- **모델 분담**: Sonnet/Opus (PDF 패턴 그대로)
- **헌법 *부분 갱신***: 절대 원칙 5개(서버 권위/프로토콜 신성/신뢰 경계/공유 코드 규율/틱 블로킹) 유지, 운영 양식 절 다이어트
- **Phase 폴더**: `01_Phases/<영호|유현|인규>/M{N}-...` namespace + frontmatter `owner:`
- **보고서**: MD + HTML 이중 박음 (캡스톤 평가 자산)
- **`/harness-review` `/cross-review`**: 스킬화 (수동 트리거)

상세 의논 결과: `01_Phases/youngho/M3-first-multiplayer/08-client-asset-zones-ui-DONE.md` 의 *의논 자산 별 섹션* 참조.

## Phase 분해 (6개)

| # | 제목 | 등급 | 도메인 | 핵심 산출물 |
|---|---|---|---|---|
| 01 | 헌법 + docs/ 다이어트 | 복잡 | 문서 | `CLAUDE.md` 부분 갱신 (절대 원칙 5개 유지, 운영 양식 절 다이어트), `00_Document/policies/` 압축 |
| 02 | SubAgent 풀 8 정의 | 대규모 | `.claude/agents/` | server / shared / client / qa + reviewer / plan-auditor / unity-bridge / coordinator (각 `.md` 정의) |
| 03 | Hook 인프라 | 대규모 | `.claude/hooks/` + `settings.json` | `dangerous-cmd-guard` / `tdd-guard` / `circuit-breaker` / `risk-detector` (trust-boundary/irreversible/unity-asset 자동 상향) / `shared-discipline-guard` |
| 04 | Knowledge 시스템 + GC Collector | 대규모 | `.claude/knowledge/` | 도메인별 `_index.md` + 파일 크기 한도 + GC Collector 에이전트 + 템플릿 |
| 05 | 슬래시 정리 + `/harness-review` `/cross-review` | 복잡 | `.claude/commands/` | 옛 16개 슬래시 다이어트 (학습 일지 트랙 B 이주, work-envelope 죽임), 신규 2개 추가 |
| 06 | 양식 다이어트 + 팀 셋업 정합 | 복잡 | 전 영역 | work-envelope 제거, 5단계 보고 조건부화, 보고서 MD+HTML 양식, Phase 폴더 namespace 정합 (`yuhyun/`, `inkyu/` 신설), 팀 셋업 가이드 갱신 |

## 의존성 그래프

```
01 (헌법/docs) ── 02 (SubAgent) ─┬─ 03 (Hook) ────────┐
                                 └─ 04 (Knowledge) ───┤
                                                      ├─ 05 (슬래시) ── 06 (양식/정합 마감)
```

- **01 → 02**: 헌법이 SubAgent 권한·도메인 본질 정의
- **02 → 03**: Hook이 SubAgent 행동 강제 (역할 강제)
- **02 → 04**: Knowledge가 SubAgent 캐시 입출력
- **03 + 04 → 05**: 슬래시가 Hook/Knowledge에 접근
- **05 → 06**: 양식 마감이 슬래시 정리 후 정합 검증

## 진입 조건

- [ ] M3 마감 PR `youngho/m3-wrap` → `main` 머지 완료
- [ ] `youngho/harness-v1` 브랜치 신규 분기 (from main)
- [ ] `/work:plan M3.5` 또는 본 plan 읽고 Phase 01부터 진입

## 마감 조건

- [ ] Phase 01~06 모두 -DONE.md 박힘
- [ ] 새 헌법 + SubAgent + Hook + Knowledge + 슬래시가 *최소 1회 실제 호출*로 검증됨
- [ ] `/harness-review` 첫 회차 실측 통과
- [ ] 팀(유현 + 인규) 신규 namespace 폴더 준비 + 셋업 가이드 공유 완료
- [ ] M4 진입 준비 게이트 (CLAUDE.md 새 모델 KPI 적용)

## 후속 (M3.5 완료 후)

- **M4 진입**: 새 하네스로. `/work:plan M4` 호출 시 *새 모델 적용* (`youngho/m4-...`, `yuhyun/m4-...`, `inkyu/m4-...` 병렬 가능)
- **외부 리뷰 mini-Phase 4건** (`Dawnholder-harness-review-2026-05-19.md`)은 M3.5 후속 별 처리 (M4 backlog와 분리)
- **Unity 자동 변경 잔여** (`Shared.dll` / `EditorSettings.asset` / `ProjectSettings.asset`) = 자연 흡수 또는 별 commit

## 의논 reference

5/20 의논 압축본은 work-pin 박혔고, *상세 결정 흐름*은 Phase 08 -DONE.md *의논 자산 별 섹션* 참조. 추가 세부:

- **복잡도 우선순위**: 1순위 = 가역성 + 신뢰경계 침범 / 2순위 = 도메인 × 영역 수 / 3순위 = Unity 비중. (소요 시간은 *입력 아님*, 결과)
- **위험 깃발**: trust-boundary / irreversible / unity-asset (자동 등급 상향)
- **처리 패턴**: 단순=Main 직접 / 보통=Worker / 복잡=Coordinator / 대규모=Team (PDF NDREAM 패턴 그대로)
- **Planning 모호점 처리**: 하이브리드 — *반드시 묻기* (등급/위험/도메인 경계/가역성 0/SubAgent 동원수) vs *AI 가정 + 통보* (변수명/스타일/세부 분기) vs *에러 인터럽트* (Hook 차단/빌드 실패)
- **Hook 정책**: PreToolUse = 차단 (exit 2) / PostToolUse = 경고만 / Stop = 보고서 형식 검증
- **Worker 6단계**: 0 완료기준 → 1 Knowledge 조회 → 2 코드 → 3 자가검증 → 4 Knowledge 업데이트 → 5 보고
- **에스컬레이션**: Sonnet 2회 실패 → Opus 재호출 → 사용자
- **TaskCreate 활용**: 대규모 등급 Phase 내부 분해에만 (Team 안)
- **`docs-writer` 별 SubAgent X**: reviewer 체크리스트로 흡수
- **Phase 입자 키움**: 5~7개/마일스톤 (옛 M3 9개는 과했음)
- **옛 학습 자산**: `00_Document/learning-journal/` `-DONE.md` ADR policies 모두 *그대로 둠* (참조만, 캡스톤 평가 자산)
- **Compaction 보존 5개**: Main = 코드 금지 / 도메인·복잡도·라우팅 / 현재 Task ID / 에스컬레이션 / 한국어
