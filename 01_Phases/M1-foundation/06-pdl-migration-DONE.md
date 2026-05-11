# Phase 06 — PacketGenerator(자체 PDL) 이주 + 하드코딩 버그 정정 완료 박제

**완료일**: 2026-05-10
**커밋**: `03994b0` (feat(tools): PacketGenerator 이주 — 4월 ServerDev → 99_Tools/, 하드코딩 버그 정정 + PDL.xml=Ping/Pong)
**소요 시간**: 약 1.5시간

> 본 Phase는 *생성기 자체 작동 검증*까지. 양쪽(서버 + 클라) 정합 + Phase 05 코드 교체는 Phase 07로 분리.

---

## 5단계 보고

### 🎯 무엇을 만들었나
4월 ServerDev 레포의 PacketGenerator 4파일을 `99_Tools/PacketGenerator/`로 이주하고, **2개 하드코딩 잠복 버그**(L178 `C_Chat` / L194 `chatLen`)를 정정. PDL.xml을 통째 교체해 `C_Ping`/`S_Pong` 두 패킷만 정의. 생성기 실행 → 3개 .cs 파일이 *컴파일 가능한 코드*로 출력됨을 검증.

### 🤔 왜 필요한가
Phase 05의 `PingPacket`/`PongPacket`은 *임시 BitConverter 코드*. ADR-002 v2가 약속한 *자체 PDL 단일 소스*가 아직 없는 상태. **Phase 06은 그 PDL 인프라를 깨우는 작업** — 다음에 새 패킷 추가 시 XML 한 줄 + 명령으로 자동화될 토대. 4월 본인이 짠 코드라 학습 일지로도 의미 있음 (자기 코드의 함정 발견).

### 🛠️ 어떻게 만들었나
- **이주 4파일**: Program.cs / PacketFormat.cs / PDL.xml / PacketGenerator.csproj. ICON.ico + bin/obj/Debug 잔재 제외.
- **두 하드코딩 정정**: ADR-002 v2가 *명시한 1개*(L178) + **grep으로 발견한 추가 1개**(L194). `chatLen` → `count`로 정정. 4월 chat 패킷 작성 시 흔적이 잔존했음.
- **Program.cs nullable 정합** (Phase 02 패턴): static string 4개 `= ""` 초기화 + `_r["name"]` `string?` 명시 + `return null` → `throw InvalidDataException`(호출자 nullable 부담 제거).
- **고려했지만 안 고른 대안**: ① `#if NET_LEGACY` 분기 통째 정리 → 본 Phase는 *작동 검증 우선*, BinaryPrimitives 정합과 함께 Phase 07로. ② `--no-wait` 인자로 Console.ReadKey 우회 → 본 Phase 외 작업.
- **신규 개념**: 코드 생성기 패턴(XML 단일 소스 → 다중 출력), `C_`/`S_` 접두사로 자동 dispatch 분리, "임시 우회 → 미루기" 패턴이 잠복 버그가 되는 메커니즘.

### 🧪 테스트 결과
- `dotnet build Dawnholder.slnx`: **6개 프로젝트 경고 0 / 오류 0**
- `dotnet run --project 99_Tools/PacketGenerator -- PDL.xml`: 3개 .cs 정상 생성
- eyeball 7개 체크 모두 통과 (PacketID enum / IPacket / C_Ping 멤버 / S_Pong 멤버 / 정정 #1 / 정정 #2 / `C_Chat` 잔재 0건)
- 생성된 GenPackets.cs 214줄 + ClientPacketManager.cs 79줄 + ServerPacketManager.cs 79줄

### ➡️ 다음 스텝
- **Phase 07: 생성 코드 양쪽 정합 + Phase 05 코드 교체 + 시연**
  - Y2 정합 결정 (Shared 통합 vs 양쪽 별도 생성)
  - BinaryPrimitives.*LittleEndian 정합 (PacketFormat.cs 템플릿 수정)
  - Phase 05 PingPacket.cs/PongPacket.cs 삭제 → 생성 코드 사용
  - Unity 시연 (Phase 05와 동일하게 RTT 로그)
- 알아두면 좋을 후속: `Console.ReadKey` 자동화 차단 — Phase 07에서 `--no-wait` 인자 분기 추가 검토.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Phase 06 범위 = 작동 검증까지** (Phase 단위 1~3h 권고). 양쪽 정합 + 코드 교체는 Phase 07로. 이 분리가 *각 단계 독립 검증* 가능.
- **하드코딩 버그 #2 발견** — ADR-002 v2가 *1개만* 명시했지만 grep으로 #2(`chatLen`) 추가 발견. 정정 시점에 *전수 검사*가 안전망.
- **return null → throw** — `Tuple<string,string,string>` 반환 메서드의 `return null`은 호출자 nullable 부담. *잘못된 PDL은 즉시 종료*가 의미적으로 정확 → throw로 교체.
- **`#if NET_LEGACY` 분기 보존** — 데드 코드지만 *동작에 영향 X*. Phase 06 변경 폭 줄이기 위해 그대로 둠. Phase 07 BinaryPrimitives 정합 시 함께 정리.
- **csproj 정합 시 ServerDev 잔재 제거** — `AppendTargetFrameworkToOutputPath` / `PackageOutputPath` / `BaseOutputPath` 같은 *옛 출력 경로 커스터마이즈*는 제거 (표준 bin/obj 사용). `ApplicationIcon` / `Content ICON.ico` 도 제거 (포트폴리오 단순화).
- **PDL.xml = Ping/Pong만** — 4월 게임용 패킷(S_BroadcastEnterGame 등)은 *옛 게임 컨셉*. 현재 Phase 단계에선 Ping/Pong만 의미 있음. 미래 패킷은 점진적 추가.
- **출력 파일 위치** — Program.cs L46-48이 `File.WriteAllText("GenPackets.cs", ...)`로 *현재 작업 디렉토리에 출력*. Phase 07에서 양쪽 wire-up 시 `99_Tools/PacketGenerator/output/` 등 명시적 경로 검토.
- **출력 파일을 commit에 포함** — 본 Phase의 *증거*. Phase 07에서 양쪽 정합 시 *생성 산출물 위치 결정*에 따라 .gitignore 또는 별도 폴더로 정리할 수도.
- **Console.ReadKey stdin redirect 함정** — `echo "" | dotnet run` 시 `Cannot read keys` exception. 출력 파일은 *exception 직전*에 이미 생성되었으므로 검증 영향 0. Phase 07에서 `--no-wait` 분기 추가 시 해결.

---

## 막혔던 지점

소소한 함정:
- **Edit 도구의 Read 선결조건** — 99_Tools/ 경로는 *복사로 만든 파일*이라 Edit 도구가 "File has not been read yet" 차단. Read 한 번 거치면 OK.
- **stdin redirect → ReadKey exception** — `dotnet run`을 비대화형으로 호출 시 `Console.ReadKey(true)`가 깨짐. 출력 파일 생성은 그 *이전*이라 검증 영향 X. Phase 07 메모.
- **빌드 경고 10개 추가 발견** — Phase 06 청사진엔 nullable 정정 *없었음*. 빌드 시점에 발견 → 즉시 정정. *청사진은 청사진, 실제 작업은 발견 기반 추가 가능*.

---

## 학습 일지 후보 키워드

`/journal:concept <키워드>` 로 펼칠 만한 것들:

- **code-generator-pattern** — XML 단일 소스 → C# 다중 출력. T4 / Roslyn Source Generator / 직접 string format 비교. 게임 / DB ORM / RPC 분야의 활용 사례.
- **dormant-bug-and-redirection** — 4월 코드의 `C_Chat` / `chatLen` 하드코딩이 *NET_LEGACY 분기에 박혀 미발현*했던 메커니즘. "지금 안 깨지면 OK"의 함정. 잠복 버그가 *나중에 표면화될 조건*.
- **xml-reader-streaming-parse** — `XmlReader`의 forward-only / pull-based 파싱 vs `XmlDocument` DOM 기반. 메모리 / 성능 trade-off. 큰 XML 시 streaming의 의미.
- **string-concat-vs-stringbuilder** — Program.cs가 string `+=`로 누적. 작은 PDL엔 OK지만 큰 PDL 시 O(n²) 비용. StringBuilder가 표준. 본 도구의 적정 입력 크기.
- **prefix-based-dispatch** — `C_Xxx` / `S_Xxx` 접두사로 *클라/서버 dispatch table 자동 분리*. 명명 강제 vs 자유의 trade-off. attribute 기반 / interface 기반 대안.

---

## 메모 (다음 세션을 위한)

- **Phase 07 진입 전 결정 필요**: Y2 정합 방식. ① Shared에 SendBufferHelper 두기(코드 중복 0) ② 생성기가 양쪽에 별도 GenPackets 출력 ③ 다른 방식. 이 결정이 PacketFormat.cs 템플릿 수정 방향 결정.
- **Phase 07 동시 작업**: BinaryPrimitives 정합 + Phase 05 PingPacket/PongPacket 삭제 + Unity 시연 (Phase 05와 동일 RTT 로그).
- **PDL 활성화 후 새 패킷 추가 흐름**: Phase 07 끝나면 `/work:new-packet <C2S|S2C> <name>` 슬래시 커맨드로 자동화 가능 (이미 정의됨).
- **출력 파일 위치 결정** — 현재는 `dotnet run` 호출 위치에 출력. Phase 07에서 *양쪽 wire-up* 시 명시적 출력 폴더(`99_Tools/PacketGenerator/output/`) 또는 *바로 양쪽 위치로 분기 출력*(서버용 한 벌 + 클라용 한 벌) 결정.
- **PRD.md 응축**(229줄) 미해결. Phase 07 진입 전 또는 후에 처리.
- 이번 Phase는 *짧고 명확*했음. 큰 시연 없이 *생성기 작동 + 정정*만. M1 Foundation 완료 직후 인프라 정비 패턴.
