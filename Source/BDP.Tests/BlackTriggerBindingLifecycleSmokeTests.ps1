$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$indexPath = Join-Path $formation 'Binding\BlackTriggerBindingIndex.cs'
$traitPath = Join-Path $formation 'Inheritance\Trait_BlackTriggerInherited.cs'
$servicePath = Join-Path $formation 'Inheritance\BlackTriggerInheritedTraitService.cs'
foreach ($path in @($indexPath, $traitPath, $servicePath)) { Assert-True (Test-Path -LiteralPath $path) "缺少绑定或真实特性设施：$path" }
$compatibility = Get-Content -LiteralPath (Join-Path $formation 'Binding\BlackTriggerCompatibilityEvaluator.cs') -Raw -Encoding utf8
$index = Get-Content -LiteralPath $indexPath -Raw -Encoding utf8
$trait = Get-Content -LiteralPath $traitPath -Raw -Encoding utf8
$service = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
Assert-True ($compatibility -notmatch 'IsColonistPlayerControlled') '装备适格不得按玩家派系拒绝。'
Assert-True ($index -match 'Rebuild' -and $index -match 'Pawn') '绑定索引必须能从现存物品重建。'
Assert-True ($trait -match ': Trait' -and $trait -match 'SourceThingId') '继承结果必须是真实 Trait 子类并记录来源物品。'
Assert-True ($service -match 'GainTrait' -and $service -match 'RemoveTrait') '真实特性必须随装备增删。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $formation 'Inheritance\Hediff_BlackTriggerInheritedEffect.cs'))) '不得继续使用 Hediff 继承。'
Write-Output 'BlackTriggerBindingLifecycleSmokeTests PASS'
exit 0
