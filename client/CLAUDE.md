# Client — Unity 2D 사이드스크롤

## Layout

```
client/Assets/
├── Scripts/
│   ├── Network/        TCP 클라이언트, 패킷 read 루프, dispatch
│   ├── Prediction/     클라이언트 prediction + reconciliation
│   ├── Rendering/      Sprite, Animator 훅, Camera
│   ├── Input/          Input → intent packet
│   ├── UI/             HUD, 메뉴
│   └── State/          서버 상태의 로컬 미러 (read-only view)
├── Scenes/
├── Prefabs/
└── Resources/
```

## 컨벤션

- Unity 6.4 LTS. URP 2D renderer.
- 새 Input System 패키지 사용. 레거시 `Input.GetKey` 금지.
- 모든 네트워크 코드는 **메인 스레드 밖**에서 read; 메인 스레드 dispatch는
  thread-safe 큐를 `Update()`에서 비우는 방식.
- 한 개념당 한 `MonoBehaviour`. 갓-오브젝트 금지.

## ⚠️ Authoritative 경계선

클라이언트는 두 종류의 상태를 가집니다:

1. **Predicted state** — 플레이어가 지금 보는 것 (로컬 캐릭터 한정).
   반응성을 위해 입력 즉시 업데이트.
2. **Authoritative state** — 서버가 말하는 것. 패킷 수신 시 업데이트.

둘이 충돌하면 **authoritative가 이김**. `Prediction/`의 reconciliation
코드가 서버 진실에 snap 또는 보간으로 맞춥니다.

로컬 플레이어가 아닌 모든 것(다른 플레이어, 몬스터, 바닥의 아이템)은
predicted state가 **없음**. 서버 브로드캐스트의 순수 미러이며, 부드러움을
위해 스냅샷 사이를 보간만 합니다.

## 금지 사항

- 데미지, XP, drop table, 보상 계산을 로컬에서 수행.
- 서버 확인 패킷 없이 인벤토리, 스탯, 통화 변경.
- 게임플레이 타이밍에 `Time.time` 사용 — 서버 tick 사용.
- 클라 스크립트에 게임 밸런스 숫자 하드코딩. `shared/GameData/`에서 가져올 것.

## 네트워크가 필요한 기능을 추가할 때

여기서 패킷을 추가하지 않습니다. 먼저 `netcode` 에이전트에게 요청.
플로우는: 서버가 진실 정의 → shared에 패킷 정의 → 클라이언트가 렌더링.
