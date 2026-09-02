@echo off
REM Double-clickable launcher for pack.ps1.
REM pack.ps1 validates the release state and then builds Release automatically.
REM The pause below is what keeps this window open even when PowerShell fails
REM before the script runs at all, such as a parse error or a blocked script.

setlocal
echo Running pack.ps1 ...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0pack.ps1" %*

echo.
echo pack.ps1 finished with exit code %ERRORLEVEL%
pause
endlocal
