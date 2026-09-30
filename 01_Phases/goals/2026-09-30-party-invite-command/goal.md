# P1a — 파티 초대 응답 요청과 팝업 책임 분리

상태: **설계 확정, 독립 baseline 테스트 준비 중**. 구현·실행 검증·PR 병합 완료가 아니다.

## 목표·기준선

사용자는 PR146 병합과 후속 진행을 승인했다. P0 merge `a2eb65ee663ff127e77481b7ddcc5e3859abc4b8`에서 `bass131/party-invite-command-p1`을 만들었다. 새 P1 PR의 병합 승인은 별도로 받는다. [P0](../2026-09-30-contracts-baseline/goal.md)의 계약·평가와 [첫 구현 계약](../2026-09-30-contracts-baseline/p1-first-contract.md)을 적용한다.

파티 팝업의 표시와 packet 조립/세션 확인/pending 소비 책임을 분리한다. 팝업은 버튼 의도·클릭음·표시/숨김·구독, 작은 기능 command는 현재 상태 조회·packet 구성·기존 순서의 로컬 처리만 맡는다. membership 확정은 서버와 기존 S_PartyUpdate 처리에 남긴다. 작은 일반 C# 타입을 사용하고 전역 UIManager/이벤트 버스/범용 command framework는 만들지 않는다.

P1 전체에서 독립 검증 가능한 첫 조각 P1a다. 메뉴 probe·다른 HUD binding·새 UX 정책은 후속이다. 처음부터 전체 UI를 다시 쓰지 않는다.

## 범위와 보존 계약

- SendRespond의 초대/세션 확인·C_PartyRespond 구성·SendIntent 호출·pending 소비를 추출한다. 상세 정상/거부/drop/지연/예외 기대값은 P0 첫 계약의 보존 표를 따른다. 실제 getter 호출 시점과 callback 순서도 보존한다.
- void SendIntent 정상 반환은 송신 접수/서버 가입 성공이 아니다. 새 ack·낙관 membership·재시도·pending 복구·오류 UI를 추가하지 않는다. 클릭음→응답 호출→팝업 숨김과 예외 시 중단 순서를 유지한다.
- 별도 작은 조각으로 실제 구독한 PartyState source를 보관해 동일 source에서 해제한다. 강제 교체 fixture와 실제 교체 도달성을 구분하며 자동 rebind/replay는 추가하지 않는다. 표시 정책이 달라지는 분기가 필요하면 먼저 사용자와 결정한다.
- 서버·PDL·Generated·공유 DLL·전송 gate·PartyState 소유권·scene/prefab/기존 meta·직렬화 이름/값·리소스 경로를 보존한다. 신규 코드/test에는 새 meta를 만들고 GUID 충돌을 확인한다.

선택한 구체 경계는 기존 UI assembly의 `UI/PartyInviteResponseCommand.cs`에 internal 일반 C# command를 두고, 현재 PartyState/UnityClientSession을 얻는 두 provider와 `void Execute(byte accept)`만 사용하는 것이다. 장기 session 참조·불필요 결과 enum/interface/registry를 만들지 않는다. 팝업의 `OnInviteReceived`가 활성 중 현재 Instance를 조회하는 기존 방식은 유지한다. 구독 source를 기억하는 것과 callback의 표시 source 정책을 바꾸는 것은 별개이며, 자동 재연결/재표시는 후속 판단이다.

## 역할·순서·파일 소유

- 메인 Astra: goal·CURRENT·로드맵·관련 기능 계약·결과 보고, 사용자 정책 결정과 통합.
- 독립 Astra: 신규 `03_Client/Assets/Tests/EditMode/PartyInviteResponseContractTests.cs`/`.meta`, 필요할 때 신규 `PartyInvitePopupBindingTests.cs`/`.meta`, 자신의 검증 산출물. 기존 fixture는 읽고 재사용하며 기존 테스트/asmdef 수정이 필요하면 정확한 파일을 메인에 요청한다.
- Sol6.1: `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs`, 신규 `PartyInviteResponseCommand.cs`/`.meta`(기존 Unity assembly 안의 기능 위치). baseline 완료 신호 전 생산 쓰기 금지. 테스트 기대값을 구현에 맞춰 수정하지 않는다.

독립 baseline Component/버튼 fixture 실행 → writer-end/source 고정 → Sol 구현 → writer-end → 동일 baseline 및 추가 경계/실패/구독 검증 → 별도 Astra 코드 검토 → 문서/PR 순서다. 일반 작업자의 추가 위임·같은 파일 동시 쓰기는 금지한다. 결함은 Sol에 반환한다. 지정 모델과 실제 runtime은 구분하고 확인 불가하면 unknown이다. 새 CLI는 --no-daemon이며 전역 설정·기존 사용자 Editor·Management/Claude worktree는 건드리지 않는다.

## 검증·실행 경계

실제 popup 경로의 pending·CanvasGroup·membership·packet 유무/값/순서를 관찰한다. 실제 serializer를 기대값 생산기로 다시 호출하지 않는다. 지연 callback 무효화는 유효 조건의 양성 송신과 짝으로 검사한다. same-source 구독은 enable/disable/re-enable·강제 source 교체·old/new 이벤트를 구분한다.

[DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)를 먼저 확인한다. 단일 Astra 검증자가 Unity6000.4.7f1의 소유한 격리 batch 환경과 PID/log/XML을 관리한다. 기존 검증 환경은 경로·소유권·입력 hash 확인 후 재사용하고 필요하면 목표 산출물 아래 격리 복사본을 만든다. 사용자 Editor를 닫거나 원본 프로젝트를 동시에 batch로 열지 않는다. Assets/meta/직렬화와 Shared/ClientNet DLL의 전후 hash를 보존한다. Windows SDK 빌드로 원본 DLL을 덮어쓰지 않는다.

baseline 대상 테스트 → 구현 후 같은 테스트와 추가 경계/실패/구독 테스트 → generation/mirror/entry 영향 회귀 → 안정된 변경의 전체 EditMode 1회. 실패 수정/새 영향이 있을 때만 필요한 검증을 반복한다. PlayMode·실제 클릭/시각/음성·서버 가입 왕복은 실제 수행 범위만 보고한다. 서버·SQL·PDL 생성과 새 전역 UI smoke 인프라는 이 작업에 포함하지 않는다.

## 완료조건

- [ ] 독립 baseline fixture로 기존 popup 정상·거부/drop·예외·pending/표시 계약을 실행하고 근거를 남긴다.
- [ ] 요청 책임을 기능 command로 분리하고 팝업의 서버 상태 낙관 변경 없이 참조 보유·구독 해제 책임을 명확히 한다.
- [ ] 독립 TestCode로 전후 계약·실패·지연 callback·구독 수명과 필요한 Unity 회귀를 확인한다. 차단·미실행은 별도 기록한다.
- [ ] 별도 코드 검토에서 과도한 추상화·패킷/에셋 변화·새 완료 보장·의도하지 않은 UX 변화가 없음을 확인한다.
- [ ] Astra가 변경 이유·대표 diff·실제 검증·한계·남은 정책을 보고하고 PR을 생성한다. 최종 head CI 후 사용자 개별 병합 승인을 받는다.

## 현재 결과·다음 실행

PR146은 UTC2026-09-30 09:55:32 병합됐다. 최신 main a2eb65e는 P0 진단 대상과 생산 코드가 같다. 이전 AI44/48을 P1 개선 후 측정값으로 재사용하지 않는다. 독립 baseline fixture와 격리 Unity 환경을 준비 중이며 생산 코드는 동결돼 있다. 설계 범위·보존 계약의 별도 Astra 검토 PASS, 근거는 `.backups/reviews/2026-09-30-p1-scope-review.md`다. 코드 검토와 시험 성공 판정은 후속이다.

다음은 독립 fixture/Unity 실행 환경 확인과 구현자의 읽기 전용 경계 설계다. 그 뒤 baseline 실행과 command 추출을 진행한다. 전송 거절 시 pending 보존/팝업 유지·재시도/자동 rebind/새 ack가 필요해지면 재현과 선택지를 사용자에게 올린다.

Management는 별도 worktree에서 PR147 마무리와 후속 선택지 설계를 맡는다. 전투 정정본 의미 검토 PASS는 `msg_ccd48e9fdcbc`, 다음 작업 prompt 접수·turn 시작은 request `6c0738ae-2f99-4017-828d-abda7386c5a0`다. PR147 병합이나 새로운 MCP/게임/DB 실행 권한은 이 전달에서 부여하지 않았다.
