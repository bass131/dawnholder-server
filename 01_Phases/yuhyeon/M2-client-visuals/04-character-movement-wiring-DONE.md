---
summary: PlayerAnimatorSync.cs(transform.position 변화 감지 → Animator.SetBool("IsMoving") + SpriteRenderer.flipX) 신설. LocalPlayerController(팀장 prediction) 0줄 수정. Walking_KG_1 Auto Slice 잔재(변동 mesh 32~38×62~64)로 인한 시각 흔들림(Idle↔Run 위/아래)을 hard reset(Single 모드 flush + Grid 100×64 + Bottom Center pivot 재구성 + Player_Run.anim 7 sprite 재바인딩)으로 봉합. ★ 5/20 수 면담 데모 도착점.
phase: 04-character-movement-wiring
work-id: yuhyeon-m2-phase04-character-movement-wiring
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 04 — 캐릭터 이동 시각 wiring 완료 박제 (★ 면담 데모 도착점)

**소요 시간**: ~2시간 (계획 30분~1h → 사고 처리로 늘어남)

## TL;DR

LocalPlayerController(팀장 M2 prediction 골격)가 이미 transform.position 갱신 중임을 코드 read로 발견 → Phase 04 파일 *전면 rewrite*(시각 wiring만으로 축소). PlayerAnimatorSync.cs 30줄 컴포넌트 신설 + Player GameObject에 박음으로 A/D 이동 시 Idle↔Run 즉시 전환 + flipX 정상화. 회귀 중 *위/아래 흔들림* 사건 발생 → 원인 진단(Walking은 Auto Slice라 frame mesh 32~38×62~64 변동, Idle은 Grid 100×64 균일) → Pivot 통일 시도 효과 X → hard reset(Single 모드 flush + Grid 100×64 재박기 + Player_Run.anim 재바인딩)으로 봉합. ★ 면담 데모 핵심 시각 완성.

## 5단계 보고

- **무엇을 만들었나** — `Scripts/Rendering/PlayerAnimatorSync.cs` 30줄 컴포넌트(Awake에서 Animator + SpriteRenderer 캐싱, LateUpdate에서 dx 감지 후 SetBool + flipX, MoveEpsilon 0.001 guard) + Player GameObject에 컴포넌트 박힘(총 6 컴포넌트). Walking_KG_1.png hard reset(Single 모드로 sub-sprite GUID 전부 flush → Multiple 모드 복귀 + 7 frame Grid 100×64 + Bottom Center pivot 재구성 + Player_Run.anim 7 sprite name 기반 재바인딩 완료, missing 0).
- **왜 필요한가** — Phase 03에서 박은 IsMoving Parameter + 양방향 Transition이 *실제 입력*으로 트리거되어야 면담 데모 ★ 시각 완성. LocalPlayerController는 위치만 갱신, Animator/flipX 트리거 코드는 *별도 컴포넌트 책임* — 헌법 정합(공유 영역 LocalPlayerController 0줄 수정, 본인 영역 신설). Walking Auto Slice 잔재는 *frame mesh 크기 변동* → 같은 transform.position에서 *frame center 다름* → Idle 정지 vs Run 이동 사이 위/아래 흔들림. 면담 데모에 *시각 결함* 표시 = M2 학습 자료 신뢰성 ↓.
- **어떻게 만들었나** — 결정 1: **Phase 04 파일 전면 rewrite** — *기존 코드 read first* 누락이 원인. LocalPlayerController.cs + PlayerPredictor.cs + InputHistory.cs + CameraFollow.cs + InputSystem_Actions 모두 read 후 *"이미 prediction layer 박혀있음, 시각만 추가"* 방향 확정. 결정 2: **PlayerAnimatorSync 별도 컴포넌트** vs LocalPlayerController 수정 → 별도 컴포넌트 (공유 영역 PR 회피 + 권한 분리 + 본인 영역 자체 완결). 결정 3: 흔들림 fix — Pivot 통일(36 PNG) 시도 → 효과 없음 → Auto Slice mesh 변동이 진짜 원인 → Grid 100×64 hard reset. 결정 4: `importer.spritesheet` 단순 박기 시 9개 잔재 발견(기존 sub-sprite GUID 보존 경향) → **Single 모드 flush 트릭**(spriteImportMode=Single + SaveAndReimport → Multiple 복귀)로 명시적 reset 패턴 학습. 새 개념 = AssetImporter.spritesheet API + spriteImportMode reset + AnimationUtility.SetObjectReferenceCurve로 sprite name 기반 재바인딩.
- **테스트 결과** — MCP 다축:
  - Player components 6개(Transform/SpriteRenderer/PlayerInput/LocalPlayerController/Animator/PlayerAnimatorSync) ✓
  - Walking_KG_1.png 7 sub-sprites 모두 (i*100, 0, 100, 64) + pivot (50, 0) ✓
  - Player_Run.anim 7 sprite keys 재바인딩 (missing 0) ✓
  - Console 에러 0, 경고 2건(둘 다 무해 — UnityClientSession nullable annotation + MainMenuController hardcode 잔재)
  - 본인 회귀 보고: "모두 만족" + 흔들림 해소 "완벽해"
- **다음 스텝** — Phase 05 진입 (정적 배경 1장 + Sorting Layer 셋업). 면담 1일 남음 — 배경 1장이면 면담 데모 임팩트 ↑. M2 학습 마감 시 MainMenuController.cs TODO 복원 + 다른 동작 PNG들도 Walking과 같은 Grid 100×64 hard reset(Phase 02 학습 컨벤션 누락분 정리).

## AC 검증 결과

```bash
# 1. PlayerAnimatorSync.cs 신설 + Player 컴포넌트 박힘
Player components (6):
  Transform / SpriteRenderer / PlayerInput / LocalPlayerController / Animator / PlayerAnimatorSync

# 2. Walking_KG_1.png hard reset 후 검증
Walking sub-sprite count: 7
  Walking_KG_1_0: rect=(0,   0, 100, 64), pivot=(50, 0)
  Walking_KG_1_1: rect=(100, 0, 100, 64), pivot=(50, 0)
  Walking_KG_1_2: rect=(200, 0, 100, 64), pivot=(50, 0)
  Walking_KG_1_3: rect=(300, 0, 100, 64), pivot=(50, 0)
  Walking_KG_1_4: rect=(400, 0, 100, 64), pivot=(50, 0)
  Walking_KG_1_5: rect=(500, 0, 100, 64), pivot=(50, 0)
  Walking_KG_1_6: rect=(600, 0, 100, 64), pivot=(50, 0)

# 3. Player_Run.anim 재바인딩
Rebound 7 keys (0 missing) in 'm_Sprite'

# 4. Console
Errors: 0
Warnings: 2 (UnityClientSession CS8632 nullable, MainMenuController CS0414 gameplaySceneName never used — 둘 다 무해, 본인 hardcode 경고는 M2 마감 시 자동 해소)

# 5. 본인 회귀 보고
"모두 만족" + 흔들림 해소 "완벽해" — Idle↔Run 시각 흔들림 0
```

## 결정 흐름

- LocalPlayerController 수정 vs 별도 컴포넌트 → **별도 컴포넌트** (공유 영역 권한 분리 + 본인 영역 자체 완결 + 팀장 prediction 골격 안 만짐).
- delta detection vs 직접 콜백 → **delta(transform.position 변화)**. *느슨한 결합* — LocalPlayerController는 본 컴포넌트의 존재 모름. 단점 = M3+ 서버 reconcile snap 시 false positive 가능 → 정밀화는 그때.
- 흔들림 fix: Pivot 통일 vs Grid re-slice → 처음 Pivot 시도 → 효과 X → **Grid 100×64 hard reset**(원본 PNG 크기 검증 = 모든 동작 *.png가 width%100=0, height=64 → 100×64가 정답).
- spritesheet 단순 박기 vs Single 모드 flush → **Single flush**(기존 sub-sprite GUID 폐기 + 새 7 entries만 박힘 보장). Animator clip은 name 기반 재바인딩.

## 막혔던 지점

- **★★★ 학습 가치 — *Phase plan ≠ code reality***: 옛 Phase 04 파일(*M2 시작에 분해*)이 *기존 코드 안 보고* 박혀 *전부 부정확*. LocalPlayerController는 이미 *완전한 prediction 클라이언트*인데 "새 PlayerController 만들기"로 박힘. Phase 진입 *전* 코드 read first 했으면 처음부터 정확. **M3+ Phase 분해 시 의식할 시니어 마인드셋**: *목표 적기 전에 해당 영역 코드 한 번 read*.
- **★★ Auto Slice vs Grid 차이**: Walking_KG_1.png는 *처음에 Auto Slice*로 박혀 frame mesh 32~38×62~64 변동 → 같은 transform.position에서 frame center 위치 다름 → 시각 흔들림. Idle은 Grid 100×64라 균일. 원인 진단에 Pivot 통일 우회 시도(효과 X) → Grid re-slice 진짜 fix. **교훈**: Sprite Editor Slice 시 *원본 PNG가 균일 grid에 맞으면 Grid by Cell Size가 기본*. Auto Slice는 *frame 크기 모를 때만* 사용.
- **★ `importer.spritesheet` 단순 박기로 안 됨**: 새 7 entries 박았는데 기존 sub-sprite GUID와 합쳐 9개 됨(Unity가 기존 보존). **명시적 flush 트릭**: `spriteImportMode = Single + SaveAndReimport` → 모든 sub-sprite GUID 폐기 → Multiple 복귀 + 새 박기. AnimationClip은 name 기반 재바인딩으로 복구.
- **Pivot 통일이 효과 없었던 이유**: pivot 값은 (0.5, 0)로 박혔지만 *mesh 자체가 32~38×62~64로 변동* → *발끝 픽셀*이 mesh의 어디에 있느냐가 frame마다 달라 → 같은 pivot이라도 시각 위치 다름. **Pivot은 *mesh 모양이 같을 때*만 통일 효과**. mesh 자체가 다르면 mesh 재구성이 정답.

## 학습 일지 후보 키워드

- **`phase-plan-vs-code-reality`** — ★★★ 새 키워드 + 면접 결정타. Phase 04 옛 파일 전면 부정확 사건. M3+ 마일스톤 분해 시 *기존 코드 read first* 룰. 시니어 마인드셋 중 가장 강력.
- **`unit-correctness-vs-end-to-end-revisited`** — Phase 01~03에서 박은 키워드. Phase 04에서 *Walking Auto Slice 잔재 + spritesheet flush 미흡*으로 **Rule of Five 도달** (5 사건 누적). `/journal:concept`로 펼치면 *5 사례 통합 면접 답변* — 단일 사건 대비 압도적.
- **`auto-slice-vs-grid-pivot-mesh`** — ★★ Sprite Editor Slice 방식의 차이 + Pivot은 mesh 같을 때만 효과. Unity 2D 학습 핵심.
- **`importer-spritesheet-hard-reset-trick`** — ★ Single 모드 flush + Multiple 복귀 패턴. Unity 6.4 AssetImporter API 함정 회피.
- **`loose-coupling-via-delta-detection`** — PlayerAnimatorSync가 LocalPlayerController를 모르고 *transform.position 변화*만 감지. 컴포넌트 간 느슨한 결합 패턴. React useEffect의 dependency 감지와 짝.
- **`server-authority-prediction-layer-already-built`** — LocalPlayerController가 *이미 prediction layer*라 헌법 #1 임시 우회 *없음*. Phase 04 파일에 박혔던 "임시 우회 약속"이 *잘못된 전제*였음 — 정정 학습.
