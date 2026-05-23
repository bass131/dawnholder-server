# Cross-Review γ 비교 — 2026-05-23 — M4.1 Phase 01 (Pre-M4 Hardcoding Audit)

> **본 파일**: α (Claude 자체 점검) + β (Codex 본인 직접 호출) γ 비교 산출물. `/cross-review` 슬래시 Step 5 정합.
>
> **입력**:
> - α: [`2026-05-23-pre-m4-cross-review-claude.md`](2026-05-23-pre-m4-cross-review-claude.md) (22건 발견)
> - β: [`2026-05-23-pre-m4-cross-review-codex.md`](2026-05-23-pre-m4-cross-review-codex.md) (α 보정 4건 + β만 발견 4건)

---

## 1. 변경 범위

- **마일스톤**: M4.1 Combat Precision (캡스톤 1 발표 6/10 전반)
- **점검 영역**: M3 응급 코드 전체 (server Combat/Maps/Handlers/Network + client Combat/Network/UI/State + shared GameData/Protocol + tools)
- **등급**: 보통 (Phase 01 정의 정합, plan 변경 트리거 X 판정 = 자동 상향 발동 X)

---

## 2. α/β 정합 비교 표

| 차원 | α (Claude reviewer) | β (Codex 본인 직접 호출) | γ 결정 |
|---|---|---|---|
| 헌법 §1~§5 점검 | ✅ M4 사전 과제 8건 제외, 그 외 발견 중심 | ✅ 같은 정신 | 일치 |
| 코드 직접 접근 | R only | R only | 둘 다 |
| 발견 양 | 22건 | 18 정합 + 4 보정 + 4 추가 = 22 보존 + 4 새 | β 우위 (보완 정밀) |
| 토큰 비용 | 메인 세션 ↑ (Explore SubAgent ~30k) | 외부 도구 (본인 계정) | 분담 정합 |
| 시각 | 영역 분류 정합 | M3.8 봉합 "완료" 표기 정밀화 + 클라 측 drift 잠복 | 상호 보완 |

---

## 3. 발견 분류 — γ 통합

### 3.1. 양쪽 다 잡음 (= 18건 + 큰 분류 동의)

- α 22건 발견 중 18건 = β 정합 동의 (S1~S7 일부 / C3~C11 / T1/T2)
- 즉시 봉합 영역 + M4.2/M4.3 이관 영역 + 설계 의도 박힘 분류 일치

### 3.2. α만 잡음 (= 0건)

- α가 β보다 더 잡은 *결함* = 0건
- α는 22건 발견 박았지만 β가 큰 분류 모두 동의 + 보정만

### 3.3. β만 잡음 (= 4건, 최우선 봉합 후보)

| ID | 위치 | 박힌 값 | γ 분류 | 즉시 박을지 |
|---|---|---|---|---|
| **B1** | `03_Client/Assets/Scripts/Input/LocalPlayerController.cs:72` | `const float AttackRangeSq = 9.0f` | **🔴 즉시 봉합 (Phase 03 흡수 의무)** | **YES** — 서버 `CombatConstants.AttackRangeSquared`와 중복. Phase 03 AABB 전환 시 클라가 옛 원형 range 붙들면 데모 체감 어긋남 |
| B3 | `98_Shared/CLAUDE.md:19` + `00_Document/ARCHITECTURE.md:213` | ProtocolVersion `Current=3` 문서 잔재 (실제 코드 `Current=4`) | **🟡 즉시 봉합 (문서 sweep, Phase 03 bump 시 동시)** | **YES** — false-promise 계열 (헌법 #4 정신 정합), Phase 03 ProtocolVersion 4→5 bump 시 같이 정정 의무 |
| B2 | `02_Server/GameServer/Program.cs:13` | listen port `7777` 고정 | **🟢 M5+ 또는 별 시점** | NO — M3.8 Hamachi 검증 통과 (7777 고정), 클라우드/EC2 진입 시 appsettings/env 이동 |
| B4 | `03_Client/Assets/Scripts/UI/HudController.cs:28~30` | mock HP/Gold `100/100/0` | **🟢 M4.3 또는 별 시점** | NO — M1/M3 UI skeleton 잔재, 현재 combat enemy HP만 표시라 plan 변경감 X |

### 3.4. α 분류 보정 (= 4건, β 정밀화)

| α 발견 | α 분류 | β 보정 | γ 결정 |
|---|---|---|---|
| C1 (`NetworkBootstrap.cs`) | "M3.8 5-B 봉합 완료" | "host만 PlayerPrefs 봉합 완료, port/timeout은 Inspector/default" | **C1 = host 봉합 완료 / port·timeout 잔여 (M4.2 이관 후보)** |
| C2 (`MainMenuController.cs`) | "M3.8 5-B 봉합 완료" | "host만 성공 시 저장, port/timeout은 default" | **C2 = host 봉합 완료 / port·timeout 잔여 (M4.2 이관 후보)** |
| Sh1 (`Constants.cs`) | "설계 의도 박힘" | "동의 + `Physics.cs` Gravity/JumpSpeed/GroundY도 같은 계열" | **Sh1 + 신규 Sh2 (Physics.cs) = 설계 의도 박힘 (tuning/table 후보, runtime config X)** |
| C12 (`LocalPlayer.prefab` collider/Rigidbody2D) | "수정 불필요 (M3.8 시연 검증 통과)" | "서버 hitbox와 별개 명시 안전" | **C12 = 서버 권위 hitbox 신뢰 X (Phase 03 서버 측 AABB 박음, Unity collider 신뢰 X)** |

---

## 4. plan 변경 트리거 판정 (γ)

**α "plan 변경 X" 권유 → β 동의 → γ 결정**:

✅ **M4.1 plan 자체 재구성 X** (별 Phase 신설 X, 별 마일스톤 신설 X)
✅ **Phase 02/03 흡수 옵션 A 박음** = B1 + B3 흡수 (이미 예정된 Phase 03 client send 정합 + PDL bump 정합 안에 들어감)

---

## 5. plan 갱신 결정 (옵션 A 즉시 봉합)

### 5.1. Phase 03 plan 갱신 의무 2건

1. **B1 흡수** — Phase 03 작업 내용에 "클라 측 `LocalPlayerController.AttackRangeSq = 9.0f` 제거 또는 shared/client combat hint 상수로 연결" 추가
2. **B3 흡수** — Phase 03 작업 내용 ProtocolVersion bump 단계에 "`98_Shared/CLAUDE.md` + `00_Document/ARCHITECTURE.md` v3 → v5 잔재 sweep" 추가

### 5.2. α 분류 보정 흡수 (claude-pre-review.md 정정)

- C1/C2 "M3.8 봉합 완료" 표기 → "host 봉합 완료 / port·timeout 잔여 (M4.2)" 정정
- Sh1에 Physics.cs 추가 박음
- C12 "수정 불필요" → "서버 hitbox와 별개" 표현 정정

---

## 6. 옛 학습 정합

- **false-promise cadence 정합 (ADR-024)**: B3 문서 잔재 = false-promise 변종 발본 (M3.6 Phase 04 학습 "98_Shared/CLAUDE.md Current=N stale" 봉합 패턴 정합). 본 Phase에서 발견 + Phase 03 sweep 의무 박음 = 같은 변종 *재발* 방지.
- **클라/서버 정합 누락 패턴**: B1 `LocalPlayerController.AttackRangeSq = 9.0f`는 서버 `CombatConstants.AttackRangeSquared = 9.0f`와 *중복 박힘* — 헌법 #4 "복사-붙여넣기 금지" 정신 위반 후보. 단 진짜 위험 X (서버가 최종 판정, 클라 9.0f는 target 추천 display hint 박힘). Phase 03 AABB 전환 시 정정 의무.
- **헌법 #1 정합**: B1이 헌법 #1 위반 X 확정 — α M3.6 Phase 05 학습 "AttackRangeSq 9.0f 헌법 #1 위반 X 확정 (서버 최종 판정)" 정합. 단 Phase 03 hitbox 정밀화 시 *클라 측 추천이 옛 원형 범위 붙들면 UX 불일치* 후보 → 즉시 봉합 박정.

---

## 7. 결정 권유 (γ 종합)

🟢 **양쪽 다 잡음 0건 + α/β 단독 결함 = 2건 (B1/B3, 둘 다 Phase 03 흡수)**

→ **GO (Phase 02 진입)** + Phase 03 plan 갱신 옵션 A 박음 (즉시 봉합)

### 다음 액션 박정 순서

1. ✅ Phase 03 plan 갱신 (B1 + B3 흡수)
2. ✅ claude-pre-review.md α 분류 보정 흡수 (C1/C2/Sh1/C12 표기 정정)
3. ✅ commit 박음 (보통 등급, 3 파일 변경)
4. ➡️ Phase 02 진입 대기 (Formulas.cs 분리 + PlayerStats 흡수)

---

## 8. 본 cross-review 학습 가치 (메타)

- **β 분담 정신 첫 실측 성공** (2026-05-23 봉합 정합) — 본인 직접 Codex 호출 분담이 *실측 가치* 박힘. β만 잡은 4건 중 B1 (실질 중요 결함) 발견 = 분담 정신 본질 가치 실증.
- **외부 시각 cross-check 가치 *재실증*** — α 자체 점검은 22건 발견 박았지만 *자기 영역 익숙함* 함정 (특히 C1/C2 "M3.8 봉합 완료" 통째 표기). β 외부 시각이 host/port 쪼개 박음 = *정밀화* 가치.
- **plan 변경 트리거 정량 임계 정합** — 22건 발견에도 *plan 변경 X* 판정이 α/β 일치 = 정량 임계 (8건+ = 재구성)의 *기계적 적용* 회피, *내용 본질* 점검이 정합. 학부생 학습 정합.

---

## 갱신 이력

- 2026-05-23: γ 비교 산출물 박음. α 22건 + β 4 보정 + β 추가 4건 = 종합 27 항목 (정합 18 + 양쪽 분류 일치 + 즉시 봉합 2 = B1/B3). plan 변경 트리거 X 결정 + Phase 03 옵션 A 흡수 결정.
