$ErrorActionPreference = 'Stop'
$p = (Resolve-Path (Join-Path $PSScriptRoot '..\..\Scenes\ProtoType_LTG.unity')).Path
$t = [IO.File]::ReadAllText($p)
$old = "  m_Sprite: {fileID: 471370097811989083, guid: b49b307c50024474596cf0fcbc82885f, type: 3}`r`n  m_Type: 3"
$new = "  m_Sprite: {fileID: 471370097811989083, guid: b49b307c50024474596cf0fcbc82885f, type: 3}`r`n  m_Type: 0"
if (-not $t.Contains($old)) {
    $old = $old.Replace("`r`n", "`n")
    $new = $new.Replace("`r`n", "`n")
}
if (-not $t.Contains($old)) { throw 'Fill type block not found' }
[IO.File]::WriteAllText($p, $t.Replace($old, $new))
Write-Host 'Fill m_Type set to Simple (0)'
