# Unity 엔진 6.6·AI Assistant 업그레이드

상태: **6000.4.7f1 기준선 측정 완료(EditMode 356/356, PlayMode 11/11, 자체 점검). 사용자가 6000.6.4f1 설치를 마쳤고, 엔진 전환 작업자를 발행한다.** 이어갈 곳은 [재개 지점](#재개-지점)이다.

- 담당: Content Astra. 시작 기준 `origin/main` = `94fc68455107c56aee2f5ba5ddffc1f1782de9c0`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/unity-upgrade-active`, branch `chore/unity-engine-upgrade-20261007`. 보류 중인 [아이템·인벤토리·재화](../2026-10-05-items-inventory-currency/goal.md#재개-지점)는 `content-active`에 그대로 둔다.
- 착수 원천: 메인 `msg_f9415cd0f5c7`(2026-10-07T08:56:34Z)이 전달한 사용자 원문 「대시보드 결정 응답: 1) 계획 검토 - Content Unity 엔진 6.6 + AI Assistant 2.20 업그레이드 - 다섯 안건 → A 승인 (초안 msg_c2ded667928a)」. 메인 전달이며 직접 입력으로 격상하지 않는다. 앞선 사용자 원문은 「음 그럼 일단 백로그중 하나인 유니티 버전업이랑 패키지 업데이트부터 해결해볼까?」와 이전 초안에 단 코멘트 「이참에 에디터 엔진 버전업도 해야 될 거 같아」다(메인 `msg_aa0263abd2f2`).
- 범위 초안: Content `msg_c2ded667928a`(v2). 이전 AI Assistant 단독 초안 `msg_b7d6727d60ab`을 대체한다. 후보의 처음 출처는 아이템 goal의 다음 계획 후보 「Unity 엔진 업그레이드 검토」(`msg_8b8e828ec7e8`)다. 그 절은 PR191 branch(`content-active`)에만 있고 main에는 아직 없다.
- 목표의 기준·상태·결과는 이 파일에 둔다. 로컬 원문·계약·판정은 `.backups/verification/2026-10-07-unity-engine-upgrade/`에 둔다(PR 미포함).

## 범위

| 항목 | 승인된 범위 |
|---|---|
| 만들 것 | 엔진 6000.6.4f1(12bfff696524) 전환과 Unity가 스스로 만든 패키지·설정 변경, AI Assistant 2.20.0-pre.2, 전후 회귀 수치, 변경 파일 전체 분류표, 새 패키지 소스의 상한·신원 정적 확인, MCP 연결 1회 확인, 운영 문서·ADR·팀원 설치 안내 갱신, worktree별 영향과 되돌리기 절차 |
| 건드릴 곳 | `03_Client/ProjectSettings/ProjectVersion.txt`, `03_Client/Packages/manifest.json`·`packages-lock.json`, `03_Client/ProjectSettings/` 아래 엔진이 추가·변경한 값(기계 로컬 값 제외), Unity가 importer 버전 때문에 바꾼 `.meta`가 있으면 분류 뒤 포함, `00_Document/operations/DEVELOPMENT.md` Unity 줄, `00_Document/domains/client.md` 버전 문단, ADR(ADR-001의 「Unity 6.4 LTS」 표기는 사실과 다르다. 6.4는 LTS가 아니었다. 갱신 방식은 ADR 색인 규칙대로 새 ADR 또는 상태 줄), 새 goal 문서와 CURRENT의 Content 줄 |
| 하지 않을 것 | 게임 코드·씬·프리팹·콘텐츠 수정, Force Reserialize Assets, 6.7 Beta·7000 Alpha, 안건 2에서 고르지 않은 패키지, MCP 승인 우회(EditorPrefs 직접 수정·화면 자동 클릭), C:/Dev checkout의 skip-worktree 세 파일, 전역 설정, content-active의 PR191 branch와 로컬 두 파일, `04_ClientNet`·`98_Shared` 변경(csproj 주석의 「Unity 6.4 LTS」 표기는 다음 계획 후보), 병합 실행 |
| 관찰 가능한 완료조건 | (1) ProjectVersion이 6000.6.4f1 (12bfff696524)이고 lock은 Unity가 해석한 결과다(손 편집 없음). (2) 추적 파일 변경 전체가 분류표에 들어간다. 기존 `.meta` 전부의 `guid:` 값이 전후 같고(개수와 불일치 0을 기계 대조) 삭제·신규 `.meta`가 없다. 기계 로컬 값(cloud 3필드 등)은 커밋에 없다. (3) 같은 명령의 EditMode 전체·PlayMode 수치가 기준선(6000.4.7f1, 같은 main) = 엔진 커밋 뒤 = AI Assistant 커밋 뒤다. 실패는 (a)~(d)로 전수 분류한다. (4) 6.6에서 실제 경로 1회: 클라이언트가 로컬 서버(7777)에 붙는 기존 PlayMode 실서버 경로 또는 동등한 실제 진입. (5) 새 패키지 소스에서 상한 검사 부재와 신원 키 방식을 file:line으로 기록한다. (6) 사용자가 Editor를 열고 승인한 뒤 첫 무해 MCP 호출이 상한 거부 없이 성공하고, 컴파일과 Play 진입 뒤 두 번째 호출도 성공한다. 음소거 값은 원래대로 돌린다. (7) relay 실행 파일 hash 전후와 옛 패키지 worktree에서의 영향을 기록한다. (8) 문서·ADR·설치 안내(hash 고정 링크)가 새 버전을 가리킨다. (9) worktree별 영향과 되돌리기 절차가 goal에 있다 |

「안건 2」는 아래 승인된 결정의 2A다. 위 네 항목은 초안 v2의 범위 표를 문구 그대로 옮겼다.

### PR 경계와 점검

PR 하나에 커밋 셋이다(3A). 구조와 동작을 나누는 [하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 5에 맞춰 커밋마다 원인을 구분한다. 이 goal 문서 커밋은 그 앞에 따로 둔다.

1. **엔진 커밋**: ProjectVersion과 Unity가 업그레이드 중 만든 manifest·lock·ProjectSettings 변경. 직후 회귀 수치를 잰다.
2. **AI Assistant 커밋**: manifest 한 줄과 Unity 재해석 lock. 다시 잰다. 2.7.0-pre.3이 6.6에서 컴파일되지 않으면 그 사실을 기록하고 이 커밋 뒤 수치만 비교한다.
3. **문서 커밋**: DEVELOPMENT·client.md·ADR·goal 결과.

신규 Opus 독립 검증과 정확한 head의 CI·원시 근거를 메인에 보낸 뒤, 그 PR 병합 직전에 사용자 명시 승인을 받는다. 병합은 [병합 관문](../../../00_Document/operations/ORCA.md#merge-gate)에 따라 메인만 한다. 같은 산출물 수정이 3회를 넘으면 메인에 체크포인트를 알린다. 확정 실패 집계는 [ORCA 정본](../../../00_Document/operations/ORCA.md#confirmed-failures)을 따로 따른다. 종료 점검은 PR 병합·결과·Gardener 뒤다.

## 승인된 결정

메인 `msg_f9415cd0f5c7`은 「A 승인」을 「다섯 안건을 모두 추천(A)대로 승인」으로 정의했다(메인 정리본 `C:/Dev/DawnHolder_Dashboard/plans/plan-content-c2ded667.md`).

| 안건 | 승인 |
|---|---|
| 1. 엔진 목표 | 6000.6.4f1, revision 12bfff696524, 설치 `unityhub://6000.6.4f1/12bfff696524` |
| 2. 엔진이 강제하지 않는 패키지 | 그대로 둔다. Unity가 업그레이드 중 스스로 바꾼 패키지만 커밋한다 |
| 3. PR 경계 | PR 하나, 커밋 셋(엔진 → AI Assistant → 문서) |
| 4. PR191과의 순서 | 업그레이드를 먼저 병합하고 PR191을 6.6에서 검증·병합한다 |
| 5. 작업 위치 | 최신 main에서 새 worktree 하나. 메인 승인이 R-1 배치 추가를 겸한다 |
| AI Assistant 목표 | 2.20.0-pre.2 |
| 검증 | 강, 독립 검증자 신규 `claude-opus-5-5`, 구현 신규 `gpt-6.1-sol` max, commit은 리드 |

메인 표본 대조(같은 메시지): 레지스트리 `dist-tags.latest` = 2.20.0-pre.2(2026-10-06T14:18:47Z), Unity 페이지 6000.6.4f1 = 12bfff696524(2026-10-01).

일정 조건(같은 메시지):

- 6.6 batch 실행은 메인의 Hub 설치 완료 알림 뒤에 시작한다. 기준선(6000.4.7f1) 측정과 goal·worktree 준비는 먼저 한다.
- 완료조건 (6) MCP 확인은 시트가 1개라 메인이 사용자 Accept 일정을 잡는다. 그 단계 직전에 메인에 알린다.
- C:/Dev checkout의 skip-worktree 세 파일 처리는 병합 직전에 메인이 사용자에게 묻는다.

착수 뒤 메인 결정(모두 메인 전달·운영 판단이며 직접 입력으로 격상하지 않는다):

- 배치 음소거(메인 `msg_6bbec1bd4219`, 2026-10-07T09:03:15Z): 이전 결정 `msg_833c357e684e`과 같은 조건을 이 goal의 모든 Unity batch에 적용한다. 정확한 HKCU `Software\Unity Technologies\Unity Editor 5.x` / `AudioMasterMute_h3604209190`만 batch 전 임시 1, finally 원래 값 복원, 다른 Unity 0일 때만, 이미 1이면 쓰지 않는다. 실행마다 「전 값 → 1 → 복원 값」을 원시로 남긴다. 완료조건 (6)의 대화형 Editor 단계에는 쓰지 않는다.
- 7777(같은 메시지): 메인이 listener 0과 다른 파트의 게임 서버 미사용을 확인하고 Content 작업자에게 소유를 배정했다. 작업자는 시작 전 listener 0·다른 Unity 0을 확인하고 끝나면 해제를 원시로 남긴다.
- 설치 확인(메인 `msg_d503ecc6f2dc`, 2026-10-07T09:07:54Z): 사용자 원문 「일단 지금 새로운 유니티 버전 설치가 다 끝났는데, 확인 가능한지 체크해봐줘」. 메인이 `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe`의 ProductVersion `6000.6.4f1_12bfff696524`를 확인했다. 플랫폼 모듈은 Windows standalone과 Linux standalone이다(Linux는 추가 설치분, 빌드 대상과 무관한 환경 사실). 6.6 batch는 기준선 작업자가 끝나 다른 Unity가 0이 된 뒤 연다.
- 작업자 배치(같은 메시지): `orca terminal split`에 작업 폴더 옵션이 없어, 리드 pane 대신 이 worktree 소속 「Terminal 1」을 split해 작업자를 연다. 소속 worktree와 cwd가 같아 R-1의 cwd만 옮기는 우회를 피한다. 메인이 승인했고 다음 작업자도 같게 연다. 「Terminal 1」은 기준 pane으로 남기고 작업자 정산 때 닫지 않는다.

## 검증 계획

- 등급: 강. 설치·실행 환경 변경이고 모든 Unity 실행과 MCP에 닿는다. 줄 수가 아니라 영역 기준이다.
- R-7 Fable 시범: 비해당. 실패 수명·보호 집합·오류 분류·비용 상한 코드를 바꾸지 않는다.
- 기준선: 업그레이드 전 이 worktree(main `94fc6845`, 6000.4.7f1)에서 EditMode 전체와 PlayMode 전체를 같은 harness로 잰다. PlayMode의 실서버 경로는 이 worktree의 WSL 복사본 서버(127.0.0.1:7777)가 필요하다. 7777과 Unity 실행은 시작 전에 다른 소유자가 없는지 확인한다.
- 각 커밋 뒤 같은 명령으로 다시 잰다. 기존 실패는 [작업 맥락 정본](../../../.agents/skills/dawnholder-task-context/SKILL.md#독립-판정과-통과-차단)의 (a)~(d)로 전수 분류한다.
- 변경 분류: 03_Client 추적 파일 전체 hash 목록과 모든 `.meta`의 `guid:` 값을 업그레이드 전후로 기계 대조한다. 바뀐 파일은 「엔진 버전 / 강제 패키지 / 선택 패키지 / ProjectSettings 엔진 값 / importer 버전 .meta」로 나눈다. 분류 밖 변경은 커밋하지 않고 먼저 보고한다.
- 독립 검증자는 위 원시 근거를 실사하고, 강 등급 필수 항목(독립 테스트 보완·실행, 실제 경로 1회)과 완료조건 (5)·(6)을 확인한다. 제품 파일은 고치지 않고 결함은 번호로 돌려준다.

## 위험과 되돌리기

| 위험 | 확인·대응 |
|---|---|
| `.meta`·GUID·직렬화 값 변화 | 위 검증 계획의 기계 대조와 분류표. Force Reserialize는 하지 않는다 |
| 기계 로컬 값 | Editor를 열면 Unity Cloud 연결 cloud 3필드가 ProjectSettings에 생길 수 있다(content-active 선례, 추정). SENTIS define 한 줄 자동 제거도 반복 관측됐다(PR191 branch의 BACKLOG 후보 `unity-sentis-define-drift`, main 미반영). 둘 다 엔진 변경과 분리해 커밋에서 뺀다. 애매하면 메인에 올린다 |
| 모든 worktree 영향 | 병합 뒤 main을 받은 worktree는 6.6 전용이 된다. 아직 main을 받지 않은 branch는 6000.4.7f1로 연다. 한 머신에 두 Editor가 깔린다. Library가 있는 곳은 C:/Dev checkout과 content-active다(2026-10-07 확인) |
| 메인 checkout C:/Dev | manifest·lock·ProjectSettings 세 파일이 skip-worktree로 숨겨져 있고 로컬 AI Assistant는 2.11.0-pre.1이다. 이 PR이 세 파일을 모두 바꾸므로 병합 뒤 그 checkout의 main 받기가 멈출 수 있다 |
| relay 공유 | relay는 Editor 시작 때 `%USERPROFILE%\.unity\relay\relay_win.exe`에 설치된다(공식 문서). 덮어쓰기 규칙은 문서에 없다. 업그레이드 전 SHA256은 `854f019d6c833f2ca8ab4dcacde31d18765ec1c6c83203d00364347de35ccd66`(수정 시각 2026-05-06)이다 |
| Library 되돌리기 비용 | 6.6이 연 Library는 6.4로 되돌아가지 않는다고 본다(추정). 되돌리면 Library를 지우고 6.4로 다시 만든다 |
| 마감 영향 | 11월 마감 전 엔진 변경이다. 기준선 비교와 실제 경로 1회를 완료조건에 둔 이유다 |

되돌리는 절차:

- 병합 전: 이 worktree와 branch를 버린다. main과 다른 worktree는 바뀌지 않는다. 사용자 홈의 relay 파일만 바뀌었을 수 있어 전후 hash로 확인한다.
- 병합 뒤: 병합 커밋을 `git revert`하는 새 PR(사용자 병합 승인)로 ProjectVersion·manifest·lock·ProjectSettings·문서를 되돌린다. 6.6으로 연 worktree는 Library를 지우고 6000.4.7f1로 다시 열며 MCP 승인도 다시 한다.

## 재개 지점

다음 리드는 이 절부터 읽는다. Run·Task·Dispatch·handle은 기록일 뿐 실행 권한이 아니다.

| 항목 | 값 |
|---|---|
| 작업 공간 / branch | `C:/Users/bass1/orca/workspaces/DawnHolder_Project/unity-upgrade-active` / `chore/unity-engine-upgrade-20261007`(upstream 없음) |
| 기준 | `origin/main` = `94fc68455107c56aee2f5ba5ddffc1f1782de9c0` |
| 리드 Run | `run_dd3bf2daea68`(2026-10-07 Content 리드, `content-active` pane) |
| Unity | 이 worktree의 Library는 6000.4.7f1 기준선 실행으로 생겼다. 6000.6.4f1은 설치됐다(메인 `msg_d503ecc6f2dc`) |
| 작업자 | 기준선 Sol 정산·종료(아래 결과). 살아 있는 작업자 0, 「Terminal 1」은 기준 pane |

다음 순서:

1. 엔진 전환 Sol 계약(6000.6.4f1로 batch 첫 열기, 변경 분류표, 같은 harness로 EditMode·PlayMode 전체) → 결과 정산 → 리드 엔진 커밋.
2. AI Assistant Sol 계약(manifest 한 줄과 Unity 재해석 lock, 같은 측정, 새 패키지 소스 정적 확인) → 정산 → 리드 커밋.
3. 문서 커밋 → 신규 Opus 독립 검증(MCP 확인 직전 메인에 알림) → PR·CI → 메인 승인 요청.
4. 병합 뒤: 결과 기록 → Gardener → 종료 기록 → R-8. 보류 goal(PR191)은 [그 재개 지점](../2026-10-05-items-inventory-currency/goal.md#재개-지점)대로 main을 통합하고 6.6에서 검증한다.

## 진척 단계

- [x] 범위 승인
- [x] 기준선 측정
- [>] 엔진 6.6 전환
- [ ] AI Assistant 올림
- [ ] 문서·ADR 갱신
- [ ] 독립 검증
- [ ] MCP 연결 확인
- [ ] PR 병합

## 실제 결과와 미실행

### 6000.4.7f1 기준선 — 자체 점검, 독립 판정 전

Sol `task_140b8f96e4fd`(Dispatch `ctx_8c2f3e765fff`, 지정·화면 `gpt-6.1-sol max`, backend unknown)이 계약 v1(SHA256 `46593deb…ce98d`)대로 HEAD `8a09417a`에서 실행했다. 보고 `sol-baseline/report.md`(SHA256 `40603ac8…bc2a5`).

| 플랫폼 | total | passed | failed | skipped | Unity exit |
|---|---:|---:|---:|---:|---:|
| EditMode 전체 | 356 | 356 | 0 | 0 | 0 |
| PlayMode 전체 | 11 | 11 | 0 | 0 | 0 |

- 실서버 경로 `MapEntryServerIntegrationTests.ProductionServer_TownHuntingGroundTown_KeyboardPortalsPreserveEntityHpAndRoster`가 Passed였다. 서버는 이 worktree의 WSL 복사본에서 띄우고 자기 서버만 내렸다.
- 기준 목록: 03_Client 추적 파일 2105개, `.meta` guid 1129개(`sol-baseline/inventory/`). 다음 단계의 기계 대조 입력이다.
- 음소거는 두 실행 모두 0 → 1 → 0이다. 실제 음향 출력은 재지 않았다.
- 같은 harness를 다음 단계에서 Unity 경로만 바꿔 쓴다: `harness/Run-UnityTests.ps1` SHA256 `8C7A3869…7B21`, 공통 helper `6C519AFC…2A1AC`.
- 리드 대조(09:35Z): 두 `results.xml`의 test-run 속성(356/356, 11/11, failed 0)과 실서버 테스트 Passed를 원시에서 다시 읽었다. 사후 음소거 값 0, Unity 0, 7777 listener 0(Windows·WSL)을 확인했다. release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 회수 대기 작업자는 0이다.
- Unity가 기준선 실행 중 스스로 바꾼 두 파일: `ProjectSettings.asset`의 Standalone `SENTIS_ANALYTICS_ENABLED;` 제거 한 줄(SHA256 C1412CD2… → 934110CB…, 이전 goal에서 관측된 것과 같은 쌍)과 `MinimapRT.renderTexture` 줄바꿈(Git blob 같음). 작업자는 사본·diff를 남기고 그대로 두었다. 리드가 엔진 전환 전에 둘 다 HEAD 상태로 되돌렸고, 실행 전 hash와 같아졌다(`lead-restore-baseline-drift.txt`). 엔진 전환에서 생기는 변경만 따로 보이게 하려는 것이다.
- 독립 판정은 아직 없다. 신규 Opus 검증의 입력이다.

## 다음 계획 후보

- `04_ClientNet/Dawnholder.Client.Net.csproj`의 「Unity 6.4 LTS」 주석 표기 — 이 goal의 「하지 않을 것」(04_ClientNet 변경 금지). 출처 초안 v2.
- 엔진이 강제하지 않아 그대로 둔 패키지의 새 버전 목록 — 승인 2A. 업그레이드 뒤 실제 목록을 여기에 적는다.
- 6.7 LTS 이동 검토 — 6.7 LTS는 연말 예정(공식 페이지, 날짜 미정). 11월 마감 뒤 판단.
