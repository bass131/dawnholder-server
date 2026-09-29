# 클라이언트 전송 라이브러리

Unity API에 의존하지 않는 .NET Standard 2.1 TCP 라이브러리다. Unity와 헤드리스 봇이 사용한다.

- [ClientSession](ClientSession.cs): 세션·프레이밍·전송 콜백.
- [Connector](Connector.cs): 연결 시작.
- [FrameValidator](FrameValidator.cs): 프레임 헤더 검사.
- [RecvBuffer](RecvBuffer.cs): 수신 데이터 누적.
- [프로젝트 설정](Dawnholder.Client.Net.csproj): 타깃·Unity DLL 복사.

프레임 계약 변경은 서버 Network, 패킷 변경은 Shared와 함께 확인한다. [프로토콜 계약](../00_Document/domains/protocol.md), [검증 안내](../00_Document/operations/DEVELOPMENT.md)를 따른다. Windows 빌드는 Unity Plugins/ClientNet에 DLL을 복사한다.
