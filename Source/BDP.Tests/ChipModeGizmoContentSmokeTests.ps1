$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$providerPath = Join-Path $contentRoot 'Trigger\UI\ChipModes\ChipModeGizmoProvider.cs'
$commandPath = Join-Path $contentRoot 'Trigger\UI\ChipModes\Command_ChipMode.cs'
$bootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'

Assert-True (Test-Path -LiteralPath $providerPath) `
    'Content must provide one formal ChipModeGizmoProvider.'
Assert-True (Test-Path -LiteralPath $commandPath) `
    'Content must provide one formal Command_ChipMode with a right-click menu.'

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$commandText = Get-Content -LiteralPath $commandPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8

Assert-True (
    ($providerText -match 'sealed\s+class\s+ChipModeGizmoProvider\s*:\s*ITriggerExternalGizmoProvider') -and
    ($providerText -match 'GetModExtension<TriggerLoadoutPanelExtension>') -and
    ($providerText -match 'ITriggerLoadoutReader') -and
    ($providerText -match 'ITriggerLoadoutCommands')
) 'The provider must use the existing player permission and Core formal surfaces only.'

Assert-True (
    ($providerText -match 'IsActive') -and
    ($providerText -match 'IsBindingMirror') -and
    ($providerText -match 'modeOptions\.Count\s*<=\s*1') -and
    ($providerText -match 'RequestCycleChipMode') -and
    ($providerText -match 'RequestSwitchChipMode')
) 'The provider must skip inactive, mirrored, and single-mode chips and wire left/right click commands.'

Assert-True ($providerText -match 'groupable\s*=\s*false') `
    '每枚芯片的形态按钮必须关闭原版聚合，避免一次输入同时切换多个实例。'

Assert-True (
    ($providerText -match 'GizmoIconTexPath') -and
    ($providerText -match 'chip\.def\.uiIcon')
) 'The current mode icon must fall back to the chip item icon.'

Assert-True (
    ($commandText -match 'sealed\s+class\s+Command_ChipMode\s*:\s*Command_Action') -and
    ($commandText -match 'RightClickFloatMenuOptions') -and
    ($commandText -match 'RightClickOptionsGetter')
) 'Command_ChipMode must add only the vanilla right-click option surface.'

$registrationCount = [regex]::Matches(
    $bootstrapText,
    'TriggerExternalGizmoRegistry\.Register\(new\s+ChipModeGizmoProvider\(\)\)').Count
Assert-True ($registrationCount -eq 1) `
    'ContentBootstrap must register exactly one generic chip-mode gizmo provider.'

Write-Output 'ChipModeGizmoContentSmokeTests PASS'
