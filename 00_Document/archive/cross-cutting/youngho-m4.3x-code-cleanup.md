# 코드 정리 기준 — M4.3X-code-cleanup

원래 작업 묶음: `01_Phases/youngho/M4.3X-code-cleanup`. 주영역은 cross-cutting이며 관련 영역은 client, server이다.

## 문제와 변경

주석·데이터 엔티티·명명·폴더 경계 기준을 보강하고 기존 구현의 책임을 정리하는 작업이다. 계획과 완료 기록을 구분한다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-b0f833e68760"></a>[Phase 01: CODE_CONVENTION 보강 (정리 기준 확정)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.3X-code-cleanup/01-convention-augment.md)<br>계획; 상태: pending; 2026-05-30 | CODE_CONVENTION에 §6 주석 정책 + §2.2 데이터엔티티/God class 구분 + naming·폴더 계층 규칙 신설 + §5 강제(reviewer 축/SubAgent) 반영 — 전체 정리의 기준 확정<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-1cfd56af3702"></a>[Phase 02: 주석+naming 정리 — 서버측](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.3X-code-cleanup/02-comment-cleanup-server.md)<br>계획; 상태: pending; 2026-05-30 | 02_Server + 98_Shared 서버측 전체 주석을 §6 기준으로 95% 노이즈 제거(코드 0변경) + §3.3 naming 위반 rename. 빌드/테스트 green 가드<br>원문 발췌 [🎯 목표]: `02_Server/`(Network + GameServer + Tests) + `98_Shared/` 서버측의 주석을 §6 기준으로 정리한다. 자명한 재진술·역사 박제·폐기된 사고과정을 제거하고, 비자명한 안전 결정 근거(5%)만 남긴다.<br>원문 발췌 [검증]: [ ] `dotnet test --no-build` — 322 그대로 green (주석/rename은 동작 불변) |
| <a id="source-7654fd26c586"></a>[Phase 06: 회귀 검증 + 강제 적용 + 마감](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.3X-code-cleanup/06-regression-enforce-close.md)<br>계획; 상태: pending; 2026-05-30 | 정리 전체(01~05) 통합 회귀 검증 + ADR-028 §5 강제(reviewer 축/SubAgent 주석 규칙) 본인 적용 확인 + PR 머지 → M4.3 애니 08b 재개 좌표 복원<br>원문 발췌 [📝 작업 내용]: [ ] 전체 회귀 — `dotnet build Dawnholder.slnx --no-incremental` 0 error + `dotnet test --no-build` 322(+) green + 헤드리스 봇 전 시나리오<br>제약·후속 [📝 작업 내용]: [ ] work-pin 복원 — M4.3X MERGED + M4.3 애니 08b 재개 좌표 (보류했던 08b~12 복원) |
| <a id="source-854baa4441ae"></a>[M4.3X — 코드베이스 정리 (Code Cleanup)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.3X-code-cleanup/_milestone-plan.md)<br>계획; 상태: in_progress; 2026-05-30, 2026-06-10 | > 사용자 명시 결정: 발표(2026-06-10)보다 기반 정리 우선 (ADR-028 "기반 부채 > 발표 데모" 연장)<br>원문 발췌 [✅ 마일스톤 완료 조건]: [x] build 0 error + test 338 green (동작 보존) + Unity Play 실측(맵전환 3번 + 에러 0)<br>원문 발췌 [갱신 이력]: 2026-05-30 — 실행 완료: Codex 갭 4종 + §6 정책 신설 + workflow 7도메인 전체 주석 전수(124 .cs, −2313/+949) + GenPackets drift 봉합. build 0/test 338 green, reviewer 🔴0. 한 묶음 커밋.<br>제약·후속 [🚫 명시적으로 안 한 것]: 포매팅(.editorconfig/Roslyn) — M4.4 이월. |
