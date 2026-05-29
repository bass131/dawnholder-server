### ADR-028: Code Convention 수립 (2 권위서 참고서 + 우리 규칙 + 강제 메커니즘)

**날짜**: 2026-05-29
**상태**: 채택됨

**결정**: `00_Document/conventions/`를 신설하고 3층으로 구성한다.
- **`refs/`** — *Game Programming Patterns*(Nystrom) 19패턴 + *게임 서버 프로그래밍 교과서*(배현직) 10장을 **각 1파일**로 정독·색인화한 참고서. 각 파일 = 책 이론 + "언제 참조" 트리거 + 우리 프로젝트 적용 + 함정. 폴더별 `_index.md`로 진입.
- **`CODE_CONVENTION.md`** — 우리가 *채택한 규칙*만 (책 이론은 refs로 위임, 섞지 않음). 핵심 = **§2.2 God class 분리 결정적 기준**(트리거 = 2+ 도메인 / 구조 = 컨테이너[상태+tick 엔진]+System[로직] / 과분할 경계 명시).
- **`INDEX.md`** — 작업 유형 → [우리 규칙 + refs 패턴/장 + 헌법] 라우팅 진입점.

강제는 **4중(§5)**: ① SubAgent 정의에 "코드 작성 전 `INDEX.md` 참조 의무" ② reviewer `REVIEW_CHECKLIST` 축 6 신설(God class·패턴 위반 자동 점검) ③ 핵심 파일 줄 수 hook ④ 본 ADR.

**이유**: M4.3 Phase 07에서 `GameMap`(665줄)이 전투+AI+respawn+broadcast를 모두 가진 God class임이 드러났는데, **reviewer가 이를 못 잡았다** — `REVIEW_CHECKLIST`가 "메서드/파일 길이 등 코드 크기 = 도구(Roslyn) 책임"으로 *명시 제외*했고 SOLID는 학습 포인트(🎓)로만 있어 강제력이 0이었기 때문이다. 사용자가 두 가지를 핵심으로 짚었다: (1) **"ADR·문서 선언만으로는 안 지켜진다(신뢰 문제)"** → 선언이 아니라 *자동 강제 메커니즘*이 필요. (2) **"기반이 애매하면 그 위 코드가 전부 부채"** → *결정적*이고 *완성형*인 규칙. 이에 대응해, 추상 SOLID를 외우는 대신 **이름 있는 패턴 카탈로그**(GPP)를 채택하고 우리 코드에 매핑하는 방식을 택했다(이 프로젝트 학습 철학 + `REVIEW_CHECKLIST` 축 5 정합). 두 권위서는 게임회사 백엔드 포트폴리오(ADR-009)와 정합하며, 우리 코드가 이미 그 위에 서 있다(Map=Actor=교과서 "방 단위 잠금", ServerCore IOCP=교과서 3장, `EnemyState`=GPP State, `GameMap.Tick`=GPP Update Method). 또한 **"구조/SRP(판단 필요)"와 "포매팅(기계적)"을 분리**했다 — 전자는 Convention+reviewer가 *지금* 판단, 후자(.editorconfig+Roslyn)는 §4로 M4.4 이월. 옛 "Reviewer 후속 Roslyn analyzer" 후보가 영영 실현 안 된 이유가 바로 이 둘을 뭉뚱그려 *도구에 통째 위임*했기 때문인데, God class 같은 구조 문제는 도구로 자동 감지가 불가능하다.

**트레이드오프**: 문서 33파일을 한 번에 신설(워크플로우 32 에이전트 / 약 118만 토큰의 일회성 비용)했다 — 그러나 SubAgent가 작업별로 *필요한 파일만 핀포인트 로드*해 오히려 토큰 효율적이고, 두고두고 쓰는 자산 + 면접/포트폴리오 가치가 비용을 상쇄한다. 문서 과분할 우려는 Convention 자신의 §0.3(과한 추상화 경계 — "과분할도 부채")으로 자기 제약한다. refs의 책 내용은 *요약·인덱스*라 저작권상 안전하나 원문 정확도는 정독에 의존(opus 검증 1회 + 실제 코드 경로 봉합 1회를 거쳐 `ServerCore`/`EnemyData` 등 가공 경로를 실제로 정정). 강제 §5의 agent/reviewer/hook은 `.claude/` self-modification 영역이라 *스테이징 → 사용자 적용*에 의존한다 — 적용하지 않으면 본 Convention도 ADR처럼 무력해지는 갭이 남는다(적용은 사용자 책임으로 분담). M4.3 발표 데모(Phase 08~12)를 보류하고 기반에 투자한 것은 사용자 명시 결정("기반 부채가 발표 데모보다 비싸다").

**관련**: [ADR-019](ADR-019-reviewer-agent.md)(reviewer — "코드 스타일 Scope 제외" 조항을 본 ADR이 *부분 뒤집음*: 구조/SRP는 축 6으로 편입, 포매팅만 도구 위임 유지), [ADR-009](../gameplay/ADR-009-portfolio-target.md)(게임회사 백엔드 포트폴리오 — 권위서 기반 정합). `.editorconfig`+Roslyn(포매팅 강제)은 CODE_CONVENTION §4로 M4.4 이월. God class 리팩토링(`GameMap`→CombatSystem/AISystem/RespawnSystem 등, 부록 A)은 본 Convention 확정 + 강제 §5 적용 후 별도 Phase에서 진행.
