# 시연 UI 마감과 재녹화 — HUD 초상화·로그인 화면 ID/PW/START/QUIT·세 회차 재녹화

시연 영상(제출 기한 2026-11-09 14:00 KST)을 다시 찍기 전에 두 화면을 고친다. 하나는 왼쪽 위 HUD 초상화가 금색 테두리 밖으로 튀어나오는 마감이다(152 A). 다른 하나는 로그인 화면을 ID·PW·START·QUIT 배치로 바꾸는 것이다(152 코멘트). 두 화면이 main에 들어가면 녹화 빌드를 다시 만들고 세 회차를 다시 녹화한다. 녹화에는 게임 BGM을 끄고 효과음만 담으며, BGM은 편집에서 따로 깐다(153 A, 메인 `msg_ee77a4e1bcd1`).

## 재개 지점

**다음 첫 일:** 메인이 사용자 159 C 「칸 크기들 조절이 좀 필요해」의 대상 칸을 사용자에게 묻는 중이다. 답이 오면 같은 goal 안 PR2 범위 초안을 써서 메인 허가를 받는다. 그때까지 녹화 빌드·재녹화는 시작하지 않는다(메인 `msg_57d3b8dc4883`). PR1은 PR302로 병합됐다(진행 기록 「2026-11-06 재검증 verify-02와 PR1 병합」).

- 범위 초안 v1을 메인이 원문과 대조해 허가했다(`msg_6feebf9d6738`). 초안 원문은 `.backups/verification/2026-11-06-hud-portrait-login-rerecord/lead/scope-draft-v1.md`(sha256 `a9ad70736c2f648e024501fcce638f9b95d138ec6c6f93d91be2abf8d9b1cf1d`)다. 초안 §5는 「화면 칸만」(155 A), §7은 「게임 BGM 켬」(153 A) 행으로 고정됐다. §7은 뒤에 「게임 BGM 끔, 효과음 켬」으로 바뀌었다(`msg_ee77a4e1bcd1`, 「적용 중인 사용자 결정」).
- 범위 초안 v2(캐릭터 겹침 그리기 순서 고정)도 허가됐다(`msg_950238658511`). 원문은 같은 폴더 `lead/scope-draft-v2.md`(sha256 `a45e845092d5bbb0ef4554b8aa0a93ba61e7dea2e6b31f31c0a7397b1e2191d3`)다.
- 범위 초안 v3(자동 진행기 화면 머무름 2.5초)는 메인이 A와 함께 허가했다(`msg_1f83e3674ddf`). 원문은 같은 폴더 `lead/scope-draft-v3.md`(sha256 `1e4ba0f162b8aa1d0fef369b3a9280030daa6b3398f4761f5591574fa916c460`)다.
- 기준선 U0은 끝났다(진행 기록 「2026-11-06 기준선 U0」).
- 열린 결정: 사용자 159 C의 「칸」이 어느 칸인지(메인이 사용자에게 묻는 중). 기존 시험 단정 변경 21행은 메인이 승인했다(`msg_724556cd0dc5`).
- 근거 폴더: `.backups/verification/2026-11-06-hud-portrait-login-rerecord/`.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1 메인 허가(`msg_6feebf9d6738`, 메인 결정 다섯 포함), v2 메인 허가(`msg_950238658511`), 두 화면 동시 녹화 메인 결정(`msg_700b25b2b482`), v3 메인 허가 + A(`msg_1f83e3674ddf`) |
| branch | PR1 `feat/content-hud-portrait-login-20261106`(PR302 병합, merge commit `423260f3921077a8c82955ce39da50d6a77f09a7`). PR2 `feat/content-hud-portrait-login-pr2-20261107`, base main `423260f3` |
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
  - BGM 끔으로 바꿈(사용자 원문, 메인 pane 제출 2026-11-06T16:08:36.950Z 무렵, 메인 전달 `msg_ee77a4e1bcd1`): 「시연중 BGM은 인게임으로 넣는거보다는 영상 편집으로 따로 넣는게 더 자연스럽겠다」. 메인 결정(사용자 원문 근거, 사용자 결정 아님): 위 153 A 적용의 「게임 BGM을 켜고 찍는다」를 「게임 BGM 끔, 효과음은 켬」으로 바꾼다. 녹화 대상 클라는 앞 녹화처럼 기존 인자 `-demo-no-bgm`(123 A)을 쓴다. BGM 곡 사용 허락(153 A)은 편집에서 그대로 쓴다. 녹화하지 않는 쪽 클라는 그대로 `-demo-mute`다.
- 159 C(A4 초상·B7 로그인 외관 확인, 메인 전달 `msg_57d3b8dc4883`): 「C 코멘트 … 「칸 크기들 조절이 좀 필요해」」(메인이 줄인 인용). 메인 지시: 어느 칸인지 메인이 사용자에게 묻고, 답을 같은 goal 안 PR2 범위로 전달한다. 크기가 바뀌면 다시 찍어야 하므로 그때까지 녹화 빌드·재녹화를 시작하지 않는다. A4·B7은 이 답이 반영될 때까지 열려 있다.
- 파티 정보 칸 글자색(메인 결정 `msg_57d3b8dc4883`, 사용자 결정 아님): U4 초상 캡처에서 어둡게 보인 파티 정보 칸은 실제 게임에서 밝은 바탕이라 고치지 않는다. 근거는 진행 기록 「2026-11-06 재검증 verify-02와 PR1 병합」이다.
- 캐릭터 겹침(사용자 직접 요청, 메인 pane 제출 2026-11-06T13:52:43.513Z 무렵, 메인 전달 `msg_8b8be28cab0e`): 「그리고 각각 캐릭터가 겹쳐있을때 Z-파이팅이 일어나는데 각각 시작위치를 다르게 하던가 그 부분을 해결해줘」. 재녹화 전에 고친다(메인 지시). 방법은 범위 초안 v2와 아래 「범위 초안 v2 처분」 절이다.
- 파티 구간 분할화면(사용자 직접 요청, 메인 pane 제출 2026-11-06T14:22:53.288Z 무렵, 메인 전달 `msg_89fbb9ec5651`): 「그리고 파티플레이 설명에는 잠시 좌우 분할화면으로 같이 플레이 한다는 느낌을 살짝 보여주면 좋을 거 같아」. 적용은 아래 「파티 구간 두 화면 동시 녹화」 절이다.
- 화면 머무름(사용자 피드백, 메인 pane 제출 2026-11-06T14:55:44.692Z 무렵, 메인 전달 `msg_2ec1e9fd94ff`): 「보니까 가끔 너무 짧게 빠르게 지나가서 제대로 보기 어려운 부분이 있는거 같네」. 적용은 아래 「범위 초안 v3 처분」 절이다.
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

### 대장간 장면과 범위 초안 v2 처분 — msg_6ffb5902f148·msg_8b8be28cab0e·msg_950238658511

138 A에 따른 메인 결정이며 사용자 결정이 아니다.

- 대장간 강화를 기사 화면에서 하라는 조건(`msg_6ffb5902f148`)은 철회됐다(`msg_8b8be28cab0e`). 이유는 사용자가 자막을 개조식으로 바꾸게 해 S8 자막에서 말하는 사람이 없어진 것이다. 메인 전달 사용자 원문은 「~해요 문체보다는 깔끔한 개조식 문체로 바꿔줘」다. 자동 진행 시나리오는 그대로이고, 편집 파일 표에 대장간 구간의 실제 회차·클라 원본을 적는다.
- 캐릭터 겹침은 클라 그리기 순서 고정만 한다(`msg_950238658511`). 서버 시작 위치 분산은 이번에 하지 않고 BACKLOG `player-spawn-position-spread`(담당 후보 Core)로 둔다. 사용자 보고에 이 선택을 적는다.
- 완료조건 E1~E4·C6, 같은 PR1·등급 강, 새 선행 시험 작업 `red-02`, 구현 계약 하나에 (가)(나)(라)를 승인했다. E2는 렌더 결과로 보는 완료 증거이고 구현 전 RED는 E1로 보인다. 독립 검증이 E2를 다시 렌더해 볼 수 있게 시험 이름·실행 방법을 판정 계약에 넣는다.

### 파티 구간 두 화면 동시 녹화 — msg_89fbb9ec5651·msg_d980d725da91·msg_700b25b2b482

138 A에 따른 메인 결정이며 사용자 결정이 아니다.

- 편집에서 파티 구간(S2 파티 맺기 끝 ~ S4a·b 함께 사냥 앞부분) 3~4초를 왼쪽 기사·오른쪽 마법사 화면으로 나눠 보여 준다. 같은 순간을 두 화면에서 보려고 1회차에서 두 클라 화면을 함께 녹화한다. 자동 진행 시나리오는 바꾸지 않는다.
- 리드 답(`msg_d980d725da91`): 앞 녹화 스크립트는 녹화하지 않는 클라를 960×540 `-WindowStyle Hidden`으로 띄워 창 단위 캡처가 잡지 못한다. 그래서 1회차 마법사 창을 1920×1080 popup으로 기사 창 뒤에 보이게 둔다. 창 단위 캡처(Windows Graphics Capture)가 가려진 창도 잡는다는 것은 추론이고 미관측이다. Player 설정 `runInBackground`·`visibleInBackground`가 1이라 뒤 창도 계속 그린다.
- 메인 결정(`msg_700b25b2b482`): (다) 허용 고침 목록에 「1회차 마법사 창 보이게(1920×1080 popup, 기사 창 뒤)」·「두 번째 창 회차 전체 녹화」 두 줄을 더한다. 녹화 도구 변경·새 설치는 없다. 녹화 빌드 self-test에 두 창 10초 확인(프레임 변화·검은 화면·멈춤·누락 수)을 더하고, 메모리 관문은 「1920×1080 두 창 + ffmpeg 둘」 기준으로 잰다. 실패하면 따로 찍은 두 회차의 비슷한 순간을 맞붙이고, 편집 파일 표에 두 화면의 파티 구간 단계 로그 시각을 적은 뒤 메인에 결과만 알린다.
- 소리는 기사 쪽 하나다. 1회차 마법사는 보이는 창이어도 `-demo-mute`다.

### 범위 초안 v3 처분 — msg_2ec1e9fd94ff·msg_d49beda3a7c0·msg_1f83e3674ddf

138 A에 따른 메인 결정이며 사용자 결정이 아니다. 근거는 사용자 피드백이고 되돌리기 쉽다.

- 영상 보조가 v8b에서 잰 창·글 화면 길이(`msg_172fe4ac0ea7`): 로그인 창 1.5초, 빈 캐릭터 목록 1.0초, 생성 창 2.2초, 선택 승인 글 0.6초, 파티 초대 창 1.1초, QUEST CLEAR 1.4초, 재시작 뒤 로그인 창 1.1초, 재접속 선택 승인 글 0.6초.
- 자동 진행기가 통제하는 화면(로그인 창·재시작 뒤 로그인·빈 목록·생성 창·파티 초대 창 수락 전·재접속 목록)은 다음 동작 전에 2.5초 이상 머문다. 단계 순서와 동작은 그대로다.
- **A(`msg_1f83e3674ddf`):** 자동 진행기로 늘릴 수 없는 선택 승인 글(맵 입장 흐름이 Town을 열 때까지만 보임)과 QUEST CLEAR(제품 연출 `QuestAlert` 약 1.57초)는 제품을 바꾸지 않고 편집에서 멈춤 화면으로 늘린다. 자동 진행기는 F8 뒤 2.5초 기다려 QUEST CLEAR 연출이 잘리지 않게 한다. 영상 보조에는 메인이 알린다.
- 바꿀 곳은 `DemoAutopilot.cs`와 `DemoCharacterMenuStep.cs`다. 시험은 구현 계약 안에서 구현자가 기존 자동 진행기 시험을 갱신하고 F2 시험을 더하며, 새 독립 검증자가 판정한다. 진행 중이던 red-02에는 넣지 않았다.

### 기준선 기록 지시 — msg_6f013457bd44

메인 결정이며 사용자 결정이 아니다.

- `HudBottomWindowLayoutSceneTests` 건너뜀 2건은 구현 뒤 비교 전에 건너뜀 이유(전제 조건·Ignore 문구)를 적는다. 이번 완료조건의 근거로 쓸 수 없으면 쓰지 않는다고 판정서에 남긴다.
- 나머지 건너뜀은 이름 묶음만 기록한다. CURRENT의 「길드 창 PlayMode 실패 11건」과 지금(실패 0, 길드 창 계열이 건너뜀)의 차이는 이번에 고치지 않고 종료 기록의 남은 한계에 한 줄로 적는다.

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

세부 설계·대안은 승인된 초안 원문 v1 §2~§12, v2 §1~§8, v3 §1~§7이 정본이다. 아래는 승인 뒤 고정한 요약이다.

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
- **(라) 캐릭터 겹침 그리기 순서 고정(v2):** 같은 맵의 캐릭터가 겹쳐도 앞뒤가 프레임마다 바뀌지 않게 한다. 내 캐릭터(로컬)는 다른 플레이어 캐릭터보다 항상 앞이고, 원격 캐릭터끼리는 entity id로 정한 고정 순서다. 그래서 기사 녹화에서는 기사가, 마법사 녹화에서는 마법사가 앞에 보인다.
  - 원인(리드 코드 읽기): 기사·마법사 비주얼 SpriteRenderer가 같은 정렬 레이어 `Player`에 `sortingOrder` 0이고 로컬·원격 모두 z=0이다. `Renderer2D.asset` 투명 정렬은 Default 모드라 레이어·순서·z가 같으면 앞뒤를 정할 값이 없다. 서버는 맵마다 시작 위치 하나라 같은 맵 캐릭터가 처음부터 겹친다. 깜빡임은 사용자가 영상에서 봤고 리드는 재현하지 않았다(미관측). 근거 줄은 범위 초안 v2 §1이다.
  - 값은 런타임에 정한다. 프리팹·장면 직렬화 값과 정렬 레이어·`Renderer2D` 설정은 바꾸지 않는다. 캐릭터 순서 값은 `Player` 레이어 이펙트(관측한 가장 낮은 값 4, 그 밖 5·100)보다 낮게 둬서 이펙트를 지금처럼 캐릭터 앞에 그린다. 직업 비주얼 교체(`ClassVisualMount.Attach`)와 맵 이동 뒤에도 규칙이 유지된다.
  - 설계 추천: 로컬은 `LocalPlayerSpawner`, 원격은 `RemoteEntityRegistry`가 비주얼을 붙인 직후 같은 helper 하나로 순서를 정한다. 내부 방식(`sortingOrder` 값, `SortingGroup` 등)은 구현자가 정한다. z를 조금씩 다르게 두는 방식은 추천하지 않는다.
  - red-02 계약의 리드 설계 고정(시험이 겨냥할 바깥 계약): 순서는 SpriteRenderer `sortingOrder`로만 정하고 `SortingGroup`을 더하지 않으며 z는 0 그대로다. 앞 캐릭터의 순서 최솟값이 뒤 캐릭터의 최댓값보다 크다. 원격끼리는 entity id가 작은 쪽이 앞이다. 관계는 로컬 1 + 원격 2까지 본다. 원격 뿌리 자리표시 조항은 지웠다(R02-F01, 진행 기록 「2026-11-06 선행 시험 red-02 U1」): 실제로 쓰이는 원격 프리팹은 `Resources/RemotePlayer.prefab`(`PlayerBase` 변형)이고 뿌리 SpriteRenderer가 없다. 리드가 계약에 적은 `Prefabs/Characters/RemotePlayer.prefab`은 런타임이 쓰지 않는 다른 에셋이다. 위 추천의 「내부 방식은 구현자」 가운데 `SortingGroup`과 방향은 이 고정으로 좁혔다.
- **(마) 자동 진행기 화면 머무름(v3):** 로그인 창·재시작 뒤 로그인·빈 캐릭터 목록·생성 창(이름·직업을 채운 뒤 생성 전)·파티 초대 창(수락 전)·재접속 목록마다 화면이 보인 뒤 다음 동작까지 2.5초 이상 기다린다. QUEST CLEAR는 F8 뒤 2.5초 기다린 다음 포털로 간다. 단계 순서와 동작은 그대로다. 단계 로그에 화면이 보인 시각과 다음 동작 시각이 남게 한다(없는 곳만 `[Demo] step` 줄을 더함). 녹화 길이는 회차당 약 20초 늘어난다(메인 어림). 선택 승인 글·QUEST CLEAR의 남은 짧음은 편집에서 늘린다(A).
- **(다) 녹화 빌드와 세 회차 재녹화:** PR1이 main에 들어간 commit을 고정 head로 녹화 빌드를 다시 만든다. 녹화 빌드 계약 v12와 스크립트를 이 goal 근거 폴더 `recording-build-01/`로 복사하고 고정 head·경로만 바꾼다. 서버 쪽 트리(`02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`)가 `518f920f` 대비 diff 0이면 서버는 지금 빌드 그대로 쓰고 R1을 다시 돌지 않는다. diff가 있으면 메인을 거쳐 Core에 R1을 청한다.

| 회차 | 녹화 대상(보이는 창) | 진행용(숨긴 창) | 보여 줄 장면 |
|---|---|---|---|
| 1 | 기사 `rec2_knight`·「여명기사」 | 마법사 `rec2_ally_mage`·「여명동료」(숨기지 않고 기사 창 뒤 1920×1080, 회차 전체 함께 녹화 — 「파티 구간 두 화면 동시 녹화」 절) | 새 로그인 화면 → 둘 다 생성 → 마을 → … → 끝 |
| 2 | 마법사 `rec2_mage`·「여명법사」 | 기사 `rec2_knight`·「여명기사」 | 새 로그인 화면 → 마법사 생성, 기사는 1회차 캐릭터 선택 → … → 끝 |
| 3 | 기사 `rec2_knight`·「여명기사」 | 마법사 `rec2_ally_mage`·「여명동료」 | 서버 재시작 뒤 같은 계정 로그인 → 목록에 1회차 기사가 남은 화면 → 선택 → 마을 입장. 둘 다 새로 만들지 않는다 |

  - 실행 계약 v3·r3-v1의 스크립트를 복사하고 허용 고침(이름·소리 인자·경로·고정 head, 그리고 메인 결정 `msg_700b25b2b482`의 「1회차 마법사 창 보이게(1920×1080 popup, 기사 창 뒤)」·「두 번째 창 회차 전체 녹화」)만 한다. 녹화 사슬(`gfxcapture`·`h264_nvenc`·WASAPI loopback)은 바꾸지 않는다.
  - 소리(153 A, `msg_ee77a4e1bcd1`): 회차마다 녹화 대상 클라는 `-demo-no-bgm`으로 효과음만 내고, 진행용 클라는 `-demo-mute`다. BGM은 편집에서 깐다. 코드 변경은 0이다(기존 옵션).
  - 회차 사이 재시작과 3회차 앞 재시작은 메인이 한다(137 A).
  - 3회차는 재시작 뒤 로그인 창·재접속 목록·마을 도착 뒤까지 끊지 않고 담는다. 녹화 끝은 「마을 도착 + 5초 이상」이다. 사용자가 확정한 해설 대본이 이 구간에 세 줄(약 14초)을 쓴다(메인 `msg_54b808acd693`).
  - 결과는 편집 파일 표(영상·소리·장면 표식·SHA256·장면 프레임 범위·소리 자름 범위, 대장간 구간의 실제 회차·클라 원본)로 메인에 보낸다. 편집과 PPT 삽입은 메인 보조 몫이다.
- **문서:** 이 goal, CURRENT의 Content 줄, BACKLOG 새 절(144 A·서버 주소 실행 인자·진짜 비밀번호 인증·서버 시작 위치 분산 네 행), `domains/client.md`의 로그인 화면·HUD 초상·캐릭터 그리기 순서 문단.

### 건드릴 곳

- (가): `03_Client/Assets/Scripts/UI/HudLayout.cs`, `UiWindowAppearance.cs`(`AddPortraitFrame`을 부르는 곳은 `HudLayout`뿐, grep 확인), HUD 시험(`HudFrameSkinTests` 등), 새 초상 시험. `UiWindowCompositionRenderTests`는 렌더 판정의 재사용 후보다.
- (나): `MainMenuLoginView.cs`, 필요하면 `MainMenuController.cs`(Tab·숨긴 주소 칸), `Demo/DemoAutopilot.cs`(PW 채움 한 곳), 로그인 시험(`MainMenuLoginWindowSceneTests`·`MainMenuLoginSceneWiringTests`·`MainMenuLoginIndependentSubmitTests`), `GuildWindowButtonLabelTests`의 로그인 칸 참조, 새 배치 시험.
- (라): `03_Client/Assets/Scripts/Rendering/`(새 helper 또는 `ClassVisualMount.cs`), `Bootstrap/LocalPlayerSpawner.cs`, `State/RemoteEntityRegistry.cs`, 새 시험(이름 `CharacterOverlapSort`로 시작).
- (마): `03_Client/Assets/Scripts/Demo/DemoAutopilot.cs`, `Demo/DemoCharacterMenuStep.cs`, 기존 자동 진행기 시험(`DemoAutopilotVerifyBoundaryTests`·`DemoLaunchOptionsTests` 등), 필요하면 새 시험(이름 `DemoAutopilotDwell`로 시작).
- (다): 근거 폴더 실행 파일만. 저장소 쓰기 0.
- 문서: 이 goal, `00_Document/operations/CURRENT.md`(Content 줄), `00_Document/operations/BACKLOG.md`(새 절), `00_Document/domains/client.md`.

### 하지 않을 것

- 서버·패킷·PDL·DB 변경, 진짜 비밀번호 확인(155 A).
- `UI.unity`·`MainMenu.unity` 직렬화 값 변경, 새 그림·새 스킨 부품(설계 추천을 따를 때).
- HP 줄·이름·파티 칸·스킬칸·미니맵 배치, 캐릭터 선택창, 자동 진행 시나리오 단계 순서(아래 화면 머무름 2.5초 이상만 바꿈, v3).
- 퀘스트 배너(`QuestAlert`) 연출 시간과 캐릭터 선택 뒤 맵 입장 흐름의 표시 시간(v3 A).
- 서버 주소 실행 인자 새로 만들기(후보 행만).
- 서버 플레이어 시작 위치 변경(후보 `player-spawn-position-spread`), 정렬 레이어·`Renderer2D` 투명 정렬 설정 변경, 적·NPC·이펙트 순서 변경.
- 대장간 강화를 기사 쪽으로 옮기는 것 같은 자동 진행 시나리오 변경(`msg_6ffb5902f148` 철회).
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
| (라) | E1 | 로컬 캐릭터와 원격 캐릭터를 같은 자리(겹침)에 두면 모든 캐릭터 SpriteRenderer의 (정렬 레이어, `sortingOrder`, z) 조합이 서로 달라 앞뒤가 정해진다. 로컬이 원격보다 앞이다. 원격 둘이면 entity id 순서로 정해진다. |
| (라) | E2 | 겹친 두 캐릭터를 60프레임 동안 렌더해 겹친 곳의 표본 픽셀이 매 프레임 같고 그 값이 로컬 캐릭터 쪽이다. 렌더 결과로 보는 완료 증거이며 독립 검증이 같은 시험으로 다시 렌더한다. |
| (라) | E3 | 직업 비주얼 교체 뒤, 맵 이동으로 다시 붙은 뒤에도 E1이 유지된다. |
| (라) | E4 | 보존: 캐릭터 순서 값이 `Player` 레이어 이펙트 순서(4·5·100)보다 낮다. 프리팹·장면·`Renderer2D.asset` diff 0. |
| (마) | F1 | 1·2회차와 3회차 단계 로그에서 로그인 창·빈 목록·생성 창·파티 초대 창(수락 전)·재시작 뒤 로그인·재접속 목록마다 화면이 보인 시각 → 다음 동작 시각이 2.5초 이상이다. QUEST CLEAR는 F8 → 포털 이동 시작이 2.5초 이상이다. 녹화 뒤 리드 판정과 좁은 독립 검증이 본다 |
| (마) | F2 | 캐릭터 메뉴 단계의 머무름을 시험으로도 본다(시각을 넣어 `Advance`를 부르면 2.5초 전에는 누르지 않고, 뒤에는 지금과 같은 순서로 누른다). 단계 순서·버튼·로그 문구의 기존 시험은 그대로 통과한다 |
| (다) | C1 | 고정 head 40자, exe sha256, exe+`_Data` manifest, 서버 쪽 트리 diff 원시를 남긴다. |
| (다) | C2 | 세 회차 영상·소리·표식이 있다. 1·2회차는 녹화 첫 프레임이 대상 역할의 `login` 줄보다 먼저라서 새 로그인 화면이 영상에 들어간다. 3회차는 목록에 1회차 기사가 남은 프레임이 있다. |
| (다) | C3 | 1·2회차 자동 진행이 `done`까지 가고, 3회차는 선택 → 마을 입장까지 가고 영상이 마을 도착 뒤 5초 이상 이어진다(`msg_54b808acd693`). Player 로그와 단계 로그가 남는다. |
| (다) | C4 | 녹화 대상 클라는 `-demo-no-bgm`(효과음만), 진행용 클라는 `-demo-mute`다(153 A, `msg_ee77a4e1bcd1`). |
| (다) | C5 | 편집 파일 표(경로·SHA256·장면 프레임 범위·소리 자름 범위, 대장간 구간의 실제 회차·클라 원본)를 메인에 보낸다. |
| (다) | C6 | 1·2회차 영상에서 두 캐릭터가 겹친 구간의 앞뒤가 바뀌지 않는다. 리드가 정지 화면 표본으로 판정하고 독립 검증이 대조한다. |
| (다) | C7 | 1회차에 기사·마법사 두 창 영상이 같은 시간대로 있어 파티 구간을 좌우로 나눌 수 있다. 녹화 빌드 self-test의 두 창 10초 확인(프레임 변화·검은 화면 0·멈춤 0·누락 수)이 먼저 통과해야 한다. 통과하지 못하면 대안(두 회차 맞붙이기)으로 가고 편집 파일 표에 두 화면의 파티 구간 단계 로그 시각을 적는다(`msg_700b25b2b482`). |
| 문서 | D1 | 이 goal·CURRENT·BACKLOG 새 절·`domains/client.md`가 PR1에 들어가고 독립 검증이 문서도 실사한다. |

- 시험으로 못 바꾸는 요구는 「얼굴이 보기 좋게 가운데」(A4)와 로그인 화면 인상(B7)이다. 대체 실측은 두 해상도 캡처와 사용자 확인이다.

### 검증 등급

- PR1 강: 로그인 배치는 `MainMenuLoginView.cs`(204행) 대부분과 688행 시험 파일을 고쳐 제품+시험 50줄을 넘는다. HUD는 렌더 경로(stencil 마스크)를 바꾼다. 실제 변경 줄 수는 시험 작성·구현 뒤 diff 원시로 다시 남긴다.
- 녹화 빌드: 앞 goal 처분(빌드 독립 검증 약, `msg_48dee7c855ad`)을 따른다. 녹화 실행: 약(저장소 diff 0, 앞 실행 계약 v3와 같음).
- 종료 기록 PR: 문서 실사.
- R-7: 비해당(승인된 시범 목표 아님).

## PR 경계와 순서

1. PR1(새 PR) `feat/content-hud-portrait-login-20261106`, base main `518f920f`: 이 goal 문서·CURRENT·BACKLOG + (가) + (나) + (라) + (마) + 자동 진행기 PW 채움 + `domains/client.md`. 결함 번호·commit은 부분별로 나눈다. 선행 시험은 `red-01`(가·나)과 `red-02`(라) 두 작업이고, (마)의 시험은 구현 계약 안에서 구현자가 쓴다(v3). 구현 계약은 하나다.
2. PR2(새 PR, 범위 대기) `feat/content-hud-portrait-login-pr2-20261107`, base main `423260f3`: 사용자 159 C 「칸 크기 조절」. 대상 칸과 범위는 메인 허가를 받은 범위 초안으로 정한다(메인 `msg_57d3b8dc4883`). 이 문서의 PR1 뒤 기록도 이 branch에 둔다.
3. 녹화 빌드와 세 회차 녹화: PR 없음. 마지막 화면 PR의 병합 commit(PR2가 생기면 PR2)이 고정 head다.
4. 종료 기록 PR(새 PR): 종료 Gardener 뒤.

순서: 선행 시험 → 구현 → 독립 검증 → PR1 병합(사용자 승인) → PR2 칸 크기 조절(범위 허가 뒤 같은 순서, 사용자 승인 병합) → 녹화 빌드·검증 → 세 회차 녹화·리드 판정·좁은 독립 검증 → 편집 파일 표 전달 → 종료 Gardener → 종료 기록 PR → 종료 점검. 막히면 바닥은 영상 v4(111.3초)이고 그 판단은 메인 몫이다.

## 진척 단계

- [x] 범위 승인
- [x] 선행 시험
- [x] 화면 세 가지 구현
- [x] 독립 검증
- [x] PR1 병합
- [>] PR2 칸 크기 조절(대상 칸 확인 대기)
- [ ] 녹화 빌드
- [ ] 세 회차 녹화
- [ ] 편집 자료 전달
- [ ] 종료 기록

## 진행 기록

### 2026-11-06 진입과 범위 허가

- 진입: 메인 `msg_5a959e007b3e`(13:16:14Z). content-active detached HEAD `518f920f` = origin/main, porcelain 0. 새 Run `run_d49db3aff0b0`(receipt `lead/run-create-01.json`).
- 범위 초안 v1 READY `msg_16396aaa9ca5` → 메인 허가 `msg_6feebf9d6738`. 같은 시각대의 153 A·155 A 전달 `msg_b6e61dd1536c`.
- 열린 PR 겹침: 열린 PR은 #207(`feat/intro-site-20261008`)뿐이고 위 「건드릴 곳」과 겹치지 않는다(원시 `lead/open-prs-01.json`).
- 범위 초안 v2(캐릭터 겹침) `msg_6ab864144170` → 메인 허가 `msg_950238658511`.

### 2026-11-06 기준선 U0

- Unity 창 1 메인 허가 `msg_8cd36e944e27`. 리드가 `scripts/run-baseline-window.ps1`로 HEAD `0b7abf58`에서 돌렸다. 스크립트 시작·끝 `date -u` 출력은 「Fri, Nov  6, 2026  1:40:13 PM」 → 「Fri, Nov  6, 2026  1:53:52 PM」이다(`lead/baseline-launch-01.clock.txt`). 관문 두 번 통과(`baseline-01/gate-editmode.json`·`gate-playmode.json`).
- EditMode: total 1830, passed 1830, failed 0, skipped 0. PlayMode: total 283, passed 235, failed 0, skipped 48. 요약 원시는 `lead/baseline-summary-01.json`(sha256 `fb59b4d205b093ec864c6b5a83f197c80b6e5e9fe163726a8fb3309340ce676a`)이고 메인이 R-2로 대조해 같다고 했다(`msg_6f013457bd44`).
- 건너뜀 48의 이름 묶음(메인 집계): `GuildWindowIndependentSceneTests` 15, `UiWindowLiveCaptureTests` 8, `GuildGrowthEffectAreaEvidenceTests` 7, `GuildGameplayWindowTests` 6, `BrightRemainingScreensCaptureTests` 3, `HudBottomWindowLayoutSceneTests` 2, `GuildLiveServerWindowTests` 2, `MapEntryServerIntegrationTests` 1.
- 창 끝 Unity 0·Windows dotnet 0(`baseline-01/return-observation.json`). Unity drift(`MinimapRT.renderTexture`·`ProjectSettings.asset`)는 `baseline-01/drift-diff.patch`로 남기고 이름으로 되돌렸다.

### 2026-11-06 선행 시험 red-01 기존 시험 분류

- 작성자 blocking ask `msg_1b8148b5163e`. 분류표 `red-01/t7-classification-v1.md`(sha256 `bd2818afb1ea042c0e2f44b78d322ec102f26814da2661546baceef2af1fc18e`): 변경 21행(모두 옛 구현 세부), 보존 28행.
- 메인 결정 `msg_724556cd0dc5`: 승인 + 이름 바꿈. A09·A10과 뜻이 어긋나는 다른 이름은 새 뜻으로 바꾸고 「옛 이름 → 새 이름」 대응표를 보고와 U1 비교 원시에 넣는다. 리드 회신 `msg_d203422de863`.
- 작성자 자기 신고: `red-01`을 `-Force`로 만들어 생성 전 부재를 따로 기록하지 못했다. 덮어쓴 파일은 없다고 했다. 메인 결정으로 차단 사유가 아니고 기록만 남긴다.

### 2026-11-06 선행 시험 red-01 U1

- Unity 창 2 메인 허가 `msg_49caa3d60745`. 작성자 worker_done `msg_27ccdcb68c70`(outcome succeeded). 보고 `red-01/report.md`(sha256 `9bbd6411686fd7cbd0fff801ff1bfaec268cca753ec2205e9c78a0439bb545d5`). 제품 파일 변경 0. 작업자 pane은 리드가 닫았다(`lead/close-red-worker-01.txt`). 메인 보고 `msg_0405e8043532`.
- 실패한 시도 둘: u1은 입력 관문의 Windows PowerShell 5.1 배열 처리 결함으로 멈췄다(Unity 미시작). u1b는 EditMode 뒤 PlayMode 직전 ProjectSettings 자동 변경 검출로 멈췄다. 리드가 원시를 확인하고 다시 돌리도록 승인했다(`msg_a841524e78dc`·`msg_e6027a25a690`). 자동 변경은 리드가 되돌렸다(`lead/u1b-drift-restore-01.txt`·`lead/u1c-drift-restore-01.txt`).

| 판 | U0 전체/통과/실패/건너뜀 | U1 전체/통과/실패/건너뜀 | 기존 시험 U1 통과/실패 | 새 시험 통과/실패 |
|---|---|---|---|---|
| EditMode(`red-01/u1b`) | 1830/1830/0/0 | 1842/1831/11/0 | 1828/2 | 3/9 |
| PlayMode(`red-01/u1c`) | 283/235/0/48 | 294/231/15/48 | 229/6 | 2/9 |

- 의도한 RED: EditMode 9(A2 2, A1 1280×720 2, B4 5), PlayMode 13(B1~B3 3, B4 2, B3·B5 2, B6 시험 쪽 1, 승인받고 고친 기존 시험 B1·B4 5). 1280×720 고리 바깥 픽셀은 기사 207, 마법사 572다.
- 시험 결함 F-01(3건): 1920×1080에서 `Assert.AreEqual(Vector2)` 정확 비교가 먼저 실패한다(`HudPortraitClipTests.cs:182` 두 건, `MainMenuLoginIdPwSceneTests.cs:194` 한 건). 그래서 1920의 픽셀·배치는 미관측이다. 리드 처분: red-02 계약에서 그 두 곳만 축마다 0.5px 허용으로 고치고 같은 Unity 창에서 두 시험 클래스를 다시 돌린다.
- 기존 시험 새 실패 F-02(1건): `KillRewardFloatTextSceneTests.KillInTheOldMap_IsForgottenAfterTheMapTransition`(313행)의 시험 자체 시간 전제 「1초 안」이 1.19332886초로 깨졌다. U0에서는 통과했고 그 시험·제품은 바뀌지 않았다. 원인은 미확정이다. 리드 처분: 고치지 않고 구현자·독립 검증자 전체 실행에서 다시 나오는지 본다.
- 미관측: T5 송신 바이트·prefs·로그와 T6 PW 길이 8은 PW 칸이 없어 앞 단정에서 멈췄다.
- 옛 이름 → 새 이름: `T4_EnterInTheServerAddressField_WithABadName_ShowsTheNameRule` → `T4_EnterInThePasswordField_WithABadName_ShowsTheNameRule`, `T5_TabInTheAccountField_SelectsTheServerAddressField` → `T5_TabInTheAccountField_SelectsThePasswordField`. U0 비교는 이 대응으로 했고 빠진 기존 시험은 0이다(`red-01/comparison-01.json`).
- 창 끝: PlayMode exit 14:21:11Z, 작성자 반환 관찰 14:21:12.115Z Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  2:22:16 PM」 Unity.exe 0·dotnet.exe 0.

### 2026-11-06 선행 시험 red-02 U1

- Unity 창 3 메인 허가 `msg_9b6e8d4c0bd6`. 작성자 worker_done `msg_c967cf76bac2`(outcome succeeded, Task `task_a9688b38f23a`). 보고 `red-02/report.md`(sha256 `91e7d113ffaf2d7608b622461de7790348d16c189105b099d6ff6403f4d1a89a`). 제품 파일 변경 0. 작업자 pane은 리드가 닫았다(`lead/close-red02-worker-01.txt`). 리드 승인 보충: S6 두 곳에 실제 값 출력 줄(`msg_47bcdc1c013a`).
- 필터 실행: EditMode `HudPortraitClipTests` 8건 통과 2·실패 6, PlayMode `CharacterOverlapSortSceneTests` 3건 + `MainMenuLoginIdPwSceneTests` 11건 = 14건 통과 2·실패 12. 실행 15:00:08Z~15:03:46Z, 반환 관찰 Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  3:11:16 PM」 Unity.exe 0·dotnet.exe 0, 자동 변경은 리드가 되돌렸다(`lead/red02-drift-restore-01.txt`).
- S6(F-01 고침): 1920×1080 실제 값은 `(1920.00012, 1080)`이었다. 세 시험이 해상도 확인을 지나 요구 단정에서 실패한다(HUD 고리 바깥 기사 585·마법사 1339, 로그인 B1~B3). red-01 대비 바뀐 것은 이 세 시험의 첫 실패 문구뿐이다.
- E2(`CharacterOverlapSortSceneTests.S3_EveryOverlappingPixelShowsLocalFor60Frames`): 60프레임 모두 겹침이 있고(프레임당 1508~1618픽셀) 모두 로컬과 다른 값이 그려졌다. 구현 뒤 완료 증거로 다시 렌더한다. 실행 방법은 `red-02/report.md` 「E2 재실행 계약」이다.
- **R02-F01(리드 계약 결함, 첫 발생):** S1·S2가 원격 뿌리 자리표시 SpriteRenderer가 없어 존재 전제에서 멈췄다. E1 관계·E3 유지·S4 일부는 미도달이다. 원인은 리드가 red-02 계약 「지금 상태」에 런타임이 쓰지 않는 `Prefabs/Characters/RemotePlayer.prefab`을 근거로 적은 것이다. 실제 주입 경로는 `Combat/CombatBootstrap.cs` 188·200행의 `Resources/RemotePlayer.prefab`(source `PlayerBase.prefab`)이다. 처분: 자리표시 조항을 설계에서 지우고, 새 시험 작성자(`red-03`)가 시험 169~185행의 자리표시 블록만 지운 뒤 같은 클래스를 다시 돌려 E1·E3 RED를 관측한다. 교정 층: 첫 발생이라 이 기록만 남긴다. 리드는 이후 계약의 「지금 상태」에 런타임 주입 경로(누가 어떤 에셋을 로드하는지)를 함께 적는다.

### 2026-11-06 선행 시험 red-03 U1

- 계약 `lead/red-contract-03.md`(sha256 `c3e9febbe5ba308b558fad2c7ad843c52d84d644d869b83c9435d266067b3c3d`). Unity 창 4 메인 허가 `msg_b8c7a240b32f`. 작성자 worker_done `msg_fd89d662d769`(outcome succeeded, Task `task_3ef2ea4f1cfb`). 보고 `red-03/report.md`(sha256 `5cc0c883c78d10747c6b9175a3d128dfff378f96be14eb793afe70b7ac765d89`). pane은 리드가 닫았다(`lead/close-red03-worker-01.txt`). 시험 commit `3b69bd01`.
- 고친 곳: `CharacterOverlapSortSceneTests`의 원격 자리표시 단정 블록만 지우고 `Visual` 존재 단정은 남겼다.
- 실패한 시도 하나: 첫 실행은 Unity 전 harness 시한 검사에서 멈췄다. 호출 셸의 `ConvertFrom-Json`이 시한을 DateTime으로 바꿔 Z가 빠지고 KST로 읽혔다. 리드가 ISO 문자열로 넘기는 재시도를 승인했다(`msg_598b1eaa8088`).
- PlayMode 필터 3건: 통과 0·실패 3. S1은 E1 관계 실패 3, S2는 6단계 모두 도달하고 관계 실패 13(E1·E3 RED). 원격이 없는 `HuntingGround new local` 단계는 비교 쌍이 없어 보존 검사 4개만 통과한다. S3은 red-02와 같은 종류의 60프레임 불일치(E2 RED)다. 레이어·이펙트 하한·z·`SortingGroup` 보존 검사 72개는 통과했다(E4 시험 쪽).
- 창 끝: 작성자 반환 관찰 15:28:20Z Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  3:34:26 PM」 Unity.exe 0·dotnet.exe 0. 자동 변경은 리드가 되돌렸다(`lead/red03-drift-restore-01.txt`).

### 2026-11-06 구현 U2

- 계약 `lead/impl-contract-01.md`(sha256 `e492b3d0e4a3aaae9b279ac409f1d958746353046065f71a185db0f842f569fc`). Unity 창 5 메인 허가 `msg_1028adb24f68`. 구현자 worker_done `msg_1f89af81d642`(outcome failed, Task `task_aedc25145341`). 보고 `impl-01/report.md`(sha256 `3239e1e61d1443102c14527fbf350eb2518ec90405336f66f6fb46d37c5e6499`), 진단 `impl-01/u2-diagnosis.md`(sha256 `c348361ed7505de1e60e4fde0794c2f1dd0f44beba0026c5c833fcfd27c17ee4`). pane은 리드가 16:23:35Z에 닫았다(`lead/close-impl-worker-01.txt`).
- 계약 보충(리드 승인): 새 `Advance(state, panel, now)` 모양 때문에 허용 목록 밖 시험 두 파일의 호출만 적응했다. `DemoAutopilotVerifyBoundaryTests.cs`(도우미 `AdvancePastDwell`과 호출 6곳)와 `DemoAutopilotVerifyConnectionTests.cs`(reflection 인자 `0f` 하나)이고, 기존 단정·기대값·순서는 그대로다. 구현자 ask `msg_fb96a4e422f5`, 리드 회신 `msg_6f197a41d3bc`.
- 결과: EditMode 1849건 통과 1849·실패 0·건너뜀 0. PlayMode 297건 통과 247·실패 2·건너뜀 48이고, 건너뜀 이름은 U0과 같다. F-02(`KillRewardFloatTextSceneTests.KillInTheOldMap_IsForgottenAfterTheMapTransition`)는 이번엔 통과했다. 창 끝 관찰 16:11:02Z Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  4:23:25 PM」 Unity.exe 0·dotnet.exe 0. 자동 변경은 리드가 되돌렸다(`lead/impl-drift-restore-01.txt`).
- 남은 실패 둘(구현자 진단, 분류 제안 (a) 유력·(d) 원인 미확정)
  - U2-F01 `HudPresentationLifecycleTests.ReenabledOrRecreatedHud_ShowsCurrentValues_AndAfterTheConnectionEnds_RestoresNone`: 시험의 `Drawn`이 `showMaskGraphic=false`인 원 마스크까지 「그려진 Graphic」으로 보고 그 Sprite 참조를 모은다. UI 장면을 내린 뒤 그 참조의 `texture`를 읽어 `MissingReferenceException`이 난다. 제품은 구현 계약대로 장면이 내려갈 때 런타임 원 sprite를 파괴한다.
  - U2-F02 `CharacterOverlapSortSceneTests.S3_EveryOverlappingPixelShowsLocalFor60Frames`(E2): 순서 값은 로컬 3·원격 2이고 S1·S2는 통과했다. 첫 프레임 겹친 표본 1618 중 128이 「로컬만」 판과 달랐고, 128 모두 로컬 실루엣 1화소 경계였다(내부 차이 0). 시험이 알파를 관측하지 않고 겹친 모든 화소가 같기를 단정한다.
- 처분(리드, `msg_b04a27595db5`): 판정 시험을 구현자가 고치면 구현과 판정의 분리가 깨진다. 그래서 두 시험 전제 보완을 새 시험 작성자 작업 `red-04`로 떼어 낸다. 구현 commit은 이 상태로 남기고 제품은 바꾸지 않는다. S3는 로컬 몸의 불투명 피복을 같은 프레임에서 따로 관측해 완전 피복 표본만 같음을 요구한다(goal E2 「표본 픽셀」 안의 시험 설계 고침). 경계 깎기·허용 오차 올리기는 쓰지 않는다.
- **R04-F01(리드 계약 결함, 첫 발생):** red-02 계약의 E2 판정이 렌더 화소의 알파 합성을 고려하지 않았다. R02-F01(런타임 주입 경로)과는 다른 종류다. 교정 층: 첫 발생이라 이 기록만 남긴다. 리드는 이후 화소 비교 계약에 표본의 피복(알파) 조건을 함께 적는다.
- 메인 동의(`msg_f0cfb4e2eab5`, 조건 둘): 1) 고친 S3는 구현 전 판(red-02·red-03 PNG)에서 여전히 실패해야 하고 그 결과를 원시로 남긴다. 허용 오차 올리기·경계 깎기·건너뜀은 쓰지 않는다. 2) 독립 검증 계약에 「구현 뒤 고친 시험 두 개가 요구를 약하게 만들지 않았는지」(고치기 전·후 단정 비교, 제품 계산 복제 여부)를 판정 항목으로 넣는다. 메인은 승인 전 이 판정 원문을 R-2로 본다.
- (마) 새 단계 로그 이름 11개(녹화 단계 판정 F1이 읽는다): `login-screen-visible`→`login-submit`, `menu empty shown`→`menu empty advance`, `menu create shown`→`menu create advance`, `menu occupied shown`→`menu occupied advance`, `invite-screen-visible`→`invite-accept`, `quest-dwell-ended`(기존 `quest` F8 줄과 짝). 기존 이름과 순서는 그대로다.

### 2026-11-06 시험 전제 보완 red-04 U1

- 계약 `lead/red-contract-04.md`(sha256 `6bb77aa8dba96e2dc77d3df2a7d13520e1e28a7a28f1b0c45c51bd9cbd19fb4a`, 메인 조건 1을 넣어 다시 조립했고 먼저 조립한 판 `red-contract-04.unissued-01.md`는 발행하지 않았다). Unity 창 6 메인 허가 `msg_a81cf3f0ad38`. 작성자 worker_done `msg_edbb432336a6`(outcome succeeded, Task `task_ccceed7bb5d0`). 보고 `red-04/report.md`(sha256 `0ae14fc0a71582272d36ac7c78879f651d48b5bf0284e490a030733a80592f0f`). pane은 리드가 16:48:28Z에 닫았다(`lead/close-red04-worker-01.txt`).
- 고친 곳(분류 (a), 표 `red-04/change-table.md`)
  - S3: 같은 프레임에서 로컬 몸만 빈 GameObject 레이어 31로 옮겨 검은·흰 배경에 두 번 더 그린다. 두 판이 같은 화소를 완전 피복으로 보고, 기존 겹친 표본과 완전 피복의 교집합에서만 「둘 다」 판이 「로컬만」 판과 같기를 요구한다. 허용 오차 2/255와 60프레임은 그대로다. 프레임마다 완전 피복 표본이 1개 이상이고 겹친 표본의 절반 이상이어야 한다(보호 기준). 레이어·카메라 설정은 판 렌더 뒤 되돌린다.
  - `HudPresentationLifecycleTests.Drawn`: 같은 GameObject에 켜진 `Mask`가 있고 `showMaskGraphic`이 거짓이면 그리지 않은 것으로 본다(두 줄).
- 조건 1 정적 원시(`red-04/prior-plates-analysis.json`, 내부 대용은 이웃 8화소가 모두 로컬인 화소): red-02·red-03 판은 내부 1414화소 중 불일치 1407로 실패, U2 판은 0이다.
- PlayMode 필터 9건(`CharacterOverlapSortSceneTests` 3 + `HudPresentationLifecycleTests` 6): 통과 9·실패 0·건너뜀 0. E2는 60프레임 모두 완전 피복 표본 불일치 0이다. 프레임별 겹친 표본 1509~1618, 완전 피복 1360~1478, 부분 피복 129~159다.
- 리드 계약 결함 하나(작성자가 바로잡음): 부록 C의 「Python 도구 작성」 제외 이유가 이 작업(정적 분석 Python)에 맞지 않았다. 작성자가 CODE_CONVENTION Python 절을 읽고 따랐다. 조립 스크립트의 고정 제외 문구 탓이며, 다음 계약부터 Python을 쓰는 작업은 그 절을 넣는다.
- 창 끝: 작성자 관찰 16:42:16Z Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  4:48:18 PM」 Unity.exe 0·dotnet.exe 0. 자동 변경 되돌리기는 worktree `index.lock`(크기 0, 16:42:53Z 생성)에 막혀 메인에 처리를 청했다(`msg_3a245816de48`). 메인이 10-21 사용자 A 조건(크기 0·611초·git.exe 0)으로 지웠고(`msg_dcb5bd3b5e89`), 리드가 16:53:52Z에 되돌렸다(`lead/red04-drift-restore-02.txt`).

### 2026-11-06 독립 검증 verify-01

- 계약 `lead/verify-contract-01.md`(sha256 `aec0739e4cbc895e5406d05d1372e9c995c4898d8fd3ee1bcb2a37ae4d987b7f`, 등급 강, 메인 조건 2 포함). Unity 창 7 메인 허가 `msg_cf0d6f697880`. 검증자 worker_done `msg_b41398752818`(Task `task_d24dcf93c5d7`, verifies `task_aedc25145341`). 판정 `verify-01/verdict.md`(sha256 `e436d1d5dd1444f658587648a467c68db4de62f2a980c67f1578eb056ed1f295`). pane은 리드가 17:32:33Z에 닫았다(`lead/close-verify-worker-01.txt`).
- U3(HEAD `d85b69a2`): EditMode 1851건 통과 1851, PlayMode 298건 통과 250·실패 0·건너뜀 48(이름 U0과 같음). F-02 재발 없음. 창 끝 17:18:13Z Unity 0·dotnet 0. 리드 확인 `date -u` 「Fri, Nov  6, 2026  5:32:23 PM」 Unity.exe 0·dotnet.exe 0. 자동 변경은 리드가 되돌렸다(`lead/verify-drift-restore-01.txt`).
- 판정: **불합격, 낮음 차단 2건.** 제품 동작 결함은 0이다.
  - V-01: `HudLayout.cs:18~19` 주석이 저장소 밖 근거 경로를 가리킨다. 리드 구현 계약 48행이 「근거 캡처 경로를 적는다」고 지시했다(리드 계약 결함, 첫 발생). 교정: 이 기록만 남긴다. 리드는 이후 계약에서 제품 주석에 근거 폴더 경로를 넣으라고 하지 않는다. 근거는 goal·보고에 둔다.
  - V-02: `CURRENT.md` Content 줄이 「다음은 선행 시험」이라 goal과 어긋났다. 리드가 단계 글을 빼고 goal 링크만 남겼다.
- 초상 오프셋 상수의 근거(V-01로 주석에서 옮김): 구현 U2 캡처 `impl-01/u2/captures/portrait-knight-1280x720.png`(sha256 `4e83f6285c29ead8929e017bb939dbcfdae312c0ab977d11a2a30a5a7ea8d9af`)·`portrait-knight-1920x1080.png`(`664f23fb922f1fb0e1153ead47e493ec522045697a28529c4d5640ace778b4d6`)·`portrait-mage-1280x720.png`(`0c0ba741b9e36f62f9f89eda4ca4cf342f41133175d1a03d4c1d6e4b16a1dbdd`)·`portrait-mage-1920x1080.png`(`960919edac9a53fa99e648dc9946a854f32ba400334dfc4ab52baead94fc09df`).
- 메인 조건 2 판정(판정 82~84행): red-04의 S3 피복판과 `Drawn` 두 줄은 요구를 약하게 만들지 않았다. 과거 PNG 재현은 첫 프레임 내부 대용이라는 한계가 있다.
- 완료조건: A1~A3·B1~B5·E1~E4·F2 통과. A4·B7은 독립 외관 통과이고 사용자 확인이 남았다. B6은 시험 부분 통과이고 녹화 실주행이 남았다. D1은 V-02였다. F1·C1~C7은 녹화 단계다.
- 비차단 관찰 D-01: 로컬 붙임 뒤 registry가 로컬 순서를 한 번 더 매긴다. 성능 손해는 측정하지 않았고 지금 구조를 유지한다(리드 결정).
- 리드 결정: 검증자가 쓴 판정용 시험 두 파일(`DemoUiFinishVerifyRenderOrderTests`는 여러 몸 렌더러 정렬, `DemoUiFinishVerifyLoginPathTests`는 실제 자동 진행 login → character list)을 남겨 PR1에 넣는다. U3에서 통과했고 재검증이 다시 본다.
- 다음: V-01은 제품 파일이라 새 구현자(`fix-01`)가 주석만 고친다. 그 뒤 새 검증자(`verify-02`)가 고친 commit을 실사하고 최종 HEAD에서 전체 EditMode·PlayMode를 다시 돈다. 메인 동의 `msg_33c9ac962433`(처리 계획 1~5, 판정용 시험 두 개 PR1 포함, 병합 준비 보고에 외관 캡처 3~4장).

### 2026-11-06 V-01 고침 fix-01

- 계약 `lead/fix-contract-01.md`(sha256 `3adabd775bfc35e1718cc3d7a1333b17fa3f35da15b6e5a499dd25ccc730a50f`). 구현자 worker_done `msg_f758fd4828c2`(outcome succeeded, Task `task_997652a8d729`). 보고 `fix-01/report.md`(sha256 `d19167d2db35d8d5875c95a8753052b13ecae042070123881b0196234045dc17`). pane은 리드가 17:44:41Z에 닫았다(`lead/close-fix01-worker-01.txt`). Unity 실행 없음.
- 고친 곳: `HudLayout.cs`의 저장소 밖 캡처 경로 주석 두 줄을 지웠다. 얼굴 중심 이유 주석은 남았다. 코드 변경 0(commit `4e2a97c7`).
- 재검증 등급: 처리 계획 4의 「약」을 「강」으로 고친다. 이번 재검증 대상에 남긴 판정용 시험 242줄이 들어가 제품+시험 50줄 이상이기 때문이다(등급 정본 「검증 강도 4주 시범」).

### 2026-11-06 재검증 verify-02와 PR1 병합

- 계약 `lead/verify-contract-02.md`(sha256 `e68d078d0809a736c3c5f30f264d6c2d34bdc87a85d9e2107f11a6758a0a2f68`, 등급 강, 메인 동의 `msg_374f8d465226`). Unity 창 8 메인 허가 `msg_e9e8e1d96090`. 검증자 worker_done `msg_667c12f0ceee`(Task `task_55c4488998d4`, verifies `task_997652a8d729`). 판정 `verify-02/verdict.md`(sha256 `b0378e3fa72f7123cea03576904a304b87d264daaa24c193a1d7e3ac3e540b48`). pane은 리드가 닫았다(`lead/verify02-worker-close-01.json`).
- 판정: **통과, 차단 결함 0건.** V-01·V-02가 해소됐고 새 결함은 없다. 메인이 원문을 R-2로 읽고 보고와 같다고 했다(`msg_25570efe6ff2`).
- U4(HEAD `c8c19321`): EditMode 1851건 통과 1851, PlayMode 298건 통과 250·실패 0·건너뜀 48(이름 U0·U3과 같음). E2는 60프레임 모두 완전 피복 표본 불일치 0이다(겹친 표본 1508~1619, 완전 피복 1364~1478). F-02 재발 없음, 원인 미확정은 그대로다. 리드 확인 `date -u` 「Fri, Nov  6, 2026  6:10:47 PM」 Unity.exe 0·dotnet.exe 0. 자동 변경은 리드가 되돌렸다(`lead/verify02-drift-restore-01.txt`).
- 완료조건: A1~A3·B1~B5·E1~E4·F2·D1 통과. A4·B7은 독립 외관 통과이고 사용자 확인에서 159 C가 왔다(「적용 중인 사용자 결정」). B6은 시험 부분 통과이고 녹화 실주행이 남았다. F1·C1~C7은 녹화 단계다.
- 사용자 확인용 캡처(U4, 같은 HEAD): `verify-02/u4/captures/portrait-knight-1280x720.png`(sha256 `4e83f6285c29ead8929e017bb939dbcfdae312c0ab977d11a2a30a5a7ea8d9af`)·`portrait-mage-1280x720.png`(`0c0ba741b9e36f62f9f89eda4ca4cf342f41133175d1a03d4c1d6e4b16a1dbdd`)·`login-idpw-empty-1280x720.png`(`2e23607cdf3482aeb5c7946d89408ee37aad6177e900b5c7da52f82875a79fb2`)·`login-idpw-filled-1280x720.png`(`47eb3a79bd4c784e568168623fe8c6f99dcdee0f73d02478ed196fe5c12e3c85`).
- 파티 정보 칸 글자색(메인 질문 `msg_25570efe6ff2`): 초상 캡처의 파티 정보 칸은 어두운 바탕에 어두운 글자로 보인다. 칸 안 글이 장면 기본 글(`전사 (Player)`·`마법사 (Other)`)이라 `PartyMemberHud`가 갱신하지 않은 시험 장면 상태다. 앞 goal 리허설 확대 `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/party-panel-crop-r4-1-vs-r4-2.png`(sha256 `9aa7fdabc0297f8ec12325acc964cede399d84f4133bf9d654cb1c0e135997c6`)에서는 밝은 바탕에 어두운 글자로 읽힌다. 그 뒤 `PartyMemberHud.cs`는 바뀌지 않았고, `99.UI/UI.unity`의 마지막 변경 `fb7ff496`은 골드 표시 객체만 지웠다. 시험 장면이 왜 어둡게 그리는지는 확인하지 않았다. 메인이 확대본을 보고 고치지 않기로 정했다(`msg_57d3b8dc4883`).
- 검증자의 범위 밖 기록: 기존 서버 시험 `GuildWire.cs:7`에 저장소 밖 근거 주석 1건이 있다. 메인 지시로 BACKLOG 후보로만 적는다(`guild-wire-test-local-evidence-comment`).
- PR302 생성(head `c8c193210f2c1cb58a2bd0aab83d059ef176fc4a`, 본문 `lead/pr1-body.md` sha256 `4b71b26cdb98183872a32c8c97b900e64f8ac365eb7ab5a63126abc23929e586`). CI 4개 모두 통과했다. 완료 시각은 code-rules 18:16:22Z, module-boundaries 18:16:24Z, architecture-tests 18:22:17Z, dotnet-tests 18:40:09Z이고 mergeStateStatus는 CLEAN이었다(`lead/pr1-view-03.json`). 병합 준비 보고 `msg_e70d66efb1c5`.
- 병합: 사용자가 메인 pane에 「병합 승인: PR302 head c8c193210f2c1cb58a2bd0aab83d059ef176fc4a」를 제출했고, 메인이 head·CI를 다시 대조해 병합했다. mergedAt 2026-11-06T19:05:01Z, merge commit `423260f3921077a8c82955ce39da50d6a77f09a7`(메인 `msg_57d3b8dc4883`). 리드가 받은 origin/main과 PR head의 tree 차이는 0이다(`lead/post-merge-fetch-01.txt`).
- 다음: 녹화는 멈춘다. 159 C의 대상 칸이 정해지면 PR2 범위 초안을 쓴다. PR2 branch는 `423260f3`에서 만들었다(`lead/pr2-branch-01.txt`).

## 다음 계획 후보

- `guild-stronghold-vault-persistence`(144 A): 길드 거점·공동 금고 DB 저장, 시연 뒤.
- `login-server-host-launch-argument`(메인 결정 5): 서버 주소 실행 인자.
- `login-password-authentication`(155 A): 진짜 비밀번호 인증.
- `player-spawn-position-spread`(메인 결정 `msg_950238658511`): 같은 맵 플레이어의 서버 시작 위치 분산.
- `guild-wire-test-local-evidence-comment`(메인 `msg_25570efe6ff2`): 서버 시험 `GuildWire.cs:7`의 저장소 밖 근거 주석.
