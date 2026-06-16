---
owner: youngho
milestone: M7
phase: milestone-closeout
title: 게임 사운드 시스템 + 청음 튜닝 + 플레이테스트 폴리시 — 마일스톤 마감
status: done
grade: 대규모
summary: 게임 사운드 0 → 사운드 시스템(AudioManager 자기-부트스트랩) + 33키 적용 + AI 생성 25개(ElevenLabs SFX 24 + Lyria 엔딩 BGM 1). 야간 무인 1패스 후 영호 인터랙티브 청음 → 대대적 재튜닝(키 분리 적종류/직업/보스별 + 메이플 사운드디자인 참고 역할별 성격 재생성 + BGM 소스 증폭 + town 전환 버그) + 플레이테스트 폴리시(보스 빈방 재출현·적 중력/낙사 소멸·캐릭터선택 포트레이트 애니). ProtocolVersion v16 **무변경**(전부 클라 표현 + 서버는 기존 패킷 재사용). 검증 WSL2 657/0/5(신규 테스트 +12), reviewer 🔴0, Unity 컴파일 0err, 영호 인게임 청음/거동 검증 통과. origin/main +16 커밋 / 182 파일.
---

# M7 마일스톤 마감 — 게임 사운드 + 청음 튜닝 + 플레이테스트 폴리시

**마감 일자**: 2026-06-16 (야간 무인 1패스 + 영호 인터랙티브 청음/폴리시 세션)
**Phase 수**: 5 (A 인프라 / B wiring / C 에셋이관 / D AI생성 / E 밸런스·클로즈아웃) + 인터랙티브 폴리시 다수
**등급**: 대규모 (3+ 도메인 [클라 사운드 + 클라 UI/씬 + 서버 게임플레이] + 비가역 main 머지 + 300줄+)
**브랜치**: `feature/m7-sound` (origin/main +16 커밋, 182 파일, +2985/-551)

---

## TL;DR

게임에 **사운드가 0**이던 상태에서 한 마일스톤으로 **사운드 시스템 + 33개 사운드**를 입혔다. 야간 AutoMode가 인프라·wiring·에셋 생성 1패스를 완주하고, 영호가 일어나 **인게임 청음**으로 톤/볼륨/길이를 직접 검수하며 대대적 재튜닝을 지시했다. 핵심 재튜닝 3축: **① 사운드 키 분리**(단일 hit/attack → 적종류·직업·보스별로 쪼갬, 메이플스토리가 *각 이벤트에 어떤 성격의 소리를 쓰는지* 참고) **② BGM 소스 증폭**(원본이 엔딩보다 15~22dB 작아 안 들리던 것을 Unity 내에서 샘플 정규화) **③ town BGM 전환 버그 수정**. 곁들여 플레이테스트에서 드러난 게임플레이 3건(보스 빈방 재출현 / 적 중력·낙사 / 캐릭터선택 포트레이트 애니)도 폴리시. **ProtocolVersion v16 무변경** — 사운드는 순수 클라 표현, 서버 게임플레이는 기존 패킷(S_EntityDeath/S_EntityState) 재사용. 영호 GO로 push/PR/admin merge.

---

## AC 검증 결과

- **WSL2 회귀(ADR-029)**: 격리 `~/dawnholder-poc`에서 `dotnet build` → **0 error**, `dotnet test --no-build` → **Passed 657 / Failed 0 / Skipped 5 / Total 662** (야간 baseline 645 → +12 신규).
- **신규 테스트(서버)**: `BossEmptyRoomRespawnTests`(4) + `EnemyGravityTests`(8: 중력 4 + 낙사 4) = +12.
- **reviewer(Opus) Tier 2-A**: 서버 게임플레이 3건(보스 재출현 / 중력 / 낙사) 전부 **🔴 0**. broadcast 1틱 지연 = 무해 판정(적 = 미예측 보간 미러), kill-plane 지적 → 낙사 소멸 요구로 이어짐.
- **Unity 컴파일**: 사운드 wiring + 캐릭터선택 클립 재타겟 + AudioManager 전부 MCP **0 error**. 빌드 BuildPlayer **Succeeded**(726MB, 0err/0warn, 씬 7).
- **헌법 #1**: origin/main 대비 02_Server는 게임플레이만(패킷 무신설), **98_Shared / ProtocolVersion v16 무변경**. 사운드 전량 클라.
- **영호 인게임 검증(육안/청음)**: 사운드/BGM 33키 + 캐릭터선택 애니 + 적 중력·낙사 + 보스 재출현 = **이상 무**.

---

## 결정 흐름 (A vs B 중 A, 이유 / 단점)

1. **사운드 키 분리 = 적종류·직업·보스별** (vs 단일 키 스탬프). 메이플스토리가 이벤트마다 다른 성격의 소리를 쓰는 걸 참고 — `hit_enemy`→슬라임(물컹)/골렘(묵직)/통상, `hit_player`→기사/마법사, `enemy_attack`→슬라임/골렘 + 울음(`cry_*`), 보스→텔레그래프/찌르기. *단점*: 키·에셋 수 증가(13 신규). *이득*: 청각 피드백이 대상을 구분 → 타격감.
2. **BGM 소스 증폭 = 샘플 정규화 후 WAV 작성** (vs AudioSource.volume 상향). `AudioSource.volume`은 [0,1] 천장이라 클립 자체가 작으면 못 키움. 원본 OGG 4곡이 엔딩(Lyria)보다 RMS 15~22dB 작아 "town 안 들림 = 사실은 너무 작음". → Unity에서 `AudioClip.GetData`(★`LoadAudioData()` + DecompressOnLoad 필수)로 샘플 읽어 peak -1dBFS 정규화(hunting만 -3.4) → 16bit PCM WAV 작성 + OGG 삭제. *단점*: ffmpeg 없어 in-Unity 수작업. *이득*: 천장 우회한 진짜 음량 정렬(RMS -16~-17dB로 엔딩과 일치).
3. **사운드 = 전량 클라 표현, 서버 무변경** (vs 서버 사운드 이벤트 패킷 신설). 헌법 #1 — 사운드는 렌더 계층. 적 종류는 클라 `EnemyRegistry.TryGetKind`로 이미 알고, 직업은 `ClassLoadout`로 로컬에 있음. *이득*: ProtocolVersion 무변경 + co-review 회피.
4. **적 중력 = FSM 독립 매 틱 적용** (vs Chase/Patrol 상태에 중력 끼워넣기). `GameMap.ApplyEnemyGravity`가 FSM 바깥에서 `Physics.Step`(inputX=0) 재사용 → 수평 AI 보존 + 수직 중력만. Hit 상태에서도 낙하 가능(주석 박음). *이득*: AI 로직 무오염 + 결정론 물리 재사용.
5. **적 낙사 = 소멸 + 책임 분리** (vs HandleEnemyDeath 재사용). kill-plane 아래 낙하는 *killer 없는* 소멸 → `DespawnEnemyByFall` 별도 경로(S_EntityDeath 브로드캐스트 + RemoveEnemy, Normal/Golem=재출현 / Boss=제거, **StageClear·OnEnemyKilled 오발동 X**). *이득*: 전투 사망과 낙사 책임 분리, 보상/클리어 오작동 0.
6. **캐릭터선택 애니 = UI Image 바인딩 재타겟** (vs 게임플레이 SpriteRenderer 클립 공유). select 포트레이트는 UI `Image`라 클립이 `Image.m_Sprite`에 바인딩돼야 함. `AnimationUtility`로 SpriteRenderer→Image 재타겟 + loop. *함정/복구*: 초기에 게임플레이 Idle 클립을 잘못 Image로 바꿔 인게임 캐릭터가 정적포즈에 멈춤 → `git checkout`으로 SpriteRenderer 복원. 게임플레이=SpriteRenderer / select=별도 `ClassSelect/` 클립으로 완전 분리.

---

## 5단계 보고

### 🎯 무엇을 만들었나

- **사운드 인프라**: `AudioManager`(자기-부트스트랩 싱글톤, 프리팹 편집 0) — `PlaySfx(key,vol,throttle)` / `PlayBgm(key,fade)` 크로스페이드 / Master·BGM·SFX 볼륨(PlayerPrefs) / 누락 클립 no-op. SFX pool 12 + UI 소스 + BGM 2채널.
- **33 사운드 키 적용**: BGM 5 · 전투 14(적종류/직업/보스 분리 반영) · 이동 3(발소리=코드 티커) · 플레이어 1 · 존 1 · UI 9. 콜사이트 wiring 33곳.
- **AI 생성 에셋 25**: ElevenLabs SFX 24 + Lyria 엔딩 BGM 1. 기존 17개는 GUID 보존 이관(BGM 4 + die 3 재사용).
- **청음 재튜닝**: 키 분리(13 신규) + 메이플 사운드디자인 참고 역할별 성격 재생성 + 마법사/기사 anime young 보이스(귀신화 원인 "shimmer" 제거) + melee 순수 휘두름 + **BGM 소스 증폭** + town 전환 수정.
- **플레이테스트 폴리시(서버)**: 보스 빈방 재출현 · 적 중력 · 적 낙사 소멸.
- **플레이테스트 폴리시(클라)**: 캐릭터선택 직업 포트레이트 Animator 구동.

### 🤔 왜 필요한가

위 "결정 흐름" 6항 참조. 일관된 원칙: **사운드/연출은 클라, 게임플레이 권위는 서버, 프로토콜은 불변**. 청음은 균일 스탬프가 아니라 *이벤트별 성격*을 묘사로 잡아 ElevenLabs에 전달(브랜드명 모르므로 성격 풀어씀).

### 🛠️ 어떻게 만들었나

```
야간 무인 1패스:
  4c378a4 docs 착수 → 4f49dd6 Phase A 인프라 → 454e55b Phase B wiring(27키)
  → 492dfcd 확장 wiring(5키) → d8bc29b Phase C 에셋이관 → 0153fd3 Phase D AI생성 25
  → c2cd0ad/ad66e65/fa0f4ab watch(party_disbanded)
영호 청음/폴리시 세션:
  77c762f 분리 wiring → d68d7e8 30개 재생성
  6351f52 서버(보스재출현+적중력/낙사) → e9346ae 사운드(BGM증폭+town+보이스)
  → d0b7a76 캐릭터선택 애니 → 94d7f7b 잔여 정리
(75ccebb = M6 마무리 아트 동반)
```

### 🧪 테스트 결과

WSL2 657/0/5(+12 신규) · reviewer 🔴0 · Unity 컴파일 0err · BuildPlayer Succeeded(726MB) · 영호 인게임 청음/거동 검증 이상 무. ProtocolVersion v16 무변경.

### ➡️ 다음 스텝

- **이번 마감 후**: push → PR → admin merge(영호 GO 완료). 비가역 = GO 게이트 통과.
- **이월(영호 결정 대기, M7 범위 밖)**: `sfx.player.respawn`(리스폰 트리거 신호 정의 필요) · 투사체/슬라임/골렘 DamageEffect prefab 미배치(사운드 무관, 아트 측) · 확장 후보(존 앰비언트 / 버튼 호버 / 보스 패턴별 타격음).
- **디스코드 공지**: 영호 직접(필요 시 AI 초안).

---

## 학습 일지 후보 키워드

- `AudioClip.GetData`는 에디터에서 `clip.LoadAudioData()` + DecompressOnLoad loadType 필수(아니면 빈 샘플).
- `AudioSource.volume` [0,1] 천장 → 클립이 작으면 못 키움. 진짜 해법 = **소스 증폭**(샘플 정규화 후 WAV 재작성).
- UI `Image` 애니는 클립이 `Image.m_Sprite`에 바인딩돼야 함(게임플레이 캐릭터는 SpriteRenderer). `AnimationUtility.SetObjectReferenceCurve`로 재타겟.
- WSL2 서버 keep-alive = `sleep infinity | dotnet run`(stdin EOF로 Console.ReadLine 즉시종료 방지). 종료 = `fuser -k 7777/tcp`.
- WSL 중첩 셸에서 `$PATH`/`$()` 이중 전개 → `(x86)` 괄호 syntax error. **리터럴 경로**로 박기([[wsl2-invocation-from-git-bash]]).
