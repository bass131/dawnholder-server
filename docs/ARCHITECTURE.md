# ARCHITECTURE — 시스템 구조

> **이 문서의 역할**: "어떻게 만들지"의 큰 그림. 기술 스택, 디렉토리 구조,
> 데이터 흐름, 핵심 패턴을 정의합니다.
>
> **루트 `CLAUDE.md`와의 차이**: 헌법은 "절대 원칙"(짧고 강함), 이 문서는
> "구조 설명"(길고 상세함). 헌법이 X헌법이라면 이건 헌법 해설서.

---

## 기술 스택

### Client
- **엔진**: Unity 2022 LTS, URP 2D
- **언어**: C#
- **주요 패키지**: 새 Input System, MessagePack-CSharp
- **빌드 타겟**: PC Windows (Standalone)

### Server
- **런타임**: .NET 8
- **호스트 형태**: 콘솔 앱 (개발) / Linux 서비스 (운영, 향후)
- **DI**: Microsoft.Extensions.DependencyInjection
- **로깅**: Serilog (Console + File sink)
- **테스트**: xUnit + FluentAssertions

### Network
- **프로토콜**: Raw TCP, length-prefixed binary frame
- **직렬화**: MessagePack-CSharp (`[Key(N)]` 명시 모드)
- **핸드셰이크**: 첫 패킷에 ProtocolVersion 교환

### Persistence
- **DB**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **마이그레이션**: EF Core code-first
- **개발 환경**: Docker Compose로 로컬 PostgreSQL

---

## 디렉토리 구조

```
project-root/
├── client/                        Unity 프로젝트
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Network/           TCP 클라이언트, 패킷 read 루프
│   │   │   ├── Prediction/        클라이언트 prediction + reconciliation
│   │   │   ├── Rendering/         스프라이트, 카메라, 애니메이터
│   │   │   ├── Input/             입력 → intent packet
│   │   │   ├── UI/                HUD, 메뉴
│   │   │   └── State/             서버 상태의 로컬 미러
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   └── Resources/
│   └── ProjectSettings/
│
├── server/                        .NET 권위 서버
│   ├── GameServer/
│   │   ├── Network/               TcpListener, Session, Framing
│   │   ├── Loop/                  Tick scheduler, world simulation
│   │   ├── Maps/                  맵별 actor, 공간 쿼리
│   │   ├── Combat/                데미지 해석, hitbox 검사
│   │   ├── Persistence/           DbContext, write queue
│   │   ├── Handlers/              PacketId → handler dispatch
│   │   └── Program.cs
│   └── GameServer.Tests/          xUnit
│
├── shared/                        클라/서버 공유
│   ├── Shared.csproj              .NET Standard 2.1
│   ├── Protocol/
│   │   ├── PacketId.cs            모든 패킷 ID enum
│   │   ├── Packets/               패킷별 파일
│   │   └── ProtocolVersion.cs
│   └── GameData/
│       ├── Formulas.cs            데미지, XP, 스탯 공식
│       ├── Constants.cs           Tick rate 등
│       └── Tables/                items.json, monsters.json 등
│
├── tools/
│   └── headless-bot/              QA 시뮬레이션 봇
│
├── docs/                          PRD, ARCHITECTURE, ADR
├── phases/                        Phase 작업 파일
└── .claude/                       Harness 설정
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

### 4. Shared Protocol Assembly

`shared/`는 .NET Standard 2.1 라이브러리로 빌드. 클라/서버 둘 다 이걸
참조합니다. 한 어셈블리에서 컴파일된 같은 타입을 쓰니까 직렬화 호환성
보장.

---

## 외부 의존성

| 의존성 | 용도 | 라이선스 |
|--------|------|----------|
| MessagePack-CSharp | 직렬화 | MIT |
| Serilog | 로깅 | Apache 2.0 |
| Entity Framework Core | ORM | MIT |
| xUnit + FluentAssertions | 테스트 | MIT |
| PostgreSQL (Docker) | DB | PostgreSQL License |

새 의존성 추가는 ADR로 기록.

---

## 변경 이력

| 날짜 | 변경 | 이유 |
|------|------|------|
| YYYY-MM-DD | 최초 작성 | - |
