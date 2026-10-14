# 05 - 승인 UI 조각의 Unity 반입 범위 초안

상태: **1판, 메인 대조·승인 전.** 메인 `msg_955f9b81cbbf`(2026-10-14T11:34:35Z)의 초안 요청에 따른다. 지금은 문서와 읽기 관측만 작성했다. Unity 쓰기·Editor 실행·새 작업자 발행·실행 예약은 아직 없다.

## 원천과 단계 경계

- 사용자 원문(메인 pane Enter 제출 전달): 「대시보드 결정 응답: 1) Art UI 키트 - 브라운 B1 최종본을 이대로 승인할지, 고칠 곳을 말할지 → A 이대로 승인」. Art pane의 직접 입력으로 격상하지 않는다.
- 승인 대상은 `E/ui-kit-b1-ring-fix/package/kit`의 sprites 33·preview 12다. `E`는 로컬 `.backups/verification/2026-10-13-demo-video-art/`다. 메인은 사용자가 [UI 키트 페이지](https://claude.ai/artifact/9vLuj33Na2PfWXtxdu8THs) version 4를 보고 승인했다고 전달했다. 별 장식 가운데 남색 마름모(D5)를 원래 색 그대로 둔다.
- 독립 기술 판정 원문은 `E/ui-kit-b1-ring-review/verdict.md`, SHA256 `203ab425ec60f8083344375b1196a4e1ccee031fbf401243a3bd6713c618c00c`다. 이번 반입의 Unity 판정을 대신하지 않는다.
- 이번 초안은 기존 [UI 창 PR](goal.md#pr-경계와-점검)의 첫 반입 단계다. 새 goal이나 별도 자산 PR로 분리하지 않는다. 네 창·조합용 프리팹·표시 helper 적용은 후속 계약이며, 이번 단계만으로 UI 창 PR의 완료조건을 충족하지 않는다.

## 만들 것과 쓰기 범위

승인 PNG를 재가공 없이 Unity Sprite로 가져오고, 명세의 PPU와 9-slice 경계를 설정한 재사용 조각 한 벌을 만든다.

| 작성자 | 쓰기 경로 | 산출물·이유 |
|---|---|---|
| 신규 구현자 | `03_Client/Assets/Art/UI/Components.meta`, `Components/` 아래 | 승인된 이름 그대로 33 PNG와 각 `.meta`. 공통 조각 책임을 UI 아래 한 폴더에 모으며 B1·날짜·작업자 이름을 제품 경로에 붙이지 않는다 |
| 신규 구현자 | 같은 폴더의 `import-spec.json`·`README.md`와 각각의 `.meta` | 명세·사용법·출처 표. spec은 승인 원천의 name/file/PNG SHA256/pixelSize/uiSize/PPU/density/mode/border/contentInset을 값 변경 없이 옮긴 데이터이며 runtime loader를 만들지 않는다 |
| 신규 독립 검증자 | `03_Client/Assets/Tests/EditMode/UiKitImportTests.cs`와 `.meta` | 기존 `Dawnholder.Client.Tests.EditMode` assembly 안의 실제 import·조각 표시 검사. 제품 파일과 구현자의 meta는 수정하지 않는다 |
| 리드 | 현재 goal·이 범위 문서, 필요할 때 CURRENT의 Art 자기 줄 | 승인 출처·상태·검증 결과·PR 근거 기록 |
| 역할별 소유자 | 계약에서 지정할 `E/`의 새 구현/검증 폴더 | 사전 메모·입력 manifest·실행 로그·화면·전후 hash·보고·판정. 완료한 기존 패키지에는 쓰지 않는다 |

새 제품 파일은 PNG/meta 66개, 폴더 meta 1개, spec/README 및 meta 4개로 **71개 예상**이며 독립 시험/meta 2개가 별도다. 아직 생성한 실적은 아니다. `Components/`는 현재 없고 부모 `Art/UI/`는 존재한다. 임의 추가 파일이 필요하면 이 경계와 대조한다.

README에는 원래 생성 도구·모델 노출 여부·참고 그림·생성 시각·원본 위치/hash·최종 PNG hash·크기, B1 변환 경위, 승인 원천 spec hash를 연결한다. 출처는 기존 `kit/production-history.json`·`kit/raw/provenance.json`·원제작 근거에서 읽으며 추정한 모델/날짜를 채우지 않는다. 12장의 소개 미리보기와 생성기·과거 실행 기록 전체는 Assets에 복사하지 않는다.

## import와 사용 계약

- 고정 원천: `kit-spec.json` SHA256 `5986771c0a75c9cefaeeedc097a06462a92ddc036596914a055f3d5ac5f18d91`. 읽기 관측에서 33 PNG의 실제 SHA256과 명세가 모두 일치했다. 원시는 `E/ui-kit-unity-import-plan/source-inspection.json`이다.
- 모든 PNG는 Sprite(2D and UI)·Single·Full Rect·중앙 pivot·PPU 200이다. 원본 PNG는 2배 밀도다. mipmap 없음, sRGB/alpha transparency 사용, Bilinear·Clamp·NPOT 크기 유지, 기본 최대 크기 2048·무압축으로 원본 크기를 보존한다. 새 자산의 플랫폼 override로 크기 축소·압축을 덮어쓰지 않는다. CPU Read/Write는 켜지 않는다.
- 원본 PNG byte/alpha/색은 바꾸지 않는다. 수락·거절 8상태의 녹색·적색과 D5 남색 마름모도 그대로다. 기존 Inventory Panel/Slot의 PPU 100·Multiple 설정을 새 키트에 복제하지 않는다.
- `borderLRTB`는 **PNG 픽셀 단위**다. Unity border 벡터에는 `(L,B,R,T)`로 옮기며 2로 나누지 않는다. `contentInsetLRTB`는 글자·본문 여백 계약이므로 sprite border에 대신 넣지 않는다.

| 조각 | 개수 | 원천 L/R/T/B(px) | 이후 Image 사용 |
|---|---:|---|---|
| Panel | 3 | 24/24/24/24 | Sliced |
| Inset | 3 | 12/12/12/12 | Sliced |
| Header | 1 | 16/16/16/16 | Sliced |
| Button | 12 | 16/16/16/16 | Sliced |
| ItemSlot | 3 | 16/16/16/16 | Sliced |
| Divider_Horizontal | 1 | 16/16/0/0 | 가로만 늘림, 높이 6 UI 유지 |
| IconBase·CircleButton·NumberBadge·PortraitFrame·Ornament 2종 | 10 | 0/0/0/0 | Simple·비율 유지, 명세가 지정한 장식 반전만 허용 |

`Image.type`·`preserveAspect`·RectTransform·Canvas 배치는 Sprite importer의 저장 항목이 아니다. 이번에는 spec/README와 검증용 임시 Image에서 사용 계약을 확인하고, 실제 창의 Image에 연결하는 일은 후속 단계로 남긴다. 임시 Canvas의 referencePixelsPerUnit 100과 기본 배율에서 명세의 `uiSize`·테두리 두께가 맞는지 실제로 확인한다. 이 시험을 위해 기존 Canvas 설정을 바꾸지 않는다.

## 하지 않을 것

- 새 이미지 생성·재채색·리사이즈·alpha 청소, 기존 자체 제작 아트/HUD 다섯 장 교체.
- 네 창의 제품 코드·상태/입력·네트워크·씬·조합용 프리팹·대장장이·길드/대장간 화면·효과 적용.
- 영구 importer·AssetPostprocessor·runtime loader·새 Unity 패키지·assembly·전역 설정·Editor 업그레이드. 명세가 고정된 importer 설정과 meta 저장으로 끝낸다.
- `ProjectSettings`·`Packages`·기존 `.meta`/GUID·문자열 Resources 경로 변경, Content 작업 사본 쓰기. 특히 사용자 변경 `Resources/MinimapRT.renderTexture`·`ProjectSettings/ProjectSettings.asset`은 건드리지 않는다.
- 서버·DB·WSL·7777·공유 DLL 빌드. Library/Temp/Logs 등 Unity 캐시는 실행 부산물이며 Git 산출물이 아니다. 최초 import가 허용 밖 추적 파일을 바꾸면 원시와 상태를 보존해 메인에 알리고 임의 복원하지 않는다.

## 관찰 가능한 완료조건과 검증

**강 검증**을 제안한다. 33개 자산·새 직렬화 설정·Unity import/렌더 경계를 다루며, 신규 독립 검사도 필요하다. 파일/줄 수는 구현 후 실제 diff로 기록하고 현재는 예상치로만 둔다.

1. 저장소 PNG가 정확히 33개이고 승인 원천과 이름·크기·SHA256이 전부 일치한다. 추가/누락 0, 출처 표와 import-spec 누락 0이다. 독립 검증자가 고정 원천과 대조하며 새 spec을 스스로 정답이라고 가정하지 않는다.
2. 해당 프로젝트의 **실제 Unity 6000.6.4f1(revision 12bfff696524)**에서 import를 완료하고 33개를 실제 Sprite로 로드한다. importer·Sprite의 PPU·rect·border·pivot·mesh·설정을 전수 대조한다. 각 새 meta GUID는 유일하고 기존 GUID와 충돌하지 않는다. 설정 변경 없는 대상 재import 뒤 GUID·meta·PNG가 유지된다.
3. 실제 uGUI Image를 사용하는 임시 fixture에서 Sliced 22개를 명세 크기·1.5배·2배로 표시해 모서리·테두리 두께와 정상 mesh/빈틈을 확인한다. Divider는 폭만 바꾸고 높이 6 UI를 유지한다. 나머지 10개는 원형/장식 비율을 보존한다. 최소 합법 크기를 명세 border 합으로 구분하고 그보다 작은 폭·높이를 정상 사용으로 판정하지 않는다.
4. fixture의 1:1 화면·확대 표본·실제 import/검사 로그를 저장한다. Image 설정값 대조만으로 화면 통과를 대신하지 않는다. 9-slice 테두리 0 등 잘못된 설정을 쓰는 **임시 객체 대조군**에서 같은 관찰이 결함을 잡는지 확인하고 실제 자산을 훼손하지 않는다. fixture의 객체/RenderTexture/열었던 씬은 시험 후 정리하며 제품 씬을 저장하지 않는다.
5. 승인 전후의 기존 추적 자산·GUID·설정·프로토콜·DLL에 의도 밖 diff가 없고, 소유 범위 내 파일만 커밋한다. 독립 시험은 새 `UiKitImportTests`의 전체 case를 실제 실행하고 결과 XML·로그·실행 모델·환경·시간을 남긴다. 목표 밖 기존 실패는 원인별로 분류하고 통과로 숨기지 않는다.

실제 Unity 실행이 막히면 import/화면 항목은 **미실행·판정 보류**다. 기존 PNG 기술 PASS, YAML 문자열 검사, 시험 0건, dotnet 빌드만으로 Unity PASS를 내지 않는다. fixture 화면은 게임 화면이 아니며 기존 goal의 네 창 실화면 완료조건은 후속 적용에서 별도 확인한다.

구현은 신규 외부 Astra xhigh, 독립 강 검증은 신규 외부 Opus 5.5를 제안한다(라우팅 신호4/애매함: 새 다파일 Unity import·명세 이관·실행 경계). 모델은 발행 시 명시하고 한 작업 뒤 정산·종료한다. 리드는 제품·시험을 대신 작성하지 않는다. 이번은 runtime 제품 코드가 없는 반입 단계이며, 후속 창 적용 코드의 선행 시험 절차는 기존 goal대로 유지한다. 승인 뒤 정식 계약에 관련 CODE 절 원문·고정 입력·쓰기 경계·실제 진입·실패 분류 기준을 넣는다.

## Content C9와 Editor 실행 순서

1. **초안 승인 전에는 발행·Unity 쓰기·실행 예약 없음.** 승인 뒤 Content의 현재 Run/handle과 C9 상태를 확인하고 Editor 선행권을 준다. C9가 실제 실행 중이면 완료/안전 중단·정리 후 반환까지 기다린다. 대기 중이면 Content가 명시적으로 양보할 때만 순서를 바꾼다. 답이 없거나 예약 시각이 지났다는 이유로 반환을 추정하지 않는다.
2. Content가 Editor 종료·자기 실행/잠금 해제를 실제 확인해 반환한 뒤, Art가 큰 실행 직전 여유 물리·commit 4GiB 이상을 원시로 남긴다. Core SQL 등 다른 큰 실행과도 최신 상태를 대조해 한 실행 창을 정한다. 이것은 지금 창을 선점하는 요청이 아니다.
3. Art의 정확한 `art-active/03_Client`만 연다. 현재 Library가 없어 첫 import가 필요하고 소요 시간/최고 메모리는 미측정이다. 기존 프로젝트 버전을 확인하며 최초 import·좁은 설정·자체 확인을 묶는다. UI/MCP 조작은 사용자 지시대로 메인이 배치한 Codex Desktop Astra 경로를 쓰고, MCP가 필요하면 메인이 시트 소유자를 정한 뒤 사용한다.
4. 구현자 쓰기 종료·Editor 정리·작업자 정산 뒤 신규 검증자가 같은 결과를 읽어 독립 검사한다. 검증자도 순서와 메모리 조건을 새로 대조한다. 구현과 검증 Editor를 동시에 띄우지 않는다. 첫 import의 Library 캐시는 재사용하되 과거 실행을 새 검증 실적으로 쓰지 않는다.
5. 마지막에 자기 Editor/자식 프로세스와 잠금 정리를 확인해 Content·메인에 실제 반환을 알린다. 타 세션의 프로세스 종료·잠금 삭제는 하지 않는다. 재시작이나 추가 큰 실행은 당시 상태와 조율한 창 안에서만 한다.

## PR 경계와 메인 대조 사항

- 기존 `feat/demo-video-art-ui-20261013`의 UI 창 PR 안에 반입 커밋을 둔다. 반입 판정과 후속 네 창 적용 판정을 분리 기록하며, 이 단계만 끝났다고 UI 창 PR을 병합 요청하지 않는다. 새 PR 경계가 필요하면 메인 대조 때 따로 판단한다.
- 기존 사용자 승인의 새 조각·9-slice 부분을 좁힌 초안이다. 조합용 프리팹·표시 helper·창 적용을 제거한 것이 아니라 후속 계약으로 남겼다. HUD 그림 다섯 장은 기존 별도 HUD PR 경계를 유지한다.
- 메인이 이 경계·완료조건·강 등급·Content 선행 순서를 승인 초안과 대조한다. 차이가 있으면 사용자가 판단한다. 현재 승인 요청은 이 문서의 반입 단계뿐이며 PR 병합 승인을 포함하지 않는다.

읽기 관측과 맥락은 `E/ui-kit-unity-import-plan/context.md`·`source-inspection.json`에 있다. 이후 승인은 메시지 ID·시각·적용 판을 goal에 기록한 뒤 발행한다.
