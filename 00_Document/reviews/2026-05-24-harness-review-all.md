# 하네스 자체 점검 — 2026-05-24 — scope=all

> `/harness-review all` — M4.1 5/6 Phase 마감 게이트 시점. reviewer(Tier 2-A) + plan-auditor(Tier 2-B) + knowledge-gc 동원. 메인 세션이 헤드라인 🔴 2건 직접 검증.

## TL;DR

- 🔴 결함 **2건** (둘 다 메인 세션 직접 검증 — false-promise 패턴 재범)
- 🟡 제안 **8건** (중복 제거 후)
- 🟢 정합 다수 (권한 경계 / 등급 매핑 / hook wiring / 트랙 A·B 분리 견고)
- knowledge-gc: 비활성화·응축·결함·승격 후보 **0건**, 미박 후보 **3건** + 트랙 B 혼입 **1건**

가장 큰 발견: **헌법 #4를 물리 강제하라고 만든 hook(`shared-discipline-guard`)과 재귀 차단을 약속한 문서들이, 정작 이 프로젝트가 가장 경계하는 "주석 약속 가짜화" 패턴을 스스로 재범**했다.

---

## 🔴 결함 2건 (메인 세션 검증 완료)

### 🔴-1 [축 1 / 헌법 #4] `shared-discipline-guard.sh:41` stale 경로 — PacketGenerator stale 검사 분기 사망

- hook이 `GEN_PACKETS="98_Shared/Protocol/GenPackets.cs"`(line 41) / `PACKET_FORMAT="98_Shared/Protocol/PacketFormat.cs"`(line 42)로 선언하나, **실재는 `98_Shared/Protocol/Generated/GenPackets.cs`** (`Generated/` 하위). `PacketFormat.cs`는 실재하지 않음.
- line 81-82 `grep -F "$GEN_PACKETS"`는 `Protocol/GenPackets.cs`가 `Protocol/Generated/GenPackets.cs`의 부분문자열이 **아니므로 영영 매칭 실패**. line 99 `[ -f "$GEN_PACKETS" ]`도 항상 false → "PDL 변경 있는데 GenPackets.cs 미갱신" 차단 분기(line 96-107)가 **결코 발동하지 않음**.
- 즉 PDL을 고치고 PacketGenerator를 안 돌려도 이 hook은 통과시킨다.
- **이게 정확히 이 hook 헤더(line 5-6)가 "옛 validate-shared-changes.sh가 경고만 해서 주석 약속이 가짜다 3회 봉합 사고 원인"이라며 막겠다고 선언한 그 패턴.** 봉합 도구가 스스로 재범.
- 비대칭 신호: `reviewer-auto-trigger.sh:37`은 `*/98_Shared/Protocol/*.cs` 글롭으로 `Generated/`까지 닿아 정합. 같은 산출물을 두 hook이 다른 정확도로 참조한 것이 함정의 단서.
- **수정**: `GEN_PACKETS="98_Shared/Protocol/Generated/GenPackets.cs"`로 정정. `PACKET_FORMAT`은 실재 산출물 확인 후 교정 또는 제거. (DLL 동반 검사 line 109-112는 경로 정확 → 일부 안전망 생존.)

### 🔴-2 [축 2/6] "재귀 차단 Hook" 단정이 코드에 미실재 — `circuit-breaker.sh`

- `_escalation.md` §6 / `coordinator.md:293` / `subagent-routing.md:184` 세 곳이 "`circuit-breaker.sh`가 Worker→Worker 재귀 호출을 *차단*한다"고 단정.
- 실재 `circuit-breaker.sh`는 (1) PostToolUse에서 *같은 도구 N회 반복 시 알림만*(exit 0, Stop 아님), (2) line 28 `Bash) exit 0`으로 Bash 제외. **SubAgent 호출(Agent/Task tool)에 대한 재귀 판정 로직이 전혀 없음** (메인 세션 grep 검증: `recur|재귀|Agent|Task|Worker` 매치 0건).
- "재귀 차단"은 문서 약속일 뿐 코드 미실재. 헌법 #2/#4 3회 봉합 시리즈와 동형 패턴.
- 부분 인지: `subagent-routing.md:6` 신선도 주석에 "재귀 차단 Hook 부재 발견 = work-pin 별 시점"이라 *발견 기록*은 있으나, 본문 3곳 단정은 미정정 잔존.
- **수정**: 택1 — (A) circuit-breaker를 "반복 알림" 책임으로 명확히 하고 문서를 "차단 아니라 *반복 알림*"으로 정정, (B) 재귀 차단은 별도 Hook 신설 전까지 "coordinator/메인 세션 규율로 강제"라고 솔직히 표기. 어느 쪽이든 "Hook이 차단한다" 단정 3곳을 실재와 일치시킬 것.

> 두 🔴 모두 *비가역 아님*(하네스 문서/스크립트 정정, 영호 단독 통제 영역). 그러나 둘 다 "잘못된 안전 가정" 유발 — "hook이 막아주겠지" → 실제 미발동. 우선순위 최상.

---

## 🟡 제안 8건 (reviewer + plan-auditor 중복 제거)

| # | 축 | 내용 | 근거 | 등급근거 |
|---|---|---|---|---|
| 🟡-1 | 구조 | `policies/INDEX.md` 전체가 **mv 이전(전환 예정) 시제**. line 12/16/31-42가 "Phase 06 전환 시 mv 예정"인데 파일은 이미 9개 실재 | `policies/INDEX.md` | 헌법 line 7이 가리키는 현재 상태표 오인 소지 |
| 🟡-2 | 헌법 | 위험 깃발 **헌법 3종 vs 실재 4종** (`harness` 누락). risk-detector.sh + grade-and-risk.md는 4종 운영 | `CLAUDE.md:183-187` vs `risk-detector.sh:96-103` | 헌법(상위)이 정책(하위)보다 1개 뒤처짐 — reviewer+plan-auditor 공통 지적 |
| 🟡-3 | 설계 | **04_ClientNet owner 모순**. server/shared는 R only인데 .csproj/빌드 설정 변경 owner 미명시. Phase 03에서 활발히 변경된 영역 | `server.md:26`/`client.md:22`/`shared.md:168`/`_routing.md:90-92` | 책임 공백, 실 사고는 아직 없음 → 🟡 (plan-auditor는 🔴 주장) |
| 🟡-4 | 메타 | **Hook 개수 7/8/10 공존**. README 본문 7과 8 혼재, 의뢰 명세 10. 실재 = 기능 hook 8 + hook-common(라이브러리). settings.json wiring은 8 정합·고아 0 | `README.md:11,34,66` vs `settings.json` | 기능 결함 아닌 문서 숫자 drift → 🟡 (plan-auditor는 🔴 주장) |
| 🟡-5 | ADR | `ADR-015` 본문/제목이 옛 hook 이름 `validate-phase-gate.sh`. ADR-022가 rename 명시, 실재는 `phase-gate-validator.sh`. INDEX.md:38도 옛 이름 | `ADR-015` + `ADR/INDEX.md:38` | superseded 각주 보강 (ADR 본문은 불변 정석) |
| 🟡-6 | 설계 | `reviewer-auto-trigger.sh`가 risk-detector 결과를 안 읽음 → "위험 깃발 → reviewer 무조건"이 hook 레벨 미구현 (prefab만 바뀌면 권유 안 뜸) | `reviewer-auto-trigger.sh:36-49` vs `review-tiering.md:31` | 메인 세션 판단으로 보완되나 hook 자동성 공백 |
| 🟡-7 | 설계 | server vs shared의 `98_Shared/` 쓰기 경계가 "정의 vs 사용" 단서로만 구분 → "새 패킷" 복잡 등급 동시 동원 시 경합 소지 | `server.md:22,106` vs `shared.md` | server.md:22를 "사용 기본, 정의는 shared 게이트"로 좁히면 해소 |
| 🟡-8 | 메타 | 시제 잔재 잡건: `_usage.md:34` "Phase 06 마감 시 박음"(완료인데 진행중 시제) / `_routing.md:126` 갱신이력 "8 확장"(본문은 9) / knowledge-gc 경로 표기 `../knowledge` vs `.claude/knowledge` 불일치 | 각 파일 | 영향 적음, 묶어 정리 |

---

## knowledge-gc 결과 (제안만 — 자동 정리 X)

- **비활성화** 0 / **응축** 0 / **결함 정정** 0 / **승격** 0 (gamma-pre-validation은 이미 plan-auditor로 내재화 — 현 위치 적절)
- **미박 후보 3건** (트랙 A 적합, Phase 05 DONE.md ★★ 명시):
  - **A.** `shared-code-discipline-relocation-pattern` → `shared/_index.md` ("서버 권위 ≠ 서버 전용 코드 위치")
  - **B.** `init-setter-net-standard-2-1-trap` → `shared/` 또는 `cross-cutting/_index.md` (.NET Standard 2.1 IsExternalInit 함정)
  - **C.** `false-promise-pattern` 확신도 갱신 — 현재 "실측 3건" → M4.1 누적 26건+ (★★★, ADR-024 제도화)
- **트랙 B 혼입 1건**:
  - **D.** `client/_index.md` `unity-version-hash-pinning` 본문 中 "학부생 본인이 처음 .gitignore 제안 → 스스로 정정 (학습 가치)" 한 줄 = 회고 성격 → 트랙 A에서 1줄 삭제 제안

> ⚠️ A·B·C·D 전부 사용자 결정 게이트 대기. 자동 박제·삭제 X.

---

## 양식 비용 평가 (정량)

| 항목 | 실측 | 목표 | 판정 |
|---|---|---|---|
| **work-pin (`current-pin.txt`)** | **104줄** | 30~40줄 | 🟡 **2.6배 초과** |
| -DONE.md (M4.1 cadence) | 01/02/05만 박힘, 03/04(보통) 없음. 평균 95줄 | 복잡/대규모만 | 🟢 정합 |
| 5단계 보고 (HTML 산출) | 누적 17건 (마일스톤 단위) | 대규모만 | 🟢 과발동 신호 없음 |

- **🟡 work-pin 비대**: 104줄은 목표 30-40의 2.6배. "본 세션 누적 commit 10건" 전체 나열 / "별 시점 박을 가닥" 9건 / "옛 브랜치 정리" 5건 등 *역사 누적*이 핀에 쌓임. pin은 *작업 좌표 보존*용(압축 양식)인데 세션 로그화 경향. → 마감된 commit 리스트·완료 Phase 상세는 -DONE.md/CHANGELOG로 이관하고 핀은 "현재 멈춤 지점 + 다음 액션"으로 다이어트 권장. (단 학습 질문 끼어듦 대비 좌표 보존이 핀 목적이라 *과도한 다이어트도 위험* — 40~50줄 현실 타협 가능.)
- -DONE.md / 5단계 보고는 등급 게이트대로 작동 (M4.1 보통 Phase는 DONE 안 박음 = 양식 다이어트 정신 정합).

---

## 결정 권유

- 🔴 **즉시 봉합 권장 (2건)**: `shared-discipline-guard.sh:41` 경로 정정 + circuit-breaker 재귀 차단 단정 3곳 정정. 둘 다 헌법 #4/false-promise 핵심 경계 패턴과 동형 → "안전망 가짜로 떠 있음" 상태. 영호 단독 통제 영역이니 GO 시 직접/위임.
- 🟡 **다음 하네스 정비 묶음 (8건)**: 시제 잔재(🟡-1/8)·헌법 4번째 깃발(🟡-2)·04_ClientNet owner(🟡-3)·hook 개수(🟡-4) 일괄. **근본 봉합 후보**: ADR-024 false-promise cadence 마일스톤 점검에 "*전환 예정/진행 중* 시제 grep" 한 줄 추가 → 마이그 완료 후 stale 시제 잔존 패턴 자동 검출.
- 🟢 그대로: 권한 경계 / 등급 매핑 / hook wiring / 트랙 A·B 분리 / 재귀 차단 *DAG 구조*(검증가 무쓰기) 견고.
- knowledge A·B·C·D: 사용자 결정 후 knowledge-gc 재호출로 실행.

---

## 봉합 적용 현황 (2026-05-24, 사용자 GO 후)

사용자가 🔴 2건 봉합 + knowledge A·B·C·D 전부 반영에 GO.

### ✅ 적용 완료

- **knowledge A** — `shared/_index.md`에 `shared-code-discipline-relocation-pattern` 신규 박제 (표 + 디테일 본문 + 갱신 이력)
- **knowledge B** — `shared/_index.md`에 `init-setter-net-standard-2-1-trap` 신규 박제
- **knowledge C** — `shared/_index.md` `false-promise-pattern` 확신도 "실측 3건" → "26건+ (ADR-024 제도화)" 갱신
- **knowledge D** — `client/_index.md` `unity-version-hash-pinning` 디테일 中 트랙 B 회고 1줄 제거
- 🔴-1 부분 — `shared-discipline-guard.sh:57` 산출물 안내 주석만 정정 통과

### ⛔ 차단 — 사용자 직접 적용 필요 (auto-mode classifier hard block)

`.claude/hooks/` + `.claude/agents/`는 **agent-control 파일이라 사용자 승인과 무관하게 self-modification 하드 차단**. 우회 시도 안 함. 아래 정확한 diff를 영호가 직접 적용 (편집기 또는 `! ` 셸):

**🔴-1 `.claude/hooks/shared-discipline-guard.sh`** (line 41-42, line 10):
```diff
@@ line 10 (주석) @@
-#      (a) PacketGenerator 산출물 (GenPackets.cs / PacketFormat.cs) 재생성됐는지 (mtime)
+#      (a) PacketGenerator 산출물 (Generated/GenPackets.cs) 재생성됐는지 (mtime)
@@ line 41-42 @@
-GEN_PACKETS="98_Shared/Protocol/GenPackets.cs"
-PACKET_FORMAT="98_Shared/Protocol/PacketFormat.cs"
+GEN_PACKETS="98_Shared/Protocol/Generated/GenPackets.cs"
+# (PacketFormat.cs는 PacketGenerator 내부 템플릿이지 98_Shared 산출물 아님 — 옛 stale 변수 제거)
```
> 근거: PacketGenerator 실 산출물 = `Generated/GenPackets.cs` (+ Server/ClientPacketManager.cs). `PacketFormat.cs`는 `99_Tools/PacketGenerator/` 내부 템플릿 클래스이고 `PACKET_FORMAT` 변수는 스크립트 어디서도 미사용(죽은 변수). 정정 후 line 81-82/99-101의 stale 검사 분기가 실제 작동.

**🔴-2 `.claude/agents/_escalation.md`** (line 158-159 — 가장 명확한 false claim):
```diff
-   └─ Hook (circuit-breaker.sh, Phase 03 산출물)가 차단:
-       "재귀 호출 차단 — Worker → Worker 직접 호출 금지"
+   └─ 구조적으로 차단: Worker는 위임 권한 없음 + coordinator만 단독 위임자.
+       (Hook 강제 아님 — circuit-breaker.sh는 *반복 도구 사용 알림* advisory.
+        Worker→Worker 재귀 판정 로직은 미실재. 차단은 coordinator/메인 규율로 강제.)
```
line 193 + coordinator.md:293 + subagent-routing.md:184의 `circuit-breaker.sh (... 재귀/재시도 차단)` 표기도 "(반복 도구 사용 *알림* advisory — 재귀 차단은 구조 강제)"로 정정 권장.
> 근거: `circuit-breaker.sh`는 PostToolUse 반복 도구 알림(exit 0)만 하고 Bash 제외, Agent/Task 재귀 판정 로직 0건 (grep 검증). `subagent-routing.md:6`은 이미 "재귀 차단 Hook 부재 발견" 기록 보유.

> 참고: `subagent-routing.md`(00_Document/policies/)는 hard block 아니라 편집 가능하나, 3곳 중 2곳(_escalation/coordinator)이 차단이라 *부분 정정은 비대칭 잔존* → 3곳 묶어 영호가 한 번에 적용 권장.
