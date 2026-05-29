---
title: 게임 서버 프로그래밍 교과서 — 장별 색인
category: refs/game-server-programming
---

# 게임 서버 프로그래밍 교과서 — 장별 색인

## 사용법

코드 수정 전 "이 작업이 어느 장과 관련 있나?"를 이 색인에서 먼저 확인한다.
각 파일 안의 **"언제 참조하나"** 섹션이 구체적 트리거를 담고 있으므로, 장 파일을 열면 그 섹션을 먼저 읽는다.

---

## 장별 요약

| # | 파일 | 한 줄 요약 | 언제 참조 |
|---|------|-----------|----------|
| 01 | [01-multithreading.md](01-multithreading.md) | 스레드·뮤텍스·교착상태·스레드풀·원자조작 — 서버 동시성 설계의 토대 | `lock` / `Interlocked` / `JobQueue` / `GameMap` Actor 스레드 경계를 건드릴 때 |
| 02 | [02-computer-network.md](02-computer-network.md) | OSI 모델·TCP/UDP 특성·스트림 vs 메시지·NAT — 소켓 계층 아래의 네트워크 기초 | RecvBuffer 스트림 경계 문제, UDP vs TCP 선택 근거, PDL 포맷 트레이드오프 논의 시 |
| 03 | [03-socket-programming.md](03-socket-programming.md) | 블로킹→논블록→Overlapped I/O→IOCP 진화 — `02_Server/Network/` IOCP 구현의 맥락 | `02_Server/Network/Session.cs` / `RecvBuffer.cs` / `SendBuffer.cs` 읽거나 수정할 때, "왜 이 구조인가" 질문이 나올 때 |
| 04 | [04-server-and-client.md](04-server-and-client.md) | 서버 권위 원칙·클라-서버 상호작용 4가지·서버 품질 4축(안정성/확장성/성능/관리) | "서버가 뭘 책임지나" 결정 시, 확장성/성능 목표 설정 시, 안정성 방어 코드 작성 전 |
| 05 | [05-game-networking.md](05-game-networking.md) | Prediction/Reconcile·Dead Reckoning·AOI·Lock-Step·치트 방어 — 레이턴시 마스킹 이론 | Prediction/Reconcile/lag compensation 설계·버그, Rate-limit·패킷 보안, AOI 도입 검토 시 |
| 06 | [06-proudnet.md](06-proudnet.md) | 상용 네트워크 엔진(ProudNet)이 소켓을 추상화하는 방식 — 자체 `02_Server/Network/`+PDL 설계의 비교 기준 | `02_Server/Network/`·`02_Server/GameServer/` 계층 분리 리팩터링, PDL 코드 생성기 확장, 스레드 모델 선택 근거 필요 시 |
| 07 | [07-database.md](07-database.md) | SQL Server·CRUD·인덱스·트랜잭션·비동기 DB 호출 — M5 영속화 마일스톤의 직접 근거 | `PlayerEntity` / `CharacterEntity` / `ItemEntity` DB 스키마 설계, 트랜잭션(아이템 거래) 구현 시 |
| 08 | [08-nosql.md](08-nosql.md) | RDBMS vs NoSQL 트레이드오프·MongoDB CRUD·샤딩·고가용성 — 로그/통계 저장의 대안 | DB 스키마가 자주 바뀔 때, write-heavy 로그·행동 통계 저장 설계, DB 수평 확장 논의 시 |
| 09 | [09-distributed-server.md](09-distributed-server.md) | 수평·기능적 분산 설계·응집도·비동기 액터 모델·고가용성 — "언제 분산하고 언제 하지 말지" 판단 기준 | 동시접속 증가로 단일 서버 병목 시, 맵 서버 레지스트리·핸드오프 확장, HA 전략 논의 시 |
| 10 | [10-distributed-cases.md](10-distributed-cases.md) | 인증·DB·매치메이킹·AI·로깅 컴포넌트별 분산 패턴 + 장르별 아키텍처 사례 | 특정 컴포넌트 분리를 고려할 때, MMORPG/FPS/RTS 장르별 서버 구조 레퍼런스 필요 시, 로그 파이프라인 설계 시 |
