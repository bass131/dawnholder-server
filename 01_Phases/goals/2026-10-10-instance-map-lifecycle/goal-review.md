# 인스턴스 맵 구현 전 설계 검토 (R-7, `[World 검증자]` Fable)

## 작업 전 맥락

- 역할·자기 태그 / 할당 작업 / 지정 모델·관찰 모델·backend unknown 여부: 신규 외부 Fable 설계 검토자 `[World 검증자]`. 할당 하나 = 인스턴스 맵 goal의 구현 전 설계 검토(ORCA R-7 2단계), 구현자 아님. 지정 `claude-fable-5-1` xhigh, 이 세션의 시스템 안내가 표시한 모델 `claude-fable-5-1`, backend 실제 모델 unknown.
- 현재 goal·할당 계약의 경로 / 작업 경로·branch·base·HEAD: goal `01_Phases/goals/2026-10-10-instance-map-lifecycle/goal.md`, 계약 = 리드 Run `run_c21dddd08312`의 Dispatch `ctx_4a306636d9b0` TASK 본문(계약 v1). 작업 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active`, branch `feat/instance-map-lifecycle-20261010`, base `bd4dbb5fab6c690f68c4d37111a52b8e93e9d992`, HEAD `9e621b1dcbe02571446c0df5b5489a0fbadaad77`(`git rev-parse HEAD` 확인, `git status --short` 출력 없음).
- 메모 작성 시점(시계 명령 출력의 시각) / 대상 파일 첫 쓰기 전 여부: `date -u` → `Sat, Oct 10, 2026  8:36:05 AM`(UTC). 이 메모가 이 파일의 첫 쓰기였고(메모만 먼저 저장, 51줄), 검토 본문은 그 뒤 두 번째 쓰기로 이어 붙였다.
- 기존 사용자 변경·다른 작성자의 소유권 / 허용 쓰기·실행·금지 범위: 미커밋 변경 없음. goal.md는 리드 소유(읽기만). 허용 쓰기는 이 파일 하나와 지정 TEMP `.backups/tmp/f1` 아래뿐(임시 파일은 쓰지 않았다). 허용 실행은 읽기 명령·`git log/show/diff/status`·`date -u`. 빌드·시험·봇·Unity·DB·웹·위임·commit·push 금지.
- 원문 요구/후속 보충의 계약 버전·시각 / 고정 입력 경로·hash / 적용 단계: 계약 v1(후속 보충 없음, `orca orchestration check` 결과 messages 0건). 요구 원천 `msg_d0646fa471c6`(범위 승인), `msg_a31ac62345e8`(R-7 사용 판단), `msg_9e1c16e57af3`(검토자 기동). 입력은 모두 HEAD `9e621b1d`의 blob(아래 표). 적용 단계 = R-7 2단계(검토 원문 작성). 3단계 메인 원문 확인 이후는 리드·메인 몫이다.
- 검증 등급·이유와 파일/줄 수 원시 근거 / R-7 해당·비해당과 이유: goal 「진행 방식」의 「강」을 그대로 인용한다(서버 판정·실패 수명, 50줄 초과 추정). 이 검토는 코드를 쓰지 않으므로 줄 수 원시 근거는 해당 없음. R-7 해당: 메인 판단 `msg_a31ac62345e8`로 시범 두 번째에 배정됐고, 4범주 중 실패 수명·보호 집합·오류 분류·비용 상한 모두에 닿는다.

### 읽기와 선택

| 읽은 파일·절 | commit 또는 미커밋 hash | 이 작업에서 필요한 규칙·경계 |
|---|---|---|
| `AGENTS.md` 전부 | blob `954fa175ab39` | 역할 분리, 검증자는 제품을 고치지 않음, 태그, 보고 양식 |
| `CLAUDE.md` 역할 경계 절 | blob `ce0e0f26adb9` | R-7 검토자는 해당 목표의 `goal-review.md`만 씀 |
| `.agents/skills/dawnholder-task-context/SKILL.md`, `references/templates.md` 「맥락 메모」 | blob `82baa3d960bb`, `2966da56c82b` | 메모 필드, 전제 표식, 설계 관찰(비차단), 자기 쓰기 감사 |
| `00_Document/operations/ORCA.md` R-7 절 | blob `572afccb73ae` | 산출물 = 불변식 목록 + 전제 [소스]/[추론]/[미측정], 평가 기준 「메인이 놓친 문제」 |
| `00_Document/conventions/CODE_CONVENTION.md`(계약에 붙은 원문, blob `44c5bb6a8f5d` 일치 확인) | blob `44c5bb6a8f5d` | 상태의 단일 소유자·수명, 실패·취소·종료 처리, 배치·이름, 사용처 없는 hook 금지 |
| `01_Phases/goals/2026-10-10-instance-map-lifecycle/goal.md` 전부 | blob `0f3c437d44dc` | 승인 범위·완료조건·초안 불변식 1~12·Content 계약·16곳 목록 |
| `00_Document/FEATURE_MAP.md` 포탈·맵 이동, 처치 진행·보스 해금, 아이템·인벤토리·재화 행 | blob `af240b244676` | 진입점과 소유 파일 |
| `00_Document/domains/server.md` 전부 | blob `a114a5830f2f` | 틱 순서, 경제 연결 수명, 종료와 broadcast 경쟁 |
| `02_Server/GameServer/Loop/GameWorld.cs` 전부, `Loop/TickScheduler.cs` `IsTickThread`·`RunLoop` | blob `57e0b50f5071`, `078512a88e88` | 등록부, 전체 맵 순회 6곳, 처치 콜백, OnTick 순서, 틱 예외 처리·측정 |
| `Maps/GameMap.cs` 지정 구간 | blob `162715d0caed` | job 큐, Tick 단계, 입장·제거·방송, 보스 재등장, 적 사망 |
| `Maps/Transitions/MapMigration.cs` 전부, `PlayerTransferState.cs`, `Maps/MapId.cs`, `Maps/PortalTable.cs` | blob `dec6522d4bd1`, `e294c83d8b15`, `4a186654b246`, `423c8a88ccc7` | 이동 순서, 목적지 없음 경로, 포탈 표 |
| `Sessions/GameSession.cs` 지정 구간 | blob `2e16ffebd3a4` | 현재 맵 int, `_migrating`·`_closing`, GetMap/GetDestMap hook, 진입·끊김 |
| `Quest/QuestRegistry.cs` 전부, `Party/PartyRegistry.cs`·`Party/PartyFlow.cs` 소속·해산·끊김, `Items/InventoryRegistry.cs` 위임 사용처 | blob `5851b3b2b47d`, `3b0381ea4741`, `d56905cd7fe2`, `93a151979364` | 진행 초기화 범위, 파티 번호 발급, 해산 콜백, 등록 때 owner 확인 |
| `GameServer.Tests/Maps/MapMigrationTests.cs`·`SessionCleanupTests.cs`·`GameWorldRegistryTests.cs` | blob `23d94b49c4b7`, `0243bdca8c6d`, `b1d1f480e663` | 월드 생성·맵 조회·틱 구동 방식(관찰 경계만) |
| 선택: E/`lead-entry/instance-architecture-research.md` 제목·결론 줄 | Git 밖(SHA256은 goal 기록 `0d5a1bf9…918da`) | 정리 즉시 vs 지연의 근거만. 외부 출처는 열지 않음 |

- 같은 책임의 예시 1~2개(경로·심볼/절) / 따를 이름·서식·오류·주석 관례: 등록부 예시 = `GameWorld._maps`(`Loop/GameWorld.cs:28`)와 `PartyRegistry`(`Party/PartyRegistry.cs:14`, 틱 스레드 전용 Dictionary + 역인덱스). 정적 표 예시 = `PortalTable.GetPortalsFor`(`Maps/PortalTable.cs:137-144`, switch). 결과값 예시 = `InventoryResult`(`Items/`, 거부를 열거형으로 돌려줌, `InventoryRegistry.cs:135-136`). 주석 관례 = 비자명한 스레드·수명 이유만 짧게.
- 재사용 helper·계약 / 재사용하지 않거나 중복이 필요한 이유: `QuestRegistry.ForgetPartyProgress`(`:76`)는 파티 열쇠 초기화에 그대로 재사용 가능. `MapMigration`의 transfer 단계(`:109-178`)는 서버가 시키는 이동과 공통 추출 대상. `GameMap.PlayerSpawnPosition`(`:131-134`)은 기본 입장 지점으로 재사용.
- 영향 파일·사용처·보존 동작: 아래 「시험 영향」 절. 보존 동작 = 포탈 검증 순서와 silent drop, 이동 패킷 순서(S_PlayerLeave → S_MapTransition → S_PlayerHp → roster → S_PlayerJoin), 경제 연결 수명, ADR-026 entity 번호 유지.
- 신규 파일 위치·이름 / 기존 경로·명명 관례 근거 / 새 폴더 필요 이유: 이 검토는 제품 파일을 만들지 않는다. 구현이 만들 등록부·열쇠·맵 종류 표의 위치와 이름은 「고정 제안」 P-01~P-03에 적었다. 새 폴더 불필요(`Maps/` 안).
- 읽기 상한을 넘긴 추가 경로·구간과 이유: `02_Server/GameServer/Program.cs:8-24`(월드 생성 1회와 provider가 지역 변수인지 확인, IM-01·P-04 근거). `Quest/QuestNotifier.cs:1-22`, `Party/PartyNotifier.cs` grep(전체 맵 조회가 맵 틱 밖에서만 불리는지, IM-16 근거). `Entities/PlayerEntity.cs` grep(entity가 맵 역참조를 갖지 않음, IM-08 근거). `Maps/Systems/RespawnSystem.cs` grep(부활 대기열이 맵 소유라 복사본과 함께 사라짐). `98_Shared/GameData/Constants.cs:13-16`(틱 50ms). 시험 전체에 `\.GetMap(` grep(계약의 16곳 grep이 놓친 동적 조회 확인, S-01 근거). `Items/InventoryLifecycleRaceTests.cs:269-310`, `Maps/GlobalEntityIdTests.cs:70-95`, `Party/GameWorldPartyIntegrationTests.cs:120-145`, `Party/QuestKillCountTests.cs:20-50`(16곳의 조회 방식 표본 4곳).
- 언어 규칙 부재·임시 기준과 승인 상태: 해당 없음(코드를 쓰지 않음).
- 관련 규칙 절 선택 / 해당 없는 절·제외 이유: 계약이 붙인 「상태와 책임」「파일 위치와 이름」「주석과 문서」「역할별 적용」을 적용한다. 제외는 계약의 이유 그대로(Unity·빌드 검사·서식·SQL/PS/TS/Python·교정 층).
- 열린 질문·차단 / 질문할 담당자 / 완료 때 처리 근거: 차단 없음. 정책 입력이 필요한 항목은 「범위 영향」 S-06에 모아 리드·메인에 넘긴다(ask로 막지 않았다. 읽기 전용 검토 범위에서 답할 수 없는 정책이라 기록으로 넘기는 편이 맞다고 봤다).

### 준수 연결

| 적용 규칙·출처 절 | 준수 계획 | 완료 뒤 실제 파일·심볼/절·근거 | 계획 변경 이유 |
|---|---|---|---|
| 상태와 책임: 단일 소유자·수명, 실패·취소·종료 처리 | 불변식마다 소유자와 실패·중복·종료·늦은 도착을 적는다 | 이 파일 IM-01~IM-17 각 「소유자」「실패·중복·종료·늦은 도착」 줄 | 없음 |
| 상태와 책임: 외부 입력 경계 검증, 늦은 결과의 영향 | 서버가 시키는 이동의 검사 순서와 거부값, 늦은 arrival job | IM-13, IM-02, IM-04 | 없음 |
| 파일 위치와 이름 | 등록부·열쇠·표의 위치와 이름을 기존 예시 근거와 함께 제안 | P-01·P-02·P-03·P-08의 「주변 관례」 줄 | 없음 |
| 주석과 문서 | 비자명한 이유(순서·계수·은퇴 표식)만 주석 대상으로 지목 | O-04·O-05 | 없음 |
| 역할별 적용·R-7 | 전제 [소스]/[추론]/[미측정], 설계 관찰(비차단), 미검토·미실행 구분 | 각 전제 줄, 「설계 관찰(비차단)」, 「미검토·해당 없음·미실행」 | 없음 |
| 읽기 상한 | 지정 입력 + 같은 책임 예시 1~2개 | 「읽기와 선택」 표와 「읽기 상한을 넘긴 추가 경로」 줄(이유 적음) | 없음 |

- 자기 쓰기 감사: 문서 끝 「자기 쓰기 감사」 절.

## 검토 결론 요약

한 줄: 리드 초안(그림 3·4, 불변식 1~12)의 뼈대는 소스와 맞고 채택할 만하다. 다만 구현 전에 고정하지 않으면 유령·잠김·누수로 이어지는 빈칸이 다섯 있다.

메인이 놓쳤을 가능성이 큰 지적(번호는 아래 불변식·제안·범위 영향):

1. IM-01: 목적지 복사본 확보가 출발 맵 제거보다 먼저여야 한다. 지금 `MapMigration`은 RemovePlayer 뒤에 목적지를 찾는다(`:118` → `:125`). 지금은 목적지 조회 실패가 설정 버그뿐이지만, 복사본은 생성이 실패할 수 있는 단계(`GameMap` 생성자가 예외를 던짐 `GameMap.cs:100-105`)이고 그 예외는 `GameMap.Tick`의 job try/catch(`:237-241`)가 삼킨다. 그러면 플레이어는 어느 맵에도 없고 `_migrating`=1이 영원히 남아 모든 입력이 버려진다(소프트 잠김). 초안 그림 4는 순서를 「빼기 → 목적지 정하기」로 두어 이 위험이 그대로다.
2. IM-02: 「들어오는 중」 계수의 감소가 arrival job의 모든 탈출 경로(closing skip `MapMigration.cs:140-144`, 예외)에 있어야 한다. 하나라도 빠지면 그 복사본은 영원히 정리되지 않고(IM-15 상한이 깨짐) 빈 복사본이 매 틱 돈다.
3. S-01: goal 「범위 근거」의 16곳(10파일) grep은 `GetMap(MapId.HuntingGround|BossRoom)` 리터럴만 센다. `Enum.GetValues<MapId>()`로 모든 맵 번호를 돌며 `GetMap(id)!`를 부르는 동적 조회 6곳(파일로는 2개가 새로 늘어 12파일)이 더 있다. 인스턴스 종류에 `GetMap(MapId)`가 null을 돌려주는 순간 `!`에서 NullReferenceException으로 터진다. 완료조건 6의 수와 전수 분류 표의 모집단이 달라진다.
4. P-04: `GameWorld`는 맵 데이터(provider)를 생성자에서만 쓰고 보관하지 않는다(`GameWorld.cs:62-93`에 필드 없음, `Program.cs:11,16`에서 지역 변수로 한 번 넘김). 나중에 복사본을 만들려면 맵별 지형·콘텐츠 또는 맵 팩토리를 월드가 들고 있어야 한다. 초안에 없다.
5. IM-16: 지금 틱 순서(Town → HG → BR → Ending)는 Dictionary 삽입 순서에 기대고, 시험이 「먼저 도는 맵/나중에 도는 맵」을 구분해 같은 틱 안의 처치→이동 순서를 단정한다(`InventoryLifecycleRaceTests.cs:70-95`, 이름 자체가 LaterTickedMap/EarlierTickedMap). 스냅샷 순서를 「공용 맵은 MapId 순, 복사본은 생성 순」으로 명시해야 이 단정이 그대로 남는다.

그 외 채택 권고: 정리된 복사본의 은퇴 표식(IM-04), 서버가 시키는 이동의 검사 순서와 결과값(IM-13), 보스 처치 초기화의 열쇠 전달 방식(IM-12·P-12).

읽는 법: 불변식은 IM-번호, 고정 제안은 P-번호, 범위 영향은 S-번호, 비차단 관찰은 O-번호다. 전제마다 [소스]=실제 파일:줄, [추론]=이유, [미측정]=확인 방법을 붙였다. 「구분」은 범위 안 기술 선택인지 메인·사용자 정책 입력이 필요한지다. 파일 경로는 `02_Server/GameServer/` 아래를 생략했고 시험은 `02_Server/GameServer.Tests/` 아래다. 줄 번호는 HEAD `9e621b1d` 기준이다.

## 불변식 목록

### 실패 수명

#### IM-01 목적지 복사본은 출발 맵을 바꾸기 전에 확보한다

- 보호 상태 집합: 세션의 `_migrating`, 출발 맵 `_players`, 목적지 복사본의 존재.
- 진입·적용 시점: 포탈 job과 서버가 시키는 이동 모두, 출발 맵의 job 안(틱 스레드). 순서 = 검증 → 목적지 찾기·없으면 만들기 → `SetMigrating(1)` → `RemovePlayer` → 들어오는 중 +1 → arrival job 등록.
- 실패·중복·종료·늦은 도착: 생성이 실패(예외)하면 아무것도 바꾸지 않고 job을 끝낸다. 플레이어는 출발 맵에 그대로 남고 다음 포탈 시도가 가능하다. 로그 한 줄.
- 깨지는 반례: 지금 순서(`SetMigrating(1)` `:115` → `RemovePlayer` `:118` → `getDestMap` `:125`)에 생성을 끼우면, 생성 예외 → `GameMap.Tick`의 catch(`:237-241`)가 삼킴 → 플레이어는 어느 맵에도 없고 `_migrating`=1 영구 → `GetMap()`이 항상 null(`GameSession.cs:557-561`) → 이동·공격·포탈·경제 요청 전부 무응답(`:579-580`). 끊어야만 풀린다.
- 관찰할 시험(공개 경계): 복사본 팩토리가 예외를 던지는 월드(시험이 provider나 팩토리로 주입)에서 Town 포탈 진입 → 틱 → 플레이어가 Town에 남아 있고(`GetPlayer` non-null), 이후 이동 입력이 위치를 바꾸며, 월드의 살아 있는 복사본 수가 0이다.
- 소유자: `MapMigration`(순서), `GameWorld`/등록부(find-or-create).
- 전제: [소스] `MapMigration.cs:105-133` 순서, `GameMap.cs:95-113` 생성자가 적 스폰과 kindId 검증을 하며 던질 수 있음, `GameMap.cs:237-241` job 예외는 로그 후 계속. [추론] 지금은 `getDestMap`이 설정 버그 외엔 실패하지 않아 순서가 문제되지 않았지만, 복사본 생성은 실패할 수 있는 단계라 순서가 결과를 바꾼다. [미측정] 실제 생성 실패 빈도는 알 수 없다. 반례 시험으로 순서만 고정한다.
- 구분: 범위 안 기술 선택. 단, `MapMigrationTests` 13번(`:675-720`)이 「RemovePlayer 직후 = GetDestMap 호출」을 결정론 hook으로 쓰므로 순서 변경은 그 시험의 hook 지점을 옮긴다(S-02).

#### IM-02 「들어오는 중」 계수는 +1마다 정확히 한 번 -1 된다

- 보호 상태 집합: 복사본의 incoming 계수, 정리 조건(IM-03), 복사본 수 상한(IM-15).
- 진입·적용 시점: +1은 출발 맵 job에서 arrival job을 넣기 직전(목적지 확보 뒤, IM-01). -1은 arrival job의 첫 문장(어느 분기로 나가든 먼저 줄인다) 또는 try/finally.
- 실패·중복·종료·늦은 도착: closing skip(`MapMigration.cs:140-144`)으로 나가도 -1. arrival job 안에서 예외가 나도 -1(try/finally). 같은 세션이 두 번 이동할 수 없으므로(`_migrating`, IM-13 검사 순서) 중복 +1은 없다. 끊긴 세션의 arrival job은 복사본이 살아 있는 한 반드시 다음 틱에 실행된다(복사본은 등록부에서 매 틱 돈다).
- 깨지는 반례: closing skip 분기가 -1을 빼먹으면, 이동 중 끊김마다 복사본 하나가 영원히 남아 매 틱 적 AI를 돈다. 봇이 「이동 중 끊김」을 반복하면 복사본이 단조 증가한다(IM-15 위반).
- 관찰할 시험: 이동 중 끊김(`MapMigrationTests` 13번 구조) → 두 틱 뒤 살아 있는 복사본 수 0, 두 맵 모두 Players 비어 있음. arrival job에서 예외를 내는 세션(시험용 Send 예외)으로도 같은 결과.
- 소유자: `MapMigration`(arrival job), 등록부(계수 저장).
- 전제: [소스] `MapMigration.cs:137-178` arrival job의 탈출 경로는 closing skip 하나와 정상 완료, `GameMap.cs:237-241` job 예외는 삼켜짐. [추론] 계수를 `GameMap`이 아니라 등록부 항목이 들면 `GameMap`의 변경 이유(복사본 수명)가 늘지 않는다. 다만 Content가 맵에서 열쇠를 읽어야 하므로 열쇠는 맵에, 계수는 등록부 항목에 두는 편이 책임이 맞는다(P-05·P-09). [미측정] 없음.
- 구분: 범위 안 기술 선택.

#### IM-03 복사본 정리는 틱 끝 한 곳에서, 「플레이어 0 ∧ 들어오는 중 0」일 때만, 틱 스레드에서만

- 보호 상태 집합: 등록부 항목, 복사본의 `_players`·`_enemies`·job 큐·부활 대기열.
- 진입·적용 시점: `GameWorld.OnTick`의 Party/Quest/Inventory Tick 뒤, barrier 완료 앞(초안 그림 4의 5단계). 맵 틱 순회 중에는 정리하지 않는다. 생성은 맵 job 안(순회 중)에서 일어나므로 순회는 스냅샷(IM-16).
- 실패·중복·종료·늦은 도착: 정리는 멱등(이미 없으면 무시). 세션 끊김은 두 번째 `DrainSessionCloses`(`:272`)가 Party 틱 전에 플레이어를 뺀 뒤이므로 같은 틱 끝에 정리된다. 월드 `Stop`은 틱을 멈출 뿐 복사본을 지우지 않는다(지금 공용 맵과 같음, 시험은 `Stop()`으로 singleton만 푼다 `GameWorldRegistryTests.cs:29-32`).
- 깨지는 반례: 정리를 arrival job 실행 전에 하면 만든 직후 빈 복사본이 첫 입장 전에 사라져 arrival job이 죽은 맵에 들어간다(초안 불변식 4의 금지 사례). 정리를 맵 틱 순회 안에서 하면 Dictionary 열거 중 수정으로 `InvalidOperationException`이 틱을 통째로 죽인다(`TickScheduler.cs:103-107`이 예외를 잡아 다음 틱으로 넘기지만 그 틱의 나머지 맵은 돌지 않는다).
- 관찰할 시험: 두 세션이 같은 틱에 Town 포탈 진입 → 틱 1 뒤 복사본 1개·플레이어 0·들어오는 중 2 → 틱 2 뒤 플레이어 2. 둘이 역포탈로 Town 복귀 → 그 틱 끝에 복사본 0. 시험은 `LifecycleTestWorld.Tick`(reflection으로 `OnTick` 호출, `SessionCleanupTests.cs:14-19`)로 정리 단계까지 돌릴 수 있다. `_world.Map.Tick(n)`처럼 맵만 돌리는 시험(`SessionCleanupTests.cs:86,172,194,218`)은 정리를 관찰하지 못하므로 정리 단정에는 쓰지 않는다.
- 소유자: `GameWorld.OnTick`(시점), 등록부(조건·실행).
- 전제: [소스] `GameWorld.cs:263-284` OnTick 순서, `:270-271` `_maps.Values` foreach, `GameMap.cs:558` `MaybeRespawnBoss`가 플레이어 0이면 보스를 새로 스폰. [추론] 정리 직전 틱의 9단계에서 빈 보스방 복사본이 보스를 한 번 더 스폰하고(전역 번호 1개 소비) 바로 버려진다. 무해하지만 로그와 번호가 낭비된다(O-01). [미측정] 없음.
- 구분: 범위 안 기술 선택.

#### IM-04 정리된 복사본은 은퇴 표식을 갖고, 거기 들어온 job은 실행되지 않는다

- 보호 상태 집합: 세션이 들고 있는 「지금 맵」 참조(IM-09)와 그 맵의 job 큐.
- 진입·적용 시점: 정리(IM-03) 때 복사본에 은퇴 표식(이름은 P-05)을 박는다. 소켓 스레드가 세션의 지금 맵을 읽어 `EnqueueJob`을 부를 때, 은퇴한 맵이면 job을 버린다(로그 1회 또는 무음).
- 실패·중복·종료·늦은 도착: 늦은 도착이 핵심이다. 소켓 스레드가 지금 맵 X를 읽은 직후 틱 스레드가 X에서 플레이어를 빼고(이동 또는 끊김) 틱 끝에 X를 정리하면, 그 뒤 들어온 job은 X의 큐에 남아 영원히 실행되지 않는다. 지금 종류의 맵 job(이동 입력·공격·스킬·포탈)은 모두 `OwnsPlayer(map, eid)`로 시작하므로 실행돼도 no-op이지만, 실행되지 않는 쪽이 더 안전하다.
- 깨지는 반례: 미래에 세션 측 guard를 맵 job 안에서 푸는 코드가 생기면(지금 `_inventoryRequestPending`은 맵 큐가 아니라 등록부 큐를 쓴다 `GameSession.cs:575-600`), 버려진 job이 guard를 영원히 잡는다. 표식이 있으면 `EnqueueJob` 거부 시점에 호출자가 알 수 있다.
- 관찰할 시험: 플레이어가 복사본을 떠나 정리된 뒤, 시험 세션이 그 맵 객체를 지금 맵으로 들고 있게 하고 입력을 보낸다 → 맵의 `EnqueueJob` 호출 횟수(테스트 subclass spy 패턴, `GameMap.cs:169-170` virtual)가 늘지 않거나, 늘어도 다음 틱에 상태가 바뀌지 않는다. 유령 entity 없음은 모든 살아 있는 맵의 Players 합집합으로 본다(P-13의 열거자).
- 소유자: `GameMap`(표식·거부), 등록부(표식 설정).
- 전제: [소스] `GameSession.cs:557-561` GetMap은 `_migrating`만 보고 등록부를 읽음(초안은 객체 참조로 바꿈), `GameMap.cs:170` EnqueueJob은 무조건 큐에 넣음, `MapMigration.cs:53` `OwnsPlayer` 선검사, `GameSession.cs:269,297,329` 이동·공격·스킬 job도 `OwnsPlayer` 선검사. [추론] 큐를 비워 실행하는 대안은 죽은 맵에서 방송·상태 변경이 일어나므로 더 나쁘다. 버리는 쪽이 「실행되지 않거나 무해」를 만족한다. [미측정] 소켓 스레드와 정리 사이의 실제 경쟁 빈도는 모른다.
- 구분: 범위 안 기술 선택. 초안 불변식 5의 구체화.

#### IM-05 끊김 정리는 살아 있는 모든 복사본에서 플레이어를 빼고, 이동 중 끊김은 arrival job이 흡수한다

- 보호 상태 집합: 모든 복사본의 `_players`, 세션 `_entityId`·`_migrating`, 파티·경제 등록.
- 진입·적용 시점: `DrainSessionCloses`(틱 처음·맵 틱 뒤 두 번, `GameWorld.cs:269,272`)가 공용 맵 + 복사본 전부를 돈다. 이동 중이면 플레이어는 어느 맵에도 없고, `CompleteWorldLeave`(`GameSession.cs:188-192`)가 `_entityId=-1`·`_migrating=0`을 박는다. 뒤늦게 도는 arrival job은 `ReadClosing()==1`로 skip하며 -1(IM-02).
- 실패·중복·종료·늦은 도착: `OnDisconnected`는 `Interlocked.Exchange`로 한 번만 큐에 넣는다(`GameSession.cs:118-124`). 두 번 끊김은 두 번째가 무시된다(`SessionCleanupTests.cs:123-129` 기존 시험). 끊긴 세션이 들고 있던 「지금 맵」 참조는 그대로 두되(IM-09) 더 이상 쓰이지 않는다.
- 깨지는 반례: 복사본을 순회에서 빠뜨리면 끊긴 세션의 entity가 복사본에 남아 유령이 되고, 복사본은 플레이어 1로 영원히 정리되지 않는다(IM-15 위반). 서버가 시키는 이동 중 끊김도 같은 arrival job 경로를 타야 한다(IM-06).
- 관찰할 시험: 복사본 안에서 끊김 → 다음 틱 끝 복사본 0, 파티 해산 통보가 남은 멤버에게 감(기존 `SessionCleanupTests.cs:140-156` 구조를 복사본에서). 이동 중 끊김은 IM-02의 시험.
- 소유자: `GameWorld.DrainSessionCloses`.
- 전제: [소스] `GameWorld.cs:210-232`, `:216` 주석 「소켓의 라우팅 힌트를 신뢰하지 않는다」(전체 순회 의도), `GameSession.cs:573` `RequestWorldClose`. [추론] 세션이 지금 맵 참조를 들고 있어도 끊김 정리는 전체 순회를 유지하는 편이 안전하다(참조가 이동 중 옛 맵을 가리킬 수 있음). [미측정] 없음.
- 구분: 범위 안 기술 선택. 초안 불변식 6 그대로.

#### IM-06 서버가 시키는 이동은 포탈 이동과 같은 transfer 단계를 쓴다

- 보호 상태 집합: 포탈 이동이 보호하는 모든 것(IM-01·02·05·09)과 이동 패킷 순서.
- 진입·적용 시점: Content 처리기가 그 플레이어의 지금 맵 job 안에서 부른다(goal Content 계약 3). 검증만 다르고(IM-13) 검증 통과 뒤는 `MapMigration`의 transfer 단계 하나를 공유한다.
- 실패·중복·종료·늦은 도착: 이동 중 끊김은 IM-05. 두 번 호출은 두 번째가 거부(IM-13). 목적지 공용 맵이 등록부에 없으면 설정 버그 → 지금과 같은 끊기 경로(IM-14).
- 깨지는 반례: transfer를 따로 베끼면 패킷 순서(S_PlayerLeave → S_MapTransition → S_PlayerHp → roster → S_PlayerJoin, `MapMigration.cs:118-174`)나 경제 등록 유지가 갈라진다. 완료조건 5 「포탈 이동과 같은 맵 이동 패킷만」이 깨진다.
- 관찰할 시험: 복사본에서 서버 이동 → Town. 수신 패킷 ID 열이 포탈 이동과 같고 `S_PortalLocked`가 없으며, 이동 뒤 인벤토리 조회 revision·재화가 그대로, 파티 소속이 그대로(`world.Party.GetPartyByEntity`).
- 소유자: `MapMigration`.
- 전제: [소스] `MapMigration.cs:105-178` transfer 단계는 지금 Execute 안에 인라인, `00_Document/domains/server.md` 「경제 상태와 연결 수명」(이동 틈에도 등록 유지). [추론] 같은 이유(이동 수명)로 함께 바뀌는 로직이 두 진입점에 생기므로 추출 대상이다(CODE_CONVENTION 「함께 바뀌는 로직」). [미측정] 없음.
- 구분: 범위 안 기술 선택.

### 보호 집합

#### IM-07 (맵, 열쇠)마다 복사본은 최대 하나이며, 같은 틱의 두 번째 입장자는 첫 입장자가 만든 복사본을 찾는다

- 보호 상태 집합: 등록부의 (MapId, InstanceKey) → GameMap 항목.
- 진입·적용 시점: find-or-create는 출발 맵 job 안(틱 스레드)에서 즉시 등록부에 넣는다. 틱 끝으로 미루지 않는다.
- 실패·중복·종료·늦은 도착: 같은 틱에 같은 열쇠로 두 job이 오면(파티 둘이 같은 틱에 포탈) 두 번째 job은 찾는다(틱 스레드 하나라 경쟁 없음). 정리(IM-03)와 생성이 같은 틱에 겹치면 생성이 먼저이고 들어오는 중 ≥1이라 정리되지 않는다. 정리된 뒤 같은 열쇠로 다시 들어오면 새 복사본(새 적·새 번호)이다.
- 깨지는 반례: 생성을 틱 끝으로 미루면 같은 틱의 두 번째 job이 복사본을 못 찾아 둘째 복사본을 만들어 파티가 갈라진다(완료조건 2 위반).
- 관찰할 시험: 파티 두 세션이 같은 틱에 포탈 → 두 틱 뒤 살아 있는 복사본 1, 둘 다 그 안. 다른 파티 두 쌍 → 복사본 2, 서로의 Players에 없음, 적 entity 번호 집합이 서로소(완료조건 1).
- 소유자: 등록부.
- 전제: [소스] `GameMap.cs:236-241` 한 맵의 job은 같은 틱 안에서 순서대로 실행, `GameWorld.cs:270` 맵 틱은 틱 스레드 하나. [추론] 틱 스레드 하나이므로 잠금 없는 Dictionary로 충분하다(`PartyRegistry.cs:33-37`과 같은 전제). [미측정] 없음.
- 구분: 범위 안 기술 선택. 초안 불변식 1·10 그대로.

#### IM-08 틱 경계에서 entity 번호는 정확히 한 맵에만 있고, 이동 중에는 0개 맵에 있다

- 보호 상태 집합: 모든 살아 있는 맵의 `_players` 합집합.
- 진입·적용 시점: `RemovePlayer`(출발) → arrival job의 `AddPlayerWithId`(도착) 사이가 「0개」 구간이다. 지금과 같다.
- 실패·중복·종료·늦은 도착: `AddPlayerWithId`는 중복을 검사하지 않는다(`GameMap.cs:180-186`). 같은 번호가 두 맵에 들어갈 경로는 arrival job이 두 번 등록되는 것뿐이며 IM-13(두 번 호출 거부)과 `_migrating`이 막는다.
- 깨지는 반례: 서버가 시키는 이동이 `_migrating` 검사 없이 두 번 수락되면 arrival job 두 개가 Town에 같은 번호를 두 번 넣는다.
- 관찰할 시험: 매 틱 뒤 모든 살아 있는 맵의 Players를 모아 entity 번호 중복이 없음을 단정하는 helper를 선행 시험이 공유한다(P-13 `AllLiveMaps`). `PlayerEntity`는 맵 역참조가 없어 맵 쪽에서만 셀 수 있다.
- 소유자: `MapMigration`(순서), `GameWorld`(열거).
- 전제: [소스] `Entities/PlayerEntity.cs:79-81` EntityId·Owner만, 맵 참조 없음. `GameWorld.cs:30-40` 번호는 전역 발급. [추론] 없음. [미측정] 없음.
- 구분: 범위 안 기술 선택.

#### IM-09 세션의 「지금 맵」은 맵 객체 참조이고, 쓰는 쪽은 틱 스레드의 입장·도착 job뿐이다

- 보호 상태 집합: `GameSession`의 지금 맵 참조, `_migrating`.
- 진입·적용 시점: 최초 진입(`EnterGameWorld`, `GameSession.cs:496-554`)은 Town 공용 맵을 월드에서 한 번 받아 저장한다(지금은 `CurrentMapId` 초기값 Town으로 등록부를 읽음 `:65`, `:557-561`). 도착 job(`MapMigration.cs:153`)이 목적지 복사본 참조로 바꾼다. 읽는 쪽은 소켓 스레드의 `GetMap()`과 `OwnsPlayer`.
- 실패·중복·종료·늦은 도착: 단일 writer(틱) + 단순 대입이라 `Volatile.Read/Write`로 충분하다(지금 `_currentMapIdValue` 주석 `:57-71`의 전제 그대로, 값만 int → 참조). 이동 중에는 참조가 옛 맵을 가리키지만 `_migrating`=1이 먼저 보이므로 `GetMap()`은 null이다(`:559`). 정리된 옛 맵을 가리켜도 IM-04가 막는다.
- 깨지는 반례: 소켓 스레드가 등록부를 읽으면(지금 방식 유지) 틱 스레드의 Dictionary 생성·정리와 경쟁해 열거 예외나 찢어진 읽기가 난다. 초안 「만들 것 5」의 이유다.
- 관찰할 시험: 복사본 안에서 이동 입력 → 그 복사본의 Players 위치가 바뀜(공용 맵 아님). `MapMigrationTests`의 `GetMap()` override 세션들은 이미 맵 객체를 직접 들므로(`:70`, `:116`, `:151`, `:180`) 이 방향과 맞는다.
- 소유자: `GameSession`.
- 전제: [소스] `GameSession.cs:57-71` 동시성 가정 주석, `:206` `SetCurrentMapId`, `MapMigration.cs:153-154` 쓰기 순서(지금 맵 갱신 뒤 `_migrating` 해제). [추론] `S_MapTransition.destMapId`는 `map.MapId`로 채우면 되므로 int 번호 필드는 더 이상 필요 없다. [미측정] 없음.
- 구분: 범위 안 기술 선택. 초안 불변식 3·만들 것 5.

#### IM-10 열쇠는 (종류, 번호)이고, 입장 시점에 한 번 정해져 그 복사본 안에서는 바뀌지 않는다

- 보호 상태 집합: 복사본의 열쇠(맵이 읽기 전용으로 노출, P-05), 등록부 키.
- 진입·적용 시점: 공용 맵 → 인스턴스 맵 입장 때 그 틱의 파티 소속(`PartyRegistry.GetPartyByEntity`, `:150-154`)으로 정한다. 파티면 (Party, partyId), 아니면 (Solo, entityId). 인스턴스 → 인스턴스(사냥터↔보스방)는 출발 복사본의 열쇠를 그대로 쓴다. 인스턴스 → 공용은 열쇠가 없다.
- 실패·중복·종료·늦은 도착: 파티 결성·해산은 Party.Tick(맵 틱 뒤 `GameWorld.cs:274`)에서 적용되므로, 같은 틱에 들어온 파티 변경은 다음 틱 입장부터 반영된다(0~1틱 지연, 지금 처치 콜백의 지연과 같은 성질 `:243`). 복사본 안에서 해산·탈퇴·끊김이 일어나도 열쇠는 그대로다(초안 불변식 2). 파티 번호와 entity 번호는 다른 계수기(`PartyRegistry.cs:106`, `GameWorld.cs:116`)라 같은 값이 나올 수 있고, 종류 필드가 구분한다(초안 불변식 11).
- 깨지는 반례: 열쇠를 long 하나로 묶으면(상위 비트 종류) 로그·시험에서 읽기 어렵고 Content가 파티 번호를 꺼내 쓸 때 비트 연산이 필요하다. 열쇠를 매 틱 다시 계산하면 해산 순간 복사본이 「주인 없음」이 된다.
- 관찰할 시험: 파티 둘이 (HG, Party p)에 들어간 뒤 한 명이 탈퇴 → 둘 다 같은 복사본에 남아 있고 복사본 열쇠가 (Party, p) 그대로. 그 뒤 보스방으로 가면 (BR, Party p). 파티 번호 == entity 번호인 두 열쇠로 복사본 둘을 만들어 서로 다름을 단정(`InstanceKey` 값 비교).
- 소유자: `MapMigration`(결정), 등록부(저장), `GameMap`(노출).
- 전제: [소스] `PartyRegistry.cs:157-161` 소속 복사 조회, `GameWorld.cs:72` Quest가 같은 조회를 쓰는 선례. [추론] 열쇠 결정은 「이동」의 책임이라 `MapMigration`(또는 그 옆 helper)에 두고 등록부는 키 타입만 안다. [미측정] 없음.
- 구분: 기술 선택. 단, 「복사본 안에서 파티가 새로 생긴 경우」는 정책 입력이 필요하다(S-06).

#### IM-11 복사본의 적은 전역 발급기로 새 번호를 받는다(ADR-026)

- 보호 상태 집합: entity 번호 전역 유일.
- 진입·적용 시점: 복사본 생성자가 content의 스폰 포인트마다 `SpawnEnemy`(`GameMap.cs:110`) → `AllocId()`(`:572`) → `GameWorld.NextEntityId`(`:116`).
- 실패·중복·종료·늦은 도착: 정리된 복사본의 번호는 재사용하지 않는다(발급기는 단조 증가). 부활 대기열(`Maps/Systems/RespawnSystem.cs`, 맵 소유)은 복사본과 함께 버려진다.
- 깨지는 반례: 복사본 팩토리가 `idAllocator`를 빠뜨리면 로컬 번호(1부터)가 겹친다(`GameMap.cs:34-38`의 (B) 경로).
- 관찰할 시험: 복사본 둘의 `Enemies.Keys` 서로소, 플레이어 번호와도 서로소. 기존 `GlobalEntityIdTests` `:78-79`·`:129`는 「생성자에서 만든 HG·BR」 전제라 복사본을 만든 뒤로 바꾼다(시험 영향 표).
- 소유자: `GameWorld`(팩토리), `GameMap`(발급 사용).
- 전제: [소스] 위 줄. [추론] 없음. [미측정] 없음.
- 구분: 기술 선택. 초안 불변식 7.

#### IM-12 보스 처치 초기화는 그 복사본 열쇠의 진행만 지운다

- 보호 상태 집합: `QuestRegistry._soloProgress`·`_partyProgress`(`:12-13`). `_bossUnlocked`(`:14`)는 지금처럼 건드리지 않는다(`ResetAllQuestProgress` `:69-73`도 안 건드림).
- 진입·적용 시점: 처치 콜백은 복사본마다 만든 closure라(`GameWorld.MakeMap` `:237-261`) 열쇠를 캡처할 수 있다. 보스면 `_quest.EnqueueJob(() => 열쇠별 초기화)`. (Party, p) → `ForgetPartyProgress(p)`(`:76`) 재사용. (Solo, e) → `_soloProgress.Remove(e)`(새 메서드).
- 실패·중복·종료·늦은 도착: 열쇠가 (Party, p)인데 p가 이미 해산됐으면 `_partyProgress`에 없어 no-op이고, 탈퇴한 사람들의 개인 진행은 남는다(사용자 승인 해석 `msg_d0646fa471c6`). 열쇠가 (Solo, e)인데 e가 복사본 안에서 파티에 들어갔으면 e의 처치는 파티 진행에 쌓이고 초기화는 개인 진행만 지운다(정책 질문 S-06).
- 깨지는 반례: 콜백 시그니처를 `(killerId, target)`로 두고 killer의 현재 파티를 다시 조회하면, 해산 뒤 처치나 다른 파티원의 처치에서 열쇠와 다른 진행을 지운다. 열쇠 캡처가 맞다.
- 관찰할 시험: 파티 A가 (BR, Party a)에서 보스 처치 → Party a 진행 0, 파티 B 진행과 혼자 C 진행 그대로(완료조건 3). 혼자 D가 (BR, Solo d)에서 처치 → d 진행만 0.
- 소유자: `GameWorld.MakeMap`(열쇠 캡처), `QuestRegistry`(초기화 메서드).
- 전제: [소스] `GameWorld.cs:244-252`, `QuestRegistry.cs:69-76`, `OnPartyDisbanded` `:234-235`가 이미 같은 메서드를 큐에 넣는 선례. [추론] `ResetAllQuestProgress`는 제품 호출처가 이 한 곳뿐이라(grep 결과) 바꾸면 사용처 없는 메서드가 된다. 시험 4파일이 직접 부르므로 제거 여부는 시험 분류와 함께 정한다(S-04). [미측정] 없음.
- 구분: 기술 선택 + S-06 정책 경계.

### 오류 분류

#### IM-13 서버가 시키는 이동은 거부를 결과값으로 돌려주고 상태를 바꾸지 않으며, 검사 순서가 고정된다

- 보호 상태 집합: 세션 `_closing`·`_migrating`, 출발 맵 `_players`, 결과값.
- 진입·적용 시점: Content가 지금 맵 job 안에서 호출(틱 스레드). 검사 순서 = ① 세션 종료 중(`IsClosing`) → ② 이동 중(`_migrating`) → ③ `OwnsPlayer(currentMap, entityId)` 아니면 「그 맵에 없음」 → ④ 출발 맵이 인스턴스가 아니면 「인스턴스 맵 아님」 → ⑤ 목적지가 공용 맵 종류가 아니면 「목적지가 공용 맵 아님」. 모두 통과하면 transfer(IM-06)로 들어가고 「수락」을 돌려준다.
- 실패·중복·종료·늦은 도착: 같은 사람에게 두 번 부르면 첫 호출이 `SetMigrating(1)`·`RemovePlayer`를 했으므로 두 번째는 ②에서 거부(완료조건 5의 「두 번째는 거부」). 거부 경로는 로그만 남기고 아무 패킷도 보내지 않는다(포탈의 `S_PortalLocked` 같은 통보는 없음. Content가 자기 패킷으로 알린다).
- 내부 불변식 실패와의 구분: 목적지 공용 맵이 등록부에 없음(설정 버그)과 복사본 생성 예외는 거부값이 아니라 IM-14·IM-01의 경로다. 결과 열거형에 「내부 오류」 값을 두지 않는다. 호출자가 처리할 수 없는 상태를 거부처럼 보이게 하지 않기 위해서다.
- 깨지는 반례: 검사 순서를 ③ 먼저 두면 두 번째 호출의 거부 이유가 「맵에 없음」이 되어 Content가 「이미 이동 중」과 구분하지 못한다. 예외로 거부하면 `GameMap.Tick`의 catch(`:237-241`)가 삼켜 Content job의 나머지가 중단된다.
- 관찰할 시험: 완료조건 5의 셋(없는 사람·이동 중·종료 중 → 거부값, 상태 불변, 패킷 0건)과 두 번 호출. 공용 맵에 있는 사람에게 호출 → 「인스턴스 맵 아님」. 좌표 생략 → `S_MapTransition.spawnX/Y == Town.PlayerSpawnPosition`.
- 소유자: `MapMigration`(검사·결과), 결과 타입은 `Maps/Transitions/` 안.
- 전제: [소스] `MapMigration.cs:53` `OwnsPlayer` 선검사(포탈과 같은 ③), `GameSession.cs:194-195`, `:204-205` `SetMigrating`·`ReadClosing` internal hook, `InventoryRegistry.cs:135-136` 거부를 열거형으로 돌려주는 선례. [추론] `_migrating` 읽기 hook이 지금 없다(`SetMigrating`만 `:204`). 검사 ②를 위해 `ReadMigrating()` 같은 internal 읽기가 필요하다. [미측정] 없음.
- 구분: 기술 선택. 결과값 이름·인자는 P-08.

#### IM-14 「목적지 맵 없음 → 끊기」 경로는 공용 맵에만 남고, 인스턴스 종류에서는 도달 불가가 된다

- 보호 상태 집합: `MapMigration.cs:126-133`의 분기.
- 진입·적용 시점: 공용 맵 조회(`GetMap(MapId)`)가 null이면 지금처럼 `SetMigrating(0)` → `Disconnect()`. 인스턴스 종류는 find-or-create라 null이 없고, 생성 실패는 IM-01로 출발 맵 잔류다.
- 실패·중복·종료·늦은 도착: IM-01을 적용하면 이 분기도 `RemovePlayer` 앞으로 온다. 그러면 끊기 전에 플레이어가 출발 맵에 남아 있어 `DrainSessionCloses`가 정상 경로로 뺀다(지금은 이미 빠진 상태에서 끊는다. 결과는 같지만 「유령 없음」 근거가 Drain 하나로 단순해진다).
- 깨지는 반례: 인스턴스 종류에서 「없으면 끊기」를 유지하면 첫 입장자가 항상 끊긴다. 분기 자체는 공용 맵의 설정 버그용으로 남겨야 한다(`Ending`처럼 content가 없는 맵도 생성자에서 등록되므로 지금은 도달하지 않는다 `GameWorld.cs:78-84`).
- 관찰할 시험: 맵 종류 표에서 공용 맵 하나를 빼고 만든 시험 월드(가능하면)에서 포탈 → 끊김 1회, 모든 맵 Players 비어 있음. 기존 `MapMigrationTests`의 `GetDestMap → null` 시험(`ObserverSession` `:152`, `TransientTestSession` `:181`)이 이 경로를 이미 덮는다. hook 시그니처가 바뀌면 같은 단정을 새 hook으로 옮긴다.
- 소유자: `MapMigration`.
- 전제: [소스] 위 줄. [추론] 없음. [미측정] 없음.
- 구분: 기술 선택. 계약의 「복사본에서도 맞는지」 질문의 답: 공용 맵에는 그대로 맞고, 복사본에는 적용하지 않으며, 복사본의 새 실패(생성)는 끊기가 아니라 잔류가 맞다.

### 비용 상한

#### IM-15 틱 끝 기준 살아 있는 복사본 수 ≤ 월드에 들어온 세션 수

- 보호 상태 집합: 등록부 크기.
- 근거: IM-03이 매 틱 끝에 「플레이어 0 ∧ 들어오는 중 0」인 복사본을 모두 지우므로, 남은 복사본마다 플레이어 ≥1 또는 들어오는 중 ≥1이다. 한 세션은 한 순간에 한 맵의 플레이어이거나(IM-08) 한 번의 이동 중(IM-13 ②)이므로 복사본별 (플레이어 + 들어오는 중)의 합 ≤ 세션 수. 따라서 복사본 수 ≤ 세션 수.
- 전제: [추론] 위 산술. IM-02(계수 누수 없음)와 IM-05(끊김이 모든 복사본을 봄)가 깨지면 상한도 깨진다. [소스] `GameSession.cs:118-124` 끊김 한 번, `PartyRegistry.cs:17` 파티 2명(복사본당 플레이어 ≤2는 상한 증명에 필요 없음). [미측정] 상한 자체는 산술이지만 틱 시간은 IM-17.
- 관찰할 시험: 봇 두 파티 시나리오 끝에 복사본 0, 중간 최대 복사본 수 ≤ 접속 봇 수(서버 로그의 생성·정리 줄을 센다, P-09 로그).
- 구분: 기술 선택. 초안 불변식 12의 증명. 「수 상한을 따로 두지 않는다」는 승인 범위(하지 않을 것) 그대로.

#### IM-16 맵 틱 순회는 틱 시작 스냅샷이고, 순서는 「공용 맵 MapId 순 → 복사본 생성 순」이다

- 보호 상태 집합: `OnTick`의 순회 목록, 같은 틱 안의 맵 간 job 순서.
- 진입·적용 시점: `OnTick` 2단계에서 등록부가 스냅샷 리스트를 채운다(재사용 버퍼). 그 틱에 만든 복사본은 다음 틱부터 돈다(초안 그림 4 그대로).
- 왜 순서를 명시하나: 지금 Town → HG → BR → Ending은 `Dictionary` 삽입 순서에 기댄다(`GameWorld.cs:78-84`, 언어 명세는 열거 순서를 보장하지 않음). 시험이 「처치 job과 포탈 job이 같은 틱에 돌 때 Town이 먼저 도므로 HG 도착은 같은 틱」을 단정한다(`InventoryLifecycleRaceTests.cs:70-95`, 이름 LaterTickedMap/EarlierTickedMap). 복사본이 공용 맵 뒤에 오면 Town → 복사본 방향은 지금과 같은 「같은 틱 도착」이 유지된다. 복사본 → 복사본(HG↔BR)은 생성 순서에 따라 같은 틱 또는 다음 틱 도착이 되며, 이것도 지금의 HG → BR(같은 틱)과 BR → HG(다음 틱) 관계와 같다(HG가 먼저 생성되는 한).
- 깨지는 반례: 스냅샷 없이 `_maps.Values`를 돌면 포탈 job의 생성에서 `InvalidOperationException`. 순서를 명시하지 않으면 구현자가 `SortedDictionary`나 해시 순서로 바꿔 위 시험이 간헐 실패한다.
- 관찰할 시험: 두 복사본 사이 이동의 도착 틱을 단정하는 시험은 복사본 생성 순서를 fixture에서 고정한다. 순회 중 생성·정리가 예외 없이 끝남은 「같은 틱에 포탈 둘 + 끊김 하나」 시험으로 본다(완료조건 8).
- 소유자: `GameWorld.OnTick`·등록부.
- 전제: [소스] 위 줄, `GameWorld.cs:154,185,199,202,217`의 다른 다섯 순회는 맵 틱 밖(Quest/Party/Inventory Tick 또는 Drain)에서만 불린다(`Quest/QuestNotifier.cs:9-20`, `Party/PartyNotifier.cs`, `InventoryRegistry.cs:141`. `Register` `:31-32`는 입장 job 안에서 불리지만 등록부를 바꾸지 않는다). 그래서 스냅샷은 OnTick 순회 하나만 필요하다. [추론] 생성·정리가 맵 job 안에서만 일어난다는 전제가 바뀌면(예: Content가 Quest job에서 서버 이동을 부름) 다른 순회도 스냅샷이 필요하다. 서버 이동 호출 위치를 「지금 맵 job 안」으로 고정한 Content 계약 3이 이 전제를 지킨다. [미측정] 없음.
- 구분: 기술 선택. 초안 불변식 10의 구체화.

#### IM-17 전체 맵 조회 다섯 곳의 비용은 O(복사본 수 × 맵당 플레이어)이며 측정 전까지 역인덱스를 두지 않는다

- 대상: `SendToEntity` `:154`, `TryGetEntityClass` `:185`, `IsActiveSession` `:199`, `IsActiveEntity` `:202`, `DrainSessionCloses` `:217`.
- 근거: 복사본 ≤ 세션 수(IM-15), 맵당 플레이어 ≤ 2(파티)이므로 호출 한 번이 O(세션 수). 호출 빈도는 처치·파티 패킷·경제 등록·끊김마다 1회. 기존 주석(`:149-150`, `:180-181`)이 역인덱스의 동기화 부채를 이유로 순회를 택했고, 복사본이 생겨도 같은 이유가 유지된다.
- 틱 시간: 복사본마다 `GameMap.Tick` 9단계가 돈다. 빈 복사본(들어오는 중만 있음)도 한 틱 돈다. 50ms 예산(`98_Shared/GameData/Constants.cs:13-16`)과 PRD의 p99 10ms(`TickScheduler.cs:79-80` 주석)는 봇 두 파티 실행 때 `[Tick] 1초 메트릭` 출력으로 본다(goal 「고른 방식」 표의 측정 계획).
- 전제: [미측정] 복사본 n개일 때 틱 시간. 확인 방법 = 봇 두 파티(복사본 ≥4) 실행의 메트릭 출력을 근거 폴더에 저장하고 p99를 읽는다. 복사본 0개(지금)와 같은 봇 수의 값을 같은 명령으로 함께 남겨 비교한다. [추론] 두 파티 규모에서는 예산 안일 가능성이 크지만 수치는 없다. [소스] 위 줄.
- 구분: 기술 선택. 성능 변경은 측정 뒤(CODE_CONVENTION).

## 고정 제안 (설계 선택, 완성 코드 아님)

각 제안: 내용 → 대안 한 줄 → 영향.

- P-01 등록부 위치·이름: `Maps/InstanceMapRegistry.cs`, `internal sealed class InstanceMapRegistry`. 책임 = (MapId, InstanceKey) → 복사본 항목(맵 + 들어오는 중 계수), 찾기·만들기·틱 스냅샷 채우기·틱 끝 정리. 생성 자체는 `GameWorld`가 넘긴 팩토리(`Func<MapId, InstanceKey, GameMap>`)로 한다. 주변 관례: `PartyRegistry`·`QuestRegistry`·`InventoryRegistry`(「무엇 + Registry」, 틱 스레드 전용 Dictionary). 대안: `GameWorld` 안에 Dictionary 하나 더. 영향: `GameWorld`가 이미 세 등록부·발급기·틱 순서를 들고 있어 변경 이유가 또 늘어난다. 분리하는 쪽이 「상태·큐는 소유 컨테이너에」에 맞는다.
- P-02 열쇠 타입: `Maps/InstanceKey.cs`, `public readonly record struct InstanceKey(InstanceOwner Owner, int Id)`와 `public enum InstanceOwner { Party, Solo }`. 정적 생성 `InstanceKey.ForParty(int)`·`ForSolo(int)`. 주변 관례: `PlayerTransferState`(readonly record struct), `Portal`(record). 대안: long 하나에 종류 비트. 영향: Content가 파티 번호를 바로 읽을 수 있고 로그·시험이 읽기 쉽다. public인 이유는 Content 처리기와 시험이 읽기 때문(record라 쓰기는 불가).
- P-03 맵 종류 표: `Maps/MapKindTable.cs`, `public static class MapKindTable`의 `IsInstanced(MapId)`가 switch로 HuntingGround·BossRoom만 true. 주변 관례: `PortalTable.GetPortalsFor`의 switch. 대안: `MapId`에 attribute. 영향: 「맵을 한 줄로 더한다」(만들 것 1)를 switch 한 줄로 만족한다. `PortalTable`도 switch라 두 표가 나란히 읽힌다.
- P-04 맵 데이터 보관: `GameWorld`가 `provider`를 필드로 보관하거나 `MakeMap`을 `Func<MapId, InstanceKey?, GameMap>`로 묶어 등록부에 넘긴다. 근거: `GameWorld.cs:62-93`에 provider 필드가 없고 `Program.cs:11,16`은 한 번만 넘긴다. 대안: `MapDataLoader`를 복사본마다 다시 읽기. 영향: 틱에서 파일 I/O를 하게 되어 공학 조건 위반. 보관이 맞다.
- P-05 맵의 열쇠 노출: `GameMap` 생성자 인자 `InstanceKey? instanceKey = null`과 읽기 전용 속성 `public InstanceKey? InstanceKey { get; }`. 공용 맵은 null. Content 접근 = `map.InstanceKey`. 들어오는 중 계수는 등록부 항목에 두되, 은퇴 표식은 `EnqueueJob`이 봐야 하므로 `GameMap`에 `internal bool IsRetired`와 `internal void Retire()`(틱 스레드 전용) 한 쌍. 대안: 열쇠를 등록부 측 Dictionary로만 두고 Content가 월드에 물음. 영향: Content의 클리어 판정이 맵 객체 하나로 끝나는 쪽이 단순하다(Content 계약 2).
- P-06 세션 필드: `_currentMapIdValue`(int) → `GameMap? _currentMap`(Volatile). `SetCurrentMapId(MapId)` → `SetCurrentMap(GameMap)`. `GetMap()`은 `_migrating==1 ? null : Volatile.Read(ref _currentMap)`. 최초 진입은 월드의 Town 공용 맵을 생성자 또는 `EnterGameWorld` 첫 줄에서 받는다. `GetDestMap(MapId)` hook → `ResolveDestination(GameMap current, int entityId, MapId dest)`(목적지 복사본 또는 공용 맵을 돌려줌, 테스트 override 유지). 대안: int 번호 + 열쇠 두 필드를 세션에 두고 소켓 스레드가 등록부를 읽음. 영향: 등록부 동시 읽기 문제(IM-09 반례). 객체 참조가 맞다.
- P-07 `MapMigration` 분할: `Execute`(포탈 검증) → `ResolveDestination` → `Transfer(session, entityId, currentMap, destEntry, destSpawn)`. 순서는 IM-01. 대안: 지금 인라인 유지 + 서버 이동에 복사. 영향: 변경 이유가 같은 두 곳(IM-06).
- P-08 서버가 시키는 이동: `MapMigration.MoveToPublicMap(GameSession session, int entityId, GameMap currentMap, MapId destination, Vector2? spawn, Func<MapId, GameMap?> getPublicMap)` → `MapMoveResult` 열거형 `{ Accepted, SessionClosing, AlreadyMigrating, NotInThatMap, NotInInstanceMap, DestinationNotPublic }`. 좌표 null이면 `destMap.PlayerSpawnPosition`. 위치: `Maps/Transitions/MapMoveResult.cs`. 호출 계약: 틱 스레드, 호출자의 지금 맵 job 안. 대안: `GameWorld` 메서드로 두고 세션만 받음. 영향: 호출자가 이미 맵 job 안에 있으므로 맵을 받는 쪽이 「지금 맵에 있음」 검사를 호출자 맵과 묶어 준다. 최종 이름은 리드가 Content에 알릴 때 정한다(Content 계약 3).
- P-09 정리 위치·로그: `GameWorld.OnTick` 5단계에서 등록부의 정리 메서드(예: `RemoveIdle()`) 호출. 생성·정리마다 콘솔 한 줄(`[Map] instance <맵> <열쇠 종류> <번호> created|retired`). 대안: 로그 없음. 영향: 봇 두 파티 확인과 IM-15 관찰이 로그를 센다.
- P-10 순회 방식: 등록부가 `FillTickSnapshot(List<GameMap> buffer)`로 공용 맵 뒤에 복사본을 생성 순으로 채운다. `GameWorld`는 버퍼를 필드로 재사용한다. 대안: 매 틱 `ToList()`. 영향: 틱당 할당 하나 차이. 측정 전이라 어느 쪽이든 되지만 버퍼 재사용이 기존 「hot-path에서 LINQ 회피」 관례(`MapMigration.cs:56`)와 맞는다.
- P-11 전체 맵 조회: `GameWorld`에 `internal IEnumerable<GameMap> AllLiveMaps`(공용 + 복사본)를 두고 다섯 순회가 이것을 돈다. 대안: 역인덱스. 영향: IM-17.
- P-12 처치 콜백: `MakeMap(MapId id, InstanceKey? key)`가 closure에 key를 캡처. 보스면 `_quest.EnqueueJob(() => _quest.ResetProgressFor(key))`. `QuestRegistry.ResetProgressFor(InstanceKey)`는 Party → `ForgetPartyProgress`, Solo → `_soloProgress.Remove`. `ResetAllQuestProgress`는 제품 호출처가 없어지므로 제거하고 시험을 요구사항 출처와 함께 옮긴다(S-04). 대안: 유지. 영향: 사용처 없는 메서드(CODE_CONVENTION 「사용처 없는 hook 금지」).
- P-13 시험 접근점(internal. `InternalsVisibleTo`가 이미 있는지는 미확인이라 구현 때 `GameServer.csproj`를 본다): `GameWorld.AllLiveMaps`, `GameWorld.TryGetInstance(MapId, InstanceKey)`, `GameWorld.GetOrCreateInstance(MapId, InstanceKey)`, `GameWorld.LiveInstanceCount`. `GetOrCreateInstance`는 틱 스레드 의도이지만 `IsTickThread` 하드 검사는 두지 않는다. 시험은 스케줄러 없이 틱을 돌리므로 `_tickThreadId`가 0이라 검사가 항상 실패한다(`TickScheduler.cs:37`). `GetMap(MapId)`는 공용 맵만 돌려주고 인스턴스 종류는 null.

## TDD로 관찰하는 방법

선행 시험 작성자(신규 Opus)가 공개 경계에서 구현 전 실패를 볼 수 있는 자리:

| 불변식 | 공개 경계 | 구현 전 기대 실패 |
|---|---|---|
| IM-01 | 던지는 팩토리 월드 + Town 포탈 + 틱 | 지금은 팩토리 주입 자체가 없어 컴파일 실패 → 시험 뼈대가 API를 고정 |
| IM-02·03·15 | 두 파티 포탈 → `LiveInstanceCount`, 퇴장 뒤 0 | 컴파일 실패(접근점 없음) |
| IM-04 | 정리된 맵 객체에 입력 → `EnqueueJob` spy 수 | 지금은 정리가 없어 맵이 살아 있음 → 단정 실패 |
| IM-05 | 복사본 안 끊김 → 두 틱 뒤 복사본 0·파티 해산 통보 | 복사본 없음 → 컴파일 실패 |
| IM-07 | 다른 두 파티 → 복사본 2, 서로의 Players에 없음, 적 번호 서로소 | 지금은 같은 HG 하나 → Players에 서로 보임(완료조건 1의 직접 반례) |
| IM-08 | 매 틱 `AllLiveMaps` Players 번호 중복 0 | 접근점 없음 |
| IM-09 | 복사본 안 이동 입력 → 그 복사본 위치 변화 | 복사본 없음 |
| IM-10 | 탈퇴 뒤 같은 복사본·같은 열쇠, 번호 같은 두 열쇠 구분 | 열쇠 타입 없음 |
| IM-12 | 파티 A 보스 처치 → B·C 진행 유지 | 지금 `ResetAllQuestProgress`가 전부 지움 → 단정 실패 |
| IM-13 | 완료조건 5의 거부 셋·두 번 호출·좌표 기본값 | 진입점 없음 |
| IM-14 | 공용 맵 없음 → 끊김(기존 시험 이전) | 기존 통과 유지 |
| IM-16 | 같은 틱 포탈 둘 + 끊김 → 예외 없음, 다음 틱 도착 | 지금은 등록부가 안 바뀌어 통과(보호 시험으로 남김) |

측정값 위장 금지: IM-17의 틱 시간은 시험이 아니라 봇 실행 로그로만 적는다. 복사본 수 상한 시험은 「≤ 세션 수」를 제품 계산으로 다시 만들지 말고 구체 수(예: 세션 4 → 복사본 ≤ 4, 퇴장 뒤 0)로 단정한다.

## 시험 영향: 조회 방식 변경 제안

행동 단정은 그대로 두고 「맵을 어떻게 얻는가」만 바꾼다. 분류는 task-context의 (a) 옛 구현 세부 단정이며 요구사항 출처는 goal 「만들 것」 1·3과 완료조건 6이다.

| 위치 | 지금 | 제안 | 단정 보존 |
|---|---|---|---|
| `Items/InventoryLifecycleRaceTests.cs:82,293`, `Items/InventoryMigrationLifetimeTests.cs:328`, `Items/InventoryWireContractTests.cs:326` | 포탈로 보낸 뒤 `GetMap(HuntingGround)` | 포탈로 보낸 뒤 `TryGetInstance(HuntingGround, InstanceKey.ForSolo(entityId))` 또는 세션의 지금 맵 | 「도착했다」 단정 그대로. 열쇠가 Solo인 이유를 fixture 주석에 적음 |
| `Maps/GameWorldRegistryTests.cs:40,55,63` | 네 맵 모두 non-null | 공용 둘 non-null, 인스턴스 둘 null, `LiveInstanceCount==0` | 「등록부 골격」 단정이 요구사항(만들 것 1)으로 바뀜. 요구사항 출처 명시 |
| `Maps/GlobalEntityIdTests.cs:78-79,129` | 생성자가 만든 HG·BR의 적 번호 1·2 | `GetOrCreateInstance` 둘을 만든 뒤 적 번호 서로 다름·단조 증가 | 「전역 유일·순서」 단정 그대로, 구체 값 1·2는 (a) |
| `Maps/SessionCleanupTests.cs:88,108,254` | `GetMap(HuntingGround)!` | 포탈로 보낸 세션의 복사본을 `TryGetInstance`로 | 그대로 |
| `Network/BroadcastTests.cs:81` | HG를 fixture 맵으로 | `GetOrCreateInstance(HuntingGround, 임의 열쇠)` 또는 `new GameMap(MapId.HuntingGround)` 단독 생성 | 방송 단정은 맵 객체 하나면 되므로 그대로 |
| `Party/GameWorldPartyIntegrationTests.cs:130`, `Party/QuestKillCountTests.cs:34`, `Party/QuestNotificationContractTests.cs:78` | HG를 fixture 맵으로 | `GetOrCreateInstance` 또는 단독 `new GameMap` | 그대로. 단 `QuestKillCountTests` 3번(`:141-159`)과 `:180`은 S-04 |
| 누락 6곳: `Maps/SessionCleanupTests.cs:43`, `Maps/GameWorldRegistryTests.cs:40`, `Integration/ServerFixtureLifecycleTests.cs:85`, `Network/ServerHostLifecycleTests.cs:87`, `Items/InventoryMigrationLifetimeTests.cs:343`, `Party/QuestNotificationContractTests.cs:52` | `Enum.GetValues<MapId>()` 전부(또는 인자 mapId)에 `GetMap(id)!` | `AllLiveMaps`로 바꿈(「모든 맵에 플레이어 없음」 단정은 복사본까지 포함해야 오히려 강해진다) | 그대로 또는 강화 |
| `GetDestMap` override 4파일: `Maps/BossPortalGateTests.cs`, `Maps/MapMigrationTests.cs`, `Maps/MapPublicationContractTests.cs`, `Network/EnterPortalHandlerTests.cs` | `GetDestMap(MapId)` | 새 hook 이름·인자로 | 단정 그대로. `MapMigrationTests` 13번은 S-02 |

## 범위 영향 (승인 범위·완료조건과 어긋날 수 있는 것)

- S-01 완료조건 6의 「16곳(10파일)」은 리터럴 grep 결과이고, 동적 조회 6곳(위 표 「누락 6곳」. 파일로는 `Integration/`·`Network/`의 2파일이 새로 늘어 12파일)을 더해야 전수 분류 표의 모집단이 맞다. 「건드릴 곳」의 시험 폴더 목록(`Maps/`·`Integration/`·`Party/`·`Items/`·`Network/`)은 이미 이 파일들을 포함하므로 폴더 범위는 그대로다. 수와 파일 수만 goal에서 고친다. → 리드가 goal 보완.
- S-02 IM-01의 순서 변경은 `MapMigrationTests` 13번(`:675-720`)의 결정론 hook(「RemovePlayer 직후 = GetDestMap」)을 무효로 만든다. 같은 상황(제거 직후·도착 전 끊김)을 재현하려면 hook을 「목적지 확보 뒤·제거 직후」로 옮긴다(예: transfer 단계가 부르는 테스트용 virtual 세션 hook). 시험 수정은 (a)이고 요구사항 출처는 이 파일 IM-01. → 「건드릴 곳」 안이지만 goal 위험 1·완료조건 6에 명시 권고.
- S-03 `GetDestMap` hook 시그니처 변경은 4 시험 파일을 건드린다. 범위 안(`Maps/`·`Network/`)이지만 16곳 목록에 없으니 goal에 적는다.
- S-04 `ResetAllQuestProgress` 제거(P-12)는 시험 4파일(`Party/QuestKillCountTests.cs:141-159,180`, `Party/QuestMembershipLifecycleTests.cs:81,115`, `Party/QuestNotificationContractTests.cs:266`)을 바꾼다. 유지하면 제품 사용처 없는 메서드가 남는다. 어느 쪽이든 「보스 처치 → 전부 초기화」를 단정하는 시험은 만들 것 7·사용자 결정 `msg_d0646fa471c6`을 출처로 바뀐다. → 리드 결정(기술 선택). 추천: 제거.
- S-05 `GetMap(MapId)`가 인스턴스 종류에 null을 돌려주는 선택(P-13)은 `GameWorldRegistryTests`의 「4맵」 단정을 바꾼다. 대안은 템플릿 맵을 남기는 것인데, 템플릿이 매 틱 돌고 적을 들고 있어 완료조건 1(서로 보지 않음)의 근거를 흐린다. → 추천 null.
- S-06 정책 입력 필요(메인·사용자). 이 검토는 확정하지 않는다.
  1. 각자 혼자 복사본에 있는 두 사람이 그 안에서 파티를 맺으면, 초안대로면 마을로 나갔다 다시 들어와야 만난다. 「만들 것 3」(복사본 안 이동은 지금 열쇠 승계)과 초안 불변식 2가 그렇게 읽힌다. 기본값으로 둘지 확인.
  2. 혼자 열쇠 복사본에서 파티에 든 사람이 보스를 잡으면 초기화는 본인 개인 진행만 지우고(열쇠 기준), 그 사이 쌓인 파티 진행은 남는다. 사용자 승인 해석(「복사본 들어갈 때의 파티 기준」)의 대칭 사례라 같은 기준으로 두면 되지만 확인.
  3. 마을에서 파티원이 이미 혼자 복사본에 들어가 있는데 다른 파티원이 뒤늦게 들어오면 (파티 열쇠) 복사본이 새로 생겨 둘이 갈린다. 1번과 같은 성질.

## 설계 관찰(비차단)

- O-01 `MaybeRespawnBoss`(`GameMap.cs:555-564`)는 플레이어 0이면 보스를 새로 스폰한다. 복사본이 비는 틱의 9단계에서 한 번 스폰하고 그 틱 끝에 정리되면 전역 번호 하나와 로그 한 줄이 낭비된다. 들어오는 중이 있는 빈 복사본에서는 오히려 지금 의도(빈 방에 새 보스)와 같다. 대안: 은퇴 예정 판단을 9단계에 넣지 않는다(정리는 틱 끝 한 곳 원칙). 영향 없음. 후속.
- O-02 등록부 항목(맵 + 계수)과 맵의 은퇴 표식이 두 객체에 나뉜다(P-05). 한 객체에 모으려면 `GameMap`이 계수를 들어야 하는데 그러면 맵의 변경 이유가 「복사본 수명」까지 늘어난다. 지금 제안이 책임 경계에 맞다고 보지만, 구현자가 읽기 쉬운 쪽을 고를 수 있다. 현재 범위.
- O-03 `GameWorld.Map`(Town 호환 속성 `:98`)은 그대로 둬도 된다. `GetMap(MapId.Town)`과 중복이지만 변경 이유가 다르지 않으므로 이번 범위에서 정리하지 않는다.
- O-04 30초 가독성: 처음 읽는 사람이 「복사본은 누가 만들고 누가 지우나」를 `InstanceMapRegistry` 파일 머리 주석 한 단락과 `GameWorld.OnTick`의 5단계 주석에서 찾을 수 있어야 한다. 「열쇠는 언제 정해지나」는 `MapMigration.ResolveDestination` 주석. 이 세 자리 외에 주석을 늘리지 않는다.
- O-05 `IsTickThread` 하드 검사를 등록부에 두지 못하는 이유(시험이 스케줄러 없이 틱을 돌림)는 등록부 주석에 한 줄 남긴다. 후속으로 「스케줄러가 돌 때만 검사」를 둘 수 있다.
- O-06 복사본 생성·정리 로그(P-09)는 봇 확인의 근거라 출력 형식을 시나리오 검사에서 파싱하기 쉽게 고정한다(맵 번호·열쇠 종류·번호·created/retired).

## 미검토·해당 없음·미실행

- 미실행: 빌드·서버 시험·봇·Unity·DB·패킷 생성기. 성능 수치 없음(IM-17은 [미측정]). 이 검토는 읽기 전용이며 실행 성공·backend 모델을 주장하지 않는다.
- 미검토: `99_Tools/headless-bot/`(봇 시나리오 등록·검사 방식), `Maps/MapPacketPublisher.cs` 내부, `Network/Session.cs` 송신 큐, Content의 던전 폴더, `Maps/Systems/` 각 시스템 본문(적 AI 등이 맵 밖 상태를 읽지 않는다는 전제는 `GameMap.cs:50-58` 주석과 시스템이 `this`를 받는 호출(`:246-271`)만으로 봤다). `InternalsVisibleTo` 존재 여부.
- 해당 없음: 클라이언트·공유 프로토콜(변경 0이 완료조건 7이고 `S_MapTransition`은 맵 번호만 보낸다 `MapMigration.cs:157-163`), DB·영속화.
- 초안 불변식 1~12 대조: 1→IM-07, 2→IM-10, 3→IM-03·09, 4→IM-02·03, 5→IM-04, 6→IM-05·17, 7→IM-11, 8→그대로(`HandlePlayerDeath` `:449-457`가 같은 맵 안 부활, 추가 지적 없음), 9→그대로(재접속은 새 세션·새 번호라 옛 복사본은 비면 정리됨), 10→IM-16, 11→IM-10, 12→IM-15. 초안에 없던 것: IM-01, IM-02(감소 짝), IM-04(은퇴 표식), IM-13(검사 순서·결과값), IM-14, IM-16(순서 명시), P-04(맵 데이터 보관), S-01(누락 6곳).

이 검토는 리드의 채택·보류와 메인 원문 확인의 입력이며 구현 발행 승인이 아니다.

## 자기 쓰기 감사

- 이 세션이 쓴 경로: `01_Phases/goals/2026-10-10-instance-map-lifecycle/goal-review.md` 하나. 첫 쓰기 = 맥락 메모(51줄, Bash heredoc), 둘째 쓰기 = 메모 + 검토 본문(Write 도구, 메모 본문은 그대로이고 「준수 연결」 표의 「완료 뒤 실제」 열과 메모 작성 시점 줄의 쓰기 순서 설명만 완료 갱신). 그 사이 큰 heredoc 한 번은 셸 파싱 실패로 아무것도 쓰지 않았다(`wc -l` 51로 확인).
- 허용 밖 쓰기: 없음. 지정 TEMP `.backups/tmp/f1` 아래에도 파일을 만들지 않았다. 기본 TEMP·`/tmp`·홈에 쓴 것 없음(쓰기 명령을 쓰지 않았고, Orca 명령의 receipt는 Orca가 자기 저장소에 남긴다).
- 확인 방법: 완료 직전 `git status --short`가 이 파일 하나만 `??`로 보임.
- 실행한 명령: `git status`·`git rev-parse`·`cat`·`sed`·`grep`·`awk`·`ls`·`wc`·`date -u`·`orca orchestration send/check`뿐.
