# MSSQL 구성 보고

`YYH_Desktop\SQLEXPRESS`의 별도 `Dawnholder_Dev`에 version 1과 최소 영속 데이터 스키마를 적용했다. Windows 통합 인증과 native WSL 최소권한 SQL 인증/rollback 저장이 모두 성공했다. 게임 서버 저장·로그인·재접속 복구 통합은 아직 미구현이다.

- 구현: 실제 C# 직업/맵/스냅샷/퀘스트 수명에 맞춘 GUID 소유 ID, 최소 체크포인트, FK/PK/CHECK/index 및 rowversion. 런타임 EntityId/파티/킬카운트/경제 데이터는 저장하지 않는다.
- 설치: 소유 DB 표시, 로컬 shared memory/통합 인증 강제, 번호별 SHA-256 migration, 트랜잭션 및 application lock. 기존 DB 채택/파괴·credential 교체 없음.
- 검증 성공: Windows PowerShell 5.1/7에서 재실행·데이터/버전 보존·CRUD rollback·중복/고아/범위 위반·stale write·두 연결 잠금·체크섬/미지 버전 거부. CHECK/default 식·소속 검증과 3개 변조 음성사례를 보강했고 독립 40 PASS. [근거](evidence/).
- WSL: Ubuntu 26.04 mirrored, 공식 mssql-tools18/msodbcsql18 18.7.1.1-1. 사용자 승인 후 혼합 인증과 loopback `127.0.0.1`/`::1`:14330만 활성화하고 SQL 서비스를 한 번 재시작했다. 방화벽/SQL Browser/기존 DB 데이터·credential은 보존했다.
- 비밀/권한: 신규 login은 DPAPI로 로컬 보관, 세 게임 테이블 SELECT/INSERT/UPDATE 및 SchemaVersion SELECT만 허용. 독립 native WSL 인증/저장/rowversion/rollback 및 다른 3 DB 접근 거부 확인. 비밀 값과 blob은 저장소/보고에 포함하지 않았다.
- 복구: `Configure-WslAccess.ps1 -Action Restore`와 안내를 준비했다. 정상 적용됐으므로 Restore 실제 실행은 미수행이다.
- 수정 경로: `99_Tools/database/`, `00_Document/operations/MSSQL.md`, 본 목표 폴더만. AGENTS/스킬/문서 지도/README/ARCHITECTURE/CURRENT 및 게임 코드는 변경하지 않았다.

[PR #127](https://github.com/bass131/dawnholder-server/pull/127), branch `bass131/feat-mssql-game-schema`. DB 구성과 native 연결의 독립 검증은 완료했고 병합은 해당 PR에 대한 사용자 명시 승인 전까지 수행하지 않는다. 다음 세션은 [handoff.md](handoff.md)와 [MSSQL 운영 안내](../../../00_Document/operations/MSSQL.md)에서 시작한다.
