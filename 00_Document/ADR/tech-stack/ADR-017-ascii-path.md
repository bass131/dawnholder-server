### ADR-017: 프로젝트 폴더 ASCII 경로 이동 (한글 경로 영구 해결)
**날짜**: 2026-05-11
**상태**: 채택됨
**결정**: 프로젝트 루트를 한글 포함 경로에서 ASCII 전용 경로 `C:\Dev\ClaudeDev`로 이동. 본 결정의 범위는 **한글 경로 호환성 해결만**이며, Burst Enable 시 발생하는 WDAC(Windows Defender Application Control) 미서명 DLL 차단 (error code 4551)은 **별도 사건**으로 본 ADR 범위 밖. 본 프로젝트는 Burst 비활성 상태로 진행 — Burst가 진짜 필요한 복잡도 시스템 도입 시 별도 ADR로 WDAC 정책 정리.
**이유**: Phase 03·04에서 한글 절대경로가 Burst JIT 컴파일러·PacketGenerator `dotnet run` 등 .NET/Unity 도구 체인에서 hang·경로 파싱 실패를 반복 유발. 우회책(publish 산출물 직접 실행, Burst Disable)으로 진행은 했으나 매 Phase 도구 신뢰 비용 누적. ASCII 경로 이동 후 검증(`dotnet build` 0 error / `dotnet test` 25/25 / `dotnet run` PacketGenerator 직접 실행 / Burst Enable 시 hang 없이 즉시 4551 에러로 떨어짐 = 컴파일러 경로 파싱은 해결됨)으로 도구 호환성 근본 회복.
**트레이드오프**: 절대경로 박힌 옛 문서·일지·노션 페이지 참조 깨짐(`-DONE.md` 등에 옛 경로 잔존 가능 — 발견 시 정정). 폴더 이동 자체가 1회성 수작업(PowerShell `Move-Item`) — 다른 머신에 클론할 땐 무관. Burst·WDAC 사건은 미해결로 남아 향후 복잡도 시스템 도입 시 재발 가능.
