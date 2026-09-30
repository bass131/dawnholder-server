# 클라이언트

Unity는 입력·예측·화면 적용을 담당한다. 최종 게임 판정은 서버가 소유한다. 전송 라이브러리는 `04_ClientNet`, Unity 연결은 [Network](../../03_Client/Assets/Scripts/Network/), 예측은 [Prediction](../../03_Client/Assets/Scripts/Prediction/)에 있다.

## 수정 흐름

- 패킷 수신과 Unity 메인 스레드 적용을 분리한다. [MainThreadDispatcher](../../03_Client/Assets/Scripts/Network/MainThreadDispatcher.cs)와 해당 핸들러를 확인한다.
- 로컬 이동은 `LocalPlayerMovement`·`PlayerPredictor`·`InputHistory`, 원격 표현은 서버 스냅샷·보간 경로를 확인한다.
- 씬 전환과 연결 종료를 혼동하지 않는다. `NetworkService`, `SceneRouter`, bootstrap의 객체 수명을 함께 확인한다.
- 입력·HP·파티 등 UI는 서버 상태의 표시와 클라이언트 임시 상태를 구분한다.

## 파티 초대 요청과 구독

[PartyInvitePopup](../../03_Client/Assets/Scripts/UI/PartyInvitePopup.cs)은 클릭음·버튼 의도·표시/숨김과 이벤트 구독을 맡고, [PartyInviteResponseCommand](../../03_Client/Assets/Scripts/UI/PartyInviteResponseCommand.cs)는 실행 시 현재 상태/세션 조회·응답 패킷 구성·SendIntent 호출·pending 소비를 맡는다. void SendIntent 정상 반환은 실제 송신 접수나 서버 가입 확정이 아니다. 요청 처리 중 예외가 나면 뒤의 pending 소비와 숨김을 진행하지 않으며 membership은 기존 서버 update로 갱신한다.

팝업은 실제 구독한 PartyState 참조를 보관하고 disable/destroy 때 같은 source에서 해제한다. 파괴된 Unity 객체의 null 판정과 CLR 참조를 구분해 남아 있는 managed event도 정리한다. 활성 callback의 현재 Instance 조회는 유지하며 source 교체의 자동 재구독·pending 재표시는 제공하지 않는다. 변경할 때 [응답 계약 테스트](../../03_Client/Assets/Tests/EditMode/PartyInviteResponseContractTests.cs), [command 경계 테스트](../../03_Client/Assets/Tests/EditMode/PartyInviteResponseCommandTests.cs), [구독 수명 테스트](../../03_Client/Assets/Tests/EditMode/PartyInvitePopupBindingTests.cs)를 함께 확인한다.

## 원격 보간

RemoteInterpolationState가 snapshot buffer·서버 시간축 렌더 시계·보간 계산을 단독 소유하며 명시적인 deltaTime으로 진행한다. RemoteEntity는 기존 공개 component API를 유지하고 Unity 프레임의 결과를 Transform에 적용한다. Initialize는 위치를 즉시 적용하고 시계를 초기화하며, SnapInterpolation은 현재 Transform을 유지한 채 buffer/clock을 초기화하고 ClearBuffer는 buffer만 지운다. 텔레포트 도착 callback은 새 snapshot 적재 직후 한 번 호출한다. registry·적 VisualFootOffset과 프레임당 catch-up의 수치 의미는 유지한다.

## 에셋을 다룰 때

기존 prefab을 저장하기 전에 추적 여부와 사용자 변경을 확인한다. 미추적 에셋은 Git으로 복원할 수 없으므로 덮어쓸 대상의 사본을 먼저 확보한다. `.meta`·GUID와 직렬화 값, Resources 등의 문자열 경로를 함께 보존한다. 이는 과거 BackGround prefab 덮어쓰기 사례에서 확인된 복구 한계다.

Unity 버전은 [ProjectVersion.txt](../../03_Client/ProjectSettings/ProjectVersion.txt)의 버전과 revision을 함께 확인한다. 과거에는 `6000.4.1f1`이라는 같은 표기에서 `8535861f39e1`과 `336a400b9ea2` revision 차이를 관찰했다. 이 숫자는 현재 설치 기준이 아니라 과거 사례다.

입력 ack·임펄스 예측의 경계는 [영역 간 경계](cross-cutting.md)에 있다. [이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/client/_index.md)은 에셋 사고와 2026-05-16 버전 관찰을 보존한다.
