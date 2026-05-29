---
owner: youngho
milestone: M4.3R
phase: 02
title: 클라 패킷 dispatch 분리 (UnityClientSession → IPacketHandler + RosterTransitionBuffer + SceneRouter)
status: done
grade: 복잡
domain: client
estimated: 3~4h
---

# Phase 02: 클라 패킷 dispatch 분리 (rank 1)

> **상태**: pending
> **마일스톤**: M4.3R
> **등급**: 복잡 (665줄 God class, 12 핸들러 + 5 책임 분해)
> **담당**: client SubAgent

---

## 🎯 목표

`UnityClientSession`(665줄)의 거대 inline switch(12 패킷 직접 dispatch)를 **서버 `Handlers/` 패턴을 미러한 `IPacketHandler` + dispatch 테이블**로 바꾼다(§3.2). 동시에 한 클래스에 섞인 5개 책임 중 분리 이득이 명확한 둘을 추출한다: 맵 전환 roster buffer 상태머신 → `RosterTransitionBuffer`, `MapIdToSceneName` 매핑 → `SceneRouter`. **동작은 완전 보존** — 어떤 패킷도 처리 누락 없이 똑같이 흐른다.

서버 `Handlers/`(IPacketHandler + HandlerRegistry + Dictionary dispatch + "핸들러 추가 절차" 문서)가 이미 성숙해 **미러할 reference 패턴이 존재** = 선행 블로커 없음.

---

## ⏪ 사전 조건

- [ ] Phase 01 베이스라인 (회귀 비교 기준) — 권장 (없어도 진행 가능, 독립)
- [x] 서버 `02_Server/GameServer/Handlers/` 패턴 성숙 (미러 대상)

---

## 📝 작업 내용

- [ ] `IClientPacketHandler` 인터페이스(또는 delegate) 정의 — 서버 `IPacketHandler` 미러
- [ ] `Dictionary<PacketID, handler>` dispatch 테이블 — 12 패킷(S_HandshakeResult/S_Pong/S_EnterMap/S_Snapshot/S_PlayerJoin/S_PlayerLeave/S_EntitySpawn/S_HitResult/S_EntityDeath/S_StageClear/S_MapTransition) 등록
- [ ] `OnRecvPacket`의 inline switch(L194~247) → dispatch 테이블 lookup으로 교체. 미등록 PacketID 방어 로그 유지
- [ ] `RosterTransitionBuffer` 추출 — `_pendingMapTransition`/`_rosterBuffer`/`OnSceneLoadedForRosterDrain`(sceneLoaded 구독 + drain). 3곳에 복붙된 overflow 가드(L355~371/395~408/466~489)를 진입 직후 공통 게이트 1곳으로 응축
- [ ] `SceneRouter`(또는 MapSceneMap) 정적 헬퍼 — `MapIdToSceneName` 분리 (클라 표현 매핑)
- [ ] `UnityClientSession`은 framing + dispatch lookup + main-thread 마샬링 컨테이너 경계만 잔류
- [ ] (가능 시) 추출된 핸들러 일부 EditMode 단위 테스트 추가 (dispatch 테이블 lookup 검증)

### ⚠️ 분리 금지 (§0.3)
- [ ] **pending spawn static 3필드**(`PendingSpawnX`/`PendingSpawnY`/`HasPendingSpawn`)는 `LocalPlayerController.Awake`가 실제 소비 → **억지 추출 금지**. 컨테이너 잔류. (추출하면 호출 경로만 늘고 가독 이득 0)

---

## ✅ 완료 조건

- [ ] `UnityClientSession.cs` < 600줄 (size-guard 경고 해소)
- [ ] 12 패킷 전부 dispatch 테이블 경유 (inline switch 0)
- [ ] Unity 컴파일 green (에러/경고 0)
- [ ] **동작 보존**: 헤드리스 봇 스모크(MapTransition/EnemyAi/BossStageClear) Phase 01 baseline 대비 회귀 0
- [ ] overflow 가드 복붙 3곳 → 1곳 응축
- [ ] reviewer 헌법 hard 위반 0 (§3.2 미러 + §0.3 과분할 점검 = 축6)

---

## 🧪 테스트

**자동**: (추가 시) dispatch 테이블 lookup EditMode 테스트. 기존 InputHistoryTests 회귀 0.
**수동**: Play로 마을→사냥터 전환 + 적 spawn/snapshot 수신 정상 확인 (roster drain 동작).

---

## 📚 학습 포인트

- **dispatch 테이블 vs switch**: 패킷 수가 늘면 switch는 한 메서드가 비대해지고 추가마다 두 곳(switch+메서드)을 손댐. `Dictionary<ID, handler>`는 등록 한 줄로 확장 — OCP(개방-폐쇄)의 구체화(§0.5).
- **서버/클라 대칭 패턴**: 같은 dispatch 구조를 양쪽이 쓰면 "한쪽 이해 = 다른 쪽 이해". 인지 부담 ↓ (§0.2).
- **상태머신 추출**: roster buffer는 "전환 대기 → 씬 로드 → drain" 명확한 상태 흐름 → 별 클래스로 떼면 UnityClientSession이 가벼워짐.

---

## ⚠️ 함정 / 주의사항

- **동작 보존이 절대 — 순수 리팩토링**: 패킷 처리 로직을 *옮기기만*. 새 동작/최적화 끼우지 말기. 누락 패킷 = 조용한 사고.
- **PacketSession 상속 + main-thread 마샬링 보존**: Unity는 메인 스레드에서만 GameObject 조작. dispatch가 main-thread 큐를 우회하면 크래시.
- **§0.3 과분할 경고**: 5 책임을 다 쪼개려 들지 말기. dispatch + roster buffer + SceneRouter만 이득 명확. pending spawn static은 잔류.
- **Phase 08(M4.3 enemy AI 클라) 시너지**: 이 Phase를 08보다 먼저 = 08 신규 패킷이 switch에 쌓이지 않고 dispatch 테이블에 등록 한 줄로 들어감.

---

## ➡️ 다음 Phase

- Phase 04 (GameSession) — 단 03 후. 또는 Phase 05/06 (client 후속).

---

## 📋 박제 (완료 후)

- **복잡 등급** — `02-client-packet-dispatch-DONE.md` 박음.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
