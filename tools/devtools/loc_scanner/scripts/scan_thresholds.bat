@echo off
setlocal

if "%~1"=="" goto :usage

set "COMMAND=%~1"
shift

if /I "%COMMAND%"=="py-over" (
    python -m tools.devtools.loc_scanner --lang py --over 150
    exit /b %errorlevel%
)

if /I "%COMMAND%"=="py-dir-over-files" (
    python -m tools.devtools.loc_scanner --lang py --dir-over-files
    exit /b %errorlevel%
)

if /I "%COMMAND%"=="cs-over" (
    python -m tools.devtools.loc_scanner --lang cs --over 350
    exit /b %errorlevel%
)

if /I "%COMMAND%"=="cs-dir-over-files" (
    python -m tools.devtools.loc_scanner --lang cs --dir-over-files
    exit /b %errorlevel%
)

echo [ERROR] Unsupported command: %COMMAND%
goto :usage

:usage
echo Usage:
echo   tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-over
echo   tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-dir-over-files
echo   tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-over
echo   tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-dir-over-files
exit /b 1
