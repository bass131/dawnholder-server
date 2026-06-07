---
owner: youngho
milestone: M4.5
phase: 05
title: 보스 클라 연출 + 원격 직업 표시 + HP 실연결 + Mage 투사체
status: done
grade: 대규모
risk: unity-asset
estimated: 5~7h
domain: client
---

# Phase 05: 보스 클라 연출 + 원격 직업 표시

> **상태**: done (2026-06-07 — 박제 = [`05-boss-client-and-remote-class-DONE.md`](05-boss-client-and-remote-class-DONE.md))
> **마일스톤**: M4.5
> **등급**: 대규모 (scope 확장으로 상향 — 1차 코드 + Prefab Variant 전환 누적 300줄+ & variant prefab 신규 저작. plan-auditor 🔴 봉합, 2026-06-07)
> **담당**: client SubAgent + unity-bridge (이펙트/prefab) + 메인 검수

---

## 🎯 목표

Phase 04 패킷을 클라가 소비한다: `S_EnemyAttack` 핸들러(피격 연출 + **HP 바 실연결 — mock 은퇴**), 보스 telegraph/패턴별 이펙트, `S_PlayerJoin.characterClass`로 **원격 플레이어가 상대 직업 모습**으로 보이게, 그리고 이월된 **Mage(Ranger) 투사체 연출**. 끝나면 보스방 양방향 전투가 화면에서 완성된다 — 발표 데모 클라이맥스.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (S_EnemyAttack + characterClass + v9)
- [ ] Phase 01 완료 (보스 prefab — 이펙트 부착 지점)
- [ ] Phase 03 완료 (HudController 정비 — UpdateHP 호출처)

---

## 📝 작업 내용

- [ ] `S_EnemyAttack` 핸들러: `HudController.UpdateHP(targetCurrentHp, max)` 호출 — **mock 초기값 은퇴** (`_mockHpCurrent` Start 호출 제거, 서버 값 단일 출처) + 피격 플래시/넉백 없는 시각 표시
- [ ] 보스 공격 이펙트: `attackPattern(byte)` 분기로 패턴별 연출 + telegraph 표시 (서버 animState 예고 틱 소비) — 서버는 논리, 클라는 표현
- [ ] 플레이어 사망→리스폰 연출: 리스폰 스냅 시 짧은 페이드(이질감 봉합 — M4.2 lifecycle 학습 재사용)
- [ ] 원격 플레이어 직업 표시: `S_PlayerJoin.characterClass` → `ClassConfig` lookup으로 원격 prefab Animator 장착 (M4.4-05 로컬 패턴의 원격 버전, 기존 원격 = 단일 모습 은퇴)
- [ ] **Mage 투사체 연출** (M4.4-05 이월): Ranger 공격 시 투사체 시각 효과 — *시각 전용*, 판정은 기존 서버 AABB 그대로 (헌법 #1 — AttackVariant 랜덤 선례)
- [ ] EditMode: characterClass → ClassConfig lookup 분기 + 미유효 폴백

### scope 확장 (2026-06-07 세션22 의논 — Prefab Variant 전환)

> **왜**: "단일 prefab + 런타임 controller swap" 전략은 직업별 시각 디테일(앵커/콜라이더/장식)을
> 에디터에서 저작할 그릇이 없음 — EffectAnchorOffset SO 필드 우회가 그 증상.
> 직업/적 종류별 **Unity Prefab Variant**로 전환해 "base = 로직 단일 출처, variant = 시각 저작 단위" 확립.

- [x] (v1 — 2차 commit d1a1bc1) `ClassConfig`에 LocalPlayerPrefab/RemotePlayerPrefab variant 참조 + Controller/EffectAnchorOffset 은퇴 + 늦은 직업 destroy 재생성
- [x] 오프셋 경로 은퇴 연쇄 한 묶음 (plan-auditor 🔴 봉합): MageRangedAttack 생성자 복원 + EffectAnchor offset 오버로드 은퇴 + EnemyAttackHandler 앵커 단일 컨벤션 (flipX 반전 유지)
- [x] 몬스터/보스: **코드 변경 0** — EnemyVisualTable이 이미 kind→prefab 테이블. 보스 EffectAnchor 저작(0.698, 1.216)은 본인 에디터 작업으로 완료 (4차 0d849bf)

### scope 확장 v2 (2026-06-07 세션22 재의논 — 로직/비주얼 분리)

> **왜**: v1(직업×Local/Remote = variant 4개)은 직업 시각 정체성이 **두 곳에 중복** —
> Knight 모습 수정 시 LocalPlayer_Knight + RemotePlayer_Knight 양쪽 동기 의무 (drift 구조).
> 사용자 지적으로 재설계: **로직 껍데기(Local/Remote base) / 직업 비주얼 prefab(직업당 1개) 분리**.
> 보너스: 늦은 직업 정보 = 비주얼 자식 교체만 — root 보존으로 보간 버퍼/매핑 자연 보존
> (v1의 destroy+재생성 긴장 관계 해소).

- [x] `ClassConfig`: LocalPlayerPrefab/RemotePlayerPrefab → **`VisualPrefab` 단일 슬롯** (직업당 비주얼 prefab 1개)
- [x] base prefab 수술: PlayerBase root의 SpriteRenderer+Animator 제거 (실측 발견 — LocalPlayer/RemotePlayer가 PlayerBase variant라 한 곳 전파) → 비주얼은 런타임 자식 장착
- [x] `ClassVisualMount` 헬퍼: root 아래 "Visual" 자식 교체 장착 — **순서 불변식: 옛 자식 비활성→파괴 → 새 자식 장착 → AnimatorDriver.Rebind()** (Awake 캐시가 장착 전 null/stale을 가리키는 함정 — plan-auditor 🔴 봉합)
- [x] `AnimatorDriver`: GetComponent → GetComponentInChildren + `Rebind()` 공개 (variant 파라미터 캐시도 리셋). 적 prefab은 root에 SR 보유 — InChildren self 포함이라 무영향
- [x] `DamageFlash`: SR 자식 탐색 + null 시 Flash()에서 지연 재바인딩 (늦은 장착 안전)
- [x] `EffectAnchor` 탐색: 직속 Find → **이름 재귀 탐색** (root>Visual>EffectAnchor 깊이 대응)
- [x] `EffectAnchor` 거울상 수학 교체 (plan-auditor 🔴 봉합 — v1 MageRangedAttack 동형 연쇄): `TransformPoint(anchor.localPosition)` 폐기 — localPosition은 *직속 부모(Visual) 기준*이라 root에서 곱하면 깨짐 → **`anchor.position`(world) 직접 + flip 시 `2*root.x − anchor.x` 거울상** (깊이 무관)
- [x] `RemoteEntityRegistry`: destroy+재생성 은퇴 → 비주얼 자식 교체 (`NeedsVisualSwap` 개명 — incoming null→false 강등 지뢰 차단)
- [x] EditMode 전체 green 재확인 (84/84 ×2)

---

## ✅ 완료 조건

- [x] 보스에게 맞으면 HP 바 실시간 감소 (mock 코드 0줄 — `_mockHp*` 필드 은퇴)
- [x] 보스 패턴별 이펙트 + telegraph 표시 Play 실측
- [x] 플레이어 사망 → 리스폰 → 전투 재개 데모 무중단 Play 실측
- [x] 2클라 실측: 상대가 상대 직업(Knight/Mage) 모습 + 모션으로 보임
- [x] Mage 공격 시 투사체 연출 (판정 변화 0 — 서버 코드 diff 0)
- [x] 클라에 데미지/HP 계산 코드 0 (표시만 — 헌법 #1) + EditMode green
- [x] (scope 확장 v2) 직업 비주얼 prefab 단일 출처 spawn — controller swap 코드 grep 0줄 + 늦은 직업 정보 시나리오(Snapshot 선도착 기본 비주얼 spawn → PlayerJoin Ranger 도착 → **비주얼 자식만 교체, root GameObject/RemoteEntity/보간 버퍼 미파괴**) Play 실측 통과 + 교체 후 Animator/SR 재바인딩 동작 확인

---

## 🧪 테스트

**자동**: EditMode — characterClass lookup/폴백 + 기존 UI 회귀
**수동**: 2클라 Play — 보스전 풀 루프(telegraph → 피격 → HP 감소 → 사망/리스폰 → 처치 → StageClear) + 원격 직업 상호 확인

---

## 📚 학습 포인트

- **패킷의 표현 힌트** — `attackPattern(byte)`: 서버는 "무슨 공격인지"만 알리고 어떻게 보일지는 클라 자유. 논리/표현 분리의 프로토콜 설계
- **mock의 수명** — Phase 03(M3)에 박힌 mock이 세 마일스톤을 살았다. "임시"는 은퇴 시점을 명시해야 임시다
- **시각 전용 연출의 경계** — 투사체가 날아가는 동안 판정은 이미 끝나 있음(서버 즉시 AABB). 연출과 판정의 시간 분리를 체감

---

## ⚠️ 함정 / 주의사항

- **투사체에 판정 욕심 금지** — "투사체가 닿을 때 데미지"로 바꾸면 서버 판정 재설계(발사체 엔티티 + 틱 추적)가 필요한 다른 마일스톤급 작업. 이번엔 시각만
- **mock 은퇴 시 첫 입장 초기값** — 서버 첫 snapshot/입장 패킷의 HP로 초기화 (mock 제거가 "빈 바"로 보이는 사고 방지)
- **원격 직업 장착은 컴포넌트 보존** — 유현 M3 약속(비주얼 교체 시 핵심 컴포넌트 보존) 준수. v2 로직/비주얼 분리가 이 약속과 *자연 정합*: root GameObject(RemoteEntity/보간) 보존 + "Visual" 자식만 교체 (v1 destroy+재생성 긴장은 v2로 해소)
- **1차 코드 commit 선행** (plan-auditor 🟡) — variant 전환이 같은 파일을 다시 덮으므로, reviewer 통과본(1차)을 먼저 commit해 diff 경계 박제
- prefab/이펙트 작업 전 백업 의무

---

## ➡️ 다음 Phase

- Phase 06 — 회귀 + 마감

---

## 📋 박제 (완료 후)

- **대규모 등급** — [`05-boss-client-and-remote-class-DONE.md`](05-boss-client-and-remote-class-DONE.md) + [`-DONE.html`](05-boss-client-and-remote-class-DONE.html) 5단계 보고 쌍둥이 박음 (2026-06-07)

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18 — HP 실연결 위치를 본 Phase로 확정[S_EnemyAttack 의존] + Mage 투사체 이월 흡수)
- 2026-06-07: 1차 코드 완료 (세션22 — 구획 A~E + 메인 정정 2 + reviewer 🔴0). 이후 사용자 의논으로 **scope 확장: Prefab Variant 전환** (직업별 시각 저작 그릇 부재 발견 — controller swap 전략 은퇴). 등급 대규모 상향 가능성 인지 (최종 diff 300줄+ 시). 보스 Attack 애니 의도 = Start 준비자세/End 발동 — P1 0.8s 클립 정합, P2(0.5s) 속도 배율은 telegraph 상수 98_Shared 이동 필요라 이월
- 2026-06-07: 2차 Variant 전환 commit d1a1bc1 (plan-auditor 🔴2 봉합 + reviewer 🔴0) + MCP로 variant 4개/이펙트 3개 생성 + EditMode 84/84 green. 직후 사용자 재의논 → **scope 확장 v2: 로직/비주얼 분리** (variant 4개 = 직업 시각 중복 drift 지적 — 직업당 비주얼 prefab 1개로 재설계, 3차 수정 GO)
- 2026-06-07: 3차 v2 commit 0dac4f9 (plan-auditor 🔴2 선봉합[거울상 수학/순서 불변식] + reviewer 🔴0 + EditMode 84/84 ×2) — ClassVisualMount/Rebind/world 거울상/NeedsVisualSwap + PlayerBase 수술 + KnightVisual·MageVisual 신설(본인 진짜 아트 교체)
- 2026-06-07: **2클라 Play 실측 통과** (보스전 풀 루프 + 원격 직업 상호 + Mage 투사체 — 사용자 확인 "전부 기능적으로 괜찮다") → 4차 본인 에셋분 commit 0d849bf (보스 EffectAnchor 0.698,1.216 + Test_Character 78파일 정리 + EffectAnchorTests.cs.meta 누락 봉합) → **done 박제** (-DONE.md + -DONE.html 5단계)
