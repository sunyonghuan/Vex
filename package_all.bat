@echo off
setlocal

set "ROOT=%~dp0"
set "PACKAGE_ARGS="

:parse
if "%~1"=="" goto run

if /I "%~1"=="--force" (
    set "PACKAGE_ARGS=%PACKAGE_ARGS% -Force"
    shift
    goto parse
)

echo Usage: package_all.bat [--force]
exit /b 2

:run
echo Publishing Vex release outputs...
call "%ROOT%publish_all.bat" || exit /b 1

echo.
echo Packaging Vex release artifacts...
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%ROOT%scripts\package_vex_artifacts.ps1" %PACKAGE_ARGS% || exit /b 1

echo.
echo Release artifacts are ready under "%ROOT%artifacts\release".
exit /b 0
