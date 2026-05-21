---
description: 새 몬스터 추가 (데이터만, 엔진 코드 변경 없음). 옛 content SubAgent 흡수.
argument-hint: <name> <level> <map>
---

"몬스터 추가" 워크플로우. 사용자 요청:
**$ARGUMENTS**

---

### 등급 판정

새 몬스터 = **보통** 등급 (데이터 값만, 1 도메인 × 2~3 파일 ≤50줄 가역적):

- 데이터 값 추가 = `qa` SubAgent 영역 (콘텐츠 데이터 값 책임)
- 스키마 자체 변경 = `shared` SubAgent (98_Shared/GameData/Tables 스키마)
- 새 AI type 필요 = **복잡** 상향 (server SubAgent 영역으로 라우팅)

---

### 작업 흐름

#### Step 1. `qa` SubAgent 위임 (데이터 값)

[`../../agents/qa.md`](../../agents/qa.md) 호출.

브리프:
1. `98_Shared/GameData/Tables/monsters.json` 읽어 기존 스키마 확인 후 다음 빈 monster id 선택
2. 다음 필드로 entry 추가:
   - id (다음 빈 ID)
   - name (영문 식별자)
   - level
   - hp / attack / defense
   - move speed / attack speed
   - sprite ref (Resources/Content/Monsters/<id>.png)
   - sound ref
   - drop table id (옛 drop_tables.json 참조)
   - AI type (기존 enum 중 하나)
3. **요청된 동작에 존재하지 않는 AI type 필요하면 STOP** — `server` SubAgent에게 먼저 라우팅 (등급 *복잡* 상향)
4. `02_Server/GameServer/Maps/Definitions/<map>.json` 해당 맵 정의 파일에 spawn entry 추가
5. 서버 컨텐츠 로더 schema 체크 통과 확인:
   ```bash
   dotnet test 02_Server/GameServer.Tests --filter Category=ContentSchema
   ```

#### Step 2. (조건부) `unity-bridge` SubAgent 위임

[`../../agents/unity-bridge.md`](../../agents/unity-bridge.md) 호출 — 클라이언트 sprite 자산이 없으면.

브리프:
- `03_Client/Assets/Resources/Content/Monsters/<id>.png` 존재 확인
- 없으면 사용자에게 ComfyUI 자산 제공 요청 (인규 영역) 또는 placeholder sprite 박음

---

### 자동 발동 Hook

- **`risk-detector.sh`**: `monsters.json` 변경은 일반 데이터 = 깃발 X (가역적, 단순 데이터)
- **`tdd-guard.sh`**: `GameData/` 변경은 TDD 영역 포함 → 스키마 라운드트립 테스트 누락 *경고만*

---

### Reviewer 자동 호출 조건부

- 데이터 값만 변경 = `98_Shared/` 포함이지만 *스키마 변경 X* → 조건부 호출 (등급 ≥ 보통 충족)
- 새 AI type 동반 (server SubAgent 라우팅) = 복잡 상향 → 무조건 호출

---

### 절대 금지

- **엔진 코드 수정 X** — AI 알고리즘 / 데미지 공식 / 스킬 로직 변경 필요하면 `server` SubAgent로 라우팅 (등급 복잡 상향)
- **클라 사이드 데미지 계산 X** — 헌법 §1 위반. 클라이언트는 렌더링만

---

### 사용자 보고

```
─────────────────────────────────────────
👾 새 몬스터 추가 완료
─────────────────────────────────────────

몬스터: <name> (id=<NN>, level=<L>, map=<map>)
등급: 보통

변경 파일:
  - 98_Shared/GameData/Tables/monsters.json (+1 entry)
  - 02_Server/GameServer/Maps/Definitions/<map>.json (+1 spawn)
  - (조건부) 03_Client/Assets/Resources/Content/Monsters/<id>.png

검증:
  - ContentSchema 테스트: PASS
  - sprite 자산: <있음/placeholder>

➡️ 다음:
  - 인게임 spawn 확인 (헤드리스 봇 시나리오)
  - 스프라이트 정식 자산 인규에게 요청 (placeholder 박혔으면)
```

---

### 옛 슬래시와 차이

- **옛 `/work:new-monster`**: `content` SubAgent 단일 위임 (데이터 + 일부 게임플레이 영향)
- **새 `/work:new-monster`**: `qa`(데이터 값) + `unity-bridge`(자산, 조건부) 분담. `content` SubAgent 자체는 *삭제* — server/qa/unity-bridge로 분산 흡수 (Phase 02 결정)
