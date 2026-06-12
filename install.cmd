@echo off
setlocal enabledelayedexpansion

echo Installing Claude Workspace Picker...

:: Detect architecture - handle native 64-bit, ARM64, and WOW64 cases
set ARCH=x64
if "%PROCESSOR_ARCHITECTURE%"=="ARM64" set ARCH=arm64
if "%PROCESSOR_ARCHITECTURE%"=="x86" (
    if "%PROCESSOR_ARCHITEW6432%"=="AMD64" set ARCH=x64
    if "%PROCESSOR_ARCHITEW6432%"=="ARM64" set ARCH=arm64
)

:: Step 1 - resolve latest release tag via PowerShell (most reliable JSON parse on Windows)
for /f "delims=" %%a in ('powershell -NoProfile -Command "(Invoke-RestMethod 'https://api.github.com/repos/cytoph/claude-workspace-picker/releases/latest').tag_name"') do set TAG=%%a
if "%TAG%"=="" (
    echo Error: Could not resolve latest release.
    exit /b 1
)
echo   [1/5] Resolved latest release: %TAG%

:: Step 2 - create install directory
set INSTALL_DIR=%LOCALAPPDATA%\ClaudeWorkspacePicker
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    echo   [2/5] Created %INSTALL_DIR%
) else (
    echo   [2/5] Install directory: %INSTALL_DIR%
)

:: Step 3 - download exe
set EXE_NAME=ClaudeWorkspacePicker-win-%ARCH%.exe
set EXE_URL=https://github.com/cytoph/claude-workspace-picker/releases/download/%TAG%/%EXE_NAME%
set EXE_PATH=%INSTALL_DIR%\ClaudeWorkspacePicker.exe
echo   [3/5] Downloading %EXE_NAME%...
curl -fsSL "%EXE_URL%" -o "%EXE_PATH%"
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to download %EXE_NAME%.
    exit /b 1
)

:: Step 4 - write starter settings.jsonc only if absent
set SETTINGS_PATH=%INSTALL_DIR%\settings.jsonc
if not exist "%SETTINGS_PATH%" (
    set SETTINGS_URL=https://raw.githubusercontent.com/cytoph/claude-workspace-picker/%TAG%/ClaudeWorkspacePicker/settings.jsonc
    curl -fsSL "!SETTINGS_URL!" -o "%SETTINGS_PATH%"
    if !ERRORLEVEL! neq 0 (
        echo Error: Failed to download settings.jsonc.
        exit /b 1
    )
    echo   [4/5] Wrote starter settings.jsonc
) else (
    echo   [4/5] settings.jsonc already exists - skipped
)

:: Step 5 - install Windows Terminal profile
"%EXE_PATH%" --install-profile
echo   [5/5] Windows Terminal profile installed

echo.
echo Done. Open Windows Terminal and select "Claude Workspace Picker" to get started.
endlocal
