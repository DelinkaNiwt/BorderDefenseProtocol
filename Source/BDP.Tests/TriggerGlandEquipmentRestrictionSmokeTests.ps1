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

function Read-Utf8 {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP'

$eligibilityPath = Join-Path $coreRoot 'Core\Trigger\Eligibility\TriggerBodyEquipmentEligibility.cs'
$canEquipPatchPath = Join-Path $coreRoot 'Patches\Patch_EquipmentUtility_CanEquip_TriggerBodyGlandRequirement.cs'
$triggerBodyPath = Join-Path $coreRoot 'Core\Trigger\State\CompTriggerBody.cs'
$equipmentTickPatchPath = Join-Path $coreRoot 'Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'
$genePath = Join-Path $coreRoot 'Core\Genes\Gene_TrionGland.cs'
$combatCommandsPath = Join-Path $coreRoot 'Core\CombatBody\Access\Contracts\ICombatBodyCommands.cs'
$sessionServicePath = Join-Path $coreRoot 'Core\CombatBodySession\CombatBodySessionService.cs'
$activationTransactionPath = Join-Path $coreRoot 'Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$messagesPath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

foreach ($path in @(
        $eligibilityPath,
        $canEquipPatchPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少无腺体触发体装备限制实现文件：$path"
}

$eligibilityText = Read-Utf8 $eligibilityPath
$canEquipPatchText = Read-Utf8 $canEquipPatchPath
$triggerBodyText = Read-Utf8 $triggerBodyPath
$equipmentTickPatchText = Read-Utf8 $equipmentTickPatchPath
$geneText = Read-Utf8 $genePath
$combatCommandsText = Read-Utf8 $combatCommandsPath
$sessionServiceText = Read-Utf8 $sessionServicePath
$activationTransactionText = Read-Utf8 $activationTransactionPath
$messagesText = Read-Utf8 $messagesPath

Assert-True ($eligibilityText -match 'HasActiveTrionGland') '装备资格服务必须使用统一的激活腺体判定。'
Assert-True ($eligibilityText -match 'TryDropEquipment|innerContainer') '资格失效服务必须包含地图掉落和库存兜底。'
Assert-True ($eligibilityText -match 'NotifyEligibilityLost') '资格失效服务必须先通知容量绑定器拆分资源。'
Assert-True ($canEquipPatchText -match 'EquipmentUtility') '必须补丁原版 EquipmentUtility.CanEquip。'
Assert-True ($canEquipPatchText -match 'CompTriggerBody') '装备补丁必须只限制 BDP 触发体。'
Assert-True ($canEquipPatchText -match 'BDP_Message_Trigger_RequiresTrionGland') '装备补丁必须返回本地化拒绝原因。'
Assert-True ($triggerBodyText -match 'TriggerBodyEquipmentEligibility') 'CompTriggerBody 必须接入运行时非法装备防线。'
Assert-True ($equipmentTickPatchText -match 'TriggerBodyEquipmentEligibility') '装备 Tick 必须执行读档后非法装备校验。'
Assert-True ($geneText -match 'HandleGlandEligibilityLost|HandleTrionGlandLost') '腺体失效必须调用统一强制脱离服务。'
Assert-True ($combatCommandsText -match 'ForceReleaseForEligibilityLoss') '战斗体命令面必须提供资格失效强制解除。'
Assert-True ($sessionServiceText -match 'ForceReleaseForEligibilityLoss') '战斗会话必须实现资格失效强制解除。'
Assert-True ($activationTransactionText -match 'HasActiveTrionGland') '战斗体激活事务必须拒绝无腺体 Pawn。'
Assert-True ($messagesText -match 'BDP_Message_Trigger_RequiresTrionGland') '语言包必须提供无腺体装备拒绝文案。'

Write-Output 'TriggerGlandEquipmentRestrictionSmokeTests PASS'
