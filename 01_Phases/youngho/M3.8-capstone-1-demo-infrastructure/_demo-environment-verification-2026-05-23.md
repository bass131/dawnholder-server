---
owner: youngho
milestone: M3.8
phase: 05
title: Demo 환경 검증 노트 — 로컬 백업 + Hamachi
date: 2026-05-23
status: in-progress (로컬 백업 = 진행 중 / Hamachi = pending)
---

# Demo 환경 검증 노트 (M3.8 Phase 05 5-A)

> 본 노트 = 캡스톤 1 시연 환경 두 갈래 검증 박제.
> - **백업 시나리오** (본인 단독 로컬 데모) = 본 세션 검증 박음
> - **Hamachi 가상 LAN** (본인 + 정유현 2인) = 정유현 시간 조율 후 별 세션 박음

---

## 1. 로컬 백업 시나리오 (본인 머신 2 Unity 인스턴스)

### 환경

- **서버** = `Dawnholder.Server` (port 7777, `IPAddress.Any` listen)
- **클라 1** = Unity Editor Play (`03_Client/`)
- **클라 2** = Unity Build 산출물 `.exe` (`03_Client/Builds/`)
- **연결 주소** = `localhost:7777` 또는 `127.0.0.1:7777`

### 빌드 정보

- 빌드 일시: 2026-05-23 (본 세션)
- 빌드 산출물: `C:/Dev/Build/client.exe` (Unity 표준 출력 풀세트 + `client_Data/` 등)
- Build Settings Scenes: MainMenu (0) / CharacterSelect (1) / Gameplay (2) / Ending (3) _(본인 확인 의무)_
- 빌드 결과: ✅ 빌드 성공

### 검증 절차

1. [ ] 서버 부팅 — PowerShell에서 `dotnet run --project 02_Server/GameServer/GameServer.csproj`
2. [ ] `netstat -an | findstr 7777` → `LISTENING` 확인
3. [ ] Unity Editor Play (인스턴스 1) — MainMenu → 시작 → CharacterSelect → 전사/원거리 선택 → Gameplay 진입
4. [ ] `.exe` 실행 (인스턴스 2) — 동일 흐름
5. [ ] 인스턴스 1 화면에 인스턴스 2 캐릭터 표시 확인 (broadcast 작동)
6. [ ] 인스턴스 2 화면에 인스턴스 1 캐릭터 표시 확인 (broadcast 작동)
7. [ ] 둘 다 움직이면 서로 화면에 위치 갱신되는지 확인 (server-authoritative movement)
8. [ ] NPC 인터랙션 (E 키) 둘 다 작동 확인
9. [ ] Stage Clear → Ending → MainMenu 흐름 둘 다 작동 확인

### 검증 결과

- 서버 listen: ✅ (port 7777)
- 2 Unity 인스턴스 동시 실행 (Editor + .exe): ✅ (runInBackground=1 봉합 후)
- 2인 같은 맵 broadcast (서로 화면에 보임): ✅
- Remote entity 보간 부드러움: ✅ (10Hz broadcast + 150ms 보간 윈도우)
- 점프 reconcile 끊김: ✅ 봉합 (SnapThreshold 1.5f)
- 지면 점프 / 공중 점프 차단 / 착지 후 재점프: ✅ (OnJump 시점 OnGround 게이트)
- 서버 콘솔 spam 차단: ✅ (OnSend 제거 + OnRecv 드문 패킷만 박음)

**본인 통찰 (★★★ 학습 자산)**:
> "애초에 클라이언트에서 입력 컨트롤을 잘 해줘야 서버에 쓸데없이 검증로직이 또 돌아서 Reconcile을 하니까 끊겨 보이는 거였네"

본 통찰 = **클라 입력 컨트롤 패턴**. 헌법 #1 (Server Authority)의 *건강한 보완*:
- 서버 권위 = 보안 게이트 (cheat 차단)
- 클라 입력 컨트롤 = *부적절 입력 송신 차단* = UX 게이트
- 둘은 *대체* 관계 아닌 *보완* 관계. 클라가 무책임하면 서버 reconcile 폭증 → 시각 끊김
- 일거삼득: 시각 부드러움 ↑ / 대역폭 ↓ / 서버 부담 ↓

**잔여 아쉬움** (본 마감 별 시점 봉합 가능):
> "BroadCast하고 다른 유저가 보는 로컬유저의 반응속도가 떨어지는게 살짝 아쉽긴 하네"

진단 = remote entity 보간 윈도우 150ms = *다른 유저가 본인을 150ms 늦게 봄* (본인은 prediction으로 즉시). jitter 흡수 trade-off. M4+ 봉합 후보:
- (A) Broadcast Hz ↑ (10Hz → 20Hz 모든 tick) + InterpolationDelay 더 줄임
- (B) Extrapolation 박음 (현재 buffer 빔 시 last-known 유지 → 예측 점프 박음)
- (C) Adaptive InterpolationDelay (RTT 변동 따라 동적 조정)
- 본 세션은 보류 = 캡스톤 1 시연 정합 충분, 본 마감(11/19) 본격 정밀화 시점.

### 발견 결함 / 봉합

본 세션 2 Unity 인스턴스 검증 도중 발견된 결함 4건 (★★★ 학습 후보):

1. **runInBackground 꺼짐** — Editor + .exe 둘 다 Focus 벗어나면 일시정지 → 2 인스턴스 동시 운영 불가. ProjectSettings.asset `runInBackground: 0 → 1` 봉합. Unity 학부생 함정 1순위 (Window→Player→Run In Background).

2. **Remote entity 보간 어색** — 옛 SnapshotTickInterval 5 (250ms broadcast 4Hz) + InterpolationDelay 200ms 콤보 → buffer 매번 50ms 빔 → "정지 + 점프" 패턴 반복. **봉합** = SnapshotTickInterval 5 → 2 (100ms broadcast 10Hz) + InterpolationDelay 0.2 → 0.15 (Constants.cs + RemoteEntity.cs).

3. **점프 중 reconcile 끊김** — 결함 #2 봉합 직후 발견 = broadcast 4배 ↑ 부작용. 100ms마다 mispredict 검사 → 점프 중 클라 가변 dt vs 서버 fixed dt drift 누적이 SnapThreshold 1.0f 초과 → snap reconcile 폭증. **봉합** = SnapThreshold 1.0f → 1.5f (PlayerPredictor.cs). 작은 drift 흡수, 큰 cheat (텔레포트)은 서버 권위 유지.

4. **점프 중 재점프 입력 송신** — 점프 중 점프 누름 → jumpEdge=true 송신 → 서버 거절 + 클라 Physics.Step OnGround 안전망으로 점프 X. 단 매 송신마다 1 packet 폭증 + reconcile 검사 빈도 ↑.
   - **1차 봉합 (결함)**: cadence 송신 시점에 `jumpEdge && _predictor.OnGround` 게이트 박음.
   - **결함 발견**: Update에서 `Predict(jumpEdge=true)` 먼저 호출 → Physics.Step 점프 적용 → OnGround=false → 같은 Update 끝 cadence 게이트에서 `_predictor.OnGround=false` → **지면 첫 점프도 차단**. 클라는 점프하지만 서버는 신호 못 받음 → reconcile snap으로 ground 복귀 = 점프 안 되는 것처럼 보임.
   - **2차 봉합 (정합)**: 게이트를 *OnJump 시점*으로 옮김 — `if (value.isPressed && _predictor.OnGround) _jumpEdgeThisTick = true;`. 점프 *입력 시점* OnGround 검사 = 정확 (착지 직후 재점프 OK, 공중 점프 차단). cadence 게이트는 원복.
   - **학습 ★★★** — *게이트 위치가 의미를 결정함* 패턴. "공중 점프 차단" 의도였지만 *언제* 검사하는지에 따라 효과 정반대. Predict 호출 전/후 OnGround 상태 차이 인지 의무.

5. **OnSend 콘솔 spam 결함 (시각 착시)** — `[GameSession] OnSend 32 bytes` 폭증 발견. server SubAgent 진단 결과 = 호출 빈도 자체는 정상 (M×N×10Hz = 40/sec for N=2 클라), `Console.WriteLine` 폭증이 *시각적 누적 결함* 인상. **봉합** = OnSend 로그 제거 (GameSession.cs, 빈 메서드). 디버그 필요 시 M5+ Serilog 도입 시 Trace 레벨.

6. **패킷 종류 추적 부재** — OnSend 로그 제거 후 디버그 가시성 부족 발견. **봉합** = OnRecvPacket에 PacketID 로그 추가 (GameSession.cs). 단 C_MoveIntent + C_Ping (spam)은 제외 — OnSend 시각 spam 패턴 반복 차단.

**산출물 4 파일 변경**:
- `98_Shared/GameData/Constants.cs` — SnapshotTickInterval 5 → 2 + 주석 갱신
- `03_Client/Assets/Scripts/State/RemoteEntity.cs` — InterpolationDelay 0.2 → 0.15
- `03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs` — SnapThreshold 1.0 → 1.5
- `03_Client/Assets/Scripts/Input/LocalPlayerController.cs` — jumpEdge && OnGround 게이트
- `02_Server/GameServer/Network/GameSession.cs` — OnSend 로그 제거 + OnRecv 로그 추가
- `03_Client/ProjectSettings/ProjectSettings.asset` — runInBackground 0 → 1

(워킹 디렉토리에 cloud 라인 변경 박혀있지만 pre-commit hook이 자동 unstage)

---

## 2. Hamachi 시나리오 (정유현 동반)

### 상태

- **pending** — 정유현 시간 조율 박힌 후 본 노트에 추가 박음
- 시간 조율 트리거 = 본 세션 마감 시점 또는 별 채널 (디스코드/카카오)

### 환경 (예정)

- **서버** = 본인 머신 `Dawnholder.Server` (port 7777)
- **클라** = 정유현 머신 Unity 클라
- **연결 주소** = 본인 Hamachi 가상 IP (`25.x.x.x` 형태)
- **방화벽** = Windows 방화벽 TCP 7777 예외 추가 (본인 + 정유현 머신)

### 검증 절차 (예정)

1. [ ] 본인 머신 Hamachi 클라이언트 설치 + 네트워크 생성 (ID/비번 박음)
2. [ ] 정유현 머신 Hamachi 클라이언트 설치 + 본인 네트워크 가입
3. [ ] 본인 머신 서버 부팅 + 가상 IP listen 검증
4. [ ] 정유현 머신 Unity 클라 부팅 + 본인 가상 IP connect
5. [ ] 2인 같은 맵 broadcast 실측 (M3 Phase 08c 정합)
6. [ ] 시연 흐름 풀세트 dry-run (메인 → 엔딩) — 끊김 없음 확인

### 검증 결과

_(정유현 동반 검증 후 박음)_

---

## 3. 산출물

- 본 노트 (`_demo-environment-verification-2026-05-23.md`)
- 빌드 산출물 `.exe` (캡스톤 1 시연 영상 녹화에도 사용 가능, `.gitignore`)
- 검증 결과 → Phase 05 -DONE.md "AC 검증 결과" 섹션 흡수

---

## 4. 작업 로그

- 2026-05-23 (본 세션 시작 시점): 빌드 + 로컬 백업 검증 시작
- _(검증 끝나면 결과 박음)_
- _(Hamachi 시점에 별 세션 박음)_
