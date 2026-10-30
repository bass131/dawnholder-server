# 로그인 화면 재구성·길드 거점 창 확대

## 재개 지점

**다음 첫 일:** PR-A·PR-B 선행 시험 작성자를 띄운다(계약 `pr-a-red-contract-v1.md`·`pr-b-red-contract-v1.md`). 각 작성자의 U1 창을 메인에 하나씩 요청한다. 창 1 기준선은 끝났다([결과](#기준-실행사후-확인--unity-창-1)).

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1을 메인이 원문과 대조해 허가했다(`msg_736bf752c0e4`). 초안 원문은 `.backups/verification/2026-10-30-login-screen-guild-window/scope-draft-v1.md`(sha256 `fa745f0ec4672113daa1656b124ac23532a78e1920cc5c93bdea96ed82b76ebb`)다. |
| branch | `feat/content-login-guild-window-20261030`, base `7bc758987e3816e0690f4df78f8160fbc3da2135`(PR279 병합) |
| 리드 | `[Content 리드 Opus]`(claude-opus-5-5 xhigh), 회신 주소 `run:run_978c01afcf2a` |
| 근거 폴더 | `.backups/verification/2026-10-30-login-screen-guild-window/` |

## 적용 중인 사용자 결정

메인이 전달한 사용자 원문과 메인 결정을 구분한다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

### 10-30 사용자 개선안 — msg_41c7eb2fcffc

메인 `msg_41c7eb2fcffc`(2026-10-30T05:18:59Z)가 전달한 사용자 원문이다(2026-10-30 14:1x KST 메인 pane 제출, 두 메시지).

> 이제 나머지 개선안
> 1. 인벤토리는 최신 GUI 디자인인데 나머지가 그렇지 않음
> 2. 길드 거점 GUI가 너무 작아서 거점 업그레이드 그림이 작음, 비율을 어떻게 크게 할지 고민
> 3. 몬스터 사냥후 인벤토리로 아이템이 지급된다는 느낌이 적음, 몬스터 사망시 "+금화 몇" 이런식으로 위로 천천히 Rerf했다가 사라지는 효과도 필요함
> 4. 메인화면 GUI를 게임 로그인 화면으로 재구성, 기존에 로그인용 화면이 없어서 생긴 문제
>
> 모든 GUI 디자인은 인벤토리와 캐릭터 선택창 기준으로 완전 최신화 진행해줄것

> 모두 끝까지 진행하고 마지막에 한번에 체크 할 수 있게 전부 작업 완수하고 보고해줘

- 메인 배분(메인 결정): Content는 개선안 2·4를 맡는다. 개선안 1과 GUI 전면 최신화의 외관은 Art, 개선안 3은 Effect다.
- 메인 운영(메인 결정): 범위 초안은 메인이 원문과 대조해 승인하고 사용자에게 중간 승인을 다시 묻지 않는다. 디자인 방향은 PR279의 밝은 2D 판타지 외관(`UiWindowAppearance.BrightSkin`)이다. 캡처 확인·직접 플레이·병합 승인은 마지막에 사용자가 한 번에 한다. 병합은 마지막까지 없고 겹치는 PR은 쌓는다.
- 이전 결정: 10-27 결정 4 A로 리드·구현자는 Claude Opus xhigh, 독립 검증은 신규 GPT Astra xhigh다. 최종 제출은 11-06이다.

### 10-30 범위 초안 v1 처분 — msg_736bf752c0e4

메인 결정이다(사용자 결정 아님).

- 선행 시험 방식: **경량 경로 미적용, 정본 TDD**다(선행 시험 Opus → 구현 Opus → 검증 Astra). 결정 34 A는 「선택·생성 화면의 검증 방식」에 대한 결정이라 이 goal로 넓히지 않는다.
- 길드 창 스킨: `GuildWindowLayout`을 쓰는 두 창(길드 현황·대장간)의 밝은 스킨 적용도 PR-B가 맡는다. 조건은 셋이다. 기존 BrightSkin 부품만 쓴다(새 그림 0). 외관 계약 시험(UiWindowAppearance 쪽)에 두 창이 기대하는 값이 있으면 바꾸기 전에 전수 분류 표를 메인에 올린다. PR-B 캡처를 Art 리드에게 보여 외관 의견을 받는다(판정은 독립 검증자다).
- Unity 창은 요청 순서대로 메인이 하나씩 배정한다. 요청은 무엇·예상 시간·관문 측정 출력 경로 세 줄이다. 관문은 물리·commit 여유 각 4 GiB와 Unity·Windows dotnet 0이다.

## 만들 것

**PR-A 로그인 화면(개선안 4)**

- 메인 메뉴를 게임 로그인 화면으로 다시 짠다. 전체 배경, 위 가운데 큰 타이틀 로고, 아래 가운데 밝은 로그인 창이다.
- 로그인 창은 `BrightSkin`을 쓴다. 제목 띠 「로그인」, 「계정 이름」 칸, 「서버 주소」 칸(작은 보조 줄), 「게임 시작」 버튼(Accept), 「종료」 버튼, 오류 한 줄이 들어간다.
- 캐릭터 선택창처럼 일반 C# view 클래스가 창을 만든다. 장면의 기존 두 입력칸·두 버튼·오류 글은 그 창 안으로 옮겨 다시 쓴다. `MainMenuController`의 직렬화 연결과 버튼 onClick이 그대로 남는다.
- 흐름은 바꾸지 않는다. Awake 연결 정리, 저장된 서버 주소·계정 이름 자동 채움, 계정 이름 정규화와 실패 안내, probe 중 중복 클릭 차단, 성공 때 저장 뒤 캐릭터 선택 장면 이동, 실패 글, 종료, MainMenuNotice 표시가 같다.
- 새 동작은 키보드 둘이다. Tab으로 계정→서버 주소 이동, Enter로 「게임 시작」.

**PR-B 길드 거점 창 확대(개선안 2)**

- 지금 거점 그림은 창(440×566) 안 416×214 칸에 비율 유지로 그려지고, 창은 화면 왼쪽 38% 기둥에 맞춰 약 0.76배로 준다. 1920×1080에서 그림은 약 580×327 픽셀이다(코드 상수로 계산, 미측정).
- 창을 가로형으로 바꾸고 그림을 창 맨 위에 창 폭 가득 둔다. 그림 칸은 원본 비율(1672×941)과 같다. 정보 줄은 그림 아래 두 열, 버튼은 맨 아래 줄이다.
- 창 자리는 화면 가운데 쪽 넓은 영역으로 옮긴다. 왼쪽 위 캐릭터 상태와 오른쪽 위 미니맵을 가리지 않는다.
- `GuildWindowLayout`을 쓰는 대장간 창의 크기·자리는 바꾸지 않는다. 두 창을 함께 열 수 있으면 겹치지 않게 정한다.
- 두 창(길드 현황·대장간)에 밝은 스킨을 적용한다(위 처분의 조건 셋).
- 레벨업 효과 영역은 그대로 둔다.

**함께 할 일**

- PR279 사후 확인: `CharacterMenuView.cs` diff 판단과 Content 시험 실행.
- Art 70 A의 `DungeonClearRewardWindow.cs` 자리 규칙 변경 사후 확인: Art PR이 열리면 diff 판단과 보상 창 시험 실행.

## 건드릴 곳과 소유

| 영역 | 파일 | 보존 |
|---|---|---|
| PR-A 화면 코드 | `03_Client/Assets/Scripts/UI/MainMenuController.cs`(view 생성·연결만), 새 view 클래스 `Scripts/UI/` 아래(+.meta, 이름은 계약에서 확정) | 직렬화 필드 7개(참조 4개·값 3개)와 FormerlySerializedAs, `OnStartClicked`·`OnQuitClicked` |
| PR-A 장면 | `03_Client/Assets/Scenes/00.Menu/MainMenu.unity` | 기존 오브젝트의 fileID·GUID·연결. 옛 오브젝트는 지우지 않고 다시 쓰거나 끈다 |
| PR-B 화면 코드 | `Scripts/UI/GuildWindowLayout.cs`, `GuildHousingLayout.cs`, `GuildWindow.cs`, 필요하면 `BlacksmithWindow.cs`의 글자 색 | 표시 값·대기·입력 허용·구독 수명·레벨업 의도 이벤트, 레벨 1·2 그림 선택 규칙, 대장간 창 동작 |
| 시험 | 새 EditMode·PlayMode 시험 | 기존 시험의 준비 코드·기대값 변경 0이 목표. 바꿔야 하면 전수 분류 표와 메인 결정 뒤 |
| 기록 | 이 goal, CURRENT Content 줄 | 직전 Content goal 기록은 고치지 않는다 |

제품 코드는 구현자가, 선행 시험은 선행 시험 작성자가, 독립 시험은 검증자가 쓴다. 리드는 goal·CURRENT·근거 폴더와 Git만 쓴다.

## 하지 않을 것

- 비밀번호·회원가입·계정 서버 같은 새 로그인 기능. 서버·패킷·DB·계정 정규화 규칙 변경.
- 캐릭터 선택 장면·Legacy/DB 분기·NetworkService 수명 변경.
- 길드 창을 Art의 공용 창 배치 부품(UiWindowPlacement)으로 옮기는 일(BACKLOG `guild-windows-shared-placement`).
- 새 그림 생성(Art 몫), 레벨업 효과 연출 변경, 병합.

## 관찰 가능한 완료조건

| 번호 - 내용 | 완료 증거 |
|---|---|
| 1 - 로그인 화면 구성 | 1920×1080·1280×720 캡처에서 로고·밝은 로그인 창·두 칸·두 버튼·오류 줄이 겹치지 않고 보인다. 창 조각이 밝은 스킨 GUID인지 시험으로 확인 |
| 2 - 로그인 흐름 보존 | 기존 메인 메뉴 시험 전부 통과(MainMenuAccountScene·MainMenuNoticeRendering·SceneTransitionRequest·LoginRejectionMenu). 새 시험으로 Tab·Enter, 중복 클릭 차단, 실패 글 표시 확인 |
| 3 - 거점 그림 확대 | 두 해상도 캡처와 시험에서 그림 표시 크기를 잰다. 1920×1080에서 이전 대비 면적 2.5배 이상, 두 해상도 모두 글자 겹침·잘림 없음, 화면 밖으로 나가지 않음 |
| 4 - 길드 동작 보존 | 기존 길드 EditMode·PlayMode 시험 전부 통과(GuildWindow·GuildHousing·GuildGrowthEffect 묶음과 대장간 창 시험) |
| 5 - 두 창 밝은 스킨 | 길드 현황·대장간 창 조각이 밝은 스킨 GUID인지 시험으로 확인, 캡처에 Art 리드 의견 |
| 6 - 사후 확인 | PR279 Content 파일 diff 판단과 Content 시험 원시. 보상 창 diff 판단과 시험 원시(Art PR 시점) |
| 7 - 독립 검증 | 각 PR의 신규 Astra 강 검증 원문. 미실행 플레이는 따로 표시 |
| 8 - 사용자 마지막 확인 준비 | PR마다 두 해상도 캡처와 플레이 확인 항목 몇 줄 |

## PR 경계와 작업 방식

- PR-A: 로그인 화면 + 이 goal + CURRENT Content 줄. base는 main.
- PR-B: 길드 거점 창 확대와 두 창 밝은 스킨. PR-A branch 위에 쌓는다. 병합 순서는 PR-A → PR-B다.
- 종료 기록 PR은 마지막에 따로 연다.
- 검증 등급은 두 PR 모두 강이다. 제품+시험 변경이 50줄을 넘을 것으로 본다(미측정, 구현 뒤 diff로 확정). 서버·DB·프로토콜은 건드리지 않는다. R-7은 비해당이다.
- 작업 방식은 정본 TDD다(위 처분). PR마다 신규 선행 시험 작성자 `claude --model claude-opus-5-5 --effort xhigh`가 요구사항 시험과 구현 전 실패 원시를 남긴다. 그 뒤 신규 구현자(같은 명령)가 구현해 같은 명령의 통과 원시를 남긴다. 마지막으로 신규 독립 검증자 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`가 강 검증한다. 작업 하나 뒤 정산·종료한다.
- PR-A와 PR-B는 제품 파일이 겹치지 않아 세션을 나란히 띄울 수 있다. Unity 창은 하나씩 받는다.

## 실행 창 추정

계획치이며 미측정이다.

| 창 | 내용 | 시간 |
|---|---|---|
| 1 | 첫 가져오기(Library 없음) + 지금 EditMode 전체 기준 실행 + PR279 사후 확인 시험 + 지금 화면 캡처 | 가져오기 30~60분 + 20분 |
| PR마다 선행 시험 | 새 시험의 구현 전 실패 실행 | 각 20~30분 |
| PR마다 구현 | 컴파일·EditMode·PlayMode·두 해상도 캡처 | 각 30~40분 |
| PR마다 검증 | Astra 강 검증 | 각 45~60분 |
| 보상 창 사후 확인 | Art PR 열린 뒤 | 15분 |

서버(7777)·DB는 쓰지 않는다. 실제 게임 안 길드 창은 사용자의 마지막 플레이에서 본다.

## 사용자가 마지막에 볼 것

- PR-A 캡처: 로그인 화면 1920×1080·1280×720, 오류 글이 뜬 화면 하나.
- PR-A 플레이 확인: 계정 이름을 넣고 Enter로 들어가 캐릭터 선택이 뜨는지, 서버를 끈 채 시작하면 오류 글이 보이는지, 종료 버튼.
- PR-B 캡처: 길드 창 거점 레벨 1·2 그림과 대장간 창, 두 해상도.
- PR-B 플레이 확인: 마을에서 길드 창을 열어 그림 크기, 레벨업 뒤 그림 바뀜과 효과, 닫기·새로고침, 대장간 창.

## 진척 단계

- [x] 범위 승인
- [x] 기준 실행·사후 확인
- [>] 로그인 선행 시험
- [ ] 로그인 화면 구현
- [ ] 로그인 독립 검증
- [ ] 길드 창 선행 시험
- [ ] 길드 창 구현
- [ ] 길드 창 독립 검증
- [ ] PR 두 개 준비
- [ ] 종료 기록

## 단계 결과와 근거

### 범위 승인

- 리드 진입 맥락 메모: `lead-context-memo-v1.md`. Run 생성 receipt: `run-create-receipt.json`(run_978c01afcf2a).
- READY와 초안 v1 전송 `msg_2092c1f6b5e4`, 메인 처분 `msg_736bf752c0e4`(2026-10-30T05:27:40Z). Art 리드 조율 `msg_0da8455d716f`.
- PR279 사후 확인의 정적 읽기: `postcheck-pr279-charactermenuview-read-v1.md`. Content 동작 코드 diff는 없었고, 인벤토리 배치 단정은 결정 50 B대로 값이 그대로다. 시험 실행은 창 1에서 한다.

### 기준 실행·사후 확인 — Unity 창 1

- 메인 배정 `msg_e5077ab0039f`. 리드가 `scripts/run-window1.ps1`로 cac54985에서 돌렸다. Unity 시작 05:33:03Z, 반환 관측 05:49:26Z(Unity 0·dotnet 0). 근거는 `window1/`이다.
- 첫 가져오기는 약 7분 40초였다(Unity 시작 05:33:03Z → EditMode 시험 시작 05:40:44Z). 초안의 30~60분 추정보다 짧았다.
- EditMode 1571/1571 통과. PR279 사후 확인 대상(CharacterMenu*·MainMenu*·ClientLogin*·InventoryPanel*·UiWindowAppearanceContract·UiWindowCompositionRender·BrightUiKitImport·BrightWindowInteraction)도 모두 통과했다(`window1/editmode-content-classes-01.txt`). PR279 사후 확인은 끝났다.
- PlayMode 185 중 통과 143·실패 23·건너뜀 19. 실패 23의 분류는 `window1/baseline-classification-01.md`다. 환경 전제(실행기 허가·캡처 폴더 변수·GameView·실서버)가 없어 실패한 것이 12, 길드 창 글자 온전함 판정의 원인 미확정이 11이다. 모두 제품 변경 전 실패다. 이후 실행은 이 목록과 비교해 새 실패만 이번 변경의 영향 후보로 본다.
- harness가 프로세스 핸들을 잡지 않아 Unity exit code는 null(미관측)이다. 선행 시험 계약에 고치는 방법을 넣었다.
- 실행 뒤 drift 두 파일(ProjectSettings의 SENTIS 정의 한 줄, MinimapRT.renderTexture 줄 끝만)을 확인하고 git 원본으로 복원했다(`window1/drift-*`).
