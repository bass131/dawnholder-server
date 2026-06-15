---
owner: youngho
milestone: M7
phase: 03
title: 사운드 에셋 생성 (Unity AI Generator) + import + 분류 배치
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~5h
domain: client
summary: AI Generator로 사운드 생성 + Sound 폴더 분류 배치 + import 설정(압축/로드 타입)
---

# Phase 03: 사운드 에셋 생성 (Unity AI Generator) + import + 분류 배치

> **상태**: pending
> **마일스톤**: M7
> **등급**: 복잡 (unity-asset — 에셋 생성/import)
> **담당**: 영호 주도 (AI Generator/청음) + client 보조 (import 설정/배치)

---

## 🎯 목표

Phase 01 인벤토리의 필수 항목에 대해 **(a) 기존 14개 고아 wav를 분류 마이그레이션하고 (b) 부족분만 신규 생성**한다.
Sound 폴더에 분류 배치 + import 설정(압축/로드 타입/모노)을 완료한다.

> **실측 (Opus plan-auditor 2026-06-15)**: `03_Client/Assets/Sound/` 루트에 **14개 wav가 이미 flat 배치**돼 있음(`AttackWarriorAB.wav`, `FrogDie.wav`, `GolemDie.wav`, `JumpLand.wav`, `JumpStart.wav`, `Magic spell cast, sparkly rising shimmer...wav`, `MonsterAttackAB.wav`, `MonsterHitAB.wav`, `MushroomDie.wav`, `PlayerHitAB.wav`, `SlimeDie.wav`, `StageClear.wav`, `VampireDie.wav` 등). 코드/prefab 어디서도 참조 0건 = 영호가 미리 받아둔 미사용 자산. 따라서 마일스톤 프레이밍 "0→1"은 *코드 인프라 측에서만 참* — 에셋 측은 *14개 고아 + 부족분 보충*이 정합.

---

## ⏪ 사전 조건

- [ ] Phase 01 — 인벤토리 + 폴더 구조 + 키 네이밍 확정 (**기존 14개 wav를 인벤토리 매핑에 사전 포함**)
- [ ] **분담 확인 (영호)**: AI Generator 사운드 생성/선정/청음은 영호, import 설정·배치 코드는 AI 보조

---

## 📝 작업 내용

**3-step 마이그레이션 + 보충**:

- [ ] **(a) 기존 14개 매핑**: 14개 wav 각각을 Phase 01 인벤토리 항목과 매칭. 매칭 안 되는 잉여(예: `MushroomDie`, `FrogDie`처럼 현재 적 종류에 없음)는 *deprecated 또는 미래 사용 보류* 박제.
- [ ] **(b) 분류 폴더로 이동** (`Sound/SFX/Combat`, `Sound/BGM` 등): **`.meta guid 보존` 의무** (자산 이동 시 git mv 또는 Unity 에디터 Move — Bash mv는 guid 유지되지만 reference link는 OK). meta 함께 이동 확인.
- [ ] **(c) 키 네이밍 규칙대로 rename**: 특히 서술형 파일명(`Magic spell cast, sparkly rising shimmer...wav` 같은 것)을 키 규칙(`sfx.skill.magic_cast.wav`)으로 정리. unity-asset 위험 — guid 보존 의무 (rename은 guid 유지).
- [ ] **(d) 부족분만 신규 생성** (Unity AI Generator — 영호): Phase 01 인벤토리에서 *기존 14개로 채워지지 않은* 항목만 생성. scope 가드.
- [ ] import 설정: 짧은 SFX = Decompress on Load + 적정 압축, BGM = Streaming, 모노/스테레오 결정
- [ ] 키 → AudioClip 매핑 테이블 채우기 (Phase 02 인프라 연결)
- [ ] 라이선스/출처 확인 (AI 생성물 + 기존 14개 둘 다 — 사용 가능 여부 영호 확인)

---

## ✅ 완료 조건

- [ ] 필수 인벤토리 항목의 에셋이 Sound 폴더에 분류 배치됨
- [ ] import 설정이 종류별로 적절 (SFX/BGM)
- [ ] 매핑 테이블로 Phase 02 인프라가 키로 로드 가능
- [ ] 영호 청음 OK (품질 게이트)

---

## 🧪 테스트

**수동 (영호 청음)**: 생성 사운드 품질/적합성 확인
**기술**: Phase 02 인프라로 각 키 재생 시 정상 로드/재생

---

## 📚 학습 포인트

- import 로드 타입 trade-off: Decompress on Load(빠른 재생/메모리 ↑) vs Streaming(메모리 ↓/디스크 I/O) vs Compressed in Memory.
- 짧은 효과음은 모노로 충분(메모리 절반), BGM은 스테레오.

---

## ⚠️ 함정 / 주의사항

- 에셋 대량 import = unity-asset 위험 — 폴더 단위로 나눠 커밋(meta guid 보존, 백업).
- AI 생성 사운드 라이선스/사용권 영호 확인 필수.
- 파일명-키 불일치 시 런타임 로드 실패 — 네이밍 규칙 엄수.

---

## ➡️ 다음 Phase

- Phase 04 — SFX wiring
