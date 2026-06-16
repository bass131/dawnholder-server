---
owner: youngho
milestone: M7
phase: 01
title: 사운드 인벤토리 + 분류 체계 + 폴더 구조
status: done
grade: 보통
estimated: 1~3h
domain: client
summary: 게임 이벤트별 필요 사운드 목록화 + Sound 폴더 분류(BGM/SFX/UI/Ambient) 확정
---

# Phase 01: 사운드 인벤토리 + 분류 체계 + 폴더 구조

> **상태**: ✅ 실측 완료 (2026-06-16) — 코드 트리거 전수조사 + 키 카탈로그 + 폴더 분류(Resources/Audio: BGM/SFX/UI) 확정. 산출물 = 세션 플랜파일 §2 키표. die 매핑 Normal→Slime/Golem→Golem/Boss(뱀파이어)→Vampire, 진짜 잉여=Frog/Mushroom.
> **마일스톤**: M7
> **등급**: 보통
> **담당**: client + 영호 의논 (분류 체계 결정)

---

## 🎯 목표

"어떤 사운드가 어떤 게임 이벤트에 필요한가"를 빠짐없이 목록화하고,
`Assets/.../Sound/` 폴더 분류 체계(BGM/SFX/UI/Ambient + 하위)를 확정한다.
이후 모든 Phase의 범위 기준이 되는 단일 인벤토리.

---

## ⏪ 사전 조건

- [ ] M6 완료 (폴리시 머지)
- [ ] **분류 체계 결정 (영호)**: 4분류(BGM/SFX/UI/Ambient)로 시작 + 하위 세분 어디까지
- [ ] **기존 14개 wav 사전 인벤토리화**: `03_Client/Assets/Sound/` 루트에 이미 flat 배치된 14개(`AttackWarriorAB`, `FrogDie`, `GolemDie`, `JumpLand`, `JumpStart`, `Magic spell cast...`, `MonsterAttackAB`, `MonsterHitAB`, `MushroomDie`, `PlayerHitAB`, `SlimeDie`, `StageClear`, `VampireDie` 등)를 인벤토리에 우선 매핑. 잉여는 deprecated 표시.

---

## 📝 작업 내용

- [ ] 게임 이벤트 전수 조사 → 사운드 후보 도출:
  - **전투 SFX**: 근접 공격, 원거리/스킬 시전, 피격(플레이어/적), 사망, 보스 등장/패턴
  - **이동 SFX**: 점프, 착지, 대시, 발소리(옵션)
  - **UI SFX**: 버튼 클릭/호버, 패널 열기/닫기, 퀘스트 완료, 스테이지 클리어, 파티 초대/수락
  - **BGM**: 마을, 일반 전투, 보스전, 메인메뉴
  - **Ambient**: 마을 환경음(옵션)
- [ ] 각 항목에 우선순위(필수/선택) 표기 — scope 가드
- [ ] Sound 폴더 구조 확정 (예: `Sound/BGM`, `Sound/SFX/Combat`, `Sound/SFX/Movement`, `Sound/UI`, `Sound/Ambient`)
- [ ] 사운드 키 네이밍 규칙 (예: `sfx.combat.melee_hit`, `bgm.town`) — Phase 02 인프라가 참조
- [ ] 인벤토리를 문서로 박제 (이 Phase의 산출물 = 표)

---

## ✅ 완료 조건

- [ ] 이벤트별 필요 사운드 목록 + 우선순위 표 완성
- [ ] Sound 폴더 분류 + 네이밍 규칙 영호 확정
- [ ] Phase 02(인프라)·03(에셋)이 이 인벤토리를 참조할 수 있음

---

## 🧪 테스트

- 산출물은 문서 — 코드 테스트 없음. 영호 리뷰 게이트.

---

## 📚 학습 포인트

- 0→1 사운드 작업에서 인벤토리를 먼저 박는 이유 — "전면 적용"의 무한 확장을 범위로 가둠.
- 사운드 키 네이밍이 코드/에셋을 느슨하게 연결(키만 알면 됨)하는 설계.

---

## ⚠️ 함정 / 주의사항

- "전면"을 글자 그대로 받으면 끝이 없음 — 필수/선택 우선순위로 1차 범위를 명확히.
- 분류를 너무 잘게 쪼개면 폴더 관리 비용 ↑ — 4분류 + 1단계 하위 정도가 보통 적정.

---

## ➡️ 다음 Phase

- Phase 02 — 오디오 재생 인프라 (AudioManager)
- Phase 03 — 사운드 에셋 생성 (병렬 가능)
