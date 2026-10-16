# 노트북 SQL Express 시연 DB 도구

상태: PR1(PR246) 병합 완료. 데스크톱 개발을 마감하고 노트북 PR2 실제 실행·원시 검증을 기다린다. PR1 오프라인 검증을 노트북 실증 성공으로 표현하지 않는다.

## 진척 단계

- [x] 목표·경계 승인
- [x] 선행 시험 준비
- [x] 도구 구현
- [x] 독립 검증
- [x] 실행 카드 확정
- [x] PR246 병합
- [>] 노트북 실제 실행
- [ ] 실증 PR 병합
- [ ] 종료 점검

## 적용 중인 사용자 결정

메인 msg_c7aae3f50612(2026-10-16T00:34:01Z)가 전달한 메인 pane Enter 제출 원문이다. 이 pane의 직접 사용자 입력으로 격상하지 않는다. 원문·발신 근거는 `.backups/verification/2026-10-16-laptop-sql-express/entry-inbox.json`이다.

> 대시보드 결정 응답: 1) Core 리드 Astra medium 시범 - 다음 goal(노트북 도구)에도 계속할지 → A 계속 · 2) 계획 검토 - Core 2 - 노트북 SQL Express 시연 DB 도구 범위 초안 → A 승인 (초안 msg_cd0661d6ed9b)

> 참고로 아직 지금 데스크탑이야

앞선 결정 msg_9fa3df979283:

> 대시보드 결정 응답: 1) 노트북 이전 - 시연용 DB를 SQL Express 직접 설치로 할지 → A Express 도구 만든다

- Core 리드만 Astra medium 시범을 계속한다. 구현자·검증자는 현행 배정, 선행 시험은 신규 claude-opus-5-5다.
- 승인 범위는 아래 a9842cbc 원문 전체다. 원문 안의 「승인 전」「현재 종료 작업」「다음 goal 미착수」는 초안 작성 당시 상태이며, 본 goal의 착수 권한은 위 새 승인 전달에서 온다.
- PR1은 현재 데스크톱에서, PR2는 사용자가 노트북에 간 뒤 노트북 Claude가 정확 버전의 실행 카드로 수행한다. PR1 통과·병합을 노트북 실증 통과로 표현하지 않는다.
- 혼합 인증 전환의 SQL 서비스 재시작 1회와 기존 연결의 잠깐 중단, 관리자 확인은 사용자가 정할 PR2 시점에 수행한다.
- 데스크톱 Windows SQL Express(D: VHDX) 및 dawnholder-sqlserver·judgment·judgment-r2는 조회·변경·삭제0. 비밀은 argv·env·로그·Git0, sa 활성화0, LAN 전체 개방0, 전역 설정 변경0.
- push 전 format-check 전체·code-rules Changed·module-boundaries 로컬 실행. 병합은 메인에서 정확 head 사용자 승인 뒤에만 한다.

### 관리자 초기화 연결 A — 메인 판단 msg_f865e434e6e7, 사용자에게 공개

메인이 connection-boundary.md를 읽고 2026-10-16T02:05:24Z에 승인 범위 안의 로컬 관리 경계로 채택했다. 사용자 직접 입력으로 격상하지 않는다. 사용자 공개는 메인이 맡는다.

> 같은 연결에서 실제 transport가 Shared memory인지 확인하고(예: 자기 세션의 net_transport), 다르면 그 자리에서 중단한다. TCP·Named Pipes fallback 0.
> 관리자 인증은 Windows 통합 인증만. sa·SQL 사용자/비밀번호 관리자 인증 0. 인스턴스 ForceEncryption 원래 값을 낮추지 않는다. 그것 때문에 막히면 우회하지 않고 보고한다.
> 이 연결로 한 일을 TLS 검증 성공으로 보고하지 않는다. TLS 검증 근거는 WSL runtime·Verify 경로(Mandatory·TSC=false·chain/name)에서만 낸다.
> 이 연결에서 읽는 것은 서버 identity, 정확한 신규 DB·login·자기 소유 메타데이터, 설치된 migration/catalog뿐이다. 기존 DB 목록·내용 읽기 0.

로컬 관리자 초기화는 lpc 고정·Integrated Security=true·Encrypt=false·TrustServerCertificate=false다. 위 조건은 실제 adapter와 독립 검증 계약에 원문으로 포함한다. 이 결정으로 데스크톱 SQL 접근이나 PR2 노트북 실행 시점 승인까지 받았다고 해석하지 않는다.

### 작업 방식 개선 A와 노트북 이주 우선

메인 전달 `msg_f4feb0b116b2`(2026-10-16T06:09:29Z)의 사용자 원문이다. 이 pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 작업 방식 개선 - 오늘 드러난 되돌이 원인을 언제 규칙에 넣을지 → C 코멘트 「A로 가면 좋은데, 일단 노트북 이주부터 빠르게」

메인 적용 판단: A를 채택하되 노트북 이주가 우선이며 이를 늦추지 않는다. 규칙 정본 변경은 10-28 뒤 Rules 「후보 출구」 목표로 넘긴다. 이후 새 구현·수정 계약은 작업자가 worker_done 전 바뀐 언어의 code-rules Changed와 서식 대상이 있는 format-check를 실행하고 원시를 남긴다. 리드 push 전 검사도 유지한다. 완료 보고에는 경로별 「실제 실행 / 대역 / 미실행」 표를 두며, 대역 경로를 실제 구현 경로 검사로 표현하지 않는다. 보존·동일성 증명은 계약에 기존 format-check 근거·파서 token 비교 등 쓸 도구를 지정하고 새 범용 harness를 만들지 않는다. 새 코드가 없는 PR2에 무관한 작업을 추가하거나 PR1을 다시 열지 않는다.

### 버전 대조 조회 명령 고정 — 메인 요청 msg_8b283e9f6b3e

- 정확한 승인 machine·instance·InstanceId·공개 출력 폴더 4입력만 받는 별도 pwsh 7 조회 카드다. 64비트 Setup Version과 같은 LPC/Windows 통합인증 연결의 Shared memory·machine/instance·sysadmin·ProductVersion을 읽고 JSON에 대상·UTC·exit를 남긴다. 두 값 불일치/미관측이면 Preflight 전에 중단한다. ForceEncryption 하향·TCP/NP fallback·기존 DB 목록/내용 조회·SQL/registry 쓰기0.
- 산출물: 데스크톱 `.backups/verification/2026-10-16-laptop-sql-express/pr2-prep/pr2-version-check.md`. SHA256 `56441CF97CAB1CA657AEDA1B0207C013E617C7A10A4F92F88AF68EF3B8897C1A`. 메인이 동일 hash로 USB `D:\Migration\pr2-version-check.md`에 복사했으며 노트북에서는 `E:\Migration\pr2-version-check.md`다(`msg_6024803a538e`).
- 신규 Astra 작성 후 신규 Opus 독립 검토. 판정 원문은 데스크톱 `.backups/verification/2026-10-16-laptop-sql-express/pr2-prep/verification-1/report.md`·`verdict.json`. 문서/오프라인 통과·차단0이며 강 실제 진입은 미실행·판정 보류다. 실제 SQL·registry·복사 블록 전체를 데스크톱에서 실행하지 않았다. 근거 폴더는 Git 밖이며 노트북에는 메인이 압축본으로 옮긴다.
- Low #1: 자식 pwsh 시작 실패 때 EXIT 줄이 오래된 LASTEXITCODE를 표시할 수 있다. 메인이 인계 8절에 **해당 실행의 새 JSON Status=Matched와 exit0 둘 다 확인, EXIT 줄 단독 판정 금지, 자식 시작 실패·JSON 부재면 중단**을 넣었다. 카드 bytes는 유지한다. InstanceId 매핑은 뒤 Preflight 검사를 유지하며 추가 registry 읽기를 이 카드에 넣지 않았다. U-1 실제 버전 두 원천 일치 여부는 노트북 원시 전까지 미확정이다.

## 재개 지점

- **다음 첫 일:** 노트북에서 USB `E:\Migration\00-노트북-에이전트-인계.md` 8절과 `E:\Migration\pr2-version-check.md`(SHA256 `56441CF97CAB1CA657AEDA1B0207C013E617C7A10A4F92F88AF68EF3B8897C1A`)를 읽는다. 조회 카드가 없거나 hash가 다르면 멈춘다. 복원·빌드·메모리 시연 뒤 승인된 실제 machine/instance, 기존 DB 보호 기준, 설정 백업/복구 범위, 관리자 확인·재시작 시간대를 사용자와 확정하고 PR2를 실행한다. 저장소 정본은 `99_Tools/database/windows-sql/README.md`다.
- 기록 시각: `2026-10-16T07:16:57Z` (`C:/Program Files/Git/usr/bin/date.exe -u '+%Y-%m-%dT%H:%M:%SZ'` 실제 출력). 메인 마감 지시 `msg_c0e097045dcb`: 사용자가 데스크톱 개발을 마감하고 앞으로 노트북에서 이어가기로 했다.
- 버전 대조에서 두 값·대상·UTC·exit를 기록한다. 불일치/미관측 또는 자식 실행 실패·새 JSON 부재면 Preflight 전에 중단한다. 성공을 EXIT 한 줄로 판단하지 않는다. 이후 카드 순서대로 Prepare/계획 승인/Preflight/Plan/Apply/Verify와 실제 저장·재접속·정상 종료를 관측한다. Cleanup은 별도 명시 승인이고 24시간 승인 만료 뒤 자동 Cleanup은 지원하지 않는다.
- 원시는 `E:\Migration\pr2-results\<UTC>\`에 명령·환경·exit·stdout/stderr·JSON·승인/hash·전후 설정/보존 근거·실제 실행/대역/미실행 구분과 함께 남긴다. 비밀·DPAPI 파일·개인키·PrivateRoot 전체는 옮기지 않는다. **노트북의 새 Core 리드가 원시를 받아 검증·기록**하며, 직접 실행하지 않은 일을 실행했다고 쓰지 않는다. PR2 결과와 함께 PR을 열고 정확 head의 사용자 병합 승인을 받는다.
- PR1 [PR246](https://github.com/bass131/dawnholder-server/pull/246)은 사용자 `병합 승인: PR246 head 28ccd0da4d32e4509ce36dae12fe5f58acfdff54` 뒤 메인이 `2026-10-16T06:16:22Z` 병합했다. 병합 commit `3261482ac75df3ed5ed9038b50d9d96935a0a445`(메인 통보 `msg_cfc56fdfab06`). PR1 완료는 PR2 실제 SQL/TLS/게임 성공이 아니다.
- 재개 branch `feat/laptop-sql-express-pr2-20261016`, base는 위 병합 commit. 노트북 새 리드는 origin에서 이 branch를 받아 이 goal부터 읽는다. 데스크톱 Codex 대화와 Run은 노트북으로 옮겨지지 않으며 새 세션의 역할·배치·현재 주소를 확인한다.
- **열린 Task·Dispatch 없음.** 모든 구현자·검증자는 정산·pane 종료했다. 이 데스크톱 lead handle `term_cccc865b-2522-43f8-836e-5251f9af803e`, Run `run_3683a9d0b02d`, 과거 Task·Dispatch는 기록일 뿐 실행 권한이 아니다. 과거 세션으로 지시하거나 작업자를 재사용하지 않는다.
- 데스크톱 근거 `.backups/verification/2026-10-16-laptop-sql-express/`는 Git 밖이다. **노트북에는 메인이 압축본으로 옮긴다.** 없으면 확인 불가로 보고하며 역사 원시를 새 실행 근거로 대체하지 않는다. git의 이 goal과 실행 카드, USB 인계·조회 카드로 다음 작업의 경계와 순서를 확인할 수 있다.

## 계획과 검증 경계

1. 신규 Opus가 요구사항 시험과 구현 전 실패 원시를 작성한다. DB 접속 없는 경계 시험을 먼저 수행하고 제품 파일은 쓰지 않는다.
2. 쓰기 종료·정산 후 구현 배정 규칙으로 신규 구현자를 연다. Windows 인스턴스 생애와 Docker 생애를 분리하고 migration/catalog·runtime 권한·DPAPI/CA 입력은 계약이 맞는 부분만 재사용한다.
3. 신규 독립 검증자가 diff·원시 실사, 독립 시험, 실제 도구 진입의 대역 범위를 검증한다. 설치/I/O/보안·보호 경계이므로 강 등급이다. Windows SQL·노트북 실증은 PR2까지 판정 보류로 명시한다.
4. PR1은 도구·시험·실행 카드만 전달한다. 노트북 사실/관리자 확인/중단 창·정확 신규 자원 이름·보존 근거를 실행 카드에서 확인한 뒤 PR2를 진행한다.
5. PR2 실제 실행 결과와 범위 안 결함 수정·재검증 뒤 전체 goal 종료 점검을 한다. 각 PR 병합은 별도 정확 head 승인이다.

R-7: 보호/실패 범주에 닿지만 이 목표의 한정 시범 지정은 전달받지 않았다. 기본 선행 시험·구현·독립 검증을 따른다. 카드 실증 회차0, 독립 검증이 놓친 실제 카드 결함은 아직 미관측이다. 변경 파일/줄 수는 구현 뒤 원시 diff로 기록한다.

## 승인 범위 원문

출처: `a9842cbc47036c4e747a13edae8b073aa82e7458:01_Phases/goals/2026-10-14-persistence-runtime-entry/goal.md`. 아래는 해당 절을 그대로 옮겼다.

## 다음 goal 범위 초안 — 노트북 SQL Express 시연 DB 도구

승인 전 후보이며 새 goal 착수가 아니다. 연결 후보는 BACKLOG `laptop-sql-express-demo-tool`이다. 아래 권고는 리드의 설계 초안이고 사용자 결정 A의 세부 승인으로 간주하지 않는다.

- **만들 것:** 이미 설치된 Windows SQLEXPRESS에 명시한 신규 시연 DB만 준비하는 도구와 노트북 실행 카드. 읽기 전용 사전 점검 → 변경 계획 표시 → 승인한 인스턴스 설정/시연 DB 준비 → 검증 → 소유 자원만 정리/설정 복구의 경계를 둔다. 재실행은 기존 DB를 초기화하지 않고 소유·버전 불일치에서 중단한다.
- **건드릴 곳:** `99_Tools/database/`의 Windows 준비 도구·시험·문서, 기존 SQL migration/catalog 재사용 접점, `99_Tools/Start-PersistenceServer.ps1`의 endpoint/CA/자격증명 입력 중 필요한 경계, 실행 안내와 새 goal. 정확 파일과 공용 코드 추출 범위는 착수 설계에서 확정한다.
- **하지 않을 것:** 기존 SQL 설치 재설치·업그레이드, VHD 도입, 기존 DB 데이터/스키마 변경·삭제, 게임 프로토콜/저장 규칙 변경, 데스크톱 보호 SQL 자원 접근, 전역 WSL 네트워크 모드 강제 전환, LAN 전체 개방, sa 활성화, 비밀의 Git/로그 기록. 현재 종료 작업에서는 노트북 SQL 접속·설정 변경0이다.
- **관찰 가능한 완료조건:** 도구의 충돌/소유권/재실행/실패 복구 시험, 기존 Docker 경로 회귀, 노트북에서 카드의 실제 진입 1회 이상과 전후 인스턴스 설정·기존 DB 보존 근거, 신규 시연 DB migration 및 WSL 서버의 TLS 검증 연결·저장/재접속·정상 종료 관측. 보호 DB 보존 확인 방식은 사용자와 노트북 담당자가 승인한 메타데이터/기존 백업 기준으로 정하며 기존 DB 내용을 읽거나 수정하는 시험을 임의 추가하지 않는다. 이 머신의 대역 시험만으로 노트북 통과를 선언하지 않는다.

### 연결 방식 권고와 확인할 분기

메인 전달 사전 정보는 DESKTOP-88BJKQH/bass1, MSSQL17.SQLEXPRESS 17.0.1000.7(SQL Server 2025 Express), Windows 인증만·TCP 꺼짐·포트 없음·ForceEncryption0·서버 인증서 없음이다. 물리 섹터4096/4096, WSL user의 SDK10.0.301(~/.dotnet), pwsh7.6.7, Unity6000.6.4f1도 메인 기록이며 여기서 재측정하지 않았다. 기존 인스턴스를 재사용하는 권고는 이 보고를 전제로 한다.

1. **TCP/포트:** 충돌 검사 후 고정 포트를 지정하고 WSL에서 접근 가능한 Windows endpoint를 실행 직전에 확인한다. NAT에서는 Windows host IP가 필요하고 mirrored에서는 localhost 연결이 가능하므로 현재 모드를 탐지한다. NAT 주소를 영구 상수로 두지 않으며, 선택한 주소/이름과 인증서 이름 검증의 일치를 먼저 설계한다. 기존 모드를 유지하고 방화벽은 해당 시연 포트·필요 WSL 범위만 사용자 승인으로 연다. [Microsoft WSL 네트워킹](https://learn.microsoft.com/en-us/windows/wsl/networking).
2. **TLS:** `Encrypt=Mandatory`·`TrustServerCertificate=false`를 유지한다. Windows SQL에 맞는 서버 인증서와 서비스 계정의 개인키 읽기 권한을 준비하고 SAN에 실제 클라이언트 연결 이름을 포함한다. 인증서는 SQL 요구사항(서버 인증 EKU·유효기간·AT_KEYEXCHANGE 등)을 점검한다. 현재 Linux 컨테이너 인증서 생성을 Windows에 그대로 적용하지 않는다. CA 신뢰는 PR242의 자식 프로세스 한정 경로를 우선 재사용하고, 인스턴스 전체 ForceEncryption 변경은 기본에 넣지 않는다. [Microsoft 인증서 요구사항](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/certificate-requirements?view=sql-server-ver17).
3. **인증/비밀:** Windows 관리자 인증으로 준비하고, 혼합 인증 전환 뒤 시연 DB 범위의 전용 runtime SQL 로그인을 만든다는 안을 추천한다. sa는 활성화하지 않는다. 인증 모드 변경은 인스턴스 재시작이 필요하므로 기존 DB 접속의 중단 영향을 사용자에게 먼저 알린다. DPAPI 비밀은 노트북 Windows 사용자로 새로 만들며 데스크톱 암호화 파일을 복사하지 않는다. [Microsoft 인증 모드 변경](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/change-server-authentication-mode?view=sql-server-ver17).
4. **공유/분리:** SQL migration/catalog·runtime 권한·TLS 연결 검증·런처의 stdin/자식 CA 전달은 공유 후보, Windows 서비스/레지스트리/인증서 저장소와 Docker 컨테이너 생애는 분리 후보다. 현행 LinuxSqlHost의 ContainerHostname 계약은 Windows 인스턴스 식별과 다르므로 억지로 재사용하지 않는다. 근거: `LinuxSqlHost/HostPlan.cs`, `SqlLifecycle.cs`, `Start-PersistenceServer.ps1`(PR242 병합 기준). 서버 저장 로직은 그대로 둔다.

### PR 경계와 사람이 할 일

추천은 새 goal 안에서 PR1 도구·실패/보호 시험·실행 카드, PR2 노트북 실제 실행 결과·필요한 범위 안 수정·최종 기록으로 나누는 것이다. PR1 승인·병합만으로 노트북 실증을 통과 처리하지 않는다. 제품 변경의 TDD·독립 검증과 실행 카드 실제 진입은 현행 규칙을 따르며, 카드 회차·놓친 결함을 새 goal에 기록한다. 이 종료 PR은 초안만 담는다.

노트북 Claude는 승인된 카드와 정확 버전을 받아 노트북에서 수행하고 원시·환경·exit·실행/미실행을 반환한다. 사용자는 인스턴스 설정 변경·인증서/개인키 권한·방화벽의 관리자 확인과 서비스 재시작 시간대를 승인한다. OS 재부팅이 필요하다고 실제 확인되면 사용자가 수행하며 무조건 재부팅하지 않는다. 실행 전 설정 백업·되돌리기 범위를 확인하고, 실패 시 기존 인스턴스를 자동 삭제하지 않는다. 이 머신 리드는 노트북 접근 권한이 없으므로 결과를 수신해 검증하며 실행을 대신했다고 보고하지 않는다.

새 리드 시작 전 사용자 결정은 이 범위/PR 분할, 인스턴스 수준 설정과 중단 창, Astra medium 시범의 계속·확대·원복이다. 현재 대화는 같은 goal 종료 기록까지만 허용된다.


## 단계 결과와 설계 결정

- 선행 시험 pretests-1 완료. 원문 `.backups/verification/2026-10-16-laptop-sql-express/pretests-1/report.md`; 제품 부재로 Plan 106/106 FAIL, Lifecycle 98/98 FAIL(의도한 RED), LinuxSqlBoundary 47 PASS. 구현에 대한 GREEN은 미확인이다.
- 기존 묶음은 pwsh7에서 346 PASS/343 FAIL/2 OBSERVED·결과 없는 suite6, PS5.1에서 1443 PASS/82 FAIL/9 OBSERVED. PS5.1의 82 FAIL은 기존 LINUX_SQL 안전 분류 목록 누락 후보이며 이번 제품 회귀로 섞거나 시험을 완화하지 않는다. 도구 제품 실행에 필요한 공유 코드 변경이면 영향 범위를 다시 검토한다.
- 선행 시험자의 런타임 홈 tool-results 출력1건과 heartbeat 초과는 공개했다(msg_aa622253482f). 상세/집계는 pretests-1/report.md와 liveness-summary-corrected.json. 최초 입력형식 오류 집계도 보존했다. 제품 통과·운영 준수로 표현하지 않는다.
- 시험 계약 §9의 실패 뒤 자동 복구 없음·소유 자원만 명시 Cleanup·차단 때 부분 정리0·ForceEncryption Preserve·승인 hash·닫힌 schema·adapter machine 확인·변경 전 journal을 채택한다. NAT 방화벽 /20 이하만 지원하는 제한은 카드에 공개하고 범위를 자동 확대하지 않는다. 이 값은 WSL의 보편적 사양이나 권장 subnet이 아니라 v1 지원 경계다.
- 실제 adapter·endpoint/TLS 이름·계정 바인딩·인증서 준비는 시험이 다루지 않는다. 판단 코어 다음 계약에서 이를 구현/실행 카드로 완성해야 PR1이 준비된다. 코어 GREEN을 실제 제품/노트북 통과로 표현하지 않는다.
- 실패한 Apply의 부분 상태 Cleanup은 각 성공 변경의 실제 사후값과 소유 receipt를 대조해야 한다. 전체 목표 설정만 비교해 복구를 영구 차단하는 구현은 허용하지 않는다. 상태 기록 자체 실패·중간 프로세스 종료·재실행·만료 후 복구 승인은 adapter와 독립 검증에서 별도로 다룬다.
- 구현은 신규 Astra xhigh(라우팅 신호1 입력/보안, 2 실패 수명, 4 새 다파일 설계), 독립 검증은 신규 Opus5.5다. 핵심 판단/수명과 실제 Windows I/O를 순차 분할하되 같은 PR1 범위를 유지한다.
- 메인 판단 msg_9448abe85e55: Claude 런타임 자동 tool-results 저장은 작업자 선택 쓰기가 아니므로 쓰기 경계 위반으로 세지 않고 이것만으로 차단하지 않는다. 해당 37965바이트 파일은 리드 정적 내용/비밀 패턴 확인에서 비밀값 미관측, 삭제0(근거 pretests-1/runtime-output-secret-check.json). 다음 검증 계약에 메인 판단 원문을 포함한다.

### 판단 코어 구현 결과

- 신규 Astra xhigh 구현1회 완료: windows-sql 하위 Common/Plan/Observation/Lifecycle/Cleanup 5파일, 945줄. 구현 자체점검 최종 Plan106 PASS, Lifecycle97 PASS/1 FAIL(CLI STRUCT-06 미구현), 부분 복구7 PASS. 기존 Linux 경계47 PASS. 원시 리드 대조는 core-implementation-1/lead-source-check.json, 작업자 원문은 report.md. 독립 검증·실제 SQL/TLS·노트북 실증은 미실행이다.
- 제품 초기 null 응답 결함과 harness 실행기/경로 오류는 원시 보존 후 수정됐다. 자체 수정이며 독립 확정 실패 집계가 아니다. liveness-final/gaps 원시와 초과 보고를 보존했다. 신규 Task/Dispatch/기동/정산/종료 receipt는 같은 근거 폴더에 있다.
- 다음 단계의 관리자 로컬 접속은 기존 Mandatory 문구와 경계를 명시해야 한다. 추천은 lpc 고정 Windows 통합 인증의 로컬 초기화만 Encrypt=false/TSC=false, runtime은 Mandatory/TSC=false 유지다. 이는 아직 채택/실행한 예외가 아니다. 메인 요청 msg_cdd6370d7730, 구체 제안 windows-adapter-design/connection-boundary.md. 의존 adapter 발행만 판정 전 보류한다.
- adapter 구현의 자체 수정 3회 초과 체크포인트: 메인 계속 판단 msg_6a2446fb44a1. 같은 산출물 수정 두 번 추가(누적 6회) 또는 2026-10-16 13:30 KST까지 worker_done 부재 시 원시 보존·정산 후 반복 실패 이유를 설계 판단으로 메인에 보고한다. 독립 확정 실패 3회 집계와 구분하며, 완료 뒤 리드 원시·diff 대조 → 신규 Opus 독립 검증을 유지한다.

### Windows adapter 구현 결과와 검증 이관

- task_10b077014450 / ctx_65494d7dee77 구현 완료·쓰기 종료. 보고 adapter-implementation-1/report.md. 리드 원시 대조 lead-source-check.json: Plan106/Lifecycle98/Linux47·own32 PASS, host 최종 빌드 로그 경고0/오류0. 독립 판정과 실제 노트북 SQL/TLS·저장/복원은 미실행. 작업자 release 뒤 동일 incarnation idle을 확인해 pane을 닫았다.
- 앞선 관리자 접속 제안의 보류는 메인 msg_f865e434e6e7로 해제됐다. 정확 lpc/Windows 통합 인증, 같은 연결 Shared memory 확인, Encrypt=false/TSC=false 로컬 관리만 허용한다. ForceEncryption 하향·TCP/NP fallback·sa·기존 DB 목록/내용 조회0. WSL runtime/Verify는 Mandatory/TSC=false chain/name 유지하며 로컬 관리를 TLS 성공으로 보고하지 않는다. 원문 adapter-implementation-1/contract.md와 inbox-20261016-16.json.


- 독립 검증1 task_3fb4165eb226/ctx_5fb661260e37: 차단 #1(구현 task_10b077014450), 연결 문자열 속성 대입 예외. 같은 결함 확정 실패1회(2 FAIL assertions를 2회로 세지 않음). 기존251 PASS, 신규154 PASS/2 FAIL/4 OBSERVED. 원문 verification-1/report.md·verdict.json. 구현 보고와 실제 연결 경로 불일치를 메인 msg_59464cba2002로 즉시 보고했다. 신규 구현자 수정 뒤 신규 독립 검증. #2 Apply 전 인증서 정리 카드·O-1 잔여 DPAPI 설명을 함께 보완하고 U-1 버전 이중 원천은 PR2 읽기 전용 실측 항목으로 남긴다. 검증자 쓰기 종료·release·동일 pane idle 확인 후 종료했다.

- adapter 보고의 연결 검사 주장 불일치, 검증 #1로 드러남. 메인 msg_6b85d1c44d91에 따라 대역 경로는 미실행(대역)으로 별도 보고한다. repair-1 task_c1b8836d1a71/ctx_cca1809e8f79는 Sql 인덱서·카드 두 파일 수정 후 쓰기 종료했고 시험 자체178행 예외로 failed 정산했다. 제품 #1 확정 실패 추가0. U-1은 실측/공식 확정 근거 없이 검사 기준을 변경하지 않고 카드에 Preflight 전 두 버전 읽기 전용 기록·불일치 중단을 명시했다. #2/O-1은 소유 증거가 필요한 자동 정리 확대 대신 수동 재승인 안내로 처리했다. 신규 Opus는 메인 msg_3a17b651940f 조건대로 교정 시험의 수정 전 FAIL/수정 후 PASS를 모두 보이고 대역 보고를 대조한다.

- verification-2 범위 통과: 동일 교정 시험 전154/2/4→후156/0/4, Plan106/Lifecycle98. 계약E는 module-boundaries 위반0, code-rules 들여쓰기67건 및 C#서식4건 실패. 공백 교정용 새 harness 과잉으로 4회 실패, 첫 발생(style-repair-1, 제품·시험 변경0). 메인 msg_941976c9e08a에 따라 다음 회차는 수동 공백 교정과 기존 검사만 사용하고 다시 막히면 재시도 없이 메인에 올린다. 제품 확정 실패 추가0.

## PR1 최종 검증과 남은 경계

- style-verification-1/report.md·verdict.json: 신규 Opus task_44a8ef0ea4ec/ctx_18add06a2d1a 통과, 차단0. 기능 Plan106/Lifecycle98/Adapter156 PASS·4 OBSERVED. code-rules Changed20대상 위반0, 전체 format-check exit0/보존421파일/Formatting.Tests244, module-boundaries clean0. 현재 미커밋 bytes를 검증했고 검증자는 쓰기 종료·release·pane 종료했다.
- 검증자 자체 T-1 대조 필터 오류는 보존 원시 별도 대조로 확인했고 기존 검사/시험은 모두 exit0. L-1 heartbeat13분20초 간격 및 L-2 WSL 상태 보고 정정을 메인에 공개했다. 서식 O-1 연속식 가독성은 비차단 관찰로 남기며 정책/기능을 이번 PR에서 넓히지 않는다.
- PR1 실행 카드는 도구와 오프라인/대역 검증 범위에서 확정했다. 실제 SQL/LPC/TLS·인증서·서비스·방화벽·DPAPI·노트북 저장/복원은 PR2 미실행이다. U-1 버전 두 원천은 Preflight 전에 읽기 전용 기록·불일치 중단한다. O-6 정확 읽기 명령은 PR2 실행 계약에서 고정하고 O-7 Cleanup 오류코드 조건은 비차단 문구 관찰로 유지한다.
- 모든 신규 코드의 첫 커밋은 최종 서식을 포함하므로 별도 공백 전용 commit은 만들지 않는다. 기존 역사 commit의 blame을 숨길 대상이 없어 .git-blame-ignore-revs 추가는 해당 없다.

- PR1: [PR246 - 노트북 SQL Express 시연 도구와 보호 시험](https://github.com/bass131/dawnholder-server/pull/246), 최초 제품 commit319c19de262cc0e1f9120a282eb38989e9c059b7. 병합은 메인이 정확 head 사용자 승인 후 완료했으며 시각·병합 SHA는 위 재개 지점에 기록했다.

## 다음 goal 첫 기록 후보 / 종료 Gardener 입력

메인 `msg_6024803a538e`의 집계: 오늘 builder 속성 대입 문제는 **3회**다(Windows adapter 제품, verification-1 시험, PR2 버전 조회 검증 harness). 후자의 원시는 `pr2-prep/verification-1/offline-1~3.*`이며 harness 실패와 제품 확정 실패 집계를 혼동하지 않는다. 목표 종료 Gardener에서 기존 BACKLOG `powershell-dotnet-boundary-checklist`에 이 발생 근거·3회를 연결한다. `prepush-ci-format-checks`는 PR246이 네 번째 발생이라는 메인 전달과, `verification-execution-claim-check`는 adapter 대역 경로를 실제 검사로 보고한 근거를 함께 대조한다(`msg_f4feb0b116b2`). 새 목표 착수나 새 규칙 도입이 아니며 이번 마감에서는 BACKLOG를 수정하지 않는다.
