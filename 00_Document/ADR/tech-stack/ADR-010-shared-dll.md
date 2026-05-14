### ADR-010: Shared 코드 공유 방식 = DLL + Embedded PDB
**날짜**: 2026-05-06 — **2026-05-11 폴더 경로 정합**
**상태**: 채택됨
**결정**: `98_Shared/`는 .NET Standard 2.1 라이브러리로 빌드. 빌드 산출물(`.dll` + `.pdb`)을 `03_Client/Assets/Plugins/`에 자동 복사해서 Unity가 참조. PDB는 `EmbedAllSources=true`로 원본 `.cs` 통째로 임베드 → IDE가 F12 시 원본 코드(주석 포함) 그대로 표시.
**이유**: 헌법 #4 ("동일 어셈블리 참조, 복사-붙여넣기 금지")의 **물리적 강제**. Unity 측에선 임베드된 소스가 ReadOnly로 떠서 수정 자체가 불가능. F12 + step into는 정상 동작 → C++의 헤더+구현 분리 모델보다 풍부 (모든 함수 바디 보임). 비개발자 팀원(유현)이 클라 작업 중 실수로 shared 코드 건드릴 가능성 0%.
**트레이드오프**: shared 수정 시 "빌드 → 복사 → Unity 새로고침" 사이클 1~2초 추가 (`dotnet watch` 자동화 가능). symlink/Unity Local Package 대비 빌드 단계 1개 더. `.dll`/`.pdb`는 빌드 산출물이라 `.gitignore` (커밋 금지).
