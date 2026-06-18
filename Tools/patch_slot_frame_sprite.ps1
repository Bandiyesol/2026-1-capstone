$path = Join-Path $PSScriptRoot '..\Assets\Scenes\ProtoType_LTG.unity' | Resolve-Path
$spriteRef = '  slotFrameSprite: {fileID: -8136509021760896224, guid: 18859343a5bfe8649b95ffbd2b5b7f4b, type: 3}'
$content = [IO.File]::ReadAllText($path)
$updated = $content.Replace('  slotFrameSprite: {fileID: 0}', $spriteRef)
if ($updated -eq $content) {
    Write-Host 'slotFrameSprite already assigned or pattern not found.'
} else {
    [IO.File]::WriteAllText($path, $updated)
    $count = ([regex]::Matches($content, '  slotFrameSprite: \{fileID: 0\}')).Count
    Write-Host "Patched $count slotFrameSprite reference(s)."
}
