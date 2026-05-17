---
summary: Phase 07 — 점프 + 중력 + Y축 prediction. Physics.Step 공유 공식(SOLID struct 묶음) + PDL 비트필드(D2 현업 정석) + Protocol.Version v2 + Y축 reconcile. ad-hoc 부산물 — Shared DLL 빌드 자동화 정합(PR #19), Step 4 사후 정정(매 frame Predict A 패턴 복귀).
phase: 07-gravity-and-jump
work-id: phase07-gravity-and-jump
status: done
completed_at: 2026-05-17
commit: f5ef2dd
---

# Phase 07 — 점프 + 중력 완료 박제

**소요 시간**: 약 5시간 (결정 4건 + 5 Step + ad-hoc PR #19 + Step 4 사후 정정 포함)

## TL;DR

사이드스크롤 점프·중력 도입. **`98_Shared/GameData/Physics.cs`** 결정론 공식(`Gravity=-20`/`JumpSpeed=8`/`GroundY=0`)을 SOLID struct 묶음(`PhysicsInput`/`PhysicsState`)으로 박고, 클라/서버가 같은 함수 호출 → drift 0(헌법 #1). PDL `C_MoveIntent.input` **byte 비트필드** 도입(D2, 현업 정석 — Quake/Source/한국 MMORPG), **Protocol.Version=2 bump**(D3, 헌법 #2), **클라 에지 + 서버 OnGround 안전망**(D4) 패턴 채택. Y축 prediction + reconcile 확장(`OnSnapshot(serverX/Y, serverVx/Vy, ackedTick)`). 부산물 — 정유현 main pull 사고로 표면화된 **Shared DLL `.gitignore` 결함을 PR #19로 영구 정정** + **Step 4 사후 정정**(fixed cadence 과해석 → 매 frame Predict A 패턴 복귀, MMORPG 장르 정합).

## 5단계 보고

- **무엇을 만들었나** — `Physics.cs` 단일 공식 함수 + `PhysicsInput`/`PhysicsState` readonly struct(SOLID Open/Closed) + `InputBits` 비트필드 인코딩/디코딩 SRP 헬퍼 + `ProtocolVersion.Current=2`(헌법 자리잡이 활용) + PDL `byte input`/`vx/vy float` 비트표 주석 + 서버 `PlayerEntity.Velocity/OnGround` + `GameMap.Tick`의 Physics.Step 위임 + 클라 `LocalPlayerController.OnJump` 에지 + `PlayerPredictor` Physics.Step 통합 + X+Y 비교 reconcile + InputHistory `jumpPressed` 확장 + Shared DLL `.gitignore` 화이트리스트(PR #19).
- **왜 필요한가** — 사이드스크롤 게임의 시그니처 동작(점프) 도입. 헌법 #1(Server Authority) 유지하면서 클라 prediction 부드러움 + cheat 방어(공중 더블점프 차단). 비트필드는 미래 입력(공격/방어/스킬) 패킷 비대 차단. Protocol.Version은 라이브 게임 클라 버전 강제 표준. 정유현 사고로 표면화된 Shared DLL git 추적 결함은 인규 합류 전 영구 정정 필수.
- **어떻게 만들었나** — 5 Step 분해(Physics.cs / PDL+InputBits / 서버 wire / 클라 wire / 수동 검증). 결정 4건 사전 확정 — D1 `System.Numerics.Vector2`(서버 패턴 일관), D2 비트필드(Codex CLI 상의 2번째 e2e — `InputBits.cs` SRP), D3 Protocol.Version bump(헌법 자리잡이 활용), D4 클라 에지 + OnGround 안전망. ad-hoc PR #19로 Shared DLL 자동 복사 시스템 정합. Step 4 사후 정정 — fixed cadence Predict로 박았다가 240Hz 뚜두두둑 발견 → MMORPG 장르 정합(Phase 06 매 frame Predict + reconcile) 패턴 복귀.
- **테스트 결과** — `dotnet build Dawnholder.slnx` 0 error / `dotnet test` **99/99 통과**(PhysicsTests 11 + InputBitsTests 14 + MoveIntentTests 9 + 기타 65) / Unity Test Framework InputHistoryTests 14 정합 / Unity_RunCommand Phase 07 smoke 5/5(결정론 수학 + reconcile) / Unity Editor 실측 — 부드러운 좌우 + 점프 포물선 + 더블점프 차단 + cheat ground만 적용 + 200ms lag reconcile 정상(권위 보정 회귀 확인) / 서버 로그 `S_Snapshot OnSend 32 bytes`(= 24 + vx/vy 8 정확).
- **다음 스텝** — Phase 08 회귀 안전망 + 데모 영상 + p99 측정 (M2 완료 증명). Anti-cheat Phase 후보(D4 후속) — M3+ last-jump-tick cooldown / 비행 시간 검증 / velocity 일관성 모니터링. 학습 일지 ★★★ 1순위 — `/journal:concept Input replay reconcile`(Phase 06 결정타) 또는 Phase 07 통째.

## 결정 흐름

| # | 갈림길 | 채택 | 이유 |
|---|--------|------|------|
| **D1** | Physics.Step Vector2 타입 | `System.Numerics.Vector2` | 서버 패턴 일관(`PlayerEntity.cs`/`GameMap.cs` 이미 사용). 현업 90%+ Vector2 struct 표준 |
| **D2** | jumpPressed 인코딩 | **비트필드 `byte input`** | 현업 정석(Quake `usercmd_t.buttons` / Source `CUserCmd::buttons` / 한국 MMORPG). 미래 입력 확장 시 패킷 비대 차단. 면접 가치 강함 |
| **D3** | Protocol.Version | bump v1→v2 | 헌법 #2 정합. `Constants.cs` 박으려다 `98_Shared/CLAUDE.md` Layout 표 자리잡이 `Protocol/ProtocolVersion.cs (예정)` 발견 → **헌법 우선으로 Protocol/ProtocolVersion.cs 채택**(자동 컨텍스트 로드 안전망 작동) |
| **D4** | jumpPressed 에지 위치 | 클라 + 서버 OnGround 안전망 | 정의 파일 #85 명시 + inputX 의도 패턴 일관. 더 엄격한 cheat 방어는 anti-cheat Phase 후보로 박힘 |

## Codex CLI 상의 (2번째 e2e — 5규칙 정합 검증)

D2 비트필드 헬퍼 위치 결정을 Codex CLI에 상의(Phase 06 InputHistory 패턴 재사용). Windows sandbox 1312 재발 없이 정상 답변 받음.

**Codex 답변 요지** (300자 응축):
- (α) **InputBits.cs 신설** 채택 — Constants는 상수 출처 유지, Physics는 계산만, InputBits는 wire format. `InputState` struct는 *지금* 안 박고 입력 늘면 자연 승격.
- 비트 0~1 = **옵션 A literal 매핑** 권장 — 2비트 enum처럼 다루면 디버깅 ↑, `11`을 invalid/reserved로 남길 수 있음. ±2 dash까지 가면 별도 dash bit.
- 함정 3건: (1) invalid 값 방어, (2) 서버/클라 중복 디코드 금지, (3) PDL 주석 비트표 고정 문서화.

모두 코드에 반영. `InputBits.Decode`가 `valid=false` 플래그 반환(서버 cheat 로깅), 양쪽 같은 헬퍼 호출, PDL XML 주석에 비트표 박음.

## AC 검증 결과

**.NET 솔루션**:
```bash
$ dotnet build Dawnholder.slnx
  성공 빌드. 경고 0개, 오류 0개.
$ dotnet test
  통과! - 실패: 0, 통과: 99, 건너뜀: 0, 전체: 99, 기간: 3s
```

**Unity_RunCommand Phase 07 Smoke 5/5**:
```
T1 idle:           pos=(0,0)         vel=(0,0)    onGround=True
T2 right+jump:     pos=(0.250,0.400) vel=(5,8)    onGround=False   ← vy=JumpSpeed
T3 air+right:      pos=(0.500,0.750) vel=(5,7)    onGround=False   ← 중력 -20*0.05
T4 air+jump(ign):  pos=(0.500,1.050) vel=(0,6)    onGround=False   ← D4 함정 회피 ✓
OnSnapshot:        reconciled=True   pos=(10,0)   vel=(0,0)        ← reconcile 정합
```

**Unity Editor 실측 (`SimulatedLatencyMs=200`, ~35초)**:
- 부드러운 좌우 이동 (사후 정정 효과 — Phase 06 패턴 복귀)
- 점프 포물선 자연 (vy=8 → 0 → -∞ 중력 누적)
- 더블점프 차단 (공중 Space 무시 — Physics.Step OnGround 안전망)
- cheat 매 frame Space → ground 닿을 때만 1회 적용
- 200ms lag에서 reconcile 부드러움 (이전 위치 회귀 = 권위 보정 정상)
- 서버 로그 `S_Snapshot OnSend 32 bytes` 정확 (24 + vx/vy 8)
- Tick 메트릭 avg 0.03~0.05ms (Physics.Step 통합 후도 부담 0)

## 막힘 / 정정 4건

1. **PDL byte 케이스 — PacketGenerator 지원 확인** (Step 2 진입 시점)
   - Phase 06에서 uint 신설 경험 떠올림 → byte는 이미 지원 확인 → 추가 변경 X.

2. **헌법 충돌 발견 — Protocol.Version 위치** (Step 2 작업 중)
   - D3 결정 핀에 `Constants.cs에 신설` 박았으나 `98_Shared/CLAUDE.md` Layout 표에 `Protocol/ProtocolVersion.cs (예정)` 자리잡이 발견.
   - `Constants.cs` Edit 시 자동 컨텍스트 로드된 헌법이 충돌 자동 탐지.
   - **헌법 우선 룰**(`CLAUDE.md > ADR > ARCHITECTURE > PRD`) 적용 → 자리잡이 위치 채택. 학습 가치 강한 사건.

3. **정유현 main pull 사고 — Shared DLL `.gitignore` 결함** (Step 2 완료 후 ad-hoc)
   - 유현이 main pull 후 Unity Safe Mode 진입. 진단: PDL이 int인가? — *오진*. 진짜 원인 = `03_Client/Assets/Plugins/**/*.dll`이 ignored라 다른 머신 pull 후 outdated/누락.
   - **PR #19로 영구 fix** — Shared.dll/.meta 화이트리스트 추가 + asmdef Unity.TextMeshPro 추가(유현 임시 패치 영구화) + 최신 Shared.dll commit. admin merge.

4. **Step 4 사후 정정 — fixed cadence 과해석** (Step 5 시연 후)
   - 정의 파일 #82 "dt=TickDuration 고정(fps 의존 차단)"을 *Predict까지 fixed cadence*로 과해석 → 240Hz 뚜두두둑.
   - 본 의미는 *송신 cadence* fps 의존 차단. Predict 자체는 가변 dt OK(Phase 06 패턴).
   - 장르(MMORPG/캐주얼) 정합 — Source/Quake/Overwatch 패턴. fixed-step + visual lerp는 격투/콘솔 RTS 패턴이라 over-engineering.
   - **A 패턴 복귀 commit `f5ef2dd`** — 매 frame Predict + Time.deltaTime + reconcile 흡수.

## 학습 일지 후보 (CONTEXT_LearningJournalCandidates.md에 박힘)

- ★★ `/journal:concept 입력 인코딩 비트필드 (D2 결정)` — 현업 정석 채택 + Codex 함정 3건
- ★★ `/journal:concept 헌법 vs 결정 우선순위 — 자동 컨텍스트 안전망` — Protocol.Version 위치 충돌 자동 탐지
- ★★★ `/journal:concept 자리잡이 패턴 효과 실증 (2건 묶음)` — Phase 04 자리잡이 + Phase 07 헌법 자리잡이
- **신규** ★★ `/journal:concept 정의 파일 과해석 — Step 4 사후 정정` — fixed cadence 잘못 → A 패턴 복귀, 장르 정합 판단
- **신규** ★★ `/journal:bug Shared DLL .gitignore 결함 — 정유현 사고 진단 + 영구 fix` — 인프라 결함 표면화 + 헌법 #4 정합

## 자산 위치 (빠른 참조)

```
98_Shared/GameData/Physics.cs           ← 결정론 공식 단일 출처 (Step 1)
98_Shared/GameData/InputBits.cs         ← 비트필드 인코드/디코드 SRP (Step 2)
98_Shared/Protocol/ProtocolVersion.cs   ← 헌법 자리잡이 활용 (Step 2, D3)
99_Tools/PacketGenerator/PDL.xml        ← byte input 비트표 + vx/vy 추가
02_Server/GameServer/Maps/PlayerEntity.cs   ← Velocity/OnGround 신설 (Step 3)
02_Server/GameServer/Maps/GameMap.cs        ← Physics.Step 위임 (Step 3)
02_Server/GameServer/Network/GameSession.cs ← InputBits.Decode 통합 + 잔재 캐스트 정정
03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs    ← Physics.Step 통합 + X+Y reconcile (Step 4 + 사후 정정)
03_Client/Assets/Scripts/Prediction/InputHistory.cs       ← jumpPressed 확장
03_Client/Assets/Scripts/Input/LocalPlayerController.cs   ← OnJump 에지 + 매 frame Predict (Step 4 + 사후 정정)
03_Client/Assets/Scripts/Network/UnityClientSession.cs    ← HandleSnapshot vx/vy 추가
02_Server/GameServer.Tests/PhysicsTests.cs       ← 11 xUnit (Step 1)
02_Server/GameServer.Tests/InputBitsTests.cs     ← 14 xUnit (Step 2)
03_Client/Assets/Tests/EditMode/Prediction/InputHistoryTests.cs  ← 14 NUnit (12 기존 + 2 jump 신규)
_phase07-decisions.html                  ← 결정 4건 시각화
_phase07-d4-edge-detection.html          ← D4 에지 처리 단독 깊이 풀이
_phase07-step4-smoothness-AvsC.html      ← Step 4 사후 정정 자료 (A vs C 비교)
```

## ad-hoc 부산물 (별도 PR)

- **PR #19 — Shared DLL commit 강제 + asmdef TMP 참조** — `c774fb5`. 정유현 사고로 표면화된 헌법 #4 잠재 결함 영구 fix. 합류자 pull 즉시 Unity 동작 보장 + CHANGELOG [M] 박힘.

## 다음 마일스톤 인계

- **Phase 08** — 회귀 안전망 + 데모 영상 + p99 측정 → **M2 First Connection 완료 증명**
- M2 완료 = 옵션 B(1인 movement + 점프) **데모 가능** (6월 캡스톤 1 발표 fallback)
- M3 진입 시 — 다인 동시 + broadcast + 다른 플레이어 보간 (Phase 07 Predictor를 다른 플레이어용 PredictionPool로 확장)
