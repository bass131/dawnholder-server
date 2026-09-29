# 당시 코드·운영 검토

같은 관심 영역의 과거 자료를 모았다. 서로 다른 시점의 주장과 결과는 원문별로 구분한다.

## 문제와 변경

아래는 현재 실행 지침이 아니라 과거 기록이다. 원문에 있던 승인·모델 배정·보고 양식을 현재 규칙으로 적용하지 않는다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-20954c707871"></a>[M3 Phase 02 — Codex Review](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-18-m3-phase-02-codex-review.md)<br>검토·조사; 상태: 명시 없음; 2026-05-18 | Phase 02의 핵심 목표인 "ProtocolVersion 약속을 실제 코드로 봉합"은 대부분 달성됐다. 다만 현 상태를 그대로 Phase 완료로 닫으면, mismatch reason 전달과 Unity first-packet 보장이라는 두 지점이 데모 직전 디버깅 리스크로 남는다.<br>원문 발췌 [검증 범위]: `dotnet test Dawnholder.slnx --nologo --no-build --filter "FullyQualifiedName~HandshakeHandlerTests&#124;FullyQualifiedName~PacketRoundTrip"` → 54 passed<br>원문 발췌 [완료 조건 검증]: 핸들러 단위 테스트 3건 통과 &#124; PASS &#124; 전체 테스트 135/0/1, 필터 테스트 54/0/0 통과.<br>제약·후속 [완료 조건 검증]: 버전 mismatch 시 즉시 disconnect &#124; CONCERN &#124; 단위 테스트상 disconnect 호출은 PASS. 단, `S_HandshakeResult` reason이 실제 네트워크에서 도착하는지는 미검증. |
| <a id="source-61abaf0d7615"></a>[M3 Phase 03+04 — Codex Review (γ 방식 5회차)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-18-m3-phase-03-04-codex-review.md)<br>검토·조사; 상태: 명시 없음; 2026-05-18 | Phase 03+04는 서버 런타임 기준으로 진행 가능하다.<br>원문 발췌 [1. dotnet test 재실측 + 회귀 0 확정]: 전체: `160 passed / 0 failed / 1 skipped / 161 total`<br>원문 발췌 [1. dotnet test 재실측 + 회귀 0 확정]: 관련 필터: `13 passed / 0 failed / 0 skipped`<br>제약·후속 [권장 후속 조치]: 3. `InternalsVisibleTo("GameServer.Tests")`는 지금은 보류하고, 분기 많은 handler가 들어오는 시점에 추가한다. |
