$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$base = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$trait = Get-Content -LiteralPath (Join-Path $base 'Inheritance\Trait_BlackTriggerInherited.cs') -Raw -Encoding utf8
$service = Get-Content -LiteralPath (Join-Path $base 'Inheritance\BlackTriggerInheritedTraitService.cs') -Raw -Encoding utf8
$comp = Get-Content -LiteralPath (Join-Path $base 'Persistence\CompBlackTriggerImprint.cs') -Raw -Encoding utf8
Assert-True ($trait -match ': Trait' -and $trait -match 'SourceThingId' -and $trait -match 'ExposeData') '继承结果必须是真实、可存档、带来源的 Trait。'
Assert-True ($service -match 'GainTrait' -and $service -match 'RemoveTrait') '继承特性必须随装备加入和移除。'
Assert-True ($service -match 'ConflictsWith' -or $service -match 'Conflicts') '添加前必须检查对立特性。'
Assert-True ($comp -match 'Notify_Equipped' -and $comp -match 'Notify_Unequipped') '印记组件必须接入原版装备生命周期。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $base 'Inheritance\Hediff_BlackTriggerInheritedEffect.cs'))) '继承不得使用 Hediff。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $root '1.6\Content\Defs\HediffDef\BlackTriggerInheritance.xml'))) '继承 Hediff XML 必须删除。'
Write-Output 'BlackTriggerInheritanceSmokeTests PASS'
exit 0
