# Fast line-based overlay menu style patcher for ProtoType_LTG.unity
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path

$gold = '{fileID: -93586019915418993, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}'
$panel = '{fileID: -8136509021760896224, guid: 18859343a5bfe8649b95ffbd2b5b7f4b, type: 3}'
$inputPanel = '{fileID: -2847192836012345678, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}'
$track = '{fileID: 6936661826696753047, guid: eab81af514d17e54dabf8084a2559c16, type: 3}'
$handle = '{fileID: 6724485060924360464, guid: 317e6d0fc161f5643a148f3c4d22cd6b, type: 3}'
$fill = '{fileID: 5845452674472730522, guid: 24866d94c34bca84e82f2f2e2ee4c5e8, type: 3}'

$buttonNames = [System.Collections.Generic.HashSet[string]]@(
    'LoginButton','GoSignUpButton','ForgotPasswordButton','QuitButton',
    'SignUpButton','BackToLoginButton','SendResetEmailButton','ForgotBackToLoginButton',
    'MainMenuButton','DeleteAccountButton','DeleteAccountConfirmButton','DeleteAccountCancelButton',
    'SkipButton','BossAlarmContinueButton','StartButton','ConfirmButton'
)
$inputNames = [System.Collections.Generic.HashSet[string]]@(
    'LoginIdInput','LoginPasswordInput','SignUpUsernameInput','SignUpEmailInput',
    'SignUpPasswordInput','SignUpPasswordConfirmInput','SignUpNicknameInput','ForgotPasswordInput',
    'DeleteAccountPasswordInput'
)

$buttonImageIds = [System.Collections.Generic.HashSet[string]]@()
$inputImageIds = [System.Collections.Generic.HashSet[string]]@()
$sliderImages = @{} # id -> part

$currentName = $null
$pendingButtonTarget = $false
$inSlider = $null
$inDropdown = $null
$dropdownImages = [System.Collections.Generic.HashSet[string]]@()
$dropdownArrowImages = [System.Collections.Generic.HashSet[string]]@()

$lines = [IO.File]::ReadAllLines($scenePath)
for ($i = 0; $i -lt $lines.Length; $i++) {
    $line = $lines[$i]
    if ($line -match '^\s+m_Name: (.+)$') {
        $currentName = $Matches[1]
        if ($buttonNames.Contains($currentName)) { $pendingButtonTarget = $true }
        if ($inputNames.Contains($currentName)) {
            # first Image component on input root is usually a few lines later
        }
        if ($currentName -in @('BgmSlider','SfxSlider')) { $inSlider = $currentName }
        elseif ($inSlider -and $currentName -eq 'Background') { $sliderImages[$null] = 'track-pending' }
        if ($currentName -in @('ScreenModeDropdown','ResolutionDropdown')) { $inDropdown = $currentName }
    }

    if ($pendingButtonTarget -and $line -match 'm_TargetGraphic: \{fileID: (\d+)\}') {
        [void]$buttonImageIds.Add($Matches[1])
        $pendingButtonTarget = $false
    }

    if ($currentName -ne $null -and $inputNames.Contains($currentName) -and $line -match '^--- !u!114 &(\d+)$') {
        $id = $Matches[1]
        # peek ahead for Image script
        for ($j = $i + 1; $j -lt [Math]::Min($i + 12, $lines.Length); $j++) {
            if ($lines[$j] -match 'UnityEngine\.UI::UnityEngine\.UI\.Image') {
                [void]$inputImageIds.Add($id)
                break
            }
        }
    }

    if ($inSlider -and $line -match '^\s+m_Name: (Background|Fill|Handle)$') {
        $part = $Matches[1]
        for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Length); $j++) {
            if ($lines[$j] -match '^--- !u!114 &(\d+)$') {
                $id = $Matches[1]
                for ($k = $j + 1; $k -lt [Math]::Min($j + 12, $lines.Length); $k++) {
                    if ($lines[$k] -match 'UnityEngine\.UI::UnityEngine\.UI\.Image') {
                        $sliderImages[$id] = $part.ToLower()
                        break
                    }
                }
                break
            }
        }
    }

    if ($inDropdown -and $line -match '^\s+m_Name: (Background|Template|Viewport|Item Background|Item Checkmark|Arrow)$') {
        $part = $Matches[1]
        for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Length); $j++) {
            if ($lines[$j] -match '^--- !u!114 &(\d+)$') {
                $id = $Matches[1]
                for ($k = $j + 1; $k -lt [Math]::Min($j + 12, $lines.Length); $k++) {
                    if ($lines[$k] -match 'UnityEngine\.UI::UnityEngine\.UI\.Image') {
                        if ($part -eq 'Arrow') { [void]$dropdownArrowImages.Add($id) }
                        else { [void]$dropdownImages.Add($id) }
                        break
                    }
                }
                break
            }
        }
    }

    if ($line -match '^--- !u!1 &') {
        if ($currentName -notin @('Background','Fill','Handle','Template','Viewport','Item Background','Item Checkmark','Arrow')) {
            if ($currentName -in @('BgmSlider','SfxSlider','ScreenModeDropdown','ResolutionDropdown')) { }
            else { $inSlider = $null; $inDropdown = $null }
        }
    }
}

$activeImageId = $null
$activeKind = $null # button|input|dropdown|track|fill|handle
$patched = 0

for ($i = 0; $i -lt $lines.Length; $i++) {
    $line = $lines[$i]
    if ($line -match '^--- !u!114 &(\d+)$') {
        $id = $Matches[1]
        $activeImageId = $null
        $activeKind = $null
        if ($buttonImageIds.Contains($id)) { $activeImageId = $id; $activeKind = 'button' }
        elseif ($inputImageIds.Contains($id)) { $activeImageId = $id; $activeKind = 'input' }
        elseif ($dropdownImages.Contains($id)) { $activeImageId = $id; $activeKind = 'dropdown' }
        elseif ($dropdownArrowImages.Contains($id)) { $activeImageId = $id; $activeKind = 'dropdown-arrow' }
        elseif ($sliderImages.ContainsKey($id)) { $activeImageId = $id; $activeKind = $sliderImages[$id] }
        continue
    }

    if ($null -eq $activeImageId) { continue }

    if ($line -match '^\s+m_Color: ') {
        switch ($activeKind) {
            'input' { $lines[$i] = '  m_Color: {r: 1, g: 0.98, b: 0.92, a: 1}'; $patched++ }
            'dropdown' { $lines[$i] = '  m_Color: {r: 0.98, g: 0.96, b: 0.93, a: 1}'; $patched++ }
            'dropdown-arrow' { $lines[$i] = '  m_Color: {r: 1, g: 1, b: 1, a: 1}'; $patched++ }
            'button' { $lines[$i] = '  m_Color: {r: 1, g: 1, b: 1, a: 1}'; $patched++ }
            default { $lines[$i] = '  m_Color: {r: 1, g: 1, b: 1, a: 1}'; $patched++ }
        }
    }
    elseif ($line -match '^\s+m_Sprite: ') {
        switch ($activeKind) {
            'input' { $lines[$i] = "  m_Sprite: $inputPanel"; $patched++ }
            'dropdown' { $lines[$i] = "  m_Sprite: $panel"; $patched++ }
            'dropdown-arrow' { $lines[$i] = "  m_Sprite: $gold"; $patched++ }
            'button' { $lines[$i] = "  m_Sprite: $gold"; $patched++ }
            'background' { $lines[$i] = "  m_Sprite: $track"; $patched++ }
            'fill' { $lines[$i] = "  m_Sprite: $fill"; $patched++ }
            'handle' { $lines[$i] = "  m_Sprite: $handle"; $patched++ }
        }
    }
    elseif ($line -match '^\s+m_Type: ') {
        switch ($activeKind) {
            'input' { $lines[$i] = '  m_Type: 1'; $patched++ }
            'dropdown' { $lines[$i] = '  m_Type: 1'; $patched++ }
            'dropdown-arrow' { $lines[$i] = '  m_Type: 0'; $patched++ }
            'button' { $lines[$i] = '  m_Type: 0'; $patched++ }
            'background' { $lines[$i] = '  m_Type: 1'; $patched++ }
            'fill' { $lines[$i] = '  m_Type: 3'; $patched++ }
            'handle' { $lines[$i] = '  m_Type: 0'; $patched++ }
        }
    }
    elseif ($activeKind -eq 'fill' -and $line -match '^\s+m_FillMethod: ') {
        $lines[$i] = '  m_FillMethod: 0'; $patched++
    }
    elseif ($activeKind -eq 'fill' -and $line -match '^\s+m_FillOrigin: ') {
        $lines[$i] = '  m_FillOrigin: 0'; $patched++
    }
    elseif ($line -match '^\s+m_PreserveAspect: ') {
        if ($activeKind -eq 'button') { $lines[$i] = '  m_PreserveAspect: 1'; $patched++ }
    }
}

# Rect layout patches
$rectMap = @{
    '17769437' = @('  m_AnchoredPosition: {x: 0, y: -100}', '  m_SizeDelta: {x: 500, y: 64}')
    '1923796080' = @('  m_AnchoredPosition: {x: -190, y: -210}', '  m_SizeDelta: {x: 400, y: 96}')
    '296524994' = @('  m_AnchoredPosition: {x: 190, y: -210}', '  m_SizeDelta: {x: 340, y: 96}')
    '135891175' = @('  m_AnchoredPosition: {x: -210, y: -95}', '  m_SizeDelta: {x: 500, y: 64}')
    '271119496' = @('  m_AnchoredPosition: {x: -210, y: -175}', '  m_SizeDelta: {x: 500, y: 64}')
    '101719391' = @('  m_AnchoredPosition: {x: 270, y: -135}', '  m_SizeDelta: {x: 340, y: 96}')
    '1854102973' = @('  m_AnchoredPosition: {x: -220, y: 120}', '  m_SizeDelta: {x: 340, y: 96}')
    '1867738619' = @('  m_AnchoredPosition: {x: -220, y: 120}', '  m_SizeDelta: {x: 340, y: 96}')
    '1641717991' = @('  m_AnchoredPosition: {x: -220, y: 120}', '  m_SizeDelta: {x: 340, y: 96}')
    '89634418' = @('  m_AnchoredPosition: {x: 0, y: 149}', '  m_SizeDelta: {x: 340, y: 96}')
    '1993552625' = @('  m_AnchoredPosition: {x: 0, y: 100}', '  m_SizeDelta: {x: 500, y: 64}')
    '871241871' = @('  m_AnchoredPosition: {x: -190, y: -100}', '  m_SizeDelta: {x: 340, y: 96}')
    '2013920909' = @('  m_AnchoredPosition: {x: 190, y: -100}', '  m_SizeDelta: {x: 340, y: 96}')
    '1726722688' = @('  m_AnchoredPosition: {x: 0, y: 52}', '  m_SizeDelta: {x: 340, y: 96}')
}
$activeRect = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!224 &(\d+)$') {
        $activeRect = $Matches[1]
        continue
    }
    if ($null -ne $activeRect -and $rectMap.ContainsKey($activeRect)) {
        if ($lines[$i] -match '^\s+m_AnchoredPosition: ') { $lines[$i] = $rectMap[$activeRect][0]; $patched++ }
        elseif ($lines[$i] -match '^\s+m_SizeDelta: ') { $lines[$i] = $rectMap[$activeRect][1]; $patched++; $activeRect = $null }
    }
}

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -like '*m_text: "\uC7AC\uC124\uC815 \uBA54\uC77C\n\uBCF4\uB0B4\uAE30"*') {
        $lines[$i] = '  m_text: "\uC7AC\uC124\uC815 \uBA54\uC77C \uBCF4\uB0B4\uAE30"'
        $patched++
    }
}

[IO.File]::WriteAllLines($scenePath, $lines)
Write-Host "Buttons: $($buttonImageIds.Count), Inputs: $($inputImageIds.Count), Dropdown parts: $($dropdownImages.Count), Slider parts: $($sliderImages.Count), Patched lines: $patched"
