@echo off
setlocal EnableExtensions DisableDelayedExpansion
set "launcherExit=1"
set "launcherEntered=0"
where node.exe >nul 2>nul
if errorlevel 1 (
  echo ERROR: Node.js is not available. See README for manual setup.
  goto failed
)
where npm.cmd >nul 2>nul
if errorlevel 1 (
  echo ERROR: npm.cmd is not available. See README for manual setup.
  goto failed
)
pushd "%~dp0frontend" >nul 2>nul
if errorlevel 1 (
  echo ERROR: The frontend folder is unavailable.
  goto failed
)
set "launcherEntered=1"
if not exist "node_modules\.bin\tsc.cmd" goto missing_dependencies
if not exist "node_modules\.bin\vite.cmd" goto missing_dependencies
if not exist "node_modules\react\package.json" goto missing_dependencies
if not exist "node_modules\react-dom\package.json" goto missing_dependencies
if not exist "node_modules\electron\dist\electron.exe" goto missing_electron

echo Building Dawnholder Management...
call npm.cmd run desktop:build
set "launcherExit=%errorlevel%"
if not "%launcherExit%"=="0" goto build_failed
start "" "%CD%\node_modules\electron\dist\electron.exe" .
set "launcherExit=%errorlevel%"
if not "%launcherExit%"=="0" goto launch_failed
popd
exit /b 0

:missing_dependencies
echo ERROR: Local frontend dependencies are missing. See README for manual setup.
goto failed

:missing_electron
echo ERROR: The local Electron binary is missing. See README for manual setup.
goto failed

:build_failed
echo ERROR: Build failed. No previous build will be launched.
goto failed

:launch_failed
echo ERROR: Electron could not be started.
goto failed

:failed
if "%launcherEntered%"=="1" popd
pause
exit /b %launcherExit%
