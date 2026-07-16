@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-git.ps1" -UnityPath "%~1"
echo.
pause
