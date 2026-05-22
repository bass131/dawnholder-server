---
owner: youngho
milestone: M3.8
title: Capstone-1 Demo Infrastructure (PRD 갱신 + 메인/엔딩 UI + 캐릭터 선택 + NPC 대화 + Hamachi)
status: in-progress
grade: 복잡
risk: low
estimated: 7~11h (총합, 5 Phase)
domain: client+server+shared+qa+meta
---

# M3.8 — Capstone-1 Demo Infrastructure

> **상태**: in-progress
> **시작**: 2026-05-22
> **마감 목표**: 2026-05-26 (캡스톤 1 마감 6/10 기준, M4.1 진입 여유 확보)

---

## 🎯 마일스톤 목표

**캡스톤 1 발표(2026-06-10) 데모 인프라 풀세트 박음**. 메인 → 캐릭터 선택(전사/원거리) → 마을(NPC 대화) → 전투(M3 broadcast + M4.1 정밀화 후) → 보스 → 엔딩 흐름을 *시연 처음부터 끝까지 끊김 없이* 보여줄 수 있는 상태 도달.

**5 Phase 구성**:

1. **Phase 01** — PRD 갱신 + 마일스톤 표 정합 (단순/meta, [H] 결정 박제)
2. **Phase 02** — 메인화면 UI + 엔딩 화면 (보통/client)
3. **Phase 03** — 캐릭터 선택 (PDL + 서버 stats + 클라 UI, 복잡/server+shared+client)
4. **Phase 04** — NPC 대화 (보통/client, 클라 단독 hardcoded)
5. **Phase 05** — Hamachi 셋업 검증 + M3.8 마감 의례 (보통/qa+meta)

**왜 본 마일스톤이 필요한가** (5/22 의논 핸드오프 §1~§3 정합):

- **교수 약속 (5/20 면담)** = "4맵 + 정밀 전투 + 하나의 완성된 Flow". *완성된 Flow* = 메인부터 엔딩까지 끊김 없는 시연. M3 응급 데모(broadcast + 단순 전투)는 *전투만* — 시연 시작점/종료점 없음
- **M4 영역과 분리** = M4 = *본 마감용 정밀화* (Combat & Map Transition). Demo 인프라(메인/캐릭터 선택/NPC/엔딩)는 *캡스톤 1 시연용* — 본 마감 후 일부 제거 가능 (NPC hardcoded → M6 길드 진입 시 정식화). 두 영역 의미 분리 = 별 마일스톤 정합
- **PRD 정합 의무** = MVP 제외 항목에 *직업/스킬 트리, 퀘스트/NPC* 박혀있음. 캐릭터 선택(전사/원거리, 스탯 분기) + NPC 대화(단순) 도입 시 PRD 갱신 의무 ([H] 위험도, CHANGELOG entry 의무)

**비-목표 (M4.1/M4.2/M4.3 또는 별 마일스톤로 미룸)**:

- 진짜 4맵 분리 + portal handoff → M4.2
- 정밀 전투 (lag compensation + AABB hitbox) → M4.1
- 데미지 공식 분리 (Formulas.cs) → M4.1 Phase 02 (M3.8 Phase 03 PlayerStats 흡수 후 진행)
- 스킬 트리 / 직업별 스킬 → M6 이후 (PRD 정합 — 본 마일스톤은 *기본 직업 2종 스탯 분기*만)
- NPC 대화 분기 / 퀘스트 시스템 → M6 길드 진입 시 정식화 (본 마일스톤은 *클라 단독 hardcoded 텍스트*만)
- PvP 지원 ADR → M4.3 (캐릭터 선택 도입 후 자연 트리거, 본 마일스톤 박지 X)

---

## 📋 Phase 분해 (5개)

| # | Phase | 등급 | 도메인 | 예상 | 담당 |
|---|---|---|---|---|---|
| 01 | PRD 갱신 + 마일스톤 표 정합 | 단순 | meta | 0.5~1h | 메인 직접 |
| 02 | 메인화면 UI + 엔딩 화면 | 보통 | client | 1~2h | client SubAgent |
| 03 | 캐릭터 선택 (PDL + 서버 stats + 클라 UI) | 복잡 | server+shared+client | 3~4h | coordinator + Worker 3 |
| 04 | NPC 대화 (클라 단독 hardcoded) | 보통 | client | 1~2h | client SubAgent |
| 05 | Hamachi 셋업 검증 + M3.8 마감 의례 | 보통 | qa+meta | 1~2h | qa SubAgent + 메인 |

**총 등급 = 복잡** (마일스톤 자체) — Phase 03 복잡(3 도메인 cross) + 나머지 단순/보통.

**박제 정합**:
- Phase 01·02·04·05 = 단순/보통 = work-pin + commit (-DONE.md 없음)
- Phase 03 = 복잡 = -DONE.md 박음
- 마일스톤 마감 = M3.8-마감 별 -DONE.md (복잡 등급 의무) + ADR-024 cadence false-promise 점검 결과 섹션 의무

---

## 🔗 의존성 그래프

```
Phase 01 (PRD 갱신) ←──── 병렬 가능 (다른 도메인)
   │                          │
   │   결정 박힘이             │
   │   다른 Phase의            │
   │   PRD 정합 의무 해소      │
   ↓                          ↓
   ↓                       Phase 02 (메인 + 엔딩 UI) ←─── 병렬 가능 (Scene 분리)
   ↓                          │                          │
   ↓                          ↓                          ↓
Phase 03 (캐릭터 선택) ─────────────────────────────── client SubAgent 영역
   │                                                  
   │  PlayerStats 박힘이 Phase 04 마을 진입 흐름의      
   │  *캐릭터 표시* 자연 흐름 트리거                   
   ↓                                                  
Phase 04 (NPC 대화) — 마을 Scene 진입 흐름 자연
   │
   │  모든 영역 박힌 후 Hamachi 검증 + 마감 의례
   ↓
Phase 05 (Hamachi + M3.8 마감 의례)
```

**의존성 사유**:
- Phase 01 *먼저 또는 병렬* = PRD 갱신은 *결정 박제*라 다른 Phase의 진입 트리거 영향 X. 단 Phase 03 진입 *전*엔 박혀있는 게 정합 (캐릭터 선택 = MVP 제외 항목 정정과 정합)
- Phase 02 *Phase 03 전 또는 병렬* = 메인화면은 캐릭터 선택 *입구*. 엔딩은 보스 처치 *출구*. 둘 다 캐릭터 선택과 영역 분리
- Phase 03 *Phase 04 전* = 마을 진입 시 *캐릭터 표시*가 PlayerStats(class enum) 박힌 후 자연. Phase 03 미박힘 상태에서 Phase 04 진행 시 *placeholder 캐릭터*로 박아야 함 — 이중 작업
- Phase 05 *모든 Phase 마감 후* = Hamachi 셋업 검증은 *완성된 인프라*가 있어야 의미 있음. 마감 의례는 마일스톤 종합

**병렬 가능** = Phase 01 ↔ Phase 02 (meta vs client 도메인 분리) / Phase 02 ↔ Phase 04 (둘 다 client 단독, Scene 영역 분리)

---

## ✅ 마일스톤 완료 조건

- [ ] Phase 01 = `PRD.md` 마일스톤 표에 M3.8 줄 추가 + MVP 제외 항목 정정 + CHANGELOG entry [H] 박음
- [ ] Phase 02 = `MainMenu.unity` Scene 신설 (시작/종료 버튼) + `Ending.unity` Scene 신설 (또는 기존 `StageClearUI.cs` 활용 + 시연용 정밀화)
- [ ] Phase 03 = PDL `C_CharacterSelect { byte characterClass }` 패킷 신설 + `CharacterClass : byte { Warrior=0, Ranger=1 }` enum + ProtocolVersion bump (3→4)
- [ ] Phase 03 = `PlayerStats { class, hp, maxHp, attack, defense, moveSpeed }` 서버 측 분기 + `EnterGameWorld` 시점에 캐릭터 클래스 검증 + default stats 박음
- [ ] Phase 03 = 클라 `CharacterSelect.unity` Scene + 2 버튼 (전사/원거리) + 선택 후 마을 진입 패킷 전송
- [ ] Phase 03 = 단위 테스트 5건+ (`CharacterSelectHandlerTests` — happy 전사/원거리 + invalid characterClass(2/255) 2건 + 중복 선택 1건)
- [ ] Phase 04 = 마을 NPC GameObject placeholder + interactable 컴포넌트 (E 키 트리거 + 단순 텍스트 출력)
- [ ] Phase 04 = NPC 대화 텍스트 클라 단독 hardcoded (예: "보스가 마을을 위협하고 있어요. 도와주세요!") — 서버 패킷 X
- [ ] Phase 05 = 본인 + 정유현 환경 Hamachi 셋업 검증 (백업 = 본인 단독 로컬 데모)
- [ ] Phase 05 = M3.8-마감 별 -DONE.md 박음 (복잡 등급) + false-promise 점검 결과 섹션 의무 (ADR-024 cadence)
- [ ] dotnet test green (회귀 0, M3 baseline + 단위 테스트 5건+ 추가)
- [ ] Unity batchmode compile 통과 (Phase 03 Shared.dll 변경 후 의무)
- [ ] CHANGELOG [H] entry 박음 (Phase 01에서 박음) + [M] entry 박음 (Phase 03/05 마일스톤 마감 시점)
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점" = M3.8 완료 + M4.1 진입 대기로 갱신
- [ ] commit + push + PR 게이트 (사용자 명시 GO 후 머지)

---

## ⚠️ 주의할 약속 (헌법 절대 원칙 충돌 가능 항목)

1. **헌법 #1 (Server Authority)** — Phase 03 캐릭터 스탯 = *서버 권위 의무*. 클라가 `C_CharacterSelect { characterClass }` 보내면 서버가 *characterClass만 받고 stats는 서버에서 박음*. 클라가 stats(HP/Attack) 조작 시도해도 무시 (`EnterGameWorld`에서 서버 측 분기로 default stats 박음).
2. **헌법 #2 (Protocol is Sacred)** — Phase 03 `C_CharacterSelect` PDL XML append-only + PacketGenerator 재생성 + Shared.dll commit 의무. **ProtocolVersion bump 3→4 박음** (append-only 필드 추가는 backward compatible이지만 클라 측 옛 빌드 호환성 위해 bump). **M4.1 Phase 03 `C_Attack` 변경은 별 bump (4→5) 박음** = 두 마일스톤 영역 분리 의미 + 시연 시점 호환성 추적 쉬움 (plan-auditor 개선 제안 봉합 결정).
3. **헌법 #3 (Trust Boundary)** — Phase 03 `characterClass` 입력 범위 검증 (0/1만 허용). 2/255 등 그 외 값은 *silent drop* (cheat-flag 후보, M4.2 cheat-flag 도입 시 기록) 또는 *handshake 실패 응답*. AttackHandler 패턴 정합.
4. **헌법 #4 (Shared Code Discipline)** — Phase 03 PDL 변경 + `PlayerStats` 신설 + ProtocolVersion bump = *Shared.dll 양쪽 영향*. 변경 후 `dotnet build` 양쪽 컴파일 + Unity 측 batchmode compile (`unity-bridge` SubAgent 또는 메인 직접) + Shared.dll commit 정합 의무. CHANGELOG 2026-05-17 (Shared.dll 미commit 사고) 학습 정합.
5. **헌법 #5 (틱 블로킹 금지)** — Phase 03 캐릭터 선택 처리 = handshake 직후 1회 처리, tick 영향 X. `EnterGameWorld` 진입 시점에 박음, tick thread 안에서 동기 처리 (await/Task.Delay 없음).

**ADR-021 (UI Additive Scene 분리)** — Phase 02 `MainMenu.unity` + `Ending.unity` / Phase 03 `CharacterSelect.unity` = 별 Scene 박음 의무. `Gameplay.unity`와 분리. 정유현 영역 침범 차단 (`Scripts/UI/`는 정유현 영역, 본 마일스톤은 *Scripts/Scene 흐름*만 박음).

---

## 📚 학습 포인트 (마일스톤 차원)

- **시연용 인프라 ≠ MVP 인프라** — 데모용 hardcoded NPC 대화는 *MVP 제외 항목*과 충돌. 시연 마일스톤에선 PRD 갱신([H])이 의무. 학부생 본인 결정만으로 박지 말고 *결정 박제* 의례 거치기.
- **마일스톤 분리 vs 흡수 판단** — Demo 인프라를 M4에 흡수할 수도 있었음. 그러나 *영역 의미*가 다름 (M4 = 본 마감용 정밀화 / Demo = 시연용 한정). 별 마일스톤로 분리하면 *본 마감 후 제거 가능* + 마일스톤 단위 호흡 유지.
- **PDL append-only + ProtocolVersion bump 두 번째 실측** (첫 번째 = M3 Phase 02 handshake). `C_CharacterSelect` 신설 = backward compatible이지만 옛 빌드 클라 호환성 위해 bump. 헌법 #2 정합 패턴.
- **서버 권위 = 클라 input만 받기, 서버에서 stats 박음** — Phase 03 `EnterGameWorld { characterClass }` → 서버 측 분기 `if (characterClass == Warrior) stats = DefaultWarriorStats`. 클라가 stats 직접 보내는 패턴은 *권위 위반*.
- **ADR-021 UI Additive Scene 패턴 첫 실측** — 옛 ADR 박힘 시점 박았지만 실측 0건. 본 마일스톤에서 메인 + 캐릭터 선택 + 엔딩 = 3 Scene 분리 박음 = ADR-021 가치 첫 정량 검증.

---

## ➡️ 다음 마일스톤

- **M4.1 — Combat Precision** (Phase 01·02·03). 본 마일스톤 Phase 03 박힌 `PlayerStats` 흡수 후 진행. `Formulas.cs` 시그니처 갱신 필요 (`ComputeDamage(attackerStats, targetStats)` — stats가 클래스 인식). M4.1 plan은 본 마일스톤 마감 시점에 *PlayerStats 흡수 반영* 갱신 의무.
- **M4.2 — Map Transition** (Phase 04·05·06, placeholder). 캡스톤 1 후반 = 6/3~6/9. 발표 데모 = M3.8 + M4.1 + M4.2 종합.
- **M4.3 — AI + Polish** (Phase 07·08·09, placeholder). 캡스톤 1 후 7월~10월.

---

## 갱신 이력

- 2026-05-22 — 본 plan 박힘. 5/22 본 세션 의논 핸드오프(`_session-handoff-2026-05-22.md`) §1~§9 흡수. M3.8 신설 결정 + 5 Phase 분해 (옛 핸드오프 §8.2 4 Phase 후보 → PRD 갱신 별 Phase 01로 분리 = 5 Phase). 캡스톤 1 마감 6/10 정합. M4.1 plan은 본 마감 시점에 PlayerStats 흡수 반영 갱신 예정.
- 2026-05-22 — plan-auditor 결과 봉합 박힘 (결함 3 + 개선 제안 4): Phase 03 등급 격차 결정 박음 (복잡 유지 + 5단계 보고는 마일스톤 마감 통합) / Phase 02 Ending Scene = 옵션 A 의무 박음 (ADR-021 정합) / Phase 04 NpcDialogPanel 이름 박음 (Scripts/Gameplay/, 정유현 영역 침범 차단) / Phase 01 등급 단순 유지 + 사유 박음 / ProtocolVersion bump = M3.8 (3→4) + M4.1 (4→5) 별 bump 결정 / Phase 03에 Hamachi 시간 조율 트리거 한 줄 박음 / Phase 05 브랜치 삭제 = 사용자 명시 GO 후 명시.
