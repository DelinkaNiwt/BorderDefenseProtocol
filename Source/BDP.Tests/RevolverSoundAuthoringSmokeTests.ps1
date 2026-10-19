# 左轮手枪专属开枪音效配置冒烟测试。

$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$armamentPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$soundDefPath = Join-Path $modRoot '1.6\Content\Defs\SoundDef\Revolver.xml'
$soundPath = Join-Path $modRoot '1.6\Sounds\Trigger\Revolver\Shot_BDP_Revolver.wav'

Assert-True (Test-Path -LiteralPath $armamentPath) '左轮武装构型 Def 文件必须存在。'
Assert-True (Test-Path -LiteralPath $soundDefPath) '左轮专属 SoundDef 必须存在。'
Assert-True (Test-Path -LiteralPath $soundPath) '左轮专属开枪音频必须存在。'

$armamentXml = [xml](Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8)
$soundXml = [xml](Get-Content -LiteralPath $soundDefPath -Raw -Encoding utf8)
$revolver = @($armamentXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef' |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_Revolver' })[0]
$sound = @($soundXml.Defs.SoundDef | Where-Object { $_.defName -eq 'BDP_Sound_RevolverCast' })[0]

Assert-True ($null -ne $revolver) '左轮手枪武装构型 Def 必须存在。'
Assert-True ($null -ne $sound) 'BDP_Sound_RevolverCast 必须存在。'
Assert-True ([string]$revolver.overrides.soundCast -eq 'BDP_Sound_RevolverCast') `
    '左轮手枪必须引用 BDP_Sound_RevolverCast。'
Assert-True ([string]$sound.context -eq 'MapOnly') '左轮专属开枪音效必须使用 MapOnly 上下文。'
Assert-True ([string]$sound.subSounds.li.grains.li.clipPath -eq 'Trigger/Revolver/Shot_BDP_Revolver') `
    '左轮专属 SoundDef 必须指向正式左轮音频。'

Write-Output 'RevolverSoundAuthoringSmokeTests PASS'
