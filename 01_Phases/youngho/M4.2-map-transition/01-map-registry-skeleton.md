---
owner: youngho
milestone: M4.2
phase: 01
title: 맵 레지스트리 + MapId enum 골격
status: pending
grade: 보통
estimated: 1.5~2h
domain: server
---

# Phase 01: 맵 레지스트리 + MapId enum 골격

> **상태**: pending
> **마일스톤**: M4.2
> **등급**: 보통 (1 도메인 × 2~3 파일 / 가역적)
> **담당**: server SubAgent

---

## 🎯 목표

`GameWorld`가 단일 `GameMap` 하나만 들고 있던 구조를 **`Dictionary<MapId, GameMap>`
맵 레지스트리**로 승격한다. 4맵(Town/HuntingGround/BossRoom/Ending)을 정의하고,
TickScheduler가 매 틱 **모든 맵을 tick** 돌리되, **기존 단일 맵 동작과 회귀 0**
(플레이어는 여전히 Town에 spawn, enemy/boss는 각자 맞는 맵으로 이동 배치).

이 Phase는 **인프라 골격만** — portal/이동은 Phase 02~03. 첫 Phase라 작게 잡았다.

---

## ⏪ 사전 조건

- [x] M4.1 마감 (combat 정밀화 완료)
- [ ] 없음 — M4.2 첫 Phase (맵 레지스트리는 새 인프라)

---

## 📝 작업 내용

- [ ] `MapId` enum 정의 (`02_Server/GameServer/Maps/MapId.cs` 신설)
  - `Town = 0`, `HuntingGround = 1`, `BossRoom = 2`, `Ending = 3`
- [ ] `GameMap` ctor에 **맵 구성 주입** — 현재 ctor가 enemy/boss를 무조건 hardcode spawn하는 것을
      맵별로 다르게:
  - Town = 빈 맵 (enemy 0)
  - HuntingGround = Normal enemy 1마리 (현 `NormalEnemySpawnX/Y`)
  - BossRoom = Boss 1마리 (현 `BossSpawnX/Y`)
  - Ending = 빈 맵 (결과 화면 골격)
  - → ctor 시그니처에 `MapId mapId` 또는 enemy 구성 인자 추가. 어떤 enemy를 spawn할지 분기.
- [ ] `GameWorld`: `readonly GameMap _map` → `readonly Dictionary<MapId, GameMap> _maps`
  - ctor에서 4맵 생성 + 등록
  - `Map` 프로퍼티(단수) → `GetMap(MapId)` + (호환용) Town 반환 임시 헬퍼
- [ ] `GameWorld.OnTick`: `_map.Tick(n)` → `foreach (map in _maps.Values) map.Tick(n)`
- [ ] `GameSession.GetMap()`: 현재 `GameWorld.Instance?.Map` → 임시로 Town 맵 반환
      (플레이어 추적은 Phase 03에서 — 본 Phase는 spawn 위치만 Town 보존)

---

## ✅ 완료 조건

- [ ] `dotnet build Dawnholder.slnx` 통과
- [ ] `dotnet test` green — **기존 테스트 회귀 0** (단일 맵 가정 깨지는 테스트는 Town 맵으로 정합)
- [ ] 서버 `dotnet run` 시 로그에 4맵이 각각 tick 도는 것 확인 (또는 `_maps.Count == 4` 단위 테스트)
- [ ] 플레이어 접속 시 여전히 Town(spawn 0,0)에 진입 — M4.1 데모 흐름 그대로 동작
- [ ] HuntingGround에 Normal enemy / BossRoom에 Boss가 각각 자기 맵에만 존재 (단위 테스트)

---

## 🧪 테스트

**자동**:
- `GameWorldTests` — `_maps.Count == 4`, 각 MapId로 GetMap 성공
- `GameMapTests` — Town `Enemies.Count == 0`, HuntingGround Normal 1, BossRoom Boss 1
- 기존 통합 테스트(M2BasicMovement 등) Town 맵 기준 회귀 0

**수동**:
- 서버 켜고 클라/봇 접속 → Town spawn + 기존 movement 정상

---

## 📚 학습 포인트

- **레지스트리 패턴**: 단일 객체 → 키 기반 컬렉션 승격. MMORPG "존(zone) 서버" 구조의 출발점.
- **Map = Actor (헌법/ARCHITECTURE)**: 각 맵이 자기 tick을 도는 독립 actor. 4맵이면 한
  scheduler가 4 actor를 순차 tick (현 단일 스레드 모델 유지 — 맵별 스레드는 M4+ 부하 시 검토).
- **ctor 의존성 주입 맛보기**: hardcode spawn → 구성 주입으로 바꾸면 맵마다 다른 콘텐츠 가능.

---

## ⚠️ 함정 / 주의사항

- `GameWorld.Instance` 싱글톤 패턴 유지 — `_maps`는 readonly, 외부 set 금지 (헌법: 정적 mutable 금지).
- 기존 `GameMap.NormalEnemySpawnX` 등 const는 **HuntingGround 좌표로 재해석** — 좌표값 자체는
  유지하되 "어느 맵의 좌표인가"만 바뀜. M3 3-zone 좌표(좌 마을/중 전투/우 보스)가 이제 맵 경계로 승격.
- `_nextEntityId`가 맵별로 분리되면 entity id 충돌 가능 — **맵 간 이동 시 id 정책**은 Phase 03에서
  결정 (본 Phase는 맵별 독립 풀로 두되, Phase 03에서 전역 풀 vs 맵별 풀 trade-off 박음).
- 단일 맵 가정한 기존 테스트가 깨질 수 있음 — Town 맵 기준으로 정합 (회귀 0 사수).

---

## ➡️ 다음 Phase

- Phase 02 — portal 정의 + S_MapTransition 패킷 + PDL bump

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit message만. -DONE.md 박지 않음.

---

## 작업 로그

- 2026-05-25: 계획 수립 (`/work:plan M4.2`)
</content>
