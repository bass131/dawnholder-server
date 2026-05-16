---
summary: MainMenu 씬에 Start/Quit 버튼 + UIController/MainMenuController로 클릭 시 Gameplay 씬 로드 / 에디터 정지 동작 (Phase 02, 첫 상호작용 UI)
phase: 02-main-menu-buttons
work-id: phase02-main-menu-buttons
status: done
completed_at: 2026-05-16
commit: b28acb8
---

# Phase 02 — 메인 메뉴 버튼 완료 박제

**소요 시간**: 약 2시간 (계획 1.5~2시간, 한글 폰트 사건 + Text vs Button 메뉴 혼동으로 상한선)

## TL;DR

MainMenu 씬에 ButtonGroup (Vertical Layout Group) + Start/Quit 버튼 2개 + UIController GameObject (MainMenuController 스크립트 부착)를 추가하고 Inspector 드래그로 onClick 이벤트 연결. Start → SceneManager로 Gameplay 씬 로드, Quit → `#if UNITY_EDITOR` 분기로 에디터/빌드 양쪽 종료. 헌법 #1(Server Authority) 준수 — 버튼은 *씬 전환*만 트리거하고 권위 상태 변경 0. Phase 진행 중 한글 폰트 누락 사건 + Text-vs-Button 메뉴 혼동 두 가지 학부생 함정 학습 박제 (별 작업 큐잉).

## 5단계 보고

- **무엇을 만들었나** — `MainMenu.unity`에 ButtonGroup(Pos Y -250, Width 300, Vertical Layout Group Padding 20 Spacing 10) + StartButton/QuitButton(Width 250, Height 60, Button - TMP) + UIController GameObject (MainMenuController 부착, gameplaySceneName="Gameplay"). `Scripts/UI/MainMenuController.cs` 신설 (Dawnholder.Client.UI 네임스페이스, OnStartClicked/OnQuitClicked 두 메서드).
- **왜 필요한가** — Phase 01 Hello UI가 *정적 표시*였다면 Phase 02는 *첫 상호작용*. 사용자 입력 → 씬 전환의 가장 간단한 흐름을 클라 영역에서 구현. 후속 Phase 03~05가 이 위에 HUD/일시정지/씬 전환 폴리시 얹음.
- **어떻게 만들었나** — Unity Editor에서 GameObject UI 메뉴로 Vertical Layout Group 박스 + Button - TMP 2개 추가 → Scripts/UI 폴더 신설 + MainMenuController.cs 작성 → UIController GameObject 만들고 스크립트 컴포넌트 부착 → 각 Button의 Inspector OnClick() 섹션에서 UIController 드래그 + Function 드롭다운으로 OnStartClicked/OnQuitClicked 선택.
- **테스트 결과** — Play 모드에서 4가지 완료조건 모두 통과: 시작 클릭 → Gameplay 씬 전환 ✓, 종료 클릭 → 에디터 Play 정지 + Console에 `[MainMenu] Quit clicked` 로그 ✓, 버튼 호버 시 색상 변화 ✓, Console 빨간 Error 0개 (노란 Account API warning은 Unity Cloud 무관). 씬 파일 검증으로 Button 컴포넌트 + onClick PersistentCalls(MainMenuController.OnStartClicked / OnQuitClicked) 정상 직렬화 확인.
- **다음 스텝** — Phase 03 (HUD 골격): Gameplay 씬에 HUD Canvas + HP바 + 자원 텍스트 + 미니맵 자리잡이 (mock 값). HudController.cs 신설.

## AC 검증 결과

Phase 02 완료조건 4개를 실제 검증한 결과:

| # | 완료조건 | 검증 방법 | 결과 |
|---|---------|----------|------|
| 1 | 시작 → Gameplay 씬 전환 | Play 모드 Start 클릭 → Hierarchy가 MainMenu → Gameplay로 바뀜 | ✓ 통과 (사용자 보고) |
| 2 | 종료 → 에디터 정지 | MainMenu 다시 열고 Play → Quit 클릭 | ✓ 통과 (사용자 보고) |
| 3 | 호버 시각 피드백 | 마우스 호버 시 Button 색상 변화 | ✓ Unity Button 기본 ColorBlock 작동 |
| 4 | Console Error 0 | Play 검증 후 Console 확인 | ✓ Error 0 (Account API warning만) |

추가 검증 (씬 파일 grep으로):
```bash
$ grep -c "Button:\|m_OnClick\|m_Sprite" MainMenu.unity
6   # 2개 Button × (Button + OnClick + Sprite) = 6
$ grep "m_MethodName" MainMenu.unity
OnQuitClicked
OnStartClicked
$ grep "gameplaySceneName" MainMenu.unity
gameplaySceneName: Gameplay
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Text - TMP 잘못 선택 사건**: 처음에 "UI → Text - TextMeshPro"를 선택해서 *글자만* 표시되는 GameObject가 만들어짐 (Button 컴포넌트 없음). 진단: 씬 파일 grep으로 컴포넌트 3개(RectTransform + CanvasRenderer + TextMeshProUGUI)만 박혀있음 확인 → 정상 Button은 Image + Button + 자식 Text까지 필요. 해결: Delete 후 "UI → Button - TextMeshPro" 재생성.
- **TMP 한글 폰트 누락**: `시작` / `종료` 입력 시 □□ 표시 + Console에 `작/종/료 was not found in LiberationSans SDF` 경고. LiberationSans SDF (TMP Essentials 기본)는 Latin만 지원. 결정: 헌법 "scope 늘면 새 Phase" 원칙대로 임시 영문(Start/Quit)으로 Phase 02 진행하고 한글 폰트 도입은 별 작업으로 큐잉 (todo stub 박제).
- **씬 저장 누락 사건**: Inspector에서 Pos Y를 -250으로 변경했지만 Ctrl+S 안 함 → 씬 파일에는 -100 잔존. 제가 파일 직접 읽어서 발견 → 사용자에게 "씬 저장 필수" 강조. 학습: Unity의 Inspector 변경과 디스크 저장 분리.
- **Inspector 드래그 vs 코드 AddListener**: Inspector 드래그 채택 (학부생 친화, 시각적). 단점은 코드만 봐선 *어디서 호출되는지* 안 보임 — 디버깅 시 씬 열어야 함. 코드 AddListener는 동적 생성 시 필수 (Phase 04+에서 도입 검토).

## 막혔던 지점 (있다면)

- **메뉴 항목 혼동** (Text - TMP vs Button - TMP): Unity Hierarchy 우클릭 → UI 메뉴에 비슷한 이름이 있어 학부생이 잘못 선택할 가능성 매우 높음. 진단을 씬 파일 직접 읽기로 5초 안에 완료 (.unity는 YAML 텍스트).
- **한글 폰트 누락**: TMP Essentials 기본 폰트가 Latin only인지 사전 인지 못함. 다음 마일스톤에서 Noto Sans CJK Korean 도입 예정.
- **씬 미저장 + Inspector 값 불일치**: Inspector에 값은 보이는데 파일은 옛 상태. *제가* 파일 읽어서 발견했지만 사용자가 혼자였으면 더 헤맸을 시나리오.

## 학습 일지 후보 키워드

추후 `/journal:concept` 또는 `/journal:bug`로 본인이 펼칠 만한 단서들:

- **Unity Button vs TextMeshPro 컴포넌트 구조**: Button = Image + Button(MonoBehaviour) + 자식 Text. Text = 단독 TextMeshProUGUI.
- **Vertical Layout Group**: Padding / Spacing / Child Alignment / Control Child Size 토글 의미
- **Button.onClick PersistentCalls**: Inspector 드래그 = 직렬화된 UnityEvent 호출. m_TargetAssemblyTypeName으로 어셈블리 추적
- **`#if UNITY_EDITOR` 컴파일러 분기**: 에디터/빌드 양쪽 분기 처리
- **SerializeField로 private 필드 Inspector 노출**: 캡슐화 + 시각 편집 양립
- **씬(.unity) YAML 구조**: GameObject + 컴포넌트 fileID 참조 흐름. AI가 파일 읽어 진단 가능
- **TMP Font Asset Creator**: SDF Atlas 생성, Unicode Range Hex 입력 (한글 AC00-D7A3)
- **헌법 "scope 늘면 새 Phase" 적용**: 한글 폰트 사건을 Phase 02 안에서 해결하지 않고 stub 박제 + 별 작업 큐잉 — 마일스톤 페이스 유지 패턴
