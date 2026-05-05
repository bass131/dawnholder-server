# Shared — Protocol & GameData

## ⚠️ Cross-Cutting 코드

**여기서 바꾸는 모든 것은 client와 server 양쪽에 영향을 줍니다.**
여기 breaking change = 프로덕션 desync. 편집증 수준으로 조심하세요.

## Layout

```
shared/
├── Shared.csproj       .NET Standard 2.1 라이브러리로 빌드
├── Protocol/
│   ├── PacketId.cs     모든 패킷 ID enum (값 절대 재사용 금지)
│   ├── Packets/        패킷당 파일 1개, [MessagePackObject]
│   └── ProtocolVersion.cs
└── GameData/
    ├── Formulas.cs     데미지, XP 곡선, 스탯 derivation
    ├── Constants.cs    Tick rate, 최대 패킷 크기, 타임아웃
    └── Tables/         정적 데이터: items, monsters, skills (보통 JSON 로드)
```

## 규칙

### PacketId
- 값은 영원히 stable. 제거된 패킷은 `[Obsolete]`로 마킹, 절대 삭제 안 함.
- 패킷은 방향별로 그룹: `C2S_*` (client→server), `S2C_*` (server→client).
- 숫자 범위: 1–999 system, 1000–1999 auth, 2000–2999 movement,
  3000–3999 combat, 4000–4999 inventory, 5000–5999 chat, 등.

### Packet structs
```csharp
[MessagePackObject]
public class C2S_Move {
    [Key(0)] public int Tick;
    [Key(1)] public sbyte InputX;     // -1, 0, 1
    [Key(2)] public bool JumpPressed;
}
```
- 항상 명시적 `[Key(N)]`. Contractless resolver 절대 금지.
- 더 높은 `[Key]`로 새 필드 추가는 backward-compatible.
- key 제거나 재정렬은 BREAKING change. `ProtocolVersion`을 bump.

### Formulas
- 순수 함수만. `DateTime.Now` 금지, seed 없는 random 금지.
- 같은 입력은 client와 server에서 같은 출력 (prediction을 위해).
- 공식이 RNG를 쓴다면 seed는 server tick + entity id에서 생성.

## Protocol 버전 핸드셰이크

클라이언트는 첫 패킷에 자신의 `ProtocolVersion`을 보냅니다. 서버는 mismatch
시 명확한 에러 코드로 거부. "관대하게 처리" 금지 — silent mismatch는
hard error보다 나쁩니다.

## 변경 머지 전

실행: `dotnet build client/ && dotnet build server/` — 둘 다 통과해야 함.
`validate-shared-changes.sh` 훅이 편집 시 자동으로 이걸 합니다.
