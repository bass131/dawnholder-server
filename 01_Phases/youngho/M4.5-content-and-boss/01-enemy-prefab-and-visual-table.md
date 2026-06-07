---
owner: youngho
milestone: M4.5
phase: 01
title: 몬스터 prefab 전환 — EnemyVisualTable SO + 외관 디테일
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~3h
domain: client
---

# Phase 01: 몬스터 prefab 전환 — EnemyVisualTable SO

> **상태**: pending
> **마일스톤**: M4.5
> **등급**: 복잡 (보통 + unity-asset 상향 — prefab 신설)
> **담당**: client SubAgent + unity-bridge (prefab/SO 에셋) + 메인 검수

---

## 🎯 목표

적 시각 표현을 `EnemyViewFactory.BuildPlaceholder`(149줄 런타임 GameObject 조립)에서 **prefab + `EnemyVisualTable` SO lookup**으로 전환한다. 끝나면 새 적 종류 추가 = "prefab 1개 + 테이블 1행"이 되고, 클라 코드에 적 종류 분기(if/switch)가 0이 된다. M4.4-05 `ClassConfig` 패턴의 적 버전.

**비주얼 교체 동반 (2026-06-07 사용자 결정)**: 아트 스타일 불일치로 외부 에셋 placeholder 은퇴 — Normal = Mushroom → **Slime**(본인 제작, `Art/Enemy/Slime/Animator/Slime.controller`), Boss = ToxicFrog → **Boss_Vampire**(`Art/Enemy/Boss_Vampire/Animator/Boss_Animator.controller`). 일반 몬스터 라인업 = 슬라임 + 골렘(Phase 02)으로 통일.

---

## ⏪ 사전 조건

- [x] M4.4 마감 (PR #67) — ClassConfig SO lookup 패턴 선례
- [x] M4.3-08b `AnimatorDriver` + `EnemyMotion` 계약 (prefab에 동일 컴포넌트 구성)

---

## 📝 작업 내용

- [ ] `EnemyVisualTable` SO 신설 (`Resources/` lookup — ClassConfig 배치 관례 정합): EnemyKind → prefab 매핑 + 폴백 정책(미등록 kind = 명시 로그 + Normal prefab 폴백, silent 빈 화면 금지)
- [ ] 적 prefab 2종 제작 (unity-bridge): `Enemy_Normal`(**Slime** — M4.3-11 본인 제작 클립/controller 연결), `Enemy_Boss`(**Boss_Vampire** — `Boss_Animator.controller`) — 현 BuildPlaceholder 구성(RemoteEnemy/RemoteEntity/AnimatorDriver/EnemyMotion/HpBar 자식) prefab으로 이전. visualFootOffset은 새 스프라이트 기준 재측정(옛 Boss -1.0f는 ToxicFrog 기준 — 그대로 복사 금지)
- [ ] Mushroom(Forest_Monsters_FREE)/ToxicFrog 에셋 은퇴 — 참조 0 확인 후 폴더 삭제는 별도 정리 커밋(guid 참조 잔존 시 Missing 사고 — 보류 시 사유 박음)
- [ ] `EnemyViewFactory` 재작성: BuildPlaceholder 은퇴 → 테이블 lookup + Instantiate (entityId/좌표 주입만)
- [ ] 몬스터/보스 **외관 디테일** (M4.4-06 Play 실측 이월): 스프라이트 크기/피벗/HP바 위치 정돈 — 본인 외관 확인 후 마감
- [ ] EditMode 테스트: 테이블 lookup(등록 kind/미등록 폴백), prefab 필수 컴포넌트 존재 단언

---

## ✅ 완료 조건

- [ ] `EnemyViewFactory`에 `new GameObject(...)` 런타임 조립 코드 0줄 (Instantiate 호출만)
- [ ] 클라 적 시각 코드에 EnemyKind if/switch 분기 0 (테이블 lookup 단일 경로)
- [ ] 미등록 kind 폴백 동작 EditMode 테스트 green
- [ ] Play 실측: HG/BR에서 Slime/Boss_Vampire가 prefab 기반으로 렌더 + HP바/사망 연출 회귀 0 (아트 스타일 통일 본인 확인)
- [ ] `dotnet test` + EditMode 전부 green (회귀 0)

---

## 🧪 테스트

**자동**: EditMode — 테이블 lookup 2분기 + prefab 컴포넌트 구성 단언
**수동**: Play — HG 슬라임/BR 보스 렌더·HP바·사망 연출 + 본인 외관 디테일 확인

---

## 📚 학습 포인트

- **데이터 주도 시각 장착** — 코드 분기(switch) vs 데이터 테이블(SO)의 trade-off: 분기는 추가마다 코드 수정+재컴파일, 테이블은 에셋 1행. 직업(ClassConfig)과 적(EnemyVisualTable)에 같은 패턴이 반복되는 걸 체감
- **prefab = 구성의 스냅샷** — 런타임 조립은 코드가 구성의 진실, prefab은 에셋이 진실. 디자이너(유현)가 코드 없이 만질 수 있는 경계가 생김

---

## ⚠️ 함정 / 주의사항

- **prefab 백업 의무** (Phase 08 BackGround 사고 학습) — prefab 작업 전 `.claude/state/scene-backups/` 백업
- **폴백은 fail-loud** — 미등록 kind에 조용한 빈 GameObject 금지. 로그 + Normal 폴백 (M4.4-03 fail-closed 정신)
- **visualFootOffset 누락 주의** — Boss -1.0f가 prefab 이전 중 증발하면 보스가 공중에 뜸 (Play로 확인)
- Resources lookup 경로 오타 = 런타임 null — ClassConfig 때 패턴 그대로 복제

---

## ➡️ 다음 Phase

- Phase 02 — 골렘 추가 (본 테이블에 1행 추가하는 첫 실전)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `01-enemy-prefab-and-visual-table-DONE.md` 박음

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18)
- 2026-06-07: 비주얼 교체 결정 반영 — Mushroom/ToxicFrog 은퇴(아트 스타일 불일치, 사용자 결정), Slime/Boss_Vampire 채택. 일반 몬스터 = 슬라임+골렘 통일
