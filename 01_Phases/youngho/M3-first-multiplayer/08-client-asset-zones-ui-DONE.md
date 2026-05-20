---
summary: M3 Phase 08 (b+c+cleanup) Unity Combat dispatch + 3-zone visual + Stage Clear UI + sprite/prefab 풀세트 + 정합 + multi-layer 배경 + GameplayTest sandbox 제거를 완료했다. 5/20 면담 응급 데모용 클라이언트 풀세트가 박혔고, 면담은 작업물 비평 없이 통과했다.
phase: 08-client-asset-zones-ui
work-id: phase08-client-asset-zones-ui
status: done
completed_at: 2026-05-20
commits: "5e645c4 (PR #39, 08b/08c), d0f8f50 (PR #40, 08 cleanup)"
---

# Phase 08 — 클라이언트 자산 + 3-zone + UI + Stage Clear 완료 박제

**소요 시간**: 08b/08c + cleanup 합쳐 5/20 하루 집중.

## TL;DR

Phase 08은 server-side Phase 06+07 완결 흐름(Boss → StageClear)을 클라이언트에서 *시각화*하는 응급 작업이다. Combat 패킷 dispatch 연결, 단일 맵 3-zone trick, Knight/Mushroom/ToxicFrog sprite + Fantasy Forest multi-layer 배경, 발바닥/HP bar 정합, Stage Clear UI placeholder 페이드인까지 박았다.

5/20 교수 중간 면담에서 풀세트로 시연했고, 작업물에 대한 비평은 크게 없었다.

## 5단계 보고

- **무엇을 만들었나** — 08b (Unity Combat dispatch + 3-zone ZoneVisualizer + Stage Clear UI placeholder), 08c (Knight/Mushroom/ToxicFrog sprite 풀세트 + Fantasy Forest multi-layer 배경 + 발바닥 정합 + HP bar 위치 정합), 08 cleanup (GameplayTest sandbox 제거 + BackGround.prefab 신설 + Gameplay 씬 적용).
- **왜 필요한가** — Phase 06+07 server-side 완결을 5/20 면담에서 *시각적으로* 보여주려면 sprite + 배경 + 3-zone 시각 + Stage Clear UI까지 클라이언트 자산이 필요했다.
- **어떻게 만들었나** — 단일 맵에 3-zone trick (4맵 분리 인프라 회피), `EnemyKind`로 sprite 분기, sprite bottom pivot + 내부 그림 영역 hardcode 보정 (M4에서 sprite asset metadata로 외부화 예정), `RuntimeInitializeOnLoadMethod` 패턴으로 씬 YAML 격리.
- **테스트 결과** — server-side 170 PASS / 1 SKIP 안정 (Phase 06+07 회귀 없음). Unity 시각 시연 = headless-bot 자동 시연 2종(EmergencyCombatSmoke / BossStageClearSmoke) + 본인 직접 이동/공격 시연 모두 OK.
- **다음 스텝** — M3.5 (새 하네스 v1 문서화, `youngho/harness-v1` 브랜치) → M4 (진짜 4맵 + 정밀 전투, sprite asset metadata 외부화, 점프 Y mispredict 수정).

## 산출물 (08b / 08c / cleanup 분리)

### 08b — Unity Combat dispatch + 3-zone + Stage Clear UI (PR #39, `5e645c4`)
- `S_HitResult` / `S_EntityDeath` / `S_StageClear` Unity dispatch 연결
- `ZoneVisualizer` 컴포넌트 (단일 맵 3-zone 시각화)
- Stage Clear UI placeholder (페이드인)

### 08c — Sprite 풀세트 + 정합 (PR #39, `5e645c4`)
- Knight (player), Mushroom (normal enemy), ToxicFrog (placeholder)
- Fantasy Forest multi-layer 배경 (parallax 준비)
- 발바닥 정합 (sprite bottom pivot 가정 + 내부 그림 영역 hardcode 보정)
- HP bar 위치 정합

### 08 cleanup (PR #40, `d0f8f50`)
- GameplayTest sandbox 제거 (Phase 06/07 emergency 잔재)
- BackGround.prefab 신설 (사용자 박은 multi-layer 배경)
- Gameplay 씬에 BackGround 적용
- ZoneVisualizer 자동 생성 비활성 (씬에 직접 박힘)

## AC 검증 결과

### 1. Unity Combat dispatch (S_HitResult / S_EntityDeath / S_StageClear)

```text
PASS
헤드리스 봇 시연 시 server broadcast -> Unity dispatch 정상 연결.
EmergencyCombatSmoke / BossStageClearSmoke 양쪽 시연에서 클라 측
시각 반응 (HP bar 감소, death sprite, Stage Clear UI) 확인.
```

### 2. 단일 맵 3-zone 시각 (ZoneVisualizer)

```text
PASS
Gameplay 씬 Play 시 좌/중/우 3-zone 표식 표시.
PR #40 cleanup 후 ZoneVisualizer 자동 생성 비활성 — 씬에 직접 박힌
인스턴스 정상 동작.
```

### 3. Stage Clear UI placeholder (페이드인)

```text
PASS
Boss 처치 -> server S_StageClear broadcast -> Unity Stage Clear UI 페이드인.
5/20 면담 시연 + BossStageClearSmoke 시각 확인.
```

### 4. Sprite 풀세트 (Knight / Mushroom / ToxicFrog)

```text
PASS
EnemyKind 분기로 sprite 할당. Knight=player, Mushroom=normal enemy,
ToxicFrog=boss placeholder. Gameplay 씬에서 시각 확인.
```

### 5. Fantasy Forest multi-layer 배경 + BackGround.prefab

```text
PASS
사용자 박은 multi-layer 배경 BackGround.prefab 신설 (PR #40).
Gameplay 씬에 적용 -> 풀스크린 배경 + parallax 준비 OK.
```

### 6. 발바닥 정합 + HP bar 위치 정합

```text
PASS
sprite bottom pivot 가정 + 내부 그림 영역 hardcode 보정.
HP bar = entity 머리 위 일정 offset.
시연 시 시각 어긋남 없음.
```

### 7. GameplayTest sandbox 제거 (cleanup)

```text
PASS
PR #40 (d0f8f50)에서 emergency 잔재 제거.
회귀 없이 main 머지 완료.
```

### 8. 회귀 안정성 (server-side)

```text
PASS
dotnet test --no-build --nologo
통과 170 / 건너뜀 1 / 실패 0
Phase 06/07 server-side 회귀 없음.
```

### 9. 5/20 면담 end-to-end 시연

```text
PASS
- Unity Editor -> Gameplay 씬 Play
- 서버 기동 (dotnet run --project 02_Server/GameServer)
- 헤드리스 봇 시연 2종 (EmergencyCombatSmoke / BossStageClearSmoke)
- 본인 Unity 직접 이동/공격 시연 -> Boss 처치 -> Stage Clear UI

면담 결과 = 작업물 비평 없음 (사실상 통과).
```

## 면담 결과 (5/20)

- **작업물 비평**: 크게 없음 (사실상 통과)
- **다음 단계 방향 코멘트**: _(추가 코멘트 있었으면 사용자가 후속 채움. 없었으면 "없음"으로 마감)_
- **시연 임팩트 강조 포인트** (시연 시 풀로 전달):
  - 헌법 #1 서버 권위 (클라는 표시만, server-authoritative HP/damage/death)
  - 헌법 #3 신뢰 경계 (silent drop / attacker 강제 / rate-limit 6단계 검증)
  - Codex γ 사전 검증 6/7회차 (코드 박기 전 봉합)
  - 단일 맵 3-zone trick (4맵 분리 인프라 회피)
  - EnemyKind 통합 (별 entity 분리 X)
- **fallback 사용**: 안 함 (Unity 시연 정상 진행, 헤드리스 봇 콘솔 로그 fallback 불필요)

## 결정 흐름 (학습 일지 참고용)

- **4맵 분리 vs 단일 맵 3-zone trick** → 3-zone 채택. 응급 단계 인프라 부담 회피, 시연 임팩트 충분. 진짜 4맵 + 정밀 전투는 M4.
- **sprite pivot 정합** → bottom pivot 가정 + sprite 내부 그림 영역 hardcode 보정. M4에서 sprite asset metadata로 외부화 예정.
- **`RuntimeInitializeOnLoadMethod` 패턴** → 씬 YAML 격리. 코드 빌드만으로 zone 시각 박힘. trade-off: 코드 빌드 무거움 vs prefab 다이렉트.
- **BackGround.prefab 복구 사고** → `PrefabUtility.SaveAsPrefabAsset`이 *기존 prefab 백업 없이 덮어쓰기*. Unity 자동 백업 X. git untracked prefab은 더 위험. M4 시점 prefab 작업 전 *반드시* git add.
- **GameplayTest sandbox 제거** → emergency 단계 잔재. main 머지 직전 cleanup PR로 정리.

## 막혔던 지점

- **BackGround.prefab 복구 사고** — 위 결정 흐름 참조. *영역 침범 위험* 표면화 (영호가 prefab 영역 만지면서 사고). 새 하네스 v1의 `unity-asset` 위험 Hook + `unity-bridge` SubAgent로 *시스템 차원 차단* 예정.
- **sprite 내부 그림 영역** — bottom pivot 가정만으로는 정합 안 맞아 *그림 영역 hardcode 보정* 추가. 임시 해결, M4에서 metadata 외부화.
- **Unity MCP 시행착오** — Scene 캡처 + RunCommand로 prefab 작업 + sprite asset 자동 검증. 단 *덮어쓰기 권한*이라 사고 위험.

## 학습 일지 후보 키워드

- **★★★ sprite pivot 정합** (`sprite-bottom-pivot-internal-hardcode`) — bottom pivot 가정 + 내부 그림 영역 hardcode 보정의 trade-off.
- **★★★ 단일 맵 3-zone trick** (`single-map-three-zone-trick`) — 4맵 분리 인프라 부담 회피.
- **★★★ BackGround.prefab 복구 사고** (`prefab-overwrite-no-backup`) — `PrefabUtility` 덮어쓰기 위험 + git untracked 위험.
- **★★ `RuntimeInitializeOnLoadMethod` 패턴** (`runtime-init-on-load-scene-yaml-isolation`) — 코드 빌드만으로 씬 요소 박기.
- **★★ Unity MCP 활용** (`unity-mcp-prefab-and-sprite-validation`) — 5/20 신규. Scene 캡처 + RunCommand.
- **★★ 3 Agent 분화 영역 침범 사고** (`three-agent-territory-overlap`) — Claude/Codex/유현 분화에서 prefab 영역 침범. 새 하네스 v1의 *영역 격리 Hook*으로 해결 예정.
- **★ EnemyKind sprite 분기** (`enemy-kind-sprite-dispatch`) — 별 entity 분리 없이 sprite 분기.

## 의논 자산 (5/20 면담 후 별 흐름)

면담 후 *새 하네스 v1 모델*을 한 세션으로 풀어 박았다. 결과는 `youngho/m3-wrap` 브랜치 work-pin에 압축 저장, `youngho/harness-v1` 별 브랜치 (M3.5)에서 문서화 예정. 핵심:

- **KPI 전환**: "학습 박제 중심" → "Planning → 구현 → 보고" (학습은 트랙 B로 분리)
- **팀 합류**: 영호(server/shared) + 유현(client) + 인규(client + ComfyUI 2D 자산)
- **정량 4등급**: 단순 / 보통 / 복잡 / 대규모 + 위험 Hook 자동 상향 (trust-boundary / irreversible / unity-asset)
- **SubAgent 풀 8개**: server / shared / client / qa + reviewer / plan-auditor / unity-bridge / coordinator
- **Knowledge 시스템 풀세트** + GC Collector (PDF NDREAM 패턴 참조)
- **양식 다이어트**: work-envelope X, 5단계 보고 = 대규모 Phase 완료 시만, work-pin 유지(압축)
- **모델 분담**: Sonnet/Opus (PDF 그대로)
- **헌법 *부분 갱신***: 절대 원칙 5개 유지, 운영 양식 절 다이어트
- **/harness-review /cross-review 스킬화** (수동 트리거)
