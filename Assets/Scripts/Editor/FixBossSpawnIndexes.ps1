# bossSpawnIndexes hex blob(0000002400000025) -> YAML list (- 36 / - 37)
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)

$replacements = [ordered]@{
    '      bossSpawnIndexes: 0000002400000025' = "      bossSpawnIndexes:`r`n      - 36`r`n      - 37"
    '      bossSpawnIndexes: 0000002600000027' = "      bossSpawnIndexes:`r`n      - 38`r`n      - 39"
    '      bossSpawnIndexes: 00000028000000290000002a' = "      bossSpawnIndexes:`r`n      - 40`r`n      - 41`r`n      - 42"
    '      bossSpawnIndexes: 0000002b0000002c0000002d' = "      bossSpawnIndexes:`r`n      - 43`r`n      - 44`r`n      - 45"
    '      bossSpawnIndexes: 0000002e0000002f' = "      bossSpawnIndexes:`r`n      - 46`r`n      - 47"
    '      bossSpawnIndexes: 0000003000000031' = "      bossSpawnIndexes:`r`n      - 48`r`n      - 49"
    '      bossSpawnIndexes: 0000003200000033' = "      bossSpawnIndexes:`r`n      - 50`r`n      - 51"
}

$patched = 0
foreach ($entry in $replacements.GetEnumerator()) {
    if ($text.Contains($entry.Key)) {
        $text = $text.Replace($entry.Key, $entry.Value)
        $patched++
    }
}

if ($patched -eq 0) {
    Write-Host '[FixBossSpawnIndexes] No hex bossSpawnIndexes lines found (already fixed?)'
    exit 0
}

[IO.File]::WriteAllText($scenePath, $text)
Write-Host "[FixBossSpawnIndexes] Patched $patched boss wave index blocks in ProtoType_LTG.unity"
