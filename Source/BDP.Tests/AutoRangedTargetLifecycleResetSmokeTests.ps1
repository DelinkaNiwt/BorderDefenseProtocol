$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$verbPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_Shoot.cs'
$hostManagerPath = Join-Path $sourceRoot 'BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$lifecyclePath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackTargetLifecycleAccess.cs'
$patchPath = Join-Path $sourceRoot 'BDP\Patches\Patch_Pawn_HealthTracker_AttackTargetLifecycle.cs'

$verbText = Read-Source $verbPath
$hostManagerText = Read-Source $hostManagerPath
$lifecycleText = Read-Source $lifecyclePath
$patchText = Read-Source $patchPath

Assert-True (
    ($verbText -match 'NotifyTargetLifecycleChanged\(Pawn\s+target\)') -and
    ($verbText -match 'sessionTarget\.HasThing') -and
    ($verbText -match 'AttackExecutionJobDefs\.RangedAttackExecution') -and
    ($verbText -match 'Reset\(\)')
) '远程宿主必须在目标生命周期变化时只清理自动会话，并保留手动强制攻击会话。'

Assert-True (
    ($hostManagerText -match 'NotifyTargetLifecycleChanged\(Pawn\s+target\)') -and
    ($hostManagerText -match 'RangedVerb\??\.NotifyTargetLifecycleChanged\(target\)')
) '正式宿主管理器必须把目标生命周期通知转发给远程宿主。'

Assert-True (
    ($lifecycleText -match 'Find\.Maps') -and
    ($lifecycleText -match 'AllPawnsSpawned') -and
    ($lifecycleText -match 'VerbHostManager')
) '目标生命周期通知必须覆盖当前地图上的 BDP 宿主，不能依赖易丢失的静态会话索引。'

Assert-True (
    ($patchText -match 'HarmonyPatch\(typeof\(Pawn_HealthTracker\),\s*"MakeDowned"\)') -and
    ($patchText -match 'HarmonyPatch\(typeof\(Pawn_HealthTracker\),\s*"SetDead"\)') -and
    ($patchText -match 'HarmonyPatch\(typeof\(Pawn_HealthTracker\),\s*"MakeUndowned"\)') -and
    ($patchText -match 'HarmonyPatch\(typeof\(Pawn_HealthTracker\),\s*nameof\(Pawn_HealthTracker\.Notify_Resurrected\)\)')
) '倒地、死亡、解除倒地和复活都必须接入同一条目标生命周期收口。'

Write-Output 'AutoRangedTargetLifecycleResetSmokeTests PASS'
