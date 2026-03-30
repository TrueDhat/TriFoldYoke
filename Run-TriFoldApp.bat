@echo off
setlocal

set "PROJECT_DIR=%~dp0TriFoldApp"
set "APP_EXE=%PROJECT_DIR%\bin\Debug\net9.0-windows10.0.19041.0\win10-x64\TriFoldApp.exe"

if not exist "%PROJECT_DIR%\TriFoldApp.csproj" (
    echo Could not find project at:
    echo %PROJECT_DIR%
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo .NET SDK was not found on PATH.
    echo Install .NET SDK and try again.
    pause
    exit /b 1
)

echo Building TriFoldApp...
dotnet build "%PROJECT_DIR%\TriFoldApp.csproj" -f net9.0-windows10.0.19041.0 -p:WindowsPackageType=None
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

if exist "%APP_EXE%" (
    start "" "%APP_EXE%"
    exit /b 0
)

echo Built output was not found, falling back to dotnet run...
dotnet run --project "%PROJECT_DIR%\TriFoldApp.csproj" -f net9.0-windows10.0.19041.0 -p:WindowsPackageType=None
if errorlevel 1 (
    echo App exited with an error.
    pause
    exit /b 1
)

endlocal
