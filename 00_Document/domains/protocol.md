# 프로토콜과 공유 코드

[PDL.xml](../../99_Tools/PacketGenerator/PDL.xml)이 패킷 정의 원본이며 [PacketGenerator](../../99_Tools/PacketGenerator/)가 [GenPackets.cs](../../98_Shared/Protocol/Generated/GenPackets.cs)를 만든다. 생성 코드의 수동 편집으로 계약을 변경하지 않는다.

## 변경 절차와 소비자

PDL 수정 → 생성기 실행 → 생성 diff와 [ProtocolVersion](../../98_Shared/Protocol/ProtocolVersion.cs) 확인 → 서버·클라이언트 핸들러·봇 점검 → 양쪽 빌드·통신 검증 순으로 진행한다. 실행 명령과 DLL 복사는 [개발 안내](../operations/DEVELOPMENT.md)를 따른다.

패킷 ID를 재사용하거나 정의 순서를 바꾸지 않는다. 필드 끝 추가도 기존 읽기 코드에 영향을 준다. 버전 일치로 연결을 제한하는 현재 핸드셰이크와 새 동작을 함께 검증한다. 전송의 프레임 길이 검사는 [서버](../../02_Server/Network/FrameValidator.cs)와 [클라이언트](../../04_ClientNet/FrameValidator.cs)에 각각 있다.

## v22 캐릭터 목록·생성·선택

`ProtocolVersion.Current=22`이며 equality handshake가 v21 연결을 거부한다. v21은 Core의 [저장 재시작 진입 변경](../../01_Phases/goals/2026-10-14-persistence-runtime-entry/goal.md)(`bcafb6bc`, 2026-10-15)이며 이 문서에 절이 아직 없다. 기존 1..48 번호와 필드는 보존하고 PDL 끝에 아래를 추가한다. LittleEndian이고 전체 길이는 4바이트 머리를 포함한다. string은 UInt16 바이트 길이 + UTF-16 LE이며 ID는 36자 D형 Guid(72바이트)다.

| ID·이름 | 머리 뒤 필드 순서 | 전체 길이 |
|---|---|---|
| 49 C_CharacterListRequest | uint requestId, byte reserved(0) | 9 |
| 50 S_CharacterListResult | uint requestId, byte result, byte invalidField, byte occupiedMask, 슬롯1..3마다 string slotNCharacterIdD, string slotNName, byte slotNClassId | 26 + 문자열 바이트, 최대 314 |
| 51 C_CharacterCreateRequest | uint requestId, byte slot, byte classId, byte reserved(0), string rawName | 13 + 이름 바이트, 최대 85 |
| 52 S_CharacterCreateResult | uint requestId, byte result, byte invalidField, byte slot, string characterIdD, string name, byte classId | 16 + 문자열 바이트, 최대 112 |
| 53 C_CharacterSelectRequest | uint requestId, byte reserved(0), string characterIdD | 11 + ID 바이트, D형 83 |
| 54 S_CharacterSelectResult | uint requestId, byte result, byte invalidField, string selectedCharacterIdD | 12 + ID 바이트, 성공 84 |

[CharacterMenuRequestReader](../../02_Server/GameServer/Handlers/Session/CharacterMenuRequestReader.cs)는 큐 등록 전 size·ID·실제 길이 일치, reserved=0, requestId≠0을 검사한다. 49는 정확히 9바이트이고 51·53의 문자열은 72바이트 이하 짝수 길이로 프레임 끝까지 차야 하며 짝 없는 surrogate를 거부한다. 구조 위반과 연결에서 이미 본 requestId 이하는 무응답으로 버린다. 연결당 한 건만 처리하며 그 사이 새 requestId는 `RequestInProgress`를 받는다.

[CharacterMenuResult](../../98_Shared/Protocol/CharacterMenuResult.cs)는 Success=0, InvalidInput=1, RequestInProgress=2, InvalidState=3, SlotOccupied=4, SlotsFull=5, NameTaken=6, CharacterUnavailable=7, Busy=8, RecoveryRequired=9, CancelledBeforeDispatch=10, Unavailable=11, OutcomeUnknown=12, SchemaMismatch=13, IntegrityFailure=14, OperationConflict=15, LegacyFlowOnly=16이다. [CharacterMenuInvalidField](../../98_Shared/Protocol/CharacterMenuInvalidField.cs)는 None=0, Slot=1, Name=2, Class=3, CharacterId=4이며 InvalidInput에서만 None이 아니다. 실패는 빈 문자열·0으로 채워 50·52·54가 26·16·12바이트다. occupiedMask bit0/1/2가 슬롯1/2/3이며 Knight도 class0이라 빈 슬롯은 mask로만 구분한다.

Stored 호스트는 로그인 `Accepted` 뒤 목록 49→50, 생성 51→52, 선택 53→54 Success → 48 Success(저장 직업) → 맵 입장 순서이며 [CharacterMenuCatalogSession](../../02_Server/GameServer/Sessions/CharacterMenuCatalogSession.cs)이 처리한다. 54는 플레이 가능 신호가 아니며 선택 거부는 54로 끝나 48·입장이 없다. 적용 미확정 결과는 12를 보낸 뒤 연결을 닫는다. 선택 단계의 기존 `C_CharacterSelect`(16)는 48 `CharacterLoadStatus.Unavailable`·class255로 거부된다. 이름 규칙은 서버 Core `PrepareCreate`가 [CreateDescriptor.cs](../../02_Server/GameServer/Persistence/Catalog/CreateDescriptor.cs)의 `CatalogValidation.Name`으로 판정한다.

Legacy 호스트는 49·51·53에 선택 단계면 `LegacyFlowOnly`(16), 아니면 `InvalidState`(3)로 답하고([CharacterMenuSession](../../02_Server/GameServer/Sessions/CharacterMenuSession.cs)) 기존 `C_CharacterSelect` → 48 → 맵 입장을 유지한다. `ServerHost.Start`는 catalog 없는 Stored 시작을 거부한다.

## v20 길드·거점·개인 강화

`ProtocolVersion.Current=20`이며 equality handshake가 v19 연결을 거부한다. 기존 1..42 번호와 필드는 보존하고 PDL 끝에 아래 필드를 추가한다. LittleEndian이고 전체 길이는 ushort size/id의 4바이트 머리를 포함한다.

| ID·이름 | 머리 뒤 필드 순서 | 전체 길이 |
|---|---|---|
| 43 C_GuildRequest | byte reserved(0) | 5 |
| 44 S_GuildSnapshot | long snapshotSequence, byte changeKind, uint guildId, uint guildRevision, int treasury, byte strongholdLevel, byte maxStrongholdLevel, byte blacksmithUnlocked, int memberCount, int onlineMemberCount, uint inventoryRevision, byte weaponLevel, byte maxWeaponLevel, int attackBonus, int strongholdCost, int weaponCurrencyCost, int weaponMaterialId, int weaponMaterialCount, byte strongholdAvailability, byte weaponAvailability, string guildName | 66 + 이름 UTF-16 바이트 수 |
| 45 C_StrongholdUpgrade | uint expectedGuildRevision | 8 |
| 46 C_WeaponEnhance | uint expectedInventoryRevision | 8 |
| 47 S_GuildActionResult | byte action, byte result, uint requestedRevision, uint guildRevision, uint inventoryRevision, long snapshotSequence | 26 |

서버 핸들러는 Read 전 정확한 길이·머리·reserved를 검사한다. 송신 이름은 1..16 UTF-16 code unit이며 기본 `새벽 길드`로 44는 76바이트다. 소비자는 Read 전에 최소66바이트·머리와 offset64의 UInt16 이름 바이트 길이(2..32, 짝수), 전체 size=66+이름 바이트 수를 검사해야 한다. 계정 GUID는 전송하지 않는다.

[GuildAction](../../98_Shared/GameData/Enums/GuildAction.cs)은 StrongholdUpgrade=1, WeaponEnhance=2다. [GuildChangeKind](../../98_Shared/GameData/Enums/GuildChangeKind.cs)는 Query=0, Membership=1, TreasuryCredit=2, StrongholdUpgrade=3, WeaponEnhance=4, Presence=5다. TreasuryCredit은 실제 Content clear의 개인 지급과 공동 적립을 함께 확정한 뒤 발행한다. 수혜자 송신 순서는 39→36→자기44이며 다른 온라인 회원도 자기 개인값을 담은44를 받는다. [GuildResult](../../98_Shared/GameData/Enums/GuildResult.cs)는 Success=0, Stale=1, NotMember=2, NotInTown=3, Dead=4, FacilityLocked=5, MaxLevel=6, InsufficientCurrency=7, InsufficientMaterial=8, RevisionExhausted=9, Unavailable=10이며 가능 여부에도 같은 코드를 쓴다. Snapshot의 Success는 현재 이용 가능 표시이며 성공 이벤트가 아니다.

44의 범위는 금고0..10억, 거점1..2/max2, 해금0/1과 level2의 동치, 회원수≥온라인수≥0, 무기0..1/max1, bonus0/5와 무기 단계의 동치다. 최고 단계의 다음 비용과 재료 수는0이며 재료ID는1이다. 단계0의 시설 잠김 상태에도 강화 가격20·재료1개를 표시한다. 가능 여부는 [서버의 거부 순서](server.md#길드와-강화)를 따르되 expectedRevision 비교만 제외한다.

접속별 sequence는1부터 증가하고 long.MaxValue 뒤 발행을 종료하며 wrap하지 않는다. 회원/온라인 변화는 sequence로 정렬하고 공동 경제 revision과 분리한다. 정상 조회는44 하나, 거점 성공은47→구매자44와 다른 회원44, 거점 거부(stale 포함)는47→현재44다. 강화 성공은47→36→44, 강화 stale은47→현재36→44, 강화의 다른 거부는47→현재44다. 47은 입력의 action·요청 판을 그대로 돌려주고 해당44의 sequence를 가리킨다. 내부 오류는 정상 거부로 바꾸지 않으며 송신 실패가 확정값을 되돌리거나 재지급하지 않는다.

소비자는 연결 세대와 더 큰44 sequence를 검사하고, 개인 판은 유효36/44의 최댓값으로 합쳐야 한다. 낮은 개인 판의44는 공동 표시만 갱신하며 개인 강화/가격/가능 여부를 덮지 않는다. 구매 pending은 연결 소유1개이며 일치하는47과 지목된44 모두를 처리할 때 끝낸다. 창 재개·시간 경과·다른 이유의 방송으로 해제/재전송하지 않는다. 조회 pending은 더 큰 Query44로 끝낸다. 첫 동기화/재조회/옛 sequence는 성장 효과를 재생하지 않는다. [GuildGrowthProbe](../../99_Tools/headless-bot/Scenarios/Guild/GuildGrowthProbe.cs)는 이 연결별 길이·sequence·pending 상관을 검사하는 봇 소비자다. 낮은 개인 판의44는 봇 상태 view를 덮지 않고 원시 상관 기록에만 남긴다. Unity 길드 소비자·컴파일과 최종 독립 판정은 별도로 확인한다.

## v19 계정 로그인

v19에서 계정 로그인을 추가했으며 v18 연결은 당시 버전 일치 검사로 거부했다. 기존 ID 1..40의 번호·필드는 보존하고 PDL 끝에 `C_Login`(41, `string accountName`)과 `S_LoginResult`(42, `byte result`)를 추가했다. 문자열은 UInt16 바이트 길이와 UTF-16 LittleEndian 내용이며, 서버는 길이 머리와 전체 프레임의 정확한 일치를 먼저 확인한다.

접속 순서는 `C_Handshake` → 성공 → `C_Login` → `Accepted` → `C_CharacterSelect`다. [AccountNameRule](../../98_Shared/GameData/Accounts/AccountNameRule.cs)은 3~16자 ASCII 영문·숫자·밑줄을 받으며 A-Z만 소문자로 바꾸고 공백은 자르지 않는다. [LoginResult](../../98_Shared/GameData/Enums/LoginResult.cs)는 Accepted=0, InvalidName=1, AlreadyOnline=2다. 거부 결과 뒤 연결을 끊고, 같은 연결의 두 번째 로그인은 결과 없이 버린다. 계정 번호는 서버에만 두며 비밀번호 인증·DB 저장은 포함하지 않는다.

## v18 던전 클리어 패킷

v18에서 `ProtocolVersion.Current`를 18로 올렸고 v17 연결은 기존 버전 일치 검사로 거부했다. 아래 v17 경제 계약의 패킷 구조와 기존 ID 1..38은 보존한다. PDL 끝에 추가한 두 패킷은 LittleEndian이고, 전체 길이는 4바이트 size/id 헤더를 포함한다.

| ID | 패킷 | 헤더 뒤 필드 순서 | 전체 길이 |
|---|---|---|---|
| 39 | S_DungeonClearResult | byte result, int currency, int reward0ItemId, int reward0Count, int reward1ItemId, int reward1Count, int reward2ItemId, int reward2Count | 33 |
| 40 | C_DungeonClearConfirm | byte reserved(0) | 5 |

39번의 결과는 기존 `InventoryResult.Success`·`CurrencyCap`·`InventoryFull`만 사용한다. 클라이언트 [DungeonClearResultHandler](../../03_Client/Assets/Scripts/Network/Handlers/Dungeon/DungeonClearResultHandler.cs)는 정확한 길이·헤더·결과값, 골드 0~`InventoryLimits.MaxCurrency`, 각 칸의 카탈로그 아이템과 수량 1~`MaxStack` 또는 빈 칸 0/0을 검사한다. 잘못된 결과는 상태를 바꾸지 않고 버린다. 유효 결과는 현재 연결의 메인 스레드에서 보상 창에 전달하며 인벤토리나 보상량을 클라이언트에서 계산하지 않는다.

[DungeonClearRewards](../../98_Shared/GameData/Dungeon/DungeonClearRewards.cs)는 임시 보상표(골드 50, Material 1, CoinPouch 1)와 패킷 칸 수 3을 공유한다. 서버는 보스방 복사본의 수혜자마다 이 표를 원자 지급하고 39번 결과를 보낸다. 성공은 표 순서의 골드·아이템과 뒤이은 36번 snapshot, 거부는 결과 코드와 골드 0·세 칸 0/0이며 snapshot은 없다. 등록된 [40번 처리기](../../02_Server/GameServer/Handlers/Dungeon/DungeonClearConfirmHandler.cs)는 선택 뒤 정확한 길이 5·헤더·reserved 0을 검증한다. 현재 맵 job에서 같은 연결의 미확인 수혜자만 마을 이동을 요청하며 나머지는 응답·상태 변경·연결 종료 없이 버린다. 클라이언트 확인은 `CanSendGameplay`일 때 `reserved=0`을 `SendIntent`로 한 번 보내는 의도이며 서버의 보상 지급이나 맵 복귀 완료를 뜻하지 않는다.

## v17 경제 패킷

v17에서 기존 ID 1..34와 필드 순서를 보존하고 PDL 끝에 아래 4개를 추가했다. 필드는 LittleEndian이며 전체 길이는 ushort size + ushort id의 4바이트 헤더를 포함한다. 슬롯은 ItemId 오름차순으로 조밀하게 채운 고정 8개이고 빈 슬롯은 0/0이다.

| ID | 패킷 | 헤더 뒤 필드 순서 | 전체 길이 |
|---|---|---|---|
| 35 | C_InventoryRequest | byte reserved(0만 허용) | 5 |
| 36 | S_InventorySnapshot | uint revision, int currency, int slot0ItemId, int slot0Count, …, int slot7ItemId, int slot7Count | 76 |
| 37 | C_ItemUse | int itemId, uint expectedRevision | 12 |
| 38 | S_ItemUseResult | byte result, int itemId, uint revision | 13 |

경제 입력의 행위자는 session이며 패킷에 행위자/보상량을 넣지 않는다. [Handlers/Inventory](../../02_Server/GameServer/Handlers/Inventory/)는 Read 전 정확한 길이·헤더, 조회 reserved=0, 사용의 정의된 양수 ItemId를 검사한다. 잘못된 입력·비활성/종료 입력은 상태를 바꾸지 않고 무응답 거부한다. 연결당 경제 요청 대기 1건을 초과하면 추가 요청을 drop하고, 처리가 끝나거나 연결이 종료되면 guard를 해제한다. 실제 등록과 이동/종료 수명은 [서버 경제 계약](server.md#경제-상태와-연결-수명)에 있다.

결과 값은 [InventoryResult](../../98_Shared/GameData/Enums/InventoryResult.cs)의 Success=0, Stale=1, NotOwned=2, NotUsable=3, CurrencyCap=4, InventoryFull=5, RevisionExhausted=6이다. 정상 조회는 현재 snapshot을 보낸다. 성공 사용은 Success 결과 → 새 snapshot, Stale은 현재 revision 결과 → 현재 snapshot 순으로 보내며 소비하지 않는다. 그 밖의 사용 거부는 결과만 보낸다. 조회·거부는 revision을 바꾸지 않고 성공한 사용/보상 묶음만 +1 한다. 수락 가능한 새 변경은 uint.MaxValue에서 거부해 wrap하지 않으며 expectedRevision 불일치의 Stale 우선순위는 유지한다.

[ItemCatalog](../../98_Shared/GameData/Items/ItemCatalog.cs)의 임시 데이터는 재료(1, 사용 불가)와 재화 주머니(2, 한 개 사용 시 재화 +50)다. 등록된 연결의 첫 상태는 revision0·재화0·빈 슬롯이다. 처치 snapshot의 지연 push는 기존 SendToEntity 경로를 사용하며 재지급하지 않는다. PR1에는 서버 메모리 상태·wire 계약만 구현되었다. 클라이언트 수신 핸들러·미러·UI는 PR2에서 구현하며 같은 연결의 낮은 revision을 무시하고 최초 revision0과 미동기를 구분해야 한다. DB 저장·로드는 구현하지 않았다.

## 서버 연결 종료

서버 Network.Session.Disconnect는 최초 호출만 종료 알림을 수행하고 정상 경로에서 callback → socket Shutdown → Close → 송신 큐·pending list 정리 순서를 유지한다. endpoint 조회나 callback이 실패해도 transport 정리를 시도한 뒤 원래 예외를 전파하며, 정리·진단 출력의 실패가 그 예외를 가리지 않는다. Shutdown·Close 실패는 기존처럼 무시하고 다음 정리를 진행한다. 클라이언트 종료 정책과 진행 중 I/O의 전체 수명은 이 계약의 변경 범위가 아니다.

## 타입과 권한

공유 공식·스탯 타입은 양쪽이 읽는 계약이다. 서버 권위는 판정을 서버가 한다는 뜻이며, 타입을 서버 프로젝트에만 두라는 뜻은 아니다. `PlayerStats`·`Formulas`는 [Shared/GameData/Combat](../../98_Shared/GameData/Combat/)에 있고 적용은 서버 시스템에서 수행한다.

Shared·ClientNet은 .NET Standard 2.1을 대상으로 한다. .NET 10 서버 타입을 옮길 때 `init`·`record`의 `IsExternalInit` 같은 런타임 차이를 확인한다. 과거에는 CS0518 오류를 관찰했고, setter/factory로 표현하거나 필요한 호환 타입을 명시하는 선택지가 있었다. 현재 프로젝트가 특정 shim을 제공한다고 추측하지 않는다.

문서에 경로나 자동 처리를 추가하면 실제 파일·호출·빌드 설정을 함께 확인한다. 디렉터리나 예정 기능이 문서에만 존재했던 과거 사례를 현재 구현의 근거로 사용하지 않는다.

[이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/shared/_index.md)에 공유 타입 이동·런타임 호환성·문서와 코드 불일치의 사례와 당시 판단을 보존한다.
