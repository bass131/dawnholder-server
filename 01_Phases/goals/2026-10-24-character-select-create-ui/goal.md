# 캐릭터 선택·생성 화면

## 재개 지점

**다음 첫 일:** Main이 여는 새 Content 리드는 현재 경로·모델·handle/incarnation을 확인하고 같은 Content Run을 인수한 뒤 Main에 회신 주소를 알린다. 이 goal의 승인 범위와 World 최신 인계를 읽고 화면/입력 구현 계약을 준비한다. 현재 리드는 정본화·인계 뒤 쓰기를 끝내며 새 worker나 Unity 실행을 시작하지 않는다.

- 현재 checkout은 C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active, branch feat/content-character-select-create-20261024다.
- 최초 branch base/현재 HEAD는 eda68f4315ba3039d602691a0ac2f309dc9d8b7c다(2026-10-24 03:27 KST fetch·원격 대조). **Content 화면 제품 PR의 최종 base는 World 제품 PR 병합 뒤 main**으로 다시 고정한다. 최초 base와 혼동하지 않는다.
- Main msg_d31b10895281이 보존 배치·scope 원문을 직접 읽고 hash 대조 후 승인했다. 현재 제품/신규시험/새 worker/새 worktree/Unity·WSL 실행은 0이다. 새 goal과 CURRENT Content줄은 이 branch에서 정본화한 미커밋 문서이며 다음 writer는 새 Content 리드다.
- World는 Main 판단에 따라 최신 main을 먼저 통합한다. main에 아직 v22/API가 없으므로 화면 전용 준비와 최종 API 통합·실행을 구분한다. World 공용2파일은 읽기 전용이다.
- 아침 사용자 손 일정은 **Core 합동 창 먼저 → Content housing 직접 플레이 45분**이다(Main msg_49f6c8888cb8). 새 리드의 housing READY에는 UI writer 쓰기 종료·보존 → housing branch 복귀 → 정확 source/DLL·snapshot 입력 고정 → Unity 재가져오기 → 실행·반환 → 필요 시 UI branch 복귀 순서와 재가져오기 시간 추정을 넣는다. 시간은 아직 측정하지 않았으므로 추정/미측정으로 표시한다. Core 종료·Main 창 승인 전 실행하지 않는다.
- 이 파일이 새 UI의 기준·상태·결과 정본이다. housing의 최신 상태는 아래 고정 branch/commit과 보존 사본에 있다. 현재 branch에 보이는 같은 housing goal 경로를 최신 housing 실행 결과로 읽지 않는다.

### 하우징 보존과 최소 인계

- 기존 branch feat/content-guild-housing-20261022는 원래39커밋을 유지하고 goal 단일 checkpoint 14cb5361183e1c6179b2e4fed299f84c389ac51b를 추가해 같은 원격 branch로 push했다. git ls-remote도 같은 SHA를 확인했다. force/reset/rebase/기존 커밋 재작성은 하지 않았다.
- checkpoint의 대상은 01_Phases/goals/2026-10-15-guild-client-ui/goal.md 한 파일(43삽입·3삭제)이다. [고정 Git 원문](https://github.com/bass131/dawnholder-server/blob/14cb5361183e1c6179b2e4fed299f84c389ac51b/01_Phases/goals/2026-10-15-guild-client-ui/goal.md)을 쓴다.
- 기계 원시는 E/housing-checkpoint-preserved-v1.json과 commit/push 출력이다. E는 이 checkout의 .backups/verification/2026-10-24-character-entry-prep다.
- housing-goal-fixed-95bde79f.md의 SHA256은 95BDE79F78D69CB096956DC93C466FF0BECB9D461F9E11A9DA7A9157E0684F4D이며 branch 전환 전 원본과 exact bytes가 일치했다. Git의 LF 정규화 blob ID는 83d58e460a6eb9f3e9e0d0a1a3a85c85707d7693다. raw 사본 SHA와 정규화 blob ID를 혼동하지 않는다.
- housing goal은 미완료다. 다음 일은 사용자 직접 플레이 45분의 READY이며 아침 Core 합동 창 뒤에 실행한다. 새 UI 시작으로 housing 완료조건을 낮추지 않는다.
- 마지막 housing Task task_d64c357823cd / Dispatch ctx_9b59f2a44392는 failed로 정산되고 worker pane은 종료·해제됐다. 이 ID/handle을 재사용하지 않는다.
- housing 최종 정산은 .backups/verification/2026-10-15-guild-client-ui/housing-v3-20261022/resume-20261023-new-lead-manual-task-final-settlement-v1.md, SHA256 0CA52D3BE11FD8B07460BCC5DC09F166E47503621A00825F99F72A4562AF2B44다.
- W4 원시는 이미 봉인됐다. 302파일/24,571,107byte, 609pins diff0은 당시 housing 정산 값이며 새 UI 실행 근거가 아니다. W4를 수정하거나 미실행 c~f/구매·성장·재접속·목표 해상도를 PASS로 바꾸지 않는다. 실제 확인/미확인은 고정 housing goal·최종 정산을 따른다.
- Core/World 저장·재기동 합동, Content 새 UI 실제 화면, housing 사용자 직접 플레이는 각각 별도 근거다. 현재 새 UI 실제 게임/직업 유지 시연은 미실행이다.
- Main 연락처는 term_af8df74f-854d-4b86-9022-65d6cd2c14d8, World 회신은 run:run_90d18490c825, Content Run은 run_5ff5a7b7de60다. 새 리드는 R-8의 run-use로 Main이 승인한 같은 Run을 인수하고 새 handle/run 주소를 알린다. 실제 대상/runtime/incarnation은 사용 직전 재확인한다.
- 현재 리드 요청/직전 화면은 GPT6.1-Sol xhigh fast이고 backend actual은 unknown이다. 새 리드 생성·기존 pane 닫기는 Main이 한다. 문서 기록을 실제 모델/맥락 전환으로 보고하지 않는다.
- housing와 새 UI는 같은 checkout을 쓰므로 동시 writer/branch 전환을 금지한다. 각 전환 전 쓰기 종료와 변경 보존을 확인한다. 추가 worktree는 승인하지 않았다.

## 적용 중인 사용자 결정

Main이 전달한 사용자 결정과 Main의 운영 승인을 구분한다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

- 사용자 A(Main msg_b0a2013d971a): 10-28 시연 장면9에 저장·재접속/직업 유지 장면을 넣고 캐릭터 서버를 재개한다. Content는 기존 캐릭터 고르기 또는 이름·직업 생성 후 마을 입장 화면을 새 goal로 맡는다. 빠르게 기반부터 진행한다.
- 유지한 사용자 결정: 10-22 경량 경로(구현자1명 구현·실행·자체 확인·수정, PR 직전 신규 Opus 독립 검증1회), 기존 시험 준비 코드·기대값 변경은 사용자 결정, 10-19 모델 라우팅과 새 Codex service_tier="fast". 자세한 범위는 아래 작업 방식 절을 따른다.
- Main msg_d31b10895281: 배치 v1 SHA256 871685FEDAB388979B44EFA5C923FE78BD2B4A91B81052170D66FC7EDC584232, scope v1 SHA256 4BB80AAB7028E71ADEF9EE12DB7E0812380F3A336EC31ACF751A1FF0C73C9D87을 그대로 승인했다. housing 단일 checkpoint/push, 최신main 새branch·goal/CURRENT, 실제SHA 인계 뒤 리드 교체 순서를 승인했다.
- 같은 Main 지시의 갱신 사실: World가 최신main을 먼저 통합한다. UI 제품 PR base는 World 제품 PR 뒤main이다.
- 사용자 아침 순서(Main msg_49f6c8888cb8): 사용자가 서버 두 번 직접 기동하는 Core 합동 창을 먼저 열고 Content housing 직접 플레이45분은 그 뒤다. 원문은 E/mail-during-placement-v1.json이다.
- 밤사이 병합·새 사용자 결정·사용자 손·범위 확대는 Main 현황판에 올린다. 실행 창은 READY 도착 순으로 Main이 하나씩 열고 이미 승인된 범위의 준비는 계속한다.

승인 원문은 E/mail-next-wake-v1.json, 승인 요청 원문·고정 scope/배치·World 회신/인계·11입력 hash 대조는 같은 E의 파일을 따른다. 아래 만들 것부터 PR 경계까지는 승인된 scope v1과 같은 기준이며 승인 전 표현·재개 상태만 갱신했다.

## 만들 것

같은 계정의 저장된 캐릭터를 목록에서 고르거나, 빈 슬롯에서 이름과 직업을 골라 새로 만든 뒤 서버가 승인한 마을 입장으로 이어지는 기본 화면을 만든다. 10-28 시연 장면 9의 저장·재접속을 화면으로 사용할 수 있게 한다. 직업 저장/DB 재기동 보장은 World/Core 소유이며 Content는 서버 응답을 정확히 표시·소비한다.

- 목록은 서버 고정 3슬롯의 이름·직업·빈칸을 보여 준다. 선택된 슬롯/생성 중/선택 중/연결 중을 구별하고 중복 버튼 입력을 막는다.
- 생성은 slot1..3, Knight0/Mage1, 사용자가 입력한 rawName 원문을 기존 API로 보낸다. NFC·이름 유일성·저장 여부는 Core가 판단하며 UI가 trim/새 정규화/DB 정책을 복제하지 않는다.
- 생성 성공은 생성 결과를 표시하고 명시적인 캐릭터 선택을 거친다. 생성 성공만으로 마을에 보내지 않는다. 같은 캐릭터를 다음 로그인 목록에서 다시 골라 입장할 수 있다.
- 기존 UI 패널·버튼·글꼴·색/상태 표현을 재사용한다. 화면 가독성과 기본 조작에 집중하며 새 대형 아트 제작을 선행시키지 않는다.
- 목록 실패를 정상 빈 목록으로 표시하지 않는다. 이름/슬롯/직업 입력 오류, 사용 중/잠시 이용 불가, 확인이 필요한 상태를 서버 결과에 맞춰 표시한다. 내부 SQL/진단문을 사용자 화면에 노출하지 않는다.
- 뒤로 가기/연결 종료/재접속/화면 파괴 시 현재 client 구독과 UI 상태를 정리한다. 옛 연결 응답이 새 목록/선택/마을 입장에 영향을 주지 않게 한다.

## 건드릴 곳과 소유

| 영역 | 제안 범위·소유 |
|---|---|
| Content 화면 | 03_Client/Assets/Scripts/Scenes/CharacterSelectController.cs와 Assets/Scenes/00.Menu/CharacterSelect.unity, 필요한 신규 화면 view/presenter와 각 meta |
| 메뉴 진입 | UI/MainMenuController.cs 및 MainMenu.unity는 명시 메뉴 화면 진입에 필요한 최소 연결만. 기존 계정 정규화/서버 주소/probe/실패 안내 보존 |
| 기존 UI 재사용 | UI/UiWindowAppearance와 기존 패널/글꼴/자산 소비. helper 자체 변경은 필요를 입증하고 범위 안 최소 수정 |
| 시험 | UI 표시/입력/pending/구독 수명과 실제 API 소비의 신규 시험. 기존 시험의 준비 코드·기대값 변경은 사용자 결정 전 0 |
| goal·추적 | 승인 뒤 새 goal 경로와 CURRENT Content 항목. housing goal의 미완료 상태/재개 포인터 보존 |
| World 읽기 전용 | PDL·Generated·ProtocolVersion·CharacterMenu 비화면 helper/handler·ClientConnectionLifetime·NetworkService·UnityClientSession |
| Core 읽기 전용 | CharacterLoad 권위/48 handler·계정/이름 정책·DB/catalog/admission·프로그램 기동 |

World 공용2파일에 필요한 변경 요청은 현재 0이다. 현 API로 부족한 접점이 발견되면 파일/구간/시작·종료를 World/Core와 먼저 조율하고 Main 범위와 대조한다. 별도 연결 stack, 직접 패킷 송신, UI 전용 requestId를 만들지 않는다. 메뉴씬에서 NetworkService를 확보하는 방식과 기존 PersistentServices 수명을 구현 계약 전에 좁혀 확인한다.

## 하지 않을 것

- 서버·PDL·패킷 번호·직업/저장 정책·DB 스키마 변경, 캐릭터 삭제/이름 변경/외형·새 직업·슬롯 확대.
- housing의 39커밋을 새 UI branch/PR에 합치거나 기존 housing 실행 봉인을 고치는 일.
- 목록 preview/로컬 PlayerPrefs 직업/54 성공으로 ClassLoadout 또는 gameplay ready를 만드는 일. Stored 실패 시 legacy로 자동 우회하지 않는다.
- 새 timeout/자동 재생성/보상성 rollback, OutcomeUnknown의 같은 연결 재시도, 새 도구/collector/harness 수리 루프.
- 별도 아트 전면 개편·범위 밖 UI·서버 자동 병합·전역 환경 설정·사용자 대신 직접 플레이.
- World/Core의 최종 검증과 현재 Content의 정적 계약 확인을 통합 PASS로 표현하는 일.

## 고정 소비 계약과 의존

World 인계는 world-api-handoff-fixed-v1.md 및 world-input-manifest-fixed-v1.json에 보존했다. World working HEAD f27946da0b8d4e16947a36ddda08f724b8c3e21f + 미커밋 입력이며 world-input-sha-check-v1.json에서 11입력 SHA가 모두 일치했다. 이는 API 인수 확인이고 제품 독립 검증이 아니다.

- protocol22, 49/50 목록·51/52 생성·53/54 ID선택, 기존1..48 보존.
- NetworkService.ConnectCharacterMenu(hostOverride)와 현재 CharacterMenu getter를 소비한다. 로그인 Accepted 뒤 최초 TryList 1회는 facade 소유다.
- CharacterMenuClient의 TryList/TryCreate/TrySelect, Changed/HasPending/PendingRequestId/LastReply를 쓴다. Slots는 정확3칸·빈칸null이다. 새 연결마다 새 client 참조를 사용한다.
- 선택 완료는 상관이 맞는54 성공 →48의 서버 저장 class 적용 →기존 서버 map entry/씬 로드 →실제 entry barrier 순서다. UI가 Town을 미리 강제로 로드하거나 기존 GameEntryPoint의 접속을 중복시키지 않는다.
- result0..16/invalidField0..4를 보존한다. 12 OutcomeUnknown은 원 결과를 안내한 뒤 helper의 연결 종료를 따른다. 실패/닫힘/옛 세대의 뒤늦은 결과를 성공으로 바꾸지 않는다.

World 후속 msg_4cb5d03d42fd는 Core의 기존 launcher에 사용자 직접 실행이 남아 밤사이 실재기동이 보류될 수 있다고 알렸다. 이는 World 전달 상태이며 Content가 Core 실행을 직접 확인한 결과가 아니다. 사용자 손이 필요한 새 입력을 대신하거나 새 launcher를 만드는 범위로 넓히지 않는다.

현재 main eda68f43에는 v22/API가 없다. World 제품 PR(서버·wire·양쪽 consumer·일치 DLL) 병합 뒤 Content UI PR이 기본 경계다. 승인된 범위 안에서 API와 독립인 화면 전용 구성·시험 설계는 병행할 수 있지만, 미통합 전송 코드를 복사하거나 혼합 v21/v22로 실행하지 않는다. 실제 API adapter 컴파일/통합/실행은 World 최종 main 기준을 다시 고정한 뒤 수행한다. 필요한 기존 제품이 새 main에 없다면 선행 의존으로 보고한다.

## 관찰 가능한 완료조건

| 번호 - 내용 | 완료 증거 |
|---|---|
| 1 - 저장 목록·빈칸 표시 | 0/1/3 캐릭터와 Knight0/빈칸 구별, 실패 목록과 정상 빈 목록 구별의 신규 시험 및 실제 화면 |
| 2 - 이름·직업 생성 | 빈 슬롯의 이름/직업 입력→서버 결과 표시. 정상과 입력/이름 충돌/가득 참의 의미 확인. 생성만으로 입장하지 않음 |
| 3 - 기존 캐릭터 선택과 마을 입장 | 실제 UI 입력→53/54→48 서버 class→실제 map/entry 흐름을 연결한 근거. 로컬 preview가 권위를 만들지 않음 |
| 4 - 저장·재접속 장면 | 같은 계정 재접속 후 목록에서 같은 character ID·이름·직업을 확인하고 선택/마을 입장. 서버 재기동 저장 검증은 Core/World 합동 근거와 연결하고 UI 자체 저장 검증으로 대체하지 않음 |
| 5 - 실패·수명 보존 | pending 중복 거절, 뒤로가기/닫힘/재접속/옛 client 구독 해제·늦은 응답 무효. OutcomeUnknown은 반복 생성 없이 연결 종료 안내 |
| 6 - 기존 동작과 표시 | 계정/서버 입력·실패 안내와 기존 게임 진입 계약 보존. 1920×1080 시연 화면 및 기존 최소 대상 해상도의 글자·겹침·버튼 접근 확인 |
| 7 - PR 전 판정 | 구현자 자체 실행·수정 종료 후 신규 Opus 1회의 독립 강 검증 원문과 실제 diff/실행 근거. 미실행 플레이/DB·사용자 직접 조작은 별도 표시 |

서버/Core 의존이 미완료이면 3·4를 PASS하지 않는다. 검증 중 제품 결함만 같은 승인 범위에서 수정하고 요구 범위/기존 시험 기대값 변경은 Main이 사용자 결정으로 올린다.

## PR 경계와 작업 방식

- 제품 PR 하나: 캐릭터 선택·생성 기본 화면과 최소 메뉴 진입·신규 시험·필요한 meta/scene·새 goal 기록. 선행 World main의 정확 merge SHA를 base에 고정한다.
- housing branch와 사용자 직접 플레이 결과는 이 PR에 싣지 않는다. 제품 병합 뒤 종료 기록/Gardener는 현행 절차를 따른다.
- Main 10-22 경량 경로: 신규 구현자 한 명이 구현·실행·자체 확인·수정, PR 직전 신규 독립 Opus 1회. 선행 시험 작성 세션을 추가로 열지 않는다.
- Main 10-19 라우팅: 구현자 Sol max, 같은 계약 두 번째 확정 실패만 신규 Astra xhigh. 새 Codex service_tier="fast". 위험/DB 경계 독립 검증은 Opus --autocompact 250000. 지정 모델 가용성은 launch/화면/backend를 구분하고 임의 대체하지 않는다.
- 기존 시험 준비 코드/기대값 변경은 사용자 결정 전 하지 않는다. 신규 시험은 제품 계산을 복제한 기대값을 만들지 않는다.
- 실행 창은 READY 뒤 Main이 하나씩 승인한다. 정확 입력/hash/DLL·명령·snapshot/Prefs 보호·기존 자원 관문과 반환을 계약에 고정한다. 리드는 제품 구현/실행을 대신하지 않는다.

## 실행 창 추정

실측 일정이 아닌 계획치다. World/Core 완료·Main 순번·빌드 실패에 따라 달라진다.

- 구현자의 첫 실제 실행 창 60분 제안: 입력/자원/백업 확인10분, 기존·신규 Unity 시험10분, Player 빌드10분, 서버/연결 준비5분, UI 생성·선택·재접속 관찰15분, 종료·보호값/자원 반환10분.
- 서버 재기동/DB 합동은 Core/World가 소유한 별도 창이다. Content가 임의로 새 서버/DB를 띄우지 않는다. 같은 실행을 공유할 필요가 있으면 Main이 소유·순번을 조율한다.
- PR 직전 신규 Opus 창은 별도 45분을 임시 제안한다. 실제 diff/필수 실행을 보고 READY에서 다시 산정하며 검증 시간을 맞추기 위해 필수 항목을 생략하지 않는다.
- 아침은 Core 합동 창(사용자가 서버 두 번 직접 기동)을 먼저 열고 housing 직접 플레이 45분을 그 뒤에 연다(Main msg_49f6c8888cb8). 새 UI 실행 창과 겹치지 않는다.

## 진척 단계

- [x] 하우징 보존
- [x] 범위·배치 승인
- [>] 새 리드 인계
- [ ] 서버 기준 고정
- [ ] 화면·입력 구현
- [ ] 입장·재접속 확인
- [ ] 독립 검증
- [ ] 제품 PR 병합
- [ ] 종료 점검

PR 번호가 정해지면 병합 단계 이름을 해당 PR번호와 병합으로 바꾼다. Main이 새 리드의 실제 준비·Run 인수를 확인한 뒤 인계 단계를 완료로 바꾼다. 현재 구현·검증·PR·병합은 미착수다.
