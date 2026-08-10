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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$devHarnessDefsRoot = Join-Path $devHarnessRoot '1.6\Defs'

$chipLoadoutConfigPath = Join-Path $bdpSourceRoot 'Chips\Config\ChipLoadoutConfig.cs'
$chipLoadoutContractPath = Join-Path $bdpSourceRoot 'Chips\Contract\ChipLoadoutContract.cs'
$chipResolverPath = Join-Path $bdpSourceRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$chipValidatorPath = Join-Path $bdpSourceRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$triggerServicePath = Join-Path $bdpSourceRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'
$transitionServicePath = Join-Path $bdpSourceRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'
$interactionInterpreterPath = Join-Path $bdpSourceRoot 'Trigger\Interaction\TriggerInteractionInterpreter.cs'
$contextsPath = Join-Path $bdpSourceRoot 'Trigger\State\CompTriggerBody.Contexts.cs'
$readsPath = Join-Path $bdpSourceRoot 'Trigger\State\CompTriggerBody.Reads.cs'
$testChipDefsPath = Join-Path $devHarnessDefsRoot 'Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'

$chipLoadoutConfigText = Get-Content -LiteralPath $chipLoadoutConfigPath -Raw -Encoding utf8
$chipLoadoutContractText = Get-Content -LiteralPath $chipLoadoutContractPath -Raw -Encoding utf8
$chipResolverText = Get-Content -LiteralPath $chipResolverPath -Raw -Encoding utf8
$chipValidatorText = Get-Content -LiteralPath $chipValidatorPath -Raw -Encoding utf8
$triggerServiceText = Get-Content -LiteralPath $triggerServicePath -Raw -Encoding utf8
$transitionServiceText = Get-Content -LiteralPath $transitionServicePath -Raw -Encoding utf8
$interactionInterpreterText = Get-Content -LiteralPath $interactionInterpreterPath -Raw -Encoding utf8
$contextsText = Get-Content -LiteralPath $contextsPath -Raw -Encoding utf8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8
$testChipDefsText = Get-Content -LiteralPath $testChipDefsPath -Raw -Encoding utf8

Assert-True (
    ($chipLoadoutConfigText -match 'public int ActivationDelayTicks = -1;') -and
    ($chipLoadoutConfigText -match 'public int DeactivationDelayTicks = -1;')
) 'ChipLoadoutConfig must expose optional activation / deactivation delay fields with -1 sentinel defaults.'

Assert-True (
    ($chipLoadoutContractText -match 'public int ActivationDelayTicks;') -and
    ($chipLoadoutContractText -match 'public int DeactivationDelayTicks;')
) 'ChipLoadoutContract must carry activation / deactivation delays into runtime.'

Assert-True (
    ($chipResolverText -match 'ActivationDelayTicks = config\.ActivationDelayTicks') -and
    ($chipResolverText -match 'DeactivationDelayTicks = config\.DeactivationDelayTicks')
) 'ChipDefinitionContractResolver must translate chip-level activation / deactivation delays into the loadout contract.'

Assert-True (
    ($chipValidatorText -match 'loadout\.ActivationDelayTicks < -1') -and
    ($chipValidatorText -match 'loadout\.DeactivationDelayTicks < -1')
) 'ChipDefinitionValidator must reject delay values below the -1 missing sentinel.'

Assert-True (
    ($triggerServiceText -match 'DefaultChipActivationDelayTicks = 60') -and
    ($triggerServiceText -match 'DefaultChipDeactivationDelayTicks = 30') -and
    ($triggerServiceText -match 'ResolveChipActivationDelayTicks\(Thing chip\)') -and
    ($triggerServiceText -match 'ResolveChipDeactivationDelayTicks\(Thing chip\)')
) 'TriggerService must resolve chip-level activation / deactivation delays with 60 / 30 defaults.'

Assert-True (
    ($contextsText -match 'ResolveChipActivationDelayTicks = triggerService\.ResolveChipActivationDelayTicks') -and
    ($contextsText -match 'ResolveChipDeactivationDelayTicks = triggerService\.ResolveChipDeactivationDelayTicks')
) 'CompTriggerBody switch context must pass chip-level timing resolvers into the transition service.'

Assert-True (
    ($transitionServiceText -match 'Func<Thing, int> resolveChipActivationDelayTicks') -and
    ($transitionServiceText -match 'Func<Thing, int> resolveChipDeactivationDelayTicks') -and
    ($transitionServiceText -match 'BuildDeactivatingContext\(\s*resolveChipDeactivationDelayTicks') -and
    ($transitionServiceText -match 'BuildActivatingContext\(\s*resolveChipActivationDelayTicks')
) 'TriggerSwitchTransitionService must build phases from chip-specific activation / deactivation delays.'

Assert-True (
    ($interactionInterpreterText -match 'Func<Thing, int> resolveChipActivationDelayTicks') -and
    ($interactionInterpreterText -match 'Func<Thing, int> resolveChipDeactivationDelayTicks') -and
    ($interactionInterpreterText -match 'BuildDeactivatingContext\(\s*resolveChipDeactivationDelayTicks') -and
    ($interactionInterpreterText -match 'BuildActivatingContext\(\s*resolveChipActivationDelayTicks')
) 'TriggerInteractionInterpreter must preview activation / deactivation timing from the actual chip-specific durations.'

Assert-True (
    $readsText -notmatch 'Props\.switchCooldownTicks'
) 'CompTriggerBody read-side switch resolution must stop depending on CompProperties.switchCooldownTicks.'

Assert-True (
    ($testChipDefsText -match '<ActivationDelayTicks>') -and
    ($testChipDefsText -match '<DeactivationDelayTicks>')
) 'DevHarness test chip defs must demonstrate chip-level activation / deactivation delay authoring.'

Write-Output 'TriggerSwitchTimingSmokeTests PASS'
