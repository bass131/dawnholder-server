@echo off
chcp 65001 >nul
title Dawnholder GameServer (WSL2 / ADR-029)
cd /d "%~dp0"

echo ============================================
echo   Dawnholder GameServer  (WSL2 경유 / ADR-029)
echo   Windows 직접 실행은 SAC(Smart App Control)가 차단
echo   -^> WSL2 Ubuntu 에서 동기화+빌드+기동
echo   Listen 0.0.0.0:7777    Stop: Ctrl+C
echo ============================================
echo.

echo [1/2] WSL2 동기화 + 빌드 ...
wsl -d Ubuntu -- bash -lc "cd /mnt/c/Dev/ClaudeDev && rsync -a --delete --exclude 'bin/' --exclude 'obj/' Dawnholder.slnx global.json 02_Server 98_Shared 99_Tools 04_ClientNet ~/dawnholder-poc/ && cd ~/dawnholder-poc && ~/.dotnet/dotnet build Dawnholder.slnx"
if errorlevel 1 (
  echo.
  echo === 빌드 실패 — 위 오류 확인 후 다시 실행 ===
  pause
  exit /b 1
)

echo.
echo [2/2] 서버 기동 ... (종료는 Ctrl+C)
wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc/02_Server/GameServer/bin/Debug/net10.0 && tail -f /dev/null | ~/.dotnet/dotnet GameServer.dll"

echo.
echo === Server stopped, exit code %errorlevel% ===
pause
