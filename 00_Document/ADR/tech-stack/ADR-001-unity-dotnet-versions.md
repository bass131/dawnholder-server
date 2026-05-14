### ADR-001: Unity 6.4 LTS + .NET 10 LTS + .NET Standard 2.1 멀티타겟
**날짜**: (Harness 셋업일) — **2026-05-06 .NET 버전 갱신**, **2026-05-09 Unity 버전 갱신**
**상태**: 채택됨 (대체: v1 ".NET 8 + Unity 2022 LTS", v2 ".NET 10 + Unity 2022 LTS")
**결정**: Unity 6.4 LTS 클라이언트 + .NET 10 LTS 권위 서버. `98_Shared/`는 .NET Standard 2.1로 빌드해 Unity가 인식 가능하게.
**이유**: C# 단일 언어 통일. .NET 10 LTS는 2028년까지 지원이라 11월 본 마감 + 시연 후 시점도 커버 (.NET 8은 2026-11-10 만료, .NET 9는 2026-05-12 만료로 부적합). .NET Standard 2.1 = Unity의 Mono/IL2CPP가 인식하는 공통 API 사양 → DLL 공유 가능. Unity 6.4 LTS 선택 이유: (a) **Unity AI MCP Server 활용 가능** — Claude Code가 Unity 에디터를 직접 조회/조작, 본 프로젝트의 Claude 중심 워크플로우와 직접 시너지. (b) Unity 6의 새 기능(GPU Resident Drawer, 향상된 2D 렌더링). (c) LTS 라이프사이클 더 김 — 2027~2028년까지.
**트레이드오프**: 웹/모바일/콘솔은 추가 작업. 기존 ServerDev 코드(.NET 9)를 클론할 때 csproj TargetFramework 마이그레이션 필요 (대부분 한 줄). .NET 10이 신규 LTS라 일부 NuGet 라이브러리는 호환성 케이스별 확인. Unity 6는 2022 대비 일부 deprecated API/내부 매개변수 변경 가능 — 학습용 ServerDev 코드는 서버/네트워크 중심이라 영향 적지만, Unity 코드 작성 시 옛 튜토리얼(2022 LTS 기준)을 그대로 옮기면 안 됨.
