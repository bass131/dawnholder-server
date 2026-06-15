---
owner: youngho
milestone: M7
type: 사운드 청음 튜닝 워크시트
date: 2026-06-16
---

# M7 사운드 개선 리스트업 (청음 피드백용)

> **현황**: AI 생성 25개 전부 **10.03초**(durationInSeconds 미지정 → ElevenLabs 기본값). 영호 원본 die 3개(enemy/golem/boss)는 0.6~1.7s 정상 → 유지.
>
> **피드백 방법**: 각 항목 "피드백" 칸에 자유롭게 — 예) `0.3s로 / 더 둔탁하게 / 톤 웅장하게 / 좋음 그대로 / 이건 빼` 등. 길이만 OK면 "길이만"이라고 적어도 됨. 그러면 그 지시대로 재생성(기존 삭제 후 재생성, " 1" 중복 방지).
>
> **재생성 메커니즘 메모**: 같은 경로 재생성 시 덮어쓰기 안 되고 `name 1.wav` 중복 생성됨 → 실제 작업 땐 기존 파일 rm 후 생성.

## ⚔️ 전투 SFX

| 키 | 트리거 | 현재 | 제안길이 | 프롬프트 핵심 | 피드백 |
|---|---|---|---|---|---|
| melee_swing | Knight 평타 스윙 | 10.03s | **0.5s** | sword swing, metallic whoosh, punchy | |
| magic_cast | Mage Thunderbolt(Q) | 10.03s | **1.0s** | arcane cast, sparkly energy charge, shimmer | |
| projectile_launch | Mage 투사체 발사 | 10.03s | **0.6s** | magic projectile, airy zip, whoosh | |
| dash | Knight Dash | 10.03s | **0.5s** | fast dash, wind swipe, low whoosh | |
| teleport_depart | Mage 텔포 출발(E) | 10.03s | **0.9s** | teleport vanish, rising shimmer, warp pop | |
| teleport_arrive | 텔포 도착 | 10.03s | **0.9s** | teleport appear, descending shimmer, chime | |
| hit_enemy | 적 타격(근접/투사체/대시) | 10.03s | **0.4s** | meaty impact thud, smack, punchy | |
| hit_lightning | 낙뢰 명중 | 10.03s | **0.6s** | lightning impact, electric crackle, zap | |
| hit_player | 플레이어 피격 | 10.03s | **0.5s** | body impact, soft grunt thud | |
| enemy_attack | 몬스터 공격 | 10.03s | **0.6s** | monster attack swing, aggressive whoosh, growl | |
| stage_clear | 스테이지 클리어 | 10.03s | **2.2s** | victory fanfare sting, orchestral flourish | |

## 🏃 이동 SFX

| 키 | 트리거 | 현재 | 제안길이 | 프롬프트 핵심 | 피드백 |
|---|---|---|---|---|---|
| jump_start | 점프 | 10.03s | **0.5s** | jump launch, quick whoosh, spring | |
| jump_land | 착지 | 10.03s | **0.4s** | landing, soft footstep thud | |
| footstep | 걸을 때(~0.32s 간격) | 10.03s | **0.3s** | single footstep on dirt, soft step | |

## 💀 플레이어 / 존

| 키 | 트리거 | 현재 | 제안길이 | 프롬프트 핵심 | 피드백 |
|---|---|---|---|---|---|
| player.death | 플레이어 사망 | 10.03s | **1.2s** | death, descending fail tone, body thud | |
| portal_enter | 포탈 진입 whoosh | 10.03s | **0.8s** | portal teleport whoosh, warp swell | |

## 🖱️ UI SFX

| 키 | 트리거 | 현재 | 제안길이 | 프롬프트 핵심 | 피드백 |
|---|---|---|---|---|---|
| button_click | 버튼 클릭 | 10.03s | **0.4s** | UI button click, digital blip | |
| panel_open | 패널 열기(대화/일시정지) | 10.03s | **0.6s** | panel open, upward swoosh, chime | |
| panel_close | 패널 닫기 | 10.03s | **0.5s** | panel close, downward swoosh | |
| toast | 토스트 알림+퀘스트 발생 | 10.03s | **0.7s** | notification chime, mellow bell ping | |
| error | 거부/에러(포탈잠김 등) | 10.03s | **0.5s** | error denial buzz, low negative blip | |
| party_invite | 파티 초대 수신 | 10.03s | **0.7s** | invite notification, two-tone rising ping | |
| party_formed | 파티 결성 | 10.03s | **0.8s** | party formed chime, warm two-note | |
| party_disbanded | 파티 해산 | 10.03s | **0.8s** | disbanded descending chime, farewell | |
| quest_complete | 퀘스트 완료 | 10.03s | **1.8s** | quest complete jingle, triumphant chime | |

## ✅ 유지 (영호 원본, 손 안 댐)

| 키 | 길이 | 비고 |
|---|---|---|
| enemy_die (슬라임) | 0.63s | 정상 |
| golem_die | 1.23s | 정상 |
| boss_die (뱀파이어) | 1.72s | 정상 |
| BGM ×5 (mainmenu/town/hunting/boss/ending) | 길게 OK(루프) | ending만 AI(Lyria), 나머지 영호 원본 |

## ❓ 미해결

- `player.respawn` — 미생성/미wiring. 리스폰 트리거 결정 필요(HP 0→복구 감지 방식).

---

**영호 피드백 후 액션**: 지시대로 ① 대상 키 기존 .wav 삭제 → ② durationInSeconds 지정 + (톤 지시 있으면) 프롬프트 수정해 재생성 → ③ import(mono/Decompress) → ④ 클립 길이 재측정 검증 → ⑤ 재빌드 → 영호 재청음.
