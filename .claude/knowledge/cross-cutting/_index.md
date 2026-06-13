---
name: knowledge-cross-cutting-index
description: 도메인 횡단 (보안/툴 함정/마이그/환경 사고) 학습 캐시 인덱스
domain: cross-cutting
maintainer: youngho
last_updated: 2026-06-14
---

# Cross-Cutting Knowledge — _index.md

> **누가 통독**: **전 SubAgent 통독** (server / shared / client / qa / unity-bridge — 자기 도메인 _index와 *함께* 통독)
> **R only 통독**: coordinator / reviewer / plan-auditor (필요 시)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| `sac-dotnet-test-block` | Smart App Control On 환경 unsigned dotnet test dll 0x800711C7 차단 | `dotnet test` 실행 시 / SAC On 머신에서 새 hash dll 로드 시 | M3 Phase 04 (`5ea1123`) 실측 |
| `projectsettings-cloud-ping-pong` | Unity Cloud Services 다인 함정, pre-commit hook으로 cloud 라인 자동 unstage | Unity Editor 켤 때 / ProjectSettings.asset stage 시 | CHANGELOG 5/19 (모든 commit 영향) |
| `gamma-pre-validation-pattern` | Phase 정의·핵심 분기점에 외부 검증 (Codex γ → plan-auditor SubAgent 내재화) | `_milestone-plan.md` 박을 때 / Phase 정의 `.md` Write 시 | M3 누적 (γ 6/7회차) + `plan-auditor` agent |
| `riot-vanguard-spawn-unknown` | 사용자 PC 상주 Vanguard 드라이버가 Node child_process.spawn 차단 | VSCode C# Dev Kit "No Solution" / Docker WSL2 spawn 사고 | 5/16 1회 실측 (Rule of Three 미달) |
| `jump-buffer-ack-vs-apply-split` | 권위서버+client prediction에서 입력 버퍼링 시 ack(소비)≠효과(적용) 시점 분리 → 클라가 ack로 입력 evict해 reconcile replay 불가, 발산하면 snap | 입력 버퍼링(jump buffer/공격 버퍼/coyote) 추가 / prediction reconcile 설계 시 | M4.3 Phase 10b (`324dfb3`) Unity Play 실측 무해 (Rule of Three 미달) |
| `impulse-class-prediction-boundary` | 임펄스 예측 가능성의 경계 = 클라가 시작점(틱·방향·지속)을 아느냐 — self-initiated=예측, server-reactive=forceAdopt 채택 | 임펄스 동작(대쉬·넉백·lunge) 추가 / prediction 설계 시 | M4.13 P5·P6 (`f151e55`) |
| `dash-facing-client-authority` | §1은 적용·판정 권위지 입력 출처가 아님 — 방향/조준 등 클라 입력 파생값은 클라가 보내고 서버가 정규화·적용 | 방향성 스킬/조준 입력 추가 / §1 신뢰경계 판단 시 | M4.13 P5 (`52e5042`, Protocol v13) |

---

## 디테일 본문

### `sac-dotnet-test-block`

**증상**: `dotnet test` 실행 시 xUnit이 dll 로드 단계에서 `FileLoadException 0x800711C7` (`ERROR_APPLOCKER_APPLICATION_BLOCKED`). `dotnet build`는 통과 (생성 시점은 차단 X, *로드 시점*만 차단).

**패턴**: Smart App Control On 머신에서 unsigned dll의 *새 hash*를 dotnet test가 로드 시도 → Microsoft 시스템 정책 ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}`가 차단. 같은 환경에서 전 빌드는 통과한 적 있음 — reputation/캐싱이 우연히 매칭. *대량 hash 변경* (PDL 재생성 등) 시점에 표면화.

**봉합**:
- **단기**: 본 머신은 `dotnet build` green만 확정, dotnet test는 별도 환경(Codex / GitHub Actions) 위탁 (옵션 B 응급 모드)
- **장기**: WSL2 안에서 dotnet test 또는 OS 재설치 후 SAC Off 셋업. SAC On → Off는 OS 재설치 필요 (Microsoft 의도된 제약)

**진단 순서 (PowerShell)**:
```powershell
Get-MpComputerStatus | Select-Object SmartAppControlState  # On/Eval/Off
Get-WinEvent -LogName "Microsoft-Windows-CodeIntegrity/Operational" -MaxEvents 5  # Event 3077 = SAC
Get-WinEvent -LogName "Microsoft-Windows-AppLocker/EXE and DLL" -MaxEvents 5  # 무관 확인용
```

**사례**: M3 Phase 04 (commit `5ea1123`), 본 머신 첫 발화. CHANGELOG 5/18 박힘.
**확신도**: 실측 1건 (본 머신). Rule of Three 미달 — 다른 머신 발생 시 재확인.
**관련 키워드**: [[riot-vanguard-spawn-unknown]] (둘 다 머신 정책 사고)

### `projectsettings-cloud-ping-pong`

**증상**: 본인 commit이 팀원 cloudProjectId / organizationId 덮어씀 (또는 반대). main `e9aa005` = 유현 Cloud, 본인 머신 = `roy_131` Cloud → 머신 켤 때마다 ping-pong.

**패턴**: Unity Cloud Services는 머신별 자기 계정 자동 채움. `/session:start` (C-1) 게이트는 *세션 시작 시점*만 작동 → 세션 *중간* commit이 빠져나감.

**봉합**: `.githooks/pre-commit` 맨 앞에 cloud 라인 검사 + 자동 unstage. 패턴 10개:
- `cloudProjectId` / `organizationId` / `projectName`
- `cloudServicesEnabled` 블록 6 자식 (Build / Game Performance / Legacy Analytics / Purchasing / UDP / Unity Ads)

cloud 라인 *만* stage → 자동 unstage (워킹 디렉토리 보존 = 머신 식별 유지). cloud + 다른 변경 → block + `git add -p` 분리 안내.

**사례**: CHANGELOG 5/19 [M]. 본인·유현 5/16부터 발견.
**확신도**: 실측 검증 2회 통과 (단독 stage / 다른 파일 함께 stage 둘 다). 다인 환경에서 *모든 commit* 시점 적용.
**관련 키워드**: [[unity-version-hash-pinning]] (Unity 다인 함정 묶음)

### `gamma-pre-validation-pattern`

**증상**: Phase 박은 후 후속 Phase에서 함정 발견 — 사후 봉합 비용 ↑↑. 옛 운영은 Codex β 외부 검증(γ 방식)에 의존.

**패턴**: 큰 결정·Phase 정의를 *박기 전* 외부 검증 = 사후 발견 함정 절감. Codex γ 6/7회차 누적 실측:
- γ 4회차 (Phase 02): 7건 발견 → 4건 즉시 봉합
- γ 5회차 (Phase 03/04): Codex Phase 04 broadcast 패턴 1순위 권유 적중
- γ 6/7회차: Phase 06/07/08 사전 검증

**봉합**: M3.5 Phase 02에서 `plan-auditor` SubAgent로 내재화 — `_milestone-plan.md` / Phase 정의 `.md` Write 직후 자동 호출. 외부 Codex 의존 → 내부 자산 전환.

**사례**: M3 Phase 02/03/04/06/07/08 (commit `4065616`, `5ea1123`, etc).
**확신도**: 실측 6건 누적 (Rule of Three 통과 ★★★). 한국 게임 회사 백엔드 면접 *사전 검증 의사결정* 어필.
**관련 키워드**: [[false-promise-pattern]] (사전 검증으로 가짜 약속 차단 가능)

### `riot-vanguard-spawn-unknown`

**증상**: VSCode C# Dev Kit "No Solution" + `Failed: Spawn .NET server ... Error: spawn UNKNOWN`. Roslyn LSP 등 *일부* dotnet spawn은 통과, 특정 호스트만 거부 — Vanguard 휴리스틱 차단 시그니처.

**패턴**: Riot Vanguard (Valorant 안티치트, 커널 드라이버 `vgk.sys`)가 *항상 상주* (Valorant 미실행 시에도). 사용자 모드 우회 불가 (커널 후킹). Docker Desktop / WSL2 / Hyper-V 광범위 충돌 가능.

**봉합 (검증된 순서)**:
1. 트레이 Vanguard 우클릭 → Exit Vanguard
2. PC 재부팅 (드라이버 unload)
3. 부팅 후 트레이 아이콘 안 뜬 상태에서 도구 재시도

**장기 옵션**: A) Vanguard 제거 (게임 시 재설치) / B) 종료+재부팅 (매번 비용) / C) VM (학부생 PC 무거움) / D) WSL2 (Vanguard도 차단 사례)

**사례**: 5/16 1회 실측 (본인 머신).
**확신도**: 실측 1건. Rule of Three 미달. 다음 합류 팀원(인규/정우)에서 동일 사고 발생 시 ★★★ 승격 후보. 한국 학부생 + Valorant/LoL 인기 조합이라 빈도 ↑ 예상.
**관련 키워드**: [[sac-dotnet-test-block]] (둘 다 머신 정책 사고), ONBOARDING.md "한국 게이밍 PC 주의사항" 박제 후보

### `jump-buffer-ack-vs-apply-split`

**증상**: 권위 서버 + client prediction 환경에서 입력을 버퍼링(지연 적용)하면, "입력을 *받았다(consumed/ack)*"는 시점과 "입력 *효과*가 서버 상태에 반영됐다"는 시점이 갈림. 클라는 ack를 받으면 그 입력을 history에서 evict → 서버가 아직 효과를 안 냈으면 reconcile이 그 입력을 *replay 못 해* → 예측을 잃고 snap할 *이론적* 창.

**패턴**: jump buffer는 "입력 누락 → 입력 지연 적용"으로 바꾸는 표준 기법인데, prediction 환경에선 두 가지를 분리해 봐야 함 — (1) "consumed"는 ack로 *즉시* 알려야 클라 InputHistory 무한 누적 차단, (2) "effect 반영"은 *또 다른(나중)* 시점. 버퍼는 (2)를 미뤄 둘이 벌어짐. 일반화: *모든 지연-적용 입력*(공격 버퍼, coyote-time 등)에 동일 구조 잠복.

**봉합/판단**: 발산 폭이 SnapThreshold 안이면 self-correct(곧 후속 snapshot에 effect 반영)이라 무시 가능. **코드로 더 막기 전 런타임 1회 측정이 정답** (premature 봉합 회피). 발산 관측 시 옵션: (a) 버퍼된 입력은 *발사 시점까지 ack 보류* (history 증가 비용) / (b) 클라도 동일 버퍼 로직을 prediction에 미러 (양쪽 결정론 유지 비용).

**사례**: M4.3 Phase 10b (commit `324dfb3`) — 서버 jump buffer. reviewer가 설계 단계에서 ack/deferred 미스매치 깃발 → Unity Play 재검증으로 발산 0(reconcile snap 4→0) 확정 → 추가 봉합 불요.
**확신도**: 실측 1건. Rule of Three 미달 — 다른 지연-적용 입력(공격 버퍼 등)에서 재발 시 ★★★ 승격.
**관련 키워드**: [[gamma-pre-validation-pattern]] (설계 단계 사전 검증으로 깃발 → 측정으로 확정)

### `impulse-class-prediction-boundary`

**증상**: 클라가 예측 못 하는 서버 임펄스 동작(대쉬·넉백·임펄스공격)이 매 스냅샷 forceAdopt로 끌려와 50ms 시각 스터터. "전부 클라 예측으로 통일"하려다 넉백에서 막힘.
**패턴**: 임펄스 예측 가능성의 경계 = *클라가 시작점(시전 틱·방향·지속)을 아느냐*. self-initiated(대쉬/lunge — 내가 시전)는 시작점을 알아 StartImpulse로 직접 예측 가능. server-reactive(넉백 — 서버가 피격 시점 결정)는 신호가 RTT 후 도착(시작 틱 정렬 불가) + 방향은 추론(근사) + hitstun 지속은 서버 전용 → 예측 근거 부족 → forceAdopt(서버 위치 채택)가 정석. "전부 예측"이 아니라 원리적 구분이 있다.
**봉합**: 서버는 단일 경로(EnterAttackState/EnterHitState→DecayImpulse, P4 공유 공식). 클라만 갈림 — self-initiated=StartImpulse 예측(forceAdopt 불요), server-reactive=forceAdopt 채택. "예측이냐 채택이냐"는 우연이 아니라 클라가 시작점을 아느냐의 원리.
**사례**: M4.13 P5(`2e1b85e`/`dcf3b12` 대쉬·lunge 예측+크러치 제거) + P6(`6ad70da` 넉백 forceAdopt 영구 결정, `f151e55` 머지).
**확신도**: 실측 1건(M4.13). Rule of Three 미달 — 향후 방향성/신규 임펄스 동작 추가 시 재확인.
**관련 키워드**: [[jump-buffer-ack-vs-apply-split]] (예측/reconcile 입력 출처·시점 분리 동류), [[dash-facing-client-authority]] (클라 입력 파생값 권위)

### `dash-facing-client-authority`

**증상**: 방향 틀고 대쉬하면 의도 반대로 "빠바박" + reconcile 클러스터. 서버 FacingDir이 C_MoveIntent(입력 큐, 지연) 파생인데 대쉬(C_SkillUse, 잡 즉시)가 방향 입력을 추월 → 서버는 옛 방향, 클라는 새 방향 예측 → 반대 대쉬.
**패턴**: "§1 서버 권위"는 *적용·판정 권위*를 서버에 두라는 것이지 *입력 출처*를 서버가 만들라는 게 아니다. 방향/조준 같은 값은 *원래도 클라 입력 파생*이라, 클라가 보내고 서버가 정규화·적용하는 게 정석 — 신뢰 경계 안 넓힘.
**봉합**: C_SkillUse.facing append(Protocol v12→v13). 서버 ActionGate가 Dash일 때만 FacingDir=클라 facing 갱신(Validate 통과 후, 거부 시 부작용 0). §3 정규화(pkt.facing==1?1:-1). **패킷 추가 전 "클라가 이미 아는 정보로 추론 가능?" 점검** — 넉백 방향은 attacker 위치로 추론 가능했음(wire bump 회피 가능 사례).
**사례**: M4.13 P5(`52e5042` facing v13, `f151e55` 머지). reviewer 6축 0위반(§1 미위반 확인).
**확신도**: 실측 1건(M4.13). Rule of Three 미달 — 조준/방향성 스킬 추가 시 재확인.
**관련 키워드**: [[jump-buffer-ack-vs-apply-split]] (입력 출처/시점 분리 동류), [[impulse-class-prediction-boundary]] (클라 예측 경계)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *도메인 횡단 패턴*을 담습니다:

- **포함**:
  - 환경 사고 (한국 PC 함정: SAC / Vanguard / WDAC)
  - 툴 함정 (PacketGenerator noManager / Unity MCP 빈 응답 / Unity 버전 hash)
  - 마이그 패턴 (격리 폴더 → 일괄 mv 전환 / dual-write Phase)
  - 검증 패턴 (γ 사전 검증 / Rule of Three / 옵션 B 응급 모드)
  - 헌법 위반 패턴 (false-promise는 *코드/문서 정합*이라 `shared/`로, 횡단 본질이면 본 캐시)
- **제외**: 단일 도메인 패턴 — 각 도메인 _index로 분리

**판단 기준**: "이 패턴이 *어느 SubAgent에 영향*인가" — 1개면 해당 도메인, 2개 이상이면 cross-cutting.

---

## 관련 자산

- CHANGELOG: [`../../../../.claude/CHANGELOG.md`](../../../../.claude/CHANGELOG.md) — [H]/[M] 박힘 = 본 캐시 후보
- 옛 memory: `~/.claude/projects/C--Dev-ClaudeDev/memory/` — 흡수 후보 (Phase 04 (2/3))
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움 — cross-cutting은 시드 가장 풍부 (5건 이상 예상).
- 2026-06-14 — M4.13 마일스톤 마감 후속. `impulse-class-prediction-boundary` + `dash-facing-client-authority` 2건 박제 (사용자 확인 게이트 통과).
