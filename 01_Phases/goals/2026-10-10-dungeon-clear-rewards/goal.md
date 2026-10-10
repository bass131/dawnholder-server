# 던전 클리어·보상과 눈에 띄는 결함 둘

상태: **범위 승인(2026-10-10). 결함 PR의 구현과 독립 검증(통과, 제품 결함 0)이 끝났고 R9 실화면이 남았다. 이어갈 곳은 [재개 지점](#재개-지점)이다. 다음 goal은 자동으로 시작하지 않는다.**

- 담당: Content 리드(`[Content 리드 Opus]`). 시작 기준 `origin/main` = `cc20d428f7988fdcb232a7c811cf2e729446abdc`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`. 결함 PR branch `fix/hud-gold-inventory-panel-20261010`(base `cc20d428`).
- 착수 원천: 메인 `msg_9fe1d49e00bb`(2026-10-10T07:39:37Z)가 전달한 사용자 승인. 원문은 아래 [적용 중인 사용자 결정](#적용-중인-사용자-결정)에 있다. 범위 원문은 리드 초안 `msg_f1bba119c853`이다.
- 로컬 원문·계약·판정은 `.backups/verification/2026-10-10-dungeon-clear-rewards/`에 둔다(Git 밖).

## 범위

파티가 보스방에서 보스를 잡으면 「던전 클리어」로 보고 그 보스방 안의 전원에게 보상을 주고, 보상 창의 「확인」으로 마을에 돌려보낸다. 인스턴스 맵이 들어오기 전에는 눈에 띄는 결함 둘과 던전 패킷·보상 창을 먼저 끝낸다.

| 항목 | 내용 |
|---|---|
| 만들 것 | (결함) HUD 골드를 서버 재화에 연결한다. 인벤토리 패널을 「I」 키로 열고 닫는다. (던전) 보스방 복사본의 클리어 정의와 보상표, 클리어 결과 패킷과 확인 요청 패킷(39·40번), 클라이언트 보상 창과 「확인」 전송, 사냥터 포탈의 던전 입구 표시, 서버 클리어 판정과 보상 원자 지급, 보스 처치자 보상을 클리어 보상으로 합치기, 「확인」 뒤 마을 복귀 요청, 봇 두 파티 시나리오, 기능 문서. |
| 건드릴 곳 | (결함) 클라이언트 `UI/HudController.cs`, `UI/InventoryPanel.cs`·`UI/InventoryPanelView.cs`, 입력 연결, EditMode 시험. (던전 패킷·창) PDL·생성 패킷·프로토콜 버전, Shared GameData의 던전 보상표(새 폴더), 클라이언트 새 보상 창·수신 핸들러. (던전 서버) 서버 새 던전 폴더, `Items/KillRewardPolicy.cs`·`Items/InventoryRegistry.cs`의 보상 연결, `GameMap` 보스 사망 분기 안쪽과 `GameWorld` 처치 콜백(World 인스턴스 맵 PR 병합 뒤), `HandlerRegistry` 등록 한 줄, 새 핸들러, 서버 시험, 봇. 마을 복귀는 World의 「서버가 시키는 이동」 진입점을 부르기만 한다. 이 goal.md, CURRENT의 Content 줄, FEATURE_MAP·영역 문서의 해당 줄. |
| 하지 않을 것 | 새 맵 번호·맵 데이터·포탈 줄. 인스턴스 생성·입장·정리, 맵 종류 표, 맵 이동, 보스 처치 때 퀘스트 초기화 범위(모두 World). 새 아트·보스 패턴. 길드·거점. DB 저장. 난이도·매칭·재입장 제한·시간제한·자동 퇴장. 기존 보스방 → 엔딩 → 마을 길의 제거. 다른 대기 후보(`inventory-ui-readability-followup`, `inventory-label-ellipsis-glyph`, `hud-pointer-attack-ux` 등). Unity scene·prefab 저작은 필요하면 메인에 요청한다. |
| 관찰 가능한 완료조건 | 아래 여덟 개다. |

1. HUD 골드가 서버 재화와 같은 값을 보이고, 첫 재화를 받기 전에는 0 대신 빈 값을 보인다. 실화면에서 HUD와 패널의 재화가 서로 다른 값으로 보이는 순간이 없다.
2. 「I」 키로 패널이 열리고 닫힌다. 게임 화면 진입 때 닫혀 있고, 같은 연결의 맵 이동 뒤에도 열림·닫힘이 유지된다. 닫힌 동안 패널은 클릭과 공격 입력을 막지 않는다.
3. 실화면 세 크기(709×399, 802×451, 1920×1080)의 마을·사냥터·보상 시점에서 HUD와 패널의 겹침이 0이다. 측정은 이전 goal의 PR191 실화면 검증과 같다.
4. 봇 두 파티가 각자 다른 보스방 복사본에서 보스를 잡는다.
5. 보스 사망 틱에 그 보스방 복사본 안에 있던 사람 전원이 클리어 보상을 정확히 한 번 받는다. 들어간 뒤 파티를 나갔어도 복사본 안이면 받는다.
6. 서버 시험이 다섯 가지를 증명한다. 같은 클리어 보상이 두 번 나가지 않는다. 클리어 뒤 들어온 사람은 받지 않는다. 사냥터 복사본에 남은 파티원은 받지 않는다. 가방이 가득 차거나 재화가 상한이면 그 사람의 보상 전체를 거부하고 거부 결과를 보낸다. 보스 처치자에게 따로 가던 보상은 나가지 않는다.
7. 실제 클라이언트에서 마을 → 사냥터 → 보스방 → 보스 처치 → 보상 창 → 인벤토리 표시 → 「확인」 → 마을 복귀를 확인한다.
8. World 인스턴스 맵 위에서 기존 전투·파티·퀘스트·일반 처치 보상이 그대로 동작하고, 보스방 → 엔딩 → 마을 길도 남는다. 게임 틱 안에서 I/O를 기다리지 않는다.

화면 겹침과 실제 플레이(완료조건 3·7)는 EditMode 시험으로 고정할 수 없다. Canvas 배치·실제 입력 장치·서버 연결이 함께 필요하기 때문이다. 이 둘은 실화면 실측으로 대신한다.

### PR 경계와 점검

| PR | 내용 | 시작 조건 | 끝나는 조건 |
|---|---|---|---|
| 결함 PR | 완료조건 1~3. 클라이언트만 바꾼다. 이 goal.md와 CURRENT의 Content 줄도 넣는다. | 범위 승인 | 독립 검증·실화면 확인 뒤 사용자 병합 승인 |
| 던전 패킷·창 PR | 39번 클리어 결과와 40번 확인 요청 패킷, 프로토콜 버전 17 → 18, 던전 보상표, 클라이언트 보상 창·핸들러·입구 표시. 서버는 이 PR에서 새 패킷을 보내지도 처리하지도 않는다. 등록되지 않은 요청은 지금처럼 버린다(`Sessions/GameSession.cs:184`). | 결함 PR의 Unity 작업 뒤(같은 checkout) | 새 버전의 서버·봇 접속과 기존 봇 회귀, Unity 컴파일·EditMode 시험. 실화면은 다음 PR에서 한다 |
| 던전 클리어 PR | 완료조건 4~8. 서버 클리어 판정·보상·마을 복귀 요청 처리, 보스 처치자 보상 합치기, 봇 두 파티, 실제 플레이 | World 인스턴스 맵 PR 병합. 규칙 단위 설계·시험은 그 전에 시작한다 | 독립 검증·실제 플레이 뒤 사용자 병합 승인 |

각 PR은 신규 독립 검증, 정확한 head의 CI와 원시 실행 근거를 메인에 보고한 뒤 **그 PR 병합 직전 사용자 명시 승인**을 받는다. 자동 병합하지 않는다. 중간 점검은 던전 패킷·창 PR 병합 때 인스턴스 맵 진행과 일정을 함께 본다. 종료 점검은 전체 PR 병합·결과 기록·Gardener 뒤다. 같은 산출물 수정이 3회를 넘으면 메인에 체크포인트를 알린다. 확정 실패 집계는 [ORCA 실패 정본](../../../00_Document/operations/ORCA.md#confirmed-failures)을 따른다.

| 시점 | 할 일 |
|---|---|
| 인스턴스 맵이 main에 들어오기 전 | 결함 PR 전부. 던전 패킷·창 PR 전부. World 리드와 기술 계약 확정. 던전 클리어 PR의 설계 절 확정, 맵 하나 위의 클리어 규칙 단위 시험과 규칙 단위 구현(새 던전 폴더 안, World·Core 파일은 건드리지 않음). |
| 들어온 뒤에만 | 보스 사망 분기와 처치 콜백 연결, 보스방 복사본 플레이어 목록 사용, 「확인」 → 마을 복귀, `KillRewardPolicy`·`InventoryRegistry` 수정, 봇 두 파티, 실제 플레이. |

## 설계

### 결함 PR의 요구

HUD와 패널은 서버 상태를 표시만 한다. 재화를 클라이언트에서 계산하지 않는다. 요구 번호는 선행 시험과 판정이 같은 번호를 쓰도록 여기서 고정한다.

| 번호 | 요구 | 확인 방법 |
|---|---|---|
| R1 | HUD 골드는 연결의 인벤토리 미러(`State/InventoryState.cs`)의 재화를 보인다. 미러가 바뀌었다고 통지하면 같은 통지 안에서 새 값으로 바뀐다. | EditMode 시험 |
| R2 | 미러에 snapshot이 없으면(첫 수신 전, 연결 종료 정리 뒤) 숫자 0 대신 빈 값을 보인다. 초기 직렬화 값(`_mockGold`)을 서버 값처럼 보이지 않는다. | EditMode 시험 |
| R3 | HUD는 실제로 구독한 미러에서 해제한다. 미러가 교체되거나 늦게 생겨도 현재 미러를 따르고, 옛 미러의 통지는 HUD를 바꾸지 않는다. disable·destroy 뒤 통지도 HUD를 바꾸지 않는다. | EditMode 시험 |
| R4 | 게임플레이 씬에서 「I」 키를 누르면 패널이 열리고, 다시 누르면 닫힌다. | EditMode 시험(입력 장치 대역) |
| R5 | 연결 뒤 첫 게임플레이 씬에서 패널은 닫혀 있다. 같은 연결의 맵 이동(씬 교체로 패널을 새로 만듦) 뒤에도 마지막 열림·닫힘을 따른다. 연결이 끝나고 새로 들어오면 다시 닫힘으로 시작한다. | EditMode 시험 |
| R6 | 닫힌 동안 패널은 보이지 않고 raycast를 받지 않는다. 그래서 클릭과 공격 입력을 막지 않는다. | EditMode 시험 |
| R7 | 일시정지 중이나 씬 전환 중에는 「I」 입력이 열림·닫힘을 바꾸지 않는다. 기존의 「일시정지·씬 전환 중 입력 차단」과 같다. | EditMode 시험 |
| R8 | 닫힌 동안에도 미러 구독과 재조회는 그대로다. 열면 그 순간의 최신 snapshot을 보인다. 열린 동안의 표시·사용·새로고침·재조회 동작은 바뀌지 않는다. | EditMode 시험과 기존 시험 회귀 |
| R9 | 실화면 세 크기에서 HUD와 패널의 겹침 0, HUD 골드와 패널 재화 일치, 「I」 키 열고 닫기, 패널이 닫힌 동안 퀘스트 알림과의 가림 0. | 실화면 실측 |

보존 동작: HUD HP·MP 표시와 세션 바인딩, 패널의 기존 표시·사용 버튼·재조회·버튼만 raycast 받는 정책, 메뉴·캐릭터 선택·Ending에는 패널이 없음, 기존 공격 입력과 포인터 raycast 차단 경로. 근거는 [클라이언트 영역 계약](../../../00_Document/domains/client.md#인벤토리-표시와-요청-수명)이다.

던전 패킷·창 PR과 클리어 PR의 설계는 각 PR 착수 때 이 절에 더한다.

### 파트 간 소유와 계약

| 대상 | 소유와 합의 |
|---|---|
| 인스턴스 맵(사냥터·보스방 복사본), 맵 종류 표, 맵 이동, 보스 처치 때 퀘스트 초기화 범위, 「서버가 시키는 이동」 진입점 | World(`[World 리드 Opus]`, 회신 주소 `run:run_c21dddd08312`). 10-08 Core 계획 v2를 World가 이어받았다(메인 `msg_6412194a47fa`). |
| 입구 | 기존 마을 → 사냥터 포탈이다. Content가 맵 종류 표·포탈 표에 더할 줄은 없다(World `msg_6e96d918962c` 1). |
| 클리어 대상 | 보스 사망 틱의 그 보스방 복사본 플레이어 목록. 파티원 재확인은 하지 않는다(같은 메시지 2). 열쇠는 `GameMap.InstanceKey`(`InstanceKey?`)다. 공용 맵은 null이고 복사본은 값이 있다. `InstanceKey`는 `Owner`(Party/Solo)와 `Id`를 가진 값 비교 record struct이며, 복사본에 들어갈 때 정해져 그 안에서 파티를 나가도 바뀌지 않는다(World `msg_14c7e14412f3` 1, World 선행 시험 commit `3511b66c`). |
| 마을 복귀 | Content의 「확인」 처리기가 플레이어 한 명씩 World 진입점을 부른다. 틱 스레드의 현재 맵 job 안에서 부른다. Content 요구: 도착 좌표 기본값은 World가 정함, 예외 대신 거부 결과, 이동 중 인벤토리·파티 보존(Content `msg_92f0e5340ca3`, World `msg_6e96d918962c` 3). World 답(`msg_25abb610ef01`): 도착은 도착 맵의 기본 입장 지점(`GameMap.PlayerSpawnPosition`, 최초 입장·재접속과 같은 곳), 인스턴스 밖·이동 중·세션 종료 중·같은 사람 두 번째 호출은 상태를 바꾸지 않는 거부, 성공하면 포탈 이동과 같은 맵 이동 패킷만 나간다. 진입점은 `MapMigration.MoveToPublicMap(GameSession session, int entityId, GameMap currentMap, MapId destination, Vector2? spawn = null)` → `MapMoveResult`다. 거부 검사 순서는 `SessionClosing` → `AlreadyMigrating` → `NotInThatMap` → `NotInInstanceMap` → `DestinationNotPublic`이고, 모두 통과하면 `Accepted`다. 공용 맵 도착은 다음 틱 이내다(World `msg_14c7e14412f3` 2). |
| PDL·생성 패킷·프로토콜 버전 | 던전 패킷·창 PR이 39·40번과 버전 17 → 18을 쓴다. World 인스턴스 맵 PR은 PDL을 바꾸지 않는다. World의 다중 계정 로그인과 같은 시기에 열리면 먼저 병합되는 쪽이 앞 번호를 갖고, 뒤 PR이 main을 받아 재생성해 번호를 다시 확인한다(같은 메시지 4). 기존 번호 재사용·재배열 금지. |
| `GameMap.cs` 보스 사망 분기, `GameWorld.cs` 처치 콜백, `Items/InventoryRegistry.cs` | World 인스턴스 맵 PR 병합 전에는 Content가 쓰지 않는다. World는 `InventoryRegistry.cs` 본문을 바꾸지 않을 계획이며, 바꿔야 하면 쓰기 전에 알린다(같은 메시지 5). |
| 포트 7777 | 서로 실행 전에 Orca 메시지로 알리고 DEVELOPMENT의 전역 lock·listener 검사를 따른다. |
| CURRENT | Content 줄만 바꾼다. 다른 파트 줄은 보존한다. |

## 검증 계획

- **검증 강도**: 세 PR 모두 강이다. 결함 PR은 클라이언트 화면 두 곳과 시험을 합쳐 제품+시험 50줄 이상이 예상되고, 애매하면 강이다. 던전 패킷·창 PR은 프로토콜·공유 DLL, 던전 클리어 PR은 서버 판정·경제·중복 방지다. 실제 파일·줄 수의 원시 근거는 각 계약에 적는다.
- **TDD**: 세 PR 모두 코드 변경이다. 신규 `claude-opus-5-5`가 요구 시험과 구현 전 실패 원시를 먼저 근거 폴더에 둔다. 쓰기 종료를 확인한 뒤 구현자가 같은 명령의 통과 원시를 둔다. 준비 단계의 실패는 제품 확정 실패에 넣지 않는다.
- **구현자**: 세 PR 모두 신규 `gpt-6-astra` xhigh다([배정](../../../.agents/skills/dawnholder-goal-loop/references/implementer-routing.md)). 결함 PR은 신호 4(배치 판단이 남은 다파일 화면 변경), 던전 패킷·창 PR은 신호 3(서버↔클라이언트 프로토콜)과 4(새 화면), 던전 클리어 PR은 신호 2(보스 사망 틱, 보상 창을 보는 동안의 퇴장·정리 수명)와 3이다.
- **검증자**: Astra 구현이라 세 PR 모두 신규 `claude-opus-5-5`다([검증자 시범](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증자-모델-시범2026-10-31까지)).
- **R-7**: 결함 PR과 던전 패킷·창 PR은 비해당이다. 네 범주(실패 수명·보호 집합·오류 분류·비용 상한)에 닿지 않는 표시·직렬화 작업이기 때문이다. 던전 클리어 PR도 비해당이다. 실패 수명과 오류 분류에 닿지만, 시범의 남은 두 자리를 World 인스턴스 맵과 Core DB 도구 컨테이너 전환 PR에 썼다(아이템·인벤토리 goal을 더해 시범 상한 세 건). 그래서 리드 설계 절과 선행 시험으로 간다(메인 결정 `msg_cfcaae303755`).
- **Unity**: 같은 checkout에서 Unity는 한 번에 하나만 실행한다. batch는 Unity 업그레이드 goal의 harness 사본을 쓴다. 실화면은 Unity MCP 시트를 메인에 요청한 뒤 한다.
- **실행과 미실행 구분**: 빌드, 서버 시험, 봇, Unity 컴파일·EditMode·PlayMode, 실화면, CI를 각각 따로 기록한다. DB는 이 goal에서 쓰지 않는다.

## 적용 중인 사용자 결정

정본에 반영하지 않은, 이 goal에만 적용하는 사용자 결정이다. 모두 메인이 전달했으며 리드 pane의 사용자 직접 입력으로 격상하지 않는다.

- **다음 goal과 우선순위**(메인 `msg_6d3f7b2831e6`, 2026-10-10 16:24 KST 메인 pane 제출). 원문 「7) 19 - Content 다음 goal → A 던전 + 결함 둘」, 「5) 17 - 마일스톤 우선순위(기능 동결 10월 28일까지) → A 인스턴스 던전 먼저」. 효과: 10-08 승인 던전 초안 v3을 goal로 옮기고, 인스턴스 맵이 들어올 때까지 기술 계약·클라이언트 쪽을 먼저 한다. 그동안 결함 둘을 같은 goal 첫 PR로 고친다.
- **범위 승인**(메인 `msg_9fe1d49e00bb`, 2026-10-10T07:39:37Z). 원문 「대시보드 결정 응답: 1) 계획 검토 - Content Content 던전 클리어·보상과 결함 둘 goal 범위(질문 셋, 추천 전부 A) → A 승인 (초안 msg_f1bba119c853)」. 효과: 범위 초안 v1 그대로이고 세 질문 모두 A다. 1 HUD 골드를 서버 재화에 연결한다. 2 「I」 키로 패널을 열고 닫는다. 3 보스 처치자 보상을 없애고 클리어 보상 하나로 합친다. PR 순서 결함 → 패킷·창 → 클리어도 승인 범위다.
- **10-08 던전 초안 v3 「전부 A」**(근거 `.backups/verification/2026-10-08-content-replan/dungeon-goal-draft-v3.md`, SHA256 `7a41a0d4…f5bd`). 1 보상은 그 순간 던전 안의 파티원 전원, 2 기존 적·보스 재사용(수치는 임시값), 3 복사본이 된 지금의 사냥터 → 보스방 길을 던전으로 쓴다.
- **메인 결정(사용자 결정 아님)**: R-7 시범은 이 goal에 쓰지 않는다(`msg_cfcaae303755`, 이전 보류 `msg_949e9610871f`를 대신함). 이 goal의 모든 Unity batch에 이전 결정 `msg_833c357e684e`와 같은 음소거 조건을 적용한다(`msg_498aa060f88d`): HKCU `Software\Unity Technologies\Unity Editor 5.x` / `AudioMasterMute_h3604209190` 하나만, 다른 Unity 0일 때만, 이미 1이면 쓰지 않음, finally 복원, 같은 폴더에 Editor가 열려 있으면 batch 금지, 실행마다 전후 원시를 남김.
- **메인 판단(사용자 결정 아님) — Unity가 다시 쓴 설정 파일**(`msg_c9fac8091065`, 2026-10-10T08:34:26Z). `ProjectSettings.asset`(사용자 미커밋)과 `TimeManager.asset`은 이 PR의 마지막 batch 뒤, 실화면 Editor를 열기 전, PR 생성 전 중 가장 이른 때에 리드가 한 번에 되돌린다. 그 전에는 두 파일을 stage·커밋하지 않고 계약마다 이 금지를 원문으로 넣는다. 되돌린 뒤 ProjectSettings는 SHA256 `4a8db0bd…`, TimeManager는 HEAD와 바이트가 같은지 원시로 남긴다.
- **메인 운영 조율(사용자 결정 아님) — 큰 실행 분리**(`msg_e30ad0e2a36c`, 2026-10-10T09:21:37Z). Core의 SQL 컨테이너와 Content의 Unity batch·PlayMode는 겹치지 않는다. 시작 직전 여유 물리·커밋 메모리(`Win32_OperatingSystem`의 FreePhysicalMemory·FreeVirtualMemory)를 원시로 남기고, 하나라도 4GB 아래면 기다린다. 상대가 큰 실행 중인지 모르면 우편함으로 알린다.
- **메인 결정(사용자 결정 아님) — 기존 시험 실패 분류**(`msg_71e41e231d55`, 이전 goal 근거 `2026-10-05-items-inventory-currency/main-test-classification-decision.json`). 독립 검증 계약에 원문을 붙였다. 실패를 (a) 옛 세부·(b) fixture·(c) 회귀·(d) 미확정으로 빠짐없이 나누고, 고친 시험은 표와 같은 명령의 전후 원시를 남긴다.

## 재개 지점

다음 리드는 이 블록부터 읽는다.

| 항목 | 값 |
|---|---|
| 작업 공간 / branch | `content-active` / `fix/hud-gold-inventory-panel-20261010`(base `cc20d428`) |
| 리드 세션 / Run | `term_df056d96-0389-436c-afcf-3d189f8dea53` / `run_b680cd89da9a`(회신 주소 `run:run_b680cd89da9a`) |
| 로컬에만 둔 변경 | 사용자 미커밋 `03_Client/Assets/Resources/MinimapRT.renderTexture`, `03_Client/ProjectSettings/ProjectSettings.asset`. 커밋·되돌리기·stash 금지 |
| 작업자·실행 자원 | 작업자 0(구현·수정 1·독립 검증 작업자 모두 정산·종료), Unity.exe 0, 7777 0(World·Core에 해제 통보) |
| 로컬 부수 변경 | batch가 다시 쓴 `ProjectSettings.asset`·`TimeManager.asset`은 독립 검증의 마지막 batch 뒤 2026-10-10T09:51:43Z에 되돌렸다. ProjectSettings는 사용자 원래 상태 `4a8db0bd…`, TimeManager는 HEAD와 같다(근거 `settings-restore/post-state.txt`). 실화면 Editor가 다시 쓰면 같은 방식으로 기록한다 |
| 남은 결정·준비 | R9 실화면: Unity MCP 시트 배정과 사용자의 content-active Editor 열기·MCP 연결 승인을 메인에 요청했다(`msg_6d76f198a58b`). 메인 답(`msg_471ee22962bc`): 사용자가 돌아온 뒤 Editor 먼저 → 연결 승인 → 검증자 기동 순서로 하고, 시트는 Content R9 검증자 한 세션에 배정한다. 그 전에는 MCP를 호출하지 않는다. PR 생성(CI)은 먼저 하고 병합 승인은 R9 결과와 함께 묻는다 |

**남은 순서**: 결함 PR 생성(CI) → R9 실화면(신규 Opus + Unity MCP) → 사용자 병합 승인 → 던전 패킷·창 PR → 던전 클리어 PR → 결과 기록·Gardener → 종료 기록 PR → 종료 점검 → R-8.

## 진척 단계

- [x] 범위 승인
- [x] 결함 선행 시험
- [>] 결함 구현·검증
- [ ] 결함 PR 병합
- [ ] 패킷·창 구현·검증
- [ ] 패킷 PR 병합
- [ ] 클리어 설계 확정
- [ ] 클리어 서버 구현
- [ ] 검증·실제 플레이
- [ ] 클리어 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 병합

## 실제 결과와 미실행

- 범위 초안·사용자 승인·World 계약 확인까지 했다. 원문은 근거 폴더의 `scope-draft-v1.md`와 `inbox/`에 있다.
- 결함 PR은 선행 시험 → 구현 → 수정 1 → 독립 검증까지 끝났다(아래 세 절). 실행한 것: Unity batch EditMode·PlayMode, WSL GameServer 서버 lane(DB 없음). 실행하지 않은 것: R9 실화면, 봇, 빌드 단독 실행, CI.

### 선행 시험 — RED 준비 성공, 2026-10-10

| 항목 | 값 |
|---|---|
| Task / Dispatch | `task_f4fb52449c6c` / `ctx_b8327985145f`, `[Content 검증자]` |
| 모델 | 지정 `claude-opus-5-5`. 최초 실행 명령 `$env:TEMP=…\.backups\tmp\pr1-tdd; claude --model claude-opus-5-5`, 화면 표시 「Opus 5.5 with xhigh effort」. backend 실제 모델 unknown |
| 계약 | `opus-pr1-tdd-contract.md` SHA256 `f0dae46d44f313384608b994cd54d742ab3583dd6f975d3d502aa392e1e40ee1`, 요약 spec `opus-pr1-tdd-spec.md`. 보충 v1.1(`msg_df9f59cf608b`): harness를 pwsh 7.6.6으로 돌리고 EditMode만 실행 |
| 보고 | `opus-pr1-tdd/report.md`, worker_done `msg_9a89f1c0dcb3`(08:31:07Z, succeeded) |

- **결과**: 새 EditMode 시험 13개(`HudGoldMirrorTests.cs` R1~R3 6개, `InventoryPanelToggleTests.cs` R4~R8 7개, 공용 대역 `GameplayViewTestSupport.cs`)가 모두 미구현으로 실패한다. 마지막 실행 `editmode-red-3`은 417개 중 통과 404, 실패 13, 건너뜀 0, exit 2다. fixture 원인 실패와 기존 시험 실패는 0이다. 컴파일 오류 0. 음소거는 0 → 1 → 0으로 복원됐다.
- **리드 R-2**: `runs/editmode-red-3/results.xml`(SHA256 `db7c6dcb…`)을 node로 읽어 test-run의 417/404/13/0과 실패 13개의 이름이 모두 새 두 시험 파일임을 확인했다. editor.log의 `error CS` 0건, 「Exiting with code 2」, mute-events 세 줄을 직접 읽었다. R1·R2 시험 본문을 읽어 기대값이 서버 리터럴과 화면 글자이고 제품 계산을 복제하지 않음을 확인했다.
- **리드 채택 결정**: (1) 열림 관찰면은 패널 root의 CanvasGroup·Canvas 활성·본문 활성이다. (2) 「I」 키 대역은 Input System의 `runPlayerUpdatesInEditMode`를 켜고 이전 값으로 되돌린다. 계약의 설정 사본 방식은 이 플래그가 직렬화되지 않아 복원하지 못하기 때문이다. 패키지 내부 필드에 기대므로 패키지 업데이트 때 fixture Assert가 먼저 깨진다. (3) 「I」 처리는 `InventoryPanel.BuildRuntime`이 만든 객체 안에 둔다. 시험이 그 객체의 Update만 부르기 때문이다. (4) 일시정지는 `Time.timeScale = 0`, 씬 전환은 SceneTransition의 끝나지 않은 요청과 맵 입장 진행이다. (5) HUD 빈 값은 숫자가 없는 글자 또는 꺼진 글자다.
- **알아 둘 것**: R7 두 시험의 본 단정(정지·전환 중 불변)은 지금 「I」 처리 자체가 없어서 통과하고, RED는 대조 단정이 만든다. 검증자는 둘이 함께 통과하는지 본다. 기존 PlayMode `InventorySceneLifecycleTests.cs` 189·398·399행은 입장 직후 패널이 보인다고 단정해 구현 뒤 실패할 것으로 예상된다(미실행 예측, 분류 (a) 후보).
- **부수 변경**: 첫 실행 때 Unity가 `ProjectSettings.asset`(사용자 미커밋, SENTIS define 한 줄 삭제, 알려진 `unity-sentis-define-drift`)과 `TimeManager.asset`(6.6 형식으로 다시 씀, 값 같음)을 저장했다. 작업자는 되돌리지 않았고 사본·diff를 `opus-pr1-tdd/side-effects/`에 두었다.
- **정산**: worker-release(retained/external_terminal) → 빈 prompt와 「done」 확인 → close(ptyKilled true). 화면 표시는 12분 48초 작업, 18.37$다.
- **리드 기동 실수(첫 발생)**: 전체 계약을 `--spec`으로 넣다가 Git Bash 인자 길이 한도(Argument list too long, exit 126)로 orca가 실행되지 않았다. worker-list 0건을 확인하고 계약 경로·hash를 가리키는 요약 spec으로 다시 붙였다. 첫 발생이라 goal에만 기록한다.
- **리드 도구 사고(첫 발생)**: 리드가 큰 우편함 JSON에 `grep -o` 범위 정규식과 `| head`를 써서 grep이 부모 없이 남아 메모리 16GB를 잡았다(메인 `msg_64f64f30c24f`). 다른 리드의 우편함 대기가 메모리 회수로 꺼졌다. 사용자가 그 프로세스를 처리했다. 이후 JSON은 node로 읽는다.

### 결함 구현과 수정 1, 2026-10-10

| 작업 | Task / 모델 | 결과 | 원시 |
|---|---|---|---|
| 1차 구현 | `task_c3ed610e39b9`, 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown) | 제품 3파일 +69/−11, 문서 2파일 +6/−2. EditMode 417/417/0/0. 커밋 `79d7fc55` | `astra-pr1-impl/report.md`, `runs/editmode-green-1/results.xml`(SHA256 `d90e188a…`) |
| 수정 1 | `task_1d9237212f9e`, 신규 `gpt-6-astra` xhigh | HUD 빈 값 `Gold: —` → `Gold: -`. EditMode 417/417/0/0. 커밋 `2ebcbcf6` | `astra-pr1-fix1/report.md`, `runs/editmode-fix-1/results.xml`(SHA256 `660075f5…`), 문자표 `glyphs-after.json` |

- **설계**: 열림 상태는 연결 객체 `UnityClientSession.IsInventoryPanelOpen` 하나가 갖는다. 새 연결은 닫힘으로 시작하고, 맵 이동은 같은 세션이라 유지되며, 연결 정리 뒤 새 세션은 다시 닫힘이다. 「I」는 `InventoryPanel.Update`가 읽고, 일시정지·씬 전환·게임플레이 Ready 전에는 무시한다. 닫힘은 CanvasGroup만 끄므로 구독과 재조회가 유지된다. HUD 골드는 `InventoryState`의 `HasSnapshot`·`Currency`와 변경 통지로 그리고, `_mockGold` 필드는 지웠다.
- **리드 발견 L-1(수정 1의 원인)**: 1차의 빈 값 `—`(U+2014)가 HUD 골드 글자의 폰트 `Pretendard SDF Proper`(정적 atlas, 자체 fallback 없음)와 TMP 전역 fallback(같은 폰트)에 없었다. EditMode 시험은 숫자만 읽어 이를 보지 못했다. 같은 산출물 수정 1회째이며 확정 실패가 아니다(`lead-r2-impl-1.md`).
- **리드 R-2**: 두 작업 모두 results.xml을 node로 다시 읽어 수치와 새 시험 13개 통과, `error CS` 0, 음소거 복원을 확인했다. 수정 1 뒤 리드가 폰트의 `m_Unicode` 11267개를 직접 읽어 HUD 골드 두 문자열의 누락 글자 0을 확인했다.

### 결함 PR 독립 검증 — 통과, 2026-10-10

| 항목 | 값 |
|---|---|
| Task / 모델 | `task_299bc7fdf7c8`, 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown) |
| 계약 | `opus-pr1-verify-contract.md` SHA256 `35cba0dc…fb54`. 보충 v1.1 메모리 확인, v1.2 Core 컨테이너 동안 대기, v1.3 재개 |
| 판정 | `opus-pr1-verify/verdict.md`, worker_done `msg_74d5dca1c629`(09:50:00Z). 통과, 제품 결함 0 |
| 수치 | EditMode 417/417 → 417/417. PlayMode(이 checkout의 WSL GameServer 7777 위) 23/16/7/0 → 26/26/0/0 |
| 커밋 | 검증자 시험 4파일 `efd0442b` |

- **기존 시험 7개 (a)**: PlayMode `InventorySceneLifecycleTests` 6개와 `InventoryServerIntegrationTests` 1개가 「입장 직후 패널 보임」과 「미러 구독자 1」을 단정해 실패했다. 메인 결정 `msg_71e41e231d55`에 따라 I로 연 뒤 원래 단정을 그대로 두게 고쳤다. 구독자 단정은 「패널 1·HUD 1·그 밖 0」으로 대상을 구분해 더 엄격해졌다.
- **새 PlayMode 시험 3개**: 실제 UI 씬 HUD 골드(빈 값·패널과 같은 재화·폰트 누락 0), 닫힌 패널 자리 클릭이 공격으로 통과하고 연 뒤에는 사용이 됨, 실제 일시정지 메뉴 동안 I 무효.
- **실제 진입**: 서버 lane 위 PlayMode에서 실제 서버 Town → 키보드 I로 열기 → 포털 이동 뒤 열림 유지 → 키보드 처치 보상 → 포인터 사용 확정까지 통과했다.
- **리드 R-2**: 일치(`lead-r2-verify.md`). XML 네 개의 hash·수치, 실패 7개 이름, 고친 시험 diff를 직접 대조했다.
- **설정 파일 복원**: 이 검증이 이 PR의 마지막 batch다. 그 뒤 메인 판단 `msg_c9fac8091065`대로 두 설정 파일을 되돌렸다(재개 지점).

### 운영 기록(첫 발생)

- Orca CLI 1.4.224의 `terminal split`에는 `--title`이 없어 첫 구현자 기동이 실패했다. 플래그 없이 다시 열었다(`astra-pr1-impl-launch-fail-title.json`).
- 수정 1 작업자의 heartbeat 하나가 subject에 태그를 붙이고 body를 비워 helper가 body-tag 위반으로 판정했다(`msg_2c48778c8c93`). identity는 맞았고 내용이 없어 처리하지 않았다. 다음 계약부터 빈 heartbeat subject를 정확히 `alive`로 쓰라고 넣었다.
- 리드가 수정 1 진행 중에 고정 입력 파일(`lead-r2-impl-1.md`)에 관찰을 덧붙여 hash를 바꿨다가 바로 떼어 내 원래 hash로 되돌렸다. 관찰은 `lead-inbox-notes.md`로 옮겼다.
- 메모리 몰림(메인 `msg_e30ad0e2a36c`) 뒤 Core SQL 컨테이너가 먼저 돌도록 Content가 15분 기다렸다(Core `msg_fced0edb6f02` → `msg_ab97dbb84891`).

## 다음 계획 후보

이 goal에서 발견한 범위 밖 후보는 출처와 함께 한 줄로 적고 [BACKLOG](../../../00_Document/operations/BACKLOG.md)의 ID를 붙인다. 등록은 착수 권한이 아니다. 아래는 결함 PR 독립 검증의 비차단 관찰이며, BACKLOG 등록 여부는 종료 기록 때 정한다.

- `HudController` 클래스 요약에 골드의 출처(InventoryState 미러)가 없다. 지금은 FEATURE_MAP·client.md에서 찾는다(판정 「사람 가독성 지적」).
- `UnityClientSession`이 UI 표시 bool을 가진다. 표시 상태가 늘면 연결이 소유하는 작은 표시 상태 객체로 옮긴다(O-1).
- `HudController`의 `OnEnable`·`Start`가 `BindInventory` 뒤 `RenderGold`를 한 번 더 부른다. 동작 영향은 없다(O-2).
- `UI.unity`에 지운 필드 `_mockGold: 0`의 직렬화 잔여가 있다. 다음 scene 저장 때 Unity가 지운다(O-3).
- EditMode 씬 전환 시험은 맵 입장과 SceneTransition이 함께 걸린 경우만 본다. 지금 제품 경로에서는 둘이 늘 겹친다(O-4).
- EditMode 가상 키보드가 Input System 내부 `runPlayerUpdatesInEditMode`에 기댄다. 패키지 업데이트 때 fixture가 먼저 깨진다(O-5).
