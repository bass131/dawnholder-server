# 프로토콜과 공유 코드

[PDL.xml](../../99_Tools/PacketGenerator/PDL.xml)이 패킷 정의 원본이며 [PacketGenerator](../../99_Tools/PacketGenerator/)가 [GenPackets.cs](../../98_Shared/Protocol/Generated/GenPackets.cs)를 만든다. 생성 코드의 수동 편집으로 계약을 변경하지 않는다.

## 변경 절차와 소비자

PDL 수정 → 생성기 실행 → 생성 diff와 [ProtocolVersion](../../98_Shared/Protocol/ProtocolVersion.cs) 확인 → 서버·클라이언트 핸들러·봇 점검 → 양쪽 빌드·통신 검증 순으로 진행한다. 실행 명령과 DLL 복사는 [개발 안내](../operations/DEVELOPMENT.md)를 따른다.

패킷 ID를 재사용하거나 정의 순서를 바꾸지 않는다. 필드 끝 추가도 기존 읽기 코드에 영향을 준다. 버전 일치로 연결을 제한하는 현재 핸드셰이크와 새 동작을 함께 검증한다. 전송의 프레임 길이 검사는 [서버](../../02_Server/Network/FrameValidator.cs)와 [클라이언트](../../04_ClientNet/FrameValidator.cs)에 각각 있다.

## v17 경제 패킷

`ProtocolVersion.Current=17`이며 v16 연결은 기존 버전 일치 검사로 거부한다. 기존 ID 1..34와 필드 순서는 보존하고 PDL 끝에 아래 4개를 추가했다. 필드는 LittleEndian이며 전체 길이는 ushort size + ushort id의 4바이트 헤더를 포함한다. 슬롯은 ItemId 오름차순으로 조밀하게 채운 고정 8개이고 빈 슬롯은 0/0이다.

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
