---
owner: youngho
milestone: M7
phase: 02
title: 오디오 재생 인프라 (AudioManager)
status: pending
grade: 복잡
estimated: 2~4h
domain: client
summary: 키 기반 사운드 재생/정지/볼륨 제어 인프라 — AudioSource 풀 + BGM 채널 + 볼륨 그룹
---

# Phase 02: 오디오 재생 인프라 (AudioManager)

> **상태**: pending
> **마일스톤**: M7
> **등급**: 복잡 (클라 신규 시스템)
> **담당**: client

---

## 🎯 목표

코드 어디서나 `AudioManager.PlaySfx("sfx.combat.melee_hit")` 식으로 키만으로 사운드를 재생/정지하고,
볼륨(마스터/BGM/SFX)을 제어할 수 있는 인프라를 만든다. 실제 에셋이 없어도 placeholder로 검증 가능.

---

## ⏪ 사전 조건

- [ ] Phase 01 — 분류 체계 + 키 네이밍 규칙 확정 (인프라 API가 키 참조)

---

## 📝 작업 내용

- [ ] AudioManager (싱글톤 또는 BuildRuntime — 기존 HUD 패턴과 정합) 설계
- [ ] SFX용 AudioSource 풀 (다중 동시 재생, GameObject 재사용)
- [ ] BGM 채널 (단일/이중) + 크로스페이드 전환
- [ ] 볼륨 그룹: 마스터 / BGM / SFX (AudioMixer 또는 코드 볼륨) — 설정 저장(PlayerPrefs)
- [ ] 키 → AudioClip 매핑 로드 (Resources 또는 ScriptableObject 테이블)
- [ ] placeholder 사운드(또는 무음 클립)로 재생 경로 검증
- [ ] 게임플레이 타이밍에 영향 없음 확인 — 사운드는 표현 전용, 서버 tick 무관 (헌법 #1)

---

## ✅ 완료 조건

- [ ] `PlaySfx(key)` / `PlayBgm(key)` / `StopBgm()` / 볼륨 set이 동작 (placeholder로 확인)
- [ ] 동시 다중 SFX 재생 시 끊김/누수 없음 (풀 동작)
- [ ] 볼륨 설정이 재시작 후 유지 (PlayerPrefs)
- [ ] WSL2 회귀 게이트 green (서버 무관)

---

## 🧪 테스트

**수동**: 디버그 키/버튼으로 placeholder SFX 연타 → 끊김/에러 없는지, BGM 전환 크로스페이드 확인
**자동**: 서버 무관 — 회귀 644/0/5 유지

---

## 📚 학습 포인트

- AudioSource **풀링** — 매 사운드마다 GameObject 만들면 GC/생성 비용. 재사용 풀이 표준.
- AudioMixer 그룹으로 볼륨을 계층 제어하는 법.
- BGM 크로스페이드 = 두 AudioSource 볼륨을 반대로 lerp.

---

## ⚠️ 함정 / 주의사항

- 사운드 재생을 게임 로직 분기에 쓰지 말 것 — 순수 부수효과(표현)여야 함.
- Decompress on Load(짧은 SFX) vs Streaming(긴 BGM) 로드 타입을 인프라가 가정하지 말고 에셋 import에서 결정(Phase 03).
- 풀 크기 고정이면 동시 재생 폭주 시 잘림 — 상한 + 우선순위 정책 고려.

---

## ➡️ 다음 Phase

- Phase 04 — SFX wiring (인프라 + 에셋 후)
