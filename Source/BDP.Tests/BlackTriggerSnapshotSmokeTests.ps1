$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$factory = Get-Content -LiteralPath (Join-Path $formation 'Snapshot\BlackTriggerMakerSnapshotFactory.cs') -Raw -Encoding utf8
$snapshot = Get-Content -LiteralPath (Join-Path $formation 'Model\BlackTriggerMakerSnapshot.cs') -Raw -Encoding utf8
$traitOptionPath = Join-Path $formation 'Model\BlackTriggerTraitOptionSnapshot.cs'
Assert-True (Test-Path -LiteralPath $traitOptionPath) '必须存在冻结的特性选择项模型。'
$traitOption = Get-Content -LiteralPath $traitOptionPath -Raw -Encoding utf8
Assert-True ($factory -match 'Take\(3\)') '最高技能与特性选项都必须受三项上限约束。'
Assert-True ($factory -match 'ResolvePlayerColonists') '记录排名必须使用统一殖民者集合。'
Assert-True ($factory -match 'Parent' -and $factory -match 'Child' -and $factory -match 'Spouse') '传承人必须支持父母、子女和配偶。'
Assert-True ($factory -notmatch 'IsColonistPlayerControlled\) continue') '传承人不得受玩家派系限制。'
Assert-True ($snapshot -match 'TraitOptions') '来源快照必须保存最多三个特性选项。'
Assert-True ($snapshot -notmatch 'InheritedTrait') '来源快照不得提前替玩家确定继承特性。'
Assert-True ($traitOption -match 'TraitDef' -and $traitOption -match 'Degree') '特性选项必须保存原 TraitDef 和 degree。'
Write-Output 'BlackTriggerSnapshotSmokeTests PASS'
exit 0
