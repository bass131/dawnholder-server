---
owner: youngho
milestone: M4.3
phase: 11
title: 애니 외관 완성 — Animator 6상태 클립 × 3객체 + RemotePlayer prefab 정합
status: pending
grade: 복잡
risk: unity-asset
estimated: 4~6h (본인 에셋 critical path)
domain: client (본인 외관 분담)
summary: 08b가 깐 AnimatorDriver 계약 위에 실제 Animator 6상태 클립·전이를 player/enemy/boss에 셋업 + RemotePlayer prefab 정합
---

# Phase 11: 애니 외관 완성

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (client / Animator 6상태 × 3객체 + prefab — unity-asset 깃발 + 발표 critical path)
> **담당**: **본인 (외관/시각 분담 — 핵심)** + client SubAgent (계약 문서/wiring 보조)

---

## 🎯 목표

08b가 깐 **`AnimatorDriver` 계약**(`AnimState` int 파라미터) 위에, **실제 Animator 6상태 클립 + 전이**를 셋업해서 화면에 진짜 애니메이션을 띄운다. LocalPlayer / RemotePlayer / Enemy(+Boss) 세 종류가 Idle/Walk/Jump/Attack/Hit/Death를 자연스럽게 재생하고, 흩어진 RemotePlayer prefab을 1개로 정합한다.

이 Phase가 끝나면 **발표 데모에서 모든 캐릭터가 메이플 스타일로 살아 움직인다** (08a 데이터 + 08b 구조 + 11 외관 = 상태머신 완성).

### ⚠️ 발표 critical path (2026-05-30 의논)
AI 몫(08a/08b 코드)은 빨리 깔리지만, **본인 몫(Animator 클립 × 캐릭터)이 발표(6/10) 페이스를 정함.** 6상태를 다 못 채우면 가진 상태부터 wiring하고 점진 추가 — 없는 상태는 placeholder(Idle 유지)로도 깨지지 않게(08b가 Animator/파라미터 null 가드). **전부 vs 우선순위(Walk/Attack/Death 먼저)는 본인 진도에 따라 조절.**

---

## ⏪ 사전 조건

- [ ] **Phase 08b 완료** — `AnimatorDriver` + `IMotionState` + 파라미터 계약(`"AnimState"` int) 존재
- [ ] **Phase 08a 완료** — 서버가 animState 송신 (RemotePlayer/enemy가 받을 상태 데이터)
- [ ] (Attack/Hit/Death 완성도 높이려면) Phase 09 boss attack animState 연동 권장 — 단 11은 09와 병렬 가능(클립 셋업은 독립)

---

## 📝 작업 내용

### Animator 클립 + 전이 셋업 (본인 외관 분담)
- [ ] **Animator Controller** — `AnimState`(int) 파라미터 기준 상태 전이 그래프. 6상태: Idle(0)/Walk(1)/Jump(2)/Attack(3)/Hit(4)/Death(5)
  - `AnimatorDriver`가 `SetInteger("AnimState", n)` 하므로, 각 상태를 `AnimState == n` 전이 조건으로 연결
  - 파라미터 이름/타입은 08b 계약과 **정확히 일치** (drift 시 애니 안 바뀜)
- [ ] **클립** — 캐릭터별(Player 전사/원거리, Enemy Mushroom, Boss ToxicFrog) 6상태 sprite 애니. 가진 sprite 자원 한도 내에서(없으면 Idle 재사용 placeholder)
- [ ] 좌우 flip은 `AnimatorDriver`가 SpriteRenderer.flipX로 처리 — 클립은 한 방향만 제작

### prefab 정합 (cleanup 의무 — work-pin)
- [ ] **RemotePlayer prefab 3개 → 1개**: `RemotePlayer.prefab` / `RemotePlayer.backup.prefab` / `Resources/RemotePlayer.prefab` 중 실제 로드 경로(`CombatBootstrap`/`Resources.Load`) 추적 → 정본 1개 통일, 나머지 제거 (백업은 git 이력)
- [ ] 정본 prefab에 `RemoteEntity`(보간) + `AnimatorDriver` + `RemotePlayerMotion` + Animator(Controller 연결) 컴포넌트 정합
- [ ] **prefab 작업 전 백업 의무** (Phase 08 BackGround prefab 사고 학습)

### enemy Animator 부착 (본인 + 보조)
- [ ] enemy는 현재 런타임 placeholder(SpriteRenderer만). Animator 부착 방식 결정 — `EnemyViewFactory`에서 `AnimatorController` 런타임 할당 or enemy prefab화. (코드 부분 client SubAgent 보조, 클립/Controller는 본인)

### 테스트
- [ ] Play 실측 — LocalPlayer/RemotePlayer/Enemy 각각 이동 시 Walk, 정지 Idle, 점프 Jump, 공격 Attack, 피격 Hit, 사망 Death 재생 + flip
- [ ] 멀티(클라+봇 또는 2대) — 상대 RemotePlayer 애니 자연스러움

---

## ✅ 완료 조건

- [ ] **RemotePlayer prefab 1개로 정합** (로드 경로 단일, 중복/backup 제거) — drift 0
- [ ] LocalPlayer/RemotePlayer/Enemy(+Boss) **각 객체가 보유 상태 클립을 재생** (최소 Idle/Walk/flip + 발표 우선순위 상태). 미보유 상태는 placeholder로 깨짐 0
- [ ] Animator 파라미터 계약(`"AnimState"` int)이 08b `AnimatorDriver`와 일치 — 서버 animState 변화가 실제 클립 전환으로 보임
- [ ] **회귀 0**: 기존 위치 보간(LocalPlayer prediction / RemotePlayer·enemy 보간) 그대로, enemy spawn/death lifecycle 정상
- [ ] prefab 변경 백업 완료 (unity-asset 안전)
- [ ] Play 발표 시나리오에서 애니 깨짐/T-pose/멈춤 0

---

## 🧪 테스트

**수동**:
- Play 풀 루프 — 마을/사냥터/보스방에서 3객체 6상태 애니 관찰 + flip
- prefab 정합 후 외관 회귀 (enemy/Player/Boss 크기·위치·HP바 그대로)

---

## 📚 학습 포인트

- **코드 계약 ↔ 에셋 계약**: `AnimatorDriver`가 `SetInteger("AnimState")` 하면, Animator Controller도 정확히 그 이름/타입의 파라미터를 가져야 함. 코드와 에셋이 "계약"으로 만나는 지점 — 이름 한 글자 틀리면 조용히 안 됨.
- **placeholder 우아한 저하(graceful degradation)**: 클립이 없어도 Idle로 떨어지면 데모가 안 멈춤. 완성도를 점진적으로 올리는 안전한 방식.
- **prefab 단일 진실**: 같은 prefab 3개 = 어느 게 진짜인지 모름 → drift. 정본 1개 + 명확한 로드 경로.

---

## ⚠️ 함정 / 주의사항

- **unity-asset 위험 깃발** (Phase 08 사고): prefab/Animator 편집·삭제 전 백업. scene/prefab YAML 손편집 금지 — Unity Editor 직접 또는 메인 세션 MCP.
- **파라미터 계약 drift = 무증상 실패**: 애니가 "그냥 안 바뀜"으로 나타나 디버깅 어려움. 08b 계약 상수를 정확히 보고 셋업.
- **Resources.Load 경로 의존**: prefab 정리 시 로드 경로 깨지면 RemotePlayer가 아예 안 뜸 — 정합 후 Play 필수 확인.
- **클립 길이 vs animState latch**: 08a가 Attack/Hit를 N틱 유지(latch)하는데, 클립 길이와 안 맞으면 끊기거나 늘어짐. 클립 길이 ↔ 서버 latch 틱 수 대략 정합.
- **발표 일정 압박**: 6상태 × 3캐릭터를 다 못 만들면 *우선순위*로. Walk(이동)/Attack(전투)/Death(처치)가 발표 임팩트 핵심. Jump/Hit는 여유 시.

---

## ➡️ 다음 Phase

- Phase 12 — M4.3 회귀 테스트 + 가벼운 마감

---

## 📋 박제 (완료 후)

- **복잡 등급** — `11-anim-visual-completion-DONE.md` 박음. (prefab 정합 결정 + Animator 계약은 commit message에도 명확히)

---

## 작업 로그

- 2026-05-29: (구) RemotePlayer 외관 봉합으로 계획 수립
- 2026-05-30: **애니 상태머신 재편** — 기존 11(RemotePlayer Animator 봉합)을 흡수해 "애니 외관 완성(3객체 6상태)"으로 확대. 08b `AnimatorDriver` 계약 위에 실제 클립 셋업.
