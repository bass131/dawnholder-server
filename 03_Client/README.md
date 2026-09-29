# Unity 클라이언트

입력·로컬 예측·서버 상태 표시·음향을 담당한다. 게임 판정은 서버가 소유한다.

- [GameEntryPoint](Assets/Scripts/Bootstrap/GameEntryPoint.cs): 클라이언트 시작 흐름.
- [NetworkService](Assets/Scripts/Network/NetworkService.cs): 연결 수명주기.
- [UnityClientSession](Assets/Scripts/Network/UnityClientSession.cs): 패킷 수신과 전달.
- [LocalPlayerMovement](Assets/Scripts/Prediction/LocalPlayerMovement.cs): 입력·예측 연결.
- [PlayerPredictor](Assets/Scripts/Prediction/PlayerPredictor.cs): 예측·재조정.

[클라이언트 계약](../00_Document/domains/client.md)에서 에셋·스레드·상태 경계를 확인한다. 패킷 변경은 Shared·서버·ClientNet과 함께 검증한다. Unity 버전과 빌드 DLL 부작용은 [개발 안내](../00_Document/operations/DEVELOPMENT.md)에 있다. `.meta`·GUID·직렬화 값을 보존하며 .NET 통과를 Unity 플레이 확인으로 대체하지 않는다.
