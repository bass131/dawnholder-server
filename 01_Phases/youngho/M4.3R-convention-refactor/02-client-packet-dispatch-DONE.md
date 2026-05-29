---
summary: UnityClientSession God class(665줄)를 §3.2 IPacketHandler+dispatch로 분리 — 11 핸들러 + RosterTransitionBuffer + SceneRouter, 224줄. reviewer CS0070 봉합. Unity 컴파일/Play-test는 사용자 수동 보류.
phase: 02-client-packet-dispatch
work-id: m4.3r-phase02-client-dispatch
status: done
grade: 복잡
owner: youngho
completed_at: 2026-05-29
commit: 0c2b59b
---

# Phase 02 — 클라 패킷 dispatch 분리 완료 박제

**소요 시간**: ~1h (client Worker + reviewer 봉합)

## TL;DR

클라의 가장 큰 God class였던 `UnityClientSession`(665줄)의 inline switch(11 패킷 직접 dispatch + 5 책임 혼재)를 서버 `Handlers/` 패턴을 미러한 `IClientPacketHandler` + Dictionary dispatch로 분리했다. roster 전환 상태머신을 `RosterTransitionBuffer`로, 씬 매핑을 `SceneRouter`로 추출해 224줄로 줄였다. reviewer가 Unity 헤드리스 컴파일로는 못 잡는 **CS0070(event 외부 raise) 컴파일 차단 1건**을 정적 분석으로 잡아 봉합했다. 동작 보존(11 패킷 1:1 이동) — 단 Unity 컴파일/Play-test는 사용자 수동 확인 보류.

## 5단계 보고

- **무엇을 만들었나** — `IClientPacketHandler.cs`(15줄) + `ClientPacketHandlers.cs`(11 핸들러, 373줄) + `RosterTransitionBuffer.cs`(99줄) + `SceneRouter.cs`(26줄). UnityClientSession 665→224줄.
- **왜 필요한가** — OnRecvPacket의 12분기 inline switch(§3.2 위반) + 5 도메인 혼재(framing/roster 상태머신/pending spawn/씬 매핑/latency 시뮬). 패킷 추가마다 switch+메서드 양쪽 수정 + 단위 테스트 불가. M4.3 Phase 08(enemy AI 클라)이 새 패킷을 추가하기 전에 dispatch 테이블로 바꿔야 부채가 안 쌓임.
- **어떻게 만들었나** — 서버 `IPacketHandler`+`HandlerRegistry` 패턴 미러: `Dictionary<PacketID, IClientPacketHandler>` 11 등록, OnRecvPacket은 lookup만. roster overflow 가드 복붙 3곳 → `RosterTransitionBuffer.TryBuffer()` 1곳 응축. UnityClientSession은 framing+dispatch+main-thread 마샬링 컨테이너만 잔류. pending spawn static 3필드는 LocalPlayerController.Awake가 소비 → 잔류(§0.3).
- **테스트 결과** — 구조 AC(224줄/dispatch 11/overflow 1곳/§0.3 잔류) 통과. reviewer 헌법 hard 위반 0(🔴 CS0070 봉합 후). **Unity 헤드리스 컴파일 불가 → 컴파일 green + Play-test는 사용자 수동(마일스톤 마감 시)**.
- **다음 스텝** — Phase 05(클라 기회성) / 06(클라 네이밍). 마일스톤 마감 시 Unity Play-test로 클라 3 Phase 일괄 확인.

## AC 검증 결과

```bash
$ wc -l 03_Client/Assets/Scripts/Network/UnityClientSession.cs
  224 ...UnityClientSession.cs   # < 600 (size-guard 해소, 665→224)

$ grep -c "PacketID\." 03_Client/Assets/Scripts/Network/ClientPacketHandlers.cs   # dispatch 등록 핸들러
  11   # S_HandshakeResult/Pong/EnterMap/Snapshot/PlayerJoin/PlayerLeave/EntitySpawn/HitResult/EntityDeath/StageClear/MapTransition
```

reviewer(Tier 2-A): 🔴 1건 → **봉합 완료**. `HandshakeResultHandler`가 `session.OnHandshakeOkEvent?.Invoke()` 직접 호출 = CS0070(C# event는 선언 클래스만 raise) → `UnityClientSession.RaiseHandshakeOk()` 추가 + 핸들러가 그걸 호출(원본 로그 순서 보존). RosterTransitionBuffer 3분기 동작 동일, PacketSession 상속 + MainThreadDispatcher 마샬링 경로 보존 확인.

⚠️ **미완(사용자)**: Unity Editor 컴파일 에러 0 + Play-test(마을→사냥터 roster drain / 적 spawn / Handshake→EnterMap→Snapshot 순서). 헤드리스 `dotnet build`로 03_Client 컴파일 불가가 구조적 한계.

## 결정 흐름 (회고 참고용)

- **"12 패킷" vs 실제 11** → 원본 switch case 정확히 11개. S_EntityState는 서버 broadcast하나 원본 클라 핸들러 없었음(default drop). 동작 보존 위해 미등록 유지(enemy 렌더는 M4.3 Phase 08 과제).
- **event raise 캡슐화(CS0070 봉합)** → `RaiseHandshakeOk()` 별도 메서드 vs SetHandshakeOk()에 fold-in. 별도 메서드 채택 — fold-in하면 원본 로그 순서(SetHandshakeOk → Debug.Log → raise)가 바뀌어 동작 변경. 별도 메서드가 순서 보존 + 기존 Set* 캡슐화 패턴과 일관.
- **pending spawn static 잔류(§0.3)** → 추출 후보였으나 LocalPlayerController.Awake 실호출자 있어 추출 시 호출 경로만 늘고 이득 0. 컨테이너 잔류.

## 막혔던 지점

- **CS0070 (event 외부 raise)** → 증상: 핸들러를 빼내며 `session.OnHandshakeOkEvent?.Invoke()`가 외부 호출이 됨. 원인: C# event는 같은 어셈블리여도 선언 *타입* 밖에서 raise 불가(구독 +=/-=만 허용). Unity 헤드리스 컴파일 불가라 빌드로 안 잡힘 → reviewer 정적 분석이 발견. 해결: 선언 클래스에 `internal void RaiseHandshakeOk()` raise 메서드 추가.

## 학습 일지 후보 키워드

- §3.2 IPacketHandler dispatch 테이블(서버/클라 대칭), C# event vs delegate raise 캡슐화 경계(CS0070), Unity 헤드리스 컴파일 한계 + reviewer 정적 분석 보완, 상태머신 추출(RosterTransitionBuffer), §0.3 pending spawn 잔류
