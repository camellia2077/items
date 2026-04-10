@echo off
setlocal
call "%~dp0scan_thresholds.bat" py-dir-over-files %*
exit /b %errorlevel%
