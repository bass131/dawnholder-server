# Phase 06 — DONE (완료)

> **상태**: 완료 — 4 완료 조건 모두 통과 + M1 마일스톤 마감
> **마일스톤**: M1-client-foundations (**전체 6 Phase + ad-hoc 1건 마감**)
> **완료일**: 2026-05-18
> **소요 시간**: 약 1시간 (예상 1~2h, 사실상 *코드 변경 0* — 회귀 + 영상 녹화 + 학습 일지 인터뷰)
> **담당**: 정유현 (@jungyoohyun0105)

---

## 🎯 무엇 — 완료된 것

### 회귀 시나리오 6/6 통과 (Play 모드 수동)
M1 전체 흐름이 한 호흡으로 끊김 없이 돌아감 확인:

| # | 단계 | 결과 |
|---|------|------|
| a | MainMenu 진입 — 페이드 인 검은 → 메뉴 보임 | ✅ |
| b | "시작" 클릭 → 페이드 아웃 → Gameplay 페이드 인 → HUD 보임 (HP 100/100, Gold 0, 미니맵 placeholder) | ✅ |
| c | ESC → 일시정지 메뉴 → 캐릭터 멈춤 (`timeScale=0`) | ✅ |
| d | "재개" 클릭 → 메뉴 닫힘 → 캐릭터 움직임 복구 (`timeScale=1`) | ✅ |
| e | ESC → "메인 메뉴" 클릭 → 검은 페이드 → MainMenu 복귀 (`timeScale 1` 유지) | ✅ |
| f | 에디터 ▶ 정지 → 정상 종료 | ✅ |

### 데모 영상 박제
- **`00_Document/learning-journal/yuhyeon/M1-client-foundations/demo.mp4`** (신규)
  - 30초, 1280×720, **754KB** (5MB 임계 한참 아래 — git commit 그대로 OK)
  - ShareX FFmpeg MP4 녹화
  - 회귀 6단계 흐름 자연스럽게 들어감

### M1 마일스톤 학습 일지
- **`00_Document/learning-journal/yuhyeon/M1-client-foundations/M1-recap.md`** (신규)
  - 인터뷰 형식 7개 항목 중 **6/7 답변**
  - 본인 답변 핵심:
    1. **한 줄 요약**: "캡스톤 게임의 *클라이언트 1단계*로, 메뉴→게임→ESC→페이드가 한 호흡으로 진행되는 UI 기초 골격"
    2. **결정 회고 (★★★ UI 씬 분리, ADR-021)**: 충돌 직접 체험 없이 *협업 위험 시나리오 예측* → 분리 선제 도입
    3. **사건 회고 (★★★ Phase 04 시각 fix)**: MCP를 단순 해답이 아니라 *학습 가속기*로 자각
    4. **다시 한다면**: 도구 도입 타이밍 / 순서 / 학습 깊이 vs 진행 속도 — *이상 vs 현실 trade-off* 인식
    5. **면접 시뮬레이션**: "AI 추천 무작정 X, *왜 이걸 써야 하고 어떤 게 합리적인지* 한 번 더 고려" — AI 비판적 활용 마인드셋
  - 미답: 개념 차원 (Singleton/Coroutine 등 깊이) → 학습 큐 7개로 이관, M2+에서 보강

### 6개 -DONE.md 페어 모두 존재
- ✅ 01-hello-ui-bootstrap-DONE.md (Phase 01)
- ✅ 02-main-menu-buttons-DONE.md (Phase 02)
- ✅ 03-hud-skeleton-DONE.md (Phase 03)
- ✅ 04-pause-menu-input-DONE.md (Phase 04)
- ✅ 05-scene-transition-fade-DONE.md (Phase 05)
- ✅ 06-regression-and-demo-DONE.md (Phase 06, **본 파일**)

---

## 🤔 왜 — 결정 흐름

### Phase 06 = 마일스톤 마감 박제 + 학습 *fresh* 시점 박기
- **이유**: Phase 끝나고 *바로* 회고하지 않으면 디테일이 사라진다는 헌법 원칙. M1 통째 회고를 *Phase 06이라는 독립 단위*로 박는 게 "데모 가능한 단위" 원칙의 종착점 — 마일스톤 끝 = 시연 가능.
- **trade-off**: 영상 녹화·일지 작성에 1시간 추가. 단 *포트폴리오 자료 0 → 1*은 100배 가치 차이.

### 영상 포맷 MP4 채택 (GIF 대신)
- **이유**: 30초 MP4 754KB vs 추정 GIF 5~10MB. 학습 자료 폴더 부담 ↓. 면접 시연 시 MP4 재생이 더 깨끗.
- **trade-off**: GitHub README 인라인 자동 재생은 GIF가 우월. 단 *지금은* README 박제 안 했고 *필요해지면 그때* 변환 (ShareX `Tools` 또는 온라인 컨버터로 1분).

### 학습 일지 = 본인 답변 *그대로* 박기 (AI 추측 채움 X)
- **이유**: ADR-013 박제 분업 — `-DONE.md`=AI(사실) / `learning-journal/`=본인(회고). 가짜 학습 방지.
- **결과**: Q2 "잘 모르겠음" 2회 답변도 *그대로* 박힘. 본인이 *지금 어디가 부족한지* 보임. 미래 본인이 학습 큐로 가져가는 입구.

---

## 🛠️ 어떻게 — 흐름

```
[1] git
   main pull (3fe9368 → 09c28ab, 5 commits FF)
     ├─ PR #23 (본인 Phase 05) 머지 확인
     └─ PR #24~26 (팀장 pre-M3 감사 + M2.5 + 캡스톤 6/10 확정)
   옛 브랜치 정리 (feature/yuhyeon-m1-phase05-scene-fade)
   신 브랜치 (feature/yuhyeon-m1-phase06-regression-demo)
   work-pin Phase 05 → Phase 06 갱신

[2] 회귀 시나리오 (Unity Editor 수동)
   MainMenu Play → 6단계 한 흐름 → 6/6 통과

[3] 영상 녹화 (ShareX FFmpeg MP4)
   winget install ShareX.ShareX (3분)
   Game 탭 1280×720 해상도 신설
   Shift + PrintScreen → Game 창 선택 → 30초 회귀 → 정지
   파일을 learning-journal/yuhyeon/M1-client-foundations/demo.mp4로 이동 (탐색기)

[4] 학습 일지 인터뷰 (/journal:phase)
   M1-recap.md 뼈대 + 자동 채울 객관 정보 박음
   인터뷰 7개 질문 → 본인 답변 그대로 박기 → 6/7 답변
   학습 큐 7개 보존 (우선순위 비움)

[5] -DONE.md 박제 (본 파일)
   다음: 5단계 보고 → commit/PR
```

---

## 🧪 테스트 — 4/4 완료 조건 통과

| # | 완료 조건 | 결과 |
|---|----------|------|
| ① | 회귀 시나리오 6단계 한 흐름 통과 (중간 에러/멈춤/페이드 깨짐 X) | ✅ |
| ② | 데모 영상/GIF가 learning-journal/yuhyeon/M1-client-foundations/ 에 박제됨 | ✅ (demo.mp4, 754KB) |
| ③ | M1 학습 일지 작성됨 (`/journal:phase` 결과물) | ✅ (M1-recap.md, 6/7 답변) |
| ④ | 6개 Phase 모두 -DONE.md 페어 존재 | ✅ (본 파일이 마지막) |

---

## ➡️ 다음

### 즉시
- **commit** — `feat(phase06): 회귀 통과 + 데모 영상 + M1 학습 일지 (M1 마감)`
- **PR push** + 머지 → **M1-client-foundations 마일스톤 마감**
- 노션 협업 기록 DB: 5번째 페이지 박제 (Phase 06 + M1 통째 회고)

### M1 마감 후 (다음 세션 또는 동일 세션 후속)
- **다음 클라이언트 마일스톤 결정** — `/work:plan <목표>` 호출. 핀에 M2-character-sprites 예시 박혀있으나 *본인이 결정*.
- 캡스톤 1 발표(2026-06-10) 23일 남음 — 1~2 마일스톤 더 가능

### 보류 작업 (본인 결정으로 *다음 마일스톤 끝날 때 같이* 정리)
- ★★★ `/journal:bug unity-setparent-world-position-stays` — Phase 04 시각 fix 사건. **fresh 시간 비용 의식 있음**. 다음 마일스톤 시작 *전*에 30분 미니 박제(키워드 3~5개만)도 옵션.
- `/journal:concept coroutine-vs-async` + `additive-scene-pattern` — 학습 큐 ★ 항목 2개. M2+ 진행 중 자연스럽게 만날 때 또는 별도 시간.
- TMP 한글 폰트 도입 — 다음 마일스톤 데모 박제 *전*에 메뉴 한글화 검토 가능.

---

## 📝 학습 보존

### 새로 익힌 것
- **회귀 테스트 사고법** — 새 기능 추가 후 *기존 동작* 깨지지 않았는지 검증. M1처럼 작은 마일스톤은 *수동*, M3+ 컨텐츠 들어가면 *자동화* 도입. 본인이 직접 6단계를 *한 호흡으로* 돌리며 "끊김 없음"을 체감.
- **"데모 가능한 단위" 원칙의 종착점** — 마일스톤 끝 = 시연 가능. 영상 한 편이 "여기까지 됐다"의 증명. *포트폴리오·면접 자료의 첫 단추*.
- **박제 분업 (ADR-013) 실측** — `-DONE.md`=AI(사실) / `learning-journal/`=본인(회고)을 *처음으로* 한 호흡에 진행. AI가 객관 정보 채우는 동안 본인은 *주관 회고만* 집중 → 가짜 학습 회피 + 시간 효율 ↑.
- **ShareX 도구 첫 도입** — winget 설치 → 단축키 확인 → MP4 녹화 → 박제까지 *15분 안에* 완결. *영상 녹화 도구 셋업의 학습 비용은 낮다*는 데이터 확보.

### 사건성 학습 (★ 학습 가치)

**"학습 일지의 *솔직한 부족* 박제"** — Q2 "잘 모르겠음" 답을 그대로 박는 것이 *진짜 학습의 입구*. AI가 추측으로 채웠다면 미래 본인이 "그때 다 알았네" 착각. 부족을 *현재 시점에 박는 것*이 미래 학습 큐의 정확성을 만듦.

**"이상 vs 현실 trade-off의 *자각*이 면접 가치"** — 본인이 Q5에서 "그때그때 개념 정리가 좋았을 것 같음 / 근데 진행도를 나가야 해서 불가피했음"이라고 박은 것은 *마감 압박 속에서 무엇을 타협했고 무엇은 안 했는지를 자각하는 개발자*의 신호. 시니어가 주니어를 평가할 때 보는 메타 인식.

**"AI 비판적 활용 마인드셋"** — Q7 본인 답변("AI 추천 무작정 X, *왜 이걸 써야 하고 어떤 게 합리적인지* 한 번 더 고려")이 *학부생 단계*에서 박힌 게 가치. AI 시대 면접관이 *진짜 보고 싶은 신호* — "이 사람이 AI에 *과의존*하지 않으면서 *제대로* 쓰는 사람인가".

---

## 🔗 산출물 (Commit 예정)

- `00_Document/learning-journal/yuhyeon/M1-client-foundations/demo.mp4` (신규, 754KB)
- `00_Document/learning-journal/yuhyeon/M1-client-foundations/M1-recap.md` (신규, M1 마일스톤 학습 일지)
- `00_Document/learning-journal/yuhyeon/M1-client-foundations/.gitkeep` (신규, 폴더 보존 마커)
- `01_Phases/yuhyeon/M1-client-foundations/06-regression-and-demo-DONE.md` (본 파일)
- `.claude/state/current-pin.txt` (수정, 단 `.gitignore`라 commit X)

---

## 작업 로그

- 2026-05-18 (Phase 05 PR #23 머지 다음 날 아침):
  - `/session:start` → 0단계 게이트 통과 (clean feature 브랜치)
  - CONTEXT.md + CHANGELOG 최근 5/17~5/18 변경 확인 (팀장 PR #24~26 인지, 캡스톤 6/10 확정)
  - `git pull origin main` (fast-forward, 5 commits)
  - 옛 브랜치 정리 + 신 브랜치 `feature/yuhyeon-m1-phase06-regression-demo`
  - learning-journal/yuhyeon/M1-client-foundations/ 폴더 신설
  - work-pin Phase 05 → Phase 06 갱신
- 단계 1: 회귀 시나리오 6단계 수동 통과 (본인 직접, Unity Editor)
- 단계 2: ShareX 설치 + 셋업 (~5분) + Game 탭 1280×720 + Shift+PrintScreen 녹화 30초
- 단계 3: ShareX 메인 → 영상 파일을 learning-journal로 이동 + 이름 `demo.mp4`로 변경
- 단계 4: `/journal:phase` 호출 → M1-recap.md 뼈대 + 인터뷰 7개 질문 → 6/7 답변 박힘 (Q2 솔직 미답)
- 단계 5: -DONE.md 박제 (본 파일)
- 단계 6: 5단계 보고 → commit → PR push → M1 마감 (다음 응답)
