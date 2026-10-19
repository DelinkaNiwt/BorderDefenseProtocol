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
$presetPath = Join-Path $modRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
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
    '(?s)<BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>\s*<defName>BDP_ChipFunction_Chameleon</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
Assert-True $presetMatch.Success 'BDP_ChipFunction_Chameleon must be declared as a formal chip function template.'
$chameleon = $presetMatch.Value

Assert-True ($chameleon -match '<Category>BDP_ChipCategory_GeneralSkill</Category>') `
    'Chameleon must use the general skill chip category.'
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
Assert-True ($chameleon -match '<EntryType>Hediff</EntryType>[\s\S]*<HediffDefName>BDP_Hediff_Chameleon</HediffDefName>') `
    'Chameleon must project BDP_Hediff_Chameleon.'
Assert-True ($chameleon -match '<HediffControlMode>AbilityButton</HediffControlMode>') `
    'Chameleon Hediff must be controlled by its Ability button.'
Assert-True ($chameleon -match '<EntryType>Ability</EntryType>[\s\S]*<AbilityDefName>BDP_Ability_Chameleon</AbilityDefName>') `
    'Chameleon must expose a dedicated Ability toggle entry.'
Assert-True ($chameleon -notmatch '<ActivationAudio>') `
    'Chameleon must not play audio during chip activation.'
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

$abilityPath = Join-Path $modRoot '1.6\Content\Defs\AbilityDef\Chameleon.xml'
$abilityText = Get-Content -LiteralPath $abilityPath -Raw -Encoding utf8
Assert-True ($abilityText -match 'CompProperties_AbilityEffect_ApplyExpressionHediff') `
    'Chameleon must use the generic Core one-shot expression Hediff effect.'
Assert-True ($abilityText -match '<warmupTime>0.5</warmupTime>') `
    'Chameleon Ability must have a 0.5 second release warmup.'
Assert-True ($abilityText -match '<warmupStartSound>Power_OnSmall</warmupStartSound>') `
    'Chameleon Ability must play the release warmup start sound.'
Assert-True ($abilityText -match '<warmupSound>GasReleasing</warmupSound>') `
    'Chameleon Ability must maintain the release warmup sound.'
Assert-True ($abilityText -match '<warmupPreEndSound>Power_OffSmall</warmupPreEndSound>') `
    'Chameleon Ability must play the release warmup end sound.'
Assert-True ($abilityText -match '<warmupPreEndSoundSeconds>0.1</warmupPreEndSoundSeconds>') `
    'Chameleon Ability must schedule the release warmup end sound before completion.'
Assert-True ($abilityText -match '<cooldownTicksRange>2400</cooldownTicksRange>') `
    'Chameleon Ability must use a 40 second native cooldown after release.'
Assert-True ($abilityText -match '<cooldownKey>BDP_AbilityCooldown_Chameleon</cooldownKey>') `
    'Chameleon Ability must declare its shared source cooldown key.'
Assert-True ($abilityText -match '<verbClass>BDP\.Core\.Abilities\.BdpVerb_CastSelfAbility</verbClass>') `
    'Chameleon Ability must use the generic self-cast Verb.'
Assert-True ($hediffText -match '<showRemainingTime>true</showRemainingTime>') `
    'Chameleon Hediff must show the remaining disappearing time in its label.'
Assert-True ($hediffText -match 'HediffCompProperties_Disappears') `
    'Chameleon Hediff must use the vanilla disappearing component.'
Assert-True ($hediffText -match '<disappearsAfterTicks>1200</disappearsAfterTicks>') `
    'Chameleon Hediff must last for 20 seconds.'
Assert-True ($abilityText -match '(?s)<casterMustBeCapableOfViolence>false</casterMustBeCapableOfViolence>\s*<!-- AbilityDef 的目标模式：释放自身状态，不要求额外目标。 -->\s*<targetRequired>false</targetRequired>') `
    'Chameleon targetRequired must be declared on AbilityDef.'
Assert-True ($abilityText -notmatch '(?s)<verbProperties>.*<targetRequired>') `
    'Chameleon targetRequired must not be declared inside VerbProperties.'
Assert-True ($abilityText -match '<targetable>false</targetable>') `
    'Chameleon self-cast Ability must not expose a ranged targeting source.'
Assert-True ($abilityText -match '<range>0</range>') `
    'Chameleon self-cast Ability must not inherit the vanilla ranged verb range.'
Assert-True ($abilityText -match '<drawAimPie>false</drawAimPie>') `
    'Chameleon warmup must not draw the vanilla aim pie.'
Assert-True ($abilityText -match '<requireLineOfSight>false</requireLineOfSight>') `
    'Chameleon self-cast Ability must not require ranged line of sight.'
Assert-True ($abilityText -notmatch '手动开启或关闭|关闭变色龙芯片') `
    'Chameleon Ability description must not promise chip shutdown.'
Assert-True ($hediffText -notmatch '自动关闭变色龙芯片') `
    'Chameleon Hediff description must not promise chip shutdown.'
Assert-True ($chameleon -notmatch '自动关闭芯片|立即关闭') `
    'Chameleon chip description must describe Hediff removal instead of chip shutdown.'

foreach ($key in @(
    'BDP_ChipFunction_Chameleon',
    'BDP_ChipFunction_Chameleon_Description',
    'BDP_Hediff_Chameleon.label',
    'BDP_Hediff_Chameleon.description',
    'BDP_ChipExclusionGroup_Stealth.label'
)) {
    Assert-True ($languageText -match ('<' + [regex]::Escape($key) + '>')) `
        "Language package must contain key: $key"
}

Write-Output 'ChameleonDefinitionSmokeTests PASS'
