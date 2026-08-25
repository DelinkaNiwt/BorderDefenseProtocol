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

Assert-True (Test-Path -LiteralPath $presetPath) '正式光束拖尾预设 XML 必须存在。'

$visualXml = [xml](Get-Content -LiteralPath $presetPath -Raw -Encoding utf8)
$presetDefs = @($visualXml.Defs.'BDP.Content.Projectiles.BeamTrail.BeamTrailPresetDef')
$expectedColor = '(0.8175, 0.971, 0.879, 0.775)'

foreach ($defName in @('BDP_TrailPreset_BrightMintLong', 'BDP_TrailPreset_BrightMintShort')) {
    $preset = $presetDefs | Where-Object { $_.defName -eq $defName } | Select-Object -First 1
    Assert-True ($null -ne $preset) "缺少正式光束拖尾预设：$defName"
    Assert-True ($preset.trailColor -eq $expectedColor) `
        "$defName 必须使用降低亮度后的默认拖尾颜色：$expectedColor"
}

Write-Output 'BeamTrailDefaultAppearanceSmokeTests PASS'
