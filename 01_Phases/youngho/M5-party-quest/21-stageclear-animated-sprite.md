---
owner: youngho
milestone: M5
phase: 21
title: StageClear 폰트(TMP) → 애니 스프라이트 교체
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 21: StageClear 폰트(TMP) → 애니 스프라이트 교체

> **상태**: pending
> **마일스톤**: M5
> **등급**: 단순 (unity-asset — UI 컴포넌트 교체 + placeholder 스프라이트)
> **담당**: youngho (client)

---

## 🎯 목표

StageClear 연출이 지금은 TMP 텍스트("🎉 Stage Clear!", `StageClearUI.cs:54`)로 동작 중이다. 이걸 **애니메이션 스프라이트**(Image + Animator)로 교체한다. 단, 진짜 StageClear 애니 시트가 아직 없으므로 **placeholder + swap-ready** — 나중에 진짜 controller를 같은 슬롯에 꽂기만 하면 코드 변경 0으로 애니가 돌게 미리 배선한다.

---

## ⏪ 사전 조건

- [ ] 없음 (독립 Phase). 현재 `StageClearUI.cs:54` TMP 동작 확인만.

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scripts/UI/StageClearUI.cs` — TMP → `Image` 애니 방식으로 교체.
- [ ] placeholder 스프라이트 시트 + anim 생성 (정적 1프레임이어도 OK — 동일 컴포넌트 구조).
- [ ] **swap-ready 배선**: `Image` + **`Animator` 컴포넌트 + controller 슬롯을 미리 부착** → 진짜 controller 드롭 = 즉시 애니.
- [ ] 수명 로직(EffectLifetime/자동 destroy/페이드) 등은 placeholder에도 동일 적용.
- [ ] 씬/prefab 편집 시 **백업 의무**.

---

## ✅ 완료 조건 (정량)

- [ ] StageClear 시 (placeholder) 애니 스프라이트 표시 — TMP 텍스트 더 이상 안 뜸 (육안).
- [ ] Unity 컴파일 0err.
- [ ] **swap 지점 박제**: "진짜 StageClear 애니 시트를 `<경로>`에 넣고 controller를 `<슬롯>`에 꽂으면 즉시 애니" 를 -DONE/완료 노트에 명시 (아침 영호가 바로 교체 가능하게).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동**: 영호 Play — 스테이지 클리어 시 placeholder 스프라이트가 TMP 대신 뜨는지 + 진짜 에셋 드롭인 교체 1회 검증.

---

## 📚 학습 포인트

- **swap-ready = 코드/에셋 분리** — placeholder도 진짜 에셋과 *동일 컴포넌트 구조*(Image+Animator+controller 슬롯)로 배선하면 교체 시 reference만 바꾸면 됨. 야간에 코드를 완성하고 아침에 아트만 꽂는 워크플로의 핵심 (헌법 외부화 정신 정합).
- **애니 슬롯 선배선** — placeholder가 정적 sprite여도 Animator 컴포넌트와 controller 슬롯을 미리 부착하면 진짜 controller 드롭 = 즉시 애니 (코드 무변경).

---

## ⚠️ 함정 / 주의사항

- **StageClear 애니 스프라이트가 아직 없음** → placeholder + swap-ready 의무. Image+Animator 슬롯을 미리 부착해야 진짜 controller가 바로 꽂힌다. 정적 placeholder도 동일 컴포넌트 구조로 배선.
- TMP를 *지우기 전에* Image 경로가 동작하는지 확인 — 둘 다 죽으면 StageClear 무연출 회귀.
- 씬/prefab 편집 백업 의무 (Phase 08 학습).

---

## ➡️ 다음 Phase

- Phase 22 — 봇 PartyQuestSmoke 시나리오 (R 트랙 회귀 시작).

---

## 📋 박제 (완료 후 -DONE.md)

- 단순 등급 → work-pin + commit message만. (단 swap 지점은 완료 노트에 명시 — 아침 교체용.)

---

## 작업 로그

- 2026-06-14: 생성.
