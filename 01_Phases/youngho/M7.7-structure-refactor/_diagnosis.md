# M7.7 구조 리팩토링 — 진단 (계획 입력)

> **상태**: 계획 전 진단. behavior-invariant 리팩토링 마일스톤, **pre-M8**(DB 영속화를 깨끗한 토대 위에 올리기 위함).
> **출처**: 2026-06-20 병렬 진단 3영역(server / shared+tools / client) + Rookiss 레퍼런스 비교(`Dotnet_MMO_Reference`).
> **근거**: 全 항목 `file:line` 실측. 이 문서 + 영호 Architecture Visualizer(Codex) 자료를 `/work:plan` 입력으로 사용.
> **불변 원칙**: 게임 동작 0 변경, 기존 테스트(WSL2 회귀)가 안전망, 각 Phase 독립 검증·되돌리기.
> 헌법 정합: `future-solid-refactor-ultracode` + `future-maps-namespace-restructure` memory를 정식화.

전반 인상: **구조의 큰 뼈대는 건강함**(의존 방향 단방향, 저수준 Network가 게임 로직 0참조, 헌법 "서버 권위·클라=렌더러" 위반 없음). 문제는 *망가진 것*이 아니라 **"개념이 한 곳에 안 모여 확장 시 여러 곳을 동시에 건드려야 하는"** 곳들. 레퍼런스가 깔끔했던 이유 = ① 정의(데이터) ↔ 처리(로직) 분리 ② 폴더=개념=네임스페이스 일치.

---

## Part 1. 확장 위험 (종속성·개념 뒤섞임)

### 🔴 1. "추가 = 산탄총 수술" (서버·클라 약점 교차)
- **새 스킬**: 서버 😀 깔끔(ActionGate/ActionRegistry 전략패턴, 파일1+등록1줄, OCP). 클라 😟 **4~5파일+switch3**: `LocalPlayerInput.SkillKeyMap`(:42-47)+`TrySendSkill` switch(:141-205) → `LocalPlayerMovement` 타이머/임펄스 → `SkillCastHandler` switch(:65-91) → 이펙트 경로 상수.
- **새 적**: 서버 😟 **`EnemyKind` switch ~10곳 산발** — `GameMap` maxHp 매핑이 `:89-95`와 `:427-433` **두 번 중복**, 사망/리스폰 분기 `:439,482,490,631,695`, `EnemyEntity` 쿨다운/State/면역 분기 `:33,38,169`, `EnemyAISystem:22`. 클라 🙂 데이터화(EnemyVisualTable)됐으나 사운드/이펙트 kind switch 잔존(`EnemyAttackHandler:130-143`).
- → **스킬·적 "정의"를 데이터(카탈로그/SO)로** = 양쪽 산탄총 동시 감소. 서버 스킬이 선례.

### 🔴 2. GameMap = God class (706줄, 7책임) — M8 직격
컨테이너여야 할 `GameMap.cs`가: ①엔티티 컨테이너 ②틱 9단계 오케스트레이션(:215-336) ③플레이어 물리 적분(:234-291) ④적 중력(:651) ⑤사망/부활(:477,502,691) ⑥**네트워크 브로드캐스트/직렬화**(S_Snapshot 직접 조립 :302-313, SendPlayerHp:531, SendInitialRosterTo:552) ⑦지형 쿼리(:125,352). M8 영속화 훅도 결국 여기 얹혀 800줄 됨. **시뮬(틱/물리) ↔ 표현(wire format) 분리**가 핵심 — 그래야 M8이 시뮬 레이어에 붙음. 레퍼런스 `GameRoom` partial(Battle/Item) 분할과 동방향.

### 🟡 3. 엔티티 분산 + 공통 베이스 부재
`PlayerEntity`(Maps/, NS `...Maps`) vs `EnemyEntity`(Combat/, NS `...Combat`). 공통 베이스(레퍼런스 `GameObject`) 없어 HP/사망/위치 **중복 정의** — 위치 표현마저 다름(Player `Vector2 Position` :69 / Enemy `float X,Y` :46-47). M8 "엔티티 저장" 추상화하려면 공통 베이스 선행 필요.

### 🟡 4. PlayerEntity: "저장 vs 휘발" 경계 없음
한 클래스에 저장 대상(`Hp`/`Position`/`Stats`) + 저장불가 휘발(`_inputQueue`/`_posHistory` 링버퍼/`ActionFsm`/`_jumpBufferRemaining` :28,47,134) 혼재. M8 스냅샷 DTO 골라내려면 경계 선행. `MapMigration`의 `capturedStats/capturedHp`(:107-108)가 스냅샷 청사진.

### 🟡 5. CombatBootstrap 단일 wiring 병목 (클라)
전역 시스템 10종을 한 `Awake()`(:53-64) 수동 조립 + 씬 화이트리스트 하드코딩(:35) → 새 시스템 = 이 파일 수정 강제.

---

## Part 2. 폴더·네이밍 정합 (핵심 병 = 폴더 ≠ 네임스페이스)

- **"Network" 3중 오버로드 (서버)**: `02_Server/Network/`(전송계층) vs `02_Server/GameServer/Network/` — 후자는 폴더=Network인데 `GameSession` NS=`...Sessions`(:14), `MapMigration` NS=`...Network`(:9) **같은 폴더 다른 NS**. 게다가 `MapMigration`은 네트워크 아니라 **존 이동 로직** = 위치 오분류.
- **폴더≠NS 광범위**: 서버 `Maps/Systems/*` 6파일 전부 NS `Maps`(Systems 아님). 클라 핸들러 23개 7서브폴더인데 NS 전부 평평한 `Dawnholder.Client.Network`.
- **"Combat 폴더인데 전투 로직 없음"**: `Combat/`엔 EnemyEntity·CombatConstants·Hitbox만, 전투 로직(CombatSystem/Actions)은 `Maps/`에.
- **상수 양분 경계 샘**: `Combat/CombatConstants.cs`(209)가 `Shared.GameData.Constants.AttackCooldownTicks`를 재import해 파생(:31,35,89,103,116) = 쿨다운이 Shared 원본+CombatConstants 미러 양쪽 존재.
- **98_Shared/GameData 14파일 평면**: 하위그룹 후보 — `Enums/`(ActionKind,AnimState,EnemyKind,HitEffect,SkillId +Protocol/CharacterClass) / `Map/`(MapDataFile,MapContent,Terrain) / `Combat/`(Formulas,PlayerStats,SkillCatalog) / 루트 코어(Constants,Physics,InputBits). enum 6개 흩어짐(CharacterClass만 Protocol/ — 비대칭).
- **PDL 원본 발견성**: 정의 `99_Tools/PacketGenerator/PDL.xml`(417), 생성물 `98_Shared/Protocol/Generated/GenPackets.cs`(2490). 헌법은 `98_Shared/Protocol/`을 신성시하나 원본은 도구 폴더 깊숙이.
- **봇 Scenarios 17개 평면 + ProbeBase 부재**: 17개 `*Probe`가 connect→handshake→CharacterSelect→EnterMap + WaitUntil을 각자 재구현(`BossFightSmoke:192-535` 대표). 수백 줄 중복. `Program.cs`(312) 거대 if-체인 디스패치.
- **네이밍 혼재**: UI 접미사 4종(Controller/Panel/UI/Popup/Hud) · `IClientPacketHandler` vs `IPacketHandler` · 봇 `*Smoke` vs `*Scenario`(DashSmokeScenario는 둘 다) · `PacketGenerator` NS가 csproj RootNamespace(`Dawnholder.Tools.PacketGenerator`)와 불일치(소스 `namespace PacketGenerator`).
- **문서 드리프트**: `98_Shared/CLAUDE.md:19` ProtocolVersion v15인데 실제 `:81` v16 · 헌법 Repo Layout 다이어그램에 04_ClientNet 누락.

**깨끗해서 손 안 댈 곳**: 소켓 복제(04_ClientNet↔02_Server/Network, ADR-012 의도된 자매구현) · 루트 00~99 레이아웃 · 패킷 dispatch 테이블 대칭(클·서버 둘 다 모범).

---

## Part 3. Phase 거친 스케치 (plan-auditor가 다듬음 — 6개 골격)

| Phase | 내용 | 위험 |
|---|---|---|
| P01 | 저위험 정합 — 문서 드리프트(v16·04_ClientNet) + NS 통일(PacketGenerator, 클라 핸들러 NS=폴더) | 낮음 |
| P02 | 봇 하니스 — ProbeBase 추출 + Scenarios 서브폴더 + Smoke/Scenario 네이밍 | 최저(테스트 도구) |
| P03 | 98_Shared 개념 그룹화 — GameData 하위폴더 + enum 모으기 | 낮음(NS 유지 시 폴더만) |
| P04 | **GameMap 분해(broadcaster 추출) + 엔티티 공통 베이스/경계** (M8 전제 핵심) | 중(순수 추출, 테스트 안전망) |
| P05 | 폴더·NS 재편 — "Network" 오버로드 해소 + Combat/Maps + UI 접미사 컨벤션 | **높음**(frozen 참조) |
| P06 | 데이터화 — EnemyKind + 스킬(산탄총 근본 제거, OCP) + 회귀 마감 | 중 |

**시퀀싱 원칙**: 저위험·독립 먼저(P01·P02) → 개념 그룹화(P03) → M8 직결 핵심(P04) → 고위험 이동(P05, frozen 참조 신중) → 근본 데이터화(P06). �separator: P05가 가장 조심(파일 이동이 frozen `-DONE`·CODEOWNERS 참조 깨뜨림 = memory `project-reorg` 교훈).

**핵심 원칙 2개**: A) 폴더=개념=네임스페이스 일치 / B) 정의는 데이터로·로직은 데이터를 읽기만.

---

## Part 4. M8 이월 메모 (이번 마일스톤 범위 밖, DB 영속화 때 참고)

레퍼런스(Rookiss) DB 패턴에서 M8에 차용/회피:
- **차용**: ① 전용 DB 스레드 + Job 큐 + "Me→You→Me" 핑퐁(게임스레드 스냅샷→DB스레드 SaveChanges→게임스레드 콜백, `DbTransaction.cs`) ② 부분 컬럼 UPDATE 트릭(`EntityState.Unchanged` + `Property.IsModified` → SELECT 없이 단일 컬럼, race 회피) ③ DB POCO(`PlayerDb`) ↔ 런타임 객체 분리 + 정적 데이터는 JSON(우리 98_Shared 정합).
- **회피(레퍼런스 함정)**: ① 계정 테이블 이원화(AccountDB↔GameDB 동기화 보장 없음) → 우리는 **계정 SSOT 1곳** ② 게임서버 토큰 검증 미배선(임의 문자열로 계정 자동생성) → 우리 §1/§3상 **TCP 로그인 게이트에서 신원 검증 필수** ③ 비번 평문·토큰 `Random().Next(int)`·`Expired.AddSeconds` 반환무시 버그 → **해시+salt·암호학적 난수·만료 정확히** ④ 주기 스냅샷 cadence 없음(leave 시 HP만) → 우리 헌법 "30초+이벤트" cadence 유지. ⑤ `PlayerServerState`(Login→Lobby→Game) 상태머신은 좋은 차용.
- **우리 규모(학부생 단독)**: 별도 AccountServer 없이 단일 LocalDB에 계정1+캐릭터N(FK)로 시작. 분리는 부하 증명 후.
