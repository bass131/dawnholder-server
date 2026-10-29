# 기사 Dash 이펙트 시트 새로 만들기

## 재개 지점

### 현재 재개 카드 — 2026-10-30 Unity 창 대기

**다음 첫 일:** 제품 PNG에 피벗 맞춤 시트가 들어갔다(sha256 `e2422227…9e7f2`, 커밋 전). 메인의 Unity 시작 지시(Art 「Unity 사용 끝」 뒤 또는 03:30 KST)를 받으면 Unity 계약 구현자를 띄운다. 복사본 batchmode로 EditMode 전체 1회, 임시 캡처 시험으로 새·지금 시트 캡처 2회를 돈다(메인에 보낸 계획 `msg_737324e02c1c`). 그 전에는 Unity를 띄우지 않는다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 v1을 사용자가 결정 55 A로 승인했고, 아이콘은 결정 56 A(같이 바뀜)다([적용 중인 사용자 결정](#적용-중인-사용자-결정)). 초안 원문은 근거 폴더 `lead/scope-draft-v1.md`(sha256 `79c9f0ea6e5cd66d7cac8721077d7a62a92383747b8d0ee096c467592ee8ec7c`)다. |
| 리드 | `[Effect 리드 Opus]`(claude-opus-5-5 xhigh). Run은 `run_fcbc2441a227`, 회신 주소는 `run:run_fcbc2441a227`다. |
| branch | `feat/knight-dash-effect-sheet-20261029`, main `dc66b77e`에서 시작했다. upstream은 아직 없다. |
| 실행 창 | 이미지 생성은 상한 3회 중 1회를 썼다. 2·3회째는 다시 메인 승인을 받아야 한다. Unity 실행 0회다. Unity 창은 메인 잠정 결정대로 Art 뒤, 늦어도 03:30~05:00 KST다. 예상은 약 25~30분이다(Content 10-28 복사본 첫 실행 8분 36초 기준, 미측정). |

## 적용 중인 사용자 결정

### 10-29 결정 60 A — 사라질 때 창끝이 앞에 남는 움직임을 그대로 둔다 — msg_2a8b7829de40

- 사용자 원문(메인 pane 제출, 메인 전달): 「대시보드 결정 응답: 1) 60 - 새 Dash 이펙트가 사라질 때 창끝이 지금보다 앞에 남는 것을 그대로 둘지 → A 본 그대로 둔다」
- 메인이 보인 A의 뜻: 59에서 본 움직임 그대로 간다. 9칸 전체를 같은 정수 이동(오른쪽 +8px, 아래 +19px)만 해서 피벗 높이·무게 중심을 지금과 3px 안으로 맞춘다. 4번째 칸부터 창끝이 지금보다 0.7~1.1단위 앞에 남는 차이는 그대로 둔다. 직접 플레이에서 어색하면 PR 병합 전에 칸별 이동으로 바꿀 수 있다.
- 사용자가 본 비교 그림: `lead/align-view-01-cells0-4.png`, `lead/align-view-01-cells5-8.png`(메인이 뷰어로 열어 보였다).
- 메인 지시: `align-01-contract-v1.md`(sha256 `fe344c17…6fd0`) 그대로 진행한다. 직접 플레이 확인에 「사라질 때 창끝이 앞에 남는 것이 어색하지 않은지」를 더한다.

### 10-29 사용자 지시 — 피벗·캐릭터 결합 위치 맞춤 — msg_79e59314eaf9

- 사용자 원문(메인 pane 제출, 59 A 뒤, 메인 전달): 「기존에 대쉬 이펙트 시트에 잡혀있는 피벗 위치랑, 캐릭터랑 결합했을때 적용 잘해야 어긋나지 않을꺼야」
- 메인 결정(완료조건에 더함, 사용자 결정 아님):
  1. `.meta`의 pivot·alignment·rect는 바꾸지 않는다.
  2. 지금 시트와 새 시트를 칸마다 저장 스크립트로 잰다. 대상은 불투명 경계 상자, 창끝 x, 몸통 중심선 y다. 칸 가운데 기준 px 차이를 9칸 표로 남긴다. 허용 오차와 그 근거(PPU 100, 화면 픽셀)는 리드가 정한다.
  3. 허용 오차를 넘으면 칸 안에서 평행 이동만 해서 맞춘다. 다시 묻지 않는다. 이동량 표와 고친 시트 SHA256을 남긴다. 크기·모양·색을 바꿔야 맞으면 멈추고 메인에 알린다.
  4. 움직임 미리보기가 실제 앵커·PPU·좌향 뒤집기를 썼는지 적는다. 안 썼으면 실제 앵커 값으로 지금 시트와 새 시트를 겹친 비교 그림(좌·우, 1·3·5·9번째 칸)을 다시 만든다.
  5. Unity 창에서 가능하면 기사+이펙트 캡처를 좌·우 한 장씩 남긴다. 못 하면 이유를 적고, 사용자 직접 플레이 확인에 「Dash 이펙트가 기사와 어긋나지 않는지 좌·우로 본다」를 넣는다.
- 독립 검증 계약에 2~5의 결과를 판정 입력으로 넣는다.

### 10-29 결정 59 A — 시안을 이대로 넣는다 — msg_f537568d022f

- 사용자 원문(메인 pane 제출, 메인 전달): 「대시보드 결정 응답: 1) 59 - 새 기사 Dash 이펙트 시안을 이대로 게임에 넣을지 → A 이대로 넣는다」
- 사용자가 본 것은 `compose-01/preview/sheet-preview.png`(sha256 `6f1ad06a…9e0c`)와 `compose-01/preview/motion-preview.gif`(sha256 `b709a1d4…a3be`)다. 메인이 두 파일과 새 시트(sha256 `5cad4cac…f1cc`)의 SHA256을 다시 대조했다.
- 메인 지시(사용자 결정 아님): 반입하는 시트는 사용자가 본 시트와 화소가 같아야 한다. 무손실 재압축을 하면 풀어낸 화소 대조를 근거로 남긴다. Unity 없이 할 수 있는 일(반입·`.meta` 불변 확인·git diff)은 바로 해도 된다.
- 메인 결정(잠정, 사용자 결정 아님): Art가 01:00~03:30 KST에 Unity 창을 쓴다. Effect batchmode는 Art의 「Unity 사용 끝」 뒤 바로 시작하는 것이 기본이고, 늦어도 03:30 KST(18:30Z)부터 05:00 KST(20:00Z)까지다. 그 전에는 Unity를 띄우지 않는다. 계약에 실행 묶음과 예상 시간을 적어 메인에 보낸다.
- 그 뒤 순서: 독립 검증(신규 Astra, 0.165.0 첫 화면 확인) → PR → 사용자 직접 플레이. 직접 플레이는 Art 플레이 확인과 묶을 수 있게 메인이 잡는다.

### 10-29 결정 57 A — Codex 0.165.0 유지 — msg_df07cfaf946a

- 사용자 원문(메인 pane 제출, 메인 전달): 「대시보드 결정 응답: 1) 57 - 저절로 설치된 Codex 0.165.0을 그대로 쓸지 → A 그대로 쓴다」
- 배경(메인 관측): 10-30 00:07 KST, 시안 Sol pane 첫 화면의 업데이트 선택창에서 아무도 고르지 않은 채 「Update now」가 실행돼 전역 Codex가 0.164.0 → 0.165.0으로 바뀌었다(npm 기록·`~/.codex/version.json`). 원인은 모른다.
- 적용: 새 Codex 세션은 0.165.0이다. 모델이 안 보이거나 지정 effort가 없으면 대체하지 않고 멈춰 보고한다. 되돌리기는 사용자 결정 없이 하지 않는다.

### 10-29 결정 55 A·56 A — 범위 승인과 아이콘 — msg_035ebac4f372

- 사용자 원문(메인 pane 제출, 메인 전달): 「대시보드 결정 응답: 1) 55 - 기사 Dash 이펙트 시트 새로 만들기 범위 초안 v1을 승인할지 → A 이 범위로 시작 · 2) 56 - Dash 스킬칸 아이콘도 새 시트의 첫 칸으로 같이 바꿀지 → A 같이 바뀐다」
- 승인 범위는 `lead/scope-draft-v1.md`(sha256 `79c9f0ea…92ee8ec7c`) 그대로다.
- 메인이 현황판에 보인 55 A의 뜻: 첫 일로 시안 그림을 1회 생성한다. 사용자가 시안(시트 미리보기와 움직임 미리보기)을 본 뒤에만 게임 파일에 넣는다. 이미지 생성은 최대 3회이고 호출마다 메인이 승인한다.
- 56 A의 뜻: 스킬칸(Q) 아이콘이 지금처럼 시트 첫 칸을 쓴다. 시안 확인 때 첫 칸이 아이콘으로 어떻게 보이는지도 같이 본다.
- 메인 결정(사용자 결정 아님): 이미지 생성 1회째(시안)를 승인했다. 입력은 지금 Dash 시트와 우리 게임 그림만이다. 2·3회째는 다시 요청한다. Art 리드에 아이콘 변경을 알린다. PR은 하나로 계획하고, 종료 기록을 같은 PR에 넣는 R-8 예외는 PR 준비 때 메인이 정한다.

### 10-29 결정 54 A — 새 이펙트 리드 — msg_260062c0a95e

- 사용자 원문(메인 pane Enter 제출, 메인 전달): 「추가 병렬 작업으로 기사 Dash 스킬도 새로 한번 이미지 시트 만들어서 적용해줘, 해상도는 비슷하게」
- 결정 54 원문: 「대시보드 결정 응답: 1) 54 - 기사 Dash 스킬 이펙트 시트를 새로 만드는 일을 새 병렬 리드에게 맡길지 → A 새 이펙트 리드를 연다」
- 메인이 보인 A의 뜻: 최신 main에서 새 작업 폴더와 새 Claude 리드를 연다. 리드가 범위 초안(만들 장 수, 이미지 생성 횟수, 하지 않을 것)을 먼저 올린다. 사용자가 승인하면 인벤토리 외관 작업과 나란히 진행한다. Unity 실행 시각은 메인이 두 작업 사이에서 나눈다.
- 사용자는 「피격도」를 붙이지 않았다. 그래서 피격 이펙트 시트는 바꾸지 않는 것이 기본이다(메인 전달).

### 10-28 방향 — 기능 다듬기와 외관

- 원문(메인 전달): 「프로젝트 개발은 이제 기능적으로 만들어진거 더 다듬고 외관작업해야 될 거 같은데」. 최종 제출은 11-06이다.

### 10-27 결정 4A — 리드·구현자 Claude

- 사용자 원문(메인 `msg_260062c0a95e` 전달): 「그리고 오늘 오후 20시에 클로드 사용량 리셋되면 모든 작업 클로드로 진행, 크로스 체크만 GPT Astra로 진행, 작업 진행에 에러사항이 너무 많고 지체되는게 너무 답답한게 이유야, 내가 둘다 비교해보니 작업 완수 능려의 차이가 너무 많이 나」, 답 A(리드까지 전부 Claude).
- 리드·구현자는 `claude --model claude-opus-5-5 --effort xhigh`다. 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 이미지 생성만 신규 Sol max 예외다. 세부는 [CLAUDE.md 4A 절](../../../CLAUDE.md#claude-4a-interim)을 따른다.

## 만들 것

범위 초안 v1의 ①이다. 사용자가 결정 55 A로 승인했다.

- 새 시트 한 장: 3000×560, 600×280 칸 9개(위 5·아래 4). 칸 순서와 재생(12fps, 0.75초)은 지금과 같다.
- 「해상도는 비슷하게」는 시트·칸 크기와 가져오기 설정(PPU 100, bilinear, 최대 2048)을 그대로 두는 것으로 지킨다. 최대 2048 설정 때문에 게임 안에서는 3000 폭이 2048 폭으로 줄어 보이며, 새 시트도 같다.
- 그림은 오른쪽을 향해 앞으로 찌르는 돌진 기운이다. 9칸은 등장 → 가장 큼 → 사라짐으로 이어진다. 지금 기사·이펙트의 2D 픽셀 느낌과 선 굵기를 따르고, 색은 주황·금빛 계열이 기본이다. 모양은 새로 그린다.
- 칸마다 테두리에 여백을 둬서 이웃 칸으로 넘치지 않게 한다.

## 건드릴 곳과 소유

- 제품: `03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/Sprites/Dash/Knight_Dash_Skill_Effect.png`의 그림 내용 한 파일. 구현자가 쓴다.
- 바꾸지 않는다: 같은 PNG의 `.meta`(GUID `1310162cca6e74c4fa3560f52e74fc51`, sprite 이름·fileID·칸 좌표·가져오기 설정), `Skill_Effect/Animator/Dash/Knight_Dash_Skill_Effect.anim`·`.controller`, `Resources/Effects/DashSkill.prefab`.
- 연결 효과: `Prefabs/Characters/ClassVisuals/SkillIconSet.asset`의 skillId 2가 이 시트의 첫 칸을 아이콘으로 쓴다. 결정 56 A로 아이콘도 새 첫 칸으로 같이 바뀐다. 이 asset 자체는 바꾸지 않는다.
- 리드: 이 goal, `00_Document/operations/CURRENT.md`의 Effect 줄, 근거 폴더. Art와 겹치는 파일은 CURRENT.md뿐이고 각자 자기 줄만 쓴다.

## 하지 않을 것

- 피격 시트(`Knight_Dash_Hit_Effect.png`)와 `DashHit.prefab`.
- 서버 Dash 판정(`02_Server/GameServer/Maps/Actions/DashAction.cs`)과 봇 시나리오(`99_Tools/headless-bot/Scenarios/Skill/DashSmoke.cs`).
- 다른 직업·스킬 이펙트, 이펙트 코드(`SkillCastHandler`·`EffectSpawnService`), 가져오기 설정 변경.
- 다른 게임의 그림·아이콘·로고·화면 캡처를 입력이나 참고 이미지로 쓰는 일. 지금 Dash 시트와 우리 게임 그림은 입력으로 쓸 수 있다.
- 전역 설정 변경, 자동 병합.

## 관찰 가능한 완료조건

1. 새 PNG가 3000×560 RGBA다. 9칸 모두 그림이 있고, 칸 테두리 1px의 알파가 0이다. 저장 스크립트로 잰다.
2. `.meta`·anim·controller·prefab·아이콘 목록의 변경이 0이다(아이콘 A일 때). git diff로 확인한다.
3. Unity 가져오기 오류가 0이고, 스킬칸 아이콘을 읽는 기존 EditMode 시험(`HudPresentationTests`)이 통과한다.
4. 적용 전에 사용자가 시안(시트 미리보기와 기사 옆 움직임 미리보기)을 확인한다.
5. 적용 뒤 사용자가 직접 플레이로 Dash를 좌우로 써 본다. 볼 것은 두 가지다. 하나는 Dash 이펙트가 기사와 어긋나지 않는지다(메인 `msg_79e59314eaf9`). 다른 하나는 사라질 때 창끝이 앞에 남는 것이 어색하지 않은지다(결정 60 A).
6. 신규 Astra 독립 검증이 PASS를 낸다.
7. 새 시트의 칸별 그림 위치가 지금 시트와 가중 평균 3px 안에서 맞는다(창 축 높이, 무게 중심). `.meta` 피벗은 그대로다(메인 `msg_79e59314eaf9`로 더함).

그림 품질은 시험으로 판정할 수 없다. 4·5의 눈 확인으로 대신한다.

## PR 경계와 작업 방식

- PR 하나를 권한다. 새 PNG와 goal 결과 기록이다. 종료 기록까지 같은 PR에 넣을지는 R-8 head 고정 예외에 대한 메인 판단을 따른다.
- 검증 등급은 약이다. 코드 변경이 0이고, 강 영역에 닿지 않는 제품 파일 1개 변경이라서다. 실제 diff가 나오면 다시 확인한다.
- TDD는 코드 변경이 없어 대상이 아니다. 대체 실측은 완료조건 1~5다.
- R-7 시범은 해당 없음이다. 메인이 고른 시범 목표가 아니다.
- 작업자 순서: 신규 Sol max(시안 생성) → 신규 Opus(투명 처리·시트 조립·미리보기, 근거 폴더만 씀) → 사용자 시안 확인 → 신규 Opus(제품 PNG 반입·Unity 확인) → 신규 Astra(독립 검증). 작업자·검증자는 작업 하나 뒤 닫고, 검증자는 동시에 하나만 연다.
- 병합은 사용자의 head 지정 승인 뒤 메인이 한다.

## 실행 창과 사용자 손

- 사용자 손은 두 번이다. 시안 확인과 적용 뒤 직접 플레이다.
- 이미지 생성: 신규 Sol max(`codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`). 상한 3회(시안 1, 다시 그리기 2). 호출마다 쓰기 전에 메인 승인을 받는다. 결과 파일은 리드가 실제 경로(`~/.codex/generated_images/<세션>/`)와 SHA256을 확인한다.
- Unity: batchmode 두 번(적용 확인 약 45~75분, 독립 검증 약 15~20분)이고 미측정 예상이다. 이 worktree에는 Library가 없어 첫 실행에 전체 가져오기가 들어간다. 시각은 메인에 요청한다. 관문은 실행 직전 물리·commit 여유 각 4GiB, Unity·Windows dotnet 0이다. ProjectSettings의 SENTIS 한 줄이 지워지면 보존·복원하고 커밋하지 않는다.

## 다음 계획 후보

- `DashSkill.prefab`의 기본 sprite fileID `3027720894737066909`가 지금 `.meta` 목록에 없다. Animator가 첫 칸을 덮어쓰므로 화면 영향은 확인하지 않았다. 이 goal에서는 고치지 않는다.

## 진척 단계

- [x] 범위 승인
- [x] 시안 생성
- [x] 시트 조립
- [x] 사용자 시안 확인
- [>] 제품 반입·Unity(반입·피벗 맞춤 끝, Unity 창 대기)
- [ ] 독립 검증
- [ ] 사용자 플레이 확인
- [ ] 제품 PR 병합
- [ ] 종료 기록

## 단계 결과와 근거

근거 폴더는 `.backups/verification/2026-10-29-knight-dash-effect-sheet/`다. Git 제외 폴더라 다른 checkout에도 있다고 가정하지 않는다.

### 리드 진입 — 2026-10-29

- 메인 진입 지시는 `msg_260062c0a95e`(14:49:13Z)다. Run `run_fcbc2441a227`를 14:52:17Z에 만들었다(`lead/run-create-01.json`). READY는 `msg_4395b627f4b8`, 범위 초안 v1은 `msg_a1c7fc494547`다.
- 맥락 메모는 `lead/lead-context-memo.md`다. 대상 실측은 `lead/measure-dash-assets-01.json`(스크립트 `lead/measure-dash-assets-01.cjs`)이다.
- 사용자가 결정 55 A·56 A로 범위와 아이콘을 정했다(메인 `msg_035ebac4f372`, 15:03:14Z). 메인은 같은 메시지로 이미지 생성 1회째를 승인했다.
- Art 리드에 아이콘 변경을 알렸다(`msg_48080b09b77f`). 답은 필요 없다.

### 시안 생성 — 2026-10-29

- 계약은 근거 폴더 `mockup-01-contract-v1.md`(sha256 `01b5affd2d3c2f21e1a1bc4176749f1d857ff476633a61b1606423345e13d057`)다. 입력은 지금 Dash 시트, 기사 공격 시트, 피격 시트 세 장이고 SHA256은 `lead/mockup-input-pins-01.txt`에 있다. 경로 확인은 `lead/mockup-01-path-check-01.txt`다.
- 15:06:54Z에 Sol max pane(term_9b78a284…)을 열었다. 첫 화면에서 Codex가 스스로 `npm install -g @openai/codex` 업데이트를 돌렸다. 리드는 입력하지 않았다. 업데이트는 성공했고 `codex-cli 0.165.0`이 됐다. 옛 설치 폴더 `.codex-8JHtRCys`는 EPERM으로 남았다. 메인에 보고했다(`msg_d2954c880d02`, `msg_f8a58e4d5ea4`). 원시는 `lead/mockup-sol-*`·`lead/codex-version-after-update-01.txt`다.
- 메인 결정(`msg_67659c133d6d`)대로 빈 셸 pane을 닫고 15:09:03Z에 새 Sol pane을 열었다. 첫 화면은 「GPT-6.1-Sol max」, 선택창 없음, Fast 표시 없음이다. 「permissions: YOLO mode」는 기존 전역 설정 표시라고 메인이 확인했다(`msg_b2435d265ac9`). 기동 기록은 `lead/mockup-01-launch-record-01.md`다.
- 작업자 `[Effect Sol]`(task_257adfbea20f)이 image_gen을 1회 불렀다(상한 3회 중 1회째). worker_done은 `msg_3c568fa3e555`다. 결과는 `mockup-01/dash-mockup-01.png`, 1536×1024 RGB, sha256 `d48502401bc0df15309e020c7b59e26d79246548137308d3a50928b2b4b43e4b`다. 원본은 `~/.codex/generated_images/01a18835-4f10-7f43-9727-6e4902d784b1/`에 있고 SHA256이 같다. 이미지 모델 이름은 unknown이다.
- 리드 대조는 `lead/mockup-01-lead-acceptance-01.md`다. 9칸 모두 칸 경계에 닿지 않았다. 생존 신호 한 구간이 330초였다(생성 뒤 복사·측정 중, 공식 질문 대기 아님). 작업자는 정산 뒤 pane을 닫았다.

### 시트 조립·미리보기 — 2026-10-29

- 계약은 근거 폴더 `compose-01-contract-v1.md`(sha256 `fb34f8c7aa9a6b1954fbf41fa106786a1ab225fe2bd03ed6fabd25285885671e`)다. 규칙 원문은 `compose-01-rules-v1.md`다. 기동 기록은 `lead/compose-01-launch-record-01.md`다.
- 작업자 `[Effect 구현 Opus]`(task_9cf28932f262)의 worker_done은 `msg_ecdc48116c06`(15:36:21Z)이고 보고는 `compose-01/report.md`다. 바탕 잡음 T = 4, 칸 경계 닿음 0칸, 배율 s = 480/460 = 1.043478이다. 창끝 높이는 9칸 모두 139.9~140.1이다.
- 새 시트는 `compose-01/Knight_Dash_Skill_Effect.png`, 3000×560 RGBA, 1,187,670 bytes, sha256 `5cad4cac427a8253f137d4cd44f502c257934c48ccb5fc46c15d98ed2e71f1cc`다. 미리보기는 `compose-01/preview/sheet-preview.png`(sha256 `6f1ad06a9fbbaee9c2f54500b0203ec2a2938db95d3766c7864c560fd4ba9e0c`)와 `compose-01/preview/motion-preview.gif`(sha256 `b709a1d4318a74d3d11eb1ef8e8b54231bdf2fe2c4c4bf2f062909a148fda3be`)다.
- 리드 대조는 `lead/compose-01-lead-acceptance-01.md`다. 리드 스크립트 `lead/verify-compose-01.cjs`로 다시 쟀다. 세 SHA256이 보고와 같고, 9칸에 그림이 있으며 테두리 위반은 0이다. 게임 시트는 그대로다. 대화 기록의 model 값은 모두 `claude-opus-5-5`다.
- 새 시트는 지금 시트의 약 2.5배 크기다. 반입 때 화소를 바꾸지 않는 무손실 재압축을 하고 풀어낸 화소를 대조한다.
- 작업자는 정산(retained, external_terminal) 뒤 pane을 닫았다. 이미지 생성은 이 단계에서 0회다.
- 사용자가 결정 59 A로 시안을 승인했다(`msg_f537568d022f`).

### 제품 반입과 피벗 맞춤 — 2026-10-29

- 반입 1차: 계약 `apply-01-contract-v1.md`(sha256 `2391aab6…966a`)다. 구현자(task_eed535fae9de) worker_done은 `msg_b3761fd282b2`다. 무손실 재압축으로 1,187,670 → 893,075 bytes가 됐고, 풀어낸 화소가 사용자가 본 시트와 같다. 리드 대조는 `lead/apply-01-lead-acceptance-01.md`다.
- 피벗 측정: 사용자 지시(`msg_79e59314eaf9`)에 따라 리드가 쟀다(`lead/measure-alignment-01.json`). 지금 시트는 칸마다 그림 경계 상자 가운데를 피벗에 맞춘 시트다. 새 시트는 가중 평균으로 창 축이 18.66px 위, 무게 중심이 8.06px 왼쪽이었다. 허용 오차는 가중 차이 3px다(PPU 100, 카메라 orthographic size 5에서 1080p 약 1.08 화면 px/시트 px).
- 이동: 9칸 같은 정수 이동 x +8, y +19다(`lead/plan-alignment-01.json`). 사용자가 결정 60 A로 칸별 차이(4번째 칸부터 창끝이 앞에 남음)를 그대로 두기로 했다.
- 피벗 맞춤: 계약 `align-01-contract-v1.md`(sha256 `fe344c17…6fd0`)다. 구현자(task_5412c4caefd3) worker_done은 `msg_e709a3be98fc`이고, 보고는 `align-01/report.md`다.
  - 제품 PNG는 893,510 bytes, sha256 `e24222278f76754b1c74362d38010a80a8f7ff70da12b013d7b0b3e461c9e7f2`다.
  - 이동 뒤 가중 차이는 창 축 0.34px, 무게 중심 0.06px이다.
  - 비교 그림은 `align-01/preview/anchor-overlay-right.png`·`anchor-overlay-left.png`, 비교 움직임은 `align-01/preview/motion-compare.gif`다.
- 리드 대조는 `lead/align-01-lead-acceptance-01.md`다. 10칸 모두 사용자가 본 시트를 (8, 19) 옮긴 것과 바이트로 같고, 불변 9개 파일은 그대로다.
- 남은 것: Unity 가져오기·EditMode 시험·Unity 캡처(메인이 정한 창), 독립 검증, 사용자 직접 플레이다.
