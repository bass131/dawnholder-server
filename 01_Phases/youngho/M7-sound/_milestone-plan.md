---
owner: youngho
milestone: M7
title: 사운드 — 전면 사운드 적용 (인벤토리 / 오디오 인프라 / 에셋 생성 / SFX·BGM wiring)
status: in-progress
created: 2026-06-15
---

# M7 — 사운드

## 배경

M5 인터랙티브 2차 플레이테스트 피드백 7번째 항목 = "Unity AI Generator로 전면 사운드 적용,
신규 Sound 폴더 분류, 적용". 영호가 "워낙 커서 별도 마일스톤"으로 판단 → M6(폴리시)에서 분리.

게임에 현재 사운드가 사실상 없음 → 0→1 작업. 오디오 재생 인프라(코드) + 에셋 생성(영호/AI Generator) +
이벤트 wiring(코드)이 섞인 cross 마일스톤. 사운드 *품질/선정*은 영호 청음 게이트.

> **상태**: 🚧 진행 중 (2026-06-16 AutoMode 착수, 영호 야간 무인 위임). M6 #112 머지 완료로 착수 조건 충족.
>
> **★ 착수 갱신 (2026-06-16, 영호 결정 반영)**:
> - **생성 = AI 단독(`elevenlabs-sound-effects-v2`)**. **BgmComposer(칩튠) 전면 배제 — 폴백조차 X** (영호 "저품질 사운드 아예 배제"). 생성 실패 = **재시도 최대 3회 → MISSING 무음**(억지 무한 생성 금지). AudioManager 누락 클립 no-op이라 게임 정상.
> - **기존 17개 처리**: 유지=BGM 4(OGG)+die 3(Slime/Golem/Vampire). **재생성=칩튠 의심 8 WAV**(공격/피격/점프/스테이지클리어/매직). 신규=빈칸 11. → **AI SFX 19개**.
> - **die 매핑**: Normal→SlimeDie, Golem→GolemDie, **Boss(뱀파이어)→VampireDie**(영호 확정, 잉여 아님). 진짜 잉여=Frog/Mushroom만.
> - **발소리 포함**(코드 티커, .anim 편집 회피). **공통 톤=모던 판타지·깔끔·타이트**.
> - **실행=무인 1패스**(M6식 per-phase 게이트 X). 청크별 로컬 commit, **push/PR/청음은 아침 영호 GO**.
> - 상세 실행계획·프롬프트 19개 초안 = 세션 플랜파일(`~/.claude/plans/nested-frolicking-pnueli.md`) + work-pin.

## Phase 순서 (5개)

1. **Phase 01 — 사운드 인벤토리 + 분류 체계 + 폴더 구조** (등급: 보통, 담당: client+영호 의논)
   - 끝나면: 어떤 사운드가 어떤 이벤트에 필요한지 목록 + Sound 폴더 분류(BGM/SFX/UI/Ambient) 확정
2. **Phase 02 — 오디오 재생 인프라 (AudioManager)** (등급: 복잡, 담당: client)
   - 끝나면: 코드에서 사운드를 키로 재생/정지/볼륨 제어 가능 (placeholder 사운드로 검증)
3. **Phase 03 — 사운드 에셋 생성 + import + 분류 배치** (등급: 복잡, risk: unity-asset, 담당: 영호+client)
   - 끝나면: 실제 사운드 에셋이 Sound 폴더에 분류돼 import 설정까지 완료
4. **Phase 04 — SFX wiring (전투/이동/UI 이벤트)** (등급: 복잡, 담당: client)
   - 끝나면: 공격/피격/점프/대시/스킬/레벨업/UI 클릭/퀘스트·스테이지 이벤트에 SFX 연결
5. **Phase 05 — BGM/Ambient + 볼륨 밸런싱 + 클로즈아웃** (등급: 복잡, 담당: client+영호 청음)
   - 끝나면: 마을/전투/보스 BGM 전환 + ambient + 볼륨 밸런싱 + -DONE 박제

## 의존성 그래프

```
01 (인벤토리/분류) ─→ 02 (오디오 인프라) ─→ 04 (SFX wiring) ─┐
                  └─→ 03 (에셋 생성)    ─────────────────────┼─→ 05 (BGM/밸런싱/클로즈아웃)
                                                              ┘
```

- **01이 분류 체계를 확정**해야 02(인프라 키 설계)·03(에셋 배치 폴더)이 정합.
- **02(인프라)와 03(에셋)은 병렬 가능** — 02는 placeholder로 개발, 03은 영호 에셋 생성.
- **04(wiring)는 02+03 후** (인프라 + 실제 에셋 필요).
- **05는 04 후**.

## 설계 분기 (Phase 진입 시 영호 확인)

- **Phase 01 분류 체계**: BGM/SFX/UI/Ambient 4분류로 충분한지, 세부 하위(전투/이동/스킬 등) 어디까지.
- **Phase 03 에셋 생성 분담**: Unity AI Generator는 영호 주도(외관/청음 영역), AI는 import 설정/배치 코드 보조.
- **서버 무관 확인**: 사운드는 전적으로 클라 표현 — 헌법 #1상 서버/프로토콜 무변경이어야 함. 어떤 사운드도 게임 판정에 영향 X.

## 이번 마일스톤 핵심 개념 (학부생 시각)

- Unity AudioSource / AudioClip / AudioMixer — 재생·라우팅·볼륨 그룹
- 사운드 풀링(AudioSource 재사용)이 왜 필요한가 (매 사운드 GameObject 생성 비용)
- BGM 크로스페이드 — 두 트랙을 겹쳐 부드럽게 전환
- 압축/로드 타입(Decompress on Load vs Streaming) trade-off — BGM은 스트리밍, 짧은 SFX는 메모리

## 위험 깃발

- **unity-asset**: Phase 03(에셋 생성/import) — 등급 반영.
- **비가역 없음**: 프로토콜/DB 무변경. 사운드는 순수 클라 표현.
- **scope 주의**: "전면 적용"은 무한 확장 가능 — Phase 01 인벤토리로 범위를 박고, 초과분은 M8+로.
