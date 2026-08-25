$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$armamentFormPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

Assert-True (Test-Path -LiteralPath $visualPath) 'Formal visual preset XML must exist.'
Assert-True (Test-Path -LiteralPath $armamentFormPath) 'Formal gun shell preset XML must exist.'

$visualXml = [xml](Get-Content -LiteralPath $visualPath -Raw -Encoding utf8)
$armamentFormXml = [xml](Get-Content -LiteralPath $armamentFormPath -Raw -Encoding utf8)
$visualText = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8

$assaultShell = @($armamentFormXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef') |
    Where-Object { $_.defName -eq 'BDP_GunClass_AssaultRifle' } |
    Select-Object -First 1
Assert-True ($null -ne $assaultShell) 'Assault rifle gun shell must exist.'
Assert-True (
    ($assaultShell.overrides.visualPresetDefName -eq 'BDP_Visual_RangedWeaponReference') -and
    ($assaultShell.overrides.compositeVisualPresetDefName -eq 'BDP_Visual_RangedWeaponReference_Dual')
) 'Assault rifle must still bind the expected single and dual visual presets.'

$targetPresetNames = @(
    'BDP_Visual_RangedWeaponReference',
    'BDP_Visual_RangedWeaponReference_Dual'
)

foreach ($presetName in $targetPresetNames) {
    $preset = @($visualXml.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
        Where-Object { $_.defName -eq $presetName } |
        Select-Object -First 1
    Assert-True ($null -ne $preset) "$presetName must exist."

    Assert-True ($null -eq $preset.StageVisuals) `
        "$presetName must not retain the temporary warmup graphic or firing/cooldown visibility trial."
}

$targetBlocks = [regex]::Matches(
    $visualText,
    '(?s)<BDP\.Core\.Expressions\.ExpressionVisualPresetDef(?:\s+[^>]*)?>\s*(?:(?!</BDP\.Core\.Expressions\.ExpressionVisualPresetDef>).)*?<defName>BDP_Visual_RangedWeaponReference(?:_Dual)?</defName>.*?</BDP\.Core\.Expressions\.ExpressionVisualPresetDef>')
Assert-True ($targetBlocks.Count -eq 2) 'Both assault-rifle visual preset XML blocks must be inspectable.'
foreach ($targetBlock in $targetBlocks) {
    Assert-True ($targetBlock.Value -notmatch '<StageVisuals>') `
        'Assault-rifle visual presets must contain only their normal graphic and inherited baseline data.'
}

Write-Output 'AssaultRifleWeaponStageVisualSmokeTests PASS'
