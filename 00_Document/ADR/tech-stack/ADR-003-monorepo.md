### ADR-003: 모노레포
**날짜**: (Harness 셋업일)
**상태**: 채택됨 (단, MES는 별도 레포 — ADR-011 참고)
**결정**: client/, server/, shared/를 한 git 레포에 둠.
**이유**: shared/Protocol을 패키지로 분리하면 1인 개발에서 오버헤드만 큼.
한 PR로 양쪽 변경이 일관됨. AI가 컨텍스트 잡기 쉬움.
**트레이드오프**: 레포가 커지면 clone 시간 증가. CI에서 client/server를
선택적으로 빌드하는 설정이 필요해질 수 있음.
