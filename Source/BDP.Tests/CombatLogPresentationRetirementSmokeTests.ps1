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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'
$retiredFiles = @(
    'Core\CombatLog\CombatLogPresentationSnapshot.cs',
    'Core\CombatLog\CombatLogPresentationRecord.cs',
    'Core\CombatLog\CombatLogPresentationFormatter.cs',
    'Core\CombatLog\BdpCombatLogPresentationRepository.cs',
    'Patches\Patch_LogEntry_ToGameStringFromPOV_BdpCombatLog.cs'
)

foreach ($relativePath in $retiredFiles) {
    $path = Join-Path $bdpSourceRoot $relativePath
    Assert-True (-not (Test-Path -LiteralPath $path)) "实验性战斗日志文件仍存在：$path"
}

$productionFiles = Get-ChildItem -LiteralPath $bdpSourceRoot -Recurse -File -Filter '*.cs'
$productionText = ($productionFiles | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
    }) -join "`n"

Assert-True ($productionText -notmatch 'CombatLogPresentationSnapshot|CombatLogPresentationRecord|CombatLogPresentationFormatter|BdpCombatLogPresentationRepository|Patch_LogEntry_ToGameStringFromPOV_BdpCombatLog|BattleLogPresentation|RegisterFire\(|RegisterImpact\(') '生产代码仍保留实验性战斗日志管线。'

$shootVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_Shoot.cs'
$projectilePath = Join-Path $bdpSourceRoot 'Core\Projectiles\BdpProjectile.cs'
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8

Assert-True ($shootVerbText -match 'new BattleLogEntry_RangedFire\(') 'BdpVerb_Shoot 必须继续写入原版远程开火日志。'
Assert-True ($projectileText -match 'new BattleLogEntry_RangedImpact\(') 'BdpProjectile 必须继续写入原版远程命中日志。'
Assert-True ($shootVerbText -match 'ResolveVanillaBattleLogWeaponDef\(') 'BdpVerb_Shoot 必须保留原版日志 weaponDef 安全适配。'
Assert-True ($projectileText -match 'ResolveVanillaBattleLogWeaponDef\(') 'BdpProjectile 必须保留原版日志 weaponDef 安全适配。'

Write-Output 'CombatLogPresentationRetirementSmokeTests PASS'
