@echo off
setlocal
call "%~dp0scan_thresholds.bat" cs-dir-over-files %*
exit /b %errorlevel%
