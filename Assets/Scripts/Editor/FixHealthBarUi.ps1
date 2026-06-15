# Restore original Charge Bars A_05 health frame; keep working slider fill layout.
$ErrorActionPreference = 'Stop'
$scenePath = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$text = [IO.File]::ReadAllText($scenePath)

$chargeBarBg = '  m_Sprite: {fileID: 8010778982932929950, guid: 8b9e6ec94adc82b45a20709abbfadbd7, type: 3}'
$resourceBarBg = '  m_Sprite: {fileID: -2914891076806108786, guid: 50faa5dfe726c1c45be0d0bb5fe6d26c, type: 3}'

if ($text.Contains($resourceBarBg)) {
    $text = $text.Replace($resourceBarBg, $chargeBarBg)
    Write-Host '[FixHealthBarUi] Background -> Charge Bars A_05'
} elseif ($text.Contains($chargeBarBg)) {
    Write-Host '[FixHealthBarUi] Background already Charge Bars A_05'
} else {
    throw 'Health Background sprite line not found'
}

# Background: Simple (not Sliced Resource bar)
$text = $text.Replace(
    "$chargeBarBg`r`n  m_Type: 1",
    "$chargeBarBg`r`n  m_Type: 0"
)
$text = $text.Replace(
    "$chargeBarBg`n  m_Type: 1",
    "$chargeBarBg`n  m_Type: 0"
)

$fillBlock = @'
  m_Sprite: {fileID: 471370097811989083, guid: b49b307c50024474596cf0fcbc82885f, type: 3}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0
'@

if ($text -notmatch [regex]::Escape($fillBlock.Substring(0, 80))) {
    $text = [regex]::Replace(
        $text,
        '(?ms)(  m_GameObject: \{fileID: 910507391\}.*?  m_Sprite: \{fileID: 471370097811989083, guid: b49b307c50024474596cf0fcbc82885f, type: 3\}\r?\n  m_Type: )(\d+)',
        '${1}0',
        1
    )
    $text = [regex]::Replace(
        $text,
        '(?ms)(  m_GameObject: \{fileID: 910507391\}.*?  m_FillMethod: )\d+',
        '${1}0',
        1
    )
}

$oldFillRect = @'
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &910507393
'@
$newFillRect = @'
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 0.5}
--- !u!114 &910507393
'@
if ($text.Contains($oldFillRect)) {
    $text = $text.Replace($oldFillRect, $newFillRect)
    Write-Host '[FixHealthBarUi] Fill RectTransform -> left-anchored slider layout'
} elseif ($text.Contains($newFillRect.Replace("`r`n", "`n"))) {
    Write-Host '[FixHealthBarUi] Fill RectTransform already correct'
} else {
    Write-Host '[FixHealthBarUi] Fill RectTransform left unchanged (already patched)'
}

[IO.File]::WriteAllText($scenePath, $text)
Write-Host '[FixHealthBarUi] Done.'
