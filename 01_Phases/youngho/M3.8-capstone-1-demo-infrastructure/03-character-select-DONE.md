---
summary: M3.8 Phase 03 마감 — PDL C_CharacterSelect (PacketID 16) + CharacterClass enum + ProtocolVersion 3→4 bump + 서버 PlayerStats 분기 (전사/원거리 factory) + CharacterSelect.unity Scene + 단위 테스트 5건. reviewer Tier 2-A PASS (위반 0). 3 도메인 cross (shared/server/client) + true-promise 사례 박힘.
phase: 03
status: done
grade: 복잡
owner: youngho
---

# Phase 03 — 캐릭터 선택 (PDL + 서버 stats + 클라 UI) 마감

## TL;DR

**캡스톤 1 데모 5-layer Server Authority 캡슐화 완성** — 클라(byte cast) → Packet(byte 1) → Handler(3단계 검증) → Session(매핑) → Stats(factory). 각 layer 자기 책임만 = "handler = 검증, session = state" 약속이 코드로 실재 박힌 *true-promise 사례*. reviewer Tier 2-A 5축 PASS (위반 0, 개선 제안 1건 M4+ 검토).

**박힘 통계**:
- 4-A Shared = PDL 변경 + CharacterClass.cs 신설 + ProtocolVersion bump (3→4) + GenPackets.cs 자동 갱신
- 4-B Server = PlayerStats.cs 신설(42줄) + CharacterSelectHandler.cs(52줄) + HandlerRegistry(+1) + GameSession(+25) + CharacterSelectHandlerTests.cs(163줄, 5 테스트)
- 4-C Client = CharacterSelectController.cs 신설 + CharacterSelect.unity Scene 신설 + Build Settings 갱신
- 4-D reviewer Tier 2-A PASS

**도메인 위임**: 메인 (4-A + 4-C) + server SubAgent (4-B) + unity-bridge MCP (4-C 클라 Scene asset) + reviewer SubAgent (4-D 자동 호출)

## AC 검증 결과

### 1. PDL 변경 + PacketGenerator 재생성 ✅

```bash
# 검증
grep -n "C_CharacterSelect\|S_StageClear = 15" 98_Shared/Protocol/Generated/GenPackets.cs
```

결과:
```
34:	S_StageClear = 15,
35:	C_CharacterSelect = 16,
1085:public class C_CharacterSelect : IPacket
```

PacketID 16 = `S_StageClear = 15` 다음 (옛 ID 재사용 X, append-only 정합). PDL.xml에 `<packet name="C_CharacterSelect"><byte name="characterClass"/></packet>` 박힘 + 주석에 헌법 #1/#3 인용.

### 2. ProtocolVersion 3→4 bump ✅

```bash
# 검증
grep "Current = " 98_Shared/Protocol/ProtocolVersion.cs
```

결과 = `public const ushort Current = 4;` + v4 이력 문단 박힘.

### 3. dotnet build green ✅

```
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:02.05
```

Shared.dll 자동 복사 검증 = `03_Client/Assets/Plugins/Shared/Shared.dll` mtime `May 23 06:55` (방금 빌드).

### 4. dotnet test green + 회귀 0 ✅

```
175 passed / 176 total (1건 기존 skip / 회귀 0)
새 테스트 5건 모두 통과:
  - happy_warrior (characterClass=0)
  - happy_ranger (characterClass=1)
  - invalid_2 (characterClass=2 silent drop)
  - invalid_255 (characterClass=255 silent drop)
  - duplicate (이미 선택 후 silent drop)
```

### 5. Unity 컴파일 + Scene 박힘 ✅

```
Unity MCP RunCommand 검증:
- Compiling: False
- Console errors: 0
- CharacterSelectController type FOUND
- OnWarriorClicked / OnRangerClicked methods OK
- CharacterSelect.unity 박힘 + Buttons found: 2 + onClick 연결 (Linked WarriorButton/RangerButton → OnWarriorClicked/OnRangerClicked)
```

Build Settings = 5 Scene 정합:
```
[0] MainMenu / [1] CharacterSelect / [2] Gameplay / [3] UI / [4] Ending
```

### 6. reviewer Tier 2-A PASS ✅

5축 점검 (헌법 5 / ADR / 구조 / 테스트 / 도메인 패턴) 위반 0건. 개선 제안 1건 = `CharacterSelectController.cs:33` `UnityClientSession.Instance` null fallback 프로덕션 silent 허용 → M4+ 검토 (본 Phase 차단 X).

## 결정 흐름

### 1. 도메인 위임 패턴 결정 (옵션 B 채택)

세 갈래 비교 박음 — (A) Coordinator + Worker 3 풀세트 / (B) 메인 직접 + Worker 부분 위임 / (C) 메인 직접 풀세트.

| 옵션 | 비용 | 안전성 | 채택 |
|---|---|---|---|
| (A) Coordinator 풀세트 | 분해 호출 1 추가 | 메인 컨텍스트 보존 ↑ | ❌ 본 Phase 작업량에 과잉 |
| **(B) 메인 + Worker 부분 위임** | 균형 | Phase 02 패턴 정합 | ✅ 채택 (server 4-B만 SubAgent) |
| (C) 메인 풀세트 | 빠름 | 컨텍스트 부담 ↑ | ❌ 보류 (서버 큰 차원 위임 가치 ↑) |

### 2. CharacterClass 위치 결정 (Protocol 폴더)

`98_Shared/Protocol/` vs `98_Shared/GameData/` 갈래 — 본인 채택 = `Protocol/` (패킷 필드와 정합). `GameData/`는 Formulas/Tables 영역 (M4.1 Phase 02 후 박힘). 패킷 enum은 *PDL과 같은 디렉토리*가 정합 일관성.

### 3. silent drop vs disconnect 결정 (Trust Boundary)

헌법 #3 정합 = invalid characterClass(2/255) 시 *silent drop + cheat-flag 후보 로그*. 이유:
- disconnect = cheat가 *정상 사용자처럼* 보이게 함 (재현 시도 인지 X)
- silent drop = cheat 시도 *서버 인지* (M4.2 cheat-flag 도입 시 기록)

서버 측 Handler step 2에 `[Trust]` 로그 박음 = 본인 추적 가능 + 정상 클라엔 영향 X.

### 4. 클라 Send 패턴 결정 (Send vs SendIntent)

옛 `UnityClientSession` 박힌 두 메서드 — `Send(buf)` 직통 / `SendIntent(buf)` (HandshakeOk 게이트 + Editor latency 시뮬). 본 Phase 채택 = **`Send(buf)` 직통** (SendIntent는 intent 패턴 한정, CharacterSelect는 의도 아님). 옛 패턴 정합.

### 5. plan 박을 때 실측 점검 의무 정합 (Phase 02 학습)

Phase 02 학습 ★★★ = "plan 박을 때 실측 점검 의무" (false-promise 9번째 변종). 본 Phase 진입 시점 실측 점검 박았더니 결함 = **PDL 위치 작은 결함 1건만** (Phase 정의 본문 `98_Shared/Protocol/PDL.xml` 박혔는데 실제 `99_Tools/PacketGenerator/PDL.xml`). 사용자 인정 박음 → 본 -DONE.md에 기록, Phase 정의 별 정정 X.

## 학습 일지 후보 키워드

### ★★★ 캡스톤 1 어필 결정타 (한국 게임 회사 백엔드)

- **`server-authority-5-layer-encapsulation`** — Controller(byte cast) / Packet(byte 1) / Handler(3단계 검증) / Session(매핑) / Stats(factory). 각 layer 자기 책임만. *true-promise 사례* (false-promise 정반대 = 약속이 코드로 실재 박힘). 면접 어필 결정타
- **`pdl-append-only-protocol-version-bump-second-실측`** — M3 Phase 02 첫 실측 + 본 Phase 두 번째. 패턴 정착 검증. PacketID 재사용 X + bump 누락 = 옛 빌드 silent 실패 함정 학부생 1순위 차단
- **`3-domain-cross-worker-위임-패턴`** — shared(메인) → server(SubAgent) → client(메인+MCP) + reviewer 자동 호출. 큰 작업 분해 실측 + Worker 위임 trade-off (Coordinator 풀세트 vs 메인 + 부분 위임)
- **`true-promise-vs-false-promise`** — false-promise 12건+ 누적 패턴 *정반대*. "약속이 코드로 실재 박힘" 사례 첫 인지. 02_Server/CLAUDE.md "handler = 검증, session = state" 약속이 코드로 실재 = reviewer "true-promise 사례" 명명

### ★★ 본 Phase 외 발견 학습

- **`mcp-runcommand-lambda-closure-결함`** — Helper 람다 안에 local 변수 박았는데 외부에서 `GetRootGameObjects()`로 다시 찾으니 Canvas 자식 Button 못 잡음. *Unity Scene 박을 때는 직접 박음 권장* (DRY ↓ vs closure 결함 ↓ trade-off)
- **`character-class-enum-protocol-folder-결정`** — Protocol vs GameData 위치 갈래. 패킷 필드와 정합한 디렉토리 = Protocol/ (M4.1 후 GameData 신설 시 별 enum 분리 검토)

### ★ 작은 결함 봉합

- **`pdl-location-plan-결함-인정`** — Phase 정의 본문 PDL 위치 결함 (작은 부정합) — 사용자 인정 박음, 별 정정 없이 본 -DONE.md에 기록

## false-promise 점검 결과 (ADR-024 cadence)

본 Phase 차원 점검 (Phase 03 진입 ~ 마감 사이 발견):

| # | 결함 | 봉합 |
|---|---|---|
| 1 | PDL 위치 본문 작은 결함 | 사용자 인정 박음, Phase 정의 정정 X (본 -DONE.md 기록) |

옛 결함 누적 (마일스톤 마감 5단계 통합 시점 박을 항목):
- M3.8 Phase 02 = 9번째 false-promise 변종 봉합 박힘 (실측 점검 안 박은 결함, 정유현 옛 자산 미인지)
- M3.8 Phase 03 = 위 1건 (작은 결함, 봉합 X 결정)

**핵심 = Phase 02 학습 정합 박혔어서 Phase 03 진입 시점 실측 점검 박았더니 새 false-promise *발견 0건*** (작은 본문 결함만). ADR-024 cadence + Phase 02 학습 정합의 *결합 효과* 첫 실측.

## 다음 Phase

- **Phase 04** = NPC 대화 (클라 단독 hardcoded, 보통, 1~2h). 본 Phase PlayerStats 박힌 후 마을 진입 자연 흐름
- **MainMenuController.cs OnStartClicked 활성화** = 본 Phase 박은 후 한 줄 정정 (Phase 02 placeholder TODO 주석 해소)
- **Hamachi 시간 조율 트리거** = 정유현에게 디스코드/카카오 박을 시점 (Phase 05 사전 조건)
