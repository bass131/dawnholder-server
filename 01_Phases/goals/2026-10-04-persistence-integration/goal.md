# 실제 SQL 설치·엔진 판정과 저장소·제한 복구 통합

상태: **중간 마감. 독립 재검증에서 INSTALL-01~04 해소, INSTALL-05 부분 해소와 신규 INSTALL-06이 남았다(960 PASS/2 FAIL/9 OBSERVED). 마지막 Sol 기동은 직접1회·메인 대리3회 모두 실패해 Task/Dispatch 미발행으로 보류했다.** 메인의 후속 결정에 따라 현재 작업 트리를 체크포인트 commit/push(PR 없음)로 보존하고 새 작업 없이 대기한다. 전체 goal은 미완료이며 실제 DB·G2·D:·서비스 단계는 실행하지 않았다.

## 사용자 중간 마감 결정

메인 `msg_06ab6853f32e`(2026-10-04T11:10:02Z)가 사용자 원문 **“결정대기 관련에서 현황판에 업데이트가 안됬네, 일단 A긴 해”**를 전달했다. 메인이 제안한 A안은 “지금부터 새 착수 동결: 진행 중인 검증·결함 수정 루프만 끝내고 새 goal·PR 범위는 열지 않는다”다. 메인 전달이며 사용자 직접 메시지로 격상하지 않는다.

- GameDev 마감 단위는 INSTALL-05 잔여·06의 **신규 Sol1회→신규 Opus1회** 수정·재검증이다. 이 마지막 회차 뒤 결함이 남으면 번호·원시·다음 할 일을 이 goal에 기록하고 멈춘다.
- 통과하면 현재 작업 브랜치에서 체크포인트 commit/push까지 한다. 새 PR은 만들지 않는다. 새 goal·계획·PR 범위를 열지 않고 다음 후보는 기록만 한다.
- 사용자 행동이 필요한 D: 연결, 실제 DB/G2, 화면 확인과 서비스 시작은 마감 뒤로 미룬다. 기존 실행 초안은 승인되지 않은 상태를 유지한다.
- 마감 지점에서 메인에 **“중간 마감 도달”**을 보고한다. 상태·branch/HEAD·미커밋 파일 수·잔여 결함/후보·재개 첫 단계를 포함하고, 이후 새 작업 없이 대기한다. 이 중간 마감을 전체 goal 완료나 Gardener/R-8 완료로 표현하지 않는다.

### 기동 실패에 따른 마감 지점 변경

메인 `msg_6454aa8d5367`(2026-10-04T11:22:37Z)이 대리 기동 실패 뒤 **현재 작업 트리의 체크포인트 commit/push(PR 없음)**를 지시했다. 따라서 위 “통과하면” 조건으로 마지막 회차를 계속 기다리지 않고, 미해결05·06을 그대로 기록해 중간 마감한다.

- 담당 Astra의 split은 `Timed out waiting for split pane handle`로 실패했다. 원시는 `installation-final-fix-launch.json`(요청 `88aba1a8-cef3-4a71-b095-41cc1646d18d`)과 `final-fix-split-timeout-terminals.json`이다. Task·Dispatch·제품 쓰기는 시작하지 않았다.
- 메인이 보고한 대리3회: 11:19:30Z `f217bd79-2459-463c-8fd8-b4c97dfcb103`, 11:20:34Z `28d595f1-73a0-407e-a02a-8cc7a2a08d0c`, 11:20:56Z `4f61ce9b-ab67-454b-9bf3-6782f9f00745`. 모두 같은 timeout이고 각 terminal list7개/새handle·잔여pane 없음이다. 이는 메인 메시지의 관측이며 Astra 직접 실측3회로 표현하지 않는다. 원문 `delivery_7e579977e120.json`에 보존했다.
- 준비 계약 `installation-final-fix-contract.md` SHA256 `45A82670D5A7933A117D77258D3C7AC54659A76CC7DF00B64B1A449FF7FAC10E`, 입력 `installation-final-fix-inputs.json` SHA256 `3B91112EA3EB564F4D1C931DD5357F35444CCF6A1AA742ED8D2076B5EE27FDE8`. 두 파일은 기동 전 역사 입력이며 **미발행**이다. 제품 범위는 Environment.Common/New-TestDatabase 두 파일, INSTALL-05 잔여·06의 마지막 Sol→Opus 한 회차다.
- 새 탭 생성·배치 우회·모델 대체·Astra 직접 구현은 하지 않는다. 메인 현황판 `term_4d1431ae-66ff-4f50-b396-bad517e0672d`는 건드리지 않는다. 분할 실패 원인 확인은 마감 뒤 메인이 따로 맡는다.
- **재개 첫 단계: Sol 기동 경로 확인 → installation-final-fix 계약으로 마지막 회차(새 Sol → 새 Opus).** 재개 승인 후 현재 HEAD·goal/메모 hash를 다시 고정하고 위 역사 입력을 그대로 실행 권한으로 쓰지 않는다. 마지막 회차 뒤에도 남으면 새 회차를 열지 않고 번호·원시·다음 할 일을 기록한다.
- 체크포인트의 실제 commit/push/원격head·보존 결과는 로컬 `mid-closeout-checkpoint.json`에 둔다. 중간 마감은 제품 PASS·G2 승인·실제 DB 성공·전체 goal 종료를 의미하지 않는다.

## 재개 지점

- 작업 경로 `C:/Dev/DawnHolder_Project`, branch `feat/persistence-integration-20261004`. 최신 main 기준 `3f0cb5e2861574ea1e6b092875de27694897b21d`에서 시작했다.
- 이전 종료 checkpoint `f137bbb6ca2a7b5bc424769f8083d2d9814dc32c`를 `2c3522e075cd3f68804f80dec1735f368669eb0e`로 cherry-pick했다. 이전 goal만 +9/-2이며 이 목표의 첫 PR에 포함한다. 원본 branch/commit은 유지한다.
- Unity 실물3 SHA·skip-worktree S3·stash2는 전환 전후 일치한다. 근거는 `.backups/verification/2026-10-04-persistence-integration/branch-checkpoint.json`이다. 상태가 깨끗하다는 Git 출력만으로 사용자 파일 보존을 판단하지 않는다.
- 다음: 재개 승인 뒤 Sol 기동 경로 확인→고정 입력 갱신→위 마지막 수정 회차다. 현재는 새 작업 없이 대기한다. 실제 DB/G2는 마감 뒤이며 검토 초안은 ExecutionApproved=false/G2=null을 유지한다.
- 현재 Run `run_da60626aa8de`. 첫 Opus Task `task_19f35f0a16b0`/Dispatch `ctx_7aa1fb01a3ce`는 작업 완료·정산·정확 pane 종료했다. task outcome=succeeded는 실사 완료이며 제품 판정은 FAIL이다. 이전 목표 식별자를 실행 권한으로 재사용하지 않는다.
- 수정 Sol Task `task_10ee107139ee`/Dispatch `ctx_4fb9710dc625`도 쓰기 종료·원문 대조·release·정확 pane 종료했다. 자기 점검은 독립 판정으로 쓰지 않고 위 신규 검증자에게 넘겼다.
- 첫 재검증 Opus Task `task_32164d72f32a`/Dispatch `ctx_2a0a1c3853f1`는 `msg_1a5fa823d2ce`로 쓰기 종료했고 원문/원시 대조·release·정확 pane 종료했다. 해당 Task 성공은 실사 완료이며 제품 판정은 통과 아님이다.

## 현재 실사 결과와 보류

- 최신 판정 `installation-recheck-2/verdict.md` SHA256 `1A85FE1DB53A7F450DC3BB8CC9703682C3A3BFCDD8EB682A1158E3C5A038FD69`: 01~04 해소, 05는 Install의 DB identity 변경 사유가 Unclassified로 사라지는 잔여, 06은 완료 journal 쓰기 실패 뒤 디스크 Pending인데 not Pending이라고 안내하는 새 결함이다. 둘 다 fail-closed와 자원 보존은 유지하지만 미해결이다. 마지막 수정 회차는 기동 실패로 보류했다. 05는 첫 재검증 실패1회, 06은 최초 발견이다.
- 부모 `recheck-source-audit.json`에서 최종 전체960/2/9·기존 lifecycle53개 전부 유지·제품35와 기타 고정 입력 불변·변경 테스트1개·Unity3/S3/stash2 보존을 대조했다. lifecycle76/2/1이며 새 OBSERVED1은 비-int SqlNumber 합성 입력의 journal 잔존이다. 실제 비-SqlException fallback 정수는 Closed 연결 helper에서 관측했고 SqlException 분기는 소스 계약만 확인했다. 실제 SQL은 미실행이다.
- 재검증 절차 관측: baseline 실행과 첫 편집의 겹침은 원본 harness hash/53행 동일 근거로 범위를 확인했다. 단계별 inbox check 누락으로 부모 지시 확인이 늦어 추가 재실행이 필요했고, 무효 launch2/parser2·자기 시험 정규식 오류1도 원문 §11에 있다. `msg_7c384bec95e7`로 즉시 메인에 보고했다. 부모 서버 시각 대조의 heartbeat 최대 간격221초/300초 초과0과 별개이며 **절차 전체 PASS로 표현하지 않는다**. 원문은 보존하고 `recheck-settlement.md`에 인수 범위를 남긴다.
- 판정 원문 `.backups/verification/2026-10-04-persistence-integration/installation-review-1/verdict.md`, SHA256 `80C03344EBB19D9C93974030497B1C5C5F7F956F3531DE8B8FDF6BC75B4E825F`. 새 시험17 PASS/36 FAIL(상류 실패 연쇄29건 포함), 기존6 suite 전후884 PASS/0 FAIL/8 OBSERVED다. 실제 DB·U-01·OS/ACL/DPAPI는 미실행이다.
- 최초 실사에서는 INSTALL-01 연결 문자열 builder 대입과 INSTALL-02 두 번째 journal 교체가 차단이었다. 03 동등 경로 lock, 04 cleanup의 계정명 상수, 05 안전한 중단 이유 소실/실제와 다른 기록 안내를 포함해 메인 `msg_16b3c72e78a6`가01~05 신규 Sol 수정을 지시했다. 외부 예외 원문 대신 비밀 없는 고정 사유와 실제 journal 기록 여부만 전달한다는 요구를 적용했다. 이후 해소/잔여는 위 최신 판정을 따른다.
- 실제 DB 보류: 읽기 전용 OS 관측에서 `MSSQL$SQLEXPRESS`는 Stopped/Manual, 등록 실행파일은 `D:\MSSQL2025\MSSQL17.SQLEXPRESS\MSSQL\Binn\sqlservr.exe`, 현재 D:는 DriveNotFound다. 단순 서비스 시작 요청은 철회했고 메인 `msg_d3a5681379d0`가 D: 원래 환경을 사용자에게 확인 중이다. 볼륨·서비스/UAC·재설치·경로 변경은 승인되지 않았다. DB 폐기 승인과 서비스 종료를 구분하며 원래 Stopped 복귀 여부는 G4 별도 결정이다.
- 신규 Opus 보류 이력: 메인 `msg_b4cec1593dd6`가 잠시 보류했으나 **`msg_e7e773056b87`로 해제**했다. 메인이 전달한 사용자 원문은 “1) Claude 주간 사용량 96% - 화요일 20:00 초기화 전까지 우선순위 → C 코멘트 「Max 20으로 플랜 방금 업그레이드 완료」”다. 신규 `claude-opus-5-5`로 재검증하며 파트당 동시 검증자1명·기존 범위는 유지한다.
- D: 사용자 결정: 메인 `msg_da0d5b05b80e`가 “5) SQL Express - 설치된 D: 드라이브가 없어 DB 단계가 막힘 → A D: 드라이브 다시 연결”을 전달했다. 사용자가 직접 연결하며 이후 메인이 읽기 전용 재확인 후 서비스 시작을 별도 사용자 판단으로 올린다. 이 답은 서비스/UAC/DB 접속 승인이 아니다. 전달 시점에는 여전히 C:/F:만 관측됐다.
- 수정 결과: `installation-fix-1/report.md` SHA256 `339F6254EF5DA2391D152D99E39326FFE2275D7F9B9C197D7605D3576E1F0A6B`. lifecycle17/36→Sol 최초51/2→진단 보완53/0, 기존6 suite884/0/8 유지. 부모 `fix-source-audit.json`에서 실제 집계937/0/8·제품4변경/31불변·테스트2 및 Unity3/S3/stash2 보존을 대조했다. OBSERVED8은 검사 대상 제외4건과 실제 엔진 미관측4건이며 성공으로 합산하지 않는다. 정확 Name/Detail은 audit와 report에 있다.
- 수정 절차 정산: `fix-settlement.md`에 표식 교정1건과 cadence5건(336/345/353/313/436초, 최대초과136초)을 개별 관측으로 보존했다. 메인 `msg_7cf40e2e0771`, `msg_d0eae81bb1a1`, `msg_9624ef1d523c`를 적용하며 절차 전체 PASS로 바꾸지 않는다. 신규 검증에 이 이력과 자체 시험의 범위/미실행을 전달한다.
- 절차 기록: heartbeat 임시 기준 `msg_9cdcc96cf620`과 개별 결정 정본 `msg_c9552132e2f7`을 적용했다. 1,656/803/764초 세 공백과 판정문 기록 누락은 **미준수**로 보존한다. 메인 `msg_16b3c72e78a6` 결정에 따라 원문을 덮지 않고 부모 `review-settlement.md`에 보충했다. 절차 전체 PASS로 바꾸지 않으며 제품 FAIL 목록만 수정 근거로 인수한다. 초기 입력의 helper3개 누락/보충과 종료35개 hash 실측도 이 부록에 있다.

## 결정 출처와 선행 결과

메인 Claude `msg_5322a941f836`(2026-10-04T07:54:31Z)가 사용자 원문 **“3) 다음 목표 - 시험 DB에 SQL 설치·엔진 판정·저장소 연결 → A 진행”**을 전달했다. 메인 전달이며 이 세션의 사용자 직접 입력으로 격상하지 않는다. 이전 [SQL 구조·오프라인 goal](../2026-10-02-persistence-repository/goal.md)은 PR169/171 병합·Gardener·메인 정산 뒤 종료했다. 새 목표는 이 승인 범위만 수행한다.

메인 `msg_837c528baf27`(2026-10-04T08:04:54Z)는 초안 SHA256 `E3EA2AFB65767B97AADB16B422E1DDF24EC54F3D121D4E49727FBFF50BBCACE0`의 범위 일치와 DB 접속 없는 첫 위임을 확인했다. G2 executor·명령·시간 창 및 관리자/UAC 단계는 **실행 전에 메인에 결정 요청**으로 보내고, 사용자가 권한 창을 확인할 수 있도록 메인이 대시보드에 올린다. 현재 비승격 `YYH_DESKTOP\bass1`(SID 끝1001)을 관측했으며 승격·UAC 실행은 하지 않았다.

- 선행 제품 merge `3e07e1b0b318881701b5b7bab8adbe087b596420`, 종료 기록 merge `bca2adf0c1b16ef467212151a9dc9bd3680bff79`. 이전 검증/CI는 새 실제 SQL 실행 근거가 아니다.
- G0 `msg_78c5c1647b7c`, G1 `msg_21ae101a52db`의 정확 범위는 이전 goal의 [승인 구간](../2026-10-02-persistence-repository/goal.md#사용자-세-안건--추천안-승인g1)을 따른다. 최종 배포는 001~004와 modules/manifest이며 과거 002~013 계획은 사용하지 않는다.
- 메인 `msg_78db7cfb8c15`가 승인한 로컬 `successor-goal-draft.md`(SHA256 `DB20282EEC7089025C0BDCBE34436DD8196DC48AF9CA03E1AF1C756DD138D578`)의 조기 엔진 게이트를 승계한다. 오래된 초안의 첫 PR마다 Gardener/R8 문구는 최신 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)의 **전체 goal 종료** 기준으로 적용한다.
- [영속성 로드맵](../../milestones/2026-09-30-contracts-persistence/roadmap.md)의 저장소·격리 검증 단계 후속이다. Q-1B는 이 목표의 DB 통합 뒤 다음 계획으로 다루며 자동 착수하지 않는다.

## 범위와 보존 동작

| 책임 | 할 일 | 보존·제외 |
|---|---|---|
| SQL 설치·엔진 | 승인된 시험 DB에 최종 001 및 migration002~004/modules 설치, 검증기·설치 수명 도구의 실제 결함 수정, 최초 빈 scalar5개 U-01 실증 | 001 원문/checksum, 공개9RPC·29열·결과/오류/grant·tx 계약. 설치 불가 과거 중간 tree 사용 금지 |
| 독립 저장소 | `02_Server/Persistence/` net10 library, immutable 요청/token/result/options, typed SQL adapter와 recovery interface, 단일 deadline·unknown 처리 | GameSession/GameWorld/Host 의존·GameServer composition·actor/Ready·Unity 연결은 D2/D3 |
| 제한 복구 | `99_Tools/PersistenceRecovery/`의 Inspect/Recover/Release/Resolve, 전용 Windows principal launcher 및 승인된 PowerShell/System.Data.SqlClient 경로 | 임의 SQL/RPC/DB/endpoint/credential, Management UI·범용 관리자 기능 제외 |
| 독립 검증 | 별도 Persistence.Tests, database/tests fixture/fault 및 Test-Database 현행 계약, 실제 Windows/WSL 연결·권한·동시성 | 제품 코드는 Sol만, 테스트 작성·판정은 신규 Opus만 |
| 통합 문서 | MSSQL 안내·D2 소비 계약·판정/미실행·PR/자원 정산 | 운영 지침·정책·다른 파트 파일의 무관한 정리 제외 |

승인된 `Microsoft.Data.SqlClient 6.1.7` 추가/restore는 구현 시 공식 지원/patch를 재확인한다. 다른 버전·의존성이 필요하면 메인 판단을 받는다. SqlDbType/길이와 엄격한 단일 result·29열·JSON 계약, 취소/timeout의 unknown을 보존한다. 새 operation ID 자동 생성·DB/메모리 fallback·부분 결과 성공 판정은 하지 않는다. 게임 틱에서 I/O 완료를 기다리지 않는다.

## DB 접속 경계 G0·G1·G2

| 경계 | 고정 내용 |
|---|---|
| G0 범위/설계 | 독립 저장소·제한 복구·검증을 위 범위로 분리. 제품/테스트/판정 소유를 Task별 명시. 새 라이브러리/범용 도구 권한을 만들지 않음 |
| G1 대상/수명 | 로컬 `.\SQLEXPRESS`, 시험 DB **Dawnholder_Dev_D1b_20261002 하나의 신규 생성→최종 설치→시험→최종 폐기 수명**. 기존 동명 자원이 있으면 중단하며 채택/DROP/재생성하지 않음. PR 사이 같은 DB 보존 |
| G1 principal | runtime SQL login **dh_d1b_runtime_20261002**, 해당 DB dh_runtime만. recovery **YYH_DESKTOP\dh_d1b_recovery** 비관리자 Windows 계정, 해당 DB dh_recovery만. 동명 계정 채택 금지 |
| G1 연결/TLS | 관리 local shared memory, Windows/WSL `tcp:127.0.0.1,14330`. Encrypt Mandatory(기존 도구 true), 해당 fixture만 TrustServerCertificate=true. 저장소 기본 false·자동 fallback 금지 |
| G1 binding | slot1, AccountId `828e39df-ba5d-4209-86ea-4e9ec1a43ed5`, CharacterId `686e8071-daa1-404d-af7d-d4bb2442748c` |
| G2 설치 경로 | 실제 실행할 provision/install/cleanup 최종 diff·hash, 비밀 없는 승인 계획/manifest, 정확 executor/명령/시간 창·ACL/identity·부작용을 신규 Opus가 **최초 접속 전에** 정적 실사 |
| G2 복구 경로 | 아직 미구현인 launcher/저장소가 초기 설치 실사로 승인됐다고 하지 않음. 구현 후 **해당 경로 첫 실행 전에** 최종 diff·identity/secret 전달/ACL을 새 Opus가 추가 실사. 실행 경로 수정도 재실사 |

기존 승인에는 DB 존재기간 만료가 정해져 있지 않다. Astra가 실제 작업자/명령에 대한 시간 창을 따로 고정하며 자원의 한 번 수명과 구분한다. G2 전에는 SQL 접속·비밀 읽기·외부 구성을 하지 않는다. 관리자/UAC 단계는 정확 명령·실행자·시간 창을 메인에 미리 전달한다. 사용자 직접 실행이 필요한 경우 메인이 요청한다.

G2 통과 후 첫 접속은 DB_NAME/instance/endpoint·ProductVersion/patch·collation/RCSI·schema/module hash·binding·SQL SID/ORIGINAL_LOGIN/role/group·legacy writer 부재를 확인한다. 실제 관측값을 기대값으로 자동 채택하지 않는다. runtime 양성 시험은 실제 권한 대조 뒤에만 진행한다. patch가 바뀌면 golden vector를 다시 검증한다.

비밀은 채팅·goal·argv·로그에 넣지 않는다. 목표 전용 ACL+DPAPI와 자식 stdin/pipe를 쓰며 실제 recovery Windows identity를 parent 관리자 성공으로 대체하지 않는다. 서비스 설치/설정/재시작·registry·방화벽·인증모드·인증서 저장소·SAC·전역 설정·기존 principal 변경은 범위 밖이다. 접속 조건이 없으면 상태·영향을 메인에 보고한다.

## 작업 순서·소유·PR 경계

1. **계획·설치 경로 준비:** 메인에 이 초안을 전달한다. 기존 승인 계획·도구를 재사용해 필요한 변경과 정확 파일 소유를 좁힌다. 신규 Sol(gpt-6.1-sol max)이 제품만 작성한다. 제품 쓰기 종료 뒤 새 Opus(claude-opus-5-5)가 G2 설치 경로·보고/실제 diff를 실사하고 필요한 테스트를 작성한다.
2. **조기 엔진 판정:** G2 통과한 명령만 한 실행자·한 DB writer 시간 창으로 수행한다. 승인 DB를 한 번 만들고 최종001~004/modules를 설치한다. 신규 Opus가 schema/설치와 빈 scalar5개 U-01을 실제 엔진에서 먼저 판정한다. 실패는 raw 보존→새 Sol 좁은 수정→새 Opus 재검증으로 해소한다.
3. **첫 PR — 설치·엔진 판정:** 필요한 설치/검증 도구 현행화, 실제 schema/U-01 근거, 새 goal과 이전 종료 checkpoint를 묶는다. 제품 SQL 외부 계약은 유지한다. 저장소 구현 전에 실제 엔진 불확실성을 줄이고 사용자가 별도로 검토할 수 있어 분리한다. 정확 PR head와 CI/미실행을 보고하고 개별 사용자 병합 승인을 받는다. 동일 DB는 계속 보존한다.
4. **저장소·복구 구현:** 첫 PR 병합을 반영한 최신 main의 후속 branch에서 같은 goal을 이어간다. 첫 엔진 판정 뒤 신규 Sol이 저장소와 복구 경계를 좁은 Task로 순차 구현한다. 새 Opus가 최종 경로의 G2 추가 실사 및 독립 테스트를 수행한다. 한 작업자/검증자 한 Task 뒤 정산·종료하며 재사용하지 않는다.
5. **독립 통합·두 번째 PR:** D1a 시험 행렬을 실제 DB·Windows/WSL에서 판정하고 필요한 수정·재검증을 완료한다. 저장소·복구·동시성/unknown·프로젝트 등록 및 소비 안내를 두 번째 PR로 묶는다. 각 PR은 별도 사용자 병합 승인, 자동 병합 금지다.
6. **전체 goal 정산:** 아래 G4를 마치고 모든 PR/결과 기록 뒤 신규 Opus Gardener를 수행한다. 메인/사용자가 결과·남은 위험·BACKLOG·다음 계획을 점검한 뒤 R-8 교체한다. 첫 PR만으로 goal 종료나 Gardener/R8을 실행하지 않는다.

PR 분할은 줄 수 상한이 아니라 독립 판정 가능한 경계에 따른다. 엔진 결과가 완료조건/범위를 바꾸면 의존 작업 전에 메인 판단을 받는다. 범위 안 제품 결함은 승인 반복 없이 고치며 최신 ORCA의 같은 계약/번호 확정 실패 집계 규칙을 따른다.

Astra는 goal/위임 계약/비밀 없는 실행 계획/결과/Git를 소유한다. Sol은 정확히 할당된 제품 파일만, Opus는 정확히 할당된 테스트·판정만 쓴다. 제품 파일은 `99_Tools/database/`의 필요한 부분, `02_Server/Persistence/`, `99_Tools/PersistenceRecovery/`, 필요한 프로젝트 등록/WSL sync로 제한한다. `Test-Database.ps1`의 검증 동작과 독립 시험 장치는 Opus 소유로 배정한다. 동시 DB writer와 같은 파일 동시 쓰기는 금지한다.

## 관찰 가능한 완료조건

시험 기대값의 정본은 [D1a verification-plan](../2026-10-01-persistence-technical-design/verification-plan.md)의 S01–S11, C01–C13, O01–O07, U01–U11, R01–R05와 [technical-spec](../2026-10-01-persistence-technical-design/technical-spec.md)이다. 오래된 미승인 상태 문구는 위 G0/G1의 후속 결정을 적용하되 시험 기대를 완화하지 않는다.

| 완료조건 | 필수 관측 |
|---|---|
| 실제 엔진 U-01 | scalar FOR JSON5개의 빈 집합/SQL NULL/명시 `[]` 결과를 실제 엔진에서 수집. 최초 설치/catalog read, malformed/unknown/drift 거부. D1a 시험 ID U01과 별개이며 source/fake/대상0으로 대체 불가 |
| schema/설치 | 001 checksum·기존 게임 행 보존, 최종001~004/modules 설치·재실행 무변경·중간 실패 원자성·catalog 변조 거부, CHECK parent_column_id/definition 실제 대조 |
| 최소권한 | runtime 및 실제 recovery Windows principal의 양성 RPC, 직접 DML/DDL·교차 role RPC 음성, SID/ORIGINAL_LOGIN/상속·legacy writer 부재, trigger 제거 뒤 잔여 조회 |
| 저장 계약 | 초기3행/Authority/Operation 원자성, token conflict/alias, 결과 종류·판정 순서·historical/current 분리, patch별 golden vector·float roundtrip, 입력 거부 전 불필요 쓰기 없음 |
| 경쟁·unknown | 실제 독립 SqlConnection A/B와 관측/barrier, old/new-first·stale release·late acquire·seal. receipt 폐기/transport 손실 별도, fault 위치·실제 commit·before/after 증빙 |
| 환경 구분 | Windows와 WSL의 net10/6.1.7 실제 연결·RPC 각각 판정. Windows PowerShell/System.Data.SqlClient recovery S06을 .NET Windows S07 성공으로 치환하지 않음 |
| 문서/규칙/CI | 사전 메모·관련 규칙 원문·실제 준수 위치·독립 판정 연결, 가독성/책임/배치·30초 탐색 실사, 해당 Changed/서식/빌드/테스트/CI의 실제 입력과 원시 근거 |
| G4 정산 | operation/owner/connection·임시 trigger/권한/프로세스 정산 후 정확 DB identity 재확인 및 최종 폐기. 이번 생성 login/Windows계정/credential 각각 SID·manifest 대조 후 정리. 잔여는 보고하며 강제 rollback/타 세션 kill/profile 임의 삭제 금지 |

각 엔진 시나리오에 명령·UTC·exit·OS/SDK/package/native dependency·코드 SHA·비밀 없는 DB identity·before/after 행/token/proof·fault 위치·commit 여부를 남긴다. 미실행은 환경과 영향을 적고 메인 판단 없이 완료로 합치지 않는다. O06/ U01/ R02는 저장소 계약까지만 확인하며 실제 Ready/actor/Host/Unity·L01–L04·E01–E04는 이번 통합 성공에 넣지 않는다.

## 범위 밖과 점검 지점

- 중간 점검: 첫 실제 엔진 판정/첫 PR 검토 및 동일 DB 보존 확인. 전체 점검: 모든 PR·G4·결과·Gardener 뒤. Q-1B는 DB 통합 뒤 사용자와 다음 계획에서 이어간다.
- C1(PS·SQL 120자 초과 식), C2(참조0 SQL 지역 선언)는 **검사 후보 채택/구현을 보류**한다. 기존 작성 규칙을 면제한다는 뜻은 아니다. 근거는 이전 goal의 Gardener와 메인 msg_5322a941f836. 공용 BACKLOG 등록/ID는 담당 파트와 조율하며 여기에는 상세 후보 장부를 만들지 않는다.
- Rules의 SQLFluff 재시범/구조 CLI·tests CI 연결은 기존 예정 후속과 소유를 확인하며 이 목표에 자동 포함하지 않는다. 새 기능·정책·도구·다른 영역 정리·게임 연동은 다음 계획 후보로 남긴다.

## 원문 근거

- 이번 로컬 근거 root: `.backups/verification/2026-10-04-persistence-integration/`. 초기 사전 메모 `astra-context.md`, 후속 `astra-followup-context.md`, 진입/이관 `branch-checkpoint.json`, 독립 `installation-review-1/verdict.md`, 부모 원시 대조 `review-source-audit.json`, 정산 `review-settlement.md`. 후속 기록은 초기 고정 입력을 덮어쓰지 않는다.
- 최신 수정/판정은 `installation-fix-1/report.md`와 `installation-recheck-2/verdict.md`, 부모 원천 대조는 `fix-source-audit.json`/`recheck-source-audit.json`이다. 중간 마감 맥락은 `astra-closeout-context.md`, 실제 Git 정산은 `mid-closeout-checkpoint.json`이다.
- 이전 승인 초안/승인 전달과 종료 checkpoint: `.backups/verification/2026-10-03-persistence-repository/{successor-goal-draft.md,successor-goal-review-delivery.json,closeout-pr171-checkpoint.json}`. 원문은 덮어쓰지 않는다.
