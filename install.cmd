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

:: Step 5 - optionally install Windows Terminal profile
set WT_FOUND=0
if exist "%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json" set WT_FOUND=1
if exist "%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe\LocalState\settings.json" set WT_FOUND=1
if exist "%LOCALAPPDATA%\Microsoft\Windows Terminal\settings.json" set WT_FOUND=1

set PROFILE_INSTALLED=0
if "%WT_FOUND%"=="1" (
    set /p INSTALL_PROFILE=  [5/5] Windows Terminal found. Install launcher profile? [Y/n]:
    if "!INSTALL_PROFILE!"=="" set INSTALL_PROFILE=Y
    if /i "!INSTALL_PROFILE!"=="Y" (
        "%EXE_PATH%" --install-profile
        if !ERRORLEVEL!==0 (
            set PROFILE_INSTALLED=1
        ) else (
            echo   [5/5] Warning: profile installation failed. Run ClaudeWorkspacePicker.exe --install-profile to retry.
        )
    ) else (
        echo   [5/5] Skipped. Run ClaudeWorkspacePicker.exe --install-profile to add the profile later.
    )
) else (
    echo   [5/5] Windows Terminal not found - skipped. Run ClaudeWorkspacePicker.exe --install-profile after installing Windows Terminal.
)

echo.
if "%PROFILE_INSTALLED%"=="1" (
    echo Done. Open Windows Terminal and select "Claude Workspace Picker" to get started.
) else (
    echo Done.
)
endlocal
