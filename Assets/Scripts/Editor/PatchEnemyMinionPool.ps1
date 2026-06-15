# PoolManager.enemyPrefabs: Enemy01-36 + boss minion slots (indices 36-50) in summon-index order
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$text = [IO.File]::ReadAllText($scenePath)

function Get-GuidFromMeta([string]$metaPath) {
    if (-not (Test-Path $metaPath)) { throw "Missing meta: $metaPath" }
    $m = [regex]::Match([IO.File]::ReadAllText($metaPath), '(?m)^guid: ([0-9a-f]{32})')
    if (-not $m.Success) { throw "No guid in $metaPath" }
    return $m.Groups[1].Value
}

function Get-PrefabLine([string]$content, [string]$guid) {
    $m = [regex]::Match($content, "(?m)^  - \{fileID: (\d+), guid: $guid, type: 3\}")
    if (-not $m.Success) { throw "Prefab line not found for guid $guid" }
    return "  - {fileID: $($m.Groups[1].Value), guid: $guid, type: 3}"
}

$enemyRoot = Join-Path $projectRoot 'Assets/Prefabs/Characters/Enemy'
$guids = New-Object System.Collections.Generic.List[string]
for ($i = 1; $i -le 36; $i++) {
    $guids.Add((Get-GuidFromMeta (Join-Path $enemyRoot ("Enemy{0:D2}.prefab.meta" -f $i))))
}

$minionNames = @(
    'Enemy_PumpkinKing.prefab'
    'Enemy_CaveRex.prefab'
    'Enemy_DeepSeaMutant.prefab'
    'Enemy_VolcanoPumpkin.prefab'
    'Enemy_LavaEarthDragon.prefab'
    'Enemy_FrostWolfBoss.prefab'
    'Enemy_IceGiantBoss_A.prefab'
    'Enemy_IceGiantBoss_B.prefab'
    'Enemy_IceGiantBoss_C.prefab'
    'Enemy_IceGiantBoss_D.prefab'
    'Enemy_IceGiantBoss_E.prefab'
    'Enemy_UndeadGuard.prefab'
    'Enemy_ImmortalUndeadBoss_A.prefab'
    'Enemy_ImmortalUndeadBoss_B.prefab'
    'Enemy_ImmortalUndeadBoss_C.prefab'
)
foreach ($name in $minionNames) {
    $guids.Add((Get-GuidFromMeta (Join-Path $enemyRoot "$name.meta")))
}

$lines = $guids | ForEach-Object { Get-PrefabLine $text $_ }
$block = "  enemyPrefabs:`n" + ($lines -join "`n") + "`n"

$anchor = '  m_EditorClassIdentifier: Assembly-CSharp::PoolManager'
$pos = $text.IndexOf($anchor)
if ($pos -lt 0) { throw 'PoolManager anchor not found' }
$keyPos = $text.IndexOf("`n  enemyPrefabs:", $pos)
if ($keyPos -lt 0) { throw 'enemyPrefabs not found' }
$keyPos += 1
$nextPos = $text.IndexOf("`n  bossPrefabs:", $keyPos)
if ($nextPos -lt 0) { throw 'bossPrefabs not found after enemyPrefabs' }
$text = $text.Substring(0, $keyPos) + $block + $text.Substring($nextPos + 1)

[IO.File]::WriteAllText($scenePath, $text)
Write-Host "[PatchEnemyMinionPool] enemyPrefabs rebuilt ($($guids.Count) entries: 36 regular + 15 minion)."
