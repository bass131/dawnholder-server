# 던전 클리어·보상과 눈에 띄는 결함 둘

상태: **범위 승인(2026-10-10). 결함 PR(PR221)은 사용자 승인으로 병합됐다(2026-10-10T15:54:25Z). 실화면이 찾은 HUD 골드 7자리 넘침(S-1)도 그 PR에서 고쳤다(R10). 지금은 던전 패킷·창 PR의 범위 초안을 메인에 올리는 단계다. 이어갈 곳은 [재개 지점](#재개-지점)이다. 다음 goal은 자동으로 시작하지 않는다.**

- 담당: Content 리드(`[Content 리드 Opus]`). 시작 기준 `origin/main` = `cc20d428f7988fdcb232a7c811cf2e729446abdc`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`. 결함 PR branch `fix/hud-gold-inventory-panel-20261010`(base `cc20d428`, 병합 뒤 원격 삭제). 던전 패킷·창 PR branch `feat/dungeon-clear-packets-20261011`(base `e775bc77`).
- 착수 원천: 메인 `msg_9fe1d49e00bb`(2026-10-10T07:39:37Z)가 전달한 사용자 승인. 원문은 아래 [적용 중인 사용자 결정](#적용-중인-사용자-결정)에 있다. 범위 원문은 리드 초안 `msg_f1bba119c853`이다.
- 로컬 원문·계약·판정은 `.backups/verification/2026-10-10-dungeon-clear-rewards/`에 둔다(Git 밖).

## 범위

파티가 보스방에서 보스를 잡으면 「던전 클리어」로 보고 그 보스방 안의 전원에게 보상을 주고, 보상 창의 「확인」으로 마을에 돌려보낸다. 인스턴스 맵이 들어오기 전에는 눈에 띄는 결함 둘과 던전 패킷·보상 창을 먼저 끝낸다.

| 항목 | 내용 |
|---|---|
| 만들 것 | (결함) HUD 골드를 서버 재화에 연결한다. 인벤토리 패널을 「I」 키로 열고 닫는다. (던전) 보스방 복사본의 클리어 정의와 보상표, 클리어 결과 패킷과 확인 요청 패킷(39·40번), 클라이언트 보상 창과 「확인」 전송, 사냥터 포탈의 던전 입구 표시, 서버 클리어 판정과 보상 원자 지급, 보스 처치자 보상을 클리어 보상으로 합치기, 「확인」 뒤 마을 복귀 요청, 봇 두 파티 시나리오, 기능 문서. |
| 건드릴 곳 | (결함) 클라이언트 `UI/HudController.cs`, `UI/InventoryPanel.cs`·`UI/InventoryPanelView.cs`, 입력 연결, EditMode 시험. R10은 `Scenes/99.UI/UI.unity`의 HUD 골드 글자(`GoldText`, 필요하면 `Gold_bar`) 직렬화 값과 PlayMode 시험(사용자 결정 37 A). (던전 패킷·창) PDL·생성 패킷·프로토콜 버전, Shared GameData의 던전 보상표(새 폴더), 클라이언트 새 보상 창·수신 핸들러. (던전 서버) 서버 새 던전 폴더, `Items/KillRewardPolicy.cs`·`Items/InventoryRegistry.cs`의 보상 연결, `GameMap` 보스 사망 분기 안쪽과 `GameWorld` 처치 콜백(World 인스턴스 맵 PR 병합 뒤), `HandlerRegistry` 등록 한 줄, 새 핸들러, 서버 시험, 봇. 마을 복귀는 World의 「서버가 시키는 이동」 진입점을 부르기만 한다. 이 goal.md, CURRENT의 Content 줄, FEATURE_MAP·영역 문서의 해당 줄. |
| 하지 않을 것 | 새 맵 번호·맵 데이터·포탈 줄. 인스턴스 생성·입장·정리, 맵 종류 표, 맵 이동, 보스 처치 때 퀘스트 초기화 범위(모두 World). 새 아트·보스 패턴. 길드·거점. DB 저장. 난이도·매칭·재입장 제한·시간제한·자동 퇴장. 기존 보스방 → 엔딩 → 마을 길의 제거. 다른 대기 후보(`inventory-ui-readability-followup`, `inventory-label-ellipsis-glyph`, `hud-pointer-attack-ux` 등). Unity scene·prefab 저작은 필요하면 메인에 요청한다. |
| 관찰 가능한 완료조건 | 아래 여덟 개다. |

1. HUD 골드가 서버 재화와 같은 값을 보이고, 첫 재화를 받기 전에는 0 대신 빈 값을 보인다. 실화면에서 HUD와 패널의 재화가 서로 다른 값으로 보이는 순간이 없다.
2. 「I」 키로 패널이 열리고 닫힌다. 게임 화면 진입 때 닫혀 있고, 같은 연결의 맵 이동 뒤에도 열림·닫힘이 유지된다. 닫힌 동안 패널은 클릭과 공격 입력을 막지 않는다.
3. 실화면 세 크기(709×399, 802×451, 1920×1080)의 마을·사냥터·보상 시점에서 HUD와 패널의 겹침이 0이다. 측정은 이전 goal의 PR191 실화면 검증과 같다.
4. 봇 두 파티가 각자 다른 보스방 복사본에서 보스를 잡는다.
5. 보스 사망 틱에 그 보스방 복사본 안에 있던 사람 전원이 클리어 보상을 정확히 한 번 받는다. 들어간 뒤 파티를 나갔어도 복사본 안이면 받는다.
6. 서버 시험이 다섯 가지를 증명한다. 같은 클리어 보상이 두 번 나가지 않는다. 클리어 뒤 들어온 사람은 받지 않는다. 사냥터 복사본에 남은 파티원은 받지 않는다. 가방이 가득 차거나 재화가 상한이면 그 사람의 보상 전체를 거부하고 거부 결과를 보낸다. 보스 처치자에게 따로 가던 보상은 나가지 않는다.
7. 실제 클라이언트에서 마을 → 사냥터 → 보스방 → 보스 처치 → 보상 창 → 인벤토리 표시 → 「확인」 → 마을 복귀를 확인한다.
8. World 인스턴스 맵 위에서 기존 전투·파티·퀘스트·일반 처치 보상이 그대로 동작하고, 보스방 → 엔딩 → 마을 길도 남는다. 게임 틱 안에서 I/O를 기다리지 않는다.

화면 겹침과 실제 플레이(완료조건 3·7)는 EditMode 시험으로 고정할 수 없다. Canvas 배치·실제 입력 장치·서버 연결이 함께 필요하기 때문이다. 이 둘은 실화면 실측으로 대신한다.

### PR 경계와 점검

| PR | 내용 | 시작 조건 | 끝나는 조건 |
|---|---|---|---|
| 결함 PR | 완료조건 1~3. 클라이언트만 바꾼다. 이 goal.md와 CURRENT의 Content 줄도 넣는다. | 범위 승인 | 독립 검증·실화면 확인 뒤 사용자 병합 승인 |
| 던전 패킷·창 PR | 39번 클리어 결과와 40번 확인 요청 패킷, 프로토콜 버전 17 → 18, 던전 보상표, 클라이언트 보상 창·핸들러·입구 표시. 서버는 이 PR에서 새 패킷을 보내지도 처리하지도 않는다. 등록되지 않은 요청은 지금처럼 버린다(`Sessions/GameSession.cs:177`). | 결함 PR의 Unity 작업 뒤(같은 checkout) | 새 버전의 서버·봇 접속과 기존 봇 회귀, Unity 컴파일·EditMode 시험. 실화면은 다음 PR에서 한다 |
| 던전 클리어 PR | 완료조건 4~8. 서버 클리어 판정·보상·마을 복귀 요청 처리, 보스 처치자 보상 합치기, 봇 두 파티, 실제 플레이 | World 인스턴스 맵 PR 병합. 규칙 단위 설계·시험은 그 전에 시작한다 | 독립 검증·실제 플레이 뒤 사용자 병합 승인 |

각 PR은 신규 독립 검증, 정확한 head의 CI와 원시 실행 근거를 메인에 보고한 뒤 **그 PR 병합 직전 사용자 명시 승인**을 받는다. 자동 병합하지 않는다. 중간 점검은 던전 패킷·창 PR 병합 때 인스턴스 맵 진행과 일정을 함께 본다. 종료 점검은 전체 PR 병합·결과 기록·Gardener 뒤다. 같은 산출물 수정이 3회를 넘으면 메인에 체크포인트를 알린다. 확정 실패 집계는 [ORCA 실패 정본](../../../00_Document/operations/ORCA.md#confirmed-failures)을 따른다.

| 시점 | 할 일 |
|---|---|
| 인스턴스 맵이 main에 들어오기 전 | 결함 PR 전부. 던전 패킷·창 PR 전부. World 리드와 기술 계약 확정. 던전 클리어 PR의 설계 절 확정, 맵 하나 위의 클리어 규칙 단위 시험과 규칙 단위 구현(새 던전 폴더 안, World·Core 파일은 건드리지 않음). |
| 들어온 뒤에만 | 보스 사망 분기와 처치 콜백 연결, 보스방 복사본 플레이어 목록 사용, 「확인」 → 마을 복귀, `KillRewardPolicy`·`InventoryRegistry` 수정, 봇 두 파티, 실제 플레이. |

## 설계

### 결함 PR의 요구

HUD와 패널은 서버 상태를 표시만 한다. 재화를 클라이언트에서 계산하지 않는다. 요구 번호는 선행 시험과 판정이 같은 번호를 쓰도록 여기서 고정한다.

| 번호 | 요구 | 확인 방법 |
|---|---|---|
| R1 | HUD 골드는 연결의 인벤토리 미러(`State/InventoryState.cs`)의 재화를 보인다. 미러가 바뀌었다고 통지하면 같은 통지 안에서 새 값으로 바뀐다. | EditMode 시험 |
| R2 | 미러에 snapshot이 없으면(첫 수신 전, 연결 종료 정리 뒤) 숫자 0 대신 빈 값을 보인다. 초기 직렬화 값(`_mockGold`)을 서버 값처럼 보이지 않는다. | EditMode 시험 |
| R3 | HUD는 실제로 구독한 미러에서 해제한다. 미러가 교체되거나 늦게 생겨도 현재 미러를 따르고, 옛 미러의 통지는 HUD를 바꾸지 않는다. disable·destroy 뒤 통지도 HUD를 바꾸지 않는다. | EditMode 시험 |
| R4 | 게임플레이 씬에서 「I」 키를 누르면 패널이 열리고, 다시 누르면 닫힌다. | EditMode 시험(입력 장치 대역) |
| R5 | 연결 뒤 첫 게임플레이 씬에서 패널은 닫혀 있다. 같은 연결의 맵 이동(씬 교체로 패널을 새로 만듦) 뒤에도 마지막 열림·닫힘을 따른다. 연결이 끝나고 새로 들어오면 다시 닫힘으로 시작한다. | EditMode 시험 |
| R6 | 닫힌 동안 패널은 보이지 않고 raycast를 받지 않는다. 그래서 클릭과 공격 입력을 막지 않는다. | EditMode 시험 |
| R7 | 일시정지 중이나 씬 전환 중에는 「I」 입력이 열림·닫힘을 바꾸지 않는다. 기존의 「일시정지·씬 전환 중 입력 차단」과 같다. | EditMode 시험 |
| R8 | 닫힌 동안에도 미러 구독과 재조회는 그대로다. 열면 그 순간의 최신 snapshot을 보인다. 열린 동안의 표시·사용·새로고침·재조회 동작은 바뀌지 않는다. | EditMode 시험과 기존 시험 회귀 |
| R9 | 실화면 세 크기에서 HUD와 패널의 겹침 0, HUD 골드와 패널 재화 일치, 「I」 키 열고 닫기, 패널이 닫힌 동안 퀘스트 알림과의 가림 0. | 실화면 실측 |
| R10 | 재화 0부터 서버 상한 1,000,000,000(`InventoryLimits.MaxCurrency`, 「Gold: 1000000000」)까지 HUD 골드가 한 줄이고, 그려진 글자가 골드 테두리(`Gold_bar`) 안에 있다. HUD와 패널의 숫자는 축약 없이 같다. 6자리 이하는 지금과 같은 글자 크기(18)로 보이고, 7자리 이상도 글자 크기 12 이상으로 읽힌다. 사용자 결정 37 A로 더했다. | PlayMode 시험(실제 UI 씬)과 실화면 재확인 |

R10의 알려진 함정(R9 판정 O-1): HUD 골드 폰트 `Pretendard SDF Proper`는 fontSize 18에서 줄 높이 34.35가 글자 상자 높이 14.92보다 크다. 그래서 TMP `isTextOverflowing`은 값과 상관없이 참이고, 자동 크기 조절은 높이 때문에 모든 값을 줄일 수 있다. 시험은 넘침 플래그 대신 줄 수와 글자 경계로 단정한다.

보존 동작: HUD HP·MP 표시와 세션 바인딩, 패널의 기존 표시·사용 버튼·재조회·버튼만 raycast 받는 정책, 메뉴·캐릭터 선택·Ending에는 패널이 없음, 기존 공격 입력과 포인터 raycast 차단 경로. 근거는 [클라이언트 영역 계약](../../../00_Document/domains/client.md#인벤토리-표시와-요청-수명)이다.

던전 패킷·창 PR과 클리어 PR의 설계는 각 PR 착수 때 이 절에 더한다.

### 던전 패킷·창 PR의 요구

클리어 결과를 알리는 패킷과 「확인」 요청 패킷, 보상 창, 던전 입구 표시를 만든다. 서버는 이 PR에서 새 패킷을 보내지도 처리하지도 않는다. 범위 초안은 리드 `msg_9747b2350231`, 메인 확인은 `msg_33d93ad1147f`다.

| 번호 | 요구 | 확인 방법 |
|---|---|---|
| P1 | PDL 맨 아래에 39 `S_DungeonClearResult`(서버 → 클라이언트)와 40 `C_DungeonClearConfirm`(클라이언트 → 서버)을 더한다. 기존 1~38의 이름·순서·필드는 그대로다. 생성 코드는 실제 생성기로 만든다. 프로토콜 버전은 18이고 버전 이력 주석에 한 줄을 더한다. | 서버 시험, 클라이언트 EditMode 시험, 생성 diff |
| P2 | `S_DungeonClearResult`의 필드는 이 순서다. 결과 byte, 받은 골드 int, 아이템 세 칸(칸마다 itemId int, count int, 빈 칸은 0/0). `C_DungeonClearConfirm`은 `reserved` byte 하나이고 0이어야 한다. | 서버 시험(독립 encoder/decoder로 바이트 대조) |
| P3 | 결과 값은 공유 enum `InventoryResult`의 `Success`·`CurrencyCap`·`InventoryFull` 셋만 쓴다. 새 enum을 만들지 않는다. | 서버·클라이언트 시험 |
| P4 | 서버는 버전 18만 받고 17 클라이언트는 기존처럼 거부한다. 서버는 40번 핸들러를 등록하지 않는다. 그래서 캐릭터 선택 뒤 세션이 보낸 40번은 상태 변화·응답·연결 종료 없이 버려진다. 서버는 39번을 만들지 않는다. | 서버 시험 |
| P5 | Shared GameData의 새 던전 폴더에 클리어 보상표를 둔다. 값은 골드 50, Material 1, CoinPouch 1이다(10-08 결정 2 A의 임시값, 지금 보스 처치자 보상과 같다). 모든 아이템은 카탈로그에 있고, 개수는 1~`InventoryLimits.MaxStack`, 골드는 0~`InventoryLimits.MaxCurrency`, 아이템 종류는 패킷 칸 수(3) 이하다. 이 PR에서 서버는 이 표를 읽지 않는다. | 서버 또는 클라이언트 시험 |
| P6 | 클라이언트 39번 핸들러는 정확한 길이·머리, 결과 세 값, 골드 범위, 칸마다 「카탈로그 id와 개수 1~`MaxStack`」 또는 「0/0」을 검증한다. 하나라도 틀리면 버리고 기록만 한다. 창은 열리지 않고 상태도 바뀌지 않는다. 맞으면 메인 스레드에서 창을 연다. | EditMode 시험 |
| P7 | 보상 창은 런타임으로 만든다. 성공이면 받은 골드와 아이템 줄(카탈로그 표시 이름과 개수)을 보인다. `CurrencyCap`·`InventoryFull`이면 받은 것이 없다는 것과 그 이유를 보인다. 「확인」은 연결이 handshake를 마친 때에만 40번(`reserved` 0)을 정확히 한 번 보내고 창을 닫는다. 두 번 눌러도 한 번만 보낸다. 창이 열린 동안 새 결과가 오면 새 결과로 바꾼다. 숨긴 창은 클릭과 공격 입력을 막지 않는다. 연결 종료 정리와 게임플레이 밖 씬에서는 닫혀 있다. | EditMode 시험 |
| P8 | 마을(Town) 게임플레이 씬에서 마을의 사냥터행 포탈 위에 「던전 입구」 글자를 런타임으로 붙인다. 사냥터·보스방에는 붙이지 않는다. 마을의 포탈은 하나라는 전제를 시험으로 고정해, 두 번째 포탈이 생기면 그 시험이 먼저 깨진다. 글자는 클릭과 공격 입력을 막지 않는다. 맵·포탈 표, 서버, scene·prefab 파일은 바꾸지 않는다. | EditMode 시험 |

**건드릴 곳 보충**(메인 조건 1): 입구 표시와 보상 창은 클라이언트 전투 씬 초기화 `03_Client/Assets/Scripts/Combat/CombatBootstrap.cs`의 installer 배열에 한 줄씩 더하고, 그 줄이 부르는 Build 메서드 둘만 더한다. 이 파일의 주석이 정한 「새 인프라 = Build 메서드 + 이 배열 1행」 방식이다. 기존 installer의 순서·내용은 바꾸지 않는다. 39번 수신은 `Network/UnityClientSession.cs`의 dispatch 표에 한 줄을 더한다(같은 파일 주석 「새 패킷 추가 = 핸들러 1개 신설 + 여기 1줄 등록만」).

**고친 기존 시험**: 패킷 수 38과 버전 17을 단정하는 기존 시험(서버 `Items/InventoryProtocolContractTests.cs`, `Maps/BossBehaviorTests.cs`의 버전 시험, 클라이언트 EditMode `InventoryProtocolCompatibilityTests.cs`)은 P1 때문에 40·18로 바뀐다. 바꾸는 쪽은 선행 시험 작성자이고, 분류표(옛 세부, 요구 출처 P1)와 같은 명령의 전후 원시를 남긴다(메인 결정 `msg_71e41e231d55`).

**보존 동작**: 기존 1~38 패킷의 직렬화, 미등록 요청을 버리는 서버 경로, 인벤토리·파티 팝업·스테이지 클리어 표시·포탈 입력, 기존 봇 시나리오, HUD와 인벤토리 패널.

**실행**: WSL `dotnet` 빌드·시험 전체, 실제 생성기 재생성 diff, Windows에서 Shared·ClientNet DLL 재빌드와 `03_Client/Assets/Plugins/` 사본 diff(메인 조건 2), 새 서버와 기존 봇 접속 회귀, Unity 컴파일·EditMode 전체, 독립 검증의 PlayMode 전체(서버 lane, 회귀). 실화면은 클리어 PR에서 한다. 7777과 Unity batch 같은 큰 실행은 Core SQL 컨테이너 재측정과 Management 운영툴 중간 점검(7777)에 미리 알려 겹치지 않게 하고, 메모리 4GB 기준을 원시로 남긴다(메인 조건 3).

### 파트 간 소유와 계약

| 대상 | 소유와 합의 |
|---|---|
| 인스턴스 맵(사냥터·보스방 복사본), 맵 종류 표, 맵 이동, 보스 처치 때 퀘스트 초기화 범위, 「서버가 시키는 이동」 진입점 | World(`[World 리드 Opus]`, 회신 주소 `run:run_c21dddd08312`). 10-08 Core 계획 v2를 World가 이어받았다(메인 `msg_6412194a47fa`). |
| 입구 | 기존 마을 → 사냥터 포탈이다. Content가 맵 종류 표·포탈 표에 더할 줄은 없다(World `msg_6e96d918962c` 1). |
| 클리어 대상 | 보스 사망 틱의 그 보스방 복사본 플레이어 목록. 파티원 재확인은 하지 않는다(같은 메시지 2). 열쇠는 `GameMap.InstanceKey`(`InstanceKey?`)다. 공용 맵은 null이고 복사본은 값이 있다. `InstanceKey`는 `Owner`(Party/Solo)와 `Id`를 가진 값 비교 record struct이며, 복사본에 들어갈 때 정해져 그 안에서 파티를 나가도 바뀌지 않는다(World `msg_14c7e14412f3` 1, World 선행 시험 commit `3511b66c`). |
| 마을 복귀 | Content의 「확인」 처리기가 플레이어 한 명씩 World 진입점을 부른다. 틱 스레드의 현재 맵 job 안에서 부른다. Content 요구: 도착 좌표 기본값은 World가 정함, 예외 대신 거부 결과, 이동 중 인벤토리·파티 보존(Content `msg_92f0e5340ca3`, World `msg_6e96d918962c` 3). World 답(`msg_25abb610ef01`): 도착은 도착 맵의 기본 입장 지점(`GameMap.PlayerSpawnPosition`, 최초 입장·재접속과 같은 곳), 인스턴스 밖·이동 중·세션 종료 중·같은 사람 두 번째 호출은 상태를 바꾸지 않는 거부, 성공하면 포탈 이동과 같은 맵 이동 패킷만 나간다. 진입점은 `MapMigration.MoveToPublicMap(GameSession session, int entityId, GameMap currentMap, MapId destination, Vector2? spawn = null)` → `MapMoveResult`다. 거부 검사 순서는 `SessionClosing` → `AlreadyMigrating` → `NotInThatMap` → `NotInInstanceMap` → `DestinationNotPublic`이고, 모두 통과하면 `Accepted`다. 공용 맵 도착은 다음 틱 이내다(World `msg_14c7e14412f3` 2). |
| PDL·생성 패킷·프로토콜 버전 | 던전 패킷·창 PR이 39·40번과 버전 17 → 18을 쓴다. World 인스턴스 맵 PR은 PDL을 바꾸지 않는다. World의 다중 계정 로그인과 같은 시기에 열리면 먼저 병합되는 쪽이 앞 번호를 갖고, 뒤 PR이 main을 받아 재생성해 번호를 다시 확인한다(같은 메시지 4). 기존 번호 재사용·재배열 금지. |
| `GameMap.cs` 보스 사망 분기, `GameWorld.cs` 처치 콜백, `Items/InventoryRegistry.cs` | World 인스턴스 맵 PR 병합 전에는 Content가 쓰지 않는다. World는 `InventoryRegistry.cs` 본문을 바꾸지 않을 계획이며, 바꿔야 하면 쓰기 전에 알린다(같은 메시지 5). |
| 포트 7777 | 서로 실행 전에 Orca 메시지로 알리고 DEVELOPMENT의 전역 lock·listener 검사를 따른다. |
| CURRENT | Content 줄만 바꾼다. 다른 파트 줄은 보존한다. |

## 검증 계획

- **검증 강도**: 세 PR 모두 강이다. 결함 PR은 클라이언트 화면 두 곳과 시험을 합쳐 제품+시험 50줄 이상이 예상되고, 애매하면 강이다. 던전 패킷·창 PR은 프로토콜·공유 DLL, 던전 클리어 PR은 서버 판정·경제·중복 방지다. 실제 파일·줄 수의 원시 근거는 각 계약에 적는다.
- **TDD**: 세 PR 모두 코드 변경이다. 신규 `claude-opus-5-5`가 요구 시험과 구현 전 실패 원시를 먼저 근거 폴더에 둔다. 쓰기 종료를 확인한 뒤 구현자가 같은 명령의 통과 원시를 둔다. 준비 단계의 실패는 제품 확정 실패에 넣지 않는다.
- **구현자**: 세 PR 모두 신규 `gpt-6-astra` xhigh다([배정](../../../.agents/skills/dawnholder-goal-loop/references/implementer-routing.md)). 결함 PR은 신호 4(배치 판단이 남은 다파일 화면 변경), 던전 패킷·창 PR은 신호 3(서버↔클라이언트 프로토콜)과 4(새 화면), 던전 클리어 PR은 신호 2(보스 사망 틱, 보상 창을 보는 동안의 퇴장·정리 수명)와 3이다.
- **검증자**: Astra 구현이라 세 PR 모두 신규 `claude-opus-5-5`다([검증자 시범](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증자-모델-시범2026-10-31까지)).
- **R-7**: 결함 PR과 던전 패킷·창 PR은 비해당이다. 네 범주(실패 수명·보호 집합·오류 분류·비용 상한)에 닿지 않는 표시·직렬화 작업이기 때문이다. 던전 클리어 PR도 비해당이다. 실패 수명과 오류 분류에 닿지만, 시범의 남은 두 자리를 World 인스턴스 맵과 Core DB 도구 컨테이너 전환 PR에 썼다(아이템·인벤토리 goal을 더해 시범 상한 세 건). 그래서 리드 설계 절과 선행 시험으로 간다(메인 결정 `msg_cfcaae303755`).
- **Unity**: 같은 checkout에서 Unity는 한 번에 하나만 실행한다. batch는 Unity 업그레이드 goal의 harness 사본을 쓴다. 실화면은 Unity MCP 시트를 메인에 요청한 뒤 한다.
- **실행과 미실행 구분**: 빌드, 서버 시험, 봇, Unity 컴파일·EditMode·PlayMode, 실화면, CI를 각각 따로 기록한다. DB는 이 goal에서 쓰지 않는다.

## 적용 중인 사용자 결정

정본에 반영하지 않은, 이 goal에만 적용하는 사용자 결정이다. 모두 메인이 전달했으며 리드 pane의 사용자 직접 입력으로 격상하지 않는다.

- **다음 goal과 우선순위**(메인 `msg_6d3f7b2831e6`, 2026-10-10 16:24 KST 메인 pane 제출). 원문 「7) 19 - Content 다음 goal → A 던전 + 결함 둘」, 「5) 17 - 마일스톤 우선순위(기능 동결 10월 28일까지) → A 인스턴스 던전 먼저」. 효과: 10-08 승인 던전 초안 v3을 goal로 옮기고, 인스턴스 맵이 들어올 때까지 기술 계약·클라이언트 쪽을 먼저 한다. 그동안 결함 둘을 같은 goal 첫 PR로 고친다.
- **범위 승인**(메인 `msg_9fe1d49e00bb`, 2026-10-10T07:39:37Z). 원문 「대시보드 결정 응답: 1) 계획 검토 - Content Content 던전 클리어·보상과 결함 둘 goal 범위(질문 셋, 추천 전부 A) → A 승인 (초안 msg_f1bba119c853)」. 효과: 범위 초안 v1 그대로이고 세 질문 모두 A다. 1 HUD 골드를 서버 재화에 연결한다. 2 「I」 키로 패널을 열고 닫는다. 3 보스 처치자 보상을 없애고 클리어 보상 하나로 합친다. PR 순서 결함 → 패킷·창 → 클리어도 승인 범위다.
- **10-08 던전 초안 v3 「전부 A」**(근거 `.backups/verification/2026-10-08-content-replan/dungeon-goal-draft-v3.md`, SHA256 `7a41a0d4…f5bd`). 1 보상은 그 순간 던전 안의 파티원 전원, 2 기존 적·보스 재사용(수치는 임시값), 3 복사본이 된 지금의 사냥터 → 보스방 길을 던전으로 쓴다.
- **메인 결정(사용자 결정 아님)**: R-7 시범은 이 goal에 쓰지 않는다(`msg_cfcaae303755`, 이전 보류 `msg_949e9610871f`를 대신함). 이 goal의 모든 Unity batch에 이전 결정 `msg_833c357e684e`와 같은 음소거 조건을 적용한다(`msg_498aa060f88d`): HKCU `Software\Unity Technologies\Unity Editor 5.x` / `AudioMasterMute_h3604209190` 하나만, 다른 Unity 0일 때만, 이미 1이면 쓰지 않음, finally 복원, 같은 폴더에 Editor가 열려 있으면 batch 금지, 실행마다 전후 원시를 남김.
- **메인 판단(사용자 결정 아님) — Unity가 다시 쓴 설정 파일**(`msg_c9fac8091065`, 2026-10-10T08:34:26Z). `ProjectSettings.asset`(사용자 미커밋)과 `TimeManager.asset`은 이 PR의 마지막 batch 뒤, 실화면 Editor를 열기 전, PR 생성 전 중 가장 이른 때에 리드가 한 번에 되돌린다. 그 전에는 두 파일을 stage·커밋하지 않고 계약마다 이 금지를 원문으로 넣는다. 되돌린 뒤 ProjectSettings는 SHA256 `4a8db0bd…`, TimeManager는 HEAD와 바이트가 같은지 원시로 남긴다.
- **메인 운영 조율(사용자 결정 아님) — 큰 실행 분리**(`msg_e30ad0e2a36c`, 2026-10-10T09:21:37Z). Core의 SQL 컨테이너와 Content의 Unity batch·PlayMode는 겹치지 않는다. 시작 직전 여유 물리·커밋 메모리(`Win32_OperatingSystem`의 FreePhysicalMemory·FreeVirtualMemory)를 원시로 남기고, 하나라도 4GB 아래면 기다린다. 상대가 큰 실행 중인지 모르면 우편함으로 알린다.
- **메인 결정(사용자 결정 아님) — 기존 시험 실패 분류**(`msg_71e41e231d55`, 이전 goal 근거 `2026-10-05-items-inventory-currency/main-test-classification-decision.json`). 독립 검증 계약에 원문을 붙였다. 실패를 (a) 옛 세부·(b) fixture·(c) 회귀·(d) 미확정으로 빠짐없이 나누고, 고친 시험은 표와 같은 명령의 전후 원시를 남긴다.
- **메인 판단(사용자 결정 아님) — R9 화면 크기**(`msg_03e0730a317d`, 2026-10-10T10:33:55Z). 새 Game view 항목을 더하지 않고, 이전 실화면이 남긴 「PR191 verify」 항목 값만 바꾼다. 끝나면 709×399로 되돌리고 그 원시를 남긴다.
- **R9 실화면 준비**(메인 `msg_67a2d57c5d69`, 2026-10-10T10:57:26Z). 원문 「대시보드 결정 응답: 1) 30 - PR221 실제 화면 확인 준비(Unity Editor·연결 승인) → A 지금 준비」. 이어서 사용자 지시(메인 `msg_2bf7955c0de9`) 「일단 유니티 에디터는 작업자가 직접 열고 MCP 연결만 승인요청 해달라고 해줘, 연결하고 싶은 세션이 미리 대기해달라고 전해줘」. 효과: Editor는 리드가 열고, 사용자는 MCP 연결 승인만 한다. 이 지시는 S-1 수정 뒤 실화면 재확인에도 그대로다(`msg_2f5bc1c2b8f2`).
- **R9 검증자의 Unity 코드 실행 허용**(결정 31·32). 메인 `msg_37e095c97cd2`가 전달한 원문 「A 맞아, 방금 Permission 줬어」, 메인 `msg_31520b4eb780`이 전달한 원문 「대시보드 결정 응답: 1) 32 - Content 실제 화면 검증 계속할지(Editor 닫을지) → A 지금 계속」, 메인 `msg_90e7310b4b68`이 전달한 원문 「Content 탭에 규칙 다시 저장했어」. 효과: 사용자가 content-active `.claude/settings.local.json`(Git 무시)에 `mcp__unity-mcp__Unity_RunCommand` 허용 한 줄을 저장했고, 검증자의 세 번째 확인 호출이 통과했다.
- **허용 규칙 지움과 S-1을 이 PR에서 고침**(메인 `msg_2f5bc1c2b8f2`, 2026-10-10T13:09:45Z). 원문 「대시보드 결정 응답: 1) 36 - Content 작업 공간의 Unity 코드 실행 허용 규칙 지우기 → A 지움 · 2) 37 - HUD 골드 7자리 이상 넘침(S-1)을 이번 PR에서 고칠지 → A 이번 PR에서 고침」. 효과: 메인이 허용 한 줄만 지웠다. 재확인 때 다시 필요하면 메인에 결정 요청으로 올리고 에이전트가 직접 허용하지 않는다. S-1을 R10으로 PR221 범위에 넣고, 고친 뒤 실화면으로 다시 확인한다. `UI.unity`의 `.meta`·GUID·의도 밖 직렬화 값은 보존한다. 수정 계획은 리드 `msg_ef372eeeb40b`, 메인 확인은 `msg_257941a5063f`다.
- **재확인 동안만 Unity 코드 실행 허용**(메인 `msg_63aa0980d58a`, 2026-10-10T14:12:50Z). 원문 「대시보드 결정 응답: 1) 38 - 골드 수정 실제 화면 재확인 동안 Unity 코드 실행 허용 → A 재확인 동안만 허용」. 효과: 메인이 content-active `.claude/settings.local.json`에 허용 한 줄을 넣었고, 재확인 검증자는 그 뒤에 새로 기동했다. MCP 사용이 끝난 뒤 리드 요청(`msg_cd7e37a05073`)으로 메인이 그 한 줄을 지웠다(`msg_aa4aae54b7d7`, 15:12:15Z). 남은 내용은 `skillOverrides`뿐이다.
- **메인 확인(사용자 결정 아님) — 던전 패킷·창 PR 범위**(`msg_33d93ad1147f`, 2026-10-10T16:04:16Z). 리드 초안 `msg_9747b2350231`의 1~6과 달라진 점 넷을 승인 범위 안의 구현 방식 선택으로 확인했다. 조건 셋은 「던전 패킷·창 PR의 요구」에 반영했다. 전투 씬 초기화 파일을 「건드릴 곳」 보충으로 적는다. 39·40을 맨 아래에 더하고 서버·Unity 양쪽 검증과 Plugins 사본 diff를 원시로 남긴다. 큰 실행은 Core 재측정·Management 중간 점검과 겹치지 않게 미리 알린다.

## 재개 지점

다음 리드는 이 블록부터 읽는다.

| 항목 | 값 |
|---|---|
| 작업 공간 / branch | `content-active` / `feat/dungeon-clear-packets-20261011`(base `e775bc775e6f93bab95a2412512c11332017abca` = PR221 병합) |
| PR | 던전 패킷·창 PR은 아직 없다. 결함 PR은 [PR221 - HUD 골드와 인벤토리 패널 「I」 키](https://github.com/bass131/dawnholder-server/pull/221)로 병합됐다(head `f135eaf1`, merge commit `e775bc77`) |
| 리드 세션 / Run | `term_df056d96-0389-436c-afcf-3d189f8dea53` / `run_b680cd89da9a`(회신 주소 `run:run_b680cd89da9a`) |
| 로컬에만 둔 변경 | 사용자 미커밋 `03_Client/Assets/Resources/MinimapRT.renderTexture`, `03_Client/ProjectSettings/ProjectSettings.asset`. 커밋·되돌리기·stash 금지 |
| 작업자·실행 자원 | 작업자 0(R10 실화면 재확인 검증자까지 정산·종료), Unity.exe 0, 7777 0(World·Core·Management에 해제 통보) |
| 로컬 부수 변경 | batch가 다시 쓴 `ProjectSettings.asset`·`TimeManager.asset`은 두 번 되돌렸다. 독립 검증의 마지막 batch 뒤 09:51:43Z(`settings-restore/post-state.txt`), R10 독립 검증의 마지막 batch 뒤 14:29:38Z(`settings-restore-2/post-state.txt`)다. 지금 ProjectSettings `4a8db0bd…`, TimeManager blob = HEAD다. R9와 R10 재확인의 Editor는 두 파일을 바꾸지 않았다. Unity가 Git 무시 대상 layout 파일만 저장했다 |
| 남은 결정·준비 | 범위는 메인이 확인했다(`msg_33d93ad1147f`). 요구는 「던전 패킷·창 PR의 요구」 P1~P8이다. 다음은 신규 Opus 선행 시험 위임이다. 큰 실행 전에 Core·Management·World에 알린다 |

**남은 순서**: 던전 패킷·창 PR(선행 시험 → 구현 → 독립 검증 → PR·CI → 사용자 병합 승인) → 던전 클리어 PR → 결과 기록·Gardener → 종료 기록 PR → 종료 점검 → R-8.

## 진척 단계

- [x] 범위 승인
- [x] 결함 선행 시험
- [x] 결함 구현·검증
- [x] R9 실화면
- [x] R10 골드 넘침 수정·재확인
- [x] 결함 PR 병합
- [>] 패킷·창 구현·검증
- [ ] 패킷 PR 병합
- [ ] 클리어 설계 확정
- [ ] 클리어 서버 구현
- [ ] 검증·실제 플레이
- [ ] 클리어 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 병합

## 실제 결과와 미실행

- 범위 초안·사용자 승인·World 계약 확인까지 했다. 원문은 근거 폴더의 `scope-draft-v1.md`와 `inbox/`에 있다.
- 결함 PR은 선행 시험 → 구현 → 수정 1 → 독립 검증 → PR 생성(CI) → R9 실화면 → R10 수정 루프와 실화면 재확인 → 사용자 병합 승인까지 끝났다(아래 다섯 절). 실행한 것: Unity batch EditMode·PlayMode, WSL GameServer 서버 lane(DB 없음), CI, Unity Editor 실화면(MCP). 실행하지 않은 것: 봇, 빌드 단독 실행, 사람 손 입력.

### 선행 시험 — RED 준비 성공, 2026-10-10

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_f4fb52449c6c` / `ctx_b8327985145f`, `[Content 검증자]` |
| 모델 | 지정 `claude-opus-5-5`. 최초 실행 명령 `$env:TEMP=…\.backups\tmp\pr1-tdd; claude --model claude-opus-5-5`, 화면 표시 「Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `opus-pr1-tdd-contract.md` SHA256 `f0dae46d44f313384608b994cd54d742ab3583dd6f975d3d502aa392e1e40ee1`, 요약 spec `opus-pr1-tdd-spec.md`. 보충 v1.1(`msg_df9f59cf608b`): harness를 pwsh 7.6.6으로 돌리고 EditMode만 실행 |
| 보고 | `opus-pr1-tdd/report.md`, worker_done `msg_9a89f1c0dcb3`(08:31:07Z, succeeded) |

- **결과**: 새 EditMode 시험 13개(`HudGoldMirrorTests.cs` R1~R3 6개, `InventoryPanelToggleTests.cs` R4~R8 7개, 공용 대역 `GameplayViewTestSupport.cs`)가 모두 미구현으로 실패한다. 마지막 실행 `editmode-red-3`은 417개 중 통과 404, 실패 13, 건너뜀 0, exit 2다. fixture 원인 실패와 기존 시험 실패는 0이다. 컴파일 오류 0. 음소거는 0 → 1 → 0으로 복원됐다.
- **리드 R-2**: `runs/editmode-red-3/results.xml`(SHA256 `db7c6dcb…`)을 node로 읽어 test-run의 417/404/13/0과 실패 13개의 이름이 모두 새 두 시험 파일임을 확인했다. editor.log의 `error CS` 0건, 「Exiting with code 2」, mute-events 세 줄을 직접 읽었다. R1·R2 시험 본문을 읽어 기대값이 서버 리터럴과 화면 글자이고 제품 계산을 복제하지 않음을 확인했다.
- **리드 채택 결정**: (1) 열림 관찰면은 패널 root의 CanvasGroup·Canvas 활성·본문 활성이다. (2) 「I」 키 대역은 Input System의 `runPlayerUpdatesInEditMode`를 켜고 이전 값으로 되돌린다. 계약의 설정 사본 방식은 이 플래그가 직렬화되지 않아 복원하지 못하기 때문이다. 패키지 내부 필드에 기대므로 패키지 업데이트 때 fixture Assert가 먼저 깨진다. (3) 「I」 처리는 `InventoryPanel.BuildRuntime`이 만든 객체 안에 둔다. 시험이 그 객체의 Update만 부르기 때문이다. (4) 일시정지는 `Time.timeScale = 0`, 씬 전환은 SceneTransition의 끝나지 않은 요청과 맵 입장 진행이다. (5) HUD 빈 값은 숫자가 없는 글자 또는 꺼진 글자다.
- **알아 둘 것**: R7 두 시험의 본 단정(정지·전환 중 불변)은 지금 「I」 처리 자체가 없어서 통과하고, RED는 대조 단정이 만든다. 검증자는 둘이 함께 통과하는지 본다. 기존 PlayMode `InventorySceneLifecycleTests.cs` 189·398·399행은 입장 직후 패널이 보인다고 단정해 구현 뒤 실패할 것으로 예상된다(미실행 예측, 분류 (a) 후보).
- **부수 변경**: 첫 실행 때 Unity가 `ProjectSettings.asset`(사용자 미커밋, SENTIS define 한 줄 삭제, 알려진 `unity-sentis-define-drift`)과 `TimeManager.asset`(6.6 형식으로 다시 씀, 값 같음)을 저장했다. 작업자는 되돌리지 않았고 사본·diff를 `opus-pr1-tdd/side-effects/`에 두었다.
- **정산**: worker-release(retained/external_terminal) → 빈 prompt와 「done」 확인 → close(ptyKilled true). 화면 표시는 12분 48초 작업, 18.37$다.
- **리드 기동 실수(첫 발생)**: 전체 계약을 `--spec`으로 넣다가 Git Bash 인자 길이 한도(Argument list too long, exit 126)로 orca가 실행되지 않았다. worker-list 0건을 확인하고 계약 경로·hash를 가리키는 요약 spec으로 다시 붙였다. 첫 발생이라 goal에만 기록한다.
- **리드 도구 사고(첫 발생)**: 리드가 큰 우편함 JSON에 `grep -o` 범위 정규식과 `| head`를 써서 grep이 부모 없이 남아 메모리 16GB를 잡았다(메인 `msg_64f64f30c24f`). 다른 리드의 우편함 대기가 메모리 회수로 꺼졌다. 사용자가 그 프로세스를 처리했다. 이후 JSON은 node로 읽는다.

### 결함 구현과 수정 1, 2026-10-10

| 작업 | Task / 모델 | 결과 | 원시 |
|---|---|---|---|
| 1차 구현 | `task_c3ed610e39b9`, 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown) | 제품 3파일 +69/−11, 문서 2파일 +6/−2. EditMode 417/417/0/0. 커밋 `79d7fc55` | `astra-pr1-impl/report.md`, `runs/editmode-green-1/results.xml`(SHA256 `d90e188a…`) |
| 수정 1 | `task_1d9237212f9e`, 신규 `gpt-6-astra` xhigh | HUD 빈 값 `Gold: —` → `Gold: -`. EditMode 417/417/0/0. 커밋 `2ebcbcf6` | `astra-pr1-fix1/report.md`, `runs/editmode-fix-1/results.xml`(SHA256 `660075f5…`), 문자표 `glyphs-after.json` |

- **설계**: 열림 상태는 연결 객체 `UnityClientSession.IsInventoryPanelOpen` 하나가 갖는다. 새 연결은 닫힘으로 시작하고, 맵 이동은 같은 세션이라 유지되며, 연결 정리 뒤 새 세션은 다시 닫힘이다. 「I」는 `InventoryPanel.Update`가 읽고, 일시정지·씬 전환·게임플레이 Ready 전에는 무시한다. 닫힘은 CanvasGroup만 끄므로 구독과 재조회가 유지된다. HUD 골드는 `InventoryState`의 `HasSnapshot`·`Currency`와 변경 통지로 그리고, `_mockGold` 필드는 지웠다.
- **리드 발견 L-1(수정 1의 원인)**: 1차의 빈 값 `—`(U+2014)가 HUD 골드 글자의 폰트 `Pretendard SDF Proper`(정적 atlas, 자체 fallback 없음)와 TMP 전역 fallback(같은 폰트)에 없었다. EditMode 시험은 숫자만 읽어 이를 보지 못했다. 같은 산출물 수정 1회째이며 확정 실패가 아니다(`lead-r2-impl-1.md`).
- **리드 R-2**: 두 작업 모두 results.xml을 node로 다시 읽어 수치와 새 시험 13개 통과, `error CS` 0, 음소거 복원을 확인했다. 수정 1 뒤 리드가 폰트의 `m_Unicode` 11267개를 직접 읽어 HUD 골드 두 문자열의 누락 글자 0을 확인했다.

### 결함 PR 독립 검증 — 통과, 2026-10-10

| 항목 | 값 |
|---|---|
| Task / 모델 | `task_299bc7fdf7c8`, 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown) |
| 계약 | `opus-pr1-verify-contract.md` SHA256 `35cba0dc…fb54`. 보충 v1.1 메모리 확인, v1.2 Core 컨테이너 동안 대기, v1.3 재개 |
| 판정 | `opus-pr1-verify/verdict.md`, worker_done `msg_74d5dca1c629`(09:50:00Z). 통과, 제품 결함 0 |
| 수치 | EditMode 417/417 → 417/417. PlayMode(이 checkout의 WSL GameServer 7777 위) 23/16/7/0 → 26/26/0/0 |
| 커밋 | 검증자 시험 4파일 `efd0442b` |

- **기존 시험 7개 (a)**: PlayMode `InventorySceneLifecycleTests` 6개와 `InventoryServerIntegrationTests` 1개가 「입장 직후 패널 보임」과 「미러 구독자 1」을 단정해 실패했다. 메인 결정 `msg_71e41e231d55`에 따라 I로 연 뒤 원래 단정을 그대로 두게 고쳤다. 구독자 단정은 「패널 1·HUD 1·그 밖 0」으로 대상을 구분해 더 엄격해졌다.
- **새 PlayMode 시험 3개**: 실제 UI 씬 HUD 골드(빈 값·패널과 같은 재화·폰트 누락 0), 닫힌 패널 자리 클릭이 공격으로 통과하고 연 뒤에는 사용이 됨, 실제 일시정지 메뉴 동안 I 무효.
- **실제 진입**: 서버 lane 위 PlayMode에서 실제 서버 Town → 키보드 I로 열기 → 포털 이동 뒤 열림 유지 → 키보드 처치 보상 → 포인터 사용 확정까지 통과했다.
- **리드 R-2**: 일치(`lead-r2-verify.md`). XML 네 개의 hash·수치, 실패 7개 이름, 고친 시험 diff를 직접 대조했다.
- **설정 파일 복원**: 이 검증이 이 PR의 마지막 batch다. 그 뒤 메인 판단 `msg_c9fac8091065`대로 두 설정 파일을 되돌렸다(재개 지점).

### PR 생성과 R9 실화면 — 통과, 2026-10-10

| 항목 | 값 |
|---|---|
| PR | PR221. 생성 때 head `318717df`, CI 4/4 성공(`pr221-checks-1.txt`). 그 사이 origin/main `bd4dbb5f`(CURRENT)와 `6d09ac1f`(PR223 인스턴스 맵·PR224 세션 가드, FEATURE_MAP·CURRENT)를 합쳤다. 두 번 모두 main 판을 그대로 두고 이 PR 줄만 다시 넣었다 |
| Task / 모델 | `task_d989fb72484d`, 신규 `claude-opus-5-5`(최초 실행 `claude --model claude-opus-5-5 --mcp-config …`, 화면 「Opus 5.5 with xhigh effort」, backend unknown) |
| 계약 | `r9-screen-contract.md` v1 SHA256 `5db1e9f5…`. Editor는 리드가 열었다(PID 20212, 6000.6.4f1) |
| 판정 | `r9-screen/report.md` SHA256 `2fc721fe…`, worker_done `msg_d0fee2244122`(13:04:15Z). 통과, 결함 S-1 하나 |
| 수치 | 세 크기 709×399·802×451·1920×1080 실제 Play. 겹침 0, HUD 골드 = 패널 재화(검사 18곳, 프레임 감시 불일치 0), 서버 `C_Attack` 6·9·8 = Enter + UI 밖 클릭, `C_ItemUse` 1씩. MCP `Unity_RunCommand` 17회(거부 2, 컴파일 실패 1, 성공 13, 종료 응답 실패 1) |

- **S-1**: 재화가 7자리(1,000,000)부터 HUD 골드 글자가 두 줄로 꺾여 골드 테두리 밖으로 나간다. 원인은 이 PR이 바꾸지 않은 `UI.unity` GoldText의 상자 폭(한 줄 117.6, 7자리 필요 폭 119.3)과 줄바꿈 설정이다. 사용자 결정 37 A로 R10이 되어 이 PR에서 고친다.
- **기계 점검 18건 FAIL의 뜻**: 실패 조건은 모두 HUD 골드의 TMP 넘침 플래그 하나다. 이 폰트에서는 값과 상관없이 참이다(R10 함정). 이전 PR191 실화면 driver는 HUD 골드를 검사하지 않아 18건이 통과였다. 회귀가 아니다.
- **권한 경위**: 첫 확인 호출이 Claude Code 자동 모드 분류기에 「Sensitive Remote Exec」로, 두 번째가 「Auto-Mode Bypass」로 막혔다. 둘 다 Unity에 닿지 않았다. 사용자가 허용 규칙을 파일에 저장한 뒤 세 번째가 통과했다(적용 중인 결정 31·32). 이전 PR191 실화면에서는 같은 도구가 허용 규칙 없이 통과했다.
- **리드 R-2**: 표본 8곳이 원천과 일치했다(`r9-prep/r2-lead-sample-check.md`). 실패 조건 분해, driver 차이, scene 값, 확대 캡처 세 장, 서버 로그 패킷 수, MCP 기록, 마감 상태 hash를 직접 대조했다.
- **마감·정산**: mute와 「PR191 verify」 709×399를 되돌렸고 GameViewSizes hash는 실행 전과 같다. 서버 Stop 12:25:19Z 뒤 7777 해제를 World·Core에 알렸다. Editor는 12:26:04Z에 정상 종료했다. 검증자 pane은 release(retained) 뒤 close했고 relay도 사라졌다.
- **한계**: R9는 `6d09ac1f`를 합치기 전 서버로 실행했다. 이 PR의 diff는 클라이언트만이다. 합친 head의 실화면은 R10 재확인 때 본다(아래 절에서 수행).

### R10 골드 한 줄 맞춤 — 통과, 2026-10-10

| 단계 | Task / 모델 | 결과 | 원시 |
|---|---|---|---|
| 선행 시험(RED) | `task_31bcf8848ee0`, 신규 `claude-opus-5-5` | PlayMode 2개 중 7~10자리 시험 실패(두 줄·테두리 밖), 6자리 이하 시험 통과. 커밋 `a9e74a1f` | `r10-tdd/report.md`, `runs/playmode-red-1/results.xml`(SHA256 `d56ea8a9…`) |
| 구현 | `task_d0826fbf1ec3`, 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown) | `UI.unity` GoldText TMP 다섯 필드(자동 크기 12~18, 줄바꿈 끔, 상하 margin −10)와 client.md 한 문장. PlayMode 네 클래스 26/26, EditMode 417/417. 커밋 `74fce329` | `r10-impl/report.md`, `runs/playmode-green-2/`(`ec3d184c…`), `runs/editmode-regression-2/`(`2928993b…`) |
| 독립 검증 | `task_3027b332eed1`, 신규 `claude-opus-5-5` | 통과, 제품 결함 0. 서버 lane 위 PlayMode 전체 28/28 → 보완 뒤 30/30, EditMode 417/417 두 번. 보완 시험 커밋 `a1a392d0` | `r10-verify/verdict.md`(`e3ea6ec1…`) |
| 실화면 재확인 | `task_1ef08b9e0e20`, 신규 `claude-opus-5-5`(`--mcp-config`) | 통과, 결함 0. 세 크기 캡처 60프레임 모두 한 줄·테두리 안. MCP `Unity_RunCommand` 14회(거부 0) | `r10-screen/report.md`(`722af848…`) |

- **방법**: 이 폰트는 크기 18의 줄 높이(34.35)가 글자 상자 높이(14.92)보다 크다. 그래서 여백 없이 자동 크기를 켜면 모든 값이 줄어든다. 상하 margin −10으로 배치 높이만 넓혔다. 처음 방식인 상자 높이 확장은 버렸다. 이 상자는 raycast 대상이라 포인터를 막는 영역도 커지기 때문이다(`msg_cdae955c1614`).
- **관측값**: 6자리 이하는 세 크기 모두 18.00이다. 가장 넓은 6자리 444,444도 18.00이다. 7자리 이상은 17.80(1,000,000)·16.85(4,444,444)·14.30(444,444,444)·14.00(상한)이다. 상한 뒤 작은 값은 18로 돌아온다.
- **테두리 선**: 배율 1 캡처 픽셀로 쟀다. 7자리 이상의 오른쪽 끝에서 어두운 칸 선까지 709·802·1920에서 2·3·6 px이 비어 있다. 위·아래도 선에 닿지 않았다.
- **보존**: scene에서 바뀐 줄은 GoldText TMP 객체의 다섯 필드뿐이다. RectTransform·`Gold_bar`·`.meta`·GUID는 그대로다. 포인터 세 점(상자 중심 적중, 위·아래 5 단위 밖 비적중)이 세 크기에서 기대대로였다.
- **합친 head의 R9 흐름**: 재확인이 `6d09ac1f`를 합친 head(`0135d5e2`)의 서버로 세 크기 흐름을 끝까지 지났다. 판정 줄 47개가 PASS이고, 서버 `C_Attack`은 연결마다 6(= Enter 3 + UI 밖 클릭 3)이다. 닫힌 패널 동안 퀘스트 배너·팝업도 관측했고 가림·가로채기는 0이다.
- **리드 결정**: 독립 검증의 보완 시험을 커밋했다. 판정의 가독성 지적대로 client.md 26행을 나누고 이유를 보탰다(`0135d5e2`). 이 문장은 재확인 검증자가 직렬화 값과 대조하고 30초 가독성 확인을 했다. 두 시험 클래스의 측정 코드 사본은 두 곳뿐이라 그대로 둔다. CODE_CONVENTION은 같은 로직이 세 곳 이상일 때 추출한다.
- **리드 R-2**: 단계마다 원천을 직접 대조했다. 대상은 XML 루트 수치, 관측 로그 줄, scene 감사 결과, 새 시험 본문, 관측 표 60행, 테두리 픽셀 결과, 서버 로그 `C_Attack` 수, 10자리 확대 캡처 두 장이다. 모두 판정과 일치했다(독립 검증은 `lead-inbox-notes.md` 14:28Z 항목, 재확인은 `r10-screen-prep/r2-lead-sample-check.md`).
- **자원**: 7777은 독립 검증에서 두 번(14:14:58Z·14:20:44Z 해제), 재확인에서 한 번(15:06:55Z 해제) 썼다. World·Core·Management에 사전·해제를 알렸다. 재확인의 802×451 Play 직전 커밋 여유가 3.35GB로 4GB 기준에 걸렸다. 재측정에서 열린 뒤 시작했다.
- **origin/main 합치기**: 재확인 뒤 `b88a1b4b`(PR225 World 마감 문서, PR226 SQL Server 컨테이너 도구)를 합쳤다. 클라이언트·서버·공유 코드 변경은 0이다. CURRENT는 Content 줄 옆에서 충돌했다. main 판을 유지하고 Content 줄과 Content 안내만 다시 넣었다.
- **병합**: CI 4/4 통과·CLEAN인 head `f135eaf1`로 메인에 승인 묶음을 보냈다(`msg_016b427a8aef`). 그 사이 PR227(Management)이 먼저 병합됐다. 겹치는 파일은 CURRENT 하나였고 `git merge-tree` 충돌이 0이라 다시 합치지 않았다. 사용자가 메인 pane에 승인 줄을 제출했고, 메인이 head·CI·CLEAN을 다시 확인해 병합했다(2026-10-10T15:54:25Z, merge commit `e775bc775e6f93bab95a2412512c11332017abca`, 메인 `msg_c91503d507b1`). 메인 R-2는 제품 파일이 클라이언트 4개이고 서버·공유 변경이 0임, 두 판정의 통과 줄, client.md 26행, merge-tree 결과를 대조했다.

### 운영 기록(첫 발생)

- Orca CLI 1.4.224의 `terminal split`에는 `--title`이 없어 첫 구현자 기동이 실패했다. 플래그 없이 다시 열었다(`astra-pr1-impl-launch-fail-title.json`).
- 수정 1 작업자의 heartbeat 하나가 subject에 태그를 붙이고 body를 비워 helper가 body-tag 위반으로 판정했다(`msg_2c48778c8c93`). identity는 맞았고 내용이 없어 처리하지 않았다. 다음 계약부터 빈 heartbeat subject를 정확히 `alive`로 쓰라고 넣었다.
- 리드가 수정 1 진행 중에 고정 입력 파일(`lead-r2-impl-1.md`)에 관찰을 덧붙여 hash를 바꿨다가 바로 떼어 내 원래 hash로 되돌렸다. 관찰은 `lead-inbox-notes.md`로 옮겼다.
- 메모리 몰림(메인 `msg_e30ad0e2a36c`) 뒤 Core SQL 컨테이너가 먼저 돌도록 Content가 15분 기다렸다(Core `msg_fced0edb6f02` → `msg_ab97dbb84891`).
- 리드가 터미널 안내를 `--enter`로 보낸 순간 메인 입력창에 대시보드 결정 문장이 미제출로 있어, 두 문장이 한 줄로 합쳐져 제출됐다. 메인은 그 줄을 승인 기록으로 세지 않았다(`msg_a19641870a3a`). 이후 리드는 같은 명령 안에서 대기 확인·화면 읽기(draft 없음)를 한 직후에만 안내를 보낸다.
- 다른 리드의 `run:` 주소로 보낼 때 리드 자신의 `--run`을 붙이면 「Run … was not found」로 실패했다. `--run` 없이 보내 성공했다(`outbox/37-*-receipt*.json`).
- 이 checkout이 PR224를 받은 직후 첫 Edit가 세션 가드 `memo-first`로 막혔다. 시계 출력을 넣은 맥락 메모를 Write로 다시 쓴 뒤 진행했다(메인 예고 `msg_9afc844173ae`대로).
- **우편함 대기를 `&`로 띄운 실수 두 번째 발생**(11:07Z, 즉시 정리, 손실 0. 첫 발생은 이전 goal PR191 실화면). 메인 판단 `msg_257941a5063f`: 더 높은 층인 PR224 가드가 orca orchestration 명령 끝의 `&`를 막는다(`99_Tools/SessionGuard/session-policy.mjs:51`). 11:07Z는 가드가 이 checkout에 들어오기 전이라 새 반복 규칙은 만들지 않는다. BACKLOG `mailbox-output-loss-hook`에 근거로 더했다. 가드가 들어온 뒤 다시 나면 가드 누락으로 Rules에 원문을 보낸다.
- 리드가 결정 요청을 `--type decision_gate`로 보내 `sender_not_assignee` 오류 receipt를 받았다. 그런데 본문은 메인에 도착해 있었다(`msg_b4661ed3ee6e`). 리드가 미도착으로 보고 status로 다시 보내 중복이 생겼다(`msg_3b796a014aa6`, 메인에 중복 알림 `msg_95f83ba75699`). 10-07 Management 관측(미도착)과 결과가 달라, 메모리 기록을 「처음부터 status로 보내고, 재전송 전에 메인 회신을 확인한다」로 고쳤다.
- 검증자가 큰 명령 출력이나 MCP 응답을 화면으로 받으면, Claude Code가 홈 `~/.claude/projects/…/tool-results/`에 사본을 자동 저장했다. R10 독립 검증에서 1건, 재확인에서 3건이다. 두 검증자 모두 허용 밖 쓰기로 스스로 보고했다. 재확인 계약에는 「큰 출력은 파일로 받는다」를 넣었다. 그래도 MCP driver 응답은 파일로 돌릴 방법이 없어 3건이 남았다.
- R10 재확인 검증자가 orca가 아닌 명령에서 `2>/dev/null`을 두 번 썼다(계약은 리다이렉트를 checkout·`.backups` 안으로만 허용). 판정에 스스로 적었고 가드는 막지 않았다. 리드도 패킷 PR 사전 조사의 원격 branch 검사에서 한 번 썼다(16:02Z). 정본은 orca 출력만 버리지 못하게 한다(CLAUDE 우편함 대기, 세션 가드 `mailboxLoss`). 계약의 「리다이렉트는 checkout·`.backups` 안으로만」이 정본보다 넓었다. 그래서 다음 계약부터 「판정 근거가 되는 명령 출력은 버리지 않는다」로 좁혀 쓴다.
- 리드 커밋 첫 시도가 `index.lock` File exists로 실패했다. 다시 확인했을 때 잠금은 없었고 git 프로세스도 0이었다. 같은 명령을 다시 내 성공했다.

## 다음 계획 후보

이 goal에서 발견한 범위 밖 후보는 출처와 함께 한 줄로 적고 [BACKLOG](../../../00_Document/operations/BACKLOG.md)의 ID를 붙인다. 등록은 착수 권한이 아니다. 아래는 결함 PR 독립 검증의 비차단 관찰이며, BACKLOG 등록 여부는 종료 기록 때 정한다.

- `HudController` 클래스 요약에 골드의 출처(InventoryState 미러)가 없다. 지금은 FEATURE_MAP·client.md에서 찾는다(판정 「사람 가독성 지적」).
- `UnityClientSession`이 UI 표시 bool을 가진다. 표시 상태가 늘면 연결이 소유하는 작은 표시 상태 객체로 옮긴다(O-1).
- `HudController`의 `OnEnable`·`Start`가 `BindInventory` 뒤 `RenderGold`를 한 번 더 부른다. 동작 영향은 없다(O-2).
- `UI.unity`에 지운 필드 `_mockGold: 0`의 직렬화 잔여가 있다. 다음 scene 저장 때 Unity가 지운다(O-3).
- EditMode 씬 전환 시험은 맵 입장과 SceneTransition이 함께 걸린 경우만 본다. 지금 제품 경로에서는 둘이 늘 겹친다(O-4).
- EditMode 가상 키보드가 Input System 내부 `runPlayerUpdatesInEditMode`에 기댄다. 패키지 업데이트 때 fixture가 먼저 깨진다(O-5).
- 실화면 driver의 HUD 골드 넘침 검사는 이 폰트에서 쓸 수 없었다(R9 판정 O-1). R10 재확인 driver는 줄 수와 글리프 quad로 바꿨다(`r10-screen/driver/`). 폰트 asset의 줄 metrics가 비정상인지는 남은 확인 후보다.
- 작은 렌더 해상도에서 HUD 골드 판독성: 709×399·802×451에서는 크기 18이든 14든 한 글자가 5~7 px이라 숫자가 뭉개져 보인다. R10의 수치 기준(크기 12 이상)은 충족한다. 작은 창의 판독성 목표가 필요한지는 사용자·메인이 정한다(R10 재확인 판정 O-1).
- HUD 골드의 높이 여유가 얇다. 배치 높이 34.92와 줄 높이 34.35의 차이가 0.57이다. 폰트·줄 간격·상자 높이가 조금만 바뀌어도 모든 값이 줄어든다. 지금은 6자리 크기 18 단정 시험이 막는다(R10 독립 판정 O-1).
- R10 두 PlayMode 시험 클래스가 측정 코드(`Measure`·`GoldLayout`)를 사본으로 가진다. 세 번째 사용처가 생기면 공용 helper로 뽑는다(R10 독립 판정 O-3).
- **게임 UI·아트 스타일 기준 문서화**: 기준 게임은 Moonlighter다. 차이는 둘이다. 시점은 Moonlighter가 Top-View, 우리는 사이드 스크롤이다. 플레이 형태는 Moonlighter가 솔로, 우리는 온라인이다. 출처는 메인 `msg_4dffdfb3323b` 원문 「우리가 원하는 게임의 스타일에 제일 가까운게 Moonlighter거든? 그래서 게임 UI나 디자인 셋업할때 참고하면 좋을거 같아, 근데 그 게임같은 경우에는 Top-View 게임인데, 우리 게임은 Side Scroll인게 차이점이야」와 `msg_4df6930b3cf0` 원문 「거기에 그 게임은 솔로게임인데 우리는 온라인 게임인거고」다. 문서 위치·형식은 goal로 올릴 때 정한다. 종료 기록 때 BACKLOG에 등록한다.
- **직업 추가 — 사제·궁수**(10-13 발표에서 추가 예정으로 표기). 출처는 메인 `msg_9f231a21d4cc`가 전달한 원문 「음 첫 대문 배경에 직업이 색깔만 바뀌고 겹치니까 좀 어색한데, 이참에 다른 직업군도 넣어볼까? 사제, 궁수도?」와 「대시보드 결정 응답: 1) 33 - 발표 표지에 사제·궁수를 넣을지 → A 추가 예정 직업으로 넣음」이다. 지금 게임 직업은 전사·마법사 둘이고 저장소에 두 직업 계획은 없다. 착수와 기획 내용은 goal로 올릴 때 사용자와 정한다. 종료 기록 때 BACKLOG에 등록한다.
