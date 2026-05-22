---
owner: youngho
milestone: M3.8
phase: 03
title: 캐릭터 선택 (PDL + 서버 stats + 클라 UI)
status: pending
grade: 복잡
risk: trust-boundary
estimated: 3~4h
domain: server+shared+client
summary: PDL C_CharacterSelect 패킷 신설 + CharacterClass enum + ProtocolVersion 3→4 bump + 서버 PlayerStats 분기 (전사/원거리) + 클라 CharacterSelect.unity Scene
---

# Phase 03: 캐릭터 선택 (PDL + 서버 stats + 클라 UI)

> **상태**: pending
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 복잡 (위험 깃발 `trust-boundary` + `irreversible` ProtocolVersion bump + `unity-asset` Scene 신설 = 3 도메인 cross)
> **담당**: coordinator + Worker 3 (server / shared / client) + reviewer 자동 호출
>
> **등급 격차 결정** (plan-auditor 결함 봉합): 본 Phase는 *3 도메인 cross + 깃발 3개 동시 발동*이라 grade-and-risk.md §3 규칙 정합하면 *대규모 자동 상향* 후보. 그러나 양식 부담 = *복잡 등급 + Reviewer 자동 호출 + 본 plan-auditor 사전 audit*로 충분 정합. **5단계 보고 MD/HTML은 본 Phase 단독 박지 X, M3.8 마일스톤 마감 시점에 통합 박음** (Phase 05 마감 의례 절 정합). 등급 격차 사유 = "Phase 단위 양식 부담 격차 vs 마일스톤 마감 통합 박음" — 후자 채택.

---

## 🎯 목표

캐릭터 선택 (전사/원거리) 기능 박음 = *PDL 패킷 신설* + *서버 측 PlayerStats 분기* + *클라 측 Scene + UI*. 학부생이 *프로토콜 추가 + 양쪽 빌드 정합 + 헌법 #3 신뢰 경계 검증*을 한 Phase에서 실측하는 핵심 학습 단위.

본 Phase 끝나면 = `MainMenu` 시작 클릭 → `CharacterSelect` Scene 진입 → 전사/원거리 버튼 클릭 → `C_CharacterSelect { byte characterClass }` 패킷 전송 → 서버가 캐릭터 클래스 검증 + `PlayerStats` 박음 + `EnterGameWorld` 진입 → 클라가 마을 Scene 로드 + 캐릭터 클래스에 맞는 시각화 (placeholder 색상 분기 OK).

---

## ⏪ 사전 조건

- [ ] Phase 02 (메인 + 엔딩 Scene) 박혀있음 — `MainMenu` 시작 버튼이 본 Phase Scene 로드
- [ ] Phase 01 (PRD 갱신) 박혀있음 — MVP 제외 항목 정정 박힌 후 본 Phase 진입 (PRD 정합 의무)
- [ ] M3 PDL `Generated/GenPackets.cs` 박힘 + PacketGenerator 작동 (Phase 06+07 결과 정합)
- [ ] 본인 머신 Unity batchmode compile 정상 (Shared.dll 변경 후 의무)

---

## 📝 작업 내용

### 4-A. PDL 변경 (shared SubAgent)

- [ ] `98_Shared/Protocol/PDL.xml` append-only로 `C_CharacterSelect` 패킷 신설:
  - PacketID = M3 Phase 07 박힌 마지막 ID (`S_StageClear` = 15) 다음 = **16** (append-only 정합)
  - 필드 = `byte characterClass` 1개
- [ ] `98_Shared/Protocol/CharacterClass.cs` 신설:
  - `public enum CharacterClass : byte { Warrior = 0, Ranger = 1 }`
- [ ] `98_Shared/Protocol/ProtocolVersion.cs` bump: `Current = 3` → **`Current = 4`**
  - 사유 = backward compatible 필드 추가지만 옛 빌드 클라 호환성 차단 (헌법 #2 정합 + M3 Phase 02 handshake 정합 패턴)
- [ ] PacketGenerator 재실행 (`dotnet run --project 99_Tools/PacketGenerator`) → `GenPackets.cs` 자동 갱신
- [ ] `dotnet build 98_Shared/` green
- [ ] **Shared.dll commit 의무** (CHANGELOG 2026-05-17 학습 정합) — `03_Client/Assets/Plugins/Shared/Shared.dll` 자동 복사 검증

### 4-B. 서버 측 (server SubAgent)

- [ ] `02_Server/GameServer/Combat/PlayerStats.cs` 신설:
  ```csharp
  public sealed class PlayerStats
  {
      public CharacterClass Class { get; init; }
      public int Hp { get; set; }
      public int MaxHp { get; init; }
      public int Attack { get; init; }
      public int Defense { get; init; }
      public float MoveSpeed { get; init; }

      public static PlayerStats Warrior() => new() { Class = CharacterClass.Warrior, Hp = 150, MaxHp = 150, Attack = 15, Defense = 5, MoveSpeed = 4f };
      public static PlayerStats Ranger() => new() { Class = CharacterClass.Ranger, Hp = 80, MaxHp = 80, Attack = 12, Defense = 2, MoveSpeed = 6f };
  }
  ```
- [ ] `02_Server/GameServer/Handlers/CharacterSelectHandler.cs` 신설:
  - `IPacketHandler` 구현 (M3 Phase 03 패턴 정합)
  - 입력 검증 = `characterClass == 0 || characterClass == 1` 외 *silent drop* (cheat-flag 후보, M4.2 도입 시 기록)
  - 중복 선택 차단 = `GameSession.HasSelectedClass == true` 시 silent drop
  - 통과 시 = `GameSession.SetCharacterClass(characterClass)` → `EnterGameWorld` 진입
- [ ] `02_Server/GameServer/Network/GameSession.cs` 갱신:
  - `PlayerStats? _stats` 필드 추가
  - `HasSelectedClass` getter 추가
  - `SetCharacterClass(byte characterClass)` 메서드 = `_stats = characterClass == 0 ? PlayerStats.Warrior() : PlayerStats.Ranger()` (헌법 #1 = 서버가 stats 박음, 클라는 *선택만*)
- [ ] `02_Server/GameServer/Network/HandlerRegistry.cs` 갱신: `CharacterSelectHandler` 등록 (PacketID 16)
- [ ] 단위 테스트 5건+ (`02_Server/GameServer.Tests/Handlers/CharacterSelectHandlerTests.cs`):
  - happy 전사 (characterClass=0) → PlayerStats.Warrior 박힘
  - happy 원거리 (characterClass=1) → PlayerStats.Ranger 박힘
  - invalid (characterClass=2) → silent drop, _stats == null
  - invalid (characterClass=255) → silent drop
  - 중복 선택 (이미 선택 후 다시 보냄) → silent drop, 옛 stats 유지

### 4-C. 클라 측 (client SubAgent + unity-bridge SubAgent)

- [ ] `03_Client/Assets/Scenes/CharacterSelect.unity` Scene 신설:
  - Canvas + EventSystem
  - 전사 버튼 (왼쪽) + 원거리 버튼 (오른쪽) + 캐릭터 placeholder 이미지 (각 버튼 옆)
  - 설명 텍스트 ("전사: 근접, 높은 HP" / "원거리: 활, 빠른 이동")
- [ ] `03_Client/Assets/Scripts/Scene/CharacterSelectController.cs` 신설:
  - 전사 버튼 OnClick → `UnityClientSession.Send(new C_CharacterSelect { characterClass = 0 })` + `SceneManager.LoadScene("Gameplay")` (또는 M3 Phase 08 박힌 Gameplay Scene)
  - 원거리 버튼 OnClick → 동일 패턴 + `characterClass = 1`
  - 헌법 #1 정합 = 클라는 *선택 의도만 보냄*, 서버가 stats 박음
- [ ] `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs` 갱신 (필요 시):
  - 캐릭터 선택 후 `EnterGameWorld` 진입 패턴 정합 — Phase 03 박힌 `S_HandshakeResult` dispatch 검토 (옛 박힘 활용 vs 새 패킷 추가 결정)
- [ ] `03_Client/Assets/Scripts/Rendering/PlayerVisual.cs` 또는 `LocalPlayer.prefab` 갱신 (옵션):
  - 캐릭터 클래스에 맞는 *placeholder 색상* 분기 (전사 = 빨강, 원거리 = 파랑) — 시연용 단순 분기, 본 마감 후 정유현 영역에서 정식 sprite 박음
- [ ] Build Settings에 `CharacterSelect.unity` 박음
- [ ] Unity batchmode compile green (`unity-bridge` 호출)

### 4-D. 정합 검증 (reviewer 자동 호출)

- [ ] reviewer SubAgent 자동 호출 (Tier 2-A) — 헌법 5 + ADR 정합 + 도메인 패턴 점검
- [ ] 결과에 따라 봉합 또는 GO

---

## ✅ 완료 조건

- [ ] `PDL.xml` `C_CharacterSelect` 패킷 박힘 + `CharacterClass` enum 박힘 + `ProtocolVersion.Current == 4` 박힘
- [ ] `GenPackets.cs` 자동 생성 갱신 박힘 + `dotnet build 98_Shared/` green
- [ ] `Shared.dll` 자동 복사 (`03_Client/Assets/Plugins/Shared/Shared.dll`) 박힘
- [ ] `PlayerStats.cs` 박힘 + `Warrior()` / `Ranger()` static factory 박힘
- [ ] `CharacterSelectHandler.cs` 박힘 + `HandlerRegistry`에 등록 박힘
- [ ] `GameSession.cs` `_stats` 필드 + `HasSelectedClass` + `SetCharacterClass` 박힘
- [ ] `CharacterSelectHandlerTests.cs` 단위 테스트 5건+ 모두 통과
- [ ] `dotnet test` green (회귀 0, M3 baseline + 5건+ 추가)
- [ ] `CharacterSelect.unity` Scene 박힘 + `CharacterSelectController.cs` 박힘
- [ ] Unity batchmode compile green
- [ ] reviewer SubAgent 결과 = PASS (또는 봉합 후 PASS)
- [ ] -DONE.md 박힘 (복잡 등급 의무, AI=사실 박제 / 본인=회고 분리)
- [ ] commit 박힘 (단독 PR 분리 X — M3.8 전체 마감 시 한 PR)

---

## 🧪 테스트

**자동**:
- `CharacterSelectHandlerTests` 5건+ (happy 2 + invalid 2 + 중복 1)
- `dotnet test` 전체 green (회귀 0)
- Unity batchmode compile (ADR-020 정합)

**수동**:
- Unity Editor → MainMenu Play → 시작 → CharacterSelect → 전사 클릭 → 서버 로그에 "PlayerStats Warrior" 박힘 + Gameplay Scene 진입
- 동일 흐름 원거리 클릭 → "PlayerStats Ranger" 박힘
- Wireshark/패킷 디버거로 `C_CharacterSelect` 페이로드 = `[16, characterClass]` 박힘 확인
- 서버에 *수동으로 invalid characterClass=2 보냄* → silent drop 확인 (서버 disconnect X, cheat-flag 후보 박힘)

---

## 📚 학습 포인트

- **PDL append-only 첫 실측 (M3.8 차원)** — M3 Phase 06+07 박힌 패턴 정합. PacketID 16 = 옛 ID 재사용 X (헌법 #2 정합). ProtocolVersion bump 3→4 = backward compatible이지만 옛 빌드 차단.
- **헌법 #1 (Server Authority) 정밀 실측** — 클라가 *stats 값 직접 보냄* X, *클래스 선택 의도만 보냄*. 서버가 stats 박음. *왜?* — 클라가 stats=999 보내면 cheat. 클래스 enum(0/1) = 검증 쉬움 + 서버가 default stats 매핑.
- **헌법 #3 (Trust Boundary) 첫 silent drop 실측** — invalid characterClass(2/255) = *disconnect* 또는 *silent drop* 둘 다 OK. 권장 = silent drop + cheat-flag 후보 (M4.2 cheat-flag 도입 시 기록). 이유 = disconnect는 *cheat가 정상 사용자처럼 보이게 함*, silent drop은 *cheat 시도가 인지되지 않게 함*.
- **단위 테스트 happy + invalid + edge 패턴** — 5건+ 의무 = 학부생 함정 (happy만 박고 invalid 빠뜨림) 차단. Codex β 학습 정합.
- **3 도메인 cross 작업 흐름** — server + shared + client 동시 변경 = coordinator 위임 + Worker 3 분담. 메인 직접 = 문맥 손실 위험. 학부생이 *큰 작업 분해* 첫 실측.
- **Shared.dll commit 의무 두 번째 실측** — M3 Phase 04 첫 실측 (SAC On 사고 동반). 본 Phase에서 두 번째 실측 = 패턴 정착 검증.

---

## ⚠️ 함정 / 주의사항

- **PacketID 재사용 절대 금지** — `S_StageClear = 15` 다음 = **16**. 옛 ID(예: 13 = `S_HitResult`) 재사용 시 헌법 #2 위반 = 서버/클라 deserialize 사고. PacketGenerator가 자동 박지만 본인 PDL.xml 박을 때 의무 검증.
- **ProtocolVersion bump 누락** — bump 안 하면 옛 빌드 클라가 *deserialize 실패 silent* 또는 *random crash*. 학부생 함정 1순위.
- **Shared.dll commit 누락** — Shared.dll만 빌드하고 commit 안 하면 클라 측 빌드 깨짐 (CHANGELOG 2026-05-17 사고). `.githooks/pre-commit`이 일부 잡지만 *Shared.dll 변경 자체*는 못 잡음 (cloud 라인 영역만 잡음). 본인 의무 검증.
- **`CharacterClass` enum 위치** — `98_Shared/Protocol/` 또는 `98_Shared/GameData/`? 권장 = `Protocol/` (패킷 필드와 정합). `GameData/`는 Formulas/Constants/Tables 영역 (M4.1 Phase 02 후 박힘).
- **중복 선택 시나리오** — 학부생 함정 = `HasSelectedClass` 검증 빠뜨림 → 같은 세션에서 캐릭터 두 번 선택 시 stats 덮어쓰기 = 권위 위반. 단위 테스트 1건 의무.
- **trust-boundary silent drop vs disconnect** — 본 Phase는 *silent drop* 권장. 이유 = cheat-flag 후보 + 정상 사용자 영향 X (정상 클라는 0/1만 보냄). 단 *재현 빈도*가 의심 패턴이면 M4.2 cheat-flag table에서 기록 + 추적.
- **`unity-bridge` SubAgent 호출 시점** — Scene 신설 + prefab 갱신 = `unity-bridge`. Script만 = `client`. 본 Phase는 *둘 다*라 client + unity-bridge 분담 또는 client가 unity-bridge 호출 (메인 경유).

---

## ➡️ 다음 Phase

- Phase 04 — NPC 대화 (클라 단독 hardcoded). 캐릭터 선택 후 마을 진입 흐름 자연.

**플러스 트리거** (plan-auditor 개선 제안 봉합): 본 Phase 마감 시점에 **정유현과 Hamachi 셋업 시간 약속 박음 의무** (디스코드/카카오 별 채널). Phase 05 사전 조건 충족 위해 *Phase 03 마감 ~ Phase 04 진행* 사이에 시간 약속 박혀있어야 Phase 05 진입 시 블로킹 X.

---

## 📋 박제

본 Phase = 복잡 등급 → -DONE.md 박음 (ADR-013 페어 박제 정책 정합).

- AI=사실 박제 = 본 Phase Worker 산출물 + 단위 테스트 결과 + reviewer 결과
- 본인=회고 = 본 Phase 학습 (`learning-journal/youngho/` 또는 Notion 트랙 B)

work-pin "현재 작업" → "Phase 03 ✅ 마감, Phase 04 미진입" 갱신.

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
