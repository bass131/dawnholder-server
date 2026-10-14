# 06 - 승인 조각으로 네 UI 창을 조합해 적용하는 범위

상태: **1판 후속 범위.** 메인 msg_785e1725976c(2026-10-14T15:03:14Z)는 기존 사용자 승인 안의 네 창·조합 프리팹·표시 helper 구현을 허용했다. 05 반입 강 PASS 뒤 같은 goal·같은 UI 창 PR에서 이어간다. 새 시각 선택은 만들지 않고 아침 사용자 확인 목록으로 남긴다. 아직 후속 작업자·제품 계약·Unity 예약은 없다.

## 만들 것

승인된 브라운 B1 조각 33개를 공통 조합 프리팹과 표시 helper로 재사용해 인벤토리, 던전 보상, 파티 초대·파티원 표시, NPC 대화창에 입힌다. 현재 창의 내용·상태·입력·요청 수명·기본 배치는 보존한다. 새 색·문구·창·배치안을 임의로 고르지 않는다. 승인 spec의 inset과 9-slice 단위를 적용하는 표시 변경이 기존 배치에서 불가능하면 그 항목을 아침 사용자 확인으로 돌린다.

05 판정의 실제 Sprite·mesh·GPU 검증에 이어 실제 Canvas·카메라 합성과 네 창 전후 화면을 확인한다. 기존 PNG 합성 미리보기로 실화면을 대신하지 않는다. 실화면의 미관 승인은 아침 사용자 확인이며 기술 판정과 구분한다.

## 제품 쓰기 경계

| 경로 | 허용 변경 |
|---|---|
| `03_Client/Assets/Scripts/UI/InventoryPanelView.cs` | 생성자·표시 조립 helper의 배경·버튼·칸·승인 여백만. snapshot·요청 판정·callback/Dispose 책임 보존 |
| `03_Client/Assets/Scripts/UI/DungeonClearRewardWindow.cs` | BuildView·표시 조립의 배경·제목·보상줄·확인 버튼만. 보상/확인 순서·씬 수명·가시성 보존 |
| `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs` | BuildRuntime/BuildButton의 그림 적용만. 응답 command·pending·예외·구독/해제 보존 |
| `03_Client/Assets/Scripts/UI/PartyMemberHud.cs` | 기존 serialized root/슬롯의 runtime 표시 초기화만. 활성 상태·CanvasGroup·재바인딩/해제·멤버 값 보존 |
| `03_Client/Assets/Scripts/Gameplay/NpcDialogPanel.cs` | BuildRuntime의 그림·초상화틀·승인 본문 여백만. 싱글톤·Show/Hide·E/ESC·문자 내용·초상화 여부 보존 |
| `03_Client/Assets/Scripts/UI/`의 새 표시 helper/skin 타입·meta | Sprite 참조·조합·스타일의 공통 책임. 상태·네트워크·입력 소유권 없음 |
| `03_Client/Assets/Resources/UI/`의 새 skin 자산·조합 프리팹 하위 폴더·meta | Art Sprite의 GUID를 참조하는 패널/제목/버튼/칸/원틀/구분선 조합. 기존 리소스 이동·덮어쓰기 없음 |
| 새 EditMode/PlayMode 시험·meta | 신규 선행 시험 작성자와 독립 검증자만 쓰기. 발행 때 정확 파일과 소유권 확정 |

PartyMemberHud의 실제 serialized 사용처는 `Scenes/99.UI/UI.unity`지만 씬은 쓰지 않는다. runtime 표시 적용으로 기존 필드·GUID·fileID를 보존한다. 이 방식으로 요구를 충족할 수 없으면 쓰기 전에 메인에 차이를 보고한다. Content의 현재 C9 쓰기·검증 대상과 위 다섯 사용처의 착수 경계를 먼저 대조한다. Content 작업 사본을 쓰지 않는다.

## 설계와 대안

추천은 Sprite 참조와 조합 프리팹을 serialized skin 자산으로 묶어 기존 Resources 경계에서 읽고, 표시 helper가 창 조립 코드에 적용하는 방식이다. 원본 PNG는 `Assets/Art/UI/Components`에 그대로 두고 GUID로 참조한다. runtime AssetDatabase 사용이나 PNG 복제는 하지 않는다. 현재 다섯 사용처에서 필요한 표시 책임만 모으고 범용 테마 엔진·확장 hook은 만들지 않는다.

대안은 각 창에 참조·조립을 중복하는 것이다. 초기 변경은 작지만 같은 버튼 상태·9-slice·여백 계약이 여러 곳에서 바뀐다. 최종 API·타입/파일명은 구현 설계이며 초안의 이름을 시험 기대답으로 강제하지 않는다.

## 보존 계약

- 승인 PNG 33개, PPU 200, Sprite Single/FullRect, meta/GUID·border·alpha를 보존한다. Canvas reference PPU 100과 PNG 밀도 2를 구분하며 meta border를 2로 나누지 않는다. Simple 조각은 preserveAspect를 유지한다.
- 인벤토리 8슬롯·재화 상한/자리수·동기화/대기/재시도/버튼 enabled·I 열림 상태의 세션 소유·Dispose를 보존한다. 보상표/지급·확인·씬 전환/중복 생성·연결 정리도 바꾸지 않는다.
- 파티 수락/거절 command·pending/예외·구독·멤버 값, NPC Show/Hide·E/ESC·초상화 없음 처리를 보존한다. 상태와 판정은 기존 소유자에 둔다.
- 기존 Canvas 정렬/Scaler·TMP 폰트/한국어 fallback·문구·기본 위치/크기를 보존한다. 장식은 raycastTarget=false이며 실제 버튼의 입력 영역·callback·클릭음·등록 횟수를 유지한다.
- 버튼의 보통/마우스 올림/누름/비활성은 승인 Sprite로 표시한다. 수락 녹색·거절 적색을 보존하고 기존 interactable 판정을 옮기지 않는다. PNG 색을 임의 tint로 바꾸지 않는다.
- 긴 한국어·최대 재화/슬롯/멤버·초상화 없음에서도 내용과 입력 영역을 확인한다. 승인에 없는 배치·색·문구 선택이 필요하면 구현하지 않고 아침 확인 목록에 적는다.

## 하지 않을 것

HUD 그림 다섯 장 교체, 길드/대장간 화면, 성장 효과, 발판/지형, 새 이미지 생성은 이 계약에서 하지 않는다. 기존 scene·Packages·ProjectSettings·공유 DLL·패킷·서버/DB/WSL·전역 설정도 바꾸지 않는다. 현재 goal의 뒤 묶음과 새 goal을 구분하며 다음 goal을 자동 착수하지 않는다.

Unity의 허용 밖 추적 파일 변경은 원시·상태를 보존해 메인에 보고한다. 이미 받은 Art 설정 1파일 복원 허용을 미래 실행 전체나 Content 사용자 파일로 확대하지 않는다. inference define 원인 확정·패키지 설정 정비는 동결 뒤 후보다.

## 완료조건과 검증

1. 실제 기존 생성 진입점에서 다섯 사용처가 승인 조각·공통 프리팹/helper를 사용한다. Sprite GUID·Image/RectTransform 단위·Raycast·부모/자식 배치를 실사한다.
2. 네 창의 실제 Canvas·카메라 합성 전후 화면을 남긴다. 1920×1080과 기존 최소 합법 화면 조건에서 내용·버튼·초상화/경계 상태를 확인한다. fixture와 게임 실화면을 구분하고 미실행을 성공으로 쓰지 않는다. 미관 확인은 아침 사용자에게 남긴다.
3. 신규 외부 선행 시험은 변경 전 실패와 변경 후 통과의 실제 수치·원시를 남긴다. 제품 계산을 복제하지 않고 실제 UI 객체/렌더와 잘못된 Sprite·입력 차단 장식 등 대조군을 사용한다.
4. 영향받는 기존 인벤토리 표시/토글·파티 응답/구독·보상창 씬 수명 시험을 실행한다. 실패는 요구 근거로 전수 분류하고 기존 동작 실패를 표시 차이로 덮지 않는다.
5. Sprite/importer/meta·기존 scene·사용자 파일을 보존하고 diff를 쓰기 범위에 한정한다. 실행 부작용·수행/미실행·원시를 분리 보고한다.

runtime 코드·프리팹·UI 합성 경계여서 강 등급을 적용한다. 파일/줄 수는 실제 diff로 기록한다. 새 외부 선행 시험 작성자 → 새 구현자 → 새 독립 검증자를 분리하고 작업 하나 뒤 정산·종료한다. 최신 모델 라우팅과 Sol 배정 xhigh 결정을 발행 때 명시한다.

새 실행 하네스·시각 계산 준비 장치를 추가하지 않고 기존 Unity/검사 도구와 리드 공식 답을 쓴다. Content21의 실제 반환과 현재 예약을 대조해 각 Unity 창을 조율한다. Core WSL 우선·Unity 병행 fresh 물리6GiB/commit4GiB 조건을 유지한다. MCP/Desktop이 필요하면 사용 전 메인에게 시트 소유·시각 지정을 받는다.

메인 msg_785e1725976c의 S2 기준: 기존 -testResults 옵션이 근거 폴더를 가리키는지 확인한다. 기본 위치 `C:/Users/bass1/AppData/LocalLow/DefaultCompany/03_Client/TestResults.xml`이 있으면 실행 직전 근거 폴더로 복사하고 크기·SHA256을 기록한다. 원본은 삭제/복원하지 않으며 덮어쓰기 발생은 부작용으로 공개하고 제품 판정을 막지 않는다.

## PR와 중지 경계

같은 `feat/demo-video-art-ui-20261013`의 UI 창 PR에 05·06 결과를 둔다. 새 자산 PR을 만들지 않는다. 기술 독립 검증·실화면 근거 뒤 PR head를 메인에 보고하고 새 미관 판단은 아침 사용자에게 남긴다. PR마다 정확 head의 사용자 병합 승인은 별도다. 현재 goal 종료점(PR head 보고·병합 대기 포함)에서 기록/종료 점검 뒤 멈추며 다음 goal 초안·작업자는 열지 않는다.
