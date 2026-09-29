# M2b — 기본 스탯과 현재 상태·캡처·이동 값

상태: 예정·미착수. [M2a](../2026-09-30-refactor-party-quest/goal.md) 통합 후 최신 main 별도 브랜치에서 시작한다. [M2](../2026-09-29-refactor-domain-state/goal.md)의 두 번째 목표다.

PlayerStats의 기본 정의를 불변으로 만들고 현재 HP는 PlayerEntity가 소유한다. Snapshot은 캡처 이후 원본 변경에 영향받지 않도록 하고 이동 전달값은 필요한 필드를 명시한다. Shared 소비자·DLL·Unity 검증을 같은 목표에서 다룬다. 기존 맵 이동의 ID/HP/Stats 보존 및 action cooldown/FSM/input/history/무적 초기화 정책은 바꾸지 않는다. 현재 MaxHp는 이동 시 Stats.MaxHp로 구성되는 기존 정책을 보존한다.

착수 때 `.backups/reviews/2026-09-30-m2-design-preflight.md`의 M2b 제안과 직접 사용처만 확인해 선택 API/정확 파일 소유/검증을 이 goal에 확정한 뒤 구현한다. 새 성장·장비 시스템과 DB 저장/복원·인증·schema 실행은 제외한다. 현재 변경·검증·PR 결과는 없다.
