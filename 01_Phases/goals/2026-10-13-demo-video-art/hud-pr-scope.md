# HUD PR 실행 범위

기준 main/시작 SHA db0169350dfe54ba6ed247684caae4e498e24c72, branch feat/demo-video-art-hud-20261018. 원천은 goal의범위/완료조건1/기존HUD다시입힘 결정 msg_82ee7240714a, 사용자A이관 msg_b39571d7f7c9, 메인병합·진행 msg_3ed580c79335다. 새goal/새완료조건면제아니다.

## 결과와 보존

- 그림: Assets/Art/UI/HUD의 Minimap_Frame.png·Status_Frame.png·Skill_Slot.png·Quest_Panel.png·Goldbar.png 다섯장만같은승인B1조각으로다시입힌다. 원canvas크기/atlas좌표·name/internalID/spriteID·.meta전체·GUID·PPU·pivot·씬/프리팹참조·런타임크기·입력/게임상태는보존한다. 기존HUD를새스타일참고로쓰지않고geometry/기능보존확인만한다. source B1은PR252로main에들어온Components PNG33/import-spec이다. 새imagegen/콘셉트잔여2회사용0,새제작대신승인조각재사용·조립이다.
- 제품HUD코드/배치/씬·기존33조각·다른자체그림은수정하지않는다. 실제VIS-05에서제품결함이면번호로반환하고이경계를임의로넓히지않는다. 새출처README/필요meta는그림적용계약에명시하고기존sprite meta는그대로둔다.
- 실제확인①PartyHUD VIS-05: 실제UI씬/Town·서버party wire수신·표시/탈퇴·올바른시점의동적raycast와두해상도가필요하다. 코드/geometry/CanvasGroup속성만으로관측을대체하지않는다.
- 실제확인②인벤토리/NPC/파티창: 현재게임의동일데이터/카메라에서이전/현재1920×1080·1280×720 실제원PNG. 각I/E/초대wire진입을쓴다. 허용709/802같은불법최소관측으로1280성공을대체하지않는다.

## 순서와 소유

1. 신규캡처작업자가main tracked client를새격리snapshot에복제하고세창의표시변경전소스만commit b0cbec6a2cf12101b0fe96fe7877de9675850635(14573c80직전)의정확Gitblob으로바꾼다. 현재코드의역사와다른점/실제API연결을준비단계에서대조한다. 이는전체역사게임촬영이아닌현재게임+이전표시재현이다. 같은fixture사본에서각2해상도·HUD기본/PartyHUD동적관측을준비한다. root제품/원시험은읽기만이다.
2. READY뒤Main의새Task/새창에서baseline한번,종료/잔여/lock·exactprefs원형반환을즉시보고한다. HUD원그림은이때까지변경0이며아트준비는별도W출력만가능하다.
3. baseline작업자정산/종료뒤그림작업자가정확5PNG와출처만적용한다. 원본5/.meta·승인33입력은보존하고새W산출물과복사bytes를대조한다. 구현자자체확인을독립PASS로쓰지않는다.
4. 신규current작업자는새HEAD의HUD5·현재세창으로같은진입/데이터·2해상도와VIS-05를한번캡처한다. 그뒤신규독립검증1명이최종diff/그림5·실제before/current원시를함께강실사한다. 구현/검증/캡처작업자재사용0,제품결함은새세션으로반환한다.

기존보상Unity4/4는완료기록으로유지한다. HUD는계획상baseline1/current1을각각새READY/새Main자원창으로요청하며이문서자체는실행창허가/실제실행이아니다. 자동retry0,추가필요는현재결과/새READY로Main에올린다. 실행전단순백업·전후존재/hash·변화보존/원형복귀/보고,실행중prefs watcher0. 복잡한관문·새감시/검사도구수리0,Main cc4772aff47e 방향을유지한다.

## PR 완료와 보고

5PNG 적용/기존IDs·meta/코드배치씬보존·출처,HUD실제전후,①②실제완료,강독립판정·정확head CI와남은한계가있다. 그림모습은사용자가확인하고PR병합은별도정확head승인뒤메인만한다. 전체goal나머지효과/지형/정리까지완료했다고쓰지않는다.
