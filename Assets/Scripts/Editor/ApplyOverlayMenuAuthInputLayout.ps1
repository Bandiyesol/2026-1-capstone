# Center auth input text vertically and fix text viewport padding
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [IO.File]::ReadAllLines($scenePath)

$textAreaRects = [System.Collections.Generic.HashSet[string]]@(
    '1606599510', '383050223', '635587260', '1761371081', '946107204',
    '1410752601', '1853615881', '1403191753', '901412514'
)
$textIds = [System.Collections.Generic.HashSet[string]]@(
    '1540389225', '787459147', '1486809014', '631047989',
    '1992459218', '1856541954', '1325736237', '1756240783',
    '311270289', '1931077766', '485683478', '1595395164',
    '1708256730', '1238799619', '684582649', '1705295950',
    '1044822793', '160223200'
)

$patched = 0
$activeId = $null
$activeKind = $null

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!224 &(\d+)$') {
        $id = $Matches[1]
        $activeId = if ($textAreaRects.Contains($id)) { $id } else { $null }
        $activeKind = if ($activeId) { 'viewport' } else { $null }
        continue
    }

    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $id = $Matches[1]
        $activeId = $null
        $activeKind = $null
        if ($textIds.Contains($id)) { $activeId = $id; $activeKind = 'text' }
        continue
    }

    if ($null -eq $activeId) { continue }

    if ($activeKind -eq 'viewport') {
        if ($lines[$i] -match '^\s+m_AnchoredPosition: ') {
            $lines[$i] = '  m_AnchoredPosition: {x: 0, y: 0}'; $patched++
        }
        elseif ($lines[$i] -match '^\s+m_SizeDelta: ') {
            $lines[$i] = '  m_SizeDelta: {x: -24, y: -12}'; $patched++
        }
        elseif ($lines[$i] -match '^\s+m_Padding: ') {
            $lines[$i] = '  m_Padding: {x: 8, y: 4, z: 8, w: 4}'; $patched++
        }
    }
    elseif ($activeKind -eq 'text') {
        if ($lines[$i] -match '^\s+m_VerticalAlignment: ') {
            $lines[$i] = '  m_VerticalAlignment: 512'; $patched++
        }
        elseif ($lines[$i] -match '^\s+m_HorizontalAlignment: ') {
            $lines[$i] = '  m_HorizontalAlignment: 1'; $patched++
        }
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "Auth input layout patched: $patched line updates"
