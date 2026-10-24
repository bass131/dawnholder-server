# 길드 성장 효과 화면 연결

상태: **Main 범위 대조 확정·표준 TDD 선행 시험 준비 중.** Main msg_a13d797a8b70이 v3 C1C425BC…49DA를 사용자 원문/원천과 대조해 승인했다. 제품 쓰기는 선행시험 작성·실제red·쓰기 종료/정산 뒤 신규 구현자부터다. Core 종료 통지와 별도30분 창 전 Unity실행은0이다.


근거 기준: E는 프로젝트 root의 .backups/verification/2026-10-24-growth-effect-screen-link, U는 .backups/verification/2026-10-24-character-entry-prep다. 이 goal의 범위/소비계약 자료·이름만 적힌 로컬 JSON/Markdown은 E 아래 원문을 가리킨다. 원래 승인초안 goal-draft-v3.md/consumer-contract-v3.md와 Main 원문 mail-wake-04.json은 고정 보존한다.

- branch feat/content-growth-effect-screen-link-20261024 / base cf207c1f355ced664e62119152e8b0686b0ec051. 문서checkpoint 전 HEAD는base와같다. UI branch feat/content-character-select-create-20261024의7파일은2215d47a69455dd547a882842d5aee9dfd43a2a7와 E/ui-checkpoint-preserved-v1의exact raw bytes7개에 보존했다. C9 branch에는 UI A6파일/별도UIgoal을 섞지 않는다. 어느branch도현재push하지않았다.

## 재개 지점

**다음 첫 일:** 신규 claude-opus-5-5 선행시험 작성자에게 승인 계약·고정입력을 발행해 C9 요구사항 시험과 실제 red 실행 READY를 받는다. Core 창 닫힘 뒤 Main에게 30분 창을 요청하고 실제 red·보호/자원반환을 확인한다. UI B는 World최종main/API·DLL 대기를 유지하며 UI지정7은 이전branch의 로컬checkpoint2215d47a69455dd547a882842d5aee9dfd43a2a7에 보존돼 있다. 제품/시험 구현을 리드가 대신하지 않는다.

Main의 Core 합동 창07:44:04~08:44:04Z가 진행 중이다. Main 닫힘 통지 전 Unity/dotnet/WSL/build/memory host/fixture·DB/7777 사용과 core-active/world-active 접근은0이다. 자기 worktree Git/파일·gh조회만 한다. 하우징 사용자 직접 플레이45분의 기존 순서는 보존하며 배정이 겹치면 Main에 우선순서를 확인한다.

## 요구사항 원천과 적용 결정

Main msg_792e4216d41c가 전달한 사용자 원문(Main pane Enter 제출2026-10-24 16:5x~17:0x KST, 정확분 미제공)이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다. 원문은 U/mail-current-terminal-5d8d84d5-v1.json이다.

> 음 그럼 레벨 디자인은 엔진켜서 직접 하면 병목이 심하니까 HTML 목업으로 완전 전면 리디자인 초안으로 만들어볼래? 아트 리소스 필요하면 Art쪽에서 병렬로 추가 작업 해달라고 요청하고, 그리고 일단 촬영은 기존의 맵으로 진행

> 그리고 콘텐츠쪽이랑 같이 진행해야하는 Art 부분은 A로 가서 일단 작업물 같이 합쳐서 진행해보는거로 해보자

둘째 문장은 「Art 시연 아트 - 촬영 전 범위를 손질만 남기고 줄일지」의 A에 대한 답이라고 Main이 제공했다. A 설명 원문은 「Art 목표를 촬영 전 손질과 종료 기록으로 줄이고, 타일·대장간·불꽃 시범은 10-28 뒤 후보로 옮긴다. Art 리드가 다시 열리면 범위를 고쳐 메인 대조를 받고, 리허설에서 나온 아트 결함만 고친다」다. 같은 결정 글의 「효과의 실제 화면 연결은 Content 쪽 후보로, 비차단 정리 4건은 백로그로 옮긴다」도 Main 제공 원천이다.

Main 해석은 Content와 함께 진행할 Art 부분을 PR254/262 성장 효과의 실제 소비 연결(C9)로 읽은 것이다. 10-26 촬영은 main의 원본 Town/HuntingGround/BossRoom이며 Level GrowthRoute 복제 맵은 제외한다. 이 goal은 그 Main 해석과 기존 길드 goal의 C9 보존 계약을 적용하며 Main a13의 범위 확정을 따른다.

## 만들 것

이미 main에 병합된 Art 효과를 기존 길드 UI의 실제 구성/이벤트 수명에 연결한다. 기존 GuildRequestController.GrowthConfirmed를 소비하고 확정된 레벨 값만 GuildGrowthEffectView.TryPlayConfirmed에 즉시 전달한다. 새 효과·임시 글자·별도 경제 판정은 만들지 않는다.

- 제품 구성 주체는 기존 GuildUiController를 재사용한다. Town의 CombatBootstrap.BuildGuildUi→GuildUiController.BuildRuntime(parent)에서 Art BuildRuntime(parent)를1회 호출한다. parent는 `_CombatBootstrap.transform`이며 효과는 GuildUi/GuildWindow/BlacksmithWindow와 형제 root다. 독립Canvas를 GuildUi canvas 아래 중첩하지 않는다. Content가 생성/참조·source별delegate/구독해제·파괴를 소유하고 별도 session polling owner나 연결 stack을 만들지 않는다.
- 기존 event가 소유한 거점1→2+성장 표식, 개인 성공47+대응44의 확인·중복 억제를 복제하지 않는다. callback에서 현재 확인 값을 읽어 값으로 전달하며 State 참조를 Art에 보관하지 않는다. 첫 동기화/Query/재접속/중복 결과는 효과를 재생하지 않는다.
- 표시 gate는 승인된 현재Town의 기존 CanPurchase()가 참인 상태다(현재연결/scene·Ready/HP/시간·전환/기존우선창). Guild/Smith창 닫힘만으로 확정 효과를 취소하지 않고 영역을 갱신한다. Town이탈/source교체·owner/parent disable·destroy/메뉴·씬전환·우선창 gate닫힘은 CancelAll·구독정리하며 옛 사건을 replay하지 않는다. Town한정은 Main msg_a13d797a8b70이 기존CanPurchase gate 재사용으로 승인했다. 요청뒤Town을떠난동안확정이도착하면효과0인취소경로를시험한다.
- Art의 TrySetAvailableArea를 사용한다. 기존 두창은440×566의 half-screen 배치이므로 그대로 충분한 영역이 있다고 가정하지 않고 Content소유 layout에서 효과영역/두창영역을 함께 확보한다. 전체표시 panel/root bounds를 실제 effect-root canvas0..1로 변환해 첫재생전·창변경/resize마다 전달한다. Art 사설 child 탐색0이며 필수본문/버튼·HUD 가독성 미달은 결함으로 남긴다. false/예외는 취소·기록하고 정상사건 생략/지연·창숨김으로 PASS하지 않는다. 구체 설계는 consumer-contract-v3.md를 따른다.

## 건드릴 곳

**Art 합의v3가 구체 root/수명·Content4파일/Art쓰기0와 두해상도 조건에 동의했다. Town표시정책과 전체범위·표준TDD/강도는 Main a13으로 확정됐다. 실제치수/창은 미측정·미배정이다.**

| 소유 | 승인 범위 | 이유/한계 |
|---|---|---|
| Content 구현자 | 03_Client/Assets/Scripts/UI/GuildUiController.cs, GuildWindowLayout.cs, GuildWindow.cs, BlacksmithWindow.cs | caller수명/값전달과 Content창 전체geometry·배치/읽기전용 occupied 연결을 함께 소유. 기존meta/GUID 보존 |
| 선행 시험 작성자→쓰기 종료 뒤 독립 검증자 | Assets/Tests/EditMode·PlayMode의 C9 소비 연결 신규 시험과 신규meta | 요구사항·실제 구성/수명·시각/입력 경로를 확인. 실제 파일명/전용fixture는 계약 발행 시 한정하며 기존 기대값은 변경하지 않음 |
| Content 리드 | 승인 뒤 새 goal.md, CURRENT의 Content 줄, domains/client.md의 해당 소비 계약 | 새 동작·소유/미실행 경계를 현재 문서에 연결. 다른 파트 줄 보존 |
| Art | UI.unity 기존HUD, GuildGrowthEffectView/Presentation·atlas 및 자산/아트 결함 | 현재 C9에서 Art 제품 쓰기를 요청한 것이 아니다. 실제 결함 발생 시 파일별 소유/수정 범위를 별도로 합의 |

Art 코드·atlas·Resources 경로는 그대로 읽어 소비한다. 소비만으로 해결되지 않는 Art API/레이아웃 결함은 번호·근거로 Art 리드에 돌리고 같은 파일을 동시에 쓰지 않는다. UI.unity와 원본3맵/프리팹은 Content 쓰기에서 제외한다. Art소유/Content동작 소유 원칙은 Artv1/v2에서 확정됐고 Content가 수락한다. 추가 Art제품 쓰기는0이며 구체4파일/root는 Art합의v3를 받았으며 Town gate는 Main a13에서 확정됐다.

## 하지 않을 것

- 캐릭터 선택·생성 B, 서버/경제·강화 규칙, PDL·버전·공유DLL, DB/seed/계정 대체, NetworkService/UnityClientSession/GuildRequestController의 API 변경.
- Level HTML/맵 리디자인·GrowthRoute, 타일·대장간·불꽃 시범, 신규 아트, 전역 UI 계층/정렬 재설계, 과거 비차단 정리4건.
- 사용자 보호 03_Client/Assets/Resources/MinimapRT.renderTexture의 어떤 checkout에서의 commit·되돌리기. 기존 UI7 변경의 무차별 add/stash/폐기·재작성.
- 승인된선행시험→구현 순서를넘긴제품쓰기, Main창전자동Unity실행, Core창중무거운실행·DB·7777·core-active/world-active 접근, PR병합/원격main push.

## 관찰 가능한 완료조건

1. 기존 CombatBootstrap→GuildUiController 실제 구성 경로에서 Art view가 씬 소유로 하나 생성되고 현재 GrowthConfirmed에만 연결된다. GUID/자산경로·기존 gameplay 진입은 보존한다.
2. 확정 거점/개인 사건에서 실제 Art 효과가 각각 한 번 표시되고 해당 확정 레벨과 기존 본문·비용·결과를 읽을 수 있다. 첫 동기화/Query/재접속/중복/미일치 결과에서0회다. 권위·pending을 새로 소유하지 않는다.
3. source 교체/disable·destroy/메뉴·씬 전환/취소·우선 창에서 잔류·재생 예약·구독 누수0이며 UI raycast/입력과 기존 길드·대장간 동작은 보존한다. 승인된Town한정/창닫힘지속·source별delegate/CancelAll 순서를 따른다. 요청뒤Town이탈중확정도착은효과0으로시험한다.
4. Art 인계의1920×1080·1280×720 및 양방향resize에서 실제 통합 경로의 캡처/정확 입력·관측으로 효과전체/라벨·두창/필수본문·버튼·HUD 가독성/입력을 확인한다. v1의709×399·802×451은 기존C8 미확인 경계로 유지하며 이C9에서 통과 처리하지 않는다(Main a13으로 범위 확정). Art 과거 fixture PASS를 새 Content 연결 PASS로 재사용하지 않는다.
5. 새 외부 독립 검증이 실제 diff·원시·관련 실패/수명과 실제 진입1회를 판정한다. 대역을 쓴 경계/실서버·DB·촬영 계정 미확인을 명시하고 미실행을 성공으로 바꾸지 않는다. 실게임의 두 성장 장면 관측 입력/창이 없으면 그 완료조건은 남긴다.

## PR 경계와 전환 방법

- 별도 C9 제품 PR 하나에 소비 연결+관련 시험/현재 계약 문서만 담는다. 캐릭터 UI6/기존 UI goal 변경과 Art 자산/맵 재설계는 섞지 않는다. 목표 종료 기록은 운영 정본 순서와 별도다.
- UI branch의 지정7개만 로컬checkpoint2215d47a에 보존했다. 메시지/해시·원시7사본을 남겼고 B는미완료다. 보호Minimap/다른사용자변경은제외했으며UI branch push0이다.
- latestmain CF207을 재대조해 기존 content-active에서 별도 feat/content-growth-effect-screen-link-20261024 branch로 전환했다. 비교 기준이 cf207c1f와 바뀌면 차이·계약 영향만 Main에 올리고 고정 입력을 조용히 바꾸지 않는다. 새 worktree는 제안하지 않는다.
- C9 새 branch에서 정본 goal 01_Phases/goals/2026-10-24-growth-effect-screen-link/goal.md와 CURRENT의 Content 줄을 만든다. UI B는 이전 branch/checkpoint로 돌아가 World 입력 뒤 재개한다. 미병합 UI goal의 상대 링크가 새branch에서 없으면 깨진 링크를 만들지 않고 branch/checkpoint·로컬근거로 인계한다.
- PR 생성은 허용 범위이며 병합은 Main이 사용자 「병합 승인: PR번호 head40자」를 받아 직접 한다. 이 범위 대조가 병합 승인이 아니다.

## 검증·Unity 창 예산과 촬영 전 전망

강 등급으로 확정됐다(소유/구독·실패 수명·실제 Unity/표시 경로). 신규 goal의 표준 TDD 기준으로 아래 **3창/총135분**을 추정한다. 모두 Main의 비중첩 배정과 실제 자동 관문 통과가 필요하며 아직 실행/창 요청은0이다.

| 순서 | 제안 창 | 하는 일 | 미확정/비용 기준 |
|---|---:|---|---|
| 1 | 선행 시험30분 | 신규 Opus의 요구사항 시험·기존 연결 부재 red | 기존 actual Unity import/시험 625초는 다른 입력의 역사값이며 이시간의 보증이 아님 |
| 2 | 구현자45분 | 신규 Astra xhigh 구현 뒤 compile·Edit/Play 자체 확인·정상 반환 | 제품/시험 고정입력·실행명령은 실제 계약에서 고정 |
| 3 | 독립60분 | 신규 Opus 독립 검증의 강 검증·실제 통합 진입·수명/입력·두 해상도(1920×1080/1280×720)·resize/두효과 관측 | 실서버 관측을 포함할지는 계정·서버 입력/소유·창을 Main과 고정. 확보 못하면 실게임 완료조건 남음 |

Main msg_a13d797a8b70은10-22경량 승인이 당시UI goal 한정임을 확인하고 새 C9로 넓히지 않았다. 이 goal은 신규Opus선행시험30분→신규구현자45분→신규독립강검증60분의 표준경로다. 독립세션을 생략하지 않는다. 실서버/촬영계정은 현재고정하지않고 창·입력이 없으면 해당실게임완료조건을남긴다.

예상 소요는 Art 계약·소유 합의/범위 고정 당일 + 구현/검증 0.5~1일의 미측정 추정이다. 필요한 창과 실게임 입력이 10-25까지 확보되면 10-26 촬영 전 끝낼 수 있는 안으로 본다. 창 실패·실게임 입력/미확정 게이트가 남으면 촬영에서 효과가 보이는 완료조건은 미완료라고 Main에 보고하며 fixture나 문서로 대체하지 않는다. housing45분과 충돌할 배정은 Main이 기존 순서로 조율한다.

창 요청 시 세 줄은 다음 경계를 담는다: (1) 단계/필요30·45·60분 및 시작·반환 여유, (2) 단일Unity·정확 새snapshot/PID·birth/서버 등 소유·다른파트 비중첩, (3) source/DLL/pin/물리·commit/잔류runtime·부작용 before/after/CAS·정확 종료와 반환 우선. 실제 관문 수치는 Core 종료와 새 입력 고정 뒤 Main과 한 번 확정하며 지금 검사/보호PS 구현은0이다. 선택Prefs/Registry/source·사용자Minimap 보존, 공유TestResults는 각 실제 진입 직전 fresh before/반환대조, GiCache는4메타/내용hash·복사·삭제·복원0의 기존 Content 정책을 적용할 준비안을 제시한다.

## 진척 단계

- [x] 범위·소유 조율
- [x] 브랜치 보존·전환
- [>] 요구사항 시험
- [ ] 소비 연결 구현
- [ ] 독립 강검증
- [ ] 제품 PR 병합
- [ ] 종료 점검

## 현재 근거와 열린 점

기록시각 2026-10-24T07:56:41Z(date -u). read-inputs-v1.json은 main7경로의 Git blob과 consumer4파일의 현재HEAD/main 일치를 확인한다. main Assets의 view GUID는 자기.meta만, 제품Scripts의 view 이름은 선언/자기BuildRuntime·주석뿐이었다. 기존 GuildRequestController에는 GrowthConfirmed가 이미 있고, GuildUiController는 현재Changed만 구독하며 CombatBootstrap이 BuildRuntime을 호출한다. 이것은 정적 관측이며 컴파일·렌더/실게임 검증이 아니다.

Art합의v3와 HUD정적참조/동적배치 한계는 art-agreement-intake-v3.json과 consumer-contract-v3.md(SHA256 A7AF0365B27E6ABC5B2D73C216CED6CB7BD16EC6C1EB03B337627CD34AEFD3B3)에 연결했다. 인터페이스/root·수명·Content4파일/Art쓰기0·두해상도는 Art합의됐고 Town정책/표준TDD·강도는 Main a13으로 확정됐고 실게임계정/서버/창은 아직 고정하지 않았다. 공개HUD API/확정safeRect는 없고 private조회·provider/Art파일쓰기를 자동추가하지 않는다. 제품쓰기는 선행시험 red·쓰기종료/정산 뒤 신규구현자에게만 발행한다. 보호Minimap/기존UI7/병합승인 경계와 Core창 제한은 유지한다.

### v3 정정/합의 기록

Art msg_cb262ccef2a6가 지적한 v2 독립검증 표의 「세 해상도/두효과」 잔류 문구를 두 해상도1920×1080/1280×720와 resize로 통일했다. v1/v2 원본은 보존한다. 709/802는 기존C8 미확인으로 남기고 C9 통과로 덮지 않는다. 공개HUD occupied API가 없으므로 root/직렬화 참조를 제품private API나 실치수로 쓰지 않는다. HudLayout의 Q/E0·1·2 및 NPC/보상도킹·HUD입력/가독성은 실제 검증에 포함하며 현재 수치는미측정이다.


## Main 범위 확정과 실제 전환

- Main msg_a13d797a8b70은15426B/C1C425BC…49DA 전문과사용자2원문·현황판A/원천표본을대조해 범위4절·조건1~5·PR경계/4파일·신규시험/goal·CURRENT·client계약을확정했다. 이대조는사용자PR병합승인이아니다. 원문E/mail-wake-04.json과인수보고에보존했다. from_handle/본문태그는현재Main과일치했고자동Re제목의R3형식관찰은별도교정통보했다.
- Core창중자기Git·문서는허용돼 UI7정확목록·기존stage0·훅부작용을확인하고2215d47a checkpoint했다. ProtectedMinimap은stage/commit/revert0·전후SHA일치, 원래UI6지문일치·원시사본7/각Gitblob·실제커밋7집합일치다. E/ui-checkpoint-preserved-v1.json과 c9-branch-entry-v1.json이근거다. main은여전히CF207이고별도C9branch로전환했다. push/제품변경/Unity·DB실행은0이다.
- Art합의는 인터페이스/root·소유·두해상도검증의범위이며표시정책/검증루트는이Main확정으로닫혔다. 실치수·가독성/실게임입력·창은실행전/미측정으로남고HUDprovider/private조회·Art쓰기0경계는유지한다. 제품쓰기는선행시험작성자의쓰기종료뒤새구현자부터다.


### 역할·모델 배정

선행시험은신규 claude-opus-5-5, 구현은신규 gpt-6-astra xhigh, 독립검증은구현자와별도의신규 claude-opus-5-5다. 구현배정은 implementer-routing.md 신호2(확정이벤트순서·source/scene수명)와4(새다파일화면/레이아웃판단)를적용했으며, Astra구현의독립검증은모델시범에따라Opus다. Main이확정한강등급·선행30/구현45/독립60분경로를유지하고선행Opus세션을독립검증에재사용하지않는다. Codex새세션에는service_tier fast를붙이고최초thread_settings priority를확인한다. 현재제품/시험쓰기·새Unity실행0이다. 기록시각 2026-10-24T08:28:09Z (date -u).
