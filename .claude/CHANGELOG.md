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
| 2026-05-18 | **`.githooks/pre-commit` 도입 + `core.hooksPath` 공유 설정** — Claude Code의 PostToolUse hook(`validate-phase-gate.sh`)이 *우회 가능*함을 정유현 PR #27 분석에서 발견 (Phase 04~06 -DONE.md frontmatter 누락 = 지역 컨벤션 드리프트). 진단: 사용자 명시 선호 시 Claude가 hook 차단 메시지 보고도 진행 + 외부 에디터 직접 편집 시 hook 작동 X + git commit 자체는 PostToolUse 매처 범위 밖. **해결**: `.githooks/pre-commit` 신설 → `git config core.hooksPath .githooks`로 공유 → commit 시점에 변경된 `-DONE.md` 각각 `validate-phase-gate.sh` 호출 → 형식 불일치 시 exit 1로 commit 차단. **신규/기존 팀원 영향**: 각 머신에 *1회* `git config core.hooksPath .githooks` 실행 필요 (setup-steps/02-common 8단계 자동 안내 박힘). 합류자(인규)는 /setup 흐름에서 자동. **기존 팀원(정유현·본인) 수동 1회**. 우회는 `--no-verify`로만 가능(의도적 학습용). main 보호 룰 추후 PR 단계 검사 추가 검토 (M3+). 영역 = 모든 팀원 매번 commit에 적용되는 가드라 [M]. PR. | [M] |
| 2026-05-18 | **γ 방식 ad-hoc 전체 감사 패턴 첫 실측 + M2.5 마일스톤 신설 + REVIEW_CHECKLIST 5.7 신설 + review-tiering 실측 1회 박제** — M2 완료 증명 직후 M3 진입 전, 본인 단독 영역(02_Server/98_Shared/99_Tools/04_ClientNet/문서)을 Claude reviewer agent (α) + Codex CLI (β) 양쪽으로 점검 + 결과 비교 (~50분 비용). 결과: α 14건 + β α 전부 동의 + 추가 6건 = 총 14+6건. Codex 추가 발견이 *우선순위 자체 뒤집음* (M3 broadcast 첫 데모 ghost player 위험이 문서 정리보다 급함). 산출물: `00_Document/reviews/2026-05-18-pre-m3-{claude,codex}-review.md` 2건 + PR #24 머지 → main `1548628`. **M2.5 별도 마일스톤 결정** = Phase 09 (Trust-boundary fail-closed) + Phase 10 (Session lifecycle race 제거) → M3 진입. 후속 인프라 갱신: REVIEW_CHECKLIST 축 5에 5.7 (세이프티 약속 일치 검증, rate-limit/ProtocolVersion 패턴 산출) + review-tiering.md 섹션 7 실측 1회 기록. ADR 신설 / `/work:audit` 슬래시 신설은 Rule of Three까지 보류 (2~3회 더 실측 후). 영역 = 인프라 새 항목 + 운영 패턴 박제라 [M]. PR #24. | [M] |
| 2026-05-17 | **PDL 수정 후 후속 작업 의무 박제 (운영 룰)** — `cee8775`(Phase 06 tick counter `uint` 통일) 후 PacketGenerator 재생성 + Shared.dll commit 둘 다 누락한 채 main push → 정유현 pull 시 빌드 회귀로 표면화. 영구 fix는 PR #19(아래 줄, `.gitignore` 화이트리스트 + Shared.dll commit). 합류자(인규)는 PR #19 덕에 같은 사고 X. **단 향후 본인이 PDL XML 수정 시** 의무 3종: (a) `PacketGenerator` 즉시 재생성, (b) `dotnet build`로 Shared.dll 갱신, (c) 두 산출물 동반 commit. 헌법 #2(Protocol is Sacred) + #4(Shared Code Discipline) 정합. 운영 룰 명시화라 [M]. | [M] |
| 2026-05-17 | **Shared DLL `.gitignore` 정정 + asmdef Unity.TextMeshPro 추가** — 정유현 main pull 사고(빌드 깨짐 → Safe Mode)로 표면화된 시스템 결함 정정. **원인**: `03_Client/Assets/Plugins/**/*.dll`이 `.gitignore`라 `Shared.dll`이 git 추적 X → 다른 팀원 pull 직후 Unity 측 Shared 어셈블리 outdated 또는 누락 → 클라 코드의 `uint clientTick`이 옛 DLL의 `int`와 충돌처럼 보임 (옛 DLL 잔재 가능성). **fix**: (1) `.gitignore` 화이트리스트 추가 — `!03_Client/Assets/Plugins/Shared/Shared.dll` + `.meta` (실제 작동은 `Shared.csproj`의 `CopyToUnityPlugins` PostBuild target이 자동 복사, 본 정정은 *commit 강제*로 다른 머신 즉시 동작 보장). (2) `Dawnholder.Client.asmdef`에 `Unity.TextMeshPro` references 추가 (정유현 Phase 03 HUD의 `HudController.cs` TMP_Text 사용분 — ADR-021 Scene 분리 후 누락). (3) `Shared.dll` 최신 빌드 commit 동봉. **합류자 영향**: pull 직후 Unity 즉시 동작 (별도 `dotnet build` 불필요). 단 본인이 Shared 수정 시엔 여전히 `dotnet build`로 새 DLL commit 필요. 헌법 #4(Shared Code Discipline) 정합. 영역 = 빌드 인프라 변경이라 [M]. PR. | [M] |
| 2026-05-17 | **정유현 영역: UI Scene 분리 + Additive Load 패턴 박제 (ADR-021)** — `03_Client/Assets/Scripts/Bootstrap/SceneBootstrap.cs` 신설 (`Awake()` → `SceneManager.LoadSceneAsync("UI", Additive)` + 중복 가드) + `Assets/Scenes/UI.unity` 신설 + Phase 03 HUD 통째 이사 (`HudController` + Canvas + 4 자식 + EventSystem) + `CODEOWNERS` 영역 분리 3 경로(`/03_Client/Assets/Scenes/UI.unity`, `/03_Client/Assets/Scripts/Bootstrap/`, `/03_Client/Assets/Scripts/UI/`)를 `@jungyoohyun0105` 단독으로 박음. 같은 PR에 `@yuhyeon` placeholder → `@jungyoohyun0105` 정정 5곳 동봉 (line 17 메모 "정정 완료" 흔적까지 자기 판단 박힘). 정유현 Phase 03 HUD 마감 직후 *영역 분리* 가치 자각 → 팀장 합의 후 ad-hoc 작업(`ad-hoc-20260517-ui-scene-split`). **이유**: Unity `.unity` 파일 YAML 자동 머지 충돌 위험 차단 + 한국 게임 회사 정석 패턴(씬 하나당 한 명). **합류자 영향**: 정유현 후속 UI 작업 단독 영역, 팀장·인규 PR 강제. `03_Client/CLAUDE.md` Layout 표 동반 갱신(`Scripts/Bootstrap/` 한 줄). 영역 분리 변경이라 [M]. PR #17. | [M] |
| 2026-05-16 | **`.vscode/tasks.json` + `launch.json` 신설** — 공통 빌드/실행 인프라 박힘. `Ctrl+Shift+B` → "Build Solution" (`dotnet build Dawnholder.slnx`, default), Tasks 메뉴 → "Run Server" (`dotnet run --project 02_Server/GameServer`), `F5` → "Debug GameServer" (coreclr 디버거 launch, preLaunchTask로 자동 빌드). `.gitignore` 화이트리스트 정정 (`!.vscode/tasks.json` + `!.vscode/launch.json` 추가). **Git Bash backslash escape 함정 회피**: VS Code의 `${workspaceFolder}` 절대경로(`\` 포함)를 Git Bash가 escape로 먹어 `C:DevClaudeDev` 망가짐 → 상대 경로 + `options.cwd: "${workspaceFolder}"`로 우회 (ADR-020 함정 클래스). **합류자 영향**: 자동 동작 — pull 직후 Ctrl+Shift+B/F5 즉시 사용 가능. 단 *첫 F5* 시 C# Dev Kit extension 설치 필요 (권장 목록에 박혀있음, 본인이 Install). Phase 06 부산물로 박힘. 공통 빌드/실행 셋업 변경이라 [M]. | [M] |
| 2026-05-16 | **정유현 영역 신설 + 슬러그 통일(`yuhyun` → `yuhyeon`)**. 신설: `01_Phases/yuhyeon/README.md` (학습 목표·트랙·팀장 메모·권장 흐름 시드), `00_Document/learning-journal/yuhyeon/` (`.gitkeep` + 짧은 README). 슬러그 정정 5건: `CODEOWNERS` 3행(폴더 + GitHub username placeholder), `learning-journal/README.md`, `.claude/templates/CONTEXT-template.md`, `.claude/setup-steps/01-intro.md`. PR #11(`01_Phases/` 영역 분리)에서 `yuhyeon` 슬러그 박힌 뒤 기존 자리잡이 `yuhyun`과 어긋난 상태 정정. 마일스톤 폴더는 *유현 본인이 `/work:plan`으로* 만들도록 비워둠(각자 phase 가시화 의도). 영역 신설 + placeholder username 정정이라 [L]. | [L] |
| 2026-05-16 | **`00_Document/learning-journal/` 정책 변경 — 비공개 → 공개**. 기존: `learning-journal/*/` ignore (사람별 폴더 비공개, 템플릿·README만 화이트리스트). 새: ignore 패턴 통째 제거, 모두 기본 공개. **이유**: 팀 학습 환경 공유 + 합류자 참고 자료 + 포트폴리오 어필. **영역 자율**: 비공개 원하면 본인이 자기 네임스페이스를 `.gitignore`에 직접 추가 (예: `00_Document/learning-journal/<자기 슬러그>/`). 본인 일지 2건 자동 추가 (`youngho/M1-foundation/01-solution-bootstrap.md`, `youngho/M1-foundation/concepts/dotnet-standard-and-embedded-pdb.md`). **합류자 영향**: 정유현/인규가 자기 첫 일지 만들 때 *자동 공개됨* — 슬랙/디스코드 미리 안내 권장. 위험도 [H] (영역 정책 뒤집기, 사적 영역이 공개로 전환). | [H] |
| 2026-05-16 | **`01_Phases/` 사람별 네임스페이스 분리** — 구조: `01_Phases/<본인 네임스페이스>/M{N}-{slug}/NN-*.md`. `learning-journal/<본인 네임스페이스>/` 패턴과 일관 (사람이 상위, 마일스톤이 그 안). 본인 작업물 27개 .md + 2개 .html 모두 `01_Phases/youngho/M{N}/`로 `git mv` (history 보존). 본인 의도 = 팀원이 자기 phase 만들 때 본인 phase와 겹침 차단. 정유현이 자기 phase 만들면 `01_Phases/yuhyeon/M{N}/...`, 인규는 `01_Phases/ingyu/...` 식. `01_Phases/README.md` + `_template.md`는 공유 그대로. 동반 갱신: CLAUDE.md 헌법 / PRD.md / commands-index.md / pin-and-done.md / ADR-013 / done-md-template.md (2건 + 실제 사례 4줄) / CONTEXT-template.md (2건) / commands/work/plan.md (2건) / commands/journal/phase.md (2건) / team-guide.html FAQ — 모두 `<본인 네임스페이스>` placeholder 또는 `youngho` 실제 경로로 갱신. 옛 빈 마일스톤 폴더(`01_Phases/M1-foundation/`, `01_Phases/M2-first-connection/`) 자동 정리. 훅(`validate-phase-gate.sh`)에 경로 박힘 X — 안전. **영역 분리 변경이라 [H]** (본인 룰: 영역 분리는 [H]). 정유현 pull 시 정유현 머신에서 본인 작업물이 `youngho/` 안에 있는 걸로 보임 (학습 자료 활용 OK). | [H] |
| 2026-05-16 | `/session:end` 동작 변경 — 단계 7.5 신설로 CONTEXT.md 자동 갱신 흐름 박힘. 자동 갱신 범위: "⏸️ 현재 멈춤 지점" 한 문단 + "학습 일지 후보" 1~2줄 + CONTEXT_History.md 이력 한 줄. 자동 갱신 X: 다른 섹션·응축(큰 마일스톤 때만 사용자 결정). 미리보기 → "OK/스킵/수정" 컨펌 후 Edit. `.gitignore`된 본인 자산 안전망(7.5-A). 동반: 본 커맨드 학부생 안내문 응축(264→242줄, -22), `doc-thresholds.md` "220줄 임계" 표에 슬래시 커맨드 행 명시화(응축 가능형 동일 룰), `commands-index.md` 두 곳 동기화. ad-hoc 케이스(session:log 단독 호출)는 본 변경 효과 본 후 실측 기반 별도 추가 결정. 매 세션 마감마다 본인이 "CONTEXT 갱신해줘" 손으로 부탁하던 부담 자동화. 모든 팀원 Phase 마감 시 호출 슬래시 동작 변경이라 [M]. | [M] |
| 2026-05-16 | `.gitattributes` 신설 (Unity YAML 풀세트 LF + .NET 솔루션 CRLF + 바이너리 명시). Windows + `core.autocrlf=true` 환경에서 Unity가 `.asset` 파일을 같은 내용으로 다시 저장 → Git stat이 touch 감지 → phantom dirty 트리거 차단. 트리거 사건: 정유현 환경에서 `EditorBuildSettings.asset` + `ShaderGraphSettings.asset`이 빈 diff + "LF will be replaced by CRLF" 경고만으로 떴음 (ad-hoc Unity 6000.4.7f1 동기화 후속). 본 파일 없으면 매 Unity 실행마다 누군가 (C-3) STOP에 걸려 일을 못 시작. 동반: 학습 보존 (8) — Unity 프로젝트 Windows 함정 = `.gitattributes` 없으면 autocrlf=true가 phantom diff 만드는 패턴. PR #8. | [M] |
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
