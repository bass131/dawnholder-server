---
summary: MainMenu 씬 + Canvas + TextMeshPro로 "Dawnholder — Welcome" 텍스트 표시 (Unity 6.4 환경 검증 완료, M1 첫 Phase)
phase: 01-hello-ui-bootstrap
work-id: phase01-hello-ui-bootstrap
status: done
completed_at: 2026-05-16
commit: 99a51ce
---

# Phase 01 — Hello UI 부트스트랩 완료 박제

**소요 시간**: 약 1시간 (계획 1~1.5시간, 예측 적중)

## TL;DR

Unity 6.4 LTS 2D 템플릿에서 MainMenu 씬을 신설하고 Canvas + TextMeshPro로 "Dawnholder — Welcome" 텍스트 1개를 화면 중앙에 표시했다. Play 모드 정상 동작 + Build Profiles 등록 + Console Error 0으로 환경 검증 완료. M1-client-foundations 마일스톤의 가장 작은 시작점으로, 후속 Phase가 얹힐 UI 토대 + Unity 워크플로우 첫 손맛 확보.

## 5단계 보고

- **무엇을 만들었나** — `Assets/Scenes/MainMenu.unity` 신설 (2D 템플릿), Canvas (Screen Space Overlay) + EventSystem + TextMeshPro 텍스트("Dawnholder — Welcome", Anchor 중앙 배치). Main Camera Background Type = Solid Color (검정)로 변경. Build Profiles의 Scene List에 MainMenu 등록 (index 0). TMP Essentials 자동 임포트(3.9MB).
- **왜 필요한가** — M1 마일스톤은 *UI 토대*. 후속 5개 Phase(메뉴 버튼 → HUD → 일시정지 → 페이드 → Regression)가 얹힐 *작동하는 씬*이 일단 있어야 함. 또한 학부생 + Unity 첫 손맛 단계라 "Hello World 수준" 환경 검증이 가장 작은 단위로 적합.
- **어떻게 만들었나** — Unity Editor에서 File → New Scene (2D Built-in) → 저장 → GameObject UI 메뉴로 Canvas/EventSystem/TMP Text 추가 → Inspector에서 Anchor·Position 중앙 설정 → File → Build Profiles에서 Scene List에 MainMenu 추가 후 index 0 드래그.
- **테스트 결과** — Play 모드 진입 시 검은 배경 + 흰 텍스트 중앙 표시 확인. Console Error 0개 (노란 Warning은 Unity Cloud Account API timeout — 본 작업과 무관). git status로 5개 파일 그룹(씬, .meta, TMP 폴더, EditorBuildSettings) 변경 잡힘.
- **다음 스텝** — Phase 02 (메인 메뉴 버튼): "시작" / "종료" 버튼 + 클릭 핸들러로 첫 *상호작용* UI. `Scripts/UI/MainMenuController.cs` 신설.

## AC 검증 결과

Phase 01 완료조건 4개를 실제 검증한 결과:

| # | 완료조건 | 검증 방법 | 결과 |
|---|---------|----------|------|
| 1 | Editor에서 MainMenu 씬 열면 텍스트 보임 | Scene 뷰 + Game 뷰 모두 확인 | ✓ "Dawnholder — Welcome" 중앙 표시 |
| 2 | Play 누르면 같은 화면 유지 | Editor 상단 ▶ 클릭 후 Game 뷰 관찰 | ✓ 검은 배경 + 흰 텍스트 유지 |
| 3 | Console Error 0개 | Console 창의 Error 카운트 확인 | ✓ Error 0 (Warning 4개 — Unity Cloud Account API timeout, 무관) |
| 4 | git에 변경 잡힘 | `git status --short --branch` | ✓ Modified 1 + Untracked 4 그룹 잡힘 |

추가 검증:
```bash
$ git log -1 --format="%h %s"
99a51ce feat(yuhyeon): Phase 01 — MainMenu 씬 + Hello UI 환경 검증
$ git show --stat 99a51ce | tail -3
 75 files changed, 60609 insertions(+)
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Background Type Uninitialized → Solid Color (검정)**: URP 2D 기본값이 Uninitialized라 노란 fallback 표시 → 깔끔한 검은 배경 위해 명시적 Solid Color. 학습: URP 카메라 클리어 동작.
- **Build Settings → Build Profiles (Unity 6 변경)**: Claude가 옛 메뉴명 안내 → 사용자가 "없는데?" 정정 → Unity 6에서 *Build Profiles*로 이름 바뀐 것 확인. 학습: Unity 메이저 버전 업데이트 추적.
- **Gizmos Game 뷰 표시**: Camera Gizmo가 Game 뷰에 비쳐 작은 캐릭터 형상 보임 → Game 뷰 Gizmos 토글 off로 해결. 학습: Editor 시각 보조 vs 빌드 화면 분리.
- **TMP Essentials 임포트 commit 여부**: 3.9MB 폴더 → commit 결정 (다른 팀원도 필요한 자산). 학습: Unity 패키지 첫 임포트 시 산출물 관리.

## 막혔던 지점 (있다면)

- **메뉴 이름 옛것 안내 받음** (Build Settings → 실제는 Build Profiles). Unity 6 변경 사항 미리 인지 못 함. 사용자가 즉시 정정해서 30초 안에 회복.
- **노란 배경 + 작은 캐릭터** (Play 시): Background Type Uninitialized 기본값 + Gizmos toggle 양쪽 원인. Claude가 두 단계로 안내 → 해결.

## 학습 일지 후보 키워드

추후 `/journal:concept`로 본인이 펼칠 만한 단서들:

- **Unity UI 시스템**: Canvas / EventSystem / TextMeshPro 관계
- **Canvas Render Mode 3가지**: Screen Space Overlay vs Camera vs World Space 선택 기준
- **Anchor & Pivot**: "부착점" vs "회전점" 직관
- **URP Camera Background Type**: Skybox / Solid Color / Uninitialized 동작 차이
- **Build Profiles (Unity 6)**: 옛 Build Settings → 다중 프로파일 관리로 진화
- **Editor Gizmos vs Game 뷰**: 빌드 화면과 Editor 시각 보조 구분
- **Unity 패키지 첫 임포트**: TMP Essentials 같은 자동 임포트 자산의 commit 정책
