# Auth panel button sizes (login / signup / forgot password)
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [System.Collections.Generic.List[string]]([IO.File]::ReadAllLines($scenePath))
$patched = 0

$authButtonSize = '  m_SizeDelta: {x: 340, y: 96}'
$wideButtonSize = '  m_SizeDelta: {x: 400, y: 96}'

$rectMap = @{
    '101719391'  = @('  m_AnchoredPosition: {x: 270, y: -135}', $authButtonSize)
    '477589353'  = @('  m_AnchoredPosition: {x: -400, y: -310}', $authButtonSize)
    '1682661287' = @('  m_AnchoredPosition: {x: -50, y: -310}', $authButtonSize)
    '1436175891' = @('  m_AnchoredPosition: {x: 280, y: -310}', $authButtonSize)
    '2135152966' = @('  m_AnchoredPosition: {x: -210, y: -430}', $authButtonSize)
    '1024599854' = @('  m_AnchoredPosition: {x: 210, y: -430}', $authButtonSize)
    '1923796080' = @('  m_AnchoredPosition: {x: -190, y: -210}', $wideButtonSize)
    '296524994'  = @('  m_AnchoredPosition: {x: 190, y: -210}', $authButtonSize)
    '89634418'   = @('  m_AnchoredPosition: {x: 0, y: 149}', $authButtonSize)
    '1723534091' = @('  m_AnchoredPosition: {x: -360, y: 180}', $authButtonSize)
    '1525049241' = @('  m_AnchoredPosition: {x: 0, y: 180}', $authButtonSize)
    '1949472873' = @('  m_AnchoredPosition: {x: 360, y: 180}', $authButtonSize)
    '871241871'  = @('  m_AnchoredPosition: {x: -190, y: -100}', $authButtonSize)
    '2013920909' = @('  m_AnchoredPosition: {x: 190, y: -100}', $authButtonSize)
    '1726722688' = @('  m_AnchoredPosition: {x: 0, y: 52}', $authButtonSize)
}

$stretchRectMap = @{
    '1259152604' = @(
        '  m_AnchorMin: {x: 0, y: 0}',
        '  m_AnchorMax: {x: 1, y: 1}',
        '  m_AnchoredPosition: {x: 0, y: 0}',
        '  m_SizeDelta: {x: 0, y: 0}',
        '  m_offsetMin: {x: 24, y: 128}',
        '  m_offsetMax: {x: -24, y: -72}'
    )
}

$panelLabelPadMap = @{
    '1405834253' = '44'
    '263836094'  = '44'
    '316402669'  = '44'
    '1448353812' = '44'
    '1985567344' = '44'
    '1009131637' = '44'
    '1731985547' = '44'
}

$labelPadMap = @{
    '949539717'  = @('44', '44')
    '454707400'  = @('44', '44')
    '1584350359' = @('44', '44')
    '1564761438' = @('44', '44')
    '1174135441' = @('44', '44')
    '168952647'  = @('44', '44')
    '1785478476' = @('52', '52')
    '458637494'  = @('44', '44')
}

function Set-LabelPadding {
    param([int]$Index, [string]$HorizontalPad)
    $lines[$Index] = '  m_offsetMin: {x: ' + $HorizontalPad + ', y: 8}'
    if ($Index + 1 -lt $lines.Count -and $lines[$Index + 1] -match '^\s+m_offsetMax: ') {
        $lines[$Index + 1] = '  m_offsetMax: {x: -' + $HorizontalPad + ', y: -8}'
    }
    else {
        $lines.Insert($Index + 1, '  m_offsetMax: {x: -' + $HorizontalPad + ', y: -8}')
    }
}

$activeRect = $null
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^--- !u!224 &(\d+)$') {
        $activeRect = $Matches[1]
        continue
    }
    if ($null -eq $activeRect) { continue }

    if ($stretchRectMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_AnchorMin: ') {
            $spec = $stretchRectMap[$activeRect]
            $lines[$i] = $spec[0]
            $lines[$i + 1] = $spec[1]
            $lines[$i + 2] = $spec[2]
            $lines[$i + 3] = $spec[3]
            if ($i + 4 -lt $lines.Count -and $lines[$i + 4] -match '^\s+m_offsetMin: ') {
                $lines[$i + 4] = $spec[4]
                $lines[$i + 5] = $spec[5]
            }
            else {
                $lines.Insert($i + 4, $spec[4])
                $lines.Insert($i + 5, $spec[5])
            }
            $patched += 6
            $activeRect = $null
        }
        continue
    }

    if ($rectMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_AnchoredPosition: ') {
            $lines[$i] = $rectMap[$activeRect][0]; $patched++
        }
        elseif ($lines[$i] -match '^\s+m_SizeDelta: ') {
            $lines[$i] = $rectMap[$activeRect][1]; $patched++; $activeRect = $null
        }
        continue
    }

    if ($labelPadMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_SizeDelta: ') {
            $pad = $labelPadMap[$activeRect][0]
            if ($i + 1 -lt $lines.Count -and $lines[$i + 1] -match '^\s+m_offsetMin: ') {
                Set-LabelPadding -Index ($i + 1) -HorizontalPad $pad
            }
            else {
                $lines.Insert($i + 1, '  m_offsetMin: {x: ' + $pad + ', y: 8}')
                Set-LabelPadding -Index ($i + 1) -HorizontalPad $pad
            }
            $patched += 2
            $activeRect = $null
        }
        continue
    }

    if ($panelLabelPadMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_SizeDelta: ') {
            $pad = $panelLabelPadMap[$activeRect]
            if ($i + 1 -lt $lines.Count -and $lines[$i + 1] -match '^\s+m_offsetMin: ') {
                Set-LabelPadding -Index ($i + 1) -HorizontalPad $pad
            }
            else {
                $lines.Insert($i + 1, '  m_offsetMin: {x: ' + $pad + ', y: 8}')
                Set-LabelPadding -Index ($i + 1) -HorizontalPad $pad
            }
            $patched += 2
            $activeRect = $null
        }
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "Auth button layout patched: $patched line updates"

# Confirm button label font size
$lines = [System.Collections.Generic.List[string]]([IO.File]::ReadAllLines($scenePath))
$activeTmp = $null
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^--- !u!114 &1731985548$') { $activeTmp = $true; continue }
    if ($activeTmp -and $lines[$i] -match '^\s+m_fontSize: ') {
        $lines[$i] = '  m_fontSize: 28'
        $activeTmp = $null
    }
    elseif ($activeTmp -and $lines[$i] -match '^\s+m_fontSizeBase: ') {
        $lines[$i] = '  m_fontSizeBase: 28'
    }
}
[IO.File]::WriteAllLines($scenePath, $lines)
