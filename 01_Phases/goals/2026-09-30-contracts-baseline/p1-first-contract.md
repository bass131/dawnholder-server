# P1 첫 구현 계약안 — 파티 초대 응답 요청 경계

작성 지정 gpt-6-astra / 실제 runtime unknown. P0 goal의 보존·책임 분리 계약과 client-boundary-inventory의 파티 행을 기준으로 작성한 **설계안**이다. 구현·테스트 실행·P1 목표 완료가 아니다. 생산 근거는 고정 main `c27b03e888986f2ec8c593cd6c626a9c515595e1`; 정식 착수 시 최신 main 차이를 확인한다. 상태·진행·승인 결과는 정식 goal 한 곳에 둔다.

## 첫 조각과 소유자

`PartyInvitePopup.SendRespond`의 초대 존재/세션 확인·C_PartyRespond 조립·SendIntent 호출·pending 소비를 기능별 `PartyInviteResponseCommand`(가칭)로 옮긴다. Popup은 Accept/Reject 의도, 클릭음, 기존 HidePopup와 표시/구독만 소유한다. 새 전역 UIManager·이벤트 버스·공통 command framework를 만들지 않는다. 서버/PDL/생성 코드/UnityClientSession의 송신 정책은 이 조각에서 바꾸지 않는다.

- 서버 PartyRegistry/PartyFlow가 가입·해산을 결정한다. 클라이언트 PartyState의 membership은 S_PartyUpdate를 반영한 mirror다.
- PendingInviter/HasPendingInvite는 아직 응답하지 않은 초대의 임시 상태이며 command가 기존 시점에 소비한다. command는 PartyId/member/leader를 낙관적으로 변경하지 않는다.
- 실제 session 참조는 호출 동안 현재 provider에서 얻고 장기 보관하지 않는다. 요청에는 accept와 현재 pending inviter ID 값을 사용한다. 화면이 넘기는 임의 inviter ID를 새 권위로 만들지 않는다.
- 작은 내부 클래스와 필요한 의존 주입만 허용한다. PartyState와 현재 UnityClientSession을 조회하는 provider/좁은 delegate 정도로 시작하고, 테스트 편의만을 위한 여러 service interface·registry는 만들지 않는다. provider를 호출하는 횟수/시점도 기존 main-thread 동작과 맞춘다.

## 보존 표 — 현재 소스의 관찰, 실제 플레이 재현 완료 아님

|조건|현재 SendRespond 결과|버튼 경로의 표시·서버 상태|
|---|---|---|
|PartyState 없음|즉시 return|클릭음 호출 후 HidePopup 실행; 서버 membership 변경 없음|
|pending 없음|경고 후 return|HidePopup 실행; 송신/소비 없음|
|session 없음 또는 HandshakeOk=false|경고 후 return|HidePopup 실행; pending 유지|
|pending+handshake, entry not ready/현재 세션 아님/closed|SendIntent는 내부 gate로 silent drop 가능. 호출이 정상 반환하면 pending 소비·기존 로그|HidePopup 실행; membership은 그대로. pending 소비를 송신 성공이라고 해석하지 않음|
|gameplay-ready|C_PartyRespond에 inviterEntityId와 accept(수락1/거절0), SendIntent 호출 후 pending 소비|HidePopup; 서버 update/error가 오기 전 가입 확정 아님|
|Editor 지연 송신 예약 후 generation/epoch/closing 변경|지연 callback에서 추가 gate로 전송 폐기 가능; pending은 이미 소비됨|재시도/되살리기 없음; 서버 확정 아님|
|직렬화/SendIntent가 예외를 던짐|현재 pending 소비와 뒤의 HidePopup까지 도달하지 않음(버튼의 클릭음은 앞서 호출)|새 catch/finally로 강제 소비·닫힘·예외 삼키기를 추가하지 않음|

S_PartyUpdate→OnPartyUpdated의 HidePopup도 유지한다. BuildRuntime의 버튼 연결/serialized field 이름·리소스 경로·색/음성/레이아웃은 변경하지 않는다. “원래 정상 UX” 또는 “이미 재현한 버그”라고 판정하지 않고 위 분기를 독립 fixture로 먼저 확인한다.

## 완료 의미: 이름으로 없는 보장을 만들지 않기

현재 `SendIntent`는 void이며 CanSendGameplay 게이트와 Editor 지연 재검사를 가진다. 첫 조각은 이를 bool 송신 성공 API로 변경하지 않는다. command의 내부 결과가 필요하다면 `NoPendingInvite`, `SessionUnavailable`, `SendIntentInvoked` 정도의 **관찰 가능한 로컬 분기**만 표현한다(최종 이름은 구현 선택). 마지막 값은 송신 함수 호출·정상 반환을 뜻할 뿐 queue accepted/delivered/server accepted가 아니다. 결과형 자체가 소비자에 필요 없다면 void+계약 주석으로 충분하다. 팝업 닫힘은 이 결과에 따라 새로 분기하지 않는다.

신뢰할 만한 LocalAccepted가 필요해지면 송신 경계에서 실제 접수 의미를 별도로 정의·검증해야 한다. 사전 CanSendGameplay 조회로 race/지연 폐기까지 성공이라고 추론하지 않는다. 이 작업은 첫 추출의 필수 범위가 아니다.

## 같은 source 구독 수명

현 Popup은 OnEnable/OnDisable에서 각각 PartyState.Instance를 조회한다. 실제 source 교체가 생기는지, 이벤트가 오래된 popup을 부르는지는 미재현이다. 기본 계약은 **구독한 source를 보관하고 그 source에서 해제**, 중복 bind를 막고 disable/destroy 후 구독 책임을 남기지 않는 것이다.

첫 추출과 구독 보강은 별도 커밋/검증 조각으로 나눌 수 있다. 구독 조각은 Popup 한 파일에만 한정하고 실제 source를 기억하는 필드/대칭 bind-unbind로 충분하다. OnEnable에 source가 없을 때 자동 polling, 활성 중 자동 새 source 발견/재구독, pending replay, popup 자동 복원은 추가하지 않는다. callback이 bound source의 이벤트인지와 조회하는 상태가 같은지 테스트로 분리한다. 기존 경로와 source 교체 fixture에서 다른 동작이 드러나면 그것을 운영 중 버그 재현으로 부르지 않고 변경 필요성/실제 도달 조건을 보고한다. 화면 정책 변경이 필요하면 사용자 결정 뒤 분리한다.

## 독립 TestCode안과 순서

1. 독립 Astra가 고정 baseline의 실제 Popup 버튼/SendRespond 경로 characterization을 먼저 작성한다. no-state/no-pending/no-session/no-handshake/entry-not-ready/ready accept·reject를 구분해 packet 유무, pending, CanvasGroup, membership 불변을 확인한다. 경고는 LogAssert로 기대하고 다른 로그 실패를 숨기지 않는다.
2. packet 확인은 실제 serializer의 기대값을 다시 호출해 비교하지 말고 peer frame을 decode하여 ID/inviter/accept를 계약값으로 검사한다. 기존 `TestSocketPair`, `ManualConnectionQueue`, `EntryBindingFixture`, `SessionTestTools.Handshake`를 필요한 부분 재사용한다. loopback만으로 실서버 가입 완료라고 하지 않는다.
3. Editor 지연 callback은 기존 `DelayedIntent_OnlyCurrentLiveSessionSends` fixture와 동일한 제어된 queue로 command→SendIntent 실제 경로를 검사한다. scheduling→pending 소비→current/entry 무효화→wire 없음과, 유효 조건의 양성 송신을 짝으로 둔다. 단순 fake sender 호출만 확인해서 gate를 검증했다고 하지 않는다.
4. 추출 후 같은 public/UI adapter 계약을 실행하고 새 command의 예외/분기 테스트를 별도로 추가한다. source가 다르면 helper만 바꾸고 기대값은 baseline 계약을 유지한다. baseline 테스트는 새 command API를 요구하지 않게 설계한다.
5. 구독 조각은 enable/disable/re-enable, 같은 source에서 정확 해제, source 교체 후 old event와 new event를 분리한 fixture를 둔다. static Instance 교체를 테스트에서 강제한 것과 실제 게임의 교체 도달성을 구분한다. 모든 fixture는 object/session/socket·singleton·latency를 자신이 만든 범위에서 복원한다.

파일 소유 후보: Sol은 `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs`, 신규 기능 command 파일+새 meta만. 필요 없는 PartyState/UnityClientSession 변경은 제외한다. 기능 파일의 실제 폴더는 기존 assembly 안에서 선택한다. 독립 Astra는 신규 EditMode `PartyInviteResponseContractTests.cs`와 필요할 때 `PartyInvitePopupBindingTests.cs` 및 새 meta를 소유한다. 기존 generation/mirror test 또는 asmdef 수정은 필요가 확인되면 구체 파일을 별도 배정한다. Popup에 production/test 동시 쓰기 없음. 테스트 소유자가 baseline fixture 작성할 동안 구현은 설계만 준비하고, 파일 소유 충돌 없는 시점에 구현→검증으로 인계한다.

## 실행 조건과 완료 제한

실제 Unity6000.4.7f1 EditMode에서 대상 테스트→영향 있는 기존 generation/mirror/entry 회귀→최종 필요한 전체 EditMode 1회 순서로 검증한다. mainThread lifecycle와 Runtime Build 버튼 wiring은 가능한 실제 Component 경로로 검사한다. 구현과 테스트가 안정되기 전 전체 suite 반복을 완료 수단으로 삼지 않는다.

기존 사용자 Editor 종료 금지. 명시 배정된 검증자가 유일 소유 Hidden batch/PID/timeout과 XML/exit를 기록한다. 사전 DEVELOPMENT 확인, Unity assets/meta/GUID와 Shared/ClientNet DLL 전후 hash 보존, 새 meta GUID 충돌 확인. baseline 비교는 고정 소스의 fixture 실행 또는 메인이 명시 허용한 좁은 임시치환/복원으로만 수행하고 원본을 임의 교체하지 않는다. .NET 성공은 Unity 성공을 대신하지 않는다. 서버/SQL/PDL 생성/원본 DLL 복사는 불필요하며 자동 실행 범위에 넣지 않는다.

실제 클릭·씬 전환·오디오와 서버 가입 왕복이 필요하면 별도 소유 자원/시나리오를 정한다. EditMode로 미실행 영역을 성공 처리하지 않는다. 처음부터 UI 전체 smoke 인프라를 구축하는 것은 필수 조건이 아니다.

## 사용자 선택이 추가로 필요한 경계

정책 변경 없는 command 추출·대칭 same-source 해제·보존 fixture는 기존 합의 안에서 구체화할 수 있다. 재현 후 **drop에도 pending을 소비할지**, **거부/미접수 때 popup을 유지·재시도할지**, **활성 중 새 source 자동 재구독/초대 재표시를 할지**, **확정 ack·에러 UI를 새로 도입할지**는 영향과 선택지를 올려 결정한다. 현재 동작을 바꾸겠다고 미리 약속하지 않는다. 승인된 P0 완료/통합과 정식 P1 goal 뒤 착수하며 PR 생성 후 병합 직전에는 별도 사용자 명시 승인이 필요하다.

## 좁게 확인한 근거

고정 c27b03e: PartyInvitePopup의 OnEnable/OnDisable, OnInviteReceived/OnPartyUpdated, 버튼/SendRespond, BuildRuntime 연결; PartyState의 Instance/ApplyUpdate/Clear/ResetSession/SetPendingInvite/ClearPendingInvite; UnityClientSession의 CanApply/CanSendGameplay/SendIntent·지연 gate. 테스트는 ClientSessionGenerationTests의 fixture와 DelayedIntent_OnlyCurrentLiveSessionSends, ClientSessionMirrorResetTests의 reset/cleanup fixture, EditMode asmdef를 필요한 부분만 읽었다. 기존 테스트가 파티 popup을 이미 검증한다고 주장하지 않는다. 코드/테스트/정책 실행·수정 없음. 이 파일 writer-end.
