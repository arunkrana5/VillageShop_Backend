$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "BUILDING VILLAGESHOP MOBILE APK" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$flutterCmd = "D:\flutter\bin\flutter.bat"

if (-not (Test-Path $flutterCmd)) {
    Write-Host "Flutter SDK not found at D:\flutter. Please ensure setup script completes." -ForegroundColor Red
    exit 1
}

Set-Location "D:\VillageShop_Mobile"

Write-Host "Fetching Flutter package dependencies (pub get)..." -ForegroundColor Yellow
& $flutterCmd pub get

Write-Host "Building debug APK file..." -ForegroundColor Yellow
& $flutterCmd build apk --debug

$apkPath = "D:\VillageShop_Mobile\build\app\outputs\flutter-apk\app-debug.apk"

if (Test-Path $apkPath) {
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "APK GENERATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "APK Location: $apkPath" -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor Green
} else {
    Write-Host "APK file was not created. Check Flutter output for details." -ForegroundColor Red
}
