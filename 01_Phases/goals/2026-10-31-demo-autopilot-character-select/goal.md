# 시연 자동 진행기 DB 캐릭터 메뉴·캐릭터 선택창 3칸·길드 입구 겹침

11-06 시연 영상을 DB 영속 서버 한 판으로 찍을 수 있게 한다. 캐릭터 선택창을 3칸 넓은 화면으로 바꾸고(85 A), PR268 자동 진행기가 그 화면을 지나 끝까지 가게 고친다(82 B·83 A). 길드 입구가 Gold 칸을 덮는 겹침도 고친다(74 A). 녹화용 빌드 계약은 Core 리드와 맺는다.

## 재개 지점

**다음 첫 일:** 녹화용 빌드 계약을 Core 리드와 맺는다. 기준은 Art 인벤토리 격자 PR과 Core 인벤토리 24칸 PR이 들어간 main이다(메인 `msg_e0b59e962663`). 두 PR이 아직이면 기다린다. 계약 초안 v4는 근거 폴더 `lead/recording-build-contract-draft-v4.md`다(이름은 사용자 결정 109 A, 창 크기는 110 A, Core 답 `msg_45e700da3330` 반영). 남은 빈칸은 두 PR의 번호·merge commit과 고정 head다. 두 PR이 들어가면 Content가 고정 head 40자를 Core에 먼저 보내고, Core는 그 SHA를 서버 branch에 받는다. Art 격자 PR은 04:33Z 기준 아직 없다(Art `msg_3f625498414d`). PR268은 main에 들어갔다(merge commit `3bb76772`, 진행 기록 11-01 「PR268 병합」). 캐릭터 선택창(PR-A, PR #291)도 main에 있다(`58c8d4cb`). PR-C(길드 입구)와 Art PR4 사후 확인은 사용자 결정 90 A로 리셋(11-03 20:00 KST) 뒤 메인이 다시 열 때까지 착수하지 않는다. Gardener와 종료 기록 PR은 PR-C·녹화용 빌드 전달 뒤다(「PR 경계와 순서」 4).

- 판정용 새 시험: 검증자 v3가 쓴 `DemoAutopilotRecheckLifecycleTests`·`DemoAutopilotRecheckLegacyTests` 두 파일과 `.meta`는 PR268에 넣지 않았다. 검증한 head를 바꾸지 않으려는 리드 결정이다. 사본과 sha256은 `pr268-verify-03/untracked-tests-saved/`에 있다. 검증자는 유지를 권고했다. 채택은 종료 기록 PR에서 BACKLOG 다음 후보로 둔다(메인 `msg_e0b59e962663`).
- 열린 결정: 없음.
- 빌드 계약 입력: PR-A·PR268(둘 다 완료)·Art 인벤토리 격자 PR·Core 인벤토리 24칸 PR 병합(`msg_d0012352013c`·`msg_d783ac17eb63`). Core 24칸 PR은 프로토콜을 v23으로 올리므로 녹화 빌드는 그 PR이 들어간 main에서 서버·클라를 함께 만든다.
- 근거 폴더: `.backups/verification/2026-10-31-demo-autopilot-character-select/`. 리드 Git 밖 재개 메모는 `lead/resume-state.md`다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1을 메인이 원문과 대조해 승인했다(`msg_967d20c30e6d`, 메인 결정). 초안 원문은 `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/scope-draft-v1.md`(sha256 `28fa67062d6395521aad8f8022e87387ca7a43dec18c101c3aed4f157756ed15`)다. |
| branch | PR-A `feat/content-demo-character-select-20261031`(PR #291), base main `5bfda911a8ba97a8f9f82b75693dec3ebc59fb54`, main 반영 commit `58c8d4cb9b8a34d66389d5573797e07564c6fa7a`. PR268 `feat/demo-autoplay-20261028`, base main 그대로(메인 `msg_db6333b6b1c6`), content-active 로컬 이름 `content/demo-autoplay-20261028`. PR-A와 최신 main을 합쳐 그 위에 쌓는다. PR-C `fix/content-guild-entry-gold-overlap-20261031`(아직 없음), base main |
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

### 10-31 사용자 결정 97 A — msg_d783ac17eb63

메인 `msg_d783ac17eb63`(2026-10-31T17:52:47Z)가 전달한 사용자 원문이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 97 - 인벤토리를 24칸(4×6)으로 늘릴지, 8칸(4×2)으로 둘지 → A 24칸으로 늘림

- 앞선 원문: 「아니면 인벤토리 크기를 더 늘려도 돼, 내 생각에는 메이플스토리 인벤토리 크기 정도면 상관없어」.
- 같은 메시지의 사용자 원문: 「병합 승인: PR291 head fb3d65b19cc253157edc361e0ac237221df32baf」 → 메인이 PR #291을 main `58c8d4cb`로 넣었다(17:51:49Z).
- 메인 지시: 시연 빌드 입력에 「Core 인벤토리 24칸 PR 병합」을 더한다. 빌드는 PR268·Art 격자 PR·Core 24칸 PR이 모두 main에 들어간 commit에서 만든다. 창 6은 그대로 가고, PR268 branch에 main(`58c8d4cb`)을 합치는 일은 창 6 뒤·구현 commit 전에 한다.
- 메인 결정(메인 화면 18:04Z, 사용자 결정 아님): 클라 받는 쪽(패킷 읽기·공유 DLL)은 Core, 화면 격자는 Art가 맡는다. 병합은 Art 격자 PR 다음 Core 24칸 PR이다. Core 24칸은 프로토콜 버전을 22에서 23으로 올리므로 그 전 클라 빌드는 새 서버에 붙지 못한다.
- Content 조율(Core `msg_50bf844d60ce` → 리드 `msg_6e1243c63eb4` → Core `msg_a6e92219cbe1`): Content가 고칠 클라 파일은 없다. 인벤토리 wire 시험 도구(`InventoryClientTestSupport`·`InventoryClientContractTests`)는 Core 24칸 PR이 고친다. 창 8은 Core 24칸 병합 전에 v22로 돈다.

### 11-01 사용자 결정 100 A·101 A·102 A — msg_a12e08143d11

메인 `msg_a12e08143d11`(2026-10-31T23:55:02Z)이 전달한 사용자 원문이다(11-01 08:5x KST, 메인 pane Enter 제출). 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 100 - 인벤토리 24칸(4열×6줄) 창 높이를 키울지(칸 81px), 지금 높이 720px를 지킬지 → A 창을 키운다(칸 81px) · 2) 101 - 목표 종료 정리 점검 세션(Gardener)을 앞으로도 Codex Sol max로 열지, 정본대로 Claude Opus로 돌릴지 → A Codex Sol max 유지 · 3) 102 - 「작업자를 Codex로」에 선행 시험 작성자도 넣을지(지금 해석), 구현자만으로 좁힐지 → A 시험 작성자 포함(대신 Astra로)

- 메인 해석(사용자 결정 아님): 선행 시험 작성자는 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, 태그 `[<파트> 시험 Astra]`다. 구현자는 신규 Sol max `[<파트> Sol]` 그대로다. 이 goal 종료 Gardener는 신규 Sol max다. 이미 떠 있는 세션의 모델은 바꾸지 않는다. 메인이 CLAUDE.md 4A 절에 반영하는 PR을 열며, 병합 전에도 지금부터 적용한다.
- 100 A는 Content 파일에 닿지 않는다. 빌드 계약 입력은 그대로다.
- 기록 시점: 원문이 온 때 검증자 v2가 기준 HEAD·미커밋 0을 실행 직전마다 확인하고 있어, 검증자 종료 뒤 이 절을 썼다(리드 `msg_e342a1af00a7`).

### 11-01 D1 회귀 시험 채택 — msg_ee36970aad0b

메인 결정이다(사용자 결정 아님, 2026-11-01T00:39:57Z). 리드 제안 `msg_a498307170a7`에 대한 답이다.

- 검증자 v2(task_9b58fb5e7fd3)가 쓴 판정용 시험 두 파일을 PR268의 회귀 시험으로 채택한다. 이번 D1 수정에는 별도 `[Content 시험 Astra]`를 두지 않는다.
- 정본 TDD 문단과의 차이: goal-loop SKILL의 TDD 문단은 시험 작성 역할이 요구사항 시험과 구현 전 실패 원시를 남기게 한다. 이번 시험은 독립 검증자가 썼다. 메인은 그 규칙이 지키려는 두 가지(구현자와 독립된 세션이 시험을 씀, 구현 전 실패 원시가 있음)가 충족됐고, 쓴 세션도 102 A의 시험 작성자 배정과 같은 GPT-6-Astra라는 이유로 승인했다.
- 구현 전 실패(RED) 원시: `pr268-verify-02/u3/playmode/results.xml`의 `DemoAutopilotVerifyConnectionTests.OpenMenuBeforeHandshake_ExposesConnectingInsteadOfLegacyNull` Failed(「an open connecting menu must not be exposed as null」, 제품 HEAD `23cc2713`). EditMode 26 cases는 같은 HEAD에서 통과했다(`pr268-verify-02/u3/editmode/results.xml`, 보존 단정).
- 시험 파일 sha256(검증자 `pr268-verify-02/final-audit/test-files.json`과 리드 재계산 일치):
  - `03_Client/Assets/Tests/EditMode/DemoAutopilotVerifyBoundaryTests.cs` `f281e5274ef49a8aef5a407e0747b8173494da91be0e70374f981b7b7ff8396d`, `.meta` `0e4931bb8fb35bc63717a4c8ea2b0329691484e8b669993bb4e5a5ec3e4898ec`(GUID `f2042b106267a04408811b647c419470`)
  - `03_Client/Assets/Tests/PlayMode/DemoAutopilotVerifyConnectionTests.cs` `4a45ad635e89765d6e346c89bad3a184803a8066e52468c85ce1b46abcf2a64b`, `.meta` `b0e45cf4440584db43e8dbff9fc4824563a1ce70e86edbc2399bbfb9679964a5`(GUID `49d8b613e1fa70d49b88c85f205f123f`)
- 다음 검증자는 이 두 시험을 구현자가 고치지 않았는지 이 commit 대비 diff로 확인한다.
- 구현은 신규 Sol max(구현 계약 v2)가 하고, 계약은 짧은 포인터(경로+SHA256)로 넘긴다.

### 11-01 사용자 결정 109 A — msg_00c45ff1d8e8

메인 `msg_00c45ff1d8e8`(2026-11-01T04:26:34Z)가 전달한 사용자 원문이다(메인 pane 제출). 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 109 - 11-06 녹화 영상에 보일 캐릭터 이름: Core 초안(새벽기사·새벽법사)으로 갈지, 직접 정할지 → A Core 초안 그대로

- 리허설: 계정 `rh1101_knight`·`rh1101_mage`, 캐릭터 「리허설기사」·「리허설법사」. 녹화: 계정 `rec_knight`·`rec_mage`, 캐릭터 「새벽기사」·「새벽법사」(Core `msg_035b91a40065` 초안 그대로).
- 메인 지시: 녹화 빌드 계약에 이 이름을 넣는다. 녹화 이름은 리허설에 먼저 쓰지 않는다.

### 11-01 사용자 결정 110 A — msg_586c6191bec5

메인 `msg_586c6191bec5`(2026-11-01T04:38:41Z)가 전달한 사용자 원문이다(메인 pane 제출). 이 pane의 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 110 - 녹화 때 기사·마법사 두 클라 창: 모니터마다 큰 창 하나씩, 한 모니터에 작은 창 둘 나란히 → A 모니터마다 1920×1080

- 메인 지시: 기사 클라는 주 모니터, 마법사 클라는 보조 모니터(주 모니터 왼쪽)에 각각 1920×1080 창(`-screen-width 1920 -screen-height 1080 -screen-fullscreen 0`)이다. 녹화는 사용자가 쓰는 도구로 두 화면을 따로 잡아 편집에서 합친다. 메인·리드는 녹화 도구를 띄우지 않는다.
- 모니터 지정 실행 옵션과 보조 모니터에서 창이 잘리는지는 리허설(R4)에서 화면으로 확인하고 원시로 남긴다. 확인 전에는 「계획」으로 적는다. 잘리면 메인에 알린다(창 크기 변경은 사용자 결정).
- 리허설 R2 관문(물리 6 GiB·commit 4 GiB)은 1920×1080 두 창 기준으로 잰다. 창 8의 960×540 값을 그대로 쓰지 않는다.

### 운영 방식(진입 메시지의 메인 결정)

- 병합은 PR마다 사용자 명시 승인으로 메인만 한다. 병합 준비 보고에는 head 40자, 판정 원문 경로·sha256, CI를 넣는다.
- Unity는 한 번에 하나다. 창마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」 세 줄로 청한다. 관문(물리·commit 여유 각 4 GiB, Unity·Windows dotnet 0, WSL 서버를 함께 쓰면 물리 6 GiB)은 매번 저장 스크립트 `scripts/measure-gate.ps1`로 잰다.
- 선행 시험 작성자·구현자는 PR-A까지 신규 claude-opus-5-5 xhigh였고, PR268부터 위 「작업자 Codex 전환」을 따른다. 11-01부터 선행 시험 작성자는 「11-01 사용자 결정 100 A·101 A·102 A」대로 신규 Astra xhigh다. 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 정본 TDD(선행 시험 → 구현 → 검증)이며 결정 34 A의 경량 경로는 넓히지 않는다.
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
- **녹화용 빌드 계약:** Core 리드와 고정 head·Unity 버전과 hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그·빌드 부작용 복원을 이 goal의 계약 절로 맞춘다. 빌드 입력은 PR-A·PR268·Art 인벤토리 격자 PR·Core 인벤토리 24칸 PR 병합이다(`msg_d0012352013c`·`msg_d783ac17eb63`). 빌드 스크립트는 근거 폴더 실행 파일로 둔다.
- **Art PR4 사후 확인:** PR4 head가 오면 main + PR4 트리에서 Content 일시정지 메뉴 좌표·입력 시험의 이름 단위 결과와 `UI.unity` diff를 본다. PR은 없다.

### 건드릴 곳

- PR-A: `03_Client/Assets/Scripts/UI/CharacterMenuView.cs`, 캐릭터 메뉴 시험(`CharacterMenuViewTests`·`CharacterMenuViewAppearanceTests`·`CharacterMenuViewCaptureTests`·`BrightWindowInteractionTests`의 캐릭터 메뉴 부분·`CharacterMenuMotionSceneTests`), `CharacterMenuBoundaryTests`·`CharacterMenuPresenterTests`(T7 19~23행, 메인 `msg_e6ef913df2cd`로 추가), 새 시험 `CharacterSelectWide*`, `domains/client.md`, BACKLOG 한 행, CURRENT 한 줄, 이 goal.
- PR268: `03_Client/Assets/Scripts/Demo/`, `CharacterSelectController.cs`(읽기 전용 통로 하나), EditMode 시험, `domains/client.md` 한 문단, PR268 본문. 11-01 D1 회귀 시험으로 PlayMode `DemoAutopilotVerifyConnectionTests.cs` 한 파일을 더한다(메인 `msg_ee36970aad0b`).
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
| 빌드 | D1 | Core에 고정 head·Unity hash·빌드 경로·exe와 `_Data` sha256·두 클라 실행 명령·로그 경로를 넘긴다. 빌드는 PR-A·PR268·Art 인벤토리 격자 PR·Core 인벤토리 24칸 PR이 모두 main에 들어간 commit에서 만들고, 격자·24칸 PR이 늦으면 기다린다(`msg_d0012352013c`·`msg_d783ac17eb63`). 리허설 빌드 뒤 병합이 더 있으면 시연 빌드는 마지막 병합 뒤 고정 head로 다시 만든다. |

### 검증 등급

- PR-A 강: 제품+시험 변경이 50줄을 넘는다(`CharacterMenuView.cs`는 457줄이고 배치 대부분을 바꾼다).
- PR268 강: 제품+시험 50줄 이상, 실제 실행 경로(Player 빌드·서버)를 지난다.
- PR-C 약: 제품 1파일·50줄 미만 예상, 영속·서버·프로토콜·I/O 도구에 닿지 않는다. 시험까지 50줄을 넘으면 강으로 올린다.
- 앞 goal의 원인 미확정 기존 실패 13행(73 A)은 실패인 채 둔다. 기준 실행과 이름 단위로 비교해 새 실패만 이번 영향 후보로 본다.

## PR 경계와 순서

1. PR-A(새 PR): 이 goal 문서 + 85 A. base main.
2. PR268(기존 PR): PR-A 위에 쌓는다. base는 main 그대로다(메인 `msg_db6333b6b1c6`, 「10-31 범위 초안 v1 처분」 2항). 병합 순서는 PR-A → PR268이다. PR-A는 main에 들어갔다(`58c8d4cb`).
3. PR-C(새 PR): base main, 독립. 사용자 결정 90 답 뒤 착수.
4. 종료 기록 PR: Gardener 뒤.

## 진척 단계

- [x] 범위 승인
- [x] 캐릭터 선택창 시험
- [x] 캐릭터 선택창 구현
- [x] 캐릭터 선택창 검증
- [x] 자동 진행기 시험
- [x] 자동 진행기 구현
- [x] DB 서버 실주행 검증
- [ ] 길드 입구 겹침(리셋 뒤)
- [x] 선택창 PR 병합
- [x] PR268 병합
- [>] 녹화용 빌드 전달
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
- 2026-10-31 자동 진행기 구현(GREEN): 구현자 `[Content Sol]`(신규 Codex `gpt-6.1-sol` max, 첫 화면 「GPT-6.1-Sol max」) task_8f6b53867201 / ctx_7a13fa46a5dd, 계약 `lead/pr268-impl-contract-v1.md`(sha256 `ceb7bda4b7432d6fcbb0f3fde0bdebcd50fbbf11a400fa4596652810814a1713`), 보고 `pr268-impl-01/report.md`(sha256 `806f21ee8e58a9c9b5b65ce18fed58f181a72e88f7b7ca4723d1e58fb7f17206`), worker_done `msg_7d2b8008e7d9` succeeded, 정산 18:19Z(release retained·external_terminal, pane 닫음).
  - 제품: `DemoLaunchOptions.Character`, 새 `Demo/DemoCharacterMenuStep.cs`(114줄, `.meta` GUID 저장소 안 하나), `CharacterSelectController.MenuState`, `DemoAutopilot.EnterGame` 연결, `domains/client.md` 한 문단. 선행 시험 두 파일은 바꾸지 않았다.
  - U2(창 6, `pr268-impl-01/u2/`): EditMode 1745/1745, PlayMode 263 중 217 통과·실패 0·skipped 46. U3 기준 이름의 결과 변화 0·제거 0이다. 새 skipped 3은 Art 캡처 시험(`DAWNHOLDER_UI_CAPTURE_DIR` 미지정)이다. 리드가 결과 XML에서 새 시험 20건(메뉴 단계 11·옵션 9) 전부 Passed를 다시 셌다.
  - Win64 빌드 exit 0(545.9초), exe sha256 `91a36fa14dd3ccf390b99720805eda0553da1ca1108381b49c98c938f69dc256`(리드 재계산 일치). Legacy DEBUG 서버 + 두 클라 한 판(`run-01/`): 기사·마법사 모두 `[Demo] step 12 done`(18:08:13Z·18:08:17Z), 경로는 Legacy(`class` 로그)다. F9 금고 보충 분기는 이번 판에서 실행되지 않아 통과로 세지 않는다.
  - 창 6(17:44:00Z~): 구현자 반환 관측 18:09:21Z, 리드 재확인 18:11:28Z 모두 Unity·Windows dotnet·Player·7777 0. Unity 생성 drift 5경로는 patch를 `lead/window6-drift/`에 남기고 리드가 18:11:10Z에 복원했다. 창 끝 `msg_de18b6fadc9c`.
  - 그 뒤: PR268 branch에 main `58c8d4cb`를 합쳤다(`89646e4f`, 트리 변화 0). 구현 commit `585486f5`.
  - 창 8 구성(Core `msg_a6e92219cbe1`): 1판 새 계정으로 생성 경로 → 서버 Enter 정지·같은 창 재시작 → 2판 같은 계정으로 선택 경로. Core는 재시작 시간, 정지 요약 두 줄, 2판 목록 캐릭터가 1판과 같은지를 본다. 원시는 Content 근거에 남기고 경로만 Core에 넘긴다. B5 이름은 계정 `v268_knight1101`·`v268_mage1101`, 캐릭터 `V268Knight`·`V268Mage`다(Core 확인).
- 2026-10-31 자동 진행기 검증(중단): 검증자 `[Content 검증자]`(신규 GPT-6-Astra xhigh, 첫 화면 「GPT-6-Astra xhigh」·v0.166.0) task_f7562aa13e39 / ctx_2f64b6a4037e, 계약 `lead/pr268-verify-contract-v1.md`(sha256 `91b0f0236bc93be10bd53724b0f6753fe4a08507399578f847db0508e5d3f99a`, 범위 PR268 전체 diff, 10-28 첫 판 포함). 사용자 결정 99 → C(메인 `msg_29d5cb57b68d`)로 정적 실사 중에 멈췄다. 부분 보고 `pr268-verify-01/partial-report.md`(sha256 `9efac4a385e0314763f7ace05959c930e1c72cb0d74b1a2933b98306ae9ff5a9`), worker_done `msg_4f6a54239e8e`(outcome failed = 중단, 제품 불합격 아님), 정산 18:41Z(retained, pane 닫음).
  - 끝낸 것: 계약·입력 39개 대조, 맥락 메모, 구현자 U2 XML·빌드 hash·Legacy 로그 재계산(일치), 선행 시험이 RED 뒤 바뀌지 않음, B1 코드 경로 확인. Unity·Player·서버는 띄우지 않았다.
  - 결함 후보 C1(미재현, 확정 아님): presenter는 생성 때 그리지 않아 메뉴 접속이 끝나기 전에도 `MenuState`가 null이다. 장면 뒤 1초에 이 값이 null이면 단계가 Legacy로 판단하고, 메뉴가 열린 동안 직업 단추는 아무것도 하지 않아 입장이 멈출 수 있다(`CharacterSelectController.cs:45~65`, `CharacterMenuPresenter.cs:41~63`, `DemoCharacterMenuStep.cs:42`). 설계 관찰 O1: `WaitFor`는 시간 초과를 경고만 하고 넘어가 `done`만으로 모든 단계 성공을 보장하지 않는다.
  - 판정용 시험 2파일(미컴파일·미실행, EditMode 8메서드 26 cases + PlayMode 지연 handshake 1건)은 작업 트리에서 빼고 사본과 sha256을 `pr268-verify-01/untracked-tests-saved/`에 남겼다. 다음 검증자가 계약 v2 입력으로 다시 쓸지 정한다.
  - 남은 것: U3, 검증자 Player 빌드, 창 8 두 판, 판정 결론.
- 2026-11-01 재기동 진입: 메인 `msg_cf050c154ac8`(사용자 원문 「지난번에 하던 작업 이어서 진행해서 마무리해보자」). 같은 대화를 `--resume`으로 이었고 새 handle은 `term_b5326416-b390-44eb-81ee-fb1ada806846`이다. 재시작 뒤 Run이 옛 handle에 묶여 `check`가 `consumer_fenced`였고, 가이드대로 `run-use --id run_0a58bc3cfea0`로 다시 묶었다(`lead/run-use-resume-01.json`). READY `msg_2c6507493680`.
- 2026-11-01 자동 진행기 검증 v2(불합격): 검증자 `[Content 검증자]`(신규 GPT-6-Astra xhigh, 첫 화면 「GPT-6-Astra xhigh」·v0.166.0) task_9b58fb5e7fd3 / ctx_12f0c34ddea6, 계약 `lead/pr268-verify-contract-v2.md`(sha256 `547fdf995e1470a4f8948adad29b7be8d2cd266d05d55cf277fa17499025ce32`, 기준 HEAD `23cc2713`, 입력 55개, 앞 검증자 부분 보고·C1·저장 시험 포함), 판정 `pr268-verify-02/verdict.md`(sha256 `42af9b71a5627e46cc41ba6dfd3f04e6d916ffeafa66ae20553fd1bfda14fe20`), worker_done `msg_32cde765a1f9` succeeded, 정산 00:37Z(release retained, pane 닫음).
  - 판정: 불합격, 차단 결함 D1(높음) 1건. B1 충족, B2·B3 D1로 차단, B4 미완료, B5 이름 고정 확인. 비차단 관찰 O1(`WaitFor` timeout이 경고만 하고 진행)·O2(제품 주석의 승인 날짜)는 이번 수정에 넣지 않는다.
  - D1: 실제 CharacterSelect 장면과 handshake를 1.2초 늦춘 소유 TCP에서 `panel=True·connected=True·handshake=False·projectionNull=True·outcome=Legacy`를 관측했다(`DemoAutopilotVerifyConnectionTests.OpenMenuBeforeHandshake_ExposesConnectingInsteadOfLegacyNull`). 그 뒤 `EnterGame`이 직업 단추를 누르지만 열린 view 가드로 아무 일도 없고 메뉴 경로로 돌아가지 않는다(코드 경로 판단). 귀속은 구현 commit `585486f5`와 리드 구현 계약 v1의 null 의미 가정이다. 구현 계약(task_8f6b53867201)의 첫 독립 불합격이다.
  - U3(창, 메인 `msg_8ddb358897ec`, 00:12:00Z~00:55:00Z): EditMode 1771/1771(U2 1745 + 판정용 26), PlayMode 264 중 217 통과·실패 1·skipped 46. 기존 이름의 결과 변화·제거·skipped 차이 0. Win64 빌드 exit 0(108초), exe sha256 `91a36fa14dd3ccf390b99720805eda0553da1ca1108381b49c98c938f69dc256`(207파일 manifest). 리드 R-2 표본(XML Passed 1771, 실패 이름·메시지, exit.json, manifest의 exe hash = 실제 파일) 일치.
  - 창 8은 리드 결정(`msg_242d513475db`, 메인 동의 `msg_865bbc326329`)으로 미실행이다. 사용자 손 약 30분을 결함 빌드에 쓰지 않고, 고친 빌드로 한 번 돈다.
  - 창 반환: 검증자 00:28:46Z·리드 00:29:04Z 모두 Unity·Windows dotnet·Player 0. Unity 생성 drift 5경로는 patch를 `lead/u3v2-drift/`에 남기고 리드가 00:29:15Z에 복원했다. 창 끝 `msg_332414fedaa4`. 판정용 시험 2파일과 `.meta`는 작업 트리에 남겼다.
- 2026-11-01 Core 문의: Content 소유 `02_Server/GameServer.Tests/Dungeon/DungeonClearTestWorld.cs`의 `ViewOf`가 칸 0~7만 센다(Core `msg_e6d433cf9c2c`). 리드 동의 `msg_97c01628f0e1`(메인 사본 `msg_7714c2cc4e9a`): Core 24칸 PR이 배열만 `slot0..slot23`으로 늘리고 주석·판정·기대값은 바꾸지 않는다. PR268은 `02_Server`를 건드리지 않는다.
- 2026-11-01 자동 진행기 D1 수정: 구현자 `[Content Sol]`(신규 Codex `gpt-6.1-sol` max, 첫 화면 「GPT-6.1-Sol max」·v0.166.0) task_f169da635137 / ctx_40e49bd2fa84, 계약 `lead/pr268-impl-contract-v2.md`(sha256 `23bf1fa8cabeb8c3a73f7cdd8dcd33feadc72b543616440b5d3e2416d64082d1`, 짧은 포인터로 전달), 보고 `pr268-impl-02/report.md`(sha256 `070544ba72e27ee9a29a4c7aad923440698896b2559e5077cf99188b9c5bdaaf`), worker_done `msg_cc814f55e8c5` succeeded, 정산 01:22Z(release retained·external_terminal, pane 닫음). 구현 commit `2d7f96f5`.
  - 제품: `CharacterSelectController.Start`에서 view 생성 직후·`ConnectCharacterMenu` 전에 `MenuState = new CharacterMenuViewState(Phase.Connecting)`과 이유 주석 두 줄(+3/−0). view·presenter·Step·Autopilot·`client.md`는 바꾸지 않았다. 채택한 회귀 시험 4파일은 `69eb9fdf` 대비 diff 0이고 sha256이 「11-01 D1 회귀 시험 채택」 값과 같다(리드 재계산).
  - U2(창 A, 메인 `msg_87ab3e087e6e`, 00:59:00Z~01:32:00Z): EditMode 1771/1771, PlayMode(재실행 `u2b`) 264 중 218 통과·0 실패·46 skipped. 검증 v2 U3 대비 바뀐 이름은 회귀 시험 `OpenMenuBeforeHandshake_ExposesConnectingInsteadOfLegacyNull` 하나(Failed→Passed)다. 리드 저장 스크립트 `lead/compare-u2b-vs-u3v2.cjs` 출력(`.json` sha256 `240ba1a0ccbd56be0558a530852d4928119f0a8f7b93f6c18e4ec492a44c58bf`).
  - 첫 PlayMode(01:04:44Z)는 Unity가 IL Post Processor runner를 띄우지 못해 시험 전에 끝났다(「IL Post Processor runner process failed to start」, named pipe `unity-ilpp-…` 없음, error CS 0, 39.9초 exit 1). EditMode가 같은 소스로 통과해 도구 실패로 보고, 리드가 같은 명령을 한 번만 다시 돌리게 했다(`msg_f836560afc81`). 원시는 `pr268-impl-02/u2/playmode/`·`u2b/playmode/`에 있다. 첫 발생이라 규칙은 만들지 않는다.
  - 창 반환: 구현자 01:13:56Z·리드 01:14Z 모두 Unity·Windows dotnet·Player 0. Unity drift 두 경로(`ProjectSettings.asset`의 Standalone define 한 줄, `MinimapRT.renderTexture`)는 patch를 `lead/windowA-drift/`(sha256 `31e7aaf1de69c44f2f5f38af86cb9c4032b4caddeac71f08dddb2084e1c883e2`)에 남기고 리드가 01:14:42Z에 복원했다. 창 끝 `msg_7f4e4891faaf`.
  - 생존 신호: 01:13:56Z→01:19:11Z 315초(보고서 작성 구간, 생존 신호 helper `lead/liveness-impl2-01.json`). 구현자가 보고에 남겼다.
  - 집계: D1은 계약 귀속 결함이라 구현자 배정의 「두 번째 독립 불합격」 집계에서 뺀다(`implementer-routing.md`).
- 2026-11-01 자동 진행기 검증 v3(통과)·창 8: 검증자 `[Content 검증자]`(신규 GPT-6-Astra xhigh, 첫 화면 「GPT-6-Astra xhigh」) task_f55cdcc996cd / ctx_4064e2beda5b, 계약 `lead/pr268-verify-contract-v3.md`(sha256 `1f3f1a57cb1d258ea2d246b00a19d564c9fd6913f44a0a53ff41f4408ecb5a2c`, 기준 HEAD `cb32652f`, 입력 80개), 판정 `pr268-verify-03/verdict.md`(sha256 `85c9f0e2ba111c1db58bef9d208c3be0f3fe2b8566a98780775f95f92ad16bde`), worker_done `msg_10993cf3c1e7` succeeded, 정산 03:44Z(release retained·external_terminal, pane 닫음).
  - 판정: 통과, 신규 차단 결함 0, D1 해소. B1~B5 충족. 비차단 관찰은 새 영어 주석과 주변 한국어 주석의 언어 차이, 기존 O1·O2, 새 T2(채택한 PlayMode 시험은 단계 결과를 출력만 하고 직접 단정하지 않는다. PendingPhase EditMode 시험이 Waiting을 따로 단정한다)다.
  - 창 B(메인 배정 `msg_2d5561cb4b17`, 01:35:30Z~02:18:00Z, 리드가 검증자에게 보낸 「Unity 창 열림」은 `msg_065799b45dc0`): EditMode 1772/1772, PlayMode 265 중 219 통과·0 실패·46 skipped. 구현자 U2 대비 새 이름은 판정용 시험 각 1개뿐이고 기존 결과 변화는 0이다. Win64 빌드 exit 0(140.6초), exe sha256 `91a36fa14dd3ccf390b99720805eda0553da1ca1108381b49c98c938f69dc256`. Unity drift는 git status 5경로였다. 내용 diff가 있는 4경로를 patch `lead/windowB-drift/`(sha256 `acc0c274…f9ad`, 검증 v2 창 drift와 같은 bytes)에 남긴 뒤 리드가 5경로를 01:50:39Z에 복원했다. 창 끝 `msg_61dc2ecaee99`.
  - 창 8(사용자 결정 104 A, 메인 「W3 Listening 확인」 `msg_69d903544d22`, 03:11:17Z~04:15:00Z): 서버는 core-active `feat/db-persistence-demo-build-20261031`(`9d35f5cd`)·v22, 사용자 손으로 띄우고 껐다. 1판(03:14Z)은 새 계정으로 두 클라 모두 `character list` → `character create` → `character select slot=1` → done이다. 사용자 Enter 정지 03:27:04.78Z 뒤 같은 창 재시작 Listening 03:30:02.46Z(177.67초, 대부분 사용자 손, launcher 자체 0.81초). 2판(03:31Z)은 같은 계정으로 `character list` → `character select slot=1` → done이고 create는 0이다. 1판 생성 ID와 2판 목록 ID가 같다(기사 `f4cc6b33-d180-4ba2-b310-e5b4f91e309d`, 마법사 `8bb7d676-a979-4540-b1aa-96197a34a242`). 마지막 정지 03:34:20.96Z. 두 번의 정지 요약 줄(stdout·stderr)은 모두 `drained=True`이고 유실 항목은 0이다. 서버 창 원문 네 파일은 `pr268-verify-03/window8/server-window/`에 있다.
  - 7777 listener: 관측한 표본(재시작 관측 938개와 각 판 전후 관측)에서 Windows 쪽은 0, WSL 쪽은 서버가 떠 있는 동안 1이었다. 리드가 계약의 「listener가 하나 있다」를 두 쪽 합으로 읽어 전달했다(`msg_393f44915e76`). 첫 발생 기록이다.
  - Core 해석(`msg_04981e9d8b04`, 저장본 `lead/core-msg_04981e9d8b04-stronghold-scope.txt`): 두 판의 기사 거점 level 2·마법사 강화 level 1은 DB 저장 범위 밖이라 영속 근거도 결함도 아니다(`FEATURE_MAP.md` 39·41행). 검증자는 요청 전에 그 값이 보이는 반증 로그가 없음을 확인했다.
  - 생존 신호(helper `lead/liveness-verify3-02.json`, sha256 `36e44185c714051345eea42e255a85daa95b226439417ab57654cbba6095f715`): 300초를 넘은 구간은 01:54:39Z→02:10:27Z(948초)와 02:14:19Z→02:45:47Z(1888초) 둘이다. 둘 다 창 8 대기 중 50초 공식 대기만 되풀이하고 별도 heartbeat를 보내지 않은 구간이다. 검증자가 `waiting-followup.md`에 남겼고 리드 안내(`msg_d3e10e977e05`) 뒤 4분 이내로 바꿨다. 첫 발생이라 규칙은 만들지 않는다.
  - 정산 순서: 리드가 `worker-release`를 지원하지 않는 `--run` 옵션으로 불러 실패했고, 그사이 pane을 먼저 닫았다. 바른 옵션으로 다시 해제했다(`lead/release-verify3-02.json`). 첫 발생 기록이다.
  - 리드 R-2 표본: 판정 sha256 일치. HEAD `cb32652f`·tracked diff 0, 채택 시험 `69eb9fdf..cb32652f` diff 0. 네 Player 로그의 step 수(14·14·13·13)·create 수(1·1·0·0)·timeout 0·Exception 0과 두 캐릭터 ID가 판정과 같다. 리드 관측(명령 시작 03:42:35.686Z, 출력 시각 03:42:39Z)에서 Dawnholder·Unity·dotnet 프로세스 0, Windows 7777 Listen 0이다. 이 출력은 바로 저장하지 않아 리드 세션 기록에서 뽑아 `lead/process-port-check-0342-transcript-extract.json`(sha256 `098bfe2cfc11e82ddd3804c91f1a9108cd7185a4472e1cb4d7aecef3a55fefc0`)에 남겼다.
  - main: `58c8d4cb` 뒤 main(`ffe7f983`)은 문서 6파일만 바뀌었고 PR268이 바꾼 파일과 겹치지 않는다. 검증한 head를 지키려고 다시 합치지 않는다.
- 2026-11-01 PR268 병합: 병합 준비 보고 `msg_3b71b8105ba2`(head `6f4f721aaf104dc002e16eccadf95dc10e12e3f6`, CI 네 workflow success, MERGEABLE·CLEAN, `origin/main` `ffe7f983`과 merge-tree 충돌 0). 사용자 승인 줄 뒤 메인이 병합했다(MERGED 04:22:01Z, merge commit `3bb767726e7e10417c8347e65e003a1fcfb6099c`, 메인 `msg_e0b59e962663`).
  - 병합 관문 문서 재실사(메인 `msg_a848a7e98689`): 검증 v3 뒤 goal 기록 commit 두 개(`3e46ea65`·`6f4f721a`)를 신규 GPT-6-Astra 검증자가 따로 판정했다. 판정 원문은 `pr268-docs-recheck-01/`·`pr268-docs-recheck-02/`에 있다.
  - 리드는 병합된 branch를 떠나 최신 main에서 `docs/content-demo-autopilot-closeout-20261101`을 만들었다. 이 branch가 종료 기록 PR이 된다.
