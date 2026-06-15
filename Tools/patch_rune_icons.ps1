# Patches RuneData.runeIcon references from Vol 6 Runes sprites (no Unity required).
$ErrorActionPreference = 'Stop'
$project = 'c:\Users\ltg\Documents\GitHub\2026-1-capstone'

$runeArtRoot = Join-Path $project 'Assets\Arts\UI\Vol 6 Ui Expansion Pack\Runes'
$dataRoot = Join-Path $project 'Assets\Data\Rune Datas'

$iconByAsset = [ordered]@{
    'Rune_Orbit.asset'     = 'Runes_01_03.png'
    'Rune_Wave.asset'      = 'Runes_02_03.png'
    'Rune_Spiral.asset'    = 'Runes_03_03.png'
    'Rune_Homing.asset'    = 'Runes_04_03.png'
    'Rune_Split.asset'     = 'Runes_05_03.png'
    'Rune_Ricochet.asset'  = 'Runes_06_03.png'
    'Rune_Vampire.asset'   = 'Runes_07_03.png'
    'Rune_Freeze.asset'    = 'Runes_08_03.png'
    'Rune_Chain.asset'     = 'Runes_09_03.png'
    'Rune_Explode.asset'   = 'Runes_10_03.png'
    'Rune_Recursion.asset' = 'Runes_11_03.png'
    'Rune_Gravity.asset'   = 'Runes_12_03.png'
    'Rune_Growth.asset'    = 'Runes_13_03.png'
    'Rune_Blink.asset'     = 'Runes_14_03.png'
}

function Get-SpriteRef([string]$pngName) {
    $metaPath = Join-Path $runeArtRoot ($pngName + '.meta')
    if (-not (Test-Path $metaPath)) { throw "Missing meta: $metaPath" }
    $meta = Get-Content $metaPath -Raw
    if ($meta -notmatch 'guid: ([0-9a-f]+)') { throw "No guid in $metaPath" }
    $guid = $Matches[1]
    if ($meta -notmatch '(?m)^\s+213:\s+(-?\d+)') { throw "No sprite fileID in $metaPath" }
    $fileId = $Matches[1]
    return "runeIcon: {fileID: $fileId, guid: $guid, type: 3}"
}

$patched = 0
foreach ($kv in $iconByAsset.GetEnumerator()) {
    $assetPath = Join-Path $dataRoot $kv.Key
    if (-not (Test-Path $assetPath)) { Write-Host "skip missing $($kv.Key)"; continue }
    $ref = Get-SpriteRef $kv.Value
    $content = [IO.File]::ReadAllText($assetPath)
    $newContent = [regex]::Replace($content, 'runeIcon: \{fileID: [^}]+\}', $ref)
    if ($newContent -eq $content) {
        Write-Host "unchanged $($kv.Key)"
        continue
    }
    [IO.File]::WriteAllText($assetPath, $newContent)
    Write-Host "patched $($kv.Key)"
    $patched++
}

Write-Host "Done. Patched $patched rune icon(s)."
