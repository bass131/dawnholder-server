# 확인한 개선과 후속 후보

2026-10-01, 기준 main `ef5f1023fe3353ee9eec5da04422232a33855233`. 이 문서는 해당 시점의 감사·인계 자료다. 각 작업의 상태·실행 결과는 연결한 goal이 정본이며, 이번 정정의 결과는 [정정 목표](goal.md)에 기록한다. 아래 과거 개선은 이번에 게임 테스트를 다시 실행한 결과가 아니다. 새로운 후보가 등록됐다고 구현 범위나 실행 권한이 추가되는 것도 아니다.

## 보존할 개선

| 기존 경계 | 변경 후와 확인 범위 | 당시 결과 정본 |
|---|---|---|
| 자연 종료 뒤 연결 상태와 재연결 판단 분산 | `ClientConnectionLifetime`이 연결 시도·세션 종료 수명을 관리 | [M1b / PR132](../2026-09-29-refactor-client-connection/goal.md) |
| 맵 이동·disconnect 경쟁과 호스트 종료 순서 | world 종료 큐와 `ServerHost`로 상태 변경·정리 순서를 명시 | [M1a / PR131](../2026-09-29-refactor-server-lifecycle/goal.md) |
| 겹친 맵 전환과 늦은 씬 완료 처리 | `MapEntryCoordinator`·`SceneLoadQueue`가 entry와 물리 load 수명을 구분 | [M1c / PR133](../2026-09-29-refactor-client-entry/goal.md) |
| Quest가 Party 진행 상태와 통보에 직접 관여 | 멤버십은 Party, solo/party 진행은 Quest, 패킷 생성은 QuestNotifier가 소유 | [M2a / PR134](../2026-09-30-refactor-party-quest/goal.md) |
| 기본 스탯의 HP와 현재 HP, mutable 전달 참조 혼합 | `InitialHp`·현재 `PlayerEntity.Hp`·`PlayerTransferState`로 의미와 소유를 분리 | [M2b / PR135](../2026-09-30-refactor-player-state/goal.md) |
| 평타·Dash의 즉시 피해 조정 분산 | `GameMap.ApplyImmediateEnemyHit`로 조정 책임 통합, 액션별 차이 보존 | [S1 / PR140](../2026-09-30-immediate-enemy-hit/goal.md) |
| Disconnect 알림 예외 뒤 자원 정리 생략 | 정리 후 기존 예외를 전파하는 종료 계약 | [S3 / PR143](../2026-09-30-session-disconnect-cleanup/goal.md) |
| PacketGenerator 출력 실패에도 exit 0 | 기존 catch의 출력 실패를 exit 1로 전달 | [S5 / PR145](../2026-09-30-generator-exit-and-rollout-report/goal.md) |
| 팝업 표시·요청 책임 혼합과 현재 Instance 재조회 해제 | 기능 command와 같은 source 해제. 강제 source 교체·파괴 fixture의 실패→성공이며 운영 누수 발생 확인은 아님 | [P1a / PR148](../2026-09-30-party-invite-command/goal.md) |

이는 선정된 개선의 요약이며 PR 전체 목록이나 프로젝트 전체 결함 해소 선언이 아니다. 각 goal의 당시 테스트·실제 플레이·미실행 구분을 함께 읽는다.

## 후속 후보의 구분

기술 후보는 정적 관찰 또는 제한 fixture이며 운영 결함 확정과 다르다. 정책 미결정은 해당 세부 정책에 대한 직접 선택 기록을 확인하지 못했다는 뜻이며, 리팩토링에서 현행 동작을 보존한 사실은 유지한다. 합의된 후속 작업은 제품 범위를 다시 결정하지 않고 구체 기술 계약과 실행 범위를 정한다. 아래 순서는 우선순위가 아니다.

| 항목 | 분류 | 근거·관찰과 한계 | 다음 행동 | 사용자 결정 필요 여부 |
|---|---|---|---|---|
| RegisterSend 예외 후 pending 잔류 | 기술 후보·명시적 범위 제외 | [Session.RegisterSend](../../../02_Server/Network/Session.cs)의 catch는 로그만 남긴다. [S3 사전 조사](../../../.backups/reviews/2026-09-30-s3-network-assessment.md)는 관찰했으나 후보를 확대하지 않았다. 실제 실패→후속 송신 정체는 미재현 | 정상 API의 실패 조건·잔류·다음 Send를 좁게 재현 | 기술 재현은 불필요. disconnect/재시도 등 처리 정책이 달라지면 필요 |
| QuestProgressHud 구독 해제 | 기술 후보·추가 조사 | [HUD](../../../03_Client/Assets/Scripts/UI/QuestProgressHud.cs)는 현재 Instance를 재조회한다. [P0 구독 계약](../2026-09-30-contracts-baseline/contracts.md)의 후속 후보이며 Popup fixture를 이 화면의 운영 누수 증거로 쓸 수 없다 | 실제 source 수명·교체 도달성과 대칭 해제 계약 확인 | 대칭 해제 자체는 기술 판단. 자동 rebind·재표시는 필요 |
| PartyMemberHud 구독 해제 | 기술 후보·추가 조사 | [HUD](../../../03_Client/Assets/Scripts/UI/PartyMemberHud.cs)의 현재 Instance 해제 패턴은 [P0 조사](../../../.backups/reviews/2026-09-30-p0-client-boundary-inventory.md)에 명시됐다. 개별 실행 goal은 미착수 | Quest HUD와 별도로 해당 source·화면 수명을 확인 | 자동 rebind·표시 변화가 있으면 필요 |
| Quest solo/보스 해금 엔트리 회수 | 기술 후보·추가 조사 | [QuestRegistry](../../../02_Server/GameServer/Quest/QuestRegistry.cs), [P0 파티·퀘스트 행](../2026-09-30-contracts-baseline/contracts.md). disconnect 회수 완료 근거 없음 | 세션 한정이라는 기존 결정에 맞춰 종료와 pending kill의 순서·회수 경로 확인 | 세션 한정 결정은 재질문하지 않음. 그 범위를 바꾸려면 필요 |
| 부분 송신의 Unity Mono/IL2CPP 도달성 | 명시 보류·검증 공백 | [S3 goal](../2026-09-30-session-disconnect-cleanup/goal.md). .NET10 TCP 관찰과 reflection 주입을 구분했고 Unity 경로는 미검증 | 대상 플랫폼의 실제 API·부분 완료 도달성과 byte 보존 시험을 구체화 | 플랫폼 시험 우선순위·자원 범위 결정. 현재 제품 정책 선택은 아님 |
| 초대 수락 drop 후 pending 소비·팝업 닫힘 | 정책 미결정 | [P1a](../2026-09-30-party-invite-command/goal.md), [command](../../../03_Client/Assets/Scripts/UI/PartyInviteResponseCommand.cs). void 호출 정상 반환은 접수/가입 보장이 아니다. 기존 동작 보존 fixture와 실제 로딩 중 클릭 도달성을 구분 | 도달 가능한 시나리오와 pending 유지·닫힘·재시도 선택지 제시 | 동작을 바꾸려면 필요. 세부 정책의 개별 사용자 확정 기록은 미확인 |
| 맵 이동 시 쿨다운 초기화 | 정책 미결정 | [M1a](../2026-09-29-refactor-server-lifecycle/goal.md), [M2b](../2026-09-30-refactor-player-state/goal.md). 새 entity의 transient 초기화를 보존한 것이며 개별 정책 선택 기록은 미확인 | 이동 전후 cooldown을 유지할지 기존 초기화를 유지할지 영향 정리 | 게임 정책 변경에는 필요 |
| 메뉴 probe 요청값·실패 정리·late 결과 | 기술 후보·정책 미결정 | [MainMenuController](../../../03_Client/Assets/Scripts/UI/MainMenuController.cs), [ConnectionProbe](../../../03_Client/Assets/Scripts/Network/ConnectionProbe.cs), [P0 메뉴 행](../2026-09-30-contracts-baseline/contracts.md). 요청 host와 callback 재조회 값이 달라질 수 있고 실패 정리·화면 수명은 추가 확인 대상 | host 수정·중복 시작·숨김/파괴·늦은 성공/실패의 현재 결과 재현 | 숨김/파괴 취소·저장·씬 전환 정책 확정 시 필요. P1b는 미착수 |
| 초대 만료 경계 | 확인 필요 기술 후보 | [PartyRegistry.Tick](../../../02_Server/GameServer/Party/PartyRegistry.cs)은 pending job을 처리한 뒤 만료를 청소한다. 이 순서 관찰만으로 만료 초대 수락 결함을 확정하지 않음 | Respond 경로·timeout 경계 tick·queue 순서를 함께 확인하고 반증 fixture 설계 | 현행 만료 계약의 기술 확인은 불필요. 허용 시간 정책을 바꾸면 필요 |
| PlayerEntity의 ‘저장 후보(M8)’ 주석 | 주석·계약 불일치 후보 | [PlayerEntity](../../../02_Server/GameServer/Entities/PlayerEntity.cs)의 구 주석과 [D0](../2026-09-29-persistence-design/goal.md)의 identity/class·안전 checkpoint 범위는 구분해야 한다. raw HP/전투 위치 저장은 현 범위 밖 | 후속 저장 경계 작업에서 주석과 D0 의미를 대조. 이번 C4 파일에는 포함하지 않음 | 기존 D0와 맞추는 기술 정정은 불필요. 저장 범위 확대는 필요 |
| HitResultHandler 표현 책임 | 추가 조사 후 판단 | [P0 전투 표현 행](../2026-09-30-contracts-baseline/contracts.md)은 권위 HP 적용 뒤 효과 순서 보존과 변경 필요성 추가 확인을 요구한다. 통합/분리 필요성이 확정된 것은 아님 | 실제 변경 이유와 HP→사운드/VFX 순서 검증 비용을 확인 | 단순 책임 분리는 기술 판단. 보이는 동작 변화는 필요 |
| DB 기술 설계·저장소·입장 연결 | 합의된 후속 작업·미착수 | [D0 설계와 구현 분할](../2026-09-29-persistence-design/implementation-plan.md), [로드맵 P2–P5](../../milestones/2026-09-30-contracts-persistence/roadmap.md). schema 접속은 GameServer 저장·복원 완료가 아니다 | D1a에서 driver/DDL/transaction/fence/recovery를 구체화. P1 완료에 의존하지 않음 | D0 제품 범위는 확정. 구체 SQL 변경·장애 시험의 대상/권한은 별도 |
| 종료 전체 deadline·DB release | 합의된 후속 작업(D2) | [ServerHost.StopCore](../../../02_Server/GameServer/Hosting/ServerHost.cs)는 단계별 timeout을 사용한다. [D0 구현 분할](../2026-09-29-persistence-design/implementation-plan.md)의 단일 deadline·저장 수명은 아직 구현하지 않았다 | 저장 계약 이후 bounded 작업·actor 인계·결과불명/release 검증 | 기존 D0 원칙은 재질문하지 않음. 새로운 복구 권한/정책은 필요 |
| Management 서버 명령·조회 계약 | 합의된 후속 작업(P6) | [로드맵](../../milestones/2026-09-30-contracts-persistence/roadmap.md)과 [P1a 종료 인계](../2026-09-30-party-invite-command/goal.md). 별도 Management의 개발기록 공동조회와 서버 제어/저장 완료 adapter를 구분 | 해당 메인과 principal·완료·오류·as-of 계약 조율. 저장 완료 adapter는 P4에 의존 | 명령 권한·운영 정책은 공동 결정. 공동조회 합의를 다시 백지화하지 않음 |
| 삭제된 유효 설계 이유 | 이번 정정 범위(C4) | `84d3566`의 RespawnSystem, `f948848`의 RemoteEntity diff. 수치·계산 책임은 이동했지만 일부 이유 설명은 삭제됐다 | 현재 소유 코드 가까이 짧은 일반 주석 복원. 완료/검증은 [정정 goal](goal.md) 확인 | 제품 정책 선택 불필요 |
| BossStates.BeginTelegraph의 즉시 발행 | 의도적 유지·범위 제외 | [S2 goal](../2026-09-30-server-packet-publication/goal.md)은 주기 snapshot과 별개인 즉시 공격 예고를 유지했다. 모든 EntityState 조립의 단일화를 약속하지 않음 | 현재 보존 근거를 유지. 새 변경 이유가 확인되기 전 통합 과제로 승격하지 않음 | 현재 변경 없음. 발행 시점/정책 변경 시 별도 판단 |
| P0 변경 후(after) 평가 | 검증 공백·후속 계획 | [P0 평가 방법](../2026-09-30-contracts-baseline/evaluation-method.md), [P0 결과](../2026-09-30-contracts-baseline/goal.md), [P7](../../milestones/2026-09-30-contracts-persistence/roadmap.md). after commit·실행 결과 없음 | 비교하려는 변경 범위와 A를 고정하고 같은 조건의 실험 가치·시점을 정함 | 새 합격선/과제/대규모 재평가 도입 시 필요. 이번 정정에는 실행하지 않음 |

## 2026-10-01 추가 — 36fb5ec7 기준

기준 main `36fb5ec751f4c482a993e77c9547968e8f828e34`, 요청 원문 `msg_8ca511af0885`([로컬 보존](../../../.backups/handoffs/2026-10-01-routing-documentation-request.json)). 위 `ef5f1023` 기준 목록을 소급 변경하지 않는다. 메뉴 연결·RegisterSend·HUD 2개 등 기존 후보는 유지한다. 아래는 후속 등록이며 이번 문서화에서 코드·ADR·패키지·보안 설정을 변경하지 않았다.

| 항목 | 관찰·한계 | 다음 행동·결정 경계 |
|---|---|---|
| `?? AddComponent` 4곳 | [ProjectileSpawner:29](../../../03_Client/Assets/Scripts/Combat/Effects/ProjectileSpawner.cs), [ProjectileLaunchHandler:94](../../../03_Client/Assets/Scripts/Network/Handlers/Skill/ProjectileLaunchHandler.cs), [EnemyAttackHandler:102](../../../03_Client/Assets/Scripts/Network/Handlers/Combat/EnemyAttackHandler.cs), [RemoteEntityRegistry:260](../../../03_Client/Assets/Scripts/State/RemoteEntityRegistry.cs)에 해당 표현을 정적으로 확인. Editor fake-null에서 AddComponent 누락 가능성은 실행 미검증 | [새 시범 goal](../2026-10-01-hierarchical-routing-pilot/goal.md)의 승인 범위로 처리. 구현·재현은 미착수 |
| ADR-029 SAC 재활성화 설명 | [ADR-029:19](../../../00_Document/ADR/harness/ADR-029-wsl2-dotnet-execution-standard.md)의 재설치 필수 문구와 [Microsoft FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)의 최신 업데이트 후 재설치 없는 재활성화 안내가 다름. FAQ의 기기에서 제공되는 경우(`if available for your device`) 조건을 보존. 이 PC Windows `26200.9457`은 레지스트리 읽기로 확인했으나 SAC 재활성화 조작·시험은 미실행 | 당시 WSL 선택 이력과 현재 지원 조건을 구분하는 문서 정정 후보. ADR 본문·보안 설정은 지금 변경하지 않음. 보안 설정 변경은 사용자 판단 필요 |
| manifest skip-worktree | [manifest.json](../../../03_Client/Packages/manifest.json)의 index flag `S`; 로컬 `com.unity.ai.assistant`는 `2.11.0-pre.1`, HEAD는 `2.7.0-pre.3`로 status에 차이가 드러나지 않음. checkout 전후 SHA256 `3E194274509B32D18F4BE03F2D9462B5CDBB721C14EEE0ED0B6A17B1360AD781` 동일 | 로컬 변경 의도·소유자와 처리 범위를 먼저 확인. flag 해제·버전 변경·덮어쓰기 금지; 후속 사용자 결정 전 보존 |
| UnityClientSession 분리 | [UnityClientSession](../../../03_Client/Assets/Scripts/Network/UnityClientSession.cs). 기존 표에는 없었으므로 사용자 전달 원문 `msg_8ca511af0885`의 유지 요청을 근거로 명시 등록. 이번에 책임 분리 필요성이나 결함을 확정한 것은 아님 | 기존 연결·entry·mirror 계약을 보존하며 변경 이유·경계·검증 비용을 좁게 조사. 기술 선택은 담당자 판단, 동작·범위 변경은 사용자 판단 |

로컬 관측 근거: `.backups/verification/2026-10-01-hierarchical-routing-handoff/baseline.json`(branch/base·manifest·OS). 공식 FAQ 확인과 이 PC의 실제 재활성화 성공은 별개다.

## 평가와 출처의 한계

[문서 정비의 63→83](../2026-09-29-ai-readiness-docs/goal.md)은 문서·운영 근거를 보강한 평가자 점수, [M0–M3의 29/32→31/32](../2026-09-29-refactor-regression/goal.md)는 고정 과제 답안 적합도, [P0의 44/48](../2026-09-30-contracts-baseline/goal.md)은 다른 여섯 과제의 진단 기준선이다. 서로 다른 대상·조건이므로 하나의 코드 품질 추세나 생산성 개선율로 합치지 않는다. P1a 뒤 점수 향상이나 탐색 속도 개선은 측정하지 않았다.

초기 사용자 요청·방향 전환 승인에 관한 goal의 에이전트 요약은 존재한다. 이번 확인 범위에서 당시 사용자 발언 원문을 직접 검증하지 못했으며 Orca 대화 검색은 비활성 상태였다. 따라서 요약을 원문 인용 증거로 표시하거나, 반대로 원문 미확인만으로 무승인을 단정하지 않는다. 원자료의 줄 수 증가나 산출물 용량 역시 결함·효용의 독립 증거로 사용하지 않는다.

작성 지정 모델 `gpt-6-astra`, 실제 runtime `unknown`. Claude의 정적 감사 원자료와 기존 goal/선택한 코드·diff를 대조했으며 원자료의 모든 후보를 이번 수정 대상으로 채택하지 않았다. 로컬 `.backups` 링크는 해당 PC의 보조 근거다. 공유 가능한 goal·코드 링크와 관찰 요약을 함께 남겼으며 원문 로그·과거 대화는 복제하지 않았다. 독립 검토 결과는 [정정 goal](goal.md)에 기록한다.
