# 석재 지형·환경물 자체 교체와 움직임7종

상태: **v2와 프레임B 사용자승인으로착수. 정적A/B 원화5회 인수·세션정산 완료, 알파/작은크기 품질판정 보류. 규격 가공 선행시험 정산·봉인 완료, 새 Sol 구현 준비.** 현재전체생성상한15회·지형생성0, 나무2종/천8장·등불6장·작은식생4장·총42frame을생성기가직접그린다. 실제원축척/속도의재생에서끊김이없어야완료다. C는A/B봉인reference뒤시작한다.

## 재개 지점

**다음 첫 일:** 봉인한 선행시험으로 신규 Sol max에 PNG 가공기 구현을 발행한다. 첫 일반 후속 메일의 실제 수신·FIFO ACK를 G0에서 대조한 뒤 제품 코드를 쓰게 한다. A3회/B2회 원본5개는 byte동일 사본·규격·장부를 인수했고 작업자를 정산/종료했다. native 인수 성공을 제품 적합/독립PASS로 세지 않는다. A 꽃다발경계/상자정렬과 B 나무·식생 halo는 actual alpha/작은규격 가공 뒤 신규실사로 판단한다. 새 가공코드는 선행시험→신규Sol→독립Opus 순서이며 실제 crop/봉인reference 뒤 C42frame을 시작한다. Unity/import/실제플레이는 Main 배정 창 전0이다.

기준main `cf207c1f355ced664e62119152e8b0686b0ec051`, branch `feat/stone-environment-replacement-20261025`, checkout `C:/Users/bass1/orca/workspaces/DawnHolder_Project/art-active`. 근거 root는 `.backups/verification/2026-10-28-stone-terrain-environment-replacement/`다. 현재 리드 회신은 `run:run_6ae742140ce9`; 실행 권한은 이번 승인과 현재 Dispatch로 확인하며 옛Task를 재사용하지 않는다.

## 적용 중인 사용자 결정

Main `msg_e035d61029d5`(2026-10-24T22:44:19Z)가 메인pane Enter 제출22:4xZ 사용자 원문을 전달했다. 이pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 계획 검토 - Art 석재 지형·환경물 자체 교체와 움직임 7종 v2 → A 승인 (초안 msg_4d7f93693159)

결정항목 효과는 「메인이 이 답을 그대로 파트에 전달하고, 파트는 이 초안대로 goal을 시작한다.」이다. 승인 원문은 `main-v2-approval-original.json`, 승인범위 원문은 `scope-draft-v2.md`21884B/SHA256 `1265651EC5D9EECA67129EEA18374B778EBADE4B5D63C479ADCF12EE5B8E5058`에 고정한다. 아래는 그 범위의 실행 분할이며 새 기능/품목/예산 추가가 아니다.

- 이전 Main `msg_742782c8674d`: 석재는 기본atlas15+companion1~3의4종. 지형 생성0, 흙/전환칩은 기록 보존·새사용목록 제외, 새벽돌B/추가2회는 닫힘. 움직임7종은 등불·Tree01·Tree02·Bush01·Bush02·Sunflower03·Stall천끝. Wooden7frame 보존은 별도 필수다.
- Main `msg_994f99a0734a`의 사용자C 「프레임이 많아야 자연스러울 것 같은데, 힘드려나?」는 질문이며 v2승인 전의 보충요청이다. 프레임 보충은 `frame-count-cost-supplement-v1.md`16384B/SHA `DE54349931EE72F6B7E2737591986564A34C0293B0A85A10016199317447C217`. 그 시점에는 A4장/13회와 추천B선별42frame/15회 답을 기다렸으며, 뒤의 b6d5가 B를 확정했다. C는 아직 미발행이고 A/B 정적 제작은 승인대로 진행한다.
- Main이 PNG child Art전체1·직전 실제OS6GiB·소유process1GiB표본cap/250ms 조건을 수용했다. Unity/import/플레이는 별도Main창이다. Codex크레딧을10-28까지 아끼지 말라는 사용자지시가 있다고 이번Main이 전달했으며 과거리셋전95%/잔량을 현재값으로 쓰지 않는다. 전역 설정을 바꾸지 않는다.

### 프레임B·끊김 품질 기준 승인

Main msg_b6d5dd816c7a(2026-10-24T22:48:52Z)가 메인pane22:4xZ Enter 사용자답을전달했다.

> 대시보드 결정 응답: 1) Art 환경물 움직임 - 프레임을 모두 4장으로 할지, 잘 보이는 것만 8·6장으로 늘릴지 → B 잘 보이는 것만 늘림

효과문장: 「큰 나무·작은 나무·가판대 천은 8장, 등불은 6장으로 늘리고 풀숲 두 종과 해바라기는 4장으로 둔다. 프레임은 모두 42장이고 그림 생성 상한은 15회로 2회 는다.」 Tree01/Tree02/Stall천8장8fps,등불6장12fps,관목2/꽃4장4fps·직접생성방식이다. 사이frame변형코드/전부8장안은안쓴다. 진폭후보/감축순서와상한은승인보충B그대로며C는봉인reference뒤진행한다.

Main msg_106909d4094e(22:49:43Z)가 메인pane22:5xZ Enter 추가사용자원문을전달했다.

> 추가적으로 너무 끊겨 보여서 퀄리티 낮게만 안보이게 하면 돼

메인해석은 B에추가한품질기준이며상한/프레임수재변경이아니다. C계약/독립판정은정지sheet가아닌실제fps·원래크기의loop재생에서반복이음새튐·색/모양깜빡임·한frame위치이탈·고르지않은박자를실패로본다. Unity반입전7종sheet/fps/시간표와원축척(필요시2배병기)미리보기재료를Main에전달한다. 리드가보고용미리보기를작성하거나Main의보조세션으로재료를인계하며Sol에보고서HTML구현을맡기지않는다. Unity clip시간배치/왕복은움직임에맞게검토하되완성한전체주기8포즈에무조건왕복을덧붙이지않고끝점중복/박자변화를구분한다. 끊김이면진폭/박자교정을먼저하고남으면실제상태/감축을Main에올린다. 약속한8/6을몰래4로줄이거나끊김상태로완료보고0이다. 원문두통은 `main-frame-b-and-quality-original.json`이다. 이pane직접사용자입력으로격상하지않는다.

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

현재상한은정적5source+공용수선2=7회, 움직임5source+수선3=8회, 총15회다. 최초A3/B2만배분하고수선5회와C5회는해당계약전미소비로둔다. 실패/재시도/제출된취소도포함, 호출전kit/회차/잔량·후native경로/bytes/SHA/실제출력/사본을기록한다. kit호출과대상수/프레임수를구분한다. 옛견본3/3·타일2/3잔량1은역사장부·전용0이다. 프레임B의15회상한은Main b6d5가전달한사용자답원문으로적용한다.

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
- [x] 정적 원본 인수
- [>] 규격 가공 준비
- [ ] 정적 독립 검증
- [ ] 움직임 제작
- [ ] 움직임 독립 검증
- [ ] 레벨 반입 실사
- [ ] 검토와 통합
- [ ] 종료 정산

## 결정과 단계 결과

- 최신origin/main을fetch해cf207c1에서새branch를만들었다. 보호2파일과Unity설정3파일의bytes/SHA는전후동일, 사용자untracked초안보존이다. 설정3파일index표시H를관측했으나이번에indexflag를변경하지않았다. 기준/원시는 `branch-start-source-and-protection.json`이다.
- 기존견본goal은branch `feat/level-redesign-art-samples-20261024`의 `0cc0a83071dde13fb6c7cb106f5ebaef7cb5a812`와새근거root의 `previous-sample-goal-at-0cc.md`에보존했다. 이새branch에옛판정/장부/정산을복제해완료처리하지않는다. 기존견본종료정산은별도후속이며전체Art/A-C9종료가아니다.

- A3회/B2회 총5개의 native와 무가공 사본·원시를 인수했다. source A/B의 실제 파일집합·입력12/보호5·원본사본SHA를 리드가 재계산했고 모두일치했다. A48경로/B29경로(own27+native2), 완료보고·release retained external_terminal 뒤 정확한 pane close ptyKilled를 확인했다. 단순원본인수 성공이며 alpha/경계/작은크기품질·제품·독립PASS는 보류다. 원시 `source-a-lead-settlement-intake-v1.json`, `source-b2-lead-settlement-intake-v1.json`, 양쪽 final-close-original.json. 최초B 시작전 취소는 Main5f87 지시대로 abandoned/닫힘·생성0이며 실패횟수나호출로세지않는다.
- A 생존신호는 기존helper로 질문대기191초를 분리하고 일반최대268초/300초초과0을 확인했다(`source-a-liveness-result.json`). B도 같은 helper로 질문 대기278초/27초를 분리했고 일반 최대165초·300초 초과0을 확인했다(`source-b2-liveness-result.json`). API의 B2 시작unknown과 실제native전문/첫hashHB·G0 양성은 구분하며 최종worker-show는completed/settled를관측했다.
- task-local 가공 경계는 `static-packing-design-v1.md`, 쓰기전근거는 `context-packing-dispatch.md`다. 좁은codec변경없이재사용하고 명시crop/최근접/RGBA복사·직접포즈sheet만만든다. Alpha실측/새실행harness는강등급·외부선행시험과독립판정 전통과0, 실제Sprite/Unity완료로표현0이다.

- 패킹 선행시험은 신규 외부 Opus에 발행했고 `worker-start`의 ready/turn_started, 최초 명령·화면, 계약 전문/해시 일치 회신을 관측했다. 상세 Task/Dispatch·경로·실행 소유는 `packing-pretest-worker-start-original.json`과 `resume.json`에 둔다. 테스트와 실행 harness만 작성하며 리드/선행시험 작성자의 제품 구현은0이다.

- 선행시험의 실제 출력 형식 질문에 공식 회신 A로 답해 manifest·실제 alpha 통계·마지막 완료 표식과 입력 거부/기존 결과 보존을 고정했다. 기존 계약과 입력 hash는 바꾸지 않았으며 `packing-output-schema-supplement-v1.json`과 `packing-pretest-output-schema-reply-original.json`이 남은 단계의 보충 근거다.
- 승인된 기존 석재 두 atlas를 현재 goal 근거로 byte 그대로 복사했고 옛 inventory와 SHA가 같았다. 기본15번/companion1~3번의 네 칩과 발판 후보 plan을 준비했으며 생성·decode·실제 crop은0이다. 근거는 `source-stone-snapshot/source-manifest.json`과 `static-packing-plans/stone-terrain-v1.json`이다.
- 실제 serialized source에서 발판은32×26·PPU32·pivot(0,1), 상자는7고유포즈를 사용하는 Open9key/Close8key·Opened포즈5임을 확인했다. 기존 타이밍·기준점 보존에 연결하고 source 줄바꿈을 포함해 대조했다. `platform-serialized-baseline-v1.json`, `chest-serialized-baseline-v1.json`, `asset-integration-handoff-draft-v1.md`는 후속 계약 준비이며 실제 Unity·world·physics 판정이 아니다.

- 실제 선행시험은 두 실행 모두 API 부재의 명시 단언92건 RED·다른 실패/skip0을 기록했다. 검사기 자체점검3건과 실제 CLI 미실행30건을 구분한다. 첫 실행의 표본간격258.7ms 초과는 보존했고 두번째는126.4ms·초과0, child 실제최대RSS71,593,984B·직전최소OS16,587,333,632B였다. 근거 `static-packing-tests/raw/pretest-01/02/run-summary.json`. 이후 완료보고/쓰기 종료와 공식정산을 대조했으며, 독립판정·실제source가공은 아직 미완료다.

- 선행시험의 manifest212파일+목록자체1·실행코드8·입력/보호19pin을 리드가 실제 재계산했고 모두 일치했다. `packing-pretest-lead-mechanical-intake-v1.json`과 `packing-pretest-seal-v1.json`에 봉인했고 작업자를 release 뒤 정확한 pane으로 닫았다. 최종harness는pretest-03/entry-01의실행SHA와같다. 이것은TDD인수이며제품/독립PASS가아니다.
- 이번 첫 수신 처리 사건: 작성자의 「dispatch주소라후속메일이안보임」 설명은 저장한check01~05의동일미ACK delivery 재생과맞지않았다. 원본보고를보존하고 `pretest-mailbox-source-reconciliation-v1.json`으로대조해Main `msg_8af1b9c84499`에즉시알렸다. 중단메일은최종실행뒤늦게읽혔으며소급승인0이다. 후속계약에기존FIFO전량처리/ACK와첫후속실제수신증거를명시한다(`followup-receipt-contract-v1.md`). 새공용가드/Orca변경은하지않는다.

- 선행시험 생존신호는 기존helper의서버시각으로17신호·최대377초·300초초과4구간(377/312/354/365초), 질문대기48초를확인했다. 작성자보고의명령시작시각과구분했고사유는 `packing-pretest-heartbeat-over-gaps-v1.md`, 원시는 `packing-pretest-liveness-result.json`이다. 초과만으로상세대화감사를확대하지않았다.

## 10-19 모델 라우팅 전달 원문

출처: `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-19/routing-decision-entry-block.md`, 근거사본 `current-model-routing-original.md`. 아래 라우팅은 현재 적용하며, 과거 리셋/소모 수치와 기간 제한은 당시 기록이다. 페이싱은 이번 Main e035의10-28까지크레딧을아끼지말라는전달과현재Main배정을우선한다. 전역설정을바꾸지않는다.

# 리드 진입 메시지에 넣을 적용 중 사용자 결정 — 모델 라우팅 개선 구성과 사용량 페이싱

이 파일은 메인이 리드 진입 메시지에 그대로 붙이는 전달 초안이다. 정본이 아니다. 정본 반영은 Rules 리드가 맡고, 반영 전까지는 각 리드가 자기 goal의 「적용 중인 사용자 결정」에 이 블록의 원문과 출처를 기록한다.

## 출처

- 사용자 원문(2026-10-19 메인 대화, Enter로 제출된 입력): 「OK A로 진행하되, 지금 워크트리로 규칙이 사본 형식 느낌으로 분산되어있어서 전부 적용하려면 시간이 좀 걸릴꺼 같은데」
- A의 내용: 현황판 결정 「모델 라우팅 - 개선 구성 적용과 10-25 리셋까지 Codex 크레딧 사용량」의 A 「개선 구성 + 크레딧 절반」. 근거 스크립트 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-19/scripts/update-decision-routing-bundle-1400.cjs`.
- 선례: Sol xhigh 전환처럼 사용자 결정에 따른 런타임 전환은 정본 병합을 기다리지 않는다. 자기 checkout의 AGENTS·ORCA·스킬 사본이 아래와 다르면 이 블록이 우선한다.

## 라우팅

| 역할 | 모델 · 실행 |
|---|---|
| 파트 리드 (Core 포함) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 구현자 일반 | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 위험 신호 (셸·보안·입력 경계, 시간·순서·동시성·수명, 런타임·언어 경계) | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 (같은 계약 두 번째 독립 불합격) | `codex --model gpt-6-astra -c model_reasoning_effort=xhigh` |
| 위험 신호 구현의 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| DB·영속 데이터·프로토콜·공유 DLL·보안 경계 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| 그 밖의 독립 검증 (문서 실사, 약·강 등급, 로컬 DB 설치·기동 도구 카드) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 선행 시험 작성, Gardener | `claude --model claude-opus-5-5` (변화 없음) |
| 확정 실패 3회 뒤 자문, 설계 검토 시범 | `claude-fable-5-1` (변화 없음) |

- 「설계 판단이 남은 새 다파일 기능」 신호와 「애매하면 Astra」 규칙은 쓰지 않는다.
- Sol 구현을 다른 새 Sol 세션이 검증하는 것을 허용한다. 구현자·검증자 분리, 작업 하나 뒤 종료, 독립 검증 생략 금지, 리드 직접 구현 금지는 그대로다.
- 로컬 DB 도구 카드: 사용자 실행 카드에서 독립 검증이 놓친 결함이 한 번이라도 나오면 Opus 검증으로 되돌린다.
- `max`가 첫 기동 화면에 없으면 대체하지 말고 메인에 보고한다. 계약에는 지정 모델·effort와 화면 관측값을 적는다.
- capacity 예외(Sol 30분 실패 뒤 Astra xhigh 새 세션)와 Claude 첫 화면 처리는 그대로다.

## 사용량 페이싱

- 사용자 원문(2026-10-19 14:1x KST, 위 A의 크레딧 부분을 바꿈): 「크레딧은 최대한 아끼고 싶긴한데, 사용량이 부족하면 어쩔 수 없긴 하네 / Codex 사용량 절감은 최대한 플랜 주간 소모량 기준으로 잡고, 크레딧은 비상 상황에서만 쓰는거로 하ㅏ」
- Codex는 플랜 주간 한도만 기준으로 나눠 쓴다. 주간 한도를 하루 약 14%p로 나눈다. 크레딧은 비상 상황에서만 쓴다.
- Codex 주간 한도는 10-19 14:11 기준 95%이고 리셋은 10-25 05:40 KST다. 그 전까지 남은 약 5%(약 1,600 크레딧)를 넘어 쓰면 크레딧이 줄어든다.
- 사용자 원문(2026-10-19 14:2x KST, 비상 사용 결정): 「음 어찌보면 지금이 그 비상상황같은데 일단 크레딧을 쓰느거로 할까, 대신에 라우팅 측정할때는 주간 사용량 기준으로만 체크하는 식으로 가고」
- 그래서 10-25 05:40까지는 크레딧을 비상으로 쓴다. 쓰는 속도는 주간 한도 기준과 같게 맞춘다. 하루 약 14%p, 크레딧으로 약 4,600이다(주간 100% ≈ 3만 2천 크레딧, 10-18 측정 환산). 라우팅의 효과는 이 주간 환산 속도로만 판단한다.
- 상한은 앞서 승인한 18,000 크레딧이다. 쓴 크레딧이 약 16,000에 닿으면 메인이 사용자에게 다시 묻는다.
- 메인이 사용률을 목표선과 비교해 앞서면 우선순위가 낮은 파트부터 새 작업자 기동 보류를 지시하고, 뒤처지면 재개를 지시한다. 리드는 그 지시를 따른다.
- 작업자 세션 하나의 크레딧이 크게 늘면(예: Sol max 구현이 세션당 200을 넘음) 리드가 메인에 알린다.

## 리드가 할 일

- 이 블록의 원문과 출처를 자기 goal.md 「적용 중인 사용자 결정」에 기록한다. 자동 압축 뒤에는 goal.md를 다시 읽고 이 결정을 따른다.
- 작업자·검증자 위임 계약마다 위 표의 지정 모델·effort·실행 명령을 적고, 기동 뒤 화면 관측값을 함께 남긴다. 자기 checkout의 AGENTS·ORCA·스킬 사본과 다르면 이 블록을 따른다. worktree의 규칙 파일은 고치지 않는다.
- 파트 배분: 동시에 두 트랙까지만 연다. 순서는 Core → World 한 트랙, 그다음 Content, 그다음 Art(PR253 병합 뒤)다. Rules·Management는 10-28 뒤다. 메인이 목표선에 따라 보류·재개를 지시한다.
- 2026-10-20 20:00 KST 전에는 새 Opus 세션을 열지 않는다. Opus 검증이 필요한 단계(DB·영속 데이터·프로토콜·공유 DLL·보안 경계, 위험 신호 구현)는 그 뒤로 미루고, 그 전에는 Sol로 끝나는 단계만 진행한다.
