# 프로토콜과 공유 코드

[PDL.xml](../../99_Tools/PacketGenerator/PDL.xml)이 패킷 정의 원본이며 [PacketGenerator](../../99_Tools/PacketGenerator/)가 [GenPackets.cs](../../98_Shared/Protocol/Generated/GenPackets.cs)를 만든다. 생성 코드의 수동 편집으로 계약을 변경하지 않는다.

## 변경 절차와 소비자

PDL 수정 → 생성기 실행 → 생성 diff와 [ProtocolVersion](../../98_Shared/Protocol/ProtocolVersion.cs) 확인 → 서버·클라이언트 핸들러·봇 점검 → 양쪽 빌드·통신 검증 순으로 진행한다. 실행 명령과 DLL 복사는 [개발 안내](../operations/DEVELOPMENT.md)를 따른다.

패킷 ID를 재사용하거나 정의 순서를 바꾸지 않는다. 필드 끝 추가도 기존 읽기 코드에 영향을 준다. 버전 일치로 연결을 제한하는 현재 핸드셰이크와 새 동작을 함께 검증한다. 전송의 프레임 길이 검사는 [서버](../../02_Server/Network/FrameValidator.cs)와 [클라이언트](../../04_ClientNet/FrameValidator.cs)에 각각 있다.

## 서버 연결 종료

서버 Network.Session.Disconnect는 최초 호출만 종료 알림을 수행하고 정상 경로에서 callback → socket Shutdown → Close → 송신 큐·pending list 정리 순서를 유지한다. endpoint 조회나 callback이 실패해도 transport 정리를 시도한 뒤 원래 예외를 전파하며, 정리·진단 출력의 실패가 그 예외를 가리지 않는다. Shutdown·Close 실패는 기존처럼 무시하고 다음 정리를 진행한다. 클라이언트 종료 정책과 진행 중 I/O의 전체 수명은 이 계약의 변경 범위가 아니다.

## 타입과 권한

공유 공식·스탯 타입은 양쪽이 읽는 계약이다. 서버 권위는 판정을 서버가 한다는 뜻이며, 타입을 서버 프로젝트에만 두라는 뜻은 아니다. `PlayerStats`·`Formulas`는 [Shared/GameData/Combat](../../98_Shared/GameData/Combat/)에 있고 적용은 서버 시스템에서 수행한다.

Shared·ClientNet은 .NET Standard 2.1을 대상으로 한다. .NET 10 서버 타입을 옮길 때 `init`·`record`의 `IsExternalInit` 같은 런타임 차이를 확인한다. 과거에는 CS0518 오류를 관찰했고, setter/factory로 표현하거나 필요한 호환 타입을 명시하는 선택지가 있었다. 현재 프로젝트가 특정 shim을 제공한다고 추측하지 않는다.

문서에 경로나 자동 처리를 추가하면 실제 파일·호출·빌드 설정을 함께 확인한다. 디렉터리나 예정 기능이 문서에만 존재했던 과거 사례를 현재 구현의 근거로 사용하지 않는다.

[이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/shared/_index.md)에 공유 타입 이동·런타임 호환성·문서와 코드 불일치의 사례와 당시 판단을 보존한다.
