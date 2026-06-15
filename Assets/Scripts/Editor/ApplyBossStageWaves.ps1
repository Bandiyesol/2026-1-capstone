# 7바이옴 스테이지 웨이브·보스 풀·StageManager stageDatas 동기화
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [System.Collections.Generic.List[string]]([IO.File]::ReadAllLines($scenePath))

function Format-UnityIntArray([int[]]$ints) {
    if ($null -eq $ints -or $ints.Length -eq 0) { return '' }
    return (($ints | ForEach-Object { "`r`n      - $_" }) -join '')
}

function Find-ComponentRange([string]$classId) {
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -eq "  m_EditorClassIdentifier: $classId") {
            $start = $i
            break
        }
    }
    if ($start -lt 0) { throw "Component not found: $classId" }

    $end = $start + 1
    while ($end -lt $lines.Count -and -not $lines[$end].StartsWith('--- !u!')) { $end++ }
    return @{ Start = $start; End = $end }
}

function Replace-ListInRange($range, [string]$key, [string[]]$entryLines) {
    $start = -1
    for ($i = $range.Start; $i -lt $range.End; $i++) {
        if ($lines[$i] -eq "  $key`:") { $start = $i; break }
    }
    if ($start -lt 0) { throw "$key not found in component range" }

    $end = $start + 1
    while ($end -lt $range.End -and $lines[$end] -match '^\s+- ') { $end++ }

    $removeCount = $end - $start - 1
    if ($removeCount -gt 0) { $lines.RemoveRange($start + 1, $removeCount) }

    for ($j = $entryLines.Count - 1; $j -ge 0; $j--) {
        $lines.Insert($start + 1, $entryLines[$j])
    }
}

$bossGuids = @(
    '2564c7bbcc91934449bb19451e2b67c6'
    'ec3211b7d6cf02243aaca86c25c27ac7'
    '7e24f602964320e4688aed6cdd0e48d9'
    '2e7edf8341f16bb47a0511b8c53caf52'
    '94650077b6d7efb4b9ab85a3d42476d3'
    'c2062b5614980a34bb8047c8ca5dbd9a'
    'ce85cad84f4527044a9402d9a46d18db'
    '3450b062414179a4caca9e93b57d903e'
    '7c2e29087d644834e9441ecdf8b396c5'
    '96bfcf9d12f16bf408cf26b126c634c0'
    'a697b2a9ba4bdcf47bd0af38bb859ade'
    '1861b773168297043b001a096fd481c3'
    '9c520ae05459fb140b8acdc7830350a8'
    '279d59defa572dd42a0189821547f3ca'
    '31a7ed74d1554da49b640936aed529fa'
    'e7f400abba330f048be418a9e0bde688'
)

$guidToLine = @{}
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s+- \{fileID: \d+, guid: ([0-9a-f]{32}), type: 3\}$') {
        $guid = $Matches[1]
        if (-not $guidToLine.ContainsKey($guid)) {
            $guidToLine[$guid] = $lines[$i]
        }
    }
}

$bossEntries = foreach ($guid in $bossGuids) {
    if (-not $guidToLine.ContainsKey($guid)) { throw "Boss guid missing: $guid" }
    $guidToLine[$guid]
}

$poolRange = Find-ComponentRange 'Assembly-CSharp::PoolManager'
Replace-ListInRange $poolRange 'bossPrefabs' $bossEntries

$gameRange = Find-ComponentRange 'Assembly-CSharp::GameManager'
Replace-ListInRange $gameRange 'bossPortraitPrefabs' $bossEntries

$spawnerRange = Find-ComponentRange 'Assembly-CSharp::Spawner'
$spawnDataStart = -1
for ($i = $spawnerRange.Start; $i -lt $spawnerRange.End; $i++) {
    if ($lines[$i] -eq '  spawnData:') { $spawnDataStart = $i; break }
}
if ($spawnDataStart -lt 0) { throw 'spawnData not found in Spawner' }

$bossSpawnStart = $spawnDataStart + 1 + (36 * 3)
$bossSpawnEnd = $bossSpawnStart
while ($bossSpawnEnd -lt $spawnerRange.End -and $lines[$bossSpawnEnd] -match '^\s+- isBoss: 1') {
    $bossSpawnEnd += 3
}
$removeBossSpawn = $bossSpawnEnd - $bossSpawnStart
if ($removeBossSpawn -gt 0) { $lines.RemoveRange($bossSpawnStart, $removeBossSpawn) }

$newBossSpawn = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $bossGuids.Count; $i++) {
    $newBossSpawn.Add('  - isBoss: 1')
    $newBossSpawn.Add('    spawnTime: 0.2')
    $newBossSpawn.Add("    prefabIndex: $i")
}
for ($j = $newBossSpawn.Count - 1; $j -ge 0; $j--) {
    $lines.Insert($bossSpawnStart, $newBossSpawn[$j])
}

function New-WaveBlock([bool]$isBoss, [int[]]$bossIndexes, [object[]]$enemyLines) {
    $block = New-Object System.Collections.Generic.List[string]
    $block.Add('    - isBossWave: ' + [int]$isBoss)
    if ($isBoss -and $bossIndexes.Length -gt 0) {
        $block.Add('      bossSpawnIndexes:' + (Format-UnityIntArray $bossIndexes))
    } else {
        $block.Add('      bossSpawnIndexes: ')
    }
    $block.Add('      enemies:')
    foreach ($enemy in $enemyLines) {
        $block.Add('      - spawnDataIndex: ' + $enemy.Index)
        $block.Add('        spawnCount: ' + $enemy.Count)
    }
    return $block
}

function New-Enemy([int]$Index, [int]$Count) {
    return [pscustomobject]@{ Index = $Index; Count = $Count }
}

$stageBossIndexes = @(@(36,37), @(38,39), @(40,41,42), @(43,44,45), @(46,47), @(48,49), @(50,51))
$stageConfigs = @(
    @{ A = 0; B = 1; Elite = -1 }
    @{ A = 2; B = 3; Elite = -1 }
    @{ A = 4; B = 6; Elite = -1 }
    @{ A = 7; B = 9; Elite = 9 }
    @{ A = 10; B = 13; Elite = 13 }
    @{ A = 14; B = 20; Elite = 19 }
    @{ A = 21; B = 35; Elite = 31 }
)

$stageRange = Find-ComponentRange 'Assembly-CSharp::StageManager'

$stagesStart = -1
for ($i = $stageRange.Start; $i -lt $stageRange.End; $i++) {
    if ($lines[$i] -eq '  stages:') { $stagesStart = $i; break }
}
$stagesEnd = $stagesStart + 1
while ($stagesEnd -lt $stageRange.End -and $lines[$stagesEnd] -match '^\s+- \{fileID: ') { $stagesEnd++ }
$lines.RemoveRange($stagesStart, $stagesEnd - $stagesStart)
$stageMaps = @(
    '  stages:'
    '  - {fileID: 779264545}'
    '  - {fileID: 588080940}'
    '  - {fileID: 621035515}'
    '  - {fileID: 1101518933}'
    '  - {fileID: 247920757}'
    '  - {fileID: 1113449814}'
    '  - {fileID: 1625262798}'
)
for ($j = $stageMaps.Count - 1; $j -ge 0; $j--) {
    $lines.Insert($stagesStart + $j, $stageMaps[$j])
}

$stageRange = Find-ComponentRange 'Assembly-CSharp::StageManager'

$stageDatasStart = -1
for ($i = $stageRange.Start; $i -lt $stageRange.End; $i++) {
    if ($lines[$i] -eq '  stageDatas:') { $stageDatasStart = $i; break }
}
if ($stageDatasStart -lt 0) { throw 'stageDatas not found in StageManager' }

$stageDatasEnd = $stageDatasStart + 1
while ($stageDatasEnd -lt $stageRange.End -and $lines[$stageDatasEnd] -notmatch '^  endingAfterStageNumber:') {
    $stageDatasEnd++
}
$lines.RemoveRange($stageDatasStart, $stageDatasEnd - $stageDatasStart)

$newStageDatas = New-Object System.Collections.Generic.List[string]
$newStageDatas.Add('  stageDatas:')
for ($stage = 0; $stage -lt 7; $stage++) {
    $cfg = $stageConfigs[$stage]
    $newStageDatas.Add('  - waves:')
    for ($wave = 0; $wave -lt 5; $wave++) {
        if ($wave -eq 4) {
            if ($cfg.Elite -ge 0) {
                $enemies = @(New-Enemy $cfg.A 5; New-Enemy $cfg.Elite 5)
            } else {
                $enemies = @(New-Enemy $cfg.A 5; New-Enemy $cfg.B 5)
            }
            foreach ($line in (New-WaveBlock $true $stageBossIndexes[$stage] $enemies)) { $newStageDatas.Add($line) }
            continue
        }
        switch ($wave) {
            0 { $enemies = @(New-Enemy $cfg.A 1) }
            1 { $enemies = @(New-Enemy $cfg.A 5) }
            2 { $enemies = @(New-Enemy $cfg.A 10) }
            3 {
                if ($cfg.Elite -ge 0) {
                    $enemies = @(New-Enemy $cfg.A 5; New-Enemy $cfg.Elite 3)
                } else {
                    $enemies = @(New-Enemy $cfg.A 5; New-Enemy $cfg.B 5)
                }
            }
        }
        foreach ($line in (New-WaveBlock $false @() $enemies)) { $newStageDatas.Add($line) }
    }
}
for ($j = $newStageDatas.Count - 1; $j -ge 0; $j--) {
    $lines.Insert($stageDatasStart + $j, $newStageDatas[$j])
}

$stageRange = Find-ComponentRange 'Assembly-CSharp::StageManager'
for ($i = $stageRange.Start; $i -lt $stageRange.End; $i++) {
    if ($lines[$i] -match '^  endingAfterStageNumber: ') {
        $lines[$i] = '  endingAfterStageNumber: 7'
        break
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host '[ApplyBossStageWaves] Done.'
