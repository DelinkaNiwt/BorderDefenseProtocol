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
$modProjectsRoot = Split-Path -Parent $repoRoot
$devHarnessDefsRoot = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs'
$mainDefsRoot = Join-Path $repoRoot '1.6\Content\Defs'

$formalChipFiles = @(
    'Things\Items\Chips\Test\ThingDefs_TestChips_PassiveMixed.xml',
    'Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml',
    'Things\Items\Chips\Test\ThingDefs_TestChips_AbilityHediff.xml'
)

$formalConfigs = @()
foreach ($relativePath in $formalChipFiles) {
    $path = Join-Path $devHarnessDefsRoot $relativePath
    Assert-True (Test-Path -LiteralPath $path) "Formal candidate chip file must exist: $path"

    [xml]$document = Get-Content -LiteralPath $path -Raw -Encoding utf8
    $formalConfigs += @(
        $document.SelectNodes(
            '//*[@Class="BDP.Core.Chips.ChipDefinitionConfig"]'
        )
    )
}

Assert-True ($formalConfigs.Count -eq 13) 'The remaining three candidate files must contain exactly 13 ChipDefinitionConfig nodes.'

$migratedChipFiles = @(
    'Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml',
    'Things\Items\Chips\Shield\ThingDefs_Chip_EnergyShield.xml'
)
foreach ($relativePath in $migratedChipFiles) {
    $path = Join-Path $mainDefsRoot $relativePath
    Assert-True (Test-Path -LiteralPath $path) "Formal main-mod chip file must exist: $path"

    [xml]$document = Get-Content -LiteralPath $path -Raw -Encoding utf8
    $formalConfigs += @(
        $document.SelectNodes(
            '//*[@Class="BDP.Core.Chips.ChipDefinitionConfig"]'
        )
    )
}

Assert-True ($formalConfigs.Count -eq 16) 'The remaining candidate files and migrated formal files must contain exactly 16 ChipDefinitionConfig nodes.'

foreach ($config in $formalConfigs) {
    $ownerDefName = $config.ParentNode.ParentNode.defName
    $activationRequirements = @($config.SelectNodes('./ActivationRequirements'))
    Assert-True (
        $activationRequirements.Count -eq 1
    ) "$ownerDefName must declare exactly one ActivationRequirements list."

    $requirements = @($activationRequirements[0].SelectNodes('./li'))
    $intensityRequirements = @(
        $requirements | Where-Object {
            $_.GetAttribute('Class') -eq 'BDP.Core.Requirements.TrionIntensityRequirement'
        }
    )
    Assert-True (
        $intensityRequirements.Count -eq 1
    ) "$ownerDefName must declare exactly one TrionIntensityRequirement."

    if ($ownerDefName -eq 'BDP_Chip_Senku') {
        Assert-True (
            $requirements.Count -eq 3 -and
            $intensityRequirements[0].Minimum -eq '4'
        ) 'Senku must declare its three confirmed requirements and Trion intensity level 4.'
        Assert-True (
            $null -ne $config.SelectSingleNode(
                './ActivationRequirements/li[@Class="BDP.Core.Requirements.SkillLevelRequirement"][Skill="Melee"][MinimumLevel="10"]'
            )
        ) 'Senku must require Melee level 10.'
        Assert-True (
            $null -ne $config.SelectSingleNode(
                './ActivationRequirements/li[@Class="BDP.Core.Requirements.SkillLevelRequirement"][Skill="Shooting"][MinimumLevel="6"]'
            )
        ) 'Senku must require Shooting level 6.'
    }
    else {
        Assert-True (
            $requirements.Count -eq 1 -and
            $intensityRequirements[0].Minimum -eq '1'
        ) "$ownerDefName must keep the current sole Trion intensity requirement at level 1."
        Assert-True (
            $null -eq $config.SelectSingleNode('./ActivationRequirements/li[@Class="BDP.Core.Requirements.SkillLevelRequirement"]')
        ) "$ownerDefName must not invent an unused skill-level requirement."
    }

    Assert-True (
        $null -eq $config.SelectSingleNode('./Trion/PowerRequirement')
    ) "$ownerDefName must remove the obsolete chip-level PowerRequirement field."
}

$mainChipConfigs = @()
Get-ChildItem -LiteralPath $mainDefsRoot -Recurse -Filter '*.xml' | ForEach-Object {
    [xml]$document = Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
    $mainChipConfigs += @(
        $document.SelectNodes('//*[@Class="BDP.Core.Chips.ChipDefinitionConfig"]')
    )
}

Assert-True (
    $mainChipConfigs.Count -eq 3
) 'The main mod must contain exactly the three explicitly migrated chip definitions.'

Write-Output 'ChipActivationRequirementAuthoringSmokeTests PASS'
