$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modRoot = $repoRoot
$presetPath = Join-Path $modRoot '1.6\Content\Defs\ChipActionPresetDef\Presets.xml'
$classificationPath = Join-Path $modRoot '1.6\Content\Defs\ChipDef\Classification.xml'
$hediffPath = Join-Path $modRoot '1.6\Content\Defs\HediffDef\Chameleon.xml'
$languageRoot = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)'

Assert-True (Test-Path -LiteralPath $presetPath) 'Chip preset XML must exist.'
Assert-True (Test-Path -LiteralPath $classificationPath) 'Chip classification XML must exist.'
Assert-True (Test-Path -LiteralPath $hediffPath) 'Chameleon Hediff XML must exist.'

$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8
$classificationText = Get-Content -LiteralPath $classificationPath -Raw -Encoding utf8
$hediffText = Get-Content -LiteralPath $hediffPath -Raw -Encoding utf8
$languageText = (Get-ChildItem -LiteralPath $languageRoot -Recurse -Filter '*.xml' |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"

$presetMatch = [regex]::Match(
    $presetText,
    '(?s)<BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipActionPresetDef>\s*<defName>BDP_Preset_Chameleon</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipActionPresetDef>')
Assert-True $presetMatch.Success 'BDP_Preset_Chameleon must be declared as a formal chip preset.'
$chameleon = $presetMatch.Value

Assert-True ($chameleon -match '<Category>BDP_ChipCategory_Status</Category>') `
    'Chameleon must use the existing status chip category.'
Assert-True ($chameleon -match '<SlotRegion>MainSub</SlotRegion>') `
    'Chameleon must be switchable in the main/sub slots.'
Assert-True ($chameleon -match '<SlotOccupancy>Single</SlotOccupancy>') `
    'Chameleon must occupy one main/sub slot.'
Assert-True ($chameleon -match '<ActivationDelayTicks>30</ActivationDelayTicks>') `
    'Chameleon activation delay must be 30 ticks.'
Assert-True ($chameleon -match '<DeactivationDelayTicks>0</DeactivationDelayTicks>') `
    'Chameleon deactivation must be immediate.'
Assert-True ($chameleon -match '<CapacityCost>100</CapacityCost>') `
    'Chameleon capacity cost must be 100.'
Assert-True ($chameleon -match '<ActivationCost>15</ActivationCost>') `
    'Chameleon activation cost must be 15.'
Assert-True ($chameleon -match 'BDP_ChipExclusionGroup_Stealth') `
    'Chameleon must belong to the stealth activation exclusion group.'
Assert-True ($chameleon -match '<HediffDefName>BDP_Hediff_Chameleon</HediffDefName>') `
    'Chameleon must project BDP_Hediff_Chameleon.'
Assert-True ($chameleon -match '<TotalPerSecond>5</TotalPerSecond>') `
    'Chameleon must sustain at 5 Trion per second.'
Assert-True ($chameleon -match '<SourceCount>1</SourceCount>') `
    'Chameleon sustain cost must define the one-source tier.'
Assert-True ($chameleon -notmatch '<UseCost>') `
    'Chameleon Hediff expression must not declare an unrelated per-use cost.'

Assert-True ($classificationText -match 'BDP_ChipExclusionGroup_Stealth') `
    'The stealth exclusion group Def must exist.'
Assert-True ($hediffText -match 'BDP\.Content\.Chameleon\.HediffCompProperties_BdpInvisibility') `
    'Chameleon Hediff must use the BDP DLC-free invisibility adapter.'

foreach ($key in @(
    'BDP_ChipAction_Chameleon',
    'BDP_ChipAction_Chameleon_Description',
    'BDP_Hediff_Chameleon.label',
    'BDP_Hediff_Chameleon.description',
    'BDP_ChipExclusionGroup_Stealth.label'
)) {
    Assert-True ($languageText -match ('<' + [regex]::Escape($key) + '>')) `
        "Language package must contain key: $key"
}

Write-Output 'ChameleonDefinitionSmokeTests PASS'
