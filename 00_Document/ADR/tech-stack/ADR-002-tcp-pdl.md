### ADR-002: Raw TCP + 자체 PDL + 코드 생성기
**날짜**: (Harness 셋업일) — **2026-05-06 직렬화 방식 갱신**, **2026-05-11 폴더 경로 정합**
**상태**: 채택됨 (대체: ADR-002 v1 "MessagePack")
**결정**: Mirror/FishNet 같은 HLAPI 대신 raw TCP + length-prefixed binary 사용. 직렬화는 MessagePack이 아니라 **자체 PDL(Packet Definition Language) XML + C# 코드 생성기**로. PDL.xml 단일 소스 → 양쪽(client/server)에 동일 패킷 클래스 자동 생성.
**이유**: 학습 목적이 네트워킹 깊이 이해. PDL이 단일 소스 역할을 해서 헌법 #4 ("복사-붙여넣기 금지") 강제. MessagePack 대비 wire format이 더 가볍고 메타데이터 0. 면접 임팩트: "Rookiss 강의 패턴 응용해 PDL 생성기 직접 구현". 본인이 4월에 이미 작성한 PDL 시스템(`99_Tools/PacketGenerator/`로 이주 완료)이 있음.
**트레이드오프**: 직접 짠 코드라 버그 가능 (생성기에서 발견된 하드코딩 버그 2건 ✅ Phase 06에서 정정 완료, commit `03994b0`). MessagePack의 schema evolution 같은 자동 호환성 없음 — packet ID 수동 관리 + Protocol.Version bump. JSON처럼 디버깅 쉽지 않음 (바이너리).
