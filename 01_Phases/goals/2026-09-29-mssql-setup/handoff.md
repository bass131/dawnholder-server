# 다음 세션 시작점

운영 명령·스키마 결정·복구의 기준 문서는 [MSSQL.md](../../../00_Document/operations/MSSQL.md), 완료 상태와 근거는 [goal.md](goal.md)다. 게임 서버 영속 저장은 아직 구현하지 않았다.

## 현재 환경

- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema`.
- 브랜치: `bass131/feat-mssql-game-schema`, PR [#127](https://github.com/bass131/dawnholder-server/pull/127), base `main`.
- Windows `SQLEXPRESS` 17.0.1000.7 서비스 / `Dawnholder_Dev` / `dh` schema / migration 1.
- Windows 관리 연결: 통합 인증, `lpc:.\SQLEXPRESS`. native WSL: `tcp:127.0.0.1,14330`, 새 최소권한 SQL 로그인. 서버는 loopback IPv4/IPv6만 리슨한다. SQL Browser/방화벽 규칙/기존 데이터는 변경하지 않았다.
- Ubuntu 26.04 mirrored WSL, native mssql-tools18/msodbcsql18 18.7.1.1-1 설치 완료. Windows SQL 서비스 시작 유형은 기존 Manual이다.
- 사용자 승인으로 관리자 Enable, 혼합 인증, 새 SQL 로그인/DPAPI 및 서비스 재시작까지 수행했다. **PR merge는 별개로 해당 PR에 대한 사용자 명시 승인 후에만 가능하다.** 자동 병합 예약 금지.

## 첫 확인과 평소 실행

```powershell
# 이 인계문의 작업 공간에서 실행. 다른 문서 작업 checkout에 쓰지 않는다.
git status --short --branch
Get-Service 'MSSQL$SQLEXPRESS'
./99_Tools/database/Install-Database.ps1   # 이미 설치됨: checksum/catalog 확인 후 no-op
./99_Tools/database/Test-Database.ps1      # 개발 DB rollback/일시 DDL 검사, 다른 DB 검사와 동시에 실행하지 않음
./99_Tools/database/Test-WslAccess.ps1     # PowerShell 7 -> native Ubuntu ODBC 인증/권한/rollback
```

재부팅 후 SQL 서비스가 중지됐다면 관리자 PowerShell에서 `Start-Service 'MSSQL$SQLEXPRESS'`를 실행한다. 새 서비스나 WSL SQL 서버를 띄우지 않는다. 이미 만든 환경에 `Configure-WslAccess -Action Enable`을 다시 실행하지 않는다.

## 비밀과 주입

`%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\credential.clixml`은 같은 PC/Windows 사용자에게 묶인 DPAPI PSCredential이다. `state.clixml`은 복구용 원래 레지스트리 값과 새 login SID를 보관한다. 파일 내용/암호/DPAPI blob을 채팅·Git·로그에 출력하거나 다른 계정으로 복사하지 않는다. 도구가 필요한 순간 Import-Clixml로 읽어 stdin(LF)으로 native Linux child에 전달하며 child 수명 동안 SQLCMDPASSWORD로 사용한다. 관리 도구의 인스턴스/DB 인자 또는 DAWNHOLDER_SQL_INSTANCE/DAWNHOLDER_SQL_DATABASE에는 비밀이 없다.

GameServer는 아직 DB 설정을 읽지 않는다. 후속 .NET 통합에서 연결문자열을 주입할 때 비밀 저장소에서 프로세스 메모리/제한된 child 환경으로 전달하는 계약부터 구현한다. 로그인 이름을 소스에 고정하거나 SQL 암호를 앱 설정/Unity 에셋/명령행에 넣지 않는다. 현재 앱용 SQL 로그인은 세 게임 테이블 SELECT/INSERT/UPDATE 및 migration 이력 SELECT만 가능하다. migration은 Windows 관리 계정으로 실행한다.

## 변경·검증 규칙

1. 기존 `migrations/001_initial.sql`을 바꾸지 않는다. 새 번호 파일, catalog 기대 정의, 의미 있는 rollback 검사를 함께 추가한다. 기존 데이터 변환은 additive 또는 별도 검토한 migration으로 한다.
2. 실제 C# ID·수명·프로토콜과 맞춰 설계한다. runtime EntityId는 DB GUID CharacterId와 다르다. Class/MapId 확장 시 CHECK와 테스트를 함께 바꾼다.
3. 재실행 후 데이터와 SchemaVersion 중복 없음, CHECK/default 식·소속, PK/FK/index, stale-write 거부를 검사한다. 독립 검증자에게 DB 실행권을 넘길 때는 연결을 닫고 동시에 테스트하지 않는다.
4. 관리자/인증 설정을 더 바꾸거나 외부 바인딩을 넓히는 것은 별도 범위다. 7777 게임 서버를 점유하지 않는다.

## 실패 시

- Windows 연결 실패: 서비스 상태·정확한 local instance·현재 Windows DB 권한부터 확인한다. GameDB로 fallback하지 않는다.
- native WSL 실패: 서비스, `Get-NetTCPConnection -State Listen -LocalPort 14330`, WSL mirrored 상태와 `/opt/mssql-tools18/bin/sqlcmd`, 동일 Windows 사용자의 DPAPI 접근을 확인한다. LF 입력 전달을 유지한다. 암호를 출력하거나 방화벽 전체를 열어 진단하지 않는다.
- migration checksum/owner/catalog 불일치: DB와 작업 브랜치를 읽기 전용 비교한다. 이력을 지우거나 기존 SQL을 몰래 수정하지 않는다.
- 관리자 설정 때문에 문제가 생겼다면 MSSQL.md의 영향 범위를 확인하고 조용한 시간에 관리자 `Configure-WslAccess.ps1 -Action Restore`를 실행한다. 원래 인증/TCP 레지스트리 복구와 신규 login 비활성화, 서비스 재시작을 수행하며 DB 데이터와 복구 파일은 보존한다. 이번 실행에서는 Restore가 필요하지 않았으므로 실제 Restore 실행 검증은 미수행이다.

## 가장 먼저 할 후속 구현

인증된 AccountId/CharacterId와 세션 EntityId 매핑 → actor에서 immutable 저장값 캡처(MapId/quest latch 포함) → bounded 비동기 persistence worker와 트랜잭션/rowversion 충돌 처리 → 재접속 load와 안전 스폰 정책 → 종료 flush/실패 재시도/복구 테스트 순으로 별도 목표를 잡는다. 직업·좌표·HP·보스 해금이 저장 후보이며 파티/킬카운트/틱 상태는 현재 저장 범위가 아니다. 지금 테이블과 native SQL probe가 동작한다는 사실을 게임 자동 저장 완성으로 해석하지 않는다.
