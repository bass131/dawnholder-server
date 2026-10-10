# Fable 설계 검토 — DB 도구 컨테이너 전환 (구현 전 불변식)

이 문서는 [R-7 Fable 구현 전 설계 검토 시범](../../../00_Document/operations/ORCA.md#r7-fable-pilot)의 세 번째 산출물이다. 리드의 goal 보완과 메인의 원문 확인 입력이며 구현 발행 승인이 아니다. Docker·WSL·SQL·`99_Tools/database` 스크립트를 실행하지 않았고, 모든 수치는 측정값이 아니라 파일에서 읽은 값 또는 전제다. 평가 기준은 「메인과 리드가 놓친 문제를 실제로 찾았나」다.

# 작업 전 맥락

- 역할·자기 태그 / 할당 작업 / 지정 모델·관찰 모델·backend unknown 여부: `[Core 검증자]`, 신규 외부 Fable 설계 검토자(Opus 독립 구현 검증 아님). 할당 작업 하나 = 「도구 컨테이너 전환 PR」과 그 PR이 컨테이너 1·2단계 실행·정리에 넘기는 계약의 구현 전 검토(실패 수명·보호 집합·오류 분류·비용 상한 4범주). 지정 모델 `claude-fable-5-1`, 관찰 모델 Fable 5.1(세션 자기 보고), backend `unknown`. 모델 자기 보고는 실제 backend 증거가 아니다.
- 현재 goal·할당 계약의 경로 / 작업 경로·branch·base·HEAD: goal `01_Phases/goals/2026-10-04-persistence-integration/goal.md`, 계약 `.backups/verification/2026-10-04-persistence-integration/r7-container-tools-review/contract.md`(v1, 2026-10-10, SHA256 `f626e1a54fd1267962b7db0b1924b227dabb0fdb4e7349a06649cb6cbd941e3d` 관측 일치, 109줄). 작업 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-active`, branch `feat/persistence-engine-judgment-20261006`, 고정 HEAD `b2e7797b3aeffc330d529c92e8f8f85a9310aed9`(관측 `git rev-parse HEAD` 일치, `git status --porcelain` 0줄). base는 계약이 따로 적지 않았고 PR base는 도구 전환 PR을 열 때 정한다.
- 메모 작성 시점(시계 명령 출력의 시각) / 대상 파일 첫 쓰기 전 여부: `date -u` 출력 2026-10-10T08:22:24Z. 이 파일의 첫 쓰기가 이 메모다(메모만 먼저 기록 → 검토 본문을 이어 씀 → 끝에 「준수 연결」·「자기 쓰기 감사」 갱신). 쓰기 직전 `test ! -e`로 부재를 확인했다. 첫 시도인 Bash heredoc 쓰기는 셸 구문 오류로 파일을 만들지 못했고(부재 재확인 뒤) Write 도구로 썼다.
- 기존 사용자 변경·다른 작성자의 소유권 / 허용 쓰기·실행·금지 범위: 작업 트리 clean, 미커밋 변경 0. 리드의 goal 쓰기는 HEAD commit으로 끝났고 검토 중 goal을 쓰지 않는다(계약). 허용 쓰기는 이 파일 하나와 기동 때 지정한 TEMP `C:\Users\bass1\orca\workspaces\DawnHolder_Project\core-active\.backups\tmp\core-r7` 아래뿐이다. 제품·테스트·goal·문서·설정 쓰기, Docker·WSL·SQL 명령, `99_Tools/database` 스크립트 실행(오프라인 시험 포함), 설치, 설정 변경, 추가 위임(Agent 도구 포함), commit·push 금지. 읽기 전용 Git·파일 읽기·텍스트 검색만 했다.
- 원문 요구/후속 보충의 계약 버전·시각 / 고정 입력 경로·hash / 적용 단계: 계약 v1(2026-10-10). 고정 입력 SHA256 관측 일치: goal.md `0e76c2cb88e2bd3ce4d89304312d18a1c12676c19aa4fccc3c03e7ca43f4976f`(HEAD `b2e7797b`의 `git show`와 작업 사본 둘 다), 승인 본문 `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-04-persistence-integration/opus-lead-entry/2026-10-10-main-sync/scope-redraft-body.txt` `6ebf9f802fd3e4144a2ec227ea3862e0c9d416f2880a0afa39fb4861f6567f9d`, CODE_CONVENTION `2e233bdacbf55b22d3c7796313766630c4c1c4ddfbc5fdf68d81cc6e848f20d0`. 리드의 경로 기계 확인은 같은 폴더 `path-check.txt`(utc 08:05:27Z, exit 0, 출력 부재 확인)다. 적용 단계 = 구현 전 설계 검토(구현 계약 발행 전). 후속 보충 지시 없음(쓰기 전 `check` 결과 메시지 0건).
- 검증 등급·이유와 파일/줄 수 원시 근거 / R-7 해당·비해당과 이유: 구현 전 설계 검토라 코드 등급 대상이 아니다. 검토 대상 도구 여섯 파일은 HEAD 기준 2672줄이다(`wc -l` 관측: Database.Common 264, Environment.Common 1213, Set-TestPrincipals 515, Remove-TestEnvironment 475, Configure-WslAccess 138, Test-WslAccess 67). R-7 해당. 메인 판단 `msg_446a9239bf3e`가 시범 세 번째 자리를 이 PR에 배정했다(goal L13). 이 결과는 구현 계약의 고정 입력이 된다.

## 읽기와 선택

모든 저장소 파일은 commit `b2e7797b` 기준이며 미커밋 변경은 없다. 줄 번호는 이 commit의 `cat -n`·`sed -n` 관측값이다. 저장소 밖 근거 파일은 읽은 시각의 내용이다.

| 읽은 파일·절 | commit 또는 미커밋 hash | 이 작업에서 필요한 규칙·경계 |
|---|---|---|
| `AGENTS.md`, `CLAUDE.md`(세션 지침으로 수신) | `b2e7797b` | 역할 경계(검토자는 goal-review만 씀), 모델 라우팅, 공학 조건(전역 설정 금지·틱 I/O 금지), 같은 산출물 3회 체크포인트(L27) |
| `.agents/skills/dawnholder-task-context/SKILL.md` 전체, `references/templates.md` L5-45 「맥락 메모」 | `b2e7797b` | 사전 메모 양식, 읽기 상한(계약 지정 + 예시 1~2개), R-7에서만 불변식 파일과 전제 표식 연결 |
| `00_Document/operations/ORCA.md` L189-204 R-7 | `b2e7797b` | 시범 절차 6단계, 출력은 goal-review.md 하나, 평가 기준 |
| goal L5-14(적용 중인 사용자 결정 첫 항목), L110-125(진척 단계), L226-262(현재 목표 우선·다음 계획 후보), L448-502(현재 범위·PR 경계와 하위 절), L504-516(개정 전 범위), L542-583(재개 지점 최신 두 블록), L584-746(DB 생성 1단계 재시도 계획: 실패 원인과 3회차 결과만), L1160-1234(옛 절: 범위와 보존 동작·G0·G1·G2·작업 순서·완료조건·G4) | `b2e7797b` | 정본 범위·보존 동작·접속 경계·검증 등급·순서와 미결, Windows 1단계의 실제 실패(③ 중복 실행, ⑧ MachineName 대소문자, ④ diskpart)와 기록된 엔진 값 |
| 승인 본문 `scope-redraft-body.txt` 전체 | SHA256 `6ebf9f80…` | 다섯 질문 원문·선택지·추천, 순서, 권한 경계 원문, 설계 결정 기록 계획 |
| `99_Tools/database/Database.Common.ps1` 전체 | `b2e7797b` | `Open-LocalDatabase` L6-40(lpc·통합 인증·MachineName 비교), `Invoke-Migrations` L150-264(자체 transaction·lock) |
| `99_Tools/database/test-environment/Environment.Common.ps1` 전체 | `b2e7797b` | 승인 계획 검증 L36-120(PlanVersion 1·Instance·Endpoint·RecoveryPrincipal·ExecutorSid), manifest 상태·필드 L215-413, 실행자 L478-496, 로컬 계정 부재 L498-512, 안전 사유 목록 L572-736, 연결 L895-951, DB identity L953-1003, ACL·비밀 파일 L1047-1179 |
| `99_Tools/database/test-environment/Set-TestPrincipals.ps1` 전체 | `b2e7797b` | 관리자 요구 L60-63, 경로 부재 L80-90, DPAPI 저장 L136-186, Windows 계정 L187-220, SQL login SID L222-318, role 검증 L383-432, 실패 처리 L489-498 |
| `99_Tools/database/test-environment/Remove-TestEnvironment.ps1` 전체 | `b2e7797b` | 정산 파일 L21-72, 자원 identity L74-134, quiescent L136-219, 관리자 요구 L246, 정리 순서 L283-425, 실패 저널 L426-464 |
| `99_Tools/database/Configure-WslAccess.ps1`, `Test-WslAccess.ps1` 전체 | `b2e7797b` | 삭제 대상의 부작용 범위: 레지스트리 변경 L17-33·L88, 상태 폴더 L14-16, Restore L100-138, 포트 listener 검증 L92-95 |
| `99_Tools/database/test-environment/New-TestDatabase.ps1`, `Initialize-CharacterBinding.ps1`, `Install-Database.ps1`, `Test-Database.ps1`, `SqlError.Common.ps1` 전체 | `b2e7797b` | 진입 스크립트의 실행자·연결 호출 순서, Create 때 엔진 관측 기록 L76-80, 2단계 도구의 `Open-LocalDatabase`·`Instance` 의존 L13-16·L51-54·L131, SQL 번호만 남기는 오류 경계 |
| `99_Tools/database/tests/TestSupport.ps1` 전체, `Invoke-OfflineTests.ps1` 전체, `TestEnvironmentLifecycle.Tests.ps1` L140-215·L355-400·L480-505·L1025-1045·L1235-1260·L1390-1405·L1750-1765, `TestDatabaseContract.Tests.ps1` L200-225·L905-930, `EnvironmentGuards.Tests.ps1` 헤더·L315-380(grep) | `b2e7797b` | 가짜 SqlClient 경계, 차단 목록(L380-396), lpc·통합 인증 단정(L1243-1258), 8개 스위트와 results.json 계약, PR215가 넣은 MachineName 경계 시험 |
| `00_Document/operations/MSSQL.md` 전체 | `b2e7797b` | 실행 host PS 5.1(L7), 도구 표(L15-22), recovery Windows principal 계약(L67), manifest schema 1(L92), WSL 역사와 Restore 절차(L165-199) |
| `00_Document/ADR/tech-stack/ADR-005-mssql-efcore.md` 전체, `ADR-029-wsl2-dotnet-execution-standard.md` L20-45 | `b2e7797b` | 통합 인증 근거(L18-20)·컨테이너 비호환 트레이드오프(L30-31), ④ 명시 이월(L31) |
| `01_Phases/goals/2026-10-05-items-inventory-currency/goal-review.md` 머리·§0~§2 일부·§3·§6·§10 | `b2e7797b` | 형식·절 구성 참고. 내용 결론은 따르지 않음 |
| `00_Document/conventions/CODE_CONVENTION.md` 「상태와 책임」·「파일 위치와 이름」·「SQL·PowerShell 작성」(계약 원문) | SHA256 `2e233bda…` | 단일 소유자·수명·실패 처리 계약, 새 이름에 날짜·마일스톤 금지, 반복 검사 한 곳 소유 |

- 같은 책임의 예시 1~2개(경로·심볼/절) / 따를 이름·서식·오류·주석 관례: 연결·신원 대조의 기존 예시는 `Open-TestEnvironmentDatabase`(Environment.Common L895-951)와 `Open-LocalDatabase`(Database.Common L6-40)다. 포트 listener 검증 관례는 Configure-WslAccess L92-95다. 안전 사유 문자열 관례는 Environment.Common L572-663(제품 리터럴 전수 목록)이다. 이 문서는 그 관례를 불변식의 「확인 방법」에 재사용한다.
- 재사용 helper·계약 / 재사용하지 않거나 중복이 필요한 이유: DPAPI 파일·ACL·hash 대조(`New-TestEnvironmentOwnedFile`·`Assert-TestEnvironmentSecretFile`)와 저널(`Start/Complete/Fail-TestEnvironmentStep`)은 컨테이너에서도 Windows 쪽 파일이라 그대로 재사용 가능하다고 본다. Windows 계정·관리자 권한 helper(`Assert-TestEnvironmentLocalAccountAbsent`, `-Administrator`, `New-LocalUser`)는 재사용 대상이 아니다.
- 영향 파일·사용처·보존 동작: 위 표의 도구·시험·문서. 보존 동작은 goal L461-468 그대로다. 이 검토는 파일을 바꾸지 않는다.
- 신규 파일 위치·이름 / 기존 경로·명명 관례 근거 / 새 폴더 필요 이유: 이 파일은 계약이 지정한 goal 폴더의 `goal-review.md`다. 첫 R-7 산출물과 같은 이름·위치 관례다. 새 폴더 없음.
- 읽기 상한을 넘긴 추가 경로·구간과 이유: (1) `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-04-persistence-integration/container-risk-check/contract.md`(SHA256 `5dc11da4…47e9`, 위험 확인 시험 계약 v1, 08:10Z 기동)와 같은 폴더 `path-check.txt`·`launch-time.txt`·`split-receipt.json`. 이유: 계약 요구항목 4(위험 확인 시험에서 더 재야 할 값)는 이미 발행된 시험 계약과 대조해야 빠진 값을 말할 수 있다. (2) `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-04-persistence-integration/g2-stage1-retry-01/approval-plan.execution.json`(비밀 없는 승인 계획). 이유: goal이 「실행 승인 계획 파일 형식이 바뀐다」고만 적어 실제 필드를 봐야 바뀔 필드와 보호할 옛 경로를 특정할 수 있다. (3) `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\` 폴더 목록과 `state.clixml`의 비밀 아닌 필드 다섯 개(InstanceId·Login 이름·Database·Port·Restored). 이유: 삭제 대상 Configure-WslAccess.ps1이 남긴 적용 상태가 아직 살아 있는지가 보호 집합 판단을 바꾼다. `credential.clixml`은 읽지 않았다. (4) `01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md` §2(L26-52). 이유: MSSQL.md L67이 recovery 주체 계약의 출처로 가리킨다. (5) `.github/workflows/` 목록과 `code-rules.yml` L40-80, `git grep Invoke-OfflineTests`. 이유: 완료조건의 「CI」가 무엇을 가리키는지 확인. (6) `00_Document/operations/BACKLOG.md` L49(`sql-environment-human-access`), `.gitattributes` 머리, `verify-schema.sql`·migrations의 `COLLATE`·`WINDOWS` grep. 이유: 사람 조회 공존·줄 끝·정렬 규칙 의존 확인. 저장소 전체 훑기·과거 대화·전체 로그 수집은 하지 않았다.
- 언어 규칙 부재·임시 기준과 승인 상태(해당 없으면 이유): PowerShell·SQL 절은 CODE_CONVENTION에 있다. Docker·컨테이너 운영 규칙 절은 저장소에 없다(`git ls-files`에 docker·compose 파일 0개). 이 문서의 Docker 관련 불변식은 임시 기준이며 리드 보완·메인 승인 전까지 정본이 아니다.
- 관련 규칙 절 선택 / 해당 없는 절·제외 이유(관련 없는 원문을 붙이지 않음): 계약이 붙인 「상태와 책임」(단일 소유자·수명·실패 처리)·「파일 위치와 이름」(새 컨테이너·볼륨·DB 이름)·「SQL·PowerShell 작성」(검토 대상 코드)을 적용한다. 「Unity와 이름」·「C# 공백 서식」·「TypeScript·Electron」·「Python 도구」·「빌드가 검사하는 규칙」·「주석과 문서」·「역할별 적용」·「변화 평가」는 계약의 제외 이유 그대로 제외한다.
- 열린 질문·차단 / 질문할 담당자 / 완료 때 처리 근거: 검토 중 ask는 보내지 않았다(계약 SHA·고정 입력이 모두 일치했고 막힌 것이 없었다). 설계·정책 질문은 §7에 담당자 표시와 함께 둔다.

## 준수 연결

| 적용 규칙·출처 절 | 준수 계획 | 완료 뒤 실제 파일·심볼/절·근거 | 계획 변경 이유 |
|---|---|---|---|
| 작업 맥락 스킬 「파일 쓰기 전 메모와 원문 계약」 | 이 파일 첫 쓰기를 메모로 시작 | 이 파일 「작업 전 맥락」(첫 쓰기, Write 도구 1회), 검토 본문은 그 뒤 Edit 3회로 §0~§10 | Bash heredoc 첫 시도가 셸 구문 오류로 실패해(파일 미생성, 부재 재확인) Write 도구로 바꿨다. 내용·순서는 같다 |
| 계약 「산출물 구성」 1~6(맥락 메모 → 불변식 목록 → 빈틈·모순 → 위험 확인 시험 추가 측정 → 열린 질문 → 준수 연결·자기 쓰기 감사) | §0~§10으로 작성, 불변식마다 번호·한 문장·범주·전제 표식·깨졌을 때·확인 방법·대안 | 맥락 메모(머리), 불변식 §2 INV-01~INV-22(각 항목에 범주·전제·깨졌을 때·확인 방법·대안), 빈틈·모순 §5 G-1~G-15(위치·문제·대안과 영향), 추가 측정 §6 M-1~M-15, 열린 질문 §7 Q-1~Q-11(담당자 열), 준수 연결(이 표)·자기 쓰기 감사 §10. 요약 §0, 보호 집합 §1, 오류 분류 §3, 비용 상한 §4, 비차단 관찰 §8, 미검토·미실행 §9는 계약 항목을 읽기 쉽게 나눈 보조 절이다 | 없음 |
| 계약 「관찰 가능한 완료와 차단」: 전제마다 [소스]/[추론]/[미측정], [소스]는 실제 파일과 줄 또는 msg ID, 미실행은 미측정 | 모든 전제에 표식, 수치는 파일에서 읽은 값만 | 각 INV의 「전제」 줄과 §5·§6·§7의 근거 열. `grep -c` 관측: `[소스]` 24곳, `[추론]` 25곳, `[미측정]` 13곳. Docker 동작 전제는 전부 `[추론]`이고 §6 측정 항목으로 돌렸다. 실행 0회는 §9 「미실행」 | 없음 |
| CODE_CONVENTION 「상태와 책임」(단일 소유자·수명·실패·취소·종료 처리 계약) | 실패 수명·보호 집합 불변식에 반영 | §1 보호 집합 표의 「소유자」 열, INV-01~INV-05(수명·실패 보존), INV-08·INV-09(옛 자원 소유·되돌림), §3 「멈춤 지점·보존」 열 | 없음 |
| CODE_CONVENTION 「파일 위치와 이름」(날짜·마일스톤·작업자 이름 금지, 승인·환경 값은 manifest·설정으로) | 새 이름·승인 계획 필드 제안에 반영 | INV-17(timeout은 플랜 값), INV-20 대안(회차 접미 금지), §8 O-1(플랜·manifest v2 필드, 이름 규칙), INV-09 대안(legacy 폴더 대신 commit 참조) | 없음 |
| CODE_CONVENTION 「SQL·PowerShell 작성」(반복 검사 한 곳 소유, 이름 있는 인자, 오류 번호 보존, 전후 대조) | 오류 분류·연결 builder 통합·시험 대조 항목에 반영 | INV-14·INV-16(번호 보존·사유 목록 전수 일치), §3 표, §8 S-2(연결 builder 하나), G-6(전후 대조 대상 줄 범위) | 없음 |
| AGENTS 「공학 조건」(전역 설정 금지, 틱 I/O 금지, 미실행을 성공으로 보고하지 않음) | Docker 설정·.wslconfig 변경을 요구하지 않는 상한 설계, 미측정 구분 | INV-18(전역 설정 불변), §8 S-5, §9 「미실행」. 틱 I/O는 이 PR 범위 밖(게임 서버 연결은 다음 goal)이라 해당 없음으로 §9에 적었다 | 없음 |

- 자기 쓰기 감사(이 세션이 쓴 모든 경로 / 허용 밖 쓰기를 하나씩 위반으로 나열 / 위반 0이면 확인 방법): §10에 전부 적었다.

---

# 검토 결과

표기: `[소스]`는 실제 파일과 줄(또는 msg ID), `[추론]`은 소스 없이 세운 전제와 그 이유, `[미측정]`은 실행해야 알 수 있는 값이다. 「goal L…」은 `goal.md`의 HEAD `b2e7797b` 줄이고, 「시험 계약 L…」은 위험 확인 시험 계약 `container-risk-check/contract.md`의 줄이다. Docker 동작에 관한 전제는 저장소에 정본이 없어 모두 `[추론]`이며 §6에서 측정 항목으로 돌려 놓았다.

## 0. 요약

근거가 충분한 지적(goal 보완 전 고정을 권함) 14건, 설계 선택 5건, 정책 입력 4건이다. 번호는 유지하며 세부는 §2의 INV 번호와 §5의 G 번호로 잇는다.

| # | 지적(근거 충분) | 연결 |
|---|---|---|
| F-1 | 삭제 예정인 `Configure-WslAccess.ps1`은 2026-09-29 Enable이 SQLEXPRESS에 적용한 레지스트리 변경(mixed 인증, TCP 14330 loopback)과 로그인 `Dawnholder_Dev_Wsl_688ea8c6f1bd`의 **유일한 Restore 경로**인데, 그 상태 파일 `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\state.clixml`이 지금 `Restored=false`로 살아 있다. 질문 5 A(보존·동결)는 처분을 뒤로 미룬 것이지 되돌릴 길을 버린 것이 아니다. PR에서 파일을 지우려면 Restore에 쓸 commit과 절차를 goal·MSSQL.md에 먼저 박아야 한다. | INV-09, G-7, P-C |
| F-2 | 「동명 자원의 채택·DROP·재생성 금지」(goal 보존 동작 L463)와 질문 4 A의 「이름 정한 컨테이너 하나·볼륨 하나」, 시험 계약 7단계 「정지 → 삭제 → 같은 이름·같은 볼륨으로 다시 만들기」가 서로 어긋난다. 한 번 수명의 단위(DB+볼륨)와 재생성 가능한 단위(컨테이너 프로세스)를 나눠 적어야 1단계 실패 뒤 재시도와 위험 확인 시험의 이름 재사용이 규칙 위반이 되지 않는다. | INV-01, INV-20, G-1 |
| F-3 | 서버 정렬 규칙(collation)은 컨테이너의 **첫 초기화 때** `MSSQL_COLLATION`으로 고정되고 볼륨에 남는다. goal 미결(L502)은 「엔진 기준값(버전·정렬 규칙) 수용을 동작 검사 전에 묻는다」고 했는데, 1단계 DB를 담을 컨테이너를 만드는 시점이 그보다 앞이라 결정 시점이 늦다. Windows 기록은 `Korean_Wansung_CI_AS`(goal L731), 리눅스 기본은 다를 가능성이 크다. | INV-21, G-3, P-B |
| F-4 | 리눅스 컨테이너의 SQL Server는 기본 인스턴스 하나라 `SERVERPROPERTY('InstanceName')`이 NULL이고 `MachineName`은 컨테이너 hostname이다. 현재 「정확 대상」 판별(`Instance -ceq '.\'+InstanceName`, L99; `Machine`·`InstanceName`·sysadmin 대조, L938-941)은 통째로 재설계 대상이다. 대체 신원은 플랜이 정한 hostname(`--hostname`)과 첫 접속 때 기록한 master `database_guid`(볼륨 교체 감지)다. | INV-07, G-2 |
| F-5 | Docker의 `--filter name=`은 부분 일치라 `dawnholder-sqlserver`로 거르면 `dawnholder-sqlserver-data`나 뒤에 글자가 붙은 다른 컨테이너도 걸린다. 보호 집합(사용자 이미지 `nousresearch/hermes-agent`와 그 컨테이너)의 판별은 `docker inspect <정확한 이름>` 또는 `^/이름$` 고정 정규식으로만 해야 한다. 시험 계약 L15는 `--filter name=...`을 허용 명령으로 적었다. | INV-06, §6 M-1 |
| F-6 | 승인 계획은 `PlanVersion`·manifest `SchemaVersion`이 1로 고정돼 있고(L76-81, MSSQL.md L92), 필드 11개가 Windows 전용이다. 형식을 바꾸면 Windows 시험 DB의 기존 manifest(`…\2026-10-02-persistence-repository\fixture-manifest.json`, State Baseline001)를 새 도구가 읽지 못한다. 질문 5 A의 「정리는 나중에」를 지키려면 옛 DB 정리에 쓸 도구 개정(commit)과 새 플랜의 별도 경로를 goal에 적어야 한다. | INV-08, G-10 |
| F-7 | recovery 주체를 「전용 비관리자 Windows 계정」으로 정한 것은 사용자 결정 `msg_dac79ea2b268`이다(technical-spec L47, MSSQL.md L67). goal L474는 새 형태를 「도구 전환 PR 설계 절에서 정한다」고 했는데 이는 사용자 결정 변경이라 리드·메인이 아니라 사용자가 답해야 한다. 이 goal의 완료조건(2단계·U-01)에 recovery 주체가 필요한지도 함께 정하면 PR 범위가 줄어든다. | INV-13, G-4, P-A |
| F-8 | 연결 실패는 지금 한 문구 「shared-memory connection failed」(L925)로만 멈추고 SQL 번호를 버린다. TCP 컨테이너에서는 「도달 불가(컨테이너 꺼짐·Docker Desktop 꺼짐·포트)」·「로그인 실패(18456, 비밀 불일치)」·「신원 불일치(다른 컨테이너)」·「엔진 변경」이 서로 다른 조치를 요구한다. 제공자 문구 억제는 유지하되 번호를 `New-DatabaseSqlFailure`로 보존해야 한다. | INV-14, INV-16, §3 |
| F-9 | `docker run -e MSSQL_SA_PASSWORD`는 값이 컨테이너 설정(`docker inspect`의 `Config.Env`)에 남아 컨테이너가 사는 동안 읽힌다. 시험 계약은 `--format`으로 Env를 빼 읽는 규칙만 둔다(L15·L23). 1·2단계·정리 카드에도 같은 규칙이 필요하고, 첫 초기화 뒤 `ALTER LOGIN sa WITH PASSWORD`로 회전해 Env 값을 무효화하는 안이 더 단단하다. `docker exec`·`cp`·`commit`·`export`는 금지 목록에 명시해야 한다. | INV-11, S-1 |
| F-10 | goal은 「연결·환경 검사·실행자 확인·계정·정리를 컨테이너 기준으로」라고만 적어 PowerShell 도구가 `docker`를 직접 부르는지(예: 정리 도구가 `docker volume rm`) 카드 단계로 두는지 정하지 않았다. 도구가 docker를 부르면 오프라인 harness 차단 목록(L380-396)과 강 등급의 실제 진입 실행이 모두 docker 대역을 요구한다. Windows에서 서비스 시작·정지가 카드 단계였듯(goal L727-730) docker 수명도 카드 단계로 두고 도구는 SQL·파일 경계만 갖는 쪽이 범위를 지킨다. | INV-04, INV-05, G-9 |
| F-11 | 도구는 한 번 수명이라 실패한 단계를 다시 시도할 수 없고(L577-585, Remove L266-267·L283) Windows 1단계는 3회차에 끝났다. 컨테이너는 볼륨을 버리면 재시도가 싸지만 「자동 정리·재시도 금지」와 「동명 금지」가 그대로면 실패 때마다 새 이름·새 승인이 필요하다. 승인 하나당 시도 횟수 상한과 실패 시도의 보존·정리 절차를 goal에 적어야 한다. | INV-03, INV-20, G-1 |
| F-12 | goal L476 「사용자 관리자 창은 없다」와 달리 `Set-TestPrincipals.ps1` L60·`Remove-TestEnvironment.ps1` L246은 관리자 토큰을 요구하고, 승인 계획의 비밀 폴더는 `C:\ProgramData\…`다. 관리자 요구 제거와 비밀 폴더를 사용자 프로필(`%LOCALAPPDATA%`) 아래로 옮기는 일이 「건드릴 곳」에 없다. | INV-12, G-5 |
| F-13 | 완료조건의 「오프라인 시험·CI」에서 CI는 DB 오프라인 스위트를 돌리지 않는다. `.github/workflows/` 네 파일 어디에도 `Invoke-OfflineTests`·`99_Tools/database` 호출이 없고 `code-rules.yml`만 pwsh로 PSScriptAnalyzer를 돌린다. 「CI」가 무엇을 뜻하는지 적지 않으면 관찰 불가능한 완료조건이다. | G-8 |
| F-14 | 컨테이너 정지·재시작 규칙이 없다. `docker stop`은 기본 10초 뒤 SIGKILL이라 SQL Server가 체크포인트를 못 끝낼 수 있고, 재시작 정책 기본값에 기대면 Docker Desktop을 다시 켤 때 SQL이 저절로 뜰 수 있다. 「서비스는 수동·정지」(goal L12)에 대응하는 `--restart no`·`docker stop -t ≥30`·`kill`/`rm -f` 금지를 플랜에 박아야 한다. | INV-02 |

설계 선택(§8, 각 대안 포함): S-1 관리 로그인은 sa 그대로+회전 vs 전용 관리 로그인+sa 비활성, S-2 연결 builder 하나(`Open-LocalDatabase`와 `Open-TestEnvironmentDatabase` 통합), S-3 docker 쪽 신원 기록 파일(`container-identity.json`)의 소유자, S-4 컨테이너 endpoint의 Connect Timeout 값, S-5 메모리 상한 값(`MSSQL_MEMORY_LIMIT_MB`·`--memory`).
정책 입력(§7, 사용자/메인): P-A recovery 주체 형태(사용자, F-7), P-B 정렬 규칙(사용자, F-3), P-C Restore 경로 보존 방식(사용자/메인, F-1), P-D 이미지 EULA 수락 기록(메인).

## 1. 보호 집합

| ID | 보호 상태 | 소유자(만들기·바꾸기·정리) | 판별 방법 | 수명·주의 |
|---|---|---|---|---|
| PS-1 | 이름 정한 컨테이너(시험 계약은 `dawnholder-sqlserver`, 1단계 이름은 승인 계획이 고정) | 실행 작업자가 카드 단계로 만들고 정지·삭제, 메인 전달 승인 아래 | `docker inspect --type container <이름>` 정확 일치, 추가로 `Id`·`Image` digest·`Created` 기록 대조 | 재생성 가능한 프로세스 host. `--restart no`, graceful stop만 |
| PS-2 | 이름 정한 볼륨(`/var/opt/mssql`: master·사용자 DB 파일·로그인 해시·정렬 규칙) | 같은 작업자, 승인 계획이 이름 고정 | `docker volume inspect <이름>` 정확 일치, `CreatedAt`·`Mountpoint` 기록 대조; SQL 쪽은 master `database_guid` | 한 번 수명(시험 DB와 함께). 실패 시 보존 단위 |
| PS-3 | 이미지(mcr.microsoft.com SQL Server 2025, 태그·digest 고정) | 작업자 pull 1회, 삭제 금지 | `docker image inspect` digest 대조 | 보존. `image rm`·`system prune` 금지 |
| PS-4 | 사용자의 다른 Docker 자원: 이미지 `nousresearch/hermes-agent:latest`(`2554d78964b4`)와 그 컨테이너, `docker-desktop` 배포판, Docker Desktop 설정·`.wslconfig` | 사용자 | 이름 정한 자원 밖은 모두 보호. 목록 명령에서 부분 일치로 섞이지 않게 정확 이름 판별 | 건드리지 않음 |
| PS-5 | 옛 Windows 자원: `MSSQL$SQLEXPRESS`(Manual·Stopped), 레지스트리 LoginMode 2·TCP 14330(09-29 Enable), 로그인 `Dawnholder_Dev_Wsl_688ea8c6f1bd`, DB `Dawnholder_Dev`·`GameDB`·`Dawnholder_Dev_D1b_20261002`(Baseline001), `C:\myVHDX.vhdx`(D:), `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\{state,credential}.clixml`, 옛 manifest `…\2026-10-02-persistence-repository\fixture-manifest.json`+`.lock`, `C:\ProgramData\Dawnholder-D1b-20261002*`(부재) | 사용자(처분은 별도 결정) | 새 승인 계획의 모든 경로·이름이 이 집합과 겹치지 않음을 플랜 검증에서 거부 | 질문 5 A 보존·동결. 새 도구는 읽지도 쓰지도 않음 |
| PS-6 | 컨테이너 시험 DB 하나와 그 identity(DatabaseId·create_date·owner_sid·database_guid·collation·RCSI) | 도구(New-TestDatabase Create), 정리는 Remove | 기존 `Assert-TestEnvironmentDatabaseIdentity` 그대로 | 한 번 수명. 동명 채택·DROP·재생성 금지(기존) |
| PS-7 | 비밀 셋: 관리(sa 또는 전용) · runtime · recovery의 DPAPI 파일과 hash, 그리고 컨테이너 Env 안의 초기 sa 값 | 도구(저장·hash), 작업자(Env 전달) | 기존 `Assert-TestEnvironmentSecretFile`(ACL 2항목·소유자·hash) | 파일은 정리 마지막에 삭제, Env 값은 회전으로 무효화(S-1) |
| PS-8 | 승인 계획 v2·수명 manifest v2·정산 파일·저널(`.lock`·`.pending`)·docker 신원 기록 | 리드(플랜), 도구(manifest), 작업자(docker 기록) | hash 고정(`ExpectedApprovalPlanHash`, `ExpectedManifestHash`) | 옛 Windows 경로와 분리된 새 폴더 |
| PS-9 | 호스트 포트 `127.0.0.1:<포트>` listener | Docker Desktop | `Get-NetTCPConnection -LocalPort`의 LocalAddress가 127.0.0.1뿐 | 7777 금지(World 조율 `msg_b1a0f5517876`), 외부 노출 금지 |
| PS-10 | 제품 SQL 계약: 9 RPC·29열·001 원문과 checksum·모듈 해시 방식 | Core(변경 금지) | 기존 `Test-ModuleStructure`·catalog | goal 보존 동작 1 |

## 2. 불변식 목록

범주 표기: FL=실패 수명, PS=보호 집합, EC=오류 분류, CC=비용 상한.

### INV-01 자원 수명은 넷이고 단위가 다르다 [FL·PS]
- 불변식: 이미지는 보존, 볼륨+시험 DB는 한 번 수명(만들기 → 설치 → 시험 → 폐기), 컨테이너는 같은 볼륨 위에 다시 만들 수 있는 프로세스 host다. 실패 때 보존 단위는 「정지된 컨테이너 + 볼륨」이다.
- 전제: `[소스]` goal L463(시험 DB 한 번 수명·동명 재생성 금지), L7-12(질문 4 A·5 A), 시험 계약 L13(이름 둘)·L32(7단계 재생성). `[추론]` 볼륨이 master·DB 파일을 함께 담으므로 DB 수명과 볼륨 수명을 분리하면 「정확 DB 하나」 판별이 볼륨 교체로 무너진다. `[미측정]` 같은 볼륨 위 재생성 뒤 master `database_guid`가 같은지(§6 M-2).
- 깨졌을 때: 컨테이너 재생성을 「동명 재생성」으로 읽으면 위험 확인 시험 7단계와 1단계 재시도가 규칙 위반이 되고, 반대로 볼륨 재생성을 허용하면 다른 볼륨의 동명 DB를 채택할 수 있다.
- 확인 방법: 승인 계획에 `VolumeName`·`ContainerName`·`ImageDigest`와 「볼륨은 한 번, 컨테이너는 재생성 가능」 문장을 넣고, 오프라인 시험이 플랜 검증을 대조한다. 1단계 카드는 컨테이너 재생성 뒤 master guid 일치를 원시로 남긴다.
- 대안 한 줄: 셋 모두 한 번 수명으로 두면 규칙은 단순하지만 Docker Desktop 재시작·정지 뒤 재시작마다 새 승인이 필요해 질문 4 A의 목적(사용자 손 줄이기)을 잃는다.

### INV-02 컨테이너는 저절로 시작하지 않고 강제로 죽이지 않는다 [FL]
- 불변식: `docker run --restart no`를 명시하고, 정지는 `docker stop -t <N≥30>`만 쓰며 `docker kill`·`rm -f`·`restart`를 쓰지 않는다. 정지 뒤 다음 단계는 DB가 ONLINE임을 확인한 뒤에만 간다.
- 전제: `[소스]` goal L12(옛 서비스 「수동·정지」 유지 결정의 취지), Environment.Common L1000-1002(`Database is not online; preserve it`), Remove L316(`LOCK_TIMEOUT 1000`)·L334(강제 없는 DROP). `[추론]` `docker stop` 기본 유예 10초 뒤 SIGKILL, 재시작 정책 기본값은 `no`지만 명시하지 않으면 플랜에서 보이지 않는다. `[미측정]` SQL Server가 깨끗이 내려가는 데 걸리는 시간(§6 M-5).
- 깨졌을 때: SIGKILL로 끊긴 DB는 재시작 때 복구 상태(RECOVERING)로 열려 도구가 「not online」으로 멈추고, 저널은 그 단계를 Failed로 남겨 한 번 수명이 소모된다.
- 확인 방법: 카드의 run·stop 줄에 두 옵션이 있는지 리드 메모와 실사자가 대조하고, 정지 원시에 컨테이너 `State.ExitCode`와 errorlog 마지막 줄을 남긴다.
- 대안 한 줄: 정지 대신 「컨테이너를 계속 켜 둔다」는 메모리 비용(INV-18)을 세션 내내 치른다.

### INV-03 실패한 시도는 자동으로 정리·재시도하지 않고, 시도 횟수에는 상한이 있다 [FL·CC]
- 불변식: 도구·카드 어느 단계가 실패해도 자원(컨테이너·볼륨·DB·비밀 파일·manifest)을 그대로 두고 메인에 보고한다. 실패 시도의 docker 자원 삭제는 저널이 Failed를 담은 뒤 명시 승인으로만 한다. 승인 하나당 시도는 3회를 넘기지 않고 넘으면 메인 체크포인트다.
- 전제: `[소스]` Set-TestPrincipals L498, New-TestDatabase L248-250, Remove L283·L462-464(모두 「자동 정리·재시도 금지」), Environment.Common L577-585(한 번 수명·재시도 거부), AGENTS L27(같은 산출물 3회 체크포인트), goal L682-736(Windows 3회차). `[추론]` 컨테이너에서는 볼륨 삭제가 곧 전체 초기화라 「정리」가 Windows보다 싸지만, 그래서 더 쉽게 반복될 수 있다.
- 깨졌을 때: 실패 원인(예: 비밀번호 정책 위반으로 SQL 초기화 실패)의 원시(`docker logs`)가 볼륨과 함께 사라져 같은 실패를 반복한다.
- 확인 방법: 승인 계획에 `Attempt` 번호와 상한, 실패 시도 보존 폴더를 두고, 카드의 「멈출 때」 절이 `docker logs <이름>` 보존을 먼저 요구한다.
- 대안 한 줄: 상한 없이 두면 Windows 1단계처럼 회차가 늘어도 멈추는 지점이 없다.

### INV-04 PowerShell 도구는 docker를 부르지 않는다 [FL·PS]
- 불변식: 여섯 도구 파일과 그 호출처는 SQL 연결·파일·DPAPI 경계만 가지며 `docker`·`wsl` 실행이 없다. 컨테이너·볼륨·이미지 수명은 카드 단계(실행 작업자)가 맡고 원시를 남긴다.
- 전제: `[소스]` goal L727-730(Windows에서 서비스 시작·정지가 카드 ⑥·⑬ 단계였음), TestSupport L351-368·Lifecycle L380-396(차단 목록이 OS 경계를 열거), goal L484(강 등급 실제 진입 실행). `[추론]` 도구가 docker를 부르면 오프라인 시험·강 등급 실제 진입 실행에 docker 대역이 필요하고 6파일 범위가 커진다.
- 깨졌을 때: 오프라인 시험이 실제 docker에 닿거나, 차단 목록이 docker를 빠뜨려 시험 중 실제 컨테이너를 만든다.
- 확인 방법: 독립 검증자가 변경된 제품 파일에서 `docker`·`wsl.exe`·`Start-Process` 호출 0건을 AST로 확인하고, 차단 목록에 새 OS 경계가 들어갔는지 대조한다.
- 대안 한 줄: 도구가 docker를 감싸면 카드가 짧아지지만 PS 5.1 → docker CLI → WSL VM의 세 경계 오류를 한 도구가 분류해야 해 EC 범주가 두 배가 된다.

### INV-05 수명 저널은 그대로이고 docker 단계는 저널 밖에서 별도 기록된다 [FL]
- 불변식: manifest 상태 기계(Planned→Created→Baseline001→Installed→Bound→PrincipalsReady→CleanupStarted→Removed), `.lock`(OpenOrCreate), `.pending` 보존 규칙은 바뀌지 않는다. 컨테이너·볼륨 생성과 정지·삭제는 Planned 전·Removed 뒤에 일어나며 비밀 없는 docker 신원 기록(S-3)에 Id·digest·CreatedAt을 남긴다.
- 전제: `[소스]` Environment.Common L340-352(상태 목록), L439-457(pending 보존·교체), L469-475(lock), Remove L283-302(정리 계획을 파괴 전에 기록). `[추론]` docker 자원은 manifest 생성 전에 있어야 하므로 저널 안에 넣으면 「manifest가 없으면 Plan」 규칙(New-TestDatabase L35-41)이 깨진다.
- 깨졌을 때: `docker run`이 볼륨만 만들고 실패하면 저널에 흔적이 없어 「미생성 자원은 정리 불필요」(goal L1229) 판정이 불가능하다.
- 확인 방법: 카드의 create 단계가 `docker volume inspect`·`docker inspect --format`(Env 제외) 출력을 기록 파일로 남기고, 정리 카드가 그 값과 현재 값을 대조한 뒤 삭제한다.
- 대안 한 줄: manifest schema v2에 `Container` 객체를 넣으면 한 파일에 모이지만 도구가 docker 값을 받아 적는 입력 경로가 새로 생긴다.

### INV-06 보호 집합 판별은 정확 이름 일치로만 한다 [PS]
- 불변식: 컨테이너·볼륨·이미지의 존재·부재·삭제 판정은 `docker inspect <정확한 이름>`(없으면 비0 exit) 또는 `^/이름$` 고정 정규식으로 하고, `--filter name=<부분>`의 결과를 부재 증명이나 삭제 대상 선정에 쓰지 않는다. 이미지는 digest로 판별한다.
- 전제: `[소스]` 시험 계약 L15(`docker ps -a --filter name=...`·`docker volume ls --filter name=...` 허용), L18(사용자 이미지 보호), L19(이미 있으면 멈춤). `[추론]` Docker의 name filter는 부분 문자열·정규식 일치다. `[미측정]` 이 PC의 Docker 버전에서 실제 일치 범위(§6 M-1).
- 깨졌을 때: 볼륨 이름이 컨테이너 이름을 접두로 가지면(`dawnholder-sqlserver` ⊂ `dawnholder-sqlserver-data`) 부재 검사가 거짓 양성으로 멈추거나, 반대로 다른 사용자 컨테이너가 삭제 대상 목록에 섞인다.
- 확인 방법: 카드의 모든 docker 목록 명령을 inspect 또는 고정 정규식으로 쓰고, 실사자가 명령 줄을 전수 대조한다.
- 대안 한 줄: 이름을 접두 관계가 없게 짓는 것(예: 볼륨 `dawnholder-sqldata`)만으로도 거짓 양성은 피하지만 다른 자원 보호는 여전히 정확 일치가 필요하다.

### INV-07 SQL 쪽 정확 대상은 hostname과 master guid로 판별한다 [PS·EC]
- 불변식: 승인 계획은 `ContainerHostname`을 고정하고 카드는 `docker run --hostname <그 값>`을 쓴다. 도구는 접속마다 `SERVERPROPERTY('MachineName') = ContainerHostname`, `SERVERPROPERTY('InstanceName') IS NULL`(기본 인스턴스), `sys.dm_os_host_info.host_platform = 'Linux'`, 관리 로그인의 sysadmin을 대조한다. 첫 접속(Create 전 preflight)에서 master의 `database_guid`를 manifest에 기록하고 이후 접속마다 같은지 대조한다.
- 전제: `[소스]` Environment.Common L927-941(현재 Machine·InstanceName·sysadmin 대조), L97-106(플랜의 `Instance`·`InstanceName` 형식 강제), New-TestDatabase L57-80(preflight), goal L682-687(MachineName 대소문자로 2회차 중단). `[추론]` 리눅스 SQL Server는 명명 인스턴스가 없고 MachineName은 OS hostname이며, master guid는 볼륨(master 파일)에 묶여 컨테이너 재생성에도 같다. `[미측정]` 위 세 값의 실제 출력(§6 M-2).
- 깨졌을 때: 같은 포트에 다른 컨테이너(또는 새 볼륨)가 떠 있어도 접속이 성공해 다른 인스턴스에 DB를 만들거나 지운다.
- 확인 방법: 플랜 검증 시험(EnvironmentGuards)에 hostname 형식·InstanceName 부재를 넣고, 가짜 SqlClient 응답에 host_platform·master guid를 추가해 불일치 거부를 시험한다.
- 대안 한 줄: 포트 고정만 믿는 안은 Docker Desktop이 포트를 다른 컨테이너에 줄 수 있어 「정확 대상」 원칙(goal L1180)을 만족하지 못한다.

### INV-08 옛 Windows 자원과 경로는 새 플랜·도구가 읽지도 쓰지도 않는다 [PS]
- 불변식: 새 승인 계획의 Database·로그인·ManifestPath·SettlementPath·비밀 폴더·신원 경로는 PS-5의 어느 값과도 같지 않다. 새 도구는 PlanVersion 2·SchemaVersion 2만 받고 v1 파일은 「버전 불일치」로 거부한다. Windows 시험 DB의 장래 정리는 도구 개정 `b2e7797b`(또는 도구 전환 PR의 base commit)로 한다고 goal에 적는다.
- 전제: `[소스]` approval-plan.execution.json(Database `Dawnholder_Dev_D1b_20261002`, ManifestPath `…\2026-10-02-persistence-repository\fixture-manifest.json`, PrivateDirectory `C:\ProgramData\Dawnholder-D1b-20261002`), goal L731(그 manifest가 Baseline001로 존재), Environment.Common L76-81(PlanVersion 1 고정), MSSQL.md L92(manifest schema 1·새 버전 선언 요구), goal L12(질문 5 A). `[추론]` 스키마를 바꾸면서 v1 호환을 유지하면 Windows 전용 분기가 제품에 남아 CODE_CONVENTION 「사용처 없는 추상화」에 걸린다.
- 깨졌을 때: 새 도구가 옛 manifest를 읽어 「Lifecycle manifest differs」로 멈추거나, 더 나쁘게는 옛 경로의 `.lock`을 열어 옛 수명과 섞인다.
- 확인 방법: 플랜 검증에 PS-5 경로·이름 거부 목록을 넣고(오프라인 시험), goal 「보존 동작」에 「Windows 정리는 commit X의 도구로」 한 줄을 추가한다.
- 대안 한 줄: 같은 ManifestPath를 재사용하면 폴더 하나로 보이지만 「Manifest already exists; this one-time lifetime cannot be restarted」(L577)에 바로 걸린다.

### INV-09 Configure-WslAccess의 Restore 경로는 삭제보다 먼저 보존된다 [PS·FL]
- 불변식: `Configure-WslAccess.ps1`을 지우기 전에 (가) 09-29 Enable 상태(`state.clixml` `Restored=false`, 변경 목록 LoginMode 1→2, TCP Enabled 0→1, loopback 14330)와 (나) Restore에 쓸 commit·명령·관리자 창 조건을 goal과 MSSQL.md 역사 절에 적는다. `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\`는 보호 집합이며 새 도구가 읽지 않는다.
- 전제: `[소스]` `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\state.clixml` 관측(InstanceId `MSSQL17.SQLEXPRESS`, Login `Dawnholder_Dev_Wsl_688ea8c6f1bd`, Database `Dawnholder_Dev`, Port 14330, Restored `false`), Configure-WslAccess L17-33(변경 목록)·L100-138(Restore), MSSQL.md L174-190(절차), goal L12(SQL Express 처분은 나중). `[추론]` Restore는 관리자 창과 서비스 재시작이 필요하므로 지금은 실행 대상이 아니지만, 처분을 정할 때 이 스크립트가 없으면 레지스트리를 손으로 되돌려야 한다.
- 깨졌을 때: SQL Express 처분 시점에 mixed 인증·TCP 14330이 켜진 채 남거나, 되돌리는 값이 사라져 사용자가 레지스트리를 직접 편집한다.
- 확인 방법: 독립 검증자가 MSSQL.md 역사 절의 commit 참조와 goal 기록을 확인한다. 실행은 없다.
- 대안 한 줄: 스크립트를 `99_Tools/database/legacy/`로 옮겨 두는 안은 처분까지 파일을 살리지만 CODE_CONVENTION 「사용처 없는 도구」와 구조 검사(미등록 literal 검사 범위)에 걸릴 수 있어 commit 참조가 가볍다.

### INV-10 포트는 127.0.0.1에만 묶이고 시작 뒤 listener를 검증한다 [PS]
- 불변식: `docker run -p 127.0.0.1:<포트>:1433`만 쓰고, 시작 뒤 `Get-NetTCPConnection -LocalPort <포트> -State Listen`의 LocalAddress가 `127.0.0.1`(필요하면 `::1`)뿐임을 원시로 남긴다. 포트 7777은 쓰지 않는다.
- 전제: `[소스]` goal L473(127.0.0.1·7777 금지), Configure-WslAccess L92-95(같은 검증 관례), 시험 계약 L13(포트 14333). `[추론]` Docker Desktop은 Windows 쪽 프록시 프로세스가 listener를 열므로 바인딩 주소 확인은 Windows에서 해야 한다. `[미측정]` 실제 listener의 LocalAddress·OwningProcess(§6 M-6).
- 깨졌을 때: `-p <포트>:1433`처럼 주소를 빼면 0.0.0.0에 열려 LAN에 노출된다.
- 확인 방법: 카드 run 줄의 `-p` 인자 대조와 시작 뒤 listener 원시.
- 대안 한 줄: 없음. 외부 노출은 goal이 금지한다.

### INV-11 비밀은 DPAPI 파일·SqlCredential·회전으로만 다루고 컨테이너 설정에서 읽히지 않는다 [PS]
- 불변식: 세 비밀(관리·runtime·recovery)은 각각 DPAPI CLIXML 파일(ACL: 실행자+SYSTEM, hash를 manifest에 기록)에만 있고, 연결 문자열에는 들어가지 않으며(`SqlCredential`+읽기 전용 SecureString, `Integrated Security=false`, `Persist Security Info=false`), 화면·로그·argv·goal에 나오지 않는다. 컨테이너 첫 초기화에 넘긴 sa 값은 초기화 뒤 `ALTER LOGIN sa WITH PASSWORD`로 회전해 `docker inspect`의 `Config.Env`에 남은 값을 무효화하거나, 회전하지 않으면 모든 카드가 `docker inspect --format`으로 Env를 빼고만 읽는다. `docker exec`·`docker cp`·`docker commit`·`docker export`·`docker logs`의 비밀 노출 경로는 금지 목록에 명시한다.
- 전제: `[소스]` goal L465(비밀은 채팅·goal·argv·로그 금지, DPAPI·stdin/pipe), Set-TestPrincipals L136-186(DPAPI 저장·hash), L239-267(비밀번호를 typed 매개변수로만), Environment.Common L1152-1179(ACL·hash 대조), technical-spec L36-38(`PersistSecurityInfo=false`, DPAPI 전달 경계), 시험 계약 L15·L23(Env 제외 inspect, `-e` 이름만 전달). `[추론]` `-e NAME`(값 없음)은 클라이언트 환경에서 값을 읽어 컨테이너 설정에 저장하므로 컨테이너가 사는 동안 inspect로 읽힌다. mssql 이미지는 `MSSQL_SA_PASSWORD`를 master 첫 초기화에만 쓰고 이후는 저장된 해시를 쓴다. `[미측정]` `MSSQL_SA_PASSWORD_FILE` 지원 여부, 회전 뒤 같은 볼륨 재생성 때 Env 값이 무시되는지(§6 M-4).
- 깨졌을 때: 컨테이너 설정을 읽는 어떤 명령이든 관리자 비밀을 평문으로 내보낸다. goal의 「stdin/pipe」 문장은 Env 전달을 포함하지 않아 규칙 위반 논쟁이 생긴다.
- 확인 방법: 독립 검증자가 카드·도구의 모든 `docker inspect` 호출에 `--format`이 있고 Env가 없는지 대조하고, 오프라인 시험이 연결 문자열 builder에 `Password`·`User ID` 키가 없음을 단정한다(Lifecycle L1243-1258의 기대 설정 표를 바꾼다).
- 대안 한 줄: 전용 관리 로그인을 만들고 sa를 비활성화하면(S-1) Env 값은 쓸모가 없어지지만 로그인 하나가 늘고 정리 단계가 하나 더 생긴다.

### INV-12 어떤 단계도 관리자 토큰을 요구하지 않고 비밀 폴더는 사용자 프로필 아래다 [PS·FL]
- 불변식: `Assert-TestEnvironmentExecutor -Administrator`는 호출처가 0이 되고(실행자 SID·기계 이름 대조는 유지), `New-LocalUser`·`Remove-LocalUser`·`Get-LocalGroupMember` 호출이 사라진다. 승인 계획의 `PrivateDirectory`·`IdentityDirectory`는 `%LOCALAPPDATA%\Dawnholder\…` 아래(비관리자가 소유자 설정·ACL 보호 가능)이며 `C:\ProgramData`를 쓰지 않는다. `ExecutorSid`는 DPAPI 복호와 docker 호출 주체를 못 박는 값으로 남긴다.
- 전제: `[소스]` goal L476(사용자 관리자 창 없음), Set-TestPrincipals L60(`-Administrator`)·L198-203(`New-LocalUser`)·L205-212, Remove L246·L392-396, Environment.Common L478-496, approval-plan.execution.json(`PrivateDirectory` `C:\ProgramData\Dawnholder-D1b-20261002`), Configure-WslAccess L14-16(`%LOCALAPPDATA%\Dawnholder\…` 관례). `[추론]` ProgramData 아래 폴더 생성·소유자 설정은 상승 토큰 없이도 될 수 있지만 검증 비용이 들고 관례가 둘로 갈린다. 「실행자 SID 대조 유지」는 비용 0에 기존 시험(EnvironmentGuards)을 살린다.
- 깨졌을 때: 1단계 카드가 다시 관리자 창을 요구해 질문 4 A의 효과가 사라지거나, 실행자 대조를 없애 다른 Windows 사용자 세션이 같은 플랜으로 실행된다.
- 확인 방법: Lifecycle harness의 `Executor` 호출 기록에서 `Administrator=True`가 0건(L1101·L1126 관례 재사용), 플랜 검증 시험에 ProgramData 경로 거부.
- 대안 한 줄: ExecutorSid까지 없애면 플랜이 짧아지지만 「누가 실행했나」가 DPAPI 성공 여부로만 남는다.

### INV-13 runtime·recovery의 role 매핑·권한 검증은 그대로이고 recovery 주체 형태는 사용자 결정이다 [PS]
- 불변식: runtime은 지금처럼 전용 SQL 로그인(무작위 SID 계획·대조, `dh_runtime`만)이다. recovery 주체의 새 형태는 사용자 결정 `msg_dac79ea2b268`을 바꾸는 일이므로 goal 보완 때 사용자에게 묻고(P-A), 어떤 형태든 `ValidatePrincipalMetadata`(서버 role 0, DB role 정확히 하나, 직접 grant 0, schema 소유 0)는 유지한다.
- 전제: `[소스]` technical-spec L47(사용자 결정, 전용 비-sysadmin Windows principal), MSSQL.md L67, goal L474(「새 형태를 도구 전환 PR 설계 절에서 정한다」), Set-TestPrincipals L222-318(SID 계획·대조)·L383-432(검증), goal L1216-1222(이번 완료조건에 R01–R05·recovery 실증 없음). `[추론]` 이 goal의 완료조건(Complete 설치·U-01·Test-Database)은 recovery 로그인 없이도 관측 가능하므로 「recovery는 이 goal에서 만들지 않음」이 가장 작은 변경이다.
- 깨졌을 때: 리드가 SQL 로그인으로 바꾸면 사용자 결정이 조용히 뒤집히고, 반대로 Windows principal을 고집하면 컨테이너에서 구현 불가다.
- 확인 방법: goal 「적용 중인 사용자 결정」에 새 답과 msg ID가 있는지, 플랜 v2의 recovery 필드가 그 답과 같은지.
- 대안 한 줄: 선택지 (가) 두 번째 SQL 로그인+DPAPI 파일(OS 계정 분리를 잃음) vs (나) 이 goal에서 recovery 미생성·후속 goal(PR이 작아짐, Set-TestPrincipals·Remove의 recovery 가지 제거).

### INV-14 연결 실패는 네 가지로 나뉘고 각각 다른 안전 사유로 멈춘다 [EC]
- 불변식: 접속 단계의 실패는 (1) endpoint 도달 불가, (2) 로그인 실패(18456 계열), (3) 접속은 됐으나 신원 불일치(hostname·master guid·sysadmin), (4) 엔진 변경(ProductVersion·collation)으로 구분돼 각각 고정 리터럴 사유와 SQL 번호(있을 때)를 남긴다. 제공자 문구는 계속 억제한다.
- 전제: `[소스]` Environment.Common L921-926(현재 Open 실패는 번호 없이 한 문구), L938-945(신원·엔진), SqlError.Common L2-24(번호만 보존), Test-Database L24-39(번호로 판정하는 관례), Environment.Common L730-733(번호를 담는 템플릿). `[추론]` TCP 컨테이너에서는 (1)이 「컨테이너 꺼짐·Docker Desktop 꺼짐·VM 일시정지」를 뜻하고 조치가 docker 쪽이며, (2)는 비밀 파일·회전 불일치라 조치가 DPAPI 쪽이다. `[미측정]` 각 경우의 실제 SqlException Number(§6 M-10).
- 깨졌을 때: 모두 같은 문구로 멈추면 실행 작업자가 원인을 추정해 비밀을 다시 만들거나 컨테이너를 다시 만든다. 둘 다 「자동 정리·재시도 금지」 위반 경로다.
- 확인 방법: 오프라인 시험이 `New-TestSqlException`(TestSupport L436-476)으로 18456과 도달 불가 번호를 주입해 사유 리터럴이 다른지 단정한다. 안전 사유 목록(L572-663)에 새 문구가 전수 등록됐는지 시험한다.
- 대안 한 줄: 번호 없이 문구만 셋으로 나누면 시험은 가능하지만 B2류 원시 대조에서 「어느 18456인지」를 잃는다.

### INV-15 docker 쪽 실패는 카드가 분류하고 도구는 「도달 불가」로만 멈춘다 [EC·FL]
- 불변식: 이미지 pull 실패, 컨테이너 생성·시작 실패, SQL 초기화 실패(비밀번호 정책·EULA·메모리)는 `docker` 종료 코드와 `docker logs <이름>`으로 카드가 분류·보존하며, 도구는 그 상태를 INV-14 (1)로만 본다. 카드는 SQL 접속 단계 전에 `docker inspect --format '{{.State.Status}}'`가 running이고 ready-wait가 성공했음을 원시로 남긴다.
- 전제: `[소스]` 시험 계약 L25-34(요구 측정 3·4·8), goal L484·L727-730. `[추론]` SQL 초기화 실패는 컨테이너가 종료 코드 1로 꺼지는 형태로만 보이고 그 이유는 로그에만 있다.
- 깨졌을 때: 도구의 「도달 불가」가 저널에 Failed로 남고, 실제 원인(예: 비밀번호 정책)은 어디에도 기록되지 않는다.
- 확인 방법: 카드의 「멈출 때」 절이 `docker logs`·`State.ExitCode` 보존을 첫 행동으로 둔다. 실사자가 카드 전수 대조.
- 대안 한 줄: 도구가 `docker logs`까지 읽으면 원인이 한 곳에 모이지만 INV-04를 깬다.

### INV-16 안전 사유 목록은 제품 throw 문구와 전수 일치한다 [EC]
- 불변식: `Get-TestEnvironmentStopReason`의 리터럴 목록과 제품 파일의 모든 `throw '…'` 문구는 1:1이다. 「shared-memory connection failed」 같은 옛 문구는 지워지고 새 문구(INV-14 네 가지, 컨테이너 hostname 불일치, master guid 변경 등)는 추가된다. 「Unclassified failure」로 떨어지는 제품 throw는 0이다.
- 전제: `[소스]` Environment.Common L572-663(목록)·L735(분류 불가 기본값), L925·L940·L944(바뀔 문구). `[추론]` 목록은 손으로 유지되므로 문구 교체 때 빠지기 쉽다.
- 깨졌을 때: 새 실패가 「Unclassified failure; provider/native text suppressed.」로 보고돼 메인이 원인을 못 본다.
- 확인 방법: 오프라인 시험이 제품 파일을 AST로 읽어 모든 상수 throw 문구가 목록에 있는지 전수 대조한다(지금 그런 시험이 있는지는 미확인, §9 미검토).
- 대안 한 줄: 없음.

### INV-17 시간 상한은 플랜 값이고 도구 기본값에 기대지 않는다 [CC]
- 불변식: ready-wait(컨테이너 시작 → 첫 접속 성공)는 회당 180초·시도 횟수 상한, SQL `Connect Timeout`은 측정(§6 M-13)으로 정한 값(현재 5초), `CommandTimeout` 30초, migration lock 5000ms·creation lock 5000ms·slot lock 2000ms·정리 `LOCK_TIMEOUT 1000`은 유지, `docker stop -t`는 ≥30초, `docker pull`은 20분, 1단계·2단계·정리 카드는 각각 실행 창(시작·끝 UTC)을 갖는다.
- 전제: `[소스]` Database.Common L26·L50·L182-187, Environment.Common L917-918(`Pooling=false`·5초), New-TestDatabase L94-101, Initialize-CharacterBinding L44-48, Remove L316, 시험 계약 L35(60분·180초·20분), goal L1186(시간 창은 자원 수명과 구분). `[추론]` `Pooling=false`라 모든 단계가 새 TCP+TLS 연결이므로 Connect Timeout이 각 단계의 실패 수명을 정한다. `[미측정]` 컨테이너 시작부터 접속 가능까지의 시간과 첫 연결 지연(§6 M-13).
- 깨졌을 때: 5초가 짧으면 정상 환경에서 거짓 「도달 불가」가 나와 한 번 수명 단계가 Failed로 소모된다(INV-03).
- 확인 방법: 플랜 v2에 timeout 필드를 두고 도구가 그 값을 쓰며, 오프라인 시험이 builder의 값을 단정한다.
- 대안 한 줄: 값을 코드 상수로 두면 플랜이 짧지만 「승인·환경 값은 manifest·설정으로」(CODE_CONVENTION 「파일 위치와 이름」)에 어긋난다.

### INV-18 메모리는 바닥과 천장을 모두 둔다 [CC]
- 불변식: 카드의 모든 단계 전에 Windows 여유 메모리를 재고 2GB 아래면 시작하지 않는다(시험 계약 관례). 컨테이너는 `MSSQL_MEMORY_LIMIT_MB`와 `docker run --memory`로 천장을 가지며 그 값은 플랜에 있다. `.wslconfig`·Docker Desktop 자원 설정은 바꾸지 않는다.
- 전제: `[소스]` goal L14(메인 요청: WSL·Docker 메모리 원시, 여유 0.7GB 사고), 시험 계약 L35(2GB 바닥), AGENTS 「공학 조건」(전역 설정 금지). `[추론]` SQL Server on Linux는 기본으로 보이는 메모리의 80%를 목표로 잡고, Docker Desktop VM은 호스트 RAM의 절반을 기본 상한으로 둔다. `[미측정]` 기동 뒤 안정 상태의 `sys.dm_os_process_memory`·`docker stats`(§6 M-14).
- 깨졌을 때: 리드·검증자·Sol 세션과 Unity가 함께 뜬 시간대에 다시 0.7GB 급 여유로 떨어져 다른 파트 세션이 죽는다.
- 확인 방법: 1·2단계·정리 카드마다 `Get-CimInstance Win32_OperatingSystem`의 FreePhysicalMemory 원시와 `docker stats --no-stream` 원시.
- 대안 한 줄: 천장 없이 바닥만 두면 측정은 되지만 SQL이 VM 메모리를 다 가져간 뒤에야 멈춘다.

### INV-19 디스크는 이미지·볼륨·VM 디스크 성장을 함께 센다 [CC]
- 불변식: pull 전·후, 볼륨 생성 후, 정리 후의 `docker system df`와 Docker Desktop VM 디스크 파일 크기, 그 드라이브의 여유 공간을 원시로 남기고, 여유 공간 바닥(플랜 값)을 둔다. 정리가 볼륨을 지워도 VM 디스크 파일이 줄지 않는 성질을 보고에 적는다.
- 전제: `[소스]` ADR-005 L25-26(SQL Server 이미지 약 1.5GB), 시험 계약 L26(크기·시간). `[추론]` WSL2 VHDX는 자동으로 줄지 않는다. `[미측정]` 실제 크기·경로(§6 M-7).
- 깨졌을 때: C: 여유가 줄어 Unity·빌드가 먼저 실패하고 원인이 DB 작업으로 보이지 않는다.
- 확인 방법: 위 원시 네 시점.
- 대안 한 줄: 없음.

### INV-20 재시도·재생성 횟수에는 상한이 있고 같은 이름 재생성은 정리 Done 뒤에만 한다 [CC·FL]
- 불변식: 이미지 pull은 승인당 1회(digest 고정), 컨테이너 재생성(같은 볼륨)은 단계당 1회, 1단계 전체 시도는 승인당 3회, 같은 이름의 볼륨·DB 재생성은 직전 수명의 정리 저널이 Done인 뒤에만 허용된다. 위험 확인 시험이 쓴 이름을 1단계가 다시 쓰려면 시험의 정리 원시(부재 확인)가 먼저 있어야 한다.
- 전제: `[소스]` goal L463, 시험 계약 L13·L32-33(같은 이름 재생성·정리 부재 확인), AGENTS L27. `[추론]` 이름을 매번 바꾸면 「이름 정한 하나」 경계가 승인마다 늘어나 보호 집합 판별이 복잡해진다.
- 깨졌을 때: 이름이 같은 옛 볼륨이 남은 채 새 수명이 시작돼 옛 DB를 채택한다.
- 확인 방법: 플랜 v2 `Attempt`·이전 정리 원시 경로 필드, 카드 시작의 부재 검사(INV-06).
- 대안 한 줄: 승인마다 새 이름(접미 번호)을 쓰면 규칙은 단순하지만 CODE_CONVENTION의 날짜·회차 접미 금지와 긴장한다.

### INV-21 정렬 규칙·엔진 기준값은 컨테이너 생성 전에 플랜에 고정되고 도구가 대조한다 [PS·FL]
- 불변식: 승인 계획 v2는 `ExpectedCollation`·`ExpectedProductVersion`(위험 확인 시험 측정값에서 사용자가 수용한 값)을 담고, 카드는 `MSSQL_COLLATION`을 그 값으로 넘기며, 도구는 Create 전 preflight에서 관측값이 플랜과 다르면 멈춘다(관측값을 기록만 하고 채택하지 않음).
- 전제: `[소스]` goal L502(미결: 수용 시점이 동작 검사 전), L731(Windows 기록 Korean_Wansung_CI_AS·17.0.1135.8·RCSI false), L466(관측값 자동 채택 금지), New-TestDatabase L76-80(지금은 Create 때 관측값을 그대로 기록), Environment.Common L942-945(그 뒤 변경만 감지), verify-schema.sql L238-320(hash 비교는 명시 `COLLATE Latin1_General_100_BIN2`라 서버 정렬 규칙과 무관). `[추론]` 서버 정렬 규칙은 첫 초기화에 정해져 볼륨에 남고, 바꾸려면 볼륨을 버려야 한다. `[미측정]` 리눅스 이미지의 기본값과 `Korean_Wansung_CI_AS` 지원 여부(§6 M-3).
- 깨졌을 때: 1단계 DB를 만든 뒤 정렬 규칙을 바꾸기로 하면 볼륨·DB를 버리고 승인을 다시 받는다. 또는 Windows 1단계 기준 상태와 비교 불가능한 기준이 조용히 생긴다.
- 확인 방법: 플랜 검증 시험에 두 필드 형식, 가짜 SqlClient 응답 불일치 때 거부 시험.
- 대안 한 줄: 기본값을 그대로 받아들이고 기록만 하는 안은 결정을 없애지만 「관측값 자동 채택 금지」와 충돌한다.

### INV-22 이미지는 digest로 고정되고 엔진 변경은 접속마다 감지된다 [PS·EC]
- 불변식: 플랜의 `ImageDigest`와 카드의 `docker image inspect` digest가 같을 때만 `docker run`한다. 도구는 기존대로 접속마다 ProductVersion·collation을 manifest와 대조해 「Engine changed」로 멈춘다.
- 전제: `[소스]` goal L471(태그·digest는 위험 확인 시험에서 재고 승인 계획에 고정), 시험 계약 L14(메인 판단 `msg_1f6c1e1ec912`: 태그 고정, digest 기록), Environment.Common L942-945. `[추론]` 태그는 움직일 수 있고 digest는 불변이다.
- 깨졌을 때: 재생성 때 다른 빌드가 떠 「Engine changed」로 멈추거나, 더 나쁘게는 patch가 달라도 ProductVersion이 같아 지나간다.
- 확인 방법: 카드 run 전 digest 대조 원시.
- 대안 한 줄: 없음.

## 3. 오류 분류와 멈춤 지점

| 분류 | 예 | 누가 감지 | 멈춤 지점·보존 | 기록 |
|---|---|---|---|---|
| 플랜·입력 거부 | 플랜 hash 불일치, PlanVersion 1, PS-5 경로·이름, ProgramData 경로, hostname 형식 | 도구(플랜 검증) | 연결 전. 자원 변화 0 | 고정 리터럴 |
| docker 자원 상태 | 이름 정한 컨테이너·볼륨이 이미 있음, 포트 점유, digest 불일치, Docker Desktop 꺼짐 | 카드(실행 작업자) | 생성 전. 채택·삭제 금지, ask | `docker` 종료 코드·출력 원시 |
| 컨테이너 기동 실패 | pull 시간 초과, 초기화 실패(비밀번호 정책·EULA·메모리), ready-wait 초과 | 카드 | 보존(정지 컨테이너+볼륨), `docker logs` 먼저 보존 | 종료 코드·logs·시간 |
| 접속 (1) 도달 불가 | 컨테이너 정지, VM 일시정지, 포트 바뀜 | 도구 | 단계 Failed 저널, 자원 보존 | 사유 리터럴+SQL 번호 |
| 접속 (2) 로그인 실패 | 18456, DPAPI 복호 실패, 회전 뒤 옛 값 | 도구 | 같음. 비밀 재생성 금지 | 사유 리터럴+번호 |
| 접속 (3) 신원 불일치 | hostname·master guid·sysadmin·host_platform 불일치 | 도구 | 같음. 다른 컨테이너 채택 금지 | 사유 리터럴 |
| 접속 (4) 엔진 변경 | ProductVersion·collation ≠ 플랜/manifest | 도구 | 같음. 새 기준값 결정 요청 | 사유 리터럴 |
| 수명·저널 거부 | manifest 존재, 미완 단계, pending, lock | 도구 | 기존 그대로 | 기존 리터럴 |
| SQL 명령 실패 | 제약·권한·lock timeout | 도구 | 기존 `New-DatabaseSqlFailure` | 번호만 |
| 정리 거부 | 정산 미완, 남은 세션·lock, identity 변경 | 도구(Remove) | 파괴 전. 강제 없음 | 기존 리터럴 |
| 정리 부분 실패 | DROP 성공 뒤 로그인 DROP 실패, 볼륨 rm 실패 | 도구 → 카드 | 저널 Failed, 볼륨·컨테이너 보존, 메인 보고 | 저널+docker 원시 |

규칙: 자동 재시도·강제 삭제·대체 대상 채택은 모든 행에서 금지다. 도구는 docker 쪽 원인을 추정하지 않고 (1)로만 멈춘다. 번호가 있는 실패는 번호를 잃지 않는다.

## 4. 비용 상한(플랜 v2에 둘 값)

| 대상 | 상한·값 | 근거 |
|---|---|---|
| ready-wait | 회당 180초, 회수 상한(측정 뒤 정함) | 시험 계약 L35, INV-17 |
| Connect Timeout | 5초 유지 또는 측정값(§6 M-13) | Environment.Common L918 |
| CommandTimeout·lock | 30초 / 5000·5000·2000·1000ms 유지 | Database.Common L50·L185 등 |
| docker stop 유예 | ≥30초, kill 금지 | INV-02 |
| pull | 20분, 1회, digest 고정 | 시험 계약 L35, INV-22 |
| 메모리 바닥·천장 | 여유 2GB 바닥 / `MSSQL_MEMORY_LIMIT_MB`·`--memory` 값(측정 뒤) | INV-18 |
| 디스크 | 여유 바닥(측정 뒤), 네 시점 원시 | INV-19 |
| 시도 횟수 | 승인당 3회, 컨테이너 재생성 단계당 1회 | INV-03·INV-20 |
| 실행 창 | 카드마다 시작·끝 UTC, 창 밖 실행 금지 | goal L1186 |
| 전체 일정 | 위험 확인 0.5일·PR 3~4일·1·2단계 2~3일·판정 PR 1일(승인 본문 추정, 측정값 아님) | 승인 본문 「일정 추정」 |

## 5. goal 범위 절의 빈틈·모순·모호함

| # | 위치 | 문제 | 대안 한 줄과 영향 |
|---|---|---|---|
| G-1 | 「보존 동작」 L463 vs 「적용 중인 사용자 결정」 질문 4 A(L11) vs 시험 계약 L32 | 「동명 재생성 금지」가 어느 자원에 적용되는지 없다. 컨테이너 재생성과 1단계 재시도, 위험 확인 시험의 이름 재사용이 모두 위반으로 읽힌다(F-2, F-11) | INV-01·INV-20 문장을 보존 동작에 추가. 영향: 플랜 v2 필드 셋(Attempt·이전 정리 원시·재생성 규칙) |
| G-2 | 「컨테이너 접속 경계」 L471-472 | 「이름은 위험 확인 시험 계약에서 정한다」인데 시험 계약은 이미 `dawnholder-sqlserver`·`dawnholder-sqlserver-data`·포트 14333을 정했고 시험 끝에 지운다. 1단계가 같은 이름을 쓰는지, hostname을 따로 정하는지 없다(F-4) | 1단계 이름·hostname·포트를 goal에 적고 시험과의 관계(같은 이름 재사용은 시험 정리 원시 뒤)를 한 줄로. 영향: INV-06·INV-07 판별 설계 |
| G-3 | 「순서와 미결」 L502 | 정렬 규칙 수용 시점이 「동작 검사 전」인데 실제 고정 시점은 1단계 컨테이너 생성 전이다(F-3) | 미결 문장을 「위험 확인 시험 뒤, 1단계 승인 계획 전」으로 옮기고 플랜 v2에 `ExpectedCollation`·`ExpectedProductVersion`. 영향: 사용자 질문 하나가 앞당겨짐(P-B) |
| G-4 | 「컨테이너 접속 경계」 L474 | recovery 주체를 리드가 정한다고 적었으나 사용자 결정 `msg_dac79ea2b268` 변경이다(F-7) | 사용자 질문으로 올리고 「이 goal에서 recovery 미생성」 선택지를 포함. 영향: Set-TestPrincipals·Remove의 recovery 가지가 PR 범위에서 빠질 수 있음 |
| G-5 | 「건드릴 곳」 L452, 「실행」 L476 | 관리자 요구(Set-TestPrincipals L60, Remove L246)와 ProgramData 비밀 폴더가 바뀌는 항목으로 없다(F-12) | 두 줄을 건드릴 곳에 추가, 플랜 v2 경로 규칙(사용자 프로필 아래). 영향: 플랜 검증 시험 추가 |
| G-6 | 「건드릴 곳」 L452 | `Database.Common.ps1` 21~32행만 적었으나 L11-18(인스턴스·DB 이름 정규식)과 L31-34(MachineName 대조)도 바뀌고, 2단계 도구 `Test-Database.ps1`(L13-16 `Instance` 필수, L51-54 연결 전 거부 문구, L131 두 번째 연결)과 그 계약 시험(TestDatabaseContract L208-222·L910-930)이 목록에 없다 | 줄 범위를 L6-40으로 넓히고 Test-Database.ps1을 목록에 넣는다. 영향: 2672줄 집계가 바뀌고 시험 「이상」 범위가 분명해짐 |
| G-7 | 「건드릴 곳」 L452 「Configure-WslAccess.ps1·Test-WslAccess.ps1 삭제」 | Restore 경로와 살아 있는 09-29 적용 상태를 다루지 않는다(F-1). MSSQL.md L171·L184·L187·L196·L209의 명령 예시와 L69·technical-spec L45의 언급도 함께 바뀌어야 한다 | 삭제 전 commit 참조·절차를 MSSQL.md 역사 절과 goal에 기록(P-C). 영향: 문서 변경 범위가 늘고 사용자 확인 하나 |
| G-8 | 「관찰 가능한 완료조건」 L455 「오프라인 시험·CI」 | CI는 DB 오프라인 스위트를 돌리지 않는다(F-13) | 「CI = code-rules 워크플로의 PSScriptAnalyzer 통과, 오프라인 스위트 8개는 검증자가 로컬 실행해 summary.json 원시」로 고쳐 적는다. 영향: 완료조건이 관측 가능해짐 |
| G-9 | 「만들 것」 2·「보존 동작」 L468 | 「정리를 컨테이너 기준으로」와 「대상은 컨테이너 시험 DB(필요하면 볼륨)」가 docker 수명이 도구 안인지 카드인지, 볼륨 삭제가 범위인지 정하지 않는다(F-10) | INV-04·INV-05 채택 여부를 한 줄로 적고 정리 순서(DB DROP → 로그인 → 비밀 파일 → 컨테이너 stop·rm → 볼륨 rm → 부재 확인)를 보존 동작에 둔다. 영향: Remove 변경 범위와 검증 방법이 정해짐 |
| G-10 | 「건드릴 곳」 L452 「실행 승인 계획 파일 형식」 | PlanVersion·SchemaVersion 올림과 Windows manifest 호환, 옛 DB 정리 도구 개정이 없다(F-6) | 보존 동작에 「Windows 정리는 commit `b2e7797b`(또는 PR base) 도구로, 새 플랜·manifest는 v2·새 경로」. 영향: INV-08 |
| G-11 | 「순서와 미결」 L497·L502 | 「Docker Desktop을 처음 켜고 메인이 알리면」은 `msg_47dbc806c3c1`(08:09:23Z)로 끝났고, 「이미지 내려받기 해석은 메인 확인 전」은 `msg_1f6c1e1ec912`로 확인됐다(시험 계약 L3·L14). goal은 commit 시점이라 뒤처져 있다 | 보완 commit에서 두 줄을 갱신. 영향: 없음(기록 정합) |
| G-12 | 「보존 동작」 L466 vs New-TestDatabase L76-80 | 「관측값 자동 채택 금지」인데 Create는 엔진 관측값을 기록하고 이후 접속이 그것과 비교한다. Windows에서는 관측 계획 문서와 B2 카드가 기대값을 대조했다(goal L732) | 플랜 v2 기대값 필드로 도구가 대조(INV-21). 영향: Windows식 관측 계획 문서를 플랜 필드로 대체 |
| G-13 | 「보존 동작」 L465 | 「비밀은 DPAPI와 자식 stdin/pipe로」인데 컨테이너 초기 비밀은 `docker run -e` 환경 변수로 넘어가고 컨테이너 설정에 남는다(F-9) | 전달 경로에 Env 전달·inspect 제한·회전을 추가(INV-11). 영향: 카드 규칙 한 줄, 선택 S-1 |
| G-14 | 「검증 등급과 구현자 배정」 L480 | 강 등급의 「실제 진입 실행」이 컨테이너에서는 실제 SQL 컨테이너를 요구할 수 있는데, 검증자는 질문 4 A의 「작업자」가 아니다 | 「검증자는 SqlConnection 대역으로 실제 진입(PR215 방식, goal L742-743), 첫 실제 실행은 1단계 카드」로 적는다. 영향: 검증 계약 문장 |
| G-15 | 「적용 중인 사용자 결정」 L14 | 메모리 원시 요청이 위험 확인 시험에만 붙어 있고 1·2단계·정리 카드에는 바닥·천장이 없다 | 비용 상한 표(§4)를 보존 동작 또는 플랜 v2에 둔다. 영향: INV-18 |

## 6. 위험 확인 시험에서 더 재야 할 값(시험 계약 v1 요구항목 1~9에 없는 것)

시험 계약(SHA256 `5dc11da4…47e9`)이 이미 재는 것은 뺐다. 읽기만으로 되는 항목을 앞에 두었고, 상태를 바꾸는 항목(M-9)은 리드가 계약 v1.1로 허용 범위를 넓혀야 잴 수 있다.

| # | 재야 할 값 | 왜 필요한가 | 연결 |
|---|---|---|---|
| M-1 | `docker ps -a --filter name=<부분 문자열>`·`docker volume ls --filter name=<부분>`이 부분 일치인지(사용자 hermes 컨테이너 이름 일부로 시험, 읽기만) | 보호 집합 판별 방식 확정 | INV-06, F-5 |
| M-2 | `--hostname <값>`으로 띄운 뒤 `SERVERPROPERTY('MachineName')`·`('ServerName')`·`@@SERVERNAME`·`('InstanceName')`(NULL 여부)·`sys.dm_os_host_info` 전 열, master `database_guid`(`sys.database_recovery_status`)를 7단계 재생성 전·후로 | 정확 대상 판별 설계. 시험 계약 5는 hostname을 정하지 않고 재생성 뒤 guid를 보지 않는다 | INV-07, F-4 |
| M-3 | 기본 `SERVERPROPERTY('Collation')`과 `SELECT COUNT(*) FROM sys.fn_helpcollations() WHERE name = N'Korean_Wansung_CI_AS'` | 정렬 규칙 결정 입력(P-B) | INV-21, F-3 |
| M-4 | `docker inspect --format '{{json .Config.Env}}'`에 `MSSQL_SA_PASSWORD` **키**가 있는지(값은 출력하지 않음, 키 이름만 남김), 7단계 재생성 때 같은 Env로 접속되는지 | Env 노출 범위와 회전 설계 | INV-11, F-9 |
| M-5 | `docker stop`(유예 30초)의 소요 시간, 컨테이너 `State.ExitCode`, errorlog 마지막 줄(정상 종료 문구), 재시작 뒤 접속 가능까지 시간 | graceful stop 상한 | INV-02, F-14 |
| M-6 | 시작 뒤 `Get-NetTCPConnection -LocalPort 14333 -State Listen`의 LocalAddress·OwningProcess(프로세스 이름), `docker port <이름>` | 127.0.0.1 바인딩 실증 | INV-10 |
| M-7 | Docker Desktop VM 디스크 파일 경로·크기(pull 전·후, 볼륨 생성 후, 정리 후), 해당 드라이브 여유 공간, `docker system df` 네 시점 | 디스크 상한 | INV-19 |
| M-8 | `SELECT SYSUTCDATETIME()`과 Windows `Get-Date -AsUTC`의 차이 | WSL 시계 표류 확인(기록 시각 정합) | §9 관찰 |
| M-9 | (상태 변경, 계약 보충 필요) master에서 `CREATE LOGIN … WITH PASSWORD=…, CHECK_POLICY=ON, CHECK_EXPIRATION=OFF, DEFAULT_DATABASE=[master]` 생성·`sp_getapplock @DbPrincipal='public'`·DROP이 리눅스에서 그대로 되는지 | Set-TestPrincipals L255-256·New-TestDatabase L94-99 구문 호환 | INV-13 |
| M-10 | 접속 성공 때 `sys.dm_exec_connections`(`@@SPID`)의 `encrypt_option`·`auth_scheme`·`net_transport`·`protocol_version`, 실패 유형별 `SqlException.Number`(컨테이너 정지 상태 1회, 틀린 비밀번호 1회) | TLS·SQL 인증 실증, 오류 분류 번호 | INV-14 |
| M-11 | `docker context show`, Docker Desktop 버전, Resource Saver(유휴 VM 일시정지) 설정값과 유휴 뒤 첫 접속 지연 | 도달 불가 분류와 Connect Timeout | INV-14·INV-17 |
| M-12 | 컨테이너 안 SQL Server 실행 사용자(`docker inspect --format '{{.Config.User}}'`, `sys.dm_os_host_info`) | root 실행 여부(「sudo 금지」의 컨테이너 대응) | PS-1 |
| M-13 | ready-wait 동안 5초 Connect Timeout으로 시도한 횟수와 첫 성공까지 시간, 안정 상태에서 접속 1회 지연 | Connect Timeout 값 결정 | INV-17, S-4 |
| M-14 | `MSSQL_MEMORY_LIMIT_MB` 미설정 상태의 `sys.dm_os_sys_info.committed_target_kb`와 Docker VM 총 메모리 | 천장 값 결정 | INV-18, S-5 |
| M-15 | `ACCEPT_EULA=Y`를 누가 넣었는지(카드·원시에 그대로 남김) | P-D 기록 | §7 |

## 7. 열린 질문

| # | 질문 | 답할 사람 | 답이 바꾸는 것 |
|---|---|---|---|
| Q-1 (P-A) | recovery 주체를 (가) 두 번째 SQL 로그인+DPAPI 파일로 할지, (나) 이 goal에서는 만들지 않고 후속 goal로 넘길지. 사용자 결정 `msg_dac79ea2b268`(비관리자 Windows 계정)의 변경이다 | 사용자(메인 전달) | PR 범위(Set-TestPrincipals·Remove의 recovery 가지), 플랜 v2 필드, technical-spec·MSSQL.md 문구 |
| Q-2 (P-B) | 컨테이너 정렬 규칙을 Windows 기록 `Korean_Wansung_CI_AS`에 맞출지 리눅스 기본값을 새 기준으로 받을지. 1단계 컨테이너 생성 전에 답해야 한다 | 사용자(위험 확인 시험 M-3 뒤) | 플랜 v2 `ExpectedCollation`, 카드의 `MSSQL_COLLATION` |
| Q-3 (P-C) | `Configure-WslAccess.ps1` 삭제 뒤 09-29 Enable을 되돌릴 경로를 (가) commit 참조+절차 기록으로 둘지 (나) 파일을 남길지 | 사용자 또는 메인 | MSSQL.md 역사 절, PR의 삭제 범위 |
| Q-4 | 위험 확인 시험이 쓰고 지운 이름(`dawnholder-sqlserver`·`-data`·14333)을 1단계가 그대로 다시 쓰는 것이 「동명 재생성 금지」에 안 걸리는지(INV-20 해석) | 메인 | goal 보존 동작 문장, 플랜 v2 이름 |
| Q-5 | docker 수명을 카드 단계로 두고 도구는 SQL·파일 경계만 갖는 설계(INV-04·INV-05)를 채택할지 | 리드 제안 → 메인 승인 | Remove 변경 범위, 차단 목록, 강 등급 실제 진입 방식(G-14) |
| Q-6 | 승인당 시도 상한(제안 3회)과 실패 시도의 docker 자원 삭제 승인 형식 | 메인 | 플랜 v2 `Attempt`, 카드 「멈출 때」 |
| Q-7 | Windows 시험 DB 정리에 쓸 도구 개정 commit을 goal에 어느 값으로 적을지(`b2e7797b` 또는 PR base) | 리드 | INV-08 기록 |
| Q-8 (P-D) | 이미지 EULA 수락(`ACCEPT_EULA=Y`)을 사용자 승인 기록의 어느 msg에 연결할지 | 메인 | 카드 원시의 근거 줄 |
| Q-9 | 완료조건 「CI」의 정의(G-8) | 리드 | 완료조건 문장 |
| Q-10 | `Test-Database.ps1`(2단계 도구)의 `Instance` 매개변수·연결 전 거부를 이 PR에서 바꿀지, 엔진 판정 PR로 미룰지 | 리드 | PR 경계, TestDatabaseContract 시험 범위 |
| Q-11 | 관리 로그인을 sa 그대로(회전 포함) 쓸지 전용 관리 로그인+sa 비활성으로 갈지(S-1). 질문 3 원문은 둘 다 열어 두었다 | 리드 제안 → 메인 | 플랜 v2 `AdminLogin`, 정리 단계 수 |

## 8. 설계 관찰(비차단)

- S-1 관리 로그인: sa 그대로+초기화 뒤 회전은 로그인 수가 적고 정리가 단순하다. 전용 관리 로그인+sa 비활성은 Env 노출을 구조적으로 없애지만 단계·정리 항목이 하나씩 는다. 어느 쪽이든 DPAPI 파일 하나·hash 하나다.
- S-2 연결 builder 하나: `Open-LocalDatabase`(Database.Common L6-40)와 `Open-TestEnvironmentDatabase`(Environment.Common L895-951)는 같은 이유(endpoint·인증·신원 대조)로 함께 바뀐다. CODE_CONVENTION 「같은 이유로 반복되는 검사는 한 곳」에 따라 하나로 모으고, `Open-LocalDatabase`라는 이름은 「Local」이 더는 사실이 아니므로 바꾸는 편이 처음 읽는 사람에게 맞다. ModuleHashConsumers 시험 L904·L963의 금지 이름 목록도 함께 본다.
- S-3 docker 신원 기록 파일: 카드가 쓰는 `container-identity.json`(Id·digest·볼륨 CreatedAt·Mountpoint·hostname·시작 UTC)은 비밀이 없고 정리 카드가 대조한다. 소유자는 실행 작업자, 위치는 1단계 근거 폴더.
- S-4 Connect Timeout: M-13 측정 뒤 플랜 값으로. 5초가 충분하면 유지.
- S-5 메모리 천장: M-14 측정 뒤 `MSSQL_MEMORY_LIMIT_MB`와 `--memory`를 같은 값 근처로. Docker Desktop 전역 설정은 건드리지 않는다.
- 관찰 O-1 manifest v2에서 지울 필드: `WindowsAccountSid`, `RecoveryLocalName`, `IdentityDirectory`의 ReaderSid 의미(Windows 계정 독자), `Instance`·`InstanceName`. 넣을 필드: `ContainerName`·`ContainerHostname`·`VolumeName`·`ImageDigest`·`AdminLogin`·`AdminCredentialPath`·`ExpectedCollation`·`ExpectedProductVersion`·`MasterDatabaseGuid`(첫 접속 기록)·timeout 값·`Attempt`. 이름에 날짜·회차·작업자 이름을 넣지 않는다.
- 관찰 O-2 오프라인 harness: 차단 목록(Lifecycle L380-396)의 `New-LocalUser`·`Get-LocalGroupMember`·`Get-Acl`은 그대로 두되 호출처가 사라지면 「차단됐으나 호출 0」을 시험이 구분해 적어야 한다. 새 경계(`ConvertFrom-SecureString`·`Get-NetTCPConnection`·`docker` 등)가 도구에 들어오면 목록에 넣는다.
- 관찰 O-3 MSSQL.md: L3·L7·L15-24의 「현재」 절과 L149-210의 「역사」 절을 나눠 쓰고, 역사 절의 명령 예시는 실행 안내가 아니라 commit 참조로 남긴다(Q-3).
- 관찰 O-4 ADR-005 L18-20의 「비밀번호 0개」 근거는 컨테이너에서 무효가 된다. 새 설계 결정 기록은 「비밀이 세 개가 되었고 DPAPI·회전·inspect 제한으로 다룬다」를 적어야 ADR-005 상태 표시와 맞는다.
- 관찰 O-5 시계: 컨테이너(WSL VM)의 `SYSUTCDATETIME()`과 Windows 시각이 어긋날 수 있다(M-8). manifest의 `CreatedUtc`(Windows)와 DB `create_date`(컨테이너)를 서로 비교하는 검사는 두지 않는다.
- 관찰 O-6 BACKLOG `sql-environment-human-access`(L49)의 「사람 조회 공존」은 컨테이너에서 「카드 실행 창 동안 사람 접속 금지」로 더 단순해진다. 이 goal 범위는 아니며 기록만 한다.

## 9. 실제 준수 위치·미검토·해당 없음·미실행

- 준수 위치: 맥락 메모(이 파일 머리, 첫 쓰기), 불변식 §2(INV-01~INV-22, 범주·전제 표식·깨졌을 때·확인 방법·대안), 보호 집합 §1, 오류 분류 §3, 비용 상한 §4, goal 빈틈 §5(G-1~G-15), 추가 측정 §6(M-1~M-15), 열린 질문 §7(Q-1~Q-11, 담당자), 비차단 관찰 §8. 소스 근거는 commit `b2e7797b`의 file:line과 저장소 밖 파일의 읽은 시각 값이다.
- 미검토(읽지 않음): `Module.Common.ps1`·`ModuleHash.Common.ps1`·`Test-ModuleStructure.ps1` 본문, modules 19개 SQL 본문, migrations 002~004 본문(role 생성 두 줄만 grep), `TestEnvironmentLifecycle.Tests.ps1`의 나머지 약 2,200줄, `ModuleBundle`·`ModuleDeployment`·`ModuleHash*`·`ModuleStructureCli` 시험, `EnvironmentGuards.Tests.ps1` 본문(grep만), `DEVELOPMENT.md` 본문(grep 0건), `implementer-routing.md`, 10-05~10-09 실행 원시(`execution*/user-window.txt`)·카드·판정 원문, Windows manifest 파일 자체(goal 기록으로만 봄), `%LOCALAPPDATA%` 아래 `credential.clixml`. 제품 throw 문구와 안전 사유 목록의 전수 대조 시험이 이미 있는지는 확인하지 않았다(INV-16 확인 방법).
- 해당 없음: C#·Unity·TypeScript·Python 절(대상 아님), 패킷·PDL(변경 없음), 저장소/GameServer 연결(다음 goal).
- 미실행: Docker·WSL·SQL 명령, `99_Tools/database` 스크립트(오프라인 시험 포함), 빌드·CI 모두 실행하지 않았다. Docker 동작에 관한 모든 전제는 `[추론]`이고 §6으로 측정에 돌렸다. 모델 backend는 unknown이며 이 문서의 어떤 수치도 측정값이 아니다.
- 이 검토는 Opus 독립 구현 검증을 대신하지 않는다. 결함 번호 체계는 구현 뒤 검증자가 부여한다.

## 10. 자기 쓰기 감사

기준 시각은 `date -u` 2026-10-10T08:31:45Z(감사 명령 실행)다. 기동 기록 `path-check.txt`(08:05:27Z)보다 새 파일을 찾는 `find -newer`와 `git status --porcelain`으로 확인했다.

- 이 세션이 명령·도구로 직접 쓴 경로는 하나다: `01_Phases/goals/2026-10-04-persistence-integration/goal-review.md`(Write 1회, Edit 6회, 그리고 줄 끝 확인 과정의 `sed -i 's/\r$//'`·`tr -d '\r' | cp` 각 1회는 바이트 수가 같아 내용 변경이 없었다. `file`은 LF·UTF-8·BOM 없음으로 읽힌다). `git status --porcelain` 출력은 `?? 01_Phases/goals/2026-10-04-persistence-integration/goal-review.md` 한 줄이다. 제품·테스트·goal·문서·설정 파일 변경 0, commit·push 0.
- 허용 TEMP(`.backups/tmp/core-r7`) 아래: 줄 끝 변환 중간 파일 `goal-review.lf.tmp`를 만들었다가 지웠다(내가 만든 유일한 TEMP 파일). 도구가 자동으로 만든 항목은 `claude/bash-edit-diff/<id>/`(Edit 도구가 diff 표시용으로 만든 작은 git 객체 저장소)와 `node-compile-cache`(기동 때 생성)다. 모두 허용 안이다.
- **허용 밖으로 분류하는 도구 자동 저장 5건:** 큰 명령 출력을 Claude Code harness가 홈 아래 세션 폴더 `C:\Users\bass1\.claude\projects\C--Users-bass1-orca-workspaces-DawnHolder-Project-core-active\c6dff741-c8b4-4860-8d56-89d30f383279\tool-results\`에 저장했다. 파일은 `b2whthnx0.txt`(goal L5-128 사본), `bki14ps5d.txt`(첫 R-7 goal-review 사본), `b439wdvls.txt`(MSSQL.md 사본), `bgqnzx5ay.txt`(goal L584-746 사본), `b8lvccvwq.txt`(근거 폴더 목록)이다. 내가 쓰기 명령을 내린 것이 아니라 읽기 출력의 자동 보존이고 비밀은 없지만, 「기본 위치의 세션 임시 폴더·홈은 허용 밖」 조건에 따라 위반으로 나열한다. 다음 검토자는 큰 파일을 `sed -n`으로 잘라 읽어 이 저장을 피할 수 있다.
- 근거 폴더 `r7-container-tools-review`의 기동 뒤 새 파일 8개(`contract.sha256`·`first-screen.json`·`launch-record.txt`·`launch-time.txt`·`ready-wait.json`·`spec.txt`·`split-receipt.json`·`worker-start.json`)는 이름과 시각으로 보아 리드의 기동 기록이며 이 세션은 그 폴더에 쓰지 않았다.
- Git Bash `/tmp`에 기동 뒤 생긴 `*.tmp.ico` 파일들은 이 세션의 명령이 만든 것이 아니라고 보지만 귀속은 증명하지 못했다.
- 실행: Docker·WSL·SQL 명령, `99_Tools/database` 스크립트(오프라인 시험 포함), 빌드, 설치, 설정 변경, 추가 위임 모두 0회. 실행한 것은 읽기 전용 Git(`rev-parse`·`status`·`show`·`grep`·`ls-files`), 파일 읽기·`sha256sum`·`wc`·`grep`·`find`·`ls`·`date`, 그리고 `orca orchestration send/check`(heartbeat 5회, check 2회)뿐이다. `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\state.clixml`은 비밀 아닌 필드만 `grep -o`로 읽었고 `credential.clixml`은 열지 않았다.
- Bash heredoc 첫 쓰기 시도 1회는 셸 구문 오류로 끝나 파일을 만들지 않았다(`test ! -e`로 재확인한 뒤 Write 도구 사용).
