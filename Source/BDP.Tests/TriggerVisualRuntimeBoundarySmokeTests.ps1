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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$visualProjectionPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualExpressionProjection.cs'
$presentationConfigPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionPresentationConfig.cs'
$visualPresetDefPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionVisualPresetDef.cs'
$visualResidentEntryPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualResidentEntry.cs'
$hostEquipmentRenderModePath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\HostEquipmentRenderMode.cs'
$executionFocusPolicyPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualExecutionFocusPolicy.cs'
$muzzleFollowPolicyPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\VisualMuzzleFollowPolicy.cs'
$runtimeServicesPath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\TriggerRuntimeServices.cs'
$visualRuntimeStatePath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\TriggerVisualRuntimeState.cs'
$equipmentPoseSamplePath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\EquipmentPoseSample.cs'
$visualRuntimeOwnerPath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\TriggerVisualRuntimeStateOwner.cs'
$bodyPath = Join-Path $bdpSourceRoot 'Core\Trigger\State\CompTriggerBody.cs'

$visualProjectionText = Get-Content -LiteralPath $visualProjectionPath -Raw -Encoding utf8
$presentationConfigText = Get-Content -LiteralPath $presentationConfigPath -Raw -Encoding utf8
$runtimeServicesText = Get-Content -LiteralPath $runtimeServicesPath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $visualPresetDefPath) 'ExpressionVisualPresetDef must exist as the final visual preset definition surface.'
Assert-True (Test-Path -LiteralPath $visualResidentEntryPath) 'VisualResidentEntry must exist as the published resident visual entry contract.'
Assert-True (Test-Path -LiteralPath $hostEquipmentRenderModePath) 'HostEquipmentRenderMode must exist as the final host-equipment render policy enum.'
Assert-True (Test-Path -LiteralPath $executionFocusPolicyPath) 'VisualExecutionFocusPolicy must exist as the final visual execution-focus policy enum.'
Assert-True (Test-Path -LiteralPath $muzzleFollowPolicyPath) 'VisualMuzzleFollowPolicy must exist as the final muzzle-follow policy enum.'
Assert-True (Test-Path -LiteralPath $visualRuntimeStatePath) 'TriggerVisualRuntimeState must exist as the dynamic visual runtime truth object.'
Assert-True (Test-Path -LiteralPath $equipmentPoseSamplePath) 'EquipmentPoseSample must exist as the equipped-weapon pose sample contract.'
Assert-True (Test-Path -LiteralPath $visualRuntimeOwnerPath) 'TriggerVisualRuntimeStateOwner must exist as the dedicated visual runtime-state owner.'

Assert-True (
    ($presentationConfigText -match 'VisualPresetDefName') -and
    ($presentationConfigText -match 'CompositeVisualPresetDefName') -and
    ($presentationConfigText -match 'ForceSuppressHostEquipment') -and
    ($presentationConfigText -match 'VisualPriority')
) 'ExpressionPresentationConfig must expose lightweight visual references rather than keeping visual authoring outside the expression presentation surface.'

Assert-True (
    ($visualProjectionText -match 'IReadOnlyList<VisualResidentEntry>\s+ResidentEntries') -and
    ($visualProjectionText -match 'HostEquipmentRenderMode\s+HostEquipmentRenderMode') -and
    ($visualProjectionText -match 'VisualExecutionFocusPolicy\s+ExecutionFocusPolicy') -and
    ($visualProjectionText -match 'VisualMuzzleFollowPolicy\s+MuzzleFollowPolicy')
) 'VisualExpressionProjection must expose resident entries plus static host/execution/muzzle policies.'

Assert-True (
    ($visualProjectionText -notmatch 'ExecutingFollowResultId') -and
    ($visualProjectionText -notmatch 'MuzzleFollowResultId')
) 'Final visual projection must stop pretending dynamic executing or muzzle truth can be carried by static result-id fields.'

Assert-True (
    ($runtimeServicesText -match 'TriggerVisualRuntimeStateOwner\s+TriggerVisualRuntimeStateOwner') -and
    ($bodyText -match 'TriggerVisualRuntimeStateOwner') -and
    ($bodyText -match 'PublishedVisualRuntimeState')
) 'CompTriggerBody runtime services must expose a dedicated visual runtime-state owner and a pure read surface for the published visual runtime state.'

Write-Output 'TriggerVisualRuntimeBoundarySmokeTests PASS'
