# Dawnholder Code Convention v6.1

> **우리가 채택한 규칙**의 단일 진실. 책 이론·함정은 [`refs/`](refs/) 참고서(GPP 19 + 교과서 10)로 분리했다(섞지 않음). 작업별 라우팅 진입점은 [`INDEX.md`](INDEX.md).
> 모든 SubAgent는 코드 작성 *전* `INDEX.md`에서 작업 유형을 찾아 본 문서의 해당 규칙 + refs를 참조한다 (강제 = §5).
> **우선순위**: 헌법(`CLAUDE.md`) > 본 문서 > refs. 
> **성격**: 이상적 도착점 — 기존 코드를 정당화하지 않고 본 기준으로 측정·리팩토링한다 (현재 갭 = 부록 A).

---

## 0. 철학 (4원칙)

- **[0.1] 기반 부채 방지** — 애매한 규칙은 안 지켜지고, 그 위 코드가 또 부채가 된다. 그래서 본 문서는 *결정적*이다: "되도록 잘"이 아니라 "언제 이렇게 / 언제 저렇게"를 못 박는다.
- **[0.2] 좋은 코드 = 변경이 쉬운 코드** = 읽는 사람이 뇌에 담을 지식이 최소인 코드. 디커플링의 목적은 그 뇌 부담 감소(장식이 아니다).
- **[0.3] ⚠️ 과한 추상화도 부채다** — YAGNI: 호출자 0개인 확장 hook 금지. 단일 책임을 억지로 쪼개지 마라. 분리 규칙(§2.2)은 "항상 쪼개라"가 아니라 *균형*이다. ("완벽 = 더할 게 없을 때가 아니라 뺄 게 없을 때")
- **[0.4] 현 우선순위** — **장기 개발속도 > 단기 구현속도 > 실행속도** (발표 전 MVP, 동시 ≤4명이라 성능 여유). 성능 최적화(Object Pool / Spatial Partition)는 *측정 후*에만.
- **[0.5] 추상 SOLID 대신 패턴 카탈로그** — SRP는 §2.2(God class 분리), OCP/DIP는 Component/Service Locator로 *구체화*해서 적용한다. 전체 카탈로그 → [`refs/game-programming-patterns/_index.md`](refs/game-programming-patterns/_index.md).

---

## 1. 서버 시스템 설계

> 근거 이론 → [`refs/game-server-programming/_index.md`](refs/game-server-programming/_index.md)

- **[1.1] 동시성 = Map = Actor** — 한 맵 = 단일 스레드. 맵 내부 `lock`/`Monitor` **금지**. 외부 스레드 → 맵 진입은 `EnqueueJob`만. 맵 간 통신은 message channel(직접 호출 X). *근거: 교과서 "방 단위 잠금"* → [`01-multithreading`](refs/game-server-programming/01-multithreading.md)
- **[1.2] 콘텐츠 / 엔진 분리** — 엔진(`02_Server/Network/`·`04_ClientNet/`·PDL·`98_Shared/GameData/Physics`)은 게임 규칙을 모른다(단방향 의존). 게임 규칙을 인프라 레이어에 섞지 마라. → [`06-proudnet`](refs/game-server-programming/06-proudnet.md)
- **[1.3] tick 루프 blocking 금지** (헌법 #5) — tick 안 `await`/`Thread.Sleep`/동기 DB/`Task.Run` 금지. 시간 기반 동작은 tick 카운트다운으로(예: `RespawnTicksRemaining--`). → [`01-multithreading`](refs/game-server-programming/01-multithreading.md)
- **[1.4] 레이턴시 마스킹** (헌법 #1) — 클라는 prediction/보간으로 지연을 가리되 권위는 항상 서버(reconcile 검증). enemy/remote 위치는 클라가 보간만(서버 좌표 변형 X). → [`05-game-networking`](refs/game-server-programming/05-game-networking.md)

---

## 2. 코드 구조

- **[2.1] 패턴 카탈로그** — 전체 = [`refs/game-programming-patterns/_index.md`](refs/game-programming-patterns/_index.md).
  - *이미 쓰는 것*: State(`EnemyState`), Update Method(`GameMap.Tick`), Game Loop(`TickScheduler`), Singleton(`GameWorld` — readonly만), Flyweight(`MapSpawnTable`).
  - *채택 후보*: Component/Service Locator(God class 분리), Observer/Event Queue(디커플링), Object Pool·Spatial Partition(성능, 측정 후).

- **[2.2] ★ God class 분리 (결정적 기준)** — 가장 중요한 규칙. (GPP Component → [`13-component`](refs/game-programming-patterns/13-component.md))
  - **트리거**: 한 클래스가 **2개 이상 도메인**(전투+AI+네트워크 등)을 가지면 분리. (줄 수는 신호일 뿐 — §2.3)
  - **구조**: **컨테이너**(상태 + tick 엔진 + actor 경계 — `GameMap`/`GameSession`/MonoBehaviour, *남긴다*) + **System**(로직만 — `CombatSystem` 등, 컨테이너를 인자로 받음).
  - **통신**: 공유 상태(System이 컨테이너의 데이터를 읽고 변경). System끼리 직접 호출 X. 데이터 소유 = 엔티티/컨테이너, System은 *변경만*.
  - **호출 규율**: System은 컨테이너의 tick 스레드 안에서만 호출(§1.1). tick에 System 실행 *순서 명문화*.
  - **❌ 과분할 경계**(§0.3): 단일 도메인인데 줄 수만 긴 건 분리 **강제 X**. "분리하면 뇌 부담이 정말 주나?" 자문. *분리 후 두 파일을 둘 다 열어야 이해되면* 잘못 쪼갠 것.

- **[2.3] 클래스 크기** — ~300줄 초과 = 분리 *검토* 신호(강제 아님). 단 2+ 도메인이면 줄 수 무관 분리. 600줄+ 단일 클래스 = 거의 확실히 God class.

- **[2.4] Composition over Inheritance** — 상속 깊이 ≤ 1. "X이기도 Y이기도"면 상속 말고 Component. → [`12-type-object`](refs/game-programming-patterns/12-type-object.md), [`11-subclass-sandbox`](refs/game-programming-patterns/11-subclass-sandbox.md)
  - **예외: 네트워크 세션 프레이밍 템플릿** — `Session → PacketSession → {Game/Unity/Bot}Session` (서버 `GameSession` / 클라 `UnityClientSession` / 봇 `BotSession` 공통)은 깊이 2를 허용한다. 중간 `PacketSession`은 length-prefixed framing(수신 버퍼 누적 → 완성 패킷 단위 절단)을 담당하는 *재사용 기반 계층*이고, 말단은 도메인 콜백만 구현한다 — 이는 "X이기도 Y이기도"가 아니라 *프레이밍 인프라 ↔ 도메인 핸들러*의 의도된 역할 분리(Rookiss 표준)다. **단 깊이 3+는 금지.**

- **[2.5] DRY — 중복 기준과 추출 방향** ⭐ (v6 신설)
  - **2회 = 신호** — "다음에 또 나오면 추출하자" 메모. 아직 강제 아님.
  - **3회 = 의무** — 같은 이유로 변경될 코드가 세 곳에 퍼져 있으면 추출한다.
  - **추출 방향** — 데이터를 소유한 객체의 메서드로. 예: 적 사망 처리는 적 상태를 소유한 `GameMap`(또는 `CombatSystem`)이 mutator를 가진다. System 간 직접 호출로 처리하면 §2.2 통신 규칙 위반.
  - **⚠️ 예외: 우연한 중복(coincidental duplication)** — 지금 *모양이 같아도* 변경 이유가 다른 코드는 묶지 않는다(§0.3 정합). "같은 모양"이 아니라 "같은 이유로 함께 변할 것"이어야 추출 대상이다.
  - **reviewer 축 6(§5.2)** — "DRY(§2.5)" 점검 편입: 동일 로직 3회+ 복붙을 강제 점검 항목으로 추가.

---

## 3. Unity 클라이언트

> 근거 → Unity C# Style Guide (Unity 6)

- **[3.1] MonoBehaviour는 한 개념만** — 순수 로직(prediction/보간/상태)은 plain C#로 추출(EditMode 테스트 가능). MonoBehaviour = 생명주기 연결 + 호출만.
- **[3.2] 패킷 핸들러 = 서버 `Handlers/` 패턴 미러** — `UnityClientSession`의 inline switch → `IPacketHandler` + dispatch 테이블 (서버와 대칭). 갭 = 부록 A.
- **[3.3] 네이밍 prefix** (prefix *규율* — 우리 규칙, 수동 적용. casing 자체는 §4 도구 영역) — **서버·클라 production 코드 공통 적용** (2026-05-29 결정):
  - **field**(private/instance): `_camelCase` (밑줄). `m_` 헝가리안 금지(ServerCore 레거시 = 정리 대상), bare `camelCase`(밑줄 누락) 금지.
  - **`[SerializeField]` 직렬화 field**: `_camelCase` **동일** (designer-facing이지만 일관성 우선). ⚠️ rename 시 `[FormerlySerializedAs("old")]`로 Inspector/prefab 값 보존.
  - **method 매개변수 / 지역 변수**: `camelCase` (**밑줄 prefix 금지** — `_endPoint` 류는 §3.3 위반. casing은 §4).
  - **const** `PascalCase` / static 필요 시 `s_`.
  - **혼용 금지** — 한 종류는 한 표기로 (코드베이스 전체 일관).
- **[3.4] SRP** — §2.2를 클라에도 동일 적용.

---

## 4. 포매팅 / 네이밍 — 자동 강제 (M4.14 P04 활성)

사람·AI가 판단할 영역이 **아니다 — 도구가 강제한다.** 공개 `PascalCase` / 지역·매개 `camelCase` / Allman 중괄호 / **중괄호 `when_multiline`** — 조건·본문이 여러 줄이거나 if-else 한쪽이 블록인 경우만 강제하고, **한 줄 가드절(`if (x) return;`)은 허용**. **강제 = `.editorconfig`(`csharp_prefer_braces = when_multiline` + `IDE0011` warning) + `EnforceCodeStyleInBuild=true`(Directory.Build.props)로 빌드/CI에서 검사**. 범위 = production만(02_Server/04_ClientNet/98_Shared; Tests·99_Tools·03_Client는 §7.1과 동일하게 경계).

> **중괄호 = when_multiline 결정** (M4.14 P03 analyzer 실측): always("단일 문장도 무조건")는 production 168건 churn 대비 가치 낮아 기각. "goto fail" 류 사고(무중괄호 밑 문장 추가)는 본문이 multi-line일 때만 가능 → when_multiline이 위험 케이스(실측 15건)만 잡고 한 줄 가드절 가독성은 보존. casing(SA1300/1312/1313)·Allman(SA1500)은 실측 production 위반 0(이미 준수). P04에서 15건 기계 수정 + 룰 활성.

---

## 5. 강제 메커니즘 (선언 ≠ 강제 — 안 지켜지면 무의미)

- **[5.1] SubAgent 정의** — server/client/shared agent 프롬프트에 "코드 작성 전 `INDEX.md` 참조 의무" 명시. (`.claude/agents/`)
- **[5.2] reviewer 축 6** — `REVIEW_CHECKLIST.md`에 "Code Convention" 축 신설: God class(§2.2)·패턴 위반·콘텐츠/엔진 혼재 점검. *기존 "코드 크기 = 도구 책임이라 안 봄" 제외 조항을 뒤집는다* (Phase 07 God class를 놓친 근원).
- **[5.3] hook (보조)** — 핵심 파일(`GameMap`/`GameSession`/`UnityClientSession`) 줄 수 임계 초과 시 경고.
- **[5.4] ADR-028** — 본 Convention 채택 의사결정 박제.

---

## 6. 주석 (Comments)

> 근거: §0.2 (좋은 코드 = 읽는 사람이 뇌에 담을 지식이 최소인 코드). **주석 노이즈는 코드를 가린다** — 주석이 코드보다 많으면(예: `EnemyEntity` 코드 40줄 / 주석 70줄) 읽는 사람이 코드를 못 본다.

- **[6.1] 코드가 말하게 (self-documenting)** — 이름·구조로 드러나는 건 주석 금지. **"무엇/어떻게"는 코드, 주석은 "왜"만.** 좋은 이름 하나 > 설명 주석 세 줄.
- **[6.2] 금지 주석** (제거 대상):
  - (a) **자명한 재진술** — `// PatrolDir = 현재 순찰 방향` (이름이 이미 말함)
  - (b) **역사·Phase 박제** — `// M3 Phase 06 Step 2`, `// M4.1 Phase 05` (언제 추가했는지는 `git blame`의 일)
  - (c) **폐기된 사고과정·대안검토** — `// 800ms 우려... → 결정:` 류 (커밋 메시지 / `-DONE.md`로)
  - (d) **backlog·TODO 남발** — `// M4+ backlog` 반복 (이슈 트래커 / work-pin으로)
  - (e) **internal 멤버에 기계적 XML doc** — `/// <summary>X 필드</summary>` (public API 계약에만)
- **[6.3] 허용 (그 5%)** — 코드만 봐선 *왜*인지 모르고 **안 적으면 누가 잘못 고쳐 사고나는 비자명한 결정 근거**. 특히 보안·프로토콜·헌법 함정:
  - 예: `C_Attack`에 `attacker` 필드 *없는* 이유 (도용 방지, 헌법 #3) / tick 스레드 invariant (lock-free 전제) / PDL append-only (재정렬 = desync).
  - 위치 = 해당 코드 **바로 위 1~2줄**, 간결히. 파일 상단 대형 블록 지양.
- **[6.4] 강제** (§5 패턴) — reviewer `REVIEW_CHECKLIST` 축 6에 "주석 노이즈(§6.2)" 점검 편입 + SubAgent(server/client/shared) 정의에 §6 준수 명시 (`.claude/` self-mod → 스테이징 → 본인 적용).

- **[6.5] 클래스 1줄 책임 헤더** ⭐ (v6 신설) — 모든 public 클래스 상단 첫 줄에 "이 클래스가 무엇을 책임지는가"를 1줄로 선언한다.
  - **형식**: `// ClassName: <책임 선언>.` — 간결하게, 동사 위주.
  - **모범 (현 `GameMap.cs` 헤더)**:
    ```
    // ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
    // 동시성 버그의 90%가 사라진다. 외부 → EnqueueJob 경유 마샬링.
    ```
    이 헤더는 "왜 이 구조인가"를 선언하는 ARCHITECTURE 인용형이다. §6.5가 정의하는 *형식 기준*은 이처럼 **비자명한 책임 선언** — 클래스 이름만으로는 알 수 없는 역할 경계나 설계 의도를 1줄(또는 2줄 이하)로 표현한다.
  - **§6.2와의 구분**: 책임 헤더는 "이 클래스가 *무엇을 책임지는가*"의 비자명 선언이다. "X 클래스입니다" 수준의 자명 재진술은 §6.2(a) 위반 — 작성하지 않는다.
  - **reviewer 축 6(§5.2)** — public 클래스에 책임 헤더 누락을 점검 항목으로 추가.

---

## 7. 코드 탐색 가이드 (v6 신설)

### [7.1] 멤버 정렬 순서

C# 클래스 내 멤버 선언 순서를 고정한다. 일관된 순서 = 읽는 사람이 "이 멤버 어디 있지?" 탐색 비용 0.

**강제 순서(SA1201)**: 필드 → 생성자 → event → 프로퍼티 → 메서드 → 중첩 타입. 같은 종류 안에서는 접근성 순(**SA1202**): public → internal → protected internal → protected → private. 파일(네임스페이스) 레벨은 struct → class. 필드 안에서 상수 → static → 인스턴스 순은 *권장*(SA1203/1204 미강제 — 도구가 검사하지 않으므로 재량).

**강제 도구**: StyleCop.Analyzers **SA1201**(멤버 종류 순서) + **SA1202**(접근성 순서)를 `.editorconfig`에 `warning` 레벨로 박는다 — 빌드 시 경고로 노출. **적용 범위 = production 게임 코드**(02_Server / 98_Shared / 04_ClientNet, 경고 0 스윕 완료 — M4.10 Phase 05). `GameServer.Tests/`·`99_Tools/`는 하위 `.editorconfig`로 완화(비상 가독성 가치 낮음), `03_Client`는 Unity NuGet 비호환으로 도구 미적용(차단막 — 발표 후 별도).

**`#region` 수동 구획 금지** — 사람이 `#region Fields`, `#region Methods`로 구획해도 컴파일러가 순서를 검사하지 않는다(drift). SA1201/1202가 자동 강제하므로 `#region` 구획에 의존하지 않는다.

### [7.2] 진입점 내비게이션

"버그가 터졌는데 어느 파일부터 봐야 하지?"를 줄이는 두 가지 도구를 병행한다.

**(a) `ENTRY_POINTS.md`** — 증상 → 시작 파일·함수 룩업표. 비상 디버깅용. 본 Phase에서 골격(형식)만 만들고, 본문은 Phase 05에서 채운다. 경로: [`ENTRY_POINTS.md`](ENTRY_POINTS.md).

**(b) 파일 상단 흐름 1줄 헤더** — 각 시스템 파일 상단에 "이 파일을 통과하는 주요 흐름"을 1줄로 적는다. 예:
```
// [흐름] C_Attack 수신 → AttackHandler.Handle → GameSession.SubmitAttack → map.EnqueueJob → CombatSystem.ProcessAttack
```
파일을 열자마자 전체 흐름 맥락을 잡는다. §6.3 안전 주석 범위 정합 — "어디로 흐르는가"의 비자명 내비게이션이다.

---

## 부록 A. 현재 갭 (본 Convention 기준 리팩토링 대상)

**God class / 대형 파일**

| 대상                                | 현재                                                                               | 위반      | 분리안                    | 타이밍 |
| ----------------------------------- | ---------------------------------------------------------------------------------- | --------- | ------------------------- | ------ |
| ~~`GameMap` (665줄)~~ **졸업** ✅   | 실측 498줄(M4.13 Skill/Action 추가분 반영, 2026-06-14 M4.14 P01 정정). 6 System(Combat/Boss/DeferredDamage/EnemyAI/Respawn/Skill) 분리 완료. `Maps/Systems/` 아래 각 System 독립 파일. `GameMap` 자체는 "container + 최소 surface mutator" 의도적 설계 — §2.2 기준 충족. | (해소)    | (완료)                    | 완료 |
| `ClientPacketHandlers.cs` (909줄)   | inline 핸들러 + VFX 보일러플레이트 대거 포함. **진짜 미실행 대상.** (옛 `UnityClientSession` 665줄은 실측 213줄로 이미 슬림 — 핸들러가 이 파일로 이동했기 때문) | §3.2      | `IPacketHandler` + dispatch 분리 | M4.12 |
| `GameSession` (700줄)               | rate-limit/handshake 등 ~95줄 추출 가능 (migration 160줄 잔류)                     | §2.2 부분 | 부분 추출                 | 미정 |
| `EnemyRegistry` (240줄)             | GameObject 빌더 결합                                                               | §3.1      | 빌더 추출                 | 선택 |

**중복(§2.5) — 전수조사 7건**

| 대상                          | 현재                                                        | 위반  | 처리안                                   | 타이밍 |
| ----------------------------- | ----------------------------------------------------------- | ----- | ---------------------------------------- | ------ |
| 적 사망 처리 3복붙            | 동일 로직이 3개 위치에 복사 (file:line 근거 — M4.10 전수조사) | §2.5  | `GameMap`/`CombatSystem` 단일 mutator 추출 | M4.10 Phase 03 |
| roster 관리 2복붙             | 플레이어 목록 조작 로직 2곳 중복                             | §2.5  | 단일 method 추출                         | M4.10 Phase 04 |
| rewind 로직 4벌               | lag compensation rewind 코드 4곳 복사                       | §2.5  | 추출·통합                                | M4.10 Phase 04 |
| `facingByte` 변환 4벌         | facing 방향 → byte 변환 로직 4곳 중복                       | §2.5  | 헬퍼 메서드 추출                         | M4.10 Phase 04 |
| 매직넘버 산재                 | 하드코딩 수치 다수 (HP/속도/쿨다운 등)                      | §2.5  | `GameValues`/`Constants` 상수화          | M4.10 Phase 02 |
| `HitEffect` enum 부재         | 피격 이펙트 종류를 int/string으로 처리                      | §2.5  | enum 신설                                | M4.10 Phase 02 |
| 클라 VFX 보일러플레이트       | `ClientPacketHandlers.cs` 내 VFX 트리거 코드 반복           | §2.5  | VFX 핸들러 계층 분리 (§3.2 정합)         | M4.12 |

→ 리팩토링은 본 Convention 확정 + 강제(§5) 적용 후 별도 Phase에서.

---

## 변경 이력

| 날짜       | 버전 | 변경                                                                                                 |
| ---------- | ---- | ---------------------------------------------------------------------------------------------------- |
| 2026-05-29 | v1   | 최초 — God class(GameMap) 발견, 4 권위서 정독                                                        |
| 2026-05-29 | v2   | refs 33파일 + INDEX 연결, `ServerCore` 가공경로 정정                                                 |
| 2026-05-29 | v3   | **슬림화** — 책 이론/인용 → refs 링크 위임, 우리 규칙 선언만 (≈218→130줄). prefix `_camelCase` 확정. |
| 2026-05-29 | v4   | §3.3 확장 — **서버·클라 공통 적용** 명문화(`m_` 헝가리안 금지) + `[SerializeField]`도 `_camelCase`(designer-facing 예외 두지 않음) + **매개변수/지역변수 `camelCase`(밑줄 금지)**. `_`-prefix 매개변수(`_endPoint` 류)를 §4 casing이 아닌 §3.3 prefix 위반으로 재분류. M4.3R Phase 01 (사용자 결정). |
| 2026-05-30 | v5   | §2.4 **네트워크 세션 프레이밍 템플릿 깊이 2 예외** 명문화 (`Session→PacketSession→GameSession`은 의도된 framing↔handler 분리 — Codex read-only 감사). **§6 주석 정책 신설** (self-documenting — §6.2 금지 5종 + §6.3 안전 예외 5%; M4.3X 대정리 기준). 제목 v3→v5 stale 정정. 동반 코드 정합: PacketGenerator 매개변수/템플릿 prefix(§3.3) + SceneTransition `[SerializeField]` rename. |
| 2026-06-11 | v6   | **4보강**: §2.5 DRY(중복 2회=신호/3회=의무, 우연한 중복 예외) + §6.5 클래스 1줄 책임 헤더(public 클래스 의무, GameMap 헤더 모범) + §7.1 멤버 정렬(상수→static→필드→프로퍼티→생성자→public→private→중첩, StyleCop SA1201/SA1202 경고 강제, `#region` 의존 금지) + §7.2 진입점 내비게이션(ENTRY_POINTS.md + 파일 흐름 1줄 헤더). **부록 A 실측 갱신**: GameMap(665→436, 6 System 분리 완료) 졸업, ClientPacketHandlers 909줄을 진짜 미실행 대상으로 기재(M4.12), 전수조사 중복 7건 편입. M4.10 Phase 01. |
| 2026-06-11 | v6.1 | **§7.1 정정 + 범위 확정** (M4.10 Phase 05): v6 초안의 "프로퍼티→생성자" 순서가 SA1201 실강제("생성자→프로퍼티")와 반대 — 도구가 검사하는 순서가 진실이므로 문서를 도구에 맞춤(선언=실재). 적용 범위 명문화: production(02_Server/98_Shared/04_ClientNet)만 강제, Tests·99_Tools는 하위 `.editorconfig` 완화, 03_Client는 도구 미적용(Unity NuGet 비호환). production 경고 189→0 스윕 동반. ENTRY_POINTS.md 본문 작성(§7.2 (a) 이행). |
| 2026-06-14 | v6.2 | **§4 중괄호 활성 = `when_multiline`** (M4.14 P03/P04). "M4.4+ 미뤄둠"을 analyzer로 강제 전환 — `EnforceCodeStyleInBuild=true` + `IDE0011`/`csharp_prefer_braces = when_multiline`. P03 실측이 결정: always는 production 168건(Codex 추정 288·검토 "~90" 둘 다 빗나감) churn 대비 가치 낮아 기각, when_multiline=15건만. casing/Allman은 실측 위반 0(이미 준수). P04에서 15건 기계 수정(거동 0 + WSL2 568/0/5) + Tests/99_Tools `IDE0011=none` 경계. 영호 결정. |
