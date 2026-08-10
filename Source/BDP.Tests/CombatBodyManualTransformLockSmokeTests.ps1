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
$modRoot = Split-Path -Parent $sourceRoot
$configDefPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CombatBodyHostConfigDef.cs'
$phaseStatePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\State\ICombatBodyPhaseState.cs'
$statePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\State\CombatBodyState.cs'
$servicePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Flow\CombatBodyCoordinator.cs'
$sessionPath = Join-Path $sourceRoot 'BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$gizmoPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\External\CombatBodyTriggerGizmoProvider.cs'
$configXmlPath = Join-Path $modRoot '1.6\Defs\CombatBodyDef\Config.xml'
$languagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Commands.xml'

$configDefText = Get-Content -LiteralPath $configDefPath -Raw -Encoding utf8
$phaseStateText = Get-Content -LiteralPath $phaseStatePath -Raw -Encoding utf8
$stateText = Get-Content -LiteralPath $statePath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$sessionText = Get-Content -LiteralPath $sessionPath -Raw -Encoding utf8
$gizmoText = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8
$configXmlText = Get-Content -LiteralPath $configXmlPath -Raw -Encoding utf8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8

Assert-True -Condition ($configDefText -match 'public int manualTransformLockTicks = 12;') -Message '短时锁缺少 12 tick 安全默认值。'
Assert-True -Condition ($configXmlText -match '<manualTransformLockTicks>12</manualTransformLockTicks>') -Message '正式 XML 未配置 12 tick 短时锁。'
Assert-True -Condition ($phaseStateText -match 'void BeginManualTransformLock\(int lockTicks\);') -Message '阶段真值口缺少启动短时锁的成员。'
Assert-True -Condition ($stateText -match 'private int manualTransformLockEndTick;') -Message '状态真值未保存短时锁截止 tick。'
Assert-True -Condition ($stateText -match 'Scribe_Values\.Look\(ref manualTransformLockEndTick') -Message '短时锁必须参与存档。'
Assert-True -Condition ($stateText -match 'public void BeginManualTransformLock\(int lockTicks\)') -Message '状态真值缺少启动短时锁的实现。'
Assert-True -Condition ($stateText -match 'Mathf\.Max\(0, lockTicks\)') -Message '负数短时锁必须按 0 处理。'
Assert-True -Condition ($stateText -match 'public bool CanActivate\(\)[\s\S]*phase == CombatBodyPhase\.Inactive[\s\S]*!IsManualTransformLocked\(\)') -Message '生成准入必须检查短时锁。'
Assert-True -Condition ($stateText -match 'public bool CanManualDeactivate\(\)[\s\S]*phase == CombatBodyPhase\.Active[\s\S]*!IsManualTransformLocked\(\)') -Message '解除准入必须检查短时锁。'
Assert-True -Condition ($serviceText -match 'internal void BeginManualTransformLock\(int lockTicks\)') -Message '原始相位服务必须只做短时锁转发。'
Assert-True -Condition ($sessionText -match 'bool activated = activationTransaction\.TryActivate\(OwnerPawn\);[\s\S]*if \(activated\)[\s\S]*BeginManualTransformLock\(\);') -Message '成功生成后必须启动短时锁。'
Assert-True -Condition ($sessionText -match 'public void RequestRelease\(\)[\s\S]*if \(!CanManualDeactivate\(\)\)[\s\S]*ExecuteExit\(CombatBodySessionExitMode\.Release\);[\s\S]*BeginManualTransformLock\(\);') -Message '成功主动解除后必须启动短时锁。'
Assert-True -Condition ($sessionText -match 'CombatBodyHostConfigResolver\.Resolve\(\)\.manualTransformLockTicks') -Message '正式命令必须读取 XML 短时锁配置。'
Assert-True -Condition ($gizmoText -match 'reader\.CanActivate\(\)[\s\S]*BDP_Command_CombatBody_TransformLocked') -Message '生成按钮必须显示短时锁反馈。'
Assert-True -Condition ($gizmoText -match 'reader\.CanManualDeactivate\(\)[\s\S]*BDP_Command_CombatBody_TransformLocked') -Message '解除按钮必须显示短时锁反馈。'
Assert-True -Condition ($languageText -match '<BDP_Command_CombatBody_TransformLocked>战斗体正在完成变换。</BDP_Command_CombatBody_TransformLocked>') -Message '语言包缺少短时锁提示。'

Write-Output 'CombatBodyManualTransformLock PASS'
