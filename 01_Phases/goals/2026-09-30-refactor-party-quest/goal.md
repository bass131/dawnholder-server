# M2a — 파티 멤버십과 퀘스트 상태·통보 분리

상태: 완료·PR #134 병합. base `4c32c917ff68450174c9883951a8d02e0223d11d`, branch `feat/refactor-party-quest-ownership`. [M1c](../2026-09-29-refactor-client-entry/goal.md) PR #133의 독립 테스트·리뷰·최신 CI를 통과하고 조건부 승인으로 병합한 뒤 시작했다. [M2](../2026-09-29-refactor-domain-state/goal.md)의 첫 목표다.

## 선택 설계와 이유

Party는 멤버십·초대, Quest는 solo/party 진행·entity 해금 latch, QuestNotifier는 패킷 생성을 소유한다. Quest가 mutable PartyState를 변경하거나 GameWorld/PartyNotifier에 직접 의존하던 경계를 복사 조회값과 불변 결과값으로 바꾼다. 같은 world tick과 기존 큐를 유지하며 범용 이벤트 버스·새 thread·DI framework를 도입하지 않는다.

| 계약 | 선택 |
|---|---|
| 멤버십 조회 | `PartyMembership(PartyId, MemberIds)`는 생성 시 복사하고 readonly wrapper로 원본 배열을 노출하지 않는다. `PartyRegistry.GetMembershipByEntity(int)`가 조회 시점 값을 반환한다. |
| Quest 의존 | 생성자에 `Func<int, PartyMembership?>`를 주입한다. `_soloProgress`, `_partyProgress`, `_bossUnlocked`를 Quest만 변경하고 PartyState.KillCount를 제거한다. |
| 진행 결과 | `readonly record struct QuestProgressUpdate(RecipientEntityId, CurrentCount, TargetCount)`의 readonly 목록을 OnKill 및 DEBUG 완료가 반환한다. 이후 멤버십·진행 변경이 이전 결과를 바꾸지 않는다. |
| 송신 | `QuestNotifier.Send(world, updates)`가 기존 S_QuestUpdate/SendToEntity 순서를 유지한다. GameWorld의 기존 Quest job 본문에서 변경 결과를 즉시 송신하며 추가 job으로 미루지 않는다. |
| 해산 폐기 | PartyRegistry의 선택적 단일 `Action<int>` callback은 실제 Disband 성공 후 한 번 호출한다. GameWorld가 이를 Quest 큐의 `ForgetPartyProgress`에 연결한다. unknown/반복 해산은 무통보, cleanup은 멱등이다. |
| 관측 | cleanup 누수·raw count 검증이 필요하면 기존 InternalsVisibleTo 범위의 좁은 read-only 조회만 둔다. mutable 저장소를 테스트에 공개하지 않는다. |

## 보존 정책

1. Party.Tick → Quest.Tick 순서와 close/map 처리 순서는 유지한다. kill enqueue 순간이 아닌 **OnKill 실행 시점** 멤버십을 한 번 조회해 적립·해금·수신자를 결정한다.
2. 새 partyId는 진행 0, 가입 중 solo 진행은 남고 해산 후 복원된다. 공유 진행을 solo에 분배하거나 solo 값을 합산하지 않는다. raw는 계속 증가하고 표시만 target으로 clamp한다.
3. 임계 도달 시 현재 멤버를 해금한다. GetKillCount의 latch 우선 조회, 보스 방향 gate/역방향 자유, 세션 closing/owner/ID 계약을 유지한다. 해금은 현재 entity 메모리 수명이며 DB 영속화가 아니다.
4. 보스 사망의 전역 reset은 solo/party 진행만 초기화하고 latch는 유지한다. reset·가입·해산 자체는 추가 QuestUpdate를 보내지 않는다. kill→reset/reset→kill의 기존 큐 순서를 보존한다.
5. Leave는 기존 이전 멤버 모두에게 partyId=0을 통보한다. disconnect는 기존 초대 정리 후 해산하고 끊긴 본인을 제외한다. Leave에 없던 초대 정리 정책을 추가하지 않는다. 모든 해산 경로와 직접 Disband는 동일 cleanup hook을 거친다.
6. cleanup은 Quest 큐에서 수행한다. 앞선 kill도 변경된 현재 멤버십을 보므로 폐기 party 진행을 부활시키지 않는다. 새 partyId/solo/latch를 지우지 않는다.
7. DEBUG 완료는 raw를 target으로 대입하고 현재 멤버를 해금·통보한다. 기존 GameSession queued job/IsActiveSession 가드는 유지하고 world의 좁은 DEBUG helper가 즉시 결과를 전달한다. Release에는 포함하지 않는다.

제외: Shared/Unity/DLL/PDL/에셋 변경, DB schema·인증·저장·서비스 조작, M2b 스탯/이동 모델, 게임 정책 변경.

## 파일 소유와 인계

- 메인: goal/CURRENT·M2 분할·이전 목표 통합 기록·Git/PR/실행 조율.
- `client_flow_audit` 구현자: 기존 `02_Server/GameServer/Quest/QuestRegistry.cs`, `Party/{PartyState,PartyRegistry,PartyNotifier}.cs`, `Loop/GameWorld.cs`, `Sessions/GameSession.cs`의 DEBUG 직접 호출부. 신규 `Party/PartyMembership.cs`, `Quest/{QuestProgressUpdate,QuestNotifier}.cs`. PartyFlow는 읽기만, 불필요 API 제거는 하지 않는다.
- `db_scope_review` 독립 TestCode: 구현 쓰기 종료 후 기존 `GameServer.Tests/Party/{QuestKillCountTests,PartyRegistryTests,GameWorldPartyIntegrationTests}.cs`, `Integration/PartyQuestSmokeTests.cs`; 필요 시 신규 Party의 `QuestMembershipLifecycleTests.cs`, `QuestNotificationContractTests.cs`. 직접 API 소비자 `Integration/{MapTransitionIntegrationTests,BossGateSmokeTests}.cs`의 기존 OnKill 호출 적응도 추가 배정했다. 그 밖의 파일은 정확 경로를 main에게 보고하고 배정받는다.
- `refactor_scope_review`: 독립 source/TestCode 리뷰, 누적 HTML 단일 작성자. 구현·테스트와 같은 파일을 동시에 쓰지 않는다.

원시 결과는 `.backups/verification/2026-09-30-party-quest/`에 둔다. 선행 제안/리뷰/테스트 계획은 `.backups/reviews/2026-09-30-m2a-{api-proposal,design-review,test-plan}.md`이며 실행 증거가 아니다. 구현 후 독립 테스트를 작성하고 실행한다. worker WSL 제한 시 main이 명령만 대행하고 worker가 raw를 판정한다.

## 완료조건

- [x] 선택 설계·보존 정책·파일 소유를 구현 전에 확정했다.
- [x] Quest의 mutable PartyState/GameWorld/PartyNotifier 의존과 교차 진행 쓰기를 제거했다.
- [x] copied membership/notification 불변성, raw/clamp/latch/reset, solo 복원·재결성, 모든 해산 cleanup을 독립 TestCode로 검증했다.
- [x] 실제 world Party→Quest 순서·queued membership·kill/reset 순서, 정확한 패킷 수신자/순서/무통보를 확인했다.
- [x] 기존 보스 gate/PartyQuest socket 통합과 서버 전체 suite·Release 경계를 검증하고 Shared/Unity 변경 부재를 확인했다.
- [x] 독립 source/TestCode 리뷰·최신 head CI·크리티컬 이슈 부재 확인 후 이번 로드맵 조건부 승인으로 직접 병합했다.
- [x] HTML에 실제 diff와 검증/미실행 범위를 기록했다.

## 실제 결과와 다음 단계

production 기존6+신규3파일의 구현·정적 확인을 마쳤다. 정확 API와 독립 관측 seam은 `.backups/verification/2026-09-30-party-quest/implementation/summary.md`에 인계했다. Quest의 타 도메인 mutable 객체/송신 의존을 제거했고 PartyFlow 및 Shared/Unity/PDL은 변경하지 않았다. 독립 source/TestCode 리뷰에서 미해결 P0–P2는 없다(`review/{code,test-review}.md`).

- 서버 Debug 전체 suite: exit 0, **775 total / 770 pass / 0 fail / 5 기존 skip**. 이전 M1c 최종751 fullname 누락0, 신규24 모두 통과. 6개 기존 warning/0 error(`verification/server-suite.log`, `server-results/`).
- Release production 빌드 exit0, 기존4 warning/0 error(`verification/release-3.log`). 첫 launcher는 dotnet PATH와 CRLF 인수 문제로 빌드 전 실패했고 수정 후 성공했다. code 변경으로 숨기지 않았다.
- Release 테스트 필터 **1/1 pass**, 실제 Release assembly에 QuestRegistry.DebugCompleteQuest/GameWorld.CompleteQuestForDebug가 노출되지 않는 계약을 확인했다(`verification/release-test.log`, `release-results/`).
- 신규 테스트는 실제 world OnTick 및 원래 death hook을 reflection으로 호출한 후 notifier→직렬화된 packet→CapturedSession을 관찰한다. 가짜 callback만 통과시키거나 queued membership을 enqueue시점 snapshot으로 대체하지 않았다. 실제 socket smoke는 기존 전체 suite로 별도 검증했다. 새 fixture의 단위 캡처 테스트가 socket 통합이라는 뜻은 아니다.
- raw/clamp/latch·solo복원/newparty0·kill/reset순서·direct/Leave/실세션disconnect cleanup·정확수신자/무통보·불변값을 확인했다. 기존 통합 ServerFixture Listener 미정리 한계는 M3로 인계했다. suite 프로세스 종료 뒤 main이 port7777 listener 부재를 확인했다.
- Shared/ClientNet/Unity/PDL 변경0, 원본 DLL 복사와 Unity 재실행 없음. 서버 내부 변경은 socket suite로 검증했으며 별도 bot/수동플레이/DB 실행을 했다고 주장하지 않는다.

최신 PR head CI와 병합 결과는 통합 뒤 기록한다. 독립 raw 판정은 `verification/summary.md`에 둔다. 다음 [M2b](../2026-09-30-refactor-player-state/goal.md)는 M2a 통합 후 최신 main에서 Shared 스탯 정의·현재 HP·캡처/이동 값을 정리한다. DB 구현 권한으로 확대하지 않는다.

최종 통합: head `83eefc8750bc4fc3f9263b78c37c3b3481124868`, CI run `36601258878` / job `109518934198` SUCCESS 2026-09-29T16:58:38Z(775/770pass/0fail/5skip). 독립 테스트·리뷰와 최신 head 조건을 확인해 PR #134를 16:59:13Z 직접 squash 병합, main `a0c60943cc89d74830d826044bd8fb852a458e7b`. 크리티컬 이슈 없음. 이전 기록의 CI 준비/대기는 그 시점 상태이며 최종 결과는 이 통합 기록을 따른다.
