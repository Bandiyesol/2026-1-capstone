$srcMeta = Join-Path $PSScriptRoot '..\Assets\Arts\UI\Vol 6 Ui Expansion Pack\Panels\Panels_06.png.meta' | Resolve-Path
$dstMeta = Join-Path (Join-Path $PSScriptRoot '..\Assets\Resources\UI') 'Panels_06.png.meta'
$newGuid = [guid]::NewGuid().ToString('N')
$content = [IO.File]::ReadAllText($srcMeta)
$content = $content.Replace('guid: 18859343a5bfe8649b95ffbd2b5b7f4b', "guid: $newGuid")
[IO.File]::WriteAllText($dstMeta, $content)
Write-Host "Created Resources meta with guid: $newGuid"
