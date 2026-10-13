# 던전 클리어·보상과 눈에 띄는 결함 둘

상태: **범위 승인(2026-10-10). 결함 PR(PR221)은 사용자 승인으로 병합됐다(2026-10-10T15:54:25Z). 실화면이 찾은 HUD 골드 7자리 넘침(S-1)도 그 PR에서 고쳤다(R10). 던전 패킷·창 PR은 [PR230](https://github.com/bass131/dawnholder-server/pull/230)으로 사용자 승인을 받아 병합됐다(2026-10-11T10:57:10Z). 던전 클리어 PR은 메인이 범위를 확인했고, 선행 시험 RED 준비·서버 구현·독립 검증이 끝났다(시험 커밋 `2e2c5c92`, 구현 커밋 `654b2728`, 2026-10-11). 독립 검증은 10-11 판정 보류(봇 미실행) 뒤 2026-10-13 봇 검증이 그 보류를 닫아 통과다. 다음은 C9 실화면이며, 사용자 복귀 뒤 메인이 Unity 코드 실행 허용 줄을 넣으면 시작한다. 이어갈 곳은 [재개 지점](#재개-지점)이다. 다음 goal은 자동으로 시작하지 않는다.**

- 담당: Content 리드(`[Content 리드 Opus]`). 시작 기준 `origin/main` = `cc20d428f7988fdcb232a7c811cf2e729446abdc`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`. 결함 PR branch `fix/hud-gold-inventory-panel-20261010`(base `cc20d428`, 병합 뒤 원격 삭제). 던전 패킷·창 PR branch `feat/dungeon-clear-packets-20261011`(base `e775bc77`, 2026-10-11에 origin/main `6782cd67`을 합침, 병합 뒤 원격 삭제). 던전 클리어 PR branch `feat/dungeon-clear-server-20261011`(base `999b1b98`).
- 착수 원천: 메인 `msg_9fe1d49e00bb`(2026-10-10T07:39:37Z)가 전달한 사용자 승인. 원문은 아래 [적용 중인 사용자 결정](#적용-중인-사용자-결정)에 있다. 범위 원문은 리드 초안 `msg_f1bba119c853`이다.
- 로컬 원문·계약·판정은 `.backups/verification/2026-10-10-dungeon-clear-rewards/`에 둔다(Git 밖).

## 범위

파티가 보스방에서 보스를 잡으면 「던전 클리어」로 보고 그 보스방 안의 전원에게 보상을 주고, 보상 창의 「확인」으로 마을에 돌려보낸다. 인스턴스 맵이 들어오기 전에는 눈에 띄는 결함 둘과 던전 패킷·보상 창을 먼저 끝낸다.

| 항목 | 내용 |
|---|---|
| 만들 것 | (결함) HUD 골드를 서버 재화에 연결한다. 인벤토리 패널을 「I」 키로 열고 닫는다. (던전) 보스방 복사본의 클리어 정의와 보상표, 클리어 결과 패킷과 확인 요청 패킷(39·40번), 클라이언트 보상 창과 「확인」 전송, 사냥터 포탈의 던전 입구 표시, 서버 클리어 판정과 보상 원자 지급, 보스 처치자 보상을 클리어 보상으로 합치기, 「확인」 뒤 마을 복귀 요청, 봇 두 파티 시나리오, 기능 문서. |
| 건드릴 곳 | (결함) 클라이언트 `UI/HudController.cs`, `UI/InventoryPanel.cs`·`UI/InventoryPanelView.cs`, 입력 연결, EditMode 시험. R10은 `Scenes/99.UI/UI.unity`의 HUD 골드 글자(`GoldText`, 필요하면 `Gold_bar`) 직렬화 값과 PlayMode 시험(사용자 결정 37 A). (던전 패킷·창) PDL·생성 패킷·프로토콜 버전, Shared GameData의 던전 보상표(새 폴더), 클라이언트 새 보상 창·수신 핸들러. (던전 서버) 서버 새 던전 폴더, `Items/KillRewardPolicy.cs`·`Items/InventoryRegistry.cs`의 보상 연결, `GameMap` 보스 사망 분기 안쪽과 `GameWorld` 처치 콜백(World 인스턴스 맵 PR 병합 뒤), `HandlerRegistry` 등록 한 줄, `Sessions/GameSession.cs`의 확인 요청 제출 메서드 하나(2026-10-11 사용자 결정), 새 핸들러, 서버 시험, 봇. 마을 복귀는 World의 「서버가 시키는 이동」 진입점을 부르기만 한다. 이 goal.md, CURRENT의 Content 줄, FEATURE_MAP·영역 문서의 해당 줄. |
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
| P5 | Shared GameData의 새 던전 폴더에 클리어 보상표를 둔다. 값은 골드 50, Material 1, CoinPouch 1이다(10-08 결정 2 A의 임시값, 지금 보스 처치자 보상과 같다). 모든 아이템은 카탈로그에 있고, 개수는 1~`InventoryLimits.MaxStack`, 골드는 0~`InventoryLimits.MaxCurrency`, 아이템 종류는 패킷 칸 수(3) 이하다. 이 PR에서 서버는 이 표를 읽지 않는다. | 서버 시험 |
| P6 | 클라이언트 39번 핸들러는 정확한 길이·머리, 결과 세 값, 골드 범위, 칸마다 「카탈로그 id와 개수 1~`MaxStack`」 또는 「0/0」을 검증한다. 하나라도 틀리면 기존 인벤토리 핸들러처럼 조용히 버린다. 창은 열리지 않고 상태도 바뀌지 않는다. 맞으면 메인 스레드에서 창을 연다. | PlayMode 시험(실제 마을 씬, 스크립트 TCP 상대) |
| P7 | 보상 창은 런타임으로 만든다. 성공이면 받은 골드와 아이템 줄(카탈로그 표시 이름과 개수)을 보인다. `CurrencyCap`·`InventoryFull`이면 받은 것이 없다는 것과 그 이유를 보인다. 「확인」은 연결이 handshake를 마친 때에만 40번(`reserved` 0)을 정확히 한 번 보내고 창을 닫는다. 두 번 눌러도 한 번만 보낸다. 창이 열린 동안 새 결과가 오면 새 결과로 바꾼다. 숨긴 창은 클릭과 공격 입력을 막지 않는다. 연결 종료 정리와 게임플레이 밖 씬에서는 닫혀 있다. | PlayMode 시험(같은 방식). PlayMode로 닿기 어려운 경로(handshake 전 「확인」)는 독립 검증에서 본다 |
| P8 | 마을(Town) 게임플레이 씬에서 마을의 사냥터행 포탈 위에 「던전 입구」 글자를 런타임으로 붙인다. 사냥터·보스방에는 붙이지 않는다. 마을의 포탈은 하나라는 전제를 시험으로 고정해, 두 번째 포탈이 생기면 그 시험이 먼저 깨진다. 글자는 클릭과 공격 입력을 막지 않는다. 맵·포탈 표, 서버, scene·prefab 파일은 바꾸지 않는다. | PlayMode 시험(실제 마을·사냥터 씬) |

**미리 정하는 공개 이름**: 선행 시험과 구현이 같은 이름을 쓰도록 여기서 고정한다. 생성 패킷의 필드는 `result`, `currency`, `reward0ItemId`·`reward0Count`·`reward1ItemId`·`reward1Count`·`reward2ItemId`·`reward2Count`(기존 snapshot의 `slot0ItemId` 꼴), 확인 요청은 `reserved`다. 보상표는 `98_Shared/GameData/Dungeon/DungeonClearRewards.cs`의 정적 클래스 `Shared.GameData.DungeonClearRewards`이고, `Currency`(int), `Items`(`IReadOnlyList<InventorySlot>`), 패킷 칸 수 `MaxItemKinds`(const int 3)를 가진다. 클라이언트 핸들러·창·입구 글자의 타입 이름은 구현이 정하고, 시험은 화면 글자와 버튼으로 관찰한다.

P6~P8을 PlayMode로 보는 이유: 실제 씬의 전투 씬 초기화와 dispatch 표를 거쳐 창과 글자가 생기는지까지 한 번에 본다. 시험이 미리 정한 내부 타입 이름에 묶이지 않고 화면 글자와 버튼으로 관찰한다. 상대는 기존 PlayMode fixture의 스크립트 TCP 상대(임시 포트, 7777 아님)다.

**건드릴 곳 보충**(메인 조건 1): 입구 표시와 보상 창은 클라이언트 전투 씬 초기화 `03_Client/Assets/Scripts/Combat/CombatBootstrap.cs`의 installer 배열에 한 줄씩 더하고, 그 줄이 부르는 Build 메서드 둘만 더한다. 이 파일의 주석이 정한 「새 인프라 = Build 메서드 + 이 배열 1행」 방식이다. 기존 installer의 순서·내용은 바꾸지 않는다. 39번 수신은 `Network/UnityClientSession.cs`의 dispatch 표에 한 줄을 더한다(같은 파일 주석 「새 패킷 추가 = 핸들러 1개 신설 + 여기 1줄 등록만」).

**고친 기존 시험**: 패킷 수 38과 버전 17을 단정하는 기존 시험(서버 `Items/InventoryProtocolContractTests.cs`, `Maps/BossBehaviorTests.cs`의 버전 시험, 클라이언트 EditMode `InventoryProtocolCompatibilityTests.cs`)은 P1 때문에 40·18로 바뀐다. 바꾸는 쪽은 선행 시험 작성자이고, 분류표(옛 세부, 요구 출처 P1)와 같은 명령의 전후 원시를 남긴다(메인 결정 `msg_71e41e231d55`).

**보존 동작**: 기존 1~38 패킷의 직렬화, 미등록 요청을 버리는 서버 경로, 인벤토리·파티 팝업·스테이지 클리어 표시·포탈 입력, 기존 봇 시나리오, HUD와 인벤토리 패널.

**실행**: WSL `dotnet` 빌드·시험 전체, 실제 생성기 재생성 diff, Windows에서 Shared·ClientNet DLL 재빌드와 `03_Client/Assets/Plugins/` 사본 diff(메인 조건 2), 새 서버와 기존 봇 접속 회귀, Unity 컴파일·EditMode 전체, 독립 검증의 PlayMode 전체(서버 lane, 회귀). 실화면은 클리어 PR에서 한다. 7777과 Unity batch 같은 큰 실행은 Core SQL 컨테이너 재측정과 Management 운영툴 중간 점검(7777)에 미리 알려 겹치지 않게 하고, 메모리 4GB 기준을 원시로 남긴다(메인 조건 3).

### 던전 클리어 PR의 요구

완료조건 4~8을 서버에서 만든다. World 리드가 GameMap·GameWorld·확인 맥락을 확인했고(아래 「World 확인」), 메인이 클리어 PR 범위를 확인했다(리드 요청 `msg_0d95711472f4`, 메인 `msg_a3531f312474`). 원고는 근거 폴더의 `clear-pr-prep/lead-design-clear-pr.md`다. 사실 근거는 HEAD `54b7d88a`의 리드 직접 대조와 읽기 전용 조사 1회다. PR230 병합 뒤 origin/main `999b1b98`에서 리드가 다시 대조했고, 아래의 코드 위치와 바뀔 기존 시험의 줄 번호가 모두 같았다.

| 번호 | 요구 | 확인 방법 |
|---|---|---|
| C1 | 보스방 복사본(`MapId.BossRoom`이고 `InstanceKey`가 있음)에서 보스가 처음 죽는 틱에, 그 틱의 그 맵 `Players` 중 세션이 살아 있는 사람이 수혜자다. 파티 소속은 다시 보지 않는다. 그 틱에 이동 중인 사람, 사냥터 복사본에 남은 파티원, 클리어 뒤 들어온 사람은 수혜자가 아니다. | 서버 시험 |
| C2 | 수혜자마다 `DungeonClearRewards` 묶음을 기존 `InventoryTransitions.Grant`로 전부 주거나 전부 거부한다. 성공이면 상태를 한 번 commit하고 revision이 1 오른다. 거부(`InventoryFull`·`CurrencyCap`)면 상태가 그대로다. | 서버 시험 |
| C3 | 수혜자마다 39 `S_DungeonClearResult`를 정확히 한 번 보낸다. 성공이면 결과 `Success`와 준 골드·아이템, 그 뒤 새 snapshot이다. 거부면 결과 코드만 있고 골드 0·빈 칸이며 snapshot은 없다. 순서는 아이템 사용과 같다(결과 → snapshot). | 서버 시험 |
| C4 | 같은 클리어의 보상은 두 번 나가지 않는다. 지급은 기존 `IsStageCleared`가 false에서 true로 바뀌는 한 번에 묶는다. 보스는 방이 빌 때만 다시 나온다. 빈 복사본은 들어오는 사람이 없으면 그 틱 끝에 정리된다. 입장 대기 중인 사람이 있어 남은 빈 보스방은 보스를 다시 내고, 그 처치는 새 클리어로 그때 방에 있는 사람에게 지급된다. 지급 단위는 복사본이 아니라 처치다(독립 검증 V-1, 리드 결정 2026-10-11, `lead-r2-clear-verify/r2.md`). | 서버 시험(`DungeonClearRepeatClearTests` 포함) |
| C5 | 보스 처치자에게 따로 가던 처치 보상은 나가지 않는다. 일반·골렘 처치 보상은 그대로다. | 서버 시험. 기존 보스 보상 시험은 옛 세부로 분류해 고친다 |
| C6 | 40 `C_DungeonClearConfirm`은 정확한 길이 5·머리·`reserved` 0만 받고, 캐릭터 선택 뒤에만 받는다. 현재 맵 job 안에서, 그 맵이 클리어된 보스방 복사본이고 보낸 사람이 그 복사본의 수혜자이며 아직 확인하지 않았을 때만 `MapMigration.MoveToPublicMap(session, entityId, map, MapId.Town)`을 부른다. 그 밖은 상태·패킷 변화 없이 버린다. `Accepted`가 아니면 수혜자 표시를 남겨 다시 누를 수 있다. | 서버 시험 |
| C7 | 마을 도착은 World 진입점이 정한 기본 입장 지점이고, 인벤토리·재화·파티가 보존된다. | 서버 시험(진입점 보존은 World 시험이 이미 증명하고, 여기서는 연결만 본다) |
| C8 | 봇 두 파티(각 두 명)가 각자 보스방 복사본에서 보스를 잡고, 각자 39번을 한 번씩 받고, 확인 뒤 마을에 도착한다. 상대 파티 복사본의 결과는 받지 않는다. 기존 봇 회귀가 그대로다. | 봇(7777, 사전 알림) |
| C9 | 실제 클라이언트에서 마을 → 사냥터 → 보스방 → 보스 처치 → 보상 창 → 인벤토리 표시 → 「확인」 → 마을 복귀. 세 크기에서 보상 창과 HUD·패널의 겹침 0. 보스방 → 엔딩 → 마을 길도 남는다. | 실화면(MCP, 사용자 결정 필요) |

서버 흐름과 소유:

1. 보스 사망은 맵 틱의 `GameMap.HandleEnemyDeath` 보스 분기 안쪽에서 일어난다(World 소유). `SetStageCleared` 직후, 복사본이면 그 틱의 수혜자 `(entityId, session)` 목록을 값으로 잡는다. 목록 계산은 서버 새 던전 폴더의 순수 규칙이 맡는다. `GameMap`은 규칙을 부르고 결과를 넘기기만 한다. 넘기는 길은 기존 `_onEnemyKilled`처럼 생성자로 주입한 콜백 하나이고, `GameWorld`가 연결한다.
2. 확인 판정에 쓰는 「이 복사본의 수혜자이고 아직 확인 안 함」 집합은 그 복사본 `GameMap`이 가진다. 복사본이 정리되면 함께 사라져 따로 정리할 수명이 없다. 바꾸는 것은 맵 틱 스레드뿐이다.
3. 지급은 World 틱의 Inventory 단계에서 한다. `InventoryRegistry`에 클리어 지급 요청 등록을 더한다. 보스 사망 틱에 등록된 세션을 잡고, 같은 World 틱의 Inventory 단계에서 수혜자마다 `Grant` → commit → 결과 송신 → 성공이면 snapshot 송신이다. 아이템 사용 결과와 같은 직접 송신이다. 처치 보상의 송신 길은 이동 중이면 생략되므로 쓰지 않는다.
4. 처치 보상: `GameWorld.MakeMap`의 처치 콜백에서 보스면 처치 보상을 등록하지 않는다. `KillRewardPolicy`의 Boss 줄은 지우고, 보스가 들어오면 기존처럼 정책 오류다. 일반·골렘은 그대로다.
5. 확인 요청은 세션에서 맵 job으로 간다. 서버 새 던전 핸들러가 길이·머리·`reserved`를 검사하고 `GameSession`의 새 제출 메서드를 부른다. 이 메서드는 포탈 입장 제출처럼 현재 맵(이동 중이면 없음)과 entityId를 잡아 현재 맵 job으로 넘긴다. job 안에서 소유 확인 → 수혜자 집합 확인 → `MoveToPublicMap` → `Accepted`면 집합에서 지운다. `HandlerRegistry`에 한 줄을 더한다.
6. 모든 단계가 World 틱 스레드의 메모리 작업이고 DB·파일 I/O는 없다.

**건드릴 곳 보충**(2026-10-11 사용자 결정): 5번의 제출 메서드는 `Sessions/GameSession.cs`의 `SubmitEnterPortal` 옆에 같은 모양으로 하나만 더한다. 이 파일의 다른 구간은 바꾸지 않는다. 쓰기 전 알림과 병합 순서는 아래 「파트 간 소유와 계약」의 세션·등록 행을 따른다.

리드가 정한 것(이유):

- `RevisionExhausted`는 `Grant`가 돌려줄 수 있지만 P3의 결과 값 셋에 없다. 연결마다 revision이 40억 번 바뀌어야 나오는 값이라 실제로 닿지 않는다. 이 경우 결과 패킷을 보내지 않고 기록만 한다. 수혜자 표시는 남아 확인은 할 수 있다. 패킷·창 PR의 P3을 바꾸지 않으려는 선택이다.
- 수혜자는 맵 `Players`로 판정한다. 아래 「파트 간 소유와 계약」의 클리어 대상 합의와 같다.
- 결과는 직접 송신한다. 처치 snapshot은 이동 중 생략될 수 있다([서버 영역 계약](../../../00_Document/domains/server.md)). 클리어 결과는 창과 「확인」의 시작점이라, 생략되면 그 사람이 보스방에 멈춘다.

바뀔 기존 시험(옛 세부, 요구 출처 C5 = 범위 승인 질문 3 A): `GameServer.Tests/Items/InventoryAuthorityBoundaryTests.cs`의 보스 보상 묶음(20행)과 그 묶음으로 거부를 보이는 두 시험(39~61행), 보스가 처치자에게 주는 50+2(168~188행). `InventoryWireContractTests.cs`의 `BossKill_GrantsPouchMaterialAndFiftyCurrencyInItemIdOrder`(254~264행). PR230의 P4 시험 `Dungeon/DungeonClearPacketContractTests.cs`도 바뀐다(요구 출처 C6). 147행은 「서버는 40 처리기를 등록하지 않는다」를 단정하고, 152행은 같은 전제를 실패 메시지에 담는다. 이 PR이 처리기를 등록하므로 옛 세부다. 나머지 단정(마을의 비수혜자가 보낸 40은 응답·종료·인벤토리 변화가 없음)은 C6에서도 맞으므로 그대로 둔다. World 병합 작업자는 같은 파일의 `JoinAndEnter`(188~197행)에 로그인 한 단계를 더한다(World `msg_3b64363cd9dd`). 선행 시험 작성자가 고치고 분류표와 같은 명령의 전후 원시를 남긴다(메인 결정 `msg_71e41e231d55`). 문서 `FEATURE_MAP.md` 20행의 「서버는 아직 39번 미발신·40번 미등록」과 `domains/protocol.md` 22행의 같은 내용도 구현과 함께 바꾼다.

봇: 두 파티 시나리오는 지금 `Party/InstanceMapIsolationSmoke.cs`(네 봇, 마을 → 사냥터 → 마을)뿐이다. 이것은 바꾸지 않고, 보스방까지 가는 새 시나리오를 더한다. 보스 문은 기존 DEBUG 치트로 연다(`BossStageClearSmoke`와 같은 방식). 단정은 파티마다 39번 한 번, 결과 Success, 골드와 아이템이 Shared 표와 같음, snapshot revision +1, 확인 뒤 마을 맵 이동, 다른 파티의 39번 0이다.

World 확인(World `msg_33914e42ac7c`, 2026-10-11T07:23:48Z, world-active `8f6c83f6` 기준). 리드 질문은 `outbox/68-world-clear-design.txt`다.

- 자리: `GameMap.HandleEnemyDeath`의 `!IsStageCleared` 안쪽, `SetStageCleared()`와 `BroadcastStageClear` 뒤다. 복사본 하나에 한 번만 도는 유일한 자리다.
- `GameMap`에는 필드 하나와 호출 한두 줄만 둔다. 수혜자 계산과 「확인 안 함」 집합 규칙은 Content 던전 폴더의 작은 타입(클리어 상태 객체)에 두고, `GameMap`은 그 객체를 들고 부르기만 한다. `GameMap.cs`는 SA1201·SA1202·IDE0011이 error인 파일이다.
- 「세션이 살아 있는 사람」은 `player.Owner`가 `IsClosing`이 아닌 사람이다. 그 틱에 이동 중인 사람은 이미 `Players`에 없다.
- 공용 맵(`InstanceKey` null)에서는 이 분기가 돌지 않는다. 공용 보스 맵이 생겨도 안전하다.
- `GameWorld.MakeMap` 처치 콜백에서 보스의 `EnqueueKill`을 빼고 클리어 콜백을 잇는 것은 Content 판단이다. 같은 람다의 보스 처치 퀘스트 리셋 `_quest.ResetProgressFor(key)`는 그대로 둔다(인스턴스 맵 goal 결정).
- 확인 맥락은 맞다(`server.md` 19행 「현재 맵 job 안에서 `MoveToPublicMap`」). `Accepted`는 이동 시작이고 도착은 다음 틱 목적지 job이다. 그래서 `Accepted`이면 수혜자 표시를 지운다. 거부 다섯은 상태·패킷을 바꾸지 않으니 표시를 남긴다.
- 함정: 목적지 맵을 못 찾으면 연결을 끊고도 `Accepted`를 돌려준다. Town은 늘 있어 실제로는 생기지 않는다. 시험에서 목적지 없는 경우를 만들 때만 유의한다.
- World 다중 계정 로그인이 바꾸는 곳: `GameMap`은 없다. `GameWorld`는 필드·생성자의 계정 등록부, `Accounts`, `DrainSessionCloses`의 `_inventory.Forget(session)` 옆 한 줄, `OnTick` 첫 `DrainSessionCloses()` 뒤 한 줄이고 `MakeMap`은 없다. `GameSession`은 로그인 상태 필드와 `TryBeginLogin`·`CompleteLogin`·`RejectLogin`·`IsLoggedIn`·`Account`(handshake 근처)다. `CharacterSelectHandler.cs` 맨 앞 게이트와 `HandlerRegistry` 한 줄도 바꾼다. Content도 `HandlerRegistry`에 한 줄을 더하니 한 줄 충돌 정도다. 먼저 병합된 쪽 뒤의 PR이 main을 받는다.
- 봇: World PR 뒤에는 모든 봇이 handshake 뒤 로그인해야 들어간다. `ProbeBase`는 자동으로 하고, handshake를 손으로 짜는 시나리오만 로그인 단계를 더한다.

### 파트 간 소유와 계약

| 대상 | 소유와 합의 |
|---|---|
| 인스턴스 맵(사냥터·보스방 복사본), 맵 종류 표, 맵 이동, 보스 처치 때 퀘스트 초기화 범위, 「서버가 시키는 이동」 진입점 | World(`[World 리드 Opus]`, 회신 주소 `run:run_c21dddd08312`). 10-08 Core 계획 v2를 World가 이어받았다(메인 `msg_6412194a47fa`). |
| 입구 | 기존 마을 → 사냥터 포탈이다. Content가 맵 종류 표·포탈 표에 더할 줄은 없다(World `msg_6e96d918962c` 1). |
| 클리어 대상 | 보스 사망 틱의 그 보스방 복사본 플레이어 목록. 파티원 재확인은 하지 않는다(같은 메시지 2). 열쇠는 `GameMap.InstanceKey`(`InstanceKey?`)다. 공용 맵은 null이고 복사본은 값이 있다. `InstanceKey`는 `Owner`(Party/Solo)와 `Id`를 가진 값 비교 record struct이며, 복사본에 들어갈 때 정해져 그 안에서 파티를 나가도 바뀌지 않는다(World `msg_14c7e14412f3` 1, World 선행 시험 commit `3511b66c`). |
| 마을 복귀 | Content의 「확인」 처리기가 플레이어 한 명씩 World 진입점을 부른다. 틱 스레드의 현재 맵 job 안에서 부른다. Content 요구: 도착 좌표 기본값은 World가 정함, 예외 대신 거부 결과, 이동 중 인벤토리·파티 보존(Content `msg_92f0e5340ca3`, World `msg_6e96d918962c` 3). World 답(`msg_25abb610ef01`): 도착은 도착 맵의 기본 입장 지점(`GameMap.PlayerSpawnPosition`, 최초 입장·재접속과 같은 곳), 인스턴스 밖·이동 중·세션 종료 중·같은 사람 두 번째 호출은 상태를 바꾸지 않는 거부, 성공하면 포탈 이동과 같은 맵 이동 패킷만 나간다. 진입점은 `MapMigration.MoveToPublicMap(GameSession session, int entityId, GameMap currentMap, MapId destination, Vector2? spawn = null)` → `MapMoveResult`다. 거부 검사 순서는 `SessionClosing` → `AlreadyMigrating` → `NotInThatMap` → `NotInInstanceMap` → `DestinationNotPublic`이고, 모두 통과하면 `Accepted`다. 공용 맵 도착은 다음 틱 이내다(World `msg_14c7e14412f3` 2). |
| PDL·생성 패킷·프로토콜 버전 | 던전 패킷·창 PR이 39·40번과 버전 17 → 18을 쓴다. World 인스턴스 맵 PR은 PDL을 바꾸지 않는다. World의 다중 계정 로그인과 같은 시기에 열리면 먼저 병합되는 쪽이 앞 번호를 갖고, 뒤 PR이 main을 받아 재생성해 번호를 다시 확인한다(같은 메시지 4). 기존 번호 재사용·재배열 금지. World 로그인 패킷 `C_Login{string accountName}`·`S_LoginResult{byte result}`(`LoginResult` Accepted0·InvalidName1·AlreadyOnline2)도 main 기준 39·40·v18을 계획한다(World `msg_4e01d634628b`, commit `757b59ac`). World가 먼저 병합되면 Content PR은 버전 단정·생성 코드·이력 주석·DLL에 더해 World 시험 `Network/LoginProtocolContractTests.cs`의 T9(로그인 패킷이 가장 큰 두 번호)와 Content P4 시험(`Dungeon/DungeonClearPacketContractTests.cs`, 직업 선택 입장에 로그인 helper가 필요)을 맞춘다. Content가 먼저면 World가 41·42를 쓴다. **결과**: PR230이 먼저 병합돼(2026-10-11T10:57:10Z) 39·40·v18은 Content가 쓴다. World 로그인은 41·42와 v19로 다시 번호를 매긴다(Content 알림 `msg_929b97d8c951`). World 병합 작업자는 Content 시험 `Dungeon/DungeonClearPacketContractTests.cs`의 `JoinAndEnter`에 로그인 한 단계를 더하고(`msg_3b64363cd9dd`), `domains/protocol.md` v18 절 첫 문장을 과거 시제로 바꾼다(`msg_725c091951ad`). |
| `GameMap.cs` 보스 사망 분기, `GameWorld.cs` 처치 콜백, `Items/InventoryRegistry.cs` | World 인스턴스 맵 PR 병합 전에는 Content가 쓰지 않는다. World는 `InventoryRegistry.cs` 본문을 바꾸지 않을 계획이며, 바꿔야 하면 쓰기 전에 알린다(같은 메시지 5). 인스턴스 맵은 main에 들어왔다(`2bbd710d`). Content는 쓰기 전에 World에 다시 알린다. |
| `Sessions/GameSession.cs`, `Handlers/HandlerRegistry.cs` | 아이템 goal 때는 GameDev(지금 Core) 소유 파일로 순차 쓰기 허용을 받았다. World 로그인 PR도 `GameSession`의 로그인 상태와 `HandlerRegistry` 한 줄을 바꾼다. Content는 쓰기 전에 World·Core 리드에 함께 알리고, 늦게 병합되는 PR이 main을 받는다(메인 조건 `msg_a3531f312474`). |
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
- **Content·Core 우선**(메인 `msg_e9af11e8efee`, 2026-10-11T07:46:38Z). 원문 「일단 좀 급한게 컨텐츠랑 코어쪽이니까 이 쪽 작업 우선해보자, 운영툴은 재개지점 맥락확보하고 세션 정리해줘」. 효과: Content·Core가 먼저다. Management는 재개 지점을 남기고 닫으며, 그 7777 중간 점검 일정은 없어졌다(메인 조건 3의 Management 겹침 대상이 사라짐). World·Rules는 큰 실행이 겹치면 양보한다. 큰 실행 전 알림은 그대로 한다.
- **라우팅 핫픽스**(메인 `msg_154e43b875cb`, 2026-10-11T11:03:16Z). 원문 「대시보드 결정 응답: 1) 모델 라우팅 - 모든 리드를 문맥 25만에서 압축하도록 지금 적용할지 → A 전체 적용 · 2) 모델 라우팅 - 리드가 같은 메시지로 두 번 깨는 것을 지금 규칙으로 막을지 → A 지금 적용 · 3) 모델 라우팅 - 로컬 DB 설치·기동 도구의 검증을 지금부터 Astra로 옮길지 → A 지금부터 Astra · 4) 모델 라우팅 - 선행 시험 작성자를 Codex로 옮기는 시점 → A 10-31 평가 때」. 이 goal에 닿는 효과는 셋이다. 1: Content 리드는 `--autocompact 250000`으로 다시 떴다. 압축 뒤 맥락을 잃어 일을 다시 한 사례가 나오면 바로 메인에 보고한다. 2: 우편함 대기 background가 살아 있는데 Orca 자동 알림으로 깨면 도구 호출 없이 한 줄로 턴을 끝낸다. 대기가 꺼졌거나 확실하지 않으면 check로 처리하고 대기를 다시 연다. 4: 선행 시험 작성자는 10-31 평가 때까지 신규 `claude-opus-5-5` 그대로다. 3은 로컬 DB 도구 검증이라 이 goal에 해당하지 않는다. **결정 2 정정**(메인 `msg_6e84b81f7e50`, 2026-10-11T11:49:52Z): 「대기를 다시 연다」는 사용자 결정 2에 없던 메인의 덧붙임이었고 ORCA 정본과 어긋났다. 사용자가 정본을 유지했다. 원문(메인 pane Enter 제출) 「대시보드 결정 응답: 1) 라우팅 정본 반영 - 메모리 부족으로 꺼진 우편함 대기를 리드가 다시 켜지 않는 규칙을 유지할지 → A 유지 · 2) 라우팅 정본 반영 - 규칙 문서 크기가 약 1.8KB 느는 것을 허용할지 → A 허용」. 정정판: 대기가 아직 돌고 있고(종료·kill 알림을 받지 않음) Orca 자동 알림으로 깼으면 도구 호출 없이 한 줄로 턴을 끝내고, 메시지는 대기가 끝나 깰 때 처리한다. 대기가 메모리 회수로 꺼졌거나 살아 있는지 확실하지 않으면 `--wait` 없는 check로 처리한다. 메모리 회수로 꺼진 대기는 스스로 다시 켜지 않는다. 메시지 도착·시간 초과로 정상 종료한 대기를 다음 대기로 여는 것은 그대로다.
- **클리어 PR의 세션 파일 제출 메서드**(메인 `msg_a3531f312474`, 2026-10-11T11:06:04Z). 원문 「대시보드 결정 응답: 1) Content 클리어 PR - 세션 파일에 확인 요청 제출 메서드 하나를 더하는 것을 범위에 넣을지 → A 넣는다」. 효과: `Sessions/GameSession.cs`에 확인 요청 제출 메서드 하나가 「건드릴 곳」에 들어간다. 나머지 범위는 메인이 리드 요청 `msg_0d95711472f4`와 승인 범위를 대조해 차이가 없었다. 메인 조건은 둘이다. `GameSession`·`HandlerRegistry`는 쓰기 전에 World·Core 리드에 함께 알리고 늦게 병합되는 PR이 main을 받는다. 7777·Unity 큰 실행은 미리 묻는다. 구현자 신규 `gpt-6-astra` xhigh(신호 2·3·4)와 선행 시험 신규 `claude-opus-5-5` 배정도 확인했다.
- **마감 준비**(메인 `msg_e5eef9da3206`, 2026-10-11T14:03:05Z). 원문 「일단 슬슬 마감 준비도 하자, 각 파트 마지막 작업 끝나면 재개 포인트 잡고 마무리하는 방향으로 지시해줘」. 효과: Content의 마지막 작업은 돌던 독립 검증의 결과 처리까지다. 새 작업자 세션은 열지 않는다. 리드는 정산하고 남은 프로세스 0을 확인한 뒤 재개 지점을 고쳐 자기 branch에 커밋·push하고 메인에 「마감 준비 완료」를 보낸다. C9 실화면과 Unity 실행 허용 결정은 다음 세션이다.
- **10-28 시연 영상 방향**(메인 `msg_acaf45a27240`, 2026-10-13T03:34:07Z). 원문 「대시보드 결정 응답: 1) 10-28 시연 영상 - 길드 거점 최소 순환을 새로 넣을지 → A 길드 거점 넣음」. 이 goal에 닿는 효과: 진행 중인 이 goal은 지금 순서(봇 → 실화면 → PR)대로 끝까지 간다. 던전 클리어 보상은 영상의 한 장면 후보다. 시연 시나리오 승인 전에는 새 goal을 열거나 준비하지 않는다.
- **10-28 시연 계획과 파트 배정 승인**(메인 `msg_2b5d1c958117`, 2026-10-13T03:46:57Z). 원문 「대시보드 결정 응답: 1) 계획 검토 - Main 10-28 시연 영상 시나리오와 파트 배정 → A , 코멘트 「A로 가되 길드거점은 맵형태로 나중에 시간나면 꼭 만들었으면 좋겠어, 시각적인 성장 이펙트나 이런게 도드라지면 좋겠네, Shader로 해결할 수 있는건 해결하고, 문제는 이제 애니메이션 시트를 만드는게 문제인데, 이게 GPT Image 2.5로 해결이 되는지, 아니면 힉스필드나 Unity AI 토큰으로 만들어서 해결해야 하는지도 문제네, 그리고 Unity AI로 시트화 시키는건 애니메이션 일관화가 프롬프팅으로는 랜덤성이 짙어서 원하는거를 뽑을때 까지 드는 비용도 생각해야 돼」」. 승인된 계획 원문은 저장소 밖 `C:/Dev/DawnHolder_Dashboard/plans/demo-video-1028-scenario.md`다. 이 goal에 닿는 효과: 이 goal은 지금 순서대로 끝낸다. 다음 Content goal(길드 화면과 두 클라이언트 촬영 리허설)은 R-8대로 새 리드가 연다. 같은 `03_Client`를 쓰는 아트 파트와 파일 소유(스크립트는 Content, 그림·셰이더 자산은 아트)와 Unity 실행 시각을 조율한다(리드 알림 `msg_587619160192`).
- **C9 실화면 동안만 Unity 코드 실행 허용**(메인 `msg_a7191a5cc619`, 2026-10-13T05:15:10Z). 원문 「대시보드 결정 응답: 1) 실화면 두 건 - 확인하는 동안만 Unity 코드 실행을 허용할지(World 로그인, Content 던전 클리어) → A 확인 동안만 허용」. 효과: Unity MCP 시트는 World 로그인 실화면이 먼저 쓴다(`msg_5caf86048690`). World가 시트를 풀면 메인이 content-active `.claude/settings.local.json`(Git 무시)에 허용 한 줄을 넣고 SHA256을 알린다. 그 알림 전에는 Editor·검증자를 띄우지 않는다. MCP 사용이 끝나면 리드가 「허용 줄 지울 차례」를 알리고, 메인이 그 한 줄만 지운다. `skillOverrides`는 남긴다.
- **메인 판단(사용자 결정 아님) — C9 실화면을 사용자 복귀 뒤로 미룸**(`msg_73e4d39722f3`, 2026-10-13T05:49:57Z). World는 05:47Z에 시트를 풀었다(`msg_28246f9ce402`). 그러나 사용자가 자리를 비웠다(메인이 전한 메인 채팅 원문 「일단 나 잠깐 자고올게」). C9는 사용자가 Unity 창의 MCP 연결 승인을 눌러야 한다. 지금 Editor를 띄우면 그 단계에서 멈춘 채 메모리만 잡으므로, 메인은 허용 줄을 넣지 않았다. 결정 A의 C9 허용은 유효하며, 사용자 복귀 뒤 메인이 줄을 넣고 알린다. 그 사이 리드는 C9 검증 계약을 준비해 둔다.

## 재개 지점

다음 리드는 이 블록부터 읽는다. 기록 시각은 `date -u` 2026-10-13T05:50:54Z(`lead-goal-edit-1013b/clock.txt`)이다. C9 실화면 계약을 준비했고, 사용자 복귀 뒤 메인의 허용 줄 알림을 기다린다.

| 항목 | 값 |
|---|---|
| 단계 | 진척 「검증·실제 플레이」 중이다. 이 PR의 독립 검증은 **통과**다. 10-11 판정의 보류를 10-13 봇 검증이 닫았다(아래 「클리어 봇 검증」 절). 남은 검증은 C9 실화면이며 아직 실행하지 않았다 |
| 다음 첫 일 | **C9 실화면: 사용자 복귀 뒤 메인이 허용 줄을 넣으면 시작**(`msg_73e4d39722f3`). 결정 A(`msg_a7191a5cc619`)의 허용은 유효하다. 메인 알림이 오면 근거 폴더 `c9-screen-prep/`의 스크립트로 이 순서를 따른다. 1 `rule-check.sh`로 허용 줄과 SHA256을 대조한다. 2 여유 물리·커밋 메모리 원시를 남기고 아트·World·Core·Rules 리드에 Unity·7777 사용을 알린다. 3 `baseline.sh`로 `ProjectSettings.asset`(`4a8db0bd…`)과 `TimeManager.asset`(HEAD와 같음)을 확인하고, 다르면 메인 판단 `msg_c9fac8091065`대로 되돌린다. 4 `pre-editor-state.ps1` → Editor 기동(`editor-open.txt`) → `wait-editor-load.ps1`(`editor-loaded.json`) → `make-inputs-preflight.sh`. 5 `node c9-screen-build.mjs <시트 알림> <허용 알림> <SHA256>`로 계약과 spec을 만든다. 시트 알림은 World `msg_28246f9ce402`다. 6 `prelaunch.sh` → `split.sh` → `pane-check.sh` → `launch.sh` → `start.sh`로 검증자를 띄운다. 검증 대상은 이 기록 커밋의 HEAD이고, 검증하는 구현 Task는 `task_2bfe92fb8745`다. 검증자의 MCP 연결 승인 요청이 오면 메인을 거쳐 사용자가 누른다 |
| C9 끝난 뒤 | 판정 → 리드 R-2 → 정산 → 메인에 「허용 줄 지울 차례」 → Editor가 다시 쓴 `ProjectSettings.asset`·`TimeManager.asset`을 `msg_c9fac8091065`대로 되돌림 → C9 결과를 goal에 기록 |
| 그다음 | PR 생성·CI → 사용자 병합 승인 → 결과 기록·Gardener → 종료 기록 PR → 종료 점검 → R-8 |
| 열린 Task·Dispatch | 없다. C9 검증 Task는 `start.sh`가 기동 때 만든다. 봇 검증 `task_2f58855b1557` / `ctx_c59f731adc3d`는 정산했다(`lead-settle-bot-verify/settlement.md`) |
| 결정 대기 | 없다. C9 실행 허용은 결정 A로 승인됐고, 메인이 사용자 복귀 뒤 허용 줄을 넣는다 |
| 작업 공간 / branch | `content-active` / `feat/dungeon-clear-server-20261011`(origin/main `999b1b98`에서 만듦). base 이후 리드 커밋은 `78431e8e`(goal 범위) → `2e2c5c92`(선행 시험) → `b13db4d1`(선행 시험 기록) → `654b2728`(서버 구현) → `2d168f19`(구현 기록) → `0d5bdbdb`(검증자 새 시험 두 파일·검증 기록) → `e353b927`(봇 검증·시연 결정 기록) → 이 기록 커밋이다. 원격에 push한다. 10-13 05:50Z origin/main은 `da717247`이고, base 이후 들어온 것은 문서 PR231·232뿐이다 |
| PR | 클리어 PR은 아직 없다. 던전 패킷·창은 [PR230](https://github.com/bass131/dawnholder-server/pull/230)으로 병합됐다(head `ac98fe69`, merge commit `999b1b98`). 결함 PR은 [PR221](https://github.com/bass131/dawnholder-server/pull/221)로 병합됐다(merge commit `e775bc77`) |
| 리드 세션 / Run | Claude 세션 `eeb74a27-8d3b-43e3-b58a-b460e30db0cd`, pane `term_36206f08-343c-4c96-a029-0d2608311ff2`(10-13 03:42Z 같은 대화로 재기동) / `run_b680cd89da9a`(회신 주소 `run:run_b680cd89da9a`). 새 세션은 RESUME 절차로 연다 |
| 근거 폴더 | `.backups/verification/2026-10-10-dungeon-clear-rewards/`(Git 밖). 봇 판정 `opus-bot-verify/verdict.md`(`17a5420e…`), 리드 R-2 `lead-r2-bot-verify/r2.md`, 이전 판정 `opus-clear-verify/verdict.md`(`609fb7ed…`), 수신 기록 `lead-inbox-notes.md`. C9 준비는 맥락 메모 `c9-screen-prep/context-memo.md`, 계약 머리 `c9-screen-contract.head.md`, 조립 `c9-screen-build.mjs`, spec 틀 `c9-screen-spec.template.md`, harness 사본 `c9-screen/harness/`다 |
| 로컬에만 둔 변경 | 사용자 미커밋 `03_Client/Assets/Resources/MinimapRT.renderTexture`(`7123af77…`), `03_Client/ProjectSettings/ProjectSettings.asset`(`4a8db0bd…`). 커밋·되돌리기·stash 금지. TimeManager는 HEAD와 같다(`1a83e54a…`). 05:50:54Z `lead-goal-edit-1013b/state-before.txt`에서 세 값을 다시 확인했다 |
| 작업자·실행 자원 | Content 작업자 pane 0, Unity 프로세스 0(05:50:54Z `lead-goal-edit-1013b/state-before.txt`·`terminal-list.json`). 봇이 끝난 04:54:22Z에 7777 listener 0, WSL dotnet 0이다(`opus-bot-verify/runs/bot-1/port-after.txt`). content-active `.claude/settings.local.json`은 `skillOverrides`만 있고 허용 줄은 0이다(05:51:43Z `c9-screen-prep/rule-check-before-allow.txt`) |
| 로컬 부수 변경 | `ProjectSettings.asset`은 사용자 원래 상태 `4a8db0bd…`다. C9의 Unity가 다시 쓰면 메인 판단 `msg_c9fac8091065`대로 되돌린다. WSL 작업 사본 아래에 경로 변환으로 생긴 빈 디렉터리 사슬이 있다. 서식 검사가 WSL 캐시와 `99_Tools/Formatting/obj`·`bin`(Git 제외)을 만들었다. 모두 저장소 밖이거나 Git 제외다 |
| 다른 파트와 맞출 것 | World 로그인 PR(41·42·v19)이 `GameSession`·`HandlerRegistry`·`Dungeon/DungeonClearPacketContractTests.cs`·`domains/protocol.md`를 함께 바꾼다. 10-13 05:50Z에는 아직 origin/main에 없다. 늦게 병합되는 PR이 main을 받는다. 들어왔다면 handshake를 손으로 짜는 봇 시나리오에 로그인 단계가 필요하다(위 「던전 클리어 PR의 요구」의 World 확인). 아트 파트(`run:run_79ffbf5c8612`)와는 Unity 실행 시각을 시작 전에 알리고, 아래 두 인계를 지킨다 |
| 아트 인계: 대장장이 | 이 goal은 `Npc_BlackSmith.prefab`·`BlackSmith.controller`·대장장이 동작 코드를 바꾸지 않는다. 컨트롤러 기본 상태 교체, 새 망치질 클립, 다듬은 시트와 `.meta`는 아트 효과 PR이 맡는다. 다음 Content goal이 상호작용 컴포넌트를 그 프리팹에 붙이게 되면 쓰기 전에 아트와 파일·시기를 나눈다(아트 `msg_55ea71c3ee0a`, 리드 회신 `msg_136ca02071fc`) |
| 아트 인계: UI 창 조각 | 창 그림은 조각 조합형이다. 조각 PNG·`.meta`·9-slice, 조각 프리팹, 표시 전용 helper, 코드로 짓는 네 창(`InventoryPanelView`·`DungeonClearRewardWindow`·`PartyInvitePopup`·`NpcDialogPanel`)에 조각을 입히는 일은 아트 UI 창 PR이 맡는다. 조각 이름·변형은 그 PR 병합 때 고정하고, Content의 길드 패널·대장간 화면은 그 조각을 쓴다. 길드 패널·대장간을 코드로 지을지 프리팹·씬으로 지을지는 미정이다. C9가 보상 창 겹침 결함을 찾으면 이 PR에서 그 파일을 고칠 수 있고, 쓰기 전에 아트와 순서를 나눈다(아트 `msg_d87cbec99258`, 리드 회신 `msg_2f7a25007d10`) |

**남은 순서**: C9 실화면(사용자 복귀 뒤 메인의 허용 줄 알림) → PR·CI → 사용자 병합 승인 → 결과 기록·Gardener → 종료 기록 PR → 종료 점검 → R-8.

## 진척 단계

- [x] 범위 승인
- [x] 결함 선행 시험
- [x] 결함 구현·검증
- [x] R9 실화면
- [x] R10 골드 넘침 수정·재확인
- [x] 결함 PR 병합
- [x] 패킷·창 구현·검증
- [x] PR230 병합
- [x] 클리어 설계 확정
- [x] 클리어 선행 시험
- [x] 클리어 서버 구현
- [>] 검증·실제 플레이
- [ ] 클리어 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 병합

## 실제 결과와 미실행

- 범위 초안·사용자 승인·World 계약 확인까지 했다. 원문은 근거 폴더의 `scope-draft-v1.md`와 `inbox/`에 있다.
- 결함 PR은 선행 시험 → 구현 → 수정 1 → 독립 검증 → PR 생성(CI) → R9 실화면 → R10 수정 루프와 실화면 재확인 → 사용자 병합 승인까지 끝났다(아래 다섯 절). 실행한 것: Unity batch EditMode·PlayMode, WSL GameServer 서버 lane(DB 없음), CI, Unity Editor 실화면(MCP). 실행하지 않은 것: 봇, 빌드 단독 실행, 사람 손 입력.
- 던전 패킷·창 PR은 선행 시험 → 구현 → 줄 끝 수정 → 독립 검증(차단 V-1) → V-1 수정 → 재검증(통과) → PR230 생성 → 사용자 병합 승인까지 끝났다(아래 세 절). 실행한 것: WSL 빌드·서버 시험, Windows 솔루션 빌드, 추출본 생성기·DLL 재현, Unity batch EditMode·PlayMode, WSL GameServer 서버 lane(DB 없음), 봇 회귀. 실행하지 않은 것: 실화면, DB.
- 던전 클리어 PR은 선행 시험 → 서버 구현 → 독립 검증(판정 보류, 봇 미실행) → 봇 검증(통과)까지 했다(아래 네 절). 실행한 것: WSL 빌드, 필터·전체 서버 시험, 서식 검사, 봇 빌드, 7777 봇 실행(C8과 기존 봇 회귀). 실행하지 않은 것: Unity·실화면, DB, CI.

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

### 패킷·창 선행 시험 — RED 준비 성공, 2026-10-11

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_cdbdb60cde2c` / `ctx_7a2bd8359a41`, `[Content 검증자]`, 06:26:24Z~07:06:37Z |
| 모델 | 지정 `claude-opus-5-5`. 최초 실행 명령 `claude --model claude-opus-5-5`(TEMP `.backups/tmp/packets-tdd`), 화면 표시 「Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `packets-tdd-contract.md` v2 SHA256 `0f34c05f…8143`, 요약 spec `packets-tdd-spec.md`. v1(`4681e7c0…`)과 다른 것은 HEAD, 시각 기록 문장, 생존 신호 문장뿐이다 |
| 보고 | `packets-tdd/report.md`, worker_done `msg_978901b3f52f`(succeeded) |
| 커밋 | 시험 11파일 `cf0cacc1`(+967/−13) |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| 서버 새 두 클래스(`Dungeon/`) | 11 / 1 / 10 / 0 | `runs/server-red-new-2/*.trx`(`77b6630f`) |
| 서버 고친 두 클래스, 고치기 전 → 뒤 | 32/32/0/0 → 32/29/3/0 | `server-before`(`1db2193c`) → `server-after-existing`(`9bd45cf8`) |
| EditMode 고친 클래스, 고치기 전 → 뒤 | 8/8/0/0 → 9/7/2/0 | `editmode-before`(`34f45f9b`) → `editmode-after-3`(`4962426c`) |
| PlayMode 새 두 클래스 | 9 / 1 / 8 / 0 | `runs/playmode-red-3/results.xml`(`038539c7`) |

- **결과**: 실패 23개가 모두 「아직 없는 타입·창·글자」 때문이다. 컴파일 오류, fixture·환경 실패, 고친 시험 밖의 기존 시험 실패는 0이다. 서버 P2는 생성 타입의 `Write()`·`Read()`를 독립 encoder bytes와 대조하고, PlayMode P6~P8은 실제 Town 씬에서 스크립트 TCP 상대(임시 포트)가 보낸 39번으로 화면 글자와 버튼을 본다.
- **지금 통과하는 둘**: P4 「선택 뒤 40번은 응답·종료·인벤토리 변화 없이 버려진다」는 이 PR에서도 그대로 두는 동작이라 구현 뒤에도 통과해야 한다. P6 「틀린 bytes 12건은 창을 열지 않는다」는 지금 39번이 미등록이라 통과한다. 짝인 성공 시험이 실패하므로 대상 0 통과가 아니다.
- **고친 기존 시험**: 버전·패킷 수 단정 넷을 (a) 옛 세부로 분류해 18·40으로 바꿨고, 클라이언트에 39·40 번호 시험 하나를 더했다. 같은 명령의 전후 원시가 위 표다. 이름에 숫자가 든 버전 시험 셋은 이름도 바꿨다.
- **시험에 없는 요구**: P3 「새 enum을 만들지 않는다」, P4 「서버는 39번을 만들지 않는다」, P7 handshake 전 「확인」과 「게임플레이 밖 씬에서 닫힘」, P8 「맵·포탈 표·scene·prefab 불변」은 독립 검증의 코드 검색·diff 검토로 본다.
- **관찰면이 구현을 묶는 지점(리드 채택)**: 보고서 표의 아홉 가지를 모두 구현 계약의 조건으로 넣는다. 「확인」은 uGUI `Button` 아래 TMP 글자다. 숨김은 비활성·CanvasGroup alpha 0·Canvas 끔 중 하나이고 화면 밖 이동이 아니다. 창은 자기 canvas에 둔다. 아이템 이름과 개수는 같은 부모 아래 둔다. 골드는 축약 없이 보인다(자릿수 구분은 허용). 입구 글자는 포탈보다 위다. 창은 39번 적용 뒤 5프레임 안에 연다. 다른 대안(창 루트의 공개 이름 고정 등)은 이름을 하나 더 묶을 뿐 판정면이 같아 택하지 않았다.
- **리드 R-2**: 작업자 스크립트 없이 원시 TRX·XML 여섯 개를 직접 읽어 hash와 수치, 실패·통과 이름이 보고와 같음을 확인했다(`lead-r2-packets-tdd/counts.txt`). 서버·PlayMode 실패 메시지 18개를 읽어 모두 미구현 사유임을 봤다. 고친 기존 시험 셋의 diff가 버전·패킷 수 단정과 그 이름·주석뿐임을 확인했다. 보호 파일 hash와 Unity.exe 0을 다시 쟀다.
- **부수 변경**: Unity batch가 사용자 미커밋 `ProjectSettings.asset`의 SENTIS define 한 줄을 지웠다(`4a8db0bd…` → `5a03375a…`). TimeManager는 그대로다. 메인 판단 `msg_c9fac8091065`대로 이 PR의 마지막 batch 뒤에 리드가 되돌린다. Unity Test Framework가 batch마다 홈의 기본 결과 사본(`AppData/LocalLow/DefaultCompany/03_Client/TestResults.xml`)을 덮어쓴다. R10 선행 시험에서도 나온 harness 실행의 기본 동작이다.
- **생존 신호(helper로 잼, `lead-r2-packets-tdd/liveness.json`)**: 신호 13개, 기동 → 첫 신호 36초(리드 계산), 가장 긴 간격 445초. 300초 초과는 06:29:13Z → 06:36:38Z(445초)와 06:36:38Z → 06:42:12Z(334초) 두 구간이다. 둘 다 시험 작성 중이었고 화면은 계속 작업 중이었다. 질문 답 대기는 24초다.
- **정산**: 대기 확인(tui-idle, 빈 prompt) → worker-release(retained/external_terminal) → close(ptyKilled true) → 07:08:51Z 목록에서 사라짐. 화면 표시는 40분 24초 작업이다.

### 패킷·창 구현 — 결과, 2026-10-11

| 항목 | 값 |
|---|---|
| 구현 Task / Dispatch | `task_2344bd7a34dd` / `ctx_4342a8667657`, `[Content 구현 Astra]`, 07:21:08Z~08:13:28Z |
| 모델 | 지정 `gpt-6-astra` xhigh. 최초 실행 명령 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 화면 표시 「GPT-6-Astra xhigh」(Codex v0.162.1). backend 실제 모델 unknown. 배정 신호 3·4, 라우팅 시범 Astra 1회차 |
| 계약 | `astra-packets-impl-contract.md` v1 SHA256 `c4321b39…`, 요약 spec `astra-packets-impl-spec.md` |
| 보고 | `astra-packets-impl/report.md`, worker_done `msg_923c54cb0e93`(succeeded, 21파일) |
| 커밋 | `ef58d7de`(구현과 아래 줄 끝 수정을 함께 담음) |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| WSL 서버 전체 | 985 / 980 / 0 / 5 | `runs/` TRX(`23366e41`). skip 5는 기존 `Skip` 속성 시험이다 |
| 서식 도구 시험 | 244 / 244 / 0 / 0 | `format-run/` |
| EditMode 전체 | 418 / 418 / 0 / 0 | `runs/editmode-full/results.xml`(`a6918d83`) |
| PlayMode 새 두 클래스, 두 번 | 9 / 8 / 1 / 0, 9 / 8 / 1 / 0 | `runs/playmode-dungeon/`(`c29c4919`), `runs/playmode-dungeon-retry/`(`f79782f4`) |

- **남은 실패 하나(F-1)**: `ConfirmPointerClick_SendsOneFiveByteConfirm_ClosesTheWindow_AndLeavesNoRaycastTarget`이 두 번 모두 같은 자리(`ScreenUiProbe.cs:93`, 「the gameplay scenes have a current EventSystem」)에서 실패했다. UI 씬은 `SceneBootstrap`이 비동기 Additive로 늦게 올리고, 창을 연 직후 raycast하는 시험은 이것 하나다. 리드는 (b) fixture로 분류하고 구현자에게 시험·제품을 고치지 말고 마치라고 답했다(`msg_09068e37836b`). 분류와 수정은 독립 검증자가 다시 판단한다.
- **설계 결정(구현자)**: 39번 핸들러가 검증한 값을 `DungeonClearRewardState`(DontDestroyOnLoad 하나, 연결별 대기 결과)에 넣고, 보상 창(overlay Canvas sort 15, CanvasGroup 숨김)이 그 상태를 그린다. 씬이 바뀌는 동안 온 결과도 상태가 들고 있다가 다음 게임플레이 씬의 창이 그린다. 「확인」은 상태를 먼저 소비한 뒤 40번을 한 번 보낸다. 연결 정리는 `NetworkService.ResetGlobalSessionMirrors`에 두 줄을 더했다. 입구 글자는 마을 포탈의 자식 TMP로, 포탈 collider 위 0.6에 둔다.
- **리드 R-2**: 원시 TRX·XML 네 개의 hash와 수치·실패 이름이 보고와 같다(`lead-r2-packets-impl/counts.txt`). 손으로 쓴 제품 diff와 새 파일 다섯을 요구 P1~P8에 대어 읽었다(`lead-r2-packets-impl/code-sample-review.md`). 이때 생성기 출력의 줄 끝 문제를 찾았다(아래).
- **생존 신호(helper, `lead-settle-packets-impl/liveness.txt`)**: 신호 18개, 가장 긴 간격 437초. 300초 초과는 314초(시작 가능을 기다리던 구간), 301초, 437초(PlayMode 재실행·보고 작성)다. 질문 답 대기는 27·59·87초다.
- **정산**: 빈 prompt 확인 → worker-release(retained/external_terminal) → close(ptyKilled true) → 목록에서 사라짐(`lead-settle-packets-impl/settlement.md`).

#### 줄 끝 수정

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_acf7ee26c948` / `ctx_cd4f70418fbb`, `[Content Sol]`, 08:24:31Z 무렵~08:44:51Z |
| 모델 | 지정 `gpt-6.1-sol` max. 최초 실행 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 화면 표시 「GPT-6.1-Sol max」(Codex v0.162.1). backend 실제 모델 unknown. 배정 신호 해당 없음(명세가 고정된 1파일 기계적 변경), 라우팅 시범 Sol 1회차 |
| 계약 | `sol-eol-fix-contract.md` v1 SHA256 `eba7ab8f…`, 요약 spec `sol-eol-fix-spec.md` |
| 보고 | `sol-eol-fix/report.md`(SHA256 `c8ce844f…`), worker_done `msg_f78354be88c4`(succeeded) |

- **한 일**: 작업본 `GenPackets.cs`의 CRLF 304곳만 LF로 바꿨다(SHA256 `2b68092c…` → `cdd0e194…`). Git이 저장하는 blob은 전후 같다(`dd20d4f8…`). Windows 솔루션 빌드 뒤 Plugins `Shared.dll`은 빌드 출력과 같은 `a9223eba…`다. ClientNet DLL은 그대로다(`dc708e2f…`).
- **재현**: HEAD 추출본에 이 PR의 Shared 소스 셋을 얹어 다른 폴더에서 빌드해도 `a9223eba…`가 나왔다. 커밋 `ef58d7de`의 GenPackets·Shared.dll bytes도 이 값과 같다(`lead-r2-eol-fix/commit-check.txt`).
- **진단**: 경제 커밋 `32626771` 추출본에서 생성기를 다시 돌려 CRLF 섞인 GenPackets로 빌드하니 그 커밋의 DLL `595e970a…`가 그대로 나왔다. 같은 커밋을 LF 그대로 빌드하면 `901996c7…`로 다르다. HEAD를 LF 그대로 빌드한 `85f9a741…`는 구현자 기준선 `8411d6d2…`와 달랐다. 구현자 기준선 때 작업본 GenPackets가 이미 CRLF 섞인 `f770d1c4…`(생성기 재생성본과 같은 bytes)였기 때문이다. `eol=lf` 정규화 때문에 이 파일은 `git status`에 수정으로 보이지 않았다(리드 확인 `lead-r2-eol-fix/r2.md`).
- **리드 R-2**: 작업본·추출본 다섯 DLL과 GenPackets hash, eol 상태, blob id, 보호 세 파일을 리드가 직접 다시 계산해 보고와 같음을 확인했다(`lead-r2-eol-fix/source-check.txt`).
- **생존 신호(helper, `lead-settle-eol-fix/liveness.txt`)**: 신호 8개, 기동 → 첫 신호 약 68초, 가장 긴 간격 386초. 300초 초과는 08:34:32Z → 08:40:58Z(보고서 작성 구간) 하나다. 작업자가 escalation으로 스스로 알렸다(`msg_036c3035a448`). 질문은 없었다.
- **정산**: tui-idle → worker-release(retained/external_terminal) → close(ptyKilled true) → 목록에서 사라짐(`lead-settle-eol-fix/settlement.md`). 화면 표시는 20분 40초 작업이다.
- **미실행**: 이 작업은 서버 시험·Unity·봇을 돌리지 않았다. AGENTS의 「서버와 Unity 사용처 양쪽 검증」은 독립 검증이 맡는다.

### 패킷·창 독립 검증·V-1 수정·재검증 — 통과, 2026-10-11

| 항목 | 독립 검증 | V-1 수정 | 재검증 |
|---|---|---|---|
| Task / Dispatch | `task_eaa1138f1ee3` / `ctx_9d95ce82d14f`, `[Content 검증자]` | `task_abd16373404a` / `ctx_9c05d9c7936b`, `[Content Sol]` | `task_d781f9564f60` / `ctx_47fddb9f9d06`, `[Content 검증자]` |
| 모델 | 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown) | 신규 `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max」, backend unknown). 배정 신호 해당 없음(명세가 고정된 1파일 수정), 라우팅 시범 Sol 2회차 | 신규 `claude-opus-5-5`(같은 화면, backend unknown). 시범 「해당 여부가 애매한 작업」(PR 전체가 프로토콜) |
| 계약 | `opus-packets-verify-contract.md` SHA256 `45467e2b…`, head `86c2098f` | `sol-v1-fix-contract.md` SHA256 `e3a3315c…`, head `648809f0` | `opus-v1-reverify-contract.md` SHA256 `44aa3462…`, head `02a7239b` |
| 결과 | `opus-packets-verify/verdict.md`(`d23ff763…`), worker_done `msg_402f7e9535e5`. **차단(V-1)**, 제품 동작 결함 0 | `sol-v1-fix/report.md`(`a9d68e83…`), worker_done `msg_cbf49afc2c72` | `opus-v1-reverify/verdict.md`(`3254edb5…`), worker_done `msg_a13c6c9cd659`. **통과, V-1 해소, 새 결함 0** |
| 커밋 | 검증자 시험 6파일 `648809f0` | 창 1파일 +5/−7 `02a7239b` | 검증자 시험 2파일 `a35b7611` |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| 독립 검증 WSL 서버 전체 | 985 / 980 / 0 / 5 | TRX `56da5b90`. skip 5는 기존 `Skip` 속성 시험이다 |
| 독립 검증 EditMode 전체 | 418 / 418 / 0 / 0 | `c18286b3` |
| 독립 검증 PlayMode 전체(서버 lane 7777) | 39 / 38 / 1 / 0 → 42 / 42 / 0 / 0 | `62a9e113` → `e46ff3fd` |
| 독립 검증 봇 회귀 | PASS 6 / FAIL 0 | `runs/bots/run-1-output.txt` |
| V-1 수정 PlayMode 던전 네 클래스(수정 전·뒤) | 12 / 12 / 0 / 0, 12 / 12 / 0 / 0 | `ac8b1999`, `27265e8c` |
| V-1 수정 EditMode 전체 | 418 / 418 / 0 / 0 | `9cf39edf` |
| 재검증 EditMode 전체 | 418 / 418 / 0 / 0 | `b220a2b5` |
| 재검증 PlayMode 전체(서버 lane 7777) | 42 / 42 / 0 / 0 → 43 / 43 / 0 / 0 | `ad320d26` → `c4abdc12` |

- **V-1(규칙 위반)**: 보상 창의 `IsGameplayScene`이 게임플레이 씬 이름 집합의 네 번째 사본이었다. 나머지는 `Combat/CombatBootstrap.cs:36` `CombatScenes`, `UI/InventoryPanel.cs:158-159`, `Bootstrap/LocalPlayerSpawner.cs:36`(GameplayTest 없음)이다. CODE_CONVENTION 「같은 이유로 함께 바뀌는 로직이 세 곳 이상이면 추출한다」 위반이고, 새 게임플레이 씬을 `CombatScenes`에만 더하면 창이 그 씬에서 숨는 위험이었다. 귀속은 리드 계약과 기존 코드다. 구현 계약이 `CombatBootstrap`의 다른 구간과 `InventoryPanel` 수정을 막았고, 이름 판정을 가진 `InventoryPanel.BuildRuntime`을 따를 예시로 지정했다.
- **V-1 처리(리드 판단 `lead-context-14.md`)**: 이 PR이 더한 범위 안 차단 결함이라 루프에서 고쳤다. 보상 창 파일은 goal 「건드릴 곳」 안이라 메인 결정은 필요 없었다. 창은 이제 이름을 알지 않고 「자기 씬이 활성 씬인가」(`IsInActiveScene`)만 본다. 표시와 「확인」 gate가 이 판정 하나를 쓴다. 창은 `CombatBootstrap` installer만 만들고, 씬 전환은 Single, UI 씬은 Additive이며 제품 코드에 `SetActiveScene`이 없다. 그래서 제품 경로에서는 수정 전 판정과 같은 결과다. 검증자가 낸 공용 helper 추출안은 `InventoryPanel`·`CombatBootstrap` 다른 구간을 바꿔 이 PR 경계 밖이라 「다음 계획 후보」로 옮겼다. 보상 창 파일의 수정은 구현 다음 이번이 두 번째다.
- **V-2(선행 시험 fixture, (b))**: 구현 단계 실패 F-1은 「확인」 포인터 시험이 UI 씬의 EventSystem이 아직 없을 때 raycast해서 났다. 검증자가 `ScreenUiProbe.WaitForEventSystem` 대기 한 줄만 더했고 단정은 바꾸지 않았다. 같은 명령 8/7/1 → 8/8/0이다.
- **검증자가 더한 시험**: 맵 이동 중 「확인」은 40을 보내지 않고 다음 맵 창이 같은 결과를 보인다, Ending에는 창이 없다, 보스방에는 입구 글자가 없다(독립 검증). 창이 남은 채 다른 씬이 활성이면 창이 닫히고 누름이 40을 보내지 않는다(재검증, V-1 판정의 거짓 분기).
- **실제 진입**: 서버 lane 위 PlayMode 전체에서 `InventoryServerIntegrationTests`·`MapEntryServerIntegrationTests`가 이 checkout의 WSL GameServer(v18)에 실제 handshake로 붙었다. 봇 여섯이 v18 서버에 접속했다. 생성기·DLL 재현에서 커밋 소스로 다시 빌드한 Shared(`a9223eba`)·ClientNet(`dc708e2f`) DLL이 커밋 bytes와 같았다.
- **리드 R-2**: 세 판정·보고의 결과 XML·TRX를 다시 세어 수치·hash·실패 이름이 맞았다. 시험 diff와 새 시험의 기대값(리터럴·독립 encoder·control)을 읽어 측정값 위장과 제품 계산 복제가 없음을 확인했다(`lead-r2-packets-verify/r2.md`, `lead-r2-v1-fix/r2.md`, `lead-r2-v1-reverify/r2.md`).
- **생존 신호(helper)**: 독립 검증 최대 258초, V-1 수정 최대 310초(보고 작성 구간 한 번), 재검증 최대 314초(서버 lane Start를 전경으로 돌린 구간 한 번, 판정에 자진 기록). 질문은 없었다.
- **정산**: 세 pane 모두 tui-idle → worker-release(retained) → close(ptyKilled) → 목록에서 사라짐(`lead-settle-packets-verify/`, `lead-settle-v1-fix/`, `lead-settle-v1-reverify/`).
- **라우팅 시범 기록**: Astra 1회차(구현)의 독립 결함은 V-1 하나이고 귀속은 리드 계약·기존 코드다. Sol 1회차(줄 끝 수정)의 독립 결함은 0이다. Sol 2회차(V-1 수정)의 독립 결함은 0이다. 절차 실패는 300초 초과 heartbeat가 구현 셋, 줄 끝 수정 하나, V-1 수정 하나다.
- **설정 파일 되돌림**: 재검증이 이 PR의 마지막 batch였다. 그 뒤 메인 판단 `msg_c9fac8091065`대로 `ProjectSettings.asset`을 `5a03375a…` → `4a8db0bd…`로 되돌렸다. 되돌림 사본과의 차이는 Unity가 지운 SENTIS define 한 줄뿐이었다. TimeManager는 HEAD와 같다(`lead-settings-restore-1011/`).
- **미실행**: 실화면(클리어 PR), 서버 시험·봇·DLL 재현의 재검증 재실행(판정 diff가 서버·공유 코드를 건드리지 않음), DB.
- **병합**: CI 4/4 통과(가장 긴 dotnet-tests 24m11s, 10:49:36Z 완료)·MERGEABLE CLEAN인 head `ac98fe69`로 메인에 승인 묶음을 보냈다(`msg_c78e1ddae8f8`). 그 사이 PR229(Rules 동결 전 규칙 문서)가 먼저 병합됐다. 겹치는 파일은 CURRENT 하나였고 `git merge-tree` 충돌이 0이라 다시 합치지 않았다(`merge-tree-704baa5e.txt`). 사용자가 메인 pane에 「병합 승인: PR230 head ac98fe6927e8ca6a41b5a66962bbb18e73feab2f」를 제출했고, 메인이 병합했다(2026-10-11T10:57:10Z, merge commit `999b1b98ba38934568f852adc2d22c9ee333d864`, 메인 `msg_8659906ef748`). 원격 branch는 병합 뒤 지워졌다(11:09Z `git ls-remote` 0건).
- **번호 확정 알림**: 병합 직후 World에 39·40·v18이 main에 들어갔고 World 로그인은 41·42를 쓴다고 알렸다(`msg_929b97d8c951`). 버전 18을 리터럴로 단정하는 서버·클라이언트 시험 위치도 함께 보냈다.

### 클리어 선행 시험 — RED 준비 성공, 2026-10-11

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_534677b6c002` / `ctx_21e5054b19a8`, `[Content 검증자]`, 11:30:57Z~11:57:09Z |
| 모델 | 지정 `claude-opus-5-5`. 최초 실행 명령 `claude --model claude-opus-5-5`(TEMP `.backups/tmp/clear-tdd`), 화면 표시 「Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `clear-tdd-contract.md` v1 SHA256 `57b95bf6…0704`(CODE_CONVENTION 7개 절 원문 포함), 요약 spec `clear-tdd-spec.md`(`7dd810c8…acef`), 고정 입력 `clear-tdd-inputs.txt`(입력 38개·보호 파일 3개) |
| 보고 | `clear-tdd/report.md`, worker_done `msg_4ea27ad985d2`(succeeded) |
| 커밋 | 시험 8파일 `2e2c5c92`(+1106/−21) |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| 고칠 기존 세 클래스, 고치기 전 | 61 / 61 / 0 / 0 | `runs/server-before/server-before.trx`(`3418d808`) |
| 같은 세 클래스 + 새 두 클래스, 고친 뒤 | 80 / 58 / 22 / 0 | `runs/server-after/server-after.trx`(`881f7b66`) |
| 봇 빌드(`sync-wsl.sh build`) | 경고 4·오류 0. 경고는 기준과 같은 기존 네 건이고 봇·새 시험 파일은 0 | `runs/server-after/build.txt`(`1e6fab94`) |

- **결과**: 실패 22개는 새 시험 19개 전부와 고친 기존 시험 3개다. 모두 아직 없는 동작 때문이다. 클리어 결과 39번 미발신, 40번 처리기 미등록, 확인 뒤 마을행 `S_MapTransition` 없음, 지금 `KillRewardPolicy`의 보스 처치 보상, Boss 종류 예외 없음이다. 컴파일 오류, fixture·환경 실패, 고친 시험 밖의 기존 시험 실패는 0이다.
- **지금 통과하는 둘(양성 대조)**: CurrencyCap 묶음 거부와 나중 아이템 넘침 거부다. 지금의 `InventoryTransitions.Grant`가 이미 묶음 전체를 거부한다. 클리어 지급이 같은 `Grant`를 쓰라는 것이 C2라 구현 뒤에도 통과해야 한다. 새 시험의 「39가 오지 않는다」「40이 버려진다」 단정은 모두 같은 시험 안의 짝(수혜자의 39 하나, 이어진 올바른 40의 이동, 처리기 등록)과 묶여 지금 실패한다. 대상 0 통과가 아니다.
- **고친 기존 시험**: 다섯 단정을 (a) 옛 세부로 분류했다. 보스 보상 묶음과 그 거부 시험, 보스 처치자 50+2, `BossKill_GrantsPouchMaterialAndFiftyCurrencyInItemIdOrder`, P4의 「40 미등록」이다. 기준 61개는 모두 통과였고, 기대값을 일괄 재생성하거나 완화한 것은 없다. 좁힌 것 하나(CurrencyCap)와 뒤집은 것 하나(P4 등록)가 있다.
- **P4 등록 단정 뒤집기(리드 수용)**: `DungeonClearPacketContractTests.cs`는 이제 `HandlerRegistry.TryGet`으로 40 처리기 등록을 단정한다(132·147행). 위 「던전 클리어 PR의 요구」의 서버 흐름 5가 등록 한 줄을 정했다. 그 아래 「바뀔 기존 시험」은 미등록 단정만 옛 세부로 분류했다. 등록 단정을 지우면 「마을 비수혜자의 40은 버린다」가 미등록 서버에서도 통과해 공허해진다.
- **시험에 없는 요구(독립 검증으로 넘김)**: wire의 CurrencyCap bytes는 코드 대조로 본다. Accepted가 아닌 이동 거부 뒤 다시 누르기는 결정적으로 재현할 수 없어 코드 검토로 본다. `RevisionExhausted`는 시험이 없어 코드 검토로 본다.
- **관찰면이 구현을 묶는 지점(리드 채택)**: 보고서 7절의 일곱 가지를 구현 계약 조건으로 넣는다. 정착은 `SettleTicks` 3 안이다. 수혜자의 경제 패킷 순서는 [39, 36]이다. 40 처리기는 `HandlerRegistry`에 등록한다. 도착 이동은 `S_MapTransition`(Town, `TownSpawnX`, 0)이다. 보스 정책 예외는 미지 종류와 같은 예외 타입이다. 버린 40 뒤에는 새 패킷 종류가 없다. 같은 틱의 40 두 번은 이동 하나다.
- **구현 계약에 넣을 것**: `InventoryAuthorityBoundaryTests.cs` 12행 머리 주석 「boss 50 + material 1 + pouch 1」을 함께 고친다. WSL 명령은 `wsl -d Ubuntu --exec bash 99_Tools/sync-wsl.sh …` 형태로 적는다. 결과 경로를 `/mnt/c/…`로 넘기는 명령에는 `MSYS_NO_PATHCONV=1`을 붙인다.
- **리드 R-2**(`lead-r2-clear-tdd/r2.md`): 시험 8파일·TRX 2개·보호 파일 3개의 hash가 보고와 같다. TRX를 직접 다시 세어 61 → 80(58/22)과 클래스별 수가 같았다. 실패 메시지 22개를 TRX에서 읽어 모두 미구현 사유임을 봤다. 고친 기존 파일의 diff가 계약 구간 안이고 `JoinAndEnter`(188~197행)는 그대로다. 새 시험 세 파일 전문과 봇 시나리오 표본을 읽었다. 기대값은 goal 리터럴·공유 보상표·`InventoryLimits.MaxStack`·조회 전후 비교로만 정하고, reflection·제품 계산 복제는 없다.
- **부수 변경**: 기준 TRX가 Git Bash 경로 변환 때문에 WSL 작업 사본 아래에 생겼다. 작업자가 근거 폴더로 옮겼고 빈 디렉터리 사슬이 남았다(저장소 밖). 작업자가 경로 없는 `git diff --stat`을 한 번 실행해 보호 파일의 변경 줄 수가 출력에 섞였다. 내용 읽기·쓰기는 없었고 보호 hash는 그대로다. 둘 다 작업자가 스스로 보고했다.
- **생존 신호(helper, `lead-settle-clear-tdd/liveness.txt`)**: 신호 22개, 가장 긴 간격 157초, 300초 초과 0, 질문 0이다.
- **정산**: 대기 확인(tui-idle, 「Baked for 26m 29s」) → worker-release(retained/external_terminal) → close(ptyKilled true) → 11:57:46Z 목록에서 사라짐(`lead-settle-clear-tdd/settlement.md`).

### 클리어 서버 구현 — 결과, 2026-10-11

| 항목 | 값 |
|---|---|
| 구현 Task / Dispatch | `task_2bfe92fb8745` / `ctx_068349d19378`, `[Content 구현 Astra]`, 12:16:31Z~12:49:11Z |
| 모델 | 지정 `gpt-6-astra` xhigh. 최초 실행 명령 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 화면 표시 「GPT-6-Astra xhigh」(Codex v0.162.1). backend 실제 모델 unknown. 배정 신호 2·3 |
| 계약 | `astra-clear-impl-contract.md` v1 SHA256 `c86500d9…`(CODE_CONVENTION 7개 절 원문 포함), 요약 spec `astra-clear-impl-spec.md`, 고정 입력 `astra-clear-impl-inputs.txt`(입력 42개·보호 파일 3개) |
| 보고 | `astra-clear-impl/report.md`, worker_done `msg_84d3ce223f45`(succeeded, 14파일) |
| 커밋 | `654b2728`(14파일 +208/−16) |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| 필터 5클래스, 쓰기 전 | 80 / 58 / 22 / 0 | `runs/filter-before/`(`bbbea2ad`) |
| 서버 전체, 쓰기 전 | 1004 / 977 / 22 / 5 | `runs/full-before/`(`b2cf6203`). 실패 이름이 필터의 22개와 같다 |
| 필터, 첫 구현 | 80 / 27 / 53 / 0 | `runs/filter-after/`(`83f40670`). 아래 SELF-1 |
| 필터, 수정 뒤 | 80 / 80 / 0 / 0 | `runs/filter-after-fix1/`(`5011602a`) |
| 서버 전체, 수정 뒤 | 1004 / 999 / 0 / 5 | `runs/full-after/`(`240c6358`). skip 5는 기존 `Skip` 속성 시험이다 |
| WSL 빌드 세 번 | 경고 4·오류 0 | `build-before.txt`·`build-after.txt`·`build-after-fix1.txt`. 경고는 기준과 같은 기존 네 건이다 |
| 서식 검사 | exit 0, 제품 294파일 수정 0, 도구 시험 244 / 244 | `format-after*`, `format-after-evidence/` |

- **구현**: 보스 분기(`GameMap.HandleEnemyDeath`)가 `Dungeon/DungeonClearState.Capture`로 그 순간의 수혜자를 잡는다. World의 Inventory 단계가 `InventoryRegistry.ApplyDungeonClear`에서 기존 `InventoryTransitions.Grant`로 지급하고 39와 성공 snapshot을 직접 보낸다. 보스는 `GameWorld.MakeMap`에서 `EnqueueKill`을 빼고 `KillRewardPolicy`에서 Boss 줄을 지웠다. 40은 `Handlers/Dungeon/DungeonClearConfirmHandler` → `GameSession.SubmitDungeonClearConfirm` → `Dungeon/DungeonClearConfirmation.Execute` → `MapMigration.MoveToPublicMap(…, MapId.Town)`이다. 요구별 위치와 시험 이름은 보고서 「요구별 구현과 시험」 표에 있다.
- **설계 결정(구현자)**: 던전 파일은 상태·확인·wire 조립 셋으로 나눴다. 상태 객체는 첫 클리어 때 만든다. 같은 복사본이 빈 방 재출현 뒤 다시 클리어되면 새 수혜자 집합으로 교체한다. 콜백은 읽기 전용 `(EntityId, Session)` 목록을 넘긴다. 결과는 `SendToEntity` 대신 캡처한 session에 직접 보내 이동 틈에서도 빠지지 않게 했다.
- **SELF-1(구현 중 자체 수정)**: 첫 구현의 필터 실패 53개가 모두 `Delegate to an instance method cannot have null 'this'`였다. 공용 맵이 `InventoryRegistry`보다 먼저 만들어져 메서드 그룹의 대상이 null이었다. `MakeMap`의 콜백만 지연 lambda로 바꾼 한 번의 수정으로 80/80이 됐다. 실패 원시와 이름별 분류(`filter-after.classification.json`)를 보존했다. 독립 검증의 확정 실패 집계가 아니다.
- **시험 대신 코드 대조로 남긴 요구 셋**: wire의 CurrencyCap bytes, Accepted가 아닌 이동 거부 뒤 다시 확인, `RevisionExhausted`다. 구현자는 코드 대조만 했다고 표시했고, 보고서 「독립 코드 대조로 남긴 요구 셋」이 읽는 순서를 준다. 독립 검증이 판정한다.
- **리드 R-2**(`lead-r2-clear-impl/r2.md`): 변경 14파일과 보호 세 파일의 hash가 보고와 같다. TRX 다섯 개를 node로 다시 세어 표의 수와 같았다. 빌드 원시 세 개의 경고·오류 수도 같다. 파일 시각으로 기준선 실행이 첫 제품 쓰기(12:22:56Z)보다 앞서고, 최종 빌드·시험·서식이 마지막 쓰기(12:26:35Z)보다 뒤임을 봤다. diff 전문(269줄)과 새 네 파일을 읽어 계약 소유 구간 안임을 확인했다. 머리 주석·부분 한정 이름 같은 관찰 다섯은 검증자에게 넘긴다.
- **생존 신호(helper, `lead-settle-clear-impl/liveness.txt`)**: 신호 12개, 기동 → 첫 신호 약 46초, 가장 긴 간격 222초, 300초 초과 0, 질문 0이다.
- **정산**: 대기 확인(tui-idle, 「Worked for 32m 45s」) → worker-release(retained/external_terminal) → close(ptyKilled true) → 12:49:41Z 목록에서 사라짐(`lead-settle-clear-impl/settlement.md`).
- **미실행**: 7777 서버, C8 봇, Unity, DB, PacketGenerator, 원본 제품 Windows 빌드는 돌리지 않았다. 승인된 서식 검사가 근거 폴더의 격리 snapshot에서 Shared·ClientNet을 빌드했지만 원본 Plugins DLL은 바뀌지 않았다. 구현자는 첫 Git 읽기 세 명령에 `GIT_OPTIONAL_LOCKS=0`을 빠뜨렸다고 스스로 보고했다(읽기 전용, 쓰기 Git 없음).

### 클리어 독립 검증 — 판정 보류(봇 미실행), 2026-10-11

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_af8b7df35b78` / `ctx_8aa7d29336af`, `[Content 검증자]`, 첫 신호 13:08:30Z, worker_done 14:29:47Z |
| 모델 | 신규 `claude-opus-5-5` xhigh. 화면 표시 「Claude Code v2.1.296 · Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `opus-clear-verify-contract.md` SHA256 `6438e486…`, 요약 spec `opus-clear-verify-spec.md`(`b693c644…`), 고정 입력 `opus-clear-verify-inputs.txt`(`f19dec0f…`) |
| 결과 | `opus-clear-verify/verdict.md`(`609fb7ed…`), worker_done `msg_6facf25b1815`. **판정 보류(봇 미실행), 차단 결함 없음.** 결함 V-1·V-2는 낮음, 비차단이다 |
| 새 시험 | `Dungeon/DungeonClearRefusalTests.cs`(82줄), `Dungeon/DungeonClearRepeatClearTests.cs`(92줄), 9결과. 이 기록 커밋에 넣었다 |

| 실행 | total / pass / fail / skip | 원시(SHA256 앞 8자) |
|---|---|---|
| 서버 전체, 새 시험 전 | 1004 / 999 / 0 / 5 | `runs/full-before/`(`81681813`) |
| 서버 전체, 새 시험 뒤 | 1013 / 1008 / 0 / 5 | `runs/full-after/`(`a8f942f7`). 늘어난 9건이 새 시험이다. skip 5는 기존 `Skip` 속성 시험이다 |
| WSL 빌드 | 경고 4·오류 0 | 판정 근거는 `runs/build-before2/`·`runs/build-after/`다. `build-before`는 근거가 아니다(운영 기록) |
| 서식 검사 | exit 0, 제품 296파일 수정 0, 도구 시험 244 / 244 | `runs/format-after-2/`. 첫 실행 `format-after`는 무효다(운영 기록) |
| C8 봇과 기존 봇 회귀 | 미실행 | `runs/bot/gate-summary.json`. 시작 조건 확인 18회, 충족 0회 |

- **V-1(낮음, 명세)**: C4의 옛 셋째 문장 「빈 복사본은 그 틱 끝에 정리된다」가 입장 대기 중인 빈 보스방에서 성립하지 않았다. `InstanceMapRegistry.RemoveIdle`이 들어오는 사람이 있는 복사본을 남긴다. 그러면 `GameMap.MaybeRespawnBoss`가 보스를 다시 내고 `_stageCleared`를 false로 되돌린다. 그 처치는 새 클리어로 지급된다. 새 시험 `DungeonClearRepeatClearTests`가 실제 월드 틱 순서로 재현했다. 구현은 C4 둘째 문장과 `server.md`대로다.
- **V-1 리드 결정 「처치마다 한 번」**: 사용자 완료조건 5·6이 처치 단위다. 판정 O-1대로 해금이 처치 뒤에도 남아, 새 복사본 재도전과 경제 영향이 같다. 그래서 제품·`server.md`·새 시험은 그대로 두고 C4만 고쳤다(`lead-r2-clear-verify/r2.md`). 「복사본마다 한 번」을 골랐다면 구현·`server.md`·새 시험의 마지막 단정이 바뀌었다.
- **V-2(낮음)**: 구현자가 고친 `InventoryAuthorityBoundaryTests.cs:10-13` 머리 주석이 클리어 묶음의 출처를 아이템 goal 승인표 리터럴이라고 적는다. 실제 출처는 공유 `DungeonClearRewards`(10-08 결정 2 A)다. 다음에 그 파일을 고칠 때 바로잡는다(「다음 계획 후보」).
- **코드 대조로 판정한 요구 셋**: wire CurrencyCap, Accepted가 아닌 이동 거부 뒤 다시 확인, `RevisionExhausted` 모두 지적이 없다. 이동 거부는 새 시험이 `AlreadyMigrating`으로 실행했다. 나머지 둘의 실행은 상태 심기 없이는 불가라 하지 않았다.
- **선행 시험 품질과 고친 기존 다섯 단정**: (a) 옛 세부 분류가 맞다. 기대값 일괄 재생성·완화와 제품 계산 복제는 없다.
- **설계 관찰**: O-1(해금 수명, World 소유)과 O-2~O-5는 「다음 계획 후보」로 옮겼다. O-6은 선행 시험의 보스 처치가 직접 호출이라는 관찰이고, 실제 전투 경로는 C8 봇이 본다.
- **판정 보류의 이유**: 강 등급은 실제 진입 1회 전에는 통과하지 않는다. 이 PR의 실제 진입은 봇 `DungeonClearSmoke`다. 리드가 조건부로 허락했지만(`msg_60b01e559689`), 사용자 게임 실행과 메모리 4GB 미달로 14:25Z까지 시작 조건이 한 번도 맞지 않았다. 봇이 통과하면 나머지 판정은 그대로 쓴다. 실패하면 새로 분류한다.
- **큰 실행 보류**: Core SQL 컨테이너 진단 동안 리드가 큰 실행을 막았다(`msg_ab3bfab2d9d8` 13:12:59Z → 해제 `msg_1d6f2b271287` 13:32:44Z, Core 끝 알림 `msg_a18ed271d458`).
- **리드 R-2**(`lead-r2-clear-verify/r2.md`): HEAD·status·보호 세 파일·새 시험 hash가 판정과 같다. TRX 두 개를 node로 다시 세어 표의 수와 같았다. 서식 exit, 봇 관문 요약, 7777 마감 원시도 판정과 같다.
- **생존 신호(helper, `lead-settle-clear-verify/liveness.txt`)**: 신호 34개, 가장 긴 간격 273초, 300초 초과 0이다. 질문 셋(메모리 `msg_c5525803aca8`, 서식 회수 `msg_a3ec91c73364`, 봇 `msg_da2c43981385`)에 모두 답했다.
- **정산**: 대기 확인(tui-idle) → worker-release(retained/external_terminal) → close(ptyKilled true) → 14:30:25Z 목록에서 사라짐(`lead-settle-clear-verify/settlement.md`).
- **미실행**: C8 봇·기존 봇 회귀, C9 실화면·Unity·MCP, DB·CI·PacketGenerator·Windows `dotnet build`.

### 클리어 봇 검증 — 통과, 2026-10-13

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_2f58855b1557` / `ctx_c59f731adc3d`, `[Content 검증자]`, 첫 신호 04:23:22Z, worker_done 04:59:54Z |
| 모델 | 신규 `claude-opus-5-5` xhigh. 화면 표시 「Claude Code v2.1.296 · Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `opus-bot-verify-spec.md` SHA256 `9980585c…`. 10-11 계약에서 봇 항목만 남긴 판이다. 고정 입력 `opus-bot-verify-inputs.txt`, 사전 확인 `opus-bot-verify-preflight.txt`. 검증 대상 HEAD `0d5bdbdb` |
| 결과 | `opus-bot-verify/verdict.md`(`17a5420e…`), worker_done `msg_4ecd858382df`. **봇 검증 통과, 새 결함 없음.** 10-11 판정의 보류가 닫혀 이 PR 독립 검증의 최종 상태는 통과다 |

| 실행 | 결과 | 원시(SHA256 앞 8자) |
|---|---|---|
| C8 봇과 기존 봇 회귀(계약의 봇 명령 한 번) | 04:52:44Z~04:54:22Z, exit 0, `PASS=8 FAIL=0`. C8 줄은 `DungeonClearSmoke: success=True parties=2 cleared=2 rewarded=4 returned=4`다 | `opus-bot-verify/runs/bot-1/output.txt`(`540810a8`) 1665·2649행 |
| 시작 관문 | 여유 물리 9,994,016KiB·가상 13,496,540KiB, 7777 listener 없음. 리드 허락 `msg_0591890f9d92`(04:52:14Z) 뒤 시작했다 | `runs/bot-1/gate.txt`·`memory.txt` |
| 서버 로그 | 8개 모두 오류 줄 0이다. C8 로그에서 보스방 복사본 둘 생성, 확인 넷, 마을 도착 넷이 보인다 | `runs/log-summary.json`(`1c7f7a9c`), `logs/` |

- **봇 관찰(비차단)**: O-B1은 두 파티가 동시에 처치해 「상대 복사본 결과 0」이 「봇마다 39 하나」로만 관찰된다는 한계다. 그 오배달은 서버 시험의 두 복사본 시험이 막는다. O-B2는 성공 줄의 단계 값이 리터럴이라 로그에 실제 관찰값이 없다는 가독성 지적이다. 둘 다 「다음 계획 후보」로 옮겼다.
- **이전 판정의 V-1·V-2**: 그대로다. V-1은 리드 결정 「처치마다 한 번」으로 C4에 반영했고, V-2는 다음 계획 후보다.
- **리드 R-2**(`lead-r2-bot-verify/r2.md`): HEAD·보호 세 파일·`output.txt` 1665·2649행·성공 줄 8·사유 줄 0·C8 서버 로그 줄·서버 로그 오류 0·로그 사본 16개 hash·성공 경로 코드(`DungeonClearSmoke.cs:113` 한 곳, `sync-wsl.sh:154-155`)가 판정과 같다. 판정서 147행의 heartbeat 간격과 World 실행 서술만 실제와 다르다(운영 기록).
- **7777 조율**: World 서식 검사의 WSL dotnet이 04:51:40Z에 0이 된 뒤 허락했다(`bot-verify-prep/wsl-idle-poll.txt`). 해제(04:54:22Z)는 World·Core·Rules·아트 리드에 알렸다(`outbox/122-bot-release.txt`).
- **생존 신호(helper, `lead-settle-bot-verify/liveness.txt`)**: 신호 14개, 가장 긴 간격 609초, 300초 초과 1건이다(운영 기록).
- **정산**: 대기 확인(tui-idle) → worker-release(retained/external_terminal) → close(ptyKilled true) → 05:01:45Z 목록에서 사라짐(`lead-settle-bot-verify/settlement.md`).
- **미실행**: C9 실화면·Unity·MCP, 서버 전체 시험(10-11 판정 그대로), DB·CI·PacketGenerator·Windows `dotnet build`.

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
- 패킷·창 선행 시험 작성자의 새 PlayMode 시험이 Unity 6.6에서 막힌 `GetInstanceID()`를 써서 batch가 컴파일 오류로 끝났다. 그 batch가 `03_Client/Temp/UnityLockfile`(0바이트)을 남겨 다음 실행의 harness gate가 닫혔다. 작업자는 파일을 건드리지 않고 공식 질문으로 물었다(`msg_57ea949f61c6`). 리드가 Unity 0과 크기·mtime을 확인하고, 지우기 직전 재확인을 조건으로 그 파일 하나만 지우게 답했다(`msg_017c756ba883`, 24초). 작업자의 자체 수리이며 제품 실패·RED로 세지 않는다.
- 위 「R10 재확인 검증자의 `2>/dev/null`」 항목에서 다음 계약부터 좁히기로 한 리다이렉트 문장을 리드가 패킷 선행 시험 계약 v2에 반만 반영했다. 좁힌 문장 「판정 근거 출력과 orca 출력은 버리지 않는다」 앞에 옛 문장 「리다이렉트 파일 대상은 checkout·`.backups` 안으로만」이 남았다. 그래서 작업자가 탐색 grep의 `2>/dev/null`을 위반으로 스스로 보고했다. 판정 근거 출력이 아니어서 결과에는 영향이 없다. 구현자 계약부터 옛 문장을 지운다.
- 선행 시험 작업자의 worker_done 첫 시도는 `--files-modified`를 공백으로 나눠 CLI가 전송 전에 거부했다. 쉼표로 고쳐 한 번만 보냈다. 다음 계약에 쉼표 형식을 적는다.
- `orca orchestration inbox`에는 `--run`이 없다. 생존 신호 helper의 원시는 `inbox --limit 500 --full --json`으로 저장했다(`lead-r2-packets-tdd/inbox-raw.json`).
- 커밋된 Plugins `Shared.dll`(`595e970a`, 커밋 `32626771`)이 HEAD 소스로 다시 빌드한 DLL(`8411d6d2`)과 달랐다. 패킷 구현자의 기준선 빌드가 찾았다(question `msg_47e32c0637b4`, 리드 확인 `lead-r2-packets-impl/baseline-drift-check.txt`, reply `msg_8c302fd00fca`). 기능 차이는 없다. `Shared.csproj`가 `EmbedAllSources`·embedded PDB라 소스 bytes가 DLL에 들어간다. 줄 끝 수정 작업자의 진단(D2)으로 그 DLL은 경제 커밋 소스와 생성기의 CRLF 섞인 출력으로 빌드된 것이 확인됐다. 구현자 기준선과 커밋 DLL 사이의 차이는 커밋 `03c4ce6d`(10-05, 아이템 goal)가 `ProtocolVersion.cs` 주석 두 줄을 바꾸고 DLL을 다시 빌드하지 않은 것이다.
- 패킷 생성기가 Windows에서 `Environment.NewLine`으로 `GenPackets.cs` 일부 줄을 CRLF로 쓴다(이번 작업본 3071줄 중 304줄). `.gitattributes`는 `*.cs text eol=lf`라 커밋은 LF가 되고, 섞인 작업본으로 빌드한 Plugins DLL은 커밋 소스로 다시 빌드한 DLL과 달라진다. 리드 R-2가 찾았고(`lead-r2-packets-impl/code-sample-review.md`), 이번 PR은 줄 끝 수정 작업자가 작업본을 LF로 맞추고 DLL을 다시 만들었다. 교정 층 후보는 생성기가 LF로 쓰게 고치는 것(구조 층)과 Shared 소스·Plugins DLL 일치 검사(검사 층)다. 경제 PR(커밋 DLL `595e970a`)에 이은 두 번째 발생이라 아래 「다음 계획 후보」에 올렸다. 생성기를 돌린 작업본에는 CRLF 섞인 파일이 남지만 `eol=lf` 정규화 때문에 `git status`에 보이지 않는다.
- Git Bash `grep -c $'\r$'`가 LF 줄까지 모두 CRLF로 세어 틀린 값을 냈다. 줄 끝은 node로 센다.
- 독립 검증의 차단 V-1은 리드 계약에 귀속됐다. 구현 계약이 따를 예시로 지정한 `InventoryPanel.BuildRuntime`의 파일에 같은 씬 이름 판정이 이미 있었고, 계약이 그 파일과 `CombatBootstrap`의 다른 구간 수정을 막았다. 구현자는 예시를 따라 네 번째 사본을 만들었다. 첫 발생이라 이 goal에만 적는다. 다음 계약부터 리드는 따를 예시를 지정할 때 그 예시 안의 중복도 맥락 메모에서 함께 확인한다.
- V-1 수정 작업자의 첫 감사 파일 `sol-v1-fix/audit-final.json`이 204,499,098바이트가 됐다. PowerShell이 Git stderr 경고의 ErrorRecord를 JSON 객체로 직렬화했다. 근거 폴더(Git 밖)라 커밋 영향은 없고, 작업자가 보고에 스스로 적고 문자열로 정규화한 감사를 다시 남겼다.
- 재검증자가 서버 lane Start를 전경으로 돌려 도구가 300초 뒤 백그라운드로 넘겼다. `Run-ServerLane.ps1`의 Start는 Stop까지 launcher에 붙어 있다(`:164-165`). 그 구간에 heartbeat 간격이 314초가 됐다. 이전 독립 검증자는 처음부터 백그라운드로 돌렸다. 첫 발생이라 이 goal에만 적는다.
- Content가 Unity·7777을 쥐고 있는 동안 World 검증자의 봇 회귀(7777만, 09:30Z~09:36Z)를 V-1 수정 단계 사이에 끼웠다. World가 시작 전에 묻고 끝나자 해제를 알려 재검증과 겹치지 않았다(World `msg_784b84931692` → `msg_91b307b9bcdf`).
- 리드의 PR230 병합 승인 요청(`msg_c78e1ddae8f8`)이 재검증 뒤 들어온 커밋을 `ac98fe69` 하나로 적었다. 실제로는 `a35b7611`(검증자 시험)과 `ac98fe69`(goal) 둘이었다. 메인 R-2가 찾았고 승인에는 영향이 없었다(`msg_8659906ef748`). 첫 발생이라 이 goal에만 적는다. 다음 보고부터 리드는 base 이후 리드 커밋을 `git log`로 뽑아 빠짐없이 적는다.
- 라우팅 핫픽스 결정 1로 메인이 Content 리드 pane을 `--autocompact 250000`과 `--resume`으로 다시 띄웠다(11:05Z). 대화는 이어졌고 handle은 `term_e90e3c30-…`으로 바뀌었다. 리드는 같은 Run에 `run-use`로 다시 묶었다(consumer_generation 3, `inbox/run-use-1011d.json`). 이 시각 앞뒤 우편함 기록의 리드 handle이 다른 이유다.
- 리드의 클리어 선행 시험 계약이 WSL 시험 명령을 `wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh test --filter "…"` 형태로 적었다. 이 형태에서는 필터 인자의 `|`를 셸이 파이프로 읽어 exit 127로 끝났다(`clear-tdd/runs/server-before/test-attempt1-shell-pipe.txt`). 작업자가 이전 선행 시험 근거의 `--exec` 형태로 바꿔 통과했다. 계약 귀속이며 첫 발생이라 이 goal에만 적는다. 다음 계약부터 `--exec` 형태를 적는다.
- 같은 작업자의 기준 시험에서 `--results-directory /mnt/c/…`가 Git Bash 경로 변환으로 `C:/Program Files/Git/mnt/c/…`가 되어 TRX가 WSL 작업 사본 아래에 생겼다(`clear-tdd/runs/server-before/trx-move.txt`). 작업자가 hash를 잰 뒤 옮기고, 작성 뒤 실행에는 `MSYS_NO_PATHCONV=1`을 붙였다. Git Bash 경로 변환은 `git show`·`orca terminal send`에서도 났지만 정본 규칙은 없다. 다음 계약에 넣고, 반복 규칙으로 올릴지는 종료 기록 때 본다.
- 다른 리드의 메시지가 기록과 다른 handle에서 왔다(Core `msg_66c29be758cb`, `term_24b8aa94…`, 이전 기록은 `term_a9595668…`). `orca orchestration run-show --id run_b36cc92a4cf4`의 `coordinator_handle`이 같은 값이라 Core 리드의 재기동(Run 갱신 11:08:26Z)으로 보고 처리했다(`inbox/core-run-show-1246.json`). 태그와 handle이 Run 기록과도 어긋나면 처리하지 않고 메인에 보고한다.
- 클리어 검증자의 서식 검사 첫 실행 중 13:44Z 무렵 메모리 부족으로 Claude Code가 검증자의 백그라운드 셸을 회수했다. 하위 프로세스는 계속 돌아 13:52:28Z exit 1로 끝났다. 출력은 콘솔 제목 설정 중 「파이프의 다른 끝에 프로세스가 없습니다」였다. 리드가 끝까지 두되 요약과 증거가 끝까지 쓰이지 않으면 무효로 정했다(`msg_241b5a3ab284`, 원시 `outbox/110-format-orphan-check.txt`). 검증자가 새 폴더로 다시 돌려 통과했다. 작업자의 긴 실행이 회수된 첫 발생이라 이 goal에만 적는다.
- 클리어 검증자가 리드의 큰 실행 보류 dispatch를 읽기 전에 13초짜리 WSL 증분 빌드(`build-before`)를 시작했다. 직전 check와 실행 사이에 보류가 도착했다. 판정 근거로 쓰지 않았고 검증자가 스스로 보고했다. 첫 발생이라 이 goal에만 적는다. 다음 계약에 「큰 실행 직전에 우편함을 확인한다」를 넣는다.
- 클리어 검증자의 큰 출력이 홈 `~/.claude/projects/…/tool-results/`에 자동 사본으로 2건 남았다. 위 R10 항목에 이어 같은 goal의 세 번째 관측이다. 계약 안내 「큰 출력은 파일로 받는다」만으로는 첫 화면 출력 몇 건이 남는다. 두 번째 이상 발생이라 교정 층 판단을 종료 기록 때 메인에 올린다. 검증자는 hook이 그 세션에 대해 쓴 `.claude/state/session-guard/` 상태 파일(Git 제외)도 허용 밖 위치로 스스로 보고했다.
- 사용자 게임(약 9GB) 실행으로 여유 메모리가 4GB 아래에 머물러 C8 봇을 돌리지 못했다. 리드가 14:15Z~14:25Z 조건부 창을 열었지만 조건 확인 18회 모두 미충족이었다. World·Core에 예고와 취소를 보냈다(`outbox/112-*`, `outbox/115-*`). 첫 발생이라 이 goal에만 적는다.
- 마감 중 리드 Bash 도구가 명령 문자열이 대략 60~90자를 넘으면 실행 wrapper가 잘려 「unexpected EOF」로 실패했다. 짧은 `echo` 시험으로 길이 문제임을 확인했고, 스크립트 파일과 glob 짧은 경로로 우회했다. 앞서 따옴표 섞인 긴 한 줄이 실패한 것도 같은 원인으로 본다. 도구 사실이라 메모리 후보다.
- 10-13 리드 재개 직후 Bash가 `echo hi`까지 「unexpected EOF」로 실패했다. 메인 `msg_c6a79968a042`에 따르면 원인은 Codex 플러그인 SessionStart hook이 `~/.claude/session-env/<세션>/` 파일에 줄을 쌓은 것이다. 사용자가 정리 스크립트로 줄였고, 메인이 같은 대화로 새 pane을 열어 03:42Z에 복구됐다.
- 봇 검증자의 첫 공식 질문(`msg_61c24e4d4f7f`, 04:25:36Z, 7777 시작 허락) 명령이 약 10분 막혀 heartbeat가 609초 끊겼다(다음 신호 04:35:45Z). ORCA의 「heartbeat 5분 유지」에 어긋난다. 검증자는 그 뒤 heartbeat를 섞는 대기로 바꿔 약 4분 간격을 지켰다. 판정서 147행은 「약 4분 간격으로 보냈다」고 적어 처음 10분과 맞지 않는다. 판정 근거에는 쓰이지 않았다. 첫 발생이라 이 goal에만 적는다(`lead-r2-bot-verify/r2.md`, `lead-settle-bot-verify/settlement.md`).
- 리드의 7777 허락 회신(`msg_0591890f9d92`)에 미리 써 둔 「지금 World는 Windows 쪽 서식 검사만 돌린다(WSL·7777 안 씀)」가 남았다. 실제로는 그 서식 검사가 WSL dotnet을 썼고 04:51:40Z에 이미 끝나 있었다. 검증자가 이 문장을 판정서 147행에 옮겼다. 허락 판단과 판정 결과에는 영향이 없다. 리드 귀속이며 첫 발생이라 이 goal에만 적는다. 다음부터 미리 쓴 회신은 보내기 직전 원시와 다시 맞춘다.
- 봇 검증자도 홈 `~/.claude/projects/…/tool-results/`에 자동 사본 2건과 `.claude/state/session-guard/` 상태 파일 1건을 허용 밖 생성으로 스스로 보고했다. 판정 근거에는 쓰이지 않았다. tool-results는 이 goal의 네 번째 관측이라 위 항목대로 종료 기록 때 교정 층 판단에 함께 올린다.

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
- **패킷 생성기 줄 끝**: 생성기가 Windows에서 `Environment.NewLine`으로 CRLF를 섞어 써서, 생성기를 돌린 작업본으로 만든 Plugins `Shared.dll`이 커밋 소스로 다시 빌드한 DLL과 달라진다. 경제 PR과 이 PR에서 두 번 났다. 생성기가 LF로 쓰게 고치는 것(구조 층)이나 Shared 소스·Plugins DLL 일치 검사(검사 층)가 후보다. 출처는 「운영 기록(첫 발생)」의 줄 끝 항목과 `lead-r2-eol-fix/r2.md`다. 종료 기록 때 BACKLOG에 등록한다.
- **직업 추가 — 사제·궁수**(10-13 발표에서 추가 예정으로 표기). 출처는 메인 `msg_9f231a21d4cc`가 전달한 원문 「음 첫 대문 배경에 직업이 색깔만 바뀌고 겹치니까 좀 어색한데, 이참에 다른 직업군도 넣어볼까? 사제, 궁수도?」와 「대시보드 결정 응답: 1) 33 - 발표 표지에 사제·궁수를 넣을지 → A 추가 예정 직업으로 넣음」이다. 지금 게임 직업은 전사·마법사 둘이고 저장소에 두 직업 계획은 없다. 착수와 기획 내용은 goal로 올릴 때 사용자와 정한다. 종료 기록 때 BACKLOG에 등록한다.
- **게임플레이 씬 이름 사본 정리**: `UI/InventoryPanel.cs:158-159`(Town·HuntingGround·BossRoom·GameplayTest)와 `Bootstrap/LocalPlayerSpawner.cs:36`(GameplayTest 없음)이 `Combat/CombatBootstrap.cs:36` `CombatScenes`와 따로 있다. 패널도 보상 창처럼 자기 씬이 활성인지 보게 하거나 공용 판정으로 모은다. `LocalPlayerSpawner`는 spawn 대상이라 의미가 같은지 먼저 확인한다. 출처는 패킷·창 PR 독립 검증 V-1과 재검증 O-4다. 종료 기록 때 BACKLOG에 등록한다.
- 보상 창의 「installer만 창을 만든다」는 불변식이 주석으로만 있다. `BuildRuntime`은 public static이고 지금 호출처는 installer 하나다(재검증 O-1).
- 보상 창 `Update`가 매 프레임 `DungeonClearRewardState.EnsureInstance()`를 불러, 상태가 없으면 만든다. 상태 생성을 한 곳에 두고 창은 null을 허용하는 안이 있다(독립 검증 O-1).
- 클라이언트는 거부 결과(`CurrencyCap`·`InventoryFull`)에 실린 골드·아이템도 받아 둔다. 창은 사유만 보인다. 클리어 PR 설계 C3의 「거부 = 골드 0·빈 칸」과 맞출지 클리어 PR 착수 때 정한다(독립 검증 O-2).
- PlayMode 던전 창 시험 두 클래스가 helper(39 encoder·`HasNumber`·`Confirms`)를 사본으로 가진다. 세 번째 사용처가 생기면 `ScreenUiProbe` 옆 probe로 뽑는다(독립 검증 O-4, 재검증 O-2).
- 서버 lane Stop이 다섯 번 모두 INT 20초 안에 끝나지 않아 TERM으로 정리됐다. 서버의 INT 처리를 확인할 후보다(독립 검증·재검증 O-5).
- **HUD PlayMode 시험 간헐 시간 초과**: World 재검증의 서버 lane PlayMode 전체(12:03:59Z~12:05:15Z)에서 `HudGoldAndPanelToggleSceneTests.ClosedPanel_LetsAClickOnItsUseSpotAttack_AndAfterIThatClickUses_AcrossAMapMove`가 「Timed out: the open panel's Use」로 한 번 실패했다. 가짜 서버 peer의 `C_ItemUse`를 실시간 3초 기다리는 곳(`HudGoldAndPanelToggleSceneTests.cs:93`)이다. 같은 HEAD 재실행은 3/3·3/3 통과였고 원인은 확정하지 못했다. 대기 한도를 늘리거나 프레임 수 기반 대기로 바꾸는 안이 있다. 출처는 World `msg_75c883fc0f0b`와 World 근거 `.backups/verification/2026-10-11-multi-account-login/reverify/verdict.md` O-4다. 클리어 PR의 검증에서 같은 시간 초과가 나면 이 관찰로 분류하고 원시를 남긴다.
- **클리어 시험 머리 주석 출처**: `InventoryAuthorityBoundaryTests.cs:10-13`이 클리어 묶음을 아이템 goal 승인표 리터럴이라고 적는다. 실제로는 공유 `DungeonClearRewards`(10-08 결정 2 A)에서 읽는다. 다음에 그 파일을 고치는 작업에서 바로잡는다(클리어 독립 검증 V-2).
- 보스 처치 뒤에도 `_bossUnlocked`가 남아, 같은 사람이 바로 새 복사본에서 다시 클리어할 수 있다. 재도전 제한이 필요하면 World·Quest가 해금 수명을 정한다. 결정은 사용자·메인이다(클리어 독립 검증 O-1).
- 재출현이 `_stageCleared`만 되돌리고 `_dungeonClear`는 다음 클리어까지 남는다. 확인 job이 `IsStageCleared`를 먼저 봐서 지금은 안전하다. 재출현 때 상태도 비우거나 상태 객체가 세대를 확인하는 안이 있다. `MaybeRespawnBoss`가 World 소유 구간이라 함께 정한다(O-2).
- `DungeonClearState`가 수혜자 `GameSession` 참조를 복사본 정리나 다음 클리어까지 든다. 확인 집합만 남기고 수혜자 목록은 콜백 뒤 버리는 안이 있다(O-3).
- `HandlerRegistry`·`GameMap`·`InventoryRegistry`·`GameSession`이 `using` 대신 `Dungeon.` 부분 한정 이름을 쓴다. 다음에 그 파일을 고치는 PR에서 `using`으로 바꾼다(O-4).
- `DungeonClearState` 머리 주석에 호출처가 없다. 한 줄을 더한다(O-5).
- **봇 동시 처치의 관찰 한계**: `DungeonClearSmoke`의 두 파티가 같은 시각에 보스를 잡아, 상대 복사본 결과 0이 「봇마다 39 하나」로만 관찰된다(`DungeonClearSmoke.cs:89,106-112`). 한 파티가 먼저 잡고 조용한 창 동안 다른 파티의 39가 0인지 본 뒤 두 번째 파티가 잡는 안이 있다(봇 검증 O-B1).
- **봇 성공 줄의 리터럴 값**: `parties=2 cleared=2 rewarded=4 returned=4`가 검사 통과 뒤 대입하는 리터럴이라 로그에 실제 관찰값(보스 ID, revision 전후, 골드)이 없다(`Program.cs:56`, `DungeonClearSmoke.cs:63,91,100,104`). 실제 통과 수와 봇별 관찰값을 찍는 안이 있다(봇 검증 O-B2).
