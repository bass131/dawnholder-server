# Phase 04: 서버 Broadcast 인프라 (PlayerJoin/Leave + multi-target Snapshot)

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 2h
> **담당 에이전트**: gameplay + netcode

---

## 🎯 목표

두 봇 접속 시 서버가 *같은 맵 전원에게* broadcast. 현재 `GameMap.cs:95` snapshot은 owner unicast만 — 이걸 multi-target broadcast로 변환. Initial roster + PlayerJoin/Leave 박음.

## ⏪ 사전 조건

- [ ] Phase 03 완료 (핸들러 layer 정합)

---

## 📝 작업 내용

- [ ] PDL — `S2C_PlayerJoin { uint entityId, position, ... }`, `S2C_PlayerLeave { uint entityId }` 신설
- [ ] *(Codex 4번 단서)* Snapshot 단일 entity → multi-entity 형태 변경 검토. 변경 시 PDL 변경 동반 (예: `S2C_Snapshot { uint frame, EntityState[] entities }`)
- [ ] `GameMap.cs:95` owner unicast → 같은 맵 전원 broadcast로 변환
- [ ] **Initial roster** = 새 플레이어 접속 시 *기존 플레이어 목록* 전송 (`S2C_InitialRoster` 또는 PlayerJoin 다발)
- [ ] Disconnect 시 PlayerLeave broadcast (자기 자신은 받지 않게)
- [ ] PacketGenerator 재생성 (`--no-manager`) + Shared.dll 빌드 + commit
- [ ] 핸들러 단위 테스트 + 간단 통합 (헤드리스 봇 2개 connect)

## ✅ 완료 조건

- [ ] 두 봇 접속 → 서로 PlayerJoin 받음 + initial roster
- [ ] 한 봇 disconnect → 남은 봇 PlayerLeave 받음
- [ ] Snapshot이 같은 맵 전원에게 도착
- [ ] handler 단위 테스트 (invalid sender / auth 실패 페어)
- [ ] PDL 변경 의무 3종 박힘

---

## 🧪 테스트

**자동**: BroadcastTests — 2 mock session 접속/이탈 시 PlayerJoin/Leave 양쪽 도착
**수동**: 헤드리스 봇 2개 + 서버 = 로그에서 broadcast 발신 확인

---

## 📚 학습 포인트

- **Broadcast vs Unicast** — 같은 패킷을 N개 세션에 보낼 때 fan-out 패턴
- **Initial roster** — 재진입자가 기존 상태를 받는 패턴. 게임 dev 보편 (lobby/raid)
- **Lifecycle race 재발 risk** — M2.5 Phase 10에서 본인이 봉합한 `_closing` + always-enqueue 패턴이 *multi-player에서 재발할 수 있음*. Codex가 *가장 큰 risk 1*로 짚음

---

## ⚠️ 함정 / 주의사항

- **Lifecycle race 재발** — Phase 10 봉합 정신을 멀티에서 일반화. "한 봇 disconnect 중 다른 봇이 그 봇 entity에 메시지 보내면?" 시나리오 deterministic 재현 테스트 박을 것
- Snapshot multi-target 시 PDL 형태 변경 동반 → 의무 3종 또 박힘
- Initial roster 누락 시 본인 외 다른 봇 화면에서 안 보임
- Disconnect 시 자기 자신은 PlayerLeave 받지 않게 (자기 entity로 자기 entity despawn 처리하면 본인 캐릭터 사라짐)
- Broadcast 발신 시 disconnect 중인 세션은 skip (Phase 10 패턴)

---

## ➡️ 다음 Phase

Phase 05 — 클라 Remote Entity Registry (가장 무거움 ★)

---

## 작업 로그

- 2026-05-18: pending (Codex β 검토: PDL 변경 가능 단서 박힘, lifecycle race 재발 risk 1순위)
