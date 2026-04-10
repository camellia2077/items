@echo off
setlocal
call "%~dp0scan_thresholds.bat" cs-over %*
exit /b %errorlevel%
