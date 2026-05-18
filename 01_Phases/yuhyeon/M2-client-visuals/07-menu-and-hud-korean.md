# Phase 07: 메뉴 + HUD 한글화

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1~1.5시간
> **담당 에이전트**: client

---

## 🎯 목표

MainMenu, PauseMenu, HUD의 모든 영문 TMP_Text를 한글로 교체한다. 직접 텍스트 교체만 사용(PRD i18n Non-Goal 정합 — Unity Localization 패키지 X).

**끝나면 데모 가능한 것**: 게임 켜고 메인 메뉴 → 시작 → 게임 화면(HP·골드 등) → ESC 일시정지 메뉴 → 종료까지 모든 텍스트가 한글.

---

## ⏪ 사전 조건

- [ ] Phase 06 완료 (한글 TMP Font Asset 박힘)
- [ ] M1 Phase 02 (MainMenu), Phase 04 (PauseMenu), Phase 03 (HUD) 모두 완성
- [ ] 본인 영역 (`/03_Client/Assets/Scripts/UI/` + `/03_Client/Assets/Scenes/UI.unity` + MainMenu.unity) 확인 (CODEOWNERS @jungyoohyun0105)

---

## 📝 작업 내용

- [ ] **MainMenu 씬** (`MainMenu.unity`) 한글화:
  - "Start" → **"시작"**
  - "Settings" → **"설정"** (있다면)
  - "Quit" → **"종료"**
  - 게임 제목/로고 텍스트 있으면 한글 (예: "Dawnholder" → 그대로 유지 OK, 제품명은 영문)
- [ ] **PauseMenu** (`UI.unity` 내) 한글화:
  - "Resume" → **"재개"**
  - "Main Menu" → **"메인 메뉴"**
  - "Quit" → **"종료"**
- [ ] **HUD** (`UI.unity` 내 HudController) 한글화:
  - "HP" 라벨 → **"체력"** (또는 그대로 "HP" 유지하고 숫자만 표시도 가능 — 본인 판단)
  - "Gold" → **"골드"**
  - 미니맵 placeholder 텍스트 있으면 한글
- [ ] **HudController.cs 코드 리터럴 정리**: 만약 `$"HP: {hp}/{maxHp}"` 같은 코드 리터럴이 있으면 한글로 (예: `$"체력 {hp}/{maxHp}"`). 본인 영역(`Scripts/UI/`)이라 자유.
- [ ] **빈 버튼 width 점검**: 영문 기준 button width가 한글에서 짧게 보이거나 잘림 → RectTransform Width 조정 또는 Auto Size
- [ ] **시각 검증**: 6 시나리오(M1 회귀) 흐름 따라가며 모든 텍스트 한글 확인

---

## ✅ 완료 조건

- [ ] MainMenu.unity / UI.unity의 TMP_Text 모두 한글 (제품명 제외)
- [ ] HudController.cs의 코드 리터럴도 한글 (있는 경우)
- [ ] 모든 한글이 tofu(깨진 사각형) 없이 또렷이 표시
- [ ] M1 회귀 6 시나리오 + 메뉴 텍스트 한글로 정상 진행
- [ ] 버튼/텍스트 영역 잘림 없음 (한글이 영문보다 짧아지는 경우가 많아 보통 문제 X)

---

## 🧪 테스트

**수동 테스트:**
1. Play → MainMenu에 "시작 / 설정 / 종료" 한글 표시
2. 시작 클릭 → 페이드 → Gameplay에 "체력 100/100", "골드 0" 한글 표시
3. ESC → "재개 / 메인 메뉴 / 종료" 한글 표시
4. 메인 메뉴 → 페이드 후 MainMenu 복귀 (한글 유지)
5. 모든 글자에 tofu 0개

**자동 테스트:** 없음 (텍스트 검증은 수동 시각)

---

## 📚 학습 포인트

- **직접 텍스트 교체 vs i18n 시스템 (Trade-off)**:
  - 직접 교체: 코드/씬에 한글 문자열 박힘. 단순. *언어 추가하려면 모든 곳 수정*.
  - i18n (Unity Localization 패키지): 키 → 언어별 테이블. 언어 추가 = 테이블만 추가. 초기 셋업 비용.
  - **PRD Non-Goals**: "로컬라이제이션 — 한국어만" → i18n 비용 회피 결정. 우리 프로젝트엔 직접 교체가 정답.
- **한글 vs 영문 너비**: 한글 1글자 ≈ 영문 1.5~2글자 너비 정도. 보통 한글이 짧아져 button 안에 깔끔. 반대 케이스(영문이 짧은데 한글 긴 경우)는 거의 없음.
- **제품명 처리 컨벤션**: 게임 제목(Dawnholder)·전문 용어(EXP/SP 같은)는 영문 유지가 통례. "익숙도"가 가독성보다 우선.
- **본인 영역(CODEOWNERS) 자유 편집**: `/Scripts/UI/`, `/Scenes/UI.unity`, `/Scenes/MainMenu.unity` 본인 단독 영역(ADR-021 + CODEOWNERS) → 팀장 승인 불필요. 단 commit 메시지에 "M2 Phase 07 메뉴/HUD 한글화" 명시.

---

## ⚠️ 함정 / 주의사항

- **Phase 06의 Fallback 안 박힌 채 진행**: tofu 발생 → Phase 06 TMP Settings 다시 확인.
- **씬 파일(.unity) 머지 충돌 위험**: 본 Phase는 MainMenu.unity + UI.unity 둘 다 만짐. 본인 단독 영역이라 안전. 단 다른 팀원이 같은 씬 만지지 않게 작업 시작 시 알려두기 (ADR-021 + CODEOWNERS).
- **코드 리터럴 한글화 시 인코딩**: `.cs` 파일이 UTF-8(BOM 없음 또는 BOM 있음)인지 확인. Unity는 UTF-8 BOM 없는 게 표준. .gitattributes에 박혀있을 것.
- **숫자 포맷 한글화 유혹**: "100/100" → "100 / 100" 같이 띄어쓰기 손대다 보면 다른 버그 발생. 숫자/구분자는 손대지 X.
- **Resume 단어 결정**: "재개" vs "계속" vs "다시 시작" — *재개*가 일반적. 게임 메뉴 컨벤션은 "계속하기"도 있음. 본인 취향.
- **버튼이 작아 보이는 효과 안 챙김**: 한글이 짧아 button이 휑할 수 있음 → RectTransform width를 살짝 줄이거나 그대로 두기. 면담 데모 디테일.

---

## ➡️ 다음 Phase

- **Phase 08 — Regression + 데모 영상 + M2 학습 일지**: M2 마일스톤 마감.

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
