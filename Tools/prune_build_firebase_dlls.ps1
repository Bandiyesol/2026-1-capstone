# 빌드 출력 폴더에 FirebaseCppApp 버전 DLL이 2개 이상이면 네이티브 크래시가 납니다.
param(
    [string]$BuildPluginsDir
)

if ([string]::IsNullOrWhiteSpace($BuildPluginsDir)) {
    $BuildPluginsDir = Join-Path $env:USERPROFILE "Downloads\더 라스트 룬\TheLastRune\TheLastRune_Data\Plugins\x86_64"
}

if (-not (Test-Path $BuildPluginsDir)) {
    Write-Host "Plugins folder not found: $BuildPluginsDir"
    exit 1
}

$appDlls = Get-ChildItem -Path $BuildPluginsDir -Filter "FirebaseCppApp-*.dll" | Sort-Object LastWriteTimeUtc -Descending
if ($appDlls.Count -le 1) {
    Write-Host "No duplicate FirebaseCppApp DLLs."
    exit 0
}

$keep = $appDlls | Where-Object { $_.Name -like "*13_10_0*" } | Select-Object -First 1
if ($null -eq $keep) { $keep = $appDlls[0] }
Write-Host "Keeping: $($keep.Name)"
foreach ($stale in $appDlls | Select-Object -Skip 1) {
    Write-Host "Removing: $($stale.Name)"
    Remove-Item -LiteralPath $stale.FullName -Force
}

Write-Host "Done. Re-run the game exe."
