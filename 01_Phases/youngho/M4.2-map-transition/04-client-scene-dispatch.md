---
owner: youngho
milestone: M4.2
phase: 04
title: 클라 4 scene dispatch + portal UX
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~5h
domain: client
---

# Phase 04: 클라 4 scene dispatch + portal UX

> **상태**: pending
> **마일스톤**: M4.2
> **등급**: 복잡 (client + Unity scene/prefab 자산 변경 = unity-asset 위험 깃발)
> **담당**: client SubAgent (네트워크 wiring) + **본인 직접** (Unity scene/portal 외관)

---

## 🎯 목표

서버 `S_MapTransition` 수신 시 **Unity scene를 4맵에 맞게 전환**하고, 플레이어가 portal에
닿으면 `C_EnterPortal` 의도를 송신한다. 맵 이동 시 본인 캐릭터 + remote entity가 새 맵
기준으로 올바르게 재구성된다.

**역할 분담** (memory `unity-visual-work-user-owned`):
- **본인 직접**: scene 4개 구성 / portal sprite·트리거 배치 / 맵별 배경·타일 외관
- **AI(client SubAgent)**: S_MapTransition 핸들러 / scene 전환 wiring / C_EnterPortal 송신 코드 /
  entity id 재배정 반영 / remote entity 재구성

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 — `C_EnterPortal` / `S_MapTransition` 패킷 (Shared.dll 클라 복사됨)
- [ ] Phase 03 완료 — 서버 migration 로직 (end-to-end 검증하려면 서버 핸드오프 필요)
- [ ] (병렬 가능) scene 외관 골격은 Phase 02/03 진행 중 본인이 미리 작업 가능

---

## 📝 작업 내용

### 본인 직접 (Unity 외관)

- [ ] 4 scene 또는 4 맵 구성 — Town / HuntingGround / BossRoom / Ending
  - ADR-021 정합: UI는 Additive Scene 분리 유지. 맵 전환 방식은 single scene 내 맵 swap vs
    scene 전환 — 본인 판단 (둘 다 가능, scene 전환이 직관적)
- [ ] portal sprite + 위치 배치 (서버 portal 좌표와 정합 — Phase 02 좌표 참조)
- [ ] 맵별 배경/타일 외관 (placeholder OK — PRD "무료 에셋 + placeholder")

### AI (client SubAgent — 네트워크 wiring)

- [ ] `S_MapTransition` 핸들러 — destMapId → scene 전환 + spawn 좌표로 본인 캐릭터 배치
- [ ] portal 트리거 감지 → `C_EnterPortal { portalId }` 송신 (입력 → intent, 헌법 #1)
- [ ] entity id 재배정 반영 — S_MapTransition.entityId로 로컬 player entity id 교체
- [ ] 맵 전환 시 remote entity 정리 + 새 맵 roster(S_PlayerJoin/S_EntitySpawn) 재구성
- [ ] reconciliation/prediction이 맵 전환 경계에서 깨지지 않게 (전환 시 prediction 버퍼 리셋)

---

## ✅ 완료 조건

- [ ] Unity Play 모드에서 portal 밟으면 scene 전환 + 본인 캐릭터 spawn 좌표 배치
- [ ] 맵 이동 후 enemy(사냥터 Normal / 보스방 Boss)가 해당 맵에서 보임
- [ ] 맵 전환 후 movement prediction/reconcile 정상 (떨림/순간이동 없음)
- [ ] Unity 콘솔 에러/경고 0 (전환 흐름) — **콘솔 직접 확인**으로 검증 (memory
      `mcp-unity-console-empty-diagnosis`: MCP 빈 응답 ≠ 통과. 빈 응답이면 콘솔 육안 + 서버 로그 역추적)
- [ ] **반드시 백업** — scene/prefab 편집 전 백업 (Phase 08 BackGround prefab 사고 학습, unity-bridge)

---

## 🧪 테스트

**수동** (Unity Play 모드):
- 마을 → portal → 사냥터: scene 전환 + enemy 보임 + 공격 가능
- 사냥터 → portal → 보스방: 보스 보임
- 왕복(보스방 → 사냥터 → 마을): 캐릭터 state 유지, 떨림 없음

**자동**:
- 클라 네트워크 로직은 04_ClientNet.Tests 또는 헤드리스 봇으로 (Phase 05 통합)

---

## 📚 학습 포인트

- **클라 = 렌더러 (헌법 #1)**: 맵 전환도 서버 S_MapTransition이 권위. 클라가 "내가 도착했다"
  자체 판정 X — 서버 통보 받고 그제서야 scene 전환.
- **scene 전환과 네트워크 상태 동기화**: scene 바뀌어도 소켓 연결은 유지 — 연결을 끊지 않고
  논리적 맵만 전환. (재접속 X)
- **prediction 경계 처리**: 맵 전환 시 좌표계가 바뀌므로 prediction 버퍼를 리셋하지 않으면
  옛 맵 좌표로 reconcile해서 캐릭터가 튐.

---

## ⚠️ 함정 / 주의사항

- **scene/prefab 백업 의무** (unity-asset 위험 깃발) — Phase 08 BackGround prefab 사고 재발 방지.
- **DLL 동기화** — Phase 02에서 Shared.dll(v6)이 `03_Client/Assets/Plugins/`로 복사됐는지 확인.
  안 되면 클라가 옛 패킷 정의로 디코드 깨짐.
- **MainThreadDispatcher** — S_MapTransition 같은 네트워크 콜백은 IOCP 스레드 → Unity 메인
  스레드로 마샬링 후 scene 전환 (Unity API는 메인 스레드 전용).
- entity id 교체 타이밍 — 새 id 받기 전 옛 id로 들어온 snapshot 처리 주의.

---

## ➡️ 다음 Phase

- Phase 05 — 통합 검증 + 봇 맵 이동 시나리오 + 마감

---

## 📋 박제 (완료 후)

- **복잡 등급** — M4.2 마일스톤 -DONE.md는 Phase 05 통합 박제. 본 Phase는 work-pin + commit.

---

## 작업 로그

- 2026-05-25: 계획 수립 (`/work:plan M4.2`)
</content>
