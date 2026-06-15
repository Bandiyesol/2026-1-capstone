# bossPrefabs / bossPortraitPrefabs 줄 끝 \r 제거 (Unity가 32칸 null 배열로 파싱하는 문제 수정)
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)

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

function Get-PrefabLine([string]$content, [string]$guid) {
    $m = [regex]::Match($content, "(?m)^  - \{fileID: (\d+), guid: $guid, type: 3\}")
    if (-not $m.Success) { throw "Prefab line not found for guid $guid" }
    return "  - {fileID: $($m.Groups[1].Value), guid: $guid, type: 3}"
}

$bossLines = $bossGuids | ForEach-Object { Get-PrefabLine $text $_ }
$bossBlock = "  bossPrefabs:`n" + ($bossLines -join "`n") + "`n"
$portraitBlock = "  bossPortraitPrefabs:`n" + ($bossLines -join "`n") + "`n"

function Replace-BlockAfterAnchor([ref]$content, [string]$anchor, [string]$key, [string]$nextKey, [string]$newBlock) {
    $pos = $content.Value.IndexOf($anchor)
    if ($pos -lt 0) { throw "Anchor not found: $anchor" }
    $keyPos = $content.Value.IndexOf("`n  $key`:", $pos)
    if ($keyPos -lt 0) { throw "$key not found after $anchor" }
    $keyPos += 1
    $nextPos = $content.Value.IndexOf("`n  $nextKey`:", $keyPos)
    if ($nextPos -lt 0) { throw "$nextKey not found after $key" }
    $content.Value = $content.Value.Substring(0, $keyPos) + $newBlock + $content.Value.Substring($nextPos + 1)
}

Replace-BlockAfterAnchor ([ref]$text) '  m_EditorClassIdentifier: Assembly-CSharp::PoolManager' 'bossPrefabs' 'bossBulletPrefabs' $bossBlock
Replace-BlockAfterAnchor ([ref]$text) '  m_EditorClassIdentifier: Assembly-CSharp::GameManager' 'bossPortraitPrefabs' 'mainMenuRoot' $portraitBlock

# 혹시 남은 bossPrefabs 줄 끝 \r 정리
$text = $text -replace '(?m)(^  - \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\})\r+$', '$1'

[IO.File]::WriteAllText($scenePath, $text)
Write-Host '[FixBossPrefabLineEndings] bossPrefabs + bossPortraitPrefabs normalized (16 entries each).'
