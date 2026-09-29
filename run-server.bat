@echo off
setlocal
chcp 65001 >nul
title Dawnholder GameServer (WSL2)
cd /d "%~dp0"
echo Sync and build this workspace, then start GameServer on port 7777.
echo Stop with Ctrl+C. Another running server must be stopped by its owner.
wsl -d Ubuntu -- bash ./99_Tools/sync-wsl.sh run
set "server_exit=%errorlevel%"
echo Server task finished with exit code %server_exit%.
exit /b %server_exit%
