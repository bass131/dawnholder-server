# 시스템 기록 재구성·원문 대응 계획

사용자 승인 범위는 운영 도구에서 기존 내용을 시스템별로 먼저 재구성하고 실제 화면에 반영하는 일이다. 원문 이동·삭제나 진행 goal 정본 전환은 포함하지 않는다. 이 문서는 원문 대응과 소유권 계획이며 현재 목표의 완료 상태는 [goal.md](goal.md)가 원본이다.

기준시점 `2026-09-30T09:05:09Z`, 기록 revision `2026-09-30-r1`, Git 근거 `c27b03e888986f2ec8c593cd6c626a9c515595e1`. [catalog.json](../../records/catalog.json)은 18개 시스템(게임·플랫폼 15, Management 3), 18개 공통 기록, 35개 출처를 담는다. 지정 작성 모델 `gpt-6-astra`, 확인된 실제 runtime `unknown`.

## 순서와 소유권

1. 기능 지도·영역 계약·완료 goal에서 주요 시스템과 설명 후보를 정한다. 과거 실행 결과는 과거 결과로 유지한다.
2. 아래 대응표로 시스템 ID·공통 기록 ID·원문 구간을 연결한다. 여러 시스템에 걸친 변경은 같은 기록을 참조한다.
3. Management가 탐색용 설명과 JSON을 작성하고 UI 담당자가 앱에서 읽기·새로고침·불러오기·편집·저장 경로를 구현한다. JSON은 빌드 시 내장하지 않고 Electron main이 실행 중 읽는 방향이며, 이 계획 문장 자체는 기능 구현·검증 완료를 뜻하지 않는다. 기존 기술 계약을 복제 정본으로 만들지 않는다.
4. Game Dev 의미 검토와 별도 Astra의 출처·상태·UI 검증을 거친다. 구현과 검증 파일의 작성자는 분리한다.
5. 검증 뒤 항목별 서술 소유권 전환 후보를 두 메인·사용자와 협의한다. API·MCP·3D·진행 goal 이전은 이번 완료로 주장하지 않는다.

| 자료 종류 | Game Dev에 유지 | Management에서 하는 일 | 보존·후속 |
| --- | --- | --- | --- |
| AGENTS·목표 루프·코드 기준·실행 안내 | 권한·역할·실행 부작용의 원본 | 관련 설명에 링크 | 자동 권한 부여·기술적 강제로 설명하지 않음 |
| ARCHITECTURE·FEATURE_MAP·domains·기술 ADR | 해당 commit의 의존 방향·코드 진입점·권위/수명/순서/실패 계약 | 시스템 목적·책임·행동을 사람용으로 재구성 | 전체 문서를 이동하지 않고 상대경로·commit·구간 연결 |
| 완료 goal·verification·보고서 | 당시 결과와 첨부 원문 | 공통 변경/결정/검증 기록으로 탐색 | 원문 수치·단위·skip·미실행 보존; 새 실행으로 재사용 금지 |
| 진행 goal·CURRENT | 현재 상태의 단일 원본 | 기준시점의 계획 카드 | 상태를 양쪽에서 독립 수정하지 않음 |
| Management 요구·결정·기존 goal | Management 소유 | 3개 시스템에 기존 선택·구현·검증을 연결 | 이번에 바뀌는 README/현재 goal은 hash 출처로 고정하지 않음 |
| API·MCP·Three.js/3D | 기술 계약 추후 조율 | 후속 표현·조회 계획 | 이번 정적 파일 조회를 API 성공으로 표시하지 않음 |

## 원문 → 시스템·공통 기록 대응

| 원문·고정 구간 | 시스템 ID | 공통 기록 ID | 재구성·보존 방식 |
| --- | --- | --- | --- |
| FEATURE_MAP 기능 표; ARCHITECTURE 요청/상태·프로토콜·맵 | connection, movement, combat, skills, enemy-ai, character-state, map-entry, party, quest, remote-rendering, packet-publication, transport, protocol, persistence | decision-preserve, verify-m3 | 주요 기존 기능을 빠짐없이 묶되 기능 존재와 개별 검증 완료를 구분 |
| domains/server 즉시 적 피해; S1 goal; 유지보수 HTML #s1; 고정 commit의 GameMap.ApplyImmediateEnemyHit/HandleEnemyDeath·DeferredDamageSystem.Process | combat, character-state, enemy-ai, engineering | change-immediate-hit, verify-immediate-hit | 평타 SubmitAttack/CombatSystem과 Dash SubmitSkillUse/SkillSystem 입구를 구분. StageClear는 보스이며 아직 clear 전일 때만, respawn 등록은 비보스만이다. 지연 피해의 clamp는 패킷 currentHp 표현이며 권위 HP 보정이 아님을 명시 |
| domains/server 맵 패킷 표현; S2 goal; HTML #s2 | packet-publication, map-entry, enemy-ai, protocol | change-publication | 표현 조립과 발행 시점·수신자·FSM 소유를 분리 |
| domains/protocol 서버 연결 종료; S3 goal; HTML #s3 | transport, connection | change-disconnect | 원래 예외 보존·정리 도달과 모든 I/O race 미검증을 함께 기록 |
| domains/client 원격 보간; S4 goal; HTML #s4 | remote-rendering, enemy-ai | change-interpolation | buffer/clock·adapter·callback 경계와 EditMode/실제 플레이 한계 보존 |
| S5 goal; HTML #s5; domains/protocol | protocol, engineering | change-generator | subprocess exit·생성 bytes 보존·TTY/atomic write 한계 구분 |
| M2a 파티·퀘스트 goal; M2b 스탯 goal | party, quest, character-state, protocol | change-domain-state | 상태 소유·불변 결과·Shared ABI를 설명하고 DB 저장 실적으로 바꾸지 않음 |
| HTML #history, #coverage, #measurement | connection, movement, character-state, combat, skills, enemy-ai, map-entry, party, quest, engineering | verify-m3, decision-preserve | M0~M3 이전 완료 기반과 제한된 답안 평가를 보존; 이번 재실행·성능 점수로 합산하지 않음 |
| HTML #evidence; domains/tooling | engineering, combat, packet-publication, transport, protocol | verify-combined, decision-standards | 과거 고정 합성 입력·834 pass/839·기존skip5·봇2 및 합성 Unity 미실행 구분 |
| D0 goal; 공통 계약·영속성 초안의 기준선/단계 | persistence, character-state, map-entry, management-operations | decision-d0, plan-contracts | D0 설계 완료와 runtime 저장 미완료 구분; P2~P7은 계획 문장으로 표시 |
| Game Dev 최종 메시지 msg_b5283836b43f / 09:05:09Z | PR140~145 관련 시스템, engineering, management-records | 위 변경 기록의 상태, plan-contracts, decision-standards | 보고서·goal에 남은 OPEN은 후속 보정으로 대체; 실시간 조회라고 하지 않음 |
| Management decisions D-01~D-09; requirements R-01~R-12 | management-desktop, management-operations, management-records | change-management-shell, decision-management-backend | 합의된 기술·요구와 아직 미구현 backend/운영·미결정 명령 분리 |
| Management foundation goal; desktop-shell/console-refinement verification | management-desktop, management-operations | change-management-shell | 이전 경로 테스트·실제 렌더와 새 경로 미실행 구분 |
| Management launcher verification 결과/범위 | management-desktop | change-launcher | cmd fixture12·main7과 실제 detached·1280×720 미검증 구분 |
| workspace-split README 소유권/보존/한계 | management-desktop, management-records | verify-workspace-split, decision-record-migration | 파일 hash 보존은 앱 실행 성공과 다름; 과거 경로와 원본 보관을 기록 |
| Game Dev 공동 협의 회신 소유권/전투 시범/남은 협의 | management-records, engineering, combat | decision-record-migration, plan-contracts | 원본 유지·Management 탐색 서술·향후 항목별 정본 협의 경계 |

## 시스템별 읽기 진입점

| 시스템 ID | 화면 제목 | 책임과 후속의 중심 |
| --- | --- | --- |
| connection | 연결·버전·캐릭터 선택 | GameSession은 세션 검증과 제출을, 클라이언트 NetworkService는 연결·수신 적용 경계를 소유한다. |
| movement | 이동·점프·로컬 예측 | MoveIntentHandler·GameSession이 요청을 검증·제출하고 PlayerPhysicsSystem이 틱에서 적용한다. |
| combat | 전투·피해·사망 처리 | CombatSystem·공격 액션은 고유 판정, GameMap.ApplyImmediateEnemyHit는 즉시 피해 결과와 치사 후처리, DeferredDamageSystem은 기존 지연 피해를 소유한다. |
| skills | 스킬·행동 가능 조건 | ActionGate·SkillSystem·각 Action은 서버 판정과 실행을, PlayerAbilityTimers는 클라이언트 예측 상태를 담당한다. |
| enemy-ai | 적·보스 AI와 표현 | EnemyAISystem·BossBehaviorSystem은 FSM·animation 선택을, publisher는 표현 조립을, 클라이언트는 표시를 소유한다. |
| character-state | 캐릭터 스탯·HP·리스폰 | 서버 엔티티와 관련 시스템이 현재 상태를, Shared의 기본 스탯·공식이 양쪽 공통 정의를 소유한다. |
| map-entry | 맵 진입·씬 전환·포탈 | MapMigration은 이동 검증·인계, SceneRouter·MapEntryCoordinator는 클라이언트 씬·entry 수명을 소유한다. |
| party | 파티·초대·멤버 상태 | PartyFlow·PartyRegistry·PartyState는 파티 상태를, PartyNotifier는 통지를 담당한다. |
| quest | 처치 진행·보스 해금 | QuestRegistry는 진행·entity 해금 latch, QuestNotifier는 패킷 표현, GameWorld는 기존 큐와 갱신 순서를 소유한다. |
| remote-rendering | 원격 보간·클라이언트 효과 | RemoteInterpolationState는 buffer·clock, RemoteEntity는 공개 component API·Transform·callback을 소유한다. 효과 생성·수명은 기존 EffectSpawnService 경계를 유지한다. |
| packet-publication | 서버 상태·패킷 발행 | publisher는 PlayerJoin·선택한 주기 EntityState 표현을, 호출자는 상태 변경·발행 간격·수신자를 소유한다. |
| transport | 전송·프레이밍·종료 | 전송 계층은 socket·송수신 큐·프레임 검증을, GameServer는 도메인 상태를 소유한다. |
| protocol | 공유 데이터·프로토콜·생성기 | PDL은 정의 원본, PacketGenerator는 생성, 각 소비자는 직렬화·호환성 검증을 담당한다. |
| persistence | DB·캐릭터 저장과 복원 | 서버가 게임 상태를 소유하고 후속 repository·authority·operation 계약이 안전한 저장과 복원 수명을 맡도록 설계한다. |
| engineering | 작성 기준·회귀·개발 도구 | Game Dev는 실행 전제·코드 계약·진행 goal을 유지하고 구현·독립 테스트·보고 역할을 분리한다. |
| management-desktop | Management 창·실행 배치 | Management가 UI·desktop main·launcher를 소유하며 창 닫기와 서버 운영 수명은 구분한다. |
| management-operations | 운영·유저/GM 관리 | Management는 표시·요청을 담당하고 서버는 권한·대상·현재 상태 검증과 게임 판정을 소유한다. |
| management-records | 개발 기록·공통 근거 탐색 | Management는 탐색용 서술·정적 catalog, Game Dev는 기술 계약·진행 goal과 CURRENT를 계속 소유한다. |

## 출처 고정과 가용성

`catalog.sources`에 정확한 상대경로/원본 절대경로, Git commit 또는 SHA256, 구간, 가용성을 둔다. Git 자료는 이 Management checkout의 고정 commit에서 읽었다. Game Dev root의 전환 중 checkout을 복제하지 않았다.

로컬 HTML 보고서·초안·분리 기록·협의 회신은 `C:/Dev/DawnHolder_Project/.backups/...` 원본 경로와 SHA256을 유지한다. 다른 PC나 checkout에서는 없을 수 있으며 파일을 찾지 못하면 미확인으로 표시해야 한다. 동일 파일의 여러 섹션을 서로 다른 source ID로 가리키는 것은 구간 연결이며 별도 실행 증거가 아니다.

기존 `05_Management` 문서들은 작성 시 Git 미추적이어서 `local-only`와 SHA256으로 고정했다. 과거 본문의 `C:/Dev/.../05_Management`는 당시 작업 경로다. 실제 출처 locator는 보존된 새 Management 파일을 가리키며, 과거 실행을 새 경로에서 수행한 것으로 바꾸지 않는다. 원시 로그·이미지를 재수집하거나 원문을 수정하지 않았다. 향후 커밋 이후 출처를 Git으로 전환할 경우 해당 정확 commit을 별도 revision으로 기록해야 한다.

PR 최종 상태 출처는 `msg_b5283836b43f @ 2026-09-30T09:05:09Z`다. PR140/142 기존 병합에 PR141/143/144/145 병합을 합쳐 모두 MERGED로 표시한다. 보고서와 협의 회신의 이전 OPEN 스냅샷을 현재값으로 사용하지 않는다. P0 문서 local commit `966d7a915a7e9e0dc2d61a73e28a5962fd885c2d`는 전달된 관찰이며 이 작업에서 root를 읽어 복사하거나 평가 PASS를 확인한 것이 아니다.

## 검증과 남은 일

작성자가 ID·출처·참조 연결을 자체 확인했더라도 독립 내용 검증을 대신하지 않는다. 출처의 구간과 핵심 주장이 일치하는지, 기록과 시스템 참조가 양방향인지, MERGED/준비/미완료가 섞이지 않는지를 별도 검증자가 확인한다.

UI에서는 시스템에서 변경 이유·책임·구현/통합/검증 상태·미실행·다음 할 일과 출처에 도달해야 한다. 새 경로 앱·1280×720 결과와 이번 화면/접근성/네트워크 검증은 현재 goal에 별도 기록하며 09:05 기준 catalog를 소급 성공으로 바꾸지 않는다.

이 작성에서는 새 게임·Unity·SQL·서버 실행, API/MCP·3D 구현, Git 병합·배포, 원문 삭제·이전을 수행하지 않았다. Game Dev의 실제 P0 평가와 P1~P7, Management backend·운영 권한·장기 기록 정본 전환은 해당 소유자와 후속 목표로 남는다.
