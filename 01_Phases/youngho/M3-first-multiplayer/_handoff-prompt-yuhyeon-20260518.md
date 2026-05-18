# 유현용 핸드오프 프롬프트 — M3 분담 합의 (5/18 박힘)

> 디스코드로 유현에게 전달. 위는 디스코드 본문, 아래 ===로 구분된 프롬프트는 유현 자기 Claude한테 그대로 복붙.

---

## 📩 디스코드 메시지 (영호 → 유현)

유현아, M3 분담 박혔어.

요약:
- 내가 `LocalPlayer.prefab` 추출했음 (`03_Client/Assets/Prefabs/Characters/`)
- M3 Phase 08은 두 갈래로 분리됨 — 너는 **08a부터 병렬 시작 가능** (의존성 0)
- M2 Phase 07(한글화)/08(회귀영상)은 시간 부족으로 **스킵 박제** (회귀는 자연 검증으로 이미 통과)
- `PlayerBase.prefab` + variant 패턴 도입 결정 (학습 가치 ↑)

합의 상세: `01_Phases/youngho/M3-first-multiplayer/_handoff-prefab-20260518.html` (화면 공유로 같이 본 거)

내 인프라 commit + main 머지 끝나면 디스코드 알릴게. 그때:
1. `git pull origin main` 받기
2. 아래 ───로 구분된 프롬프트를 너 Claude한테 그대로 복붙
3. Claude가 자동으로 자기 머신 CONTEXT.md / work-pin 갱신 + Phase 08a 진입 안내

다음 음성: 5/19 (내 Phase 05 일부 + 너 08a 진척 점검)

---

## 🤖 유현 Claude용 프롬프트 (그대로 복붙)

───────────── 복붙 시작 ─────────────

영호가 M3 분담 박았어. 본 세션에서 다음 처리해줘:

**【1단계】 변경된 파일 read (5개)**:
- `01_Phases/yuhyeon/M2-client-visuals/07-menu-and-hud-korean.md` (M2 Phase 07 스킵 박제)
- `01_Phases/yuhyeon/M2-client-visuals/08-regression-and-demo.md` (M2 Phase 08 스킵 박제)
- `01_Phases/youngho/M3-first-multiplayer/_handoff-prefab-20260518.html` (5/18 합의 박제 — 영호 골격 + 유현 비주얼 분담)
- `01_Phases/youngho/M3-first-multiplayer/08a-asset-prefab-preparation.md` (내 다음 작업 정의)
- `03_Client/Assets/Prefabs/Characters/LocalPlayer.prefab` (영호 추출 완료, 컴포넌트 6개 보존 — 회귀 검증 필요)

**【2단계】 내 머신 CONTEXT.md "현재 멈춤 지점" 섹션 갱신**:

옛 박제 = M2-client-visuals Phase 07/08 마감 예정
새 박제 = M2 Phase 07/08 스킵 박힘 → M3 Phase 08a 진입 (영호 마일스톤 합류, 영호 골격 + 내 비주얼 분담)

컨텐츠 예시 (응축해서 박을 것):
"5/18 영호 M3 분담 박힘. LocalPlayer.prefab 추출 완료(Prefabs/Characters/). M2 Phase 07/08 스킵 박제. M3 Phase 08a(Asset+PlayerBase variant) 진입 예정 — 사전 조건 = 영호 RemotePlayer.prefab placeholder + RemoteEntity.cs main 머지 후 pull. PlayerBase.prefab variant 패턴 도입 결정. 영역 분리: 내 = Prefabs/Characters/ + Sprites + Scripts/Rendering, 영호 = Scripts/Network + Scripts/Input + Scripts/State. RemoteEntity 컴포넌트는 영호 시그니처 — 비주얼 교체 시 절대 보존. 다음 음성 5/19. 면담 5/20."

**【3단계】 내 머신 work-pin (`.claude/state/current-pin.txt`) 갱신**:

```
WORK-ID: m3-phase-08a-asset-prefab-preparation
PHASE: 08a/09
현재 작업: M3 Phase 08a — 캐릭터 Asset 정착 + PlayerBase.prefab variant + LocalPlayer.prefab 회귀 검증 + RemotePlayer.prefab 비주얼 교체
완료 조건: 4건
  1. LocalPlayer.prefab M2 회귀 통과 (혼자 모드 정상)
  2. PlayerBase.prefab + variant 패턴 박힘 (Local/Remote가 base 공유)
  3. RemotePlayer.prefab 비주얼 진짜 캐릭터로 교체 + RemoteEntity 컴포넌트 보존
  4. 헤드리스 봇 2명 + 본인 Unity 클라 멀티 시나리오 통과

다음 액션:
  1. LocalPlayer.prefab 회귀 검증 (Unity Editor Play 모드 1분)
  2. PlayerBase.prefab + variant 패턴 박기
  3. RemotePlayer.prefab 비주얼 교체 (RemoteEntity 컴포넌트 보존 핵심)

주의할 약속:
  - **RemoteEntity 컴포넌트는 영호 영역 시그니처 — 비주얼 교체 시 절대 건드리지 X** (5/18 합의 핵심)
  - 본인 영역 = Prefabs/Characters/ + Sprites import + Scripts/Rendering
  - 영호 영역 = Scripts/Network + Scripts/Input + Scripts/State (UnityClientSession.cs / LocalPlayerController.cs)
  - PlayerBase 순환 참조 함정 — base가 variant 참조 X
  - Sprite Editor 잔재 GUID — Single 모드 flush 트릭 (M2 Phase 04 패턴 재사용)
  - Inspector missing reference 0건 확인

마일스톤 컨텍스트:
  M3 First Multiplayer & Demo Stage (영호 마일스톤 합류) — 5/20 교수 중간 면담 응급 데모
  Phase 08a 의존성 0 = 영호 Phase 05/06/07과 완전 병렬
  다음 음성: 5/19 (영호와 진척 체크)
  면담: 5/20

브랜치: 새 브랜치 권유 — feature/yuhyeon-m3-phase08a-asset-prefab (main 최신에서 분기)
```

**【4단계】 Phase 08a 진입 안내**:

학부생 멘토링 톤으로:
1. 첫 작업 = LocalPlayer.prefab 회귀 검증 (Unity Editor Play 모드 — WASD 이동 + Idle/Run 애니메이션 + CameraFollow 통과 확인)
2. 둘째 = PlayerBase.prefab 신설 + variant 패턴 (Transform + SpriteRenderer + Animator만 base에)
3. 셋째 = RemotePlayer.prefab 비주얼 교체 (회색 박스 → 진짜 캐릭터, RemoteEntity 보존)

각 단계 trade-off + 함정 설명. 5단계 보고는 코드 작업 끝나면.

**【5단계】 다음 음성 = 5/19 영호와 진척 체크**

학부생 멘토링 톤 유지 (이미 박혀있음). 모르는 건 물어보고, 의심 가는 거 미리 짚고.

───────────── 복붙 끝 ─────────────

---

## 🔍 참고 파일 (유현이 직접 보고 싶을 때)

- 합의 박제: [`_handoff-prefab-20260518.html`](./_handoff-prefab-20260518.html) (화면 공유로 본 합의)
- 영호 Phase 05 정의: [`05-client-remote-entity-registry.md`](./05-client-remote-entity-registry.md)
- 정유현 Phase 08a 정의: [`08a-asset-prefab-preparation.md`](./08a-asset-prefab-preparation.md)
- 정유현 Phase 08b 정의 (후속): [`08b-zone-ui-integration.md`](./08b-zone-ui-integration.md)

---

## 📋 영호 본인 체크리스트 (전달 전 확인) 

- [ ] 인프라 commit + push 완료
- [ ] main 머지 PR 올림 (또는 직접 머지)
- [ ] 디스코드에 위 메시지 + 합의 HTML 화면 공유 (또는 파일 첨부)
- [ ] 5/19 음성 일정 확인
