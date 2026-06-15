# Append VoidApostle to PoolManager.bossPrefabs (index 16 for VoidCalamity apostlePoolIndex).
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)
$apostleGuid = 'bb21eee63b4ee804bb286514ac5f903d'

if ($text -match "guid: $apostleGuid") {
    Write-Host '[PatchVoidApostlePool] VoidApostle already in scene.'
    exit 0
}

$m = [regex]::Match($text, "(?m)^  - \{fileID: (\d+), guid: e7f400abba330f048be418a9e0bde688, type: 3\}")
if (-not $m.Success) { throw 'VoidCalamityBoss pool line not found' }
$line = "  - {fileID: $($m.Groups[1].Value), guid: $apostleGuid, type: 3}`n"
$anchor = "  - {fileID: $($m.Groups[1].Value), guid: e7f400abba330f048be418a9e0bde688, type: 3}`r`n  bossBulletPrefabs:"
$replacement = "  - {fileID: $($m.Groups[1].Value), guid: e7f400abba330f048be418a9e0bde688, type: 3}`r`n$line  bossBulletPrefabs:"
if (-not $text.Contains($anchor)) {
    $anchor = $anchor.Replace("`r`n", "`n")
    $replacement = $replacement.Replace("`r`n", "`n")
}
if (-not $text.Contains($anchor)) { throw 'bossPrefabs/bossBulletPrefabs boundary not found' }
$text = $text.Replace($anchor, $replacement)
[IO.File]::WriteAllText($scenePath, $text)
Write-Host '[PatchVoidApostlePool] Added VoidApostle at bossPrefabs[16].'
