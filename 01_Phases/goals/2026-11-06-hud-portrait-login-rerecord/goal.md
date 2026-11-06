# 시연 UI 마감과 재녹화 — HUD 초상화·로그인 화면 ID/PW/START/QUIT·세 회차 재녹화

시연 영상(제출 기한 2026-11-09 14:00 KST)을 다시 찍기 전에 두 화면을 고친다. 하나는 왼쪽 위 HUD 초상화가 금색 테두리 밖으로 튀어나오는 마감이다(152 A). 다른 하나는 로그인 화면을 ID·PW·START·QUIT 배치로 바꾸는 것이다(152 코멘트). 두 화면이 main에 들어가면 녹화 빌드를 다시 만들고 게임 BGM을 켠 채 세 회차를 다시 녹화한다(153 A).

## 재개 지점

**다음 첫 일:** 선행 시험 작성자 `[Content 시험 Astra]` 계약을 쓰고 연다. 그 뒤 메인에 Unity 창 1(선행 시험 RED)을 청한다.

- 범위 초안 v1을 메인이 원문과 대조해 허가했다(`msg_6feebf9d6738`). 초안 원문은 `.backups/verification/2026-11-06-hud-portrait-login-rerecord/lead/scope-draft-v1.md`(sha256 `a9ad70736c2f648e024501fcce638f9b95d138ec6c6f93d91be2abf8d9b1cf1d`)다. 초안 §5는 「화면 칸만」(155 A), §7은 「게임 BGM 켬」(153 A) 행으로 고정됐다.
- 열린 결정: 없음.
- 근거 폴더: `.backups/verification/2026-11-06-hud-portrait-login-rerecord/`.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1 메인 허가(`msg_6feebf9d6738`, 메인 결정 다섯 포함) |
| branch | PR1 `feat/content-hud-portrait-login-20261106`, base main `518f920faaca89419d9f9fde89e4b3f76a75156d` |
| 리드 | `[Content 리드 Opus]`(claude-opus-5-5 xhigh), Run `run_d49db3aff0b0`, 회신 주소 `run:run_d49db3aff0b0` |
| 메인 | `[메인 Claude]`, handle `term_751a1f26-7aac-4a33-8a57-a3d4cec8a3d4` |

## 적용 중인 사용자 결정

메인이 전달한 사용자 원문이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다. 원문 사본은 근거 폴더 `lead/main-msg_*.txt`다.

- 시각 기준: 메인이 적은 KST 시각은 어림이었다(메인 정정 `msg_db0be3a049e4`). 아래 시각은 메인 메시지의 `created_at`(UTC)이다.
- 152 A와 코멘트(메인 pane 제출, 메인 전달 `msg_5a959e007b3e` created_at 2026-11-06T13:16:14Z 이전): 「대시보드 결정 응답: 1) 계획 검토 - Main 152 - HUD 초상화 마감을 어떻게 고칠지 → A 게임에서 고치고 다시 녹화, 코멘트 「추가로 메인 타이틀을 좀 수정해야 할 거 같은데 , ID [ 입력박스 ] \n PW [ 입력박스 ] \n START 버튼 \n QUIT 버튼 , 이 배치로 바꿔보자」」.
- 151 C(같은 메시지, 영상 v4를 보고): 「캐릭터 HUD 초상화쪽 마감이 별로네, 그거 좀 수정하고」, 「이후에 인게임 BGM을 포함한 시연 영상으로 넘어가자」. 메인 전달에 따르면 사용자는 광고 같지 않은 실제 게임 플레이 시연을 원한다.
- 153 A·155 A(대시보드 결정 응답 메인 pane 제출, 메인 전달 `msg_b6e61dd1536c` created_at 2026-11-06T13:19:32Z): 「1) 계획 검토 - Main 153 - 인게임 BGM 4곡을 발표 영상에 써도 되는지 → A 써도 됨 · 2) 계획 검토 - Content 155 - 로그인 화면의 PW 칸을 진짜 비밀번호 확인으로 만들지 → A 화면 칸만」.
  - 155 A 적용(메인 지시): 로그인 화면은 배치만 바꾼다. PW는 서버가 확인하지 않는다(`C_Login`·서버·DB·PDL 변경 없음). 진짜 비밀번호 인증은 시연 뒤 후보로 BACKLOG에 적는다(`login-password-authentication`).
  - 153 A 적용(메인 지시): 재녹화는 게임 BGM을 켜고 찍는다. 앞 녹화의 `-demo-no-bgm`(123 A)은 이번 녹화 계약에서 뺀다. 곡 출처는 사용자가 따로 적지 않았다. 기록은 「출처 미기재, 사용자 153 A로 사용 허용」이다. 녹화하지 않는 쪽 클라의 BGM·효과음은 꺼서 섞이지 않게 한다.
- 144 A(메인 `msg_a0317d5084da`가 전달, 이 goal 진입 `msg_5a959e007b3e`가 다시 전달): 「3) 계획 검토 - Content 144 - 길드 거점·공동 금고를 DB에 저장할지 → A 시연 뒤 후보 (초안 msg_df3f063feba5)」. BACKLOG 등록은 이 goal 첫 PR에 넣는다(`guild-stronghold-vault-persistence`).
- 137 A·138 A(메인 `msg_c69495189d38`, 앞 goal 「11-06 사용자 결정 137 A·138 A」 절): 「1) 137 - 서버 시작·정지·재시작을 메인이 대신 할지: 메인이 대신 vs 지금처럼 사용자 손 → A 메인이 대신 · 2) 138 - 운영 결정(이름 초안·재시작 순서·처분 같은 것)을 메인이 추천안으로 정할지 → A 메인이 정함」. 이 goal의 서버 시작·정지·재시작은 메인이 한다. 리드·작업자는 서버 창을 읽지도 누르지도 않는다.
- 사용자 확인 범위(메인 `msg_4783c3e62d2c`, 앞 goal 같은 이름 절): 「앞으로는 내가 체크해야 하는 부분은 계획 승인, 결과 받고 피드백, 관리자 권한 승인, 에이전트 단독으로 정하기 어려운 트레이드오프 문제(되돌리기 어려운 경우)」. 리드는 결정 요청을 이 기준으로 거르고, 메인이 정할 수 있는 것은 추천안과 함께 「메인 결정 요청」으로 보낸다. PR 병합의 `병합 승인:` 한 줄은 그대로다.
- 140 A(메인 `msg_391a8e696163`, 앞 goal 같은 이름 절): 「140 - 작업자·검증자가 끝나고 리드에 보고하지 않는 문제를 어떻게 막을지 → A 자동 감시 + 계약 한 줄」. 새로 여는 모든 작업자·검증자·Gardener 계약의 마지막 단계에 「마지막 행동은 worker_done 전송이다(결과 경로·SHA256·판정 한 줄). worker_done을 보내기 전에는 턴을 끝내지 않는다. 막히면 worker_done 대신 ask나 status로 리드에 알린다.」를 넣는다.
- 11-05 작업자 모델(메인 `msg_1c83cff0bb95`, 앞 goal 「11-05 사용자 결정」 절): 「이제 앞으로 모든 Codex 작업자는 Astra xhigh, 검증자도 xhigh로 진행해줘」, 「이유는 11월 10일에 구독이 끝나는데 그때 갱신 더 안할 예정이라」. 메인 결정 4(`msg_6feebf9d6738`)로 이 goal의 새 Codex 세션은 역할과 무관하게 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`로 연다. CLAUDE.md 4A 표(구현자·Gardener Sol max)보다 이 결정이 앞선다. 정본 반영은 11-09 뒤 Rules 몫으로 메인이 들고 있다.
- 제출 기한(메인 `msg_8d6a75a7daed`, 앞 goal 「11-04 재진입」 절): 「제출기한이 좀 늘어났어, 11월 9일 14시까지야」. 2026-11-09(월) 14:00 KST까지 브로슈어·PPT(시연 영상 삽입 필수)·시연 영상 파일을 USB로 낸다. 발표는 11-11(수) 16:00 KST다.
- 녹화 관련 앞 결정(앞 goal 같은 이름 절): 녹화 자동화 「녹화는 내가 하는게 아니라 직접 시나리오 봇 돌려서 전부 클로드로 진행해줘」(`msg_297808ae9fe2`), 131 C 「같은 시나리오니까 각각 한번씩 녹화하고 이후에 편집할때 조정」(`msg_cf71f4c479f7`), 80 A(DB에는 새 시험 계정·캐릭터 행만, 새 DB·DDL·행 삭제 없음).

### 범위 초안 v1 처분과 메인 결정 — msg_6feebf9d6738

138 A·사용자 확인 범위에 따른 메인 결정이며 사용자 결정이 아니다. 되돌리기는 각 항목의 새 결정이다.

1. 로그인 글자는 영문 「ID」·「PW」·「START」·「QUIT」(사용자 원문 그대로)다.
2. 재녹화 이름은 새 이름 셋이다. 계정 `rec2_knight`·`rec2_mage`·`rec2_ally_mage`, 캐릭터 「여명기사」·「여명법사」·「여명동료」다. 생성 장면을 유지한다.
3. (가)와 (나)는 한 PR이다. 결함 번호·commit은 부분별로 나눈다.
4. 작업자 모델은 위 11-05 결정을 따른다. 태그는 구현 `[Content 구현 Astra]`, 시험 `[Content 시험 Astra]`, 검증 `[Content 검증자]`다.
5. 서버 주소는 지금 코드 그대로(저장값 → 127.0.0.1)다. 실행 인자는 BACKLOG 후보 한 줄이다(`login-server-host-launch-argument`).

- Art 리드는 지금 열려 있지 않다. A4의 Art 의견은 받지 않고 외관은 독립 검증자 판정과 사용자 확인으로 간다. HUD 파일을 쓰기 전에 Art에 알리는 단계도 뺀다.
- Unity 창 요청은 「무엇·예상 시간·관문 측정 출력 경로」 세 줄로 보낸다. 사용자가 자는 동안에도 병합 승인 앞까지 간다. PR1이 준비되면 head·CI·판정서 경로와 함께 병합 준비 보고를 보낸다.

### 운영 방식

- 병합은 PR마다 사용자 명시 승인으로 메인만 한다.
- Unity는 한 번에 하나다. 관문(물리·commit 여유 각 4 GiB, Unity·Windows dotnet 0, WSL 서버를 함께 쓰면 물리 6 GiB)은 앞 goal의 `scripts/measure-gate.ps1`을 이 goal 근거 폴더 `scripts/`로 복사해 잰다.
- 정본 TDD(선행 시험 → 구현 → 신규 독립 검증)다. 작업자·검증자는 작업 하나 뒤 정산·종료한다.
- 메인 답을 기다릴 때는 우편함 대기를 열지 않고 턴을 끝낸다(앞 goal 「11-05 사용자 결정」 절의 대기 규칙). 작업자가 돌 때만 대기를 연다.

## 요구 원천

- 문제 근거 그림: `C:/Dev/DawnHolder_Dashboard/plans/brochure-1109/work/photo-candidates-v2-1.jpg` C03·C07 왼쪽 위(저장소 밖, 메인 제공).
- 원인(리드 관측): 문제의 초상은 `HudController`·`HudLayout`이 다루는 `character_status` 안의 `status_icon1`(기사)·`status_icon2`(마법사)다. `PartyMemberHud`(파티 정보 칸)에는 초상이 없다. 초상 Image는 120×120·`preserveAspect`이고 머리 그림의 사각 영역(기사 `knight_icon_0` 934×904)을 거의 꽉 채운다. `UiWindowAppearance.AddPortraitFrame`은 `PortraitFrame_128`을 그 위에 같은 크기로 덮을 뿐이다. 테두리 안쪽은 원이라 사각 모서리가 금색 고리 밖으로 나온다.
- 녹화 재사용 원천(앞 goal 근거 폴더 `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/`): 녹화 빌드 계약 v12(sha256 `e0c846ced1c70ac00a6d73762ddd6c6f5578cbe833e7a4831ad1994d29717083`), 진짜 녹화 실행 계약 v3(`e4fec9185aadefd05b94077a8466f66b3e78af36f18002258f4a5271b219ef33`), 3회차 계약 r3-v1(`3a58472538c552abbcfbad3586028a549595c934b2508dcfb7f8f3760f81ab47`).
- 앞 goal: [시연 자동 진행기·캐릭터 선택창](../2026-10-31-demo-autopilot-character-select/goal.md), [로그인 화면·길드 창](../2026-10-30-login-screen-guild-window/goal.md).

## 범위

세부 설계·대안은 승인된 초안 원문 §2~§12가 정본이다. 아래는 승인 뒤 고정한 요약이다.

### 만들 것

- **(가) HUD 초상 자르기:** 초상마다 테두리 안쪽 원 크기의 원형 마스크를 두고 머리 그림은 그 원 안에서만 보이게 한다. 금색 테두리는 마스크 밖 맨 위에 그린다. 얼굴이 원 가운데에 오게 그림 크기·위치를 맞춘다. 기사·마법사 둘 다다. 설계 추천은 `HudLayout.Awake`에서 stencil `Mask`와 그림 자식을 만들고, 원 모양은 코드가 만드는 원 텍스처로 하는 것이다. 이 추천을 따르면 `UI.unity`와 새 그림·`.meta`가 0이다.
- **(나) 로그인 화면 배치:** 위에서 아래로 「ID [칸]」, 「PW [칸]」, START 버튼, QUIT 버튼이다(메인 결정 1). 버튼은 한 줄에 하나다. 장면 객체를 옮기는 `MainMenuLoginView` 방식을 유지한다.
  - PW 칸은 계정 칸을 복제해 코드로 만들고 이름은 `PasswordInput`이다. `MainMenu.unity`는 쓰지 않는다.
  - PW 보임새(메인 `msg_b6e61dd1536c` 요청): 입력 글자는 `*`로 가린다(TMP `contentType` Password, 글꼴에 있는 ASCII 글자). 칸 모양은 ID 칸과 같다. 칸 안 안내 글(placeholder)과 「확인하지 않음」 같은 안내 문구는 두지 않는다. PW는 155 A로 화면 칸일 뿐이라는 사실은 `domains/client.md`와 이 goal에 적는다. 최대 길이는 32자다.
  - PW 값은 보내는 패킷·PlayerPrefs·로그 어디에도 넣지 않는다. 빈 PW도 막지 않는다(확인하는 척하지 않는다).
  - 서버 주소 칸은 화면에서 숨긴다. 객체와 직렬화 연결은 남긴다. 값은 지금 코드 그대로 PlayerPrefs `ServerHost` → 없으면 `_defaultHost`(127.0.0.1)다(메인 결정 5).
  - Tab은 ID → PW다. ID·PW 칸의 Enter는 START와 같은 시작을 한 프레임에 한 번 부른다.
  - 배너(`TITLE`)와 창이 겹치지 않게 창 머리 띠 「로그인」을 뺀다(메인 R-2가 배치 원문의 세부로 받음).
  - 자동 진행기 `DemoAutopilot.EnterGame`은 지금처럼 `AccountNameInput`·`ServerHostInput`을 채우고, PW 칸에 고정 글자 8자를 더 채운다. 영상에 가림 글자가 보이게 하려는 것이다.
- **(다) 녹화 빌드와 세 회차 재녹화:** PR1이 main에 들어간 commit을 고정 head로 녹화 빌드를 다시 만든다. 녹화 빌드 계약 v12와 스크립트를 이 goal 근거 폴더 `recording-build-01/`로 복사하고 고정 head·경로만 바꾼다. 서버 쪽 트리(`02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`)가 `518f920f` 대비 diff 0이면 서버는 지금 빌드 그대로 쓰고 R1을 다시 돌지 않는다. diff가 있으면 메인을 거쳐 Core에 R1을 청한다.

| 회차 | 녹화 대상(보이는 창) | 진행용(숨긴 창) | 보여 줄 장면 |
|---|---|---|---|
| 1 | 기사 `rec2_knight`·「여명기사」 | 마법사 `rec2_ally_mage`·「여명동료」 | 새 로그인 화면 → 둘 다 생성 → 마을 → … → 끝 |
| 2 | 마법사 `rec2_mage`·「여명법사」 | 기사 `rec2_knight`·「여명기사」 | 새 로그인 화면 → 마법사 생성, 기사는 1회차 캐릭터 선택 → … → 끝 |
| 3 | 기사 `rec2_knight`·「여명기사」 | 마법사 `rec2_ally_mage`·「여명동료」 | 서버 재시작 뒤 같은 계정 로그인 → 목록에 1회차 기사가 남은 화면 → 선택 → 마을 입장. 둘 다 새로 만들지 않는다 |

  - 실행 계약 v3·r3-v1의 스크립트를 복사하고 허용 고침(이름·소리 인자·경로·고정 head)만 한다. 녹화 사슬(`gfxcapture`·`h264_nvenc`·WASAPI loopback)은 바꾸지 않는다.
  - 소리(153 A): 회차마다 녹화 대상 클라는 소리 인자 없이 BGM과 효과음을 내고, 진행용 클라는 `-demo-mute`다. 코드 변경은 0이다(기존 옵션).
  - 회차 사이 재시작과 3회차 앞 재시작은 메인이 한다(137 A).
  - 결과는 편집 파일 표(영상·소리·장면 표식·SHA256·장면 프레임 범위·소리 자름 범위)로 메인에 보낸다. 편집과 PPT 삽입은 메인 보조 몫이다.
- **문서:** 이 goal, CURRENT의 Content 줄, BACKLOG 새 절(144 A·서버 주소 실행 인자·진짜 비밀번호 인증 세 행), `domains/client.md`의 로그인 화면·HUD 초상 문단.

### 건드릴 곳

- (가): `03_Client/Assets/Scripts/UI/HudLayout.cs`, `UiWindowAppearance.cs`(`AddPortraitFrame`을 부르는 곳은 `HudLayout`뿐, grep 확인), HUD 시험(`HudFrameSkinTests` 등), 새 초상 시험. `UiWindowCompositionRenderTests`는 렌더 판정의 재사용 후보다.
- (나): `MainMenuLoginView.cs`, 필요하면 `MainMenuController.cs`(Tab·숨긴 주소 칸), `Demo/DemoAutopilot.cs`(PW 채움 한 곳), 로그인 시험(`MainMenuLoginWindowSceneTests`·`MainMenuLoginSceneWiringTests`·`MainMenuLoginIndependentSubmitTests`), `GuildWindowButtonLabelTests`의 로그인 칸 참조, 새 배치 시험.
- (다): 근거 폴더 실행 파일만. 저장소 쓰기 0.
- 문서: 이 goal, `00_Document/operations/CURRENT.md`(Content 줄), `00_Document/operations/BACKLOG.md`(새 절), `00_Document/domains/client.md`.

### 하지 않을 것

- 서버·패킷·PDL·DB 변경, 진짜 비밀번호 확인(155 A).
- `UI.unity`·`MainMenu.unity` 직렬화 값 변경, 새 그림·새 스킨 부품(설계 추천을 따를 때).
- HP 줄·이름·파티 칸·스킬칸·미니맵 배치, 캐릭터 선택창, 자동 진행 시나리오 단계와 길이.
- 서버 주소 실행 인자 새로 만들기(후보 행만).
- DB 행 삭제·DDL, 녹화 도구 새 설치·전역 설정 변경, 영상 편집·PPT 삽입.

### 관찰 가능한 완료조건

| 부분 | 번호 | 완료조건 |
|---|---|---|
| (가) | A1 | 기사·마법사 초상 각각, 1280×720·1920×1080 캡처에서 머리 그림 픽셀이 금색 고리 바깥에 0이다. 판정은 렌더 결과 픽셀로 한다(구조만 보는 단정으로 대신하지 않는다). |
| (가) | A2 | 머리 그림은 마스크 아래, 테두리는 마스크 밖 맨 위다. 테두리 원과 마스크 원의 중심이 같고 마스크 지름은 테두리 안쪽 지름 이하다. |
| (가) | A3 | 보존: 직업 확정 전 두 초상 숨김, 확정 뒤 그 직업만 보임. 초상 자리 120×120·(16,-16), HP 줄·이름 위치 그대로. `UI.unity` diff 0. |
| (가) | A4 | 캡처(기사·마법사, 두 해상도)를 메인에 넘긴다. 외관 판정은 독립 검증자, 최종 확인은 사용자(결과 받고 피드백)다. |
| (나) | B1 | 위에서 아래로 ID 칸·PW 칸·START·QUIT 순서다. 네 요소의 사각형이 서로, 그리고 배너와 겹치지 않는다. 800×450 기준·1280×720·1920×1080에서 글자가 잘리지 않는다. |
| (나) | B2 | 서버 주소 칸이 화면에 보이지 않는다. 시작은 저장값 또는 127.0.0.1로 지금처럼 probe한다. |
| (나) | B3 | PW 입력은 `*`로 보인다. 그 값이 보내는 패킷·PlayerPrefs·로그에 없다. 빈 PW도 시작한다. |
| (나) | B4 | Tab은 ID→PW다. ID·PW 칸의 Enter와 START 클릭은 각각 시작을 한 번만 부른다. QUIT는 지금 종료 경로 그대로다. |
| (나) | B5 | 보존: 계정 이름 규칙·오류 문구·중복 클릭 차단·probe 성공 뒤 CharacterSelect 진입. 옛 배치를 단정하던 기존 시험은 전수 분류 표로 고친다. |
| (나) | B6 | 자동 진행기가 새 화면에서 `login`을 지나 `character list`에 닿는다. 시험과 (다) 녹화 실주행 두 곳에서 본다. |
| (나) | B7 | 캡처(빈 칸, 입력된 칸, 두 해상도)를 메인에 넘겨 사용자 확인을 받는다. |
| (다) | C1 | 고정 head 40자, exe sha256, exe+`_Data` manifest, 서버 쪽 트리 diff 원시를 남긴다. |
| (다) | C2 | 세 회차 영상·소리·표식이 있다. 1·2회차는 녹화 첫 프레임이 대상 역할의 `login` 줄보다 먼저라서 새 로그인 화면이 영상에 들어간다. 3회차는 목록에 1회차 기사가 남은 프레임이 있다. |
| (다) | C3 | 1·2회차 자동 진행이 `done`까지 가고, 3회차는 선택 → 마을 입장까지 간다. Player 로그와 단계 로그가 남는다. |
| (다) | C4 | 녹화 대상 클라는 소리 인자 없음, 진행용 클라는 `-demo-mute`다(153 A). |
| (다) | C5 | 편집 파일 표(경로·SHA256·장면 프레임 범위·소리 자름 범위)를 메인에 보낸다. |
| 문서 | D1 | 이 goal·CURRENT·BACKLOG 새 절·`domains/client.md`가 PR1에 들어가고 독립 검증이 문서도 실사한다. |

- 시험으로 못 바꾸는 요구는 「얼굴이 보기 좋게 가운데」(A4)와 로그인 화면 인상(B7)이다. 대체 실측은 두 해상도 캡처와 사용자 확인이다.

### 검증 등급

- PR1 강: 로그인 배치는 `MainMenuLoginView.cs`(204행) 대부분과 688행 시험 파일을 고쳐 제품+시험 50줄을 넘는다. HUD는 렌더 경로(stencil 마스크)를 바꾼다. 실제 변경 줄 수는 시험 작성·구현 뒤 diff 원시로 다시 남긴다.
- 녹화 빌드: 앞 goal 처분(빌드 독립 검증 약, `msg_48dee7c855ad`)을 따른다. 녹화 실행: 약(저장소 diff 0, 앞 실행 계약 v3와 같음).
- 종료 기록 PR: 문서 실사.
- R-7: 비해당(승인된 시범 목표 아님).

## PR 경계와 순서

1. PR1(새 PR) `feat/content-hud-portrait-login-20261106`, base main `518f920f`: 이 goal 문서·CURRENT·BACKLOG + (가) + (나) + 자동 진행기 PW 채움 + `domains/client.md`.
2. 녹화 빌드와 세 회차 녹화: PR 없음. PR1 병합 commit이 고정 head다.
3. 종료 기록 PR(새 PR): 종료 Gardener 뒤.

순서: 선행 시험 → 구현 → 독립 검증 → PR1 병합(사용자 승인) → 녹화 빌드·검증 → 세 회차 녹화·리드 판정·좁은 독립 검증 → 편집 파일 표 전달 → 종료 Gardener → 종료 기록 PR → 종료 점검. 막히면 바닥은 영상 v4(111.3초)이고 그 판단은 메인 몫이다.

## 진척 단계

- [x] 범위 승인
- [>] 선행 시험
- [ ] 초상·로그인 구현
- [ ] 독립 검증
- [ ] PR 병합
- [ ] 녹화 빌드
- [ ] 세 회차 녹화
- [ ] 편집 자료 전달
- [ ] 종료 기록

## 진행 기록

### 2026-11-06 진입과 범위 허가

- 진입: 메인 `msg_5a959e007b3e`(13:16:14Z). content-active detached HEAD `518f920f` = origin/main, porcelain 0. 새 Run `run_d49db3aff0b0`(receipt `lead/run-create-01.json`).
- 범위 초안 v1 READY `msg_16396aaa9ca5` → 메인 허가 `msg_6feebf9d6738`. 같은 시각대의 153 A·155 A 전달 `msg_b6e61dd1536c`.
- 열린 PR 겹침: 열린 PR은 #207(`feat/intro-site-20261008`)뿐이고 위 「건드릴 곳」과 겹치지 않는다(원시 `lead/open-prs-01.json`).

## 다음 계획 후보

- `guild-stronghold-vault-persistence`(144 A): 길드 거점·공동 금고 DB 저장, 시연 뒤.
- `login-server-host-launch-argument`(메인 결정 5): 서버 주소 실행 인자.
- `login-password-authentication`(155 A): 진짜 비밀번호 인증.
