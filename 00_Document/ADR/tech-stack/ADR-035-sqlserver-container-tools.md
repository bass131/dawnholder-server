# ADR-035: 로컬 시험 DB를 Linux SQL Server 컨테이너로 전환

- 날짜: 2026-10-10
- 상태: 채택된 범위의 도구 구현. 실제 컨테이너 설치·엔진 판정과 독립 검증의 완료 상태는 [진행 goal](../../../01_Phases/goals/2026-10-04-persistence-integration/goal.md#현재-범위pr-경계)에서 확인한다.
- 원천: 컨테이너 범위 승인 msg_4fdac90aac0a, 사용권 결정 26 A(msg_3a98521517fa), 복구 주체·엔진 기준·Restore 결정 27~29 A(msg_33c4463ed26f), R-7 채택/승인 msg_0571a9679c71 및 정확 대상 보충 v1.1(msg_119f0a3962b8).

## 결정과 이유

Windows PowerShell 5.1 도구는 승인 계획 v2의 loopback TCP endpoint에서 Linux SQL Server를 관리한다. Windows SQL Express와 옛 시험 DB·접근 설정은 보존한다. SQL Server 선택과 SQL 계약은 유지하고 로컬 실행 환경과 인증 경계만 바꾼다. [ADR-029](../harness/ADR-029-wsl2-dotnet-execution-standard.md)의 트레이드오프 ④(LocalDB의 Linux 부재)는 이 선택으로 닫는다. 게임 서버/저장소의 연결은 후속 작업이다.

[ADR-005](ADR-005-mssql-efcore.md)의 Windows 통합 인증과 “비밀번호 0개” 근거를 이 시험환경에서는 대체한다. 관리 sa와 전용 runtime의 **비밀 두 개**를 Windows DPAPI PSCredential CLIXML로 보관한다. recovery 주체는 사용자 결정 27 A로 만들지 않으며 후속 복구 기능 goal에서 형태를 정한다. dh_runtime/dh_recovery role과 공개 RPC 권한, runtime의 무작위 SID·최소권한 대조는 그대로다.

비밀 파일 생성과 권한은 기존 실행자+SYSTEM ACL·owner·hash helper가 소유한다. 새 New-TestAdminCredential 진입점은 Planned manifest 이후, 첫 컨테이너 기동 전에 sa 파일을 생성하고 hash를 manifest 저널에 고정한다. 아직 생성되지 않은 파일 hash를 계획에 요구하는 순환을 피한다. 이미 있는 폴더/파일이나 부분 실패는 보존하며 교체·회전·재시도하지 않는다. SqlCredential와 읽기 전용 SecureString을 쓰고 연결 문자열에는 User ID/Password를 넣지 않는다.

컨테이너 첫 초기화의 sa 값만 프로세스 환경에서 docker run -e MSSQL_SA_PASSWORD(이름만)에 전달한다. 값이 컨테이너 Env에 남는 비용을 수용한다. sa 회전이나 전용 관리 로그인 추가는 비밀 교체 도중 잠김·다른 계정 수명·정리 실패를 추가하므로 채택하지 않았다(msg_5aed29ae6d6c). 대신 모든 inspect를 Env 없는 --format으로 제한하고 exec/cp/commit/export를 금지한다. logs는 장애 분류에만 쓰며 저장 출력의 비밀 부재를 확인한다. 컨테이너·볼륨의 정리 후 정확 이름 부재 원시를 남긴다. DPAPI도 같은 Windows 실행 사용자가 복호할 수 있으므로 관리자 비밀을 이 사용자에게서 숨기는 설계가 아니다.

컨테이너와 볼륨의 생성·시작·정지·삭제는 실행 카드가 맡는다. 도구는 SQL·파일 경계만 소유한다. Docker까지 도구 안에 넣는 안은 Docker/WSL 오류 분류·오프라인 대역·자원 실패 수명을 늘린다. 이미지는 보존, 볼륨+시험 DB는 한 번 수명, 컨테이너는 그 볼륨 위 프로세스 host다. 도구는 DB → runtime 로그인 → 비밀 파일을 정리한 뒤 Removed를 기록하고, 카드는 컨테이너 정지·삭제 → 볼륨 삭제 → 부재 확인을 수행한다. 실패의 자동 정리·재시도는 없다.

정확 대상은 고정 hostname의 MachineName, 기본 인스턴스 NULL, Linux, sa/sysadmin과 **master family_guid**로 판별한다. master database_guid는 같은 이미지의 서로 다른 두 볼륨에서 같았으며 이미지 템플릿 값으로 보인다(마지막 원인 설명은 추론, 관측 표본 2; 계약 보충 v1.1). 같은 볼륨에서 유지되고 서로 다른 볼륨에서 달랐던 family_guid를 선택했다. ServerName/@@SERVERNAME은 재생성 때 남거나 바뀌어 판별에 쓰지 않는다. 도구가 생성하는 시험 DB 자체는 기존 database_guid·create_date·owner_sid·ID·collation·RCSI identity를 유지한다. family_guid 관측 표본은 범용 고유성 증명이 아니며 카드의 컨테이너/볼륨 신원 원시와 함께 확인한다.

정렬 규칙과 ProductVersion은 첫 컨테이너 기동 **전** 승인 계획의 ExpectedCollation/ExpectedProductVersion에 고정한다. 생성 후 관측값을 채택하면 다른 정렬 규칙을 쓰기 위해 볼륨 수명을 버릴 수 있다. Create의 preflight가 기대값과 다르면 DB를 만들지 않으며, 이후에도 manifest의 기록값과 대조한다. 계획값을 코드 상수로 두지 않는다. digest 고정과 --pull=never는 카드가, SQL 엔진 대조는 단일 Open-TestEnvironmentDatabase helper가 맡는다.

## 대안과 비용

| 선택 | 채택하지 않은 안 | 이유·남은 비용 |
|---|---|---|
| SQL Server Linux 컨테이너 | Windows Express 계속 사용, 원격 DB, PostgreSQL 전환 | 기존 SQL/RPC/migration을 보존하며 반복 사용자 관리자 창 의존을 줄임. Docker Desktop/WSL VM·이미지·볼륨의 메모리/디스크 비용이 생김 |
| SQL 인증·DPAPI 두 파일 | Windows 통합 인증, recovery SQL 계정 즉시 추가 | Linux 경계에 맞추되 복구 주체 결정은 후속 goal. 비밀·ACL·삭제 증거를 관리해야 함 |
| sa 회전 없음 | 첫 기동 후 회전, 별도 관리자+sa 비활성화 | 추가 잠김·교체 실패 수명을 피함. Env 접근 제한과 정리 증빙이 계속 필요 |
| manifest에 관리 hash | 생성 전 계획에 hash 고정 | 순환을 피함. Plan→관리 비밀 생성→컨테이너 기동 순서를 카드가 지켜야 함 |
| 연결 helper 하나 | Open-LocalDatabase와 수명 도구의 별도 builder | 대상·인증·신원 판단의 중복 제거. Open-LocalDatabase를 제거하고 Test-Database 두 연결을 같은 helper로 이관 |
| PlanVersion/SchemaVersion 2만 | Windows v1 자동 이관/호환 가지 | 옛 자원과 새 수명을 섞지 않음. 옛 정리/Restore는 cc20d428 전체 도구를 별도 승인으로 사용 |

ConnectTimeout·ready/pull/stop·메모리/디스크 바닥·천장·Attempt는 계획 필드다. CommandTimeout 30초와 기존 SQL lock 상한은 보존한다. 카드별 실행 창, Windows 여유 2GiB 바닥, 디스크 네 시점 원시를 둔다. StopTimeoutSeconds의 30 이상 검증은 유지하며 **강제 종료까지의 대기**를 뜻한다.

재측정의 세 정지는 **시작 직후(첫 접속 성공 뒤 0.577~3.495초) 값이므로 정상 상태의 정지 값으로 쓰지 않는다**. 시작 완료 뒤의 첫 추가 측정도 `docker stop -t 30` 실행 30.75초 뒤 컨테이너 ExitCode 137이었으므로 정상 종료 유예로 해석하지 않는다. 측정 조건·원시·미측정 범위는 [운영 안내의 재측정 근거](../../operations/MSSQL.md#실행-카드의-docker-규칙과-정리)를 따른다. SIGTERM 처리 원인은 미측정이다.

`docker kill`·`rm -f`·`restart` 명령은 금지한다. 현재 정지 규칙은 컨테이너·볼륨을 바로 지우는 정리 경로와 실패 보존 경로에만 적용한다. 정지 뒤 같은 볼륨을 다시 쓰는 절차(SHUTDOWN 경로와 실행 주체 포함)는 1단계 카드 설계에서 정하며 그 전에는 볼륨 재사용을 위한 정지를 하지 않는다. SHUTDOWN 소요시간·종료 로그와 family_guid 표본의 관측 범위는 [운영 안내의 재측정 근거](../../operations/MSSQL.md#실행-카드의-docker-규칙과-정리)에 기록한다. Docker 전역 설정·.wslconfig를 바꾸지 않는다. 시간·메모리 비용 상한은 승인 계획으로 정하며, [운영 안내의 재측정 소요시간](../../operations/MSSQL.md#실행-카드의-docker-규칙과-정리) 자체가 비용 상한이나 정상 종료 보장은 아니다.

## 검증과 역사 경계

오프라인 시험은 가짜 연결·SQL 응답·신원/비밀 경계로 실제 진입과 거부 경로를 확인하고, AST로 모든 고정 사유와 허용 목록·금지 OS 호출을 대조한다. 실제 엔진의 잠금·SQL 문법·볼륨 보존·DPAPI/ACL·Docker 실행을 대신하지 않는다. 그 검증과 1·2단계 실행은 goal의 별도 계약이다.

Configure-WslAccess/Test-WslAccess 삭제에 앞서 [MSSQL 역사 절](../../operations/MSSQL.md#역사--windows-sql-express와-wsl-접근)에 2026-09-29 Enable 상태와 cc20d428 전체 도구의 Restore 경로를 남긴다. 옛 기록을 현재 실행 권한으로 확대하지 않는다.
