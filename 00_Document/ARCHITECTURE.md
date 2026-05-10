# ARCHITECTURE — 시스템 구조

> **이 문서의 역할**: "어떻게 만들지"의 큰 그림. 기술 스택, 디렉토리 구조,
> 데이터 흐름, 핵심 패턴을 정의합니다.
>
> **루트 `CLAUDE.md`와의 차이**: 헌법은 "절대 원칙"(짧고 강함), 이 문서는
> "구조 설명"(길고 상세함). 헌법이 X헌법이라면 이건 헌법 해설서.

---

## 기술 스택

### Client
- **엔진**: Unity 6.4 LTS, URP 2D
- **언어**: C#
- **주요 패키지**: 새 Input System
- **빌드 타겟**: PC Windows (Standalone)

### Server
- **런타임**: .NET 10 LTS
- **호스트 형태**: 콘솔 앱 (개발) / Linux 서비스 (운영, 향후)
- **DI**: Microsoft.Extensions.DependencyInjection
- **로깅**: Serilog (Console + File sink)
- **테스트**: xUnit + FluentAssertions

### Network
- **프로토콜**: Raw TCP, length-prefixed binary frame
- **직렬화**: 자체 PDL(Packet Definition Language) XML + C# 코드 생성기 (ADR-002)
- **핸드셰이크**: 첫 패킷에 ProtocolVersion 교환

### Persistence
- **DB**: PostgreSQL 16
- **ORM**: Entity Framework Core 10 (.NET 10 LTS와 같이 묶음)
- **마이그레이션**: EF Core code-first
- **개발 환경**: Docker Compose로 로컬 PostgreSQL

---

## 디렉토리 구조

폴더는 탐색기 정렬 고정용 숫자 prefix를 사용. Y2 갈래(ADR-012)로 클라/서버 socket 코드는 *분리*된 라이브러리에 둠.

```
project-root/
├── 00_Document/                  PRD, ARCHITECTURE, ADR, learning-journal
├── 01_Phases/                    Phase 작업 파일 (M{N}-{slug}/{NN}-*.md + -DONE.md)
│
├── 02_Server/                    .NET 10 LTS 권위 서버
│   ├── GameServer/
│   │   ├── Network/              게임 도메인 세션 (GameSession 등)
│   │   ├── Loop/                 Tick scheduler, world simulation
│   │   ├── Maps/                 맵별 actor, 공간 쿼리
│   │   ├── Combat/               데미지 해석, hitbox 검사
│   │   ├── Persistence/          DbContext, write queue
│   │   ├── Handlers/             PacketId → handler dispatch
│   │   └── Program.cs
│   ├── GameServer.Tests/         xUnit + FluentAssertions
│   └── Network/                  ★ ServerCore (Listener/Session/Buffers)
│                                  별도 csproj (.NET 10). Y2 분리: 클라와 다른 어셈블리.
│
├── 03_Client/                    Unity 6.4 LTS 프로젝트
│   ├── Assets/
│   │   ├── Plugins/
│   │   │   ├── Shared/           ← Shared.dll 자동 복사 (ADR-010)
│   │   │   └── ClientNet/        ← Dawnholder.Client.Net.dll 자동 복사
│   │   └── Scripts/
│   │       ├── Network/          Unity wrapper (UnityClientSession,
│   │       │                      MainThreadDispatcher, NetworkBootstrap)
│   │       ├── Prediction/       클라 prediction + reconciliation (예정)
│   │       ├── Rendering/        스프라이트, 카메라, 애니메이터 (예정)
│   │       ├── Input/            입력 → intent packet (예정)
│   │       ├── UI/               HUD, 메뉴 (예정)
│   │       └── State/            서버 상태의 로컬 미러 (예정)
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   └── Resources/
│   └── ProjectSettings/
│
├── 04_ClientNet/                 ★ Y2 갈래: 클라용 socket 라이브러리
│                                  .NET Standard 2.1 (Unity 호환).
│                                  Connector / ClientSession / RecvBuffer / SendBuffer.
│                                  산출 DLL이 03_Client/Assets/Plugins/ClientNet/로
│                                  자동 복사 (ADR-010 + ADR-012).
│
├── 98_Shared/                    클라/서버 공유 (Protocol + GameData)
│   ├── Shared.csproj             .NET Standard 2.1, Embedded PDB + EmbedAllSources
│   ├── Protocol/
│   │   └── Generated/            PDL 자동 생성 (Phase 06/07 활성화, ADR-002)
│   │       └── GenPackets.cs     PacketID enum + 패킷 정의 (BinaryPrimitives.*LittleEndian)
│   └── GameData/                 (Phase M2+에서 채워질 예정)
│       ├── Formulas.cs           데미지/XP/스탯 공식
│       ├── Constants.cs          Tick rate 등
│       └── Tables/               items.json, monsters.json 등
│
├── 99_Tools/
│   ├── headless-bot/             QA 시뮬레이션 봇 (M2 이후 작성 예정)
│   └── PacketGenerator/          자체 PDL 코드 생성기 (Phase 06 이주 완료, ADR-002)
│
├── Dawnholder.slnx               .NET 솔루션 (02_Server + 04_ClientNet + 98_Shared)
├── global.json                   .NET SDK 핀
├── CLAUDE.md                     프로젝트 헌법 (단일 진실 공급원)
├── CONTEXT.md                    세션 핸드오프 (응축, 200줄 한도)
├── CONTEXT_History.md            CONTEXT 갱신 이력 (외부화)
└── .claude/                      Harness (agents, commands, hooks, templates)
```

---

## 데이터 흐름 — "플레이어가 한 칸 움직이기"

이 흐름이 우리 게임의 가장 기본 사이클입니다. 모든 액션은 이 패턴을 따라요.

```
┌──────────┐                                ┌──────────┐
│  Client  │                                │  Server  │
└────┬─────┘                                └─────┬────┘
     │                                            │
     │  1. 키 입력 감지 (Input Module)            │
     │ ─────────────────────────────              │
     │                                            │
     │  2. Predicted Move (즉시 보임)             │
     │     캐릭터를 화면에서 한 칸 움직임          │
     │                                            │
     │  3. C2S_Move 패킷 전송 ──────────────────► │
     │     (Tick=42, InputX=1, JumpPressed=false)│
     │                                            │
     │                                            │  4. 서버 검증
     │                                            │     - 입력 범위 OK?
     │                                            │     - 이동 거리 한계 OK?
     │                                            │     - 충돌 없음?
     │                                            │
     │                                            │  5. 권위 위치 확정
     │                                            │     맵 상태 업데이트
     │                                            │
     │  6. ◄────────────── S2C_Snapshot 브로드캐스트
     │     (Tick=42, EntityId=...,Position=...)  │
     │                                            │
     │  7. Reconciliation                         │
     │     - 서버 위치 == 예측 위치?              │
     │       → OK (변경 없음)                    │
     │     - 다르면?                              │
     │       → 서버 위치로 snap 또는 보간        │
     │                                            │
     │  8. 다른 플레이어/몹들도 같은 패킷 수신    │
     │     → 그들 화면에서 우리 캐릭터 보간 표시  │
     │                                            │
```

### 핵심 포인트

- **클라이언트 prediction**은 본인 캐릭터에만 적용. 다른 캐릭터는 서버 스냅샷 기반 순수 보간.
- **서버는 절대 클라이언트 위치를 그대로 믿지 않음.** 항상 검증.
- **Tick** 번호가 모든 동기화의 기준. "언제 일어난 일이냐"의 단위.

---

## 핵심 패턴

### 1. Map = Actor

각 맵(방)은 단일 스레드 actor로 동작합니다. 맵 안에서는 lock 없음.
맵 간 통신은 message channel만. 이게 동시성 버그의 90%를 막아줍니다.

### 2. Tick 기반 시뮬레이션

서버는 50ms마다 (20 TPS) 한 번씩 "틱"을 돌립니다. 한 틱 안에서:
1. 들어온 입력 패킷 모두 처리
2. AI/물리/타이머 업데이트
3. 변경 사항 브로드캐스트

이 루프 안에서 절대 await 안 합니다 (CLAUDE.md 원칙 5).

### 3. Persistence Write Queue

DB 쓰기는 절대 동기로 안 합니다. `Channel<SaveIntent>` 큐에 넣고, 별도
백그라운드 워커가 배치로 씁니다. 게임 루프는 DB 응답을 안 기다림.

### 4. Shared Protocol + Y2 socket 분리 (ADR-010 + ADR-012)

**Protocol/GameData**: `98_Shared/`는 .NET Standard 2.1 라이브러리. 양쪽 공유.
한 어셈블리에서 컴파일된 같은 타입을 쓰니까 직렬화 호환성 자동 보장.

**Socket 레이어는 양쪽 분리**:

| 위치 | 대상 | 타겟 | 역할 |
|---|---|---|---|
| `02_Server/Network/` | 서버 전용 | .NET 10 | Listener / Session / Buffers (ServerCore) |
| `04_ClientNet/` | 클라 전용 | .NET Std 2.1 | Connector / ClientSession / Buffers |

같은 패턴(SocketAsyncEventArgs)을 두 벌 두는 비용 vs *각자 환경 제약(Unity Mono/IL2CPP vs .NET 10 GC)에 맞춰 최적화 가능 + 한쪽 변경이 다른 쪽 빌드 안 깸*의 trade-off에서 후자 채택. 현업 표준(Mirror/FishNet/gRPC도 양쪽 분리).

**DLL 파이프라인** (ADR-010): 빌드 시 `Shared.dll`은 `03_Client/Assets/Plugins/Shared/`로, `Dawnholder.Client.Net.dll`은 `03_Client/Assets/Plugins/ClientNet/`로 자동 복사. PDB는 `EmbedAllSources=true`로 원본 .cs 임베드 → Unity F12 시 한국어 주석까지 ReadOnly 표시. 헌법 #4("복사-붙여넣기 금지")의 물리적 강제.

---

## 외부 의존성

| 의존성 | 버전 | 용도 | 라이선스 |
|--------|------|------|----------|
| Serilog | 4.x | 로깅 | Apache 2.0 |
| Entity Framework Core | 10.x | ORM | MIT |
| xUnit + FluentAssertions | latest | 테스트 | MIT |
| PostgreSQL (Docker) | 16 | DB | PostgreSQL License |

**직렬화는 외부 의존성 없음** — 자체 PDL(`99_Tools/PacketGenerator/`, ADR-002 v2). Phase 06에서 이주·Phase 07에서 양쪽 wire-up 완료. `BinaryPrimitives.*LittleEndian`으로 wire format 플랫폼 무관. MessagePack은 ADR-002 v1에서 채택했으나 v2(2026-05-06)에서 자체 PDL로 변경.

새 의존성 추가는 ADR로 기록.

---

## 변경 이력

| 날짜 | 변경 | 이유 |
|------|------|------|
| (Harness 셋업일) | 최초 작성 | - |
| 2026-05-10 | 폴더 prefix 정렬 + ADR-002 v2(자체 PDL) + ADR-012(Y2 분리) 반영 | 디렉토리 구조 통째 재작성 + MessagePack 의존성 제거 + EF Core 8→10 + Y2 socket 분리 모델 명시. 2026-05-09 prefix 변경 + 2026-05-06 ADR-002 v2 / 2026-05-10 ADR-012 시점에 ARCHITECTURE는 누락됐던 것 일괄 정합. |
| 2026-05-11 | Phase 06/07 활성화 반영 + ADR-012 진화 | Protocol 구조 갱신: 옛 `PacketId.cs`/`Packets/`/`ProtocolVersion.cs` (Phase 07에서 삭제·미작성) 대신 `Generated/GenPackets.cs` (PDL 자동 생성)로 정정. PacketGenerator "이주 예정" → "이주 완료". headless-bot은 M2 이후로 시점 재조정. ADR-012는 "전부 분리"에서 *책임 단위 분리/통합*으로 진화(Phase 07 사용자 통찰). |
