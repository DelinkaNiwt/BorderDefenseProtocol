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
$shootPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$shootText = Get-Content -LiteralPath $shootPath -Raw -Encoding utf8

Assert-True (
    ($shootText -match 'internal void NotifyShotFiredForRecoil\(int shotTick\)') -and
    ($shootText -match 'lastShotTick = shotTick;')
) 'BDP 射击 Verb 必须提供只写原版 lastShotTick 的内部通知成员。'

Assert-True (
    ($shootText -match 'VerbHostSurfaceAccess\.TryGetByResultId\(\s*CasterPawn,\s*sourceResultId,') -and
    ($shootText -match 'binding\.RangedVerb\.NotifyShotFiredForRecoil\(shotTick\)')
) '真实发射必须按来源 ResultId 同步对应正式远程 Verb。'

$tryEmitIndex = $shootText.IndexOf('internal bool TryEmitPlan(ProjectileInitPlan plan)')
$projectileIndex = $shootText.IndexOf('ThingDef projectileDef = plan.ProjectileDef', $tryEmitIndex)
$shotTickIndex = $shootText.IndexOf('NotifyShotFiredForRecoil(shotTick)', $tryEmitIndex)

Assert-True ($tryEmitIndex -ge 0) '必须保留 TryEmitPlan（发射计划）入口。'
Assert-True (
    ($projectileIndex -gt $tryEmitIndex) -and
    ($shotTickIndex -gt $projectileIndex)
) '必须先确认投射物定义，再记录成功开火时刻。'

Assert-True (
    ($shootText -notmatch 'Dictionary<string,\s*int>\s+\w*Recoil') -and
    ($shootText -notmatch 'Projectile.*Recoil.*Callback')
) '不得新增自定义后坐力计时字典或投射物回调。'

Write-Output 'DualWeaponRecoilShotOwnershipSmokeTests PASS'
