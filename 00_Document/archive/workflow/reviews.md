# 당시 코드·운영 검토

같은 관심 영역의 과거 자료를 모았다. 서로 다른 시점의 주장과 결과는 원문별로 구분한다.

## 문제와 변경

아래는 현재 실행 지침이 아니라 과거 기록이다. 원문에 있던 승인·모델 배정·보고 양식을 현재 규칙으로 적용하지 않는다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-90db90fcf462"></a>[Harness Review Follow-up 1/5 — MessagePack 잔재 정정](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-19-harness-review-followup-1of5.md)<br>검토·조사; 상태: 명시 없음; 2026-05-19, 2026-05-18 | 기준 결정: `00_Document/ADR/tech-stack/ADR-002-tcp-pdl.md` |
| <a id="source-6ec819ecedc5"></a>[Cross-Review — 2026-05-21 — M3.5 Phase 06 atomic 발효 + 후속 봉합](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-21-cross-review-m3.5-phase06.md)<br>검토·조사; 상태: 명시 없음; 2026-05-21 | 양쪽 통과 = 헌법 절대 원칙 5개 본질 / ADR-022 본문 10개 결정 commit 박힘 / CHANGELOG [H] 풀세트 / 격리 폴더 atomic 정리 / inkyu/ 신설<br>제약·후속 [γ 비교 분석 — 합 9건]: dotnet test 실측 &#124; ❌ 본 머신 SAC &#124; ⏸️ Codex 환경도 sandbox 실패 &#124; 양쪽 미수행 |
| <a id="source-ec9002748f2a"></a>[Cross-Review — 2026-05-22 — M3.6 plan + Phase 01 baseline](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-22-cross-review-m3.6-plan.md)<br>검토·조사; 상태: 명시 없음; 2026-05-22, 2026-05-19 | 5. Phase 03 ask 매처 측정 데이터 부족 → M4 1주차 후속 측정 결정 박음<br>원문 발췌 [🟡 한쪽만 잡음 → 본인 결정 권유]: β3 Unity batchmode/EditMode test → Phase 05 정의 강화 (required 승격)<br>원문 발췌 [옛 학습 정합]: 🆕 codex-cloud-test-environment-value-confirmation (★★ 후보) — Codex 환경 dotnet test 170 passed / 46초 = 본 머신 SAC On 차단 회피 가치 *측정 정량 실증<br>제약·후속 [β — Codex CLI 결과 (코드 직접 접근 + dotnet test 재실측)]: β3 &#124; Client audit dotnet build로 안 덮임 — `03_Client/Assets/Tests/EditMode/Dawnholder.Client.Tests.EditMode.asmdef` 실재. |
| <a id="source-92051c6c8921"></a>[하네스 자체 점검 — 2026-05-24 — scope=all](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-24-harness-review-all.md)<br>검토·조사; 상태: 명시 없음; 2026-05-24 | 🔴 결함 2건 (둘 다 메인 세션 직접 검증 — false-promise 패턴 재범)<br>제약·후속 [🔴-1 [축 1 / 헌법 #4] `shared-discipline-guard.sh… (뒤 내용은 원문)]: line 81-82 `grep -F "$GEN_PACKETS"`는 `Protocol/GenPackets.cs`가 `Protocol/Generated/GenPackets.cs`의 부분문자열이 아니므로 영영 매칭 실패. |
| <a id="source-e50058de61de"></a>[하네스 자체 점검 — 2026-06-26 — scope=all](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-06-26-harness-review-all.md)<br>검토·조사; 상태: 명시 없음; 2026-06-26, 2026-06-18 | > 읽기 전용 점검 — 본 문서가 유일 산출. 결함은 *제안*만, 봉합은 영호 결정. |
