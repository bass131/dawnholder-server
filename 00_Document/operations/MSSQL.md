# 로컬 MSSQL 개발 DB

`99_Tools/database`는 Windows SQL Express의 별도 개발 DB를 설치하고 검증하는 도구다. **게임 서버의 로그인·저장·재접속 복구는 아직 연결하지 않았다.** 서버가 실행 중이어도 이 테이블에는 자동 저장되지 않는다.

## 실제 구성과 실행

2026-09-29 구성: `YYH_Desktop\SQLEXPRESS`, Express 17.0.1000.7, `Dawnholder_Dev`. Windows 관리 도구는 통합 인증/shared memory, native WSL은 전용 SQL 인증/`tcp:127.0.0.1,14330`을 사용한다. 사용자 승인으로 mixed 인증과 loopback TCP만 활성화했다. 기존 `GameDB`의 `dbo.accounts(playerID, playerName, playerMoney, playerDate)`와 `BaseballData`, `Northwind`의 데이터·스키마는 수정하지 않았다. 다른 프로젝트의 accounts를 게임 계정으로 재사용하지 않는다.

저장소 루트의 Windows PowerShell 5.1 또는 PowerShell 7에서 실행한다. SQL Express가 실행 중이고 현재 Windows 사용자가 설치할 DB의 DDL 권한을 가져야 한다. 최초 생성에는 CREATE DATABASE 권한이 필요하다. 별도 NuGet, SQL PowerShell 모듈, 게임 빌드는 필요 없다.

```powershell
./99_Tools/database/Install-Database.ps1
./99_Tools/database/Test-Database.ps1
```

설정 주입은 두 스크립트의 `-Instance '.\SQLEXPRESS' -Database 'Dawnholder_Dev'` 인자 또는 현재 프로세스의 다음 환경변수로 한다. 이 변수는 **DB 도구 전용**이며 현재 GameServer가 읽는 설정이 아니다.

```powershell
$env:DAWNHOLDER_SQL_INSTANCE = '.\SQLEXPRESS'
$env:DAWNHOLDER_SQL_DATABASE = 'Dawnholder_Dev'
```

도구는 `.\인스턴스명` 및 `Dawnholder_Dev`/`Dawnholder_Dev_<suffix>`만 받는다. `lpc:`로 로컬 shared memory 연결을 강제하고 SQL MachineName도 확인한다. 암호를 받거나 저장하지 않는다. 로컬 개발 인증서에 한해 TrustServerCertificate를 사용하며 외부 서버 연결용 설정으로 복사하지 않는다.

처음 만든 DB에 소유 표시 `Dawnholder.DatabaseTool=development-v1`를 남긴다. 동명 DB에 표시가 없으면 채택하거나 변경하지 않고 실패한다. 기존 DB의 호환성을 추측하지 말고 조사 후 별도 suffix DB를 선택한다. CREATE DATABASE와 소유 표시 초기화 사이에 프로세스가 죽은 경우에도 자동 채택하지 않는다. 남은 빈 DB는 수동 조사 대상으로 보존한다.

`dh.SchemaVersion`은 migration 번호·파일명·정규화한 SQL의 SHA-256·적용 시각을 기록한다. CRLF/LF 차이는 체크섬에 영향을 주지 않는다. 생성 잠금과 migration 트랜잭션 잠금으로 동시 설치를 직렬화한다. 적용된 파일이 바뀌거나 DB에 알 수 없는 버전이 있으면 실패하며, 기존 migration을 수정하는 대신 새 번호의 SQL을 추가한다. 설치 재실행은 기존 게임 데이터를 수정하지 않는다. 자동 down migration, DROP, TRUNCATE, seed 계정은 없다.

## 저장 모델과 코드 근거

| 테이블 | 역할·제약 |
|---|---|
| `dh.Account` | 애플리케이션이 발급하는 GUID AccountId PK, 생성 UTC. 게임 인증 계정의 소유 식별자 자리만 마련하며 비밀번호·로그인명·외부 인증 공급자는 아직 정의하지 않음. |
| `dh.Character` | GUID CharacterId PK, AccountId FK, Class 0/1 CHECK, 생성 UTC, rowversion. AccountId 인덱스로 계정별 캐릭터 조회. 여러 캐릭터를 허용하되 슬롯/이름 같은 미구현 정책은 추가하지 않음. |
| `dh.CharacterProgress` | CharacterId PK/FK로 최대 한 체크포인트. MapId 0~3, real 좌표, int Hp/MaxHp, BossUnlocked bit, 저장 UTC, rowversion. `MaxHp > 0`, `0 <= Hp <= MaxHp`. FK 삭제 cascade 없음. |
| `dh.SchemaVersion` | 버전 PK, 파일명 UNIQUE, 양수 버전 CHECK, SHA-256 및 적용 UTC. 게임 계정과 무관한 설치 이력. |

- [GameWorld](../../02_Server/GameServer/Loop/GameWorld.cs)의 `NextEntityId()`는 프로세스 메모리에서 증가하고 [GameSession](../../02_Server/GameServer/Sessions/GameSession.cs)은 재접속 때 새 entity를 만든다. 따라서 runtime EntityId를 DB PK로 쓰지 않는다. 향후 인증된 AccountId/CharacterId와 세션 EntityId의 서버 내부 매핑이 필요하다. PlayerSnapshot의 과거 “DB primary key 대응” 주석은 현재 ID 수명과 일치하지 않는다.
- [CharacterClass](../../98_Shared/Protocol/CharacterClass.cs)는 Knight=0, Mage=1. [PlayerStats](../../98_Shared/GameData/Combat/PlayerStats.cs)의 공격력·방어력·이동/점프 속도는 Class로 다시 생성한다. 레벨·경험치·장비·재화·길드는 현재 코드에 저장할 계약이 없어 넣지 않았다.
- [PlayerSnapshot](../../02_Server/GameServer/Maps/PlayerSnapshot.cs)의 Position/Hp/MaxHp를 체크포인트 컬럼으로 표현한다. [MapId](../../02_Server/GameServer/Maps/MapId.cs)는 Town=0, HuntingGround=1, BossRoom=2, Ending=3이다. MapId는 Snapshot에 없어 향후 actor에서 함께 캡처해야 한다.
- [QuestRegistry](../../02_Server/GameServer/Quest/QuestRegistry.cs)의 `_bossUnlocked`는 현재 **세션 한정** latch다. 여기서는 향후 캐릭터별 재접속에도 해금을 유지할 수 있는 저장 자리를 마련했다. 기존 솔로 카운트와 파티 공유 카운트는 보스 처치 때 초기화되고 서로 수명이 달라 저장하지 않는다. 현재 latch를 실제 DB에 쓰거나 복원하지 않는다.

재접속 저장 범위의 제안은 직업, 마지막 서버 체크포인트(맵·좌표·HP), 보스 해금이다. 파티 멤버십·초대·킬카운트·몬스터/보스 인스턴스·입력 큐·속도·FSM·틱·쿨다운·무적·position history는 복원하지 않는다. 이 정책의 실제 게임 적용은 후속 목표에서 확정한다. dead HP, 변경된 맵 지형/스탯 밸런스, BossRoom/Ending 재입장에 대한 안전 스폰 정책도 그때 구현해야 한다.

DB의 좌표형은 C# float에 대응하는 real이다. SQL은 현재 맵의 실제 지형 범위를 알지 못하므로 저장 전 서버에서 유한값/유효 위치를 확인해야 한다. 음수 HP 스냅샷은 0으로 정규화해야 하며, 클라이언트가 보낸 HP/좌표를 그대로 저장하면 안 된다.

## 후속 서버 통합 계약

tick/actor에서 Class, 좌표, HP, MapId, quest latch를 값으로 복사하고 비동기 큐/worker에서 DB I/O를 실행한다. Snapshot의 Stats 참조 자체를 여러 스레드에서 변경 가능한 상태로 공유하지 않는다. 계정 소유권 확인, 서버 인증, 세션 종료 flush, bounded queue/backpressure, 중복 저장, 실패 재시도/종료 복구는 별도 구현 대상이다.

저장은 읽었던 rowversion으로 낙관적 동시성을 확인해야 한다. 예시의 모든 값은 서버가 검증한 SQL 파라미터이며 인증된 계정은 클라이언트 주장값이 아니다.

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

## 실제 검증

[목표 결과](../../01_Phases/goals/2026-09-29-mssql-setup/goal.md)와 [근거 폴더](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/)를 참고한다.

`Test-Database.ps1`은 GUID로 격리된 시험 행만 만들고 끝에서 반드시 rollback한다. 카탈로그, PK/unique/FK/check/nullability, 두 직업·네 맵·HP 경계, CRUD, 데이터가 있는 상태의 migration 두 번 재실행, 체크섬/미지 버전 거부, stale rowversion, 별도 연결의 migration 잠금 및 쓰기 잠금 timeout을 검사한다. CHECK/default의 실제 식과 소속 테이블·열도 비교하며, Class CHECK 완화·해금 DEFAULT1·잘못된 default 열을 잠깐 적용한 세 음성사례가 거부됨을 각각 rollback으로 확인한다. 운영 트래픽이 없는 개발 DB에서 실행한다. 삭제도 이 트랜잭션에서 만든 행만 대상으로 한다. rollback 전 남겨둔 witness 행이 rollback 후 없음을 확인한다. 실패한 경우에도 finally에서 rollback하며 실제 게임 행을 seed하거나 commit하지 않는다. rowversion 내부 카운터는 rollback 후에도 증가할 수 있고 연속 번호를 보장하지 않는다.

Windows PowerShell 5.1/7 및 독립 재검토는 통과했다. Ubuntu 26.04 mirrored 모드에 Microsoft 공식 26.04 저장소의 `mssql-tools18`/`msodbcsql18` 18.7.1.1-1을 설치했다. 승인된 관리자 Enable 후 native WSL SQL 인증·최소권한·rollback 저장이 성공했다. 초기 TCP 실패 기록은 [초기 probe](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl.txt), 최종 성공은 [native 인증 검사](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl-auth.txt)에 구분한다. **게임 서버 저장/재접속 복구는 미구현이며 Restore 실행 검증은 미실행**이다.

## WSL 연결 및 관리자 복구

이 PC는 사용자 승인 및 독립 검토 후 관리자 Enable까지 적용했다. 재설치할 필요가 없으며 평소에는 아래 Test-WslAccess만 사용한다. 다른 PC의 Enable 및 현재 PC의 Restore는 영향 범위를 확인한 점검 시간에 관리자 PowerShell에서 실행한다. 스크립트 자체가 UAC를 열지는 않는다. 이번 적용은 승인된 elevated PowerShell을 한 번 실행해 SQL 서비스를 재시작했으며, 복구 테스트만을 위한 추가 재시작은 하지 않았다.

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

작업 재개 순서와 현재 PR/승인 경계는 [인계문](../../01_Phases/goals/2026-09-29-mssql-setup/handoff.md)에서 시작한다. DB 엔진은 Windows 서비스이며 WSL에서 별도 서버 프로세스를 띄울 필요가 없다. 서비스 시작 유형은 기존 Manual을 유지했으므로 재부팅 후에는 관리자 PowerShell에서 필요할 때 시작한다.

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
# 중지 상태일 때만 관리자 PowerShell에서 실행
Start-Service 'MSSQL$SQLEXPRESS'
./99_Tools/database/Test-WslAccess.ps1
```

설계 참고: [SQL application lock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql), [TCP ListenAll 및 고정 포트](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-a-server-to-listen-on-a-specific-tcp-port), [Windows Export-Clixml/DPAPI](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/export-clixml).
