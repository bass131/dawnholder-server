# Phase 08: 유현 Asset 통합 + 3-zone 시각화 + Stage Clear UI

> **2026-05-18 분리 결정** — 본 Phase는 *시점이 다른 두 종류*가 섞여있어 두 갈래로 분리:
> - [`08a-asset-prefab-preparation.md`](08a-asset-prefab-preparation.md) — Asset import + Prefab variant + LocalPlayer 추출 검증 + RemotePlayer 비주얼 교체 (정유현, 의존성 0 = 본인 Phase 05와 *완전 병렬*)
> - [`08b-zone-ui-integration.md`](08b-zone-ui-integration.md) — 3-zone 배경 + StageClear UI + HP 바 (정유현, 본인 Phase 07 후속)
>
> 본 파일은 *분리 흔적*으로 보존. 신규 작업은 08a/08b 참조.
>
> **상태**: superseded (분리 → 08a + 08b)
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 2.5h
> **담당 에이전트**: client

---

## 🎯 목표

면담 시각 임팩트. 유현 Asset(캐릭터/적/보스 스프라이트) 통합 + 단일 맵 3-zone 시각화 (마을/전투/보스) + StageClear UI + HP 바.

## ⏪ 사전 조건

- [ ] Phase 07 완료 (서버 흐름 다 깔림)
- [ ] 유현 Asset 받음 (Phase 01 smoke check 통과)

---

## 📝 작업 내용

- [ ] **캐릭터 스프라이트 + Animator** (대기/이동 2상태 minimum) — 본인 + remote entity prefab (Phase 05 placeholder 박스 → 진짜 캐릭터)
- [ ] **적/보스 스프라이트** — EnemyEntity / BossEntity prefab
- [ ] **3-zone 배경 시각화** — 맵 배경 스프라이트 또는 zone별 색깔/텍스트 라벨 (좌=마을, 중=전투, 우=보스)
- [ ] **Stage Clear UI** — Canvas + 큰 텍스트 + 페이드. *(정유현 영역 `Scripts/UI/`이지만 응급은 본인이 박고 commit 메시지에 "응급, 후속 인계 예정")*
- [ ] **HP 바** (간단) — 본인 + 적/보스 머리 위. 서버 HpUpdate 받아 표시만
- [ ] zone 도달 시 카메라 전환 또는 자연스러운 카메라 추적 (응급은 카메라 follow만)

## ✅ 완료 조건

- [ ] 본인 + 타인 캐릭터 스프라이트 + 대기/이동 애니메이션
- [ ] 적/보스 스프라이트 표시
- [ ] 3-zone 배경 (텍스트 라벨 OK)
- [ ] Stage Clear UI 표시 (보스 사망 시)
- [ ] HP 바 (적/보스 HP 0 → 0 표시 후 사라짐)

---

## 🧪 테스트

**수동**: Unity Editor 풀-쓰루 = 접속 → 이동 (zone 전환 시각화) → 적 처치 → 보스 처치 → StageClear UI 확인

---

## 📚 학습 포인트

- **Asset import** — Sprite Mode (Single/Multiple), Pixel Per Unit, Pivot 설정
- **Animator State Machine** — 응급은 *대기 → 이동* 2상태 minimum. 복잡화 함정 회피
- **Canvas UI** — World Space vs Screen Space, RectTransform, anchor 설정
- **CODEOWNERS 영역 침범** — 정유현 UI 영역에 응급 commit 시 *디스코드 사전 안내* + commit 메시지 명시
- **Sprite Renderer vs UI Image** — World 오브젝트 = SpriteRenderer, UI Canvas = Image

---

## ⚠️ 함정 / 주의사항

- **Asset 포맷 mismatch** — Phase 01 smoke check에서 잡혔어야 함. 안 잡혔으면 placeholder fallback
- **Animator State Machine 복잡화** — 응급은 2상태로 minimum. transitions 무리 X
- **정유현 영역 침범** — `Scripts/UI/`에 응급 commit 시 *commit 메시지에 "응급, 후속 인계 예정" 박기 + 디스코드 사전 안내*
- **HP 바 위치** — Screen Space로 박으면 카메라 이동 시 위치 깨짐 → World Space + canvas billboarding
- **Stage Clear UI 페이드** — 응급은 fade-in 0.5s + 텍스트 stay 3s + 시연 후 manual 닫기

---

## ➡️ 다음 Phase

Phase 09 — 데모 리허설 + 마지막 fix

---

## 작업 로그

- 2026-05-18: pending
