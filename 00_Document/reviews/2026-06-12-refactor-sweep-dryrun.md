# 무인 리팩토링 스윕 (`--dry-run`) — 2026-06-12

> `/refactor-sweep --dry-run` 첫 실전. **코드/git 일절 미변경 — 진단 + 제안만.** reviewer 4 도메인 병렬 fan-out(Step 1) → 종합(Step 5). Step 0(브랜치·baseline)·Step 2~4(수정·게이트·재검증) 스킵.

## TL;DR

- **모드**: `--dry-run` (진단만, 자동수정 0, commit 0)
- **4 도메인 부합도**: **전부 🔴 0** — server / shared / clientnet / client 모두 심각 위반 없음
- **발견 총량**: ✅ 저위험 ~25건(전부 §6.2 주석 노이즈) / **🔶 고위험 0건** / 📋 제안 몇 건
- **핵심 발견**: 우리 production 코드는 CODE_CONVENTION v6.1에 **매우 높게 부합**. God class·네이밍·DRY·멤버정렬·콘텐츠/엔진 분리 = 거의 완벽(M4.10 스윕으로 이미 졸업). **이번 라운드의 거리 = 주석 토큰 정리 위주.**
- **자동화 가치 관점**: 이번엔 큰 리팩토링(🔶) 거리가 없음 → commit 모드로 돌려도 *주석 정리 ~25건*이 주 산출. 단 그 정리도 §6.3 안전주석 옥석 구분이 핵심(아래 5).

---

## ⚠️ Codex cross-review 정정 (2026-06-12, 사후 추가)

이 리포트는 **Codex 외부 cross-review로 2건 정정**됨 — *self-assessment bias 발견*(Claude 슬래시로 Claude reviewer가 자기 코드 진단). 상세 = §8.

1. **줄수/좌표가 단일 기준으로 재확인 안 됨**: §1·§3의 줄수 일부가 *진단 전 1차 스캔(feature/m4.12 브랜치) 힌트*를 받아써서 현재 트리와 불일치. **현재 브랜치(feature/refactor-sweep-skill = main 기준) 단일 기준 wc -l** = GameMap 498 / GameSession 471 / **LocalPlayerMovement 393**(m4.12는 410 — 쿨다운 HUD +17) / Physics 350. Codex 측정(434/415/335/321)은 *코드 줄* 기준 — 어느 쪽이든 *한 기준 재확인*이 원칙(carry-over "박제 전 file:line 실측" 위반). `SkillCastHandler.cs:92`의 `ProjectileLaunchHandler(685-695)`는 분리 전 옛 좌표(stale) → 현 디스패치 진입점 = `UnityClientSession.cs:56`.
2. **LocalPlayerMovement = 🟢 분리X가 아니라 🟡 재검토** (Codex 발견): §3 참조.

---

## 1. 도메인별 부합도 점수

| 도메인 | 🔴 심각 | 🟡 개선 | 총평 |
|---|---|---|---|
| **server** (`02_Server/GameServer/`, 37파일 ~4.1k줄) | 0 | 4~6 | God class 0(GameMap 498=졸업 container / GameSession 471=⛔) · 네이밍 0 · 책임헤더 0 · 정렬 0. 주석 노이즈 ✅ 6건만 |
| **shared** (`98_Shared/`, 15파일 ~1.4k줄) | 0 | ~10 | God class 0 · 콘텐츠/엔진 0 · DRY 0 · 네이밍 0 · 정렬 0. 주석 노이즈 ✅ ~10건만 |
| **clientnet** (`04_ClientNet/`, 6파일 657줄) | 0 | ~6 | God class 0(ClientSession 312=§2.4 프레이밍 예외) · 순수 인프라(콘텐츠/엔진 청정). #region + 주석 노이즈 ✅ |
| **client** (`03_Client/Assets/Scripts/`, 80파일 6.4k줄) | 0 | 4 | **헌법 §1 서버권위 위반 0 확인** · 네이밍 0 · 핸들러 도메인 분리 졸업. 전부 📋(Unity 검증 불가) |

---

## 2. ✅ 저위험 무인 후보 — 주석 노이즈(§6.2 역사·Phase 박제) ~22건

**모두 "토큰만 외과 절제, 사유(왜)는 보존"** — 줄 통째 삭제 아님.

**server (6)**: `Maps/PlayerEntity.cs:128`(줄 통째 가능) · `Maps/GameMap.cs:77`(M4.10 토큰만) · `Combat/CombatConstants.cs:119,128` · `Maps/Systems/SkillSystem.cs:61,90`
**shared (~10)**: `GameData/Constants.cs:26,85,97,104` · `GameData/SkillId.cs:14-16` · `GameData/AnimState.cs:23` · `GameData/EnemyKind.cs:6` · `GameData/Terrain.cs:5` · `GameData/Formulas.cs:34`
**clientnet**: `ClientSession.cs` `#region` 4줄 제거(§7.1 #region 금지) + `:104` 자명 재진술 절반
**client (📋, 7)**: `ClassConfig.cs:14` · `MageClassConfig.cs:9` · `MageRangedAttack.cs:9` · `RemoteEntityRegistry.cs:15` · `RemoteEnemy.cs:16` · `ZoneVisualizer.cs:41` · `ProjectileLaunchHandler.cs:19` + stale ref `SkillCastHandler.cs:92`("ProjectileLaunchHandler(685-695)" 옛 줄번호 — 909줄 졸업 전 좌표)

---

## 3. 🔶 고위험 무인 0건 + 🟡 재검토 1건 (Codex 정정)

구조 변경 = God class 분리(§2.2/2.3) **0건**은 유지(아래). 단 Codex가 **🟡 재검토 1건**(LocalPlayerMovement)을 발굴 — reviewer 후한 판정.

- **🟡 `LocalPlayerMovement.cs` `Update()`(:234-307) 책임 과다 (Codex 발견)**: 한 루프가 *쿨다운 감쇠4 + source-gating 타이머 + lock gating + prediction + `C_MoveIntent` 인코딩·송신 + InputHistory replay + 시각보간* = **5~7종**. **God class(2+도메인)는 아님**(단일 관심사 오케스트레이션 + `IsMovementLocked`/`ResolveGatedInput` 등 순수로직 이미 static 추출). 단 §3.1 "MonoBehaviour 한 개념" 관점에선 분리 검토 가치 — reviewer가 §0.3 과분할 회피를 강조하다 과소평가. **→ M4.13(임펄스 클래스 재설계: 행동입력게이트 P1 / 클라예측B InputHistory P5)에서 어차피 이 영역을 건드리므로 거기서 같이 본다**(지금 급히 분리 X).
- **GameMap(498)·GameSession(471)·LocalPlayerMovement(393)·Physics(350)·ClientSession(312)** = 전부 600줄 미만. God class(2+도메인) 기준 **분리 강요 금지**(§0.3)는 유지 — Codex도 "컨테이너+System 패턴 타당, 무조건 분리 X" 동의. (LocalPlayerMovement는 줄 수가 아니라 *Update 책임 폭*이 🟡 사유.)
- **부록 A의 DRY 7건**(적사망·roster·rewind·facingByte)은 **M4.10에서 이미 처리 완료** — 현재 코드에 잔존 중복 미관측.

→ **이게 dry-run의 핵심 가치**: "큰 리팩토링을 자동으로 시켜두면 가치 있다"고 기대했는데, 실측해보니 *우리 코드는 이미 그 단계를 졸업*했음. 자동수정으로 무리하게 God class를 쪼개면 오히려 부채. **부록 A 문서가 stale**(졸업한 갭을 아직 미해결로 기재)이 진짜 정정 대상.

---

## 4. 📋 제안만 (자동수정 제외 — 사람 트랙)

| 항목 | 위치 | 왜 제외 | 제안 |
|---|---|---|---|
| **VFX 스폰 DRY** | client `HitResultHandler.cs:69-75` vs `SkillCastHandler.cs:157-180` | 📋 03_Client(Unity 검증 불가) + Rule of Three 근접(2곳) | `EffectSpawner.Spawn(path,pos,facingSign,ref warned,name)` static을 `Combat/Effects/`에 추출 → 두 핸들러 공유. 3번째 호출자 생기면 추출 적기 |
| **GameSession 부분추출** | server `Network/GameSession.cs` | ⛔ trust-boundary(보안 §3) | 무인 영구 제외. 부록 A "~95줄 추출 가능"도 사람 + 재검증 트랙 |
| **ProtocolVersion 버전이력** | shared `Protocol/ProtocolVersion.cs:9-46` | 📋 = 노이즈 아님(의도적 버전 로그, 98_Shared/CLAUDE.md가 "단일 진실"로 명시) | 손대지 말 것 |

---

## 5. ⚠️ false positive 경계 — reviewer가 "보존" 판정한 것 (중요)

reviewer 4명이 §6.2 노이즈(제거)와 **§6.3 안전주석(보존)을 정밀 구분**. 다음은 노이즈처럼 보여도 *안 적으면 누가 잘못 고쳐 사고나는 비자명 근거*라 **제거 금지**:

- **shared**: `Constants.cs:42-44`(AttackCommitWindow vs AnimLatch 혼동 방지) · `:81-86`(ExternalImpulseEpsilon 보색 계약) · `Physics.cs:113-144`(분기/적분 순서 invariant — 재정렬 금지) · `InputBits.cs`(PDL 와이어 약속) · `MapDataFile.cs`(직렬화 순서) · `ProtocolVersion.cs`(bump 약속)
- **server**: `Handlers/**` 주석 전부(헌법 §3 검증 근거) · `GameSession` rate-limit/closing race 근거
- **clientnet**: `ClientSession.cs:50-53`(Trust Boundary fail-closed 순서) · `SendBuffer`/`FrameValidator` 동기화 약속 · `RecvBuffer` 링버퍼 학습 다이어그램

→ **교훈**: refactor-sweep의 진짜 난이도는 God class 같은 대공사가 아니라 **"왜(§6.3)와 언제(§6.2)가 한 줄에 섞인 주석에서 토큰만 절제"**하는 옥석 구분. "false positive가 놓침보다 훨씬 나쁘다"는 reviewer 원칙이 여기서 실전.

---

## 6. 결론 + commit 모드 가치 평가

- **우리 코드는 클린코드(우리 채택분)에 매우 높게 부합** — 4 도메인 🔴 0. 자랑할 만한 상태.
- **이번 라운드 commit 모드 산출 예상**: ✅ 주석 토큰 ~22건 정리. 🔶 0이라 *큰 리팩토링은 없음*.
- **단 주석 정리도 기계적 sweep 위험** — §6.3 안전주석 동반 손실 위험이 있어, 토큰만 절제하는 정밀 작업. 첫 commit 모드는 1~2 도메인 + `--max` 작게 권장.
- **진짜 1순위 부채 = 문서 stale**: CODE_CONVENTION **부록 A**가 졸업한 갭(GameMap 졸업·DRY 7건 처리·ClientPacketHandlers 분리)을 미해결로 기재. 코드보다 문서 정정이 먼저.

### 다음 (영호 선택)
- (a) **commit 모드 v1** — server/shared 주석 토큰 ~16건 정리(`--max=8`). 단 §6.3 보존 정밀 필요.
- (b) **부록 A 정정** — 문서가 코드를 잘못 기술하는 것부터(가장 안전한 부채 청소).
- (c) **client DRY 추출**(EffectSpawner) — 낮에 Unity 띄우고.

---

## 8. Codex cross-review (γ) — self-bias 게이트 실증

영호가 dry-run 결과를 외부 시각(Codex)에 던져 cross-check. **이 리포트가 "Claude 슬래시로 Claude reviewer가 자기 코드 진단"이라 self-assessment bias가 의심된 것** — 정확히 그 bias를 잡았다.

**Codex 이견 2 (둘 다 정당)**:
1. **줄수/좌표 단일기준 미재확인** — feature/m4.12 1차 스캔 힌트가 main 기준 진단에 섞임 + `SkillCastHandler.cs:92` 옛 좌표. → carry-over "박제 전 file:line 실측" 위반. (정정 박스 ①)
2. **LocalPlayerMovement 책임 과다를 "🟢"로 묶기엔 넓음** — Update 5~7종. → §3 🟡 격상.

**Codex 동의 2**:
- `ProtocolVersion.cs:3` / `InputBits.cs:5` 안전주석 보존 = 맞음.
- GameMap/GameSession 컨테이너+System 패턴 = 타당(무조건 분리 X).

**★메타 교훈 (이 사건의 핵심)**: dry-run이 "🔴0 🔶0 깨끗"이라 했는데 외부가 self-bias를 적발. **commit 모드였고 cross-check가 없었다면 → reviewer 놓침(LocalPlayerMovement)을 "안전"으로 통과시키고 stale 힌트 기반 자동수정할 뻔.** 이것이 `/refactor-sweep`에 **"🔶 고위험 무인 자동수정 전 외부 cross-check 게이트"**가 필요한 실증. 슬래시 함정에 반영(2건):
- ⓐ 진단 힌트는 진단 대상 브랜치에서 실측(옛 브랜치 값 주입 금지).
- ⓑ 고위험 commit 모드 첫 회차는 외부 cross-check 1회 권장.
