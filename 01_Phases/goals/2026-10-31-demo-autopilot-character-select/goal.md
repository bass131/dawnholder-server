# 시연 자동 진행기 DB 캐릭터 메뉴·캐릭터 선택창 3칸·길드 입구 겹침

11-06 시연 영상을 DB 영속 서버 한 판으로 찍을 수 있게 한다. 캐릭터 선택창을 3칸 넓은 화면으로 바꾸고(85 A), PR268 자동 진행기가 그 화면을 지나 끝까지 가게 고친다(82 B·83 A). 길드 입구가 Gold 칸을 덮는 겹침도 고친다(74 A). 녹화용 빌드 계약은 Core 리드와 맺는다.

## 재개 지점

**다음 첫 일:** PR268 자동 진행기 구현자 계약을 신규 Codex Sol max(`[Content Sol]`)로 발행한다. 선행 시험(R1~R5)은 commit됐고 U1에서 새 API 부재로 컴파일이 실패했다(진행 기록 10-31 「자동 진행기 시험」). PR268 branch에는 PR-A와 최신 main을 합쳤다. 캐릭터 선택창(PR-A, PR #291)은 독립 검증을 통과해 병합 준비 보고를 올렸다. PR-C(길드 입구)와 Art PR4 사후 확인은 사용자 결정 90 A로 리셋(11-03 20:00 KST) 뒤 메인이 다시 열 때까지 착수하지 않는다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1을 메인이 원문과 대조해 승인했다(`msg_967d20c30e6d`, 메인 결정). 초안 원문은 `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/scope-draft-v1.md`(sha256 `28fa67062d6395521aad8f8022e87387ca7a43dec18c101c3aed4f157756ed15`)다. |
| branch | PR-A `feat/content-demo-character-select-20261031`(PR #291), base main `5bfda911a8ba97a8f9f82b75693dec3ebc59fb54`. PR268 `feat/demo-autoplay-20261028`, base main 그대로(메인 `msg_db6333b6b1c6`), content-active 로컬 이름 `content/demo-autoplay-20261028`. PR-A와 최신 main을 합쳐 그 위에 쌓는다. PR-C `fix/content-guild-entry-gold-overlap-20261031`(아직 없음), base main |
| 리드 | `[Content 리드 Opus]`(claude-opus-5-5 xhigh), Run `run_0a58bc3cfea0`, 회신 주소 `run:run_0a58bc3cfea0` |
| 근거 폴더 | `.backups/verification/2026-10-31-demo-autopilot-character-select/` |

## 적용 중인 사용자 결정

메인 진입 메시지 `msg_f6f3c9b1a51e`(2026-10-31T14:31:28Z)가 전달한 사용자 원문이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

- 10-27 결정 4 → A: 리드까지 전부 Claude, 독립 검증만 GPT Astra(CLAUDE.md 「결정 4A 적용 기간의 Claude 리드·구현자」).
- 10-31 「대시보드 결정 응답: 2) 82 - 시연 영상을 두 번 나눠 녹화해 편집으로 이을지, 자동 진행기를 고쳐 DB 서버 한 판으로 찍을지 → B 자동 진행기를 고쳐 한 판 · 3) 83 - 자동 진행기(PR268)를 독립 검증 뒤 병합해 최신 GUI 빌드로 녹화할지 → A 검증 뒤 병합」.
- 10-31 79 C 코멘트 「시연은 영상으로 보여줄 예정임」, 72 B(시연은 빌드로), 80 A(DB에는 새 시험 계정·캐릭터 행만 허용, 새 DB·DDL·행 삭제 없음).
- 10-31 78 C 코멘트 「플레이에는 문제가 없음, 근데 캐릭터 선택창에 빨간 박스 부분으로 내가 강조한 부분이 쓸데없이 공간을 많이 차지함, 캐릭터 선택 슬롯을 4개로 하고, 빈 캐릭터칸 선택시 새로운 캐릭터 생성 UI로 진입하게 수정, 나머지는 괜찮음」. 빨간 박스는 캐릭터 선택창 오른쪽 설명 상자다.
- 10-31 「85 - 캐릭터 칸을 지금 4개로 늘릴지(DB 구조 변경 포함), 3칸으로 화면만 먼저 고칠지 → A 3칸으로 화면만 지금」. 4칸은 11-06 뒤 후보(BACKLOG `character-slot-count-four`)다.
- 10-31 74 A: 「길드 · G」 입구가 Gold 칸을 덮는 겹침을 작은 목표로 고친다(배분 `msg_ad91509e146b`·`msg_141a6249a95c`).
- 10-30 원문: 「모든 GUI 디자인은 인벤토리와 캐릭터 선택창 기준으로 완전 최신화 진행해줄것」. 최종 제출은 11-06이다.

### 10-31 범위 초안 v1 처분 — msg_967d20c30e6d

메인 결정이다(사용자 결정 아님, 2026-10-31T14:48:03Z).

1. 85 A를 먼저 하고 PR268을 그 위에 쌓는다. PR-A 구현 직후 객체 이름을 고정해 PR268 선행 시험을 겹치는 것도 승인.
2. PR-A 병합 전까지 PR268 base를 PR-A branch로 바꾼다. 바꾼 시각과 receipt를 이 goal에 남긴다. → 메인 `msg_db6333b6b1c6`(2026-10-31T17:21:43Z, 메인 결정)로 대체됐다. 리드의 base 변경 명령이 자동 권한 판정에 막혔으므로 메인이 대신 실행하지 않고 생략한다. PR-A가 먼저 main에 들어가면 PR268 diff는 같아진다. PR268 설명에 「PR-A 병합 전에는 PR-A commit이 함께 보인다」를 한 줄 적는다.
3. 이 goal 동안 PR268 branch의 commit/push는 Content 리드가 한다. `town-direct-20261027` worktree는 건드리지 않는다. fast-forward만 민다(force push 금지).
4. PR-C는 NPC 대화·던전 보상 창이 떠 있는 동안 입구를 숨긴다. 사용자가 마지막 캡처에서 다르게 원하면 그때 바꾼다.
5. DB 영속 서버 실주행(창 8)을 PR268 병합 전 검증의 실제 경로로 삼는다. 서버 기동은 Core 카드 v2 블록 그대로 사용자 손이다. 창 8 시각이 보이면 Core 리드와 먼저 맞추고 메인에 청한다. 같은 서버 세션 안에서 Core 쪽 확인을 함께 하는 등 사용자 손을 줄이는 안이 있으면 같이 올린다.
6. `-demo-character`가 없으면 만들지 않고 멈춘다.
- 순서 조정(사용량): Claude 주간 사용량 82%로 사용자 결정 90(A 녹화 길만 Claude / B 구현자만 Codex)이 올라갔다. 답이 올 때까지 PR-A → PR268은 진행하고, PR-C와 Art PR4 사후 확인은 착수하지 않는다. 리드 턴을 아낀다.

### 10-31 사용자 결정 90 A — msg_3089e44cef4f

메인 `msg_3089e44cef4f`(2026-10-31T14:53:28Z)가 전달한 사용자 원문이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 90 - Claude 주간 사용량 82% — 이 속도면 내일 저녁 소진, 리셋(화 20:00)까지 약 50시간 멈춤 → A 녹화 길만 Claude로

- 선택지 A 원문: 「Content(자동 진행기·선택창 3칸)와 Core(녹화)만 계속한다. Art는 PR4 캡처·검증까지만 하고 75(버튼 진하게)는 리셋 뒤로 미룬다. 메인은 대기 간격을 늘리고, 기다리는 리드는 현황판 대기로 둬 자동 깨움을 줄인다」. 리셋은 2026-11-03 20:00 KST다.
- 이 goal에서의 뜻(메인 지시): PR-A → PR268 → 녹화용 빌드 계약까지 진행한다. PR-C(74 A)와 Art PR4 사후 확인은 리셋 뒤 메인이 다시 열 때까지 착수하지 않는다. 범위 초안 v1은 그대로다.
- 사용량이 리셋 전에 바닥나면 리드·구현자가 함께 멈춘다. 단계 끝마다 이 재개 지점을 고치고 작업자 계약·근거 경로를 파일로 남긴다. 턴을 아끼고 작업자 계약에도 같은 원칙을 적는다.

### 10-31 작업자 Codex 전환 — msg_374115bbd7af

메인 `msg_374115bbd7af`(2026-10-31T16:46:14Z)가 전달한 사용자 원문이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 그러면 작업자를 Codex로 바꿔줘, 이번에 사용한도 다시 2배 되었거든

- 같은 메시지로 온 바로 앞 원문: 「대시보드 결정 응답: 1) 92 - 인벤토리 4칸 격자 개편을 리셋 뒤에 할지, 지금 녹화 준비와 함께 할지 → B 지금 함께 · 2) 93 - 아이콘만 남은 칸에서 아이템을 어떻게 쓸지(지금은 줄마다 「사용」 버튼) → A 우클릭으로 사용」. 92 B로 Art가 인벤토리 4열 격자를 녹화 준비와 함께 만든다.
- 메인 해석(사용자 결정 아님): 「작업자」는 구현자와 선행 시험 작성자다. 둘은 신규 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`, 태그 `[Content Sol]`이다. 같은 계약에서 Sol 구현이 두 번째 독립 불합격을 받으면 다음 시도는 신규 `gpt-6-astra` xhigh `[Content 구현 Astra]`다. 리드(Claude Opus)와 독립 검증자(신규 GPT-6-Astra xhigh)는 그대로다.
- 첫 화면에 지정 모델·max가 없으면 대체하지 않고 메인에 보고한다. 업데이트 창이 뜨면 Esc로 넘긴다. 실행 중인 작업자의 모델은 바꾸지 않는다.
- 적용: PR268 자동 진행기의 다음 선행 시험 작성자·구현자부터다. PR-A 작업자는 이미 Claude로 끝났다.
- 빌드 입력 보충(메인 `msg_d0012352013c`, 16:48:55Z): 시연 빌드 입력에 「Art 인벤토리 격자 PR 병합」을 PR-A·PR268 병합과 함께 더한다. 빌드는 셋이 모두 main에 들어간 commit에서 만들고, 격자 PR이 늦으면 기다린다. Core는 리허설 착수 조건에 같은 항목을 더했다(Core `msg_4ea40b55db0c`).

### 운영 방식(진입 메시지의 메인 결정)

- 병합은 PR마다 사용자 명시 승인으로 메인만 한다. 병합 준비 보고에는 head 40자, 판정 원문 경로·sha256, CI를 넣는다.
- Unity는 한 번에 하나다. 창마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」 세 줄로 청한다. 관문(물리·commit 여유 각 4 GiB, Unity·Windows dotnet 0, WSL 서버를 함께 쓰면 물리 6 GiB)은 매번 저장 스크립트 `scripts/measure-gate.ps1`로 잰다.
- 선행 시험 작성자·구현자는 PR-A까지 신규 claude-opus-5-5 xhigh였고, PR268부터 위 「작업자 Codex 전환」을 따른다. 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 정본 TDD(선행 시험 → 구현 → 검증)이며 결정 34 A의 경량 경로는 넓히지 않는다.
- 질문으로 멈추지 않고 되돌리기 쉬운 쪽으로 진행해 보고에 적는다. 사용자 손이 필요한 일은 메인에 모은다. 근거 값은 저장 스크립트 출력에서만 옮긴다.

## 요구 원천

- Core 서버 계약 v1.1(읽기만): `core-active/.backups/verification/2026-10-31-db-persistence-demo-build/lead/server-contract-for-autopilot-v1.1.md`, sha256 `ced3f61f01024b004de8bec94b7f2f46680dbc071ede83db8e840063dd95a2ed`. 캐릭터 이름 2~12자·서버 전체 유일, 계정 이름 3~16자 소문자·숫자·`_`, 계정당 3칸, 삭제 경로 없음, F8·F9는 영속 서버에서도 같은 경로(소스 확인 수준).
- Core goal `2026-10-31-db-persistence-demo-build`(core-active 미커밋): 녹화 순서 R0~R8, 녹화 클라는 Content 빌드, 리허설은 PR268 병합 뒤 main 빌드.
- 앞 goal의 넘김: [로그인 화면·길드 창 goal 「다음 계획 후보」](../2026-10-30-login-screen-guild-window/goal.md#다음-계획-후보) 앞 네 줄.

## 범위

PR별 세부(만들 것의 세부 동작, 대안, Unity 창 표)는 승인된 초안 원문 2~9절이 정본이다. 아래는 요약이다.

### 만들 것

- **PR-A 캐릭터 선택창(85 A, 강):** 오른쪽 설명 상자(설명 배경·선택 제목·설명 글)를 없애고 세 칸을 패널 안쪽 폭 전체로 넓힌다. 차 있는 칸을 누르면 선택되고 「선택하고 입장」이 켜진다. 빈 칸을 누르면 칸 줄 대신 생성 화면(이름 칸·이름 규칙·Knight/Mage 그림 버튼·「캐릭터 생성」·「취소」)이 열린다. BACKLOG `character-menu-selecting-detail-text`를 흡수해 닫는다. `domains/client.md`에 캐릭터 선택 화면 절을 더한다.
- **PR268 자동 진행기(82 B·83 A, 강):** 최신 main과 PR-A를 합치고 캐릭터 메뉴 단계를 더한다. 같은 직업 캐릭터가 있으면 그 칸을 선택해 입장하고, 없으면 첫 빈 칸에 `-demo-character` 이름으로 만든 뒤 다시 골라 입장한다. 만들 수 없으면(이름 인자 없음, 빈 칸 없음, 생성 거절) 다시 시도하지 않고 멈춘다. 한 실행의 생성 요청은 최대 1번이다. Legacy 경로는 그대로 Knight/Mage 버튼이다. 칸 상태는 `CharacterSelectController`의 읽기 전용 통로 하나로 읽는다. 단계 로그에 캐릭터 단계를 더한다.
- **PR-C 길드 입구(74 A, 약):** NPC 대화·던전 보상 창이 떠 있는 동안 「길드 · G」 입구를 숨긴다(그때 입구는 이미 눌리지 않는다). 창이 닫히면 원래 자리에 다시 보인다.
- **녹화용 빌드 계약:** Core 리드와 고정 head·Unity 버전과 hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그·빌드 부작용 복원을 이 goal의 계약 절로 맞춘다. 빌드 입력은 PR-A·PR268·Art 인벤토리 격자 PR 병합이다(`msg_d0012352013c`). 빌드 스크립트는 근거 폴더 실행 파일로 둔다.
- **Art PR4 사후 확인:** PR4 head가 오면 main + PR4 트리에서 Content 일시정지 메뉴 좌표·입력 시험의 이름 단위 결과와 `UI.unity` diff를 본다. PR은 없다.

### 건드릴 곳

- PR-A: `03_Client/Assets/Scripts/UI/CharacterMenuView.cs`, 캐릭터 메뉴 시험(`CharacterMenuViewTests`·`CharacterMenuViewAppearanceTests`·`CharacterMenuViewCaptureTests`·`BrightWindowInteractionTests`의 캐릭터 메뉴 부분·`CharacterMenuMotionSceneTests`), `CharacterMenuBoundaryTests`·`CharacterMenuPresenterTests`(T7 19~23행, 메인 `msg_e6ef913df2cd`로 추가), 새 시험 `CharacterSelectWide*`, `domains/client.md`, BACKLOG 한 행, CURRENT 한 줄, 이 goal.
- PR268: `03_Client/Assets/Scripts/Demo/`, `CharacterSelectController.cs`(읽기 전용 통로 하나), EditMode 시험, `domains/client.md` 한 문단, PR268 본문.
- PR-C: `GuildUiController.cs`(입구 표시 한 곳), PlayMode 시험 하나, `domains/client.md` 한 줄, BACKLOG 한 행.

### 하지 않을 것

- 칸 수 4, DB·저장 프로시저·서버·패킷·launcher 변경, `CharacterMenuPresenter`·`CharacterMenuClient`의 응답 해석 변경.
- 새 그림·새 스킨 부품. 기존 `UiWindowAppearance.BrightSkin` 부품만 쓴다.
- DB 행 삭제·DDL, 같은 실행에서 이름을 바꿔 가며 다시 만들기, 인자 없는 일반 실행의 동작 변경.
- `HudLayout`의 Gold 옮김 규칙, `UI.unity`, 입구 크기·글자.
- 서버 쪽 녹화 순서(Core), 녹화 도구와 녹화 자체(메인), 새 저장소 빌드 도구.

### 관찰 가능한 완료조건

| PR | 번호 | 완료조건 |
|---|---|---|
| PR-A | A1 | 목록이 뜬 상태에서 설명 배경·선택 제목·설명 글 객체가 없다. |
| PR-A | A2 | 세 칸의 바깥 사각형이 겹치지 않고, 세 칸과 간격의 합이 패널 안쪽 폭의 90% 이상이다. 1280×720·1920×1080에서 칸 글자가 잘리지 않는다. |
| PR-A | A3 | 빈 칸을 한 번 누르면 생성 화면이 열리고 칸 줄은 숨는다. 「취소」는 이름·직업을 지우고 칸 줄로 돌아온다. |
| PR-A | A4 | 생성 요청은 (칸, 직업, 이름 원문)을 그대로 넘기고 한 번만 간다. 생성 뒤 칸 줄로 돌아오고 다시 골라야 입장한다. |
| PR-A | A5 | 차 있는 칸을 누르고 「선택하고 입장」을 누르면 선택 요청이 한 번 간다. |
| PR-A | A6 | 보존: 대기·입장 중 입력 잠금, 뒤로·연결 끊김 뒤 잠금, Legacy 전용 응답 때 메뉴 닫기, 이중 제출 잠금. 바뀌지 않는 기존 시험은 그대로 통과한다. |
| PR-A | A7 | 출하 CharacterSelect 장면을 `EntryWirePeer` 대역으로 띄워 `Start`→`Update`→생성 화면→생성→선택 요청까지 지난다. 실제 GameServer·DB 화면은 PR268 DB 실주행에서 처음 보며, 그 전까지 미관측 외부 경계로 적는다. |
| PR-A | A8 | 캡처(빈 목록, 한 칸 선택, 생성 화면, 두 해상도)를 Art 리드에 보여 외관 의견을 받는다. 판정은 독립 검증자다. |
| PR268 | B1 | 인자 없는 실행은 자동 진행기 객체를 만들지 않는다. |
| PR268 | B2 | 메뉴 경로의 세 갈래(선택, 생성 후 선택, 멈춤)가 실제 `CharacterMenuView`를 상대로 정해진 칸·버튼만 누르고 생성 요청은 최대 1번이다. |
| PR268 | B3 | 메뉴가 닫히면(Legacy) Knight/Mage 버튼 경로로 간다. |
| PR268 | B4 | 실제 경로 두 판: 구현자가 Player 빌드 두 클라 + Legacy DEBUG 서버로 `[Demo] step … done`, 독립 검증자가 Player 빌드 두 클라 + DB 영속 서버로 새 시험 계정에서 생성 → 선택 → … → `done`. 단계 로그와 Player.log를 남긴다. |
| PR268 | B5 | 검증 실주행의 시험 계정·캐릭터 이름은 시험용이 드러나게 정하고 리허설·녹화용 이름과 다르게 미리 적어 메인과 Core에 넘긴다. |
| PR-C | C1 | NPC 대화 창 때문에 Gold 칸이 오른쪽 끝으로 옮겨진 동안 입구가 Gold 칸과 화면 사각형으로 겹치지 않는다(1280×720·1920×1080). |
| PR-C | C2 | 던전 보상 창 경우도 같은 기준이다. |
| PR-C | C3 | 두 창이 닫히면 입구가 원래 자리에 보이고 눌린다. 기존 Gold·HUD 시험은 그대로 통과한다. |
| 빌드 | D1 | Core에 고정 head·Unity hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그 경로를 넘긴다. 빌드는 PR-A·PR268·Art 인벤토리 격자 PR이 모두 main에 들어간 commit에서 만들고, 격자 PR이 늦으면 기다린다(`msg_d0012352013c`). 리허설 빌드 뒤 병합이 더 있으면 시연 빌드는 마지막 병합 뒤 고정 head로 다시 만든다. |

### 검증 등급

- PR-A 강: 제품+시험 변경이 50줄을 넘는다(`CharacterMenuView.cs`는 457줄이고 배치 대부분을 바꾼다).
- PR268 강: 제품+시험 50줄 이상, 실제 실행 경로(Player 빌드·서버)를 지난다.
- PR-C 약: 제품 1파일·50줄 미만 예상, 영속·서버·프로토콜·I/O 도구에 닿지 않는다. 시험까지 50줄을 넘으면 강으로 올린다.
- 앞 goal의 원인 미확정 기존 실패 13행(73 A)은 실패인 채 둔다. 기준 실행과 이름 단위로 비교해 새 실패만 이번 영향 후보로 본다.

## PR 경계와 순서

1. PR-A(새 PR): 이 goal 문서 + 85 A. base main.
2. PR268(기존 PR): PR-A 위에 쌓는다. PR-A 병합 전에는 base를 PR-A branch로 둔다. 병합 순서는 PR-A → PR268이다.
3. PR-C(새 PR): base main, 독립. 사용자 결정 90 답 뒤 착수.
4. 종료 기록 PR: Gardener 뒤.

## 진척 단계

- [x] 범위 승인
- [x] 캐릭터 선택창 시험
- [x] 캐릭터 선택창 구현
- [x] 캐릭터 선택창 검증
- [x] 자동 진행기 시험
- [ ] 자동 진행기 구현
- [ ] DB 서버 실주행 검증
- [ ] 길드 입구 겹침(리셋 뒤)
- [>] 선택창 PR 병합
- [ ] PR268 병합
- [ ] 녹화용 빌드 전달
- [ ] 종료 기록

## 진행 기록

- 2026-10-31 진입: 메인 `msg_f6f3c9b1a51e`. Run `run_0a58bc3cfea0`(receipt `lead/run-create-01.json`), READY `msg_0d3cf8e912e2`, 범위 초안 v1 `msg_6aac15fe228e`, 승인 `msg_967d20c30e6d`.
- 2026-10-31 캐릭터 선택창 시험(RED): 작성자 `[Content 구현 Opus]` task_a1373aa975b2 / ctx_7483fcc14f3b, 계약 v2 `lead/pr-a-red-contract-v2.md`(sha256 `f7951db199f878cec2fe5f4c01eb8c1131cd776895607c60f5366fce3861a141`), 보고 `pr-a-red-01/report.md`(sha256 `c16b7d2f6c111e3333d3b871b3138ff5baf5fd98886e06880c5077ac0e4cec22`), worker_done `msg_70dd260bdcd2` succeeded, 정산 15:44Z(release·pane close).
  - T7: 기존 시험 충돌 23행 표(`pr-a-red-01/t7/t7-table-v1.md`) → 메인 `msg_e6ef913df2cd`가 23행 전부 승인, 15행은 「설명 상자 없음」 한 단정·이름 유지. 쓰기 범위를 넓히려 계약을 v1에서 v2로 올렸다.
  - 기준 U0(창 1, `baseline-01/`): EditMode 1694/1694, PlayMode 258 중 215 통과·43 Ignored·실패 0.
  - U1(창 2, `pr-a-red-01/u1b/`): EditMode 1707 중 48 실패, PlayMode 259 중 1 실패·43 skipped. 새 시험 14건 중 12건 실패, 보존 단정 2건(T3 OccupiedSlot·T5) 통과. 같은 이름 기존 시험의 Passed→Failed 35건과 새 이름 2건은 모두 승인된 다섯 파일 안이며 승인 밖 새 실패는 0이다(리드가 결과 XML로 재집계).
  - 구현자에게 넘길 관측: 지금 칸 span은 header 720 중 320(44.4%)이다. 100폭 칸에서 12자 이름의 선택 칸 글이 넘친다. T6 대역 흐름은 생성 뒤 목록 응답을 보내지 않는다(Presenter가 생성 성공만으로 칸을 채움).
  - 첫 발생 기록: U1 첫 시도는 `-File`로 넘긴 쉼표 목록이 배열로 풀리지 않아 harness exit 3으로 Unity 기동 전에 멈췄다. 작성자가 harness를 고쳤다(sha256 `9de61bda…`→`754ec592…`).
  - 첫 발생 기록: 작성자가 창 밖(15:17:21~27Z, 직전 Unity 0 관측)에서 Unity 동봉 Windows dotnet으로 오프라인 컴파일했다. 계약 금지 목록에는 없었다. 다음 계약부터 「Windows dotnet도 배정된 창 안에서만」을 적는다.
  - 메인 대조(`msg_c63f0c6c0b3e` 3항): 그 15:17:21~27Z는 Art PR4 K2 캡처 창(15:12:30~15:32:30Z) 안이었다. K2는 통과해 다시 찍지 않는다. 구현 계약 v1부터 위 문장을 넣었다.
- 2026-10-31 캐릭터 선택창 구현(GREEN): 구현자 `[Content 구현 Opus]` task_836a399a68fa / ctx_4e160b797a9a, 계약 `lead/pr-a-impl-contract-v1.md`(sha256 `9371be936177b6a43d46a79c2e4ca4b1d93e9a0bbd2299390321565550e7118a`), 보고 `pr-a-impl-01/report.md`(sha256 `efe3084a1de98988363997c1ee458f1d91f1fbe1eed76f00b482e8e78b807fc1`), worker_done `msg_0e085589d119` succeeded, 정산 16:19Z(release·pane close).
  - 제품: `CharacterMenuView.cs` 한 파일(122+/65−). 설명 상자 세 객체를 없앴고, 세 칸은 232폭·간격 12로 Header 폭 720 전부를 쓴다. `CreateForm`은 칸 줄 자리에 열리고 그동안 칸 줄·`Submit`은 꺼진다. `domains/client.md` 「캐릭터 선택 화면」 절을 더했다.
  - U2(창 3, `pr-a-impl-01/u2/`, 한 번): EditMode 1707/1707, PlayMode 259 중 216 통과·실패 0·skipped 43(U1과 같은 이름). U1 실패 49건이 모두 통과로 바뀌었고 통과에서 바뀐 시험은 0이다(리드가 결과 XML로 재집계).
  - 구현자가 남긴 비차단 관찰: `Cancel`이 `Result` 글도 지운다. 생성 화면에 어느 칸을 만드는지 보이는 제목이 없다. 생성 화면이 열린 동안 칸 클릭 guard를 직접 겨냥한 시험은 없다. 보고 1절의 diff 줄 수(120+)는 실제 122+와 다르다.
  - BACKLOG: 이 goal로 승격된 세 행(`character-select-three-wide-slots`·`demo-autoplay-character-menu`·`demo-recording-build-contract`)에 goal 링크를 달고, `character-menu-selecting-detail-text`는 흡수 폐기했다.
- 2026-10-31 캐릭터 선택창 검증: 검증자 `[Content 검증자]`(신규 GPT-6-Astra xhigh) task_8a5535cbf971 / ctx_eaf67d259cea, 계약 `lead/pr-a-verify-contract-v1.md`(sha256 `0a7b2edbf4ecbf5b6dcb82746c4175e60723a0f9075d809a9477e109d94c1aaf`), 판정 `pr-a-verify-01/verdict.md`(sha256 `92c1340d7fd21931acc868a6e6a532117957b6431fda54d097ff8838d938bb94`), worker_done `msg_678676dce5dc`, 정산 16:59Z(pane close·release retained).
  - 판정: 통과, 차단 결함 0. A1~A8 통과. A7은 `EntryWirePeer` 대역으로만 지났고 실제 GameServer·DB 화면은 PR268 DB 실주행까지 미관측이다.
  - U3(창 4, `pr-a-verify-01/u3/`): EditMode 1715/1715, PlayMode 259 중 216 통과·실패 0·skipped 43, 둘 다 exit 0. U2→U3에서 결과가 바뀐 기존 시험은 0이다.
  - 판정 시험 `Tests/EditMode/CharacterSelectWideVerifyInteractionTests.cs`(8 cases)와 `.meta`를 이 PR에 남긴다. 생성 화면이 열린 동안 숨은 칸·`Submit` 클릭 guard를 직접 겨냥한 시험이 그 전까지 없었다(판정 O5).
  - 정정 기록(비차단 D1·D2, 원문 보고는 그대로 둔다): 구현 보고의 diff 줄 수 120+는 실제 122+/65−다(`git show --numstat 03b2b189`). RED 보고의 T7 분류 (a) 19·(c) 4는 실제 (a) 20·(c) 3이다(`t7-table-v1.md` 23행, 리드가 다시 셈).
  - 설계 관찰 O1~O4는 막지 않는다. `Cancel`이 `Result`도 지우는 동작(O1)은 유지한다. A8 Art 의견 `msg_7f9778ed1c3d`(저장본 sha256 `2be227f1c5ecd477ffb7d8bf6685c22dfe3458f09047b2310c2d9b922a826c11`)는 막을 것이 없었고, 권고 셋은 이 PR에 넣지 않고 종료 기록의 다음 후보로 넘긴다. 셋은 생성 화면 안내 줄 「슬롯 N에 새 캐릭터 만들기」, 고른 직업 버튼 강조와 그림 위치, 칸 그림 1.3배다.
  - PR268 base: 리드의 `gh pr edit 268 --base feat/content-demo-character-select-20261031`은 PR-A push(17:00:43Z) 직후 자동 권한 판정에 막혔다. 다시 시도하지 않고 메인에 요청했다. PR268 base는 아직 main이다.
  - 그 뒤: PR #291 생성(17:03:39Z). CI 네 workflow 성공(dotnet-tests 17:25:46Z). 병합 준비 보고 `msg_1d5403db75f2`. base 변경은 메인 `msg_db6333b6b1c6`으로 생략했다(「10-31 범위 초안 v1 처분」 2항).
- 2026-10-31 자동 진행기 시험(RED): 작성자 `[Content Sol]`(신규 Codex `gpt-6.1-sol` max, 첫 화면 「GPT-6.1-Sol max」·v0.166.0) task_17a3e8c1b9fb / ctx_d0c084e473d0, 계약 `lead/pr268-red-contract-v1.md`(sha256 `e8c0fc539f51356378ec5be3c3122997182b608d5d4338f52eba4d3a76e21d73`), 보고 `pr268-red-01/report.md`, worker_done `msg_54f15ab248f6` succeeded, 정산 17:30Z(release retained·pane close).
  - 준비: PR268 branch(`1237cb58`)에 PR-A head `fb3d65b1`을 합쳤다(`252a59e1`, 글자 충돌 0). 작성 뒤 RED commit `60e0683d`, 이어 최신 main `b8631ccb`(Art PR290)를 합쳤다(충돌 0, 바뀐 파일 겹침 0).
  - 설계 고정(리드 결정, 계약 「설계 고정」): `DemoLaunchOptions.Character`, `Demo/DemoCharacterMenuStep.cs`의 `DemoCharacterMenuStep(DemoRole, string?)`·`Advance(CharacterMenuViewState?, Transform?)`·`StopReason`·`DemoCharacterMenuOutcome { Waiting, Creating, Selecting, Legacy, Stopped }`, `CharacterSelectController.MenuState`. 생성 거절은 「생성을 보낸 뒤 같은 직업 칸이 없는 `Ready`」 하나로 본다. 클릭은 `CharacterMenu/Panel` 아래 경로로만 찾는다(Legacy 장면에도 `Knight`·`Mage` 단추가 있다).
  - 시험: `DemoLaunchOptionsTests`에 4개, 새 `DemoCharacterMenuStepTests` 10개 메서드(11 cases)로 모두 15건. 실제 `CharacterMenuPresenter`·`CharacterMenuView`에 연결 대역으로 응답 bytes를 넣는다. 대역 도우미는 `CharacterMenuPresenterTests`의 private 중첩 타입이라 새 파일에 옮겼다.
  - U1(창 5, `pr268-red-01/u1/`, 17:23:27Z~17:24:18Z): EditMode 한 번, Unity exit 1, 결과 XML 없음. 고유 진단 32개(로그 96회, 각 3회)가 모두 부재 이름(`Character` CS1061 3, `DemoCharacterMenuStep` CS0246 10, `DemoCharacterMenuOutcome` CS0103 19)이고 그 밖의 오류는 0이다(리드가 로그에서 다시 셈). PlayMode는 같은 컴파일에 막혀 돌리지 않았다.
  - 시험 불가 요구: `CharacterSelectController.MenuState`는 실제 메뉴 접속이 있어야 null이 아니고 PlayMode 어셈블리는 internal을 보지 못한다. 대체 실측은 창 6 Legacy DEBUG 서버 실주행(Legacy 경로)과 창 8 DB 영속 서버 실주행(메뉴 경로), 독립 검증자의 코드 실사다.
  - 생존 신호: 17:18:37Z→17:23:44Z 구간이 307초로 300초를 7초 넘었다(U1 시작 준비 중). 작성자가 보고에 남겼다.
