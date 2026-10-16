# 노트북 SQL Express 시연 DB 도구

상태: PR1(PR246) 병합 완료. 노트북 PR2 단계 1 실제 조회에서 버전 불일치로 Preflight 전에 중단했다. 원시를 보존했으며 후속 조사·수정 계약이 필요하다. PR1 오프라인 검증을 노트북 실증 성공으로 표현하지 않는다.

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

### 노트북 PR2 실행 주체 — 메인 전달 msg_60f74e4e5818

메인 pane에 Enter로 제출된 결정을 메인이 2026-10-16T09:02:59Z 전달했다. 이 Core pane의 직접 사용자 입력으로 격상하지 않는다. 원문은 `.backups/verification/2026-10-16-laptop-sql-express/laptop-entry-inbox.json`에 보존한다.

> 대시보드 결정 응답: 1) 노트북 SQL Express 2단계 - 실행 카드를 누가 돌릴지 → A Claude 세션 실행

- 메인이 노트북에 보조 Claude 실행 세션을 열고, 새 Core 리드는 원시를 검토·기록한다. Core 리드는 SQL·레지스트리·인증서·방화벽·서비스 변경 명령을 실행하지 않는다.
- Unity 첫 실행·메모리 모드 시연 뒤 카드 실행을 시작한다. 실행 계약 초안은 먼저 준비한다. 실제 대상·보호 기준·관리자 확인·파일/hash·서비스 중단 창 승인은 카드의 각 관문을 유지한다.
- 같은 노트북에서 결과를 전달하므로 USB 경유는 불필요하다는 메인 판단을 적용한다. 현재 `.backups` junction은 카드의 입력/출력 경로로 사용할 수 없어, 링크 없는 실행 경로를 계약 발행 전에 확정한다. junction은 이동·삭제하지 않는다.
- Astra medium 시범 계속과 PR2 끝 평가, 작업 방식 개선 C 코멘트, 나머지 파트 보류를 유지한다. 도구 결함은 실행 세션이 고치지 않고 원시와 함께 반환하며, 새 Core 리드가 일반 수정·독립 검증 절차를 조정한다.

### PR2 실행 전용 배치 — 메인 판단 msg_fff55ed77460

- 메인이 2026-10-16T09:12:02Z 실행 SourceRoot·cwd를 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-parallel`로 확정했다. 사용자 결정이 아닌 메인 배치 판단이다. 도구 기준은 detached HEAD `7ac2a01ff98bfa5656251c70b582c6e9143aa26f`다.
- 카드의 `.backups/windows-sql/` 경로를 그대로 쓰며 실행 세션이 정상 디렉터리로 만든다. `core-active/.backups` junction은 보존한다. 리드 v1의 `99_Tools` 아래 중첩 출력안은 해당 트리의 WSL 동기화에 출력이 섞이고 경로 치환 오류가 생길 수 있어 채택하지 않았다.
- 메인이 신규 `claude-opus-5-5`·`[노트북 실행 Opus]` 세션을 열고 카드 한정 쓰기 권한을 발행 계약에 명시한다. handle·incarnation과 실행자 자신의 경로 조상 검사는 발행·실행 전에 확인한다. Unity 첫 실행·메모리 모드 시연은 아직 미완료이며 메인 확인 뒤 발행한다.
- 계약 v1은 보존하고 `.backups/verification/2026-10-16-laptop-sql-express/execution-contract-laptop-pr2-draft-v2.md`에 배치를 반영했다. 사후 공개 결과 보존본은 `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-16-laptop-sql-express/pr2-results/<UTC>`에 둔다. 실제 카드 실행·독립 판정은 미실행이다.

### PR2 실행 계약 발행 — 메인 통보 msg_67a979cc857c

- 메인은 2026-10-16T09:54:48Z `core-parallel`의 신규 `[노트북 실행 Opus]` 세션에 계약을 발행했다고 통보했다. 발행본은 `.backups/verification/2026-10-16-laptop-sql-express/execution-contract-laptop-pr2-issued.md`다. 리드는 파일 hash 일치와 현재 실행 세션의 경로·incarnation을 대조했으며 상세는 `lead-context-laptop-pr2.md`에 둔다. Task·Dispatch 없는 메인 보조 세션이다.
- v2보다 발행본 덧붙임이 우선한다. 카드 한정 쓰기·단계별 원시 보고를 적용한다. 발행 당시의 실행 pane 직접 승인 한정은 아래 `msg_3368df03ed2e` 정정에 따라 단계 2부터 메인 경유 사용자 확인으로 바뀐다. 실행자는 비밀 없는 파일 목록과 원시 경로를 보내고, 검토 보존본 복사는 Core 리드가 한다. 전체 PrivateRoot·비밀은 복사하지 않는다.
- Unity 첫 실행·메모리 시연·서버 정상 종료 완료와 PR2 전체 실행 창은 메인 관측·사용자 원문 전달로 접수했다. 원문은 「OK 일단 이상은 없어」, 「그래 1단계 뿐 만 아니라 전부 마무리 해보자」다. 이는 실행 pane의 단계별 승인이나 실행 직전 자원 관문을 대신하지 않는다.
- 현재는 실행 세션의 단계별 원시를 기다린다. 실제 SQL/TLS/게임 실증 성공·원시 검토·독립 판정은 아직 확인하지 않았다. 실행 종료 뒤 신규 독립 검증을 조정하며 알림 없이 대기·반복 조회하지 않는다.

### 사용자 확인 경로 정정 — msg_3368df03ed2e

메인이 2026-10-16T10:11:49Z 전달한 사용자 원문(메인 pane Enter 제출)이다. 실행 pane 직접 입력으로 격상하지 않는다.

> 보통 메인 세션통해서 이야기 하지 않았나? 이번에 노트북으로 환경 옮기면서 그런 맥락이 전부 없어졌나

- 메인은 발행본 덧붙임 3의 직접 입력 한정이 자신의 설계였음을 밝히고 **단계 2부터 메인 경유 확인**으로 정정했다. 단계 1에서 이미 받은 직접 승인은 그대로 유효하다는 통보다. 실제 승인 원시 검토와 이 통보를 구분하며 기존 발행본은 보존한다.
- 실행자는 확인 지점마다 단계 이름, 사용자에게 보일 파일·명령 전문의 경로/SHA256, 안건별 최대 두 선택지·선택 결과·추천·중단/재시작/관리자 확인 영향을 메인에 보낸다. 메인이 사용자에게 묻고 제출 원문과 메인 메시지 ID를 전달한다.
- 승인 기록은 「메인 전달 사용자 원문」으로 남기며 실행 pane 직접 입력으로 바꿔 적지 않는다. 사용자가 실행 pane에 직접 제출한 승인도 유효하다. UAC 필요 명령은 직전 메인에 알리고 실제 창은 사용자가 직접 누른다.
- 단계 1의 보호할 DB 유무 답은 메인이 사용자에게 받아 단계 2 확인 전에 전달한다. 나머지 계약·파일/hash 관문·실행 경계는 유지한다. 정정 원문은 `.backups/verification/2026-10-16-laptop-sql-express/laptop-followup-check-3.json`에 보존한다.

### 기존 DB 보호 기준 확정 — 메인 전달 msg_29f42287a999

2026-10-16T10:19:14Z 메인이 전달한 사용자 원문(메인 pane Enter 제출)이다. 실행 pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 노트북 SQL Express 2단계 - 지켜야 할 기존 DB가 있는지 → A 없음·중요하지 않음

- 사용자가 본 A 설명은 「기존 DB 보호는 SQL 데이터 폴더의 기존 .mdf·.ldf 파일 이름과 크기를 실행 전후로 비교하는 것으로 한다. 따로 백업하지 않고 다음 단계로 간다.」다. 중요한 DB를 먼저 백업하는 B는 선택하지 않았다.
- 이전 실행자의 `user-approval.md`에서 안건 2의 추천 A를 포함한다고 해석한 부분은 이 후속 원문으로 확정한다. 당시 미답·해석 기록을 소급 변경하지 않는다. 다음 실행 발행본의 단계 1·2에 이 원문과 메시지 ID를 붙인다.
- 이 선택은 기존 DB 목록·내용 조회나 변경·삭제 권한을 추가하지 않는다. 파일 이름·크기 비교와 실제 DB 내용 보존 검증을 구분한다. 버전 수정 루프와 나머지 승인·실행 관문은 유지한다.

### PSScriptAnalyzer 프로젝트 저장 승인과 실행 실패

메인 `msg_63dad403f0fc`(2026-10-16T11:15:00Z)가 전달한 사용자 원문이다.

> 대시보드 결정 응답: 1) 노트북 SQL Express 2단계 - PowerShell 규칙 검사 도구를 노트북에 받을지 → A 프로젝트 폴더에만 받기

- 승인 범위는 CI와 같은 `Save-Module -Name PSScriptAnalyzer -RequiredVersion 1.25.0 -Repository PSGallery`를 지정 물리 경로 `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-02-agent-rule-context/tool-cache/modules`에 한 번 실행하는 것이다. 전역 설치·신뢰 설정·PATH·실행 정책 변경은 하지 않는다.
- 리드 수집 스크립트 실행은 exit1이었다. 여러 출력 스트림을 같은 `stdout.txt`에 직접 리다이렉트하면서 파일 잠금 충돌이 발생했다. PSGallery 다운로드 실패로 단정하지 않으며 manifest 부재·hash 미생성이다. 원시는 `version-fix/pssa-download/`의 명령·스크립트·stdout/stderr·result.json에 보존했다.
- 실패 시 보고하라는 계약에 따라 재실행·출처 변경 없이 메인에 알린다. 이 수집 오류는 제품 VF-1 실패와 구분한다. code-rules PowerShell 검사는 계속 미실행이며 구현 작업의 오프라인 시험은 이어간다.
- 이후 메인 판단 `msg_0d8a4925e26d`가 수집 오류 교정 후 같은 명령의 재실행 1회를 허용했다. 스트림별 파일을 분리한 실행은 2026-10-16T11:17:37Z~11:17:51Z exit0으로 끝났고 manifest의 ModuleVersion=1.25.0과 SHA256을 확인했다. 원시는 `version-fix/pssa-download/retry-1/result.json`·`manifest-check.json`에 보존하며 첫 실패 원시도 유지한다. 전역 설치·설정 변경은 하지 않았다.
- 구현자에게 `msg_568b600dcca2`로 실제 manifest 경로/hash를 전달하여 기존 계약 단계 4의 미정 입력을 확정했다. 다운로드 성공은 code-rules 통과가 아니며 검사 결과는 후속 원시로 확인한다.

### 최초 범위 승인

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

- **다음 첫 일:** 버전 결함 VF-1의 신규 Astra 구현 결과를 받아 diff·GREEN·쓰기 종료를 대조하고 신규 Opus 강 독립 검증에 넘긴다. 메인 `msg_f4ac87fe9763`의 범위 안 수정 요청에 따른 계약은 `.backups/verification/2026-10-16-laptop-sql-express/version-fix/pretests-contract-v1.md`, 선행 Task/Dispatch는 같은 폴더 `pretests-dispatch.json`, 현재 구현은 `implementation-dispatch.json`에 있다. 구현 기동의 자동 제출 미관측을 R-5 Enter 1회로 복구했고 실제 Working을 확인했으나 transcript clipping으로 제출 전문 대조는 미완료여서 메인에 보고했다. 실제 실행은 중단 상태이며 추가 SQL/레지스트리 조회·Preflight 진행은 하지 않는다. 재실행은 수정 commit·새 실행 세션·새 발행본을 사용한다. 아래 USB·데스크톱 주소는 이전 인계 기록이며 현재 실행 권한이 아니다.
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

### PR2 단계 1 — 버전 불일치로 중단

- 실행자 보고 `msg_79683f39bded`와 새 JSON을 리드가 대조했다. 2026-10-16T10:11:23Z 실제 조회에서 Registry64 `Setup/Version=17.0.1000.7`, SQL `ProductVersion=17.0.1145.4`, `Status=Failed`, `ExitCode=1`, `ErrorCode=VERSION_MISMATCH`다. 부모 pwsh exit0은 조회 통과가 아니며 자식 exit 줄은 1이다.
- 같은 SQL 연결의 `Shared memory`, 정확 machine/instance, sysadmin1은 JSON에서 확인했다. 이는 관리 조회 경계의 근거이며 TLS/게임 성공이 아니다. 조회 카드와 실행 파일은 첫 네 입력 뒤 본문이 동일함을 대조했고, 공개 원시 9파일의 원본/보존본 SHA256이 모두 일치했다. 근거는 위 원시 폴더의 `copy-manifest.json`이다.
- 실행자는 Preflight 전 중단·추가 조회/재실행/제품 수정/설정 변경 없음으로 보고했다. 리드는 원시를 읽었고 SQL에 직접 접속하지 않았다. 패치로 두 값이 달라졌다는 설명은 아직 추론이며 확인 사실로 쓰지 않는다. 후속 설치/Prepare/Apply/Verify/게임 단계와 신규 독립 판정은 미실행이다.
- `user-approval.md`의 직접 원문은 「승인」이며 여러 안건 추천 전체 승인이라는 부분은 실행자 해석이다. 보호할 DB 유무 답은 부재로 기록되어 있으므로 단계 2 전 메인 경유 답을 받아야 한다. 현재 보고를 보호 기준 확정으로 격상하지 않는다.

### VF-1 버전 원천 계약 수정 착수

- 선행 시험 완료 보고 `msg_934d754fb5f2`와 원시를 대조했다. 새 suite 95 PASS/66 의도한 RED/4 OBSERVED, 기존 adapter 152 PASS/0 FAIL이며 제품 PASS가 아니다. 보고·시험 hash·정식 결과는 `version-fix/pretests-1/report.md` 및 `raw/run-20261016T110010Z/`에 있다. 작업자 정산·release 뒤 정확 pane 종료를 확인했다.
- 남은 검사 환경: PSScriptAnalyzer 1.25.0 manifest 부재로 code-rules PowerShell 검사는 환경 실패, format-check는 해당 선행 작업에서 C# 변경 없음·WSL 미승인으로 미실행이다. 리드는 메인에 환경 경로 확인을 요청했다. 생존 신호 초과 4구간과 사유 미확정은 `version-fix/pretests-liveness-complete.json`에 보존한다.
- 신규 Astra 구현 계약은 `version-fix/implementation-contract-v1.md`다. Instance 버전 검사·README·새 `windows-sql/version-check.md`만 고치고 선행 시험은 고정 입력으로 유지한다. 기존 USB 카드는 보존하며 새 조회 카드를 수정 commit에 포함하여 이후 새 실행 세션에 발행한다.
- 선행 시험 질문 `msg_5466aa64781a`에 리드가 `msg_20ebf34b20fc`로 조회 카드 **네 입력 유지**를 확정했다. 카드에서는 PatchLevel과 같은 LPC 연결의 ProductVersion을 조사·대조하고, 일치한 실제 값을 이후 plan 승인으로 고정한다. 제품은 승인 ExpectedProductVersion까지 대조한다. 카드의 다섯째 승인 버전 인자·적응형 미판정은 허용하지 않는다. 원계약을 보존한 보충은 `version-fix/pretests-clarification-1.md`이며 남은 선행 시험과 다음 구현·검증 계약에 적용한다.
- 메인 `msg_f4ac87fe9763`가 완료조건을 막는 범위 안 결함으로 수정 루프를 요청했다. 리드는 제품의 `WindowsSql.Instance.ps1`과 `WindowsSql.Sql.ps1`, 고정 조회 카드의 동일 비교 경계를 확인하고 계획 `msg_322185baa670`을 먼저 보냈다.
- 설계는 승인 `ExpectedProductVersion`과 정확 Registry64 `Setup/PatchLevel` 및 같은 LPC 연결의 SQL `ProductVersion`을 대조한다. `Setup/Version`은 설치 메타데이터로 구분하며 PatchLevel 부재·형식오류·불일치에 Version fallback은 허용하지 않는다. schema·정확 identity·보안/TLS·소유 경계는 유지한다. 메인 관측의 PatchLevel=17.0.1145.4와 업데이트 원인 추론은 리드 실제 조회로 격상하지 않는다.
- 신규 Opus 선행 시험을 발행했고 턴 시작 receipt를 확인했다. 다음은 신규 Astra xhigh 구현(입력 및 PS/.NET 경계 라우팅 신호 1·3), 신규 Opus 강 독립 검증이다. 실제 SQL 재실행은 이 오프라인 검증과 구분한다. 계약·모델/신원·원시는 `version-fix/`에 둔다. 아직 제품 수정·독립 PASS는 없다.
- 실행 세션 정산 보고 `msg_a3c6f5426572`는 단계 1 중단, 후속 단계 미실행·pending 없음·설정 변경 없음이며 미답 보호 DB 질문을 이관했다. 세션 종료는 메인 소유이며 재사용하지 않는다. 이번 원시는 위 9개 공개 파일을 명시 복사한 범위로 보존했다.
### PR1 선행 시험과 초기 결정


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

### PR2 버전 수정과 CR-1 후속

- VF-1 제품 구현 쓰기 종료 및 정산·pane 종료. 보고 `version-fix/implementation-1/report.md`: 버전161/0·adapter152/0·plan106/0·lifecycle98/0 자체점검이며 독립 PASS가 아니다. Changed는 선행시험 들여쓰기57건 CR-1로 exit1, 전체 통과 보류다.
- 메인 `msg_3a101e62cfd7`는 이미 승인된 범위의 CR-1 수정 → 신규 Opus 강 독립 검증 → push 전 전체 format-check/Changed/module-boundaries → 새 카드hash·재발행 초안까지 계속하라고 요청했다. 구현 정산 뒤 후속 발행을 멈춘 공백이 있었으며 별도 사용자 결정을 기다릴 사유는 없었다.
- 신규 Opus 테스트 소유 세션에 CR-1 공백만 발행했고 턴 시작을 확인했다. 계약 `version-fix/cr1-fix-contract-v1.md`, receipt `version-fix/cr1-start.json`. 기존 formatter 또는 손 수정·token 동일 대조로 좁히며 기대값·hash 대상·제품은 불변이다. 파일 자체 bytes hash 변화는 별도 기록한다. 이 완료 뒤 새 Opus가 강 독립 판정을 수행한다. 실제 SQL/registry/WSL/Unity 실증은 아직 미실행이다.
- CR-1 수정 완료·정산·종료. 리드가 보고/Compare/Changed 원시를 대조했으며 57행 선행공백만 변경·9849 token 동일·Changed2대상 진단0이다. 자체점검 원문 `version-fix/cr1-fix-1/report.md`. 신규 Opus 강 검증의 접수/턴 시작을 확인했고 계약과 receipt는 `version-fix/verification-contract-v1.md`, `version-fix/verification-start.json`이다. 다음 첫 일은 독립 판정 처리 후 push 전 전체 검사와 새 카드 재발행 초안이다.
- VF-1/CR-1 독립 강 검증은 오프라인 통과·차단0이다. 원문 `version-fix/verification-1/report.md`/`verdict.json`: 신규시험116 PASS, 기존4묶음517 PASS, 실패0. 원문과 주요 원시를 리드·메인이 대조했다. 실제 SQL/registry/TLS/DB/게임은 미실행이다. 비차단 O-1(plan진단)/O-3(주석탐색)/O-7(시험실행기결합)과 CI등록은 후속 후보, O-5검출공백은 신규시험으로 보완, O-6wrapper한계는 새 발행안내에 강조했다.
- push 전 Changed3대상 진단0. Windows format 진입점은 필수SDK10.0.301 부재로 시작단계 실패(원시 `version-fix/prepush-1/format/sdk.log`). module 첫 관문은 commit여유4GiB 미달로 실행보류. 메인 `msg_ab0dca643372`가 Unity프로세스4개 중 실제 Editor1개가 메인 소유임을 확인했다. 남의 프로세스종료0.
- 메인 `msg_18e9f313a766`에 따라 SDK를 설치하지 않고 로컬 commit 후 새 Core전용 WSL Git clone에서 정본 Linux format-check.sh와 module-boundaries를 실행한다. push는 전체검사 후다. Windows실패원시는 보존하며 Linux진입점으로 대체한다. 새 카드hash와 사용자보호DB/메인경유승인/childexit+새JSON 조건을 담은 v3초안은 메인이 대조했고 새commit만 확정하면 발행한다.
- 생존 신호 정산은 `version-fix/closeout-all-inbox.json`/`closeout-all-liveness.json`. 구현자350·313초, 검증자620·376·424초 초과가 있었고 정확 원인은 보고에서 명시하지 않아 미확정이다. 시험수정자는 초과0. 최초 terminal-only inbox집계는 dispatch없음 입력오류로 별도 보존했다.
- 메인 `msg_80ce00333c66`(2026-10-16T12:55:12Z)이 전달한 사용자 원문: 「유니티 닫았어, 이어서 진행해」. 메인 전달을 실행 pane 직접 입력으로 격상하지 않는다. 사용자 종료 후 리드는 Editor0·물리24.22GB·commit22.10GB를 재관측했고 전용 WSL clone의 로컬commit `6be5e2c054eb7af15fe518e12cb03a9bf39efba1`에서 Linux 전체 서식검사를 시작했다. Unity는 게임 단계 직전 메인이 다시 연다. 원시 `version-fix/prepush-linux-1/`, Windows실패·메모리실패는 그대로 보존했다. 이 행은 검사 대상 코드와 별도의 이후 운영 기록이다.
- Linux 전체 format exit0(필수244/244·실제Debug/Release421파일 보존), Changed3대상/위반0/exit0, module-boundaries completed/clean/위반0/exit0. 원시 `version-fix/prepush-linux-1/` 및 `version-fix/prepush-1/code-rules/`. module 첫 출력경로의 RUNNER_TEMP 경계 실패는 보존하고 같은 작업 폴더 안으로 실행경로를 정정해 통과했다. 제품·가드 수정은 없다.
- 코드 commit `6be5e2c054eb7af15fe518e12cb03a9bf39efba1`을 push했고 원격 head 일치를 확인했다. 이후 이 두 운영 기록 행은 로컬 goal에 남아 다음 기록 commit으로 묶는다. 새 실행 세션과 core-parallel checkout 동기화·발행은 메인 소유다. 실제 SQL 조회·TLS·저장/재접속은 아직 미실행이며 PR2 완료/병합 준비가 아니다.
- 메인 `msg_24925b81eebe`가 재실행 발행본 v3와 새 실행세션을 통보했다. 발행본 `execution-contract-laptop-pr2-issued-v3.md` 전문/hash를 리드가 확인했다. core-parallel의 source는6be5e2c0이며 메인이 clean/hash/reparse를 확인했다고 전달했다. 단계1부터 새카드/네입력 승인은 메인경유, 기존DB보호 기준 재질문0, Unity는 단계7직전 메인이 재개한다. 리드는 단계별 공개원시 실사·보존·goal기록과 실행종료후 독립검증을 맡는다. 새 실행 실적은 아직 수신하지 않았다.
- 메인 `msg_7eb730588514`(2026-10-16T13:49:15Z) 전달 사용자 원문: 「대시보드 결정 응답: 1) 노트북 SQL Express 2단계 - 단계 1 버전 조회를 실행할지 → A 승인」. 메인 전달 사용자 승인으로 기록하며 직접 입력으로 격상하지 않는다. 승인 대상은 DESKTOP-88BJKQH/SQLEXPRESS/MSSQL17.SQLEXPRESS와 새 출력 `core-parallel/.backups/windows-sql/version-check/20261016T134137Z`, approval-request.md `6407BC04…EF7B` 및 version-check-run.ps1 `ECFFCFED…BE2A`다. 실행자는 직전 hash대조 뒤 읽기전용조회1회만 수행하고 실패시중단·성공값은 단계5계획후보로만 쓴다. 승인 접수이며 실제 결과는 아직 수신하지 않았다.

- 단계1 실제 조회 Matched(msg_945923b903f8). 리드가 새JSON/hash·Requested·UTC실행창·자식stdout/exit0·stderr0·승인스크립트 입력4줄 외 동일을 대조했다. Registry64 Setup/PatchLevel과 SQL ProductVersion 모두17.0.1145.4, Setup/Version17.0.1000.7은 메타데이터다. 공개9파일의 비밀패턴0·복사hash동일 근거는 pr2-results/20261016T134137Z/01-version-check/lead-source-check.json. 값은 단계5 plan 후보이며 적용승인 아님. 단계2 읽기전용 facts 준비 중, 실제 TLS·DB·게임저장/재접속 및 최종독립검증은 미완료다.

- 단계2 읽기전용 관측에서 SQLAgent$SQLEXPRESS 의존서비스(Stopped/Disabled)가 확인돼 발행계약에 따라 중단했다(msg_6661d79868b5). 설정·서비스·DB·인증서·방화벽 변경0 보고. 원시3파일 보존/대조는 pr2-results/20261016T134137Z/02-facts/lead-source-check.json. DS-1: RestartService에서만 dependent Count를 검사하여 선행 registry 변경 후 실패할 가능성(소스 실사, 실제 Apply 재현0·독립 확정실패0). 사전차단 보강과 비활성 Agent 지원예외 판단을 메인에 보고, 우회0·실행중단유지. 보호 DB 파일 목록은 비승격 권한부족으로 미관측, WSL 실제모드 미관측이며 facts의 한글 일부는 인코딩 손실로 보인다.

- 메인 범위판단 msg_87d420e8d4fb/msg_b5227e44a175: DS-1 사전차단과 모든 의존서비스 Stopped+Disabled일 때 허용·재시작직전 재검사는 이번 PR2 범위 안이다(사용자 직접결정 아님). 같은 산출물 수정2회, 독립확정실패0. 신규 Opus 선행시험→Astra xhigh구현→신규 Opus강검증→Linux검사셋→push→v4초안으로 진행한다. v4에 보호DB파일명/크기를 승격창에서 전후관측하고 WSL실제모드를 확인하는 단계를 명시한다. 실행세션 정산은 메인소유. 예상2~3시간은 계획값이며 실측 아님.

- 신규 선행시험 task_1318d91e0013/ctx_88799784ae2a 기동·턴시작 확인. 계약/receipt는 dependent-service-fix/pretests-contract-v1.md 및 pretests-start.json. Opus5.5 xhigh화면/backend unknown. 실행v3 정산메모와 첫수집실패 원시는 pr2-results/20261016T134137Z/closeout-copy-manifest.json에 보존했다. 단계3~7/Cleanup 미실행·대역0이며 SID/IP 포함 원시는 로컬 Git제외 보존만 한다.

- 메인 전달 사용자 결정 `msg_b3a379301c66`(2026-10-16T14:08:00Z): 원문 「A로 가자」. 질문은 「노트북 SQL Express 2단계 - 실제 SQL 작업을 오늘 새벽에 할지 내일 할지」, 선택한 A 설명은 「도구 고치기가 끝나는 01~02시쯤 메인이 너를 부른다. 그때부터 실제 SQL 작업을 단계마다 승인하고 관리자 창을 눌러 03~04시쯤 끝낸다.」다. 메인 전달이며 이 pane 직접입력으로 격상하지 않는다. 시간은 예상 일정으로 실제완료를 보장하지 않는다. DS-1 수정 루프·독립검증·push 전 검사셋을 유지하고 push 후 즉시 commit/v4초안을 메인에 전달한다. 오늘밤 진행 선택이 이후 SQL 변경·승격·단계승인을 대신하지 않으며 각 단계 승인은 메인경유로 받는다.

- DS-1 선행시험 완료·정산·pane종료. report는 dependent-service-fix/pretests-1/report.md, 최종44PASS/68FAIL/15OBSERVED는 의도RED이며 기존adapter152/lifecycle98은 실패0. 제품41파일hash불변·Changed1대상위반0을 리드가 대조했다. helper 집계 pretests-liveness.json의 현재dispatch 초과395초1회(긴시험파일쓰기 보고), 공식질문대기243초는 제외. 신규Astra제품구현 뒤 기존시험의 SCM/CIM fixture를 신규Opus검증자가 먼저 보완하고 실행한다. 구현자에게는 기존adapter실행을 금지해 실제SCM조회위험을 막았다. 정확오류 문자열은 구현설계로 두되 의미있는 안전대조군·첫쓰기0·Stop0 판정은 유지한다.

- DS-1 신규Astra xhigh 구현 task_00a1727c8004/ctx_5b72628eaf19의 공식 입력접수·턴시작 확인. 최초준비wait10초는 timeout이었으나 다음wait satisfied 및 화면모델·cwd 확인 후 발행했다. 계약/receipt는 dependent-service-fix/implementation-contract-v1.md 및 implementation-start.json, backend unknown. 다음첫일은 구현완료원문/실제diff 대조 후 신규Opus 강검증(기존시험fixture 보완을 실행보다 먼저)이다.

- DS-1 구현 task_00a1727c8004 쓰기종료·정산·pane종료. 제품3파일+46/-4, 고정시험111/1/15 및 Plan106/Lifecycle98·Changed3대상0을 리드가 원문/diff/집계와 대조했다. 1FAIL 귀속은 미확정으로 전체통과보류. 신규Opus 강검증 task_f6cb87675c6e/ctx_1c83a9e34cc7의 공식턴시작 확인, 계약 dependent-service-fix/verification-contract-v1.md. 제품hash고정·테스트만쓰기, SCM대역보완을 기존시험 실행보다 먼저 수행한다. 후속 첫일은 독립판정 원문을 처리한 뒤 Linux전체검사·push·v4초안이다.

- DS-1 구현자 생존신호 helper 정산: dependent-service-fix/implementation-liveness.json, 최대간격304초 초과1회(정확원인미보고), 공식질문대기229초·21초 제외. 독립확정실패 집계와 구분한다.

- DS-1 독립강검증1 차단 #1: README 적용단계/공개코드 누락(task_00a1727c8004 귀속, 확정실패1회). 원문 dependent-service-fix/verification-1/report.md·verdict.json 직접확인. 신규69/0/11·교정선행112/0/15·기존635PASS, 제품3hash불변. fixture오류는 독립귀속/동일명령전후로 교정, 신규시험은 재시작검사/Stop변이 검출을 보완했다. 검증자정산종료 뒤 README만 새수정·새검증으로 계속한다(같은산출물수정3회). O3 Cleanup조건유지, O1 snapshot잔여는 수동복구안내에 포함; R1~3/O5는 후보이며 코드수정확대0. 실제노트북/CLI전체/Linux셋은 미완료.

- #1 README수정 계약은 dependent-service-fix/readme-fix-contract-v1.md. 새pane 준비가 Codex업데이트선택창으로 막혀 R6에 따라 메인처리요청, 리드입력/업데이트/Task발행0. pane정보는 readme-fix-launch.json. 다음첫일은 메인처리뒤 동일성·모델·준비확인 및 계약연결이다. 검증자생존신호 helper는 verification-liveness.json:371/413/458/371/474초 초과5회, 원인미보고; 별도제품확정실패로세지않는다.

- 메인 msg_e8e91d474fff가 업데이트창 이번Skip만 처리(설치/설정변경0 전달). 리드가 동일incarnation·v0.162.1·Astra xhigh·idle 확인후 task_752958cb5be0/ctx_c285bd441113 발행. receipt는 accepted/turn_start_unobserved였고 JSONdraft5284chars와 계약길이+머리말4787 대조조건 충족으로 R5 텍스트없는Enter1회. 사후transcript의 해당Task user입력 및 화면 TASK절 계약전문을 대조했고 draft소멸·Working·live 관측. transcript도구는입력일부clip이므로 전문대조는 화면TASK절 근거, backend unknown. 원시 readme-fix-draft-precheck.json/readme-fix-transcript.json/readme-fix-after-enter-screen.json. 다음은 README수정완료뒤 새독립문서실사와 신규시험재실행.

- #1 문서수정 task_752958cb5be0 완료·정산종료. README§1/§8 +27/-6, 다른database93파일hash불변·diffcheck0 원시를 대조했다. 신규Opus5.5 xhigh/backendunknown 문서재검증 task_c8ce28c799e4/ctx_a63b6ce59172 공식턴시작확인. 계약 readme-verification-contract-v1.md, 제품/시험고정·근거만쓰기, 문서실사와지정두대역시험재실행. 다음첫일은판정처리뒤 Linux검사셋/commit/push/v4초안이다.

- 2026-10-16T16:26:44.7852119Z DS-1 README 재검증 task_c8ce28c799e4 통과·기존#1 해소·새결함0. report/verdict 전문과 raw/a01-audit/command 원시 및 현재6hash를 리드가 대조했다. 두 시험69+112 PASS/0 FAIL/26 OBSERVED. RO-1 Cleanup 재개불가 상태 명시·RR-1 snapshot 문단 주어는 비차단후보로 보존하고 재수정하지 않는다. 검증자 release/동일incarnation idle/종료 완료. 생존신호 readme-closeout-liveness-corrected.json: 문서수정352초/검증301초 초과각1회, 정확원인미보고. 최초 inbox --run 미지원 오류와 helper입력오류는 원시보존 후 지원옵션으로 정정했다. goal의 CRLF20줄은 의미변경 없이 LF로 정규화한다. 다음은 승인된 local commit/전용WSL Linux검사/Changed/push/v4계약이며 실제SQL/TLS/게임은 미완료.
