### ADR-020: 훅 실행 환경 의존성 — Git Bash on Windows + 검증 패턴
**날짜**: 2026-05-14
**상태**: 채택됨
**결정**: Claude Code의 bash 훅(`.claude/hooks/*.sh`)은 **Git for Windows 설치 + 시스템 PATH에 `C:\Program Files\Git\bin` 등록**을 전제로 한다. 훅 작성 시 (a) `#!/usr/bin/env bash` shebang 사용 / (b) PowerShell 전용 문법 금지 (POSIX bash만) / (c) Windows 경로 분리자(`\`) 직접 박지 말고 forward slash 또는 변수 사용 / (d) 도구 의존(`grep`/`sed`/`awk`)은 Git Bash 번들로 충족 가정. **신규 훅 박은 후엔 반드시 동작 검증** — 검증 패턴은 부록 A의 *파일 append 트레이스* 방식 권장(stderr만으로는 결정적 증거 못 얻음, ADR-018 사후 검증에서 실측).
**이유**: Claude Code는 OS 셸을 사용하지 않고 bash를 호출(`sh -c ...`)하므로 Windows에서 Git Bash가 없으면 훅이 *조용히 실패*한다 — 실측: ADR-018 박은 5개 훅이 48시간 silent fail로 의심 → PATH에 Git Bash 누락이 원인 확인(`C:\Program Files\Git\bin`가 시스템 PATH에서 빠져있었음). 또한 **Claude Code가 PostToolUse/Stop 훅의 exit-0 stderr를 도구 결과 stream에 silent 처리**한다는 부수 발견 — 즉 훅이 정상 실행돼도 사용자/AI 모두 안 보이는 영역에 박힌다. 따라서 "박았으니 작동하겠지" 가정은 위험. 검증은 1회성이라도 명시적으로 수행해야 함.
**트레이드오프**: ① Linux/macOS 머신에서 본 프로젝트를 clone하면 훅 PATH 문제는 없으나 Windows 전제 결정이 다수(ADR-017 ASCII 경로 = Windows 호환성 문제 해결, 본 ADR-020 = Windows에서 bash 호출 가정) → cross-OS 이식 비용 누적. ② 검증 패턴 강제 시 신규 훅 박는 비용 증가(코드 + 검증 라운드). ③ 파일 append 트레이스는 `.claude/state/hook-trace.log` 같은 임시 산출물을 만들어 cleanup 의식 필요(검증 후 .gitignore + 삭제). ④ Git Bash 의존은 Windows 머신에서 Git for Windows 미설치 시 즉시 작동 불가 — 팀원 온보딩에 한 줄 추가 필요("Git for Windows 정식 설치 + PATH 확인").

---

#### 부록 A — 훅 동작 검증 패턴 (재사용 자산)

stderr 마커만으로는 Claude Code가 silent 처리하는 환경에서 결정적 증거를 얻을 수 없다. 대신 **파일 append 트레이스**:

```bash
# 훅 본문 어디든 박기 (한 줄)
echo "[HOOK-MARKER] <훅이름> $(date +%Y-%m-%dT%H:%M:%S) pid=$$ event=$CLAUDE_HOOK_EVENT" >> "$CLAUDE_PROJECT_DIR/.claude/state/hook-trace.log"
```

`hook-trace.log`를 grep으로 확인:
- 훅별 마커 줄이 박혀있으면 → 정상 실행 확인 ✅
- 마커가 없으면 → 훅 자체가 호출 안 됨 (PATH·shebang·permission 문제)
- pid·timestamp로 실행 빈도/타이밍 추적 가능

**Cleanup 의식**: 검증 끝나면 마커 줄 제거 + `hook-trace.log`는 `.gitignore`. 영구 박는 마커가 아니라 일시 도구. ADR-018 사후 검증(2026-05-14)에서 PostToolUse 3개 + Stop + UserPromptSubmit 5/5 정상 동작 확정에 사용된 패턴.

**언제 검증할까**:
- 신규 훅 박은 직후 (가장 중요)
- 환경 변경 후 (PATH·Git 업데이트·OS 마이그)
- 훅 동작이 의심될 때 (silent fail 가설 검증)
