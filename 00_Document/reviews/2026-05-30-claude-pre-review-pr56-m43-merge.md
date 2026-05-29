# Pre-Review for Codex β — 2026-05-30 — PR #56 (feature/m4.3 → main) 머지 전 cross-review

> 본 문서는 `/cross-review` Step 3-A 산출물. 본인이 별 세션에서 Codex β 호출 시 입력 참조 자료.
> β 결과를 가져오면 Claude가 γ 비교 → `2026-05-30-cross-review-pr56-m43-merge.md` 박음.

## 변경 범위

- **브랜치**: `feature/m4.3` vs `main` (origin/main = `1078fec`)
- **규모**: 119 파일, +9094 / -1422, 13 커밋
- **등급**: 대규모 (위험 깃발: irreversible — ProtocolVersion 6→7 bump + trust-boundary GameSession 추출 + unity-asset Shared.dll)
- **3 덩어리**:
  1. **Code Convention / ADR-028** (하네스: `CODE_CONVENTION.md`, `INDEX`, reviewer 축6, `convention-size-guard.sh` hook) — `3afc528`/`db31fda`/`6dc0e26`
  2. **M4.3 발표 Phase 07 — 적 AI 서버** (patrol/chase FSM + `S_EntityState`, ⚠️ ProtocolVersion 6→7 bump + PDL append-only) — `61bbea4`
  3. **M4.3R 리팩토링 7 Phase** (God class 3개 분리 + 네이밍 + EnemyViewFactory + 테스트, 순수 리팩토링 PDL/Protocol 변경 0) — `400b6b0`~`2d447ad`

## main 대비 diff 요약 (자연어)

God class 3개(GameMap 665→260 / UnityClientSession 665→224 / GameSession 700→566)를 컨테이너+System/dispatch/추출 클래스로 분해하고, 네트워크 레이어 네이밍(m_ → _camelCase)을 서버 Network/ + ClientNet 자매 양쪽 대칭 정합. 별개로 M4.3 발표 Phase 07이 적 AI 서버(patrol/chase FSM)를 추가하며 `S_EntityState`(PacketID 19) PDL append + ProtocolVersion 6→7 bump. Code Convention(ADR-028) 하네스 기반도 같은 브랜치에 묶임. 서버 빌드 0/0, 테스트 322 통과/0 실패/4 skip. 클라 Play-test 통과.

## α (Claude reviewer) 결과 요약 — GO 🟢 (🔴 0 / 🟡 2)

| 축 | 결과 | 근거 |
|---|---|---|
| 헌법 #2 Protocol | 🟢 | PDL `S_EntityState`가 `</PDL>` 직전 append (`PDL.xml:218`), PacketID 19 enum 끝(`GenPackets.cs:38`), ID 0~18 불변. `ProtocolVersion.cs:42` Current=7 + Phase09 묶음 주석. Shared.dll 40960→43008 = S_EntityState 반영, 양쪽 동일 어셈블리(헌법 #4) |
| 헌법 #3 trust-boundary | 🟢 | IntentRateLimiter.TryConsume rate-limit byte-for-byte 이전 + fail-closed drop 유지. MapMigration 검증 3단(portal/존재/근접2unit) 그대로. attacker는 패킷 아닌 `_entityId` 강제(`GameSession.cs:384`) — 도용 방어 보존. migration 상태 Volatile 캡슐화 |
| 동작 보존 (M4.3R) | 🟢 | EnemyAI/Combat/Respawn System 전부 `GameMap.Tick` 직접 호출(EnqueueJob 미경유, §1.1 정합). 주석 "본문 그대로" + EnemyAiTests 12 신규 + 315 회귀 0. tick 안 await/Sleep/DB 0 |
| cross-phase (02 dispatch ↔ 07 패킷) | 🟢 | 클라 S_EntityState 핸들러 0건 = **위반 아님**. Phase 08로 의도 분할(07-DONE.md + 08 pending 명시). 미등록 패킷 = `LogWarning + drop`(forward-compatible). handshake version mismatch가 1차 cutoff |
| ADR-012 자매 동시변경 | 🟢 | `m_` 잔존 서버 Network/ + ClientNet 양쪽 0 매치. ClientNet 4파일 + Shared.dll 동일 PR 동시 변경(빌드 비대칭 0) |

**α 🟡 2건 (동작 무관, 머지 차단 아님)**:
- `02_Server/GameServer.Tests/Network/BroadcastTests.cs:226` 주석 `SnapshotTickInterval=5` stale (코드는 상수 직접 참조라 무관)
- `03_Client/.../ClientPacketHandlers.cs:13` 주석 "12개" → 실제 11개 (Phase 08에서 자연 정합)

## Codex β 점검 가닥 (본인 직접 호출 시 참고 — α가 못 봤을 수 있는 차원)

α는 헌법/ADR 정적 점검 우위. β(Codex)는 **코드 직접 접근 + 동작 추론** 우위. 다음을 특히 봐달라고 던지면 좋음:

1. **God class 추출의 *숨은 동작 변화***: GameMap/UnityClientSession/GameSession 분해에서 — 필드 초기화 순서, null 체크 누락, 추출 경계에서 예외 전파 경로 변경, 이벤트 구독/해제 누락이 있는지 (α는 "본문 그대로" 주석을 신뢰했으나 β는 실제 diff로 검증)
2. **trust-boundary 추출 race**: IntentRateLimiter/MapMigration이 GameSession에서 분리되며 — tick thread vs 네트워크 콜백 thread 간 동시성 경계가 미묘하게 바뀌었는지 (M4.2 γ 9회차 패턴: 봉합/추출이 새 race 차원 도입)
3. **Phase 07 적 AI 서버 결함**: aggro 스캔 O(N·M) / chase 경계 clamp 없음 / aggro 타이브레이크 (work-pin reviewer 🟡 기지 항목 — β가 재확인)
4. **dotnet test 재실측**: 322/0/4가 본인 환경에서도 재현되는지 (clean build → test)
5. **옛 사고 패턴 잠복**: false-promise 변종 / 문서-코드 drift (α가 🟡 2건 잡음 — β가 추가로 더 있는지)

## 본인 Codex 호출 명령어 (별 세션 터미널)

```bash
# 권장 — PR 머지 전 main 대비 변경분 검토
codex review --base main
```

- 위 pre-review MD를 prompt에 붙이거나 첨부해서 "α가 이렇게 봤는데 β 시각으로 cross-check + 위 5가지 가닥 점검" 형식으로 던지면 γ 비교가 깔끔.
- 결과 가져오는 형식: (A) raw 출력/요약 던지기 → Claude γ 비교 / (B) "β 스킵" → α 단독 진행 / (C) "Codex가 봉합 박음" → diff 보여주면 γ 비교 + 후속.
