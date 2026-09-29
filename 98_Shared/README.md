# 공유 계약

서버·Unity·봇이 함께 사용하는 패킷, 게임 데이터·공식, 맵 형식을 담는다. 적용·판정 권한은 서버에 있다.

- [ProtocolVersion](Protocol/ProtocolVersion.cs): 핸드셰이크 버전.
- [GenPackets](Protocol/Generated/GenPackets.cs): PDL 생성 코드; 직접 편집하지 않음.
- [Physics](GameData/Physics.cs): 공유 물리 계산.
- [Combat](GameData/Combat/): 스탯·스킬 데이터·공식.
- [Map](GameData/Map/): 맵 데이터 형식.

패킷 원본은 [PDL](../99_Tools/PacketGenerator/PDL.xml)이다. [변경 계약](../00_Document/domains/protocol.md)에서 ID·버전·런타임 호환성과 양쪽 소비자를 확인한다. [개발 안내](../00_Document/operations/DEVELOPMENT.md)의 Windows 빌드는 Unity Plugins/Shared에 DLL을 복사한다.
