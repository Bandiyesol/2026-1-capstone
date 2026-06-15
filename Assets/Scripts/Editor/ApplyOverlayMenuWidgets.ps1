# Settings panel: horizontal slider fill, plain track, gold diamond dropdown arrow

$ErrorActionPreference = 'Stop'

$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path

$lines = [IO.File]::ReadAllLines($scenePath)



$panel = '{fileID: -8136509021760896224, guid: 18859343a5bfe8649b95ffbd2b5b7f4b, type: 3}'

$handle = '{fileID: 6724485060924360464, guid: 317e6d0fc161f5643a148f3c4d22cd6b, type: 3}'

$fill = '{fileID: 5845452674472730522, guid: 24866d94c34bca84e82f2f2e2ee4c5e8, type: 3}'

$dropdownArrow = $handle



$kinds = @{

    '203911044'='background'; '775372620'='fill'; '1119350327'='handle'

    '1368830286'='background'; '962496253'='fill'; '286849912'='handle'

    '1137530197'='dropdown'; '2091469695'='dropdown'

    '1604282809'='dropdown-arrow'; '1741326871'='dropdown-arrow'

}



$activeId = $null

$activeKind = $null

$patched = 0

for ($i = 0; $i -lt $lines.Length; $i++) {

    if ($lines[$i] -match '^--- !u!114 &(\d+)$') {

        $id = $Matches[1]

        $activeId = $null; $activeKind = $null

        if ($kinds.ContainsKey($id)) { $activeId = $id; $activeKind = $kinds[$id] }

        continue

    }

    if ($null -eq $activeId) { continue }

    if ($lines[$i] -match '^\s+m_Color: ') {

        switch ($activeKind) {

            'dropdown' { $lines[$i] = '  m_Color: {r: 0.98, g: 0.96, b: 0.93, a: 1}' }

            'dropdown-arrow' { $lines[$i] = '  m_Color: {r: 1, g: 1, b: 1, a: 1}' }

            'background' { $lines[$i] = '  m_Color: {r: 0.32, g: 0.26, b: 0.20, a: 0.85}' }

            default { $lines[$i] = '  m_Color: {r: 1, g: 1, b: 1, a: 1}' }

        }

        $patched++

    }

    elseif ($lines[$i] -match '^\s+m_Sprite: ') {

        switch ($activeKind) {

            'dropdown' { $lines[$i] = "  m_Sprite: $panel" }

            'dropdown-arrow' { $lines[$i] = "  m_Sprite: $dropdownArrow" }

            'background' { $lines[$i] = "  m_Sprite: $panel" }

            'fill' { $lines[$i] = "  m_Sprite: $fill" }

            'handle' { $lines[$i] = "  m_Sprite: $handle" }

        }

        $patched++

    }

    elseif ($lines[$i] -match '^\s+m_Type: ') {

        switch ($activeKind) {

            'dropdown' { $lines[$i] = '  m_Type: 1' }

            'dropdown-arrow' { $lines[$i] = '  m_Type: 0' }

            'background' { $lines[$i] = '  m_Type: 1' }

            'fill' { $lines[$i] = '  m_Type: 3' }

            'handle' { $lines[$i] = '  m_Type: 0' }

        }

        $patched++

    }

    elseif ($activeKind -eq 'fill' -and $lines[$i] -match '^\s+m_FillMethod: ') {

        $lines[$i] = '  m_FillMethod: 0'; $patched++

    }

    elseif ($activeKind -eq 'fill' -and $lines[$i] -match '^\s+m_FillOrigin: ') {

        $lines[$i] = '  m_FillOrigin: 0'; $patched++

    }

}



# Arrow: gold diamond handle (matches slider thumb), no rotation

$arrowRectIds = @('1604282808', '1741326870')

$activeRect = $null

for ($i = 0; $i -lt $lines.Length; $i++) {

    if ($lines[$i] -match '^--- !u!224 &(\d+)$') {

        $activeRect = $Matches[1]

        continue

    }

    if ($null -eq $activeRect -or $arrowRectIds -notcontains $activeRect) { continue }

    if ($lines[$i] -match '^\s+m_LocalRotation: ') {

        $lines[$i] = '  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}'; $patched++

    }

    elseif ($lines[$i] -match '^\s+m_LocalEulerAnglesHint: ') {

        $lines[$i] = '  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}'; $patched++

    }

    elseif ($lines[$i] -match '^\s+m_SizeDelta: ') {

        $lines[$i] = '  m_SizeDelta: {x: 24, y: 24}'; $patched++

        $activeRect = $null

    }

}



[IO.File]::WriteAllLines($scenePath, $lines)

Write-Host "[ApplyOverlayMenuWidgets] Settings sliders/dropdowns patched: $patched updates"


