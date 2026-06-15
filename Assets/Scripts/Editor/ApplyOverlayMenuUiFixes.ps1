# Button sizes, slider fill direction, dropdown list styling, skip/start tweaks
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [IO.File]::ReadAllLines($scenePath)

$panel = '{fileID: -8136509021760896224, guid: 18859343a5bfe8649b95ffbd2b5b7f4b, type: 3}'
$patched = 0

$rectMap = @{
    '477589353'  = @('  m_AnchoredPosition: {x: -400, y: -310}', '  m_SizeDelta: {x: 340, y: 96}')
    '1682661287' = @('  m_AnchoredPosition: {x: -50, y: -310}', '  m_SizeDelta: {x: 340, y: 96}')
    '1436175891' = @('  m_AnchoredPosition: {x: 280, y: -310}', '  m_SizeDelta: {x: 340, y: 96}')
    '1723534091' = @('  m_AnchoredPosition: {x: -360, y: 180}', '  m_SizeDelta: {x: 340, y: 96}')
    '1525049241' = @('  m_AnchoredPosition: {x: 0, y: 180}', '  m_SizeDelta: {x: 340, y: 96}')
    '1949472873' = @('  m_AnchoredPosition: {x: 360, y: 180}', '  m_SizeDelta: {x: 340, y: 96}')
    '1024599854' = @('  m_AnchoredPosition: {x: 210, y: -430}', '  m_SizeDelta: {x: 340, y: 96}')
    '2135152966' = @('  m_AnchoredPosition: {x: -210, y: -430}', '  m_SizeDelta: {x: 340, y: 96}')
    '1854102973' = @('  m_AnchoredPosition: {x: -220, y: 120}', '  m_SizeDelta: {x: 340, y: 96}')
    '1867738619' = @('  m_AnchoredPosition: {x: -220, y: 120}', '  m_SizeDelta: {x: 340, y: 96}')
    '89634418'   = @('  m_AnchoredPosition: {x: 0, y: 149}', '  m_SizeDelta: {x: 340, y: 96}')
}

$activeRect = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!224 &(\d+)$') {
        $activeRect = $Matches[1]
        continue
    }
    if ($null -ne $activeRect -and $rectMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_AnchoredPosition: ') {
            $lines[$i] = $rectMap[$activeRect][0]; $patched++
        }
        elseif ($lines[$i] -match '^\s+m_SizeDelta: ') {
            $lines[$i] = $rectMap[$activeRect][1]; $patched++; $activeRect = $null
        }
    }
}

$sliderFillIds = @('775372620', '962496253')
$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $activeId = if ($sliderFillIds -contains $Matches[1]) { $Matches[1] } else { $null }
        continue
    }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_Type: ') { $lines[$i] = '  m_Type: 3'; $patched++ }
    elseif ($lines[$i] -match '^\s+m_FillMethod: ') { $lines[$i] = '  m_FillMethod: 0'; $patched++ }
    elseif ($lines[$i] -match '^\s+m_FillOrigin: ') { $lines[$i] = '  m_FillOrigin: 0'; $patched++ }
}

$dropdownPanelIds = @(
    '2082178653', '429347562',
    '976252722', '929304898',
    '1226631230', '1718883106'
)
$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $activeId = if ($dropdownPanelIds -contains $Matches[1]) { $Matches[1] } else { $null }
        continue
    }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_Color: ') {
        $lines[$i] = '  m_Color: {r: 0.98, g: 0.96, b: 0.93, a: 1}'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Sprite: ') {
        $lines[$i] = "  m_Sprite: $panel"; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Type: ') {
        $lines[$i] = '  m_Type: 1'; $patched++
    }
}

$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &1405834254$') { $activeId = '1405834254'; continue }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_Color: ') {
        $lines[$i] = '  m_Color: {r: 0.1, g: 0.08, b: 0.06, a: 1}'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_fontColor: ') {
        $lines[$i] = '  m_fontColor: {r: 0.1, g: 0.08, b: 0.06, a: 1}'; $patched++
    }
    elseif ($lines[$i] -match '^\s+rgba: ') {
        $lines[$i] = '    rgba: 4279505940'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_fontSize: ') {
        $lines[$i] = '  m_fontSize: 28'; $patched++; $activeId = $null
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "UI fixes patched: $patched line updates"
