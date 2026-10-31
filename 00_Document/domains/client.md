# 클라이언트

Unity는 입력·예측·화면 적용을 담당한다. 최종 게임 판정은 서버가 소유한다. 전송 라이브러리는 `04_ClientNet`, Unity 연결은 [Network](../../03_Client/Assets/Scripts/Network/), 예측은 [Prediction](../../03_Client/Assets/Scripts/Prediction/)에 있다.

## 수정 흐름

- 패킷 수신과 Unity 메인 스레드 적용을 분리한다. [MainThreadDispatcher](../../03_Client/Assets/Scripts/Network/MainThreadDispatcher.cs)와 해당 핸들러를 확인한다.
- 로컬 이동은 `LocalPlayerMovement`·`PlayerPredictor`·`InputHistory`, 원격 표현은 서버 스냅샷·보간 경로를 확인한다.
- 씬 전환과 연결 종료를 혼동하지 않는다. `NetworkService`, `SceneRouter`, bootstrap의 객체 수명을 함께 확인한다.
- 입력·HP·파티 등 UI는 서버 상태의 표시와 클라이언트 임시 상태를 구분한다.

## 메뉴와 계정 로그인

`MainMenuController`는 서버 주소와 계정 이름을 받고 공유 `AccountNameRule`로 이름을 먼저 검사한다. 서버 probe 성공 때 [AccountLoadout](../../03_Client/Assets/Scripts/Bootstrap/AccountLoadout.cs)에 정규화한 이름을 저장한다. 접속에는 프로세스의 `SessionAccountName`을 쓰고, PlayerPrefs `AccountName`은 다음 메뉴의 입력칸을 채우는 데만 사용한다. 이름이 없거나 잘못되면 `NetworkService`는 메인 메뉴로 돌아간다.

[ClientConnectionLifetime](../../03_Client/Assets/Scripts/Network/ClientConnectionLifetime.cs)은 연결에 이름과 직업을 고정하고, handshake 성공 뒤 로그인 요청을 한 번, `Accepted` 뒤 직업 선택을 한 번 보낸다. `LoginResultHandler`가 받은 유효 거부(`InvalidName`·`AlreadyOnline`)는 원격 종료가 뒤따라도 세션 정리 전까지 메인 큐에서 적용하며, 소유자는 세대·동일 세션·로그인 요청 여부를 검사한다. `Accepted`·정의 밖 결과와 다른 패킷은 닫힌 전송에서 적용하지 않으며, 정의 밖 결과는 열린 현재 연결의 메인 스레드 gate를 통과했을 때 연결을 끊는다. 거부는 `LoginRejected` → [MainMenuNotice](../../03_Client/Assets/Scripts/UI/MainMenuNotice.cs) → 메인 메뉴의 오류 글자 순서로 전달한다. 메뉴 시작 때 안내를 소비한다. 이전 연결의 늦은 결과는 generation/구독 해제로 무효화한다.

[MainMenuLoginView](../../03_Client/Assets/Scripts/UI/MainMenuLoginView.cs)는 `MainMenuController.Start`에서 만들어져 MainMenu 장면을 로그인 화면으로 배치한다. 장면 Canvas의 CanvasScaler를 실행 중 800×450 높이 맞춤으로 바꾸고, 장면의 `TITLE`을 위쪽에, 계정·서버 주소 입력칸과 시작·종료 버튼, 오류 글자를 밝은 스킨의 `LoginWindow` 안으로 옮긴다. 장면 파일과 컨트롤러의 직렬화 참조·버튼 onClick은 그대로다. view는 배치와 키 입력만 맡는다. 두 입력칸의 Enter는 같은 프레임의 두 번째 제출을 무시하고 시작 버튼과 같은 `MainMenuController.OnStartClicked`를 부르며, 계정 칸의 Tab은 서버 주소 칸을 선택한다(컨트롤러 `Update` → `HandleKeys`). 시작 판단은 `OnStartClicked`에 남는다.

## 캐릭터 선택 화면

[CharacterMenuView](../../03_Client/Assets/Scripts/UI/CharacterMenuView.cs)는 `CharacterSelectController`가 메뉴 경로에서 만드는 표시 전용 view다. 연결·캐릭터 식별·입장 권한은 [CharacterMenuPresenter](../../03_Client/Assets/Scripts/UI/CharacterMenuPresenter.cs)와 `CharacterMenuClient`가 갖고, view는 받은 상태를 그리며 누른 버튼을 요청 콜백으로 넘긴다. 상태 글 아래에는 한 번에 한 화면만 보인다. 칸 줄과 「선택하고 입장」 버튼이거나, 칸 줄 자리의 생성 화면(`CreateForm`)이다. 메뉴가 닫히는 Legacy 경로의 Knight/Mage 버튼은 `CharacterSelectController`가 따로 소유한다.

칸 줄은 `Slot1~3` 세 칸이 제목 띠(`Header`)의 안쪽 폭 전체를 같은 폭과 간격으로 채운다. 차 있는 칸을 누르면 그 칸이 선택된다. 「선택하고 입장」(`Submit`)은 차 있는 칸이 선택된 Ready에서만 눌리고 선택 요청을 한 번 보내며, 생성하지 않는다. 빈 칸을 한 번 누르면 칸 줄과 `Submit`이 숨고 생성 화면이 열린다. view는 생성 화면을 연 칸을 선택으로 기억하고, 생성 화면이 열린 동안에는 칸이 바뀌지 않는다.

생성 화면은 이름 칸·이름 규칙 글·Knight/Mage 버튼·「캐릭터 생성」(`Create`)·「취소」(`Cancel`)로 이뤄진다. `Create`는 (칸, 직업, 이름 원문)을 생성 콜백에 한 번 넘기고, 다음 상태가 그려지기 전의 두 번째 누름은 무시한다. 이름은 다듬지 않으며 규칙에 어긋난 이름도 서버가 판정하도록 보낸다. `Cancel`은 요청 없이 이름·직업 초안(Knight)·칸 선택·결과 글을 지우고 칸 줄로 돌아온다. 생성 성공으로 그 칸이 찬 Ready가 오면 생성 화면이 닫히고 선택된 칸이 없으므로, 입장하려면 칸을 다시 골라야 한다. 생성 거절이면 칸이 빈 채라 생성 화면과 입력이 남고 `Result`에 결과 글이 보인다. 대기·입장 중과 뒤로·연결 종료 뒤에는 모든 변경 입력이 잠긴다.

EditMode 시험은 view를 직접 만들어 칸 줄·생성 화면·잠금과 두 해상도(1280×720·1920×1080)의 글자 맞춤을 본다. PlayMode [CharacterSelectWideSceneTests](../../03_Client/Assets/Tests/PlayMode/CharacterSelectWideSceneTests.cs)는 출하 CharacterSelect 장면을 `EntryWirePeer` 대역 연결로 띄워 생성 화면 → 생성 요청 → 선택 요청까지 지난다. 실제 GameServer·DB를 상대로 한 이 화면은 자동 진행기(PR268)의 DB 영속 서버 실주행 전까지 미관측 외부 경계다.

## 파티 초대 요청과 구독

[PartyInvitePopup](../../03_Client/Assets/Scripts/UI/PartyInvitePopup.cs)은 클릭음·버튼 의도·표시/숨김과 이벤트 구독을 맡고, [PartyInviteResponseCommand](../../03_Client/Assets/Scripts/UI/PartyInviteResponseCommand.cs)는 실행 시 현재 상태/세션 조회·응답 패킷 구성·SendIntent 호출·pending 소비를 맡는다. void SendIntent 정상 반환은 실제 송신 접수나 서버 가입 확정이 아니다. 요청 처리 중 예외가 나면 뒤의 pending 소비와 숨김을 진행하지 않으며 membership은 기존 서버 update로 갱신한다.

팝업은 실제 구독한 PartyState 참조를 보관하고 disable/destroy 때 같은 source에서 해제한다. 파괴된 Unity 객체의 null 판정과 CLR 참조를 구분해 남아 있는 managed event도 정리한다. 활성 callback의 현재 Instance 조회는 유지하며 source 교체의 자동 재구독·pending 재표시는 제공하지 않는다. 변경할 때 [응답 계약 테스트](../../03_Client/Assets/Tests/EditMode/PartyInviteResponseContractTests.cs), [command 경계 테스트](../../03_Client/Assets/Tests/EditMode/PartyInviteResponseCommandTests.cs), [구독 수명 테스트](../../03_Client/Assets/Tests/EditMode/PartyInvitePopupBindingTests.cs)를 함께 확인한다.

## 인벤토리 표시와 요청 수명

[InventorySnapshotHandler](../../03_Client/Assets/Scripts/Network/Handlers/Inventory/InventorySnapshotHandler.cs)는 v17의 고정 길이·header·ID와 8슬롯의 상한·compact·정렬을 확인하고 수신 버퍼가 재사용되기 전에 값을 캡처한다. 현재 연결의 `EnqueueApply`를 통과한 snapshot만 [InventorySnapshotState](../../03_Client/Assets/Scripts/State/InventorySnapshotState.cs)의 재화·슬롯·revision을 바꾼다. 초기 미동기화와 유효한 revision 0을 구분하며, 낮은 revision과 같은 revision의 다른 값은 거부하고 동일 snapshot은 통지를 반복하지 않는다.

[InventoryState](../../03_Client/Assets/Scripts/State/InventoryState.cs)는 맵 사이에 유지되는 root MonoBehaviour다. CombatBootstrap 또는 먼저 도착한 유효 snapshot이 생성하고, NetworkService의 연결 종료 정리가 파티·퀘스트·인벤토리의 모든 값을 비운 뒤 관찰자에게 통지한다. 사용 결과·대기 작업은 각 UnityClientSession의 [InventoryRequestController](../../03_Client/Assets/Scripts/Network/InventoryRequestController.cs)가 소유하고 세션 cleanup에서 폐기한다. 옛 연결의 packet·지연 callback·늦은 종료는 새 미러를 변경하지 않는다.

조회는 MapEntryCoordinator의 HP·roster 바인딩과 `Ready` 대입이 끝난 통지에서 epoch마다 한 번 시작한다. 대기·실패·Ending에서는 조회하지 않는다. 같은 연결의 맵 이동 동안 기존 snapshot은 보존하지만 새 Ready 뒤 응답을 확인하기 전에는 최신 확인 완료나 사용 가능으로 표시하지 않는다. 응답이 없으면 5초 간격으로 한 회복 구간당 조회를 최대 3번 시도하고 멈춘다. 세션 소유 [InventoryTimeoutScheduler](../../03_Client/Assets/Scripts/Network/InventoryTimeoutScheduler.cs)는 하나의 timeout 등록을 유지하며 교체·취소·종료 때 등록과 callback을 제거한다. MainThreadDispatcher가 응답 기한을 메인 스레드에서 처리하고 예외를 격리한다. 응답 기한은 편집기 송신 지연 FIFO와 분리되며 옛 epoch/종료 callback은 무효화된다. 다음 Ready 또는 사용자 새로고침은 새 회복 구간을 시작할 수 있다. 주입하는 응답 시간 경계도 송신 지연의 `postDelayed`와 별도다.

[HudController](../../03_Client/Assets/Scripts/UI/HudController.cs)의 골드는 InventoryState의 `HasSnapshot`·`Currency`를 읽고 `OnInventoryChanged` 통지 안에서 갱신한다. snapshot 전과 연결 종료 정리 뒤에는 `Gold: -`를 보인다. 늦게 생기거나 교체된 미러에 다시 묶고, 교체·disable·destroy 때 실제로 구독한 참조에서 해제한다. 재화는 클라이언트에서 계산하지 않는다. [UI 씬](../../03_Client/Assets/Scenes/99.UI/UI.unity)의 HUD 골드는 재화 0~1,000,000,000을 축약 없이 Gold_bar 안의 한 줄로 표시한다. 글자 크기는 1920×1080 기준 Canvas에서 32로 고정하고 화면 크기에 따라 Canvas와 함께 축소한다. 자동 글자 크기와 줄바꿈은 끄며 글자·장식 배경은 raycast를 받지 않는다.

[HudLayout](../../03_Client/Assets/Scripts/UI/HudLayout.cs)은 UI 씬 소유의 장식 프레임·초상·실제 Q/E 슬롯 배치와 원본 Sprite mesh에 맞춘 아이콘 크기를 맡는다. HP·직업·재화·스킬 상태는 기존 컨트롤러가 소유한다. NPC 대화 또는 보상 창의 실제 가시 영역이 하단 HUD의 기본 위치와 겹치면 행동 바를 왼쪽, 재화 바를 오른쪽으로 옮긴다. 창이 닫히면 기본 위치로 돌아가며 HudLayout disable 때도 기본 위치를 복구한다.

[InventoryPanel](../../03_Client/Assets/Scripts/UI/InventoryPanel.cs)은 게임플레이 씬의 패널 하나를 만들고 실제 구독한 미러와 요청 controller를 보관해 disable/destroy 또는 source 교체 때 같은 참조에서 해제한다. 늦게 생긴 UI도 현재 snapshot을 다시 표시한다. 「I」 키의 새 눌림마다 열림·닫힘을 바꾸며, 열림 상태의 단일 소유자는 [UnityClientSession](../../03_Client/Assets/Scripts/Network/UnityClientSession.cs)의 `IsInventoryPanelOpen`이다. 새 연결은 닫힘으로 시작하고 같은 연결의 맵 이동 뒤 새 패널도 마지막 상태를 따른다. 연결 cleanup은 끝난 세션의 Instance를 해제하므로 다음 세션은 다시 닫힘으로 시작한다. 닫힌 동안에도 패널의 구독과 연결의 Ready 재조회는 유지하고, 열 때 최신 snapshot을 다시 표시한다.

[InventoryPanelView](../../03_Client/Assets/Scripts/UI/InventoryPanelView.cs)는 기존 TMP 한국어 fallback·Canvas 관례로 8슬롯·표시명/수량·재화·동기화 안내·사용 버튼과 서버 결과를 표시한다. 패널은 씬 소유이며 메뉴/캐릭터 선택/Ending에는 남지 않는다. 닫힌 패널은 CanvasGroup의 alpha·interactable·blocksRaycasts를 꺼 표시·클릭·raycast를 받지 않는다. 열린 패널은 버튼과 제목 띠의 잡는 칸만 raycast를 받으며 메뉴·페이드 아래에서 표시한다. 일시정지·진행 중 씬 전환·게임플레이 Ready 전에는 「I」와 버튼 입력을 막되 열림 상태는 유지한다. 패널의 크기와 자리는 공용 배치 부품 [UiWindowPlacement](../../03_Client/Assets/Scripts/UI/UiWindowPlacement.cs)가 정한다. 기본 자리는 화면 왼쪽 12, HUD의 `character_status`·`Party_Info` 아래 끝에서 12 아래이며, 그 영역에 최대 1.3배까지 맞춘다. 제목 띠의 보이지 않는 잡는 칸을 끌면 패널이 끈 만큼 움직이고, 패널 전체는 화면 안에 머문다. 옮긴 자리는 게임을 켜 둔 동안만 기억해 「I」로 닫았다 열거나 맵을 옮겨도 유지하고, 다시 켜면 기본 자리다.

[LocalPlayerInput](../../03_Client/Assets/Scripts/Input/LocalPlayerInput.cs)은 공격 액션의 실제 activeControl을 [GameplayPointerInput](../../03_Client/Assets/Scripts/Input/GameplayPointerInput.cs)에 넘긴다. 키보드·마우스의 공격은 왼쪽 Ctrl과 Enter이며 마우스 버튼은 공격 동작에 묶이지 않는다. 그래서 UI 위 포인터 차단은 포인터 바인딩인 터치 탭에만 남고, 그 터치의 현재 좌표에서 EventSystem uGUI raycast가 맞으면 공격 송신·예측·쿨다운 전에 반환한다. InputSystem 콜백 시점에는 EventSystem의 이전 프레임 포인터 캐시를 사용하지 않는다. UI 밖 포인터와 키보드·게임패드 공격은 기존 gate/전략을 유지한다. EventSystem 또는 장치가 없으면 기존 입력을 유지하며, 포인터 raycast 예외는 로그 후 해당 탭의 공격을 막는다.

사용은 현재 연결·게임플레이 Ready·snapshot 확인·소유량·ItemCatalog의 사용 가능 조건을 다시 읽고 현재 revision으로 요청한다. Material은 사용할 수 없고 소유 CoinPouch만 사용한다. 중복 클릭은 결과 대기 중 막으며 사용 요청을 자동 반복하지 않는다. 송신 반환은 사용 성공이 아니고, [ItemUseResultHandler](../../03_Client/Assets/Scripts/Network/Handlers/Inventory/ItemUseResultHandler.cs)의 검증된 서버 결과만 성공/거부 신호다. 성공·Stale 뒤의 snapshot이 누락되거나 결과가 오지 않으면 제한된 재조회로 회복한다. 로컬에서 수량·재화를 바꾸지 않으며 낮은 revision의 결과는 현재 표시를 덮지 않는다.

## 던전 보상 창과 입구 글자

39번 [DungeonClearResultHandler](../../03_Client/Assets/Scripts/Network/Handlers/Dungeon/DungeonClearResultHandler.cs)는 [v18 패킷 계약](protocol.md#v18-던전-클리어-패킷)을 검증한 뒤 `EnqueueApply`로 [DungeonClearRewardState](../../03_Client/Assets/Scripts/State/DungeonClearRewardState.cs)에 적용한다. 이 root는 결과를 받은 연결을 소유자로 기록하고 최신 결과 하나를 보관한다. 씬 전환 중 창이 없어도 결과를 보존하며 새 결과는 이전 표시를 대체한다. `NetworkService.ResetGlobalSessionMirrors`는 다른 미러와 함께 값을 비운 뒤 통지한다. 옛 연결의 늦은 callback은 세션 적용 gate가 거부하고, 창도 현재 연결의 identity와 종료 여부를 확인한다.

[DungeonClearRewardWindow](../../03_Client/Assets/Scripts/UI/DungeonClearRewardWindow.cs)는 `CombatBootstrap`이 게임플레이 씬에 만드는 전용 Canvas다. 성공이면 서버가 보낸 정수 골드와 카탈로그 이름·개수를 표시하고, 용량 거부이면 받은 보상이 없다는 안내와 이유만 표시한다. 현재 연결의 `CanSendGameplay`가 참일 때만 「확인」이 pending 결과를 먼저 소비하고 40번(`reserved=0`)을 한 번 보낸다. 같은 프레임 중복 클릭과 자동 재시도는 송신을 추가하지 않으며, 송신 예외에서도 소비한 창은 닫힌다. 정상 반환은 서버 처리 완료의 확인이 아니다. 게임플레이 밖 씬·연결 종료·disable에서는 CanvasGroup의 표시와 입력을 모두 끄고, disable/destroy 때 실제로 구독한 상태에서 해제한다. 열린 창도 확인 버튼만 raycast를 받으며 닫힌 창은 클릭과 공격을 막지 않는다.

창은 `LateUpdate`에서 활성 HUD의 `character_status` 하단을 읽어 그 아래 공간에 배치한다. 인벤토리 패널의 자리는 읽지 않으므로 「I」로 열고 닫아도 보상 창과 「확인」 위치가 바뀌지 않는다. 배치 후 현재 활성 씬의 창에 같은 연결의 pending 결과가 있고, 현재 `EventSystem`과 `currentInputModule`이 모두 활성·enabled인 경우에만 표시와 raycast를 공개한다. 이는 입력 기반 준비 조건이며 모든 HUD 배치가 끝났다는 보장은 아니다. 「확인」의 송신 가능 여부는 별도로 현재 연결의 `CanSendGameplay`를 따른다.

StageClear 배너는 보상 표시를 덮지 않도록 숨긴다. 15번 뒤 39번이 오면 보상 창을 공개할 때 `StageClearUI.Hide`를 호출하고, 39번 뒤 15번이 오면 `StageClearUI.Show`가 같은 연결의 pending 보상을 확인해 숨긴 채 반환한다. `Hide`는 진행 중 fade를 먼저 멈춰 다음 프레임에 배너가 다시 나타나지 않게 한다. `StageClearHandler`의 `Show` 뒤 StageClear SFX 호출 경로는 남아 있으며 배너 숨김과 별개다. 이 코드 경로 확인은 실제 소리 청취나 709×399·802×451·1920×1080 화면에서의 겹침 검증을 대신하지 않는다.

[DungeonEntranceLabel](../../03_Client/Assets/Scripts/UI/DungeonEntranceLabel.cs)은 같은 bootstrap에서 Town 씬의 기존 단일 `PortalTrigger` 위에 「던전 입구」를 붙인다. 포탈 collider의 위쪽에 여백을 둔 world-space TMP 글자이며 raycast를 받지 않는다. 포탈의 자식으로 씬과 함께 사라지고, HuntingGround·BossRoom에는 생성하지 않는다. 맵·포탈 표와 scene·prefab은 이 표시에 관여하지 않는다.

## 길드 창 배치와 밝은 스킨

[GuildUiController](../../03_Client/Assets/Scripts/UI/GuildUiController.cs)는 자기와 같은 부모(`CombatBootstrap`) 아래에 [GuildWindow](../../03_Client/Assets/Scripts/UI/GuildWindow.cs)와 [BlacksmithWindow](../../03_Client/Assets/Scripts/UI/BlacksmithWindow.cs)를 만든다. 두 창의 Canvas·배치·표시는 [GuildWindowLayout](../../03_Client/Assets/Scripts/UI/GuildWindowLayout.cs)이, 그림 조각은 `UiWindowAppearance.BrightSkin`이 맡는다. 크기는 Screen이 아니라 각 창 Canvas의 rect(CanvasScaler 결과)로 매 `LateUpdate`에 다시 계산한다.

길드 창은 가로형이다. 혼자 열리면 HUD 왼쪽 위 `character_status`와 오른쪽 위 미니맵 사이의 가운데 칸에 놓인다. 이 칸은 16:9 화면에서 잰 두 HUD 위치로 정한 Canvas 비율이며 HUD 계층을 조회하지 않는다. [GuildHousingLayout](../../03_Client/Assets/Scripts/UI/GuildHousingLayout.cs)이 거점 그림을 제목 띠 바로 아래 안쪽 폭 전체에 1672:941 비율로 두고, 정보 줄을 그림 아래 두 열로, 레벨업·새로고침 버튼을 마지막 줄에 둔다. 거점 레벨 1·2의 정지 그림 선택 규칙은 그대로다.

같은 부모의 대장간 창이 열려 있으면 길드 창만 왼쪽 기둥(Canvas 폭 2~40%, 아래에서 높이 2~82%)으로 비킨다. 이때 창은 통째로 줄지 않고 좁은 배치로 바뀐다. 글자는 대장간 창 글자와 같은 크기로 두고, 거점 그림 칸을 줄이며, 두 안내 문구는 각자 한 줄을 쓴다. 대장간 창이 닫히면 혼자일 때 자리와 크기로 돌아온다. 이 관찰은 `GuildWindow`가 하며, 대장간 창은 길드 창을 모르고 자기 자리·크기를 유지한다. 글자는 밝은 바탕에 진한 잉크, 안내 문구에 옅은 잉크, 어두운 제목 띠와 금고 띠에 금색, 버튼에 흰색이다. 행동 버튼(거점 레벨업·무기 강화)은 Accept, 닫기·새로고침은 Default 조각이다.

## 처치 보상 글

[KillRewardFloatText](../../03_Client/Assets/Scripts/Combat/Effects/KillRewardFloatText.cs)는 `CombatBootstrap`이 게임플레이 씬에 만드는 월드 공간 TMP 글이며, 몬스터가 죽은 자리 위에 「+금화 N」과 늘어난 아이템마다 「+<카탈로그 이름> N」을 띄워 천천히 올리다가 끝에서 흐리게 지운다. 처치 보상을 따로 알리는 패킷은 없으므로 [KillRewardPairing](../../03_Client/Assets/Scripts/Combat/Effects/KillRewardPairing.cs)이 InventoryState 통지 때 서버가 확정한 스냅숏을 직전 사본과 비교한 증가분만 쓴다. 줄어든 값은 무시하고 접속 뒤와 미러 리셋 뒤의 첫 스냅숏은 기준만 잡는다. 던전 클리어 Success 결과의 몫(증가분)은 바로 다음 스냅숏의 증가에서 빼며, 스냅숏이 뒤따르지 않는 CurrencyCap·InventoryFull 결과는 다음 처치를 깎지 않는다. 사망 기록은 `EnemyRegistry.Despawn`이 레지스트리에서 지우기 직전의 위치를 내보낸 것이다. 늘어난 값은 실제 시간 1.0초 안의 사망 기록과만 짝지어지고 여럿이면 로컬 플레이어에 가장 가까운 것을 쓰며, 사망 기록이 없으면 글도 없다. 출처를 구분할 패킷이 없어 같은 창 안의 다른 증가(재화 주머니 사용 등)도 함께 뜰 수 있다. 글과 사망 기록은 씬 소유라 맵 이동 때 씬과 함께 사라지고, 연결 종료는 InventoryState 리셋 통지로 비운다. 글은 Canvas·collider 없이 TextMeshPro만 써서 raycast를 받지 않으며 공격과 UI 포인터 판정을 바꾸지 않는다.

## 원격 보간

RemoteInterpolationState가 snapshot buffer·서버 시간축 렌더 시계·보간 계산을 단독 소유하며 명시적인 deltaTime으로 진행한다. RemoteEntity는 기존 공개 component API를 유지하고 Unity 프레임의 결과를 Transform에 적용한다. Initialize는 위치를 즉시 적용하고 시계를 초기화하며, SnapInterpolation은 현재 Transform을 유지한 채 buffer/clock을 초기화하고 ClearBuffer는 buffer만 지운다. 텔레포트 도착 callback은 새 snapshot 적재 직후 한 번 호출한다. registry·적 VisualFootOffset과 프레임당 catch-up의 수치 의미는 유지한다.

## 에셋을 다룰 때

기존 prefab을 저장하기 전에 추적 여부와 사용자 변경을 확인한다. 미추적 에셋은 Git으로 복원할 수 없으므로 덮어쓸 대상의 사본을 먼저 확보한다. `.meta`·GUID와 직렬화 값, Resources 등의 문자열 경로를 함께 보존한다. 이는 과거 BackGround prefab 덮어쓰기 사례에서 확인된 복구 한계다.

Unity 버전은 [ProjectVersion.txt](../../03_Client/ProjectSettings/ProjectVersion.txt)의 버전과 revision을 함께 확인한다. 2026-10-07부터 기준은 `6000.6.4f1`(`12bfff696524`)이고, 설치 방법은 [개발 안내](../operations/DEVELOPMENT.md#환경)에 있다. 6.6에서 달라져 코드와 에셋을 바꿀 때 확인할 점은 셋이다. 근거는 [전환 goal](../../01_Phases/goals/2026-10-07-unity-engine-upgrade/goal.md#실제-결과와-미실행)에 있다.

- `Object.GetInstanceID()`는 컴파일 오류(CS0619)다. `GetEntityId()`를 쓴다.
- 엔진이 강제한 Input System 1.20.0은 `InputSystem.settings`를 바꿀 때 이전 설정이 `HideAndDontSave`이면 파괴한다. 이 프로젝트에는 InputSettings 에셋이 없어 기본 설정이 그런 임시 객체다. 테스트가 전역 입력 설정을 바꾸면 [MapEntryPlayFixture](../../03_Client/Assets/Tests/PlayMode/MapEntryPlayFixture.cs)처럼 손대지 않은 사본을 두고 복원한다.
- Cainos의 Lucid Editor 에디터 코드와 그것을 상속하는 Cainos 에디터 스크립트는 6.6에서 컴파일되지 않아 지웠다. 런타임 코드와 아트는 남겼다. 그 에셋을 다시 가져오면 지운 코드가 돌아와 컴파일이 막힌다.

과거에는 `6000.4.1f1`이라는 같은 표기에서 `8535861f39e1`과 `336a400b9ea2` revision 차이를 관찰했다. 이 숫자는 현재 설치 기준이 아니라 과거 사례다.

입력 ack·임펄스 예측의 경계는 [영역 간 경계](cross-cutting.md)에 있다. [이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/client/_index.md)은 에셋 사고와 2026-05-16 버전 관찰을 보존한다.
