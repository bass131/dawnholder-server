# MSSQL 구성 보고

`YYH_Desktop\SQLEXPRESS`의 별도 `Dawnholder_Dev`에 version 1을 적용했다. `dh.Account`/`dh.Character`/`dh.CharacterProgress`와 migration 이력이 생성되었으며 기존 GameDB, BaseballData, Northwind를 수정하지 않았다.

- 구현: 실제 C# 직업/맵/스냅샷/퀘스트 수명에 맞춘 GUID 소유 ID, 최소 체크포인트, FK/PK/CHECK/index 및 rowversion. 런타임 EntityId/파티/킬카운트/경제 데이터는 저장하지 않는다.
- 설치: 소유 DB 표시, 로컬 shared memory/통합 인증 강제, 번호별 SHA-256 migration, 트랜잭션 및 application lock. 기존 DB 채택/파괴·credential 교체 없음.
- 검증 성공: Windows PowerShell 5.1/7에서 재실행·데이터/버전 보존·CRUD rollback·중복/고아/범위 위반·stale write·두 연결 잠금·체크섬/미지 버전 거부. [근거](evidence/).
- WSL: mirrored 네트워크지만 native TCP probe 실패. TCP 비활성, Windows-only 인증, 비도메인 PC, Linux sqlcmd 미설치. SQL 인증/게임 서버 연결 성공으로 주장하지 않는다.
- 관리자안: `Configure-WslAccess.ps1`은 읽기 전용 Plan만 실행했다. 승인 시 loopback 14330, 별도 최소권한 SQL login, DPAPI 저장, 서비스 재시작 및 Restore를 제공한다. `Test-WslAccess.ps1`은 향후 native SQL 인증/권한/rollback 검사이며 미실행이다.
- 수정 경로: `99_Tools/database/`, `00_Document/operations/MSSQL.md`, 본 목표 폴더만. AGENTS/스킬/문서 지도/README/ARCHITECTURE/CURRENT 및 게임 코드는 변경하지 않았다.

현재 독립 검토 중이며 PR 정보는 검토 반영 후 기록한다. 서비스·인증 모드 변경은 메인이 사용자에게 구체적인 적용 여부를 확인한 뒤 진행할 별도 관리자 단계다. 실제 게임 저장·인증·재접속 복구 통합은 후속 목표다.
