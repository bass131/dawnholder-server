# HP와 공격 이벤트 — M4.7-hp-sync-and-combat-events

원래 작업 묶음: `01_Phases/youngho/M4.7-hp-sync-and-combat-events`. 주영역은 protocol이며 관련 영역은 server, client이다.

## 문제와 변경

HP 변경 이벤트와 공격 시작·명중 이벤트를 분리했다. rate-limit·rewind 검사는 유지하고 HUD 고착·허공 공격 표시 문제를 다뤘다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-99cbd95311de"></a>[Phase 01: 프로토콜 신설 (v10 토대)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/01-protocol-packets.md)<br>완료 표기된 작업 정의; 상태: done | HP갈래·공격갈래가 둘 다 의존하는 v10 프로토콜 토대를 깐다. PDL append-only, 기존 enum 시프트 0. |
| <a id="source-b275cd745428"></a>[Phase 02: 서버 HP 송신 (HP갈래)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/02-server-hp-send.md)<br>계획; 상태: in-progress | 플레이어 HP가 변할 때만 권위 이벤트 `S_PlayerHp`를 송신 — 표시 미러(M4.5 임시) 제거의 서버 본체.<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-ae848ffd19b5"></a>[Phase 03: 서버 공격 모델 정비 (공격갈래)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/03-server-combat-model.md)<br>계획; 상태: pending | 공격 스윙(연출)을 명중(데미지)에서 분리 — 허공 스윙 허용. 데미지 모델(단일 타겟 AABB)은 불변, 연출/이벤트만 명중에서 뗀다.<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-329961fcbc73"></a>[Phase 04: 클라 HP 신뢰 경로 (HP갈래)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/04-client-hp-trust.md)<br>계획; 상태: in-progress | 서버 권위 `S_PlayerHp`를 신뢰해 HUD 갱신 + M4.5 표시 미러(PlayerStats.MaxHp 추측) 제거.<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-d2486bc6c4da"></a>[Phase 05: 클라 공격 입력+예측+원격 연출 (공격갈래)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/05-client-combat.md)<br>계획; 상태: pending | 허공 스윙 허용(입력 시 항상 스윙 + 로컬 Attack 예측) + 원격 플레이어 공격(Mage 투사체/근접 스윙) 연출.<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-46a2a3bc4e46"></a>[Phase 06: 회귀 + 마감](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/06-regression-and-close.md)<br>완료 표기된 작업 정의; 상태: done | v10 구조급(HP 동기화 + 공격 모델 정비) 전체 회귀 입증 + 마일스톤 마감. |
| <a id="source-7079446c42f7"></a>[M4.7 — HP 동기화 + 공격 모델 정비 마일스톤 마감 보고](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/_milestone-DONE.html)<br>결과 기록; 상태: 명시 없음; 2026-06-09 | 🎯 무엇을 만들었나 — 두 구조급 결함을 한 묶음으로 봉합했다. ① 플레이어 HP 권위 동기화 — 변할 때만<br>원문 발췌 [본문]: dotnet test green(479/0/4 skip) + 신규 봇 3종 PASS + 기존 봇(EmergencyCombat·BossFight) 회귀 0<br>원문 발췌 [본문]: ProtocolVersion 9→10(S_PlayerHp 21 + S_PlayerAttack 22) · 클린빌드 0/0 · test 479/0/4 · 신규 봇 3종 + 기존 봇 회귀 0 ·<br>제약·후속 [본문]: 479 통과/0 실패/4 skip(WSL2 = ADR-029) + 신규 헤드리스 봇 3종 PASS |
| <a id="source-f6b0a82b42a5"></a>[M4.7 — HP 동기화 + 공격 모델 정비 마일스톤 기록](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/_milestone-DONE.md)<br>결과 기록; 상태: done; 2026-06-09 | M4.7 완전 마감 (6 Phase + Play 봉합 2건, PR #83 HP갈래 + #84 공격갈래·Play봉합 + 본 마감 PR).<br>원문 발췌 [AC 검증 결과]: 마일스톤 완료 조건 대조 (2026-06-09 세션27, WSL2 = ADR-029 표준 경로, 메인 직접 실측):<br>원문 발췌 [AC 검증 결과]: [x] `dotnet test` green — 479 통과/0 실패/4 skip(Total 483, Duration 1m41s)<br>제약·후속 [이월 명시 (➡️ 다음)]: 다음 마일스톤 가닥 (사용자): 외관·연출(배경/컷신/NPC) 또는 M4.8 원거리 전투 모델 또는 M5 Persistence(LocalDB Linux + GenPackets Write 풀링 + Serilog/DI) |
| <a id="source-f47b465419b9"></a>[M4.7 — v10 구조급 (HP 동기화 + 공격 모델 정비)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.7-hp-sync-and-combat-events/_milestone-plan.md)<br>계획; 상태: planned; 2026-06-09 | > 선행: M4.6 완전 마감. 설계 근거: 승인된 plan(plan-mode `delegated-prancing-gray.md`) — Explore 2건(HP 동기화 / 공격 이벤트) + Explore 1건(공격 스윙 게이팅) + Plan 에이전트 설계 + 사용자 결정 3건(방향 v10 / HP=전용 이벤트 / 허공 스윙 동승).<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다.<br>제약·후속 [➡️ 다음 마일스톤]: M4.6 이월 흡수: `ActionFsm↔Fsm` 네이밍 통일 / harness REVIEW_CHECKLIST.md 봉합 |
