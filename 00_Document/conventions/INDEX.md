# Dawnholder Convention — 마스터 색인

> **SubAgent 진입점.** 코드를 작성하기 *전에* 이 색인에서 현재 작업 유형을 찾아, 연결된 [우리 규칙(CODE_CONVENTION) + 패턴/장 참고서(refs) + 헌법]을 읽는다.
>
> 우선순위: 헌법(`CLAUDE.md`) > `CODE_CONVENTION.md`(우리 채택 규칙) > `refs/`(책 이론 참고서). 헌법과 충돌 시 헌법이 이긴다.

---

## 폴더 구조

```
00_Document/conventions/
├── INDEX.md                  ← (이 파일) 작업 유형 → 참조 라우팅
├── CODE_CONVENTION.md        ← 우리가 채택한 규칙 (결정적). 근거는 refs를 가리킴
├── ENTRY_POINTS.md           ← 증상→시작점 룩업 (비상 디버깅). 골격 확정, 본문은 Phase 05에서 채움
└── refs/                     ← 두 권위서를 정독한 참고서 (이론 + 우리 적용 + 함정)
    ├── game-programming-patterns/   _index.md + 19개 패턴 (01~19)
    └── game-server-programming/     _index.md + 10개 장 (01~10)
```

- **빠른 사용**: 아래 라우팅 표 → 해당 파일 열기 → 각 파일의 "언제 참조하나" 섹션 먼저 읽기.
- **패턴 전체 카탈로그**: [`refs/game-programming-patterns/_index.md`](refs/game-programming-patterns/_index.md)
- **서버 시스템 전체 카탈로그**: [`refs/game-server-programming/_index.md`](refs/game-server-programming/_index.md)

---

## 작업 유형별 라우팅

| 작업 유형 | 우리 규칙 (CODE_CONVENTION) | GPP 패턴 (refs) | 게임서버 교과서 (refs) | 헌법 |
|----------|---------------------------|----------------|----------------------|------|
| **God class 분리** | §2.2 (분리 결정 기준) | [13-component], [14-event-queue], [09-update-method], [15-service-locator] | [09-distributed-server] (응집도) | — |
| **enemy/boss AI · FSM** | §1.1, §2.2 | [06-state], [09-update-method], [12-type-object] | [01-multithreading], [05-game-networking] | #1, #5 |
| **새 패킷 추가** | §1.2 (콘텐츠/엔진) | [01-command], [14-event-queue] | [02-computer-network], [03-socket-programming] | #2, #3 |
| **동시성 · 락 · tick 루프** | §1.1, §1.3 | [08-game-loop], [18-object-pool] | [01-multithreading] | #5 |
| **prediction / reconcile / lag comp** | §1.4 | [07-double-buffer] | [05-game-networking] | #1 |
| **클라 패킷 핸들러 분리** | §3.2 (서버 Handlers 미러) | [01-command], [13-component] | [04-server-and-client] | #1 |
| **전역 서비스 / 매니저 설계** | §2.1, §2.2 | [05-singleton] (먼저!), [15-service-locator] | — | 정적 mutable 금지 |
| **DB 영속화 (M5)** | — | [17-dirty-flag] | [07-database], [08-nosql] | — |
| **성능 최적화 (측정 후)** | §0.3, §0.4 | [16-data-locality], [18-object-pool], [19-spatial-partition] | [09-distributed-server], [10-distributed-cases] | #5 |
| **맵/존 확장 · 분산** | §1.1 (Map=Actor) | [19-spatial-partition] | [09-distributed-server], [10-distributed-cases] | — |
| **데이터 주도 (몬스터/아이템 종류)** | §2.4 | [02-flyweight], [04-prototype], [12-type-object] | — | #1 (서버 권위) |
| **스킬/버프/공격 유형 확장** | §2.4 | [11-subclass-sandbox], [12-type-object], [10-bytecode] | — | #1 |
| **이벤트/알림 (죽음→broadcast 등)** | §2.1 | [03-observer], [14-event-queue] | — | — |

> 링크 경로: GPP는 `refs/game-programming-patterns/NN-*.md`, 교과서는 `refs/game-server-programming/NN-*.md`. 정확한 파일명은 각 `_index.md` 참조.

---

## 자주 쓰이는 패턴 조합 (GPP _index에서)

- **God class 분리**: Component(13) + Event Queue(14) + Update Method(09)
- **Enemy AI 확장**: State(06) + Type Object(12) + Update Method(09)
- **틱 루프 최적화**: Game Loop(08) + Object Pool(18) + Data Locality(16)
- **네트워크 패킷 구조**: Command(01) + Event Queue(14) + Double Buffer(07)
- **전역 서비스**: Service Locator(15) (Singleton(05) 대신)

---

## 참조 의무 (강제 — CODE_CONVENTION §5 정합)

`server` / `client` / `shared` SubAgent는 코드 작성 전:
1. 본 색인에서 작업 유형을 찾는다.
2. 연결된 `CODE_CONVENTION.md` 규칙 + `refs/` 파일을 읽는다.
3. 특히 **God class 분리(§2.2)**와 **과한 추상화 경계(§0.3)**는 모든 코드 작업의 공통 점검 항목.

reviewer는 변경 코드가 본 색인이 가리키는 규칙을 위반하는지 점검한다 (REVIEW_CHECKLIST 축 6).
