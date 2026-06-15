# ProtoType_LTG 오버레이 UI(로그인·회원가입·설정·기록 등) 복구
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Split-Path -Parent
$editor = Join-Path $root 'Assets\Scripts\Editor'

$scripts = @(
    'ApplyOverlayMenuStyle.ps1'
    'ApplyOverlayMenuAuthInputLayout.ps1'
    'ApplyOverlayMenuAuthInputText.ps1'
    'ApplyOverlayMenuAuthButtons.ps1'
    'ApplyOverlayMenuInputsDropdowns.ps1'
    'ApplyOverlayMenuUiFixes.ps1'
    'ApplyOverlayMenuWidgets.ps1'
)

foreach ($name in $scripts) {
    $path = Join-Path $editor $name
    if (-not (Test-Path $path)) { throw "Missing script: $path" }
    Write-Host "==> $name"
    & $path
}

Write-Host '[RestoreOverlayMenuUi] PS1 patches complete. Run Unity menu: Tools/UI/Apply Overlay Menu Style (All) if sprites/colors still look plain.'
