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

$compPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionInfoHediffComp.cs'
$propsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionInfoHediffCompProperties.cs'
$injectorPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionInfoInjector.cs'
$bootstrapPath = Join-Path $repoRoot 'Source\BDP\Core\Bootstrap\CombatBodyWoundInfoBootstrap.cs'
$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$patchRoot = Join-Path $repoRoot 'Source\BDP\Patches'

Assert-True (Test-Path -LiteralPath $compPath) 'CombatBodyWoundTrionInfoHediffComp.cs must exist.'
Assert-True (Test-Path -LiteralPath $propsPath) 'CombatBodyWoundTrionInfoHediffCompProperties.cs must exist.'
Assert-True (Test-Path -LiteralPath $injectorPath) 'CombatBodyWoundTrionInfoInjector.cs must exist.'
Assert-True (Test-Path -LiteralPath $bootstrapPath) 'CombatBodyWoundInfoBootstrap.cs must exist.'
Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'

$compText = Get-Content -LiteralPath $compPath -Raw -Encoding utf8
$propsText = Get-Content -LiteralPath $propsPath -Raw -Encoding utf8
$injectorText = Get-Content -LiteralPath $injectorPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$patchText = (Get-ChildItem -LiteralPath $patchRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True ($compText -match 'override\s+string\s+CompTipStringExtra') 'Wound Trion info comp must use the vanilla HediffComp tooltip extension.'
Assert-True ($compText -match 'CombatBodyWoundTrionDrainUtility\.TryResolvePublishedDrainPerSecond') 'Wound Trion info comp must read the active Trion ledger entry, not only the theoretical wound calculation.'
Assert-True ($compText -notmatch 'CombatBodyWoundTrionDrainUtility\.TryResolveDrainPerSecond') 'Wound Trion info comp must stop showing theoretical drain after the ledger entry expires.'
Assert-True ($utilityText -match 'TryResolvePublishedDrainPerSecond') 'Wound drain utility must expose a published-ledger drain query for display.'
Assert-True ($utilityText -match 'GetDrainSnapshot\(\)') 'Published wound drain display must read the Trion reader snapshot.'
Assert-True ($utilityText -match 'BuildDrainKey') 'Wound drain utility must own the shared wound drain key builder.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.BuildDrainKey') 'Wound drain binding must use the same wound drain key builder as display.'
Assert-True ($compText -match 'Trion流失：') 'Wound Trion info comp must show Trion drain wording.'
Assert-True ($compText -match 'return\s+null;') 'Wound Trion info comp must stay silent when no positive drain exists.'
Assert-True ($propsText -match 'compClass\s*=\s*typeof\(CombatBodyWoundTrionInfoHediffComp\)') 'Wound Trion info comp properties must bind the comp class.'
Assert-True ($injectorText -match 'DefDatabase<HediffDef>\.AllDefsListForReading') 'Wound Trion info injector must iterate loaded HediffDefs.'
Assert-True ($injectorText -match 'typeof\(Hediff_Injury\)\.IsAssignableFrom') 'Wound Trion info injector must first target injury hediff defs.'
Assert-True ($injectorText -match 'HasInfoComp') 'Wound Trion info injector must have a duplicate guard.'
Assert-True ($bootstrapText -match 'StaticConstructorOnStartup') 'Wound Trion info injector must be called from a RimWorld startup hook.'
Assert-True ($bootstrapText -match 'CombatBodyWoundTrionInfoInjector\.Apply\(\)') 'Wound Trion info bootstrap must call the injector.'
Assert-True ($patchText -notmatch 'HealthCard') 'Wound Trion info display must not add a HealthCard UI patch.'

Write-Output 'CombatBodyWoundTrionInfoSmokeTests PASS'
