---
owner: youngho
summary: M6 플레이테스트 폴리시 완료 — Animator/렌더소팅/HUD/NPC대화 4 Phase + 통합 플레이테스트 8라운드(보스·골렘·파티UI·미니맵·퀘스트배너 시트애니·엔딩 정적). 프로토콜 무변경(v16), main 머지 준비.
phase: 05-integration-playtest-closeout
work-id: m6-playtest-polish
status: done
completed_at: 2026-06-15
commit: 9b9ddb4+후속(클로즈아웃)
grade: 대규모
---

# M6 — 플레이테스트 폴리시 완료 박제

**소요 시간**: 2026-06-15 (1일, 다회 플레이테스트 반복)
**시각화**: `_milestone-DONE.html`

## TL;DR

M5(#111, v16) 머지 후 2차 플레이테스트에서 영호가 발견한 체감 품질 6건을 4 Phase로 1차 구현하고, Phase 05 통합 플레이테스트에서 8라운드에 걸쳐 보스/골렘 거동·파티UI·미니맵·퀘스트 UI 크래시를 수정. 퀘스트 발생/완료는 AI 생성 배너 스프라이트시트 애니로, 엔딩은 정적 고화질 이미지 + 아무키 탈출로 마감. 신규 기능 0, 프로토콜 무변경(v16). WSL2 게이트 645/0/5 green.

## 5단계 보고

- **무엇을 만들었나** — (P01) Boss/Slime/Golem Animator 전이 재구축, (P02) 렌더 소팅 5레이어 + RenderLayers.cs, (P03) 상단 HUD 재배치 + 퀘스트 3줄, (P04) NPC 대화패널 TMP 재구축, (P05) 통합 플레이테스트 8R: 보스 텔레그래프 피격취소·골렘 windup/교차재스폰·파티UI 즉시표시·미니맵 컬러dot+단색지형·퀘스트UI 크래시 2건·퀘스트 발생/완료 배너 AI 시트 애니·엔딩 정적 이미지+아무키 탈출.
- **왜 필요한가** — M5에서 기능(파티/퀘스트)은 들어갔으나 거동·렌더·UI 표현이 어색. 밸런스/프로토콜이 아닌 표현 레이어 보정이라 한 마일스톤으로 묶음.
- **어떻게 만들었나** — Unity 에셋(Animator/SortingLayer/Scene)은 메인 MCP RunCommand로 1차 조정 + 영호 육안. 코드는 client 도메인 수정. 퀘스트 배너 = AI 생성 시트 → 프로그래밍 크로마키(`m=min(R,B)−G`+de-spill) → deprecated `spritesheet` 4×4 슬라이스 → 프레임사이클 코루틴(QuestAlert 텍스트→배너Image). 엔딩 = AI 시트 애니 실험 후 정적 단일 이미지 채택(런타임 배경 애니 코드 폐기). 프로토콜 무변경(퀘스트 텍스트=클라 로컬 콘텐츠, 헌법 §1).
- **테스트 결과** — WSL2 회귀 645 passed / 0 failed / 5 skipped(build 0err). Unity 컴파일 0 error(매 커밋 link-check). 영호 8라운드 직접 Play 육안 검증. 알려진 flaky=CombatSmoke(격리 pass).
- **다음 스텝** — M7 사운드(계획 사전분해 완료). future polish=QuestGrantedPopup 텍스트→시트. 영호 직접=디스코드 v16 공지, MainMenu/배너 등 M6 외 변경분 커밋.

## AC 검증 결과

Phase 파일 완료조건 = 6 피드백 항목 통합 검증 + WSL2 회귀 green + 컴파일 0err.

```bash
# WSL2 .NET 회귀 게이트 (최종 머지 상태)
$ wsl -d Ubuntu -- bash -lc "... dotnet build && dotnet test Dawnholder.slnx --no-build"
  Build: 0 Error(s)
  Passed!  - Failed: 0, Passed: 645, Skipped: 5, Total: 650, Duration: 1m51s

# Unity 컴파일 + 런타임 에셋 상태 (MCP RunCommand)
  EndingController OK | UI/Ending=0(시트 폐기) | QuestAvailable=16 QuestClear=16

# 브랜치 머지 가능성
$ git rev-list --count origin/main..HEAD   # 19 (클로즈아웃 커밋으로 +1)
$ git rev-list --count HEAD..origin/main   # 0  (origin/main 5e563ab, 충돌 없음)
```

- ✅ C/D(보스·일반몹 모션 완주) — P01 Animator 재구축, 영호 Play 확인
- ✅ E(렌더 소팅) — P02 5레이어, 캐릭터>지형
- ✅ A/B(파티 HUD 재배치 + 퀘스트 3줄) — P03
- ✅ F(NPC 대화패널) — P04 TMP + 초상화
- ✅ 플레이테스트 8R 전 항목 영호 육안 OK

## 결정 흐름 (회고 참고용)

- **퀘스트 텍스트 출처**: 서버 패킷 확장(v bump) vs 클라 로컬 → **클라 로컬**(단일 퀘스트, 프로토콜 무변경, 헌법 §1).
- **퀘스트 배너 애니 재생**: AnimationClip+Animator vs 프레임사이클 코루틴 → **코루틴**(QuestAlert가 런타임 빌드라 프리팹/컨트롤러 마찰 0).
- **엔딩 연출**: 스프라이트시트 애니 vs 영상(VideoPlayer) vs 정적 → **정적 고화질 이미지**(풀스크린에서 작은 인물 애니가 어색, 영호 판단으로 정적이 품질 우위).
- **스프라이트 슬라이스**: 정식 ISpriteEditorDataProvider vs deprecated spritesheet → **deprecated**(정식 API가 MCP RunCommand에서 user-interaction 차단).

## 막혔던 지점

- **퀘스트 UI 전멸(5R→6R)**: 증상=첫 헌팅그라운드 진입 시 퀘스트 UI 전부 미표시. 5R에 타이밍 race로 *오진* → 영호 지시로 에디터 콘솔 로그 확인 → 진짜 원인 2건(QuestAlert outline-before-font 예외로 코루틴 사망 + PartyMemberHud GetComponent?? fake-null) 확정·수정. 교훈=UI "다 안 보임"은 race 추측 X, Play+콘솔 예외부터.
- **maxTextureSize Standalone 클램프**: 슬라이스 rect가 텍스처 밖으로 나감 → Standalone 플랫폼설정 2048이 끼어들어 텍스처 절반잘림 → default+Standalone 8192 + overridden=false로 해소.
- **AI 업스케일 in-place**: Unity UpscaleImage가 prompt 못 받고 savePath 무시하고 원본 덮어씀 → 백업 필수 확인.

## 학습 일지 후보 키워드

- Unity Animator Exit Time / Has Exit Time / Transition Duration
- 2D SortingLayer vs Order in Layer
- BuildRuntime(코드 UI 생성) vs Scene 수동 배치
- `GetComponent ?? AddComponent` fake-null 함정 / TryGetComponent
- TMP outline은 font/머티리얼 할당 후
- 프로그래밍 크로마키(`m=min(R,B)−G`) + de-spill
- MCP 스프라이트 슬라이스(deprecated spritesheet) / maxTextureSize 플랫폼 클램프
- 이미지 업스케일: 생성형(디테일 추가, 손상) vs bicubic(충실, 디테일 0) — 천장=소스 해상도
- 런타임 UI 진단: Play + 콘솔 예외 우선(타이밍 race 추측 금지)
