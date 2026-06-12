#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

Write-Host 'Installing Claude Workspace Picker...'

try {
    # Step 1 - resolve latest release
    $release = Invoke-RestMethod 'https://api.github.com/repos/cytoph/claude-workspace-picker/releases/latest'
    $arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') { 'arm64' } else { 'x64' }
    $assetName = "ClaudeWorkspacePicker-win-$arch.exe"
    $exeAsset = $release.assets | Where-Object { $_.name -eq $assetName }
    if (-not $exeAsset) { throw "No $assetName asset found in release $($release.tag_name)" }
    $exeUrl = $exeAsset.browser_download_url
    $tag = $release.tag_name
    $settingsUrl = "https://raw.githubusercontent.com/cytoph/claude-workspace-picker/$tag/ClaudeWorkspacePicker/settings.jsonc"
    Write-Host "  [1/5] Resolved latest release: $tag"

    # Step 2 - create install directory
    $installDir = Join-Path $env:LOCALAPPDATA 'ClaudeWorkspacePicker'
    if (-not (Test-Path $installDir)) {
        New-Item -ItemType Directory -Path $installDir | Out-Null
        Write-Host "  [2/5] Created $installDir"
    } else {
        Write-Host "  [2/5] Install directory: $installDir"
    }

    # Step 3 - download exe (always overwrite - this is how updates work)
    $exePath = Join-Path $installDir 'ClaudeWorkspacePicker.exe'
    Write-Host '  [3/5] Downloading ClaudeWorkspacePicker.exe...' -NoNewline
    Invoke-WebRequest -Uri $exeUrl -OutFile $exePath
    Write-Host ' done.'

    # Step 4 - write starter settings.jsonc only if absent
    $settingsPath = Join-Path $installDir 'settings.jsonc'
    if (-not (Test-Path $settingsPath)) {
        Invoke-WebRequest -Uri $settingsUrl -OutFile $settingsPath
        Write-Host '  [4/5] Wrote starter settings.jsonc'
    } else {
        Write-Host '  [4/5] settings.jsonc already exists - skipped'
    }

    # Step 5 - install Windows Terminal profile
    & $exePath --install-profile
    Write-Host '  [5/5] Windows Terminal profile installed'

    Write-Host ''
    Write-Host 'Done. Open Windows Terminal and select "Claude Workspace Picker" to get started.'
}
catch {
    Write-Host ''
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host 'Installation failed. Check your internet connection and try again.' -ForegroundColor Red
    exit 1
}
