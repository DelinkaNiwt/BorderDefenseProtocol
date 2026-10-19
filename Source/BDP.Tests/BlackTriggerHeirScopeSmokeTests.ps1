$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }

$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$factoryPath = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Snapshot\BlackTriggerMakerSnapshotFactory.cs'
$factory = Get-Content -LiteralPath $factoryPath -Raw -Encoding utf8

$fillHeirs = [regex]::Match(
    $factory,
    'private static void FillHeirs\(.*?(?=\r?\n\s*///|\z)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
$resolvePlayerColonists = [regex]::Match(
    $factory,
    'private static List<Pawn> ResolvePlayerColonists\(\).*?(?=\r?\n\s*///|\z)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline).Value

Assert-True ($fillHeirs -match 'ResolvePlayerColonists\(\)') '传承人候选必须使用玩家殖民者集合。'
Assert-True ($fillHeirs -notmatch 'ResolveAllLivingHumanlikes\(\)') '传承人候选不能直接使用跨派系人类集合。'
Assert-True ($resolvePlayerColonists -match 'pawn\.IsColonist') '玩家殖民者集合必须复用原版 Pawn.IsColonist 判定。'
Assert-True ($resolvePlayerColonists -notmatch 'IsColonistPlayerControlled') '玩家殖民者集合不能限制为已生成且无精神状态的单位。'

Write-Output 'BlackTriggerHeirScopeSmokeTests PASS'
exit 0
