# BiomeGimmickSpawner.gimmickIndexes hex blobs -> YAML int lists
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)

$replacements = [ordered]@{
    '  gimmickIndexes: 0000000001000000' = "  gimmickIndexes:`r`n  - 0`r`n  - 1"
    '  gimmickIndexes: 0300000004000000' = "  gimmickIndexes:`r`n  - 3`r`n  - 4"
    '  gimmickIndexes: 05000000' = "  gimmickIndexes:`r`n  - 5"
    '  gimmickIndexes: 06000000' = "  gimmickIndexes:`r`n  - 6"
    '  gimmickIndexes: 0700000008000000080000000800000009000000' = "  gimmickIndexes:`r`n  - 7`r`n  - 8`r`n  - 8`r`n  - 8`r`n  - 9"
    '  gimmickIndexes: 0a0000000b0000000c000000' = "  gimmickIndexes:`r`n  - 10`r`n  - 11`r`n  - 12"
}

$patched = 0
foreach ($entry in $replacements.GetEnumerator()) {
    if ($text.Contains($entry.Key)) {
        $text = $text.Replace($entry.Key, $entry.Value)
        $patched++
    }
}

if ($patched -eq 0) {
    Write-Host '[FixGimmickIndexes] No hex gimmickIndexes lines found (already fixed?)'
    exit 0
}

[IO.File]::WriteAllText($scenePath, $text)
Write-Host "[FixGimmickIndexes] Patched $patched gimmick index blocks."
