# Phase 01: 솔루션 부트스트랩 + DLL 빌드 파이프라인 — 학습 일지

> **작성일**: 2026-05-09
> **Phase 파일**: `phases/M1-foundation/01-solution-bootstrap.md`
> **소요 시간**: 예상 1.5~2h / 실제 ~2h
> **상태**: 완료 (단, "구현/도구 차원" + "Embedded PDB 깊이"는 추가 학습 큐로 넘김)

---

## 🎯 한 줄 요약

shared 라이브러리를 .NET Standard 2.1로 빌드해서 .NET 10 서버와 Unity 클라이언트가 같은 코드를 ReadOnly로 공유할 수 있게 했고, 빌드 시 자동으로 Unity Plugins/까지 흘러가는 파이프라인을 검증했다.

---

## 📦 결과물

- `.NET 10` 솔루션 + `global.json`(SDK 10.0.203 고정)
- `shared/Shared.csproj` — `.NET Standard 2.1` 라이브러리, embedded PDB(`<DebugType>embedded</DebugType>` + `<EmbedAllSources>true</EmbedAllSources>`), post-build target으로 `.dll`을 `client/Assets/Plugins/Shared/`로 자동 복사
- `shared/GameData/Constants.cs` — 첫 공유 상수 (`ServerTickRate`, `TickIntervalMs`)
- `server/GameServer` (.NET 10 콘솔 호스트), `server/GameServer.Tests` (xunit, .NET 10)
- `client/` 안에 Unity 6.4 LTS Universal 2D 빈 프로젝트
- `.gitignore` 깊이 패턴 정정 (`Plugins/**/*.dll`)

검증된 것: `dotnet build` 통과, `dotnet run` 출력 정상, Unity 에디터 IntelliSense + F12 → 원본 .cs(한국어 주석 포함) ReadOnly 표시.

---

## 🧠 새로 배운 것

### 개념 차원
- **`.NET Standard 2.1`과 `.NET Framework`의 차이 — .NET 생태계는 단일 런타임이 아니다.** 셋(Framework, Standard, .NET Core 계열)이 각각 다른 본질을 가지고 있다는 분리. 특히 "Standard"는 실제 런타임이 아니라 "이 API는 모든 .NET 변종이 공통으로 지원한다는 약속"이라는 게 인상 깊었다.
- **C# 컴파일러는 단일(Roslyn)이지만 타겟 프레임워크에 따라 기본 언어 버전이 다르게 잡힌다.** `.NET Standard 2.1` → C# 8 기본, `.NET 10` → C# 13 기본. CS8400 함정에서 학습. `<LangVersion>latest</LangVersion>`로 덮어쓸 수 있음.
- **Embedded PDB의 존재** — 디버그 정보를 .dll 안에 통째 압축. 단 메커니즘과 본 프로젝트에서의 가치는 표면만 이해. /journal-concept로 따로 파보기.

### 구현 차원
- (이번 Phase는 코드량이 적어서 깊은 구현 학습은 적었음. Phase 02 ServerCore 이주 시 본격적으로 들어옴.)

### 도구 차원
- (위와 같음. 이번 Phase는 csproj XML 다루는 정도. MSBuild Target 시스템 자체는 표면만.)

---

## 🤔 결정 포인트

- **결정**: TDD 강제 영역(공식·직렬화·상태머신) + Hook 보강을 Phase 02 진입 직전으로 미룸
- **고려한 대안**: Phase 01 시작 전에 헌법 6번째 원칙 + Hook 3개를 다 박는 안 (엄격하게 가는 안)
- **선택 이유**: Hook은 코드가 있어야 진짜 모양 잡힘. 코드 없이 추측 기반 가드를 만들면 YAGNI + 다시 손볼 가능성 큼.
- **트레이드오프**: 미룬 기간 동안 환각 스킵 위험. Phase 01이 토대 작업이라 환각 발휘할 영역이 좁았던 게 위험을 줄임.
- **남은 의문**: "처음부터 엄격하게 잡았어야 했나?" 결정 후에도 의심 남음. Phase 02 시작 시점에 답이 명확해질 것.
- **ADR 격상?**: 아직. Phase 02 진입 시 결정 다시 검토.

(보조 결정들도 있었음 — Unity 2022 LTS → 6.4 LTS는 ADR-001 v3로 격상됨, "(A) 작은 재정의 vs (B) 큰 한방"은 (A) 채택으로 Phase 분해 결정.)

---

## 🐛 막혔던 지점

30분 이상 막힌 것 없음. Phase 01이 토대 작업이라 실코드량이 적었던 게 이유. 작은 함정 3개(CS8400, .gitignore 깊이 패턴, Unity Hub의 client/ 점유)는 5분 이내에 해결됨.

→ 별도 트러블슈팅 일지 작성 불필요.

→ 진짜 막힘은 Phase 02(ServerCore 이주)부터 시작될 가능성 큼. 4월 .NET 9 코드를 .NET Standard 2.1로 마이그레이션할 때 "이 API는 Standard에 없네" 같은 함정이 예상됨.

---

## 💡 다시 한다면

당장 다르게 할 것 없음. Phase 01이 토대 작업이라 단순했고, 결정도 정보 가용 시점에 적절히 내려졌음. 특히 Unity 2022 → 6.4 LTS 변경은 Unity AI 2.0 업데이트가 직전 세션 후에 들어왔기 때문에 "더 일찍 결정하지 못한 것"이 본인 페이스 문제가 아니라 정보 가용성 문제.

**메타 교훈** (Phase 02부터 적용):
- 외부 정보(Unity AI 2.0 같은)가 들어오면 작업 흐름에 끼워넣는 게 정상. "미리 결정 못 한 것"이 아님.
- 단 Phase 단위 시작 시점에 헌법/ADR/Phase 파일 통독을 빼먹지 않기 (이번에 빼먹었다면 outdated 발견이 늦었을 것).

---

## ❓ 아직 모르는 것 / 다음에 배울 것

```
🟢 가장 가치 큰 큐 (시간 나면 먼저)
- Embedded PDB의 실제 메커니즘 + 본 프로젝트에서의 가치 (현재 표면만)
- .NET Standard 2.1 자체 — "API 호환 사양"이라는 추상 개념의 메커니즘
  → 두 개를 한 번에 묶어 /journal-concept으로 정리 추천

🟡 자주 마주칠 큐
- MSBuild Target / Item / Property 시스템
  (<Target>, <ItemGroup>, <Copy> 등을 그냥 박았는데, csproj 만질 때마다 마주침)
- Unity의 Mono 런타임이 .NET Standard 2.1을 인식하는 메커니즘
  (Phase 04 이후 클라 작업 시 깊어질 영역)

🟠 작은 호기심 큐
- .slnx (XML 기반 솔루션) vs .sln 차이
```

---

## 🎤 면접 시뮬레이션

**Q-A: "왜 `shared`를 `.NET Standard 2.1`로 빌드해요? `.NET 10`이 더 최신이잖아요?"**
A: Unity의 Mono 런타임은 .NET Standard 2.1까지 인식한다. 서버는 .NET 10 최신을 쓰지만, 양쪽이 공통으로 인식 가능한 .NET Standard 2.1을 shared의 타겟으로 잡으면 한 어셈블리로 양쪽이 같은 코드를 공유할 수 있다.

**Q-B: "PDB를 임베드하면 뭐가 좋아요? 그냥 별도 .pdb 파일로 두는 거랑 차이가 뭔가요?"**
A: 아직 답 못 함. /journal-concept "Embedded PDB"로 따로 학습 예정. 현재 표면 인지: .dll에 디버그 정보를 같이 압축해서 .pdb 파일이 별도로 안 생기게 한 것.

**Q-C: "shared.dll을 클라이언트가 수정하지 못하게 막는 게 왜 중요한가요?"**
A: shared는 클라/서버가 같은 패킷 정의와 공식을 공유하는 코드라, 클라가 임의 수정하면 (1) 양쪽이 다른 바이트를 주고받아 통신 자체가 깨지고 (runtime desync), (2) 데미지 수식 같은 로직이 클라 임의대로 바뀌어 핵 취약점이 된다. 그래서 빌드 시스템(DLL + Embedded PDB)으로 클라가 shared를 수정할 경로 자체를 차단했다.

---

## 🔗 관련 링크

- Phase 파일: `phases/M1-foundation/01-solution-bootstrap.md`
- 관련 ADR:
  - ADR-001 v3 (Unity 6.4 LTS + .NET 10 LTS + .NET Standard 2.1)
  - ADR-010 (DLL + Embedded PDB)
  - ADR-011 (시나리오 B — ServerDev 부분 채택)
- 노션 세션 로그: https://www.notion.so/35b76ceccb7881b1949ae8a5a48cf493

---

## 작성 메모

- ✅ 한 줄 요약 작성됨 (3차 다듬음)
- ✅ 새로 배운 것 (개념 차원) 작성됨
- ⚠️ 새로 배운 것 (구현/도구 차원) 비어있음 — Phase 01 코드량이 적어 자연스러운 결과
- ✅ 결정 포인트 작성됨 (TDD/Hook 미루기 결정 — 남은 의문 포함)
- ✅ 막혔던 지점 — 정직히 "30분 이상 막힘 없음"으로 박음
- ✅ 다시 한다면 — 메타 교훈 형태로 박음
- ✅ 아직 모르는 것 — 4개 큐 우선순위 매김
- ✅ 면접 시뮬레이션 — Q-B는 정직히 "아직 답 못 함" + 학습 큐로 표시
