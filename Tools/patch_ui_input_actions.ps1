$path = Join-Path $PSScriptRoot '..\Assets\Scenes\ProtoType_LTG.unity' | Resolve-Path
$brokenGuid = 'ca9f5fa95ffab41fb9a615ab714db018'
$projectGuid = '2bcd2660ca9b64942af0de543d8d7100'
$content = [IO.File]::ReadAllText($path)
$count = ([regex]::Matches($content, $brokenGuid)).Count
if ($count -le 0) {
    Write-Host "No broken Input Actions GUID found (already patched)."
    exit 0
}

$content = $content.Replace($brokenGuid, $projectGuid)
[IO.File]::WriteAllText($path, $content)
Write-Host "Replaced $count Input Actions GUID reference(s) in ProtoType_LTG.unity"
