$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$presetPath = Join-Path $modRoot '1.6\Content\Defs\BeamTrailDef\Presets.xml'
$fujinProjectilePath = Join-Path $modRoot '1.6\Content\Defs\ThingDef\Projectiles\FujinRangedStrike.xml'

Assert-True (Test-Path -LiteralPath $presetPath) '正式光束拖尾预设 XML 必须存在。'

$visualXml = [xml](Get-Content -LiteralPath $presetPath -Raw -Encoding utf8)
$presetDefs = @($visualXml.Defs.'BDP.Content.Projectiles.BeamTrail.BeamTrailPresetDef')
$expectedCommonColor = '(0.8175, 0.971, 0.879, 0.775)'
$expectedFujinColor = '(0.45, 1.0, 0.55, 0.775)'

foreach ($defName in @('BDP_TrailPreset_BrightMintLong', 'BDP_TrailPreset_BrightMintShort')) {
    $preset = $presetDefs | Where-Object { $_.defName -eq $defName } | Select-Object -First 1
    Assert-True ($null -ne $preset) "缺少正式光束拖尾预设：$defName"
    Assert-True ($preset.trailColor -eq $expectedCommonColor) `
        "$defName 必须使用通用亮薄荷色拖尾：$expectedCommonColor"
}

$fujinPreset = $presetDefs | Where-Object { $_.defName -eq 'BDP_TrailPreset_SlimBrightGreen' } | Select-Object -First 1
Assert-True ($null -ne $fujinPreset) '缺少风刃专用细长拖尾预设。'
Assert-True ($fujinPreset.trailColor -eq $expectedFujinColor) `
    "风刃必须使用高饱和绿色拖尾：$expectedFujinColor"

$brightMintLong = $presetDefs | Where-Object { $_.defName -eq 'BDP_TrailPreset_BrightMintLong' } | Select-Object -First 1
$fujinTrail = $fujinPreset
$fujinProjectile = ([xml](Get-Content -LiteralPath $fujinProjectilePath -Raw -Encoding utf8)).Defs.ThingDef
Assert-True ([string]$fujinProjectile.modExtensions.li.preset -eq 'BDP_TrailPreset_SlimBrightGreen') `
    '风刃必须引用专用细长拖尾预设。'
Assert-True ([math]::Abs(([double]$fujinTrail.trailWidth * 2.0) - [double]$brightMintLong.trailWidth) -lt 0.0000001) `
    '风刃专用拖尾宽度必须是通用长拖尾的一半。'
Assert-True ([math]::Abs(([double]$fujinTrail.trailWidth) - 0.05525) -lt 0.0000001) `
    '风刃专用拖尾宽度必须为 0.05525。'

Write-Output 'BeamTrailDefaultAppearanceSmokeTests PASS'
