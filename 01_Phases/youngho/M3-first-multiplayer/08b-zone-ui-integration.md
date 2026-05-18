# Phase 08b: 3-zone 배경 + StageClear UI + HP 바 통합

> **상태**: pending (정유현 작업, 영호 Phase 07 후속)
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 1h
> **담당 에이전트**: client
> **담당 사람**: 정유현 (영역: `Scripts/UI/`, `Scenes/UI.unity`, Canvas)

---

## 🎯 목표

면담 시각 임팩트 마감. 단일 맵 3-zone 시각화 + Stage Clear UI + HP 바. 영호 서버 흐름(Phase 06 응급 전투 + Phase 07 보스+StageClear) 완료 후 합치는 시점.

**끝나면 데모 가능한 것**: 면담 완성 데모 — 멀티 접속 + 진짜 캐릭터 + 3-zone 진입 + 전투 + 보스 + Stage Clear UI 풀-쓰루.

---

## ⏪ 사전 조건

- [ ] Phase 08a 완료 (캐릭터 비주얼 정착)
- [ ] 영호 Phase 06 완료 (서버 응급 전투 + HP 패킷)
- [ ] 영호 Phase 07 완료 (보스 + StageClear 서버 신호)

---

## 📝 작업 내용

### 1. 3-zone 배경 시각화
- [ ] 단일 맵에 zone 표시 — 좌(마을) / 중(전투) / 우(보스)
- [ ] 응급 모드 = zone별 색깔 배경 또는 텍스트 라벨 (Sprite 작업 부담 ↓)
- [ ] 카메라 follow = M2 CameraFollow 그대로 (zone별 카메라 전환 X)

### 2. Stage Clear UI
- [ ] Canvas — World Space vs Screen Space 결정 (응급 = Screen Space, 면담 후 검토)
- [ ] 큰 텍스트 "Stage Clear" + 페이드 인 0.5s + stay 3s + manual 닫기
- [ ] 영호가 박은 서버 `S_StageClear` 패킷 dispatch → UI 표시
- [ ] CODEOWNERS 본인 영역(`Scripts/UI/`, `Scenes/UI.unity`) 자유 편집

### 3. HP 바
- [ ] 본인 + 적/보스 머리 위 HP 바 (World Space + Canvas billboarding 또는 SpriteRenderer 직접)
- [ ] 영호가 박은 `S_HpUpdate` 패킷 받아 표시
- [ ] HP 0 → 0 표시 후 0.5s fade out

---

## ✅ 완료 조건

- [ ] 3-zone 배경 시각화 (zone 도달 시 시각 차이 보임)
- [ ] Stage Clear UI 표시 (보스 사망 시)
- [ ] HP 바 표시 (적/보스 HP 0 → 사라짐)
- [ ] 면담 풀-쓰루 통과 (접속 → 이동 → zone → 전투 → 보스 → Clear)

---

## 🧪 테스트

**수동**: 헤드리스 봇 2명 + 본인 Unity 클라 = 풀-쓰루 한 흐름
**자동**: 없음

---

## 📚 학습 포인트

- **World Space vs Screen Space Canvas** — 카메라 이동 시 위치 깨짐 차이
- **UI 페이드 패턴** — CanvasGroup.alpha + tween (DOTween 또는 수동 코루틴)
- **서버 신호 → UI dispatch** — UnityClientSession에서 신호 받아 UI 컴포넌트 호출
- **응급 모드 UI 최소화** — 면담 위주, 정밀화는 M4+

---

## ⚠️ 함정 / 주의사항

- **HP 바 World Space billboarding** — 캐릭터 회전 따라가지 않게 LateUpdate에서 카메라 방향 정렬
- **StageClear 페이드 stuck** — alpha 1로 박힌 채 다음 stage 가면 면담 데모 깨짐. manual 닫기 또는 timeout
- **CODEOWNERS 영호 영역 침범 X** — 서버 dispatch는 영호 Phase 07에서 박힘. 유현은 *받아 표시*만

---

## ➡️ 다음 Phase

- **Phase 09** — 데모 리허설 + 마지막 fix (영호 + 유현 협업)

---

## 작업 로그

- 2026-05-18: 정의 신설 (5/18 분담 합의 결과)
