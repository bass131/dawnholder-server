---
description: 새 패킷을 양쪽 wiring까지 한 번에 추가 (shared+server SubAgent 분담)
argument-hint: <C2S|S2C> <PacketName> <reason>
---

"새 패킷 추가" 워크플로우 시작. 사용자 요청:
**$ARGUMENTS**

---

### 등급 판정 (먼저)

새 패킷 = 보통/복잡 등급:

- **보통**: C2S/S2C 한 방향 + 핸들러 stub만 + ProtocolVersion bump 없음 → Worker 1개 위임
- **복잡**: ProtocolVersion bump 동반 (필드 모양 breaking change) → 자동 *irreversible* 깃발 발동 → 복잡 상향 → Coordinator + Worker 2개 + Reviewer

`risk-detector.sh` Hook이 `Protocol.Version` bump 자동 검출 시 등급 상향 + work-pin에 박음.

---

### 작업 흐름 (Coordinator 분해 권장)

#### Step 1. `shared` SubAgent 위임 (PDL + Protocol 정의)

[`../../agents/shared.md`](../../agents/shared.md) 호출 — 98_Shared/ 단독 게이트.

브리프:
1. 적절한 숫자 범위에서 다음 빈 PacketId 선택 (`98_Shared/CLAUDE.md` 참조). 방향은 첫 번째 인자.
2. `98_Shared/Protocol/PDL.xml`에 새 패킷 정의 추가 (append-only — 옛 ID 재사용 금지, 헌법 §2):
   ```xml
   <packet name="<PacketName>" id="<NN>" direction="<C2S|S2C>">
     <field name="..." type="..."/>
   </packet>
   ```
3. `PacketGenerator` 재실행 → `98_Shared/Protocol/Packets/<Name>.cs` 자동 생성 확인
4. `Shared.dll` 재빌드 + commit (`shared-discipline-guard.sh` Hook이 PDL.xml 변경 시 GenPackets stale + Shared.dll commit 누락 자동 차단)
5. ProtocolVersion bump 필요 여부 판단 — 기존 패킷 필드 모양 변경 없으면 bump X (헌법 §2 추가만 OK)

#### Step 2. `server` SubAgent 위임 (핸들러 + dispatch)

[`../../agents/server.md`](../../agents/server.md) 호출.

브리프:
1. `02_Server/GameServer/Handlers/`에 `<PacketName>Handler.cs` 신설 (`IPacketHandler` 구현)
2. `HandlerRegistry` Dictionary에 등록 (헌법 §3 신뢰 경계 — handler 본문에 6단 검증 패턴: handshake → target lookup → alive → rate-limit → range → mutation+broadcast)
3. C2S면 핸들러 본문 = decode + 검증 + GameSession 메서드 호출만 (tick thread는 EnqueueJob 람다로 위임)
4. S2C면 GameSession 또는 GameMap에서 Send/Broadcast 호출 박음 (BroadcastToAll 패턴 정합)

#### Step 3. `client` SubAgent 위임 (Unity wiring)

[`../../agents/client.md`](../../agents/client.md) 호출.

브리프:
1. C2S면 `03_Client/Assets/Scripts/Network/` 또는 `04_ClientNet/` 안 send helper 추가
2. S2C면 receive handler 추가 (Unity main thread dispatch — `MainThreadDispatcher` 정합)
3. 양쪽 빌드 OK 확인

---

### 자동 발동 Hook (M3.5 신규)

- **`shared-discipline-guard.sh`** — PDL.xml Edit/Write 시 GenPackets 재생성 누락 / Shared.dll commit 누락 자동 차단 (exit 2). 우회 불가.
- **`risk-detector.sh`** — ProtocolVersion bump 시 `irreversible` 깃발 발동 → 등급 자동 상향 + work-pin 박힘
- **`tdd-guard.sh`** — `Protocol/Packets/**` 변경 시 라운드트립 테스트 누락 *경고만*

---

### Reviewer 자동 호출 (Tier 2-A)

본 작업은 *무조건 호출* 조건 충족:
- `98_Shared/` 변경 포함 ✅
- 새 핸들러 추가 ✅

[`../../agents/reviewer.md`](../../agents/reviewer.md) 호출 — 5축 점검 (헌법 §2/§3 + ADR-002 + 테스트 커버리지 + 도메인 패턴).

---

### 사용자 보고

```
─────────────────────────────────────────
📦 새 패킷 추가 완료
─────────────────────────────────────────

패킷: <Name> (id=<NN>, direction=<C2S|S2C>)
등급: <보통/복잡> (위험 깃발: <flag 또는 없음>)

SubAgent 동원:
  - shared: PDL.xml + GenPackets + Shared.dll commit (커밋 <hash>)
  - server: <Name>Handler + Registry 등록 (커밋 <hash>)
  - client: send/receive wiring (커밋 <hash>)

Hook 발동:
  - shared-discipline-guard: PASS
  - <risk-detector / tdd-guard 발동 결과>

Reviewer (Tier 2-A): <✅ 통과 / 🔴 N개 위반>

➡️ 다음:
  - 라운드트립 테스트 추가 (PacketRoundTripTests.cs)
  - 핸들러 단위 테스트 (happy + invalid + auth)
  - work-pin 갱신
```

---

### 옛 슬래시와 차이

- **옛 `/work:new-packet`**: `netcode` SubAgent 단일 위임 → 패킷 모양 + 핸들러 + 클라 한꺼번에
- **새 `/work:new-packet`**: `shared`(PDL) + `server`(핸들러) + `client`(wiring) 3 SubAgent 분담 — 권한 경계 명확화 (헌법 §4 정합) + `shared-discipline-guard` Hook 자동 발동

---

### 함정

- **PDL.xml 수정 후 GenPackets 재실행 누락** — `shared-discipline-guard.sh`가 자동 차단. 우회 불가
- **Shared.dll commit 누락** — 정유현 머신 pull 시 빌드 회귀 (5/17 사고). Hook이 자동 차단
- **PacketId 재사용** — 헌법 §2 위반. PDL.xml은 append-only
- **클라이언트 입력 검증 누락** — 헌법 §3. handler 본문에 6단 검증 의무
