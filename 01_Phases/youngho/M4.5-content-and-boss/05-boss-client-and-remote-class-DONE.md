---
owner: youngho
milestone: M4.5
phase: 05
title: 보스 클라 연출 + 원격 직업 표시 + HP 실연결 + Mage 투사체 + v2 로직/비주얼 분리
status: done
completed: 2026-06-07
grade: 대규모
summary: M4.5 Phase 05 완료 (세션22, 4 commits 120파일 +1394/−9416, 코드 19파일 +692/−64). Phase 04 패킷의 클라 소비로 보스방 양방향 전투가 화면에서 완성 — S_EnemyAttack(ID 20) 핸들러(HP 실연결 mock 은퇴 + DamageFlash + 패턴별 이펙트 + 리스폰 페이드) + S_PlayerJoin.characterClass 원격 직업 표시 + Mage 투사체(시각 전용, 서버 diff 0). 3회 구조 진화: 1차 controller swap(16e0af4) → 2차 Prefab Variant 4개(d1a1bc1, drift 지적으로 폐기) → 3차 v2 로직/비주얼 분리(0dac4f9 — ClassConfig.VisualPrefab 단일 슬롯 + ClassVisualMount + AnimatorDriver.Rebind + EffectAnchor world 거울상) + 4차 본인 에셋분(0d849bf). 검증 = EditMode 84/84 ×2 + plan-auditor 🔴4 선봉합 + reviewer 🔴0 ×3 + 2클라 Play 실측 통과(사용자 확인). 5단계 보고 시각판 = 05-boss-client-and-remote-class-DONE.html.
---

# Phase 05 박제: 보스 클라 연출 + 원격 직업 표시

**소요**: 세션22 — client Worker 구획 + Unity MCP prefab 수술/신설 + 메인 검수, 3회 구조 진화
**시각 보고서**: [`05-boss-client-and-remote-class-DONE.html`](05-boss-client-and-remote-class-DONE.html) — v2 런타임 조립 흐름 포함 (대규모 5단계 보고 HTML 박제)

## 5단계 보고

- 🎯 **무엇을 만들었나** — Phase 04 패킷 클라 소비: S_EnemyAttack 핸들러(HP 바 실연결 + 피격 플래시 + 패턴별 이펙트 + 사망/리스폰 페이드) + 원격 직업 표시 + Mage 투사체. 구조 산출 = v2 로직/비주얼 분리(직업 시각 = VisualPrefab 단일 출처). 상세 = TL;DR.
- 🤔 **왜 필요한가** — 보스 전투 논리는 서버에 완성됐지만 화면엔 안 보였음(HP mock 세 마일스톤 생존, 원격 = 단일 모습). v1 variant 4개는 직업 시각이 두 곳 중복 = drift 구조 → 사용자 지적으로 v2 재설계. 헌법 #1 — 클라 데미지/HP 계산 코드 0줄.
- 🛠️ **어떻게 만들었나** — 3회 구조 진화를 commit 경계로 박제(16e0af4 → d1a1bc1 → 0dac4f9 → 0d849bf). PlayerBase 수술 한 곳 전파(Local/Remote가 그 variant라는 실측 발견) + ClassVisualMount 순서 불변식 + EffectAnchor world 거울상. 상세 = 박제 사실 표.
- 🧪 **테스트 결과** — EditMode 84/84 green ×2(신규 12) + plan-auditor 🔴4 전부 선봉합 + reviewer 🔴0 ×3 + 2클라 Play 실측 통과(보스전 풀 루프 + 원격 직업 상호 + Mage 투사체 — 사용자 확인 "전부 기능적으로 괜찮다"). 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — Phase 06 회귀 + 마일스톤 마감. 이월 = 원격 Ranger 투사체(공격 이벤트 패킷 부재 — 구조급) / 보스 P2 애니 속도 배율(telegraph 상수 98_Shared 이동).

## TL;DR (🎯 무엇 / 🤔 왜)

보스방 양방향 전투가 **화면에서 완성**됐다 — 발표 데모 클라이맥스. 핵심 갈래 셋:

1. **패킷 소비 = 표시만 (헌법 #1)** — `S_EnemyAttack`(ID 20)의 `targetCurrentHp`를 그대로 HUD에 표시. 클라 계산은 max 조회(`PlayerStats.ForClass` — 98_Shared 단일 출처)뿐, 데미지/HP 수식 0줄. mock HP는 M3부터 세 마일스톤을 산 끝에 은퇴 — "임시"는 은퇴 시점을 명시해야 임시다.
2. **직업 시각의 단일 출처 (v2 로직/비주얼 분리)** — LocalPlayer/RemotePlayer = 로직 껍데기(base), 직업 모습 = `ClassConfig.VisualPrefab`(KnightVisual/MageVisual.prefab, 직업당 1개)을 런타임 "Visual" 자식으로 장착. Knight 모습 수정 = 한 파일 — 본인 화면/타인 화면 drift 구조 차단. 늦은 직업 정보는 비주얼 자식만 교체(root/RemoteEntity/보간 버퍼 보존 — 유현 M3 컴포넌트 보존 약속 정합).
3. **연출과 판정의 시간 분리** — Mage 투사체는 시각 전용(서버/98_Shared diff 0). 투사체가 날아가는 동안 판정은 이미 서버에서 끝나 있음(즉시 AABB). telegraph(P1 0.8s/P2 0.5s)는 서버 animState broadcast를 기존 경로(EnemyMotion→AnimatorDriver)로 소비.

## 박제 사실 (🛠️ 어떻게)

| 차수 | commit | 산출 |
|---|---|---|
| 1차 | 16e0af4 | `EnemyAttackHandler`(ID 20 — 본인 피격 = EffectAnchor 위치 + facing, HUD UpdateHP, DamageFlash, targetCurrentHp≤0 → PlayRespawnFade) / `HudController` mock 2필드 은퇴(MP/Gold는 서버 채널 부재로 유지) / `SceneTransition.PlayRespawnFade` — `_respawnFade` 별도 핸들로 `isTransitioning` 비점유(페이드 중 S_MapTransition silent drop desync 봉합 — 권위 전환이 이김) / `BossAttackEffectSpawner`(패턴별 Resources + facing flip) / `ClassLoadout.ByteToClass`(긍정 화이트리스트) / controller swap 경로(이후 은퇴) |
| 2차 (v1) | d1a1bc1 | Prefab Variant 4개(Knight/Mage × Local/Remote) + ClassConfig variant 참조 + EffectAnchorOffset/Controller 슬롯 은퇴 연쇄(plan-auditor 🔴 봉합 — MageRangedAttack spawnOffset 동형). **사용자 재의논으로 폐기** — variant 4개 기계 생성이라 손실 0 rm ("지금이 제일 싼 시점") |
| 3차 (v2) | 0dac4f9 | `ClassConfig.VisualPrefab` 단일 슬롯 / `ClassVisualMount`(순서 불변식: 옛 자식 **비활성→파괴** → 장착 → `Rebind()` — Destroy는 프레임 끝 지연 파괴라 비활성 없이 재캐시하면 죽어가는 컴포넌트를 잡는 함정) / `AnimatorDriver.Rebind`(InChildren 재캐시 + variant 캐시 리셋) / `DamageFlash` 지연 재바인딩 / `EffectAnchor` 이름 재귀 탐색 + **world 거울상 `2·root.x − anchor.x`**(localPosition은 직속 부모 기준이라 TransformPoint가 중첩 깊이에서 깨짐 — plan-auditor 🔴 봉합) / `RemoteEntityRegistry` 비주얼 자식 교체(`NeedsVisualSwap` 순수 함수 — **incoming null → false** 강등 지뢰 차단, 메인 검수 발견) |
| 에셋 (MCP) | 0dac4f9 | `PlayerBase.prefab` 수술(root SR+Animator 제거 — Local/Remote가 PlayerBase variant라는 실측 발견으로 한 곳 전파, 참조처 grep 안전 확인) / `ClassVisuals/KnightVisual·MageVisual.prefab` 신설(SR+Animator+EffectAnchor) / 보스 이펙트 P1·P2 + Mage 투사체 prefab / Knight·Mage.asset wiring — 전부 `Unity_RunCommand` + `PrefabUtility` (DeleteAsset 차단 → Bash rm + Refresh 우회) |
| 4차 (본인) | 0d849bf | Enemy_Boss.prefab EffectAnchor 저작(0.698, 1.216) / KnightVisual 진짜 아트 교체(Knight_Attack0) / Test_Character 옛 테스트 아트 78파일 정리 / EffectAnchorTests.cs.meta 누락 봉합 — **Play 통과 후 분리 commit**(AI 작업분과 경계 보존) |

**테스트 신규 12**: EffectAnchorTests 3(폴백/중첩 world/flipX 거울상 9.7 값 고정) + RemoteEntityRegistryTests 5(NeedsVisualSwap 계약) + ClassLoadoutTests 4(ByteToClass 화이트리스트).

## AC 검증 결과

- 완료 조건 7항목 전부 green: ① HP 바 실시간 감소 + mock 0줄 ② 패턴별 이펙트 + telegraph Play 실측 ③ 사망→리스폰→재개 무중단 ④ 2클라 상대 직업 모습+모션 상호 확인 ⑤ Mage 투사체(서버 diff 0) ⑥ 클라 계산 코드 0 + EditMode 84/84 ⑦ (v2) controller swap grep 0줄 + 늦은 직업 자식 교체 시나리오 + Rebind 동작
- EditMode **84/84 green ×2** (Unity TestRunnerApi MCP 실측 — GetConsoleLogs 빈 응답은 LogEntries reflection 기본 바인딩 우회로 교차 확인)
- plan-auditor 🔴 총 4건 전부 코드 착수 **전** 선봉합 (grade 상향 / spawnOffset 연쇄 / 거울상 수학 / 순서 불변식) + reviewer 🔴 0 ×3회
- 2클라 Play 실측(에디터 + 스탠드얼론 빌드, PlayerPrefs 분리로 직업 상이 선택) — 사용자 최종 확인 "전부 기능적으로 괜찮다"

## 결정 흐름 (회고 참고용)

- **controller swap → variant 4 → 로직/비주얼 분리** — 1차는 "직업별 시각 디테일을 에디터에서 저작할 그릇 부재"(EffectAnchorOffset SO 우회가 증상), 2차는 "직업 시각 두 곳 중복 drift". 구조 결함을 발견 즉시 갈아탐 — variant 폐기 비용은 기계 생성이라 0, "지금이 제일 싼 시점".
- **늦은 직업 = destroy+재생성 vs 자식 교체** — 자식 교체 채택. root 보존으로 보간 버퍼/entityId 매핑/유현 컴포넌트가 자연 보존 — 재생성의 상태 이관 코드 자체가 사라짐.
- **거울상 기준 = facing 부호 vs flipX** — flipX 채택. SpriteDefaultFacesLeft 스프라이트는 facing(+1)에서 flipX=true라 "facing<0 = 거울상"이 항상 참이 아님. 화면 진실 = flipX.
- **리스폰 페이드 = isTransitioning 점유 vs 별도 핸들** — 별도 핸들. 점유하면 페이드 1초 동안 S_MapTransition LoadScene이 silent drop → 씬 desync. 권위 전환이 리스폰 페이드를 중단하고 이김.
- **미상 직업 기본 비주얼 = 투명 vs Warrior** — Warrior. Snapshot 선도착 잔상(투명 엔티티) 방지, PlayerJoin 도착 시 NeedsVisualSwap이 교체.

## 막혔던 지점 / 이월 (➡️ 다음)

- **원격 Ranger 투사체 부재(구조급 이월)** — 타 클라의 공격 *이벤트* 패킷 자체가 없음(스냅샷 결과만 전파). 공격 이벤트 broadcast 신설 = 프로토콜 설계 동반, 별도 마일스톤급.
- **보스 P2 0.5s 애니 속도 배율 이월** — Attack 클립 0.8s 저작은 P1 정합, P2는 telegraph 틱 상수의 98_Shared 이동 후 배율 적용 필요.
- EffectAnchor 거울상 PlayMode 통합 테스트(EditMode는 수학만 고정) / 연속 사망 페이드 skip·Resources.LoadAll 캐싱(reviewer 🟡) 이월.
- Unity MCP 함정 누적 박제: AssetDatabase.DeleteAsset/CopyAsset 인터랙션 차단(File.Copy/rm 우회), 테스트 도메인 리로드 후 GetConsoleLogs 리셋(LogEntries reflection — using 차단이라 기본 바인딩 GetMethod만 가능).

## 학습 일지 후보 키워드

로직/비주얼 분리(시각 정체성 단일 출처) / Prefab Variant 상속과 base 수술 전파 / Destroy 지연 파괴와 비활성→파괴 순서 불변식 / world 거울상 수학(localPosition은 직속 부모 기준) / flipX = 화면 진실(facing 부호 함정) / mock의 수명(은퇴 시점 명시) / 연출과 판정의 시간 분리 / 구조 결함은 발견 즉시가 제일 싼 교체 시점 / incoming null 강등 지뢰(부정 조건 대신 명시 계약)

## 다음 Phase

- **Phase 06 — 회귀 + 마일스톤 마감** (M4.5 마지막. CHANGELOG [M] 줄 동승 — 본 Phase v2 구조 전환)
