---
owner: youngho
milestone: M4.3R
phase: 05
title: 클라 구조 기회성 (EnemyViewFactory 추출 + PlayerPredictorTests)
status: done
grade: 보통
domain: client
estimated: 2~3h
---

# Phase 05: 클라 구조 기회성 (rank 3 + rank 5)

> **상태**: pending
> **마일스톤**: M4.3R
> **등급**: 보통 (2 클래스 + 테스트 신설, 독립)
> **담당**: client SubAgent

---

## 🎯 목표

부록 A "선택" + 신규 발견 2건을 묶어 처리한다: (1) `EnemyRegistry`(`03_Client/Assets/Scripts/Combat/EnemyRegistry.cs`, 240줄)에서 GameObject 빌더 책임을 `EnemyViewFactory`로 추출(§3.1 — 레지스트리는 dict 관리만), (2) 이미 순수 C#로 잘 추출된 `PlayerPredictor`(`03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs`)의 reconcile 알고리즘에 **EditMode 테스트를 신설**(§3.1의 "테스트 가능성" 의도를 절반만 실현한 갭 메움).

---

## ⏪ 사전 조건

- [ ] 없음 — 독립 (Phase 02와 같은 client 도메인이라 순차)

---

## 📝 작업 내용

### rank 3 — EnemyViewFactory 추출
- [ ] `BuildPlaceholder`(76줄 L163~238) + sprite 로딩(`GetWhiteSquare`/`TryLoadEnemySprites`/`LoadFirstSpriteAt` + static sprite 캐시 4개)을 `EnemyViewFactory`(또는 `EnemyPlaceholderBuilder`) 정적 클래스로 추출
- [ ] `EnemyRegistry`는 dict 관리(`_enemies` + `Spawn`/`ApplyHit`/`Despawn`/`TryGetNearest`/`Clear`) + factory 호출만 잔류
- [ ] **미래 prefab 교체 약속 보존** — `_normalPrefab`/`_bossPrefab` SerializeField(L18 주석)로 갈아끼울 수 있게 factory 인터페이스 유지

### rank 5 — PlayerPredictorTests
- [ ] `PlayerPredictorTests`(EditMode) 신설 — reconcile snap/replay 경로 박제(`OnSnapshot` mispredict 판정 + 미-ack 입력 replay, L90~119)
- [ ] (선택) `UnityEngine.Vector2`/`Mathf.Abs`(L48~49,96) → `System.Numerics.Vector2` + `System.Math` 교체로 InputHistory처럼 완전 순수화 (테스트 의존 정리)

### ⚠️ 분리 금지 (§0.3)
- [ ] PlayerPredictor는 이미 순수 C# — **추가 클래스 쪼개기 금지** (테스트 보강만)

---

## ✅ 완료 조건

- [ ] `EnemyRegistry`는 dict 책임만, `EnemyViewFactory` 분리 (빌더 로직 이동)
- [ ] `PlayerPredictorTests` N개 통과 (reconcile snap/replay 검증)
- [ ] Unity 컴파일 green
- [ ] 동작 보존: 기존 InputHistoryTests + Play로 적 spawn/렌더 정상
- [ ] reviewer 헌법 hard 위반 0

---

## 🧪 테스트

**자동**: PlayerPredictorTests(신규) — mispredict 시 snap + 미-ack 입력 재적용. 기존 InputHistoryTests 회귀 0.
**수동**: Play로 적 placeholder 렌더 + HP bar 정상 (factory 추출 후 외형 동일).

---

## 📚 학습 포인트

- **§3.1 두 책임 분리**: "레지스트리(데이터 관리)"와 "뷰 빌더(GameObject 생성)"는 다른 관심사. dict만 보고 싶은 사람이 76줄 빌더를 통과 안 하게.
- **테스트 가능성 = §3.1의 진짜 목적**: 순수 C#로 추출하는 이유는 "MonoBehaviour 없이 EditMode 테스트". PlayerPredictor는 추출은 됐는데 테스트가 없어 의도 절반만 실현 — 테스트가 추출을 *완성*.

---

## ⚠️ 함정 / 주의사항

- **prefab 교체 약속 보존** — 정유현 미래 작업(SerializeField prefab)을 막지 않게 factory 인터페이스 유지.
- **순수화는 선택** — Vector2 교체가 다른 호출자에 영향 주면 보류. 테스트 신설이 핵심.
- **외관(sprite/scale/pivot)은 본인 분담 인접** — factory가 비주얼 상수를 들지만 코드 추출이라 AI 영역. 실제 prefab/sprite asset 교체는 본인(memory `unity-visual-work-user-owned`).

---

## ➡️ 다음 Phase

- Phase 06 (클라 네이밍) / Phase 07 (네트워크 prefix)

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit message만.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
