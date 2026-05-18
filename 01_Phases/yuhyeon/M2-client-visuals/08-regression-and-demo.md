# Phase 08: Regression + 데모 영상 + M2 학습 일지

> **2026-05-18 스킵 결정** — 회귀는 phase별 자연 검증으로 *이미 통과* (M1 6 + M2 추가 4 시나리오 정유현 Phase 01~06 누적). 별도 데모 영상은 M3 데모 영상에 흡수. M2 학습 일지는 M3 마감 시 통합 회고로.
>
> **상태**: skipped (2026-05-18)
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1시간
> **담당 에이전트**: client

---

## 🎯 목표

M1 회귀(6 시나리오) + M2 추가 시나리오(4 시나리오) 한 흐름 통과 → ShareX로 60~90초 데모 영상 녹화 → `/journal:phase` 인터뷰로 M2 학습 일지 작성 → `-DONE.md` 박제 → M2-client-visuals 마일스톤 통째 마감.

**끝나면 데모 가능한 것**: 면담/캡스톤 발표 자료 한 편 (영상 + 학습 일지 + Phase 박제 8개).

---

## ⏪ 사전 조건

- [ ] Phase 01~07 모두 완료 (-DONE.md 페어 7개 박힘)
- [ ] ShareX 또는 OBS 설치 (M1 Phase 06에서 이미 셋업)

---

## 📝 작업 내용

- [ ] **M1 회귀 6 시나리오 통과 확인** (이전 마일스톤 안 깨짐):
  - a. MainMenu 진입 (페이드 인 정상)
  - b. 시작 클릭 → 페이드 → Gameplay → HUD 한글 표시
  - c. ESC → 일시정지 메뉴 → 캐릭터 + 애니 멈춤 (timeScale=0)
  - d. 재개 → 메뉴 닫힘 → 캐릭터/애니 복구
  - e. ESC → 메인 메뉴 → 페이드 → MainMenu 복귀
  - f. 종료 → 에디터 정지
- [ ] **M2 추가 시나리오 통과 확인**:
  - g. Gameplay에서 A/D 키로 캐릭터 좌우 이동 (Run 애니 + flipX 정상)
  - h. 이동 멈춤 → Idle 애니 즉시 복귀
  - i. 모든 메뉴/HUD 텍스트 한글 표시 (tofu 0개)
  - j. 배경 sprite 정상 표시 (캐릭터 절대 배경 뒤로 안 숨음)
- [ ] **데모 영상 녹화** (ShareX 또는 OBS):
  - 해상도 1280×720 권장
  - 길이 60~90초 (M1 30초 + M2 추가 분 30~60초)
  - 흐름: MainMenu → 시작 → 캐릭터 이동(좌/우) → ESC → 일시정지 → 재개 → 메인 메뉴 → 종료
  - 저장: `00_Document/learning-journal/yuhyeon/M2-client-visuals/demo.mp4` (또는 .gif)
- [ ] **M2 학습 일지 작성**: `/journal:phase` 호출 → Claude 인터뷰 → 본인이 답 채우기
  - 출력: `00_Document/learning-journal/yuhyeon/M2-client-visuals/M2-recap.md`
- [ ] **`-DONE.md` 박제** (Claude): `08-regression-and-demo-DONE.md` (frontmatter 필수)
- [ ] **5단계 보고 + M2 마감 권유** (Claude)

---

## ✅ 완료 조건

- [ ] M1 회귀 6 + M2 추가 4 = **10 시나리오 모두 한 흐름 통과** (중간 멈춤/에러/페이드 깨짐 X)
- [ ] `demo.mp4` (또는 .gif) 가 `learning-journal/yuhyeon/M2-client-visuals/`에 박힘 + git 추적
- [ ] `M2-recap.md` 작성됨 (질문 6~8개 답변 채워짐)
- [ ] `08-regression-and-demo-DONE.md` 박힘 + frontmatter 통과 (pre-commit hook 통과)
- [ ] M2-client-visuals 디렉토리에 -DONE.md 페어 **8/8** 완성

---

## 🧪 테스트

**수동 테스트:**
- 위 10 시나리오 한 흐름 (스크립트 없이 즉흥적으로)
- 두 번째 흐름: 같은 시나리오 다시 (재현 가능성 확인)

**자동 테스트:** 없음 (M3+ PlayMode 테스트 자동화 검토)

---

## 📚 학습 포인트

- **회귀 테스트 사고법 확장**: M1 = 6 시나리오, M2 = M1 + 4. 마일스톤 누적될수록 회귀 항목 누적. 수동에서 자동(PlayMode 테스트) 전환 시점이 옴 (M3+).
- **"데모 가능한 단위" 원칙 재확인**: M1 = 흐름 골격, M2 = 외관. 두 마일스톤이 합쳐져 *처음으로 캐릭터가 살아있는 게임처럼 보임*.
- **박제 분업 (ADR-013)**: `-DONE.md` = AI 사실 박제, `learning-journal/M2-recap.md` = 본인 회고. 가짜 학습 방지.
- **포트폴리오 누적**: M1 demo.mp4 + M2 demo.mp4 → GitHub README 임베드 + 면접 시연 첫 2단추.

---

## ⚠️ 함정 / 주의사항

- **회귀 도중 새 버그 발견 시 *그 자리에서 fix X***: 새 Phase로 떼어 핀에 박고 처리. (헌법 + M1 Phase 06 결정 재확인)
- **영상 파일 크기**: 60~90초 1280×720 mp4 = 1~3MB 보통. 5MB 미만이면 그냥 commit OK. 5MB+ 면 git LFS 검토.
- **학습 일지 안 쓰고 다음 마일스톤 가기**: "내가 뭘 배웠는지" 1주일 뒤 기억 안 남. *반드시* `/journal:phase` 호출.
- **M1 회귀가 깨졌을 때**: M2 작업 중 어디서 깨졌는지 추적 (대개 Phase 04 PlayerController가 옛 placeholder 위치를 가로챘거나, Phase 06 폰트가 옛 텍스트 렌더 깨뜨림). 별도 fix Phase로 떼어내기.
- **timeScale=0 흐름 정합**: M1 Phase 04 결정 — Animator도 timeScale에 영향 받음. 일시정지 중 캐릭터 + 애니 둘 다 멈춰야 함. Phase 02 함정 점검 항목.

---

## ➡️ 다음 Phase

**M2-client-visuals 마일스톤 마감**. 다음은:

- (선택) **`/journal:bug unity-setparent-world-position-stays`** — M1 Phase 04 시각 fix 사건 (M1 마감 시점에 밀어둔 ★★★ 면접 결정타). M2 마감 직후가 가장 fresh — 마지막 호출 기회.
- **다음 마일스톤 결정** — 팀장 합의 필요. 후보:
  - 본인 트랙 M3 = 서버 합류 (M2 PlayerController → prediction layer 분리 + reconciliation)
  - 본인 트랙 M3 = 컨텐츠 (몬스터 sprite + 공격 애니 + 대화 UI)
  - 팀장 트랙 M3-multiplayer 합류 (두 명이 같은 맵에서 보이는 클라 합류)

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
