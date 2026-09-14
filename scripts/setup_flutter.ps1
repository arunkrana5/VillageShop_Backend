$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "AUTOMATED FLUTTER SDK DOWNLOAD & ENVIRONMENT SETUP" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$targetDir = "D:\flutter"
$zipPath = "D:\flutter_sdk.zip"

if (Test-Path "$targetDir\bin\flutter.bat") {
    Write-Host "Flutter SDK is already installed at $targetDir" -ForegroundColor Green
} else {
    Write-Host "Fetching latest Flutter Windows SDK release information..." -ForegroundColor Yellow
    $json = Invoke-RestMethod -Uri "https://storage.googleapis.com/flutter_infra_release/releases/releases_windows.json"
    $currentHash = $json.current_release.stable
    $release = $json.releases | Where-Object { $_.hash -eq $currentHash }
    $downloadUrl = "https://storage.googleapis.com/flutter_infra_release/releases/" + $release.archive

    Write-Host "Downloading Flutter SDK using curl from: $downloadUrl" -ForegroundColor Yellow
    curl.exe -L $downloadUrl -o $zipPath --retry 5 --retry-delay 3

    if (-not (Test-Path $zipPath)) {
        Write-Host "Download failed. Please check internet connection." -ForegroundColor Red
        exit 1
    }

    Write-Host "Extracting Flutter SDK to D:\ ... (This may take 1-2 minutes)" -ForegroundColor Yellow
    Expand-Archive -Path $zipPath -DestinationPath "D:\" -Force

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Write-Host "Extraction completed successfully." -ForegroundColor Green
}

# Add D:\flutter\bin to User PATH if not already present
$userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($userPath -notlike "*D:\flutter\bin*") {
    Write-Host "Adding D:\flutter\bin to User PATH environment variable..." -ForegroundColor Yellow
    $newPath = $userPath + ";D:\flutter\bin"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    $env:PATH += ";D:\flutter\bin"
    Write-Host "User PATH updated successfully." -ForegroundColor Green
} else {
    $env:PATH += ";D:\flutter\bin"
}

Write-Host "============================================================" -ForegroundColor Green
Write-Host "Testing Flutter installation..." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green

& "D:\flutter\bin\flutter.bat" --version
