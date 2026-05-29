---
owner: youngho
milestone: M4.3R
phase: 06
title: 클라 네이밍 정합 (isPaused + SerializeField 규칙)
status: done
grade: 보통
domain: client
estimated: 1h
---

# Phase 06: 클라 네이밍 정합 (rank 6 + rank 8)

> **상태**: pending
> **마일스톤**: M4.3R
> **등급**: 보통 (옵션 A 확정 — SerializeField 8파일 `_camelCase` rename)
> **담당**: client SubAgent

---

## 🎯 목표

Phase 01에서 결정한 SerializeField 규칙에 따라 클라 private field 네이밍(§3.3)을 정합한다. `PauseMenuController.isPaused`처럼 정당화 여지 없는 명백 위반은 무조건 봉합하고, SerializeField는 정책 결과대로 처리한다.

---

## ⏪ 사전 조건

- [ ] **Phase 01 완료 필수** — SerializeField 규칙 **옵션 A 확정**(`[SerializeField]`도 `_camelCase`) + §3.3 명문화 완료 (2026-05-29)

---

## 📝 작업 내용

### rank 6 — 무조건 봉합 (단순)
- [ ] `PauseMenuController.isPaused`(L36) → `_isPaused` (사용처 L64/67/70/75/83 동반 수정). SerializeField 아닌 순수 private = 명백 §3.3 위반

### rank 8 — SerializeField `_camelCase` 통일 (옵션 A 확정)
- [ ] `[SerializeField]` 필드 `_camelCase` rename — NetworkService(serverHost→`_serverHost`/serverPort→`_serverPort`/pingIntervalSeconds→`_pingIntervalSeconds`) + 동반 7파일(CameraFollow/HudController/NpcDialogPanel/NpcInteractable/PortalTrigger/MainMenuController/SceneBootstrap)
- [ ] **각 rename에 `[FormerlySerializedAs("old")]` 부착** — Inspector/prefab/scene 직렬화 값 보존 (없으면 값 리셋)
- [ ] **본인 scene/prefab 직렬화 영향 조율** (memory `unity-visual-work-user-owned` — rename은 본인 외관 도메인 인접)

---

## ✅ 완료 조건

- [ ] `isPaused` → `_isPaused` (Unity 컴파일 green)
- [ ] SerializeField 8파일 `_camelCase` rename + `[FormerlySerializedAs]` 부착 완료 (Inspector 값 보존 확인)
- [ ] §3.3 prefix 위반 0 (클라 private field — grep 확인)
- [ ] 동작 보존: Play로 일시정지 메뉴 + Inspector 직렬화 값 정상

---

## 🧪 테스트

**자동**: Unity 컴파일.
**수동**: Pause 메뉴 토글 + 옵션 A면 Inspector에서 SerializeField 값 보존 확인(rename은 직렬화 이름 변경 = Inspector 값 리셋 위험 → FormerlySerializedAs 또는 값 재설정 확인).

---

## 📚 학습 포인트

- **`[SerializeField]` rename 함정**: Unity는 필드 *이름*으로 직렬화 → rename 시 Inspector 값이 리셋될 수 있음. `[FormerlySerializedAs("oldName")]`로 마이그레이션. (옵션 A 선택 시 핵심 함정)
- **네이밍 일관성의 가치(§3.3)**: 혼용(`_x`/`x`)은 "이 필드는 왜 다르지?" 인지 부담. 규칙 하나로 통일 = 읽을 때 생각 안 해도 됨.

---

## ⚠️ 함정 / 주의사항

- **🔴 SerializeField rename = Inspector 값 리셋 위험** (옵션 A) — `[FormerlySerializedAs]` 없이 rename하면 직렬화 끊겨 prefab/scene 값 날아감. 본인 분담(외관/scene) 인접 → rename 전 영향 확인 + 본인과 조율.
- **옵션 B면 이 Phase 거의 무작업** — isPaused 1건. 정책 결정이 코드 양을 좌우.

---

## ➡️ 다음 Phase

- Phase 07 (네트워크 prefix) — 마지막

---

## 📋 박제 (완료 후)

- **보통/단순 등급** — work-pin + commit message만.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
