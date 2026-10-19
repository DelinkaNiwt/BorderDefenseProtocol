$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$legacyPaths = @('Defs\BlackTriggerFactConditionGroup.cs','Defs\BlackTriggerTraitInheritanceDef.cs','Matching\BlackTriggerFactPair.cs','World\BlackTriggerReservation.cs','Inheritance\BlackTriggerTraitInheritanceSelector.cs','Inheritance\BlackTriggerInheritedEffectService.cs','Inheritance\Hediff_BlackTriggerInheritedEffect.cs')
foreach ($relative in $legacyPaths) { Assert-True (-not (Test-Path -LiteralPath (Join-Path $formation $relative))) "旧模型文件仍存在：$relative" }
$roots = @($formation,(Join-Path $root '1.6\Content\Defs\BlackTrigger'),(Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml'),(Join-Path $root 'Languages\ChineseSimplified (简体中文)\Keyed\BlackTrigger.xml'))
$pattern = 'FirstFactKey|SecondFactKey|LastCandidates|Reservations|ReservedThingDefName|formationGroups|minimumMakerPotential|BlackTriggerTraitInheritanceDef|Hediff_BlackTriggerInheritedEffect|形成倾向|最终意志|未定型遗物'
$matches = @()
foreach ($scanRoot in $roots) { if (Test-Path -LiteralPath $scanRoot) { $matches += @(rg -n --glob '*.cs' --glob '*.xml' $pattern $scanRoot 2>$null) } }
Assert-True ($matches.Count -eq 0) ("旧形成模型仍有运行时残留：`n" + ($matches -join "`n"))
Write-Output 'BlackTriggerLegacyModelRemovalSmokeTests PASS'
exit 0
