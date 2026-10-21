# 캐릭터 목록·생성·선택 서버 처리

로그인한 계정의 캐릭터 세 칸을 조회하고, 이름과 직업을 정해 만들고, 고른 캐릭터로 마을에 들어가는 서버 처리를 연결합니다.
이름 검사와 실제 저장은 Core 기능을 사용하고, 화면 제작은 Content가 맡습니다.
선택한 캐릭터의 직업은 저장된 서버 값으로 결정하며, 같은 계정의 길드 가입 보상이 중복되지 않게 유지합니다.
Core가 저장 기능을 검증하는 동안 전송 형식과 대역 시험을 준비하고, 실제 연결 뒤 서버·Unity 양쪽을 검증하는 안입니다.
Core 준비 시점부터 약 3~5작업일을 예상하며, 10-23~24 연결 확인과 10-28 기능 동결을 목표로 합니다. 일정은 추정입니다.

**상태: 오류 매핑 선행 실행·전수 분류 완료, Core 최종 소비 코드 인계 대기.** 새 실행은 빌드 오류0·경고57, 실제 TRX589=기존407P/신규182제품부재RED/skip0이다. 신규182는 mapper 존재 첫 단정에서 멈춰 Core 의존·서명·의미·wire는 미도달이다. 전송217·Unity 실제session313의 과거 GREEN은 유지하며 실제 Core bridge·저장·서버 입장·Content/Guild 통합·PR 전 전체 독립 강 판정은 남아 있다.

## 재개 지점

**다음 첫 일: Core의 최종 소비 코드 인계·검증 기준과 정확 파일별 쓰기 이전 확인 → 현재 기준 입력 고정 → 신규 Sol max에게 실제 Core enum을 받는 좁은 오류 mapper 구현 계약.** Core `msg_518e1d92bcf6`는 APIv2/소스12개를 읽기 전용으로만 제공했고 최종 소비 가능 코드 인계는 아직 없다고 명시했다. 현재 World에 Core catalog 타입·mapper가 없으므로 미검증 복사나 가짜 Core로 이 선행조건을 우회하지 않는다. 후속은 mapper 한 책임부터 시작하고 서비스 adapter·tick/generation·handle Close·54→48→기존 입장은 별도 좁은 계약으로 잇는다. 이 goal은 진행 중이며 제품/전체 검증 완료나 병합이 아니다.

이번 실행 근거 E는 `.backups/verification/2026-10-18-character-server/approval-01/catalog-failure-mapping-pretest-03/`다. 새 세션이 Main `msg_832c4db2b8a5`로 같은 Run `run_90d18490c825`를 현재 handle로 인수한 뒤 새 Opus 실행자에게 제품·시험 readonly의 선행 실행을 발행했다. 현재 Task/Dispatch·모델/화면·고정 SHA와 정산 원시는 E의 `runtime-binding-v1.json`, `lead-worker-start-v1.json`, `lead-settlement-review-v1.json`을 따른다. 이전 handle·Task·창·clone·prepared/approval은 역사이며 다음 실행 권한이 아니다.

- **실제 결과:** 기존 Capture A→B→C→D를 정확1회 실행해 A0/B0/C1/D0·retry/kill0이었다. 빌드 오류0·경고57로 T1/T2 컴파일 결함이 해소됐고, TRX589=407P/182F/skip0을 실제 발견했다. 기준6클래스407의 fullname/결과 누락·추가·변경0, 신규182의 모든 message/첫frame은 `CharacterMenuCatalogFailureMappingTestSupport.cs:25`의 mapper 타입 부재 RED로 일치했다. 뒤 Core 의존·서명·의미·wire 단정은 미관측이며 의미 통과/실패나 실제 회귀로 세지 않는다. 원문 `report.md`·`metadata.json`, 리드 전수 대조 `lead-trx-review-v1.json`과 실행 파일 `lead-review-trx-v1.ps1`에 연결했다.
- **반환·정산:** D 자원/runtime0·동일lock 재획득·source/Plugins742·backup742·prepared580·승인16 보존을 먼저 읽고 Main `msg_16346dade28d`로 TRX 해석 전 창을 반환했다. 전체쓰기종료·정확 worker_done·보고/감사118 SHA차이0을 대조하고 completed·identity·idle→release(retained/external)→정확pane close(ptyKilled true)·리드 생존을 확인했다. 열린 World 작업자는 없다. 기존 helper의 보통 신호 간격 초과0이며 공식 질문 대기를 별도 분리했다(`lead-liveness-v1.json`). succeeded는 실행·분류 완료만 뜻하고 제품 PASS/전체 강 독립판정이 아니다.
- **보고 정정:** 작성자의 경고 상세 parser가 대문자 코드만 추출해 xUnit11종을 누락했다. 원문92발생/46종을 전체114발생/57종으로 원시 재집계했고 기준 실행도 같은57종·신규/삭제0, 두 보완 시험 진단0이었다. 원문을 보존하고 `lead-diagnostic-correction-v1.1.json`·`lead-review-diagnostics-v1.1.ps1`에 정정했으며 불일치는 Main `msg_3e42988c378c`로 즉시 보고했다. 빌드 요약57warning/0error와 TRX 결과는 바뀌지 않는다. 이번 첫 관측은 결과/근거 기록으로 남기며 새 공용 규칙·도구를 만들지 않았다. 리드 자체 기록 형식 교정도 `lead-settlement-review-v1.json`에서 원문과 연결했다.
- **Core 접점:** `msg_dc0e289ccd6d`로 현재 Run/Task·소비 상태를 보냈고 Core 고정 note/manifest SHA와 실제12파일 차이0을 확인했다(`lead-core-fixed-source-review-v1.json`). 최종인계 보류·Program/ServerHost/RuntimeStartup/AdmissionServices/Lifetime Core 소유·World session/menu 책임을 유지한다. `msg_2392c1cdb5cd`로 선행 RED·다음 좁은 계약 순서를 회신했다. 실제 Core API/DB 저장·재접속·실패 수명·새 wire/입장 통합은 남아 있으며 runtime19의 SQL 인증 기동 확인을 전체 Catalog/World 통과로 확대하지 않는다.
- **다음 경계:** 이번 원본 제품·시험 변경0, DB/7777/Unity/DLL배포/역복사/설치/전역변경0, push/PR/merge0이다. 이후 실행은 현재 HEAD/goal·새 Task·고정 입력·새 Main 창으로 별도 승인하며 이번 approval을 다시 쓰지 않는다. Core 최종인계 전에는 읽기 전용 계약 조율만 진행하고 리드는 우편함 wait/sleep/반복 조회를 열지 않는다.

### 2026-10-21 재실행 전 인계 — 당시 상태

**다음 첫 일: 창이 열리면 메인이 연 새 리드 세션에서 고정 시험 SHA 재확인 → 새 실행자/현재 기준 입력 → A→B→C→D.** Main `msg_80a517f29947`의 사용자 결정에 따라 창을 기다리며 세션을 유지하지 않는다. 현재 시험 보완 작성자는 정산·종료했고 리드는 이 재개점과 사용자 결정 원문을 local commit한 뒤 메인에 종료 준비를 보고하고 턴을 끝낸다. 리드 pane은 메인이 idle을 확인해 닫는다. 다음 진입은 **같은 대화 이어 붙이기가 아닌 새 세션**이며 같은 Run `run_90d18490c825`를 현재 handle로 인수한다. 이 goal은 진행 중이며 완료/병합이 아니다.

근거 W는 `.backups/verification/2026-10-18-character-server/approval-01/catalog-failure-mapping-testfix-01/`다. 같은 노트북의 Git 제외 근거를 보존하며 이관은 필요 없다. 최종 local HEAD/goal SHA/현재 상태는 W의 `lead-window-wait-handoff-v1.json`과 실제 Git에서 확인한다.

| 고정 인계 파일 | SHA256 |
|---|---|
| `ready-plan-v1.md` | `2C78076F3F8EAC1F3DC175C406BD14D69801B62280839758A2F4F5CFB747C0D7` |
| `ready-hashes-v1.json` (승인 대상14) | `12EDDF2359CD8F5E614919AEF2A3464E1CDADE141B925B88525B2719D802D780` |
| `prepared-inputs-v1.json` (575, 당시 기준) | `29E292B0EA2CDBFC8B760C52E105503663F56393719E97411110F24688BE17FF` |
| `static-settlement-report-v1.md` | `DEAD6C4D384830121ECAAFC35E5FE796EDBADED23CECB5423C670531D98B80E5` |
| `static-settlement-metadata-v1.json` | `95BF7E39CB156EC0FAD33FD86F5ADFA4B3E170372B6B3097647A00F3D721918E` |
| `lead-settlement-review-v1.1.json` | `F56809970D2605F5530BED43E57052B0AEF452DB4C6D9A08C16576B8E7914F99` |

- **수정과 실제 상태:** 새 선행시험 작성 Task `task_a9ab435a7e81`/Dispatch `ctx_c10a18413474`가 T1(get 안 `field`→`coreField`, 39·41줄)과 T2(switch8갈래 원소 `(decimal)` 명시,77~84줄)만 두 파일 +10/-10으로 보완했다. 역변환하면 두 전체 원본 SHA가 복구돼 이름·입력표·기대값·literal18·factory 전 단정·실제 typed Core 접점·mapper 먼저 부재 단정·undefined 탐색이 보존됨을 리드가 직접 대조했다. 수정 뒤 Tests SHA는 `5B5A454DF61579F339C234BB775DCD129AAEBA008B744FC14BD76E40C7B916AF`(208줄), Support SHA는 `7579A0474A4ED4EF599DE2E250AC1A10CEE49E097EB615A523CC40940459F21E`(120줄)다. 경로는 `02_Server/GameServer.Tests/Network/CharacterMenuCatalogFailureMappingTests.cs`와 `CharacterMenuCatalogFailureMappingTestSupport.cs`다.
- **미실행 경계:** 수정 뒤 caller/backup/clone/WSL/dotnet/build/test는 모두0이며 compile·경고·발견수·RED/PASS/skip/TRX·기존407 전후는 미관측이다. 마지막 실제 실행은 아래 09:04~09:06Z의 컴파일 실패이고 과거407 GREEN을 현재 수정의 통과로 세지 않는다. 신규제품부재RED와 뒤 의미 단정도 아직 미관측이다. Core bridge/실제저장·선택/입장/Content·Guild와 전체PR 독립 강은 남아 있다.
- **다음 새 실행자의 입력:** 위 고정 시험 SHA와 READY의 정적 결과를 인계받되 옛 Task/Dispatch/incarnation/창/lastEntry/clone을 권한으로 쓰지 않는다. 당시 READY는 HEAD `83dad1f65697f1123171a7bffbd3d1a34e0fb190`·두시험 M·당시 goal SHA에 고정돼 있다. **이번 goal 기록·checkpoint commit 뒤에는 이 config/prepared/approval을 그대로 실행할 수 없다.** 새 Task·현재 HEAD/branch/clean·현재 goal SHA로 새 버전 config·source inventory·prepared·승인을 만든다. 기존 Capture SHA `34D390970C1451409D37932735EB8274B1E6E8C97D89B87B1E6D9A4BE4DEED16`와 Bash SHA `EC2AE822B5C3D363C8EA64AB4601039076640535FF34D23C094C2B77784D1EB5`는 그대로 재사용한다. 실행자는 현재 배정표의 신규 Opus 선행 실행자이며 기존 작성자를 재사용하지 않는다. 입력 목록은 구조·count·기계 hash/차이만 확인하고 수백 행·전체 hash 로그·과거 대화를 반복 출력하지 않는다.
- **기다리는 창 요청:** 새30분 WSL 창, lastEntry=start+240초, Unity0·unityOwner없음, owner는 새 실행 Task다. 메인이 새 리드/실행자 준비 여유가 있는 start/deadline/lastEntry를 진입 메시지에 함께 배정한다. fresh runtime/Run 인수·회신주소 통지·시험SHA/현재기준 재실사 후 그 창으로만 실행하며 이전 승인 파일은 없다. A1560/B1440/C1320/D120초·Windows6/4/40GiB·Linux4/12GiB/runtime0·같은lock·SDK10.0.301·새clone·단1회/restore-build-test각1/retry·kill0·원본보존 관문을 유지한다. 같은7클래스(기존407+오류매핑)로 실제발견/TRX·원실패/뒤의미미도달을 전수 분류하고 D 자원/runtime/동일lock/원본·준비SHA를 먼저 확인해 Main에 창을 반환한 뒤 TRX를 실사한다. DB/서버7777/Unity/DLL복사/역복사/설치/전역변경0이다.
- **정산·종료:** Main 창대기 결정 전달 `msg_12cfc73ee709`, 정적범위 축소 보충 v1.1 `msg_f4a48ac34d9f`에 따라 작성자는 READY까지만 마쳤다. 전체쓰기종료09:46:13.697Z/worker_done `msg_8a8f967d7817`, 보고2·증거40·시험2 SHA와 prepared575 차이0, heartbeat11/최대274초/초과0을 리드가 원시 대조했다. 리드 비교의 시각 형식 자동변환 차이는 원시 v1을 보존하고 `lead-settlement-review-v1.1.json`에서 ISO 문자열로 다시 읽어 실제차이0으로 확인했다. completed·정확identity·idle→release(retained/external)→정확pane close(ptyKilled true)·리드생존을 확인했다. succeeded는 **정적 보완·READY 완료**이며 제품/시험/독립 판정 통과가 아니다. 열린 World 작업자 Task는 없다. push/PR/merge는 하지 않았다.

아래 10-19 종료·10-21 첫 재개/실행 정산은 당시 기록이며 현재 실행 권한이 아니다.
- **2026-10-21 재개 실행 정산:** Main 재진입 `msg_14ff7b5796bd`에 따라 origin/main `db0169350dfe54ba6ed247684caae4e498e24c72`를 자기 branch에 받았고 규칙파일변경0·충돌0이었다. 신규 Opus 선행 실행 Task `task_43cf9b016b36`/`ctx_dc45b62aa694`는 제품·시험 readonly로 A→B→C→D를 한 번 수행했다. caller09:04:47~09:06:11Z/83.626초·원exit1·retry/kill0, restore7/비시험프로젝트6은 성공했으나 GameServer.Tests가 새 시험 두 파일의 고유진단3·경고59로 Build FAILED였다. test/기존407 보존·신규제품부재RED/의미·TRX는 미관측이다. D 자원/runtime·같은lock·원본742/준비564/승인14 SHA차이0을 먼저 대조해 `msg_bd71772f2574`로 Main 창을 반환한 뒤 C를 실사했다. 원문은 `approval-01/catalog-failure-mapping-pretest-02/report.md` SHA256 `913A90876D2F3C76C331BEBF51B2206F5AE8298050715B7C98C4A6487164B9D8`, metadata `A2783C5604614F49922021CC223A1956D2A824630AC04D8D9475C329CAD7CCAD`, 리드 정산 `lead-settlement-review-v2.json`이다. 보고12/쓰기감사55/backup742 hash와 작성자 컴파일분류 차이0을 확인했다. **신호 초과0 보고는 원시18개의 실제 간격과 달라 최대318초/초과3으로 리드 정산에서 교정하고 `msg_84379646a7fe`로 Main에 즉시 보고했다.** 원문은 수정하지 않았다. 전체쓰기종료09:11:12Z/worker_done `msg_0eb1dbec4f8e` 뒤 completed·정확identity·idle을 대조하고 release(retained/external)→해당pane만close(ptyKilled true)·리드생존을 확인했다. succeeded는 실행·분류 작업 완료이며 제품/시험 통과가 아니다. 두 시험의 컴파일 결함은 첫 실제 발생으로 기록하고 기존 작성층에서 수정하며 새 반복 규칙·도구는 만들지 않는다. 전체 PR 독립 강은 후속이다.

- **종료 원천과 Task 처리:** Main `msg_f67c2b23de92`(2026-10-18T22:32:57Z)가 사용자 귀환·노트북 종료 전 정리 지시를 전달했다. 오늘 WSL 창은 배정하지 않는다. `task_fb7b455f8af8`/`ctx_f2ce01b6eb0d`는 정적 READY만 완료한 상태에서 `msg_d5072c0251ad`로 정리 지시를 받아 전체쓰기를22:35:43Z에 종료했고 `msg_9d00cfdb53c0`으로 failed 정산했다. 이는 실행 미완료의 사용자 종료 사유이며 제품결함·독립NOTPASS·확정실패가 아니다. release 뒤 정확 identity/완료 idle을 확인해 해당 pane만 닫았다. 열린 World 작업 Task는 없으며 재개 때 새 Task를 만든다.
- **마지막 완료와 보존:** 실행 완료 제품은 `fc2b2302b241e77c46d8422f46d5528f7cafc01f`의 응답 조립/기존요청407P·0F·0skip이며 아래 CI 정산 원문/TRX를 따른다. 현재 두 신규 시험은 +328/-0·제품/기존시험변경0의 미실행 선행 체크포인트로 로컬 commit한다. 준비543/558·원본742 보존을 확인한 기존 실사는 그대로 보존했고 종료 직전 승인13/두시험 hash도 차이0이었다. 이번 정리 commit으로 goal/CURRENT·HEAD·Git status 기준이 바뀌므로 옛 준비파일을 그대로 실행하지 않고 새 버전 입력을 만든다. 옛 원문/SHA/.backups는 수정·삭제하지 않는다.
- **사용자·메인 대기:** 이어갈 머신·근거 이관/원격 전송 여부는 메인 별도 안내를 기다리며 push0이다. 실행 창/시각·lastEntry는 재개 때 새로 받는다. 실제 Core 소비 인계·소유 조율과 전체 PR 독립 강은 남아 있으며 대역 준비를 실제 저장/입장 통과로 해석하지 않는다. 다른 머신이면 Git 제외 `.backups` 근거도 별도로 보존·이관해야 한다.
- **이번 밤 결정 기록:** 사용자 위임 원천 `msg_02a8258a3fc2`, Core 독립 선행 `msg_4dc4f7f5bdd5`, R5 한정 복구 `msg_645c6d8ebd9f`, Snapshot 관찰 보완 `msg_1c4aa90c0a9e`, 응답 조립 실행 창 `msg_3d910fa5eadd`는 이 goal의 적용 결정/단계 정산과 원문에 기록했다. 오류 매핑 창의 Content 우선 대기 `msg_fcc93dae7dd4`/`msg_56558bbe7ced`는 아래 근거의 mail-notification-03/04.json에 보존했다. 이번 사용자 종료 전달 `msg_f67c2b23de92`는 mail-notification-05.json과 이 재개점에 추가했다.

근거 E는 `.backups/verification/2026-10-18-character-server/approval-01/catalog-failure-mapping-pretest-01/`다. 최종 로컬 HEAD/상태·세션 ID·프로세스 확인은 E의 `lead-shutdown-final-state.json`에서 확인한다.

| 고정 산출물 | SHA256 |
|---|---|
| `ready-plan-v1.md` | `B29EE62735808D873BA789C8BA105F5CCE77BD3A3D4872C36F3974257B850AA5` |
| `prepared-inputs-v1.json` (558개) | `B2C501B6BF2EBB549577F98FBC5F40FF74D2244E092E7D65EFC2CFD8F7775D21` |
| `task-spec-v1.0.md` | `B0E99F097846221B994D982D99F7751C0E1DE878BB56B296EB21944F2E78FF55` |
| `manifest-v1.json` (543개) | `1B037DC15C92ED20E8EBCAF310C1F8919568695E346C31A7B53516089C141112` |
| `shutdown-report.md` | `50225AA070C9D34353DD459CC270CBD402AC7EC6A6D679AC97B593199B799DAA` |
| `shutdown-metadata.json` (승인13·시험2 SHA 포함) | `2234D6CC92C68DA99BFD5F88548F4A03C6685178E8C2C55CC2EC3D0139F19E0F` |

보존한 [오류 의미 매핑 시험 계약](../../../.backups/verification/2026-10-18-character-server/approval-01/catalog-failure-mapping-pretest-01/task-spec-v1.0.md)은 실제 Core CatalogStatus/InvalidField 두 enum의 typed API를 시험 helper에서만 늦게 바인딩한다. 가짜 Core enum/namespace·제품 string/object/reflection facade는 만들지 않는다. 직접 오류/사용자 필드·두 선택불가의 동일 외부 표현·Unknown12를 먼저 고정하며 NotApplied.Rejection·생성/선택 수명·실제 bridge는 후속이다. 실제 Core 타입 제품 구현은 검증된 인계 뒤다. 재개 시 Core API v2·최신 main/Content 소유 변화를 대조하고 전체 C1~C9·[전송 v1](packet-contract-v1.md)·[Core 소비 v1](core-consumer-contract-v1.md)·PR 경계를 유지한다.

Core 최신 회신 `msg_9ffeac1a66ca`(21:30:11Z)은 새 소비 checkpoint/최종인계 없음·API v2/HEAD/공개signature·host 소유 불변을 재확인했다. 보고된 SQL5 LocalDB·Resume PS2 독립 대역97/97·기존360PASS는 서버저장API 소비 인계가 아니며 실제 Express/관리자 Resume/서버 저장재접속은 미실행이다. 리드는 이 별도 검증을 재실행하거나 World 통합 통과로 세지 않았다. 원문은 `approval-01/catalog-response-implementation-01/mail-notification-05.json`이며 Core 인계 대기 없이 공개 계약 기반 선행시험을 이어간다.

- **응답 조립 제품 정산:** Astra xhigh Task `task_c4d748307ef8`/Dispatch `ctx_acfc9eb141f7`, 제품 신규1/기존1 +180/-39·시험변경0이다. 원문 `approval-01/catalog-response-implementation-01/report.md` SHA256 `4EB639272F12ADC6BE23E7D1759608FB9830E0ECE367447177EBCBD1EEA838F1`과 실제diff/준수·원시를 직접 읽고841산출물 hash차이0·작성자407행 전후 결과/message·stack 분류 차이0을 확인했다. TRX SHA256 `65336F156D37F642C21E8DFC88A2F8955FFF2F8616D924CE775D61B6562E8AC3`, 동일407P/0F/0skip·기존316 보존/신규91 타입부재RED→의미/byte 단정P·이름 누락/추가0이다. Main `msg_3d910fa5eadd` 창의 caller21:16:10~21:18:23Z/133.035초·원exit0/A0B0C0D0·backup740/restore-build-test각1회/retry·kill0/build57warning·0error다. D runtime0/Unity0/동일lock·원본740/준비834 보존을 먼저 대조해 `msg_77d419e0d130`로 TRX분석전 반환했다. succeeded/전체쓰기종료·release/정확pane종료·리드생존/reclaimable0이다. 신호7/max1994초·초과1/질문대기1900초는 Main `msg_c3029b3391ac`의 Content 독점창 대기와 연결되며 R5 Enter0이다. 절차상 콘솔표시/분류명령 교정과 미실행 한계는 같은 근거 `lead-settlement-context.md`에 보존했다. 실제 Core/DB/Stored/54→48·입장·전체독립강 통과가 아니다.

- **응답 조립 선행시험 정산:** 신규 Sol xhigh Task `task_d819e71aa834`/Dispatch `ctx_6fb00f94b4fa`, 신규시험2파일+513/-0·제품/기존시험0이다. 원문 `approval-01/catalog-response-pretest-01/report.md` SHA256 `E400F8E3EEB979B85A3B089FCE2981599C1CB2763C3630D2574340D3ECC73A89`과 실제diff/준수·407명/91개 실패message·stack을 직접 읽고792artifact hash차이0·작성자 전수분류 차이0을 확인했다. TRX SHA256 `E1991647A6042D606097DDAD9EF62F4256CDFED0A459258A9014A21838AFAB87`, 407=기존316P/신규91제품부재RED/skip0이며 뒤 API/byte/오류거부 단정은 미도달이다. Main `msg_57bdb7d3f26a` 창의 caller20:18:25~20:20:57Z 원exit1/A0B0C1D0·backup739/restore-build-test각1회·retry/kill0·build57warning/0error다. 원본739/준비817·runtime/동일lock/자원 반환을 먼저 대조해 `msg_33f9b372765e`로 창을 반환했다. succeeded/전체쓰기종료·release/정확pane종료·리드생존/reclaimable0, 신호13/max188초/초과0·질문대기269초다. R5 복구·작성자의check누락1건/집계교정과 리드 콘솔그룹표시 정정은 같은 근거 `lead-settlement-context.md`에 보존하며 제품확정실패/독립NOTPASS로 세지 않는다.

- **Snapshot 관찰 보완 정산:** 신규 Sol xhigh Task `task_5d22c932756a`/Dispatch `ctx_509981ae316c`, 시험1파일+8/-1·제품변경0이다. 원문 `approval-01/server-request-fixture-fix-01/report.md` SHA256 `8742D9141EEBB581B78CA13D4B1E0F23C9A6F0CB301D4B7DEB5F14C4B0F897AB`과 실제diff/준수·6행표를 직접 읽고88산출물hash차이0·동일316 fullname/결과를 대조했다. TRX SHA256 `DB248C609940B3BC37D176C43205AAFC12D11A56EA65F31E221E445C4E645C2E`, 316P/0F/0skip·기존310P/최초229P/224회귀 보존·6F→6P다. 요청없는 실제tick의 ID6 NotEmpty/개수 인과대조와 !=6 전체Single/byte·player/class/Guild 단정을 유지했다. Main `msg_84a56fb1136e` 창에서 caller19:32:52~19:35:21Z 원exit0/A0B0C0D0·backup737/restore-build-test각1회·retry/kill0, 원본737/준비800 보존·동일lock/runtime/자원을 먼저 대조해 `msg_11f21f9bd20f`로 반환했다. succeeded/쓰기종료·release/정확pane종료·리드생존/reclaimable0, 신호7/max285초/초과0·질문대기270초다. 빌드57warning/0error이며 신규 xUnit2031(시험252)은 후속 독립 검증 입력으로 남긴다. 자체 선행 시험 결과이며 Stored/실DB·전체독립강 통과가 아니다.

- **서버 요청 제품 구현 정산:** Astra xhigh Task `task_870cb2cac067`/Dispatch `ctx_20655312d485`, 기존2/신규9제품+345/-0·시험변경0이다. 원문 `approval-01/server-request-implementation-01/report.md` SHA256 `20AFF2E14947A78AD3EC90103B4814AF620D5A3A9B9D40FA16453D5B5DA62467`과 실제diff/준수·원시를 읽고148산출물hash차이0·전후316명/결과·전체실패message/stack분류차이0을 대조했다. 현재310P/6F/0skip, 기존229P유지/81RED해소/6RED잔여이며 뒤Active응답/class/player/Guild 단정은 미도달이다. outcome failed를 보존했고 독립 제품 확정실패로 세지 않는다. 원caller PATHEXT누락exit2와 복구outer UTC재해석의 미시작을 구분해 원시를 보존했다. 새Main `msg_037148ecd578`의 실제복구19:00:40~19:03:01Z A0/B0/C1/D0·backup737/restore-build-test각최초1회·자동retry/kill0, D runtime0/동일lock·737원본/812보존을 먼저 확인해 `msg_2690b6fabd05`로 창을 반환했다. failed/settled·release/동일pane종료·리드생존/reclaimable0, 신호17/max475초·초과3구간/질문대기3건과 리드ACK지연교정은 같은 근거 `lead-settlement.md`에 남겼다.

- **서버 요청 선행시험 정산:** 신규 Sol xhigh Task `task_d416fa466675`/Dispatch `ctx_a9c41920f0c9`는 시험3파일+825/-0·실제316=229P/87F/0skip을 완료했다. 원문 `approval-01/server-request-pretest-01/report.md` SHA256 `B98BA50386C4575B8616DD3A657834715D4C0EEBD4FBD31CA38589EBAF737487`를 직접 읽고 TRX316개 이름/결과·87실패 message/stack과 분류 차이0을 대조했다. 기존224회귀 전부P·신규5P, 등록부재63/World 수명 타입부재24의 뒤 semantic은 미도달이다. Main `msg_330fc47b3163` 안에서 caller17:52:31~17:54:54Z A0/B0/C1/D0·rawexit1, restore/build/test각1회/재시도·kill0이다. 원본728·준비767 drift0와 runtime0/동일lock 반환을 먼저 확인해 `msg_185772dc6f8d`로 창을 반환했다. completed/release·동일pane종료·리드생존/reclaimable0, 신호9/max407초·초과3구간/질문대기1건은 같은 근거 `lead-settlement.md`에 남겼다. 자체 선행 RED이며 제품GREEN·Stored/실DB·전체독립강 통과가 아니다.

Core `msg_80f596e64e30`(17:01:21Z)은 검증된 제한 checkpoint 없음·API v2/HEAD 불변·Program/ServerHost/Admission 소유/예약 유지와 LocalDB 전체적용/후속 독립검증 뒤 인계를 보고했다. 이 인계는 실제 bridge/저장 통합의 전제이며 Core 독립 시험 발행의 전제가 아니다. 문의 `msg_89667aa993a5`는 해소됐고 원문은 근거 `approval-01/mail-notification-overnight-02.json`이다. Content `msg_139c3c8fe0e3`의451cbf4a UI 정산·공용2 추가예약0과 World 선구현→완성PR2 Guild보존 직렬통합 합의도 유지한다. network289/UI13의 각 자체 근거를 full PR2 인계로 해석하지 않는다.

실제2단계 선행 작성 Task `task_d60dec8ebf76`/Dispatch `ctx_fe2fe54b151c`와 제품 연결·F1 시험 수정 세션은 모두 종료·정산했다. 추가38사례와 기존253/권위22 회귀는 동일313 필터에서 모두 통과했다. Core48 제품 파일·화면 변경은 포함하지 않았다. PR 전 전체 독립 강은 신규 외부 세션 한 번으로 별도 수행한다.

- **F1 시험 수명 수정 정산:** 신규 Sol xhigh Task `task_34d29eeac61b`/Dispatch `ctx_624eee3a3e3b`, 원문 `approval-01/unity-session-fixture-fix-01/report.md` SHA256 `4DEECA62201D6519243CA3F4C34102156325514D2AAECDF7540436F395806A32`와 실제 준수·diff를 직접 읽었다. 시험 한 파일의 MenuFacadeProbe만 +32/-18이고 제품9·모든 단정/입력·313이름·meta를 보존했다. Main `msg_6ba0ffe52769` 창의 PS/Unity 각1회 rawexit0·XML313P/0F/0skip, 기존309 회귀0·F1 네실패→P를 원시 전수 비교했다. XML SHA256은 `16352E2A033148571689D90DFA9F8851F4B15DB969A5127EBDE21F3F9CD73B3E`다. 원본2100/meta1126·Prefs137 전체차이0·프로세스0을 먼저 확인해 Main `msg_1ddd9765f3b0`로 창을 반환했다. succeeded/release·정확pane종료·리드생존/reclaimable0이며 재사용0이다. 생존 초과1구간1259초는 승인 실행창 대기·턴 종료 지시에 따른 대기였고, 공식 질문 대기2건과 구분했다. 상세는 같은 근거 `lead-settlement.md`다. 실제 서버/DB·전체 독립 강·Content 통합·플레이/CI 통과로 확대하지 않는다.
- **Unity 자동 사용자 캐시 정산 결정:** Main `msg_5e2263130930`(15:25:21Z)은 정확15argv 실행의 LocalLow/Unity/Caches/GiCache 자동 갱신을 통상 실행 부작용으로 분류해 현물을 보존하고 실패/범위위반으로 세지 않은313GREEN 정산을 지시했다. 디렉터리는 실행 전 존재했지만 before 내용/hash·이번 변경량은 unknown이며 외부cache 불변을 주장하지 않는다. 직접 수정/삭제/복원/재실행0, 추가감사·도구·전역설정·경로변경0이다. 원문은 같은 근거 `runtime-sideeffects-observation.json`·`lead-gi-cache-log-observation.json`·`settlement-supplement-v1.2.md`다. 다음 READY에 「Unity 사용자 캐시(LocalLow/Unity/Caches 아래 GiCache 등) 자동 갱신 — 관측만, 보존」을 포함한다. 성능JSON은 부재이며 성능측정 성공을 뜻하지 않는다.

- **실제session 구현 정산:** Astra xhigh Task `task_a30c77e4802e`/Dispatch `ctx_071275baff63`, 원문 `approval-01/unity-session-implementation-01/report.md` SHA256 `1A990BB326B7F096389094F50F70BC5CC3FF27B5A1F9D810BBBBBC5E11A7062A`를 직접 읽고 제품9diff/실제Unity1회·XML313=309P/4F/skip0를 대조했다. 신규34가 통과로 바뀌었고 기존275/이름·결과는 보존됐다. F1 네실패는 fixture의 original Lifetime null→reflection TargetException이며 facade본문 미도달로 전체GREEN미충족 outcome failed 정산했다. 제품확정실패/독립NOTPASS 집계는0이다. Prefs137 전체차이0·원본2100/meta1126 보존/프로세스0을 먼저 확인해 Main `msg_e13c10e12ba9`로 창을 반환했다. failed/release·동일pane종료·리드생존/reclaimable0을 확인했다. 성능JSON 부재/과거기본LocalLow자동출력의 beforeunknown 한계·생존초과332초1구간은 같은 근거 `lead-settlement.md`에 남겼으며 성능측정 성공·전체통과는 주장하지 않는다.

- **실제session 선행시험 정산:** 원문 `approval-01/unity-session-pretest-01/report.md` SHA256 `B08C08A42EB12A8471712FF68D7AE9008700614E9545993E9D5686E3B72CCED0`를 직접 읽고 원 XML313=275P/38F/0skip·전실패를 대조했다. 신규33은 session CharacterMenu getter,5는 facade ConnectCharacterMenu API 부재이며 이후 semantic 미도달이다. 기존253 fullname/결과 차이0, 별도 기존 결과48 authority22도 모두P다. 승인 `msg_6d44ce1ccef5`의 Unity13:00:23~13:10:47Z·caller rawexit2를 요구 RED와 구분했고, Prefs137 전체차이0·원본2093/meta1122·고정250 보존과 관련프로세스0을 확인해 XML 리드분류 전 Main `msg_d7a8ce88c97f`로 창을 반환했다. 완료/release·동일pane 종료·리드 생존·reclaimable0이며 재사용0이다. R1 통지 baseline 정적교정·liveness 초과4구간/이유와 실행·보존 원시는 같은 근거의 `lead-settlement.md`에 남겼다. 이 결과는 자체 선행 RED이며 독립 최종강·실DB·서버입장·실제접속/플레이/CI 통과가 아니다.

- **원본 DLL 배포 정산:** 신규 Astra xhigh Task `task_e4378aa46242`/Dispatch `ctx_7264952668f1`, 원문 `approval-01/dll-deployment-01/report.md` SHA256 `1C77A1B18728DC9B1E39076FA8A4D3918F6DEEABF9DD84CBEADCFAB10A290151`을 직접 읽고 실제 PS7 진입1회 exit0·제품 DLL1binary diff를 대조했다. 원본Shared.dll은 P1 시험에 사용한 동일113664bytes/SHA256 `B8B2828E27A1A1667A2B5F703A2C7A97822F70FC47EF974C9F3694DCB5A4ECD3`이며 구본은 단일백업했다. 리드 현물 대조에서 고정159파일drift0, meta1122→1122/변화0이다. 보고 UTC 서식 교정은 원본·원시와 구분했고 제품/실행 재시도0이다. completed/release·정확pane종료·리드생존/reclaimable0, 생존7신호/max198초/초과0을 확인했다. 자체배포 완료이며 새빌드·Unity·WSL·DB·실제54→48·최종독립강/CI는 미실행이다. 상세는 같은 근거 `lead-settlement.md`다.

- 후속 소비 계약은 [Unity 비화면 소비 v1](unity-consumer-contract-v1.md)이다.1단계 pure client/reader/Lifetime 최소 분기와2단계 실제 공용2파일/handler→48 연결을 구분해 선행 시험을 작성한다. Core `msg_45b43bc4ff25`/`msg_7d13fd6f7b61`은 API v2/host 미커밋·최종 독립강/인계0 유지와 DB Apply exit1·SQL Stopped/복구 준비를 보고했다. 환경 완료로 해석하지 않는다. Content `msg_f256203ca943`의 PR251 병합041bea25/PR2 착수·공용2파일 소유 유지도 대조했다. 최신 origin/main을 fetch해 실제041bea25와 신규 변경 경로를 확인했으며 이 계약의 PDL/버전/Lifetime/기존LifetimeTests와 중첩0이다. 현 World checkpoint dcf906a0은 유지하고 통합 전 최신main/Content/Core 인계는 다시 반영한다.
- Unity 첫 시험 세션은 공식22088자 계약 대신 JSON draft에 CODE 부록 꼬리345자만 보여 전체제출을 확인하지 못했다. 메인 `msg_c40beb9591b3` 판단으로 Enter0·abandon/fence·정확pane종료했고 고정입력drift0/신규시험부재를 확인했다. 도구한계이며 확정실패/모델격상으로 세지 않는다. 리드가 LF제거exactsuffix를 기계결과읽기전에 보고한 오류는 실제false 원문 확인 뒤 `msg_054938d10286`으로 즉시정정했다. 원시와정산은 `approval-01/unity-pretest-01/lead-abandon-settlement.md`에보존했고 동일계약/모델의신규세션으로다시발행한다. 새검사도구/반복규칙은만들지않는다.

- **Unity1단계 구현 정산:** 신규 Astra xhigh Task `task_3c45b744b364`/Dispatch `ctx_42bc4c8c5618`, 최종 원문 `approval-01/unity-implementation-01/report.md`와 `actual-compliance.md`를 직접 읽고 허용8제품파일·같은253 XML의227RED→P/기존26P유지·원본2093/시험/Plugins 보존을 대조했다. Shared 격리빌드·Unity최초1회·양쪽잔존/lock/prefs137복귀 뒤 메인 `msg_c83e21b8d4ff`로 실행창을 반환했다. H1은 PS capture의 VoidTaskResult 출력으로 원caller만실패한 결함이며 공개·원시보존·메인 사전승인 후두[void]억제/완료build-sync-backup반복0의남은caller로끝냈다. Windows SDK존재를확인없이전제한리드계약은정확WSL SDK로준비보충하고메인에정정했다. 제품확정실패/새검사도구0, 구현자완료·정확pane종료·리드생존을확인했다. 생존초과4구간·질문대기3건, package/licensing/종료로그미확정·일회근거script설정분리한계는 같은 근거 `lead-settlement.md`에 보존했다. 원본Shared21·실제session/handler48/서버DB·양쪽배포·플레이/Player/CI·전체독립강은남는다.

- **Unity 선행 작성·실제 RED 정산:** Task `task_fdeff68e69e6`/Dispatch `ctx_7410f00e785e`, 원문 `approval-01/unity-pretest-02/report.md` SHA256 `5E56C67B8E2B6028C76F3E764D82E58D6D47E43C02B30E8E72F27C12F1BD025F`. 허용 시험5파일만 작성했고 메인 `msg_3b169e7b1d1e` 창에서09:35:42~09:45:39Z Unity6000.6.4f1/TF1.8.0을1회 실행했다. XML253=26P/227요구RED/0skip, 부재4그룹은 client48/reader161/reply1/Lifetime event17이며 이후 semantic assertion은 미도달이다. 원본2086파일·고정47입력 불변, 이전 Lifetime26케이스 통과다. prefs7 Binary 변화는 보존·공식ask 후 메인 `msg_c8887e153cb3`가 정확복귀를 승인했고137전체 차이0·Unity/Hub0을09:49:53Z 확인해 메인 `msg_5918f221b7fb`로 창을 반환했다. 완료 Dispatch 정산·정확pane종료·리드생존을 확인했다. suite Child집계32를 setup으로 잘못 명명한 분석과 빈 pipeline의 delta파일 부재는 원시/정정에 구분했고 시험·harness를 바꾸지 않았다. 신호 초과5구간/질문대기2건은 같은 근거 `liveness-result.json`과 `lead-settlement.md`에 한 줄 정산하며 제품 확정실패로 세지 않는다. 전체 강·Shared22양쪽배포·서버DB·2단계handler/48·플레이/Player/CI 통과가 아니다.

- 전송 구현 정산: Task `task_3ae4e9a31fdb`/Dispatch `ctx_0f0348dd8a48`, 최종 원문 `approval-01/packet-implementation-01/report.md` SHA256 `F402D911BAB03EC1DB32BE5D1AE92189F6C05D92D467F75BB078C5A8A1658283`. 메인 `msg_1e9c1a8d8980` 창에서 실제08:20:39~08:22:29Z A/B/G/C/D0, 기존 generator→허용GenPackets1파일복사→동일필터217P/0F/0skip을 수행했다. 리드가 원TRX/이전행 전수 연결로49P유지·168RED→P를 확인했다. 보호720/백업723/고정30불변·복원0·D 잔존검사/lock 반환을 대조하고 메인 `msg_d25f596a6aa5`로 창을 반환했다. G빌드0경고/0오류·C빌드50경고/0오류이며50경고 기존여부는 미분류다. 완료 세션 정산·실제 pane 종료, 리드 생존을 확인했다. 패킷 green은 전체 강·실DB·서버 실제 입장·Unity/UI·양쪽DLL배포 통과가 아니다.

- 실제 RED 정산: Task `task_1c9662d93c45`/Dispatch `ctx_b89364be768b`, 최종 원문 `approval-01/compile-red-02/report.md` SHA256 `025FF95D0D4E3B567ABB297A8F5EA1E89CD8BF01838503881D599F4041991C3F`. A0/B0/C1/D0, 보호723불변·복원0, WSL 잔존검사/lock 반환을 실사했다. 신규 요구168은 protocol22부재1/공개enum부재1/ID49..54부재166이며168개 별도 제품 결함으로 세지 않는다. SDK10.0.301/빌드50warnings·0errors를 실제 관측했으나 warnings의 기존 여부 전수 분석은 하지 않았다. 완료 세션 정산·실제 pane 종료, 리드 생존을 확인했다. 메인 `msg_9b7aca39f10a`로 실행창을 반환했으므로 다음 실행은 새 창이 필요하다. 이전 고정 goal은 같은 근거 `goal-frozen-at-settlement.md`에 보존했다.

- 첫 실행 준비 정산: Task `task_5befbf9b9e27`/Dispatch `ctx_233962009f0f`, 실패 원문 `approval-01/compile-red-01/report.md`(SHA256 `F47C3FB8A68772B7B7C79C2F5A3D6F186676F91E582F2EC946084B54A67F4FF6`)와 `lead-settlement.md`. 결함은 기본 JSON 파서의 DateTime을 문자열 파싱해 KST로 재해석한 승인 시각32400초 차이다. 저장 harness/WSL/dotnet/compile/RED 실행0, 고정 입력·산출·source hash drift0, 실제 pane 종료 완료다. 메인은 신규 세션 수정·READY 보류와 자동 모델 격상/제품 확정 실패 집계 없음에 동의했다. 당시 goal 원본은 같은 근거 `goal-frozen-at-settlement.md`에 보존했다.

- 사용자 선택은 **A — 이 범위로 지금 병행 착수**다. 승인 당시 goal 172줄과 메인 정리본은 근거 `approval-01/inputs/`에 같은 bytes로 보존했다. 기존 이름·슬롯·직업 정책과 C1~C9·PR 경계는 바꾸지 않았다.
- 현재 작업 branch와 기준 main은 `.backups/verification/2026-10-18-character-server/entry-git.json`, 세션·Run·메시지 원시는 같은 근거 폴더에 있다. 이전 길드 goal의 Run·Task·Dispatch는 재사용하지 않는다.
- Core는 API v2 미커밋 구현의 공개 선언을 재확인했으며 실제 DB·저장재접속·강 판정은 남아 있다. Content는 Unity 공유 파일의 길드 PR2 중첩과 전체 쓰기 종료 미정을 회신했다. API 문서·정적 회귀를 실제 DB/최종 통합 완료로 해석하지 않는다.

## 요구사항 원천과 적용 결정

- 진입·초안 작성 지시: 메인 `msg_bbdc401da68a`(2026-10-18T04:45:09Z). 메인 창에 제출된 다음 사용자 원문을 메인이 전달했으며, 이 pane의 직접 사용자 입력으로 격상하지 않는다.
  > 그리고 일단 지금은 빠르게 기반작업을 우선순위로 잡고, 기반 작업 마무리 되면 컨텐츠쪽 유지보수랑 추가사항 빠르게 찍어내자 / CORE: DB 테이블이랑 스키마 구성 / 나머지 파트 흐름 : 로그인화면 - 캐릭터 선택창(기존 캐릭터 선택 or 캐릭터 생성) -> 마을 진입(여기서 길드 거점 or 사냥터) -> 사냥터 진입 후 클리어 -> 재화로 길드 거점 성장 및 추가보상
- 같은 전달의 사용자 결정: 「대시보드 결정 응답: 1) 기반 작업 - 마을에서 들어가는 길드 거점을 지금은 패널로 둘지, 별도 맵으로 바꿀지 → A 지금은 패널 · 2) 기반 작업 - Core DB를 흐름 전체 표로 설계하고, 서버 연결은 어디까지 먼저 할지 → A 캐릭터부터 연결」.
- 메인 운영 판단: 서버 패킷 처리는 World, 화면은 Content, DB 표·저장 연결은 Core 목표다. 최초 허가는 범위 초안 작성이었고, 아래 사용자 A 승인 뒤 조건을 지켜 병행 착수한다.
- Core의 기존 승인 기본값은 `01_Phases/goals/2026-10-17-character-storage-foundation/goal.md`의 원천을 따른다. 해당 문서는 현재 별도 `core-active`에서 읽으며 아직 이 main에 통합되지 않았다. 계정당 세 칸, 생성 시 이름·기사/마법사 선택, 이름 NFC 후 2~12자·한글 완성형/ASCII 영문·숫자·서버 전체 유일과 ASCII 대소문자 무시다. World가 이 정책을 변경하거나 별도 정규화하지 않는다.
- 앞 리드 종료 원문: `.backups/verification/2026-10-14-guild-server/laptop-resume-20261017/closeout-01/final-closure-01/final-status.md`의 4항. PR250 병합 이후 새 리드로 진입했으며 역사 goal은 수정하지 않는다.
- 새 branch는 최신 main에서 만들었다. 상세 SHA는 진입 근거에 두고, 계약 발행 때 최신 통합 기준을 다시 고정한다.
- **사용자 범위·착수 승인:** 메인 `msg_fb6c227d3b39`(2026-10-18T05:35:50Z)가 전달한 사용자 원문은 「대시보드 결정 응답: 1) 계획 검토 - World World 다음 목표 - 캐릭터 목록·생성·선택 서버 처리 범위 승인과 착수 시점 → A 승인 (초안 msg_68890d486286)」다. 메인 pane 제출의 전달이며 이 pane 직접 입력으로 격상하지 않는다. 승인 대상은 초안 v0.1 172줄/hash `5ED02523858CCA4612046AA7F4FD007244FB0C2EDEB9CFF247E76962D1BB248A`와 사용자에게 제시된 A 「이 범위로 지금 병행 착수」다. 원문은 근거 `inbox-next-03.json`, 승인본과 메인 정리본은 `approval-01/inputs/`에 보존한다.
- **착수 조건:** Unity 최소 수신 소유와 정확 파일은 Content 쓰기 종료·합의 뒤 계약 전 고정, 49..54/v22는 최신 main/타 파트 예약 대조 뒤 고정, Core 미검증 코드 복사0, 중량 실행은 메인 「실행 창 시작」 뒤다. 범위 승인만으로 이 조건을 생략하지 않는다.

## 적용 중인 사용자 결정

### 실행 창 대기 중 세션 정산·종료 — 2026-10-21

현재 메인 `msg_80a517f29947`(2026-10-21T09:18:15Z)이 전달한 사용자 원문과 적용 방식이다. 메인 pane Enter 제출 원문이며 이 pane의 직접 사용자 입력으로 격상하지 않는다. 실행 창 대기에 한해 ORCA R-8·현재 규칙 사본보다 이 결정이 우선하며 사본은 수정하지 않는다. 현재 작성 중이던 시험 보완은 중단하지 않고 READY 경계까지 진행했다. 이후 미배정 실행은 새 세션으로 이관하고 정적 작성자를 정산한다. 이 결정 원문은 mail-wake-23.json과 main-window-wait-decision-pending-goal.md에도 보존했다.

````text
[메인 Claude] 사용자 결정: 실행 창을 기다리는 동안에는 세션을 닫고, 창이 열리면 재개 지점으로 다시 연다
사용자 원문(메인 pane Enter 제출, 2026-10-21 18:1x KST):
「그리고 만약에 한 파트에서 이미 서버를 쓰고 있으면 다른 대기 파트에서 계속 리드나 작업자들이 우편확인 하는게 아니라 재개 지점 작성하고 세션을 종료하는 식으로 간다음 다시 열어서 재개하는 식으로 가자

토큰 낭비가 심해」

메인 적용 방식(Rules 정본 반영 전까지 이 메시지가 기준이다):
1. 언제: 실행 창(Windows dotnet·WSL·Unity·SQL)을 요청했는데 메인이 「대기열」로 답했을 때. 또는 다른 파트가 창을 쓰는 동안 자기 파트에 실행 말고 남은 일이 없을 때.
2. 작업자·검증자: READY와 고정 입력을 파일·SHA로 남기고 정산·종료한다. 창을 기다리며 check --wait·heartbeat·반복 조회로 세션을 살려 두지 않는다. 창이 열리면 리드가 새 실행자를 연다. 그래서 READY·승인 파일은 「같은 작업자·같은 incarnation」이 아니라 고정 파일·SHA로 실행할 수 있게 써라. 이미 같은 세션 조건이 박힌 READY는 다음 계약부터 고친다.
3. 리드: goal 「재개 지점」에 기다리는 창 요청(READY 경로·SHA, 필요한 길이·lastEntry·Unity 수, 창이 열리면 할 첫 일)을 쓰고 local commit한다. 그다음 메인에 「재개 지점 기록·종료 준비」 status(goal SHA·commit 포함)를 보내고 턴을 끝낸다. pane은 메인이 idle을 확인하고 닫는다.
4. 다시 열기: 창이 비면 메인이 그 파트 리드를 새 세션(같은 대화 이어 붙이기 아님)으로 열고 같은 Run을 run-use로 인수하게 한다. 진입 메시지에 창 배정을 함께 넣는다. 리드는 goal 재개 지점부터 읽는다.
5. 하지 않는 것: 지금 작업 중인 작업자를 이 결정으로 중간에 끊지 않는다. 창을 기다리는 동안 상태 확인 메일을 서로 주고받지 않는다. 계획·계약 작성처럼 창이 필요 없는 일은 계속한다.
6. 기록: goal 「적용 중인 사용자 결정」에 원문과 이 메시지 ID를 기록하라. ORCA R-8의 「목표 중간에 문맥을 비우려 수동 교체하지 않는다」는 실행 창 대기에 한해 이 사용자 결정이 우선한다.

지금 파트별 적용:
- Core: 보정 SQL 창(09:20Z~09:30Z)을 쓰는 중이라 해당 없음. 창을 반환한 뒤 다음 실행 창이 또 필요하면 이 방식으로 기다린다.
- World: 새 시험 컴파일 결함 수정 READY가 나왔을 때 창이 차 있으면 이 방식으로 닫는다.
- Content: 측정 도구 수정 작업자는 작업 중이라 계속한다. 그 뒤 Unity 창을 기다리게 되면 이 방식으로 닫는다.
- Art: 효과 PR 계약·선행 시험 작성은 계속한다. Unity 창을 기다리게 되면 이 방식으로 닫는다.
````

### 2026-10-21 재진입 — 모델 라우팅과 사용량 페이싱

현재 메인 `msg_14ff7b5796bd`(2026-10-21T07:33:53Z)가 같은 대화 재진입과 아래 사용자 결정 전문을 전달했다. 사용자 재개 입력은 메인 pane의 2026-10-21 16:29 KST 「이어서 진행해보자」이며 사용자 직접 입력으로 격상하지 않는다. 같은 노트북에서 이어가므로 근거 이관은 필요 없고, 자기 feature branch commit/push는 담당 리드가 goal 절차대로 판단한다. main push·PR 병합 권한은 없다.

원문은 `.backups/verification/2026-10-18-character-server/resume-2026-10-21-01/main-entry-original.json`에 보존했다. 아래 블록은 전달 원문 그대로이며 자기 checkout의 AGENTS·ORCA·스킬 사본과 충돌하면 이 결정이 우선한다. 규칙 사본은 수정하지 않는다. **10-20 20:00 KST 전 새 Opus 금지와 과거 임시 Sol 선행시험 배정은 만료했다.** 현재 새 선행시험은 Opus, 일반·위험 신호 구현은 Sol max, 위험·DB·프로토콜·공유 DLL·보안 독립 검증은 Opus다. 실행 창은 메인이 직렬 배정하며 현재 새 창은 없다. Core→World와 Content 두 트랙·사용량 페이싱을 적용한다.

````markdown
# 리드 진입 메시지에 넣을 적용 중 사용자 결정 — 모델 라우팅 개선 구성과 사용량 페이싱

이 파일은 메인이 리드 진입 메시지에 그대로 붙이는 전달 초안이다. 정본이 아니다. 정본 반영은 Rules 리드가 맡고, 반영 전까지는 각 리드가 자기 goal의 「적용 중인 사용자 결정」에 이 블록의 원문과 출처를 기록한다.

## 출처

- 사용자 원문(2026-10-19 메인 대화, Enter로 제출된 입력): 「OK A로 진행하되, 지금 워크트리로 규칙이 사본 형식 느낌으로 분산되어있어서 전부 적용하려면 시간이 좀 걸릴꺼 같은데」
- A의 내용: 현황판 결정 「모델 라우팅 - 개선 구성 적용과 10-25 리셋까지 Codex 크레딧 사용량」의 A 「개선 구성 + 크레딧 절반」. 근거 스크립트 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-19/scripts/update-decision-routing-bundle-1400.cjs`.
- 선례: Sol xhigh 전환처럼 사용자 결정에 따른 런타임 전환은 정본 병합을 기다리지 않는다. 자기 checkout의 AGENTS·ORCA·스킬 사본이 아래와 다르면 이 블록이 우선한다.

## 라우팅

| 역할 | 모델 · 실행 |
|---|---|
| 파트 리드 (Core 포함) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 구현자 일반 | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 위험 신호 (셸·보안·입력 경계, 시간·순서·동시성·수명, 런타임·언어 경계) | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 (같은 계약 두 번째 독립 불합격) | `codex --model gpt-6-astra -c model_reasoning_effort=xhigh` |
| 위험 신호 구현의 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| DB·영속 데이터·프로토콜·공유 DLL·보안 경계 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| 그 밖의 독립 검증 (문서 실사, 약·강 등급, 로컬 DB 설치·기동 도구 카드) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 선행 시험 작성, Gardener | `claude --model claude-opus-5-5` (변화 없음) |
| 확정 실패 3회 뒤 자문, 설계 검토 시범 | `claude-fable-5-1` (변화 없음) |

- 「설계 판단이 남은 새 다파일 기능」 신호와 「애매하면 Astra」 규칙은 쓰지 않는다.
- Sol 구현을 다른 새 Sol 세션이 검증하는 것을 허용한다. 구현자·검증자 분리, 작업 하나 뒤 종료, 독립 검증 생략 금지, 리드 직접 구현 금지는 그대로다.
- 로컬 DB 도구 카드: 사용자 실행 카드에서 독립 검증이 놓친 결함이 한 번이라도 나오면 Opus 검증으로 되돌린다.
- `max`가 첫 기동 화면에 없으면 대체하지 말고 메인에 보고한다. 계약에는 지정 모델·effort와 화면 관측값을 적는다.
- capacity 예외(Sol 30분 실패 뒤 Astra xhigh 새 세션)와 Claude 첫 화면 처리는 그대로다.

## 사용량 페이싱

- 사용자 원문(2026-10-19 14:1x KST, 위 A의 크레딧 부분을 바꿈): 「크레딧은 최대한 아끼고 싶긴한데, 사용량이 부족하면 어쩔 수 없긴 하네 / Codex 사용량 절감은 최대한 플랜 주간 소모량 기준으로 잡고, 크레딧은 비상 상황에서만 쓰는거로 하ㅏ」
- Codex는 플랜 주간 한도만 기준으로 나눠 쓴다. 주간 한도를 하루 약 14%p로 나눈다. 크레딧은 비상 상황에서만 쓴다.
- Codex 주간 한도는 10-19 14:11 기준 95%이고 리셋은 10-25 05:40 KST다. 그 전까지 남은 약 5%(약 1,600 크레딧)를 넘어 쓰면 크레딧이 줄어든다.
- 사용자 원문(2026-10-19 14:2x KST, 비상 사용 결정): 「음 어찌보면 지금이 그 비상상황같은데 일단 크레딧을 쓰느거로 할까, 대신에 라우팅 측정할때는 주간 사용량 기준으로만 체크하는 식으로 가고」
- 그래서 10-25 05:40까지는 크레딧을 비상으로 쓴다. 쓰는 속도는 주간 한도 기준과 같게 맞춘다. 하루 약 14%p, 크레딧으로 약 4,600이다(주간 100% ≈ 3만 2천 크레딧, 10-18 측정 환산). 라우팅의 효과는 이 주간 환산 속도로만 판단한다.
- 상한은 앞서 승인한 18,000 크레딧이다. 쓴 크레딧이 약 16,000에 닿으면 메인이 사용자에게 다시 묻는다.
- 메인이 사용률을 목표선과 비교해 앞서면 우선순위가 낮은 파트부터 새 작업자 기동 보류를 지시하고, 뒤처지면 재개를 지시한다. 리드는 그 지시를 따른다.
- 작업자 세션 하나의 크레딧이 크게 늘면(예: Sol max 구현이 세션당 200을 넘음) 리드가 메인에 알린다.

## 리드가 할 일

- 이 블록의 원문과 출처를 자기 goal.md 「적용 중인 사용자 결정」에 기록한다. 자동 압축 뒤에는 goal.md를 다시 읽고 이 결정을 따른다.
- 작업자·검증자 위임 계약마다 위 표의 지정 모델·effort·실행 명령을 적고, 기동 뒤 화면 관측값을 함께 남긴다. 자기 checkout의 AGENTS·ORCA·스킬 사본과 다르면 이 블록을 따른다. worktree의 규칙 파일은 고치지 않는다.
- 파트 배분: 동시에 두 트랙까지만 연다. 순서는 Core → World 한 트랙, 그다음 Content, 그다음 Art(PR253 병합 뒤)다. Rules·Management는 10-28 뒤다. 메인이 목표선에 따라 보류·재개를 지시한다.
- 2026-10-20 20:00 KST 전에는 새 Opus 세션을 열지 않는다. Opus 검증이 필요한 단계(DB·영속 데이터·프로토콜·공유 DLL·보안 경계, 위험 신호 구현)는 그 뒤로 미루고, 그 전에는 Sol로 끝나는 단계만 진행한다.
````

### 취침 중 판단은 메인 위임 — 아침까지

메인 `msg_02a8258a3fc2`(2026-10-18T16:57:21Z)가 전달한 사용자 원문(메인 pane Enter 제출,10-19 02:0x KST)은 「나 잘테니까, 이후 작업 판단을 메인 세션의 판단과 내 세컨드 브레인 정보로 이어서 진행해줘 / 전적으로 신뢰할게」다. 이 pane의 직접 사용자 입력으로 격상하지 않는다. 아침까지 범위·계획·우선순위 판단은 기존 결정과 속도·가역성·가장 싼 환경의 검증 기준에 따라 메인에게 status로 올리고 사용자 결정 대기로 멈추지 않는다. 이후 개별 판단은 **사용자 위임에 따른 메인 결정 + 이 원천 메시지 ID와 해당 결정 ID**로 기록한다.

관리자/UAC·SQL Express 서비스/인증/로그인/방화벽 변경·PR 병합 승인 줄·전역설정·되돌릴 수 없는 삭제는 위임 밖이다. 이 일들은 아침에 바로 실행할 수 있는 명령·근거를 준비해 메인에 전달하며 직접 수행하지 않는다. GUI Unity는 메인 실행창 배정대로 진행하되 새 배정 없는 실행은 없다. 현재 World에는 사용자 판단 대기 안건이 없고, Core의 실제 검증·소비 타입/쓰기종료 인계 전제는 유지한다. Core의 별도 LocalDB 계획을 World의 실행/복사 허가나 실DB 통과로 해석하지 않는다. 원문은 근거 `approval-01/mail-notification-latest-01.json`이다.

### 사용자 위임에 따른 메인 결정 — Core 독립 서버 선행시험 지금 발행

메인 `msg_4dc4f7f5bdd5`(2026-10-18T17:04:26Z)는 위임 원문 `msg_02a8258a3fc2`에 따라 사용자 A 승인안의 「Core가 실제 DB 검증을 하는 동안 패킷 형식과 대역 시험을 먼저 준비」를 적용해 Core 인계 대기를 해제했다. Core 공개 API v2 문서만으로 고정할 수 있는 World 시험을 지금 발행하며, 실제 Core 타입은 인계 뒤 좁은 이음에서 연결한다. 미검증 Core 코드 복사0·번호 예약/재사용 금지·서버/Unity 양쪽 검증·대역과 실제 저장/재접속의 구분은 유지한다. 원문은 근거 `approval-01/mail-notification-overnight-03.json`이다.

전송49..54/v22는 기존 예약 절차와 Core/Content 예약0 대조를 마쳤고 제품217GREEN·Unity313GREEN까지 진행했다. 새 번호/프로토콜 변경 없이 첫 묶음은 [서버 요청 경계·수명 v1](server-request-contract-v1.md)로 좁힌다. 실제 GameSession 요청 경계/Legacy 안내와 Core 없이 존재할 수 있는 World 요청 수명을 먼저 작성·RED·구현하며 Stored bridge/선택·Unknown/실제 입장·길드 전체 요구는 후속 묶음에 유지한다. Main 본문의 Opus 표기는 현재 사용자 임시 모델 결정과 이전 정정 `msg_239b72df7331`을 따라 **신규 Sol xhigh**로 적용하며 메인에 알린다. 새 사용자 승인 대기나 추가 독립 검증을 이 단계에 만들지 않는다.

### 사용자 위임에 따른 메인 결정 — 정상 Snapshot 동반 시험 보완

메인 `msg_1c4aa90c0a9e`(2026-10-18T19:07:49Z)는 사용자 위임 `msg_02a8258a3fc2`에 따라6건을 **(b) fixture/관찰 입력 누락**으로 분류했다. 실제 active메뉴 응답과 원래맵tick의Snapshot6이 함께 잡혀 전체Packets Single에서 실패했으며 정상snapshot을 제품에서 억제하지 않는다. 현재writer정산 뒤 신규Sol xhigh가 시험1파일만 보완한다.

조건은 정확ID6만 분리·Single와 예상외패킷 거부 유지, 요청없는tick대조 또는 실제Snapshot/원상태대조의 **인과 단정 하나**, 응답전체byte/class/player/Guild 단정 보존·다음실행의 실제도달,6건 원실패 이름/message·분류b·변경줄 표의 다음계약/전체독립입력 포함이다. 새Main창·같은명령1회에서 검사하고 새제품결함은 번호로 반환한다. 원문은 `approval-01/server-request-fixture-fix-01/main-decision-original.json`에 보존했다. 이 결정은 선행시험 보완이며 제품전체/독립강 통과가 아니다.

### 사용자 위임에 따른 메인 결정 — 현재 inline draft의 Enter 1회 복구

메인 `msg_645c6d8ebd9f`(2026-10-18T19:55:02Z)는 사용자 위임에 따라 현재 `task_d819e71aa834`/`ctx_6fb00f94b4fa`의 placeholder 없는555자 inline draft에 텍스트 없는 Enter1회와 사후 제출 전문 대조를 허용했다. 사전에는 줄바꿈만 복원해 공식payload 끝부분과 대조하고, 사후 전문 불일치/식별불가는 정적중지 status와 경로·SHA만의 짧은 주입으로 정식계약을 다시 읽게 하는 조건이다. 다른 세션의 R5 예외로 확대하지 않는다.

실전송Enter1 뒤 draft소멸/Working을 관측했고 실제Codex rollout의 user 제출을 식별했다. 공식6205자/제출6198자는7LF 차이이며 나머지 문자·공백은 일치하지만 ordinalfalse를 보존했다. 조건대로 중지 `msg_52637d300fab`/짧은pointer `msg_5eade9a1a9a3`를 보냈고 작성자는 쓰기0에서 정식계약 전문·SHA 재대조 `msg_66011af0c388` 뒤 재개했다. 당시 goal은516고정입력에 포함돼 원문을 먼저 `approval-01/catalog-response-pretest-01/main-inline-recovery-original.json`에 보존하고 전체쓰기종료 뒤 이 기록을 반영했다. 초반 잘못 붙인 wait-submit 옵션은 CLI local validation에서 전송0으로 거절됐으며 실제 Enter1과 구분했다. 미래 기술적 강제·공용 규칙 정비 완료를 뜻하지 않는다.

### 동결까지 기능 산출물 우선

메인 `msg_5c3dc59360f6`(2026-10-18T07:00:22Z)이 전달한 사용자 원문(메인 창 직접 입력,16:00 KST)은 「목표까지 일단 빠르게 빠르게 작업물 쳐내고 이후에 안정성 찾아가는거로 진행하자, 일단 좀 윤곽이 보여야 어떤걸 고쳐야 할지 보일 것 같아」다. 메인 전달이며 이 pane의 직접 입력으로 격상하지 않는다.

메인 적용 기준은10-28까지 보이는 기능 산출물 우선, 감시·검사·운영도구 보강과 부가 경계 방어는 안정화 후보로 남김, 실제 제품/사용자 파일 손상 없는 도구 결함은 한계를 기록하고 진행이다. 사용자 파일은 실행 전 백업과 전후 존재/hash 대조·변경 시 복원/보고로 보호한다. 절차 위반은 기록하고 같은 세션에서 계속하며 승인 밖 쓰기/실행이 실제 발생할 때만 세션 교체한다. 독립 검증은 PR 전 한 번 유지하고 창마다 별도로 추가하지 않는다. 범위 안 판단은 리드가 하며 메인에는 사용자 결정·실행 창·완료조건 변경만 올린다. 수정3회 초과 체크포인트는 알림이며 작업 중단이 아니다. READY는 Task/Dispatch·정확 argv·예상 시간·백업 위치로 짧게 보낸다. 병합 승인·비밀정보/관리자/UAC/SQL서비스/방화벽·전역설정·동시쓰기·미실행 보고 경계는 유지한다.

World는 UTC를 좁게 고치고 실제 관문 오프라인 확인만 한다. 새 감시/검사 helper를 만들지 않으며 compile/RED 뒤 서버 구현을 바로 잇는다. 첫 실행자 종료는 이 결정 전 정산이므로 되돌려 재사용하지 않는다. 다음 발행부터 이 기준을 적용한다.

### WSL 병행 실행과 Unity 최소 소비 소유

메인 `msg_ccd4007ebfec`(07:17:23Z)이 전달한 사용자16:17 KST 결정은 「대시보드 결정 응답: 1) World 서버 시험 - Content Unity 실행 중에도 WSL 시험을 같이 돌리도록 시험 시작 조건 한 줄을 좁힐지 → A 조건 좁혀서 지금 같이 돌리기」다. Win32_Process 단일 snapshot에서 Unity.exe 본체와 직접 부모가 그 Unity.exe인 dotnet.exe만 차단 대상에서 제외했다. 나머지 Windows/Linux 대상과 자원6/4/40GiB·실행시간 관문은 유지했다. 실제 RED는 이 승인 창 안에서 끝났고 반환했다. 이 결정은 Unity 실행 중복 허가가 아니다.

Content `msg_e6e7c1e46014`·Core `msg_e715a95fb09f`와 메인 `msg_165321b60e07`(07:46:53Z)로 [정확 소유 제안 v0.1](../../../.backups/verification/2026-10-18-character-server/approval-01/server-implementation-draft-01/unity-consumer-ownership-proposal-v0.1.md)(SHA256 `A15881D4523A9715B7B8D78F791BD6A9A2E2591C5F8752A5A9BA8F25DE7E7C59`)의 선행 파일만 World 자기 checkout 쓰기를 허용했다. 신규 Network/CharacterMenu의 Client/Reply/ReplyReader3개, Handlers/CharacterMenu의 List/Create/Select 결과 handler3개·각 meta/두 폴더 meta, 신규 EditMode CharacterMenuClientTests/CharacterMenuPacketContractTests와 meta, 기존 ClientConnectionLifetime.cs/동명Tests.cs의 명시 메뉴·legacy 보존 최소 분기다. 경로 접두는 모두 `03_Client/Assets/`이며 세부 경로는 제안 원문을 따른다. 기존 meta/GUID는 보존한다.

공용 UnityClientSession.cs·NetworkService.cs는 Content PR2 실제 쓰기 종료·기준/diff 인계 뒤 World가 갱신 기준에서 최소 접점을 잇는다. Core48 authority/handler와 모든 화면/폼/버튼·Content checkout/실행 snapshot은 제외한다. Core는 현재 연결/generation의 pending과 맞는54성공만 기존48선택 문맥을 열고54→48/main-thread 순서·연결당 첫48권위·class는48만 적용·입장 전 gameplay금지를 보존하는 의미에 동의했다. 구현 통과나 Core 통합 완료는 아니다. Unity 컴파일/EditMode는 별도 독점 창으로 요청한다. 메인은 승인 범위 안 리드 소유 합의는 리드끼리 확정하고 결과만 보고하도록 확인했다.

### 신규 선행 시험·독립 검증의 임시 모델

Content가 보존한 메인 전달 전문 `msg_623c6ff92c9b`(2026-10-17T14:41:45Z)의 사용자 원문은 「대시보드 결정 응답: 1) Claude 사용량 - 주간 리셋(화 20:00)까지 새로 여는 선행 시험·독립 검증 세션을 Codex로 돌릴지 → A 지금부터 Codex로」다. A의 설명은 **새 선행 시험·독립 검증 모두 gpt-6.1-sol xhigh**, 기존 Opus는 맡은 작업을 끝내고 **2026-10-20 20:00 KST(11:00 UTC) 리셋 뒤 새 세션부터 원래 배정 복귀**다. 리드·구현자·독립성·태그·권한 경계는 바꾸지 않는다. 원문 보존본은 `approval-01/inputs/content-main-decisions-originals.md`다.

같은 전문의 `msg_e0ebcfcf3db5`(2026-10-17T14:42:35Z)의 추가 사용자 원문은 「대신 Sol의 성능이 빈얃하다고 느낄경우 Astra로 격상」, 「Astra로 격상시에는 Effort는 High로」다. 필수 관찰/실행 누락·원시와 다른 주장·반복 맥락 손실·범위 오해 쓰기 등 **이 작업의 실제 신호**를 리드/메인이 판단할 때 같은 작업의 **다음 새 세션만 gpt-6-astra high**로 올리고 근거를 보고한다. 실행 중 교체·다른 파트의 신호 차용은 하지 않는다.

승인 전달에는 선행 시험을 Opus로 적은 문구가 있어, 원문의 적용 대상이 선행 시험까지 포함됨을 `msg_6e1c369d08c8`로 메인에 보고했다. 메인 `msg_239b72df7331`(2026-10-18T05:39:31Z)은 이 문구가 임시 결정을 놓친 것임을 확인하고 **World 예외 없이 이번 신규 선행/독립 모두 Sol xhigh**로 정정을 수용했다. 초안의 Opus 선행/검증 설명은 위 한시 사용자 결정에 맞춰 정정했다. 리셋 뒤 발행분은 그 시점 원래 배정과 다시 대조한다. AGENTS 정본 개정은 아니다.

### 신규 계약의 우편함·자식 환경

다음 두 결정의 원천은 `msg_bbdc401da68a`가 전달한 사용자 현황판 응답이다. 공용 규칙·helper 정본 반영은 10-28 뒤 Rules 작업이며, 이번 목표에서 운영 도구를 만들지 않는다.

1. 「대시보드 결정 응답: 1) 운영 규칙 - 작업자가 우편함 확인을 다른 명령과 한 번에 묶는 일을 막는 교정을 언제 할지 → A 동결 뒤 Rules 첫 작업으로」. 이번 모든 신규 계약에는 아래 문장을 넣는다.
   > 우편함 check·inbox의 반환은 그 결과만 받는 별도 도구 호출로 읽고 해석한 뒤에 다음 행동을 시작한다. check와 실행 명령을 한 셸 호출이나 한 batch로 묶지 않는다.
2. 같은 전달의 「… 4) 운영 도구 - Windows PowerShell 5.1 자식 실행이 모듈 경로 함정에 걸리는 일을 막는 공용 helper를 언제 만들지 → A 동결 뒤 Rules 후보로」. 이번 모든 신규 계약에는 아래 문장을 넣는다.
   > PowerShell 7 안에서 Windows PowerShell 5.1(powershell.exe)을 자식으로 띄울 때는 띄우기 직전 그 자식의 환경에서만 PSModulePath를 지운다. 현재 세션·사용자·시스템 환경 변수는 바꾸지 않는다.

메인 전달 원문 전체는 근거 폴더 `inbox-before-run.json`에 보존한다. 첫 peek를 다른 읽기와 묶은 것은 이 결정을 수신하기 전이며 사전 메모와 READY에 공개했다. 수신 뒤에는 check·inbox를 단독 도구 호출로 처리한다. 공용 helper 구현이나 미래 세션의 기술적 강제를 완료했다고 주장하지 않는다.

## 범위

### 만들 것

1. 로그인한 계정의 캐릭터 **목록·생성·ID 선택** 요청을 처리하는 World adapter. 서버가 보유한 계정 문맥으로 Core `CharacterCatalogService`를 호출하고 불변 결과를 World 큐에서 반영한다.
2. 요청/응답 세 쌍의 전송 정의, 입력·길이 검사, 결과 코드의 의미별 매핑, 서버·봇·Unity의 양쪽 직렬화 계약 검증. 기존 번호·필드와 권위 직업 결과48은 보존한다.
3. 연결당 요청 하나, 요청 상관값·세대·원래 deadline, 닫힌 연결·늦은 완료 폐기, 선택 handle 종료를 명시한 수명 처리. Core host의 미결 생성 소유권을 사용한다.
4. 호스트 시작 시 고정된 Legacy/Stored 모드를 소비한다. 명시 Legacy만 기존 직업 선택 흐름을 허용하며 Stored 실패와 구버전 선택 요청으로 저장을 우회하지 못하게 한다.
5. 선택 완료 → 기존 권위 직업 결과48 → 실제 입장 → inventory/guild 등록의 보존과 실제 서버 경로 검증. 계정 단위 길드 가입 seed와 기존 경제·강화 수명은 유지한다.

### 건드릴 곳

아래는 제안 소유 경계다. 작업자 발행 전에 실제 최신 경로·함수와 Core/Content 쓰기 종료를 대조해 좁은 파일 목록으로 고정한다. 디렉터리 표기는 그 안의 전면 수정 허가가 아니다.

| 소유 | 제안 대상 | 경계 |
|---|---|---|
| World 제품 | `02_Server/GameServer/Handlers/Session/`, `Handlers/HandlerRegistry.cs`, `Sessions/GameSession.cs`와 같은 책임의 메뉴 adapter | 세션 경계·패킷 처리. 기존 세션에 모든 저장 책임을 합치지 않는다. 새 파일명은 책임에 맞춰 계약 때 확정한다. |
| World/Core 접점 | `Loop/GameWorld.cs`, `Program.cs`의 소비·주입 부분 | Core가 전달한 host mode/catalog/admission 접점을 받는 최소 변경만. Core는 Program/ServerHost/RuntimeStartup/AdmissionServices/Lifetime을 소유한다. Core 쓰기 종료·통합 뒤 소비하며 동시 쓰기 금지. |
| World 공유 계약 | `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/Generated/GenPackets.cs`, `ProtocolVersion.cs`, 같은 책임의 신규 공개 결과 enum | PDL에서 재생성한다. 생성기 자체 개선은 제외한다. 현재 generator는 manager 생성 생략이 기본이므로 존재하지 않는 manager를 추가하지 않는다. |
| World 시험 | `02_Server/GameServer.Tests/`의 패킷·세션·입장 회귀, `99_Tools/headless-bot/`의 좁은 메뉴 시나리오·등록 | 선행 시험/독립 검증자와 제품 구현자 소유권을 분리한다. 관련 기존 실패를 전수 분류한다. |
| Content 조율 | 위 정확 소유 제안의 Unity 메뉴 소비6파일·시험2파일·meta 및 ClientConnectionLifetime 최소 분기 | 한정 쓰기 확정. 공용 UnityClientSession.cs·NetworkService.cs는 Content PR2 종료/기준 인계 뒤 직렬 연결하며 Core48파일·화면은 제외한다. |
| DLL·양쪽 빌드 | `98_Shared/Shared.csproj`, `04_ClientNet/`의 현 빌드 경로, Unity `Assets/Plugins/Shared/`·`ClientNet/` 산출물 | 프로젝트 설정을 바꾸는 뜻이 아니다. 실제 재생성/복사와 양쪽 해시·소비자 버전을 확인한다. 기존 `.meta`·GUID·직렬화 값을 보존한다. |
| 리드 문서 | 이 goal, 승인 뒤 고정할 패킷 계약, `CURRENT.md`의 World 줄, 관련 기능·서버·protocol 계약 | 현재는 goal 초안과 CURRENT 자기 줄만 쓴다. 실제 계약 변화와 함께 필요한 현재 문서만 갱신한다. |

### 하지 않을 것

- DB schema·SQL·저장 repository·Core Catalog·복구 큐·host 미결 소유 로직의 구현. World는 전달된 접점을 소비하며 결함은 Core에 반환한다.
- 캐릭터 선택/생성 화면·씬·버튼·미술·전체 UI 흐름 제작. 화면 작업과 목표는 Content가 별도로 승인받는다.
- 삭제·이름 변경·비밀번호 인증·게임 중 캐릭터 교체·별도 취소 패킷, 계정 로그인 정책 변경.
- 재화·인벤토리·무기·길드의 영속 저장 연결, 신규 보상/성장 수치·seed 수명 변경, 별도 길드 맵.
- 패킷 번호 재사용, 기존 선택 패킷의 ID 선택 재해석, Stored 오류의 Legacy fallback, 무한 재시도·새 OperationId로 미확정 작업 재생성.
- 생성기 기능 확장·list/새 binary GUID codec, TRX 집계 helper·봇 목록 드리프트 검사·공용 운영 규칙 정비. Gardener 후보는 이번에 채택하지 않는다.
- 무관한 코드 정리, 전역 설정 변경, 승인 밖 DB·프로세스·Unity 실행, World의 병합 실행·자동 병합 예약.

### 관찰 가능한 완료조건

| 번호 | 조건 | 필요한 관측 |
|---|---|---|
| C1 | 목록은 빈 계정/1명/3명·슬롯 순서를 정확히 반환한다. 조회만으로 게임 행이나 player/owner를 만들지 않는다. | Core 대역 호출·결과/부작용0, 실제 Core 연동 빈 계정/생성 뒤 조회. 3명 초과·중복 슬롯·잘못된 class는 정상 목록으로 자르지 않는다. |
| C2 | 생성은 서버 계정 문맥과 Core PrepareCreate의 descriptor를 사용한다. Core 오류의 의미를 보존하며 World가 이름 정규화/유일성 검사를 복제하지 않는다. | 입력 경계·이름/슬롯/직업 한도, Core 거부 매핑·동시/중복 요청, dispatch 전 host 소유 등록을 통과한 접점. 생성 성공만으로 입장/길드 등록0. |
| C3 | 선택 ID는 그 계정 소유 캐릭터만 허용하고 저장된 class로 입장한다. | 없는ID/타계정ID의 같은 외부 거부·다른 내부 진단, 성공 응답 → 48 → 입장 → 길드 순서, 저장 class와 상반된 로컬 class가 권위에 영향0. |
| C4 | 요청중/입장중/게임중/닫힘과 재접속 세대를 구분한다. | 동시 추가 요청이 원래 pending을 손상시키지 않음, 중복 requestId가 Core 새 작업을 만들지 않음, 종료 직전/직후 handle 수령·늦은 완료에서 player/inventory/guild 등록0. |
| C5 | 미확정 결과를 확정 성공·실패로 바꾸지 않고 원래 ID/descriptor/deadline을 보존한다. | Unknown/StillUnknown·송신 실패·닫힘 시험. 최종 미확정 안내 뒤 종료와 Core host 소유 지속, 새 5초·새ID·임의 rollback0. 실제 DB 복구 내부 안전성은 Core 독립 판정도 필요하다. |
| C6 | 시작 모드가 고정되며 Stored 실패의 우회 입장이 없다. | Legacy 정상 흐름, Stored 초기화 실패·실행 오류·기존 직업 선택 요청, 허위 빈 성공 목록/fallback0. |
| C7 | 새 wire가 양쪽에서 동일하게 해석되고 기존 ID1..48이 보존된다. | byte 고정 프레임·최대길이·잘린/홀수/과대/잘못된 UTF-16·reserved/중복ID·명시 LE·GUID D형, PDL 재생성 diff·정확 버전 handshake·서버/봇/Unity 소비·실제 DLL hash와 Unity 컴파일. |
| C8 | 기존 길드·개인·공동 상태의 수명을 유지한다. | 같은 계정의 다른 캐릭터 재접속으로 seed 추가0, 실제 입장 이후 등록, 기존 개인 연결 수명/공동 프로세스 수명·던전 보상·강화 회귀. |
| C9 | 신규 독립 검증자가 강 등급의 diff·근거·시험·현재 실제 경로 실행을 판정한다. | 원시/새 독립 시험/실제 서버 수신→Core adapter→응답/입장 경로, 실제 Core DB 저장→재접속 접점, 양쪽 소비자 확인. 미실행은 판정 보류이며 문서·대역만으로 전체 통과하지 않는다. |

## 패킷·API 계약의 현재 제안

기준 main의 제품 코드는 protocol21·PDL48개다. `domains/protocol.md`에는 역사 v20 설명이 남아 있으므로 현재 값은 실제 `ProtocolVersion.Current`와 PDL/생성 enum을 기준으로 한다. 이번 제품 변화 시 현재 설명을 함께 맞춘다.

| 제안 ID | 제안 이름 | 핵심 내용 |
|---|---|---|
| 49/50 | `C_CharacterListRequest` / `S_CharacterListResult` | requestId와 고정3슬롯·occupiedMask |
| 51/52 | `C_CharacterCreateRequest` / `S_CharacterCreateResult` | slot·rawName·class와 Core가 만든 summary |
| 53/54 | `C_CharacterSelectRequest` / `S_CharacterSelectResult` | CharacterId 선택과 선택 결과. 직업 입력 없음 |

초안의 **49..54와 차기 protocol22는 승인 후 재대조를 거쳐 이 목표에 배정했다.** 최신 origin/main은 여전히21/48개이며 메인 `msg_239b72df7331`의 6개 checkout 관측, Core `msg_66dc4898461f`와 Content `msg_85481145274c`의 사용/예약0을 확인했다. 필드/번호/결과/FQN은 [전송 계약 v1](packet-contract-v1.md)에 고정했다. 제품은 아직21/48개이며 문서로 생성·배포되지 않았다. 새 충돌은 재사용하지 않고 새 계약으로 조정한다. 기존 ID/필드·equality handshake와 양쪽 소비자 검증은 보존한다.

- 보존 패킷 초안은 이전 근거 `character-packet-draft-01/packet-contract-v0.2.md`다. hash는 이번 `input-manifest.json`에 실제 파일에서 계산해 보존했다. 현재 계약은 별도 v1이며 과거 초안을 고치지 않았다. 고정 입력 목록과 hash는 `approval-01/packet-pretest-01/contract-input-manifest-v1.json`에 둔다.
- 전송 제안: 4바이트 header·숫자 explicit LE·UInt16 **바이트** 길이+UTF-16LE string, ID는 non-empty GUID D문자열, 목록은 고정3칸이다. 큰 문자열 할당/업무 호출 전에 구조·상한을 검사한다. 새로운 wire enum은 Core ordinal cast 대신 의미로 매핑한다.
- 서버의 계정/operation/session/generation/owner/fence/SQL은 wire에 싣지 않는다. NotFound/NotOwned는 외부 선택불가로 통합하고, 서버가 만든 계정·OperationId 입력 오류는 내부 진단을 보존한 IntegrityFailure로 구분한다.
- requestId는 nonzero·연결 내 재사용 금지, uint 끝에서 wrap하지 않고 새 연결을 사용한다. 화면의 `(연결, 종류, requestId)` 상관 처리 구현은 Content와 고정한다.
- 선택 성공 응답은 플레이 준비 완료가 아니다. Stored 성공은 **54 → 48의 저장 class → 실제 맵 입장 패킷 → 길드 가입/44** 순서다. 실패는 성공48/입장/등록을 만들지 않는다.
- Core API v2는 PrepareCreate/불변 descriptor, dispatch 전 host 미결 생성 소유권, 불변 Legacy/Stored를 고정한 **구현 계약**이다. 실제 코드·DB·독립 강 검증 완료 여부는 회신/새 근거로 확인한다.

## 파트 접점과 선행 조건

| 담당 | 필요한 인계 | 현재 상태/World 행동 |
|---|---|---|
| Core | API v1+v2의 의미·버전/hash, mode/catalog/admission 주입 타입, host 미결 생성/close 소유, 새 profile preflight, 실제 DB/독립 판정·PR 기준 | `msg_66dc4898461f`로 v2 hash·의미 불변, 실제 공개 선언·소유 경계·예약0을 재확인했다. [소비 접점 v1](core-consumer-contract-v1.md)에 연결했다. 실제 SQL/저장재접속/강 판정·main 인계는 남았다. |
| Content | 신규 UI 목표 시점, Unity wire 소비자/상관값 경계 소유, 새 패킷 제안 이견·번호 예약 여부 | `msg_85481145274c`: v0.2 추가 변경 요구/예약0, 비화면 책임 의견 일치. UnityClientSession/NetworkService는 현재 쓰지 않지만 길드 PR2에도 필요해 소유 이전 아님. 전체 종료/한정 이전은 미정. 기존48 입장 파일은 Core/메인 배정도 필요하다. |
| 메인 | 사용자 범위·착수 승인, 두 파트의 공유 파일 경계, 중량 실행 창, R-7 시범 해당 여부 | A 승인 완료. R-7 미적용 확정. 공유 실행 큐는 전달 기준 Art → Content → Core DB. World의 새 실행 창은 아직 없다. |

Core main 통합 전에는 고정 API 대역으로 패킷/수명 요구를 준비할 수 있다. 제품 branch에 Core 미검증 변경을 복사해 독자 저장 구현을 만들지 않는다. 최종 통합 때는 승인된 Core 결과와 최신 main에서 새 기준·충돌·실행 계약을 고정한다.

Core 회신 기준 실제 소비 접점은 `CharacterCatalogService(ICharacterCatalogRepository, ICharacterRepository, SafeDefaults, TimeProvider, int capacity=128)`, `CharacterAdmissionServices(CharacterCatalogService, TimeProvider)`의 `Mode/Catalog`, `ServerHost.StorageMode/CharacterCatalog`다. Startup의 profile2 preflight 뒤 Program이 host 생성 전 `world.AttachPersistence(Services)`를 호출한다. 선택 handle의 `Completion/Close(SafeCheckpoint?)`, lease의 `Checkpoint`, host의 `FlushAsync`를 사용한다. Core는 Sessions/GameWorld/PDL을 변경하지 않았다고 보고했다. World는 이 보고를 계약 입력으로 삼되 새 통합판에서 직접 대조한다.

Core는 생성 `_pending.Add` 뒤 dispatch, 기본 capacity128, host 복구는 원ID/descriptor로 1회·최대5초라고 보고했다. 이는 Core 코드 관측 보고이며 실제 SQL 실패 수명 증명은 아니다. 사용자 deadline과 별도 복구 예산을 혼동하지 않는다. **초기 전달의 「Core 실제 DB 검증 중」은 현재 상태가 아니다.** 회신 시점에는 관리자 별도 승인 전·DB 실행0이며 실제 인계 시점은 미정이다. 정적 시험 수치 원시를 World가 다시 실행/실사한 주장은 하지 않는다.

## PR 경계와 예상 단계

**제품 PR 한 개**로 전송 정의·버전·서버 adapter·봇/시험·양쪽 wire 소비자와 DLL 근거를 함께 검토한다. PDL/버전만 따로 병합해 기존 클라이언트가 먼저 끊기는 중간 배포는 하지 않는다. Content 화면 PR은 별도 목표이며 이 PR에서 전체 화면/플레이 완료를 주장하지 않는다. Core 제품 PR은 Core 소유다.

Content의 최소 wire 소비자를 별도 제품 PR에 둘 필요가 생기면, 양쪽 준비 전 배포되지 않는 합류점·완료조건을 메인과 먼저 재계획한다. 아직 PR 번호는 없으며 생기면 진척 단계에 실제 `PR번호 병합`을 적는다. 종료에는 규칙에 따른 Gardener·결과 포함 종료 기록 PR 하나·종료 점검을 별도로 둔다.

| 단계 | 예상 소요 | 선행·점검 |
|---|---|---|
| 범위 승인·파트 계약 | 0.5~1작업일 | API/번호/소비자 소유·실행 조건 고정. 승인 초안과 대조 뒤 다음 단계. |
| 새 선행 시험 | 0.5~1작업일 | 코드 goal TDD. 별도 세션에서 요구·보존 시험과 구현 전 실패 원시. 발행 시 임시 모델 결정을 적용한다. |
| 전송·World 연결 | 1~2작업일 | 고정 대역과 Core 인계 기반. wire/수명·오류·등록 경계, 쓰기 종료 확인. |
| 강 독립 검증·수정 | 1~2작업일 | 신규 Opus의 양쪽 소비자/실제 경로/DB 접점. 범위 안 결함 수정·재검증은 새 세션. |
| PR·CI·통합·종료 | 0.5~1작업일 | 각 PR 사용자 head 승인·메인 병합. Gardener·종료 기록/점검. |

독립 가능한 준비를 겹친 **총 경과 3~5작업일은 일정 추정**이며 각 행을 단순 합산한 작업량과 다르다. Core가 10-20~21에 검증 결과를 전달하고 World/Content wire 소유가 일찍 정해지면 10-23~24 연결 검증을 목표로 한다. 10-25~27은 수정·리허설 여유로 보고 10-28 기능 동결 전 완료 여부를 점검한다. Core 지연·공유 자원 창 부족·강 검증 보류는 이 일정을 밀 수 있으며 10-21 촬영 완료를 약속하지 않는다.

중간 점검은 계약 고정 뒤와 최종 Core 통합 뒤다. 완료조건을 막는 범위 안 결함만 고치며, 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다. 별도 확정 실패 집계와 섞지 않는다.

## 검증과 위임 계획

- **등급: 강.** 서버 권위·영속 경계·프로토콜/공유 DLL·입력 해석·시간/종료 수명을 바꾸기 때문이다. 현재 제품 diff0이며 변경 파일/줄 수는 아직 미측정이다. 계약/구현 뒤 실제 diff 원시로 기록한다.
- 구현자는 난이도 신호1~4에 따라 **신규 gpt-6-astra xhigh**다. 선행 시험과 독립 검증은 원래 신규 Opus 배정이지만 **위 한시 결정 기간에 새로 여는 세션은 각각 신규 gpt-6.1-sol xhigh**를 적용한다. 제품·시험 소유를 분리하고 발행 시각과 실제 화면 모델을 기록한다. 전송·서버 수명·Unity 소비자는 정확 소유권을 고정한 좁은 계약으로 나눈다. 리드는 제품 구현·테스트 작성·검증 판정을 대신하지 않는다.
- R-7의 실패 수명/오류 분류/비용 상한에 닿지만 **이번 목표의 시범은 미적용**이다. 메인 `msg_355be9930592`와 `msg_fb6c227d3b39`가 시범 한도 사용을 근거로 결정했다. Fable 세션을 열지 않으며 확정 실패3회 뒤 Advisor 규칙은 그대로다.
- 계약에는 사전 맥락·실제 경로 검사·CODE_CONVENTION 관련 절 원문·고정 입력/hash·원시 수치·쓰기 종료·한 작업 뒤 정산/종료·재사용 금지와 위 사용자 결정 두 문장을 넣는다. 같은 파일을 동시에 쓰지 않는다.
- 실행 순서안은 정적 입력/PDL/버전 대조 → 신규 선행 시험 실패 → 구현/생성 → SDK10.0.301 서버/Shared/ClientNet 빌드·회귀 → 현재 서버와 봇 경로 → 승인된 Core DB 저장/재접속 접점 → 격리 Unity6000.6.4f1 소비자/컴파일·관련 EditMode/회귀다. 정확 명령·입력·환경·hash·메모리·프로세스/포트·DLL 복사·정리 관문은 계약 발행 때 고정한다. 이 표는 실행 허가가 아니다.
- Unity·WSL 전체 시험·서버·DB 실증은 메인의 **「실행 창 시작」** 배정 후 승인된 단계 묶음으로만 실행한다. 원본 자산·DLL 부작용과 WSL/Windows 산출물 차이를 명시한다. 다른 파트 실행 창을 사용하지 않는다.
- 신규 독립 검증자는 실제 diff·보고·원시부터 실사하고 독립 테스트를 작성/보완·실행한다. 대역으로 실행한 실제 서버 진입, 실DB 접점, Unity 컴파일/시험, 실제 UI 플레이·Player build·CI를 각각 구분한다. 전체 PASS는 완료조건별 근거가 있을 때만 가능하다. DB/UI/Player 미실행을 과거 결과로 채우지 않는다.
- 이전 길드 검증의 ENV-D01 한 줄 설정 차이는 당시 한정 수락이다. 새 실행에 자동 적용하지 않으며 정확 변경·원본 보존·실행 wrapper/실제 결과와 미입증 범위를 기록해 메인에게 판단받는다.

## 진척 단계

- [x] 범위 초안
- [x] 범위 승인
- [x] 서버 계약
- [x] 전송·소비 선행시험 — 패킷/Unity 최초 RED 완료, 서버 adapter 시험은 연결 단계에 남음
- [x] 클라이언트 계약 — 소비 v1 확정, 공용2파일 실제 인계는 연결 단계에 남음
- [>] 전송·서버 연결 — 전송217·Unity 실제session313 GREEN, 서버/Core·Content 통합 남음
- [ ] 독립 강 검증
- [ ] 제품 병합
- [ ] 종료 기록
- [ ] 종료 점검

## 결정과 단계 결과

- 새 리드의 읽기→사전 맥락→새 branch/Run→READY를 수행했다. 진입 시 추적 변경0, 최신 main과 새 HEAD의 diff0을 확인했다. 사전 맥락은 `.backups/verification/2026-10-18-character-server/context-entry.md`, 원 지시는 `inbox-before-run.json`이다.
- Core/Content에는 준비 상태와 기술 경계만 문의했고, 둘 다 현재 작업 중인 화면을 확인해 터미널 입력을 넣지 않았다. Core 회신으로 실제 준비·host 접점·미실행을 구분했고 Content는 회신 대기다. 작업자·검증자0, 제품/시험/PDL/DLL 변경0, 중량 실행0이다.
- 이 초안 자체점검은 내용·로컬 원천·현재 Git/PDL의 정적 대조다. 독립 문서 통과·코드/DB 검증·목표 완료를 뜻하지 않는다. 승인된 산출물의 독립 세션 절차는 생략하지 않는다.
- 사용자 A 승인 뒤 원본 입력을 동결하고 승인 원문·R-7 미적용·임시 모델 원문과 진척 단계를 반영했다. 승인 기록 commit과 실제 준수·자기 쓰기 감사는 근거 `approval-01/`에 둔다. Content 회신의 합의 방향은 근거 `coordination-note-01.md`이며 Unity 파일 쓰기 배정은 아직 없다.
- 승인 기록 commit `2bf2206621d31ee3675af1ee74b67348abe46d90`은 goal/CURRENT 두 파일만 포함하며 제품 diff0이다. 이어 서버 전송·Core 소비 접점을 각각 v1 문서로 작성하고 패킷 시험 작성 계약을 발행했다. 계약 고정과 작성 완료는 문서 실사·강 검증 통과를 뜻하지 않는다.
- 메인 `msg_0b519c7ab848`(2026-10-18T05:49:03Z)은 Unity 소유 관문이 Unity 쓰기/소비자 동작을 정하는 계약에만 적용된다고 확인했다. 준비된 패킷 시험 **작성만**의 발행을 허용하며 실제 compile/RED는 실행 창 뒤 별도 계약으로 둔다. 원문은 근거 `approval-01/inbox-next-04.json`이다. 기존 v1 spec을 보존하고 권한 확인만 반영한 새 발행 버전/입력 manifest를 만든다. 전체 목표의 C1~C9·강 판정/PR 경계는 바꾸지 않는다.
- **패킷 시험 작성 결과:** 신규 외부 Sol xhigh가 `CharacterMenuPacketContractTests.cs`와 `CharacterMenuPacketFixtures.cs`를 작성했다. 기존1..48 보존·새6패킷/공개enum·독립 byte/offset fixture를 요구로 검사하며 새 타입 부재는 reflection assertion으로 남긴다. 코드/보고·실제 diff·입력/출력 해시를 리드가 정적으로 대조하고 `worker_done succeeded`를 **작성 준비 완료만**으로 정산했다. 실제 실행·독립 판정은 미수행이다. 원문과 Task/Dispatch·수치/해시는 근거 `approval-01/packet-pretest-01/report.md`, `lead-post-settlement-audit.json`, `lead-settlement.md`에 있다.
- **정산·종료:** 완료 Dispatch를 release한 뒤 외부 pane 유지 반환을 구분하고 정확 작성자 pane만 닫았다. 리드 생존·현재 worktree에 리드 pane만 남음·reclaimable0을 확인했다. 생존 신호 helper의 초과 구간은 이유 미기록으로 정산 원시에 남겼으며 질문 대기와 분리했다. 신호 초과만을 이유로 추가 대화 감사를 만들지 않았다. 제품 확정 실패와 섞지 않는다.
- **CONTRACT-PS01(첫 발생):** 리드가 근거/harness 작성을 허용하면서 PowerShell 언어절 제외 이유를 쓰기0으로 적어 실제 일회성 정적 점검 script 작성/실행과 차이가 생겼다. 작성자의 자진 공식 질문 뒤 메인 `msg_dd0cf0173e26`에 보고하고 실행 원본·원시를 동결했다. 정산 보충1.2에 관련 규칙 원문 전체를 제공했지만 사전 누락을 소급 준수로 만들지 않았다. 후속 메모에 가독성 지적·미검토를 남겼고 script는 공용도구로 채택/재사용하지 않는다. 교정은 이번 리드 계약의 원문 선택·제외 설명 층에서 처리했다. 공용 검사 구현은 승인 범위 밖이며 첫 발생 기록이므로 새 반복 규칙을 만들지 않았다.

## 보류와 남은 우려

전송217GREEN·Unity 실제session313GREEN과 원본Shared22 배포는 완료했다. World 자기 기준의 공용2파일·최소handler 연결은 끝났고 Content 완성PR2의 Guild 의존성과 함께 직렬통합해야 한다. Core 실제DB/최종통합·강판정과 실제 서버54→48/입장·양쪽 전체 소비자 검증이 남는다. 후속 중량 실행은 사용자 캐시 자동 갱신의 관측·보존을 포함한 새 READY/실행창이 필요하고 현재 단계 GREEN과 DLL 배포 자체점검은 C7전체·C1~C9의 독립 강 판정을 대신하지 않는다.

TRX helper와 봇 등록/회귀 목록 드리프트는 앞 목표 Gardener의 미채택 후보이며 새로 채택하지 않는다. 기존 BACKLOG와의 통합 판단은 메인에게 남긴다. 범위 밖 후보 장부나 운영 규칙 정비를 이번 초안에서 늘리지 않는다.


## 안정화 후보

- 10-28 뒤: 감시·검사·운영 도구의 공통화/보강, receipt 자동 검사와 기능 완료조건을 막지 않는 부가 경계 사례 방어. 지금은 알려진 한계로 기록하고 기능 산출물을 잇는다(msg_5c3dc59360f6).
