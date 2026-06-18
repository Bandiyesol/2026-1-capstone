$ErrorActionPreference = 'Stop'
$repo = Join-Path $PSScriptRoot '..'
$resourcesUi = Join-Path $repo 'Assets\Resources\UI' | Resolve-Path
$resourcesFonts = Join-Path $resourcesUi 'Fonts'
if (-not (Test-Path $resourcesFonts)) {
    New-Item -ItemType Directory -Path $resourcesFonts | Out-Null
}

function Copy-AssetWithNewGuid {
    param(
        [string]$SourceFile,
        [string]$DestFile
    )

    Copy-Item $SourceFile $DestFile -Force
    $metaSource = "$SourceFile.meta"
    $metaDest = "$DestFile.meta"
    if (-not (Test-Path $metaSource)) {
        Write-Host "skip meta: $SourceFile"
        return
    }

    $newGuid = [guid]::NewGuid().ToString('N')
    $meta = [IO.File]::ReadAllText($metaSource)
    $meta = [regex]::Replace($meta, '^guid: [0-9a-f]{32}', "guid: $newGuid", 1)
    [IO.File]::WriteAllText($metaDest, $meta)
    Write-Host "copied $DestFile (guid $newGuid)"
}

$pairs = @(
    @{
        Source = Join-Path $repo 'Assets\Arts\UI\Pixel Buttons\Cross_Idle.png'
        Dest   = Join-Path $resourcesUi 'Cross_Idle.png'
    },
    @{
        Source = Join-Path $repo 'Assets\Arts\UI\Pixel Buttons\Cross_Pushed.png'
        Dest   = Join-Path $resourcesUi 'Cross_Pushed.png'
    },
    @{
        Source = Join-Path $repo 'Assets\Arts\UI\Vol 6 Ui Expansion Pack\Runes\Runes_13_01.png'
        Dest   = Join-Path $resourcesUi 'Runes_13_01.png'
    }
)

foreach ($pair in $pairs) {
    if (-not (Test-Path $pair.Source)) {
        Write-Warning "missing source: $($pair.Source)"
        continue
    }
    Copy-AssetWithNewGuid -SourceFile $pair.Source -DestFile $pair.Dest
}

$fontFiles = @(
    'neodgm SDF.asset',
    'neodgm Korean Fallback SDF.asset'
)
foreach ($fileName in $fontFiles) {
    $source = Join-Path $repo "Assets\Arts\UI\Fonts\$fileName"
    $dest = Join-Path $resourcesFonts $fileName
    if (-not (Test-Path $source)) {
        Write-Warning "missing font: $source"
        continue
    }
    Copy-AssetWithNewGuid -SourceFile $source -DestFile $dest
}

Write-Host 'UI runtime asset copy complete.'
