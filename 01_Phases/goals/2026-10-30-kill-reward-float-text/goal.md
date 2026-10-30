# 몬스터 처치 때 「+금화 N」 떠오르는 글

## 재개 지점

**다음 첫 일:** 리드가 캡처 계약을 쓰고 캡처 작업자(신규 Opus)를 연다. 캡처 Unity 창은 Editor 하나만으로, 예상 시간과 함께 메인에 따로 요청한다. 그 뒤 독립 검증자를 연다. 기존 시험 1건 (a)의 수정과 전후 재실행은 검증자가 맡는다(아래 「구현 결과」).

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1(`lead/scope-draft-v1.md`, sha256 `e8a69526…`)을 메인이 사용자 원문과 대조해 통과시켰다(메인 `msg_22555c3ffe7a`). 사용자 중간 승인은 없다(메인 운영 방식). |
| 리드 | `[Effect 리드 Opus]`(claude-opus-5-5 xhigh). Run `run_5f3209972e68`, 회신 주소 `run:run_5f3209972e68`. |
| branch | `feat/kill-reward-float-text-20261030`. 65 A branch `feat/attack-key-left-ctrl-20261030`의 PR282 head `70ab4f19` 위에 쌓았다. 두 PR이 `client.md`와 `CURRENT.md` Effect 줄을 함께 고치기 때문이다. PR base는 65 A branch이고, 앞 PR이 병합되면 차례로 바뀐다. 작업 공간 effect-active. |
| 근거 폴더 | `.backups/verification/2026-10-30-kill-reward-float-text/`(Git 제외). 리드 원시는 `lead/`다. |
| 실행 창 | Unity는 effect-active `03_Client`만 쓴다. 시험 실행은 batchmode, 캡처는 Editor 창 하나다. 창은 메인 배정이다. |

## 진척 단계

- [x] goal과 계약 기록
- [x] 선행 시험과 실패 원시
- [x] 구현과 통과 원시(기존 시험 1건 (a)는 독립 검증에서 고친다)
- [>] 캡처
- [ ] 독립 검증 통과
- [ ] PR 생성과 확인 항목
- [ ] 제품 PR 병합
- [ ] 결과 기록과 종료

## 적용 중인 사용자 결정

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

## 남은 우려

- 같은 순간 몬스터 둘이 죽으면 가까운 쪽 위에 뜬다. 다른 몬스터의 보상처럼 보일 수 있다.
- 처치와 같은 1.0초 안에 들어온 다른 출처의 증가(보상 분배 등)도 몬스터 위에 함께 뜰 수 있다. 던전 클리어 몫만 뺀다(「만들 것」 3).
- 파티원이 잡은 몬스터의 보상 분배 스냅숏도 그 몬스터가 내 화면에 있으면 그 위에 뜬다.
- 몬스터가 화면 밖에서 죽어 사망 패킷이 오지 않으면 글이 없다. 금화는 HUD에서만 늘어난다.
- 처치 뒤 1.0초 안에 재화 주머니를 쓰면 「+금화 50」이 몬스터 위에 뜬다. 서버 원문에서 처치 밖 금화 증가는 이 경로 하나다(선행 시험 report 요구항목 4, `InventoryRegistry.cs` 93~108). 위 「다른 출처의 증가」의 실제 사례이며, 막지 않는다(「만들 것」 3).

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

## 다음 계획 후보

- 선행 시험이 `DungeonClearRewardWindowSceneTests`의 private helper(`AfterEarlierPacketsApply`·`EnterReadyTown`)를 복제했다. 기존 시험 쓰기 금지 때문이다. `MapEntryPlayFixture`로 올리는 일은 후속 후보다.

## 결과

아직 없다.
