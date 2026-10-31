# 시연 자동 진행기 DB 캐릭터 메뉴·캐릭터 선택창 3칸·길드 입구 겹침

11-06 시연 영상을 DB 영속 서버 한 판으로 찍을 수 있게 한다. 캐릭터 선택창을 3칸 넓은 화면으로 바꾸고(85 A), PR268 자동 진행기가 그 화면을 지나 끝까지 가게 고친다(82 B·83 A). 길드 입구가 Gold 칸을 덮는 겹침도 고친다(74 A). 녹화용 빌드 계약은 Core 리드와 맺는다.

## 재개 지점

**다음 첫 일:** 캐릭터 선택창(PR-A) 구현 계약을 발행한다. 선행 시험(RED)은 commit됐고, 기준은 진행 기록 10-31 「캐릭터 선택창 시험」 항목이다. PR-C(길드 입구)와 Art PR4 사후 확인은 사용자 결정 90 A로 리셋(11-03 20:00 KST) 뒤 메인이 다시 열 때까지 착수하지 않는다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1을 메인이 원문과 대조해 승인했다(`msg_967d20c30e6d`, 메인 결정). 초안 원문은 `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/scope-draft-v1.md`(sha256 `28fa67062d6395521aad8f8022e87387ca7a43dec18c101c3aed4f157756ed15`)다. |
| branch | PR-A `feat/content-demo-character-select-20261031`, base main `5bfda911a8ba97a8f9f82b75693dec3ebc59fb54`. PR268 `feat/demo-autoplay-20261028`(head `1237cb58`), PR-A 위에 쌓는다. PR-C `fix/content-guild-entry-gold-overlap-20261031`(아직 없음), base main |
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
2. PR-A 병합 전까지 PR268 base를 PR-A branch로 바꾼다. 바꾼 시각과 receipt를 이 goal에 남긴다.
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

### 운영 방식(진입 메시지의 메인 결정)

- 병합은 PR마다 사용자 명시 승인으로 메인만 한다. 병합 준비 보고에는 head 40자, 판정 원문 경로·sha256, CI를 넣는다.
- Unity는 한 번에 하나다. 창마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」 세 줄로 청한다. 관문(물리·commit 여유 각 4 GiB, Unity·Windows dotnet 0, WSL 서버를 함께 쓰면 물리 6 GiB)은 매번 저장 스크립트 `scripts/measure-gate.ps1`로 잰다.
- 선행 시험 작성자·구현자는 신규 claude-opus-5-5 xhigh, 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 정본 TDD(선행 시험 → 구현 → 검증)이며 결정 34 A의 경량 경로는 넓히지 않는다.
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
- **녹화용 빌드 계약:** Core 리드와 고정 head·Unity 버전과 hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그·빌드 부작용 복원을 이 goal의 계약 절로 맞춘다. 빌드 스크립트는 근거 폴더 실행 파일로 둔다.
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
| 빌드 | D1 | Core에 고정 head·Unity hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그 경로를 넘긴다. 리허설 빌드는 PR268 병합 뒤 main, 시연 빌드는 마지막 병합 뒤 고정 head로 다시 만든다. |

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
- [>] 캐릭터 선택창 구현
- [ ] 캐릭터 선택창 검증
- [ ] 자동 진행기 시험
- [ ] 자동 진행기 구현
- [ ] DB 서버 실주행 검증
- [ ] 길드 입구 겹침(리셋 뒤)
- [ ] 선택창 PR 병합
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
