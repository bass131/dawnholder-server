# 애니메이션 시트 제작 방법 비교

[10-28 시연 아트 goal](goal.md)의 첫 산출물이다. 사용자 질문은 「대장장이 같은 NPC의 새 동작 시트를 GPT 이미지 생성으로 만들 수 있는지, Higgsfield나 Unity AI 토큰이 필요한지, 원하는 결과까지 드는 재시도 비용은 얼마인지」였다([적용 중인 사용자 결정](goal.md#적용-중인-사용자-결정)의 계획 승인 코멘트).

- 작성: Art 리드. 웹 자료의 확인 날짜는 모두 2026-10-13이다.
- 표시: 「공식」은 벤더 문서·약관, 「커뮤니티」는 포럼·보도·경쟁사 글, 「측정」은 이 저장소나 이 goal에서 잰 값, 「추론」은 리드 판단이다.
- 웹 자료는 읽기 전용 조사 에이전트가 모았다. 리드가 원문을 직접 연 것은 Unity 스프라이트 시트 문서 하나다. 나머지는 조사 보고를 옮긴 것이다. openai.com과 help.openai.com은 직접 접근이 막혀 검색 요약을 거쳤다.

## 결론

새로 만들 시트는 대장장이 「망치질」 하나다. 제작 순서는 ① 구독에 포함된 GPT 이미지 생성 무료 시범(생성 세 번 상한) → ② 시범이 실패하면 Unity AI 스프라이트 시트(사용자 승인과 비용 상한 뒤) → ③ Higgsfield다. 셋 다 쓰지 못하면 저장소에 이미 있는 대장장이 「대화」 시트로 대신한다. 무료 시범은 한 번 생성으로 후처리하면 쓸 수 있는 초안을 얻었다. 대기 동작과의 색 차이는 색 맞춤으로 대조군 가까이 줄었다. 사용자는 이 결과를 다듬어 대장장이의 기본 동작으로 쓰기로 정했다. 대장간 앞에서는 늘 망치질하고 대기로 바꾸지 않으므로, 전환 때의 자세 튐과 색 차이가 영상에 나오지 않는다(메인 `msg_720a56af6bf1`, [무료 시범 결과](#무료-시범-결과)). 그래서 Unity AI·Higgsfield와 대화 시트 대체는 쓰지 않는다.

팀원 ComfyUI는 후보에서 뺐다. 사용자가 직접 써 본 결과 실용성이 낮았고, 발표용 명목으로 넣은 후보였다고 답했다(메인 `msg_48ddc03797c6`).

## 실제로 필요한 애니메이션

| 장면 | 필요한 움직임 | 지금 있는 것 | 결론 |
|---|---|---|---|
| 거점 성장 | 레벨업 연출 | 스테이지 클리어 연출 방식 | 셰이더·입자로 해결, 시트 불필요 |
| 대장간 대화 | 대장장이 상호작용 | 대기 동작만 컨트롤러가 쓴다. 「대화」 시트는 있지만 애니메이션으로 묶이지 않았다 | 기존 시트를 잘라 묶기만 한다 |
| 강화 성공 | 대장장이 망치질 | 없음 | 새로 만들 유일한 시트(16프레임) |
| 강화 성공 | 불꽃·번쩍임 | 없음 | 셰이더·입자로 해결 |

기존 시트 규격(측정, `03_Client/Assets/Art/Characters/NPC/BlackSmith/`의 PNG 헤더와 `.meta`): 동작 하나에 투명 배경 PNG 한 장(대기 2048x1646), 4x4 격자 16프레임, 칸 512x411, 피벗은 칸 아래 가운데, bilinear 필터, 1단위당 180픽셀(Pixels Per Unit). 외곽선이 진한 픽셀 아트풍을 고해상도로 그린 그림이다(리드 눈 확인).

## 후보 비교

| 기준 | GPT 이미지 생성(Codex 내장) | Unity AI 스프라이트 시트 | Higgsfield |
|---|---|---|---|
| 프레임 일관성 | 참조 그림 여러 장을 받는다. 공식 안내가 반복 캐릭터의 일관성 실패 가능성을 적는다(공식). 한 장에 4x4를 요청하면 시트가 불완전하거나 포즈가 반복된다는 보고가 있다(커뮤니티, 직전 모델 기준). 최신판 기준 자료는 미확인이다 | 참조 스프라이트를 첫 프레임으로 5초 영상을 만들고, 균등 간격 16프레임을 4x4로 배열한다(공식, 리드 직접 확인). 기존 시트 형식과 같다. 문서 예시가 「캐릭터 동작 또는 회전하는 물체」라서 망치질 품질은 미확인이다 | 시트 전용 기능은 미확인이다. 영상을 만든 뒤 프레임을 뽑는 방식이 된다(추론). 외형 고정 기능은 실사 인물 사진 20장 이상을 학습하는 방식이라 그림 NPC에 맞는지 미확인이다(공식) |
| 재시도 비용 | 추가 결제는 없다. Codex 포함 한도를 평균 3~5배 빨리 쓰고 초과분은 크레딧으로 간다(공식). Sol 작업 한도와 겹친다 | 크레딧을 쓴다. 개인 요금제는 체험 1,000크레딧 뒤 월 10달러다. 행동당 추정치는 이미지 1~6, 영상 5~15크레딧이며 예고 없이 바뀔 수 있다(공식). 시트 한 번의 단가는 미확인이다(생성 버튼에 표시) | 크레딧을 쓴다. 공식 가격 페이지는 읽지 못했다. 경쟁사 글 기준 월 19~129달러, 영상 5초당 10~23크레딧이다(커뮤니티) |
| 시간 | 복잡한 요청은 장당 최대 약 2분(공식) | 미확인 | 이미지 수 초, 8초 영상 약 1.5~4분(공식, 업체 자체 측정) |
| 사용 조건 | 출력은 사용자 소유(공식 약관, 검색 요약 경유) | 입출력은 사용자 소유이고 파트너 모델 약관이 함께 적용된다. 문서는 생성물을 출시 전에 바꿀 임시물로 안내한다(공식) | 소유를 주장하지 않고 상업 사용을 제한하지 않는다. 다만 입력·출력을 모델 학습에 쓸 수 있다(공식 약관) |
| 이 프로젝트에서 | Sol 작업자가 바로 쓴다. 요청 크기를 지키지 않는다(측정) | 패키지 `com.unity.ai.assistant` 2.20.0-pre.2가 이미 설치돼 있고, Unity 6000.6.4f1은 요구 버전(6.3 이상)을 넘는다(측정·커뮤니티). Unity 실행 권한 한 자리와 겹친다 | 새 계정·결제가 필요하다 |

- 사용자가 말한 「GPT Image 2.5」는 실제 이름이다. 2026-09-08에 나온 ChatGPT Images 2.5이고 직전 모델은 gpt-image-2다(공식·보도). 다만 Codex 내장 도구는 실제 모델 이름을 밝히지 않는다(측정).
- 예전 「Unity Points」는 지금 「Unity AI Credits」다(커뮤니티, Unity 직원 글).
- 사용자가 걱정한 Unity AI의 무작위성은 공식 자료로 확인하지 못했다. 다만 문서가 생성물을 임시물로 안내하고, 단가가 행동당 추정치라는 점은 재시도 비용이 미리 정해지지 않는다는 뜻이다(추론). 그래서 유료 생성은 승인 때 비용 상한을 함께 정한다.

### 지난 발표 그림에서 잰 Codex 이미지 생성의 성질

원천은 저장소 밖 `C:/Dev/DawnHolder_Dashboard/plans/midterm-1013/art/SOURCES.md`(2026-10-10)다.

- 생성 도구가 실제 이미지 모델 이름을 밝히지 않았다.
- 요청 크기를 지키지 않았다(256x256 → 1254x1254, 1600x500 → 2243x701). 4x4 격자를 맞추려면 생성 뒤 자르고 정렬하는 후처리가 필요하다.
- 투명 배경은 요청대로 나왔다(외곽 alpha 0).

## 무료 시범 결과

2026-10-13에 한 번 생성하고 멈췄다. Sol 작업자가 계약의 네 조건(16칸, 투명 배경, 같은 인물, 동작 순서)을 첫 시도에서 관찰해 남은 두 번을 쓰지 않았다. 아래 값은 리드가 원본을 직접 열어 잰 것이다(측정). 시범 그림과 측정 원시는 Git 밖 근거 폴더 `.backups/verification/2026-10-13-demo-video-art/`의 `sheet-trial/`(Sol)과 `lead-analysis/`(리드)에 있다.

| 항목 | 시범 결과 | 기존 대기 시트 |
|---|---|---|
| 생성 횟수·시간 | 1회, 호출 전후 시계 간격 94초 | — |
| 참고 그림 | 대기 시트·전신 원화·초상화를 도구 입력으로 첨부 | — |
| 이미지 모델 이름 | 미노출 | — |
| 크기 | 1399x1124 RGBA(요청 2048x1644) | 2048x1646 |
| 배경 | 네 모서리 alpha 0. 인물 상자 밖 반투명 점 24,848개(최대 alpha 125) | 상자 밖 1,352개 |
| 인물 분리 | 서로 닿지 않는 16명 | 16명 |
| 격자 | 줄 간격이 고르지 않다. 발 높이가 y=276·585·852·1111이다(균등 격자라면 약 276·557·838·1119). 균등하게 자르면 둘째 줄 발이 셋째 칸으로 넘어간다 | 같은 줄의 발 높이가 같고 줄 간격이 고르다 |
| 인물 키 | 똑바로 선 자세가 첫 줄 241~242px, 넷째 줄 232~233px로 약 4% 차이 | 336~340px |
| 색 분포: 같은 시트 첫 프레임과 | 0.89~0.96 | 0.90~0.95 |
| 색 분포: 바로 다음 프레임과 | 0.88~0.96 | 0.93~0.99 |
| 색 분포: 대기 시트 첫 프레임과 | 0.75~0.81 | 같은 너비로 줄인 대기 시트 0.89~0.95 |

- 색 분포 값은 불투명 픽셀의 색을 채널당 16단계로 나눈 히스토그램의 겹침(histogram intersection)이다. 1이면 분포가 같다. 대기 시트를 시범 그림 너비로 줄여 같은 계산을 한 대조군이 0.89~0.95이므로, 크기 차이를 빼고도 색이 대기 시트와 어긋난다.
- **후처리**: 인물마다 붙어 있는 픽셀 덩어리를 찾아 발 가운데를 칸 아래 가운데에 맞췄다. 16명 모두 350x320 칸 안에 들어갔다(측정). 상자 밖 반투명 점은 이 단계에서 떨어진다.
- **리드 눈 확인**:
  - 얼굴·수염·옷·벨트 공구·망치가 모든 칸에서 같은 인물로 보인다. 앞치마와 피부는 대기 시트보다 붉고, 망치 머리는 조금 크다.
  - 동작 순서는 들어 올림(1~4) → 머리 위(5~6) → 내려침(7~8) → 허리 높이(9~10) → 반동(11~12) → 준비 자세(14~16)다.
  - 13번째 프레임에서 망치가 어깨 위로 다시 올라가서 반복 재생 때 한 번 튄다. 5~6번째는 몸이 정면을 본다.
  - 16번째와 1번째는 거의 같은 자세라 반복 이음매가 자연스럽다(색 분포 0.96).
- **화면 크기**:
  - 마을 장면 `Town.unity`의 메인 카메라는 orthographic size 4라서 화면 세로가 8단위다(측정, 장면 파일).
  - 대기 시트는 1단위당 180픽셀(Pixels Per Unit)이다. 대장장이 NPC는 부모가 없고 배율이 1이다(측정, meta·장면 파일).
  - 따라서 1080p 화면에서 대장장이 키는 약 254px다(339 ÷ 180 × 1080 ÷ 8). 메인이 계산했고 리드가 같은 값을 확인했다.
  - 시범 그림은 1단위당 약 128픽셀로 두면 대기 동작과 같은 크기가 된다. 이때 242px 그림을 약 1.05배로 그린다. 대기 그림은 오히려 약 0.75배로 줄여 그린다.
  - 그래서 1080p에서는 해상도 차이가 거의 드러나지 않는다(추론). 반면 인물이 화면 높이의 약 24%라서 색 차이는 보일 수 있다(추론).
  - 실행 중에 카메라를 확대하지 않는다고 가정했다. 게임 화면에서는 확인하지 않았다.
- **색 맞춤(시험)**:
  - 재정렬한 시트의 R·G·B 채널을 각각 대기 시트의 색 분포에 맞췄다(채널별 histogram matching). 불투명 픽셀만 기준으로 삼았고 투명도는 그대로 뒀다.
  - 대기 시트 첫 프레임과의 색 분포 겹침이 0.751~0.812에서 0.867~0.908로 올라 대조군(0.89~0.95)에 가까워졌다(측정).
  - 시트 안 일관성은 크게 변하지 않았다. 같은 시트 첫 프레임과는 0.888~0.964에서 0.892~0.955로, 바로 다음 프레임과는 0.883~0.964에서 0.866~0.954로 바뀌었다(측정).
  - 눈으로 보면 피부와 앞치마의 붉은 기가 빠져 대기 동작과 같은 톤이 됐다. 16칸 모두 튀는 색이 없다. 망치 머리의 보랏빛 회색은 원본에도 있다.
  - 결과는 `lead-analysis/attempt-1-colormatch/realigned-matched.png`(SHA256 `f56df4ea…`)다. 나란히 비교는 같은 폴더 `pair-idle0-original0-matched0.png`(왼쪽부터 대기·원본·색 맞춤)다.
- **미리보기**(12fps 반복 GIF): `lead-analysis/attempt-1-realign/analyze/preview.gif`(16프레임), 같은 폴더 `preview-drop13.gif`(13번째를 뺀 15프레임), 색 맞춤한 15프레임 `lead-analysis/attempt-1-colormatch/analyze/preview-drop13.gif`, 기준 `lead-analysis/baseline-idle/preview.gif`.
- 하지 않음: Unity 가져오기, 게임 화면 확인.

## 출처

| 후보 | 주장 | 분류 | 출처 |
|---|---|---|---|
| GPT | GPT Image 2.5(ChatGPT Images 2.5) 2026-09-08 출시, API 모델 gpt-image-2.5-flare·sunburst | 공식·보도 | https://developers.openai.com/api/docs/models/gpt-image-2.5-flare , https://9to5mac.com/2026/09/08/openai-releases-chatgpt-images-2-5-with-sharper-details-and-more-precise-editing/ |
| GPT | 직전 모델 gpt-image-2(2026-04-21) | 공식 | https://developers.openai.com/api/docs/models/gpt-image-2 |
| GPT | 참조 이미지 여러 장 입력, 반복 캐릭터 일관성 실패 가능성, 복잡한 요청 최대 약 2분 | 공식 | https://developers.openai.com/api/docs/guides/image-generation |
| GPT | 한 장 4x4 시트의 불완전·포즈 반복 보고(gpt-image-2 기준) | 커뮤니티 | https://community.openai.com/t/developing-sprite-sheets-with-gpt-image-2/1379831 |
| GPT | Codex 이미지 생성은 포함 한도를 평균 3~5배 빨리 씀, 초과분 크레딧 | 공식 | https://learn.chatgpt.com/en-US/docs/pricing |
| GPT | API 이미지 출력 토큰 단가 | 공식 | https://developers.openai.com/api/docs/pricing |
| GPT | 출력은 사용자 소유 | 공식(검색 요약 경유) | https://openai.com/policies/terms-of-use |
| Higgsfield | Soul ID는 사진 20장 이상 학습 | 공식 | https://higgsfield.ai/blog/consistent-characters-locations |
| Higgsfield | Popcorn 참조 4장·최대 8프레임 | 공식 | https://higgsfield.ai/creator-hub/help-center/ai-models/how-do-i-use-popcorn |
| Higgsfield | 요금제·크레딧 단가(경쟁사 글) | 커뮤니티 | https://krea.ai/blog/higgsfield-pricing-explained-2026-unlimited-credits-and-real-monthly-costs |
| Higgsfield | 생성 시간 자체 측정 | 공식 | https://higgsfield.ai/blog/how-long-ai-video-generation-takes |
| Higgsfield | 소유 주장 없음·상업 사용 허용·입출력 학습 사용 가능·크레딧 이월 없음 | 공식(일부 검색 요약 경유) | https://higgsfield.ai/terms-of-use-agreement |
| Higgsfield | 유료 플랜 워터마크 없음 | 공식 | https://higgsfield.ai/creator-hub/help-center/account-and-privacy/who-owns-my-generations-and-can-i-use-them-commercially |
| Unity AI | 첫 프레임 → 5초 영상 → 균등 16프레임 4x4, 예시 「character action or a rotating object」 | 공식, 리드 직접 확인 | https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.1/manual/Sprite/spritesheet-overview.html |
| Unity AI | Unity Points → Unity AI Credits, 새 베타는 Unity 6.3 이상 | 커뮤니티(Unity 직원 글) | https://discussions.unity.com/t/unity-ai-beta-2026-is-here/1703625 |
| Unity AI | 배경 제거 권장, 생성물은 출시 전 교체할 임시물 안내 | 공식 | https://unity.com/blog/unity-ai-sprite-generator |
| Unity AI | 스프라이트 모델 파트너(Scenario·Layer) | 공식 | https://unity.com/legal/third-party-ai-terms |
| Unity AI | 개인 요금제 체험 1,000크레딧 뒤 월 10달러 | 공식 | https://unity.com/resources/what-is-unity-ai |
| Unity AI | 행동당 크레딧 추정치, 예고 없는 변경 가능 | 공식 | https://docs.unity.com/en-us/ai/credits/credits-about |
| Unity AI | 입출력 사용자 소유, 권리 확인 책임은 사용자 | 공식 | https://activation.unity3d.com/kr/legal/unityai-models-partners |
| Unity AI | Scenario 생성물 소유·상업 사용 허용 | 공식 | https://scenario.com/terms-and-conditions |
