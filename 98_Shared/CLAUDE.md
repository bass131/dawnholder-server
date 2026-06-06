# Shared — Protocol & GameData

## ⚠️ Cross-Cutting 코드

**여기서 바꾸는 모든 것은 client와 server 양쪽에 영향을 줍니다.**
여기 breaking change = 프로덕션 desync. 편집증 수준으로 조심하세요.

**경계**: Shared는 *양쪽 동기 필수인 cross-cutting*만 담음 — 패킷 정의 + 게임 데이터.
**socket 인프라 (Connector/Listener/Session/Buffer 등)는 양쪽 분리** (Y2 갈래, ADR-012). 책임 단위 표는 ADR-012 본문 참조.

## Layout

```
98_Shared/
├── Shared.csproj         .NET Standard 2.1 라이브러리로 빌드 (ADR-010)
├── Protocol/
│   ├── Generated/        ★ 자체 PDL이 자동 생성한 패킷 클래스 (ADR-002 v2)
│   │   └── GenPackets.cs   PacketID enum + IPacket + C_Xxx/S_Xxx 클래스
│   └── ProtocolVersion.cs  Current=8 정의 (M4.3 Phase 08a bump — S_Snapshot/S_EntityState에 animState append) — M4.3 Phase 07 v7 bump (S_EntityState 추가, enemy AI 위치/상태 브로드캐스트) — M4.2 Phase 02 v6 bump (C_EnterPortal/S_MapTransition 추가, 맵 전환) — M4.1 Phase 06 v5 bump (C_Attack.attackerClientTick, lag compensation) — M3.8 PR #49 v4 bump (C_CharacterSelect) — M3 Phase 06 v3 bump (C_Attack/S_EntitySpawn/S_HitResult/S_EntityDeath/S_StageClear) — 핸드셰이크 코드 M3 Phase 02 봉합 완료
└── GameData/
    ├── Constants.cs      Tick rate, 최대 패킷 크기, 타임아웃
    ├── InputBits.cs      MoveIntent 입력 비트필드 인코딩/디코딩 (M2 Phase 07 박힘)
    ├── Physics.cs        결정론 Step 함수 — 클라/서버 동일 시뮬 (Time.deltaTime 직접 사용 금지)
    ├── PlayerStats.cs    플레이어 스탯 (M4.1 Phase 05에 02_Server에서 이동, 헌법 #4 정합) — Class/Hp/MaxHp/Attack/Defense/MoveSpeed, private ctor + factory(Warrior/Ranger) 패턴
    ├── Formulas.cs       데미지 공식 (M4.1 Phase 05 박힘) — ComputeDamage(PlayerStats, EnemyStats, int) → int + EnemyStats struct (M4.3 Phase 07: MoveSpeed/AggroRange/PatrolRange 추가, NormalDefault() factory). XP 곡선/스탯 derivation은 M5+
    └── Tables/           (M5 진입 시 박힘 예정 — 현재 미박힘) 정적 데이터: items, monsters, skills (보통 JSON 로드)

> M3.6 Phase 04 점검 발견 (2026-05-22): 옛 Layout 표가 Formulas.cs/Tables/를 *현재 존재*하는 것처럼 박혀있었고 (false-promise 5번째 발본), 실재 InputBits.cs/Physics.cs는 약속 미박힘이었음 (역방향 격차). 본 정정 = M4 진입 예정 명시 + 실재 파일 박음.
```

## 규칙

### PacketId
- 값은 영원히 stable. 은퇴한 ID는 *PDL.xml에서 통째 제거 X* (재사용 방지). 주석으로 deprecated 표시.
- 패킷은 방향별 접두사 (Rookiss 패턴, ADR-012):
  - **`C_*`** = Client → Server
  - **`S_*`** = Server → Client
  - 생성기가 접두사로 *클라/서버 dispatch table 자동 분리* (M2.5+ 핸들러 layer 분리 시점)
- 숫자 범위 예약 (PDL.xml 정의 순서대로 자동 부여, 충돌 방지):
  - 1–999 system (Ping/Pong, Heartbeat, Disconnect 등)
  - 1000–1999 auth, 2000–2999 movement, 3000–3999 combat,
    4000–4999 inventory, 5000–5999 chat 등

### Packet 정의 (PDL.xml + 자동 생성)
*수동 작성 X*. PDL.xml 단일 소스 → 코드 생성기 (`99_Tools/PacketGenerator/`).

```xml
<!-- PDL.xml -->
<packet name="C_Move">
    <int name="tick"/>
    <sbyte name="inputX"/>     <!-- -1, 0, 1 -->
    <bool name="jumpPressed"/>
</packet>
```

→ 생성된 `C_Move` 클래스 (98_Shared/Protocol/Generated/GenPackets.cs):
- `BinaryPrimitives.*LittleEndian` 명시 (wire format = 플랫폼 무관 약속)
- `Read(ArraySegment<byte>)` / `Write() : ArraySegment<byte>` (byte[] 반환)
- SendBufferHelper 의존 X — 양쪽 socket 인프라 분리(ADR-012)와 정합

**필드 추가는 backward-compatible** — PDL.xml 끝에 추가만. 재정렬·제거는 BREAKING (Protocol.Version bump).

새 패킷 추가는 `/work:new-packet <C_|S_> <name>` 슬래시 커맨드 사용.

### Formulas
- 순수 함수만. `DateTime.Now` 금지, seed 없는 random 금지.
- 같은 입력은 client와 server에서 같은 출력 (prediction을 위해).
- 공식이 RNG를 쓴다면 seed는 server tick + entity id에서 생성.

## Protocol 버전 핸드셰이크

클라이언트는 첫 패킷에 자신의 `ProtocolVersion`을 보냅니다. 서버는 mismatch
시 명확한 에러 코드로 거부. "관대하게 처리" 금지 — silent mismatch는
hard error보다 나쁩니다.

## 변경 머지 전

실행: `dotnet build Dawnholder.slnx` — 통과해야 함. (Unity 측은 `98_Shared/` DLL이 `03_Client/Assets/Plugins/Shared/`로 자동 복사된 뒤 Unity 컴파일을 통해 별도 검증.)
`shared-discipline-guard.sh` 훅 (PreToolUse Edit|Write 차단, ADR-022 — 옛 `validate-shared-changes.sh` rename + 강화)이 편집 시 자동으로 이걸 합니다.
