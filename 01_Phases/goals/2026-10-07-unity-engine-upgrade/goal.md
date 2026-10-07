# Unity 엔진 6.6·AI Assistant 업그레이드

상태: **6000.4.7f1 기준선 측정 완료(EditMode 356/356, PlayMode 11/11, 자체 점검). 6000.6.4f1은 네 번 열었고 모두 컴파일되지 않았다. 원인은 차례로 테스트 한 줄(CS0619), 기존 AI Assistant 2.7.0-pre.3(UAC0005), 서드파티 Lucid Editor 두 파일(CS0619), 같은 Lucid Editor의 다른 한 줄(CS0619)이다. 테스트 한 줄은 고쳤고, AI Assistant 2.20.0-pre.2는 오류 없이 들어왔다. 사용자 결정대로 Lucid Editor 에디터 코드 123개를 지우자 다섯째 열기가 컴파일됐다. 그때 PlayMode는 0/11이었다. 6.6이 강제한 Input System 1.20.0이 임시 입력 설정을 파괴해 테스트 fixture의 저장·복원이 깨진 것으로 추정했다. 사용자 결정대로 그 구간을 고친 뒤 다시 재니 EditMode 356/356, PlayMode 11/11(실서버 경로 통과)로 기준선과 같다(자체 점검). 측정 중 Unity가 지운 기계별 SENTIS define 한 줄은 메인 결정대로 base 값으로 되돌려 커밋에서 뺐다. 신규 Opus 독립 검증은 PASS(차단 결함 0, 비차단 1)이고, 커밋된 tree에서 EditMode 356/356, PlayMode 11/11을 다시 냈다. 사용자 결정대로 검증자 테스트 2개를 업그레이드 커밋에 넣었다. 완료조건 (6) MCP 확인도 PASS다(차단 0, 비차단 1: relay 신원 키가 hash 기반이라 goal relay 줄을 고침). 그때 대화형 Editor가 6.6 형식으로 저장한 설정 두 파일을 사용자 결정대로 업그레이드 커밋에 넣었다. 그 커밋의 약 등급 재검증도 PASS다(EditMode 356/356, PlayMode 13/13, 차단 0). PR204가 사용자 승인으로 병합됐다(2026-10-07T18:01:57Z, merge commit `7086d45b`). 종료 Gardener도 끝났다(정리 후보 2, 채택 전). 종료 기록의 첫 문서 실사는 NOT PASS(차단 2: 리드 맥락 메모 사후 작성, 리드 대조 시각의 수기 기록)였고, 메인 결정대로 정정했다. 종료 기록 PR206을 열었고, 재실사의 NOT PASS(차단 3)도 정정했다. 남은 것은 새 문서 실사, PR206 병합 승인, 종료 점검, R-8이다.** 이어갈 곳은 [재개 지점](#재개-지점)이다.

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

「안건 2」는 아래 승인된 결정의 2A다. 위 네 항목은 초안 v2의 범위 표를 문구 그대로 옮겼다. 착수 뒤 바뀐 것은 일곱이다([승인된 결정](#승인된-결정)).

- 메인 범위 판정 `msg_98f1c822d84e`으로 「건드릴 곳」에 테스트 한 줄이 더해졌다.
- 사용자 결정 A(`msg_0f223740ea22`)로 완료조건 (3)의 「엔진 커밋 뒤 = AI Assistant 커밋 뒤」는 「업그레이드 커밋 뒤」 한 번이 됐다. 「엔진 커밋 뒤」를 잴 수 없는 이유는 [PR 경계](#pr-경계와-점검)에 있다.
- 사용자 결정(`msg_0af85d53aa50`)으로 「건드릴 곳」에 `03_Client/Assets/Art/Environment/Others/Cainos/Third Party/Lucid Editor/Editor/Experimental/`의 `SimpleTreeView.cs`·`TreeMenu.cs`와 각 `.cs.meta`, 네 파일의 삭제가 더해졌다. 완료조건 (2)의 「삭제 `.meta` 없음」은 이 두 `.meta`만 사용자 결정 예외다.
- 사용자 결정(`msg_626eb68b0772`)으로 삭제 범위가 `03_Client/Assets/Art/Environment/Others/Cainos/Third Party/Lucid Editor/Editor/` 아래 에디터 코드 전체와 그것을 상속하는 Cainos 에디터 스크립트로 넓어졌다. 지울 파일은 읽기 전용 관계 점검 표로 정하고, 「건드릴 곳」과 완료조건 (2)의 `.meta` 삭제 예외는 그 표로 갱신한다. Prop 애니메이션·상호작용 런타임 코드와 Lucid Editor Runtime 속성은 지우지 않는다.
- 관계 점검 표와 메인 판단(`msg_ff681cef67de`)으로 지울 파일이 추적 파일 123개로 정해졌다. 이것이 「건드릴 곳」의 삭제 전체이고, 위 Experimental 네 파일도 이 안에 들어간다. 경로 기준은 `03_Client/Assets/Art/Environment/Others/Cainos/`다.
  - `Third Party/Lucid Editor/Editor/` 폴더 전체: `.cs` 56개, 각 `.cs.meta` 56개, 하위 폴더 `.meta` 5개(`Attributes`·`Experimental`·`Extensions`·`InspectorProperty`·`Utils`).
  - 그 폴더의 `Third Party/Lucid Editor/Editor.meta`.
  - `Pixel Art Platformer - Village Props/Script/Editor/`의 `ChestEditor.cs`·`ElevatorEditor.cs`와 각 `.cs.meta`.
  - 그 폴더의 `Pixel Art Platformer - Village Props/Script/Editor.meta`.
  - 완료조건 (2)의 「삭제 `.meta` 없음」은 이 표의 `.meta` 65개(`.cs.meta` 58, 폴더 `.meta` 7)만 사용자 결정 예외다. guid 목록은 관계표(`sol-relations/relations.json`)에 있다. 그 밖의 삭제·신규 `.meta`와 guid 불일치는 여전히 0이어야 한다.
  - 지운 뒤 빈 폴더 7개도 디스크에서 지운다. 폴더가 남으면 Unity가 새 guid로 폴더 `.meta`를 만들기 때문이다.
- 사용자 결정 A(`msg_4aa892767efc`)로 「건드릴 곳」에 `03_Client/Assets/Tests/PlayMode/MapEntryPlayFixture.cs`의 입력 설정 저장·복원 구간(현재 160~161·182~189·356·359행)이 더해졌다. 단언과 다른 테스트 동작은 바꾸지 않는다.
- 사용자 결정 1A(`msg_3dc8c0c52851`)로 「건드릴 곳」에 `03_Client/Assets/Tests/PlayMode/MapEntrySceneLifecycleTests.cs`가 더해졌다. 독립 검증자가 더한 fixture 회귀 테스트 2개(+64줄)만이다. 기존 테스트와 단언은 바꾸지 않고, 새 파일·새 `.meta`는 없다.
- 사용자 결정 A(`msg_3d959b3798a7`)로 MCP 확인 중 대화형 Editor가 6.6 형식으로 바꾼 `03_Client/ProjectSettings/PackageManagerSettings.asset`·`QualitySettings.asset`을 업그레이드 커밋에 넣었다. 둘 다 「건드릴 곳」의 「엔진이 추가·변경한 값」에 들어가므로 범위는 그대로다. `PackageManagerSettings.asset`의 entity id 두 값은 Editor 실행 중에 매겨지는 객체 핸들로 본다(필드 타입 근거의 추론). base의 옛 값 `-942/-944`는 git 이력에서 한 번도 바뀌지 않았다. 필드 이름이 바뀌어 6.6 형식으로 넣으려면 어떤 값이든 하나는 들어가야 하므로, 손으로 고치지 않고 그대로 넣었다.

### PR 경계와 점검

PR 하나에 커밋 둘이다. 처음 승인은 커밋 셋(3A)이었고, 사용자 결정 A(`msg_0f223740ea22`)로 엔진 커밋과 AI Assistant 커밋을 합쳤다. 구조와 동작을 나누는 [하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 5에 맞춰 커밋마다 원인을 구분한다. 이 goal 문서 커밋은 그 앞에 따로 둔다.

1. **업그레이드 커밋**: 테스트 한 줄, PlayMode fixture의 입력 설정 저장·복원 수정, 독립 검증자의 fixture 회귀 테스트 2개, manifest의 AI Assistant 한 줄(2.20.0-pre.2), 관계 점검 표에 따른 Lucid Editor 에디터 코드와 `.meta` 삭제, 그 상태로 6.6이 열기와 측정에서 만든 변경. 그 변경은 ProjectVersion·manifest·lock·ProjectSettings·URP 전역 설정과 새 설정 asset 둘이다. MCP 확인 때 대화형 Editor 열기·종료가 6.6 형식으로 저장한 설정 두 파일도 넣는다(사용자 결정 `msg_3d959b3798a7`). 분류표 기준으로 넣고, 줄 끝만 바뀐 파일과 기계 로컬 값(SENTIS define 등, [위험과 되돌리기](#위험과-되돌리기))은 뺀다. 직후 회귀 수치를 잰다. 분류표는 lock 항목마다 「엔진 강제」와 「AI Assistant 의존」을 나눈다.
2. **문서 커밋**: DEVELOPMENT·client.md·ADR·goal 결과.

「엔진 커밋 뒤」를 잴 수 없는 이유: 2.7.0-pre.3은 6.6 분석기 오류(UAC0005)로 컴파일되지 않는다. 컴파일이 실패하면 Unity는 ProjectVersion을 6000.4.7f1로 둔 채 끝나, 엔진 전환만 담은 커밋을 Unity 생성 결과로 만들 수 없다. 두 커밋으로 나누려면 한 번에 해석되는 lock을 손으로 쪼개야 하는데, 이는 완료조건 (1)에 어긋난다. 이 이유는 PR 본문에도 적는다.

신규 Opus 독립 검증과 정확한 head의 CI·원시 근거를 메인에 보낸 뒤, 그 PR 병합 직전에 사용자 명시 승인을 받는다. 병합은 [병합 관문](../../../00_Document/operations/ORCA.md#merge-gate)에 따라 메인만 한다. 같은 산출물 수정이 3회를 넘으면 메인에 체크포인트를 알린다. 확정 실패 집계는 [ORCA 정본](../../../00_Document/operations/ORCA.md#confirmed-failures)을 따로 따른다. 종료 점검은 PR 병합·결과·Gardener 뒤다.

## 승인된 결정

메인 `msg_f9415cd0f5c7`은 「A 승인」을 「다섯 안건을 모두 추천(A)대로 승인」으로 정의했다(메인 정리본 `C:/Dev/DawnHolder_Dashboard/plans/plan-content-c2ded667.md`).

| 안건 | 승인 |
|---|---|
| 1. 엔진 목표 | 6000.6.4f1, revision 12bfff696524, 설치 `unityhub://6000.6.4f1/12bfff696524` |
| 2. 엔진이 강제하지 않는 패키지 | 그대로 둔다. Unity가 업그레이드 중 스스로 바꾼 패키지만 커밋한다 |
| 3. PR 경계 | PR 하나, 커밋 셋(엔진 → AI Assistant → 문서). 착수 뒤 사용자 결정 A로 커밋 둘(업그레이드 → 문서)로 바뀌었다(아래) |
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
- 작업자 배치(같은 메시지, **사용자 결정 `msg_02668751092e`으로 대체**, 아래): `orca terminal split`에 작업 폴더 옵션이 없어, 리드 pane 대신 이 worktree 소속 「Terminal 1」을 split해 작업자를 연다. 소속 worktree와 cwd가 같아 R-1의 cwd만 옮기는 우회를 피한다. 메인이 승인했고 다음 작업자도 같게 연다. 「Terminal 1」은 기준 pane으로 남기고 작업자 정산 때 닫지 않는다.
- 테스트 한 줄 범위 판정(메인 `msg_98f1c822d84e`, 2026-10-07T09:49:48Z, 메인 범위 판정이며 사용자 결정 아님): 6000.6.4f1 첫 열기가 `03_Client/Assets/Tests/EditMode/ComponentNullCharacterizationTests.cs`(176,62)의 CS0619 `Object.GetInstanceID()`(「Use GetEntityId instead」)로 컴파일되지 않았다. 메인은 이를 승인된 엔진 전환이 만든 범위 안 결함으로 보고 「건드릴 곳」에 그 한 줄(instance id 기록)을 더했다. 같은 파일의 다른 줄과 다른 테스트 파일은 밖이고, 다른 파일의 컴파일 오류가 새로 나오면 고치기 전에 다시 묻는다. 신규 Opus 테스트 작성자가 그 한 줄만 고치고, 기록 이름 변경(instanceId → entityId)이 기록 행을 읽는 다른 곳에 닿는지 grep으로 확인한다. 엔진 커밋에 Unity 생성 변경과 이 한 줄을 함께 묶는 것도 승인됐다(커밋 셋 구조 유지). 독립 검증자는 이 줄이 엔진 커밋에 들어간 이유와 단언 불변을 판정한다. 이 판정은 첫 열기 보고가 UAC0005를 빠뜨린 상태에서 내려졌다(아래 [교정 기록](#교정-기록)).

착수 뒤 사용자 결정:

- 엔진·AI Assistant 커밋 합침(메인 `msg_0f223740ea22`, 2026-10-07T10:34:26Z, 리드 결정 요청 `msg_d560680ed27c`): 사용자 원문은 메인 창에서 Enter로 제출된 「에이.합칩으로 가자」다. 메인이 「에이」를 A로, 「합칩」을 「합침」의 오타로 읽었다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 승인 안건 3을 바꾼다. PR 하나에 커밋 둘이다: 업그레이드 커밋(엔진 전환 + AI Assistant 2.20.0-pre.2 + 테스트 두 줄) → 문서 커밋.
  - 다음 Sol이 manifest의 `com.unity.ai.assistant` 줄만 2.20.0-pre.2로 바꾸고 6.6으로 한 번 연다. lock은 Unity 해석 결과 그대로 두고, 분류표에 패키지별로 「엔진 강제」와 「AI Assistant 의존」을 나눠 적는다.
  - 측정은 기준선(6000.4.7f1) 대 업그레이드 커밋 뒤 한 번이다. 「엔진 커밋 뒤」 측정 불가 사유를 goal과 PR 본문에 적는다.
  - 2.20.0-pre.2도 6.6에서 컴파일되지 않거나, 테스트 한 줄·AI Assistant 밖의 다른 컴파일 오류가 나오면 고치기 전에 다시 묻는다.
  - 리드 교정(로그의 error 줄 전체를 진단 코드 구분 없이 뽑아 보고와 개수 대조)을 다음 Sol 계약과 리드 원시 대조에 바로 적용한다.
- Lucid Editor 두 파일 삭제(메인 `msg_0af85d53aa50`, 2026-10-07T11:17:26Z, 리드 결정 요청 `msg_1f8ec97c7b67`): 리드는 A(10곳에 `<int>`)와 B(업그레이드 중단)를 올렸다. 사용자 원문은 메인 창에서 Enter로 제출된 두 문장이다. 첫째는 「음 사실 아트파일만 필요하고, 솔직히 코드는 필요 없긴한데」다. 둘째는 메인이 다시 낸 선택지(A 막힌 두 파일만 삭제, B 10곳 고침)에 대한 답 「오케이, 어차피 업데이트 할 이유도 없을거고, 그리고 어차피 에디터 코드 쓸 이유도 없으니, 막힌 두 파일만 삭제하자」다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 지울 것은 `03_Client/Assets/Art/Environment/Others/Cainos/Third Party/Lucid Editor/Editor/Experimental/`의 `SimpleTreeView.cs`·`SimpleTreeView.cs.meta`·`TreeMenu.cs`·`TreeMenu.cs.meta` 네 파일뿐이다. 같은 폴더의 `TextFieldPopup`·`Toolbar`, Lucid Editor의 다른 파일, Cainos 스크립트는 그대로 둔다.
  - 「건드릴 곳」에 네 파일 삭제를 더하고, 완료조건 (2)의 「삭제 `.meta` 없음」은 이 두 `.meta`만 예외로 둔다. 업그레이드 커밋에 함께 넣는다.
  - 삭제는 생산 코드 라우팅대로 새 Sol이 하고, 6.6으로 다시 열어 측정까지 간다. error 줄 전수 대조는 그대로다. 또 다른 파일 오류가 나오면 고치기 전에 다시 묻는다.
  - 메인 확인 사실: 두 파일은 서로만 참조한다. Chest·Elevator 등 Cainos 스크립트는 Lucid Editor Runtime 속성을 쓰므로 Lucid Editor 전체 삭제는 안 된다. Chest·Elevator·MovingPlatform을 쓰는 prefab·scene은 Cainos 폴더 안에만 있고 바깥 참조는 0이다(GUID grep).
  - 서드파티 재가져오기 때 이 삭제가 덮어써지는 위험은 PR 본문에 사실로만 적는다(사용자 원문 「어차피 업데이트 할 이유도 없을거고」).
- Lucid Editor 에디터 코드 삭제와 관계 점검(메인 `msg_626eb68b0772`, 2026-10-07T11:58:54Z, 리드 결정 요청 `msg_891379abb77e`): 리드는 A(`SerializeReferenceDropdown.cs` 한 줄 `children` → `childList`)와 B(에디터 코드 통째 삭제)를 올렸다. 사용자 원문은 메인 창에서 Enter로 제출된 대시보드 결정 응답의 3번이다. 「B로 가는게 맞을 듯, 애초에 에디터 자체를 안쓰는데, 근데 문제가, 기존에 코드중에 Prop들 에니메이션이나 상호작용이 같이 얽혀있는 코드가 있는데, 관계성을 하나씩 다 체크하고 지울 거 지워야 할 거 같음, 근데 어차피 기존 에셋을 쓰는거보다 나중에 우리가 새로 sprite 에셋이나 애니메이션을 새로 우리 게임 아트스타일로 재 창조해야할거야, 그때 Design 워크트리가 또 필요할거임」. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 메인 해석: 방향은 B다. Lucid Editor의 Editor 폴더 코드와 그것을 상속하는 Cainos 에디터 스크립트(ChestEditor·ElevatorEditor)를 지운다. Prop 애니메이션·상호작용 런타임 코드(Chest·Elevator·MovingPlatform·Lucid Runtime 속성 등)는 지우지 않는다.
  - 절차: 새 Sol이 읽기 전용으로 지울 후보와 파일마다 근거 표를 만든다. 근거는 (a) 런타임 코드(Editor 폴더 밖 `.cs`) 참조 0, (b) prefab·scene·asset의 m_Script GUID 참조 0, (c) 우리 게임 코드(Cainos 밖) 참조 0, (d) 남는 에디터 코드가 참조하지 않거나 함께 지워짐이다. Editor 폴더 밖 파일이 후보에 들어가거나 (a)~(c) 중 하나라도 0이 아니면 지우지 말고 묻는다.
  - 리드가 표를 원시로 표본 대조해 메인에 보내고, 메인이 표본 대조한 뒤 진행을 알린다. 사용자 재확인은 런타임 의존이 나올 때만이다. 그 뒤 새 Sol이 표의 파일과 각 `.meta`만 지우고(앞서 지운 Experimental 두 파일 포함) 6.6으로 다시 열어 잰다. 커밋 둘 구조는 그대로다.
- 관계표의 「묻기」 4개 처리(메인 `msg_ff681cef67de`, 2026-10-07T12:42:31Z, 리드 요청 `msg_5d06f372b151`): 메인 결정이며 사용자 결정이 아니다. 메인은 사용자에게 보고했다. 위 절차의 「사용자 재확인은 런타임 의존이 나올 때만」에 따른 판단이다.
  - 회신 원문: 「제외하고 진행」. Temp 사본은 「후보를 참조하는 다른 코드가 아니라 updater가 만든 후보 자신의 수정본이라 판정 범위에서 뺀다. 4개는 「지움」이다.」
  - 대상 4개: `EditorIcons.cs`·`SerializeReferenceDropdown.cs`와 각 `.meta`. 판정을 「묻기」로 만든 것은 `03_Client/Temp/ScriptUpdater/` 아래 사본 하나다. 그 사본은 Git 제외 경로이고, 앞선 측정 열기 때 6.6 API updater가 만들었다.
  - 그래서 표의 123개 전부가 「지움」이다.
  - 삭제·측정 계약의 Temp 대책 조건(메인 동의 원문): 「Unity Editor가 그 worktree에서 꺼진 상태를 먼저 확인하고 03_Client/Temp를 비운다(비우기 전후 원시). 열기 뒤 삭제 경로 123개 재출현 0을 확인한다. Library는 건드리지 않는다.」
  - 다른 오류가 나오면 고치기 전에 묻는다.
- PlayMode fixture 수정(메인 `msg_4aa892767efc`, 2026-10-07T13:50:48Z, 리드 결정 요청 `msg_8703e1ea06ad`): 사용자 원문은 메인 창에서 Enter로 제출된 「대시보드 결정 응답: 1) Unity 6.6 - PlayMode 0/11을 고치려고 테스트 fixture 수정을 업그레이드 범위에 넣을지 → A 범위에 넣고 고침」이다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 신규 Opus 테스트 작성자가 `MapEntryPlayFixture.cs`의 입력 설정 저장·복원 구간만 고친다. 단언과 다른 테스트 동작은 바꾸지 않는다.
  - 작성자는 고친 방식이 1.20.0 동작(임시 설정 즉시 파괴)에 기대는지, 1.19.0으로 돌아가도 안전한지를 보고에 한 줄로 남긴다.
  - 그 뒤 새 Sol이 같은 작업 트리에서 EditMode·서버 lane·PlayMode를 다시 잰다.
  - 원인 추정(첫 테스트 TearDown, 나머지 10개 SetUp)이 맞는지는 재측정 뒤 독립 검증이 원시로 판정한다.
  - 커밋 전 독립 검증 항목에 둘을 둔다. 하나는 `ProjectSettings.asset` 57행 `m_StackTraceTypes` 끝의 `i`, 다른 하나는 relay 교체의 영향이다. MCP Accept 단계 전에는 메인에 미리 알린다(시트 1개).
- 작업자 배치 바꿈(메인 `msg_02668751092e`, 2026-10-07T14:23:02Z): 사용자 원문은 메인 창에서 Enter로 제출된 「음 아마 우리 규칙에서 문장상 모호한게 있었나보네, 창 하나를 띄울때는 굳이 Pane을 하나 더 만들어서 Agent를 할당할 필요는 없어」다. 사용자가 이 worktree 탭의 빈 기준 pane과 Sol pane을 보고 물었다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 위 「작업자 배치」 승인을 대신한다. 다음 작업자는 기준 pane을 split하지 않고 이 worktree에 바로 연다. 명령은 `orca terminal create --worktree path:<이 worktree> --title "<역할> <작업>" --command "<R-5 최초 실행 명령>" --json`이다. 그 handle로 R-5 3~4단계(tui-idle, R-6 첫 화면, worker-start)를 한다. create 결과의 worktree·cwd를 기록한다.
  - 기준 pane(`term_31d75e83`)은 재측정 Sol을 정산하고 그 pane을 닫은 뒤, 대기 화면을 확인하고 닫았다.
  - terminal create가 이 worktree에 붙지 않거나 실패하면 우회하지 않고 원문을 보존해 메인에 알린다.

- 검증자 테스트와 MCP 확인 시점(메인 `msg_3dc8c0c52851`, 2026-10-07T16:02:46Z, 리드 결정 요청 `msg_2ad10e8364fc`): 사용자 원문은 메인 창에서 Enter로 제출된 「대시보드 결정 응답: 1) Unity 6.6 - 검증자가 더한 fixture 회귀 테스트 2개를 업그레이드 커밋에 넣을지 → A 넣음 · 2) Unity 6.6 - MCP 확인(시트 1개, 네가 Hub로 열고 Accept)을 지금 할지 다음에 할지 → A 지금」이다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 1A: 검증자 테스트 2개를 커밋 재구성 때 결함 #1 메시지 수정과 함께 업그레이드 커밋에 넣는다. 「건드릴 곳」에 그 파일을 더했다(위 범위 절).
  - 2A: 메인이 시트 보유를 현황판에 「Content MCP 검증자」로 적었다. 리드가 계약을 고정하고 `orca terminal create`로 MCP 검증자를 연다. 사용자가 Hub로 열 차례가 되면 메인에 status 한 통(무엇을 열지, Accept 위치, 기다리는 화면)을 보낸다. 사용자 손 단계는 그 메일로만 요청한다.
  - 첫 무해 호출 뒤 「Connection revoked」가 나오면 우회하지 않고 알린다. 시트 활성화 뒤에도 Project Settings > AI > Unity MCP에서 다시 승인해야 풀린 선례가 있다(메인 전달).
  - 순서: 커밋 재구성(#1·테스트·goal) → MCP 확인 → push·PR·CI → 병합 승인 요청.
- 대화형 설정 두 파일(메인 `msg_3d959b3798a7`, 2026-10-07T16:49:37Z, 리드 결정 요청 `msg_979515280783`): 사용자 원문은 메인 창에서 Enter로 제출된 「대시보드 결정 응답: 1) Unity 6.6 - 대화형 Editor가 바꾼 설정 파일 2개를 업그레이드 커밋에 넣을지 → A 넣음」이다. 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 두 파일을 업그레이드 커밋에 합친다. 신규 Opus가 약 등급으로 재검증한다. 두 파일 실사와 EditMode·PlayMode 전체 재실행을 하고, MCP는 다시 확인하지 않는다.
  - 그다음 goal 갱신(비차단 #1 relay 신원 키 정정 포함), push, PR, CI, 병합 승인 요청 순서다.
  - 같은 메시지의 대체 조건: 재검증자가 Opus 5.5 safeguard 선택창에 막히면 고르지 않는다. 막힌 세션을 정산·종료한 뒤 같은 계약으로 신규 `gpt-6-astra` xhigh 검증자로 바꾸고 메인에 status를 보낸다. 사용자 원문은 「만약 또 SafeGuard로 막히면 Astra로 대체해서 진행해줘」(메인 창 Enter 제출, 메인 전달)다.

착수 뒤 메인 결정(이어서):

- 원격 반영 시점(메인 `msg_a9234fbe1251`, 2026-10-07T14:43:08Z, 리드 순서 보고 `msg_f78b6a1533cf`): 「순서에 이견 없다. 커밋 둘은 독립 검증이 끝날 때까지 로컬에 두고, 그 뒤 원격에 올려라.」 리드가 보고에 든 이유는 독립 검증 결과를 두 커밋에 반영할 수 있게 하려는 것이다.
- SENTIS define 한 줄(메인 `msg_7146de24b2de`, 2026-10-07T14:59:47Z, 리드 결정 요청 `msg_a5a934c8efd3`): 메인 결정이며 사용자 결정이 아니다(메인이 사용자에게 보고함). 회신 원문은 「**A — 그 한 줄을 base 값으로 되돌려 커밋에서 뺀다.**」다.
  - 근거 원문: 「승인된 goal 위험표 129행이 이미 정했다: 「SENTIS define 한 줄 자동 제거도 … 둘 다 엔진 변경과 분리해 커밋에서 뺀다」. A는 범위 변경이 아니라 그 기준의 적용이라 사용자 결정이 따로 필요 없다.」
  - 진행 원문: 「제품 파일 편집은 원래 Sol 몫이다. 리드가 직접 고치려던 편집이 권한 분류기에 막힌 것은 맞는 결과다. 우회하지 말고 신규 Sol(…)에 그 한 줄만 맡겨라. Unity는 실행하지 않는다.」
  - 같은 메시지: 리드 직접 편집 시도를 커밋 구성 누락과 함께 [교정 기록](#교정-기록)에 첫 발생으로 적는다. 166행 `m_ColorGamuts` 끝 `i`도 57행과 함께 독립 검증 항목에 넣는다. MCP 단계는 판정 뒤 별도 신규 Opus로 하고, 열기 전에 시트·사용자 Accept 일정을 요청한다. decision_gate는 Dispatch 없는 발신자라 거절되니 결정 요청은 status나 question으로 보낸다.

## 검증 계획

- 등급: 강. 설치·실행 환경 변경이고 모든 Unity 실행과 MCP에 닿는다. 줄 수가 아니라 영역 기준이다.
- R-7 Fable 시범: 비해당. 실패 수명·보호 집합·오류 분류·비용 상한 코드를 바꾸지 않는다.
- 기준선: 업그레이드 전 이 worktree(main `94fc6845`, 6000.4.7f1)에서 EditMode 전체와 PlayMode 전체를 같은 harness로 잰다. PlayMode의 실서버 경로는 이 worktree의 WSL 복사본 서버(127.0.0.1:7777)가 필요하다. 7777과 Unity 실행은 시작 전에 다른 소유자가 없는지 확인한다.
- 업그레이드 커밋 뒤 같은 명령으로 다시 잰다(사용자 결정 A, 「엔진 커밋 뒤」는 잴 수 없음). 기존 실패는 [작업 맥락 정본](../../../.agents/skills/dawnholder-task-context/SKILL.md#독립-판정과-통과-차단)의 (a)~(d)로 전수 분류한다.
- 변경 분류: 03_Client 추적 파일 전체 hash 목록과 모든 `.meta`의 `guid:` 값을 업그레이드 전후로 기계 대조한다. 바뀐 파일은 「엔진 버전 / 강제 패키지 / 선택 패키지 / ProjectSettings 엔진 값 / importer 버전 .meta」로 나눈다. 분류 밖 변경은 커밋하지 않고 먼저 보고한다.
- 독립 검증자는 위 원시 근거를 실사하고, 강 등급 필수 항목(독립 테스트 보완·실행, 실제 경로 1회)과 완료조건 (5)·(6)을 확인한다. 제품 파일은 고치지 않고 결함은 번호로 돌려준다.

## 위험과 되돌리기

| 위험 | 확인·대응 |
|---|---|
| `.meta`·GUID·직렬화 값 변화 | 위 검증 계획의 기계 대조와 분류표. Force Reserialize는 하지 않는다 |
| 기계 로컬 값 | Editor를 열면 Unity Cloud 연결 cloud 3필드가 ProjectSettings에 생길 수 있다(content-active 선례, 추정). SENTIS define 한 줄 자동 제거도 반복 관측됐다(PR191 branch의 BACKLOG 후보 `unity-sentis-define-drift`, main 미반영). 둘 다 엔진 변경과 분리해 커밋에서 뺀다. 애매하면 메인에 올린다 |
| 모든 worktree 영향 | 병합 뒤 main을 받은 worktree는 6.6 전용이 된다. 아직 main을 받지 않은 branch는 6000.4.7f1로 연다. 한 머신에 두 Editor가 깔린다. Library가 있는 곳은 C:/Dev checkout과 content-active다(2026-10-07 확인) |
| 메인 checkout C:/Dev | manifest·lock·ProjectSettings 세 파일이 skip-worktree로 숨겨져 있고 로컬 AI Assistant는 2.11.0-pre.1이다. 이 PR이 세 파일을 모두 바꾸므로 병합 뒤 그 checkout의 main 받기가 멈출 수 있다 |
| relay 공유 | relay는 Editor 시작 때 `%USERPROFILE%\.unity\relay\relay_win.exe`에 설치된다(공식 문서). 덮어쓰기 규칙은 공식 문서에 없고, 아래 (2)처럼 패키지 코드에서 확인했다. 업그레이드 전 SHA256은 `854f019d6c833f2ca8ab4dcacde31d18765ec1c6c83203d00364347de35ccd66`(수정 시각 2026-05-06)이다. 컴파일이 처음 통과한 다섯째 열기 뒤 `0cd32f102a64fc8585671ec3ce82a78046011452a2b6d5f655d90e26369e014b`(100,741,544 bytes, 수정 시각 2026-10-06 23:18 KST)로 바뀌었다. 옛 패키지 worktree(content-active 2.7.0-pre.3, C:/Dev 2.11.0-pre.1)도 이제 이 relay를 쓴다. 독립 검증(V11, 정적·hash)이 다음을 확인했다. (1) 현재 파일은 2.20.0-pre.2 번들 `RelayApp~/relay_win.exe`와 hash·크기·시각이 같아, 그 패키지가 덮어쓴 것이 맞다. (2) 세 버전 공통 `ServerInstaller.cs`는 Editor 세션마다 한 번, 번들 버전이 설치본 `relay --version`보다 새로울 때만 복사한다. 옛 두 패키지의 번들은 `1.0.12-build.96`, 2.20.0-pre.2는 `build.99`라 옛 패키지는 지금 relay를 덮어쓰지 않는다. (3) 현재 relay는 Windows에서 Authenticode Valid(Unity Technologies SF)다. 독립 검증은 이 값을 근거로 「신원 키가 경로+publisher라 relay 교체만으로 재승인은 필요 없을 것」이라고 추론했다. 그러나 MCP 확인에서 Editor는 relay 서명을 유효로 읽지 않았다(`SignatureValid: 0`, 서명자를 NAVER 루트로 읽음, 원인 미확인). 그래서 신원 키를 hash 기반(`Hash:0cd32f10…`)으로 저장했다. 따라서 relay 실행 파일이 바뀌면 키도 바뀌어 재승인이 필요할 수 있다. `claude.exe`도 서명이 없어 hash 키라 Claude Code 업데이트 때도 같다(둘 다 재승인 동작은 미관측, 근거 `opus-mcp/verdict.md` 결함 #1). (4) `relay --version`이 5초 안에 답하지 못하면 설치본을 `0.0.0`으로 보고 옛 패키지가 되돌려 쓸 수 있다(실행하지 않아 미확인). 근거 `opus-verify/verdict.md` 「V11 근거」 |
| Library 되돌리기 비용 | 6.6이 연 Library는 6.4로 되돌아가지 않는다고 본다(추정). 되돌리면 Library를 지우고 6.4로 다시 만든다 |
| 마감 영향 | 11월 마감 전 엔진 변경이다. 기준선 비교와 실제 경로 1회를 완료조건에 둔 이유다 |

되돌리는 절차:

- 병합 전: 이 worktree와 branch를 버린다. main과 다른 worktree는 바뀌지 않는다. 사용자 홈의 relay 파일만 바뀌었을 수 있어 전후 hash로 확인한다.
- 병합 뒤: 병합 커밋을 `git revert`하는 새 PR(사용자 병합 승인)로 ProjectVersion·manifest·lock·ProjectSettings·문서를 되돌린다. 6.6으로 연 worktree는 Library를 지우고 6000.4.7f1로 다시 열며 MCP 승인도 다시 한다.

## 재개 지점

다음 리드는 이 절부터 읽는다. Run·Task·Dispatch·handle은 기록일 뿐 실행 권한이 아니다.

| 항목 | 값 |
|---|---|
| 작업 공간 / branch | `C:/Users/bass1/orca/workspaces/DawnHolder_Project/unity-upgrade-active` / 종료 기록 branch `docs/unity-upgrade-closeout-20261008`. 제품 branch `chore/unity-engine-upgrade-20261007`는 PR204로 병합됐고 원격에서 지워졌다 |
| 기준 | 제품 PR의 처음 base는 `94fc68455107c56aee2f5ba5ddffc1f1782de9c0`이다. 종료 기록 branch의 base는 PR204 병합 뒤 main `7086d45b92b76742d8c11facbf8a1031b81d0853`이다 |
| 리드 Run | `run_dd3bf2daea68`(2026-10-07 Content 리드, `content-active` pane). 기록일 뿐이다. 사용자가 밤에 리드 세션을 모두 닫기로 해서(메인 `msg_28b3df556ef8`) 이 pane은 메인이 닫는다. 다시 열 때는 새 리드가 새 Run으로 시작한다([RESUME](../../../00_Document/operations/RESUME.md)) |
| Unity | 이 worktree의 Library는 6000.4.7f1 기준선 실행으로 생겼다. 그 뒤 6000.6.4f1 열기를 다섯 번 거쳤고, 앞의 넷은 컴파일 실패였다. 앞의 네 열기가 남긴 부분 변경은 리드가 HEAD로 정리했다(`lead-restore-*.txt`). 다섯째 열기(Lucid 123개 삭제 뒤)부터 컴파일된다. 이어진 측정과 fixture 수정 뒤 재측정도 이 Library에서 했다. 지금 Unity는 0이다 |
| 작업자 | 기준선 Sol, 엔진 첫 열기 Sol, 테스트 작성 Opus, 재열기 Sol, 업그레이드 열기 Sol, 측정 Sol, 관계 점검 Sol, 삭제·측정 Sol, fixture 작성 Opus, 재측정 Sol, SENTIS 한 줄 Sol, 독립 검증 Opus, MCP 확인 Opus, 재검증 Opus, 종료 Gardener Opus, 종료 기록 문서 실사 `gpt-6-astra` 둘 정산·종료(아래 결과). 이 goal 갱신 뒤의 실사자도 실사마다 새로 열고 정산·종료한다. 밤에 닫을 때 살아 있는 작업자는 0이고, 확인 값은 메인에 보내는 CLOSE-READY에 적는다. 기준 pane은 사용자 결정(`msg_02668751092e`)대로 닫았고, SENTIS Sol부터 `orca terminal create`로 이 worktree에 바로 열었다 |
| commit | PR204로 main에 들어갔다(아래 「PR204 병합」). 측정 때 쓴 로컬 checkpoint 셋(테스트 두 줄, 다섯째 열기 상태, fixture 수정), SENTIS 한 줄 되돌림, 검증자 테스트 2개, 대화형 설정 두 파일은 업그레이드 커밋 `62228618` 하나로 합쳤다. `62228618`의 tree는 재검증 대상 `71c98065`와 같고 메시지 한 문장만 다르다. 앞선 판정의 대상 `bebbb4e0`·`0a574c77`·`71c98065`·`ba513147`은 이 worktree의 로컬 객체로만 남아 있다 |
| 열린 PR | 종료 기록 [PR206](https://github.com/bass131/dawnholder-server/pull/206). head는 이 goal 갱신 commit이고, 정확한 40자는 병합 승인 요청에 적는다. 병합 전이다 |
| 로컬에만 둔 근거 | `.backups/verification/2026-10-07-unity-engine-upgrade/`(Git 제외)에 계약·판정 원문·harness가 있다. 이 worktree는 PR206 병합 뒤 지울 대상이고, 지우는 것은 사용자 확인 뒤다. 지우기 전에 이 근거 폴더를 보존 위치로 옮긴다. 보류 goal(PR191)의 6.6 batch도 이 harness를 출발점으로 쓴다 |

다음 순서:

1. 관계 점검(Sol 표 → 리드 표본 대조 → 메인 표본 대조와 진행 알림)은 끝났다(아래 결과, 메인 `msg_ff681cef67de`).
2. 삭제·측정 Sol은 끝났다(아래 결과). PlayMode 0/11이었다.
3. 사용자 결정 A(`msg_4aa892767efc`)에 따른 fixture 수정·재측정과 업그레이드 커밋은 끝났다(아래 결과).
4. 첫 문서 커밋 뒤 업그레이드 커밋에 SENTIS define drift가 섞인 것을 찾았다. 메인 결정 A(`msg_7146de24b2de`)대로 Sol이 그 한 줄을 되돌렸고, 리드가 업그레이드 커밋을 다시 만들었다(아래 결과).
5. 문서 커밋은 이 goal 갱신과 함께 만들었다(DEVELOPMENT Unity 줄과 hash 고정 설치 링크, client.md 버전 문단, ADR-001 상태 줄과 갱신 문단).
6. 신규 Opus 독립 검증은 PASS(비차단 결함 1)로 끝났다(아래 결과). 사용자 결정 1A·2A(`msg_3dc8c0c52851`)로 검증자 테스트 2개를 커밋에 넣고 MCP 확인은 지금 한다.
7. 커밋 재구성은 이 goal 갱신과 함께 했다. 결함 #1 메시지를 고치고 검증자 테스트를 업그레이드 커밋에 넣었다.
8. 완료조건 (6) MCP 확인은 PASS로 끝났다(아래 결과). 사용자 결정 A(`msg_3d959b3798a7`)로 대화형 설정 두 파일을 업그레이드 커밋에 넣고, 문서 커밋을 이 goal 갱신과 함께 다시 만들었다.
9. 두 파일을 더한 커밋의 약 등급 재검증은 PASS(비차단 결함 1)로 끝났다(아래 결과). 결함 #1의 문구를 커밋 메시지와 이 goal에서 고치고 두 커밋을 다시 만들었다.
10. push·PR204·CI·메인 승인 요청·병합은 끝났다(아래 「PR204 병합」).
11. 결과 기록(`f1f9cc73`)과 종료 Gardener는 끝났다(아래 「종료 Gardener」). 정리 후보 둘은 채택 전이다.
12. 종료 기록 문서 실사는 NOT PASS(차단 2)였다. 메인 결정 A(`msg_b8151750a5df`)대로 정정했고(`2a6e5893`), 종료 기록 PR206을 열었다. 재실사도 NOT PASS(차단 3)라서 이 goal 갱신으로 다시 정정했다(아래 「종료 기록 문서 실사」).
13. 다음 순서(기록 2026-10-07T19:06:04Z, `date -u` 출력): 이 goal 갱신 commit의 새 독립 문서 실사(신규 `gpt-6-astra` xhigh) → PASS면 PR206 CI 확인과 병합 승인 요청(status) → 사용자 승인과 메인 병합 → 종료 점검 → R-8. 마지막 실사 결과는 병합 관문 정본대로 goal에 쓰지 않고 근거 폴더와 승인 묶음으로 전한다. 다음 goal은 자동으로 시작하지 않는다.
14. 사용자 손·결정 대기: PR206 병합 승인, Gardener 정리 후보 둘의 채택, 던전 goal 초안과 PR191 재개 계획의 결정, `unity-upgrade-active` 삭제 확인. 메인 checkout의 skip-worktree 세 파일 처리는 메인 몫이다.
15. 던전 goal 초안과 PR191 재개 계획: 메인 `msg_3a4fc4686315`(사용자 결정 「A 계획만 갱신」)대로 근거 폴더에만 썼다. 위치는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active/.backups/verification/2026-10-08-content-replan/`의 `dungeon-goal-draft.md`·`pr191-resume-plan.md`다. 게임 goal 재개 조건(운영 셋업 3단계 병합 뒤)은 그대로라서 PR191 작업과 작업자 기동은 하지 않았다. 재개 때는 [보류 goal의 재개 지점](../2026-10-05-items-inventory-currency/goal.md#재개-지점)과 그 계획을 함께 읽는다. 시작 전에 메인에 계획을 status로 알린다(메인 `msg_a31233c60bf2`).

## 진척 단계

- [x] 범위 승인
- [x] 기준선 측정
- [x] 엔진 6.6·AI Assistant 전환
- [x] 문서·ADR 갱신
- [x] 독립 검증
- [x] MCP 연결 확인
- [x] 설정 두 파일 재검증
- [x] PR204 병합
- [x] 종료 Gardener
- [>] 종료 기록·점검

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
- 리드 대조(정확한 시각 미기록): 두 `results.xml`의 test-run 속성(356/356, 11/11, failed 0)과 실서버 테스트 Passed를 원시에서 다시 읽었다. 사후 음소거 값 0, Unity 0, 7777 listener 0(Windows·WSL)을 확인했다. release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 회수 대기 작업자는 0이다.
- Unity가 기준선 실행 중 스스로 바꾼 두 파일: `ProjectSettings.asset`의 Standalone `SENTIS_ANALYTICS_ENABLED;` 제거 한 줄(SHA256 C1412CD2… → 934110CB…, 이전 goal에서 관측된 것과 같은 쌍)과 `MinimapRT.renderTexture` 줄바꿈(Git blob 같음). 작업자는 사본·diff를 남기고 그대로 두었다. 리드가 엔진 전환 전에 둘 다 HEAD 상태로 되돌렸고, 실행 전 hash와 같아졌다(`lead-restore-baseline-drift.txt`). 엔진 전환에서 생기는 변경만 따로 보이게 하려는 것이다.
- 독립 판정은 아직 없다. 신규 Opus 검증의 입력이다.

### 6000.6.4f1 첫 열기 — 컴파일 실패, 자체 점검

Sol `task_5148d0531753`(Dispatch `ctx_cf508cb9ae6b`, 지정·화면 `gpt-6.1-sol max`, backend unknown)이 계약 v1(SHA256 `ed997bec…13f1`)대로 HEAD `1b7f3956`에서 실행했다. 보고 `sol-engine/report.md`(SHA256 `4959aec0…0301`), 분류표 `sol-engine/classification.json`(SHA256 `de8c5ca7…3b7e`).

- 첫 batch 열기는 exit 1이다(74초, 대화형 입력 없음). editor.log 4733·4753·4788줄은 `ComponentNullCharacterizationTests.cs(176,62)` CS0619 `Object.GetInstanceID()` 「Use GetEntityId instead」, 4794줄은 「Scripts have compiler errors.」다. ProjectVersion은 6000.4.7f1 그대로였다.
- 같은 로그 4214·4746·4759줄에는 AI Assistant 2.7.0-pre.3의 `Runtime/Utils/AssemblyUtils.cs(28,20)` UAC0005도 있었다. 이 작업자 보고와 분류표, 리드 대조, 메인 판정이 모두 이 오류를 빠뜨렸다([교정 기록](#교정-기록)). 대소문자 무시로 「error 」를 담은 줄은 8줄이다(코드 없는 제목 2, CS0619 3, UAC0005 3).
- Unity가 컴파일 전에 만든 변경은 셋이다: `manifest.json`, `packages-lock.json`(lock 항목 35개 변화: core 21, 최소 버전 강제 14, 분류 불가 0. 모듈 physicscore2d·tetgen·timelinefoundation 추가, vr 제거), 새 `ProjectSettings/PhysicsCoreProjectSettings2D.asset`(2키). 기존 ProjectSettings의 byte 변화와 SENTIS 반복 drift는 없었다. AI Assistant는 2.7.0-pre.3 그대로였다.
- `.meta` 1129 → 1129, guid 불일치·삭제·신규 0이다. relay SHA256·크기·수정 시각은 전후 같다(`854f019d…cd66`). 음소거 0 → 1 → 0이다.
- 계약 축소(리드 `msg_80dcfd34ed46`, `msg_38ffb4483224`): 컴파일 실패 뒤 EditMode를 한 번 시도했으나, 첫 열기가 남긴 `Temp/UnityLockfile`(0바이트) 때문에 harness gate가 Unity를 띄우지 않았다(`executed=false`). 서버 lane·PlayMode는 미실행이다. 6.6 수치는 아직 없다.
- 리드 대조(정확한 시각 미기록): editor.log 두 줄, `upgradeOpenExit=1`, `.meta` 대조 수치, Unity 0, 음소거 0, Windows·WSL 7777 listener 0을 원시와 현재 상태에서 다시 읽었다. release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true).
- 리드 정리: 테스트 수정 뒤 다음 열기가 Unity 생성 변경 전체를 한 번에 남기도록 manifest·lock을 HEAD로 되돌리고 새 asset과 잠금 파일을 지웠다. 사본은 `sol-engine/runs/*/changed-files`·`untracked-files`에 같은 SHA256으로 있다(`lead-restore-first-open.txt`).

### 테스트 한 줄 — 작성자 자체 점검, 독립 판정 전

신규 Opus 작성자 `task_8371d9d4f1ac`(Dispatch `ctx_88fb40a7e9a4`, 지정 `claude-opus-5-5`, 화면 「Opus 5.5 with xhigh effort」, backend unknown)가 계약 v1(SHA256 `393d5659…cfbe6`)대로 HEAD `1069e76f`에서 썼다. 보고 `opus-testfix/report.md`(SHA256 `bb9e9b8b…06b7`).

- `ComponentNullCharacterizationTests.cs` 176행 `value.GetInstanceID()` → `value.GetEntityId()`, 177행 기록 이름 `instanceId=` → `entityId=`. `git diff --numstat` `2 2`, blob `8533ec76` → `bf98cbf5`. 177행은 같은 기록의 이름이라 「그 한 줄」의 일부로 셌다.
- `Assert.` 줄은 14 → 14이고 내용이 같다. `Describe()` 반환값은 진단 기록에만 쓰이고 단언이 읽지 않는다. 기록 행 `[B-CHAR]`을 이 파일 밖에서 읽는 곳은 0건이다(작성자 grep, 리드가 `git grep`으로 다시 확인).
- `Object.GetEntityId`는 6000.6.4f1 XML 29749행과 6000.4.7f1 XML 30339행에 있다. `EntityId.ToString()` 형식은 문서로 확인되지 않아 미확인이다. Unity 컴파일은 작성자가 실행하지 않았다.

### 6000.6.4f1 재열기 — 컴파일 실패(AI Assistant), 자체 점검

Sol `task_25df1a718eef`(Dispatch `ctx_f3b07049d9f8`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `698173a3…6b34`)대로 테스트 두 줄이 든 로컬 checkpoint HEAD에서 실행했다. 보고 `sol-remeasure/report.md`(SHA256 `ebebeba5…e687`), 분류표 `sol-remeasure/classification.json`(SHA256 `820d4369…e8cd`).

- 재열기는 exit 1이다(13.8초). editor.log 1097·1102·1110줄은 같은 UAC0005, 1112줄은 「Scripts have compiler errors.」다. CS0619는 사라졌다. 대소문자 무시로 「error 」를 담은 줄은 4줄이다(코드 없는 제목 1, UAC0005 3). ProjectVersion은 다시 6000.4.7f1 그대로였다.
- 패키지 코드는 `#if UNITY_6000_5_OR_NEWER && !UNITY_EDITOR`일 때만 `CurrentAssemblies`를 쓰고, Editor에서는 `AppDomain.CurrentDomain.GetAssemblies()`를 쓴다. 6.6 분석기가 이를 오류로 막는다. PackageCache는 고치지 않는다.
- Unity가 만든 세 파일(manifest·lock·`PhysicsCoreProjectSettings2D.asset`)은 첫 열기와 SHA256이 같다. `.meta` guid 변화와 relay 변화는 없다.
- 계약 축소(리드 `msg_ba57bc88d457`): EditMode·PlayMode·서버 lane은 미실행이다. 두 로그의 error 줄 전수 추출을 더했다(`sol-remeasure/runs/diagnostics-final/all-error-lines.md`).
- 리드 대조(정확한 시각 미기록): 두 로그의 「error 」 줄을 리드가 따로 뽑아 8줄·4줄과 코드별 개수가 작업자 집계와 같음을 확인했다. 현재 세 파일 SHA256이 첫 열기 사본과 같고 Unity 0이었다. release는 `retained/external_terminal`, 대기 화면 확인 뒤 pane을 닫았다.
- 리드 정리: 사용자 결정 A의 다음 열기를 깨끗한 상태에서 하도록 manifest·lock을 HEAD로 되돌리고 새 asset과 잠금 파일을 지웠다(`lead-restore-remeasure.txt`).

### AI Assistant 2.20.0-pre.2 업그레이드 열기 — 컴파일 실패(Lucid Editor), 자체 점검

Sol `task_dc04bf3e6a75`(Dispatch `ctx_d28b5138f39a`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `6370bf6e…55b2`)대로 checkpoint HEAD `16b2253b`에서 실행했다. 보고 `sol-upgrade/report.md`(SHA256 `74c67c5e…a0ab`), 분류표 `sol-upgrade/classification.json`(SHA256 `22e7205a…c1fb`), 정적 확인 `sol-upgrade/ai-assistant-source.md`(SHA256 `948c4d0d…cde1`).

- 기동: worker-start가 `turn_start_unobserved`였다. pane의 draft가 공식 preamble과 같은 크기의 붙여넣기 placeholder여서, 리드가 [공식 계약 draft 복구](../../../00_Document/operations/ORCA.md#official-contract-draft)대로 텍스트 없는 Enter를 한 번 보냈다. 시작은 관측했다(`sol-upgrade-draft-identity.txt`).
- manifest의 AI Assistant 한 줄을 2.20.0-pre.2로 바꾸고 연 결과 exit 1(55.7초)이다. ProjectVersion은 6000.4.7f1로 남았다. 「error 」 줄은 31줄이다(제목 1, CS0619 30, UAC0005 0). 고유 위치는 Lucid Editor `SimpleTreeView.cs` 9곳과 `TreeMenu.cs` 1곳이다(`TreeView`·`TreeViewItem`·`TreeViewState` 「is now deprecated」). AI Assistant 쪽 오류 줄은 없다.
- lock 변화 38항목: 엔진 강제 35, AI Assistant 목표 1, AI Assistant 의존 2(`com.unity.cloud.gltfast` 제거, 공유 의존 `com.unity.nuget.mono-cecil` 1.11.6 유지·깊이 변화). 기존 ProjectSettings 변화 0, SENTIS drift 없음, `.meta` 1129 → 1129이고 불일치·삭제·신규 0이다.
- relay SHA256·크기·수정 시각은 전후 같다. 이번 batch 열기에서는 새 패키지가 relay를 덮어쓰지 않았다. 대화형 Editor 시작 때의 동작은 미확인이다. 음소거 0 → 1 → 0이다.
- 새 패키지 소스 정적 확인(완료조건 (5) 근거, 독립 판정 전):
  - 상한: `AcpEntitlementWiring.cs:45~62`가 개발용 override가 없으면 `ConnectionPolicy.Unlimited`를 설치한다. 연결 수 계측과 cap 판정 코드(`ConnectionCensus.cs:515~560`)는 남아 있다. 그래서 「상한 검사 코드가 없다」가 아니라 「기본 정책에서 상한이 적용되지 않는다」가 맞는 표현이다.
  - 신원 키: `ExecutableIdentityComparer.cs:56~76`이 서명이 유효하면 `Signed:<경로>:<SignaturePublisher>`, 서명이 없으면 `Hash:<SHA256>`을 쓴다. 파일 버전과 MCP clientInfo는 키에 들어가지 않는다. 서명된 같은 경로의 실행 파일은 업데이트 뒤에도 같은 키다. 서명 없는 실행 파일은 내용이 바뀌면 키가 달라진다(코드 기반 추정).
- 리드 대조(정확한 시각 미기록): 「error 」 31줄, 정적 확인 두 인용(`ExecutableIdentityComparer.cs:56~76`, `AcpEntitlementWiring.cs:45~62`)을 원문에서 다시 읽었다. 현재 Unity 0이다. release는 `retained/external_terminal`, 대기 화면 확인 뒤 pane을 닫았다.
- 리드 정리: 다음 열기를 깨끗한 상태에서 하도록 manifest·lock을 HEAD로 되돌리고 새 asset과 잠금 파일을 지웠다(`lead-restore-upgrade-open.txt`).

### Lucid 두 파일 삭제 뒤 측정 열기 — 컴파일 실패(Lucid Editor 다른 줄), 자체 점검

Sol `task_9cf22ad2da19`(Dispatch `ctx_0826018fe9b0`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `8dae5bef…5f21`)대로 checkpoint HEAD `244acad1`에서 실행했다. 보고 `sol-measure/report.md`(SHA256 `6c99a781…7270`), 분류표 `sol-measure/classification.json`(SHA256 `8a97ab1d…b77a`).

- 발행: 계약 전문(33KB)으로 worker-start를 부르자 bash가 「Argument list too long」으로 실행하지 않았다(exit 126, 디스패치 미생성). 리드는 짧은 발행문(`sol-measure-dispatch.md`)에 계약 경로와 SHA256을 담아 다시 발행했고, 작업자가 hash를 직접 대조한 뒤 전문을 읽게 했다.
- manifest 한 줄과 Experimental 네 파일 삭제 뒤 연 결과 exit 1(17.3초)이다. ProjectVersion은 6000.4.7f1로 남았다. 「error 」 줄은 4줄이다(`Lucid Editor/Editor/SerializeReferenceDropdown.cs(107,51)` CS0619 `AdvancedDropdownItem.children` 「Please use childList」 3줄, Assembly-CSharp-Editor 제목 1줄).
- 앞선 열기 로그에서 이 파일은 컴파일 명령의 파일 목록(3345행)에만 있었다. 앞선 오류는 `TreeView` 상속·시그니처 같은 선언 단계 오류였다. csc가 선언 오류가 있으면 메서드 본문 오류를 보고하지 않아 이번에야 드러난 것으로 추정한다.
- `.meta` 1129 → 1127, 삭제 2(승인된 `SimpleTreeView.cs.meta`·`TreeMenu.cs.meta`), 불일치·신규 0이다. 패키지 분류는 엔진 강제 35, AI Assistant 목표 1, AI Assistant 의존 2로 앞선 열기와 같다.
- 작업자 자기 보고: heartbeat 5분 간격을 한 번 넘겼고, 보고 생성 보조 스크립트가 두 번 중단됐다(원시 보존). Unity 재실행이나 추적 파일 추가 수정은 없었다.
- 리드 대조(정확한 시각 미기록): 「error 」 4줄, `.meta` 대조 두 파일과 guid를 원시와 작업 트리에서 다시 읽었다. 현재 Unity 0이다. release는 `retained/external_terminal`, 대기 화면 확인 뒤 pane을 닫았다. 닫은 뒤 새로 만든 기준 pane도 exited됐다.
- 리드 정리: 관계 점검을 HEAD 기준으로 하도록 manifest·lock과 Experimental 네 파일을 HEAD에서 되살리고, 새 asset과 잠금 파일을 지웠다(`lead-restore-measure.txt`).

### Lucid Editor 관계 점검 — 읽기 전용, 리드·메인 표본 대조

Sol `task_ca7dae29abca`(Dispatch `ctx_11ec28d046c8`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `7722735d…454a`)대로 checkpoint HEAD `439c742e`에서 실행했다. 파일 삭제와 Unity 실행은 하지 않았고, 추적 파일 변경은 0이다. 산출물은 셋이다.

- 표 `sol-relations/relations.md`(SHA256 `85522e6d…9a52`)
- 기계 판독 `sol-relations/relations.json`(SHA256 `d9e9a321…f6f634`)
- 보고 `sol-relations/report.md`(SHA256 `2e82de6e…e19d7`)

결과는 다음과 같다.

- 후보는 123개다. `.cs` 58개, `.cs.meta` 58개, 폴더 `.meta` 7개다. 리드의 출발 집합과 같아, 더하거나 뺀 것은 0이다. 판정은 지움 119, 묻기 4였고, 메인 판단으로 123개 모두 지움이 됐다([승인된 결정](#승인된-결정)).
- (b) guid 65개는 `Assets`·`ProjectSettings`·`Packages` 전체에서 자기 `.meta`에만 있다. Asset Serialization은 Force Text이고, 바이너리 직렬화 파일은 0이다. 바이너리 헤더로 의심되는 파일 222개 안의 비문자열 참조까지 없다고 증명하지는 못한다(작업자 공개 한계).
- (a)(c)(d) 남는 런타임 코드와 우리 게임 코드는 후보 형식을 실제로 쓰지 않는다(0줄). 남는 Cainos 런타임은 39개다(Lucid Runtime 34개, `Chest`·`Elevator`·`MovingPlatform`·`BoundingPlatform`·`SecondOrderDynamics`). `Chest.cs`·`Elevator.cs`의 `using Cainos.LucidEditor;`는 문제없다. Runtime 파일이 그 namespace를 계속 선언하므로 삭제 뒤에도 해석된다. `ChestEditor`·`ElevatorEditor`는 `LucidEditor`를 상속하지만 함께 지워진다.
- 「묻기」 4개의 원인은 `03_Client/Temp/ScriptUpdater/` 아래 `SerializeReferenceDropdown.cs` 사본 하나다.
  - `updates.txt`가 이 사본을 Assets의 후보 자신에 짝지었다. 원본과의 차이는 107행 `children` → `childList` 한 줄이다.
  - Git 제외 경로(`.gitignore:44`)에 있다. 리드가 파일 속성에서 읽은 수정 시각이 2026-10-07 11:42Z라서(작업자에게 준 답, `sol-relations/raw/coordinator-ask.stdout.txt` 3행) 측정 열기 때 생긴 것으로 본다(추론).
- 리드 대조(정확한 시각 미기록, `lead-sample-relations.md`)는 다섯 가지를 직접 확인했다.
  - 후보를 `git ls-files`로 다시 셌다.
  - guid 65개를 전체에서 grep했다.
  - 형식 이름 71개를 후보 밖 `.cs` 181개에서 grep했다. 걸린 37줄은 모두 namespace·using·주석이었다.
  - asmdef가 0개임을 확인했다.
  - Temp 사본의 diff와 `updates.txt`를 읽었다.
- 메인 대조(`msg_ff681cef67de`): 후보 재계수, Temp 사본, guid 표본 둘, namespace를 확인했다.
- 작업자 자기 보고: heartbeat 5분 간격을 한 번 넘겼다(5분 46초). release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다. 닫은 뒤 기준 pane이 다시 exited됐다.

### Lucid 123개 삭제 뒤 측정 — 컴파일 통과, PlayMode 전 실패, 자체 점검

Sol `task_709082e8b853`(Dispatch `ctx_1794f1407528`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `3aaa47df…8e5f`)과 추가 지시 1(`541132a6…f207`)대로 checkpoint HEAD `a1ba0d80`에서 실행했다. 보고 `sol-delete/report.md`(SHA256 `e13d4f5a…6f05`), 분류표 `sol-delete/classification.json`(SHA256 `3dc4dae0…b50d`).

- 준비: Unity 0을 확인하고 `03_Client/Temp`를 비웠다(6 → 0). manifest 한 줄을 바꾸고 고정 목록 123개와 빈 폴더 7개를 지웠다.
- 열기: exit 0(137초), ProjectVersion 6000.6.4f1 (12bfff696524)이다. 「Scripts have compiler errors」는 없다.
  - 「error 」 전수는 1줄이다. 19914행 「Batchmode quit successfully invoked」 다음 줄인 「Curl error 42: Callback aborted」이고, 종료 구간 네트워크 요청 취소로 분류했다(리드 답 `msg_e53c423586f7`, 메인 동의 `msg_960ff3ab8bbd`).
  - 대소문자 무시 error 단어 줄은 기준선 EditMode 18·PlayMode 14, 열기 9다. 라이선스 줄은 기준선에도 있다. 기준선에 없던 새 종류는 Curl뿐이다.
- 재출현: 열기 뒤와 측정 뒤 모두 123경로·7폴더가 0이다. `.meta`는 1129 → 1064, 삭제 65(고정 guid 집합과 같음), 신규·불일치 0이다. 남긴 Cainos 파일 753개는 byte 변화 0이다.
- 패키지: 변화 38 = 엔진 강제 35 + AI 목표 1 + AI 의존 2. 앞선 두 열기의 lock과 차이 0이다. `com.unity.inputsystem` 1.19.0 → 1.20.0은 Editor manifest minimumVersion이라 엔진 강제다.
- Unity 생성 변경은 다음과 같다.
  - ProjectVersion·manifest·lock.
  - URP GlobalSettings assetVersion 10 → 11.
  - 새 `PhysicsCoreProjectSettings2D.asset`·`ProjectAuditorSettings.asset`.
  - 테스트 뒤 `ProjectSettings.asset`(serializedVersion 28 → 30, 새 키, SENTIS 한 줄 drift). 57행 `m_StackTraceTypes` 끝에 `i`가 붙었다. 6.6 URP 템플릿(serializedVersion 29)에는 없다. 원인은 미확인이고 독립 검증 항목이다.
  - `MinimapRT.renderTexture`는 줄 끝만 바뀌었다.
- 측정:

  | 플랫폼 | 기준선 6000.4.7f1 | 6000.6.4f1 |
  |---|---|---|
  | EditMode | 356/356 | 356/356(exit 0) |
  | PlayMode | 11/11 | 0/11(exit 2) |

  - `ComponentNullCharacterizationTests` 17개는 기준선과 같이 모두 통과했다.
  - PlayMode 첫 테스트는 TearDown의 `InputSystem.settings` 복원에서 ArgumentNullException이 났다. 나머지 10개는 파괴된 InputSettings를 Instantiate하다가 MissingReferenceException이 났다. 실서버 경로도 fixture 준비에서 실패해 실제 진입은 미확인이다.
- 리드 원인 분석(`lead-analysis-playmode-inputsettings.md`, 정적 읽기, 추정):
  - 1.20.0 `InputManager.settings` setter(190~192)는 이전 settings가 `HideAndDontSave`면 `DestroyImmediate`한다. 1.19.0에는 이 줄이 없다.
  - 프로젝트에 InputSettings 에셋이 없어 원래 settings는 그 임시 객체다. fixture가 사본으로 바꾸는 순간 원래 것이 파괴된다.
  - `InputSystem.settings`를 쓰는 곳은 fixture뿐이다. 메인도 같은 줄들을 원천 대조했다(`msg_4aa892767efc`).
- 자원: 매 batch 음소거는 0 → 1 → 0이었다. 작업자 서버만 Stop했고 7777은 비었다. Unity 0이다.
- relay: `854f019d…cd66` → `0cd32f10…e014`로 바뀌었다(위 「위험과 되돌리기」).
- AI Assistant 소스: 정적 확인 14개 파일의 hash와 행이 앞선 확인과 같다.
- 작업자 자기 보고: 분류 문서 helper가 한 번 중단됐다. 첫 출력은 보존하고 문서 단계만 다시 돌렸다.
- 리드 대조: heartbeat가 5분을 두 번 넘겼다(5분 45초, 6분 25초). 보고에는 적히지 않았다.
- 리드 대조(정확한 시각 미기록): EditMode·PlayMode `results.xml` 집계, 디스크 `.meta` 1064, 재출현 0, relay 현재 hash, 음소거 0, 7777 listener 0, 두 버전 setter 비교를 원시와 현재 상태에서 다시 읽었다. release는 `retained/external_terminal`이었고 pane을 닫았다. 이번에는 기준 pane이 살아 있다.

### PlayMode fixture 입력 설정 수정 — 작성자 자체 점검, 독립 판정 전

신규 Opus 작성자 `task_6d644d17cac8`(Dispatch `ctx_7ecdd6024bd3`, 지정·세션 표시 `claude-opus-5-5`, backend unknown)가 계약 v1(SHA256 `0b51fe86…f515`)대로 checkpoint HEAD `c9f4c2d7`에서 썼다. 보고 `opus-fixturefix/report.md`(SHA256 `0254af03…c98b`).

- 바꾼 파일은 `MapEntryPlayFixture.cs` 하나다(numstat 15/2, blob `77f7b6a0` → `d0db9de9`). 단언 줄 변화는 0이다.
  - 필드 하나를 더해 원본 설정의 손대지 않은 사본을 둔다.
  - 복원은 원본이 Unity null이 아니면 원본, 파괴됐으면 사본을 넣는다.
  - 정리는 지금 `InputSystem.settings`가 아닌 사본만 파괴한다.
- 작성자의 1.20.0 의존 판단(메인 지시 한 줄): 「고친 방식은 1.20.0의 임시 설정 즉시 파괴에 기대지 않는다. … 그래서 1.19.0으로 돌아가도 안전하다.」 근거는 1.20.0 setter `InputManager.cs:190~192`와 1.19.0 setter `InputManager.cs:103~113`이다.
- 작성자는 Unity를 실행하지 않았다. 실행 결과는 아래 재측정에 있다.
- 리드 대조(`lead-review-fixturefix.md`, SHA256 `60533714…2025`): 세 경우를 코드로 따라가 확인했다.
  - 1.20.0, 에셋 없음: 테스트 뒤 살아 있는 설정은 사본 하나이고, 다음 준비가 그 사본을 원본으로 보관한다.
  - 1.19.0: 원본으로 복원하고 두 사본을 모두 파괴한다.
  - 1.20.0, 에셋 있음: 원본이 파괴되지 않아 원본으로 복원하고, 에셋 값은 바뀌지 않는다.
  - 1.20.0에서 전역 설정이 원본에서 사본으로 바뀌는 것을 「누수 없음」으로 본 해석은 독립 판정 항목이다.

### fixture 수정 뒤 6000.6.4f1 재측정 — 기준선과 같음, 자체 점검

Sol `task_fd269354bea8`(Dispatch `ctx_f63b8e064c1f`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `4fa9e931…b508`)대로 checkpoint HEAD `4603a49e`에서 실행했다. 보고 `sol-fixture-measure/report.md`(SHA256 `d382e4e9…6682`).

| 플랫폼 | 기준선 6000.4.7f1 | 6.6 fixture 수정 전 | 6.6 fixture 수정 뒤 |
|---|---|---|---|
| EditMode | 356/356 | 356/356 | 356/356(exit 0) |
| PlayMode | 11/11 | 0/11 | 11/11(exit 0) |

- 실서버 경로 `ProductionServer_TownHuntingGroundTown_KeyboardPortalsPreserveEntityHpAndRoster`가 Passed다. PlayMode editor.log 14156행에 「[M1c Production Unity] Town->HG->Town entity=7, roster=8, hp=150/150, keyboard/physics portals PASS」가 있다. 완료조건 (3)·(4)의 측정 근거이고, 독립 판정은 아니다.
- Unity는 테스트 batch 둘만 실행했다. 열기 batch·대화형 Editor·MCP는 없었다. 현재 테스트 이름 367개는 모두 기준선에 있다.
- 로그: 「error 」 전수는 EditMode 11줄, PlayMode 3줄이다. error 단어 줄의 새 종류는 0이다. 「Scripts have compiler errors」는 없다.
- 상태:
  - `.meta`는 1064다(삭제 65, 신규·불일치 0). 123경로·7폴더 재출현은 0이다.
  - 테스트가 바꾼 추적 파일은 `MinimapRT.renderTexture` 하나이고 줄 끝만 바뀌었다. 리드가 HEAD로 되돌렸다.
  - `ProjectSettings.asset`은 테스트 전후 같다(SHA256 `333733a3…348b`). 57행 끝 `i`는 남았고 원인은 확정하지 않았다.
  - relay는 측정 내내 `0cd32f10…e014`였다.
- 작업자 자기 보고: heartbeat 5분 간격을 한 번 넘겼다(5분 32초). 보조 스크립트가 두 번 실패했고 원시를 보존했다. 측정은 다시 돌리지 않았다.
- 리드 대조(재측정 정산 때): 두 `results.xml` 집계, PlayMode 11개 이름별 Passed, 실서버 로그 행을 원시에서 다시 읽었다. 현재 Unity 0, 57행 hash도 확인했다. 메인도 두 `results.xml`과 실서버 테스트 결과를 직접 봤다(`msg_a9234fbe1251`). release는 `retained/external_terminal`이었고 pane을 닫았다.

### 업그레이드 커밋 — 리드 구성

업그레이드 커밋 `bebbb4e0`(부모 goal 커밋 `33925c73`)은 측정에 쓴 로컬 checkpoint 셋에 SENTIS 한 줄 되돌림과 검증자 테스트 2개를 더해 합친 것이다. 셋은 `c2ad6aa0`(테스트 두 줄), `c9f4c2d7`(다섯째 열기 상태), `4603a49e`(fixture 수정)다. 거쳐 간 커밋은 둘이다.

- `cf232267`: SENTIS drift를 담고 있었다(아래 절).
- `1d310500`: 독립 검증 대상이었다. 지금 커밋과의 차이는 검증자 테스트 +64줄뿐이고, 결함 #1 메시지도 고쳤다(사용자 결정 1A `msg_3dc8c0c52851`).

- 커밋 tree는 재측정한 `4603a49e`의 tree와 둘만 다르다. 하나는 `ProjectSettings.asset` 597행이고, base와 같은 `Standalone: SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`다. 다른 하나는 검증자 테스트 +64줄이다. 독립 검증자의 1회차가 `1d310500` tree에서, 2회차가 그 tree에 검증자 테스트를 더한 상태에서 테스트를 돌렸다(아래 절).
- `33925c73` 대비 추가 2, 삭제 123, 수정 8이다.
  - 추가: `PhysicsCoreProjectSettings2D.asset`, `ProjectAuditorSettings.asset`.
  - 삭제: 관계표의 123개.
  - 수정: manifest, lock, ProjectVersion, `ProjectSettings.asset`, URP 전역 설정, 테스트 세 파일(테스트 한 줄, fixture, 검증자 테스트).
- 줄 끝만 바뀐 `MinimapRT.renderTexture`는 넣지 않았다.
- 커밋 메시지에 「엔진 커밋 뒤」를 잴 수 없는 이유, SENTIS define을 뺀 이유, 결정 ID를 적었다.

### SENTIS define 한 줄 되돌림 — 작업자 자체 점검, 리드 대조

리드가 첫 문서 커밋 뒤 검증자 계약을 쓰다가 `cf232267`의 `ProjectSettings.asset`에서 `Standalone: SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY` → `Standalone: APP_UI_EDITOR_ONLY` 변경을 찾았다(`lead-sentis-drift-check.txt`).

- 원인(패키지 코드 정적 읽기): `com.unity.ai.inference@9a123aee5df7/Editor/Analytics/AnalyticsDefineManager.cs`의 `[InitializeOnLoadMethod]`가 Editor 시작마다 define을 넣거나 뺀다. 조건은 `FORCE_SENTIS_ANALYTICS`가 없으면 `UNITY_2023_2_OR_NEWER && ENABLE_CLOUD_SERVICES_ANALYTICS`일 때 `EditorAnalytics.enabled`(사용자 분석 설정)이고, 아니면 false다. 그래서 기계·사용자별 값이다.
- 업그레이드와 무관하다. lock의 `com.unity.ai.inference`는 전후 2.6.1이고, 기준선 6000.4.7f1 실행도 같은 줄을 지웠다.
- Sol `task_989f7560f4d5`(Dispatch `ctx_208d217ffd5e`, 지정·화면 `gpt-6.1-sol max`, backend unknown)가 계약 v1(SHA256 `cace2a62…0728`)대로 HEAD `9d1a6221`에서 597행 한 줄만 base 값으로 되돌렸다. Unity는 실행하지 않았다. 보고 `sol-sentis/report.md`(SHA256 `98bc4bbe…2ee0`).
- 작업자 자기 보고: heartbeat 5분 간격을 두 번 넘겼다(5분 55초, 5분 5초). 보조 시각 계산 한 번이 시간대 변환으로 틀렸고, 원문 문자열로 다시 계산해 정정했다.
- 리드 대조(정확한 시각 미기록): `git diff --numstat` 1/1, 작업 트리 변경은 그 파일 하나다. base 대비 define 변경 0, 재측정 tree 대비 597행 한 줄 차이다. CR 0, 끝 byte LF, BOM 없음, 57·166행 그대로다. 대조는 release 전에 했다(리드 진술). release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). release receipt `lead-release-sentis.json`의 파일 수정 시각은 2026-10-07T15:16:13Z다. 이 worktree 터미널은 0이다.
- 리드가 그 한 줄을 업그레이드 커밋에 합쳐 `1d310500`을 만들었다(그 뒤 검증자 테스트를 더해 `bebbb4e0`).

### 신규 Opus 독립 검증 — PASS(비차단 결함 1), 완료조건 (6) 제외

검증자 `task_f82aaa818461`(Dispatch `ctx_83408a4a0a40`, 최초 실행 `claude --model claude-opus-5-5`(MCP 설정 없음), 화면 「Opus 5.5 with xhigh effort」, backend unknown)가 계약 v1(SHA256 `78c0b6fc…e617`)대로 HEAD `ee5c848b`(문서 커밋), 부모 `1d310500`(업그레이드 커밋)을 판정했다. 판정 `opus-verify/verdict.md`(SHA256 `8c7c59c7…102c`), worker_done `msg_043cec851d8e`.

- 결론: **PASS.** 차단 결함 0. 완료조건 (1)(2)(3)(4)(5)(7)(8)(9) 충족. (6)은 이 계약 밖(별도 MCP 세션).
- 실행(6000.6.4f1 batch, 매번 음소거 0 → 1 → 0, 7777은 자기 서버만 쓰고 해제):

  | 회차 | 상태 | EditMode | PlayMode |
  |---|---|---|---|
  | 1회차 | 커밋된 tree(HEAD `ee5c848b`, 깨끗한 상태) | 356/356 | 11/11, 실서버 경로 Passed(editor.log 13988행 hp=143/150) |
  | 2회차 | 1회차 + 검증자 테스트 2개 + Unity drift | 356/356 | 13/13(기존 11, 더한 2) |

- V5 `i` 두 곳: 같은 6.6 바이너리로 저장소 밖 임시 폴더에 만든 새 빈 프로젝트에도 57행·165행 끝에 `i`가 있다. 1회차 중 6.6이 이 파일을 다시 쓴 결과도 재측정 때와 byte가 같다. 그래서 6.6 직렬화기의 정상 값이고 커밋해도 같은 6.6에서 흔들리지 않는다고 판정했다. `i`의 의미와 다른 머신·patch는 미확인이다.
- V7 fixture: 원인 추정이 수정 전 실패 원시와 맞는다. 표현 정정이 하나 있다. 나머지 10개는 NUnit `[SetUp]`이 아니라 테스트 본문의 `Prepare()` 단계(185행 `Instantiate`)에서 실패했다. 검증자 테스트가 6.6에서 교체된 원본의 `destroyedByPrepare=True`를 세 번 관측했다. 「누수 없음」 해석은 맞다. 1.20.0에서 테스트 뒤 전역 설정은 사본이지만 값·`hideFlags`가 같고 객체 수가 늘지 않는다.
- V11 relay: 위 「위험과 되돌리기」 relay 줄에 반영했다.
- 결함 #1(낮음, 비차단, 귀속 리드): 업그레이드 커밋 메시지가 메인 결정 `msg_ff681cef67de`를 「user decisions」에 넣었다. 커밋 재구성 때 메시지만 고친다.
- 설계 관찰: O1(fixture 조용한 복원 생략)과 O3(harness Before gate 한 번 제한)는 「다음 계획 후보」, O2(relay 줄 보강)는 반영했고, O4(SENTIS drift)는 기존 후보로 충분하다.
- 2회차 사전 gate: 고정 `Save-BaselineState.ps1`이 근거 폴더마다 Before gate를 한 번만 써서 막혔다. 검증자 질문(`msg_7b923adfd35d`)에 리드가 조건을 붙여 승인했다(`msg_930075533294`). 조건은 고정 harness만 쓰기, 실행 전후 상태 직접 기록, 추적 변경 셋 확인이다.
- 더한 테스트: `03_Client/Assets/Tests/PlayMode/MapEntrySceneLifecycleTests.cs`(기존 파일, +64, 새 `.meta` 없음)의 `RepeatedFixtureCycles_RestoreGlobalInputSettingsValues_WithoutGrowingSettingsObjects`, `SurvivingOriginalSettings_IsRestoredAsItself_AndBothFixtureCopiesAreDestroyed`. 사용자 결정 1A(`msg_3dc8c0c52851`)로 업그레이드 커밋 `bebbb4e0`에 넣었다(파일 SHA256 `e9bb9c2e…217a`, 검증자 판정의 값과 같음).
- 작업자 자기 보고: heartbeat 5분 간격을 두 번 넘겼다(5분 1초, 6분 41초).
- 리드 대조(정확한 시각 미기록):
  - results.xml 네 개의 test-run 속성과 실서버 test-case Passed, 로그 13988행.
  - 더한 테스트 2개 Passed.
  - 새 프로젝트 사본 57·165행의 `i`.
  - relay 서명 Valid와 hash.
  - 커밋 메시지의 #1 문구.
  - release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 이 worktree 터미널은 0이다.
- 정리: 검증 중 Unity가 남긴 SENTIS 한 줄과 `MinimapRT.renderTexture` 줄 끝은 사본(`lead-cleanup-after-verify/`)을 남기고 HEAD로 되돌렸다.

### 완료조건 (6) MCP 연결 확인 — PASS(비차단 결함 1)

검증자 `task_99aa199d4606`(Dispatch `ctx_14de51602c50`, 최초 실행 `claude --model claude-opus-5-5 --mcp-config C:/Users/bass1/.unity/claude-mcp.json`, 화면 「Opus 5.5 with xhigh effort」, backend unknown)가 계약 v1(SHA256 `6df69ed4…2723`)대로 판정했다. 대상은 HEAD `0a574c77`(문서 커밋)과 부모 `bebbb4e0`(업그레이드 커밋)이다. 판정은 `opus-mcp/verdict.md`(SHA256 `0728dd38…e726`), worker_done은 `msg_90c32731eed3`다. 사용자 손 단계(Hub로 열기, MCP 화면 열어 두기, 닫기)는 메인 status로만 요청했다(`msg_80dafb49e51a`, `msg_be080196a72f`). 사용자 원문은 메인 전달이다(`msg_b16c42149456`, `msg_daedd9f45820`).

- 결론: **PASS.** 차단 결함 0. 완료조건 (6)의 세 부분을 모두 실제 호출로 관측했다.
  - (a) 사용자가 Hub로 연 6000.6.4f1 Editor에서 첫 `Unity_ReadConsole`이 거부·상한 문구 없이 성공했다. 이 프로젝트의 경고 30개를 돌려받았다. `Library/AI.MCP/connections-v2.asset`에는 `Status: 1`, `Approved by user`, `DialogShown: 0`이 적혔다. 사용자가 Accept를 눌렀는지는 검증자가 볼 수 없어 미확인이다.
  - (b) 컴파일 요청과 Play 진입·종료는 전용 도구가 없어 `Unity_RunCommand`의 고정 코드 한 줄씩으로 했다(리드 결정, 파일 쓰기 코드 없음). Editor.log에 `Requested through public api`와 도메인 리로드가 있다. Play 중 두 번째 `Unity_ReadConsole`이 성공했고 런타임 로그 「PersistentServices 생성 완료」가 들어 있었다.
  - (c) 음소거 값은 Editor를 열기 전 `0x0`, 종료 뒤 `0x0`이다. 중간 측정도 모두 `0x0`이다. 대화형 단계라 이 값을 쓰지 않았다.
- 결함 #1(낮음, 비차단): relay 신원 키가 hash 기반으로 저장됐다. 위 「위험과 되돌리기」 relay 줄 (3)을 고쳤다.
- 대화형 Editor가 바꾼 추적 파일 둘(되돌리지 않음): 열자마자 `PackageManagerSettings.asset`(+6/-4)이, 닫을 때 `QualitySettings.asset`(+13/-6)이 6.6 형식으로 바뀌었다. 앞선 batch 실행들은 두 파일을 저장하지 않았다. 사본은 `lead-cleanup-after-mcp/`에 있다. 리드 결정 요청(`msg_979515280783`)에 사용자 결정 A(`msg_3d959b3798a7`)로 업그레이드 커밋에 넣었다(아래).
- 설계 관찰(비차단):
  - O1: `Unity_RunCommand`는 코드를 다른 namespace로 감싸므로, 고정 코드는 완전한 이름으로 써야 한다. 첫 컴파일 요청이 이 때문에 컴파일되지 않았다(실행 안 됨, 부작용 없음).
  - O2: 6.6 대화형 Editor 로그는 기본 경로가 아니라 `03_Client/Logs/Editor.log`(gitignore)에 쌓였다.
  - O3: Editor 안 Assistant 게이트웨이 relay가 컴파일 리로드 뒤 다시 붙지 않았다. 180초 뒤 꺼졌다가 Play 리로드 때 다시 떴다. MCP 경로는 named pipe라 영향이 없었다.
  - O4: 프로세스 목록 한 번이 게이트웨이 relay를 놓쳤다(원인 미확인, 판정 영향 없음).
- 리드 대조(정확한 시각 미기록):
  - RunCommand 코드 원문 세 개가 각각 한 줄 호출과 `result.Log`뿐이다.
  - 두 번째 호출 응답에 `"success": true`와 런타임 로그 줄이 있다.
  - Editor.log 발췌에 컴파일 요청과 `successfully reloaded assembly`가 있다.
  - 음소거 원시는 열기 전과 종료 뒤 모두 `0x0`이다.
  - `opus-mcp/` 전체에서 Hub 토큰 값은 `<REDACTED>`뿐이다.
  - release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 시트가 풀렸다. 이 worktree 터미널은 0, Unity 0이다.

### 대화형 설정 두 파일 커밋 반영 — 리드 구성

- 사용자 결정 A(`msg_3d959b3798a7`)대로 업그레이드 커밋 `bebbb4e0`의 tree에 두 파일만 바꿔 넣어 `71c980653da1a37af9648d4ebe1264963a915283`을 만들었다. 부모는 그대로 `33925c73`이다.
  - `git diff --stat bebbb4e0 71c98065`는 `PackageManagerSettings.asset` +6/-4, `QualitySettings.asset` +13/-6의 두 파일이다.
  - 두 파일의 SHA256은 사본(`lead-cleanup-after-mcp/*.interactive`)과 같다. `PackageManagerSettings`는 `1be74f03…10e5`, `QualitySettings`는 `7ec339d3…0de3`이다.
- 커밋 메시지에는 두 파일 문단과 결정 출처를 더했다. 다른 문단은 그대로다.
- 문서 커밋은 그때의 goal 갱신을 담아 `71c98065` 위에 `ba513147`로 다시 만들었다. 앞선 문서 커밋 `0a574c77`과 다른 곳은 goal뿐이다.
- 재검증 뒤(아래) 결함 #1 문구를 고쳐 업그레이드 커밋을 `62228618d4c8da692f218c9e4b0f69262bbeceb8`로 다시 만들었다. tree는 `71c98065`와 같은 `a9c1b2fa`이고, 메시지의 entity id 한 문장만 다르다. 문서 커밋은 이 goal 갱신을 담아 그 위에 다시 만들었다.

### 설정 두 파일 약 등급 재검증 — PASS(비차단 결함 1)

검증자 `task_9d01d3977345`(Dispatch `ctx_2b92ab95cae9`, 최초 실행 `claude --model claude-opus-5-5`(MCP 설정 없음), 화면 「Opus 5.5 with xhigh effort」, safeguard 선택창 없음, backend unknown)가 계약 v1(SHA256 `d1abacf1…20cf5`)대로 판정했다. 대상은 HEAD `ba513147`(문서 커밋)과 부모 `71c98065`(업그레이드 커밋)이다. 판정은 `opus-reverify/verdict.md`(SHA256 `f5d16e18…b7b3e`), worker_done은 `msg_c79135ab39b7`이다.

- 결론: **PASS.** 차단 결함 0.
  - R1 커밋 대조: 바뀐 것은 두 파일뿐이고, 커밋 blob과 사본이 byte 단위로 같다. 문서 커밋은 goal만 다르다.
  - R2 두 파일 실사: 바뀐 줄 29개 중 25개는 형식 이행이다. 나머지 4개는 이름이 바뀐 필드가 세션 entity id를 실은 줄이고, 그 밖은 0이다. 레지스트리 URL·scope·플래그와 기존 품질 값은 base와 같다.
  - R2의 근거는 두 가지다. 6.6·6.4 Editor DLL의 필드 이름 문자열이 하나고, 6.6 batch로 만든 새 프로젝트의 `QualitySettings.asset`이 다른 하나다(`serializedVersion: 5`, `meshLodThreshold: 1`). 새 프로젝트는 `PackageManagerSettings.asset`을 만들지 않아 그 파일은 DLL 근거만 있다.
  - R3 기존 테스트(6.6 batch, 깨끗한 HEAD tree): EditMode 356/356, PlayMode 13/13이다. 실서버 경로가 Passed이고, 음소거는 세 실행 모두 0 → 1 → 0, 7777은 정리 뒤 listener 0이다.
  - R4 실행 뒤: 두 파일은 batch 세 번 뒤에도 그대로다. `ProjectSettings.asset` SENTIS 한 줄과 `MinimapRT.renderTexture` status(내용은 HEAD와 같음)가 다시 생겼다.
  - R5 goal 실사: MCP 결과, relay 정정, 결정 원문, 교정 기록이 원천과 맞다. 인용된 msg ID 10개가 근거 폴더 원문에 있다.
- 결함 #1(낮음, 비차단, 귀속 리드): 커밋 메시지와 goal 범위 절이 entity id 값을 「저장할 때마다 바뀜, main도 그랬음」으로 적었다. 그러나 main의 옛 값은 이력에서 한 번도 바뀌지 않아, 이 문장은 관측이 아니다. 두 곳 모두 「필드 타입 근거의 추론, 옛 값은 이력에서 불변」으로 고쳤다.
- 설계 관찰(비차단):
  - O1: 선택 실행 `-createProject`가 사용자 EditorPrefs 세 값(`LastUsedProjectPath`·`kWorkspacePath`·`kProjectBasePath`)을 지운 임시 경로로 바꿨다. 실행 전 값은 기록되지 않아 복원하지 않았다. 다음에 프로젝트를 열면 Unity가 다시 쓰는 값으로 보인다(추론). 앞선 독립 검증의 같은 실행도 같은 효과였을 수 있다(미확인). 아래 [교정 기록](#교정-기록)에 적었다.
  - O2: 음소거 쓰기·복원 절차가 harness 세 곳에 중복돼 있다. 다음 계획 후보로 둔다.
- 리드 대조(정확한 시각 미기록):
  - results.xml 두 개의 test-run 속성이 356/356, 13/13이고 실패 0이다.
  - EditorPrefs 원시(`raw/63-probe-side-effects.txt`)에 세 값의 새 경로가 있다.
  - 최종 상태 원시(`raw/70-final-state.txt`)는 Unity 0, 음소거 0, 7777 listener 0이고, 실행마다 「0 → 1 → 0」이다.
  - release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 이 worktree 터미널은 0이다.
- 정리: 재검증이 남긴 SENTIS 한 줄과 `MinimapRT.renderTexture`는 사본(`lead-cleanup-after-reverify/`)을 남기고 HEAD로 되돌렸다. 작업 트리는 깨끗하다.

### PR204 병합

- main 통합: 승인 요청 전 main이 `94fc6845`에서 `8e498440`(PR199~PR203)으로 움직여, branch에 main을 병합했다(`11ba78898b12bec9b90167eb73655a22bebe4073`). 이미 원격에 있던 goal 커밋을 다시 쓰지 않으려고 rebase 대신 병합을 골랐다.
  - main 쪽 44파일은 `.agents`·`00_Document`·`01_Phases`·`05_Management`·`AGENTS.md`·`CLAUDE.md`이고, `02_Server`·`03_Client`·`04_ClientNet`·`98_Shared`는 없다.
  - 충돌은 `CURRENT.md`의 인접한 worktree 줄 하나였다. main의 Core·Rules·Management 줄과 이 branch의 Content 두 줄을 남겼다.
  - 병합 뒤 remerge-diff(`lead-remerge-diff-11ba7889.txt`)의 바뀐 파일은 `CURRENT.md` 하나다. 이 원시는 병합 뒤에 남겼다(아래 교정 기록).
  - 병합 뒤 main 대비 차이는 140파일 +853/-8147로, 병합 전 branch의 변경과 같다. 제품 쪽이 바뀌지 않아 Unity는 다시 돌리지 않았다.
- push: 원격 branch를 `33925c73`에서 `11ba7889`로 fast-forward했다.
- PR: [PR204](https://github.com/bass131/dawnholder-server/pull/204)(본문 `lead-pr-body.md`)다. CI는 넷 다 pass다. module-boundaries 2m29s, code-rules 1m31s, architecture-tests 5m11s, dotnet-tests 18m37s다(`lead-pr204-checks-final.txt`). Unity 테스트는 CI에 없다. GitHub 상태는 MERGEABLE / CLEAN이었다.
- 승인 요청: 리드 `msg_f403653f4f98`(PR head·CI·판정 원문 셋·표본 대조·남은 우려).
- 병합(메인 `msg_a31233c60bf2`): 사용자 원문은 메인 창에서 단독 한 줄로 Enter 제출된 「병합 승인: PR204 head 11ba78898b12bec9b90167eb73655a22bebe4073」이다(메인 전달). 메인이 직전 head·상태를 대조하고 `--merge --match-head-commit`으로 병합했다. 2026-10-07T18:01:57Z, merge commit `7086d45b92b76742d8c11facbf8a1031b81d0853`이다. 메인은 R-2로 세 판정 SHA256과 결론, 재검증 results.xml, PR 파일 140개를 직접 봤다.
- 병합 뒤: 원격 branch `chore/unity-engine-upgrade-20261007`는 지워졌다(`git ls-remote` 0줄). 종료 기록은 최신 main에서 만든 `docs/unity-upgrade-closeout-20261008`에서 한다. 메인 checkout의 skip-worktree 세 파일 처리는 메인이 사용자 답을 기다리는 중이며, 이 worktree 작업과는 무관하다(메인 전달).

### 종료 Gardener — 보고서 완료, 정리 후보 2(채택 전)

Gardener `task_62754424b256`(Dispatch `ctx_67f8f8292155`, 최초 실행 `claude --model claude-opus-5-5`, 화면 「Opus 5.5 with xhigh effort」, backend unknown, 검증자 모델 시범 대상 아님)가 계약 v1(SHA256 `c6d91fdc…152e`)대로 결과 기록 commit `f1f9cc73`을 기준으로 회고했다. 보고서는 `gardener/report.md`(SHA256 `3716bec1…3b41`), worker_done은 `msg_f7ebb100d68c`이다. 쓰기는 그 한 파일뿐이고 범위 밖 쓰기는 0이다. heartbeat 5분 초과도 0이다.

- 분류: 확정 실패·CI 실패·새 경고 억제는 0이다.
  - 비차단 결함 셋(세 판정의 각 #1)은 하위 종류가 달라 묶지 않았다.
  - 사용자 환경 부작용 두 건(Hub 토큰 기록, EditorPrefs 변경)은 다른 부류로 판단했다. 하나는 기밀성, 다른 하나는 사용자 상태 변경이다. 각각 첫 발생이고 goal에 교정이 있어 후보로 올리지 않았다.
  - harness O2·O3, heartbeat 초과, 나머지 교정 기록은 이미 분류돼 있거나 한 번뿐이라 후보로 올리지 않았다.
- 후보 1(기존 후보에 근거 추가): Unity 실행 뒤 추적 파일 drift 두 종류다. SENTIS define 한 줄이 4회, `MinimapRT.renderTexture` 줄 끝이 5회 생겼고, 6.6에서는 매번 같은 hash 쌍이다. PR191 branch에만 있는 `unity-sentis-define-drift`에 근거를 더하고 MinimapRT까지 넓히는 안이다.
  - MinimapRT의 원인 근거: `.gitattributes`가 `.renderTexture`의 줄 끝을 고정하지 않는다(`eol: unspecified`, 리드가 `git check-attr`로 다시 확인).
  - 검사화(높은 층부터): `*.renderTexture text eol=lf`와 `%YAML` 추적 파일의 eol 지정 검사 → Unity 실행 뒤 「알려진 drift」 분류 단계 → 커밋 전 define 변경 경고(warning 파일럿).
  - 소유는 CodeMap·Rules 경계이고, Unity 클라이언트 주인 Content가 협의한다.
- 후보 2(새 후보): PowerShell 보고 helper에서 큰따옴표 문자열 안 `$이름` 바로 뒤에 한글 조사가 붙어 StrictMode가 멈춘 실패다. 작성자 넷이 다섯 번 겪었다.
  - 검사화: 작성자가 실행 전에 부르는 AST 기반 자기 점검 helper(이름에 한글이 든 변수 표현 찾기) → 추적 `.ps1`·`.psm1` warning 파일럿 → 계약 양식 한 줄 순서다.
  - 소유는 Rules(helper·양식)·CodeMap(정적 진단)이다.
- 리드 표본 대조(정확한 시각 미기록): `sol-remeasure` stderr의 `'$ReductionMessageId에' 변수는 설정되지 않았으므로` 문구, `sol-delete` 첫 시도 stderr의 `'$ErrorDecisionId를'` 문구, 독립 검증 뒤와 재검증 뒤 SENTIS drift 사본의 같은 SHA256(`333733a3…`), MinimapRT 사본 `7123af77…`, `.gitattributes` 속성을 확인했다.
- 정산: release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true). 이 worktree 터미널은 0이다.
- 채택과 소유는 메인을 거쳐 사용자가 정한다. 이 goal은 구현·BACKLOG 등록을 하지 않는다.

### 종료 기록 문서 실사 — 두 번 NOT PASS, 정정 뒤 새 실사 전

신규 `gpt-6-astra` xhigh 실사자(Task `task_accc3443c4fe`, Dispatch `ctx_0a9806384ac4`, 최초 실행 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 화면 「GPT-6-Astra xhigh」, backend unknown)가 계약 v1(SHA256 `00ee5d93…bfbb`)대로 HEAD `7d8c71ba`를 실사했다. 판정 원문은 `doc-audit/verdict.md`(SHA256 `789a1d48…aee8`)이고, worker_done은 `msg_6084722213ed`다.

- 범위: 종료 기록 branch의 goal·CURRENT 변경, PR204 재검증 뒤 goal 변경과 업그레이드 커밋 메시지 변경, 새 참조다. 뒤의 둘은 사후 실사라서 PR204 병합 승인을 바꾸지 않는다.
- 판정: NOT PASS, 차단 2건이다. 그 밖의 tree 동일성·CI·병합 기록·참조·Gardener 인용·커밋 메시지 차이는 원천과 맞았다.
- 결함 #1(차단): 리드가 종료 기록(goal·CURRENT)과 PR204 재검증 뒤 쓰기(goal·커밋 메시지) 전에 맥락 메모를 갱신하지 않았다. 완료 준수 칸도 「미작성」이었다.
  - 메인 결정 `msg_b8151750a5df`(메인 결정이며 사용자 결정이 아님): 「#1 → A(사후 기록으로 수용)」.
  - 리드는 정정 전 메모와 사후 보완 기록을 `astra-context-closeout.md`에 남겼다. 사후 보완은 사전 기록으로 소급하지 않는다. 아래 [교정 기록](#교정-기록)에 두 번째 발생으로 적었다.
- 결함 #2(차단): 리드 대조 시각이 손으로 적은 어림값인데 관측처럼 읽혔다. 같은 부류 열한 줄을 「정확한 시각 미기록」으로 고쳤다(아래 교정 기록).
- 리드 표본 대조(R-2): 업그레이드 커밋 `62228618`과 `71c98065`의 tree가 둘 다 `a9c1b2fa…`임을 직접 확인했다. 실사 원시 `raw/48-context-and-timestamps.txt`의 commit 시각으로 결함 #2의 두 모순을 확인했다.
- 정산: release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true).
- 재실사: 신규 `gpt-6-astra` xhigh 실사자(Task `task_a3d577f752a8`, Dispatch `ctx_04667731263d`, 화면 「GPT-6-Astra xhigh」, backend unknown)가 계약 v1(SHA256 `87f830ba…b959`)대로 정정 commit `2a6e5893`을 실사했다. 판정 원문은 `doc-reaudit/verdict.md`(SHA256 `ce202762…d41c`)이고, worker_done은 `msg_df8771f4bd2a`다.
  - 판정: NOT PASS, 차단 3건이다. 결함 #1의 사후 기록 처리는 메인 결정대로 충족으로 봤다. 열한 줄의 시각 정정도 맞다고 봤다.
  - #2: 위 「SENTIS define 한 줄 되돌림」의 리드 대조 줄이 release receipt 파일의 수정 시각을 release 시각으로만 적었다. 리드가 정정 범위를 「리드 대조(시각)」 꼴로만 찾아 놓쳤다. 「정확한 시각 미기록」으로 바꾸고, 그 값이 파일 수정 시각임을 밝혔다.
  - #3: 리드 사후 메모의 표가 병합 관문 교정 줄을 `dd383e3d`에 귀속했다. 실제로는 `f1f9cc73`에서 들어갔다. 메모를 고치고 그 파일 끝에 정정 기록을 남겼다.
  - #4: 진척 이름 「엔진 6.6 전환·AI Assistant 올림」의 화면 폭이 31이었다(goal-loop 상한 30). goal을 시작할 때부터 있던 이름이다. 「엔진 6.6·AI Assistant 전환」으로 줄였다.
  - 정산: release는 `retained/external_terminal`이었고, 대기 화면을 확인한 뒤 pane을 닫았다(ptyKilled true).
- 다음: 이 정정을 새 독립 문서 실사에 넘긴다. 마지막 실사 결과는 병합 관문 정본대로 goal에 쓰지 않고 근거 폴더와 승인 묶음으로 전한다.

### 교정 기록

첫 발생만 기록한다. 반복 규칙은 두 번째 발생부터 만든다.

- 작업자 보고 오류: 엔진 Sol이 미추적 파일 이름을 원시를 읽기 전에 `EntityIdSettings.asset`으로 보고했고(`msg_5ad15110534c`), 21초 뒤 실제 이름 `PhysicsCoreProjectSettings2D.asset`으로 고쳤다(`msg_e6bb33655c16`). 리드는 고치기 전 이름을 메인에 전달했다가(`msg_1ae62a1c1a38`) git status 원시로 확인하고 정정했다(`msg_0070907addae`). 다음 계약의 차단 항목에 「원시를 읽기 전의 보고」를 넣었다.
- 리드 계약 이탈: 엔진 Sol 계약은 「리드의 쓰기는 끝났다」고 적었지만, 리드가 작업 중에 이 goal에 메인 범위 판정을 기록했다. 기록한 정확한 시각은 남기지 않았다. 그 판정 메시지 `msg_98f1c822d84e`의 `created_at`(2026-10-07T09:49:48Z) 뒤이고, 작업자 질문 `msg_2003e93b047a`의 `created_at`(10:05:07Z) 전이다. 작업자가 최종 status에서 발견해 그 질문으로 물었고, 리드가 소유를 확인해 보고에서 분리했다. 다음부터 작업자 실행 중 리드 기록은 근거 폴더에만 두고 추적 파일은 정산 뒤 쓴다.
- 컴파일 오류 보고 누락: 첫 열기 로그의 UAC0005 3줄을 엔진 Sol 보고·분류표가 빠뜨렸다(오류 추출이 CS 코드만 보았다). 리드는 보고된 줄만 표본 대조해 놓쳤고, 메인은 「오류는 테스트 한 줄」이라는 보고 위에서 범위를 판정했다. 재열기 1회 뒤에야 드러났다. 리드가 메인에 즉시 보고했고(`msg_d560680ed27c`), 메인도 로그를 직접 보지 않았던 사실을 사용자에게 바로잡아 알렸다(`msg_0f223740ea22`). 교정: 다음 계약과 리드 원시 대조에서 로그의 error 줄을 진단 코드 구분 없이 전부 뽑아 보고와 개수를 대조한다.
- 커밋 구성 누락: 리드가 업그레이드 커밋을 만들 때 측정 tree를 그대로 넣으면서, 위험표가 「커밋에서 뺀다」고 정한 SENTIS define drift를 빼지 않았다. 리드가 검증자 계약을 쓰다가 스스로 찾아 메인에 올렸다(`msg_a5a934c8efd3`). 교정: 커밋을 만들기 전에 base 대비 diff를 위험표의 기계 로컬 값 목록(cloud 필드, SENTIS define)과 대조한다.
- 리드 직접 편집 시도: 그 한 줄을 고치려고 리드가 제품 설정 파일 편집 명령을 냈고, Claude Code 권한 분류기가 거부했다(「Modify Shared Resources」, 명령은 실행되지 않음). 제품 파일 편집은 Sol 몫이다. 리드는 우회하지 않고 HEAD를 같은 tree로 되돌려 상태를 보존한 뒤 메인에 보고했다. 메인 결정(`msg_7146de24b2de`)대로 Sol에게 맡겼다.
- heartbeat 5분 간격 초과가 여섯 작업자 연속으로 반복됐다(측정 Sol, 관계 점검 Sol, 삭제·측정 Sol, 재측정 Sol, SENTIS Sol, 독립 검증 Opus).
  - 둘째 계약부터 「오래 걸리는 실행·보고 작성 전에 먼저 보낸다」를 넣었지만 막지 못했다.
  - 삭제·측정 Sol은 초과를 보고에 적지 않았다. 재측정 Sol, SENTIS Sol, 독립 검증 Opus는 적었다.
  - 초과 폭은 1분 30초 이하였고, 작업 결과나 정산에 영향은 없었다.
  - 반복 규칙은 heartbeat를 소유한 live preamble·ORCA 정본 층의 일이다. 그래서 이 goal에서는 기록만 하고 메인에 알린다.
- 원시에 들어간 Hub 토큰: MCP 검증자가 Editor 프로세스의 명령줄을 원시로 남길 때, Hub가 넘긴 `-accessToken`·`-hubSessionId` 값이 첫 M3 기록 한 곳에 들어갔다. 검증자가 스스로 찾아 바로 가렸고, 측정 스크립트도 그 뒤 자동으로 가리게 고쳤다. 근거 폴더는 로컬이라 push되지 않는다. 리드 계약은 명령줄 기록을 요구하면서 가리기 조건을 두지 않았다. 교정: Hub가 띄운 Editor의 명령줄을 기록하게 하는 다음 계약에는 두 인자의 값을 가린다는 조건을 넣는다.
- 결정 답 수신 지연: 리드가 우편함을 기다릴 때 깨우는 유형(`--types`)에서 `dispatch`를 빠뜨렸다. 메인이 사용자 결정 A를 dispatch 유형(`msg_3d959b3798a7`, 16:49:37Z)으로 보냈는데, 리드는 메인이 다시 알린 17:13:41Z(`msg_033db43efc8e`) 뒤에야 읽었다. 그동안 리드는 메인에 대기 알림을 보냈다. 약 24분 늦었고 작업 결과에는 영향이 없다. 교정: 리드의 우편함 대기는 `--types`에 `dispatch`를 넣거나, 결정 답을 기다릴 때는 유형을 거르지 않는다.
- 사용자 EditorPrefs 변경: 재검증 계약이 선택 실행으로 허용한 6.6 batch `-createProject`가 사용자 EditorPrefs 세 값을 지운 임시 경로로 바꿨다. 계약은 이 부작용을 예상하지 못해 실행 전 값을 남기게 하지 않았다. 그래서 원래 값은 모르고 복원하지 않았다. 앞선 독립 검증 계약도 같은 실행을 허용했다. 교정: `-createProject`를 허용하는 다음 계약은 그 세 값의 전후를 원시로 남기고 원래 값으로 되돌리게 하거나, 그 실행을 빼고 다른 근거를 쓴다.
- 진행 중에 바뀐 병합 관문 정본: 이 PR이 진행되는 동안 main에 병합 관문 정본 갱신(PR199)이 들어왔다. 새 정본은 승인 묶음에 두 가지를 요구한다. 하나는 main 통합 때의 remerge-diff·같은 제품 blob 근거이고, 다른 하나는 검증 통과 뒤 goal·문서를 고쳤을 때 최종 head의 바뀐 부분을 한 번 재실사한 기록이다. 리드는 승인 요청 때 이 갱신을 읽지 않았다. 그래서 remerge-diff는 병합 뒤에야 남겼고, 재검증 뒤 고친 goal 결과 기록과 결함 #1 문구에 대한 재실사 기록은 없다. 그 변경은 문서와 커밋 메시지뿐이고 tree의 제품 부분은 재검증한 것과 같다. 교정: main 통합 뒤에는 병합 관문 정본을 다시 읽고 승인 묶음을 그 기준으로 만든다.
- 리드 맥락 메모 사후 작성(두 번째 발생): 리드가 종료 기록 commit `f1f9cc73`·`7d8c71ba`(goal·CURRENT)와 PR204 재검증 뒤 쓰기(goal, 업그레이드 커밋 메시지)를 하기 전에 맥락 메모를 갱신하지 않았다.
  - 리드 메모 `astra-context.md`는 업그레이드 커밋을 로컬로 만든 기록에서 멈췄고, 준수 연결의 완료 칸은 「미작성」으로 남았다.
  - 종료 기록 문서 실사가 차단 #1로 찾았고, 메인 결정 `msg_b8151750a5df`로 사후 기록으로 받아들였다. 사후 보완은 `astra-context-closeout.md`의 「사후 보완」 절에 작성 시각과 함께 남겼다. 기존 메모의 칸은 고치지 않았다.
  - 첫 발생은 [운영툴 기록 원본 일원화 goal](../../../05_Management/goals/2026-10-06-record-source-unification/goal.md)의 PR202 실사 Z1(리드 메모 사후 작성)이다(메인 전달). 반복 규칙의 층 판단은 Rules 몫이고, 메인이 이 판정 경로를 근거로 넘긴다. 이 goal은 기록만 한다.
- 리드 대조 시각의 수기 기록: 결과 절의 「리드 대조」 시각 열한 개는 리드가 대조할 때 `date -u`로 남긴 값이 아니라, 기록을 쓸 때 손으로 적은 값이었다.
  - 재검증 뒤와 Gardener 뒤 두 줄에 적었던 범위(17:36~17:40Z, 18:24~18:26Z)는 그 줄을 담은 commit의 commit 시각보다 늦어 원천과 모순됐다. commit 시각은 `dd383e3d`가 2026-10-07T17:35:49Z, `7d8c71ba`가 18:24:18Z다(`git show -s --format=%cI`).
  - 종료 기록 문서 실사가 그 둘을 차단 #2로, 독립 검증 뒤와 MCP 확인 뒤 두 줄을 범위 밖 알림으로 냈다. 리드가 같은 부류 일곱 줄을 더 찾아 열한 줄 모두 「정확한 시각 미기록」으로 고쳤다. 위 「리드 계약 이탈」의 어림 시각은 메시지 `created_at` 두 값 사이로 바꿨다.
  - REPORTING 「수치와 원시 근거」의 「수기로 채운 상수나 예상값을 실제 계측처럼 쓰지 않는다」를 어겼다.
  - 재실사가 같은 부류 한 줄을 더 찾았다. 「SENTIS define 한 줄 되돌림」의 리드 대조 줄이 release receipt 파일의 수정 시각을 release 시각으로만 적었다. 리드가 정정 범위를 「리드 대조(시각)」 꼴의 grep으로만 찾아 놓쳤다. 이 줄도 고쳤다.
  - 이 goal 밖에서도 반복됐다. 아이템 goal의 리드 메모(`content-active`의 `.backups/verification/2026-10-05-items-inventory-currency/astra-context.md` 468행)에 2026-10-05 「시각 표기 정정(두 번째 발생)」이 있다. 같은 Content 리드 역할이다. 반복 규칙의 층 판단은 Rules 몫이므로 메인에 근거로 넘긴다.
  - 교정: 결과에 시각을 적을 대조는 시작할 때 `date -u` 출력을 근거 폴더에 남기고 그 값만 옮긴다. 남기지 않았으면 시각을 적지 않는다.

## 다음 계획 후보

- `04_ClientNet/Dawnholder.Client.Net.csproj`의 「Unity 6.4 LTS」 주석 표기 — 이 goal의 「하지 않을 것」(04_ClientNet 변경 금지). 출처 초안 v2.
- 엔진이 강제하지 않아 그대로 둔 패키지의 새 버전 목록 — 승인 2A. 업그레이드 뒤 실제 목록을 여기에 적는다.
- 6.7 LTS 이동 검토 — 6.7 LTS는 연말 예정(공식 페이지, 날짜 미정). 11월 마감 뒤 판단.
- Cainos 에셋을 우리 아트 스타일의 새 sprite·애니메이션으로 재창조. 그때 Design 파트(worktree)가 필요하고, R-1 추가 파트는 사용자 승인 대상이다. 사용자 원문 「음 사실 아트파일만 필요하고, 솔직히 코드는 필요 없긴한데」(메인 `msg_0af85d53aa50`)와 「… 나중에 우리가 새로 sprite 에셋이나 애니메이션을 새로 우리 게임 아트스타일로 재 창조해야할거야, 그때 Design 워크트리가 또 필요할거임」(메인 `msg_626eb68b0772`). 이번 goal에서 착수하지 않는다.
- fixture 복원의 조용한 생략(독립 검증 설계 관찰 O1): `MapEntryPlayFixture.cs` 364~365행은 원본과 사본이 모두 죽어 있으면 복원을 건너뛰고 오류를 남기지 않는다. 그러면 런타임 사본 설정이 다음 테스트로 샐 수 있다. 도달 경로는 관측되지 않았다(추론). 대안은 그때 `InvalidOperationException`을 던져 정리 오류로 모으는 것이다. Content 후속 후보이며 이 goal에서 고치지 않는다. 출처 `opus-verify/verdict.md`.
- 근거 폴더 harness의 Before gate 한 번 제한(독립 검증 설계 관찰 O3): `harness/Save-BaselineState.ps1` 104~107행은 근거 폴더마다 Before gate를 한 번만 쓴다. 그래서 독립 테스트를 더한 2회차 측정에 쓸 수 없었다. 이번에는 리드 승인(`msg_930075533294`)으로 읽기 전용 상태 기록으로 대신했다. 라벨별 gate를 받게 하는 것은 이 harness를 다시 쓸 goal의 후보다.
- harness의 음소거 절차 중복(재검증 설계 관찰 O2): 음소거 쓰기·복원이 `harness/Run-UnityTests.ps1`, `opus-verify/New-TempProbeProject.ps1`, `harness/New-SettingsProbeProject.ps1` 세 곳에 따로 있다. 이번 실행의 동작 차이는 없다. `Baseline.Common.ps1`의 함수 하나로 모으는 것은 이 harness를 다시 쓸 goal의 후보다. 출처 `opus-reverify/verdict.md`.
- PR191 branch BACKLOG의 `unity-sentis-define-drift` 원인 갱신 — 이번에 원인이 `com.unity.ai.inference`의 `AnalyticsDefineManager.cs`(사용자 분석 설정에 따라 define을 넣고 뺌)로 드러났다(위 「SENTIS define 한 줄 되돌림」). 그 후보는 PR191 branch에만 있어 이 goal에서 고치지 않는다. 출처 메인 `msg_7146de24b2de`.
- Content 게임 goal(아이템 PR191 보류분·던전)은 이 goal의 다음이 아니다. Rules 운영 셋업 2·3단계(TDD 정본화·검증자 임시 쓰기 경계, 넘긴 후보 도착 확인)가 병합된 뒤에 연다. 그 첫 계획에는 기존 마일스톤 규칙([milestones](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md))대로 Core와 묶은 「11월 마감 고리」 로드맵 초안을 넣는다. 사용자 원문 「대시보드 결정 응답: 1) Core·Content 재개 시점 - 최소 운영 셋업 몇 단계 뒤에 게임 goal을 다시 열지 → A 3단계 뒤 재개」(메인 `msg_de382995358d`, 메인 전달이며 직접 입력으로 격상하지 않는다).
