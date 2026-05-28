@echo off
chcp 65001 >nul
title Dawnholder GameServer
cd /d "%~dp0"

echo ============================================
echo   Dawnholder GameServer
echo   Listen 0.0.0.0:7777   Stop: press Enter
echo ============================================
echo.

dotnet run --project "02_Server/GameServer/GameServer.csproj"

echo.
echo === Server stopped, exit code %errorlevel% ===
pause
