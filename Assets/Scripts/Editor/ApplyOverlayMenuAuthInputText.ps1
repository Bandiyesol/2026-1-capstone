# Style auth input placeholder/text colors, sizes, and font style
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$lines = [IO.File]::ReadAllLines($scenePath)

$inputFieldIds = [System.Collections.Generic.HashSet[string]]@(
    '17769438', '130089726', '135891176', '271119497',
    '350459579', '427598823', '843805450', '1540312774',
    '1993552626'
)
$textIds = [System.Collections.Generic.HashSet[string]]@(
    '1540389225', '787459147', '1486809014', '631047989',
    '1992459218', '1856541954', '1325736237', '1756240783',
    '311270289', '1931077766', '485683478', '1595395164',
    '1708256730', '1238799619', '684582649', '1705295950',
    '1044822793', '160223200'
)
$placeholderIds = [System.Collections.Generic.HashSet[string]]@(
    '787459147', '631047989', '1856541954', '1756240783',
    '1931077766', '1595395164', '1238799619', '1705295950',
    '160223200'
)

$patched = 0
$activeId = $null
$activeKind = $null

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {
        $id = $Matches[1]
        $activeId = $null
        $activeKind = $null
        if ($inputFieldIds.Contains($id)) { $activeId = $id; $activeKind = 'field' }
        elseif ($textIds.Contains($id)) {
            $activeId = $id
            $activeKind = if ($placeholderIds.Contains($id)) { 'placeholder' } else { 'text' }
        }
        continue
    }

    if ($null -eq $activeId) { continue }

    if ($activeKind -eq 'field' -and $lines[$i] -match '^\s+m_GlobalPointSize: ') {
        $lines[$i] = '  m_GlobalPointSize: 24'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_fontColor: ') {
        if ($activeKind -eq 'placeholder') {
            $lines[$i] = '  m_fontColor: {r: 0.55, g: 0.48, b: 0.40, a: 0.85}'; $patched++
        }
        else {
            $lines[$i] = '  m_fontColor: {r: 0.35, g: 0.28, b: 0.20, a: 1}'; $patched++
        }
    }
    elseif ($lines[$i] -match '^\s+rgba: ' -and $activeKind -ne 'field') {
        if ($activeKind -eq 'placeholder') {
            $lines[$i] = '    rgba: 3866335032'; $patched++
        }
        else {
            $lines[$i] = '    rgba: 4281809208'; $patched++
        }
    }
    elseif ($lines[$i] -match '^\s+m_fontSize: ') {
        if ($activeKind -eq 'placeholder') {
            $lines[$i] = '  m_fontSize: 22'; $patched++
        }
        else {
            $lines[$i] = '  m_fontSize: 24'; $patched++
        }
    }
    elseif ($lines[$i] -match '^\s+m_fontSizeBase: ' -and $activeKind -ne 'field') {
        if ($activeKind -eq 'placeholder') {
            $lines[$i] = '  m_fontSizeBase: 22'; $patched++
        }
        else {
            $lines[$i] = '  m_fontSizeBase: 24'; $patched++
        }
    }
    elseif ($lines[$i] -match '^\s+m_fontStyle: ' -and $activeKind -ne 'field') {
        $lines[$i] = '  m_fontStyle: 0'; $patched++
    }
    elseif ($lines[$i] -match '^\s+m_Color: ' -and $activeKind -ne 'field') {
        if ($activeKind -eq 'placeholder') {
            $lines[$i] = '  m_Color: {r: 0.55, g: 0.48, b: 0.40, a: 0.85}'; $patched++
        }
        else {
            $lines[$i] = '  m_Color: {r: 0.35, g: 0.28, b: 0.20, a: 1}'; $patched++
        }
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "Auth input text styled: $patched line updates"
