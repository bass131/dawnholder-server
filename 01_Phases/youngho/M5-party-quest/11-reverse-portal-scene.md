---
owner: youngho
milestone: M5
phase: 11
title: 역방향 포탈 씬 배치 (Unity 외관, placeholder swap-ready)
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 11: 역방향 포탈 씬 배치 (Unity 외관, placeholder swap-ready)

> **상태**: pending
> **마일스톤**: M5 (트랙 B — 포탈 메커니즘 / B3)
> **등급**: 단순 (단순이나 **unity-asset 위험 깃발** — prefab/scene 변경, 백업 의무)
> **담당**: client / unity-bridge (씬 배치는 Unity MCP 또는 아침 영호 육안)

---

## 🎯 목표

Phase 09에서 만든 역방향 포탈 *데이터*를 실제 **씬에 GameObject로 배치**한다. 각 맵(Town/HuntingGround/BossRoom)에 역방향 Portal 오브젝트 + `PortalTrigger`(portalId=2) + 스프라이트를 두어, 플레이어가 눈으로 보고 위 방향키로 되돌아갈 수 있게 한다. 포탈 스프라이트 에셋이 아직 없으므로 **placeholder + swap-ready**로 배선한다.

---

## ⏪ 사전 조건

- [ ] Phase 10 (B2 — 포탈 진입 겹침+위키) 완료 — `PortalTrigger` 동작이 씬 배치 검증의 전제.
- [ ] Unity Editor 열림 + MCP 브리지 연결 (야간 MCP 시도 시) 또는 아침 영호 육안.

---

## 📝 작업 내용

- [ ] 각 맵 씬에 역방향 Portal GameObject 추가:
  - **Town 씬**: (BossRoom→HuntingGround / HuntingGround→Town 도착 지점에 맞춰) 필요 시
  - **HuntingGround 씬**: HuntingGround→Town 역방향 포탈
  - **BossRoom 씬**: BossRoom→HuntingGround 역방향 포탈
- [ ] 각 역방향 Portal에 `PortalTrigger` 컴포넌트 + **`portalId=2`** 설정 (Phase 09 데이터와 정합).
- [ ] **스프라이트 = placeholder (swap-ready)** — 아래 swap-ready 규율 준수.

### Swap-Ready 배선 (에셋 없음 → placeholder)

- [ ] **SpriteRenderer 슬롯 분리** — 포탈 sprite를 코드에 하드코딩하지 않고 SpriteRenderer 컴포넌트 슬롯(Inspector)으로 노출. placeholder는 정방향 포탈과 동일한 sprite를 재사용하거나 기본 quad/단색 sprite.
- [ ] **Animator 슬롯 미리 부착** — 진짜 포탈이 애니메이션(빛나는 효과 등)일 수 있으므로 Animator 컴포넌트 + controller 슬롯을 *미리* 부착. placeholder가 정적 sprite여도 컴포넌트 구조는 진짜 에셋과 동일하게.
- [ ] **swap 지점 박제 의무** — 완료 후 `-DONE.md`(또는 마일스톤 노트)에 **"어느 GameObject의 어느 SpriteRenderer/Animator 슬롯에, 어느 경로의 진짜 sprite/animator controller를 꽂으면 코드 변경 0으로 동작하는지"**를 명시.

---

## ✅ 완료 조건

- [ ] **4맵 양방향 이동 육안 확인** (아침) — 정방향/역방향 둘 다 포탈로 왕복 가능.
- [ ] 역방향 Portal에 `PortalTrigger`(portalId=2) + SpriteRenderer + Animator 슬롯이 모두 배선됨 (placeholder여도 구조 완비).
- [ ] **백업 의무 이행** — 씬/prefab 편집 전 백업 (Phase 08 BackGround prefab 사고 학습).
- [ ] swap 지점이 `-DONE.md`에 박힘 (영호가 진짜 에셋을 바로 꽂을 수 있게).

---

## 🧪 테스트

**자동**: 없음 (씬 외관).
**수동(아침)**: 영호 육안 — 4맵 양방향 왕복, placeholder 포탈이 보이고 진입 동작, swap 슬롯 확인.

---

## 📚 학습 포인트

- **swap-ready 패턴** — 에셋이 없어도 *같은 컴포넌트 구조*(SpriteRenderer/Animator)로 placeholder를 배선하면, 나중에 reference만 바꿔 진짜 에셋을 꽂는다. 에셋 바인딩을 로직에서 분리(헌법 외부화 정신). "코드 변경 0 드롭인"이 목표.
- **씬 편집 = 위험 작업** — prefab/scene은 YAML이라 손상되면 복구가 어렵다. unity-asset 깃발이 단순 등급에도 백업 의무를 건다 (Phase 08 학습).

---

## ⚠️ 함정 / 주의사항

- **포탈 스프라이트 없음 → placeholder + swap-ready 의무** — SpriteRenderer/Animator 슬롯을 미리 분리·부착. 진짜 sprite/anim 드롭 = 즉시 동작. swap 지점을 `-DONE.md`에 박지 않으면 영호가 어디 꽂을지 모른다.
- **백업 의무** (Phase 08 prefab 사고 학습) — 씬/prefab 편집 전 반드시 백업.
- **portalId 정합** — 씬의 `PortalTrigger.portalId=2`가 Phase 09 `PortalTable` 역방향 항목과 일치해야 lookup 성공.
- **야간 MCP 조건** — Unity Editor 닫혀 있으면 이 Phase는 아침 자동 폴백 (헤드리스 1순위 블록과 독립).

---

## ➡️ 다음 Phase

- Phase 12 (P1) — 클라 파티 패킷 핸들러 + 클라 파티 미러 상태 (트랙 P 시작).

---

## 📋 박제 (완료 후)

- 단순 → work-pin + commit message. **단, swap 지점은 `-DONE.md` 또는 마일스톤 노트에 별도 박제 의무** (swap-ready Phase 규율).

---

## 작업 로그

- 2026-06-14: 생성.
