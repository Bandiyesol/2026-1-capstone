$path = Join-Path $PSScriptRoot '..\Assets\Scenes\ProtoType_LTG.unity' | Resolve-Path
$old = @"
  stages:
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
"@
$new = @"
  stages:
  - {fileID: 438762768}
  - {fileID: 657933317}
  - {fileID: 732791334}
  - {fileID: 1521182821}
  - {fileID: 750514609}
  - {fileID: 416928887}
  - {fileID: 55985378}
"@
$content = [IO.File]::ReadAllText($path)
if ($content.Contains($old)) {
    $content = $content.Replace($old, $new)
    [IO.File]::WriteAllText($path, $content)
    Write-Host 'patched StageManager stages in ProtoType_LTG.unity'
} else {
    Write-Host 'pattern not found (may already be patched)'
}
