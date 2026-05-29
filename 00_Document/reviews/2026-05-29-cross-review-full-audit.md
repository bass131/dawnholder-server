# Cross-Review — 2026-05-29 — 전체 프로젝트 Harness + Code + Architecture 감사

> γ 10회차. 점검 브랜치 `review/full-audit-2026-05-29`. M4.2 마감(main `59d695e`) 이후 *전체 코드베이스 스냅샷* 감사 (특정 diff 아님).
> α = Claude reviewer SubAgent 3분할 / β = Codex `gpt-5.5` xhigh read-only `codex exec` (Claude 직접 호출 — 사용자 명시 위임).

## 변경 범위
- 대상: 02_Server / 98_Shared / 04_ClientNet / 03_Client(스크립트) / 99_Tools / 00_Document / .claude 전체
- 등급: **대규모** (3+ 도메인 전수 / 읽기 전용 감사라 비가역 깃발은 없음 — 봉합 단계에서 등급 재산정)
- 규모: 추적 965파일, C# 117개

## 실행 메모 (γ 9회차 함정 재발 + 봉합)
- **Codex β 1차 헛돎**: prompt 가드 "DO NOT run shell commands"가 Codex의 *정상 파일 읽기 도구*까지 차단 → "unread, 결함 0" 반환. γ 9회차에 박힌 함정(셸 우회 금지 가드 과잉) 재발.
- **봉합**: 가드를 "파일 읽기 ALLOWED / 웹·네트워크·MCP·쓰기 FORBIDDEN"으로 정밀화 후 재실행 → 14건 발견. **교훈: read-only 샌드박스에선 파일 읽기 = 셸이므로, 금지는 '웹/네트워크/쓰기'로 한정.** (cross-review.md 함정 절 갱신 후보)

---

## α — Claude reviewer 결과 (3분할, 🔴 0 / 🟡 14 / 🟢 다수)

### α1 서버 + 공유 — 헌법 5축 위반 0
- 🟡 `Network/Session.cs:37` frame 길이 헤더만 host-endian(`BitConverter.ToUInt16`), payload는 LittleEndian → §2 wire 약속 부분 누수
- 🟡 `Maps/PlayerEntity.cs:95` + `GameMap.cs:248` lag comp rewind **silent fallback** — tick 미기록 시 현재 위치 반환(같은-tick), "rewind" 표기되나 정확도 미달
- 🟡 `GameSession.cs` rate-limit 필드 동기화 없음 (IOCP 직렬화로 현재 무버그, invariant 미명시)
- 🟡 `Combat/EnemyEntity.cs:41` enemy `stats=default` → `EnemyStats.Defense=0` 모든 적 무방어 (보스 포함)
- 🟡 `Loop/GameWorld.cs:120` Dictionary 순서 주석 부정확 (동작 무영향)

### α2 클라 + ClientNet — §1 위반 0
- 🟡 `UI/HudController.cs:34` HUD 영구 mock — `UpdateHP/UpdateGold` 호출처 0, 본인 HP 서버값 미반영
- 🟡 `RemotePlayer.prefab` 3벌 중복 + `LocalPlayer.backup` (Resources 사본만 실사용)
- 🟡 `UnityClientSession` vs `CombatBootstrap` sceneLoaded **등록 순서 암묵 계약** — 뒤집히면 roster 패킷 조용히 drop
- 🟡 `RemoteEnemy` 보간 부재 — enemy spawn 좌표 고정 (AI 이동 도입 시 표면화)
- 🟡 `Connector.cs:83` 소켓 예외 swallow — connect 실패 NetworkService 미전달
- 🟡 `NetworkService.cs:131` port 7777 고정 / `MainThreadDispatcher` static 큐 잔류

### α3 아키텍처 + 하네스 — 치명 0
- 🟡 `ADR.md:47~51` 후보 ADR 번호(022~026)가 **이미 채택된 번호와 충돌** — §2 "ID 재사용 금지"의 문서판
- 🟡 `ADR_History.md` 이력 누락 4건(023/024/025/027)
- 🟡 `hooks/README.md:34,66` 옛 "7" 잔존 (실제 8)
- 🟢 `New_Harness/` 죽은 경로 / ADR-026 stray `</content>` 태그 / hook 내부 상대경로(추측)
- ✅ 통과: SubAgent 9 / Knowledge 5 / 슬래시 10 / policies 8 / Hook 등록 / false-promise 없음 카운트 정합

---

## β — Codex 결과 (14건; CONFIRMED 12 / LIKELY 2)

1. [CONFIRMED][B] `99_Tools/PacketGenerator/Program.cs:135` + `PDL.xml:13` — packet ID가 XML 순서에서 파생, 명시 ID / 은퇴-ID 레지스트리 없음. PDL 삭제/재정렬/삽입이 ID를 조용히 shift/재사용 → §2 위험.
2. [CONFIRMED][A/B] `Maps/PlayerEntity.cs:49,109` + `PlayerStats.cs:40` — 클래스 스탯이 권위 `Hp/MaxHp`를 초기화 안 함. 전부 100/100. Warrior 150 / Ranger 80 무시.
3. [CONFIRMED][B] `Maps/GameMap.cs:227,235,248` — invalid C_Attack target/cooldown/rewind 실패가 cheat-flag 로그 없이 silent return. 핵 시도 감사 흔적 없음 → §3.
4. [CONFIRMED][B] `Network/Session.cs:42` + `GenPackets.cs:1157` — 패킷이 4..4096 bytes로만 검증, 생성 shape 정확 길이 미검증. trailing byte 수용.
5. [CONFIRMED][A] `Input/LocalPlayerController.cs:138` + `Physics.cs:14` — 클라 prediction이 가변 `Time.deltaTime`로 shared physics 호출(고정 timestep 요구 위반). fps별 발산.
6. [CONFIRMED][A] `04_ClientNet/ClientSession.cs:296` — recv/decode 예외가 로그만, 재등록/disconnect 안 함. 단일 악성 패킷이 클라를 "연결됐으나 영구 미수신"으로.
7. [CONFIRMED][A] `UnityClientSession.cs:163` + `NetworkService.cs:100` — remote disconnect가 Instance만 clear, `NetworkService._isConnected/_session` 미리셋 → 이후 Connect() no-op 가능.
8. [CONFIRMED][A] `UnityClientSession.cs:91,163` — 세션마다 sceneLoaded 구독, disconnect 시 미해제 → reconnect 시 죽은 세션 누수 + stale 콜백.
9. [CONFIRMED][B] `GameSession.cs:347,354` + `GameMap.cs:404` — `C_MoveIntent.clientTick`을 monotonic/range 검증 없이 `LastClientTick`에 복사 + snapshot에 echo. 클라 제어 tick이 ack 메타데이터로 권위화 → reconcile 오염.
10. [CONFIRMED][A] `Physics.cs:55` + `PlayerStats.cs:40` — 클래스별 MoveSpeed는 dead data; 이동은 항상 전역 `Constants.MoveSpeed`.
11. [CONFIRMED][A] `Formulas.cs:41` — 데미지 계산이 Math.Max clamp 전 overflow 가능. 고스탯이 음수 wrap → 1.
12. [LIKELY][B] `LocalPlayerController.cs:100,112` — 클라가 local 3-unit 타겟 범위로 C_Attack 게이트. 순수 input forwarder 아님 — 서버 hitbox 규칙 변경 시 유효 의도 억제.
13. [LIKELY][A] `Network/Session.cs:155` + `04_ClientNet/ClientSession.cs:181` — `Disconnect()`가 try/finally 미보호. reset 소켓이 flag 설정 후 throw → cleanup skip.
14. [CONFIRMED][C] `00_Document/policies/subagent-routing.md:28,181,184` — 상대 링크가 `../agents/` `../hooks/`를 가리키나 실제는 `.claude/agents` `.claude/hooks`. 하네스 문서가 죽은 경로 안내.

---

## γ 비교 분석

### 🔴🔴 양쪽 다 잡음 (corroborated — 최우선 신뢰)
| 결함 | α | β | 판정 |
|---|---|---|---|
| 소켓 예외 swallow → 클라 hang | α2 (Connector/ClientSession) | β6 | **실재 확정**. 클라가 연결됐으나 미수신. |
| sceneLoaded 취약성 | α2 (등록 순서 계약) | β8 (unsubscribe 누수) | **상보적**. 같은 코드의 *다른* 결함 2개 — 둘 다 실재. |
| cheat-flag 로그 부재 (silent drop) | α1 (rewind fallback) + "cheat-flag=M4.3 backlog" | β3 (attack/cooldown/rewind) | **실재, 기지 backlog**. 헌법 §3 강화 = M4.3 명시 이월. |

### 🟡 판정 *갈림* (γ 핵심 — 실측이 final arbiter, Step 4-B)
| 결함 | α 판정 | β 판정 | 권유 |
|---|---|---|---|
| 가변 dt prediction (β5) | **의도된 drift** (SnapThreshold 1.5f 문서화) | **결함** (fps 발산) | Play 모드 실측 후 결정. M4.3 "reconcile drift 튜닝" 이월과 동일 항목. |
| 클라 타겟 범위 게이트 (β12) | **의도된 UX hint** (서버 최종 판정) | **§1 우려** (순수 forwarder 아님) | 서버 hitbox 규칙과 클라 힌트 범위 *동기화 약속*만 박으면 해소. 현재 무해. |

### ⚠️ β만 잡음 (α 누락 — **진짜 위험 신호**, 일부 Claude 직접 검증 완료)
| 결함 | 검증 | 심각도 |
|---|---|---|
| **β2 클래스 HP 미적용** (Hp/MaxHp 100/100 고정, Stats.Hp/MaxHp 죽은 값) | ✅ Claude 코드 직독 확정 (`GameMap.cs:142` 생성만, Hp 미설정) | **HIGH** — 클래스 선택이 전투 HP 무의미. Warrior=Ranger=100. |
| **β10 MoveSpeed dead** (전역 Constants 사용) | ✅ Claude 직독 확정 (`Physics.cs:55`) | MED — 전사/원거리 이동속도 동일. |
| β11 데미지 overflow + 주석 false claim | ✅ 직독 — 거동 맞음, 현 스탯(12~15) 비현실적 | LOW(이론) + 주석 정정 필요 |
| β9 clientTick 무검증 → reconcile 오염 | 미직독 (β CONFIRMED) | MED — §3 trust boundary 갭 |
| β7 reconnect 죽은 세션 | 미직독 (α2-5 부분 중첩) | MED |
| β4 패킷 정확 길이 미검증 (trailing byte) | 미직독 | MED |
| β1 PDL 생성기 은퇴-ID 레지스트리 없음 | α1이 "현 append-only OK" 확인 / β는 *잠복 취약성* | MED — 98_Shared/CLAUDE.md의 "은퇴 ID 제거 X" *관례*가 생성기로 강제되진 않음 |
| β13 Disconnect try/finally 미보호 | 미직독 | LOW |
| β14 subagent-routing.md 죽은 링크 | α3 인접(policies INDEX는 OK 확인했으나 본 파일 미점검) | LOW(하네스 문서) |

### α만 잡음 (β 누락)
- α1-a endian (Session.cs:37) / α1-c rate-limit 동기화 invariant / α1-d **EnemyStats 무방어** (β2/β10의 *적 버전* — 합치면 "스탯 컨테이너 절반만 wiring" 전체 그림)
- α2-1 **HUD 영구 mock** / α2-2 prefab 3벌 / α2-6 port·dispatcher
- α3-1 ADR 번호 충돌 / α3-2 ADR_History 누락 / α3-3 hooks README "7"

---

## 결정 권유

### 🔵 큰 그림
세 도메인 모두 **헌법 5대 절대 원칙 hard 위반 0건** (양쪽 합의). 그러나 β가 α 사각인 **"클래스 스탯 절반만 wiring" 클러스터**(β2 HP + β10 MoveSpeed + α1-d 적 방어)를 발굴 — 캡스톤 데모 *기능 정합*상 가장 의미 있는 발견. 나머지는 견고함 위의 정리 항목.

### ➡️ 우선순위 (봉합은 *별도 작업* — 본 감사는 읽기 전용)
1. **HIGH — β2 클래스 HP 미적용**: `AddPlayer`에서 `entity.Hp = entity.MaxHp = stats.MaxHp` wiring (또는 PlayerEntity 생성자가 Stats에서 초기화). server 도메인 보통 등급. **봉합 후 EditMode 테스트로 Warrior=150/Ranger=80 전투 HP 검증 + Play 실측** (Step 4-A/4-B).
2. **HIGH — β6/α2-5 소켓 hang**: recv 예외 시 재등록 또는 명시 disconnect. client 도메인.
3. **MED 묶음 (M4.3 편입 권장)**: β9 clientTick 검증 + β3 cheat-flag(이미 이월) + β1 PDL 은퇴-ID 가드 + β10 MoveSpeed + β7/β8 reconnect/구독 누수.
4. **하네스 문서 sweep (1 PR)**: β14 + α3-1·2·3 + α3 🟢 묶어 정정 (메인 세션 직접, 도메인 외).
5. **판정 갈림 (β5/β12)**: Play 모드 실측 후 결정 — 실측 통과면 "의도 유지 + 주석 명문화", 발현이면 봉합.

### 옛 학습 정합
- γ 9회차 함정 (셸 우회 가드 과잉) **재발 → 즉시 봉합** = cross-review.md 함정 절 강화 후보.
- "β 신뢰 맹목 X" 준수 — β2/β10/β11 Claude 직독 검증, 판정 갈림 2건은 실측 유보.
- M4.3 이월 풀세트와 다수 중복(reconcile drift=β5, cheat-flag=β3, prefab=α2-2, sceneLoaded=β8) → **본 감사가 M4.3 backlog의 외부 교차검증**.

---

## 검증 + 봉합 (γ10 2~3라운드, 2026-05-29 추가)

사용자 위임: "본인 분담(Play 모드) 제외 전부 검증 + 필요시 수정 + Codex와 대화하며 진행". Claude 자가 코드 직독 + Codex 정적 교차검증 (round 1 검증 → 봉합 → round 2 재측 → round 3 수렴).

### 검증 결과 (A1~A6 + B1) — Claude 직독 + Codex round 1 **전원 일치 CONFIRMED**, 단 심각도 재산정
| ID | 발견 | 진위 | 심각도 재산정 |
|---|---|---|---|
| A1/β9 | clientTick 무검증 echo | CONFIRMED | **LOW** — 서버 권위 미사용 + 본인 reconcile만 영향(자기손해). 타 플레이어/서버상태 오염 X → M4.3 cheat-flag 묶음 |
| A2/β1 | PDL ID 순서 파생, 은퇴-ID 레지스트리 X | CONFIRMED | MED, 단 이름중복 가드 + append-only 관례로 부분완화 → **ADR감** (explicit id 속성) |
| A3/β4 | 패킷 정확 길이 미검증 (trailing byte) | CONFIRMED | **LOW** — processLen=dataSize라 경계 안 깨짐, under-length는 Disconnect → 엄격성 갭 |
| A4/β7 | reconnect 죽은 세션 (NetworkService 미리셋) | CONFIRMED | MED, 단 데모상 서버 안 죽음 + α2 "M4 재시도/백오프 backlog"와 동일 |
| A5/β13 | Disconnect try/finally 부재 | CONFIRMED | **MED → 봉합** |
| A6/β14 | subagent-routing.md 죽은 링크 | CONFIRMED | **→ 봉합** |
| B1/β2 | 클래스 HP 권위 전투 미적용 | CONFIRMED | **HIGH → 봉합** |

### 봉합 4건 (이번 패스 적용)
1. **B1 클래스 HP** — `PlayerEntity` 생성자가 `MaxHp=Stats.MaxHp; Hp=Stats.Hp` 초기화 (옛 `=100` 하드코딩 제거). `AddPlayerWithId`는 ctor가 MaxHp 확정 + 직후 Hp=이월값 → migration 정합. **회귀 테스트 3건 신설** (`PlayerStatsWiringTests.cs`: 전사 150 / 원거리 80 락인).
2. **β13/A5 Disconnect 견고화** — `Session.cs`(서버) + `ClientSession.cs`(클라) Shutdown/Close/Clear 각 단계 **독립 try/catch** → 어느 단계 예외도 다음 단계 안 막음 (FD 누수 + 큐 누수 동시 차단).
3. **A6/β14 죽은 링크** — `subagent-routing.md`의 `../agents/` `../hooks/` → `../../.claude/...` (편집 가능 정책 문서). `../knowledge/`는 하네스 문서 sweep으로 이월.
4. **β11 = 과대평가 + 원복** — 옛 `Math.Max(1,...)`는 *음수를 절대 반환 안 함*(Max가 ≥1 보장), "거대→1" magnitude만 비현실적. 제 long 격상 fix는 `(int)` 캐스팅에서 **음수 wrap = 회귀** → 테스트가 잡음(Step 4-A 표본) → **코드 원복 + 주석만 정정**.

### γ 재측 로그 (Step 4-A "봉합이 새 결함 도입" 표본 2건)
- **β11**: 봉합 → `FormulasTests.ComputeDamage_LargeBaseDamage_OverflowSafe` 실패 → fix가 회귀 실증 → 원복.
- **β13**: round 2 Codex가 "Shutdown/Close 한 try 공유 → Shutdown throw 시 Close skip" 지적 → 각 단계 독립 보호로 정제 → round 3 수렴 확인.

### 검증 산출물
- **테스트: 303 통과 / 0 실패 / 4 skip** (베이스라인 300 + B1 회귀 3건, 클린 `--no-incremental` 빌드).
  - ⚠️ **증분 빌드 stale 함정 관측**: `dotnet build -clp:ErrorsOnly`(증분) 직후 `dotnet test`가 GameWorld 싱글톤 227 거짓실패 → `--no-incremental` 클린 빌드로 해소. **교훈: 테스트 신뢰는 클린 빌드 후에만** (knowledge 박제 후보).
- **빌드: 0 에러 / 0 경고** (server + shared + clientnet + tools). Shared.dll / ClientNet.dll Plugins 자동 복사 정합.
- **봉합 미적용 (이월 사유 명시)**: β10 MoveSpeed(shared+client 결정론 → Play 실측 필요=본인 분담), β7 reconnect(라이프사이클+Play), β1 PDL-ID(ADR), β9/β4(LOW → M4.3 cheat-flag).

## 산출물
- 본 파일 + Codex 원본 출력 `.claude/staging/codex-audit-output.txt` (감사) + `codex-verify-round{1,2,3}-out.txt` (검증)
- 봉합 diff: `02_Server/GameServer/Maps/PlayerEntity.cs`, `02_Server/Network/Session.cs`, `04_ClientNet/ClientSession.cs`, `98_Shared/GameData/Formulas.cs`(주석), `00_Document/policies/subagent-routing.md`, `02_Server/GameServer.Tests/Maps/PlayerStatsWiringTests.cs`(신규)
