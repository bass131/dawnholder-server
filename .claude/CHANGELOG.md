# CHANGELOG — 하네스 변경 이력

> **이 파일의 역할**: 팀장(유영호)이 헌법/ADR/하네스/공유 파일을 변경할 때마다
> 한 줄씩 박제. 팀원이 매일 작업 시작 시 `/session:start`가 이 파일의 최근 줄을
> 자동으로 보여줘서 본인이 옛 결정 기반으로 작업하는 사고 예방.
>
> **갱신 주체**: 팀장(유영호) 단독. 팀원은 읽기만.
>
> **갱신 규칙**:
> - 변경 commit 직후 한 줄 추가
> - 형식: `YYYY-MM-DD — 한 줄 요약 (영향 범위 / 위험도)`
> - 위험도 표기: `[L]` (저위험, 추가만) / `[M]` (중간, 행동 변경) / `[H]` (고위험, 결정 뒤집기)
> - 슬랙(또는 디스코드)에도 같은 한 줄 알림 박기

---

## 갱신 룰 짧게

**저위험 [L]** — 새 슬래시 커맨드, 새 서브에이전트, 새 ADR 박제 (기존 결정 변경 X), 학습 일지 템플릿 보강
**중간 [M]** — 슬래시 커맨드 동작 변경, 기존 ADR 사후 보강, 헌법 일부 섹션 추가
**고위험 [H]** — 헌법 절대 원칙 수정, 기존 ADR 뒤집기 (PostgreSQL→MSSQL 같은 결정 정정), 파일 권한 분리, 영역 분리 변경

[H]는 팀장이 슬랙/디스코드에 추가 안내 + 팀원 작업 일시 중단 요청 권장.

---

## 이력 (최신이 위)

| 날짜 | 변경 | 위험도 |
|------|------|--------|
| 2026-05-16 | Unity Editor 버전 6000.4.1f1 → 6000.4.7f1 동기화. revision hash 통일(`f3c3c4248748`). 발견: 같은 라벨 6000.4.1f1인데 본인 hash(`8535861f39e1`)와 정유현 hash(`336a400b9ea2`) 어긋남 → Hub UI에 hash 검색 기능 없는 한계 우회 위해 `unityhub://VERSION/HASH` 딥링크로 통일. 변경: setup-steps/03-unity-client.md(설치 + hash 검증 절차 박제), 04-finalize.md(검증 보고 1곳), README.md(2곳), team-guide.html(2곳 + "막혔을 때" 표 2행 추가 + 푸터 v1.2), CONTEXT.md 보류 중에 (B2) 6.4 minor 점프 재논의 메모. Known Issue UUM-139557(Image Inspector NRE, 6000.4.7f1에도 잔존) 회피 가이드 동반. 정유현(이미 합류, 사용 중)이 실제로 재설치해야 하고 setup 가이드 동작 변경이라 [M]. | [M] |
| 2026-05-15 | `/session:start` 0단계 게이트에 (B+) 정책 박힘. `ProjectSettings.asset` 변경 시 세 갈래: (C-1) cloud 라인만 → 자동 정리, (C-2) cloud + 다른 변경 → STOP+분리 옵션, (C-3) cloud 변경 X → 기존 STOP. 각자 Unity Cloud 계정(Unity AI 토큰 분리) + cloud 라인 commit 차단을 게이트가 자동 처리. 동반 갱신: `team-guide.html` "막혔을 때" 표에 한 행 박제. 모든 팀원 매일 첫 호출 슬래시 동작 변경이라 [M]. | [M] |
| 2026-05-15 | `/session:start` STOP 메시지 (B)·(C)에 "처음 보는 STOP이면 CHANGELOG 최상단 / 가이드 3번 섹션 박스 보세요" 한 줄 추가 + 가이드 3번 섹션(`00_Document/team-guide.html`) 기존 ⚠ 안전 매뉴얼 박스 아래에 💡 "갑자기 STOP 떴어요? — 하네스 변경 후 첫 세션" note 콜아웃 신설 (3단계 처리 안내 + [H] 변경 슬랙 동반 안내). 같은 날 [H] 게이트 변경의 후속 — 옛→새 전환 시점에 학부생 백지가 패닉 안 하도록 컨텍스트 경로 명시. 동작 변경 아니라 안내 문구 추가라 [M]. | [M] |
| 2026-05-15 | `/session:start` git 안전 게이트 신설 ([0단계] 추가): `git status --porcelain=v1 --branch`로 브랜치+워킹 상태 점검, main 브랜치 또는 uncommitted 변경 시 CONTEXT 읽기 진입 자체 차단 (STOP 메시지 + 해결 안내). 파괴적 명령(`reset --hard`/`checkout .`/`clean -fd`) Claude 실행 절대 금지 명시, 사용자가 요청해도 안내만. 가이드 v1.1 동반 갱신 (`00_Document/team-guide.html`, README에 링크 추가) (다이어그램 순서 뒤집기: `/session:start` 먼저 → 게이트 통과 시 `git pull`, "막혔을 때" 표에 Git 충돌·복구 행 분리, 안전 매뉴얼 콜아웃 박스 신설). 모든 팀원 매일 첫 호출 슬래시 동작 변경이라 [H]. | [H] |
| 2026-05-15 | README 갱신 (Action 1 후속): L1 헌법 표 셀에 `00_Document/policies/` 외부화 명시 ("절대 원칙·라우팅·스택만 / 운영 정책은 외부화"), 추가 인프라 섹션에 `policies/INDEX.md` + `REVIEW_CHECKLIST.md` 2개 항목 신설, 폴더 구조 `00_Document/` 설명에 policies·REVIEW_CHECKLIST 명시. 팀원·외부 독자가 한눈에 헌법/정책 분리 패턴을 보게. | [L] |
| 2026-05-15 | 헌법 응축 (354→175줄, -51%) + 정책 4개 외부화 (`00_Document/policies/`). 신규: `reporting-format.md` (5단계 보고 + work-envelope) / `pin-and-done.md` (current-pin + -DONE.md 박제 + Phase 완료 권유) / `doc-thresholds.md` (220/350줄 임계) / `review-tiering.md` (ADR-019 Tier 2). 헌법 자체는 절대 원칙·라우팅·스택만 유지. 자기참조 정책 루프 해소. 6파일 7건 참조 갱신 (훅·커맨드·템플릿·ADR.md). 훅 grep 패턴↔외부 정책 양식 정합 6항목 확인. | [M] |
| 2026-05-15 | ADR-019 박제: Reviewer 서브에이전트 도입 (Tier 2 자동 리뷰). 신규 `.claude/agents/reviewer.md` + `00_Document/REVIEW_CHECKLIST.md` (5축 점검 매핑) + 헌법 `## Tier 2 자동 리뷰` 섹션 추가. 도메인 에이전트 코드 변경 후 메인 세션이 트리거 조건(새 핸들러/패킷/공식/≥10줄/`98_Shared/` 포함) 충족 시 reviewer 자동 호출. 코드 스타일 검증은 의도적 Scope 제외 (Roslyn analyzer 도입 후보로 미루). 서브에이전트 6→7. | [M] |
| 2026-05-14 | `/session:end` 슬래시 커맨드 신설 (Phase 마감 절차 8단계) + `/session:log` Codex 분기 추가 (Codex 없는 팀원은 Claude 단독 박제) + 헌법 Phase 완료 권유 섹션 통합 (15 → 16 슬래시) + inject-current-pin.sh 훅 확장 (commit 안 된 -DONE.md 경고 주입) | [M] |
| 2026-05-14 | 협업 셋업 인프라 박제: `/setup` 슬래시 커맨드 + setup-steps 5개 + CHANGELOG 시스템 + CODEOWNERS + `/session:start` CHANGELOG 자동 확인 | [M] |
| 2026-05-14 | `.vscode/settings.json` 화이트리스트로 공유 (통합 터미널 Git Bash + 자동 저장 + LF + 저장 시 포맷) | [M] |
| 2026-05-12 | ADR-005 v2: PostgreSQL → MSSQL LocalDB + Windows 통합 인증 정정 | [H] |
| 2026-05-14 | ADR-020 박제: 훅 실행 환경 의존성 (Git Bash + PATH 셋업 필수) | [L] |
| 2026-05-14 | 협업 베이스라인 박제: CONTEXT 시스템 각자 보유 (.gitignore) + learning-journal 사람별 네임스페이스 + `.vscode/extensions.json` 화이트리스트 | [M] |

---

## 이력 외부화 정책

220줄 넘으면 `CHANGELOG_History.md`로 분리 (헌법 ADR-014 — 문서 세분화 정책).
또는 분기별로 자연 분리: `CHANGELOG_2026Q2.md` 등.
