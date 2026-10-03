# 원칙의 근거와 채택 경계

하네스는 에이전트가 작업을 읽고 실행하고 결과를 확인하는 환경과 피드백 수단을 말한다. 여기서는 외부 자료의 주장과 프로젝트가 선택한 운영 조건을 구분한다. 채택 권한은 사용자 결정에 있고, 외부 글의 성공 사례가 같은 효과를 보장하지 않는다. 선택과 보류는 [ADR-034](../../../ADR/harness/ADR-034-harness-principles.md), 현재 실행 상태는 [goal](../../../../01_Phases/goals/2026-10-03-harness-principles/goal.md)이 소유한다.

## 원문에서 확인한 근거

**pstack의 반복 교정과 읽기 부담.** 반복되는 교정을 도구·메타데이터·검사로 옮기고, 판단이 필요한 내용은 사례로 설명하자는 제안이다. 다른 원칙은 독자가 추적할 단계와 기억할 상태를 구분하며, 새 독자가 값의 출처와 변경 주체를 짧은 시간에 찾는지 묻는다. 조회한 기준은 `cursor/plugins`의 commit `23e4138daa01c42d4969f7a5465f82704e64f798`다. [반복 교정 원칙](https://github.com/cursor/plugins/blob/23e4138daa01c42d4969f7a5465f82704e64f798/pstack/skills/principle-encode-lessons-in-structure/SKILL.md), [읽기 부담 원칙](https://github.com/cursor/plugins/blob/23e4138daa01c42d4969f7a5465f82704e64f798/pstack/skills/principle-minimize-reader-load/SKILL.md).

**OpenAI의 저장소 지식과 경계 검사.** Ryan Lopopolo의 2026-02-11 글은 짧은 진입 문서에서 저장소 정본으로 안내하고, 계층 경계를 검사하며, 진단에 수정 지침을 넣은 팀의 경험을 설명한다. 이 팀의 자율 작업과 병합 방식은 특정 환경의 사례다. [Harness engineering의 지식 정본·경계 검사·학습 한계 절](https://openai.com/index/harness-engineering/).

**Böckeler의 사전 안내와 사후 관측.** 2026-04-02 글은 행동 전에 길을 안내하는 수단과 실행 후 결과를 관측하는 수단을 함께 다룬다. 결정적 검사와 모델의 의미 판단은 비용과 성격이 다르며, AI가 만든 테스트만으로 기능 신뢰를 충분히 얻었다고 보기 어렵다고 지적한다. [Harness engineering for coding agent users](https://martinfowler.com/articles/harness-engineering.html).

**Böckeler의 유지보수성 검사 실험.** 2026-05-27 글은 수정 방법을 담은 진단, 경고·억제의 관리 비용, 검사 상태 이력의 관측을 설명한다. 기능이 있는 CLI와 실제로 호출되는 검증 흐름은 별개라는 한계도 남긴다. [Maintainability sensors for coding agents의 경고 관리·효과·결론 절](https://martinfowler.com/articles/sensors-for-coding-agents.html).

**Kent Beck의 변경 분리.** 2025-06-25 경험담의 Tidy First 부록은 구조 변경과 동작 변경을 별도 커밋으로 나누고 전후 테스트로 보존을 확인하도록 한다. 본문에서는 반복 루프, 요청하지 않은 기능, 테스트 삭제·비활성화 등을 경고 신호로 다룬다. [Augmented Coding: Beyond the Vibes](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes).

**Anthropic의 단순한 구성과 실제 피드백.** 2024-12-19 글은 필요한 만큼만 복잡도를 추가하고, 실제 환경 결과로 진행을 확인하며, 도구 인터페이스를 명확히 만들도록 제안한다. 현재 페이지도 당시 도구 설명의 시점 한계를 표시한다. 이 글을 최신 제품 기능 목록으로 사용하지 않는다. [Building effective agents](https://www.anthropic.com/engineering/building-effective-agents).

## 프로젝트에서 적용할 때의 대가

아래는 외부 주장을 Dawnholder에 적용하며 내린 설계 판단이다. 정책 정본의 실제 절차를 이 자료에 복제하지 않는다.

| 우려·대안 | 프로젝트의 선택과 이유 |
|---|---|
| 지적마다 문구만 추가하면 읽기 비용이 늘고 실제 실행 여부를 확인하기 어렵다 | 반복성·검출 가능성·사용처가 확인된 문제를 검사/helper 후보로 삼는다. 판단이 필요한 설계·가독성은 독립 검토로 남긴다 |
| 검사를 늘리면 오탐과 유지 비용도 늘 수 있다 | 새 검사는 warning 파일럿의 실제 결과를 보고 승격한다. 도구 실행 실패를 위반이나 통과와 섞지 않는다. 기존 CI 정책을 소급 완화하는 결정은 아니다 |
| 모든 간접 계층을 제거하면 의도한 경계까지 잃을 수 있다 | 30초 읽기 확인은 탐색 문제를 찾는 수단이다. 파일 길이·계층 수만으로 합치거나 나누지 않고 책임·소유·변경 이유를 함께 판단한다 |
| 에이전트가 구현과 기대값을 같은 계산으로 만들면 테스트가 독립 근거가 되지 않는다 | 작성자와 검증자를 분리하고 요구사항·보존 동작에서 기대값을 얻는다. 린터 성공은 사람 가독성이나 실제 플레이·DB 검증을 대신하지 않는다 |
| 완성도보다 처리량을 우선하는 운영은 현재 목표와 다를 수 있다 | 독립 검증과 PR별 사용자 병합 승인을 유지한다. 자동 병합을 도입하지 않는다 |

pstack의 주석 제거 도구도 패키지에 존재하지만 전체 패키지를 채택한 것은 아니다. 프로젝트는 비자명한 이유와 계약을 가까이 남기는 [현행 주석 정책](../../CODE_CONVENTION.md#주석과-문서)을 유지한다. [pstack의 해당 도구 목록](https://github.com/cursor/plugins/blob/23e4138daa01c42d4969f7a5465f82704e64f798/pstack/README.md).

원문 확인이 곧 도구 설치·자동 선택·정책 강제의 증거는 아니다. 외부 성과 수치와 문서 예시를 이 프로젝트의 실행 실적으로 사용하지 않는다.
