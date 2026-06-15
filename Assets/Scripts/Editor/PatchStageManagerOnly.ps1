# StageManager stageDatas / stages / ending — anchor 기반 단일 치환 (씬 구조 파손 방지)
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)

function Format-UnityIntArray([int[]]$ints) {
    if ($null -eq $ints -or $ints.Length -eq 0) { return '' }
    return (($ints | ForEach-Object { "`r`n      - $_" }) -join '')
}

function New-WaveYaml([bool]$isBoss, [int[]]$bossIndexes, [object[]]$enemies) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('    - isBossWave: ' + [int]$isBoss)
    if ($isBoss -and $bossIndexes.Length -gt 0) {
        $lines.Add('      bossSpawnIndexes:' + (Format-UnityIntArray $bossIndexes))
    } else {
        $lines.Add('      bossSpawnIndexes: ')
    }
    $lines.Add('      enemies:')
    foreach ($e in $enemies) {
        $lines.Add('      - spawnDataIndex: ' + $e.Index)
        $lines.Add('        spawnCount: ' + $e.Count)
    }
    return ($lines -join "`r`n")
}

function E([int]$Index, [int]$Count) { return [pscustomobject]@{ Index = $Index; Count = $Count } }

$stageBoss = @(@(36,37), @(38,39), @(40,41,42), @(43,44,45), @(46,47), @(48,49), @(50,51))
$cfg = @(
    @{ A = 0; B = 1; Elite = -1 }
    @{ A = 2; B = 3; Elite = -1 }
    @{ A = 4; B = 6; Elite = -1 }
    @{ A = 7; B = 9; Elite = 9 }
    @{ A = 10; B = 13; Elite = 13 }
    @{ A = 14; B = 20; Elite = 19 }
    @{ A = 21; B = 35; Elite = 31 }
)

$stageLines = New-Object System.Collections.Generic.List[string]
$stageLines.Add('  stageDatas:')
for ($s = 0; $s -lt 7; $s++) {
    $c = $cfg[$s]
    $stageLines.Add('  - waves:')
    for ($w = 0; $w -lt 5; $w++) {
        if ($w -eq 4) {
            if ($c.Elite -ge 0) { $en = @(E $c.A 5; E $c.Elite 5) }
            else { $en = @(E $c.A 5; E $c.B 5) }
            $stageLines.Add((New-WaveYaml $true $stageBoss[$s] $en))
            continue
        }
        switch ($w) {
            0 { $en = @(E $c.A 1) }
            1 { $en = @(E $c.A 5) }
            2 { $en = @(E $c.A 10) }
            3 {
                if ($c.Elite -ge 0) { $en = @(E $c.A 5; E $c.Elite 3) }
                else { $en = @(E $c.A 5; E $c.B 5) }
            }
        }
        $stageLines.Add((New-WaveYaml $false @() $en))
    }
}
$newStageDatas = $stageLines -join "`r`n"

$anchorStart = '  m_EditorClassIdentifier: Assembly-CSharp::StageManager'
$sm = $text.IndexOf($anchorStart)
if ($sm -lt 0) { throw 'StageManager not found' }
$stageDatasStart = $text.IndexOf("`r`n  stageDatas:", $sm)
if ($stageDatasStart -lt 0) { $stageDatasStart = $text.IndexOf("`n  stageDatas:", $sm) }
if ($stageDatasStart -lt 0) { throw 'stageDatas anchor not found' }
$stageDatasStart += 2

$endingStart = $text.IndexOf("`r`n  endingAfterStageNumber:", $stageDatasStart)
if ($endingStart -lt 0) { $endingStart = $text.IndexOf("`n  endingAfterStageNumber:", $stageDatasStart) }
if ($endingStart -lt 0) { throw 'endingAfterStageNumber anchor not found' }
$endingStart += 2

$text = $text.Substring(0, $stageDatasStart) + $newStageDatas + "`r`n" + $text.Substring($endingStart)

$sm = $text.IndexOf($anchorStart)
$stagesStart = $text.IndexOf("`r`n  stages:", $sm) + 2
$stageDatasStart2 = $text.IndexOf("`r`n  stageDatas:", $stagesStart)
$newStages = @(
    '  stages:'
    '  - {fileID: 779264545}'
    '  - {fileID: 588080940}'
    '  - {fileID: 621035515}'
    '  - {fileID: 1101518933}'
    '  - {fileID: 247920757}'
    '  - {fileID: 1113449814}'
    '  - {fileID: 1625262798}'
) -join "`r`n"
$text = $text.Substring(0, $stagesStart) + $newStages + "`r`n" + $text.Substring($stageDatasStart2 + 2)

$text = [regex]::Replace($text, '(?m)(  m_EditorClassIdentifier: Assembly-CSharp::StageManager[\s\S]*?^  endingAfterStageNumber: )\d+', '${1}7', 1)

[IO.File]::WriteAllText($scenePath, $text)
Write-Host '[PatchStageManagerOnly] stageDatas + stages + ending patched.'
