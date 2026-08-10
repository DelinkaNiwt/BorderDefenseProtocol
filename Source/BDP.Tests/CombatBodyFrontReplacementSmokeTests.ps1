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

$frontStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontState.cs'
$frontModePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontMode.cs'
$frontPresetDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontPresetDef.cs'
$hostConfigPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyHostConfigDef.cs'
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$combatBodyConfigXmlPath = Join-Path $repoRoot '1.6\Defs\CombatBodyDef\Config.xml'

Assert-True -Condition (Test-Path -LiteralPath $frontStatePath) -Message 'CombatBodyFrontState must exist.'
Assert-True -Condition (Test-Path -LiteralPath $frontModePath) -Message 'CombatBodyFrontMode must exist.'
Assert-True -Condition (Test-Path -LiteralPath $frontPresetDefPath) -Message 'CombatBodyFrontPresetDef must exist.'
Assert-True -Condition (Test-Path -LiteralPath $combatBodyConfigXmlPath) -Message 'CombatBodyDef/Config.xml must exist.'

$hostConfigText = Get-Content -LiteralPath $hostConfigPath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$frontStateText = Get-Content -LiteralPath $frontStatePath -Raw -Encoding utf8
$combatBodyConfigXmlText = Get-Content -LiteralPath $combatBodyConfigXmlPath -Raw -Encoding utf8

Assert-True -Condition ($hostConfigText -match 'public CombatBodyFrontMode frontMode = CombatBodyFrontMode\.MirrorOriginal;') -Message 'CombatBodyHostConfigDef must safely default frontMode to MirrorOriginal.'
Assert-True -Condition ($hostConfigText -match 'public string frontPresetDefName = null;') -Message 'CombatBodyHostConfigDef must expose an optional frontPresetDefName.'
Assert-True -Condition ($frontStateText -match 'ThingOwner<Apparel> combatApparelContainer;') -Message 'CombatBodyFrontState must hold combat apparel container.'
Assert-True -Condition ($bridgeText -match 'ApplyFrontReplacement\(') -Message 'PawnCombatBodyBridge must apply front replacement.'
Assert-True -Condition ($bridgeText -match 'RestoreFrontReplacement\(') -Message 'PawnCombatBodyBridge must restore front replacement.'
Assert-True -Condition ($bridgeText -match 'HashSet<int> activeApparelThingIds = new HashSet<int>\(frontState\.ActiveApparelThingIds\);') -Message 'PawnCombatBodyBridge must identify front apparel by saved thing ids.'
Assert-True -Condition ($bridgeText -match 'Where\(apparel => activeApparelThingIds\.Contains\(apparel\.thingIDNumber\)\)') -Message 'PawnCombatBodyBridge must leave unrelated currently worn apparel untouched.'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontMode\.MirrorOriginal') -Message 'PawnCombatBodyBridge must support MirrorOriginal front mode.'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontMode\.Preset') -Message 'PawnCombatBodyBridge must support Preset front mode.'
Assert-True -Condition ($combatBodyConfigXmlText -notmatch 'BDP_DefaultCombatBodyFrontPreset|BDP_CombatBodyArmor') -Message 'CombatBody config defs must not retain the retired concrete front sample.'

Write-Output 'CombatBodyFrontReplacement PASS'
