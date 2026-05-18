# Phase 08a: Asset 사전 작업 + Prefab variant + LocalPlayer 추출 검증 + RemotePlayer 비주얼 교체

> **상태**: pending (정유현 작업, 의존성 0 = 본인 Phase 05와 *완전 병렬*)
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 1.5h
> **담당 에이전트**: client
> **담당 사람**: 정유현 (영역: `03_Client/Assets/Prefabs/Characters/`, Sprites Asset import, `Scripts/Rendering/`)

---

## 🎯 목표

캐릭터 비주얼 Asset 정착 + Prefab 구조 정착 + 영호 Phase 05의 RemotePlayer.prefab placeholder를 진짜 캐릭터로 교체. *본인 Phase 05/06/07과 완전 병렬* — 영호 작업 막혀도 진행 가능.

**끝나면 데모 가능한 것**: 멀티 접속 시 모든 캐릭터가 *진짜 그림*으로 표시 (Local + Remote 둘 다).

---

## ⏪ 사전 조건

- [ ] 본인 Phase 05 *일부 commit + 빠른 main 머지* 완료: `RemotePlayer.prefab` placeholder + `RemoteEntity.cs` 컴포넌트 박힘 → main pull로 받기
- [ ] LocalPlayer.prefab 이미 박혀있음 (`03_Client/Assets/Prefabs/Characters/LocalPlayer.prefab`, 영호 5/18 추출)

---

## 📝 작업 내용

### 1. 캐릭터 스프라이트 + Animator 정착
- [ ] 캐릭터 스프라이트 import (Idle + Run 2 동작 — M2 Walking_KG_1.png 등 재활용 가능)
- [ ] Animator clip 정착 (Idle + Run, M2 Phase 03 Transition 정합)
- [ ] (필요시) Sprite Editor Grid 100×64 hard reset 정합 (M2 Phase 04 패턴)

### 2. LocalPlayer.prefab 추출 검증 (영호가 5/18 추출함 — 유현 1차 검증)
- [ ] Unity Editor Play 모드 → M2 회귀 시나리오 통과 (혼자 접속 + WASD 이동 + Idle/Run 애니메이션 + flipX + CameraFollow)
- [ ] 컴포넌트 6개 보존 확인 (Transform/SpriteRenderer/PlayerInput/LocalPlayerController/Animator/PlayerAnimatorSync)
- [ ] Inspector에 missing reference 0건

### 3. PlayerBase.prefab + variant 패턴 도입 (5/18 합의 = 도입)
- [ ] `PlayerBase.prefab` 신설 (Transform + SpriteRenderer + Animator만 — *비주얼 공통*)
- [ ] `LocalPlayer.prefab` → PlayerBase variant + Local 전용 컴포넌트 3개(PlayerInput/LocalPlayerController/PlayerAnimatorSync)
- [ ] `RemotePlayer.prefab` → PlayerBase variant + RemoteEntity 컴포넌트
- [ ] 비주얼 갱신 시 PlayerBase 1회 수정 → Local/Remote 둘 다 자동 반영 검증

### 4. RemotePlayer.prefab 비주얼 교체
- [ ] 영호가 박은 회색 박스 placeholder → 진짜 캐릭터 스프라이트로 교체
- [ ] **★ 영호 영역 `RemoteEntity` 컴포넌트는 절대 건드리지 X** (5/18 합의 핵심)
- [ ] entityId 라벨 = 디버그용 (응급 모드 유지, 면담 후 제거)

---

## ✅ 완료 조건

- [ ] LocalPlayer.prefab M2 회귀 통과 (혼자 모드 정상)
- [ ] RemotePlayer.prefab 진짜 캐릭터로 보임 + RemoteEntity 컴포넌트 보존
- [ ] PlayerBase.prefab variant 패턴 박힘 (Local/Remote 둘 다 base 공유)
- [ ] 헤드리스 봇 2명 + 본인 Unity 클라 = 모두 진짜 캐릭터 표시

---

## 🧪 테스트

**수동**: M2 회귀(혼자 모드) + 멀티 시나리오(헤드리스 봇 2명 + 본인 = 진짜 캐릭터 3마리)
**자동**: 없음 (Asset 검증은 시각 수동)

---

## 📚 학습 포인트

- **Prefab Variant 패턴** — Unity의 상속 메커니즘. base 1회 수정 → 모든 variant 자동 반영. 비주얼 일관성 + DRY 원칙
- **Asset import 컨벤션** — Sprite Mode, Pixel Per Unit, Pivot, Grid Slice (M2 Phase 04 hard reset 패턴 재사용)
- **컴포넌트 약속 = trust boundary** — `RemoteEntity` 컴포넌트는 *영호 영역 시그니처*. 유현이 비주얼 작업 시 *건드리지 않음*이 영역 분리 약속
- **분담 검증** — 추출/교체 후 *기존 기능 회귀 X* 확인 = 신뢰의 핵심

---

## ⚠️ 함정 / 주의사항

- **`RemoteEntity` 컴포넌트 실수 삭제** → 영호 Phase 05 깨짐 (보간/spawn 동작 X). prefab 작업 시 *Inspector 잠금* 후 진행 권장
- **Prefab Variant 순환 참조** — base가 variant 참조하면 깨짐. PlayerBase가 자식 prefab을 참조하지 않도록
- **Sprite Editor 잔재 GUID** — M2 Phase 04 사고 재현 가능. Single 모드 flush 트릭 (Phase 04 DONE 참조)
- **컴포넌트 wiring 깨짐** — 추출 직후 *Inspector에서 missing reference 0건* 확인

---

## ➡️ 다음 Phase

- **Phase 08b** — 3-zone 배경 + StageClear UI + HP 바 (영호 Phase 07 후속)

---

## 작업 로그

- 2026-05-18: 정의 신설 (5/18 분담 합의 결과)
