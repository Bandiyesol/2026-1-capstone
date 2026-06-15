# UTF-8 — 악세사리·UI SFX 파일을 하위 폴더로 정리합니다.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$base = Join-Path $PSScriptRoot '..\Assets\Arts\Audio\SFX' | Resolve-Path
$accessory = Join-Path $base 'Accessory'
$ui = Join-Path $base 'UI'

New-Item -ItemType Directory -Force -Path $accessory | Out-Null
New-Item -ItemType Directory -Force -Path $ui | Out-Null

$uiFiles = @(
    '입력창에 입력 소리.mp3',
    '마법진 이동 소리.mp3'
)

$accessoryFiles = @(
    '제우스의 심판.mp3',
    '심연의 군주.mp3',
    '번개 맞은 검.mp3',
    '에키드나의 목걸이.mp3',
    '마법의 구.wav',
    '그림자 가면.mp3',
    '투명 망토.mp3',
    '폭탄광.mp3',
    '불사조의 망토 폭발 소리.mp3',
    '불사조의 망토 버프 소리.wav',
    '미네르바의 지혜.mp3',
    '미다스의 장갑.mp3',
    '시간술사의 모래시계 가방.wav',
    '신의 방패.mp3',
    '무한의 마력.wav',
    '재앙의 씨앗 씨앗 심는 소리.mp3',
    '재앙의 씨앗 폭발 소리.wav',
    '영혼의 랜턴 공전 소리.mp3',
    '영혼의 랜턴 총알 소리.mp3',
    '용의 심장 심장 뛰는 소리.mp3',
    '용의 심장 울음소리.mp3',
    '금지된 마법서.mp3',
    '번개 깃든 악령.mp3'
)

function Move-WithMeta([string]$fileName, [string]$destDir) {
    $src = Join-Path $base $fileName
    if (-not (Test-Path -LiteralPath $src)) {
        Write-Warning "없음: $fileName"
        return
    }
    $dest = Join-Path $destDir $fileName
    Move-Item -LiteralPath $src -Destination $dest -Force
    $meta = "$src.meta"
    if (Test-Path -LiteralPath $meta) {
        Move-Item -LiteralPath $meta -Destination "$dest.meta" -Force
    }
    Write-Host "OK $fileName -> $(Split-Path $destDir -Leaf)"
}

foreach ($f in $uiFiles) { Move-WithMeta $f $ui }
foreach ($f in $accessoryFiles) { Move-WithMeta $f $accessory }

# 파일명 끝 공백 정리
$gateOld = Join-Path $accessory '차원 여행자의 게이트 .mp3'
$gateNew = Join-Path $accessory '차원 여행자의 게이트.mp3'
$gateOldRoot = Join-Path $base '차원 여행자의 게이트 .mp3'
if (Test-Path -LiteralPath $gateOldRoot) {
    Move-WithMeta '차원 여행자의 게이트 .mp3' $accessory
}
if (Test-Path -LiteralPath $gateOld) {
    Rename-Item -LiteralPath $gateOld -NewName '차원 여행자의 게이트.mp3'
    $gateMetaOld = "$gateOld.meta"
    if (Test-Path -LiteralPath $gateMetaOld) {
        Rename-Item -LiteralPath $gateMetaOld -NewName '차원 여행자의 게이트.mp3.meta'
    }
    Write-Host 'OK 차원 여행자의 게이트 — 파일명 공백 제거'
}

Write-Host 'SFX 정리 완료.'
