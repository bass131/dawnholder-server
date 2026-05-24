---
owner: youngho
milestone: M4.2
phase: 02
title: portal 정의 + S_MapTransition 패킷 + PDL bump
status: pending
grade: 복잡
risk: trust-boundary
estimated: 2~3h
domain: cross
---

# Phase 02: portal 정의 + S_MapTransition 패킷 + PDL bump

> **상태**: pending
> **마일스톤**: M4.2
> **등급**: 복잡 (shared+server 2도메인 / PDL 변경 = 양쪽 영향 / trust-boundary)
> **담당**: shared SubAgent (PDL) + server SubAgent (portal entity)

---

## 🎯 목표

맵 사이를 잇는 **portal**을 서버 권위로 정의하고, 맵 전환 패킷 2종을 PDL에 추가한다.
이 Phase는 **프로토콜 + 데이터 정의**까지만 — 실제 player migration 로직은 Phase 03.

- `C_EnterPortal` — 클라가 "이 portal로 들어가겠다"는 **의도만** 전송 (헌법 #3 — 좌표 X)
- `S_MapTransition` — 서버가 "너는 이제 맵 B로 간다, spawn 좌표 여기" 권위 통보
- portal entity — 맵별 portal 좌표 + 목적지 MapId (서버 정의)
- ProtocolVersion **5 → 6 bump** (PDL 모양 변경)

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 — `MapId` enum + 맵 레지스트리 존재 (목적지를 MapId로 지정하려면 필요)

---

## 📝 작업 내용

### PDL (shared)

- [ ] `99_Tools/PacketGenerator/PDL.xml` **맨 아래에 append** (헌법 #2 — 은퇴 ID 재사용 금지):
  - `C_EnterPortal { int portalId }` — 클라 의도. portalId만 (좌표/목적지 X — 서버가 결정)
  - `S_MapTransition { byte destMapId, float spawnX, float spawnY, int entityId }`
    — 목적지 맵 + spawn 좌표 + (재배정될 수 있는) entityId
- [ ] `Shared.GameData` 또는 `Protocol`에 `ProtocolVersion.Current` **5 → 6**
- [ ] **PacketGenerator 후속 3종 의무** (99_Tools/CLAUDE.md):
  - `dotnet run --project 99_Tools/PacketGenerator/` 재생성
  - `dotnet build Dawnholder.slnx` → Shared.dll 갱신 (PostBuild 자동 복사)
  - 세 산출물 동반 commit (PDL.xml + GenPackets.cs + Shared.dll)

### portal entity (server)

- [ ] portal 정의 — 맵별 portal 좌표 + 목적지. (예: Town 우측 끝 → HuntingGround,
      HuntingGround 우측 끝 → BossRoom). `GameMap`에 `IReadOnlyList<Portal>` 또는 const 정의.
  - `Portal { int PortalId, Vector2 Position, MapId Dest, Vector2 DestSpawn }`
- [ ] portal 좌표는 M3 3-zone 경계 좌표 재활용 (자연스러운 이동 흐름)
- [ ] **AddPlayer/Migration 로직은 박지 않음** — Phase 03. 본 Phase는 portal **데이터 + 패킷 정의**만.

---

## ✅ 완료 조건

- [ ] PDL 재생성 후 `GenPackets.cs`에 `C_EnterPortal` / `S_MapTransition` PacketID 생성 확인
- [ ] `ProtocolVersion.Current == 6` + handshake mismatch 테스트 (옛 v5 클라 거절)
- [ ] `dotnet build Dawnholder.slnx` 양쪽(server + ClientNet) 통과
- [ ] `dotnet test` green — handshake 버전 테스트 갱신 (5→6)
- [ ] portal 정의 단위 테스트 — 각 맵의 portal 목적지가 의도대로 (Town→HuntingGround 등)
- [ ] 세 산출물(PDL/GenPackets/Shared.dll) 동반 commit

---

## 🧪 테스트

**자동**:
- `PacketRoundTripTests` — C_EnterPortal / S_MapTransition write→read 일치
- `HandshakeHandlerTests` — clientVersion 5 (옛) → mismatch 거절, 6 → ok
- `PortalTests` — 맵별 portal 목적지 정합

**수동**:
- 봇/클라가 v6으로 handshake 통과 확인

---

## 📚 학습 포인트

- **헌법 #2 (Protocol is Sacred) 실전**: append-only ID 부여 + ProtocolVersion bump.
  은퇴 ID 재사용 금지 = wire 호환성 사고 방지.
- **헌법 #3 (Trust Boundary) 설계**: C_EnterPortal이 목적지/좌표를 **안 보내는** 이유 —
  보내면 클라가 임의 맵 순간이동(텔레포트 핵) 가능. 클라는 "의도(portalId)"만, 목적지는 서버 권위.
- **PDL → 코드 생성 파이프라인**: 수동 직렬화 코드 작성 금지. PDL 단일 출처 → 생성기 → 양쪽 공유.

---

## ⚠️ 함정 / 주의사항

- **PacketGenerator 후속 3종 누락 = 다른 머신 pull 빌드 회귀** (정유현 PR #19 사고 패턴, 99_Tools/CLAUDE.md).
- ProtocolVersion bump 잊으면 옛 클라가 새 서버에 붙어 디코드 깨짐 — handshake 게이트가 막아야 함.
- `S_MapTransition.entityId` — 맵 간 이동 시 entity id 유지/재배정 정책은 Phase 03에서 확정.
  본 Phase는 패킷 **필드만** 정의 (값 채우기는 Phase 03 migration 로직).
- portalId는 클라가 보내는 값이라 **untrusted** — Phase 03에서 범위/근접 검증 (헌법 #3).

---

## ➡️ 다음 Phase

- Phase 03 — 맵 간 player migration 로직 (본 Phase에서 정의한 패킷/portal 활용)

---

## 📋 박제 (완료 후)

- **복잡 등급** — 단, M4.2 마일스톤 -DONE.md는 Phase 05 마감 시 통합 박제. 본 Phase는
  work-pin + commit + (PDL 변경) CHANGELOG 후보 메모.

---

## 작업 로그

- 2026-05-25: 계획 수립 (`/work:plan M4.2`)
</content>
