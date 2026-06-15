---
owner: youngho
milestone: M6
phase: 01
title: Animator ExitTime 보정 (보스 피격복귀 + 일반몹 공격)
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~4h
domain: client
summary: 보스 피격 복귀 후 공격 모션 완주 + 일반몹 공격 모션 완주 후 IDLE 복귀하도록 Animator 전이 Exit Time 조정
---

# Phase 01: Animator ExitTime 보정 (보스 피격복귀 + 일반몹 공격)

> **상태**: pending
> **마일스톤**: M6
> **등급**: 복잡 (unity-asset 위험깃발 → 보통에서 상향)
> **담당**: client (Animator는 03_Client 에셋) — 실작업은 영호 육안 검증 게이트

---

## 🎯 목표

보스가 피격(HIT) 상태에서 복귀한 직후 공격 모션이 끝까지 재생되고,
일반 몹(Slime/Golem)이 공격 모션을 완주한 뒤에야 IDLE로 복귀한다.
"공격이 어색하게 뚝 끊긴다"는 2차 플레이테스트 체감 문제 해소.

---

## ⏪ 사전 조건

- [ ] M5 main 머지 완료 (충족 — #111)
- [ ] M6 브랜치(`feature/m6-playtest-polish`) 생성 (충족)
- [ ] **분담 확인**: Animator 조정을 AI(MCP)가 1차 할지 영호 직접 할지 Phase 시작 시 질문

---

## 📝 작업 내용

대상 컨트롤러 (실측 확인됨):
- `03_Client/Assets/Art/Enemy/Boss_Vampire/Animator/Boss_Animator.controller`
- `03_Client/Assets/Art/Enemy/Slime/Animator/Slime.controller`
- `03_Client/Assets/Art/Enemy/Golem/Animator/Golem.controller`

- [ ] 보스 HIT→공격 전이 / 공격→IDLE 전이의 `m_HasExitTime`·`m_ExitTime`·`m_TransitionDuration` 현재값 실측
- [ ] 보스: 공격 모션이 끝까지 재생되도록 Exit Time 보정 (클립 길이 기반)
- [ ] 일반몹(Slime): 공격→IDLE 전이가 클립 완주 후 일어나도록 Exit Time/HasExitTime 설정
- [ ] 일반몹(Golem): 동일 패턴 적용
- [ ] 서버 권위와 충돌 없는지 점검 — Animator 전이는 클라 표현 전용이어야 하고, 서버 상태(AnimState)가 진실. 표현 타이밍만 늘리는 것이지 게임플레이 판정은 서버 유지 (헌법 #1)

---

## ✅ 완료 조건

- [ ] 보스 피격 직후 공격 모션이 중간에 끊기지 않고 끝까지 재생됨 (영호 육안)
- [ ] 일반몹 공격 모션이 완주된 뒤 IDLE 복귀 (영호 육안)
- [ ] Animator 변경이 .controller YAML diff로만 잡힘 (게임플레이 코드 무변경 또는 표현 타이밍 한정)
- [ ] WSL2 회귀 게이트 그대로 green (서버 무관 변경 확인)

---

## 🧪 테스트

**수동 (영호 육안)**:
- 보스전에서 보스를 때려 피격시킨 뒤 보스 공격이 자연스럽게 끝까지 나오는지
- 일반몹 공격 모션이 완주되는지

**자동**:
- 서버측 무변경이면 WSL2 회귀 644/0/5 유지 확인

---

## 📚 학습 포인트

- **Exit Time**: 전이가 일어나기까지 현재 상태(클립)가 재생되는 정규화 시간(0~1). `Has Exit Time`이 꺼져 있으면 조건 충족 즉시 전이 → 모션이 잘림.
- **Transition Duration**: 두 상태가 블렌딩되는 시간. 0이면 딱 끊김.
- 서버 권위 게임에서 Animator는 "표현"일 뿐 — 타이밍을 늘려도 데미지/판정은 서버 tick 기준이라는 경계.

---

## ⚠️ 함정 / 주의사항

- Exit Time을 1.0으로 박으면 클립 100% 재생을 기다림 — 반응성이 필요한 전이(피격 진입 등)엔 오히려 독. 들어가는 전이(→HIT)는 즉시, 나가는 전이(공격→IDLE)는 완주, 식으로 방향별로 구분.
- AnyState 전이가 우선순위를 가로채면 Exit Time이 무시될 수 있음 — AnyState 조건도 함께 점검.
- `.controller` 직접 YAML 편집은 fileID 깨짐 위험 — MCP RunCommand(AnimatorController API) 또는 Unity 에디터 우선, 손편집은 최후.

---

## ➡️ 다음 Phase

- Phase 02 — 렌더 소팅 레이어 정립 (독립, 병렬 가능)
