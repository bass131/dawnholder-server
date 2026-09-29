# 플레이테스트 후속 조정 — M6-playtest-polish

원래 작업 묶음: `01_Phases/youngho/M6-playtest-polish`. 주영역은 client이며 관련 영역은 server이다.

## 문제와 변경

애니메이터·렌더 순서·HUD·NPC 대화를 정리하고 플레이테스트에서 발견한 표현과 진행 문제를 수정했다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-f2072262a1f4"></a>[Phase 01: Animator ExitTime 보정 (보스 피격복귀 + 일반몹 공격)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/01-animator-exit-time.md)<br>계획; 상태: pending | 보스 피격 복귀 후 공격 모션 완주 + 일반몹 공격 모션 완주 후 IDLE 복귀하도록 Animator 전이 Exit Time 조정<br>원문 발췌 [🧪 테스트]: 서버측 무변경이면 WSL2 회귀 644/0/5 유지 확인 |
| <a id="source-f30d3abed7d2"></a>[Phase 02: 렌더 소팅 레이어 정립](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/02-render-sorting-layers.md)<br>계획; 상태: pending | SortingLayer를 BG→마을지형→세부지형→NPC→Player→UI 순으로 정의하고 prefab/scene/코드에 일관 배정<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-a036a3bf3b6c"></a>[Phase 03: 상단 HUD 재정비 (파티 HUD 이동 + 퀘스트 텍스트/패널 확장)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/03-top-hud-rework.md)<br>계획; 상태: pending; 2026-06-15 | 파티 멤버 HUD를 퀘스트 mockup 위치로 이동(mockup 제거) + 퀘스트 이름/목표/카운트 3줄 표시 + 뒷 패널 확장<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
| <a id="source-843799e9d45e"></a>[Phase 04: NPC 대화 패널 재구축](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/04-npc-dialog-rebuild.md)<br>계획; 상태: pending | NpcDialogPanel을 BuildRuntime 패턴으로 재구축 — Menu_Button.png 배경 + NPC 초상화 + 대사 텍스트, CombatBootstrap 통합<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다.<br>제약·후속 [⏪ 사전 조건]: [ ] 영호 미커밋 NPC 초상화 reorg(Portrait/ 폴더)가 M6 브랜치에 이월돼 있음 (충족 — working tree) |
| <a id="source-e144f92e2ae2"></a>[Phase 05: 통합 플레이테스트 + 클로즈아웃](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/05-integration-playtest-closeout.md)<br>계획; 상태: pending | 6개 폴리시 항목 통합 검증 + WSL2 회귀 게이트 + -DONE.md/HTML 박제 + (영호 GO 시) PR/머지<br>원문 발췌 [📝 작업 내용]: [ ] WSL2 sync+build+test 회귀 게이트 (baseline 644/0/5)<br>원문 발췌 [🧪 테스트]: 자동: WSL2 회귀 644/0/5 |
| <a id="source-4919bdec0500"></a>[M6 — 플레이테스트 폴리시 (DONE)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/_milestone-DONE.html)<br>결과 기록; 상태: 명시 없음; 2026-06-15 | 파티 HUD→퀘스트 위치 + 이름/목표/카운트 3줄. 클라 로컬 콘텐츠(§1)<br>원문 발췌 [본문]: WSL2 회귀(.NET): ✅ 645 passed / 0 failed / 5 skipped, build 0 error (baseline 644 +1). 서버 변경(2R·4R) 각 라운드 게이트 통과, 7R/8R 클라 전용이라 .NET 결과 불변.<br>제약·후속 [본문]: 알려진 flaky: CombatSmoke_ZeroLag 전체 실행 시 간헐 timeout(격리 pass, 환경성) |
| <a id="source-630e3b6aecfb"></a>[M6 — 플레이테스트 폴리시 완료 기록](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/_milestone-DONE.md)<br>결과 기록; 상태: done; 2026-06-15 | M6 플레이테스트 폴리시 완료 — Animator/렌더소팅/HUD/NPC대화 4 Phase + 통합 플레이테스트 8라운드(보스·골렘·파티UI·미니맵·퀘스트배너 시트애니·엔딩 정적). 프로토콜 무변경(v16), main 머지 준비.<br>원문 발췌 [5단계 보고]: 테스트 결과 — WSL2 회귀 645 passed / 0 failed / 5 skipped(build 0err). Unity 컴파일 0 error(매 커밋 link-check). 영호 8라운드 직접 Play 육안 검증. 알려진 flaky=CombatSmoke(격리 pass).<br>원문 발췌 [AC 검증 결과]: Phase 파일 완료조건 = 6 피드백 항목 통합 검증 + WSL2 회귀 green + 컴파일 0err.<br>제약·후속 [5단계 보고]: 테스트 결과 — WSL2 회귀 645 passed / 0 failed / 5 skipped(build 0err). Unity 컴파일 0 error(매 커밋 link-check). 영호 8라운드 직접 Play 육안 검증. 알려진 flaky=CombatSmoke(격리 pass). |
| <a id="source-795909dd3102"></a>[M6 — 플레이테스트 폴리시](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M6-playtest-polish/_milestone-plan.md)<br>계획; 상태: pending; 2026-06-15 | M5(파티/퀘스트) main 머지(#111, ProtocolVersion v16) 직후 인터랙티브 2차 플레이테스트에서<br>계획 자료이며 완료 여부는 별도 결과 기록을 확인한다. |
