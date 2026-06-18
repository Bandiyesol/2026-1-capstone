$downloads = Join-Path $env:USERPROFILE 'Downloads'
$docx = Get-ChildItem -Path $downloads -Filter '*PPT.docx' | Select-Object -First 1
$pptx = Get-ChildItem -Path $downloads -Filter '*PPT.pptx' | Select-Object -First 1

function Extract-DocxText($path) {
    $temp = Join-Path $env:TEMP 'extract_docx'
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
    Copy-Item $path (Join-Path $env:TEMP 'temp.docx.zip')
    Expand-Archive -Path (Join-Path $env:TEMP 'temp.docx.zip') -DestinationPath $temp -Force
    $xml = Get-Content (Join-Path $temp 'word\document.xml') -Raw -Encoding UTF8
    $matches = [regex]::Matches($xml, '<w:t[^>]*>(.*?)</w:t>')
    $sb = New-Object System.Text.StringBuilder
    foreach ($m in $matches) { [void]$sb.Append($m.Groups[1].Value) }
    return $sb.ToString()
}

function Extract-PptxSlides($path) {
    $temp = Join-Path $env:TEMP 'extract_pptx'
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
    Copy-Item $path (Join-Path $env:TEMP 'temp.pptx.zip')
    Expand-Archive -Path (Join-Path $env:TEMP 'temp.pptx.zip') -DestinationPath $temp -Force
    $slides = Get-ChildItem (Join-Path $temp 'ppt\slides') -Filter 'slide*.xml' | Sort-Object { [int]($_.BaseName -replace 'slide','') }
    $out = @()
    foreach ($slide in $slides) {
        $xml = Get-Content $slide.FullName -Raw -Encoding UTF8
        $matches = [regex]::Matches($xml, '<a:t[^>]*>(.*?)</a:t>')
        $sb = New-Object System.Text.StringBuilder
        foreach ($m in $matches) { [void]$sb.Append($m.Groups[1].Value) }
        $out += [PSCustomObject]@{ Slide = $slide.Name; Text = $sb.ToString() }
    }
    return $out
}

Write-Output "DOCX: $($docx.FullName)"
Write-Output "PPTX: $($pptx.FullName)"
Write-Output ''
Write-Output '===== DOCX ====='
Write-Output (Extract-DocxText $docx.FullName)
Write-Output ''
Write-Output '===== PPTX ====='
foreach ($s in (Extract-PptxSlides $pptx.FullName)) {
    Write-Output "--- $($s.Slide) ---"
    Write-Output $s.Text
    Write-Output ''
}
