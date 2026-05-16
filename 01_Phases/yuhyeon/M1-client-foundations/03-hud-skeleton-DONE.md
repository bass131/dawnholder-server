---
summary: Gameplay 씬에 HUD Canvas overlay + HP 슬라이더 + HP/Gold 텍스트 + 미니맵 RawImage 자리잡이 + HudController.cs (Phase 03, mock 값 표시 + 서버 데이터 진입점 API)
phase: 03-hud-skeleton
work-id: phase03-hud-skeleton
status: done
completed_at: 2026-05-16
commit: 392a31a
---

# Phase 03 — HUD 골격 완료 박제

**소요 시간**: 약 2시간 (계획 2~3시간 하한선, Slider Value=0 함정 + Anchor 부호 학습으로 상한선까지는 안 감)

## TL;DR

Gameplay 씬에 HUD Canvas (Screen Space - Overlay, Sort Order 10, Scale With Screen Size 1920×1080) + 4개 UI 자식 (HpSlider 좌상단 빨강 / HpText 좌상단 / GoldText 좌상단 노랑 / MinimapPlaceholder 우상단 회색)을 추가하고 `Scripts/UI/HudController.cs` (Dawnholder.Client.UI 네임스페이스, SerializeField로 슬라이더/텍스트 참조 수집, Start()에서 mock HP 100/100 + Gold 0 표시, public UpdateHP/UpdateGold API). 헌법 #1 (Server Authority) 준수 — HUD는 *표시 전용*, 데미지/획득 *계산* 없음. mock 값은 임시이고 다음 마일스톤에서 패킷 핸들러가 UpdateHP/UpdateGold 호출해 서버 데이터로 갈아끼움. Phase 진행 중 Slider Value=0 사건 + 우상단 anchor 부호 학습 두 가지 학부생 함정 박제.

## 5단계 보고

- **무엇을 만들었나** — `Gameplay.unity`에 HUD Canvas (Sort Order 10, Scale With Screen Size 1920×1080 Match 0.5) + 4개 자식 UI (HpSlider Pos 20/-20 W300 H24 빨강 Fill / HpText Pos 330/-20 W200 H24 / GoldText Pos 20/-50 W200 H24 노랑 / MinimapPlaceholder 우상단 Pos -20/-20 200×200 반투명 회색). `Scripts/UI/HudController.cs` 신설 (Dawnholder.Client.UI 네임스페이스, [SerializeField] Slider hpSlider + TMP_Text hpText/goldText, Start()에서 UpdateHP(100,100)+UpdateGold(0), public UpdateHP/UpdateGold).
- **왜 필요한가** — Phase 02가 *메인 메뉴 진입*이었다면 Phase 03은 *게임 내 표시*의 첫 골격. HP/자원/미니맵 자리잡이를 미리 박아둬야 다음 마일스톤(M2~M3)에서 서버 패킷이 도착했을 때 *어디에 그릴지*가 이미 결정돼있음. 헌법 #1의 물리적 현현 — 클라는 "표시 컨테이너"고 데이터는 외부에서 주입.
- **어떻게 만들었나** — Unity MCP 연결이 revoke 상태라 손작업 모드로 진행. (1) HUD Canvas 신설 + Scale With Screen Size 모드 + Sort Order 10 (캐릭터 위에 그려지게). (2) HpSlider/HpText/GoldText 좌상단 anchor (Alt+클릭으로 anchor+pivot+position 한번에 좌상단 정렬). (3) MinimapPlaceholder 우상단 anchor (Pos X도 -20 — 우상단 anchor에선 안쪽 방향이 음수). (4) HudController.cs 작성 시 SerializeField + null 가드 채택 (Inspector 슬롯 누락 시 NullRef 회피). (5) HudController를 HUD Canvas 자체에 부착 (자식이 아니라 부모) + Inspector에서 HpSlider/HpText/GoldText 드래그 연결.
- **테스트 결과** — Play 모드에서 4가지 완료조건 모두 통과 (사용자 수동 검증 후 보고): HUD 4요소 모두 표시 ✓ / HP바 빨강 가득 (Value=1) ✓ / 1920×1080 / 1280×720 / 800×600 세 해상도에서 좌상단/우상단 anchor 유지 ✓ / Console Error 0개 (노란 Account API + UnityClientSession.cs warning 2건은 Phase 03 무관 기존 경고). Inspector에서 Mock Hp Current를 50으로 바꾸고 재Play 시 HP바 절반 + "HP 50 / 100" 표시되는지의 *추가* 확인은 권장만 했고 사용자 미수행 — 코드 로직 자체는 단순 비율 계산이라 회귀 위험 낮음.
- **다음 스텝** — Phase 04 (일시정지 메뉴): ESC 키 토글 + Resume/Quit 패널. 새 Input System 액션 연동 첫 등장. 같은 Gameplay 씬에 PauseMenu Canvas 추가 + InputAction "Pause" 바인딩.

## AC 검증 결과

Phase 03 완료조건 4개를 실제 검증한 결과:

| # | 완료조건 | 검증 방법 | 결과 |
|---|---------|----------|------|
| 1 | HUD 4요소 표시 | Play 후 Game 뷰 좌상단/우상단 시각 확인 | ✓ 통과 (사용자 캡처) |
| 2 | HP바 빨강 + 가득 참 | Play 후 Fill 색 + Value=1 시각 확인 | ✓ 통과 (Start()→UpdateHP(100,100)) |
| 3 | 3 해상도에서 anchor 유지 | Game 뷰 Aspect 토글로 검증 | ✓ 통과 (사용자 보고: "다 정상") |
| 4 | Console Error 0 | Play 후 Console 빨간 ⊘ 카운트 | ✓ 통과 (warning 2건만, Phase 03 무관) |

씬 파일 grep 검증은 사용자 commit 후 별도 수행 가능 (Hud Controller m_Script GUID + slot 참조 fileID 확인).

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Slider Value=0 함정**: HpSlider 만들고 Fill 색을 빨강으로 바꿨는데도 Game 뷰에 회색만 보임 → Inspector에서 Slider 컴포넌트의 Value가 0이라 Fill이 0% 차서 색깔과 무관하게 안 그려진 것. 진단: Inspector 캡처 보고 Value 칸 즉시 발견. 해결: Value=1로 변경. 학습: Slider는 *비율(0~1)*로 동작하고, Fill 색은 *비율이 0보다 클 때만* 시각화됨. 코드에서 `slider.value = current/max` 패턴이 핵심 (HudController.UpdateHP에 박힘).
- **우상단 anchor 부호 학습**: 좌상단 anchor에선 Pos X가 +(오른쪽 안쪽)였는데 우상단으로 옮기면 Pos X가 -(왼쪽 안쪽)로 바뀜. 외우는 법은 "anchor 좌표(0~1) 기준 화면 안쪽 방향이 음수". 우상단 anchor (1,1)에선 화면 안쪽이 (-,-) 방향. MinimapPlaceholder Pos X=-20, Y=-20.
- **Anchor preset Alt+클릭**: 그냥 클릭 = anchor만 이동(Position 자동 보정으로 슬라이더가 화면 밖으로 튀는 함정), Shift+클릭 = anchor+pivot, Alt+클릭 = anchor+pivot+position 0,0 리셋. 학부생 함정 회피 단축키. 이번 Phase에서 처음 박힌 Slider는 Pivot=0.5,0.5 (중앙) 그대로라 "동작엔 영향 없지만 깔끔함은 떨어짐" 상태로 진행 — 다음 Phase에서 더 엄격히 적용 검토.
- **HudController 부착 위치 결정**: 자식(HpSlider 등)에 붙이면 자기 부모 모르고 형제 못 찾음. **HUD Canvas 자체에 부착**이 자연스러움 — 슬라이더/텍스트는 *자식*이라 SerializeField 슬롯 드래그가 한 번에 가능. 학습: "이 컴포넌트가 *조작하는* 대상의 *공통 조상*에 붙인다"가 일반 패턴.
- **Image vs RawImage**: 미니맵 자리잡이로 RawImage 채택. Image는 Sprite 에셋(9-slice/UI 최적화) 표시용, RawImage는 임의 Texture(런타임 RenderTexture 등) 표시용. 미니맵은 다음 마일스톤에서 미니맵 카메라 RenderTexture를 끼울 예정이라 RawImage가 정석.
- **Mock 값 분리 패턴**: HudController에 mockHpCurrent/mockHpMax/mockGold를 SerializeField로 노출하고 Start()에서 UpdateHP/UpdateGold 호출 → "표시 API"와 "초기값"이 분리됨. 다음 마일스톤에서 패킷 핸들러가 UpdateHP를 호출하면 mock 값은 자연스럽게 무시됨 (덮어쓰기). Start()에서 mock을 *직접* hpSlider.value=1.0 하드코딩하지 않은 이유.
- **MCP 연결 revoke 우회**: 세션 시작 시 Unity MCP가 "Connection revoked" 상태였음. Edit > Project Settings > AI > Unity MCP에서 재승인 안내했지만 사용자가 그냥 손작업 모드로 진행 결정 → Phase 01·02와 동일한 방식(코드는 Claude 작성, 씬 작업은 사용자 수동 + 단계별 캡처 검증). 학습: MCP는 학습 효율을 *높이는* 도구지 필수가 아님. 손작업이 *학부생이 Unity Editor 익숙해지는 데*는 더 좋을 수도.

## 막혔던 지점 (있다면)

- **Slider Value=0 함정** (위 결정 흐름에 상세). 진단을 Inspector 캡처로 즉시 했지만, 사용자가 혼자였으면 "Fill 색 분명히 빨강으로 했는데 왜 안 보이지?" 한참 헤맸을 시나리오.
- **Anchor 좌상단 누락 (HpSlider 1차)**: Anchor preset 버튼을 *그냥* 클릭해서 anchor만 좌상단으로 갔지만 Position이 자동 보정되어 슬라이더가 화면 중앙쯤에 떠있음. Alt+클릭으로 재시도 + Pos X/Y 직접 입력으로 해결. HpText는 처음부터 Alt+클릭이라 한 번에 됨 → "처음 한 번 헷갈리는데 두 번째부터는 자연스러움" 패턴.
- **MCP revoke** (위 결정 흐름에 상세). 차단은 아니지만 학습 효율 손실. 재승인 절차는 본인 환경에서 별도 시도 필요.

## 학습 일지 후보 키워드

추후 `/journal:concept` 또는 `/journal:bug`로 본인이 펼칠 만한 단서들:

- **Canvas Scaler 3가지 모드**: Constant Pixel Size / Scale With Screen Size / Constant Physical Size — 어느 상황에 무엇을 쓰는지 (4K 모니터 / 모바일 / PC)
- **Anchor & Pivot 차이**: Anchor=부모 안 어디 부착, Pivot=내 회전·위치 중심점. Alt+클릭 단축키
- **Anchor 부호 규칙**: anchor 좌표(0~1) 기준 화면 안쪽 방향이 음수 (좌상단 → +X-Y, 우상단 → -X-Y, 좌하단 → +X+Y, 우하단 → -X+Y)
- **Slider 컴포넌트 구조**: Background / Fill Area > Fill / Handle Slide Area > Handle. Value 0~1 비율. Fill Rect 참조로 동작
- **Image vs RawImage**: Sprite vs Texture. UI 아이콘 vs 미니맵·카메라 출력
- **SerializeField 캡슐화 패턴**: private 필드 + Inspector 노출. public 노출 회피 → 외부에서 직접 변경 차단, 메서드 통해서만 접근
- **컴포넌트 부착 위치 결정 패턴**: "조작 대상의 공통 조상에 붙임"
- **Mock 값 → 서버 데이터 Bind 전이**: Start()에서 mock 표시 API 호출 → 패킷 핸들러가 같은 API를 *런타임에* 호출하면 자연스럽게 갈아끼움. 헌법 #1과 직접 연결
- **Unity Hierarchy의 부모-자식 좌표계**: 자식 RectTransform의 Pos는 *부모 anchor 기준*. Canvas 자체가 화면이고 그 안에 anchor가 박힘
- **Inspector 슬롯 드래그**: 게임오브젝트 통째 드래그 → Unity가 슬롯 타입에 맞는 컴포넌트 자동 추출 (HpSlider GameObject → Slider 컴포넌트만 잡힘)
