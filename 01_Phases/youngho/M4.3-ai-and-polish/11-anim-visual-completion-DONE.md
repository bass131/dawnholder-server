---
owner: youngho
phase: 11
title: 애니 외관 완성 — 캐릭터/적 클립 제작 + Animator 계약 wiring 5종
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.3 Phase 11 완료(재편 종료). 본인이 5종(Knight/Mage/Slime/Golem/Boss) 클립·상태 배치 제작, AI가 08b 계약 wiring(AnimState 파라미터+Any State 전이+Loop 정리)을 Unity MCP로 일괄 박음. Knight 공격 2종 랜덤(AttackVariant)+2단 체인(Jump/Stabbing)은 directed 진입으로 핑퐁 방지. prefab 연결+Play 실측은 2026-06-06 마일스톤 재편으로 M4.4-05에 흡수(본인 결정 — Art prefab 단독 조작 불가가 정상이라 기능 구현과 병행 관측).
---

# Phase 11 박제: 애니 외관 완성

**소요**: 세션 8~11 (본인 클립 제작 critical path + AI wiring 1세션)

## TL;DR

본인이 만든 5종 캐릭터(Knight/Mage/Slime/Golem/Boss_Vampire)의 Animator를 08b가 박아둔 계약(`AnimatorDriver`가 매 프레임 int `"AnimState"` 0~5 set)에 맞게 wiring했다. 상태/클립/속도 튜닝은 본인이 에디터에서, 파라미터·전이·Loop 정리는 AI가 Unity MCP(AnimatorController API)로. 자산은 PR #60으로 main에 먼저 박혔고(173파일), 이후 폴더 재배치 + 슬라임 Hit/Death·골렘 5종(+Hit)·NPC 2종·타이틀 애니 추가(`b4ff1b2`) + wiring(`2940f2f`)으로 완성. **prefab 연결과 Play 실측은 마일스톤 재편으로 M4.4-05에 흡수** — Art용 controller는 완성됐지만 게임 prefab(LocalPlayer/RemotePlayer)에는 아직 미연결이 사실 상태.

## 박제 사실 (커밋 단위)

- `740d57e`/PR #60 (세션 8~9): 미술자산 173파일 main 박제 (Knight/Mage/Slime/Boss 시트+클립, Golem 컨셉, 배경, Boss Stabbing 이펙트)
- `901ca7b`: Town 옛 배경 제거 + Main Camera prefab + Knight/Slime controller default state (본인)
- `b4ff1b2` (세션 11): 221파일 — 슬라임 Hit/Death 모션 + 골렘 스프라이트 5종·Animator + NPC(BlackSmith/Glocery) + MainMenu 타이틀 배경 애니(세션 10 Image 바인딩 fix 포함) + 폴더 재배치(Knight/Mage→Playable/, Boss→Enemy/Boss_Vampire/, UI_character→UI/UI_character_Icon/ — R100 rename, guid 보존) + 옛 배경 삭제(guid 참조 전수 체크 후 의도된 Missing 2건 결정)
- `2940f2f` (세션 11): **Animator 계약 wiring 5종** + `AnimatorDriver.cs` AttackVariant + Golem Hit(본인 제작) — 상세 아래

## Wiring 내용 (`2940f2f`, 18파일)

| Controller | 파라미터 | Any State | 특수 |
|---|---|---|---|
| Knight | AnimState + **AttackVariant** | 6 (0/1/3×2/4/5) | 공격 0/1 랜덤 분기, 지상 5상태→Jump_Start directed, Start→Peek(풀재생→스왑), **Peek→Exit 제거**(공중 Idle 튕김 방지) |
| Mage | AnimState | 5 | 지상 4상태→Jump_Start directed. Cast_Channeling=스킬용 의도적 미연결 |
| Slime | AnimState | 5 | — |
| Golem | AnimState | 5 (Hit 포함) | default state Move→Idle(본인 선정정) |
| Boss | AnimState | 4 (0/1/4/5) | Idle/Walk/Hit→Stabbing_Start directed→(풀재생)→End 체인 |

- 전이 공통: Equals 조건 + Exit Time 해제 + Duration 0(픽셀아트 블렌드 무의미) + Can Transition To Self 해제
- Loop off 9건 (Attack/Hit/Jump_Peek 계열 — 본인 테스트용 임시 On 정리). Idle/Walk만 Loop 유지
- `AnimatorDriver.cs`: Attack 진입 순간 `AttackVariant` 0/1 랜덤 set (파라미터 보유 controller만 — 1회 캐시. 시각 전용 랜덤, 판정은 서버 권위라 무해)

## AC 검증 결과

- wiring 5종 디스크 YAML 검증: 파라미터 m_Type 3(int) + 조건 mode 6(Equals) + CanTransitionToSelf 0 전수 확인, Knight AnimState 조건 11(AnyState 6+directed 5)/AttackVariant 2/Exit 잔존 0
- Loop 재확인 9/9 off. 에디터 콘솔: wiring 관련 에러 0 (Golem_Hit.png import reset 1건은 Unity AI Image 패키지 잔여 — 무관)
- ⚠️ **Play 실측 미수행 — 의도** (아래 이월)

## 결정 흐름 (회고 참고용)

- **2단 체인은 directed 진입** — Any State 전이는 "조건 참인 동안 계속" 평가라 Peek/End에서 체인 앞상태(Start) 재발동 핑퐁. Can Transition To Self 해제는 자기 자신만 막음. 지상 상태들→Start 직접 전이가 정답.
- **Knight 공격 랜덤 = 파라미터 분기** — Animator 단독 랜덤 불가 → AttackVariant int + AnimatorDriver가 진입 에지에서 Random.Range(0,2). 클라 시각 전용이라 헌법 #1 무위반.
- **Unity MCP RunCommand(AnimatorController API) > YAML 손편집** — 5종 wiring 무사고. Phase 08 BackGround 사고 학습 정합 (백업: .claude/backup/Golem.controller.bak).
- **폴더 이동 안전 검증 = git rename 검출** — Unity 폴더 이동은 status에 D+?? 로 찍히지만 add 후 R100이면 guid 보존 확인.

## 막혔던 지점 / 이월

- **prefab 연결 + Play 실측 → M4.4-05 흡수** (2026-06-06 마일스톤 재편, 본인 결정): LocalPlayer prefab은 아직 옛 Player.controller + PlayerAnimatorSync(IsMoving), RemotePlayer는 controller null. Art prefab엔 조작 스크립트가 없어 단독 테스트 불가가 정상 → 직업 장착 구조(ClassConfig SO) 구현하면서 관측 병행. 관측 체크리스트: Knight 점프 Start→Peek 유지(핑퐁 0) / 공격 두 모션 혼합 / 슬라임 Hit·Death.
- **Boss 연속 공격 시 End hold 고정 가능성** — animState 3이 끊김 없이 유지되면 End에서 멈춤. Phase 09(M4.5) 보스 공격 cadence 설계 시 확인.
- **의도된 Missing 2건** (`b4ff1b2`): BossRoom/HuntingGround 배경(본인 새 컨셉 예정) + Gameplay.unity 죽은 오브젝트 2개.

## 학습 일지 후보 키워드

Any State 전이의 지속 평가 vs directed 전이 / 시각 전용 랜덤과 서버 권위의 경계 / Unity 에디터 API 기반 자산 wiring(MCP) / 클립 바인딩 classID(SpriteRenderer vs UI Image — 세션 10) / git rename 검출 = guid 보존 검증 수단

## 다음 Phase

- **M4.3 Phase 12** — 경량 마감 (회귀 + 현 브랜치 PR 머지)
- **M4.4-05** — 직업 장착 구조에서 본 Phase 이월분(prefab 연결 + 관측) 흡수
