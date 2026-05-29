# Dawnholder Code Convention v3

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

## 4. 포매팅 / 네이밍 — 자동 (M4.4+)

사람·AI가 판단할 영역이 **아니다 — 도구가 강제한다.** 공개 `PascalCase` / 지역·매개 `camelCase` / Allman 중괄호 / 단일 문장도 중괄호 유지. **강제 = `.editorconfig` + Roslyn**(Microsoft 베이스). 도입 = M4.4+ (기계적이라 미뤄도 부채 아님).

---

## 5. 강제 메커니즘 (선언 ≠ 강제 — 안 지켜지면 무의미)

- **[5.1] SubAgent 정의** — server/client/shared agent 프롬프트에 "코드 작성 전 `INDEX.md` 참조 의무" 명시. (`.claude/agents/`)
- **[5.2] reviewer 축 6** — `REVIEW_CHECKLIST.md`에 "Code Convention" 축 신설: God class(§2.2)·패턴 위반·콘텐츠/엔진 혼재 점검. *기존 "코드 크기 = 도구 책임이라 안 봄" 제외 조항을 뒤집는다* (Phase 07 God class를 놓친 근원).
- **[5.3] hook (보조)** — 핵심 파일(`GameMap`/`GameSession`/`UnityClientSession`) 줄 수 임계 초과 시 경고.
- **[5.4] ADR-028** — 본 Convention 채택 의사결정 박제.

---

## 부록 A. 현재 갭 (본 Convention 기준 리팩토링 대상)

| 대상                         | 현재                                                           | 위반      | 분리안                                  | 타이밍         |
| ---------------------------- | -------------------------------------------------------------- | --------- | --------------------------------------- | -------------- |
| `GameMap` (665줄)            | 전투+AI+respawn+broadcast (4 도메인)                           | §2.2      | CombatSystem / AISystem / RespawnSystem | 리팩토링 Phase |
| `UnityClientSession` (665줄) | 패킷 핸들러 12 inline                                          | §3.2      | IPacketHandler + dispatch               | 리팩토링 Phase |
| `GameSession` (700줄)        | rate-limit/handshake 등 ~95줄 추출 가능 (migration 160줄 잔류) | §2.2 부분 | 부분 추출                               | M4.4           |
| `EnemyRegistry` (240줄)      | GameObject 빌더 결합                                           | §3.1      | 빌더 추출                               | 선택           |

→ 리팩토링은 본 Convention 확정 + 강제(§5) 적용 후 별도 Phase에서.

---

## 변경 이력

| 날짜       | 버전 | 변경                                                                                                 |
| ---------- | ---- | ---------------------------------------------------------------------------------------------------- |
| 2026-05-29 | v1   | 최초 — God class(GameMap) 발견, 4 권위서 정독                                                        |
| 2026-05-29 | v2   | refs 33파일 + INDEX 연결, `ServerCore` 가공경로 정정                                                 |
| 2026-05-29 | v3   | **슬림화** — 책 이론/인용 → refs 링크 위임, 우리 규칙 선언만 (≈218→130줄). prefix `_camelCase` 확정. |
| 2026-05-29 | v4   | §3.3 확장 — **서버·클라 공통 적용** 명문화(`m_` 헝가리안 금지) + `[SerializeField]`도 `_camelCase`(designer-facing 예외 두지 않음) + **매개변수/지역변수 `camelCase`(밑줄 금지)**. `_`-prefix 매개변수(`_endPoint` 류)를 §4 casing이 아닌 §3.3 prefix 위반으로 재분류. M4.3R Phase 01 (사용자 결정). |
