# 로컬 MSSQL 개발 DB

`99_Tools/database`는 Windows SQL Express의 별도 개발 DB를 설치하고 검증하는 도구다. **게임 서버의 로그인·저장·재접속 복구는 아직 연결하지 않았다.** 서버가 실행 중이어도 이 테이블에는 자동 저장되지 않는다.

## 현재 저장 연동 구현과 실행 경계

현재 작업은 [저장소 구현 목표](../../01_Phases/goals/2026-10-02-persistence-repository/goal.md)다. `001_initial.sql`은 보존하고, 새 migration002~014와 수명 관리 도구를 구현·검토 중이다. **새 SQL의 적용·DB 연결·계정/권한 변경은 아직 실행하지 않았다.** 이 문서 아래의 2026-09-29 검증 결과는 새 저장 연동의 통과 근거가 아니다.

현재 installer는 기본 DB를 자동 생성하는 진입점이 아니다. 명시한 DB·manifest·승인 계획과 별도로 검토한 계획 hash를 대조하고, 수명 도구가 만든 정확한 DB만 설치한다. 인자 없이 과거의 `Install-Database.ps1` 명령을 실행하지 않는다. 실제 명령·실행 주체·승인 입력은 최초 실행 전 G2 독립 검토 후 goal에 고정한다. 승인 계획의 문자열이나 `ExecutionApproved` 값 자체가 사용자 승인을 대신하지 않는다.

| 도구 | 책임 |
|---|---|
| [New-TestDatabase.ps1](../../99_Tools/database/test-environment/New-TestDatabase.ps1) | 승인된 이름의 부재 확인, 한 번의 시험 DB 생성과 수명 기록 |
| [Install-Database.ps1](../../99_Tools/database/Install-Database.ps1) | 동일 DB의 001 기준 설치와 전체 migration 설치 단계 |
| [Initialize-CharacterBinding.ps1](../../99_Tools/database/test-environment/Initialize-CharacterBinding.ps1) | 승인된 슬롯·계정·캐릭터 식별자 바인딩 |
| [Set-TestPrincipals.ps1](../../99_Tools/database/test-environment/Set-TestPrincipals.ps1) | 승인된 시험 principal·권한·격리 credential 구성 |
| [Remove-TestEnvironment.ps1](../../99_Tools/database/test-environment/Remove-TestEnvironment.ps1) | identity·미확정 작업·owner·연결 정산 대조 후 정확한 자원 정리 |
| [Environment.Common.ps1](../../99_Tools/database/test-environment/Environment.Common.ps1) | 승인 입력·수명 manifest·identity·단계 기록의 공통 경계 |

DB명·principal·executor·endpoint·TLS·경로는 검토된 승인 계획에 둔다. 기존 DB나 같은 이름의 자원을 자동 채택하거나 다른 이름으로 재생성하지 않는다. 실패·부분 실행은 원문과 자원을 보존하고 조사하며 강제 연결 종료나 자동 정리로 넘기지 않는다. migration checksum은 개행을 정규화하며 적용 이력·파일·module/catalog 대조를 유지한다. 이번 재번호화는 새 SQL을 아직 적용하지 않은 구현 단계에서 승인한 변경이며, 적용된 migration을 나중에 덮어쓰는 운영 규칙이 아니다.

기존 `Test-Database.ps1`과 `Test-WslAccess.ps1`의 아래 실행 기록은 001 구성 당시의 경로다. 현재 저장소 RPC·권한·경쟁·복구 검증은 별도 검증 계획을 따르며, 과거 도구의 성공을 새 계약의 검증으로 합치지 않는다.

## 2026-09-29 기존 구성 기록

2026-09-29 구성: `YYH_Desktop\SQLEXPRESS`, Express 17.0.1000.7, `Dawnholder_Dev`. Windows 관리 도구는 통합 인증/shared memory, native WSL은 전용 SQL 인증/`tcp:127.0.0.1,14330`을 사용한다. 사용자 승인으로 mixed 인증과 loopback TCP만 활성화했다. 기존 `GameDB`의 `dbo.accounts(playerID, playerName, playerMoney, playerDate)`와 `BaseballData`, `Northwind`의 데이터·스키마는 수정하지 않았다. 다른 프로젝트의 accounts를 게임 계정으로 재사용하지 않는다.

당시 Windows PowerShell 5.1/7 도구는 명시 인자 또는 프로세스의 `DAWNHOLDER_SQL_INSTANCE`·`DAWNHOLDER_SQL_DATABASE`를 읽고, `lpc:` 로컬 shared memory·MachineName·`Dawnholder.DatabaseTool=development-v1` 소유 표시를 확인했다. 이 변수는 GameServer 설정이 아니며 현재 시험환경 수명 도구의 승인 입력을 대신하지 않는다. 기존 DB와 계정은 현재 목표의 수정·정리 대상에서 제외한다.

## 저장 모델과 코드 근거

| 테이블 | 역할·제약 |
|---|---|
| `dh.Account` | 애플리케이션이 발급하는 GUID AccountId PK, 생성 UTC. 게임 인증 계정의 소유 식별자 자리만 마련하며 비밀번호·로그인명·외부 인증 공급자는 아직 정의하지 않음. |
| `dh.Character` | GUID CharacterId PK, AccountId FK, Class 0/1 CHECK, 생성 UTC, rowversion. AccountId 인덱스로 계정별 캐릭터 조회. 여러 캐릭터를 허용하되 슬롯/이름 같은 미구현 정책은 추가하지 않음. |
| `dh.CharacterProgress` | CharacterId PK/FK로 최대 한 체크포인트. MapId 0~3, real 좌표, int Hp/MaxHp, BossUnlocked bit, 저장 UTC, rowversion. `MaxHp > 0`, `0 <= Hp <= MaxHp`. FK 삭제 cascade 없음. |
| `dh.SchemaVersion` | 버전 PK, 파일명 UNIQUE, 양수 버전 CHECK, SHA-256 및 적용 UTC. 게임 계정과 무관한 설치 이력. |

- [GameWorld](../../02_Server/GameServer/Loop/GameWorld.cs)의 `NextEntityId()`는 프로세스 메모리에서 증가하고 [GameSession](../../02_Server/GameServer/Sessions/GameSession.cs)은 재접속 때 새 entity를 만든다. 따라서 runtime EntityId를 DB PK로 쓰지 않는다. 후속 연동에는 서버가 확인한 AccountId/CharacterId와 세션 EntityId의 매핑이 필요하다. PlayerSnapshot도 현재 이 메모리 ID와 DB 계약을 구분한다.
- [CharacterClass](../../98_Shared/Protocol/CharacterClass.cs)는 Knight=0, Mage=1. [PlayerStats](../../98_Shared/GameData/Combat/PlayerStats.cs)의 공격력·방어력·이동/점프 속도는 Class로 다시 생성한다. 레벨·경험치·장비·재화·길드는 현재 코드에 저장할 계약이 없어 넣지 않았다.
- [PlayerSnapshot](../../02_Server/GameServer/Maps/PlayerSnapshot.cs)은 Position/CurrentHp/MaxHp와 불변 PlayerStats 정의를 캡처한다. [MapId](../../02_Server/GameServer/Maps/MapId.cs)는 Town=0, HuntingGround=1, BossRoom=2, Ending=3이다. 기존 컬럼은 더 넓은 checkpoint를 표현할 수 있지만, 선택된 첫 연동 범위에서는 동적 snapshot을 저장하지 않고 Town 안전 spawn과 저장 클래스의 기본 풀HP로 복귀한다.
- [QuestRegistry](../../02_Server/GameServer/Quest/QuestRegistry.cs)의 `_bossUnlocked`는 현재 **세션 한정** latch다. 여기서는 향후 캐릭터별 재접속에도 해금을 유지할 수 있는 저장 자리를 마련했다. 기존 솔로 카운트와 파티 공유 카운트는 보스 처치 때 초기화되고 서로 수명이 달라 저장하지 않는다. 현재 latch를 실제 DB에 쓰거나 복원하지 않는다.

사용자가 선택한 첫 연동 범위는 개발 고정 계정1/캐릭터1, 최초 클래스 유지, Town 풀HP 복귀, quest/보스 해금 세션 한정이다. identity/class와 안전 checkpoint를 다루며 전투 위치/HP/해금, 파티·킬 수·적 인스턴스·입력/FSM/cooldown/위치 이력은 복원하지 않는다. 선택 범위는 [DB 설계 목표](../../01_Phases/goals/2026-09-29-persistence-design/goal.md), 상세 계약은 [저장소 기술 설계](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md)에 있다. 저장소 구현은 진행 중이며 실제 게임 저장/복원 연결은 미구현이다.

DB의 좌표형은 C# float에 대응하는 real이다. SQL은 실제 지형을 모르므로 서버가 안전 spawn의 유한값/유효 위치를 검증한다. 이번 안전 projection은 Town/저장 클래스 기본 HP·MaxHp/해금false이며 runtime rawHP 전송 계약을 바꾸지 않는다. 클라이언트가 보낸 HP/좌표를 저장 권위로 쓰지 않는다.

## 후속 서버 통합 계약

게임 tick에서 DB 완료를 기다리지 않는다. 현재 PlayerStats는 불변 정의이며 후속 worker에는 identity/class·안전 projection과 작업 수명의 불변 값만 넘긴다. 첫 범위는 create/load·권위 획득/해제·미확정 작업 종결이고 주기/매 logout 동일 값 쓰기나 Quest snapshot pipeline은 없다. DB 직렬화/fence·읽기 token·종료 인계·권위 class 입장은 [설계 계약](../../01_Phases/goals/2026-09-29-persistence-design/design.md)대로 별도 구현·검증해야 한다.

저장은 읽었던 rowversion으로 낙관적 동시성을 확인해야 한다. 아래 SQL은 기존 전필드 checkpoint 후보의 예시이며, 이번 MVP의 실행 쿼리나 영구 해금 정책이 아니다. acquire/load와 모든 mutation/release의 DB 직렬화/fence 및 Character 읽기 의존성은 추가 설계 대상이다. 예시 값은 서버가 검증한 SQL 파라미터이며 계정은 클라이언트 주장값이 아니다.

```sql
UPDATE p
SET MapId=@MapId, PositionX=@X, PositionY=@Y, Hp=@Hp, MaxHp=@MaxHp,
    BossUnlocked=CASE WHEN p.BossUnlocked=1 OR @BossUnlocked=1 THEN 1 ELSE 0 END,
    SavedUtc=SYSUTCDATETIME()
OUTPUT inserted.Version
FROM dh.CharacterProgress AS p
JOIN dh.Character AS c ON c.CharacterId=p.CharacterId
WHERE p.CharacterId=@CharacterId AND c.AccountId=@AuthenticatedAccountId
  AND p.Version=@ExpectedVersion;
```

결과 0행은 충돌/권한 없음/미존재를 처리할 신호다. 재시도 전에 상태를 다시 읽어야 하며, 무조건 덮어쓰기 하면 안 된다. SQL rowversion 자체가 stale write를 자동 차단하지는 않는다. Character와 Progress를 함께 바꿀 때는 하나의 트랜잭션에서 두 버전을 검사한다. SavedUtc는 진단 시각이며 동시성 토큰이 아니다. UTC와 rowversion은 성공적인 새 저장마다 갱신하고, 해금은 서버 정책에 따라 단조롭게 유지한다.

## 2026-09-29 구성 검증 기록

[목표 결과](../../01_Phases/goals/2026-09-29-mssql-setup/goal.md)와 [근거 폴더](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/)를 참고한다.

`Test-Database.ps1`은 GUID로 격리된 시험 행만 만들고 끝에서 반드시 rollback한다. 카탈로그, PK/unique/FK/check/nullability, 두 직업·네 맵·HP 경계, CRUD, 데이터가 있는 상태의 migration 두 번 재실행, 체크섬/미지 버전 거부, stale rowversion, 별도 연결의 migration 잠금 및 쓰기 잠금 timeout을 검사한다. CHECK/default의 실제 식과 소속 테이블·열도 비교하며, Class CHECK 완화·해금 DEFAULT1·잘못된 default 열을 잠깐 적용한 세 음성사례가 거부됨을 각각 rollback으로 확인한다. 운영 트래픽이 없는 개발 DB에서 실행한다. 삭제도 이 트랜잭션에서 만든 행만 대상으로 한다. rollback 전 남겨둔 witness 행이 rollback 후 없음을 확인한다. 실패한 경우에도 finally에서 rollback하며 실제 게임 행을 seed하거나 commit하지 않는다. rowversion 내부 카운터는 rollback 후에도 증가할 수 있고 연속 번호를 보장하지 않는다.

Windows PowerShell 5.1/7 및 독립 재검토는 통과했다. Ubuntu 26.04 mirrored 모드에 Microsoft 공식 26.04 저장소의 `mssql-tools18`/`msodbcsql18` 18.7.1.1-1을 설치했다. 승인된 관리자 Enable 후 native WSL SQL 인증·최소권한·rollback 저장이 성공했다. 초기 TCP 실패 기록은 [초기 probe](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl.txt), 최종 성공은 [native 인증 검사](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl-auth.txt)에 구분한다. **게임 서버 저장/재접속 복구는 미구현이며 Restore 실행 검증은 미실행**이다.

## 기존 WSL 연결 및 관리자 복구 기록

당시 이 PC는 사용자 승인 및 독립 검토 후 관리자 Enable까지 적용했다. 아래는 기존 도구의 구성·복구 기록이며 현재 저장 연동 시험환경의 실행 절차가 아니다. 기존 설정·credential을 새 목표에 재사용하거나 Enable·Restore·서비스 재시작을 이번 승인 범위로 해석하지 않는다. 다른 PC의 Enable 및 현재 PC의 Restore는 별도로 영향 범위를 확인한 점검 시간에 관리자 PowerShell에서 실행한다. 스크립트 자체가 UAC를 열지는 않는다. 당시 적용은 승인된 elevated PowerShell을 한 번 실행해 SQL 서비스를 재시작했으며, 복구 테스트만을 위한 추가 재시작은 하지 않았다.

```powershell
# 현재 설정과 정확한 변경 목록만 출력. 관리자 권한 불필요, 쓰기 없음.
./99_Tools/database/Configure-WslAccess.ps1 -Action Plan
```

적용된 변경 및 다른 PC에서의 승인 범위:

1. `SQLEXPRESS`의 LoginMode를 Windows-only(1)에서 mixed(2)로 변경한다. 기존 로그인/암호를 교체하지 않는다.
2. TCP Enabled=1, ListenOnAllIPs=0. 등록된 `127.0.0.1`과 `::1`만 Enabled=1, TcpPort=14330, TcpDynamicPorts=''로 바꾼다. 다른 IP는 비활성화한다. IPAll은 보존하고 ListenAll=0으로 무시된다. SQL Browser 및 방화벽 규칙은 변경하지 않는다.
3. 난수 이름의 신규 SQL login과 `Dawnholder_Dev` user를 만들고, 세 게임 테이블에 SELECT/INSERT/UPDATE, SchemaVersion에는 SELECT만 부여한다. 삭제·DDL·db_owner/sysadmin·다른 사용자 DB 권한은 부여하지 않는다.
4. 같은 인스턴스를 쓰는 모든 DB에 잠깐 영향을 주는 SQL 서비스 재시작 1회가 필요하다. 다른 사용자 SQL 세션이 있으면 스크립트는 중단한다. 검사 직후 새 접속까지 막는 유지보수 모드는 아니므로 실행 전에 다른 도구/사용자의 SQL 작업을 중지해야 한다.
5. `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS`에 원래 레지스트리 값 및 신규 login SID를 `state.clixml`, 256비트 난수 기반 암호를 Windows DPAPI `credential.clixml`로 저장한다. 폴더 ACL은 실행 사용자와 SYSTEM으로 제한한다. 동일 PC/Windows 사용자만 복호화할 수 있다. 저장소에 복사하거나 암호를 출력하지 않는다. 기존 폴더가 있으면 자동 재적용/비밀 교체 대신 거부한다.

```powershell
# 사용자가 승인한 뒤, 동일 Windows 계정의 관리자 PowerShell에서 실행.
./99_Tools/database/Configure-WslAccess.ps1 -Action Enable

# 문제가 있으면 같은 관리자 계정에서 원래 설정 복구 및 서비스 재시작.
./99_Tools/database/Configure-WslAccess.ps1 -Action Restore
```

Restore는 원래 설정을 복구하고 SID가 일치하는 신규 login만 비활성화한다. 데이터·DB user·암호화된 복구 파일은 삭제하지 않는다. 적용 후 다른 관리자가 바꾼 값이 있으면 덮어쓰지 않고 수동 검토를 요구한다. Enable 도중 실패해도 백업을 보존하므로 무조건 재실행하지 말고 Restore를 사용한다. SQL 서비스가 시작 실패한 경우에도 원래 레지스트리를 먼저 복구한 뒤 시작한다. 백업 경로 생성 직후 중단되어 state.clixml이 없다면 아직 SQL/레지스트리 변경 전이다. SQL 설정이 복구된 뒤에도 데이터와 이전 시도 기록은 보존하며 새 Enable은 별도 검토한다.

WSL Ubuntu에 native `mssql-tools18`의 `/opt/mssql-tools18/bin/sqlcmd`가 필요하며 이 PC에는 설치 완료했다. 새 환경에서는 Ubuntu 버전에 맞는 [Microsoft 공식 절차](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup-tools)를 따른다. 이번에는 Ubuntu 26.04용 `packages-microsoft-prod.deb`로 서명된 저장소를 등록하고 `ACCEPT_EULA=Y apt-get install -y mssql-tools18`로 필요한 ODBC 의존성만 설치했으며 기존 패키지 upgrade는 하지 않았다. 구성 후 Windows PowerShell **7**에서 실행한다.

```powershell
./99_Tools/database/Test-WslAccess.ps1 -Distribution Ubuntu
```

이 probe는 DPAPI 비밀을 LF로 끝나는 stdin으로 Linux child에 전달하고, child 수명 동안만 SQLCMDPASSWORD 환경변수에 둔다. Windows CRLF를 쓰면 암호에 CR이 붙어 로그인 실패하므로 LF를 명시한다. 비밀을 명령 인자·콘솔·Linux 파일에 출력하지 않는다. localhost의 self-signed 개발 인증서를 신뢰하고 SQL 로그인/권한/rollback 저장을 검사한다. 실패하면 메시지와 포트 상태를 확인하되 방화벽 전체 공개나 LAN 바인딩 확대를 자동 수행하지 않는다. 프로브 성공은 native Linux ODBC 경로 검증이며 .NET GameServer의 저장 서비스 구현을 의미하지 않는다.

## 다음 세션

당시 DB 구성·접속 인계는 [인계문](../../01_Phases/goals/2026-09-29-mssql-setup/handoff.md), 현재 작업·승인 범위는 [CURRENT](CURRENT.md)의 goal에서 확인한다. DB 엔진은 Windows 서비스이며 WSL에서 별도 서버 프로세스를 띄울 필요가 없다. 서비스 시작 유형은 기존 Manual을 유지했으므로 재부팅 후에는 관리자 PowerShell에서 필요할 때 시작한다.

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
# 중지 상태일 때만 관리자 PowerShell에서 실행
Start-Service 'MSSQL$SQLEXPRESS'
./99_Tools/database/Test-WslAccess.ps1
```

설계 참고: [SQL application lock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql), [TCP ListenAll 및 고정 포트](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-a-server-to-listen-on-a-specific-tcp-port), [Windows Export-Clixml/DPAPI](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/export-clixml).
