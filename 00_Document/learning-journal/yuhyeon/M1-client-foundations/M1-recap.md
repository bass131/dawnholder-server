# M1-client-foundations — 마일스톤 학습 일지

> **작성일**: 2026-05-18
> **work-id**: yuhyeon-m1-milestone-recap
> **마일스톤**: M1-client-foundations (6 Phase + ad-hoc 1건)
> **소요 시간**: ~3일 (2026-05-16 ~ 2026-05-18)
> **상태**: 완료 (개념 차원만 추가 학습 필요 — 학습 큐로 이관)
> **데모 영상**: [demo.mp4](demo.mp4) (30초, 1280×720, 754KB)

---

## 🎯 한 줄 요약

> 면접관이 "M1에서 뭐 했어요?" 물으면 30초 안에 답할 한 문장.

캡스톤 게임의 **클라이언트 1단계**로, "메뉴 → 게임 → ESC → 페이드"가 한 호흡으로 진행되는 UI 기초 골격을 만들었습니다.

---

## 📦 결과물 (Phase별)

| # | Phase | 결과물 | 완료일 |
|---|-------|--------|--------|
| 01 | Hello UI Bootstrap | MainMenu 씬 + 최소 Canvas + TMP_Text 표시 | 2026-05-16 |
| 02 | Main Menu Buttons | "시작" / "종료" 버튼 + `MainMenuController.cs` + 씬 전환 wire | 2026-05-16 |
| 03 | HUD Skeleton | Gameplay 씬 HUD Canvas + HP/Gold/Minimap placeholder + `HudController.cs` | 2026-05-16 |
| — | ad-hoc UI Scene 분리 | UI.unity 씬 분리 + Additive Load 패턴 + ADR-021 + CODEOWNERS 영역 박제 | 2026-05-17 |
| 04 | Pause Menu + Input | ESC 입력 + 일시정지 메뉴 + `PauseMenuController.cs` + `Time.timeScale` 0/1 | 2026-05-17 |
| 05 | Scene Transition Fade | FadeCanvas 프리팹 + `SceneTransition.cs` Singleton + DontDestroyOnLoad + CanvasGroup α 페이드 | 2026-05-17 |
| 06 | Regression + Demo | 회귀 6단계 통과 + 30초 데모 영상 + M1 학습 일지 (이 문서) | 2026-05-18 |

**핵심 시연 흐름**: MainMenu → 시작 → 페이드 → Gameplay HUD → ESC → 일시정지 → 재개 → ESC → 메인 메뉴 → 페이드 → MainMenu

---

## 🧠 새로 배운 것

> 마일스톤 전엔 몰랐거나 어렴풋했던 것. **본인 말로** 적기.

### 개념 차원

**본인 응답**: "이건 잘 모르겠음 / 정말 모르겠음" (2회).

→ Phase 04~05에서 손으로 짠 코드(Singleton, `DontDestroyOnLoad`, Coroutine, `CanvasGroup`, `Time.unscaledDeltaTime` 등)는 *동작하게 만든 단계*까지 갔으나, **본인 입으로 개념 단위로 풀어 설명할 수준엔 아직 도달하지 못함**. 솔직 인정. 키워드는 아래 "아직 모르는 것" 섹션의 다음 학습 큐로 이관.

### 구현 차원
_(인터뷰에서 따로 묻지 않음 — Q3 막혔던 지점에서 자연스럽게 채워질 수 있음)_

### 도구 차원
_(인터뷰에서 따로 묻지 않음 — 필요 시 본인이 추후 추가)_

---

## 🤔 결정 포인트 (마일스톤 단위)

> M1 진행 중 내린 굵직한 결정과 그 이유.

### 결정 1: UI 씬 분리 (ad-hoc, ADR-021) — ★★★ 본인이 고른 회고 결정

- **결정**: HUD를 Gameplay 씬에 박지 않고 `UI.unity` 별도 씬으로 Additive Load
- **고려한 대안**: HUD를 Gameplay 씬에 직접 박기 (Single 씬 흐름)
- **본인 회고 (선택 이유)**:
  > "UI 씬을 분리 안 하고 Gameplay에 작업을 했을 경우, Gameplay는 **다른 작업자도 공통으로 사용하는 부분**이라 머지를 했을 때 **내가 했던 거를 위에 덮어씌우면서 사라질 수 있는 위험**이 있었기 때문에 UI 씬을 분리하는 방향으로 결정하게 되었다."
- **핵심 포인트**: 본인이 *충돌을 직접 겪지 않은 상태*에서 협업 위험을 *시나리오로* 예측 → 회피 수단을 선제적으로 도입. 학부생이 *팀 작업 위험을 사전에 보는 사고*는 흔치 않은 신호.
- **트레이드오프**: Additive Load 부담(2 씬 동시 로딩 + Bootstrap 코드 1개 추가) vs *.unity 파일 머지 충돌 회피*. **선택은 후자가 압도적으로 안전**.
- **ADR 격상 여부**: 이미 **ADR-021**로 박힘 + `CODEOWNERS` 영역 분리(3 경로) 동반 → "결정"과 "권한"의 *짝* 패턴까지 박힌 케이스.
- **지금 다시 본다면?**: 같음. 분리가 정답.

### 결정 2: Phase 04 timeScale=0 → Phase 05 `unscaledDeltaTime` 짝
- **결정**: 페이드 코루틴에서 `Time.unscaledDeltaTime` 사용 (timeScale 영향 안 받음)
- **고려한 대안**: `Time.deltaTime` (timeScale=0이면 페이드 멈춤)
- _(선택 이유 / 결정 사슬의 *왜* — 인터뷰 Q4에서 채움)_

### 결정 3: 방어 코드 정확한 배치 (Context-Aware Defensive Coding)
- **결정**: MainMenuController엔 `SceneTransition.Instance` null fallback 없음 / PauseMenuController엔 fallback 있음
- _(선택 이유 — 컨텍스트 보장 vs 보장 X. 인터뷰 Q4에서 채움)_

---

## 🐛 막혔던 지점 (마일스톤 단위)

> 진짜로 막혔던 사건. ★ 갯수 = 면접 가치.

### 사건 1: Phase 04 시각 fix (★★★ 면접 결정타)
- **증상**: 일시정지 메뉴 활성화는 됐는데 *화면에 안 보임*. Console 깨끗.
- **1차 디버깅**: 1.5시간 추측 (BG 컬러? Sort Order? Anchor?)
- **2차 디버깅**: Unity MCP `Unity_RunCommand` 4회 dump로 5분 만에 좁힘
- **진짜 원인 (기술적)**: `SetParent(parent, worldPositionStays=true)` 손작업이 옛 부모(rect 0×0)의 stretch 결과를 음수 sizeDelta로 박아 MenuRoot가 (0,0) 점이 됨
- **본인이 기억하는 한 줄**: "오브젝트를 자식으로 상속시켰어야 했는데 그것을 안 해서 오류가 발생한 거였음" — 부모-자식 관계 설정 시점·플래그 누락이 핵심.

**본인 회고 (그때 느낌 + 학습)**:

> 세팅한 건 다 맞는데 어디서 틀렸나 의문이 있었음. MCP로 연결해서 한 번에 문제를 찾으니까 **아직 내가 모르고 있는 게 많음**을 느꼈음.
>
> 동시에, MCP를 잘만 활용하면 단순 해답 도구가 아니라 **내 학습 곡선에 도움을 주고 작업 속도에 가속을 실어줄 수 있겠다**는 생각이 듦. → *AI 도구를 학습 가속기로 보는 마인드셋*.
>
> 같은 일이 또 발생하면 이제 쉽게 대처 가능할 것 같다 — 한 번 데인 경험이 다음에 *증상-원인* 매핑을 빠르게 만들어줌.

- **별도 일지 후보 (★ 강력 추천, 지금이 fresh)**: `/journal:bug unity-setparent-world-position-stays` — *기억 살아있을 때* 따로 떼서 박기. 면접 답변 1개 더 확보.

### 사건 2: _(M1 통째 회고이지만 더 떠오르는 사건 없음 — 추후 본인이 추가 가능)_

---

## 💡 다시 한다면

> 지금 처음부터 다시 한다면 무엇을 다르게 할까. **이게 진짜 학습의 증거**.

### 1. 도구 도입 타이밍 — *작업 시작 시점*부터 MCP

> "일단 작업 시작 시 MCP를 연결하고 시작했을 것이고, 하나의 문제 생기면 **너무 오래 끌지 않고** 같이 MCP 연결해서 **협업을 통해 빠르게 해결**했을 것."

→ 핵심 인식: **혼자 추측으로 1.5시간 끄는 것보다 AI/팀과 빠르게 같이 해결**이 낫다. *블로킹 시간을 줄이는 시니어 마인드*. Phase 04 1.5h 추측 디버깅의 직접 학습.

### 2. 순서 — UI 씬 분리를 *처음부터*

> "씬 분리해야 할 것을 미리 알았다면 처음부터 진행했을 것임."

→ 현실: Phase 03 HUD를 Gameplay 씬에 박았다가 ad-hoc 작업으로 *UI.unity로 이사*. 처음부터 분리했으면 이사 비용 0. ADR-021 결정 사슬에서 *결정 시점이 늦었던 비용*을 체감.

### 3. 학습 깊이 vs 진행 속도 — 이상 vs 현실의 균형

> "학습의 깊이는 그때그때 개념을 정리하면 더 좋았을 것 같음. **근데 이번에는 진행도를 좀 나갔어야 하는 상황이라 불가피했음.**"

→ 이상: 각 Phase 끝날 때마다 개념을 본인 말로 정리.
→ 현실: 캡스톤 1 마감(6/10)이 보이는 상황에서 *진행도 확보*가 우선.
→ **이상 vs 현실의 trade-off를 인식하고 있음** — 면접에서 들리는 가치: "마감 압박 속에서도 *어디까지 타협했고 어디는 안 했는지*를 자각하는 개발자".
→ 후속 액션: M2 들어가기 전 *부족한 개념*은 `/journal:concept`로 별도 보충 (선택). M1 키워드 학습 큐는 "아직 모르는 것" 섹션 참조.

---

## ❓ 아직 모르는 것 / 다음에 배울 것

> M1에서 등장했지만 표면만 본 것. 미래 학습 큐.

**본인 답 (Q6 우선순위)**: "지금 잘 모르겠음" — 우선순위는 비워두고 *학습 큐엔 모두 보존*. M2 가면서 *자연스럽게 만날 때 그때 정리* 방침.

### 학습 큐 (M2 진행 중 또는 별도 시간 시 정리)

| # | 키워드 | 어디서 등장 | 추정 비용 | 권장 액션 |
|---|--------|-------------|----------|----------|
| 1 | **Singleton 패턴 (Unity 변형)** — `static Instance` + Awake 중복 체크 | Phase 05 `SceneTransition.cs` | 30분 | `/journal:concept singleton-pattern-unity` |
| 2 | **`DontDestroyOnLoad`** — 씬 전환 시 GameObject 살아남게 | Phase 05 `SceneTransition.cs` | 15분 | (1번과 같이) |
| 3 | **Coroutine vs async/await** — Unity 메인 스레드 보장 vs C# 표준 비동기 | Phase 05 페이드 로직 | 1시간 | `/journal:concept coroutine-vs-async` (★ 가장 중요) |
| 4 | **`CanvasGroup`** — alpha + blocksRaycasts 한 컴포넌트로 처리 | Phase 05 FadeCanvas | 15분 | 간단, 그때그때 |
| 5 | **`Time.unscaledDeltaTime` vs `deltaTime`** — timeScale 영향 받는지 여부 | Phase 04~05 결정 사슬 | 15분 | `/journal:concept unity-time-scaling` |
| 6 | **Additive Scene Load 패턴** — UI.unity Additive vs Single | ad-hoc UI 씬 분리 (ADR-021) | 30분 | `/journal:concept additive-scene-pattern` (★ 면접 결정타와 연결) |
| 7 | **Unity fake-null vs C# null-conditional (`?.`)** — Roslyn "Null check can be simplified" hint를 *왜 안 따랐는지* | Phase 04 디버깅 등장 | 30분 | `/journal:bug unity-setparent-world-position-stays` 일지 안에 한 섹션으로 박는 게 자연스러움 |

---

## 🎤 면접 시뮬레이션

> 면접관이 M1에 대해 물을 만한 질문 + 본인 답변.

### Q1 (본인이 떠올린 질문): "혼자 안 풀리는 문제 만나면 어떻게 접근해요?"

**A**:
> "MCP / AI 활용해서 문제점을 찾고 해결해나가는데, 여기서 AI가 여러 개의 선택 기준을 주면 **왜 이걸 써야 하고 어떤 게 더 합리적인지** 고려를 해서 진행을 함. **무작정 AI가 추천한 대로 쓰기보다는 한 번 더 생각해보며 사용**."

**왜 이 답이 강한가** (면접 가치):
- *"단순 prompt-and-go 개발자"*가 아니라 *"AI 의견을 평가해서 결정하는 개발자"*로 들림.
- Phase 04 시각 fix 사건(★★★)의 직접적 학습 — 1.5h 추측보다 MCP가 빠르다는 *데이터*를 본인이 직접 체감한 답변이라 신뢰도 높음.
- AI 시대에 면접관이 *진짜 보고 싶은 신호* — "이 사람이 AI에 *과의존*하지 않으면서 *제대로* 쓰는 사람인가". 본인 답이 그 신호를 정확히 박음.

---

## 🔗 관련 링크

- 마일스톤 폴더: [01_Phases/yuhyeon/M1-client-foundations/](../../../../01_Phases/yuhyeon/M1-client-foundations/)
- ADR-021 (UI Scene 분리): `00_Document/ADR/`
- 데모 영상: [demo.mp4](demo.mp4)
- 작업 일지 후보 (★★★): Phase 04 시각 fix → `/journal:bug unity-setparent-world-position-stays`
- 별도 작업 큐:
  - TMP 한글 폰트 도입: [`2026-05-16-tmp-korean-font-todo.md`](../2026-05-16-tmp-korean-font-todo.md)

---

## 작성 메모

- [x] 한 줄 요약 ✓ (Q1)
- [ ] 새로 배운 것 — **"잘 모르겠음" 2회**. 솔직 박힘. 학습 큐로 이관 (Q2 → Q6)
- [x] 결정 포인트 ✓ — 결정 1 (UI 씬 분리, ★★★) 깊이 답변 / 결정 2~3은 본인 선택으로 비움
- [x] 막혔던 지점 본인 회고 ✓ — Phase 04 시각 fix + MCP 학습 통찰 + 회복 자신감 (Q3)
- [x] 다시 한다면 ✓ — 3 영역 (도구 도입 / 순서 / 학습 깊이 vs 진행 속도) (Q5)
- [x] 아직 모르는 것 ✓ — 학습 큐 7개 보존, 우선순위 비움 (Q6)
- [x] 면접 시뮬레이션 ✓ — 본인이 떠올린 질문 + 답변 (Q7, ★ AI 비판적 활용 마인드셋)

### 자가 평가
- **답한 항목**: 6/7 (Q2 개념 차원만 미답)
- **면접 답변 가능 여부**:
  - ✓ 가능: MCP 활용 마인드셋 / 협업 위험 예측 / 마감 vs 학습 trade-off / Phase 04 회복 자신감
  - ✗ 불가: 개념 단위 (Singleton/Coroutine 등 *깊이* 답변)
- **다음에 보강할 것**: Q2 개념 차원. 학습 큐 7개 중 ★ 표시(★ Coroutine / ★ Additive Scene) 우선.
