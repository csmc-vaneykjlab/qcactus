@echo off
setlocal

:: ============================================================
::  QCactus Build + Installer Script
::  Run this from the repo root on a Windows machine.
::  Requires:
::    - .NET 6 SDK  (https://dotnet.microsoft.com/download)
::    - Inno Setup 6 (https://jrsoftware.org/isinfo.php)
:: ============================================================

set PROJECT=ThermoDust\ThermoDust.csproj
set PUBLISH_DIR=publish
set ISS_SCRIPT=installer\QCactus.iss
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo.
echo ============================================================
echo  Step 1: Clean previous publish output
echo ============================================================
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
    echo Cleaned: %PUBLISH_DIR%
)

echo.
echo ============================================================
echo  Step 2: Publish (self-contained, win-x64)
echo ============================================================
dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:PublishReadyToRun=true ^
    -o "%PUBLISH_DIR%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: dotnet publish failed. Check the output above.
    exit /b 1
)
echo Publish complete: %PUBLISH_DIR%\

echo.
echo ============================================================
echo  Step 3: Copy images into publish output
echo ============================================================
if not exist "%PUBLISH_DIR%\images" mkdir "%PUBLISH_DIR%\images"
xcopy /s /y "ThermoDust\images\*" "%PUBLISH_DIR%\images\" >nul
echo Images copied.

echo.
echo ============================================================
echo  Step 4: Compile Inno Setup installer
echo ============================================================
if not exist %ISCC% (
    echo ERROR: Inno Setup not found at %ISCC%
    echo Download from: https://jrsoftware.org/isinfo.php
    exit /b 1
)

%ISCC% "%ISS_SCRIPT%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Inno Setup compilation failed.
    exit /b 1
)

echo.
echo ============================================================
echo  Done!
echo  Installer: installer\Output\QCactus_Setup.exe
echo ============================================================
endlocal
