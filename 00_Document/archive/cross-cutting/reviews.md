# 당시 코드·운영 검토

같은 관심 영역의 과거 자료를 모았다. 서로 다른 시점의 주장과 결과는 원문별로 구분한다.

## 문제와 변경

아래는 현재 실행 지침이 아니라 과거 기록이다. 원문에 있던 승인·모델 배정·보고 양식을 현재 규칙으로 적용하지 않는다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-d1638a3deafc"></a>[Cross-Review — 2026-05-29 — 전체 프로젝트 Harness + Code + Architecture 감사](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-05-29-cross-review-full-audit.md)<br>검토·조사; 상태: 명시 없음; 2026-05-29 | 가변 dt prediction (β5) &#124; 의도된 drift (SnapThreshold 1.5f 문서화) &#124; 결함 (fps 발산) &#124; Play 모드 실측 후 결정. M4.3 "reconcile drift 튜닝" 이월과 동일 항목.<br>원문 발췌 [검증 결과 (A1~A6 + B1) — Claude 직독 + Codex round… (뒤 내용은 원문)]: B1/β2 &#124; 클래스 HP 권위 전투 미적용 &#124; CONFIRMED &#124; HIGH → 봉합<br>원문 발췌 [봉합 4건 (이번 패스 적용)]: 1. B1 클래스 HP — `PlayerEntity` 생성자가 `MaxHp=Stats.MaxHp; Hp=Stats.Hp` 초기화 (옛 `=100` 하드코딩 제거). `AddPlayerWithId`는 ctor가 MaxHp 확정 + 직후 Hp=이월값 → migration 정합.<br>제약·후속 [검증 산출물]: 봉합 미적용 (이월 사유 명시): β10 MoveSpeed(shared+client 결정론 → Play 실측 필요=본인 분담), β7 reconnect(라이프사이클+Play), β1 PDL-ID(ADR), β9/β4(LOW → M4.3 cheat-flag). |
| <a id="source-ba4e3f260786"></a>[무인 리팩토링 스윕 (`--dry-run`) — 2026-06-12](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-06-12-refactor-sweep-dryrun.md)<br>검토·조사; 상태: 명시 없음; 2026-06-12 | 모드: `--dry-run` (진단만, 자동수정 0, commit 0) |
| <a id="source-c6ae21f1e911"></a>[무인 리팩토링 스윕 — 2026-06-13 (commit 모드 v1 첫 실전)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-06-13-refactor-sweep.md)<br>검토·조사; 상태: 명시 없음; 2026-06-13, 2026-06-12 | 브랜치: `refactor/auto-20260613` (출발: `main`)<br>원문 발췌 [2. 적용 commit (파일별 atomic, 9개)]: 1 &#124; `677d683` &#124; shared &#124; §6.2 &#124; ✅ &#124; Constants 마일스톤 토큰 4건 (SnapshotTick/ExternalImpulseEpsilon/BossTelegraph×2) &#124; test 561/0<br>원문 발췌 [5. 테스트 / 회귀]: baseline → 최종: WSL2 build+test, 561/0 → 561/0 (비감소 ✅, 신규 fail 0). build 0 error.<br>제약·후속 [6. 실패 미적용 — 0건]: 회귀 red·이분 격리 없음. 16건 전부 1회 게이트 통과. |
| <a id="source-73b628e3c907"></a>[아키텍처 논리 감사 — Dawnholder (2026-06-19)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/2026-06-19-architecture-logic-audit.html)<br>검토·조사; 상태: 명시 없음; 2026-06-19 | 📌 한눈 결론 (TL;DR)<br>제약·후속 [본문]: SkillId↔ActionKind↔CharacterClass 매핑 분산, AnimState 정의역, MeleeAction Knight/Mage 하드코딩, bot Program.cs if-chain dispatch = 미검증 low. |
| <a id="source-c0330cc19f23"></a>[reviews/ — 과거 리뷰 기록 인덱스](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/reviews/INDEX.md)<br>검토·조사; 상태: 명시 없음; 2026-05-18, 2026-05-19 | > 전부 *과거 기록*이라 사실상 동결 — 결정 자체는 `../ADR/INDEX.md`·`../policies/INDEX.md`에 박혀 있고, 여기는 *그 결정에 이르는 검토 과정*입니다. |
