---
owner: youngho
milestone: M4.3
phase: 11
title: RemotePlayer 외관 봉합 — Animator 미작동 + prefab 3개 정합
status: pending
grade: 보통
risk: unity-asset
estimated: 1~2h
domain: client
---

# Phase 11: RemotePlayer 외관 봉합

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 보통 (client 단일 / prefab unity-asset 깃발)
> **담당**: 본인 (외관 분담) + client SubAgent (코드 wiring 보조)

---

## 🎯 목표

멀티플레이 시 **다른 플레이어(RemotePlayer)의 Animator가 안 돌아가서** 가만히 미끄러지는 어색함을 봉합하고, **흩어진 RemotePlayer prefab 3개를 1개로 정합**한다. 발표에서 2인 플레이를 보여줄 때 상대 캐릭터가 자연스럽게 걷/뛰어야 한다.

---

## ⏪ 사전 조건

- [x] M3 Phase 05 remote entity registry (RemotePlayer mirror 인프라)
- [ ] 07~10과 독립 — **Phase 10과 병렬 가능**

---

## 📝 작업 내용

### prefab 정합 (cleanup 의무 — work-pin)
- [ ] 현존 3개 점검: `RemotePlayer.prefab` / `RemotePlayer.backup.prefab` / `Resources/RemotePlayer.prefab`
  - 어느 것이 실제 런타임에 로드되는지 추적 (Resources.Load 경로 확인)
  - 정본 1개로 통일, 나머지 제거 (백업은 git 이력에 있으니 작업 디렉토리에서 정리)
- [ ] **prefab 작업 전 백업 의무** (Phase 08 BackGround prefab 사고 학습)

### Animator 봉합 (본인 외관 분담)
- [ ] RemotePlayer Animator가 미작동하는 원인 추적 — Animator Controller 누락? 파라미터 미연결? mirror가 위치만 갱신하고 애니 트리거 안 함?
- [ ] RemotePlayer 위치 델타(또는 서버 state)로 Idle/Walk 애니 전환 wiring — 코드 부분은 client SubAgent 보조 가능, Animator 클립/전이 셋업은 본인
- [ ] 이동 방향 좌우 flip 정상

### 테스트
- [ ] Play 2인(클라+봇 또는 클라 2대) — 상대 캐릭터가 움직일 때 walk 애니 + flip 정상

---

## ✅ 완료 조건

- [ ] RemotePlayer prefab이 **1개로 정합** (Resources 로드 경로 단일, 중복/backup 제거)
- [ ] 멀티 시 상대 플레이어 Animator 정상 (이동 시 walk, 정지 시 idle, 좌우 flip)
- [ ] Play 회귀 0 (기존 RemotePlayer 위치 보간 그대로)
- [ ] prefab 변경 백업 완료 (unity-asset 안전)

---

## 🧪 테스트

**수동**:
- Play 2인 — 한쪽 이동 시 다른 쪽 화면에서 상대가 걷/뛰는지, 방향 flip 되는지
- prefab 정합 후 enemy/Player 외관 회귀 없는지 확인

---

## 📚 학습 포인트

- **prefab 단일 진실**: 같은 것 3개가 흩어지면 어느 게 진짜인지 모름 → drift. 정본 1개 + 명확한 로드 경로.
- **mirror 객체의 표현 레이어**: RemotePlayer는 서버 위치를 받아 그리는 mirror. 위치 갱신(데이터)과 애니(표현)는 별도 — 위치만 옮기고 애니 트리거를 빼먹으면 미끄러짐.

---

## ⚠️ 함정 / 주의사항

- **unity-asset 위험 깃발** (Phase 08 사고): prefab 편집/삭제 전 백업. scene/prefab YAML 직접 손편집은 위험 — Unity Editor 또는 메인 세션 MCP 경유.
- **Animator/Sprite = 본인 분담** (memory `unity-visual-work-user-owned`): 클립/전이/컨트롤러 셋업은 본인 직접. AI는 "어디서 애니 트리거를 호출해야 하는지" 코드 위치만 보조.
- **Resources.Load 경로 의존**: prefab 정리 시 로드 경로 깨지면 RemotePlayer가 아예 안 뜸 — 정합 후 Play 필수 확인.

---

## ➡️ 다음 Phase

- Phase 12 — M4.3 회귀 테스트 + 가벼운 마감

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit message만. -DONE.md 박지 않음. (단 prefab 정합 결정은 commit message에 명확히)

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
