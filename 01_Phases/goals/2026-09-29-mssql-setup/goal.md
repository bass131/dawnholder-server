# MSSQL 개발 환경 및 최소 스키마

- 상태: Windows DB 적용·자체 검증 완료, 메인 배정 독립 리뷰 중. 관리자 단계 미실행, PR 병합은 해당 PR에 대한 사용자 승인 필요.
- 범위: 별도 작업 브랜치의 DB 전용 도구/SQL/문서, 로컬 SQL Express의 전용 개발 DB.
- 제외: 게임 서버 저장/로그인 통합, 기존 GameDB 변경, 서비스 재시작, TCP/방화벽/인증 설정 변경.
- 완료조건: 실제 로컬 적용, 재실행 보존, PK/FK/unique/check/index 검사, rollback CRUD 및 동시성 검사, Windows/WSL 결과 구분.
- 조사: `YYH_Desktop\SQLEXPRESS`, Express 17.0.1000.7, Windows 통합 인증/Shared memory 성공. 기존 GameDB(accounts), BaseballData, Northwind 보존.
- 설계: 접속별 EntityId와 영속 CharacterId 분리. 계정은 인증 없는 소유 식별자만, 직업 및 서버 체크포인트/보스 해금 저장용 스키마. 파티/킬카운트/틱 상태는 제외. 게임 연결 전까지 실제 재접속 복구는 없음.
- 연결 제약: TCP 비활성, SQL Browser 중지, Windows TCP 및 WSL native TCP probe 실패. Windows-only 인증/비도메인 PC. 메인 지시에 따라 관리자/UAC/인증모드/서비스 변경은 미실행, 실행·복구 스크립트만 준비.
- 산출물: `99_Tools/database/`, `00_Document/operations/MSSQL.md`, 이 목표 폴더의 검증 근거 및 보고서.

## 결과 및 검증

- 실제 적용: `Dawnholder_Dev`, `dh.SchemaVersion` 버전 1, `dh.Account`, `dh.Character`, `dh.CharacterProgress`.
- Windows PowerShell 5.1 및 7: 설치 재실행과 rollback 검증 통과. PK/FK/unique/check/index, CRUD, 데이터 보존, stale rowversion, 두 연결 잠금, checksum/미지 버전 거부 확인.
- 근거: [pwsh](evidence/windows-pwsh.txt), [PowerShell 5.1](evidence/windows-ps51.txt), [WSL probe](evidence/wsl.txt), [관리자 변경 계획](evidence/admin-plan.txt), [구문 검사](evidence/powershell-parse.txt).
- 관리자 계획: loopback 14330 전용 TCP, 신규 DB 최소권한 SQL login, Windows DPAPI 비밀 보관, 원래 레지스트리 복구 및 신규 login 비활성화. Enable/Restore/WSL SQL 인증은 미실행.
- 게임 프로토콜·서버·공유 DLL·Unity 에셋 변경 없음. 게임 빌드/실행·자동 저장·재접속 복구는 이번 검증 대상 아님.
- 문서 연결 제안: 메인 소유 `DEVELOPMENT.md` 또는 문서 지도에서 `operations/MSSQL.md` 링크 추가 검토. 본 작업에서는 수정하지 않음.

## 남은 단계

1. 독립 리뷰 피드백 반영 및 PR 작성.
2. 메인이 구체적인 관리자 계획의 사용자 적용 승인 여부 확인. 현재 환경에서 WSL native SQL 연결은 완료로 표시하지 않음.
3. 게임 저장 서비스/인증/계정과 세션 매핑/재접속 정책은 별도 목표.
