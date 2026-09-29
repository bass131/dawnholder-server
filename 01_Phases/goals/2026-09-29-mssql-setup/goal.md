# MSSQL 개발 환경 및 최소 스키마

- 상태: Windows/WSL DB 구성·자체 검증·독립 재검증 완료. [PR #127](https://github.com/bass131/dawnholder-server/pull/127)은 병합하지 않았으며 해당 PR에 대한 사용자 명시 승인 필요.
- 범위: 별도 작업 브랜치의 DB 전용 도구/SQL/문서, 로컬 SQL Express의 전용 개발 DB.
- 제외: 게임 서버 저장/로그인 통합, 기존 DB 데이터·credential 변경, 방화벽 공개, LAN 바인딩. 후속 사용자 승인으로 제한적 TCP/혼합 인증/새 최소권한 SQL login 및 서비스 재시작을 포함함.
- 완료조건: 실제 로컬 적용, 재실행 보존, PK/FK/unique/check/index 검사, rollback CRUD 및 동시성 검사, Windows/WSL 결과 구분.
- 조사: `YYH_Desktop\SQLEXPRESS`, Express 17.0.1000.7, Windows 통합 인증/Shared memory 성공. 기존 GameDB(accounts), BaseballData, Northwind 보존.
- 설계: 접속별 EntityId와 영속 CharacterId 분리. 계정은 인증 없는 소유 식별자만, 직업 및 서버 체크포인트/보스 해금 저장용 스키마. 파티/킬카운트/틱 상태는 제외. 게임 연결 전까지 실제 재접속 복구는 없음.
- 연결 구성: 사용자 승인 후 mixed 인증 및 `127.0.0.1`/`::1`:14330만 활성화. Windows DPAPI에 새 login 비밀 보관, native Ubuntu 26.04 ODBC 연결 성공. SQL Browser·방화벽·기존 DB 데이터는 보존.
- 산출물: `99_Tools/database/`, `00_Document/operations/MSSQL.md`, 이 목표 폴더의 검증 근거 및 보고서.

## 결과 및 검증

- 실제 적용: `Dawnholder_Dev`, `dh.SchemaVersion` 버전 1, `dh.Account`, `dh.Character`, `dh.CharacterProgress`.
- Windows PowerShell 5.1 및 7: 설치 재실행과 rollback 검증 통과. PK/FK/unique/check/index, CRUD, 데이터 보존, stale rowversion, 두 연결 잠금, checksum/미지 버전 거부 확인.
- 근거: [pwsh](evidence/windows-pwsh.txt), [PowerShell 5.1](evidence/windows-ps51.txt), [최종 DB 상태](evidence/catalog.txt), [관리자 Enable](evidence/admin-enable.txt), [native WSL 인증](evidence/wsl-auth.txt), [패키지](evidence/wsl-packages.txt), [관리자 변경 계획](evidence/admin-plan.txt), [구문 검사](evidence/powershell-parse.txt). `wsl.txt`는 적용 전 실패를 남긴 초기 조사다.
- 독립 검증: P2였던 CHECK/default 실제 식 누락을 수정하고 40 PASS 및 3개 음성사례 복구 확인. native WSL 로그인/저장/rowversion/rollback, 세 테이블 최소권한, 다른 3 DB 접근 불가, loopback 전용 listener 및 게임행 0을 별도 확인. 메인 전달 원시 근거: `%TEMP%/dawnholder-mssql-independent-20260929/`.
- 관리자 Enable 성공, 복구를 위한 Restore 코드/안내는 준비했으나 실제 Restore 실행은 불필요하여 미실행.
- 게임 프로토콜·서버·공유 DLL·Unity 에셋 변경 없음. 게임 빌드/실행·자동 저장·재접속 복구는 이번 검증 대상 아님.
- 문서 연결 제안: 메인 소유 `DEVELOPMENT.md` 또는 문서 지도에서 `operations/MSSQL.md` 링크 추가 검토. 본 작업에서는 수정하지 않음.

## 남은 단계

1. PR 최종 문서/CI 확인 및 사용자의 해당 PR 병합 승인. 자동 병합 금지.
2. 게임 저장 서비스/인증/계정과 세션 매핑/재접속 정책은 별도 목표.
3. 다음 세션은 [handoff.md](handoff.md)에서 시작. 현재 DB에 Enable을 반복하거나 비밀 파일을 출력하지 않음.
