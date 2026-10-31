# 몬스터 처치 때 「+금화 N」 떠오르는 글

## 재개 지점

**다음 첫 일:** [PR283](https://github.com/bass131/dawnholder-server/pull/283)은 병합됐다(병합 commit `f5adcf31`). 이 goal의 결과 기록은 Effect 세 goal이 함께 쓰는 종료 기록 PR(branch `docs/effect-closeout-20261031`)에 넣는다. 그 PR이 병합되면 메인이 R-8로 리드를 새로 연다. 결과는 아래 [결과](#결과)에 있다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1(`lead/scope-draft-v1.md`, sha256 `e8a69526…`)을 메인이 사용자 원문과 대조해 통과시켰다(메인 `msg_22555c3ffe7a`). 사용자 중간 승인은 없다(메인 운영 방식). |
| 리드 | `[Effect 리드 Opus]`(claude-opus-5-5 xhigh). Run `run_5f3209972e68`, 회신 주소 `run:run_5f3209972e68`. |
| branch | `feat/kill-reward-float-text-20261030`. 65 A branch `feat/attack-key-left-ctrl-20261030`의 PR282 head `70ab4f19` 위에 쌓았다. 두 PR이 `client.md`와 `CURRENT.md` Effect 줄을 함께 고치기 때문이다. PR base는 65 A branch이고, 앞 PR이 병합되면 차례로 바뀐다. 작업 공간 effect-active. 2026-10-31에 PR282가 병합되어 base가 main으로 바뀌었고, PR285와 `client.md`가 부딪혀 DIRTY가 됐다. 리드가 최신 main `dd9e755b`를 통합했다(commit `2fd74253`, 아래 [결과](#결과)). |
| 근거 폴더 | `.backups/verification/2026-10-30-kill-reward-float-text/`(Git 제외). 리드 원시는 `lead/`다. |
| 실행 창 | Unity는 effect-active `03_Client`만 쓴다. 시험 실행은 batchmode, 캡처는 Editor 창 하나다. 창은 메인 배정이다. |

## 진척 단계

- [x] goal과 계약 기록
- [x] 선행 시험과 실패 원시
- [x] 구현과 통과 원시(기존 시험 1건 (a)는 독립 검증에서 고친다)
- [x] 캡처
- [x] 독립 검증 통과
- [x] PR 생성과 확인 항목
- [x] PR283 병합
- [>] 결과 기록과 종료

## 적용 중인 사용자 결정

### 10-31 결정 76 A·77 A — 종료 Gardener 하나로 묶음, PR283 병합 뒤 바로 플레이 — msg_84d148dc5236

- 사용자 원문(2026-10-31 17:3x KST 메인 창에 Enter로 제출, 메인 전달 `msg_84d148dc5236`): 「대시보드 결정 응답: 2) 76 - Effect 종료 점검(Gardener)을 끝난 goal 셋에 하나로 묶을지 → A 하나로 묶음 · 3) 77 - 병합된 GUI 개선 묶음 직접 플레이를 언제 할지 → A PR283 병합 뒤 바로」
- 76 A: 종료 Gardener는 세 goal(65 A·개선안 3·실패 23건)에 하나만 연다. 보고서 한 파일에 goal별 절을 두고, 각 goal의 입력은 그대로 본다. **정본(goal마다 Gardener)과 다른, 이번 한 번의 사용자 결정 예외다.** 65 A goal과 실패 23건 goal은 이 절을 가리킨다.
- 77 A: 사용자가 병합된 main으로 직접 플레이한다. 메인이 effect-active에 서버 1개와 Editor 1개를 배정했다. 기록은 아래 [사용자 직접 플레이 창](#사용자-직접-플레이-창--2026-10-31결정-77-a)이다.

### 10-30 개선안 3 — 몬스터 사망 때 떠오르는 보상 글

- 사용자 원문(2026-10-30 14:1x KST 메인 pane 제출, 메인 전달 `msg_55345fc2fc09`): 「몬스터 사냥후 인벤토리로 아이템이 지급된다는 느낌이 적음, 몬스터 사망시 "+금화 몇" 이런식으로 위로 천천히 Rerf했다가 사라지는 효과도 필요함」
- 메인 해석: 「Rerf」는 「떠올랐다가」의 오타다.
- 공통 원문: 「모든 GUI 디자인은 인벤토리와 캐릭터 선택창 기준으로 완전 최신화 진행해줄것」, 「모두 끝까지 진행하고 마지막에 한번에 체크 할 수 있게 전부 작업 완수하고 보고해줘」

### 10-30 메인 처분 — 범위 초안 v1 통과 — msg_22555c3ffe7a(메인 결정, 사용자 결정 아님)

- 서버·패킷 무변경 클라이언트 안을 받는다. 값은 서버가 확정한 스냅숏 차이에서만 나오고 클라이언트가 보상을 판정하지 않는다.
- 「천천히」는 사용자가 쓴 말이다. 떠오르는 시간은 1.2초보다 조금 길게(1.5초 이상) 잡는다. 값은 상수 하나로 두고 캡처와 사용자의 마지막 플레이에서 맞춘다. 플레이 확인 항목에 「속도가 너무 빠르거나 느린가」를 넣는다.
- 남은 우려에 「처치와 같은 1.0초 안에 들어온 다른 출처의 증가도 몬스터 위에 함께 뜰 수 있다」를 더한다. 막을지는 리드가 정하고 이유를 계약에 적는다. 리드 결정은 아래 「만들 것」 3을 본다.
- PR 하나, 65 A branch 위에 쌓기, 병합 순서 65 A → 이 PR. 65 A와 70 A의 순서 무관 원칙은 그대로다. Art 답이 늦을 때의 기본값(`DungeonEntranceLabel` TMP 글꼴, 금화색)도 받는다.

### 10-30 메인 처분 — goal 차이 5곳 대조 통과 — msg_2fc417986879(메인 결정, 사용자 결정 아님)

- 범위 초안과 goal의 차이 다섯 곳(시험 층, Art 값, `client.md` 자리, 클리어 값이 증가분이라는 사실 교정, 「재화 주머니」 이름)은 리드 결정 또는 사실 교정이고 사용자 판단이 필요 없다.
- 조건 둘: 계약에 「어떤 시험이 증가분 계산을 가르는가」를 한 줄로 적는다(선행 시험 계약 보충 01로 반영). 캡처는 1920×1080·1280×720 두 장 모두 사용자 확인 묶음에 넣는다.

### 10-30 메인 처분 — 기존 시험 (a) 수정은 독립 검증자가 — msg_b50d7db4c2ab(메인 결정, 사용자 결정 아님)

- 구현 실행에서 실패한 기존 시험 1건(`InventorySceneLifecycleTests` 483~485행, 구독자 형식을 정확히 셋으로 고정)의 분류 (a)를 받는다. 수정을 독립 검증자에게 맡기는 처리도 받는다.
- 조건 셋:
  1. 정본대로 줄 단위 분류 표, 같은 명령의 전후 원시(65/1 → 66/0), 요구 출처(goal 「만들 것」 2)를 판정에 남긴다.
  2. 단정을 약하게 하지 않는다. 「정확히 셋」을 「정확히 넷(새 처치 글 형식 포함)」으로 바꾼다. 개수 단정을 지우거나 「셋 이상」으로 바꾸지 않는다. 이번에 돌지 않은 486~497행(Ending 구독자 0 등)은 수정 뒤 실제로 돌아 통과해야 한다.
  3. 486~497행이 수정 뒤 처음 돌면서 새 실패가 나면 (d) 미확정으로 두고 통과로 바꾸지 않는다. 그때는 리드를 거쳐 메인에 보고한다.
- 캡처 창은 Editor 하나만, 예상 시간과 함께 따로 요청한다.

### 10-27 결정 4 A·10-30 메인 운영 방식

- 65 A goal(`01_Phases/goals/2026-10-30-attack-key-left-ctrl/goal.md`)의 같은 이름 절을 따른다. 선행 시험 작성자·구현자는 신규 Opus xhigh, 독립 검증은 신규 GPT Astra xhigh다. 캡처·직접 플레이·병합 승인은 마지막에 한 번에 한다.

### 메인 결정 — 기존 시험 실패의 전수 분류 — msg_71e41e231d55

- 원문은 [하네스 원칙 goal의 재전달 절](../2026-10-03-harness-principles/goal.md#기존-테스트-대량-실패의-분류)이다. 선행 시험 작성자와 독립 검증자 계약에 그 절 원문을 그대로 붙인다.

## 리드 결정 — 서버를 바꾸지 않는 클라이언트 안

- 클라이언트 안 vs 새 패킷 안 중 클라이언트 안이다. 핵심 단점은 「어느 몬스터의 보상인지」를 시간 창으로 짝짓는다는 점이다. 같은 순간 둘이 죽으면 다른 몬스터 위에 뜰 수 있다.
- 고른 이유: 마감 주에 `ProtocolVersion`·생성 코드·`Shared.dll`·서버(Core 영역)를 건드리지 않는다. 되돌리기 쉽다. 받는 값은 서버가 확정한 합계의 차이라 틀리지 않는다.
- 순서 근거: 서버는 같은 틱에 사망 방송을 인벤토리 전송보다 먼저 보낸다(`GameWorld.cs` :362~:368). 클라이언트의 사망 처리(`EntityDeathHandler.cs` `EnqueueWorldApply`)와 스냅숏 적용(`InventorySnapshotHandler.cs` :40 `EnqueueApply`)은 같은 FIFO(`UnityClientSession.cs` :231 `_post`)를 탄다.

## 파트 간 계약 — Art 리드 답 — msg_ae88de6f6d11(리드 간 조율, 사용자 결정 아님)

- 원문 저장본: `lead/art-answer-msg_ae88de6f6d11.json`(sha256 `6d91be6a…e379`). Art 근거는 `art-active/.backups/verification/2026-10-29-inventory-character-select-look/floating-text-style-7c94be35-01.txt`(기준 commit 7c94be35)다.
- 리드 표본 대조(08:2xZ, effect-active HEAD `abff8e8c`): `InventoryPanelView.cs` :54~:55·:68, `QuestAlert.cs` :170·:172·:182~:183, `GuildGrowthEffectPresentation.cs` :128~:132·:338, `DungeonEntranceLabel.cs` :32·:35가 답의 값과 같다.
- 글꼴: `Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")`. 한국어는 TMP 전역 Pretendard fallback이 맡는다.
- 색: 금화 줄 #FFEB33(인벤토리 제목 색 (1, 0.92, 0.2)), 아이템 줄 #FFF0BA(길드 성장 레벨 글자 (1, 0.94, 0.73)).
- 외곽선: #281400(`Color32(40, 20, 0, 255)`), 두께 0.22. 그림자 없음. 글꼴(material)을 외곽선보다 먼저 넣는다. 외곽선 material 복사본은 표시 쪽이 소유하고 font asset을 바꾸지 않는다(`GuildGrowthEffectPresentation.cs` :128~:132 방식).
- 겹침: Art 70 A는 `client.md` :38·:50을 고친다. `EnemyRegistry.cs`·`CombatBootstrap.cs`·`Scripts/Combat/Effects/`는 Art 쓰기 예정에 없다. Art PR3는 기본 스킨 asset만 바꾼다.
- 리드 결정(되돌리기 쉬운 쪽): 이 PR의 `client.md` 문단은 70 A의 :38·:50과 65 A의 :40 문단에서 떨어진 절에 둔다. 그러면 65 A와 70 A의 순서 무관 원칙을 이 PR과 70 A 사이에도 그대로 쓸 수 있다. PR을 열 때 메인에 쌓는 순서 판단을 함께 올린다.

## 만들 것

1. 몬스터가 죽을 때 그 위치를 사망 기록으로 남긴다. `EnemyRegistry.Despawn`이 사전에서 지우기 직전의 위치를 내보낸다. 구현자가 다른 자리를 고르면 이유를 보고한다.
2. 스냅숏이 적용될 때 직전 값과 비교해 늘어난 금화와 아이템별 개수를 구한다. 줄어든 값은 무시한다. 접속 뒤 첫 스냅숏, 연결 종료·캐릭터 전환 뒤 첫 스냅숏은 기준만 잡는다.
3. 다른 출처의 증가:
   - 던전 클리어 보상은 뺀다. `S_DungeonClearResult`가 금화·보상 칸을 실어 스냅숏보다 먼저 오고(`InventoryRegistry.cs` :222 → :223), `DungeonClearRewardWindow`가 이미 보여 준다. 클리어 결과를 받은 직후 스냅숏의 증가에서 그 몫을 뺀다. 빼고 남는 값이 처치 보상이다. 클리어 결과의 값은 증가분이다(서버 `DungeonClearPackets.cs` :21이 보상 상수를 싣는다, `lead/facts-ir3-01.md`). 서버는 Success일 때만 결과 뒤 스냅숏을 보내므로 Success 몫만 빼고, CurrencyCap·InventoryFull 몫이 다음 스냅숏을 깎지 않게 한다.
   - 그 밖의 증가(보상 분배, 퀘스트 등)는 막지 않는다. 클라이언트는 출처를 구분할 패킷이 없다. 처치와 같은 시간 창이면 그 처치가 낳은 증가로 보는 것이 원문 「몬스터 사망시」에 가깝다. 선행 시험 작성자가 그 밖의 증가 경로를 원문에서 찾아 report에 목록으로 적는다.
4. 늘어난 값이 있고 1.0초 안(실제 시간)의 사망 기록이 있으면 그 위치 위에 글을 띄운다. 여럿이면 로컬 플레이어에 가장 가까운 것을 쓰고 지운다. 사망 기록이 없으면 띄우지 않는다. 창 길이는 상수 하나다.
5. 줄은 「+금화 10」이 먼저이고, 늘어난 아이템마다 「+<카탈로그 이름> N」을 덧붙인다. 이름은 `ItemCatalog.For(id).DisplayName`이다.
6. 움직임: 월드 공간 TextMeshPro가 1.5초 이상 천천히 올라가고 끝 쪽에서 투명해진 뒤 사라진다. 시간·거리는 상수다. 글꼴·색·외곽선은 위 「파트 간 계약」의 Art 값(LiberationSans SDF, 금화 #FFEB33·아이템 #FFF0BA, 외곽선 #281400·0.22, 그림자 없음)이다.
7. 수명: 맵 이동·캐릭터 전환·연결 종료 때 남은 글과 사망 기록을 지운다. 글은 raycast를 받지 않는다(Art 70 A 불변식 I1 보존).
8. `00_Document/domains/client.md`에 짝짓기 규칙과 수명을 한 문단으로 적는다. 자리는 65 A의 :40 문단과 70 A의 :38·:50에서 떨어진 절이다.

## 건드릴 곳과 소유

| 파일 | 변화 | 쓰는 세션 |
|---|---|---|
| `03_Client/Assets/Scripts/Combat/Enemies/EnemyRegistry.cs` | 사망 기록 내보내기 | 구현자 |
| `03_Client/Assets/Scripts/Combat/Effects/` 아래 새 파일 | 증가분 계산·짝짓기(일반 C#), 표시 MonoBehaviour, 떠오르는 글 | 구현자 |
| `03_Client/Assets/Scripts/Combat/CombatBootstrap.cs` | 표시 MonoBehaviour 생성 연결 | 구현자 |
| `00_Document/domains/client.md` | 한 문단 | 구현자 |
| EditMode·PlayMode 새 시험 | 아래 완료조건 | 선행 시험 작성자 |
| goal.md·CURRENT Effect 줄 | 범위·결과·근거 | 리드 |

- 새 파일 자리 근거: `Combat/Effects/`에 TMP 글을 만드는 `ZoneVisualizer.cs`가 있다. 계산·짝짓기는 Unity 객체 없이 시험할 수 있게 일반 C# 타입으로 둔다.
- 파일 겹침: Content(`msg_46b1928a158c`)는 겹침 없음. CURRENT.md는 같은 파일 다른 줄이다. Art(`msg_ae88de6f6d11`)와는 `client.md` 하나가 겹치고 줄은 떨어뜨린다. 65 A와는 `client.md`가 겹쳐 쌓는다.

## 하지 않을 것

- 서버, 패킷, PDL, 생성 코드, `Shared.dll`, `ProtocolVersion`.
- HUD 금화 표시, 인벤토리 창, 던전 클리어 보상 창.
- 다른 플레이어의 보상 표시, 데미지 숫자, 사운드.
- 몬스터 사망 연출(0.8초 뒤 파괴) 변경.
- 짝짓기를 정확하게 만드는 패킷 안. 필요하면 다음 계획 후보로 남긴다.

## 관찰 가능한 완료조건

1. 계산·짝짓기 규칙의 시험이 통과한다: 늘어난 금화·아이템만 줄이 되고, 줄어든 값·첫 스냅숏은 줄 0이다. 창 안의 사망만 짝지어지고 여럿이면 가장 가까운 것을 쓴다. 클리어 결과 몫은 빠진다. 선행 시험은 이 규칙을 PlayMode 가짜 peer로 화면에 뜬 글을 관측해 본다(리드 결정, 아래 「시험 층」). 구현자는 계산 타입의 EditMode 단위 시험을 더할 수 있다.
2. PlayMode 시험(가짜 peer, 실제 TCP)이 통과한다: 적 스폰 → 사망 → 금화 10·재료 1 늘어난 스냅숏이면 사망 위치 위에 두 줄 글이 생기고, 위로 움직이다가 정해진 수명 뒤 사라진다. 사망 없는 스냅숏·감소 스냅숏·첫 스냅숏은 글이 없다. 맵 이동 때 남은 글이 지워진다. 글이 떠 있는 동안 UI 포인터 판정과 키보드 공격이 그대로다.
3. 기존 EditMode 전체와 영향 PlayMode(인벤토리·HUD·적 처리 시험)가 통과한다.
4. 사용자가 볼 캡처가 근거 폴더에 있다. 1920×1080과 1280×720에서 몬스터가 죽은 자리 위에 글이 떠 있는 프레임이다.
5. 신규 독립 검증자의 강 등급 판정이 통과다.
6. 사용자 직접 확인(아래)에서 문제가 없다.
7. 사용자가 `병합 승인: PR<번호> head <40자>`를 제출한다.

## PR 경계와 검증

- PR 하나다. 65 A branch 위에 쌓고, 병합 순서는 65 A → 이 PR이다.
- 검증 등급은 강이다. 시간 창 짝짓기와 글 수명은 시간·순서·수명 위험 신호이고, 제품과 시험이 합쳐 50줄을 넘을 것으로 본다.
- 강 등급의 「실제 경로 1회」는 Unity PlayMode의 실제 TCP 가짜 peer 실행으로 채운다.
- 시험 층(리드 결정): 선행 시험은 제품 이름을 부르지 않는 블랙박스 PlayMode 시험이다. 처치 글은 씬의 활성 월드 공간 TMP 중 「+」로 시작하는 줄로 찾는다. 아직 없는 타입을 EditMode에서 부르면 시험 assembly 컴파일이 깨져 RED가 미실행이 된다(`2026-10-15-guild-client-ui` goal 348·378). 이름으로 찾는 adapter 선례(`GuildGrowthEffectTestAdapter.cs`)도 있지만, 그 방식은 제품 API 이름을 리드가 미리 고정해야 한다. 블랙박스 쪽이 구현 선택을 덜 묶고 실제 경로를 그대로 탄다. 핵심 단점은 PlayMode 실행 시간이 조금 는다는 점이다.
- 화면·에셋 요구(색 #FFEB33·#FFF0BA, 외곽선 #281400·0.22)는 시험으로 보지 않는다. 대체 실측은 두 해상도 캡처이고, 독립 검증자가 캡처와 제품 원문으로 대조한다. 글꼴 asset 이름만 시험이 본다.
- R-7 비해당이다.

## 실행 창과 사용자 손

- Unity 창: 선행 시험 30~40분, 구현·캡처 40~60분, 검증 40~50분. 단계마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」를 보낸다.
- 관문은 매번 저장 스크립트로 잰다: 물리·commit 여유 각 4 GiB, Unity 0, Windows dotnet 0.
- 서버·DB는 시험에 필요 없다. 사용자 확인 때만 리드가 Legacy 서버 하나를 띄운다.
- 캡처 2장(1920×1080·1280×720): 몬스터가 죽은 자리 위로 「+금화 10」「+재료 1」이 떠 있는 장면.
- 사용자 확인 항목:
  1. 일반 몬스터를 잡으면 그 자리 위로 「+금화 10」과 재료 한 줄이 천천히 떠올랐다가 사라진다.
  2. 떠오르는 속도가 너무 빠르거나 느리지 않다.
  3. 골렘을 잡으면 금화와 「재화 주머니」 줄이 뜬다(카탈로그 이름, `ItemCatalog.cs` :9).
  4. HUD 금화 숫자와 인벤토리는 전처럼 늘어난다.
  5. 마을 접속 직후와 맵 이동 직후에는 글이 뜨지 않는다. 던전 클리어 보상은 보상 창에만 나온다.
  6. 글이 떠 있는 동안에도 공격(왼쪽 Ctrl·Enter)·인벤토리 클릭이 그대로 된다.
  7. 사냥터에 들어오자마자 잡아 퀘스트 알림이 글 앞부분을 잠깐 가려도 보상을 받았다는 느낌이 충분하다.
  8. 실제로 때려 잡을 때 남은 HP 막대(0.8초)가 보상 줄로 헷갈리거나 읽기를 방해하지 않는다.
  9. 1280×720의 밝은 땅 위에서도 재료 이름과 개수가 편하게 읽힌다.
  - 7~9는 캡처 관찰 셋을 리드가 「느낌을 막지 않는다」로 판단해 더한 줄이다(메인 `msg_8a321c889f8b`, 아래 「독립 검증 결과」).

## 남은 우려

- 같은 순간 몬스터 둘이 죽으면 가까운 쪽 위에 뜬다. 다른 몬스터의 보상처럼 보일 수 있다.
- 처치와 같은 1.0초 안에 들어온 다른 출처의 증가(보상 분배 등)도 몬스터 위에 함께 뜰 수 있다. 던전 클리어 몫만 뺀다(「만들 것」 3).
- 파티원이 잡은 몬스터의 보상 분배 스냅숏도 그 몬스터가 내 화면에 있으면 그 위에 뜬다.
- 몬스터가 화면 밖에서 죽어 사망 패킷이 오지 않으면 글이 없다. 금화는 HUD에서만 늘어난다.
- 처치 뒤 1.0초 안에 재화 주머니를 쓰면 「+금화 50」이 몬스터 위에 뜬다. 서버 원문에서 처치 밖 금화 증가는 이 경로 하나다(선행 시험 report 요구항목 4, `InventoryRegistry.cs` 93~108). 위 「다른 출처의 증가」의 실제 사례이며, 막지 않는다(「만들 것」 3).
- 처치 글은 월드 공간 글이라 화면 UI 아래에 그려진다. 캡처에서는 사냥터 진입 때 뜨는 「QUEST AVAILABLE」 배너(1920×1080)와 퀘스트 부여 패널(1280×720)이 글 일부를 덮었다. 표지판과 겹친 곳도 있다(아래 「캡처 결과」). 고치지 않았고, 사용자 확인 7·9에서 본다.
- 처치 직후 0.5초에는 죽은 몬스터의 HP 막대(`Enemy_Normal.prefab`의 `HpBar`, 0.8초 사망 연출 동안 남음)가 두 줄 사이에 있었다. 1.0초 장에서는 사라졌다. 정체는 독립 검증자가 prefab과 코드로 확인했다. 대본 사망은 피해 없이 바로 죽여서 막대가 가득 찬 녹색이다. 사용자 확인 8에서 본다.

## 선행 시험 결과

- 작성자: 신규 `claude-opus-5-5` xhigh(대화 기록 model 값 그대로, backend unknown). task `task_718d4eb31030`, worker_done `msg_af7da954d052`. 보고서는 근거 폴더 `pretest-01/report.md`(sha256 `554743c5761fd761ee33854ab23e7828090a46da19580d3983db7a0170545051`)다. 리드 대조는 `lead/accept-pretest-01.md`와 `lead/check-pretest-unity-return-01.json`이다. 결과 XML 두 개의 건수·실패 이름·메시지가 보고서와 같고, 제품·기존 시험 blob이 바뀌지 않았다.
- 계약: v1(`pretest-01-contract-v1.md`)과 보충 01(메인 `msg_2fc417986879`의 「어떤 시험이 증가분 계산을 가르는가」, 리드 `msg_eda0126c04f7`). 증가분은 기준 금화 25·재료 2에서 두 번 처치하는 3.1 시험과 기준 25에서 클리어 50 뒤 85가 되는 3.6 시험이 가른다.
- 시험: `03_Client/Assets/Tests/PlayMode/KillRewardFloatTextSceneTests.cs`(16건)와 관측 helper `KillRewardFloatTextProbe.cs`. 처치 글은 「+」 줄이 있는 활성 월드 `TextMeshPro`로 찾는다(관측 계약 `pretest-01/observation-contract.md`, sha256 `098831dd…f143`). 음성 시험 일곱 건은 같은 시험 안의 양성 대조로 끝난다.
- Unity 창: 메인 배정 `msg_5938da1e34f9`(시한 10:15:00Z), 리드 관문 통과 뒤 전달. 마지막 Unity 끝 09:40:42Z, 반환 보고 `msg_dc26f07a7885`.
- 1회째 EditMode 전체: 1578건 모두 통과. 새 PlayMode 시험 assembly도 이 실행에서 오류 없이 컴파일됐다.
- 2회째 PlayMode 5클래스: 49건 중 실패 16. 실패는 새 시험 16건 전부이고 메시지는 「처치 글이 없다」 또는 「양성 대조에서 처치 글이 없다」다. 기존 네 클래스(`InventorySceneLifecycleTests`·`HudGoldAndPanelToggleSceneTests`·`MapEntrySceneLifecycleTests`·`DungeonClearRewardWindowSceneTests`) 33건은 통과했다. (a)~(d) 분류 대상은 0건이다.
- 아직 돌지 않은 단정: 상승·유지·흐려짐·사라짐, 맵 이동 뒤 개수, 시험 전제 두 개(새 맵 준비 1.4초 안, 증가 스냅숏 1.0초 창 안), raycast·Ctrl, 글꼴 이름이다. 처치 글이 없어 거기까지 가지 못했다. 구현 뒤 실행에서 처음 판정된다.
- 미시험: 캐릭터 전환. 클라이언트에 그 경로가 없다(grep 0건). 캐릭터 선택은 새 연결에서만 일어나므로 연결 종료 시험이 그 경계를 덮는다.
- 과정 기록(통과 차단 아님):
  - 작성자가 첫 입력 대조의 `sha256sum` 1회를 파일로 남기지 않았다고 스스로 보고했고, 저장 스크립트로 다시 재 남겼다.
  - 3.8 raycast 음성 단정은 3D TMP에 collider가 없으면 늘 참이다. 같은 시험의 「누를 때 처치 글이 떠 있다」·「C_Attack 1건 더」 단정이 판별력을 맡는다. 독립 검증 계약에 넣는다.
  - 생존 신호 18회, 최대 간격 293초(정본 helper, `lead/settle-pretest-01.json`).

## 구현 결과

- 구현자: 신규 `claude-opus-5-5` xhigh(대화 기록 model 값 291회 그대로, backend unknown). task `task_d8667182f596`, worker_done `msg_6db565386c32`(outcome failed, 원인은 아래 기존 시험 1건). 보고서는 근거 폴더 `implement-01/report.md`(sha256 `8d9a018a761b88f5334515c0f9122cef098c6ccc99e944604c2d5453a32cdf9b`)다. 리드 대조는 `lead/accept-implement-01.md`, 정산은 `lead/settle-implement-01.json`이다.
- 제품 commit `ecf8da22`: `EnemyRegistry.Despawn`이 지우기 직전 위치로 `EnemyDied`를 낸다. 계산은 일반 C# `KillRewardPairing`, 표시는 씬 소유 MonoBehaviour `KillRewardFloatText`(둘 다 `Combat/Effects/`)이고 `CombatBootstrap` installer 한 행으로 만든다. `client.md`에 `## 처치 보상 글` 절이 생겼다. 계산 타입 EditMode 시험 11건을 더했다. 상수는 짝짓기 창 1.0초, 수명 1.8초, 흐려짐 마지막 0.5초, 상승 0.9(ease-out), 시작 높이 1.6이다.
- Unity 창: 메인 배정 `msg_dd51eef0fd31`(시한 11:00:00Z), 리드 관문 10:33:15Z 통과 뒤 전달. 마지막 Unity 끝 10:42:41Z, 반환 보고 `msg_3ce5488c1b97`. drift 두 파일은 알려진 drift로 복원했다.
- 1회째 EditMode 전체: 1589건 모두 통과(기존 1578 + 새 11).
- 2회째 PlayMode 10클래스: 66건 중 65 통과·1 실패. 선행 실패 16건은 모두 통과했다. 3.2 표본에서 0.209초→1.005초 사이 0.527 올랐고, 1.392초에 alpha 0.663으로 보였으며, 1.804초에 0개가 됐다. 3.7 전제는 0.21초 < 1.4초, 0.20초 < 1.0초였다. 3.8은 raycast hit 0개, `C_Attack` 0 → 1이었다.
- 실패 1건: `InventorySceneLifecycleTests.MapTransition_RebuildsOnePanelWithTheStoredMirror_RequeriesAfterReady_AndEndingHasNone`. 483~485행이 `InventoryState.OnInventoryChanged` 구독자 형식을 `InventoryPanel`·`GuildUiController`·`HudController` 셋으로 고정했고, 「만들 것」 2의 새 구독자가 넷째로 보였다. 구현자 분류 (a), 리드 동의(`msg_2b350d347514`), 메인 처분 `msg_b50d7db4c2ab`(위). 시험이 이 단정에서 멈춰 486~497행은 돌지 않았다. 같은 형식의 구독자 집합 단정은 저장소 시험에 이것 하나다(리드 `git grep`).
- 미실행: 10클래스 밖 PlayMode, 캡처, 게임 화면, 직접 플레이, 빌드.
- 과정 기록(통과 차단 아님):
  - 리드 계약 누락(첫 발생): 선행 시험 계약 req5의 영향 grep이 `root` 아래 찾기만 보게 해서 위 구독자 집합 단정을 미리 잡지 못했다. 다음 같은 종류의 계약에는 새 구독이 생기는 이벤트의 구독자 수·형식 단정 grep을 넣는다.
  - 리드 R-3 이탈(첫 발생, 09:56Z 리드 발견): Art 리드 답 `msg_ae88de6f6d11`에 대한 회신 `msg_084d109e07ee`를 `reply --id`로 보내 subject가 자동 「Re: [Art 리드 Opus] …」가 됐다. R-3은 일반 회신을 `send --subject "[자기 태그] …"`로 보내게 한다. body 첫머리는 `[Effect 리드 Opus]`였다. 이후 일반 회신은 `send`로 보낸다(첫 적용 `msg_8e5730889d6f`). 첫 발생이라 규칙을 새로 만들지 않는다(AGENTS 「설계 우선순위」 교정 층).
  - 구현자가 스스로 적은 절차 이탈 10건: 첫 명령의 `git branch --show-current`, 빈 `node -e ""` 1회, stderr를 버린 읽기 명령, heartbeat 간격 302초 1건 등이다. 저장소 쓰기는 없고 허용 밖 쓰기 0건이다(정산과 일치).

## 캡처 결과

- 캡처 작업자: 신규 `claude-opus-5-5` xhigh(대화 기록 model 값 263회 그대로, backend unknown). task `task_0d42bb2a3304`, worker_done `msg_44a0b6f03086`(성공). 보고서는 근거 폴더 `capture-01/report.md`(sha256 `3cef77c53b29a56e36d0ec0b68cdfd761346147257844fcfa3aab7bf59692482`)다. 리드 대조는 `lead/accept-capture-01.md`와 `lead/check-capture-unity-return-01.json`, 정산은 `lead/settle-capture-01.json`이다.
- 시험: `03_Client/Assets/Tests/PlayMode/KillRewardFloatTextCaptureTests.cs` 1건이다. runner가 캡처 폴더를 정하지 않으면 Ignore로 끝나므로 평소 PlayMode 실행에서는 돌지 않는다. 제품 타입을 부르지 않고 `KillRewardFloatTextProbe`로 글을 찾는다.
- Unity 창: 메인 배정 `msg_6ffc1b3c546d`(시한 12:15:00Z), 리드 관문 11:49:41Z 통과 뒤 전달. Editor 창 모드로 1회 실행했고 재시도는 없다. 마지막 Unity 끝 11:55:09Z, 반환 보고 `msg_790a253fe958`. drift 두 파일은 알려진 drift로 복원했다. `GameViewSizes.asset`과 단축키 profile은 전후 모두 변화가 없다(메인 `msg_f11bdc50a980` 조건).
- 결과: 1건 통과·실패 0. 장면은 대본 peer의 사냥터이고, 해상도마다 새 처치 한 번에 「+금화 10」「+재료 1」 두 줄이 사망 자리 위에 떴다. 두 줄의 화면 경계는 화면 안이고, 처치 글의 빠진 글자는 0이다.
- 사용자 확인용 PNG(근거 폴더 `capture-01/captures/attempt-01/`):
  - `1920x1080-main.png` sha256 `0b68ec24167d712871e23ad21c54545aebf0e6ef75b59adc972af629226f5b05`
  - `1280x720-main.png` sha256 `ff2cd7aae60afe3df868211ea65832219a15e1a26a4e4b06d5ae082380e628aa`
  - 각 해상도의 `1.0s`·`1.5s` 장도 같은 폴더에 있다. 1.5초 장의 alpha는 0.584와 0.594다.
- 제품 관찰(고치지 않음, 위 「남은 우려」): 화면 UI 배너·패널과 표지판이 글 일부와 겹친다. 두 줄 사이에 녹색 막대가 보인다.
- 미실행: 실서버·DB, 직접 플레이, 두 해상도 밖, 색·외곽선·읽힘 판정(독립 검증자와 사용자 확인 몫).
- 과정 기록(통과 차단 아님):
  - 작업자 계약 밖 실행(첫 발생): Editor 로그 감시가 남긴 자기 고아 `tail`·`grep`을 `Stop-Process`로 껐다. 대상이 그 둘뿐인 것은 원천으로 확인했다. 다음 Editor 캡처 계약에는 로그 감시와 자기 감시 프로세스 정리를 허용 실행에 넣는다.
  - 작업자가 스스로 적은 이탈: 빈 `node -e ""` 1회(계약이 인라인 node를 막았다), 일부 읽기 명령의 출력 미저장. 처음 heartbeat 네 개의 태그 누락은 내용 없는 heartbeat라 AGENTS 수신 규칙의 예외 안이다. 생존 신호 최대 간격 307초 1건은 창 배정 대기 중이었다.

## 독립 검증 결과

- 검증자: 신규 `gpt-6-astra` xhigh(최초 실행 명령·첫 화면·rollout turn_context 모두 같은 값, backend unknown). task `task_6d0f4991c62e`, worker_done `msg_f14f95c0be4b`. 판정은 근거 폴더 `verify-01/verdict.md`(sha256 `7898c78b5302004f983e1d491cbd2dca83a2e427ed81bc33c3238a3f65fabc45`)이고 **PASS**(강 등급), 제품 결함 번호 없음이다. 리드 표본 대조는 `lead/accept-verify-01.md`와 `lead/r2-verify-sample-01.stdout.txt`, 정산은 `lead/settle-verifier-01.json`이다.
- 실행(메인 창 `msg_bfb291b2879a`, 시한 12:52:00Z 안, 마지막 Unity 종료 12:40:06Z, 반환 보고 `msg_609337207b90`):
  - EditMode 전체: 1594건 모두 통과(구현 뒤 1589 + 검증자 새 5).
  - PlayMode 영향 10클래스(실제 loopback TCP 가짜 peer): 66건 모두 통과. 구현 실행과 같은 filter다.
  - 새 PlayMode 2건 통과. 캡처 시험 1건은 캡처 변수가 없어 의도대로 Ignore다.
- 기존 시험 (a): `InventorySceneLifecycleTests.cs` 484행의 구독자 집합을 「정확히 넷」(새 `KillRewardFloatText` 포함)으로 바꿨다. `AreEquivalent` 단정은 그대로다. 같은 명령에서 65/1 → 66/0이고, 이번에 처음 돈 486~497행(Ending 구독자 0 등)도 통과했다. 새 (d)는 없다(메인 처분 `msg_b50d7db4c2ab` 조건 셋 충족).
- 새 시험(commit `e9600e1b`): 1.0초 창 경계(0.999 짝지음, 1.001 거절), 금화 감소·재료 증가가 섞인 스냅숏, 가장 가까운 사망 한 번 소비, 연결 reset 뒤 클리어 몫 버림, 골렘 줄 순서와 두 처치 글 동시 유지. 기대값은 모두 literal이다.
- 캡처 관찰 셋(메인 `msg_8a321c889f8b`): 검증자는 셋 모두 「결함 아님, 사용자 확인」으로 판정했다. 1280×720에서도 퀘스트 패널이 글 앞부분과 겹친다는 사실을 바로잡았다. 리드 판단도 같다. 화면 UI가 월드 글 위에 그려지는 순서는 의도된 것이고 첫 진입 연출은 약 1.6~2.7초 뒤 사라진다. HP 막대는 보존 대상인 0.8초 사망 연출이다. 색·외곽선은 Art 계약값이다. 셋 다 「지급된다는 느낌」과 「천천히 떠올랐다가 사라짐」을 막지 않아 사용자 확인 7~9로 남긴다.
- 과정 기록(기록만, 메인 `msg_2ad9c188b1d8` 기준 리드 적용):
  - 메모 전 저장하지 않은 읽기 조회(리드 답 `msg_91cd4716bb5b`).
  - 생존 신호 300초 초과 1건(정본 helper 333초, heartbeat만 세면 381초, 리드 답 `msg_4901ce8da4de`).
- 정산: rollout 호출 57회 중 git 쓰기·ORCA 전체 조회 0. worker-release는 retained/external_terminal이고, pane은 대기 상태를 보고 닫았다.

## 다음 계획 후보

- 선행 시험이 `DungeonClearRewardWindowSceneTests`의 private helper(`AfterEarlierPacketsApply`·`EnterReadyTown`)를 복제했다. 기존 시험 쓰기 금지 때문이다. `MapEntryPlayFixture`로 올리는 일은 후속 후보다. 검증자의 새 PlayMode 시험도 작은 입력 helper를 파일 안에 두었다(판정 설계 관찰 O1). 같은 후보에서 함께 본다. BACKLOG `dungeon-clear-test-helpers-shared`.
- 사용자 플레이 창의 Editor 기동 기록 사고가 두 번째로 났다(아래 「사용자 직접 플레이 창」). 종료 Gardener 정리 후보 1이다. 근거 폴더 스크립트 복사를 없애는 저장소 플레이 창 helper와 node 시험을 두고, 자기 실행 시간 상한을 필수로 둔다. BACKLOG `verification-harness-powershell-helper`에 덧붙였다.
- 계약 허용 실행 목록 밖 명령이 세 goal에서 10건 반복됐다. 종료 Gardener 정리 후보 2이고, 집계 정본 helper와 fixture 시험을 제안했다. BACKLOG `contract-exec-allowlist-audit`.

## 결과

- PR283 병합: 2026-10-31T08:34:08Z, head `2fd74253d894dfd7d1f4bc66ea6e7fb2f6e60945`, 병합 commit `f5adcf31e605af3b30581746c20a64642bb144fc`. 메인이 새 head를 R-2로 대조한 뒤 사용자 승인 줄을 받아 병합했다(메인 `msg_ea34b7ba5963`·`msg_84d148dc5236` 1). 기록은 `lead/closeout-01/pr-merged-01.json`, CI는 `lead/closeout-01/pr283-checks-final-01.json`이다.
- 완료조건 대조:

| 조건 | 결과 | 근거 |
|---|---|---|
| 1 계산·짝짓기 규칙 시험 | 충족 | [독립 검증 결과](#독립-검증-결과) |
| 2 PlayMode 가짜 peer 시험 | 충족. 66건 모두 통과 | 같은 절 |
| 3 기존 EditMode 전체와 영향 PlayMode | 충족. 기존 시험 (a) 1건은 메인 처분대로 검증자가 고쳤다 | 같은 절 |
| 4 사용자가 볼 캡처 | 충족. 1920×1080·1280×720 | [캡처 결과](#캡처-결과) |
| 5 강 등급 PASS | 충족. main 통합 뒤에도 판정을 유지했다(아래) | 같은 절, 아래 main 통합 |
| 6 사용자 직접 확인 | 충족. 병합 뒤 플레이에서 「플레이에는 문제가 없음」·「나머지는 괜찮음」(결정 78 C). 병합은 플레이보다 먼저 했다 | 아래 [사용자 직접 플레이 창](#사용자-직접-플레이-창--2026-10-31결정-77-a) |
| 7 병합 승인 | 충족 | 위 병합 기록 |

- 생존 신호: 종료 때 리드가 이 Run의 저장 우편함 원시 전체를 정본 helper로 다시 쟀다(`lead/closeout-01/liveness-select-01.json`). 네 단계 값은 위 단계별 기록과 같다. 작성자는 최대 293초로 초과가 없다. 300초 초과는 셋이고 모두 2026-10-30 UTC다.
  - 구현자 10:22:07Z→10:27:09Z 302초(heartbeat→heartbeat): 대기 시한을 길게 잡아 넘었고, 구현자가 절차 이탈 9로 적었다. 뒤로는 240초로 줄였다(`implement-01/report.md` 233행).
  - 캡처 작업자 11:23:42Z→11:28:49Z 307초(heartbeat→heartbeat): Unity 창 배정 대기 중이었다(`lead/accept-capture-01.md` 35행).
  - 검증자 12:33:17Z→12:38:50Z 333초(status→heartbeat): 문맥 압축 뒤 실행 확인이 늦었다. 검증자가 자기 이탈로 적었다(`verify-01/verdict.md` 180행, heartbeat만 세면 381초).
### main 통합 — 2026-10-31

- 계기: 메인 `msg_20097f42c3e3` 2. 앞 PR이 병합된 뒤 PR283은 DIRTY·CONFLICTING이었다(base main, head `fef2f850`). 메인은 받은 승인 줄을 head가 바뀔 것이라 쓰지 않았다.
- 한 일: 최신 main `dd9e755b`를 통합한 commit 하나 `2fd74253d894dfd7d1f4bc66ea6e7fb2f6e60945`(부모 `fef2f850`·`dd9e755b`). 충돌은 `client.md` 하나다. PR285의 「길드 창 배치와 밝은 스킨」 절은 main 그대로 두고, 그 뒤에 이 goal의 「처치 보상 글」 절을 글자 그대로 붙였다. `CURRENT.md`는 자동 통합됐고 main과 다른 줄은 Effect 줄 하나다.
- 확인(근거 폴더 `lead/integrate-main-01.md`):
  - 122경로를 두 부모와 비교했다. 이 PR만 바꾼 20경로는 PR283 head blob, main만 바꾼 100경로는 main blob이다. 양쪽이 바꾼 것은 문서 두 파일뿐이다(`lead/check-merge-blobs-01.json`).
  - remerge-diff는 `client.md` 하나다(`lead/remerge-diff-01.txt`). 새 head 대 main은 22파일 +2132/−2로 원래와 같다. 21파일은 patch-id가 같고, `client.md`는 더한 4줄의 sha256이 같다(`lead/patch-id-per-file-01.txt`).
  - 새 head CI 4개(code-rules·module-boundaries·architecture-tests·dotnet-tests) 모두 SUCCESS다(`lead/ci-final-01.json`).
- 판정: main 통합과 문서 충돌 해소뿐이라 병합 관문대로 기존 독립 판정(PASS)을 유지했다. 메인이 새 head를 R-2로 대조해 같다고 했다(`msg_ea34b7ba5963` 1). 새 head에서 Unity 시험은 다시 돌리지 않았다.
- 절차 이탈(리드 자기 신고, 메인 `msg_ea34b7ba5963` 5): `client.md` 충돌 해소는 저장소 파일 쓰기인데, 맥락 메모 갱신보다 먼저 했다. 메모는 그 뒤 `lead/closeout-01/closeout-context-01.md`에 썼다. BACKLOG `lead-direct-write-context-memo`의 네 번째 관측으로 덧붙였다.

### 사용자 직접 플레이 창 — 2026-10-31(결정 77 A)

- 메인 배정(`msg_84d148dc5236` 3): effect-active에 서버 1개와 Unity Editor 1개를 연다. 다른 worktree의 Unity는 이 창 동안 쓰지 않는다. 플레이는 병합된 main으로 하고 65 A(왼쪽 Ctrl)와 이 goal을 함께 본다.
- 플레이 전 상태(`lead/play-01/play-01-preserve.json`, 08:38:26Z):
  - HEAD `f5adcf31`(main)이고 미커밋은 문서 네 파일뿐이다. 알려진 drift 두 파일은 HEAD와 같다. 사용자 설정 두 곳은 GameViewSizes.asset 없음, Shortcuts 폴더 파일 0으로 남겼다.
  - 관문은 Unity 0, Windows dotnet 0, 7777 수신 대기 없음, 물리 여유 17.12 GiB, commit 여유 26.56 GiB였다.
  - 스크립트의 미커밋 검사 하나는 `trim` 결함으로 false가 나왔다. 실제 값은 문서뿐이다(`play-01-preserve-note.md`).
- 서버: Orca 터미널 `term_cbebc49e-9815-4bb6-b7af-909f8f8000b7`에서 46.2초 만에 「GameServer running on :7777 (PID 1156)」이 나왔다(`play-01-server.json`).
- Editor: `Unity.exe -projectPath effect-active/03_Client`를 GUI로 열었다. batchmode·장면 자동 열기·자동 Play 인자는 없다. 본체 PID 7404, 판본 6000.6.4f1(12bfff696524)이다(`play-01-launch-state-01.json`).
- 메모리 사고(같은 원인의 두 번째 발생):
  - Editor를 띄운 PowerShell(PID 19300, `play-01-editor.ps1`)이 끝나지 않고 메모리를 잡았다.
  - 측정 둘: 리드가 08:48:40Z에 작업 집합 8.71 GiB·물리 여유 1.92 GiB로 쟀다(`play-01-launch-state-01.json`). 메인이 끄기 직전에 10.03 GiB·물리 여유 0.78 GiB로 쟀다(`msg_476db413d3d1` 1).
  - 리드의 `taskkill`은 Claude 자동 모드 분류기가 거부했다. 사용자가 메인 창에서 `! taskkill //PID 19300 //F`로 직접 끝냈다. 메인은 08:52:43Z에 19300 없음, Editor 7404 살아 있음, 물리 여유 12.81 GiB로 쟀다(`msg_476db413d3d1` 1). 리드의 08:52:52Z 기록에서도 19300은 없었고 물리 여유는 12.70 GiB였다(`play-01-launch-state-02.json`).
  - 원인: 앞 goal의 Editor 스크립트를 결함째 출력 경로만 바꿔 옮겼다. 그 스크립트는 `Get-Content`로 읽은 판본 줄을 `ConvertTo-Json -Depth 8`에 그대로 넣는다. PS5.1에서는 그 줄에 붙은 속성 때문에 객체 그래프가 끝없이 펼쳐진다고 본다. 이 펼침은 리드 추론이고, 앞 goal과 마찬가지로 재현하지 않았다. 같은 스크립트가 같은 단계에서 멈췄다는 점에서 [앞 goal의 메모리 사고](../2026-10-29-knight-dash-effect-sheet/goal.md#사용자-플레이-확인--2026-10-30)와 같은 원인이다.
  - 두 번째 발생이라 반복 규칙 후보다(메인 `msg_476db413d3d1` 3, 채택은 사용자 결정). 판본 줄을 `[string]`으로 바꾼 2판 `play-01-editor-02.ps1`을 만들었고 실행하지 않았다. 가장 높은 교정 층 제안은 `lead/closeout-01/correction-proposal-editor-record-01.md`에 써서 종료 Gardener 입력에 넣었다.
- 서버 교체(결정 81 A, 메인 `msg_3412074211cc`): 사용자가 새 캐릭터 선택창을 보려고 Legacy 서버 탭을 직접 Ctrl+C로 끄고, core-active의 DB 서버가 7777을 맡았다. 리드는 Legacy를 다시 켜지 않았고 Editor 7404는 그대로 두었다. 결정 80 A로 개발 DB에 시험 계정·캐릭터 행을 만드는 것이 허용됐다. 플레이 화면의 시험 캐릭터는 10-29에 만든 「시험새벽1029」다(메인 `msg_a2b3903508fb`).
- 플레이 결과(결정 78 C, 메인 전달 `msg_a2b3903508fb`): 사용자 원문은 「대시보드 결정 응답: 1) 78 - 병합된 GUI 묶음 직접 플레이(카드 20줄) → C 코멘트 「플레이에는 문제가 없음, 근데 캐릭터 선택창에 빨간 박스 부분으로 내가 강조한 부분이 쓸데없이 공간을 많이 차지함, 캐릭터 선택 슬롯을 4개로 하고, 빈 캐릭터칸 선택시 새로운 캐릭터 생성 UI로 진입하게 수정, 나머지는 괜찮음 [Image #2]」」이다.
  - 이 goal과 65 A(PR282·PR283)는 「나머지는 괜찮음」으로 문제가 없다(메인 `msg_a2b3903508fb` 1).
  - 캐릭터 선택창 의견(오른쪽 설명 상자가 넓음, 슬롯 4개, 빈 칸 선택 시 생성 화면)은 두 goal 범위 밖이다. 리드는 처리하지 않았다.
- 끝(결정 84 A, 메인 전달 `msg_36f268442791`): 사용자 원문은 「대시보드 결정 응답: 1) 84 - 플레이 끝: DB 서버 끄기(Enter)와 Editor 닫기(네 손) → A 둘 다 끝 · …」이다. 메인이 PID 7404 없음·Unity 0을 확인했다. 리드 확인 루프는 10:11:51Z에 Unity 0을 보았다.
- 복원(`lead/play-01/play-01-restore.json`, 10:12:04Z, Unity 0 확인 뒤):
  - 바뀐 파일은 `MinimapRT.renderTexture` 하나였고, 플레이 전 사본(HEAD와 같은 bytes)으로 되돌렸다. `ProjectSettings.asset`은 바뀌지 않았다. 알 수 없는 변경은 0이다.
  - 사용자 설정 두 곳(GameViewSizes.asset은 플레이 전에도 없음, Shortcuts 폴더는 파일 0)은 플레이 전과 같다. 되돌린 것은 없다.
  - 복원 뒤 `git status`는 종료 기록 문서 다섯 파일뿐이다(`lead/play-01/git-status-after-restore-01.txt`). 그때 Windows dotnet 0, 7777 수신 대기 없음, 물리 여유 17.38 GiB였다.
  - Legacy 서버 탭은 사용자 Ctrl+C 뒤 「Server task finished with exit code 130.」으로 빈 프롬프트였다. 다시 켜지 않고 닫았다(`server-terminal-close-01.json`).
  - 메인에 알렸다(`msg_c3a81be9ec02`). Unity 창은 메인이 Art에 넘긴다.
- 절차 이탈(리드 자기 신고):
  - 10:12Z쯤 터미널 목록을 저장하는 명령 앞에 빈 `node -e ""`를 한 번 붙였다. 메인이 하지 말라고 지시한 것이다. 출력과 결과에는 영향이 없다(`msg_c3a81be9ec02`). 종료 기록 중 10:58Z쯤 fetch·점검 명령 끝에 한 번 더 붙였다. 11:01Z쯤에는 점검 결과 JSON을 요약해 보는 데 인라인 `node -p`를 한 번 썼다(읽기만). 둘 다 출력과 결과에는 영향이 없고, 종료 기록 PR 준비 보고에서 메인에 신고했다.
  - 이 플레이 창의 전 상태 저장·서버·Editor·복원은 맥락 메모 없이 했다. 계획은 각 스크립트 머리 주석과 메인 지시에만 있었다. 그중 저장소 추적 파일 쓰기는 `MinimapRT.renderTexture` 복원 하나다. 메모는 그 뒤 10:14:32Z에 `lead/closeout-01/closeout-context-01-addendum-01.md`로 보충했다. BACKLOG `lead-direct-write-context-memo`의 다섯 번째 관측으로 덧붙였다(종료 Gardener GR-06).

### 종료 Gardener — 2026-10-31(결정 76 A)

- 결정 76 A에 따라 세 goal(65 A·개선안 3·실패 23건)에 Gardener 하나, 보고서 한 파일을 두었다. **정본(goal마다 Gardener)과 다른, 이번 한 번의 사용자 결정 예외다**(사용자 원문은 위 [76 A 절](#10-31-결정-76-a77-a--종료-gardener-하나로-묶음-pr283-병합-뒤-바로-플레이--msg_84d148dc5236), 메인 전달 `msg_84d148dc5236`). 보고서 맨 앞에도 같은 문단이 있다.
- 보고서: [gardener.md](gardener.md). 근거 폴더 원본 `lead/closeout-01/gardener/context-gardener-report.md`(sha256 `a93523ef267c4e7d0ae7c79e239b8fd3cbb3716a49d7acb5eddac10e5514aadf`, 59,844 B)를 bytes 그대로 옮겼다(`lead/closeout-01/gardener-settle-01.json`). worker_done은 `msg_002fdea7597b`(10:50:40Z)다.
- 계약·기동: 계약 `lead/closeout-01/gardener-contract-v1.md`, 고정 입력 v2 108파일, 기동 기록 `gardener-launch-record-01.md`. pane `term_feb9fa3b-7e28-4de4-9676-d564a39c34d2`, 첫 화면 「Opus 5.5 with xhigh effort」. 대화 기록의 model 값은 `claude-opus-5-5` 295회다. 쓰기는 보고서 한 파일뿐이다. 정산 정규식이 잡은 쓰기형 git 1건은 `git merge-base` 읽기였다.
- 결론: 차단 관찰 0, 리드 반환 6, 정리 후보 2. 세 goal의 판정(독립 검증 PASS 셋, 결정 78 C)을 바꿀 근거는 없었다. 후보 도착 검사는 65 A·개선안 3 goal이 exit 0, 실패 23건 goal이 exit 1이었다(GR-01).
- 리드 반환 처분:

| 번호 | 내용 | 처분 |
|---|---|---|
| GR-01 | 실패 23건 goal 「다음 계획 후보」 둘째 줄에 BACKLOG ID·goal 링크가 없다 | Content goal 링크를 넣고, Content가 쓰는 중인 행 이름을 적었다 |
| GR-02 | 플레이 전 상태의 「사용자 설정 sha256을 남겼다」가 원시와 다르다(파일 없음·0개) | 「GameViewSizes.asset 없음, Shortcuts 폴더 파일 0」으로 고쳤다 |
| GR-03 | 「08:52:43Z 전에 사라졌다」의 원시가 리드 근거에 없다 | 08:52:43Z는 메인 실측(`msg_476db413d3d1` 1)으로, 리드 기록은 08:52:52Z로 나눠 적었다 |
| GR-04 | 시험 캐릭터 이름의 원천 msg가 틀렸다 | 원천 `msg_a2b3903508fb`와 「10-29에 만든」을 달았다 |
| GR-05 | 300초 초과 8구간에 구간·이유가 대부분 없다 | 세 goal 「결과」의 생존 신호 줄에 구간마다 시각과 이유를 적었다. 65 A 작성자 두 구간의 이유는 보고서 시각으로 본 것이라고 밝혔다 |
| GR-06 | 플레이 창의 맥락 메모 부재와 `MinimapRT` 복원 쓰기가 goal·BACKLOG에 없다 | 위 플레이 창 절차 이탈에 적고 BACKLOG `lead-direct-write-context-memo`에 다섯 번째 관측으로 덧붙였다 |

- 정리 후보: 후보 1(Editor 기동 기록 사고)은 BACKLOG `verification-harness-powershell-helper`에 덧붙였다. 후보 2(계약 허용 목록 밖 명령)는 맞는 기존 행이 없어 새 행 BACKLOG `contract-exec-allowlist-audit`로 두었다. 둘 다 제안의 이관이며 채택이 아니다. 채택은 메인을 거쳐 사용자가 정한다.
- 기존 행에 덧붙인 관측(보고서 「첫 발생·기존 후보 연결」): 단계 정산 때 생존 신호 helper 미실행의 세 번째 발생은 BACKLOG `heartbeat-interval-review`에, 리드 R-3 회신 이탈의 세 번째 관측은 BACKLOG `orca-general-reply-tag-guard`에 적었다. 나머지 연결은 보고서에만 둔다.
- 설계 관찰(비차단, 리드 처분):
  - O1 PR286의 `DEVELOPMENT.md` 실행 전제 표가 23건만 담는데 범위를 말하지 않는다. 이 종료 기록 PR에서는 고치지 않는다. 다음에 그 표를 고치는 goal이 범위 한 문장이나 캡처 시험 행을 더한다.
  - O2 연속 병합에서 「base 바뀐 뒤 병합 전 확인」이 빠지는 구조. 병합 관문은 메인 몫이라 메인에 알린다.
  - O3 GR-01을 goal 링크로 고쳐서, 후보 도착 검사는 원본 위치에서 다시 돌렸다. 세 goal 모두 exit 0이다(`lead/closeout-01/check-closeout-01.json`, 10:58:04Z).
  - O4 공격 키 goal이 다른 goal 근거 폴더의 원시를 든 것. 틀리지 않아 그대로 두고, 다음 종료부터 한 폴더에 모은다.
  - O5 Gardener 계약의 파일 보기 문장. 후보 2 행의 보조 항목에 넣었다.
- Gardener 자신의 이탈: 계약 허용 목록 밖 읽기 전용 셸 명령 10항목을 스스로 적었다(보고서 「자기 쓰기 감사」). 생존 신호는 15회, 300초 초과 1구간 10:39:04Z→10:46:39Z 455초(heartbeat→heartbeat, `lead/closeout-01/gardener-settle-01.json`)다. 이유는 확인하지 않았다.
