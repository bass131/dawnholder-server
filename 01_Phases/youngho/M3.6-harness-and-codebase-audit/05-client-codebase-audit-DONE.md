---
summary: M3.6 Phase 05 마감 (복잡) — 클라 코드 전수조사. Coordinator 비-호출 결정 (단일 도메인 + 복잡 등급 = 메인 직접 분해 정합). client Worker + unity-bridge 자문 + reviewer Tier 2-A 3 SubAgent. 헌법 5/5 PASS 0 위반 (클라 측 시각) + false-promise 4건 신규 (누적 11건) + Unity batchmode compile 첫 실측 통과 (β3 봉합) + 봉합 옵션 A 1건 (04_ClientNet/CLAUDE.md 1줄).
phase: 05
status: done
grade: 복잡
owner: youngho
---

# Phase 05 — 클라 코드 전수조사 (마감)

## TL;DR

**ADR-022 새 하네스 v1의 *복잡 등급 + 단일 도메인 분해 패턴 첫 실측*** — Phase 04 (대규모) Coordinator 분해 대비 본 Phase는 *메인 세션 직접 분해* 선택 (Coordinator 비-호출). client Worker 시나리오 8개 + unity-bridge 읽기 자문 3 질문 + reviewer Tier 2-A 5축 통합. **클라 측 시각 = 헌법 #1 (서버 권위) 준수 검증 0 위반** (서버 측 Phase 04 = 위반 가능성 점검과 본질적 비대칭).

**핵심 메타 발견 ★★★** (3건):
1. **false-promise 4번째 변종 후보 = "문서 내 자기 불일치"** — `04_ClientNet/CLAUDE.md:42` 같은 문서 안에서 같은 경로를 두 형태로 박음 (`Plugins/Client.Net/` vs `Plugins/ClientNet/`). git diff에 안 잡힘 — grep 검증 필요. M3.6 누적 11건 중 4번째 변종 후보.
2. **클라 vs 서버 감사 시각 본질적 비대칭** — 서버 = 헌법 위반 가능성 점검 / 클라 = 헌법 #1 준수 검증. 동일 5축 체크리스트로 점검하되 위험도 분포가 본질적으로 다름.
3. **단일 도메인 + 복잡 등급 = Coordinator 비-호출 정합** — Phase 04 (대규모) Coordinator 호출 정합 대비 본 Phase는 *분해 비용 > 가치*. 메인 세션 직접 분해 + client Worker 단독 + unity-bridge 읽기 자문 + reviewer 자동 = 3 SubAgent로 충분.

**Worker 통합 결과**: P0=0 / P1=2건 (AttackRangeSq 외부화 + HudController 패킷 연결 — *둘 다 M4 backlog 묶음*) / P2=4+2건 (Phase 06 종합 마감 묶음 1건 + 별 시점 backlog 5건) → **본 Phase 봉합 옵션 A** (04_ClientNet/CLAUDE.md 1줄, ~2 단어).

## AC 검증 결과

### 1. client Worker 점검 결과 박힘 (본인 영역) ✅

client Worker (Sonnet, 106k tokens / 518s, 시나리오 8건):
- A. Network/ — PASS (P2 발견 2건 — NetworkBootstrap UI 통합 TODO + MainThreadDispatcher priority queue OK)
- B. Prediction/ — PASS (M4 backlog 정합 = jump Y-axis mispredict M4 봉합 약속 명시)
- C. Rendering/ — PASS (sprite bottom pivot foot alignment 정합, EnemyRegistry visualFootOffset 보정)
- D. Input/ — PASS, **P1 발견 1건** (AttackRangeSq=9.0f 클라 하드코딩 + 98_Shared/GameData/Constants.cs 미정의 = 역방향 false-promise)
- E. State/ — PASS (RemoteEntityRegistry Dictionary + spawn/despawn idempotent + 보간 200ms 정합)
- F. Prefabs/ — PASS (RemotePlayer_new.prefab 정체 unity-bridge 위임 명시)
- G. 04_ClientNet/ — PASS (P2 발견 1건 — SmokeProbe.cs dead code 후보)
- H. 유현 영역 경계 보고 — PASS (변경 0건, 영역 경계 모호 케이스 3건 박음)

### 2. 04_ClientNet/ Y2 정합 확인 ✅

- `.NET Standard 2.1` 단독 (`<TargetFramework>netstandard2.1</TargetFramework>`)
- `UnityEngine` 참조 0건 (Y2 분리 정신 정합)
- DLL 자동 복사 파이프라인 (`CopyToUnityPlugins` PostBuild target) 실측 OK
  - `03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll` 존재 확인
  - `03_Client/Assets/Plugins/Shared/Shared.dll` 존재 확인

### 3. 유현 영역 경계 보고 자료 박힘 (변경 0건) ✅

- 점검 파일 5건 — Bootstrap/SceneBootstrap.cs + Scripts/UI/HudController.cs + 3건 더 (보고만)
- UI.unity = git log만 (YAML 보지 않음, prefab 사고 학습 정합)
- CODEOWNERS 3경로 @jungyoohyun0105 단독 정합
- 영역 경계 모호 케이스 3건 발견 → **별 파일 `_yuhyeon-area-review-2026-05-22.md` 박음** (Phase 06 종합 보고에서 인용)

### 4. unity-bridge 읽기 자문 결과 박힘 (변경 0건) ✅

unity-bridge (Sonnet, 50k tokens / 263s, 질문 3건):
- **Q1 RemotePlayer_new.prefab 정체 = 실제 운용 prefab** (Gameplay.unity GUID `5b7701fc...` 참조). RemotePlayer.prefab이 오히려 방치 구버전. 이름 정리 P2 backlog.
- **Q2 prefab variant 체인 PASS** — PlayerBase → LocalPlayer / RemotePlayer_new (정상). 주의 2건 (RemotePlayer 이름 역전 + LocalPlayer m_Name "LocalPlayer_new" 오염).
- **Q3 백업 cadence ADR 미존재** — .backup.prefab git tracked 안전 + 정리 시점 ADR 신설 P2 후보. 무한 누적 위험.
- 변경 0건 (읽기 자문 의무 완수).

### 5. reviewer 5축 점검 결과 박힘 (P0/P1/P2 분류) ✅

reviewer Tier 2-A (Opus, 105k tokens / 309s):
- 축 1 헌법 5/5: **PASS** ✅ (재검증 통과, 핵심 증거 line 단위 박음)
- 축 2 ADR 정합: PASS (ADR-010/012/019/021/022 모두 정합)
- 축 3 ARCHITECTURE: PASS (prediction+reconciliation + local-vs-remote branch + HandshakeOk gate + dispatch 흐름)
- 축 4 테스트: PASS (보조 의견 1건 — handler dispatch 단위 테스트 부재, 헌법 정합)
- 축 5 도메인 패턴: **PASS + false-promise 11건 누적 정착** (4 변종 발견)
- **추가 발견 1건 (P2)**: `04_ClientNet/CLAUDE.md:42` path 자기 불일치 (4번째 변종 후보)
- **AttackRangeSq 헌법 #1 위반 재판단 = 비위반 확정** (서버 측 CombatConstants.cs:24 재검사 정합 + 클라 9.0f = display hint = 핵 위협 0)

### 6. P0 발견 0건 → 본 Phase 즉시 봉합 의무 = 면제 ✅

P0 = 0건. client Worker + unity-bridge + reviewer 모두 헌법 5/5 위반 0건 확인.

### 7. dotnet build green 유지 (04_ClientNet/ 영역) ✅

본 Phase 봉합 (04_ClientNet/CLAUDE.md 1줄) 후 `dotnet build Dawnholder.slnx` 실측 = **경고 0 오류 0 GREEN** (변경이 .md 문서 1개뿐, csproj/코드 영향 0건).

### 8. Unity batchmode compile 첫 실측 통과 (β3 봉합) ✅

client Worker 실측: `Unity.exe -batchmode -quit` 시도 → exit code 0 + Editor.log 에러 0건. **β3 봉합 첫 실측 통과** = "Unity 미검증 리스크" 명시 의무 면제. (단 라이선스 offline activation 우회 가능성 명시 — exit code 0이 컴파일 통과만 보장, 전체 play test 별 영역).

### 9. -DONE.md 박힘 (복잡 등급) ✅

본 파일. 5단계 보고 + HTML 페어 면제 (대규모 등급만 의무, 복잡 등급은 단순 양식).

## 결정 흐름

### §1. Coordinator 비-호출 결정 (Phase 04 대비)

**비-호출 선택 사유**:
- 단일 도메인 (client + 04_ClientNet 둘 다 client SubAgent 권한)
- 복잡 등급 (Phase 04 대규모 vs 본 Phase 복잡 = 한 단계 낮음)
- Phase 04 메타 발견 ★★★ `coordinator-decomposition-in-action` 정합 = "단일 도메인 + 복잡 등급 = Coordinator 호출 비용 > 가치"

**대안**: 메인 세션이 직접 시나리오 8개 명세 박은 prompt → client Worker 단독 호출 → unity-bridge 읽기 자문 (병렬) + reviewer Tier 2-A (병렬) 3 SubAgent.

**결과 검증**: Worker 결과 + unity-bridge 자문 + reviewer 추가 발견 모두 정합 통과. **Coordinator 비-호출 정합 1회차 실측 박힘** (Rule of Three 1/3).

### §2. P1 봉합 분기 결정 (reviewer 권장 + 본인 판단)

**P1 = 2건 모두 M4 backlog 묶음 결정**:

1. **AttackRangeSq 9.0f 외부화** — 98_Shared/GameData/Constants.cs 추가 = 헌법 #4 (Shared Discipline) 정합 의무 = shared + client 2 도메인 변경. **M4 Formulas.cs 박힘 약속과 묶음 가치 ↑** (Phase 04 AC 검증 §2 정합).
2. **HudController mock → 실제 패킷 연결** — 서버 측 자원 갱신 패킷 신설 + 클라 dispatch = 보통 등급 작업. **M4 전투 패킷 설계와 묶음 자연스러움** (forward false-promise 변종, 명시적 미래 약속 박힘).

### §3. P2 봉합 분기 결정 (옵션 A 변형)

**옵션 A 변형 = 본 Phase 1건 봉합 + Phase 06 묶음 0건 + 별 시점 backlog 5건**:
- 본 Phase 묶음: `04_ClientNet/CLAUDE.md:42` path 정정 (~2 단어, 단순 등급, client SubAgent 권한 내)
- 별 시점 backlog (5건):
  - `NetworkBootstrap` UI 통합 TODO (M4 자연 봉합)
  - `StageClearUI.BuildRuntime()` Reflection IL2CPP risk (배포 빌드 시점)
  - `RemotePlayer_new.prefab` 이름 정리 (unity-bridge 작업, M4 backlog)
  - `SmokeProbe.cs` dead code 재검토 (M5+ 정기 감사)
  - `.backup.prefab` 정리 cadence ADR 신설 (별 시점 ADR 후보)

**reviewer 권장 (Phase 06 묶음) 대비 본인 판단 = 본 Phase 봉합**:
- 1줄 + client 권한 내 + 봉합 비용 ~30초 = 본 Phase 묶음이 자연
- Phase 06 마감 PR 깨끗 (P2 부담 ↓)
- false-promise 4번째 변종 발본 *즉시* (학습 자산 박음 시점과 봉합 시점 일치)

## 학습 일지 후보 키워드 (트랙 B 박을 시점)

### ★★★ (3건 신규)
- **`region-boundary-audit-without-change`** — 변경 0건 보고만 패턴. CODEOWNERS + 영역 경계 점검 정합. 한국 게임 회사 *팀 작업 경계* 어필 결정타.
- **`asymmetric-decision-evidence-staging`** — 본인 + 유현 의논 시점 *근거 박제*까지 = 비대칭 의사결정 정합. `_yuhyeon-area-review-2026-05-22.md` 별 박음 = 패턴 첫 실측.
- **`false-promise-variant-4-document-self-inconsistency`** — 같은 문서 안에서 같은 사실 두 형태로 박음 (`04_ClientNet/CLAUDE.md:42`). git diff에 안 잡힘 — grep 검증 필요. M3.6 4번째 변종 후보 (forward + 역방향 + 시기상조 + 자기 불일치).

### ★★ (4건 신규)
- **`handshake-race-window-main-thread-gate`** — 소켓 워커 스레드 → main thread MainThreadDispatcher.Enqueue → 같은 스레드 visibility 보장 패턴. M3 Phase 02 race window 봉합 정합.
- **`runtime-code-gen-scene-yaml-avoidance`** — 씬 YAML 편집을 RuntimeInitializeOnLoadMethod + 런타임 코드 생성으로 회피. 영역 격리 + git 충돌 차단 동시 달성 (CombatBootstrap + StageClearUI + EnemyRegistry).
- **`serialized-field-reflection-il2cpp-risk`** — [SerializeField] private + Reflection 패턴 = Mono 동작 / IL2CPP stripping 차단. link.xml 또는 [Preserve] 봉합책. 배포 빌드 함정.
- **`coordinator-non-call-single-domain-complex`** — 단일 도메인 + 복잡 등급 = Coordinator 비-호출 정합 1회차 실측 (Phase 04 대규모 호출 정합과 대비).

### ★ (2건 신규)
- **`local-vs-remote-entity-branch-dispatch`** — LocalEntityId 비교 분기. 본인 prediction reconcile / 타인 보간 buffer. 서버 entityId 박는 이유 = 분기.
- **`enemy-static-sprite-scene-reload-lifetime`** — static Sprite 필드 씬 재로드 후 잔존. 다중 씬 설계 시 Unload 콜백에서 null 처리 필요.

## 관련 파일

### 본 Phase 변경 (1건)
- `04_ClientNet/CLAUDE.md:42` — path 정정 (`Client.Net.dll` → `Dawnholder.Client.Net.dll` + `Plugins/Client.Net/` → `Plugins/ClientNet/`)

### Phase 별 박음
- `01_Phases/youngho/M3.6-harness-and-codebase-audit/05-client-codebase-audit.md` (Phase 정의)
- `01_Phases/youngho/M3.6-harness-and-codebase-audit/_yuhyeon-area-review-2026-05-22.md` (유현 재논의 자료 — 별 박음)

### 점검 (변경 0건)
- `03_Client/Assets/Scripts/Network/UnityClientSession.cs` (HandshakeOk gate, LocalEntityId 분기)
- `03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs` (input replay reconcile)
- `03_Client/Assets/Scripts/State/RemoteEntityRegistry.cs` (Dictionary + idempotent)
- `03_Client/Assets/Scripts/State/RemoteEntity.cs` (보간 200ms)
- `03_Client/Assets/Scripts/Input/LocalPlayerController.cs` (AttackRangeSq 67줄 — M4 backlog)
- `03_Client/Assets/Scripts/Combat/EnemyRegistry.cs` (TryGetNearest 정합)
- `03_Client/Assets/Scripts/UI/StageClearUI.cs` (Reflection IL2CPP risk — 별 시점)
- `03_Client/Assets/Scripts/UI/HudController.cs` (forward false-promise — M4 backlog)
- `03_Client/Assets/Prefabs/Characters/` (5 prefab — unity-bridge 자문 결과 박힘)
- `04_ClientNet/Dawnholder.Client.Net.csproj` (Y2 + ADR-010 정합)
- `.github/CODEOWNERS` (3경로 @jungyoohyun0105 단독 정합)

### 비교 baseline
- `04-server-codebase-audit-DONE.md` (Phase 04 서버 측 결과, 클라 vs 서버 시각 비대칭 비교)
- `02_Server/GameServer/Combat/CombatConstants.cs:24` (서버 AttackRangeSquared=9.0f, 클라 9.0f와 매치 = display hint 정합)

## ➡️ 다음 Phase

- **Phase 06** (외부 리뷰 4건 흡수 + 종합 마감) — Phase 04 + 05 둘 다 끝났음. 진입 가능.
- 본 Phase는 Phase 06 종합 보고에서 *유현 영역 재논의 자료* + *false-promise 4번째 변종 후보* + *클라 vs 서버 시각 비대칭* 3건 인용.
