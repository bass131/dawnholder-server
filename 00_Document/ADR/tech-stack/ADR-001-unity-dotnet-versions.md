### ADR-001: Unity 6.4 LTS + .NET 10 LTS + .NET Standard 2.1 멀티타겟
**날짜**: (Harness 셋업일) — **2026-05-06 .NET 버전 갱신**, **2026-05-09 Unity 버전 갱신**, **2026-10-07 Unity 6.6 갱신**
**상태**: 채택됨 (대체: v1 ".NET 8 + Unity 2022 LTS", v2 ".NET 10 + Unity 2022 LTS"). 2026-10-07부터 엔진은 `6000.6.4f1`이다(아래 「2026-10-07 갱신」). 제목과 본문의 「Unity 6.4 LTS」는 당시 표기를 그대로 둔 것이다. 6.4는 LTS가 아니라 Supported Update였으므로 이유 (c)는 사실과 다르다.
**결정**: Unity 6.4 LTS 클라이언트 + .NET 10 LTS 권위 서버. `98_Shared/`는 .NET Standard 2.1로 빌드해 Unity가 인식 가능하게.
**이유**: C# 단일 언어 통일. .NET 10 LTS는 2028년까지 지원이라 11월 본 마감 + 시연 후 시점도 커버 (.NET 8은 2026-11-10 만료, .NET 9는 2026-05-12 만료로 부적합). .NET Standard 2.1 = Unity의 Mono/IL2CPP가 인식하는 공통 API 사양 → DLL 공유 가능. Unity 6.4 LTS 선택 이유: (a) **Unity AI MCP Server 활용 가능** — Claude Code가 Unity 에디터를 직접 조회/조작, 본 프로젝트의 Claude 중심 워크플로우와 직접 시너지. (b) Unity 6의 새 기능(GPU Resident Drawer, 향상된 2D 렌더링). (c) LTS 라이프사이클 더 김 — 2027~2028년까지.
**트레이드오프**: 웹/모바일/콘솔은 추가 작업. 기존 ServerDev 코드(.NET 9)를 클론할 때 csproj TargetFramework 마이그레이션 필요 (대부분 한 줄). .NET 10이 신규 LTS라 일부 NuGet 라이브러리는 호환성 케이스별 확인. Unity 6는 2022 대비 일부 deprecated API/내부 매개변수 변경 가능 — 학습용 ServerDev 코드는 서버/네트워크 중심이라 영향 적지만, Unity 코드 작성 시 옛 튜토리얼(2022 LTS 기준)을 그대로 옮기면 안 됨.

**2026-10-07 갱신**: 클라이언트 엔진을 `6000.6.4f1`(revision `12bfff696524`)로, AI Assistant를 `2.20.0-pre.2`로 올렸다. .NET 10과 .NET Standard 2.1 결정은 그대로다. 근거와 측정은 [전환 goal](../../../01_Phases/goals/2026-10-07-unity-engine-upgrade/goal.md)에 있다.
- 이유: 6.4는 지원이 끝나 더 고쳐지지 않는다. 6.6은 승인 시점에 지원받던 버전이고, 6.5·6.6 변경을 한 번에 받는다. AI Assistant는 2.16.0-pre.1부터 기본 정책에서 MCP 연결 상한을 적용하지 않는다. 이전 2.7.0-pre.3은 6.6에서 컴파일되지 않는다.
- 검토한 대안: 6.4 마지막 패치 `6000.4.12f1`은 변경이 가장 작지만 지원이 끝났다. 6.7 Beta와 7000 Alpha는 마감 전 엔진으로 쓰지 않는다. 6.6도 LTS가 아니다. 다음 LTS인 6.7(연말 예정)로 옮길지는 11월 마감 뒤에 판단한다.
- 비용: 6.6에서 컴파일되지 않는 서드파티 Lucid Editor 에디터 코드와 그것을 상속하는 Cainos 에디터 스크립트(추적 파일 123개)를 지웠다. 테스트 두 곳도 고쳤다(`GetEntityId`, Input System 1.20.0의 설정 파괴). 6.6으로 연 Library는 6.4로 되돌리지 못한다고 본다. 그래서 worktree마다 Library를 다시 만들고 MCP 승인을 다시 한다.
