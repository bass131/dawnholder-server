# 석재 지형·환경물 자체 교체와 움직임7종

상태: **v2 사용자A 승인으로 착수. 정적 A/B 원화source 계약 준비, 움직임 프레임 답 대기.** 현재 전체 이미지 상한13회, 지형 새생성0이다. 선별8/6장·15회안은 아직 승인되지 않았다.

## 재개 지점

**다음 첫 일:** 신규 Sol max A/B에 원화source만 좁게 발행한다. A=S1등불/석상·S2가판대/중첩소품·S5Wooden상자7frame의3회, B=S3나무2·S4관목2/꽃의2회다. 모델/경로/공식Task·Dispatch·계약 전문/첫신호·G0를 대조한 뒤 승인 범위의 최초 source5회를 진행한다. 새 가공 코드는 선행시험 뒤 별도 외부 구현하며 원화 생성과 구분한다. C 계약은 사용자 프레임 답과 A/B의 봉인reference 뒤 고정한다. Unity/import/실제플레이는 Main 배정 창 전0이다.

기준main `cf207c1f355ced664e62119152e8b0686b0ec051`, branch `feat/stone-environment-replacement-20261025`, checkout `C:/Users/bass1/orca/workspaces/DawnHolder_Project/art-active`. 근거 root는 `.backups/verification/2026-10-28-stone-terrain-environment-replacement/`다. 현재 리드 회신은 `run:run_6ae742140ce9`; 실행 권한은 이번 승인과 현재 Dispatch로 확인하며 옛Task를 재사용하지 않는다.

## 적용 중인 사용자 결정

Main `msg_e035d61029d5`(2026-10-24T22:44:19Z)가 메인pane Enter 제출22:4xZ 사용자 원문을 전달했다. 이pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 계획 검토 - Art 석재 지형·환경물 자체 교체와 움직임 7종 v2 → A 승인 (초안 msg_4d7f93693159)

결정항목 효과는 「메인이 이 답을 그대로 파트에 전달하고, 파트는 이 초안대로 goal을 시작한다.」이다. 승인 원문은 `main-v2-approval-original.json`, 승인범위 원문은 `scope-draft-v2.md`21884B/SHA256 `1265651EC5D9EECA67129EEA18374B778EBADE4B5D63C479ADCF12EE5B8E5058`에 고정한다. 아래는 그 범위의 실행 분할이며 새 기능/품목/예산 추가가 아니다.

- 이전 Main `msg_742782c8674d`: 석재는 기본atlas15+companion1~3의4종. 지형 생성0, 흙/전환칩은 기록 보존·새사용목록 제외, 새벽돌B/추가2회는 닫힘. 움직임7종은 등불·Tree01·Tree02·Bush01·Bush02·Sunflower03·Stall천끝. Wooden7frame 보존은 별도 필수다.
- Main `msg_994f99a0734a`의 사용자C 「프레임이 많아야 자연스러울 것 같은데, 힘드려나?」는 질문이며 v2승인 전의 보충요청이다. 프레임 보충은 `frame-count-cost-supplement-v1.md`16384B/SHA `DE54349931EE72F6B7E2737591986564A34C0293B0A85A10016199317447C217`. 현재 A4장/13회와 추천B선별42frame/15회 중 답 대기다. 프레임 의존 C는 미발행, A/B 정적 제작은 승인대로 진행한다.
- Main이 PNG child Art전체1·직전 실제OS6GiB·소유process1GiB표본cap/250ms 조건을 수용했다. Unity/import/플레이는 별도Main창이다. Codex크레딧을10-28까지 아끼지 말라는 사용자지시가 있다고 이번Main이 전달했으며 과거리셋전95%/잔량을 현재값으로 쓰지 않는다. 전역 설정을 바꾸지 않는다.

## 범위

| 구분 | 승인 내용 |
|---|---|
| 만들 것 | stone-only4종 에셋/사용표, 승인17유형의 자체 정적/상태 그림, 움직임7종sheet·새시각prefab/clip, source/native·가공·호출장부·GUID/규격 이관표, Level후속가시배치와 실제검증·독립판정. |
| 건드릴 곳 | 신규 `03_Client/Assets/Art/Environment/Terrain/Stone/`, `Structure/Shared/`, `Vegetation/` 후보의PNG/meta·새시각자산과 규격문서. 실제제품경로는 가공/반입 계약 전 고정한다. 이번source A/B는 근거root의각owner와도구관리native원본만쓰며 Assets0이다. Level은 자기후속계약/branch에서scene/Tile참조·배치를단독작성한다. |
| 하지 않을 것 | Cainos원본/meta/GUID 덮기·삭제·이동, 기존촬영맵 변경, 새벽돌생성, 보류5유형/미사용장식 제작, 보존건물/배경/캐릭터/NPC/효과/UI/출정문 재제작, 충돌/서버판정/one-way/가시피해/시소물리/상자보상 변경, 새공용도구/가드·전역설정·자동병합. |
| 관찰 가능한 완료조건 | 승인17유형의선택부품/상태가자체그림에대응, 새가시지형흙/전환0. 후속승인배치의7loop를실제관측하고frame0·크기/PPU/anchor·geometry/가림/상태를검증한다. 실제physics/TilemapCollider·Bake와기존파일/GUID·촬영맵/보호를보존하고신규외부독립판정을받는다. 미실행/감축/비선택잔존을분리하며모든패키지참조제거라하지않는다. |

## 제작집합과 소유

총17유형은 최상위9(Statue·Stall·RoadLamp·Tree01/02·Bush01/02·Sunflower03·ChestWooden)+Stall중첩7(Kettle·GrainBox·CrateSmall·WhiteBottle·Basket·WineBottle·Cauldron)+발판그림1이다. Stall의천막/기둥/탁자/자루/과일4종과Lamp의기둥/지지대/체인/등on/off는하위부품까지대응한다. Basket2인스턴스는1유형이다. PlatformPlank wrapper는외부Props그림을참조하므로기존석재에서생성0으로시각대체한다.

LogBench·Sign01·Sign02·Spike·Seesaw01은LevelPR1비활성/7종밖이라보류한다. Banner/Torch/FireBowl/Rock/Fence도0이다. 기존건물2장은개별출처미확정인보존대상이며자체라고단정하지않는다. source출처·라이선스/구매미확정·실제GUID/fileID·PPU/rect는 `scene-provenance-facts-v1.json`에고정한다.

- A/B/C는역할분리다. 최초source는A3회/B2회로독립계약·서로다른owner. 이단계는native원화와공간사본/실제metadata까지만만들며PNG재가공·제품반입코드0이다.
- 새crop/정렬/packing코드가필요하면예술에셋의TDD예외와구분해새Opus선행시험→새Sol구현→새독립검증으로발행한다. 같은source세션을다른작업에재사용하지않고해당역할의신규후속작업자로연결한다. raw source성공을정적제품완료로세지않는다.
- C는프레임답과봉인reference뒤시작. A/B의봉인참조를다시쓰지않고수정은새버전/새경로로인계한다. 파일/쓰기종료·검증소유권을작업별로명시하고worker추가위임/commit/push0이다.

## 예산·자원·모델

현재상한은정적5source+공용수선2=7회, 움직임4source+수선2=6회, 총13회다. 최초A3/B2만배분하고수선4회와C4회는해당계약전미소비로둔다. 실패/재시도/제출된취소도포함, 호출전kit/회차/잔량·후native경로/bytes/SHA/실제출력/사본을기록한다. kit호출과대상수/프레임수를구분한다. 옛견본3/3·타일2/3잔량1은역사장부·전용0이다. 프레임B의15회상한은답원문전적용하지않는다.

실행은Art PNGchild전체1·직전availableOS≥6442450944B·process별1GiB표본cap/250ms. 표본을hard상한으로쓰지않는다. 소유불명/관측불가/기준미달은시작0·원시보존·공식ask다. cloud요청/텍스트와로컬PNG를구분하고Unity/import/플레이/WSL/dotnet/DB/build/7777은Main배정전0. Main감시PID22928공지는허용pattern자동확장아니다.

모델은10-19적용사용자결정원문이낡은checkout사본보다우선한다. 리드Solxhigh, 구현A/B/C=`codex --model gpt-6.1-sol -c model_reasoning_effort=max`; 위험가공/Unity포함독립실사=`claude --model claude-opus-5-5 --autocompact 250000`, 그밖의독립문서/일반정적은Solxhigh. 선행시험/Gardener Opus, 같은계약두번째독립실패뒤Astraxhigh·확정실패3회뒤Advisor는정본대로다. 요청/명령/화면/native설정/backendunknown을나눠기록한다. 작업당신규세션·정산종료·재사용0, Art검증자동시1. R-7은Main이선정한시범목표가아니므로이번source계약에는해당없으며새공용비용/보호도구를구현하지않는다.

## Level·Unity 보존과 검증

Level `msg_6aa90b381a87`의currentPR1은기존Statue/Stall만가시배치하고나머지많은장식을비활성화한다. v2의새시각장식6개+Stall천후속배치안은이번승인에포함됐으며Main이Level에원문을전달한다. 최종좌표/sorting과Level계약은Level소유, 현재PR1고정입력은조용히바꾸지않는다. 원문layout/PPU/Tile/sceneSHA는근거root의source사본/manifest로연결한다. worldrenderer실측은아직없고source rect/PPU계산으로대체하지않는다.

같은GUIDPNG교체대신새PNG/meta/GUID·새시각자산을선택이관한다. 원본/기존경로·GUID·문자열·serialized값을보존하고바꾸는참조는전후대응표에명시한다. 신규6장식은Collider/trigger/게임상태0, 기존NPC/Enemy/portal이동0이다. Wooden상자7frame/상태·clip은미선택상자3종과구분해보존한다.

Ground_1/_4는32px·PPU32·ColliderType1(Sprite)·physicsShape[]/fallback이다. 새알파그림은같은meta빈배열만으로충돌보존을보장하지않는다. 배정Unity창의기존실제shape원시→신규역할별정확이식/독립TilemapCollider·서버Bake대조를반입관문으로둔다. 불가하면반입차단/Main방법판단, ColliderType을Grid로바꿔성공처리0. PlatformPlank0·one-way윗면/셀점유도실행결과로보존한다.

검증은강등급(입출력/보호·비용·PNG가공/Unity경계)이며코드전후줄수는실제diff가나오면기록한다. source만의원화는선행실패시험이의미없는이유를계약에남기고subject/누락/alpha/native경로·실측규격/시각으로대체한다. 실제가공·제품·Unity는새독립세션의실제진입/테스트·source보존/내용/가독성을판정한다. 맥락/규칙원문/실제Task연결부재·측정위장·복제기대값은차단, 제품결함은번호로반환한다. 수행하지않은게임/DB/physics를PASS로보고하지않는다.

## PR·일정·마감

1. Art 정적/기존상태PR: 신규PNG/meta·규격/이관표, 독립실사. 지금source원화만으로열지않는다.
2. Art 움직임PR: 프레임결정뒤7종sheet/clip·독립실사. 정적PR의고정입력/head에의존한다.
3. Level후속참조/scenePR: Level단일작성·해당실제검증. 사용자exacthead승인/Main전용병합, Codex병합/자동예약0이다.

10-26은기존맵촬영, 실제촬영종료통지전제품main병합0. 10-25원화/규격준비→10-26로컬제작/독립실사와촬영→10-27반입/loop실제검증→10-28확정범위정산을계획하며미실측납기를보장하지않는다. 감축은해바라기→관목2→작은나무→천→큰나무→등불, 큰나무/등불은끝까지남긴다. 필수정적/상자/충돌/GUID/독립검증을줄이지않고미달은Main에올린다.

## 진척 단계

- [x] 승인 범위 고정
- [>] 정적 원화 제작
- [ ] 규격 가공 준비
- [ ] 정적 독립 검증
- [ ] 움직임 제작
- [ ] 움직임 독립 검증
- [ ] 레벨 반입 실사
- [ ] 검토와 통합
- [ ] 종료 정산

## 결정과 단계 결과

- 최신origin/main을fetch해cf207c1에서새branch를만들었다. 보호2파일과Unity설정3파일의bytes/SHA는전후동일, 사용자untracked초안보존이다. 설정3파일index표시H를관측했으나이번에indexflag를변경하지않았다. 기준/원시는 `branch-start-source-and-protection.json`이다.
- 기존견본goal은branch `feat/level-redesign-art-samples-20261024`의 `0cc0a83071dde13fb6c7cb106f5ebaef7cb5a812`와새근거root의 `previous-sample-goal-at-0cc.md`에보존했다. 이새branch에옛판정/장부/정산을복제해완료처리하지않는다. 기존견본종료정산은별도후속이며전체Art/A-C9종료가아니다.
