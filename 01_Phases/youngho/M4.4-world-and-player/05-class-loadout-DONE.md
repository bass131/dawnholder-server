---
owner: youngho
milestone: M4.4
phase: 05
title: 직업 장착 구조 — ClassConfig SO + IAttackStrategy + AnimatorDriver 교체
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.4 Phase 05 완료 (4 commits, 세션16). 캐릭터 선택값(PlayerPrefs)으로 Animator controller + 공격 전략이 ScriptableObject lookup으로 데이터 장착 — 조작 코드 직업 if/switch 분기 0. 로컬 애니 hybrid(Idle/Walk/Jump 로컬 예측 + Attack/Hit/Death 서버 animState 우선)로 PlayerAnimatorSync 은퇴 + 08b AnimatorDriver 계약 완성. PlayerStats.ForClass 신설로 서버/클라 직업 ternary 중복 봉합. 부수: Town EventSystem 중복 제거, GitHub Actions CI 테스트 안전망 신설(SAC 로컬 차단 분담). 검증 = build 0/0 + CI 392/0/4(로컬 SAC 차단 → ubuntu 러너 대체) + EditMode 49/0/0 + 봇 5/5(desync 0) + Play 실측(직업 2종 + M4.3 관측 체크리스트) + reviewer 🔴0. ProtocolVersion 8 불변.
---

# Phase 05 박제: 직업 장착 구조

**소요**: 세션16 단일 세션 — server/client Worker 병렬 + 메인 검수 + reviewer + 자산 작업 MCP 위임(사용자 명시)
**선행**: Phase 04 머지(PR #65) 직후 main `be5a3cb` 기준 새 브랜치

## TL;DR

캐릭터 선택 → Town 진입 시 **직업별 외관/모션/공격이 데이터로 장착**된다 (전사=Knight 모션+근접, 원거리=Mage 모션). `ClassConfig`(abstract SO)의 파생 에셋 2개가 Animator controller와 공격 전략 factory를 공급하고, 이동값은 `PlayerStats.ForClass`(98_Shared 신설)가 단일 출처 — 서버 `GameSession`과 클라 `LocalPlayerMovement`에 중복돼 있던 직업 ternary를 한 곳으로 수렴했다(헌법 #4). 로컬 플레이어 애니는 hybrid: 클라가 즉시 알 수 있는 Idle/Walk/Jump는 로컬 예측, 클라가 추측할 수 없는 Attack/Hit/Death는 서버 animState 우선(헌법 #1). 새 직업 추가 = SO 에셋 1개 (+새 공격 방식이면 파생 클래스 1개) — OCP 달성.

## 박제 사실

| 스테이지 | 산출 | commit |
|---|---|---|
| A shared+server | `PlayerStats.ForClass(CharacterClass)` 신설 (invalid→Warrior fallback 단일 출처) + `GameSession` ternary 교체 + Shared.dll 동반 | `aec4529` |
| B client 코드 | `ClassConfig`(abstract SO)+Knight/Mage 파생 / `ClassLoadout`(Resources/ClassConfigs lookup, fail-loud) / `AttackIntent` 공통 송신 + `KnightMeleeAttack`·`MageRangedAttack` / `ProjectileVisual`(순수 시각) / spawn 장착(LocalPlayerSpawner) / `LocalPlayerMotion` hybrid / 본인 분기 animState 전달 / PlayerAnimatorSync·NearestTargetAttackStrategy 은퇴 / EditMode 12케이스 | `56d1861` |
| C 자산 (MCP 위임) | ClassConfig 에셋 2개(Knight→Warrior, Mage→Ranger) + LocalPlayer.prefab 정리(missing-script 제거 + Motion·Driver 부착, 백업 동반) + RemotePlayer Knight controller(임시 고정) + MageProjectile placeholder + Town EventSystem 중복 제거(재bake byte-identical) | `3bcf31f` |
| D ci | GitHub Actions `dotnet-tests.yml` — ubuntu, PR+push 트리거, build→test 분리 | `3eed8f4` |

## 결정 흐름

1. **ClassConfig 파생형 (사용자 확정)** — 전략 SO 분리형(에셋 4개) 대비 에셋 2개로 입자 단순. 전략은 plain class 유지, SO는 `CreateStrategy()` factory만 — 런타임 상태(투사체 prefab 등)는 ctor 주입.
2. **이동값 SO 보유 금지** — `PlayerStats.ForClass` 단일 출처. SO에 중복 보유 시 서버와 영구 mispredict drift. 직업 분기 책임도 ForClass 한 곳 (조작 코드 grep 분기 0의 실현 수단).
3. **애니 hybrid = "클라가 추측 가능한 것"의 경계** — 이동 상태는 입력만으로 즉시 도출 가능(반응성 우선 로컬), 공격 적중/피격/사망은 서버 lag-comp 판정 결과라 클라가 미리 알 수 없음(서버 animState 우선, latch 8틱이 지속 보장). `ResolveAnimState` 순수 함수 분리로 EditMode 검증.
4. **투사체 = 순수 시각 + 프레임 이동량 도달 판정** — 콜라이더/물리 0 (판정은 서버 기확정 — 헌법 #1). 거리 임계(0.1) 방식은 프레임 이동량(~0.16)이 더 커서 타겟 주위 영구 진동 — 이동 전 잔여 거리 ≤ 이번 프레임 이동량이면 도달 처리.
5. **투사체 발사 타이밍 이월 (사용자 실측 피드백)** — 입력 즉시 발사가 공격 모션과 어긋남 → Animation Event(클립 프레임 콜백)로 스폰 시점을 거는 작업과 묶어 이월. placeholder prefab 보관 + Mage.asset 연결만 해제.
6. **Town EventSystem 제거** — UI 씬(Additive)의 EventSystem이 단일 출처. Town에만 중복이 있어 2개 동시 존재 경고 — HG/BossRoom과 대칭화. 씬 수정 → 재bake → bin byte-identical 확인(EventSystem은 타일맵 무관).
7. **SAC 게이트 → CI 분담 (사용자 결정)** — SAC 0x800711C7 간헐 차단이 로컬 `dotnet test`를 반복적으로 끊음(이번 마감도 지연). 서명 대안 비실용 판정(공인 인증서 유료, self-signed는 SAC 미통과). 본인 머신 = 빌드+봇 확정 / 테스트 안전망 = CI(ubuntu, SAC 무관) 구도로 전환.

## AC 검증 결과

- `dotnet build Dawnholder.slnx --no-incremental` → 경고 0 / 오류 0 (직접)
- `dotnet test`: 로컬 SAC 차단 → **CI run 27065133608 (ubuntu): 396 total / 392 passed / 0 failed / 4 skipped** — Phase 04 baseline 일치, 회귀 0
- Unity 컴파일 클린 (신규 타입 9 로드 + 은퇴 타입 2 제거 확인) + EditMode **49/0/0** (신규 12: FindConfig 4 + ResolveAnimState 8)
- 봇 신선 서버 일괄 **5/5 PASS** — M2BasicMovement `desync=(0.00, 0.00)` = ForClass 이동값 경로 prediction 일치 실증
- Play 실측 (본인): 직업 2종 외관/모션/속도(4 vs 6) 장착 + **M4.3 이월 관측 체크리스트 통과** (Knight 점프 Start→Peek 핑퐁 0 / 공격 두 모션 랜덤 혼합 / 피격·사망 모션) + EventSystem 2개 경고 소멸 + 포탈 맵 전환. 서버 로그 증빙: `Warrior — Hp:150 Atk:15 Def:5 Spd:4` / `Ranger — Hp:80 Atk:12 Def:2 Spd:6` / C_Attack 판정
- 조작 코드 직업 if/switch 분기 0 — grep 검증 (허용 잔존 = 선택 UI 본질 / default 값 표기 / handshake 범위 검증)
- reviewer 🔴0 / 🟡3 → 전부 해소 (prefab missing-script 제거 / ClassConfig 에셋 생성 / Main Camera.prefab 커밋 제외)
- ProtocolVersion 8 불변

## 학습 일지 후보 키워드

- 클라가 추측 *가능한* 상태 vs *불가능한* 상태 — 애니 hybrid가 헌법 #1을 가르는 경계
- 데이터 주도 분기 — if/switch → SO lookup (OCP: 새 직업 = 에셋 1개)
- 아트명(Knight/Mage) ≠ 프로토콜명(Warrior/Ranger) — 도메인 어휘 매핑을 데이터 필드로 연결
- 프레임 이동량 기준 도달 판정 (거리 임계 + 호밍 = overshoot 영구 진동 함정)
- 환경 차단의 구조적 우회 — 로컬/CI 검증 분담 (반복 대기 비용 > 인프라 1회 비용)
- SubAgent 산출물 검수 — 존재하지 않는 enum 값(CharacterClass.Mage)을 컴파일 전 발본

## 관측/이월

- **Mage 투사체 연출 이월** — Animation Event 발사 타이밍 작업과 한 묶음 (placeholder `MageProjectile.prefab` 보관, `Mage.asset` 연결 해제 상태). 시각 디테일 폴리싱 전반 = 기능 완료 후 일괄 (사용자 결정).
- **발판(one-way platform) 저작 미실시** — bake 로그 `platforms=0`. Phase 03 E 잔여(발판 저작+재bake)는 이번 Play 실측에 포함 못 함 — Phase 06 마감 전 본인 저작 or 명시 이월 결정 필요.
- **원격 직업 표시** — RemotePlayer Knight 임시 고정 유지. S_PlayerJoin class append(v9)는 M4.5 보스 Phase 묶음.
- **CI 후속**: actions/checkout·setup-dotnet v4 → Node.js 24 강제(2026-06-16~) deprecation annotation — v5 bump 한 줄 추후. 봇 시나리오의 CI 편입은 백로그.
- **Unity MCP 에셋 API 차단 패턴 박제** — DeleteAsset/CopyAsset 인터랙션 차단 → File.Copy 우회(guid 보존), RunCommand는 Shared.dll 미참조 → SerializedObject/reflection 우회 (memory 등재).
