---
owner: youngho
milestone: (ad-hoc, M4.9 진입 선행)
phase: youhyun-art-integration
title: 유현 작업물 통합 (사운드 도구/대화창 UI/스킬 애니/SFX) + 워킹트리 잔여물 봉합 + Pretendard 폰트 Static 전환
status: done
completed: 2026-06-09
grade: 복잡
summary: M4.9 진입 선행으로 유현 아트/사운드 작업물 4개 stacked PR(#90 handoff 사운드도구 / #87 대화창 9-slice UI / #88 Knight·Mage 스킬 애니+teleport / #89 SFX 13종)을 main에 통합 머지하고, 그동안 워킹트리에 dirty로 남던 영호 고유 잔여물 5건을 영구 봉합한 작업. 핵심 = ① Pretendard 한글 폰트를 Dynamic→Static 전환(Unity MCP RunCommand로 TryAddCharacters, 한글 완성형 11172+ASCII=11267자) — Dynamic atlas가 게임 실행마다 글자를 런타임 누적해 매 세션 git dirty 나던 churn을 영구 종결. 첫 굽기 64pt 4-atlas=132MB가 GitHub 100MB hard limit에 걸려(이 repo LFS 미사용) sampling 64→40pt로 압축해 2-atlas 68MB로 해결. ② 유현 작업이 base=docs/yuhyeon-ui-art-handoff인 stacked PR이라 handoff를 먼저 main 머지 후 #87/#88/#89를 base retarget해 순차 머지. ③ 봉합 ③ menu_button_frame이 유현 #87(border 45 단일 9-slice)과 충돌 — 영호 옛 멀티스프라이트 실험 잔재라 유현 버전 채택(merge --theirs), 봉합에서 제외. CODEOWNERS 분석으로 머지 권한 최적화: 03_Client 공유 영역(#87/#88/#89, author=유현)은 영호 정식 승인 / 99_Tools 단독+봉합 PR(author=영호)은 self-approve 불가라 admin. 영호 2클라 게임 테스트("이상 무") 통과 후 머지. main 257d5e3 안착, 워킹트리 완전 클린.
---

# 유현 작업물 통합 + 워킹트리 잔여물 봉합 — 박제

**마감 일자**: 2026-06-09
**등급**: 복잡 (03_Client 광범위 180 files / irreversible 5 PR 머지 + admin / unity-asset 위험 깃발)
**WORK-ID**: ad-hoc-20260609-youhyun-integration
**다음**: M4.9 텔레포트 plan 착수 (teleport 리소스 #88로 진입)

---

## TL;DR

M4.9 진입 *전* 선행으로, 유현이 올린 아트/사운드 작업 4개와 영호 워킹트리에 매 세션 dirty로 남던 잔여물 5건을 한 흐름에 통합·봉합했다.

**유현 작업 = stacked PR 4개** (base가 main이 아니라 `docs/yuhyeon-ui-art-handoff`): #90 handoff(99_Tools BGM 3곡 + SFX 합성 도구) → #87 대화창 Dialogue.prefab 9-slice UI + UI 에셋 중복정리 → #88 Knight·Mage 스킬 애니 + teleport(M4.9 리소스) → #89 SFX 13종 wav. handoff를 먼저 main에 머지한 뒤 #87/#88/#89의 base를 main으로 retarget해 순차 머지 = stacked 구조를 깔끔히 풀었다.

**Pretendard 폰트 Static 전환 (핵심 난제)**: 기존 폰트는 Dynamic atlas라 게임 실행 때마다 화면에 뜬 한글을 런타임에 atlas로 구워 .asset에 저장 → 매 세션 git dirty(work-pin "Pretendard churn"의 정체, baseline은 char 0으로 비워진 상태). Unity MCP RunCommand로 `TryAddCharacters`(한글 완성형 AC00-D7A3 11172 + ASCII = 11267자) 후 `atlasPopulationMode=Static`으로 박제 → 런타임 누적 종결. 단 첫 굽기 64pt 4-atlas = **132MB**가 GitHub 100MB hard limit에 막혀(이 repo는 LFS 미사용, `*.asset text eol=lf` 추적) sampling을 64→40pt로 낮춰 2-atlas **68MB**로 압축. SDF 특성상 선명도 손실 거의 없음.

**충돌 1건 = 봉합 ③ vs 유현 #87**: 둘 다 `7_menu_button_frame.png.meta`를 건드림 — 영호 봉합은 spriteMode Multiple(9칸 분할, 옛 실험 잔재), 유현 #87은 Single + border 45(대화창 NameBox 정식 9-slice). 유현 게 정답이라 merge 충돌을 `--theirs`로 유현 버전 채택하고 봉합에선 ③ 제외(①②④⑤만).

**CODEOWNERS 기반 머지 권한 최적화**: 03_Client는 `@bass131 @ingyu @jungyoohyun0105` 공유라 author=유현인 #87/#88/#89는 영호(공유 코드오너) **정식 승인**으로 머지(admin 불필요). 99_Tools(@bass131 단독)인 handoff와 author=영호인 봉합 PR만 self-approve 불가라 **admin**. admin 예외를 2건으로 최소화.

---

## AC 검증 결과

실제 실행 명령 + 결과 (추측·요약 아님):

### 1. Pretendard Static 굽기 (Unity MCP RunCommand)
- 64pt 1차: `TryAdd ok=True missing=0 / CharCount=11267 GlyphCount=11267 AtlasTex=4 / pop=Static` → `ls` 결과 **138,881,471 bytes (132MB)** → GitHub 거부 판정
- 40pt 압축: `PointSizeProp type=Float / ok=True missing=0 sampling=40 / char=11267 AtlasTex=2` → `ls` **72,207,293 bytes (68MB)** / `grep m_AtlasPopulationMode: 0`(Static) `m_PointSize: 40`
- Unity 콘솔: `ReadConsole Types=[Error]` → **0 log entries** (reimport 에러 0)
- 폰트 `.meta` 무변경 = guid 유지 (참조 안 깨짐) 확인

### 2. 유현 PR 4개 main 머지
- `gh pr merge 90 --merge` → `gh pr view 90 state=MERGED` (handoff, CI test=pass 2m16s 확인 후)
- `gh pr edit 87 --base main` retarget → `gh pr review 87 --approve` → merge → `state: MERGED`
- #88/#89 동일 패턴 → 각 `state: MERGED`

### 3. 봉합 ①②④⑤ PR
- #87 merge 충돌 = `7_menu_button_frame.png.meta` 1건만 → `git checkout --theirs` → `grep` 결과 `spriteMode: 1 / spriteBorder: {x:45...}` (유현 버전 채택 확인)
- 봉합 브랜치 `git status --short` = ①②④⑤만 staged(③ 없음, Shared.dll drift는 `git checkout HEAD`로 복원)
- `git push` → `remote: warning: GH001: Large files detected`(68MB, 차단 아님) → PR #91
- `#91 mergeStateStatus=BLOCKED`(영호 author self-approve 불가) → `CLAUDE_ADMIN_BYPASS_REASON=... gh pr merge 91 --admin --merge` → `state: MERGED`

### 4. 서버 구동 + 영호 게임 테스트
- WSL2 빌드: `dotnet build GameServer.csproj` → **Build succeeded. 0 Warning 0 Error**
- 구동(run_in_background): `ss -tln | grep :7777` → **LISTENING_7777_OK**, tick 루프 #960까지 정상(avg 0.02~5ms / 50ms 예산), `OnConnected from 127.0.0.1` 핸드셰이크 정상
- **영호 2클라 게임 테스트 = "OK 이상 무"** (대화창 9-slice / Knight·Mage 스킬 애니 / SFX / 한글 폰트 / menu_button border 45 전부 정상)
- 서버 `pkill -f 'GameServer[.]dll'` → 종료 확인

### 5. 최종 상태
- `git log --oneline` main HEAD = **257d5e3** (Merge #91) ← #90/#87/#88/#89/#91 전부 위
- `git status --short` = **빈 출력** (워킹트리 완전 클린, Shared.dll drift도 없음)
- 머지된 브랜치 정리: 로컬 2개 삭제 / 원격은 "Automatically delete head branches"로 자동 삭제됨 / 백업 삭제

---

## 결정 흐름

이 작업의 주요 갈림길과 영호 결정:

1. **잔여물 5건 봉합 방식** → 전부 커밋(복원 X). ①controller(1회성 마이그레이션)·②scene(ToneTest 보존)·③meta·⑤Portrait는 커밋이 곧 영구 봉합. (③은 이후 유현 충돌로 철회)
2. **Pretendard churn 처리** → Static 전환(영호 선택). 현 상태 커밋만 하면 Dynamic이라 새 글자 만날 때마다 재발 → 영구 봉합 위해 Static.
3. **글자 세트** → 한글 전체 11172 + ASCII(영호 선택). MMORPG 임의 한글(닉네임/채팅) 대비. 완성형 2350·실사용만은 기각.
4. **132MB GitHub 거부 대응** → 한글 전체 유지 + sampling 해상도 압축(영호 선택, 64→40pt). 완성형 축소·Git LFS 도입은 기각(LFS는 유현도 설치 필요한 협업 셋업 변경).
5. **폰트 굽기 주체** → 영호 명시 위임으로 AI가 Unity MCP 직접 처리(평소 "Unity 외관=영호 직접" 원칙의 명시 예외).
6. **통합 방식** → 하이브리드(영호 선택): 통합 브랜치에 다 합쳐 영호 게임 테스트 → 통과 후 유현 PR 순차 머지(작성자/리뷰 보존). 통합 브랜치 한방 PR·테스트 없이 순차 머지는 기각.
7. **③ menu_button 충돌** → 유현 #87(Single + border 45) 채택. 영호 옛 멀티스프라이트(9칸 분할) 실험 잔재 철회.
8. **머지 권한** → CODEOWNERS 분석으로 admin 최소화(03_Client 공유 영역 유현 PR 3개는 영호 정식 승인 / 99_Tools 단독·봉합 PR만 admin). 각 머지 영호 GO 후 진행.

---

## 학습 일지 후보 키워드

knowledge 트랙 A 박제 후보 (사용자 확인 후):

- **TMP 폰트 Dynamic→Static MCP 굽기**: `TMP_FontAsset.TryAddCharacters(chars, out missing)` + `atlasPopulationMode=Static` + `SerializedObject`로 `m_AtlasWidth/m_FaceInfo.m_PointSize` 설정. atlas 텍스처는 TMP가 sub-asset 자동 등록. RunCommand에서 TMPro 직접 참조 가능(reflection 불필요).
- **폰트 atlas 크기 = sampling point size로 제어**: glyph 수 고정(11267)이어도 sampling 64→40pt면 atlas 면적 (40/64)²≈0.39배 → 4장→2장 → 132MB→68MB. SDF는 벡터처럼 스케일돼 40pt도 UI 선명도 손실 거의 없음.
- **132MB = GitHub 100MB hard limit 거부**: 단일 파일 100MB 초과 시 push 자체 차단. 이 repo는 LFS 미설치(`git lfs ls-files` 0건) + `*.asset text eol=lf` 추적. 대용량 에셋은 글자수↓ / sampling↓ / LFS 중 택1.
- **stacked PR main 머지**: base가 main 아닌 공통 브랜치(handoff)면 → 공통부터 main 머지 → 나머지 `gh pr edit N --base main` retarget → 순차 머지. retarget 시 diff가 main 기준 재계산되어 공통부분 중복 안 됨.
- **CODEOWNERS로 머지 권한 판정**: 공유 영역(여러 오너) + author가 그 중 1명 아니면 → 다른 오너 정식 승인 가능. 단독 영역 or author=유일 오너면 self-approve 불가 → admin. admin 예외 최소화가 헌법 정신(Auto Mode classifier가 불필요 bypass 거부).
- **merge 충돌 theirs/ours**: merge 중 ours=현재 브랜치, theirs=가져오는 브랜치. `git checkout --theirs <path>` + `git add` 후 `git commit`으로 한쪽 버전 통째 채택. 같은 에셋을 두 사람이 다르게 import한 충돌에 유효.
- **PR 머지 후 원격 브랜치 자동 삭제**: repo "Automatically delete head branches" 켜져 있으면 `gh pr merge` 시 head 브랜치 원격 자동 삭제 → `git push --delete`가 "remote ref does not exist"(이미 삭제, 정상).
- **gh 출력 파싱 함정**: `gh pr merge`가 성공해도 후속 `head -N` 잘림 + python 파싱 실패로 exit 1로 보일 수 있음. 머지 성공 판정은 `gh pr view N --json state --jq .state == MERGED`로 직접 확인.

---

## Phase 박제 요약

| 단계 | 내용 | 결과 |
|---|---|---|
| 잔여물 봉합 (1차) | ①②③④⑤ 5건 커밋 (③ 포함, 통합 전) | 40c02f3 (이후 ③은 유현 버전으로 교체) |
| Pretendard Static | 64pt 132MB → 40pt 68MB 압축 굽기 | char 11267 / Static / 에러 0 |
| 테스트 통합 브랜치 | handoff+#87+#88+#89 merge (③ 충돌 theirs) | 052c83a (영호 테스트용, 머지 후 삭제) |
| 유현 PR 머지 | #90 admin / #87·#88·#89 정식 승인 | 전부 MERGED |
| 봉합 ①②④⑤ PR | ③ 제외, main 최신 기준 새 브랜치 | #91 admin MERGED |
| 정리 | 브랜치/백업 정리 + work-pin 갱신 + 본 박제 | main 257d5e3 클린 |
