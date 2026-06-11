---
owner: youngho
milestone: M4.9
phase: 07
title: 스킬 슬롯 쿨다운 UI + 전체 회귀 + 발표용 재빌드 + 마감 박제
status: pending
grade: 복잡
risk: (전체 회귀 + 발표용 비가역 빌드)
estimated: 2.5h
domain: client+qa
---

# Phase 07: 스킬 슬롯 쿨다운 UI + 전체 회귀/마감

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 복잡 (UI 신설 + 전체 회귀 + 발표용 재빌드)
> **담당**: client Worker(Sonnet) + qa
> **의존**: Phase 01~06 **전부** (모든 스킬이 박혀야 슬롯 매핑 확정)

---

## 🎯 목표

스킬 HUD를 신설해 **클래스별 보유 스킬 아이콘 + 쿨다운 fill**을 표시하고, M4.9 전체를 회귀 검증한 뒤 발표용 클라를 재빌드하고 마일스톤을 마감한다. 이 Phase가 끝나면 발표 영상에 쓸 4스킬(Thunderbolt/Teleport/Dash + 평타) 데모 루프가 쿨다운 UI와 함께 완성되고, `C:\Dev\Build` 클라가 최신(PR #96 + M4.9 포함)으로 갱신된다.

---

## ⏪ 사전 조건

- [ ] Phase 01~06 전부 완료 — 4스킬 + 비주얼 + 클래스 게이트 + Dash/Teleport 동작
- [ ] UI.unity에 `skill_slot_1~5` 슬롯 오브젝트 존재 확인 (없으면 영호와 슬롯 레이아웃 확정)
- [ ] 스킬별 쿨다운 값(Thunderbolt/Dash/Teleport) = 98_Shared 단일 진실에서 클라가 거울로 읽을 수 있음

---

## 📝 작업 내용

**client (UI)**:
- [ ] `SkillHudController` 신설 (`03_Client/Assets/Scripts/UI/`) — 한 개념 한 MonoBehaviour
- [ ] UI.unity `skill_slot_1~5` 슬롯 연결 — 클래스별 보유 스킬 아이콘 표시 (Mage: Thunderbolt/Teleport, Knight: Dash 등)
- [ ] 쿨다운 fill — 스킬 사용 시 fill amount로 남은 쿨다운 시각화 (98_Shared 쿨다운 상수 거울, 서버 권위 시간 기준)
- [ ] 클래스별 슬롯 아이콘 매핑 (SkillCatalog 정합 — Knight는 Dash 슬롯만, Mage는 Thunderbolt/Teleport 슬롯)

**qa (전체 회귀)**:
- [ ] `dotnet test` **전체** green (M4.8 회귀 + M4.9 신규)
- [ ] 봇 **전 시나리오** — 기존(RangedHitSmoke/ThunderboltAoeSmoke/FreezeSmoke 등) + 신규(DashSmoke/TeleportSmoke) 전부 PASS
- [ ] Unity 콘솔 0에러
- [ ] 2클라 풀 데모 루프 — 4스킬 전부 시전 + 이펙트 + 쿨다운 UI + 클래스 게이트 확인

**발표용 재빌드 (백로그 회수)**:
- [ ] `C:\Dev\Build` 클라 재빌드 — **PR #96(보스 facing fix) + M4.9 전부 포함** (현재 6/10 11:50 빌드는 PR #96 미포함 — work-pin 백로그)
- [ ] Managed DLL(`Dawnholder.Client.dll`) mtime으로 빌드 신선도 확인

**마감**:
- [ ] `_milestone-DONE.md` + `.html` 5단계 보고(대규모 마일스톤) 박제
- [ ] CHANGELOG[M] 추가

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` 전체 green — 회귀 **0**
- [ ] 봇 전 시나리오 PASS (기존 + DashSmoke + TeleportSmoke)
- [ ] Unity 콘솔 error CS **0**
- [ ] 스킬 HUD — 클래스별 보유 스킬 아이콘 표시 + 시전 시 쿨다운 fill 갱신
- [ ] **2클라 풀 데모** — Mage(Thunderbolt+Teleport+평타) / Knight(Dash) 4종 전부 이펙트+쿨다운 UI 동작, 클래스 게이트(서로 스킬 불가) 확인
- [ ] `C:\Dev\Build` 클라 = PR #96 + M4.9 포함 재빌드 완료 (DLL mtime 신선)
- [ ] **재빌드 클라로 발표 시나리오 dry-run 1회** — 보스 facing(PR #96) 육안 확인 + 4스킬 전부 1회 시전 (DLL mtime 신선도 ≠ 내용 정합)
- [ ] `_milestone-DONE.md` + `.html` 5단계 보고 박제 + CHANGELOG[M]

---

## 🧪 테스트

**자동**:
- 전체 `dotnet test` (스킬 게이트 + Dash + Teleport + 기존 전투/보스)
- 봇 전 시나리오 회귀

**수동**:
- 2클라 발표 데모 리허설 — 4스킬 시전 루프 + 쿨다운 UI 관찰 + 클래스 게이트 시연

---

## 📚 학습 포인트

- **쿨다운 UI = 서버 권위 시간의 클라 거울**: fill amount는 클라가 "스킬 쓴 시점 + 쿨다운 상수"로 계산하지만, 진짜 쿨다운 판정은 서버다(헌법 §1). UI는 어디까지나 시각 힌트 — 클라 fill이 0이어도 서버가 아직 쿨다운이면 silent drop된다. UI와 서버 게이트가 같은 98_Shared 상수를 거울로 써야 어긋나지 않는다.
- **회귀 테스트의 가치**: 신규 스킬 3개를 넣으면서 기존 썬더볼트/평타/보스 로직이 안 깨졌는지 확인하는 게 회귀. "새 기능 동작"만 보고 "옛 기능 보존"을 안 보면 발표 직전에 옛 버그가 튀어나온다.
- **발표용 빌드 = 증분 빌드 함정**: Unity 증분 빌드는 exe가 안 바뀌어도 Managed DLL은 바뀐다 — `Dawnholder.Client.dll` mtime이 신선도 기준(work-pin carry-over). "빌드했는데 옛 코드"를 mtime으로 잡는다.

---

## ⚠️ 함정 / 주의사항

- **발표용 재빌드 = 백로그 회수 의무**: work-pin에 "현 C:\Dev\Build 클라에 PR #96 미포함"이 박혀 있다 — 이 Phase에서 반드시 PR #96 + M4.9 둘 다 포함해 재빌드. 빼먹으면 발표 영상에 옛 보스 facing 버그가 찍힌다.
- 빌드 전제 = Unity 에디터 컴파일 에러 0 (패키지 하나만 깨져도 빌드 거부 — HEAD manifest broken 백로그 주의).
- 쿨다운 UI를 SkillCatalog와 따로 박으면 클래스별 슬롯이 어긋난다 — Phase 02 매핑 재사용.

---

## ➡️ 다음 Phase

- M4.9 마감. 발표(6/17) 후 후속(마나/MP, 추가 스킬, 리바인딩 UI 등)은 별 마일스톤.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 + 마일스톤 마감 → `_milestone-DONE.md` + `.html` 5단계 보고(대규모 마일스톤 캡스톤 자산) + CHANGELOG[M].

---

## 작업 로그

- 2026-06-10: 계획 작성
