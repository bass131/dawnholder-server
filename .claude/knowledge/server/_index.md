---
name: knowledge-server-index
description: Server 도메인 (02_Server/ + 98_Shared/ 서버측) 학습 캐시 인덱스
domain: server
maintainer: youngho
last_updated: 2026-05-20
---

# Server Knowledge — _index.md

> **누가 통독**: `server` SubAgent (필수) + `coordinator` / `reviewer` / `plan-auditor` (R only, 필요 시)
> **함께 통독**: 본 캐시 + [`../shared/_index.md`](../shared/_index.md) + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| `lifecycle-race-broadcast-skip` | N-1 fan-out broadcast 시 IsClosing session skip — race window deterministic 재현 | 새 broadcast 패킷 신설 시 / lifecycle race 의심 증상 시 | M3 Phase 04 (commit `5ea1123`) + Phase 10 일반화 |

---

## 디테일 본문

### `lifecycle-race-broadcast-skip`

**증상**: Session A가 OnDisconnected 처리 *중*에 (`_closing=true`) Session B의 broadcast가 도착 → Send 호출 시점에 A 소켓 이미 닫힘 → 예외 또는 silent drop. N-1 fan-out에서 *N개 receiver마다* race window 존재.

**패턴**: TCP socket lifecycle은 *비동기 cleanup*. broadcast 발신 시점과 receiver의 cleanup 시점이 겹치면 race. Phase 10에서 처음 발견 (단일 session) → Phase 04에서 N-1 fan-out 일반화.

**봉합 (3종 skip)**:
```csharp
// GameMap.BroadcastToAll(payload, except=null)
foreach (var session in _players.Values) {
    if (session == null) continue;            // 1. owner null skip
    if (session == except) continue;          // 2. except skip (자기 자신)
    if (session.IsClosing) continue;          // 3. IsClosing skip (race 봉합)
    session.Send(payload);
}
```

`IsClosing` getter는 `Volatile.Read(_closing)`으로 thread-safe (`OnDisconnected` 다른 thread).

**Deterministic 재현 (테스트 패턴)**:
- `LifecycleRace_NewJoinBroadcastSkipsClosingSession` (BroadcastTests.cs)
- 순서: `s2.OnConnected → s1.OnDisconnected → Tick` (s2 enter 시점에 _players=[s1] + s1.IsClosing=true → skip 분기 *반드시* 통과해야 PASS)
- 잘못된 순서(s1.OnDisconnected → s2.OnConnected): cleanup이 FIFO로 먼저 처리 → s2 enter 시 _players=[] → skip 분기 *안 거치고도* 통과 (false confidence)

**사례**:
- M3 Phase 10 — 단일 session race 첫 발견 + `IsClosing` 도입
- M3 Phase 04 (commit `5ea1123`) — N-1 fan-out 일반화 + deterministic 재현 테스트
- Codex γ 5회차 Medium #1 — 순서 보강 (γ 검토로 false confidence 발견)

**확신도**: 실측 2건 + γ 검토 1건. Rule of Three 미달 — 다음 lifecycle race 패턴 발견 시 ★★★ 승격.
**관련 키워드**: [[gamma-pre-validation-pattern]] (γ가 false confidence 발견)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *02_Server/ + 98_Shared/ 서버측* 패턴을 담습니다:

- **포함**: 권위 검증 / lifecycle / broadcast / handler dispatch / tick loop / 영속화 / cheat-flag
- **제외**:
  - Protocol 모양 / PDL 정의 → [`../shared/`](../shared/_index.md)
  - Unity 측 prediction / reconcile → [`../client/`](../client/_index.md)
  - 헤드리스 봇 / 부하 / 퍼징 → [`../qa/`](../qa/_index.md)
  - 환경 사고 / 툴 함정 / 마이그 패턴 → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../CLAUDE.md) "Server Authority" + "Trust Boundary" + "No Blocking Calls" 절대 원칙
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/server.md`](../../agents/server.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
