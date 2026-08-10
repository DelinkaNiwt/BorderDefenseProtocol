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
$categorySourcePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Defs\TriggerCategoryDef.cs'
$propertiesPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompProperties_TriggerBody.cs'
$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$categoryDefsPath = Join-Path $repoRoot '1.6\Defs\Trigger\TriggerCategoryDefs.xml'

Assert-True (Test-Path -LiteralPath $categorySourcePath -PathType Leaf) 'Core TriggerCategoryDef must exist.'
Assert-True (Test-Path -LiteralPath $categoryDefsPath -PathType Leaf) 'Formal trigger category defs must exist.'

$categoryText = Get-Content -LiteralPath $categorySourcePath -Raw -Encoding UTF8
$propertiesText = Get-Content -LiteralPath $propertiesPath -Raw -Encoding UTF8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding UTF8
$categoryDefsText = Get-Content -LiteralPath $categoryDefsPath -Raw -Encoding UTF8

Assert-True (
    ($categoryText -match 'namespace BDP\.Core\.Trigger') -and
    ($categoryText -match 'public sealed class TriggerCategoryDef\s*:\s*Def') -and
    ($categoryText -notmatch 'enum\s+TriggerCategory')
) 'TriggerCategoryDef must be a public extensible Core Def, not a fixed enum.'

Assert-True ($propertiesText -match 'public\s+TriggerCategoryDef\s+triggerCategory\s*;') `
    'CompProperties_TriggerBody must expose the triggerCategory Def reference.'

Assert-True (
    ($triggerBodyText -match 'SpecialDisplayStats\s*\(') -and
    ($triggerBodyText -match 'triggerCategory') -and
    ($triggerBodyText -match '触发器类别') -and
    ($triggerBodyText -match 'StatDrawEntry')
) 'CompTriggerBody must display the resolved trigger category in item information.'

foreach ($defName in @('BDP_TriggerCategory_Border', 'BDP_TriggerCategory_Neighbor', 'BDP_TriggerCategory_Black')) {
    Assert-True ($categoryDefsText -match [regex]::Escape("<defName>$defName</defName>")) `
        ('Formal trigger category is missing: ' + $defName)
}

Write-Output 'TriggerCategoryDefContractSmokeTests PASS'
