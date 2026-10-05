# 서버

`02_Server/GameServer`는 권위 상태·게임 판정을, `02_Server/Network`는 전송·프레이밍을 담당한다. 공유 계약은 [프로토콜](protocol.md)에 있다.

## 수정 흐름

일반 맵 입력은 [GameSession](../../02_Server/GameServer/Sessions/GameSession.cs) → [Handlers](../../02_Server/GameServer/Handlers/) → 세션 제출 메서드 → [GameMap](../../02_Server/GameServer/Maps/GameMap.cs)의 큐 → [Systems](../../02_Server/GameServer/Maps/Systems/) 순으로 처리한다. 경제 입력은 아래의 World 경제 큐로 전달한다. 상태 변경은 소유한 틱 흐름 안에서 수행한다.

- 핸들러는 파싱·입력 검증·세션 메서드 호출을 맡는다. 세션의 핸드셰이크·entity ID·종료 상태를 직접 우회 변경하지 않는다.
- 새 입력에는 정상 경로와 잘못된 길이·범위·상태·권한의 거부 경로를 확인한다.
- 맵 이동은 [MapMigration](../../02_Server/GameServer/Maps/Transitions/MapMigration.cs), 파티·퀘스트 갱신 순서는 [GameWorld](../../02_Server/GameServer/Loop/GameWorld.cs)에서 확인한다.

## 경제 상태와 연결 수명

[HandlerRegistry](../../02_Server/GameServer/Handlers/HandlerRegistry.cs)가 [InventoryRequestHandler·ItemUseHandler](../../02_Server/GameServer/Handlers/Inventory/)로 v17 입력을 보낸다. 핸들러는 Read 전에 정확한 길이·헤더·reserved/아이템 ID를 검사하고, `GameSession.SubmitInventoryRequest/SubmitItemUse`는 연결·클래스·entity·이동 상태와 연결당 대기 1건 guard를 검사해 [InventoryRegistry](../../02_Server/GameServer/Items/InventoryRegistry.cs)에 작업을 넣는다. 수신 스레드는 경제 상태를 직접 읽거나 바꾸지 않는다.

`GameSession.EnterGameWorld`의 입장 job은 실제 `AddPlayer`와 entity 소유자 설정 뒤 `InventoryRegistry.Register`를 호출한다. 등록할 때만 `GameWorld.IsActiveSession`으로 맵의 실제 owner를 확인한다. Registry의 entity→session 등록은 World 틱에서만 생성·해제하고, 처리 시 등록의 동일 session·EntityId·closing을 다시 확인한다. 따라서 소스 맵에서 제거되고 목적지 맵에 도착하기 전의 공백도 같은 경제 연결 수명이다. `Entry.State`는 첫 경제 작업에서 생성하는 불변 [InventoryState](../../02_Server/GameServer/Items/InventoryState.cs)이며 초기 revision·재화·슬롯은 0이다.

실제 처치 callback `GameWorld.MakeMap`은 퀘스트 분기와 독립적으로 `EnqueueKill`을 호출한다. [KillRewardPolicy](../../02_Server/GameServer/Items/KillRewardPolicy.cs)가 임시 처치자 지급·적 종류별 묶음을 정하며, job은 그때 등록된 세션 owner와 처치 값을 고정한다. [InventoryTransitions](../../02_Server/GameServer/Items/InventoryTransitions.cs)는 전체 묶음 또는 아이템 1개 사용의 다음 상태를 먼저 검사·계산하고 Registry가 한 번 commit한다. 성공마다 revision+1, 거부·조회는 불변이며 상한 거부는 부분 지급하지 않는다. 카탈로그·8슬롯/스택/재화 상한의 자리표시 값은 [공유 Items](../../98_Shared/GameData/Items/)에 있다.

`GameWorld.OnTick`은 맵 틱 전후에 연결 종료를 정리한 다음 Party → Quest → Inventory → cleanup barrier 순으로 실행한다. `DrainSessionCloses`의 `InventoryRegistry.Forget`은 등록·상태·요청 guard를 정리한다. 종료 뒤 요청/보상과 이전 owner의 늦은 job은 새 상태를 만들지 않는다. 요청 guard는 처리의 성공·거부·예외에도 해제한다. 등록 전 입력과 새로 도착한 이동 중 입력은 무응답 거부하며, 이미 큐에 들어간 요청은 이동 틈에서도 같은 등록으로 처리할 수 있다.

조회·사용 결과는 캡처한 session에 직접 답하고, 처치 snapshot은 기존 `GameWorld.SendToEntity`로 보낸다. 이 공용 경로는 맵 소속이 없거나 다음 맵 job 전에 소유자가 사라지면 push를 생략할 수 있다. commit된 경제 상태는 유지되어 이후 조회로 받을 수 있으며 송신 실패 뒤 rollback·재지급·재시도 큐는 없다. wire와 결과 순서는 [v17 경제 패킷](protocol.md#v17-경제-패킷)에 있다. PR1은 메모리 상태와 패킷까지이며 DB 저장·로드, 클라이언트 상태 미러·인벤토리 UI(PR2)는 구현하지 않았다.

## 즉시 적 피해

평타·Dash는 대상 선택·피해 계산·시전과 생존 시 넉백을 각 액션에서 처리한다. `GameMap.ApplyImmediateEnemyHit`는 틱 흐름 안에서 계산된 피해의 HP 반영·공격자 지정·Hit 통지·치사 후처리를 소유하고 생존 여부를 반환한다. 즉시 피해의 음수 HP도 Hit 패킷에 그대로 보낸다. Hit → Death → 보스 StageClear → 제거·리스폰 등록 → 처치 콜백 순서를 보존한다. 지연 피해는 기존 `DeferredDamageSystem` 경로를 유지한다.

## 맵 패킷 표현

입장·맵 이동의 등록과 수신자 정책은 기존 호출자가 소유하며, PlayerJoin의 roster·broadcast wire 조립은 MapPacketPublisher가 공유한다. 호출자는 최초 EnterMap 또는 이동 MapTransition → HP → 등록 전 snapshot의 roster → 본인 제외 join 순서를 유지한다. 일반 적·보스 시스템은 FSM·latch와 발행 간격을 결정하고 GameMap을 통해 EntityState 표현을 위임한다. 일반 적 Hit 우선과 보스 Attack 우선, AI → 보스 → 중력의 발행 시점을 보존한다.

## 종료와 broadcast

수신자의 연결 정리와 broadcast는 겹칠 수 있다. [MapPacketPublisher](../../02_Server/GameServer/Maps/MapPacketPublisher.cs)의 수신자 필터와 세션 `IsClosing`을 확인한다. 종료된 세션을 건너뛰어도 이미 시작된 전송의 수명주기까지 자동 해결되는 것은 아니다.

과거 `LifecycleRace_NewJoinBroadcastSkipsClosingSession` 사례는 `s2.OnConnected → s1.OnDisconnected → Tick` 순서로 닫히는 수신자가 목록에 남는 분기를 만들었다. 반대 순서에서는 큐가 먼저 정리되어 해당 분기를 거치지 않고도 테스트가 통과했다. 경쟁 조건 검증은 호출 순서와 실제 분기 진입을 함께 확인한다.

근거: [테스트 코드](../../02_Server/GameServer.Tests/), 과거 관찰 커밋 `5ea1123`, [이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/server/_index.md). 당시 사례는 2회 관찰과 별도 리뷰를 기록했다.
