$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$path = Join-Path $repoRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'

[xml]$document = Get-Content -LiteralPath $path -Raw -Encoding utf8

function Get-ChipConfig {
    param([string]$DefName)
    return $document.SelectSingleNode(
        "//ThingDef[defName='$DefName']/modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']"
    )
}

$senku = Get-ChipConfig 'BDP_Chip_Senku'
$kogetsu = Get-ChipConfig 'BDP_Chip_Kogetsu'
Assert-True ($null -ne $senku -and $null -ne $kogetsu) 'Senku and Kogetsu chip configs must exist.'

$senkuRequirements = @($senku.SelectNodes('./ActivationRequirements/li'))
Assert-True ($senkuRequirements.Count -eq 3) 'Senku must declare exactly three activation requirements.'
Assert-True (
    $senkuRequirements[0].GetAttribute('Class') -eq 'BDP.Core.Requirements.TrionIntensityRequirement' -and
    $senkuRequirements[0].Minimum -eq '4'
) 'Senku requirement 1 must be Trion intensity level 4.'
Assert-True (
    $senkuRequirements[1].GetAttribute('Class') -eq 'BDP.Core.Requirements.SkillLevelRequirement' -and
    $senkuRequirements[1].Skill -eq 'Melee' -and
    $senkuRequirements[1].MinimumLevel -eq '10'
) 'Senku requirement 2 must be Melee level 10.'
Assert-True (
    $senkuRequirements[2].GetAttribute('Class') -eq 'BDP.Core.Requirements.SkillLevelRequirement' -and
    $senkuRequirements[2].Skill -eq 'Shooting' -and
    $senkuRequirements[2].MinimumLevel -eq '6'
) 'Senku requirement 3 must be Shooting level 6.'

$kogetsuRequirements = @($kogetsu.SelectNodes('./ActivationRequirements/li'))
Assert-True (
    $kogetsuRequirements.Count -eq 1 -and
    $kogetsuRequirements[0].GetAttribute('Class') -eq 'BDP.Core.Requirements.TrionIntensityRequirement' -and
    $kogetsuRequirements[0].Minimum -eq '1'
) 'Kogetsu must remain at the single Trion intensity level 1 requirement.'

Write-Output 'SenkuActivationRequirementSmokeTests PASS'
