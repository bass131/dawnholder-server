### ADR-011: 기존 ServerDev 코드 부분 채택 (시나리오 B)
**날짜**: 2026-05-06 — **2026-05-11 PacketGenerator 후속 박음**
**상태**: 채택됨
**결정**: 본인이 4월에 학습 목적으로 작성한 `C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server`의 코드 일부를 채택. **채택**: ServerCore (Listener/Session/RecvBuffer/SendBuffer/JobQueue), PacketGenerator + PDL.xml, DummyClient. **새로 작성**: Server 게임 로직 (GameRoom/ClientSession 등), Unity 클라이언트 전체 (기존 코드는 3D였고 본 프로젝트는 2D).
**이유**: ServerCore는 4월에 디버깅 끝낸 검증된 SocketAsyncEventArgs 패턴 → 시간 절약. PDL 시스템은 면접 임팩트 큰 자체 구현물. 게임 로직(GameRoom 등)은 헌법 #1(Server Authority) 위반 — 클라가 보낸 좌표 무검증 적용 — 이라 새로 짜야 함. 6월 캡스톤 C 옵션(2인 movement) 6주 안에 가려면 시간 절약 필요.
**트레이드오프**: 본인 코드 빚 일부 안고 시작. 발견된 PacketGenerator 버그(`PacketFormat.cs` 178번 줄 하드코딩 `C_Chat`, `chatLen` 2건) ✅ Phase 06에서 정정 완료 (commit `03994b0`). 기존 코드의 한글 주석/네이밍 컨벤션이 새로 짜는 부분과 미묘하게 안 맞을 수 있음 → 이주 시 정리. **참고**: 기존 `GameRoom.Move()`의 Server Authority 위반은 **학습 일지 1호 후보** — "처음엔 이렇게 짰다 → 헌법 적용해 이렇게 진화" 면접 서사로 활용.
