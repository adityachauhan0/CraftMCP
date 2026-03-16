@echo off
setlocal

cd /d "%~dp0"

set "DOTNET_EXE=C:\Program Files\dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" set "DOTNET_EXE=dotnet"

echo.
echo [1/3] Running tests...
call "%DOTNET_EXE%" test tests\CraftMCP.Tests\CraftMCP.Tests.csproj
if errorlevel 1 goto :verification_failed

echo.
echo [2/3] Building solution...
call "%DOTNET_EXE%" build CraftMCP.sln
if errorlevel 1 goto :verification_failed

echo.
echo [3/3] Launching CraftMCP...
call "%DOTNET_EXE%" run --project src\CraftMCP.App\CraftMCP.App.csproj
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo CraftMCP exited with code %EXIT_CODE%.
) else (
    echo.
    echo CraftMCP closed normally.
)
goto :end

:verification_failed
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Verification failed. CraftMCP was not launched.

:end
echo.
echo Manual review launcher finished.
pause
exit /b %EXIT_CODE%
