# 유현용 핸드오프 프롬프트 — M3 Phase 06/07 프로토콜 변경 알림 (5/19)

> 디스코드로 유현에게 전달. 위는 디스코드 본문, 아래 ===로 구분된 프롬프트는 유현 자기 Claude한테 그대로 복붙.

---

## 📩 디스코드 메시지 (영호 → 유현)

유현아, M3 전투/보스 쪽 들어가면서 프로토콜 버전이 바뀔 예정이야.

요약:
- Phase 06에서 `ProtocolVersion.Current`가 **2 → 3**으로 bump 예정
- 신규 combat 패킷 추가 예정:
  - `C_Attack`
  - `S_EntitySpawn`
  - `S_HitResult`
  - `S_EntityDeath` 또는 Option B면 HP 0 기반 despawn
- Phase 07에서 `S_StageClear` 별도 추가 예정
- `Shared.dll`도 같이 갱신되므로, 내가 Phase 06/07 main 머지했다고 말하기 전에는 네 쪽에서 combat dispatch를 미리 붙이지 않는 게 안전함
- stale branch에서 서버 붙으면 handshake mismatch로 접속이 끊기는 게 정상임. 이건 버그가 아니라 ProtocolVersion 안전망이야

지금 네가 할 수 있는 안전 작업:
1. Phase 08a asset/prefab variant 쪽 계속 진행
2. `RemoteEntity` 컴포넌트는 보존
3. combat/StageClear UI dispatch는 내가 Phase 06/07 머지 알림 준 뒤 pull 받고 진행

내가 main 머지 끝나면 다시 말할게. 그때 `git pull origin main` 받고 아래 프롬프트를 네 Claude한테 붙여줘.

---

## 🤖 유현 Claude용 프롬프트 (그대로 복붙)

───────────── 복붙 시작 ─────────────

영호가 M3 Phase 06/07 서버 전투 + 보스 StageClear 프로토콜 변경을 진행 중이야. 본 세션에서 다음 처리해줘.

## 1단계 — 변경 영향 이해

다음 내용을 먼저 전제로 잡아:

- `ProtocolVersion.Current`가 **2 → 3**으로 bump 예정
- stale Shared.dll / stale branch로 서버에 붙으면 handshake mismatch로 disconnect 되는 게 정상
- 전투/보스 패킷은 영호 Phase 06/07 머지 후 `98_Shared/Protocol/Generated/GenPackets.cs`와 Unity `Assets/Plugins/Shared/Shared.dll`에 반영됨
- 네가 `98_Shared/`나 generated packet 파일을 직접 수정하면 안 됨. main pull로 받아야 함

## 2단계 — 지금 바로 해도 되는 작업

Phase 08a 범위는 계속 진행 가능:

- `PlayerBase.prefab` + variant 패턴
- `LocalPlayer.prefab` 회귀 검증
- `RemotePlayer.prefab` 비주얼 교체
- `RemoteEntity` 컴포넌트 보존
- 캐릭터 sprite / animator / rendering 쪽 정리

주의:

- `Scripts/Network/UnityClientSession.cs`의 combat packet dispatch는 영호 Phase 06/07 merge 후 진행
- `Scripts/UI/` StageClear/HP 표시도 서버 패킷 이름이 확정된 뒤 연결
- UI mock은 만들어도 되지만, generated packet type을 가정해서 컴파일 코드를 미리 박지 말 것

## 3단계 — main pull 이후 해야 할 작업

영호가 "Phase 06/07 main 머지 완료"라고 말하면:

1. `git pull origin main`
2. Unity 재시작 또는 domain reload로 새 `Shared.dll` 반영
3. 서버 접속 시 handshake OK 확인
4. `S_EntitySpawn` / `S_HitResult` / `S_EntityDeath` 또는 HP 0 despawn 흐름 확인
5. Phase 08b에서 `S_StageClear` 수신 → Stage Clear UI 표시 연결

## 4단계 — 디버깅 기준

증상별 판단:

- 접속 직후 끊김 + protocol mismatch 로그  
  → stale branch 또는 stale `Shared.dll`. pull/rebuild 먼저.

- `Unknown PacketId` warning  
  → generated packet / Unity plugin DLL mismatch 가능성. main pull + Unity reload.

- enemy가 안 보임  
  → `S_EntitySpawn` 수신 dispatch 또는 enemy prefab/render binding 확인.

- HP UI가 안 바뀜  
  → `S_HitResult.currentHp/maxHp` dispatch 확인. 클라에서 damage 계산하지 말 것.

- StageClear UI가 안 뜸  
  → Phase 07 `S_StageClear` 수신 여부부터 확인. 클라가 보스 사망을 자체 판정하지 말 것.

## 5단계 — work-pin 메모 추가 권장

네 `.claude/state/current-pin.txt` 주의사항에 한 줄 추가:

```text
ProtocolVersion v3 예정/반영 — Phase 06/07 main pull 전 combat packet dispatch 선작업 금지. stale Shared.dll이면 handshake mismatch가 정상.
```

학부생 멘토링 톤 유지. 모르는 packet 이름은 추측하지 말고 main pull 후 generated `GenPackets.cs`를 기준으로 확인해.

───────────── 복붙 끝 ─────────────

---

## 영호 본인 체크리스트

- [ ] Phase 06/07 main 머지 후 유현에게 pull 알림
- [ ] ProtocolVersion 3 bump 여부 확인
- [ ] `Shared.dll` 갱신 포함 여부 확인
- [ ] 유현이 combat dispatch를 stale branch에서 먼저 붙이지 않게 안내
