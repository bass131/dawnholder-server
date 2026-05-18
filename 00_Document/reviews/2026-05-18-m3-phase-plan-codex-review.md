# M3 Phase 분해 — Codex β xhigh 검토 결과

> **γ 방식 3회차 (Rule of Three)** — Codex β xhigh 검토 박제
> **검토 prompt**: [`2026-05-18-m3-phase-plan-codex-prompt.md`](2026-05-18-m3-phase-plan-codex-prompt.md)
> **검증일**: 2026-05-18
> **build check**: `dotnet test Dawnholder.slnx --nologo` → **132 통과 / 0 실패 / 1 skip** (현 빌드 안전)

---

## 결론

분해 안 자체는 *Codex에 던질 수 있는 수준*이지만, **실제 코드 상태보다 낙관적**. 특히 M3 핵심인 "두 명 서로 보임" + "전투/보스/StageClear"가 현 구조에서 생각보다 큰 변경.

---

## 주요 발견 6건

### 1. Phase 03/04 (broadcast + movement 동기) 과소추정 ★

- 현재 서버 snapshot = [`GameMap.cs:95`](../../02_Server/GameServer/Maps/GameMap.cs#L95) **owner unicast**
- 클라 = [`UnityClientSession.cs:152`](../../03_Client/Assets/Scripts/Network/UnityClientSession.cs#L152) 들어온 `S_Snapshot`을 무조건 로컬 reconcile로 넘김
- "broadcast 인프라"만으로 부족 — **클라 측 별도 인프라 필요**:
  - `entityId` 기준 로컬/타인 분기
  - remote player **registry**
  - prefab **spawn/despawn**
  - remote **interpolation buffer**

→ Phase 03/04에 명시. 두 Phase로 **분리** 권장.

### 2. Phase 05/06 (전투) 3.5h 과소

- [`PlayerEntity.cs:11`](../../02_Server/GameServer/Maps/PlayerEntity.cs#L11) 현재 = **이동 엔티티만**. HP/Enemy/Boss/Combat state 없음
- 공격 패킷 + cooldown/rate-limit + 서버 반경 판정 + enemy HP + death broadcast + boss clear trigger **모두 신설** = 2h+1.5h는 과소
- 응급 데모면 **더 강한 제약**: "적 AI 없음, 고정 위치 HP dummy, 공격은 서버 range+cooldown만"

### 3. PDL 변경 의무에 `--no-manager` 위험 빠짐

- [`99_Tools/CLAUDE.md:31`](../../99_Tools/CLAUDE.md#L31) — PacketGenerator는 *`--no-manager` 없이 돌리면 컴파일 깨지는 manager 파일* 생성 가능
- Phase 01/03/05/06 작업 지시에 명시 필요: `dotnet run --project 99_Tools/PacketGenerator -- --no-manager` 사용 또는 generator 기본값 반전

### 4. Phase 04도 PDL 변경 가능성

- 현재 `S_Snapshot`은 **단일 entity**. multi-target broadcast 시 형태 변경 필요할 수 있음
- `join` / `initial roster` / `player leave` / `enemy` / `death` / `stage clear` 결국 새 패킷
- PDL 변경 의무 목록에 "Phase 04도 snapshot 형태 바꾸면 PDL 대상" 단서 추가

### 5. 48h 완주 = '현실적으로 빠듯'

- 총 24h 추정 = **서버 변경만** 보면 가능
- Unity 멀티 캐릭터 표시 + asset import + 2-client 리허설 포함 시 부족 확률 ↑
- 추천: Phase 07 asset 통합을 늦게 몰지 말고 **초반에 "asset import smoke / placeholder fallback 확정" 30분 선행 체크** 분리

### 6. `98_Shared/CLAUDE.md:19` 정정 필요

- 현재: "핸드셰이크 코드 미구현, **M2.5 Phase 09** 처리 예정"
- 실제: M3 Phase 01로 넘어옴 → "**M3 Phase 01** 처리 예정"으로 정정 필요 (Codex 향후 검토 시 혼동 차단)

---

## 권장 수정 3개

**A.** Phase 03/04에 *"client remote entity registry + local/remote snapshot routing + interpolation buffer"* 명시

**B.** PDL 작업 공통 주의에 *"PacketGenerator `--no-manager` 또는 기본값 fix 필요, bool/string/list 사용 금지"* 명시

**C.** Phase 08 전에 *"2-client headless 또는 수동 리허설 체크리스트: join, leave, move, attack, boss death, reconnect"* 추가

---

## 가장 큰 risk

**멀티 캐릭터 클라 상태 구조** + **전투/StageClear의 서버 권위 범위 과소추정**

---

## Claude 측 반영 (분해 표 v2)

- **Phase 01 신설** = 선행 smoke check (asset import smoke + PDL 도구 정합 `--no-manager` + `98_Shared/CLAUDE.md:19` 정정 묶음, 1h)
- **Phase 03·04 분리** = 서버 broadcast (3) / 클라 remote entity registry + interpolation buffer (4)
- **Phase 05·06 명시** = combat state 신설 + 강한 단순화 (적 AI 없음, 고정 위치, range+cooldown만)
- **Phase 04에 PDL 가능성 단서** 추가
- **Phase 09** (新) = 헤드리스/수동 리허설 체크리스트 (join/leave/move/attack/boss death/reconnect)

→ Phase 수: 8개 → **9개**
→ 총 예상: ~18h 작업 + 50% buffer = **~27h** (48h 안에 빠듯하지만 가능)
