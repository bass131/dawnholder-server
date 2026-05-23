---
name: knowledge-client-index
description: Client 도메인 (03_Client/ + 04_ClientNet/ Unity 패턴) 학습 캐시 인덱스
domain: client
maintainer: youngho
last_updated: 2026-05-24
---

# Client Knowledge — _index.md

> **누가 통독**: `client` SubAgent + `unity-bridge` SubAgent (둘 다 필수) + `coordinator` / `reviewer` / `plan-auditor` (R only)
> **함께 통독**: 본 캐시 + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| `prefab-overwrite-untracked-disaster` | PrefabUtility.SaveAsPrefabAsset 백업 없이 덮어쓰기 — untracked prefab은 git history도 없음 | prefab 신규 생성 / 수정 시 | M3 Phase 08 BackGround 사고 1건 |
| `unity-version-hash-pinning` | 같은 라벨(예: 6000.4.1f1) 다른 hash 가능, hash까지 통일이 정답 (`.gitignore`로 우회 X) | 신규 팀원 환경 셋업 / Unity Hub 카탈로그 churn 시 | 5/16 사건 (본인 + 정유현 hash 어긋남) |

---

## 디테일 본문

### `prefab-overwrite-untracked-disaster`

**증상**: 사용자가 박은 prefab(예: `BackGround.prefab` — 풀스크린 multi-layer 배경)이 `PrefabUtility.SaveAsPrefabAsset` 호출로 *백업 없이 덮어쓰기* → untracked 상태였으면 git history도 없어서 *복원 경로 0*.

**패턴**: Unity Editor MCP는 prefab 작업 시 *backup 의무 없음*. AI가 prefab 수정 시 stage 상태 확인 안 하고 진행 → 사용자 작업 통째 사라짐.

**봉합 (의무 절차)**:
```bash
# prefab 수정 *전* 반드시
git add 03_Client/Assets/Prefabs/<target>.prefab
git status --porcelain | grep <target>  # stage 확인

# 신규 prefab 생성 시 — 생성 직후 1차 commit
# 1차 commit이 빈 prefab이라도 *복원 baseline* 가치
```

`unity-bridge` SubAgent의 Hard Rule #1로 박힘 ([`../../agents/unity-bridge.md`](../../agents/unity-bridge.md) 46~60줄).

**사례**: M3 Phase 08 BackGround prefab 사고 (사용자 박은 풀스크린 배경 prefab을 본 AI가 덮어씀, 복원 비용 X). work-pin "주의할 약속"에 박힘.
**확신도**: 실측 1건. *방지 비용 << 사고 비용* — Rule of Three 기다리지 않고 즉시 절차화. M4 prefab 작업 전 *반드시* git add 약속.
**관련 키워드**: [[unity-version-hash-pinning]] (Unity 다인 함정), [[false-promise-pattern]] (절차 박혀도 안 지키면 가짜)

### `unity-version-hash-pinning`

**증상**: 본인(`8535861f39e1`)과 정유현(`336a400b9ea2`) 둘 다 라벨은 `6000.4.1f1`인데 hash 어긋남. `ProjectVersion.txt` churn으로 매 Unity 실행 시 흔들림.

**패턴**: Unity가 *re-spin* (라벨 그대로 빌드만 갱신)을 가끔 함. Hub 카탈로그가 시점마다 다른 hash 서빙. 라벨 통일만으로 부족 — *같은 라벨 다른 hash* 흔하게 발생.

**봉합**:
- `m_EditorVersion` (라벨) *+* `m_EditorVersionWithRevision` (hash) 둘 다 통일
- 동기화는 Unity Hub 딥링크로 hash까지 강제 설치 (Hub UI에 hash 검색 기능 없음) — `unityhub://VERSION/HASH`
- `.gitignore`는 함정 — 신규 팀원이 임의 Unity 버전으로 열어도 막을 안전망 사라짐
- 옵션 (B1) 같은 LTS minor 내 다음 패치 점프 = 깔끔 (재설치 부가비용 0)

**사례**: 5/16 사건 (본인+정유현 hash 어긋남).
**확신도**: 실측 1건. 신규 합류(인규) 환경 셋업 시 재발 확인 예정 (Rule of Three 후보).
**관련 키워드**: [[projectsettings-cloud-ping-pong]] (Unity 다인 함정 묶음), [[prefab-overwrite-untracked-disaster]] (Unity asset 사고 묶음)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *03_Client/ + 04_ClientNet/ Unity 측* 패턴을 담습니다:

- **포함**: prediction / reconciliation / remote entity registry / 보간 / Unity Editor MCP / prefab/scene 작업 패턴 / Unity 버전 함정 / Cloud ID 함정
- **제외**:
  - Protocol 모양 / PDL → [`../shared/`](../shared/_index.md)
  - 서버측 권위 / lifecycle → [`../server/`](../server/_index.md)
  - 환경 사고 / 툴 함정 (Unity와 무관한) → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../CLAUDE.md) "Server Authority" (클라 = 단순 렌더러)
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/client.md`](../../agents/client.md) + [`../../agents/unity-bridge.md`](../../agents/unity-bridge.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
- 2026-05-24 — `/harness-review all` knowledge-gc 후속. `unity-version-hash-pinning` 디테일 中 트랙 B(회고) 성격 1줄 제거 (트랙 A/B 경계 정신 정합).
