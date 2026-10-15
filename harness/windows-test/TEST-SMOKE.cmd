@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0TEST-SMOKE.ps1"
set "testExit=%errorlevel%"
pause
exit /b %testExit%
