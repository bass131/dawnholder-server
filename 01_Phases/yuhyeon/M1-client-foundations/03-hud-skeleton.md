# Phase 03: HUD 골격 — HP·자원·미니맵 자리잡이

> **상태**: pending
> **마일스톤**: M1-client-foundations
> **예상 소요**: 2~3시간
> **담당 에이전트**: client

---

## 🎯 목표

Gameplay 씬에 HUD Canvas overlay 추가. **HP바** (Slider) + **자원 텍스트** (HP 100/100, Gold 0) + **미니맵 자리잡이** (빈 RawImage) 표시. 모두 mock 값 — 서버 데이터 연결은 별 Phase. 해상도 변경에도 어그러지지 않음.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (메인 메뉴 → Gameplay 씬 전환 가능)
- [x] Gameplay 씬에 캐릭터 존재 (팀장 M2)

---

## 📝 작업 내용

- [ ] Gameplay 씬에 새 HUD Canvas 추가 (Screen Space Overlay, Sort Order 10 — 위에 그려지게)
- [ ] Canvas Scaler 모드 = Scale With Screen Size, Reference Resolution 1920x1080
- [ ] HUD Canvas 안에 4개 UI 요소 배치:
  - **HP 슬라이더** (좌상단, Anchor 좌상단): Slider value=1, Fill 색 빨강
  - **HP 텍스트** (HP 옆): "HP 100 / 100"
  - **자원 텍스트** (HP 아래): "Gold: 0"
  - **미니맵** (우상단, Anchor 우상단): RawImage 200x200, 회색 배경
- [ ] `Scripts/UI/HudController.cs` 작성:
  - `[SerializeField] Slider hpSlider;`
  - `[SerializeField] TMP_Text hpText, goldText;`
  - `Start()`에서 mock 값 표시: HP 100/100, Gold 0
  - `public void UpdateHP(int current, int max)` 메서드 (다음 마일스톤에서 호출 예정)
- [ ] HudController 컴포넌트를 HUD Canvas에 부착 + Inspector로 슬라이더/텍스트 참조 연결
- [ ] 씬 저장

---

## ✅ 완료 조건

- [ ] Gameplay 씬 Play 시 HP바·자원·미니맵 자리잡이 모두 보임
- [ ] HP바가 빨강색으로 가득 차 있음 (mock 100/100)
- [ ] Gold 텍스트 "Gold: 0" 표시
- [ ] 1920x1080 / 1280x720 / 800x600 세 해상도에서 HUD 위치 어그러지지 X
- [ ] Console Error 0개

---

## 🧪 테스트

**자동 테스트:**
- 없음

**수동 테스트:**
- Game 뷰 해상도 토글 (16:9 → 4:3 → Free Aspect)로 HUD 위치 확인
- Window → General → Game View → Aspect 변경 시 HP바가 좌상단 유지, 미니맵이 우상단 유지

---

## 📚 학습 포인트

- **Canvas Scaler 3가지 모드**:
  - *Constant Pixel Size*: 픽셀 그대로. 해상도 작으면 UI 거대.
  - *Scale With Screen Size* (이번 Phase): 기준 해상도 대비 비율 유지. UI 일관성.
  - *Constant Physical Size*: 물리 단위(인치). 모바일에서 유용.
- **Anchor 활용**: 좌상단 Anchor = 화면 왼쪽 위 부착. 화면 커져도 좌상단에 붙음. 미니맵은 우상단 Anchor.
- **Slider 컴포넌트**: value 0~1. min/max value 따로 설정 가능. Fill Rect 색으로 시각화.
- **SerializeField vs public**: SerializeField는 외부 노출 없이 Inspector 노출만 (캡슐화). 학부생 권장 패턴.
- **헌법 1번 재확인**: HP 값 *계산*은 서버. 클라는 *받아 표시*만. UpdateHP 메서드는 외부(서버 패킷 핸들러)가 호출하는 "표시 API".

---

## ⚠️ 함정 / 주의사항

- Anchor를 중앙에 두고 좌상단 배치하면 화면 커질수록 HUD가 가운데로 떠다님 — Anchor를 좌상단으로 *명확히* 설정.
- Canvas Scaler 안 설정하면 4K 모니터에선 HUD가 깨알처럼 보임.
- SerializeField 참조 안 연결한 채 Play하면 NullReferenceException — Inspector에서 *모든 슬롯* 채워야.
- 헌법 1번 유혹: "공격 받으면 HP 깎는 코드 클라에 짜자" — X. 그건 서버. 클라는 *서버가 알려준* HP만 표시.

---

## ➡️ 다음 Phase

Phase 04 — 일시정지 메뉴: ESC 키 토글 + Resume/Quit. Input System 액션 연동.

---

## 작업 로그

- 2026-05-17: /work:plan으로 생성
