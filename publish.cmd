@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "C:\AbimToolsScripts\Publish-RevitPlugin.ps1" ^
  -Config "%~dp0publish.json"

if errorlevel 1 (
    echo.
    echo PUBLISH FAILED
    pause
    exit /b 1
)

echo.
echo PUBLISH COMPLETED
