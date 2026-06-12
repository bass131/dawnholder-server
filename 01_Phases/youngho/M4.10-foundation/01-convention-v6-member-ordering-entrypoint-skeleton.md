---
owner: youngho
phase: 01
status: done
grade: 복잡
summary: CODE_CONVENTION v5→v6(DRY·멤버정렬·진입점·책임헤더 4보강) + 부록 A 실측 갱신 + .editorconfig 멤버정렬 룰 + ENTRY_POINTS.md 골격
---

# Phase 01: 컨벤션 v6 확정 + Roslyn 멤버정렬 + 진입점맵 골격

> **상태**: done
> **마일스톤**: M4.10
> **등급**: 복잡 (문서 + .editorconfig 설정, 코드 변경 0)
> **담당**: shared Worker(Sonnet)
> **의존**: 없음 — **선행 Phase**. 02~04가 이 측정 기준 위에 올라탄다.

---

## 🎯 목표

`CODE_CONVENTION.md`를 **v5 → v6**으로 올린다. v5는 "이상적 도착점"을 *선언*했지만 측정 도구가 비어 있었다. v6는 측정 가능한 4개 도구를 추가한다: **①DRY(중복) 규칙 ②멤버 정렬 순서 고정 ③진입점 내비게이션 ④클래스 1줄 책임 헤더**. 동시에 부록 A의 stale한 실측을 갱신하고(GameMap은 이미 분리 → 졸업), `.editorconfig`에 멤버정렬 룰을 박아 **빌드 경고로 강제**되게 하며, `ENTRY_POINTS.md` **골격**(형식만)을 만든다. 이 Phase가 끝나면 02~05가 "측정 기준"을 손에 쥔다.

이 Phase는 **코드를 한 줄도 안 바꾼다** — 문서(`.md`) + 설정(`.editorconfig`)만.

---

## ⏪ 사전 조건

- [ ] M4.9 마감 — ProtocolVersion 11, 스킬 시스템 완성 상태
- [ ] 현 `CODE_CONVENTION.md` v5(§0~§6 + 부록 A) 정독 — v6은 *보강*이지 재작성이 아님
- [ ] 전수조사 output 정독 — 중복 7건 file:line 근거(rootCauses), 부록 A 실측용 GameMap/ClientPacketHandlers 줄 수
- [ ] 현 코드 줄 수 실측 — `GameMap.cs`(436줄, Systems/ 6파일 분리 확인), `UnityClientSession.cs`(213줄), `ClientPacketHandlers.cs`(909줄)

---

## 📝 작업 내용

> 문서 4보강 → 부록 A 실측 → .editorconfig → ENTRY_POINTS 골격 순.

**① §2.5 DRY (중복) 규칙 신설** — `CODE_CONVENTION.md`:
- [ ] "중복 **2회 = 신호**(검토), **3회 = 의무**(추출)" 기준 못 박기
- [ ] 추출 방향 = **데이터를 소유한 객체의 메서드로** (예: 적 사망 처리는 GameMap이 mutator를 소유하므로 GameMap 메서드로 — System 간 직접 호출 X, §2.2 정합)
- [ ] **예외: 우연한 중복(coincidental duplication)** — 지금 똑같이 생겼지만 *이유가 다른* 코드는 묶지 말 것(§0.3 과한 추상화도 부채). "같은 모양"이 아니라 "같은 이유로 함께 변할 것"일 때만 추출
- [ ] reviewer **축 6**(§5.2)에 "DRY(§2.5)" 점검 편입 — God class·패턴·콘텐츠/엔진 혼재에 더해 중복 3회+를 강제 점검

**② §7.1 멤버 정렬 순서 고정** — `CODE_CONVENTION.md`:
- [ ] C# 표준 순서 못 박기: **상수 → static 필드 → 인스턴스 필드 → 프로퍼티 → 생성자 → public 메서드 → private 메서드 → 중첩 타입**
- [ ] **Roslyn StyleCop SA1201/SA1202로 강제** — 사람이 수동 `#region`으로 구획하던 방식을 *대체*. "선언 ≠ 강제"(§5) 원칙 정합: 도구가 자동 강제하면 안 깨진다
- [ ] `#region` 의존 금지 명시(수동 구획은 drift)

**③ §7.2 진입점 내비게이션** — `CODE_CONVENTION.md` (둘 다 채택):
- [ ] **별도 문서 `ENTRY_POINTS.md`** — "증상 → 시작 파일·함수" 룩업표 (비상 디버깅용). 이 Phase는 골격(형식)만, 본문은 Phase 05에서 채움
- [ ] **각 시스템 파일 상단 흐름 1줄 헤더** — 예: `// [흐름] C_Attack 수신 → AttackHandler.Handle → GameSession.SubmitAttack → map.EnqueueJob → CombatSystem.ProcessAttack`. 파일을 열자마자 어디로 흐르는지 1줄로 (§6.3 안전 주석 범위 — "왜/어디로"의 비자명 내비게이션)

**④ §6.5 클래스 1줄 책임 헤더** — `CODE_CONVENTION.md`:
- [ ] 모든 **public 클래스** 상단에 책임 1줄 헤더 의무화 (예: `// GameMap: 한 맵의 상태 컨테이너 + tick 엔진 + actor 경계. 로직은 System에 위임.`)
- [ ] **모범 = 현 GameMap 헤더** (이미 컨테이너 책임을 1줄로 박아 둠 — 이걸 표준으로 인용)
- [ ] §6.2 금지 주석(자명 재진술·역사 박제)과 구분: 책임 헤더는 "이 클래스가 *무엇을 책임지는가*"의 비자명 선언

**부록 A 실측 갱신** — `CODE_CONVENTION.md` 부록 A:
- [ ] **GameMap 졸업** — 옛 "GameMap (665줄) 4 도메인 God class" 행을 *삭제*. 실측: 436줄, 6 System(Combat/Boss/Deferred/EnemyAI/Respawn/Skill) 분리 완료 → "container + 최소 surface mutator" 의도적 설계로 정정
- [ ] **`UnityClientSession` 실측 정정** — 옛 665줄 → 213줄(이미 슬림). 진짜 미실행분은 **`ClientPacketHandlers.cs` 909줄**(inline 핸들러 + VFX 보일러플레이트)임을 강조 → 부록 A의 "진짜 미실행" 대상으로 기재(타이밍 = M4.12)
- [ ] **전수조사 중복 7건 편입** — 부록 A에 "중복(§2.5)" 항목 추가: 적 사망 3복붙 / roster 2복붙 / rewind 4벌 / facingByte 4벌 / 매직넘버 산재 / HitEffect enum 부재 / 클라 VFX 보일러플레이트. 각 타이밍(M4.10 vs M4.12) 표기

**`.editorconfig` 멤버정렬 룰** — 리포 루트 `.editorconfig`(없으면 신설):
- [ ] StyleCop SA1201/SA1202(멤버 종류/접근성 순서) 룰을 `warning` 레벨로 추가 — 빌드 시 경고로 노출
- [ ] §4가 예고한 "M4.4+ Roslyn 강제"의 멤버정렬 부분 실현. 단 **이 Phase는 룰만 박고 전체 스윕은 Phase 05** (경고가 *뜨는지*만 확인, 0으로 만드는 건 05)

**`ENTRY_POINTS.md` 골격** — `00_Document/conventions/ENTRY_POINTS.md` 신설:
- [ ] 형식만: 표 헤더(`증상 | 시작 파일 | 시작 함수 | 흐름 요약`) + 카테고리 섹션 자리(전투 / 이동 / 스킬 / 맵이동 / 동기화) — 내용은 Phase 05에서 채움
- [ ] `00_Document/conventions/INDEX.md`에서 ENTRY_POINTS.md 링크 추가

---

## ✅ 완료 조건 (정량)

- [ ] `CODE_CONVENTION.md`에 **v6** 박힘 — §2.5(DRY) / §6.5(책임 헤더) / §7.1(멤버 정렬) / §7.2(진입점) 4섹션 존재 + 변경 이력 표에 v6 행 추가
- [ ] 부록 A가 **현재 실측과 정합** — GameMap 행 삭제(졸업), ClientPacketHandlers 909줄이 미실행 대상으로 기재, 중복 7건 편입
- [ ] 리포 루트 `.editorconfig`에 멤버정렬 룰(SA1201/SA1202)이 존재하고, `dotnet build` 시 **빌드 경고로 작동**(현 코드에서 경고가 *뜨는지* 확인 — 이 Phase는 0으로 안 만듦)
- [ ] `00_Document/conventions/ENTRY_POINTS.md` **골격** 존재(표 헤더 + 5 카테고리 자리) + INDEX.md 링크
- [ ] **코드 변경 0** — `git diff --stat`이 `.md` + `.editorconfig`만 보여줌 (`.cs` 0건)

---

## 🧪 테스트

**자동**:
- `dotnet build` — `.editorconfig` 룰 추가 후 빌드가 *깨지지 않고*(경고는 OK, 에러는 X) 멤버정렬 경고가 stdout에 노출되는지 확인

**수동**:
- `CODE_CONVENTION.md` v6 통독 — 4보강이 v5 §0 철학(특히 §0.3 과한 추상화 금지)과 충돌 없는지
- 부록 A를 `GameMap.cs`(436줄) 실측과 대조 — 졸업 정합 확인

---

## 📚 학습 포인트

- **`#region` 수동 구획 vs Roslyn 자동 정렬의 차이**: `#region`은 사람이 손으로 "여기부터 필드, 여기부터 메서드"를 구획하는 주석성 도구다 — 누가 새 멤버를 엉뚱한 곳에 추가해도 컴파일러는 모른다(drift). 반면 SA1201/1202는 *분석기(analyzer)*가 빌드 때마다 순서를 검사해 경고를 띄운다. "선언만 하면 안 지켜진다"(§5)는 헌법 정신의 물리적 강제 — 자동 강제가 더 안 깨지는 이유다.
- **진입점 맵이 비상 디버깅에 주는 가치**: 버그가 터졌을 때 "원격 캐릭터가 천천히 따라온다"는 *증상*에서 출발해 어느 파일·함수부터 봐야 하는지를 룩업표 한 줄로 안다 = 백지 탐색 비용 0. 특히 다음 마일스톤(M4.11 동기화)의 디버깅 자산이 된다.
- **컨벤션 = 측정 기준**: "좋은 코드를 쓰자"는 측정 불가능한 구호다. "중복 3회 = 추출 의무", "멤버는 이 순서", "public 클래스는 책임 1줄"은 *기계나 reviewer가 판정 가능한* 기준이다. 측정 가능해야 강제 가능하고, 강제돼야 지켜진다.

---

## ⚠️ 함정 / 주의사항

- **컨벤션은 "이상적 도착점"이다 — 현 코드를 정당화하지 말 것.** v6를 쓰면서 "지금 코드가 이러니까 이게 맞다"고 거꾸로 맞추면 안 된다. 기준이 먼저, 코드가 그 기준으로 *측정*된다.
- **부록 A를 실측 없이 옛 줄 수로 박지 말 것.** GameMap은 이미 436줄로 분리됐다(6 System). 옛 665줄/4도메인으로 적으면 컨벤션이 거짓말을 한다 — 반드시 `wc -l`/Glob로 실측 후 기재.
- **이 Phase는 멤버정렬 룰을 *박기만* 한다.** 전체 코드를 경고 0으로 만드는 스윕은 Phase 05다. 여기서 스윕까지 하면 02~04 코드가 아직 안 박혀 다시 스윕해야 한다(중복 작업).
- **`.editorconfig`가 빌드를 *에러로* 깨뜨리지 않게** — 멤버정렬은 `warning` 레벨로. `error`로 박으면 현 코드가 빌드 불가가 되어 02~04가 막힌다.

---

## ➡️ 다음 Phase

- Phase 02 (매직넘버 단일화) / Phase 03 (적 사망 통합) / Phase 04 (roster 통합) — 모두 이 컨벤션 v6 + 멤버정렬 룰 위에 올라간다.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (컨벤션 v6 = 측정 기준 확정, 4보강 + 부록 A 졸업 사실 박제).

---

## 작업 로그

- 2026-06-11: 계획 작성 (전수조사 "골격 건강, 진짜 병은 중복" 판정 + v5 부록 A 미실행 갭 → v6 측정 기준 확정 Phase로 선행 배치)
