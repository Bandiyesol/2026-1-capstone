# Validates enemy/boss pools, spawnData, stage waves, and boss summon indices.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$scenePath = Join-Path $projectRoot 'Assets/Scenes/ProtoType_LTG.unity'
$enemyRoot = Join-Path $projectRoot 'Assets/Prefabs/Characters/Enemy'
$bossRoot = Join-Path $projectRoot 'Assets/Prefabs/Characters/Boss'
$text = [IO.File]::ReadAllText($scenePath)

function Get-GuidFromMeta([string]$path) {
    $m = [regex]::Match([IO.File]::ReadAllText($path), '(?m)^guid: ([0-9a-f]{32})')
    if (-not $m.Success) { throw "No guid: $path" }
    return $m.Groups[1].Value
}

function Guid-ToName([string]$guid, [hashtable]$map) {
    if ($map.ContainsKey($guid)) { return $map[$guid] }
    return "UNKNOWN($guid)"
}

# Build guid -> prefab name map
$guidToName = @{}
Get-ChildItem $enemyRoot -Filter '*.prefab.meta' | ForEach-Object {
    $guidToName[(Get-GuidFromMeta $_.FullName)] = $_.BaseName -replace '\.prefab$',''
}
Get-ChildItem $bossRoot -Filter '*.prefab.meta' | ForEach-Object {
    $guidToName[(Get-GuidFromMeta $_.FullName)] = $_.BaseName -replace '\.prefab$',''
}

function Parse-GuidList([string]$content, [string]$key) {
    $anchor = "  ${key}:"
    $pos = $content.IndexOf($anchor)
    if ($pos -lt 0) { throw "Missing $key" }
    $pos += $anchor.Length
    $lines = New-Object System.Collections.Generic.List[string]
    while ($pos -lt $content.Length) {
        if ($content[$pos] -eq "`r") { $pos++; continue }
        if ($content[$pos] -eq "`n") { $pos++; continue }
        if (-not $content.Substring($pos).StartsWith('  - ')) { break }
        $lineEnd = $content.IndexOf("`n", $pos)
        if ($lineEnd -lt 0) { $lineEnd = $content.Length }
        $line = $content.Substring($pos, $lineEnd - $pos)
        $m = [regex]::Match($line, 'guid: ([0-9a-f]{32})')
        if ($m.Success) { $lines.Add($m.Groups[1].Value) }
        $pos = $lineEnd + 1
    }
    return ,$lines.ToArray()
}

function Parse-SpawnData([string]$content) {
    $m = [regex]::Match($content, '(?ms)  m_EditorClassIdentifier: Assembly-CSharp::Spawner\r?\n  spawnPoint:[\s\S]*?  spawnData:\r?\n((?:  - isBoss: \d\r?\n    spawnTime: [\d.]+\r?\n    prefabIndex: \d+\r?\n)+)')
    if (-not $m.Success) { throw 'Spawner spawnData not found' }
    $entries = [regex]::Matches($m.Groups[1].Value, 'isBoss: (\d)[\s\S]*?prefabIndex: (\d+)')
    $result = @()
    foreach ($e in $entries) {
        $result += [pscustomobject]@{ isBoss = [int]$e.Groups[1].Value; prefabIndex = [int]$e.Groups[2].Value }
    }
    return ,$result
}

function Parse-StageDatas([string]$content) {
    $m = [regex]::Match($content, '(?ms)  m_EditorClassIdentifier: Assembly-CSharp::StageManager\r?\n[\s\S]*?  stageDatas:\r?\n(.*?)  endingAfterStageNumber:')
    if (-not $m.Success) { throw 'stageDatas not found' }
    $block = $m.Groups[1].Value
    $stageChunks = [regex]::Split($block, '(?m)^  - waves:')
    $stages = New-Object System.Collections.Generic.List[object]
    for ($i = 1; $i -lt $stageChunks.Length; $i++) {
        $chunk = $stageChunks[$i]
        $waves = New-Object System.Collections.Generic.List[object]
        $waveMatches = [regex]::Matches($chunk, '(?ms)- isBossWave: (\d)\r?\n      bossSpawnIndexes:(.*?)\r?\n      enemies:(.*?)(?=\r?\n    - isBossWave:|\r?\n  - waves:|\z)')
        foreach ($wm in $waveMatches) {
            $bossIdx = @()
            if ($wm.Groups[2].Value.Trim()) {
                $bossIdx = [regex]::Matches($wm.Groups[2].Value, '- (\d+)') | ForEach-Object { [int]$_.Groups[1].Value }
            }
            $enemies = @()
            foreach ($em in [regex]::Matches($wm.Groups[3].Value, 'spawnDataIndex: (\d+)\r?\n        spawnCount: (\d+)')) {
                $enemies += [pscustomobject]@{ spawnDataIndex = [int]$em.Groups[1].Value; spawnCount = [int]$em.Groups[2].Value }
            }
            $waves.Add([pscustomobject]@{
                isBossWave = [int]$wm.Groups[1].Value -eq 1
                bossSpawnIndexes = $bossIdx
                enemies = $enemies
            })
        }
        $stages.Add($waves.ToArray())
    }
    return ,$stages.ToArray()
}

$issues = New-Object System.Collections.Generic.List[string]
$ok = New-Object System.Collections.Generic.List[string]

$poolAnchor = '  m_EditorClassIdentifier: Assembly-CSharp::PoolManager'
$poolPos = $text.IndexOf($poolAnchor)
$poolSlice = $text.Substring($poolPos)
$enemyGuids = Parse-GuidList $poolSlice 'enemyPrefabs'
$bossGuids = Parse-GuidList $poolSlice 'bossPrefabs'
$spawnData = Parse-SpawnData $text
$stageDatas = Parse-StageDatas $text

$biomeNames = @('Forest','Cave','Sea','Lava','Snow','Desert','Void')
$expectedBossPaths = @(
    @('PumpkinKing','HeavenEyeBoss')
    @('UndergroundDrillerBoss','CaveRex')
    @('DeepSeaMutant','DrownedSpiritBoss','StormDragonBoss')
    @('LavaTyrano','VolcanoPumpkin Core','LavaEarthDragon')
    @('FrostWolfBoss Core','IceGiant')
    @('DesertGuardianBoss','ImmortalUndeadBoss')
    @('AbyssalPredator','VoidCalamityBoss')
)

$expectedMinions = @(
    'Enemy_PumpkinKing','Enemy_CaveRex','Enemy_DeepSeaMutant','Enemy_VolcanoPumpkin','Enemy_LavaEarthDragon',
    'Enemy_FrostWolfBoss','Enemy_IceGiantBoss_A','Enemy_IceGiantBoss_B','Enemy_IceGiantBoss_C',
    'Enemy_IceGiantBoss_D','Enemy_IceGiantBoss_E','Enemy_UndeadGuard',
    'Enemy_ImmortalUndeadBoss_A','Enemy_ImmortalUndeadBoss_B','Enemy_ImmortalUndeadBoss_C'
)

Write-Host '=== Monster Setup Validation ===' -ForegroundColor Cyan
Write-Host "Enemy pool: $($enemyGuids.Length) entries (expected 51)"
Write-Host "Boss pool: $($bossGuids.Length) entries (expected 17 with VoidApostle)"
Write-Host "SpawnData: $($spawnData.Length) entries (expected 52 = 36 normal + 16 stage bosses)"
Write-Host ''

# 1) Enemy01-36 order
for ($i = 0; $i -lt 36; $i++) {
    $expectedGuid = Get-GuidFromMeta (Join-Path $enemyRoot ("Enemy{0:D2}.prefab.meta" -f ($i + 1)))
    $actualName = Guid-ToName $enemyGuids[$i] $guidToName
    if ($enemyGuids[$i] -ne $expectedGuid) {
        $issues.Add("Enemy pool[$i]: expected Enemy{0:D2}, got $actualName" -f ($i + 1))
    }
}

# 2) Minion pool 36-50
for ($i = 0; $i -lt $expectedMinions.Length; $i++) {
    $poolIdx = 36 + $i
    $expectedGuid = Get-GuidFromMeta (Join-Path $enemyRoot "$($expectedMinions[$i]).prefab.meta")
    $actualName = Guid-ToName $enemyGuids[$poolIdx] $guidToName
    if ($enemyGuids[$poolIdx] -ne $expectedGuid) {
        $issues.Add("Minion pool[$poolIdx]: expected $($expectedMinions[$i]), got $actualName")
    } else {
        $ok.Add("Minion pool[$poolIdx] = $($expectedMinions[$i])")
    }
}

# 3) Boss pool order (first-seen biome order)
$expectedBossGuids = @(
    'PumpkinKing.prefab','HeavenEyeBoss.prefab','UndergroundDrillerBoss.prefab','CaveRex.prefab',
    'DeepSeaMutant.prefab','DrownedSpiritBoss.prefab','StormDragonBoss.prefab','LavaTyrano.prefab',
    'VolcanoPumpkin Core.prefab','LavaEarthDragon.prefab','FrostWolfBoss Core.prefab','IceGiant.prefab',
    'DesertGuardianBoss.prefab','ImmortalUndeadBoss.prefab','AbyssalPredator.prefab','VoidCalamityBoss.prefab',
    'VoidApostle.prefab'
) | ForEach-Object { Get-GuidFromMeta (Join-Path $bossRoot "$_.meta") }

for ($i = 0; $i -lt $expectedBossGuids.Length; $i++) {
    $actualName = Guid-ToName $bossGuids[$i] $guidToName
    if ($bossGuids[$i] -ne $expectedBossGuids[$i]) {
        $issues.Add("Boss pool[$i]: expected $($expectedBossGuids[$i] | ForEach-Object { Guid-ToName $_ $guidToName }), got $actualName")
    }
}

# 4) SpawnData mapping
for ($i = 0; $i -lt 36; $i++) {
    $sd = $spawnData[$i]
    if ($sd.isBoss -ne 0 -or $sd.prefabIndex -ne $i) {
        $issues.Add("spawnData[$i]: expected normal prefabIndex=$i, got isBoss=$($sd.isBoss) prefabIndex=$($sd.prefabIndex)")
    }
}
$stageBossGuids = $expectedBossGuids[0..15]

for ($i = 0; $i -lt $stageBossGuids.Length; $i++) {
    $idx = 36 + $i
    $sd = $spawnData[$idx]
    if ($sd.isBoss -ne 1 -or $sd.prefabIndex -ne $i) {
        $issues.Add("spawnData[$idx]: expected boss prefabIndex=$i, got isBoss=$($sd.isBoss) prefabIndex=$($sd.prefabIndex)")
    }
}
if ($spawnData.Length -ne 36 + $stageBossGuids.Length) {
    $issues.Add("spawnData length $($spawnData.Length) expected $(36 + $stageBossGuids.Length)")
}

# 5) Stage waves
$expectedStages = @(
    @{ typeA = 0; typeB = 1; elite = -1; bosses = @(36,37) }
    @{ typeA = 2; typeB = 3; elite = -1; bosses = @(38,39) }
    @{ typeA = 4; typeB = 6; elite = 6; bosses = @(40,41,42) }
    @{ typeA = 7; typeB = 9; elite = 9; bosses = @(43,44,45) }
    @{ typeA = 10; typeB = 13; elite = 13; bosses = @(46,47) }
    @{ typeA = 14; typeB = 19; elite = 19; bosses = @(48,49) }
    @{ typeA = 21; typeB = 35; elite = 31; bosses = @(50,51) }
)

for ($s = 0; $s -lt $expectedStages.Length; $s++) {
    $exp = $expectedStages[$s]
    $waves = $stageDatas[$s]
    if ($waves.Length -ne 5) { $issues.Add("Stage $($s+1): expected 5 waves, got $($waves.Length)"); continue }

    $bossWave = $waves[4]
    $expBoss = $exp.bosses
    if (($bossWave.bossSpawnIndexes | ForEach-Object { $_ }) -join ',' -ne ($expBoss -join ',')) {
        $issues.Add("Stage $($s+1) bossSpawnIndexes: expected [$($expBoss -join ',')], got [$($bossWave.bossSpawnIndexes -join ',')]")
    } else {
        $bossNames = $expBoss | ForEach-Object {
            $sd = $spawnData[$_]
            Guid-ToName $bossGuids[$sd.prefabIndex] $guidToName
        }
        $ok.Add("Stage $($s+1) ($($biomeNames[$s])) bosses: $($bossNames -join ' / ')")
    }

    # Validate wave enemy indices are in biome range
    foreach ($w in $waves) {
        foreach ($e in $w.enemies) {
            $pi = $spawnData[$e.spawnDataIndex].prefabIndex
            $name = Guid-ToName $enemyGuids[$pi] $guidToName
            if ($pi -lt $exp.typeA -or ($pi -ne $exp.elite -and $pi -gt $exp.typeB -and $pi -lt 36)) {
                if ($pi -ge 36) {
                    $issues.Add("Stage $($s+1) wave uses minion/boss pool index $pi ($name) in regular wave spawnData[$($e.spawnDataIndex)]")
                } elseif ($exp.elite -ge 0 -and $pi -eq $exp.elite) {
                    # elite ok
                } elseif ($pi -lt $exp.typeA -or $pi -gt $exp.typeB) {
                    $issues.Add("Stage $($s+1): spawnDataIndex $($e.spawnDataIndex) -> enemy pool[$pi] ($name) outside biome range [$($exp.typeA)..$($exp.typeB)] elite=$($exp.elite)")
                }
            }
        }
    }
}

# 6) Boss summon index vs minion pool
$summonChecks = @(
    @{ file = 'PumpkinKing.prefab'; field = 'summonMonsterIndex'; expected = 36; minion = 'Enemy_PumpkinKing' }
    @{ file = 'CaveRex.prefab'; field = 'summonMonsterIndex'; expected = 37; minion = 'Enemy_CaveRex' }
    @{ file = 'DeepSeaMutant.prefab'; field = 'summonEnemyIndex'; expected = 38; minion = 'Enemy_DeepSeaMutant' }
    @{ file = 'VolcanoPumpkin Core.prefab'; field = 'summonEnemyIndex'; expected = 39; minion = 'Enemy_VolcanoPumpkin' }
    @{ file = 'LavaEarthDragon.prefab'; field = 'summonMonsterIndex'; expected = 40; minion = 'Enemy_LavaEarthDragon' }
    @{ file = 'FrostWolfBoss Core.prefab'; field = 'summonEnemyIndex'; expected = 41; minion = 'Enemy_FrostWolfBoss' }
    @{ file = 'ImmortalUndeadBoss.prefab'; field = 'guardEnemyIndex'; expected = 47; minion = 'Enemy_UndeadGuard' }
    @{ file = 'ImmortalUndeadBoss.prefab'; field = 'summonEnemyIndex'; expected = 48; minion = 'Enemy_ImmortalUndeadBoss_A' }
)

foreach ($check in $summonChecks) {
    $prefabText = [IO.File]::ReadAllText((Join-Path $bossRoot $check.file))
    $m = [regex]::Match($prefabText, "$($check.field): (\d+)")
    if (-not $m.Success) { $issues.Add("$($check.file): missing $($check.field)"); continue }
    $val = [int]$m.Groups[1].Value
    if ($val -ne $check.expected) {
        $issues.Add("$($check.file) $($check.field)=$val, expected $($check.expected) -> $($check.minion)")
    } else {
        $actualName = Guid-ToName $enemyGuids[$val] $guidToName
        if ($actualName -ne $check.minion) {
            $issues.Add("$($check.file) $($check.field)=$val points to pool[$val]=$actualName, expected $($check.minion)")
        } else {
            $ok.Add("Summon $($check.file) -> pool[$val]=$actualName")
        }
    }
}

# IceGiant summon list
$iceText = [IO.File]::ReadAllText((Join-Path $bossRoot 'IceGiant.prefab'))
$iceIndexes = [regex]::Matches($iceText, '(?m)^  - (\d+)') | ForEach-Object { [int]$_.Groups[1].Value }
if ($iceIndexes.Count -eq 0) {
    $hex = [regex]::Match($iceText, 'summonEnemyIndexes: ([0-9a-f]+)')
    if ($hex.Success) { $issues.Add("IceGiant summonEnemyIndexes still hex blob: $($hex.Groups[1].Value)") }
    else { $issues.Add('IceGiant summonEnemyIndexes missing') }
} else {
    $expectedIce = 42..46
    if (($iceIndexes -join ',') -ne ($expectedIce -join ',')) {
        $issues.Add("IceGiant summon indexes [$($iceIndexes -join ',')] expected [42,43,44,45,46]")
    } else {
        foreach ($idx in $iceIndexes) {
            $ok.Add("IceGiant summon pool[$idx]=$(Guid-ToName $enemyGuids[$idx] $guidToName)")
        }
    }
}

# VoidCalamity void minions + apostle pool
$voidText = [IO.File]::ReadAllText((Join-Path $bossRoot 'VoidCalamityBoss.prefab'))
if ($voidText -match '(?m)^  voidMinionIndexes: [0-9a-f]{8,}') {
    $issues.Add('VoidCalamity voidMinionIndexes is hex blob (expected YAML list 21-35)')
} else {
    $block = [regex]::Match($voidText, '(?ms)voidMinionIndexes:(.*?)(?:\r?\n  voidSummonBaseInterval:)')
    if ($block.Success) {
        $voidIdx = [regex]::Matches($block.Groups[1].Value, '- (\d+)') | ForEach-Object { [int]$_.Groups[1].Value }
        $expectedVoid = 21..35
        if (($voidIdx -join ',') -ne ($expectedVoid -join ',')) {
            $issues.Add("VoidCalamity voidMinionIndexes [$($voidIdx -join ',')] expected [21..35] (Enemy22-36)")
        } else {
            $ok.Add('VoidCalamity voidMinionIndexes 21-35 (Void stage enemies)')
        }
    } else {
        $issues.Add('VoidCalamity voidMinionIndexes block missing')
    }
}

$m = [regex]::Match($voidText, 'apostlePoolIndex: (\d+)')
if ($m.Success) {
    $apIdx = [int]$m.Groups[1].Value
    if ($apIdx -ge $bossGuids.Length) {
        $issues.Add("VoidCalamity apostlePoolIndex=$apIdx out of boss pool range (0..$($bossGuids.Length - 1))")
    } else {
        $apName = Guid-ToName $bossGuids[$apIdx] $guidToName
        if ($apName -ne 'VoidApostle') {
            $issues.Add("VoidCalamity apostlePoolIndex=$apIdx -> $apName, expected VoidApostle")
        } else {
            $ok.Add("VoidCalamity apostlePoolIndex=$apIdx -> VoidApostle")
        }
    }
}

# 7) Hex blob remnants
if ($text -match 'bossSpawnIndexes: [0-9a-f]{8,}') { $issues.Add('Scene still has hex bossSpawnIndexes blob') }
if ($text -match 'gimmickIndexes: [0-9a-f]{8,}') { $issues.Add('Scene still has hex gimmickIndexes blob') }

Write-Host "--- OK ($($ok.Count)) ---" -ForegroundColor Green
$ok | Select-Object -First 30 | ForEach-Object { Write-Host "  OK $_" }
if ($ok.Count -gt 30) { Write-Host "  ... and $($ok.Count - 30) more" }

Write-Host ""
if ($issues.Count -eq 0) {
    Write-Host 'ALL CHECKS PASSED' -ForegroundColor Green
    exit 0
}

Write-Host "--- ISSUES ($($issues.Count)) ---" -ForegroundColor Red
$issues | ForEach-Object { Write-Host "  FAIL $_" -ForegroundColor Red }
exit 1
