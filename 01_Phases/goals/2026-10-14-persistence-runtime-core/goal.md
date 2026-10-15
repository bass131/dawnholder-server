# 저장소·복구 인터페이스와 저장 작업 핵심

상태: A/B 독립 강 검증과 유일한 재검증1회 뒤 통과했다(#1·#2 해소, 새동작결함0). 실제 Persistence237/237·전체1297중1292pass/0fail/기존skip5·좁은서식exit0. 검증자를 정산·종료하고 PR240(https://github.com/bass131/dawnholder-server/pull/240)을 열었다. 현재 head/CI와 병합 관문은 PR의 실제 상태 및 메인의 정확 head 승인에서 확인한다. 실제DB/게임 연결은 후속 승인goal 범위다. 아래 진행 기록은 해당 시점의 원시를 보존한다.

## 적용 중인 사용자 결정

### 2026-10-14 - 리드 전원 Sol xhigh, Sol 작업자 xhigh 전환
메인 msg_18c90b33c9a1(2026-10-14T14:21:18Z, from term_e2961048-266e-4503-b52b-dfb986d8c9b5)이 전달한 사용자 원문:
- 23:2x KST 「음 그리고 리드급 아무래도 Sol Max로 전체적으로 확장해야 될 거 같아, Astra 토큰 사용량이 감당이 안되네」
- 이어서 「음 Sol 그리고 정정사항은 작업자랑 리드 전부 Xhigh로 변경, 핫픽스 적용 지시해줘」

앞선 리드 하나 Sol max 시범(msg_928a88962458)은 대체된다. 메인 전달이며 이 pane의 사용자 직접 입력으로 격상하지 않는다. 원문 전체는 formal-g2-card-review-20261014/lead-sol-transition-source.json에 보존한다.

메인 해석에 따라 기본 다섯과 승인된 추가 리드는 gpt-6.1-sol xhigh, 태그 [Core 리드 Sol]로 전환한다. 새 Sol 배정 작업자는 codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh로 연다. Astra 배정 작업자는 gpt-6-astra xhigh 유지, 독립 검증자 모델 시범·R7·Advisor 배정도 유지한다. 실행 중 작업자 모델은 바꾸지 않는다. 전환 중 같은 파트 Astra/Sol 리드 태그는 모두 인정하되 from_handle·Task·Dispatch를 대조한다.

리드는 안전 지점에서 메인에게 「Sol 전환 준비됨」을 보내고 턴을 끝낸다. 메인이 같은 Codex 대화를 Sol xhigh로 다시 열고 새 handle에 진입 메시지를 주면 같은 Run을 run-use로 인수한다. 진행 중 검증자는 유지한다. 현재 이 기록은 전환 준비이며 실제 런타임 모델 변경 완료가 아니다. Rules 정본 핫픽스 전에도 이 사용자 결정이 적용 근거다.


### 2026-10-14 - 10-28 기능 동결까지 시연 우선 모드
메인 msg_beda86f6bc2f(2026-10-14T14:11:26Z, from term_e2961048-266e-4503-b52b-dfb986d8c9b5)가 전달한 사용자 원문(메인 pane Enter 제출, 2026-10-14 23:10 KST 무렵): 「대시보드 결정 응답: 1) 10-28 시연 - 동결까지 시연 경로 우선 모드로 검증 절차를 줄일지, 지금 절차를 유지할지 → A 시연 우선 모드」. 메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다.

지금부터 2026-10-28 기능 동결까지 적용하며 동결 뒤 원래 절차로 돌아간다. 10-13 승인 시나리오의 영상 장면, 특히 3·6·7·8 길드 순환과 5 보스 보상의 실제 동작이 목표다. 이 절은 아래 과거 절차·기록보다 현재 적용에서 우선하며 과거 원시는 고치지 않는다.

- 동작 영향 없는 시험 중복·문서 표현·주석 위치·이름·가독성·배치 지적은 통과 차단에서 빼고 번호와 근거를 「동결 뒤 정리」에 남긴다. 제품 계산을 복제해 항상 통과하는 시험과 측정값 위장은 계속 차단한다.
- 같은 산출물의 독립 재검증은 1회까지다. 그 재검증에서도 차단되면 새 검증자를 열지 않고 메인에 올려 진행·좁은 수정·장면 축소 판단을 받는다.
- 생존 간격·hash 전수 누락·보고 형식 같은 운영 미준수는 판정문에 공개하고 제품 판정을 막지 않는다. 금지 구간 실행·메모와 결과의 불일치·결론과 본문의 모순은 메인에 올린다.
- 작업자 계약에 새 하네스·시각 계산 같은 준비 장치를 더하지 않는다. 기존 도구와 리드 공식 답으로 시작하며 실제 필요한 실행과 중량 순서 조율은 유지한다.
- 시연 장면과 관계없는 작업은 현재 지점에서 멈추고 동결 뒤 잇는다. 시연 밖 새 후보는 착수하지 않는다.
- 서버 상태/판정 소유·입력 검증·패킷 ID·PDL/생성코드/양쪽 직렬화·공유DLL 양쪽 검증, 구현/검증 분리와 실제 실행 독립 검증1회, 미실행 정직한 보고, Unity GUID/사용자파일 보호, Core 보존DB·옛실패자원 접근금지, 중량순서 조율과 PR별 정확head 병합승인을 유지한다.

원문 전체는 core-active의 .backups/verification/2026-10-04-persistence-integration/formal-g2-card-review-20261014/lead-demo-priority-source.json에 보존했다. 적용 작업 메시지는 msg_8599e13e4e83이다.
## 결정과 기준

메인 msg_863bfe8bbf4d(2026-10-14T11:26:10Z)가 전달한 사용자 원문: 「A로 가자, 알림사운드가 안들려서 온지도 몰랐네」. 대상은 현황판 core-parallel-steps-1014 「Core 영속화 - 2·3단계를 지금 나란히 시작할지, 1단계(SQL 판정)를 끝낸 뒤 순서대로 할지」다. 메인 전달이며 이 pane의 사용자 직접 입력으로 격상하지 않는다.

선택 효과: 별도 후속 goal의 2단계 저장소·복구 인터페이스와 3단계 저장 작업 핵심을 메인이 연 worktree 하나에서 신규 Astra 구현자 최대2명으로 대역 개발한다. 실제 DB 연결 실증은 1단계 판정 뒤 별도 범위·실행 승인으로 수행한다. 현재 SQL 검증 Start11:02:22Z/StopNew12:07:22Z/Deadline12:32:22Z와 보호 자원 경계를 유지한다.

- 메인 개설 worktree: C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-parallel. 기준 main20ce59ddd72d0cb72b6e230e386a62587b9bfe1b, 진입 dirty0을 직접 확인했다.
- branch: feat/persistence-runtime-core-20261014. 메인 허용으로 초기 bass131/core-parallel에서 이름을 바꿨다. 리드는 [Core 리드 Astra] 한 명, commit/push도 이 리드만 수행한다.
- 정본 goal: 이 파일. core-active의 기존 SQL goal/제품은 별개다. 원래 초안은 기존 SQL 근거의 catalog-verification-review-2/lead-parallel-goal-draft.md에 보존한다.
- 현재 조회한 로컬 origin/main은20ce59ddd72d0cb72b6e230e386a62587b9bfe1b이다. 새 작업 기준 SHA는 메인의 실제 개설 시 최신main SHA로 다시 고정한다.
- 요구 근거: D0 implementation-plan.md의 D1b/D2, D1a technical-spec.md 2·3.3·6·7절. 최신 FEATURE_MAP/로그인 계약과 대조한다. 옛 고정slot1을 다계정 저장으로 확대하지 않는다.

## 만들 것·소유

1. 공통 계약 문서의 단일 작성자는 Core 리드다. D1a의 ICharacterRepository 5연산과 별도 ICharacterRecoveryRepository, immutable 요청/결과/token, owner/fence/sequence, operation ID, monotonic absolute deadline을 위임 전 좁게 고정한다. 기술 설계에서 정한 의미를 바꾸는 결정은 메인에 올린다. 문서 계약을 제품 구현으로 대체하지 않는다.
2. Astra 구현자 A(gpt-6-astra xhigh)는 2단계 인터페이스와 값 타입의 유일한 코드 writer다. immutable 입력과 token 복사, 결과 구분·현재 권위 확인에 필요한 경계, runtime/recovery API 분리를 구현한다. 실제 SQL driver/connection factory/RPC 전송 구현·권한 실증은 이번에 넣지 않는다. 게임 연결·fallback 구현도 없다.
3. Astra 구현자 B(gpt-6-astra xhigh)는 3단계 순수 저장 작업 수명 코드만 쓴다. 캐릭터 한 lane, 진행1개·유한 대기·별도 final 인계, 비차단 접수, unknown 동안 freeze/동일ID 조정, NoWriteNeeded와 조건부 release, 전체 deadline·취소/늦은 응답·미완료 정산을 구현한다. 고정 공통 API를 소비하며 A 파일을 수정하지 않는다. GameSession/World/Host는 연결하지 않는다.
4. 대역은 시험 소유다. 운영 제품에 fake 저장소 fallback을 넣지 않는다. 공통 API 변경은 단일 writer A와 리드의 계약 갱신 뒤 B가 반영한다.

## 건드릴 곳

- A 신규 후보: 02_Server/GameServer/Persistence/의 ICharacterRepository.cs, ICharacterRecoveryRepository.cs 및 요청/결과/identity/token 값 타입. 타입별 정확 목록은 기존 예시와 사용 이유를 확인해 계약에서 고정한다.
- B 신규 후보: 같은 Persistence/의 CharacterPersistenceOwner.cs, PersistenceWorker.cs, CloseCaptureTicket.cs 및 필요한 책임별 작은 내부 helper. A 타입 파일과 중복 쓰기 금지.
- 시험: 02_Server/GameServer.Tests/Persistence/의 계약·작업 수명 시험과 제어 가능한 repository/clock 대역. 선행 시험 작성자와 독립 검증자만 쓴다. 구현자는 시험 기대를 바꾸지 않는다.
- 문서: 새 goal, 필요한 현재 server 영역/FEATURE_MAP의 해당 계약 설명만. 리드 단독 writer.
- 프로젝트는 기존 net10/xUnit 자동 파일 포함을 사용한다. 현재 csproj에는 C# 기본 glob가 있으므로 새 dependency/csproj 편집은 계획하지 않는다. 필요가 생기면 이유·범위를 먼저 대조한다.

## 하지 않을 것

실제 DB/컨테이너/비밀/보존 자원 접속·읽기·변경, SQL/migration/catalog/현재 core-active 제품 수정, SqlClient 도입·설정·설치, 다계정 저장·길드 금고/거점 레벨, GameSession/GameWorld/ServerHost/Inventory 및 World 현재 파일 쓰기, PDL/공유DLL/Unity/포트7777/실서버 실행, 단계4 로딩 게이트·단계5 실제 재접속 복원 연결. 현 SQL goal의 C# 제외는 그대로이며 본 별도 goal만 위 신규 범위의 권한을 가진다. recovery 주체 설치/자격증명/관리 실행은 별도다.

## 검증·관찰 가능한 완료조건

- 강 등급. 새 코드 goal이므로 정본 TDD를 적용한다: 리드의 문서 API 계약 고정 → 신규 Opus 선행 요구 시험·구현 전 실패 원시/쓰기 종료 → A/B 병행 구현 → 두 writer 종료 뒤 신규 독립 Opus 강 검증. 구현자 최대2명이며 파트 검증자는 동시에1명이다. 현재 SQL 검증자의 종료 전 후속 검증자를 열지 않는다. 선행 시험 작성자는 역할상 검증자가 아니지만 CPU/메모리 큰 실행은 현재 SQL 우선 창과 별도로 조율한다.
- 불변 token alias/잘못된 identity·입력, historical/current 권위 분리, runtime/recovery 분리, Accepted≠Durable, full queue·unknown freeze·동일ID 조정·late 결과·해제·취소·유한 종료/deadline을 제어 가능한 대역으로 반증한다. 실제 제품 API 진입점을 실행하며 제품 로직을 복제한 기대값은 금지한다.
- no-change close는 checkpoint 호출0/필요한 조건부 release를 관측한다. I/O가 멈춰도 접수/인계가 대기하지 않고 미확정은 Unresolved/RecoveryRequired로 남는지 본다. 작업자와 검증자는 같은 요구/명령의 실패→통과 원시를 연결한다.
- 양쪽 구현 통합 compile·관련 회귀·서식/규칙·독립 판정 통과와 정확 head PR 승인. 기존 기능·패킷·Unity 자산 변경0. DB lock/권한/commit durability·실제 game/Host/재접속은 미실행으로 명시한다.
- 테스트/빌드는 승인된 새 checkout의 전용 WSL clone과 경로 소유 lock을 사용한다. DLL 역복사·Windows post-build 자동 복사·다른 clone 사용은 하지 않는다. 현재 SQL 창 중에는 준비/작성 우선, 무거운 실행은 충돌 없는 창과 fresh 자원 확인 뒤 한다.

## PR 경계와 완료 표시

사용자 PR 하나 A(msg_be066f877900)에 따라 공통 계약·2단계 인터페이스·3단계 핵심·시험을 제품 PR 하나로 묶고 두 구현자 종료 뒤 신규 독립 Opus의 강 판정 한 번, 해당 PR 정확 head 사용자 병합 승인 한 번으로 진행한다. 앞선 차이1건은 해소됐다. 기능의 실제 DB/게임 연결은 다음 PR 범위로 남기며 마일스톤2/3 전체 완료로 표시하지 않는다. 두 writer는 같은 branch의 서로 다른 파일만 쓰고 commit/push는 Core 리드 한 명이다. 독립 강 판정·CI 후 메인이 정확 head 사용자 병합 승인을 받는다. 병합은 메인 전용 checkout에서 메인만 수행한다. 전체 goal 종료의 Gardener·종료 기록 PR은 현 정본을 따른다.

메인 대조 msg_1fa454e2825a(2026-10-14T11:41:27Z): 병행 범위·공통 문서 writer는 사용자 A와 일치한다. 리드 goal/API 문서/선행 시험 계약 초안만 허용하며 PR 경계 답 전 선행 시험 작성자·구현자 기동은 보류한다. 이 메시지를 새 DB 실행·처분 권한으로 사용하지 않는다. 작업자는 이후 R-5에 따라 이 worktree에 terminal create --worktree로 연다. 리드 pane은 core-active에 유지한다.

## PR 하나 승인과 발행 준비 — 2026-10-14

메인 msg_be066f877900(11:44:21Z)이 전달한 사용자 원문: 「대시보드 결정 응답: 1) Core 병행 goal - 2·3단계를 PR 하나로 묶을지(리드 초안), 단계별 PR 둘로 나눌지 → A PR 하나」. 메인 전달이며 이 pane 직접 입력으로 격상하지 않는다. 선택 효과는 공통 계약·2단계 저장소 인터페이스·3단계 핵심을 한 PR, 두 구현자 뒤 독립 검증 한 번, 정확 head 병합 승인 한 번이다. 메인은 원래 초안9bef2054…132f를 승인했고 goal/API고정→새 Opus 선행시험→새 Astra A/B→새 독립 Opus 순서 진행을 허용했다. 실제 DB·보호 자원·새 dependency는 여전히 범위 밖이다. 앞의 PR 미결·새 세션 보류는 이 결정으로 해제됐다.

고정 API는 api-contract.md이며 선행 작성자의 구체 시험 표면을 구현자 A/B에 동일 전달한다. 발행 입력은 로컬 .backups/verification/2026-10-14-persistence-runtime-core/pretest/goal-at-dispatch.md·inputs.json으로 고정한다. live goal 기록은 이후 리드만 갱신한다. 현재 SQL 검증자와 구분한 선행 시험 작성자 역할이며 build/test 실행은 별도 자원 조율 뒤다.

## 선행 시험 작성자 발행

R-5로 새 worktree에 직접 연 신규 Claude 세션의 최초 작업을 연결했다. Task `task_3d8d5c2e3864`, Dispatch `ctx_185721d8383b`, handle `term_21d40c01-6bd3-43e6-b9b8-1f631154c554`, incarnation `a6ec37cf-903a-4b81-96a1-bd53b4cd2928`이다. 최초명령은 checkout 안 TEMP/TMP `.backups/tmp/pre` 지정 뒤 `claude --model claude-opus-5-5`; 화면 Claude Code2.1.299 / Opus5.5 xhigh / backend unknown이다. tui-idle satisfied·첫 화면 선택창 없음·작업 경로/branch를 확인하고 `input_accepted`·`turn_started`를 받았다. 근거는 pretest/`terminal-create.json`, `terminal-ready.json`, `terminal-first-screen.json`, `worker-start.json`이다.

계약 v1 SHA256 `A4E0D0CC896C088B7D46DE390808C01499F72D96186F48E0E3AF66AA79FE8D34`, 고정입력18개는 `inputs.json`이다. 제품·기존 시험 쓰기 금지, 신규 Persistence 시험·자기 근거만 허용했다. 최초 build/test/WSL은 공식 질문 뒤 별도 창이며 현재는 읽기/작성 단계다. 현재 SQL 검증자는 별도 Task `task_ce97eaa9e5c8`로 최종 판정 작성 중이다. Core 신규 SQL 자원은11:56:16Z 정리됐고 리드12:01:34Z 정확 부재/14334 해제를 확인해 World 생성기1회에 다음 순서를 넘겼다. Content/Art의 후속 실행은 각 리드 조율로 진행하며 이 작성자는 큰 실행을 끼워 넣지 않는다. 아직 선행 시험/구현·최종 독립 판정 결과는 없다.

## 선행시험 생존 신호 관측 — 2026-10-14

공식 원시 `check-after-sql-close.json`의 msg_ae71f3ae0c04(12:05:51Z)→`check-after-sql-settlement.json`의 msg_e4d1a61a0adb(12:12:26Z)는395초다. 현재 Task/Dispatch/from이 일치한다. 리드는 msg_18c3515fc4d9로 기존300초 유지와 해당 구간 실제 작업/영향 원시의 context/report 기록을 전달했다. 메인 msg_af606189f271의 오늘 한정 기준은600초 이하·생존만의 문제·영향 증거 없음·금지 창 실행/쓰기 없음이 모두 확인될 때만 적용하며 현재는 확인 대기다. 사용자 결정이나600초 주기 허용으로 바꾸지 않는다. 첫 WSL/build/test 공식 ask/자원조율은 아직 남고 작성은 계속한다. SQL 검증자는 정확 pane 종료·자원 반환을 마쳤으며 World H1 정산 뒤 Content→Art 후속조율 순서다. Core 신규 중량 실행·예약0이다.

## 공통 API 표면 준비

선행 작성자 msg_3ca207995263의 v1을 리드가 읽어 ReadAdmissionResult의 static Probe와 property Probe 이름 충돌을 발견했다. msg_75a846ca32c2로 property를 Admission으로만 정정했고 msg_0e3c3dd202b1이 v2를 보고했다. 리드 직접 SHA2561201A711047801BCDB0F3F1B4E8F1F3070FF72A4108BF9541F97954A31A4D215 일치 확인. 의미/상태 경계는 바꾸지 않았고 v1은 pretest/raw/api-surface-v1.md에 보존됐다는 작성자 보고다. 최종 선행시험 종료 뒤 같은 표면을 A/B에 함께 전달한다. 현재는 작성 중이며 build/test/WSL 실행0 보고, 제품 구현자는 아직 기동하지 않았다.

## 첫 선행 RED 실행 질문·자원 차례

신규시험10파일 쓰기 종료 후 공식 ask msg_0d164c01e6eb(12:32:30Z)가 왔다. 첫 실행 A는 이 checkout 전용 WSL clone에서 SDK10.0.301의 sync-wsl.sh test와 Persistence 필터1회다. 예상 제품타입 부재 compile 차단과 시험 실행0을 구분한다. 별도 시그니처 throw stub compile B는 계약 밖이라 제외했다(msg_cd2caa2943d6). 실제명령/입출력/exit/PID/시각·전용lock·종료를 남기고 일반 Linux /tmp 대신 허용TEMP 아래 wsl 경로를 wslpath 확인 후 프로세스 TMPDIR로만 전달한다. 실행 상한은 GNU timeout TERM900초/kill-after30초이며 helper제품/전역환경/전체WSL 종료는 금지다(msg_ed9952bd5db3).

현재 실행 허가는 아직 없다. Core 중량실행/예약0으로 Art 최초 반입을 먼저 두었다(msg_99a8b6355662). Art msg_47ef4a15333a의 release 창은 UTC12:44:48Z~13:29:48Z(새시작마감12:54:48Z)이며 해당 통지 시 실제시작은 미확인이었다. Art 실제반환·최신파트조율·fresh관문 뒤 같은 Core ask에 정확 창을 회신한다. 만료시각만으로 자원반환을 추정하지 않는다.

## 선행 요구시험 인수·첫 실제 진입·세션 정산 — 2026-10-14

worker_done msg_9d1ca9e3ff7f(13:07:58Z)의 선행시험10파일·2155줄/메서드92·사례160(작성자 inventory), API v2 SHA1201A711…D215를 인수했다. report.md SHABC6300E7F34D1B3E44A800C8352FCFC69D53E2BB933600464B950805F0B114B0·context.md SHA8A40275E…94EA와 실제 준수 갱신/원시를 리드가 읽었다. 아직 제품 구현/독립 PASS가 아니다.

Art 실제반환 msg_55ca50d6650a와 최신 충돌없음 msg_6910b5179925, Content msg_6c3bfcddd150/World msg_d0788dcaa021/Rules msg_c5bf510456ed를 대조했다. 공식 reply msg_5987232e9ac0의13:03:32Z~13:23:32Z 창(StopNew13:05:32Z)에서 test1회13:03:51~13:04:07Z exit1, 제품API 부재 고유94 compile오류·기존파일오류0·실행test0이다. 본문 compile 정합과 assertion 통과/실패는 미관측이다. 리드가 raw/red의 자기PGID/root/dotnet0·lockfree·nativeTMP leaf 부재·시험10hash불변을 확인해13:04:09Z 실제반환을 네파트에 통지했다. TMPDIR는 msg_8cff506f0a40/보충 SHA96C69FE4…3B2C로 native sibling exactleaf와 process-only UseSharedCompilation=false로 바뀌었다. 이전 drvfs 계획은 대체됐으며 전역설정/helper 변경0이다.

정본 생존helper 원시는26신호/max1063초/초과395·1063·652초이고 자동 questionWaits0이다. 마지막두구간은 intervening status 때문에 helper가 제외하지 못한 공식blocking ask를 포함한다. 실제 CLI시작/timeout/회신을 대조한 ask밖 시간은 각각93초/112초로 계약의 공식대기 제외를 적용한다.395초는 운영미준수로 남기며 당시읽기/설계 보고·첫쓰기시점/실행0·영향 증거 없음에 Main msg_af606189f271의 오늘한정 기준을 적용했다. 근거 pretest/lead-liveness-messages.json·lead-liveness-result.json·lead-settlement.md. helper PASS나600초 주기 허용으로 바꾸지 않는다.

완료한 선행작성자를 release(external_terminal/retained) 뒤 정확pane close해 ptyKilled=true·부모정상을 확인했다. 재사용하지 않는다. 다음은 동일 고정 API/시험을 신규 Astra A(값·인터페이스)와 B(owner 핵심)의 겹치지 않는 계약으로 나눠 구현하고, 쓰기 종료 뒤 신규 Opus 한 명의 강 검증이다. 실제DB/게임통합은 계속 범위 밖이다.

## 신규 Astra A/B 구현 발행 — 2026-10-14

선행작성자 정산 뒤 같은 checkout에 terminal create로 신규 두 세션을 열었다. 최초 명령은 각 process TEMP/TMP를 .backups/tmp/rta·rtb로 지정한 gpt-6-astra xhigh이며 화면/경로/branch·tui-idle을 확인했다. backendunknown이다.

- A - 값·요청·결과·저장소 계약: task_1ec9da6a571b / ctx_9ae06c0e8223 / term_b05e096f-4579-4fb0-930d-58a5ba52c95d, incarnation a587e668-744f-48b5-a3da-2b60ba2b9d23. 계약 SHA32F2AAA08F83C282A58D10C3979EB80C3F6C9E9B073F9031397981DC7F2EDF11.
- B - 저장 작업 수명: task_de733c6afaf3 / ctx_05da3ac97420 / term_5c410018-c73d-42e1-afc6-43feb3a9fece, incarnation6d727a4c-4334-4efa-8e1d-4a3af952efdb. 계약 SHA642555C2C73C8D9DCD5BD4C5CB6137FC4666A354900BB9C4B7B982E65165E6D0.

두 worker-start receipt는 모두 ready/input_accepted/turn_started다. source 계약은 별도 파일에 규칙 원문6절·소유·요구·실행 경계를 두고 최초 작업에는 정확 경로와 SHA를 전달했다. 각 inputs34개/누락0·새 출력 기존0·상위폴더존재를 기계확인했고 A/B 소유목록 교집합0이다. 허용목록 A44/B13은 선택적 helper를 포함한 쓰기 경계이며 실제 생성파일 수가 아니다. namespace/API v2/시험10파일은 공통고정이다.

현재 작성·정적검사만 허용하고 두 writer가 끝날 때까지 build/test/format/WSL/DB/Unity/7777/새dependency 실행·제품통합은0이다. 시험결함은 구현자가 임의수정하지 않는다. 후속 신규Opus는 두 제품변경과 시험의 본문compile/행동·기존회귀·서식/규칙을 강검증하며 현재제품완료/단계2·3전체완료로 표시하지 않는다. 메인 통지는 E2/send-main-implementers-started.json이다.

## 공통 API 보충1

A 공식질문 msg_72c05c4ce435를 기술명세3.2/3.3/5.5와 대조해 기존의미안의 추가getter·recoverydiagnostic값을 리드가 [보충1](api-surface-addendum-1.md) SHA988E3CC4F5315DDF4BE88C8DF044D0AED76F3BD067E6439C90463FFB2220CACF 로 고정했다. 원래API/선행v2/시험/초기입력은 불변이다. Recover payload의 관찰LastSequence 제외·이전owner/새owner 분리·absence/부분값/역사와현재분리를 명시하며 A단일writer/B소비만 유지한다. B의 미발행대기NotDispatched 정산·별도final처리 정책도 원래미고정범위안으로 확인했다. 독립검증에서 보충과 실제diff를 대조한다.

## 구현 인수 (2026-10-14)
- A task_1ec9da6a571b/ctx_9ae06c0e8223, B task_de733c6afaf3/ctx_05da3ac97420의 실제 worker_done·report/context를 대조했다. A44파일1383줄, B12파일887줄(중간893에서 쓰지 않는 참조6줄 제거). 모든 제품/고정 입력 재hash A78/B46개 불일치0, 원시는 각 implementation-a,b/lead-current-hash-check-corrected.json이다. 최초 리드 집계식 오류 원시는 별도로 보존했다.
- API 보충1 SHA988E3CC4F5315DDF4BE88C8DF044D0AED76F3BD067E6439C90463FFB2220CACF 유지, 선행시험/기존 추적파일 변경0이다. A/B 정적 검사는 실행 시험이 아니며 통합 compile·같은 RED명령 시험·독립 강 판정은 미실행이다.
- 공식 생존 helper 최대 A217/B233초, 초과0. 정확 idle/incarnation 확인 후 각각 release→terminal close ptyKilled=true, 재사용0. 근거는 implementation-a,b/lead-settlement.md와 lead-worker-release/lead-terminal-close.json이다.
- SQL 카드 실행 전 검증을 우선하며 Core 동시 검증자1 기준으로 이 goal의 신규 Opus는 다음 순서다. 최종 제품 판정/PR는 아직 미완료이며 메인 msg_4c94daf40a1d로 진행 상태를 전달했다.

## 동결 뒤 정리
- 현재 이 결정으로 이관 확정한 지적은 없다. 이후 독립 판정의 동작 영향 없는 지적만 원래 번호·근거·후속 내용을 남긴다. 동작 결함과 위장 시험/측정은 여기에 숨기지 않는다.
현재 계약 적용: A/B 제품 쓰기는 이미 끝났으며 신규 통합 독립 검증은 아직0회다. 다음 계약부터 시연 우선 모드의 비동작지적 후속·운영미준수 공개·재검증1회상한·새 준비장치 추가금지를 적용한다. 실제 API/소유수명 동작의 독립 실행과 구현/검증 분리는 유지한다.

## 오늘 밤 Core 우선 적용
메인 판단 msg_eb86e55c3e55(2026-10-14T14:38:34Z), 사용자 원문(메인 pane Enter 제출, 23:4x KST): 「우편함 처리와 리드 감독을 나 자는사이에 계속 처리해줘, 목표는 마일스톤 작업을 향해 작업하는거야 / 특히 Core쪽 진전이 별로 안돼서 그 부분을 우선적으로 지시해줘」. 메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다.
A/B 신규Opus 통합 강검증을 지금 열고 SQL#1 수정과 병행한다. 검증 통과하면 하나의 PR/head/CI를 메인에 보고하고 병합은 사용자 정확head 승인뒤 메인만 한다. WSL은 직전물리여유6GiB 이상·다른파트WSL중량과 비중복일 때 Unity순서를 기다리지 않고 실행하며 충돌시Core우선이다. SQL컨테이너는Unity와 겹치지 않는다. 다음실제DB/로딩게이트/재접속 goal은 리드초안만 쓰고 메인승인·현재두작업종료뒤 착수한다.
- 최초 통합 강 검증 발행: 신규 Opus task_3a9faf50660d/ctx_aede046055fd, term_325ca668-c9ca-4eaa-8caf-4b4a7bd408f3/incarnation98093004-41a0-4cb0-b90b-e1632341ccc2. 지정/화면 Opus5.5 xhigh/backend unknown, ready/input_accepted/turn_started. verification-1 계약 SHA316AEB1462E372FB52C98DBA957AE2BC4CDF798E4F69C2E11AC76D271627683B·92입력/부재0. 기존도구·실제API·동일RED필터와 관련회귀/서식 검증이며 WSL직전관문·리드공식답으로 시작한다. SQL#1 구현과 병행, 파트검증자1명이다.

## 다음 goal 초안 작성 주체 변경
메인 msg_922cbf57815f(2026-10-14T14:49:04Z)이 전달한 사용자 원문(메인 pane Enter 제출, 2026-10-14T14:44:08Z/23:44 KST): 「그리고 영속성 부분은 너가 더 전문가니까 Goal 초안 작성하고 진행까지 계속 진행해줘, Core쪽 진도를 좀 빠르게 뺴보자」. 메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다.
다음 실제 DB 연결→로딩 게이트→재접속 직업 복원의 goal 초안은 메인이 작성한다. 앞선 msg_eb86e55c3e55의 리드초안 항목을 대체한다. 리드가 이미 작성한 lead-next-goal-draft.md는 원문 그대로 보존하며 더 쓰지 않는다. 메인 초안 수신 뒤 goal.md에 옮기고 코드·스키마·명세와 어긋나는 곳만 회신한다. 현재 A/B 통합검증과 SQL#1 좁은수정은 계속한다. 메인은 dh.CharacterAuthority의 slot1 PK/CHECK와 계정이름별 UUIDv5 로그인 차이를 초안에서 다룬다고 통지했다.

메인 시각정정 msg_d8a5ccb79390: 초안작성주체 msg_922cbf57815f와다음goal승인 msg_fcb2a973fede의 사용자원문시각은2026-10-14T14:44:08Z/23:44 KST다. 내용은그대로이며메인초안 최신SHA055A04FF697EC14A0DF17B4D1656777D8F54717BC04ACE5135CAC03A18DFBCDE를적용한다.
## 통합 검증 실행 허용
- World 실제반환 msg_9ede6461809f 뒤15:10:18Z 리드 fresh 물리15531999232/commit32684462080bytes·WSLdotnet0을 확인했다. 공식ask msg_8f735a2d3d5d에 reply msg_230b201bdaa2로 「지금1회시작」을답했다. 실제시작은이허용시점에미관측/예약1이다.
- 선행10무수정동일Persistence필터1회→필요독립반증보완/동일필터→GameServer.Tests회귀→기존서식검사다. 실제DB/SQL/Unity/7777/DLL역복사0, 새준비장치0, 각실행직전fresh관문과자기정산을유지한다. 원시 verification-1/lead-pre-release-resource.json·lead-official-execution-reply.json.

## 통합 검증 최초 실제 결과
- 실행(1)15:10:54Z→15:11:11Z exit1. SDK10.0.301 restore/build 성공·compile오류0, 선행160시험159통과/1실패/skip0. 실패는 Acquired.Code null기대와실제Acquired의차이로 원문분류중이다. API부재 RED94컴파일오류/시험0과 이번실제159통과를구분한다. 현재전체독립판정은진행중이다.
- 15:11:13Z 자기pgid/root/leaf프로세스0·workspace lock반환·native TMP제거/부재를 raw/run-1에서확인했다. 다음독립반증/동일필터·회귀/서식을같은허용으로진행하며 Core WSL예약1·새진입전fresh관문을유지한다.

- 독립시험추가실행(2):226개224통과/2실패(선행A.Code·독립B미발행class불일치status),compile오류0.15:13:31Z정산. 전체회귀(3):1286개1279통과/2동일실패/기존skip5·기존회귀실패0,15:16:15Z정산. 최종요구근거/차단판정은아직대기하며제품쓰기0이다.
- 기존format-check1회는15:19Z시작,직전물리16689304KiB/commit33629184KiB·WSL/Windowsdotnet0을검증자가확인했다. 현재전체독립판정/쓰기종료전이므로새검증기동0·SQL실행0을유지한다.

## 최초 통합 독립 판정 정산
- Opus task_3a9faf50660d/ctx_aede046055fd의 done msg_61b3db28160a·verdict/context 전체를인수했다. lifecycle succeeded는검증수행완료이며제품판정은차단#1/#2다. verdict SHA BA6163160225930C47737A75995611CB429B0B49FB3E5744CF0F89E42E275EC3, context B80788FB7B298E211635AD663B05742F064E6658A647F4BAD3740284A5F32575.
- #1(A,낮음): v2 api-surface52의상태별property shape에맞지않게 runtime Applied결과Code를채운다. #2(B,낮음): 저장소미발행class불일치가NotDispatched 대신Rejected를반환한다(technical-spec6). 실제시험2실패로차단, 데이터손상/현재저장권위영향은관측되지않았다. Recovery결과내부표면은A선택범위이므로확장해고치려면별도계약판정이필요하다.
- 제품56/선행10 무수정·독립시험4개추가. Persistence226중224통과/2실패,전체1286중1279통과/2동일실패/기존skip5·기존회귀실패0. formatter409입력/format지적0·Formatting.Tests244통과·exit0. 실제DB/Host/게임/Unity/CI0이다.
- 생존helper max226초/초과0. 정확idle/identity→release→close ptyKilled=true,재사용0,정산원시 verification-1/lead-settlement.md·lead-worker-release.json·lead-terminal-close.json. 전체WSL/Core예약0·SQL0을World/Content에반환했다.
- 최초검증이끝났고재검증0이다. 밤우선지시대로메인에원문과좁은수정안을올리고새검증을더열지않는다. PR/head는제품판정해소뒤다.

동결 뒤 정리 인계:
- O1 - 신규분석기warning11개 선언순서/중괄호. 독립verdict의정확11위치를따라동결뒤정리하며현재비차단이다.
- O2 - FEATURE_MAP의Persistence 진입점링크. 문서탐색지적·현재비차단.
- O3 - receipt의progress token필수여부를계약에명시할후보. 현재v2는요구하지않고current대조fail-closed,동결뒤판단.
- O4 - 미발행직전deadline에서unfinishedID가보수적으로남을수있음. 실제위험미측정·현재비차단관찰,동결뒤판단.
- O5 - final checkpointclass불일치면release없이RecoveryRequired가남는정책. 현재안전거부방향이며동결뒤판단.
- O6 - 미생성선택PersistenceWorker 소유목록표현. 기록만.

## A/B 좁은 수정 승인과 발행
- 메인 판단 msg_b2124c0c72b9(2026-10-14T15:41:26Z): runtime결과의Applied/FenceLost Code를null로맞추고receipt.Code/Rejected·NotApplied·Conflict reason은유지한다. owner의RPC미발행class불일치는NotDispatched로바꾸며새상태전이를추가하지않고Settle미경유의미를보고한다. recovery는범위밖선택표면이라동결뒤계약해석후보로둔다.
- 신규Sol xhigh task_a4f4d0b8e5d1/ctx_dfa5fc9b3afe,term_e4f13373-2a9e-4794-87c1-96237c2dfcd4/incarnation3441c284-b3b5-4ee2-953a-dec82c937d9f. 지정/화면GPT6.1Sol xhigh/backend unknown·ready/input_accepted/turn_started. correction-1 계약 SHA655B86FB2DBA300E584533D24550C30C5314F7D19704DF1546AE4CA09F0E3AF1·85입력/누락0,793줄5파일중정확반환구간만쓰기·before5사본보존.
- SQL신규Opus재실사1회 task_6d73e82a96fd/ctx_bfe75abd1aad와서로다른파일에서병행한다. 그검증정산뒤A/B 신규Opus재검증1회다. 제품시험수정/실제compile·WSL·DB·새하네스0·recovery수정0을유지한다.

동결 뒤 정리 추가: recovery 결과 Code의nullable/Applied표면 해석 확인. v2에서A선택권으로둔표면이며현재#1runtime수정에자동확대하지않는다.
## A/B 좁은 수정 완료 정산
- worker_done msg_4ee2a9c29e68(2026-10-14T16:06:45Z), task_a4f4d0b8e5d1/ctx_dfa5fc9b3afe. 승인5제품파일의 반환7줄만 변경했다. runtime Acquired/Durable/CheckpointNoChange/Released/Applied/FenceLost Code=null, owner class 불일치 RPC 미발행은 NotDispatched다. Receipt.Code·기존 flags/전이·recovery 표면은 그대로다.
- 리드가 report/context/정확 diff 원문과 실제5파일을 대조했다. correction-1/lead-hash-audit.json은 입력85/불일치0·승인변경5/다른80보존·사전 사본5불일치0이다. 시험14/다른제품51도 보존했다. 수정 후 실제 compile/test/format/WSL/DB/게임 실행0이며 독립 PASS를 주장하지 않는다.
- correction-1/lead-settlement.md에 원문hash·실제 준수·생존 신호4/max292초·external release→정확 idle/incarnation→pane close ptyKilled=true→reclaimable0을 기록했다. 작업자는 종료하고 재사용하지 않는다.
- SQL 카드 재실사1회 정산 뒤 신규 Opus A/B 독립 재검증1회다. 같은 산출물 재검증1회 상한과 다시 차단되면 메인 판단을 유지한다. Core WSL/DB 실행·예약0; World99 WSL 허가/예약1을 알고 새 Core WSL은 별도 조율한다.
## A/B 유일한 독립 재검증1회 발행
- 신규 Opus task_9c0fe16a9c27/ctx_61c4983cd70d, term_944eaabd-7300-4d0f-bd3b-356ae034b4e7/incarnation c432132b-c34e-4304-aceb-b03f2e499d4d. 최초 명령 claude --model claude-opus-5-5, 화면 Opus5.5 xhigh·ClaudeCode2.1.299, backend unknown. SQL 카드 재실사 정산·pane종료 뒤 유일 Core검증자다.
- reverification-1 계약 SHA256 A796319C5B83E15DD279893B89626F2616F7229DB7C2191B64A2FAA2AC6124CE, 입력112/누락0/출력부모 확인, firstscreen 무권한modal·ready/input_accepted/turn_started 관측. 기존 제품56·시험14의 입력을 고정했다.
- PersistenceResultShapeTests.cs의 #1 runtime factory Theory만 독립 추가한다. 제품56·다른시험13·기존 기대/본문·원시·goal은 readonly다. #2는 기존 class불일치 미발행 시험과 실제source/diff를 대조한다. recovery표면/새상태전이/새준비하네스·시간장치0이다.
- 현재 정적 실사/시험준비만이며 실제 WSL/compile/test/format은 정확계획 공식ask→fresh물리6GiB/commit4GiB·다른WSL0·리드 지금1회시작 뒤다. 기존 wrapper 파라미터만 재사용한다. Persistence범위1회 통과 뒤 관련 전체회귀1회·이번6파일의 좁은서식/규칙을 수행하고 새 근거 없이 기존12.5분 전체formatter를 반복하지 않는다.
- 같은산출물의 독립 재검증1회 상한을 사용한다. 다시차단되면 새검증0/메인판단이다. 통과 시 현재goal/API·제품56·시험14만 PR에 올리며 승인된 다음goal문서는 이번PR에서 제외한다. WSL반환 뒤 승인된 별도G2 실행 순서를 유지한다.
## A/B 재검증 실제 실행 허가
- 공식ask msg_121d1aaa5175(16:41:07Z)의 준비를 실사하고 reply msg_0a3efa6ba3d3(16:58:30Z)로 Persistence1→통과시 기존 전체solution회귀1→통과시 이번6파일 narrow whitespace1을 순차 허가했다. #1 독립Theory11사례가 기존시험1파일에 추가됐고 다른시험13/제품56은 보존 보고다. 리드가 실제Theory·wrapper파라미터diff·narrow명령을 읽었다.
- lead-memory-at-permit.json은fresh16:58:29.9191119Z 물리16588439552/commit여유35017248768 bytes다. WSLdotnet/testhost/MSBuild0·WindowsUnity/dotnet/MSBuild0, Art실제Unity반환과World예약0을 확인했고 Core우선WSL허가/예약1을 통지했다. 리드 최초pgrep 인용실패exit127 원시는 보존하고 부재근거로 쓰지 않는다.
- 각900초+kill30초/자기PGID·clone lock·nativeleaf 정산과 시작직전fresh6GiB/4GiB/다른WSL0·실패시후속단계0/자동반복0이다. 허가는actual시작/완료/PASS와 구분하며 원시를 후속 확인한다. CoreG2/SQL/DB0이고 이번WSL전체반환 뒤에만 G2다.
## A/B 독립 재검증1회 통과와 PR 준비
- 신규 Opus task_9c0fe16a9c27/ctx_61c4983cd70d, done msg_b4c21b3b0520. 독립 판정 통과/#1·#2 해소/새동작결함0, verdict SHA256 BAFD093F5FA8CB43B29E85E1BA04C4F1730E8774A188BEA2AF09345DA1958F8E, context SHA256 052064BC9FEE85007FB30404C17916E9775A1D5BFE4A2C75F06A0721868606EC. 리드가 원문/실제source/diff/세run원시를 읽었다.
- 실제 Persistence237/237, 전체GameServer.Tests1297중1292pass/0fail/기존skip5(전체slnx의 시험프로젝트1개), SDK10.0.301·compileerror0/기존고유warning18/새시험warning0다. narrow6파일 whitespace exit0/report[]와 실제matching 수 미출력/추론 한계·6파일bytes정적검사 통과를 같이 둔다. DB/게임/Unity/Host 연결/CI는 이 판정에서 미실행이다.
- reverification-1/lead-final-input-audit.json은112입력의 허용시험1변경·제품56/다른시험13 변경0이다. 추가104줄을 뺀 원본SHA574AEAD08DF1145B35F919EBDA5154457455014795FE7414723DF24C1A173E60 일치. lead-settlement.md에 실제준수·생존공식reply 보완·예방240초초과/ask대기·실측한계와 release→정확idle/incarnation→close ptyKilled=true→reclaimable0을 보존했다.
- 현재goal/API3문서·제품56·시험14를 PR1개에 올린다. 승인된 2026-10-14-persistence-runtime-entry/goal.md는 이번PR에서 제외한다. 병합은 메인 전용checkout에서 사용자 정확PR/head승인 뒤만이다. Goal전체종료/Gardener는 병합·실제후속상태를 정산해 별도로 수행하며 PR준비를 마일스톤완료로 확대하지 않는다.

동결 뒤 정리 추가:
- R1 - recovery 결과 Code의 계약 해석 확인. 메인 msg_b2124c0c72b9에서 이번 runtime 수정 범위 제외. 기존 후속에 연결하고 지금 제품 변경0이다.
- R2 - runtime 결과 Code(거부)와 Receipt.Code(이력)의 공개 property 설명. 결과4파일 Code property에 짧은 설명 후보, 동작무관/비차단.
- R3 - v2 API표면에서 ConflictCode 유지가 메인 결정으로만 명시된 부분. 다음 API표면판에 결정반영 후보, 동작무관/비차단.
## PR240 - 저장소 계약과 저장 작업 owner
- 제품56·시험14·현재goal/API3문서, 총73파일/5639추가행으로 최초 commit8a81825ef8154a1f285e5f9c0df13d56b8837710을 push하고 PR240을 열었다: https://github.com/bass131/dawnholder-server/pull/240 .
- 테스트된70 C#파일의 staged blob과 작업파일 raw blob을 lead-staged-source-audit.json에서 대조해 불일치0을 확인했다. 이 절과 현재상태의 PR연결 기록은 리드의 문서 정산이며 제품/시험 변경0이다. 최신 정확head와 CI는 PR실제상태/로컬lead-pr-view 기록으로 확인하고 이 goal에 순환 head값을 덮어쓰지 않는다.
- 승인된 다음goal 폴더는 untracked로 보존했고 PR에서 제외했다. 병합은 메인 전용checkout과 사용자 PR별 정확40자head승인만으로 한다. Codex 병합/자동병합 예약0이다.
