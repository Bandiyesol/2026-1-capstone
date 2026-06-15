# Uniform auth input sizes, slider track fix (no silver cap), full dropdown styling
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [IO.File]::ReadAllLines($scenePath)

$panel = '{fileID: -8136509021760896224, guid: 18859343a5bfe8649b95ffbd2b5b7f4b, type: 3}'
$handle = '{fileID: 6724485060924360464, guid: 317e6d0fc161f5643a148f3c4d22cd6b, type: 3}'
$patched = 0

$rectMap = @{
    '350459578'  = @('  m_AnchoredPosition: {x: 0, y: -15}', '  m_SizeDelta: {x: 500, y: 64}')
    '427598822'  = @('  m_AnchoredPosition: {x: 0, y: -95}', '  m_SizeDelta: {x: 500, y: 64}')
    '1540312773' = @('  m_AnchoredPosition: {x: 0, y: -175}', '  m_SizeDelta: {x: 500, y: 64}')
    '130089725'  = @('  m_AnchoredPosition: {x: 0, y: -255}', '  m_SizeDelta: {x: 500, y: 64}')
    '843805449'  = @('  m_AnchoredPosition: {x: 0, y: -335}', '  m_SizeDelta: {x: 500, y: 64}')
    '1137530195' = @('  m_AnchoredPosition: {x: 100, y: 100}', '  m_SizeDelta: {x: 500, y: 64}')
    '2091469693' = @('  m_AnchoredPosition: {x: 100, y: 200}', '  m_SizeDelta: {x: 500, y: 64}')
    '1741326870' = @('  m_AnchoredPosition: {x: -28, y: 0}', '  m_SizeDelta: {x: 36, y: 36}')
    '1604282808' = @('  m_AnchoredPosition: {x: -28, y: 0}', '  m_SizeDelta: {x: 36, y: 36}')
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

# Slider Background: plain dark Panels_06 track (removes silver end caps from Horizontal Slidebar_01)
$sliderBgIds = @('203911044', '1368830286')
$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $activeId = if ($sliderBgIds -contains $Matches[1]) { $Matches[1] } else { $null }
        continue
    }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_Color: ') {
        $lines[$i] = '  m_Color: {r: 0.32, g: 0.26, b: 0.20, a: 0.85}'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Sprite: ') {
        $lines[$i] = "  m_Sprite: $panel"; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Type: ') {
        $lines[$i] = '  m_Type: 1'; $patched++
    }
}

# Dropdown closed box + arrow (integrated look, not separate gold button)
$dropdownRootIds = @('1137530197', '2091469695')
$dropdownArrowIds = @('1741326871', '1604282809')
$activeId = $null
$activeKind = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $id = $Matches[1]
        $activeId = $null; $activeKind = $null
        if ($dropdownRootIds -contains $id) { $activeId = $id; $activeKind = 'root' }
        elseif ($dropdownArrowIds -contains $id) { $activeId = $id; $activeKind = 'arrow' }
        continue
    }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_Color: ') {
        if ($activeKind -eq 'root') {
            $lines[$i] = '  m_Color: {r: 0.98, g: 0.96, b: 0.93, a: 1}'; $patched++
        }
        else {
            $lines[$i] = '  m_Color: {r: 0.72, g: 0.58, b: 0.28, a: 1}'; $patched++
        }
    }
    elseif ($lines[$i] -match '^\s+m_Sprite: ') {
        if ($activeKind -eq 'root') { $lines[$i] = "  m_Sprite: $panel" }
        else { $lines[$i] = "  m_Sprite: $handle" }
        $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Type: ') {
        $lines[$i] = if ($activeKind -eq 'root') { '  m_Type: 1' } else { '  m_Type: 0' }
        $patched++
    }
}

# Dropdown caption + item label text colors
$dropdownTextIds = @('2059207743', '1809011410', '606931207', '366479664')
$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $activeId = if ($dropdownTextIds -contains $Matches[1]) { $Matches[1] } else { $null }
        continue
    }
    if ($null -eq $activeId) { continue }
    if ($lines[$i] -match '^\s+m_fontColor: ') {
        $lines[$i] = '  m_fontColor: {r: 0.12, g: 0.10, b: 0.08, a: 1}'; $patched++
    }
    elseif ($lines[$i] -match '^\s+rgba: ') {
        $lines[$i] = '    rgba: 4279571738'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_fontSize: ') {
        $lines[$i] = '  m_fontSize: 24'; $patched++; $activeId = $null
    }
}

# Dropdown list: template backgrounds, items, scrollbars
$dropdownListImageIds = @(
    '2082178653', '429347562',
    '976252722', '929304898',
    '1226631230', '1718883106',
    '1137530197', '2091469695'
)
$activeId = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $activeId = if ($dropdownListImageIds -contains $Matches[1]) { $Matches[1] } else { $null }
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

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "Inputs/dropdowns/slider patch complete: $patched line updates"
