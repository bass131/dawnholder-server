# 효과 가용 영역·시험 환경 보완 계획

2026-10-23 · Art 리드 Sol · 같은 Art goal의 다음 PR 계획이다. 현재 계획 작성만 수행했으며 새 작업자·제품/시험 변경·Unity 실행은 없다.

PR260은 head `56fe09b86659b0cba4abaedb95defdcdc4f4636d`, merge `e3d06ca32e0c58d43615caf8501affcc56313781`, 2026-10-23T11:47:52Z에 병합됐다. PR259까지 반영한 최신 main `24c80053071759d1c7e283175ba6289c5036c2c6`에서 로컬 branch `feat/demo-video-art-effect-layout-20261023`를 준비했다. 보호 MinimapRT·사용자 concept-alignment 초안은 원본 그대로 보존한다.

## 승인 출처와 착수 경계

- Main `msg_ec4bedd8e3ad`: 효과 문구와 대장간 버튼 겹침 V-1 및 기존 두 fixture의 AudioListener 누락 F-1은 Art 소유 보완이다. 로그 무시·기대값 완화 없이 빠진 입력만 보완한다.
- Main `msg_6f8d42f7a719`: HUD와 섞지 않는 작은 Art PR 하나로 최신 main의 새 branch에서 수행한다. Content가 정규화 가용 영역 값을 넘기고 Art가 효과 전체·라벨 경계·리사이즈를 소유하며 미지정은 현재 동작을 보존한다. 두 fixture는 활성 listener가 없을 때만 fixture 소유 listener를 준비·정리한다.
- Content `msg_b8be53889c8e`는 값 형식·소유권 방향에 동의하고 정확 심볼·범위·실패 처리를 한 문장으로 확정해 달라고 요청했다. 아래 `TrySetAvailableArea(Rect?)`·null/finite/invalid 세부는 당시 Art 맥락의 구체화안이다. Content가 이 정확 심볼까지 직접 승인했다고 확대하지 않으며 메인 대조 뒤 현재 Content 소비 계약에 같은 문장으로 고정한다. 원문 연결은 아래 근거 경로다.
- Main `msg_16762b50c9c4`·`msg_24cba0b59248`: HUD-F03을 이 다음 Art PR의 **첫 수정**으로 넣는다. BACKLOG로 이관하지 않는다.

**범위 판정:** 기존 goal 안의 이미 승인된 V-1/F-1 보완과 지정된 HUD-F03 후속이다. 새 사용자 범위 승인은 필요하지 않다. 착수 전 메인의 이번 계획·계약 대조와 공유 실행창 배정은 필요하다. 이 문서 전달만으로 새 세션이나 실행창을 사용하지 않는다.

## 범위

| 항목 | 계획 |
|---|---|
| 만들 것 | 첫 수정으로 HUD-F03의 잘못된 주석 수치를 근거에 맞춘다. 이어 효과 가용 영역 API와 배치를 추가하고, 두 기존 PlayMode fixture의 owned AudioListener 준비·정리를 보완한다. |
| 건드릴 곳 | `03_Client/Assets/Scripts/UI/GuildGrowthEffectView.cs`·`GuildGrowthEffectPresentation.cs`, `Assets/Tests/EditMode/GuildGrowthEffectTests.cs`, `Assets/Tests/PlayMode/GuildGrowthEffectLifecycleTests.cs`·`GuildGrowthEffectIndependentLifecycleTests.cs`, `HudGoldFitsGoldBarSceneTests.cs:103~105`의 주석, 이 goal·CURRENT Art 줄. 신규 독립 검증자만 `Assets/Tests/PlayMode/GuildGrowthEffectIndependentAreaSceneTests.cs`와 새 `.meta`를 쓴다는 배정을 계약에서 확정한다. |
| 하지 않을 것 | Content의 `GuildUiController`/adapter/이벤트·서버 상태 소비 변경, 길드·대장간 창 숨김이나 이벤트 생략·지연, 효과 시간/입력/raycast/취소 계약 완화, 로그 무시·skip 확대·기대값 일괄 갱신, HUD 재설계, 이미지 생성·기존 에셋 삭제, 월드씬/Tile/Palette/bin/충돌/포탈 변경, 새 guard/runner/helper. 보호 사용자2파일은 쓰거나 commit하지 않는다. |
| 관찰 가능한 완료조건 | 주석의 수치와 출처가 일치한다. 새 API의 유효/무효/null·기본 호환성이 시험과 실제 진입으로 확인된다. 1920×1080·1280×720에서 동시 두 효과 전체와 라벨이 주어진 가용 영역 안에 있고 resize/갱신 뒤에도 계약을 지킨다. 기존 listener를 소유·제거하지 않으며 fixture가 만든 listener만 정리된다. 기존 Audio8 case의 실제 결과와 새/기존 시험 결과를 원시로 분리해 보고한다. 독립 판정·모든 CI 후 별도 사용자 head 병합 승인을 받는다. |

제품 새 폴더나 그림 자산은 필요하지 않다. 실제 구현에서 다른 파일/helper가 필요하면 소유·변경 이유를 계약에 먼저 대조하며 계획 밖 확장을 자동으로 더하지 않는다.

## API와 소유권

공개 심볼은 `public bool TrySetAvailableArea(Rect? normalizedAvailableArea)` 하나다. 기존 `BuildRuntime(Transform)`·`TryPlayConfirmed(GuildChangeKind, int)`·`CancelAll()`의 호출 계약을 보존한다. 입력이 null이면 기존 기본 배치로 돌아간다. 유효 값은 finite 좌표·크기, 0..1 경계 안의 양수 크기다. 무효 입력은 false이며 직전 가용 영역·배치·진행 효과를 바꾸지 않는다. 배치 값 변경이 확정 이벤트의 수락·레벨·시간·취소 수명을 바꾸지 않는다.

**메인·Content에 동일하게 고정할 계약 문장:** `GuildGrowthEffectView.TrySetAvailableArea(Rect? normalizedAvailableArea)`는 null이면 기본 배치 복귀, 유한·0..1 안·양수 영역만 수락하고 무효 입력은 false/직전 배치·진행 효과를 보존하며, Content는 자기 창 root 전체를 기준으로 값만 계산·갱신하고 Art는 효과 전체·라벨·resize와 두 fixture의 활성 listener가 없을 때만 owned listener 생성·정리를 맡는다.

Content는 자기 창 root의 전체 Rect를 기준으로 영역 값을 계산·갱신한다. Art가 사설 child를 뒤지거나 Content 파일을 고치지 않는다. Art는 두 효과와 라벨의 전체 경계·화면 resize·진행 중 영역 갱신을 담당한다. 작은 영역 등에서 기존 가독성·비중첩 조건을 동시에 충족하지 못하면 임의로 계약을 완화하지 않고 실제 치수와 대안을 메인에 올린다.

이 Art PR의 완료는 가용 영역을 받은 표현의 독립 검증이다. 실제 Content C9 화면은 **이 PR 병합 뒤 Content의 별도 소비 PR**에서 값 전달과 버튼/효과 비중첩을 확인한다. Art API 단독 시험을 Content 실제 화면 통과로 보고하지 않는다.

## 작업 순서와 검증

1. 메인이 이 계획을 기존 승인과 대조한 뒤 정확 입력·파일 소유·관련 CODE 원문·실행/복귀 계획을 계약에 고정한다. 같은 goal·별도 PR로 진행하며 새 goal을 자동 만들지 않는다.
2. 신규 Sol 구현자 한 명이 먼저 HUD-F03 주석만 바로잡고 전후 diff를 남긴다. 이후 V-1 API/배치와 F-1 fixture를 구현·시험·자체 점검한다. 준비·실행 전담 세션을 추가하지 않는다.
3. 새 API는 null/미지정의 기본 동작, finite 경계, NaN/Infinity·영역 밖·0/음수 크기 거부와 상태 보존, 진행 중 갱신·resize, 동시 두 효과/라벨·취소/disable/destroy를 확인한다. 시험 기대값을 제품 배치 계산으로 복제하지 않고 실제 경계·결과로 판단한다.
4. 기존 GUI/렌더링·fixture·실행 감독을 재사용한다. Main이 별도로 배정한 창에서만 실행하고 입력 hash·정확 원시·prefs/Settings 복귀·소유 잔여0을 확인한다. 창 반환을 XML 해석보다 먼저 보고한다. 현재 자원 조회/창 예약은 하지 않았다.
5. Audio8은 이전 원 XML의 정확한 이름과 새 결과를 대조한다. 원140의 기존 case와 신규 case를 구분하며 skip/Explicit/제외를 통과로 세지 않는다. 원시의 실제 통과·실패 수를 집계하고 미실행을 적는다. 수리 전8실패를 삭제하거나 통과로 바꾸지 않는다.
6. 구현자 쓰기 종료·정산 뒤 PR 직전 신규 Opus 한 명이 강 검증을 수행한다. 새 배치 API·입력·수명 변경이 있어 보고·실제 diff·독립 시험과 **이번 변경의 실제 새 진입 실행**이 필요하다. 예전 HUD의 직접 실행0 판정 예외를 이 새 API에 자동 확대하지 않는다. 실행 창은 메인이 배정하며 관문 실패/미실행은 보류로 보고한다.
7. 판정 원문·규칙 준수/실제 경로·실패 분류·설계 관찰을 확인하고 리드만 commit/push·PR 생성·모든 CI·정확 head 보고를 맡는다. 병합은 메인에서 사용자 별도 head 승인 후 수행한다.

구현자는 사용자10-19 결정에 따라 `gpt-6.1-sol` max, 신규 Codex에는 Fast `-c service_tier="fast"`를 명시한다. 첫 신규 Codex rollout의 `thread_settings`에 `service_tier:"priority"`가 실제 있는지 원문 경로·줄을 확인하며 default면 작업 시작 전 메인에 보고한다. 별도 승인 없이 기존 세션 모델/전역 설정을 바꾸지 않는다. 신규 독립 검증자는 `claude-opus-5-5 --autocompact 250000`, 실제 최초 명령·화면과 backend unknown을 구분한다. 요청 모델 부재를 임의 대체하지 않는다.

## 별도 후속과 새 범위 승인

다음 후보인 마을 타일·대장간·불꽃 시범은 Main `msg_d80b44cef580`의 **조건부 승인된 별도 PR**로 유지한다. Sol 정지3회·불꽃 시트 최대2회·30 Unity Credits, 생성 없는 견적 우선, 정지3장 방향 확인, Main의 Opus 에셋 제작 세션/MCP 좌석/창 배정, 기존 애니메이션 보존 조건을 다시 묻지 않는다. 이 효과 손질 PR에 이미지나 월드씬을 섞지 않는다.

전체29베이스/51시트, 추가 생성/예산·새 모델 예외, Level의 실제 지형/배치/충돌·맵 데이터 변경, 새로운 기능·판정은 기존 승인 밖이다. 해당 확장이 필요해질 때 새 범위·소유자 승인을 메인에 올린다. 현재 이 확장을 요청하거나 착수하지 않는다.

## 근거

- 현재 병합 지시: `hud-reconstruction-pr/pr-final-review-02/main-merge-and-next-plan-original.json`.
- 승인 원문: `hud-reconstruction-pr/f01-diagnosis-01/check-update-14-acked.json`·`check-update-15-acked.json`(Main 두 판단과 Content 방향 동의 원문), 구체 API 안은 같은 폴더 `lead-context.md:113~115`. 모두 `.backups/verification/2026-10-13-demo-video-art/` 아래이며 원문을 수정하지 않는다.
- 병합·최신 main·보호 대조 및 이번 사전 메모: `.backups/verification/2026-10-13-demo-video-art/post-hud-next-pr-plan-01/`.
- HUD 검증·실패 원시·메인 조건부 수용: `hud-reconstruction-pr/pr-final-review-02/lead-final-pr-ci-report.json`·`verdict.md`·`lead-main-acceptance-addendum.md`.
