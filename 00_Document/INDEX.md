# 문서 지도

현재 Codex 운영 기준은 사용자 합의와 [AGENTS](../AGENTS.md)다. 과거 Claude 헌법·모델 배정·슬래시 명령·학습 장부 규칙은 자동 적용하지 않는다.

## 현재 작업

| 문서 | 용도 |
|---|---|
| [CURRENT](operations/CURRENT.md) | 활성 목표의 `goal.md`로 가는 포인터 |
| [DEVELOPMENT](operations/DEVELOPMENT.md) | 개발 환경, 실행 진입점, 변경 영역별 검증 |
| [목표 루프 스킬](../.agents/skills/dawnholder-goal-loop/SKILL.md) | 다단계 작업의 메인·작업자·독립 검증자 운영 |
| [ORCA](operations/ORCA.md) | 필요한 경우의 Orca 다중 세션 운영 |
| [FEATURE_MAP](FEATURE_MAP.md) | 기능별 구현 위치 |

## 게임 설계와 기술

| 문서 | 용도 |
|---|---|
| [PRD](PRD.md) | 게임 요구사항과 범위 |
| [ARCHITECTURE](ARCHITECTURE.md) | 시스템 구조; 예정 사항은 현행 구현과 대조 |
| [ADR](ADR/INDEX.md) | 기술·게임플레이·과거 운영 결정과 근거 |
| [ADR 이력](ADR_History.md) | 결정 변경 이력 |
| [컨벤션](conventions/INDEX.md) | 코드 규약과 진입점 |
| [리뷰 체크리스트](REVIEW_CHECKLIST.md) | 관련 기술 점검 참고; 과거 에이전트 운영 요구는 자동 적용하지 않음 |

## 과거 기록과 산출물

- [Phase 기록](../01_Phases/)은 과거 작업 정의와 결과를 보존한다. 현재 목표는 `01_Phases/goals/`에서 관리한다.
- [정책](policies/INDEX.md), [슬래시 명령 목록](commands-index.md), [학습·운영 장부](ledgers/INDEX.md), [팀원 가이드](team-guide.html)는 기존 Claude 운영의 역사 자료다.
- [리뷰](reviews/INDEX.md), [사례 연구](case-studies/), [보고서](reports/), [문서 보관함](archive/README.md)에서 과거 근거를 찾을 수 있다. HTML 보고는 목표마다 의무가 아니다.
- 삭제된 `CLAUDE.md`·`.claude` 운영 파일을 가리키는 과거 링크는 [고정 보관 브랜치](https://github.com/bass131/dawnholder-server/tree/archive/claude-setup-2026-09-29)의 같은 경로에서 확인한다. 과거 문서 링크를 복원하려고 현재 절차에 옛 운영 파일을 되살리지 않는다.
