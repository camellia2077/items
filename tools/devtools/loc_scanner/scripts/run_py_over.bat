@echo off
setlocal
call "%~dp0scan_thresholds.bat" py-over %*
exit /b %errorlevel%
