$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$defenseRoot = Join-Path $sourceRoot 'BDP.Content\LightSoul'
$hediffPath = Join-Path $modRoot 'Common\Content\Defs\HediffDef\LightSoul.xml'
$jobDefPath = Join-Path $modRoot 'Common\Content\Defs\JobDef\LightSoul.xml'
$keyedPath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'
$jobLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\JobDef\LightSoul.xml'
$propertiesPath = Join-Path $defenseRoot 'HediffCompProperties_LightSoulDefenseWatch.cs'
$compPath = Join-Path $defenseRoot 'HediffComp_LightSoulDefenseWatch.cs'
$verbPath = Join-Path $defenseRoot 'Verb_LightSoulDefenseWatch.cs'
$utilityPath = Join-Path $defenseRoot 'LightSoulDefenseWatchUtility.cs'
$jobDriverPath = Join-Path $defenseRoot 'JobDriver_LightSoulDefenseWatch.cs'
$defOfPath = Join-Path $defenseRoot 'LightSoulDefenseDefOf.cs'
$waitPatchPath = Join-Path $defenseRoot 'Patches\Patch_JobDriver_Wait_CheckForAutoAttack.cs'
$verbPatchPath = Join-Path $defenseRoot 'Patches\Patch_Pawn_TryGetAttackVerb_LightSoulDefenseWatch.cs'
$rotationPatchPath = Join-Path $defenseRoot 'Patches\Patch_Pawn_RotationTracker_UpdateRotation.cs'
$tooltipPatchPath = Join-Path $defenseRoot 'Patches\Patch_TooltipUtility_ShotCalculationTipString.cs'

foreach ($path in @(
    $hediffPath, $jobDefPath, $keyedPath, $jobLanguagePath, $propertiesPath, $compPath,
    $verbPath, $utilityPath, $jobDriverPath, $defOfPath, $waitPatchPath, $verbPatchPath,
    $rotationPatchPath, $tooltipPatchPath
)) {
    Assert-True (Test-Path -LiteralPath $path) ('光魂注视警戒闭环缺少文件：' + $path)
}

[xml]$hediffXml = Get-Content -LiteralPath $hediffPath -Raw -Encoding UTF8
[xml]$jobDefXml = Get-Content -LiteralPath $jobDefPath -Raw -Encoding UTF8
$defenseHediff = $hediffXml.SelectSingleNode('/Defs/HediffDef[defName="BDP_Hediff_LightSoulDefenseState"]')
$shieldHediff = $hediffXml.SelectSingleNode('/Defs/HediffDef[defName="BDP_Hediff_LightSoulDefenseShield"]')
$watchComp = $defenseHediff.comps.li | Where-Object { $_.Class -eq 'BDP.Content.LightSoul.HediffCompProperties_LightSoulDefenseWatch' }
$watchVerb = $watchComp.verbs.li | Select-Object -First 1

Assert-True ($null -ne $watchComp) '光魂防护 Hediff 必须挂载正式注视警戒 Verb 组件。'
Assert-True ($watchVerb.verbClass -eq 'BDP.Content.LightSoul.Verb_LightSoulDefenseWatch') '注视警戒必须由正式 Verb 表达。'
Assert-True ([single]$watchVerb.range -eq [single]15.9) '注视警戒距离必须直接使用 Verb 的 XML range，临时值为 15.9 格。'
Assert-True ($watchVerb.isPrimary -eq 'true') '注视警戒 Verb 必须声明为组件主行为。'
Assert-True ($watchVerb.violent -eq 'false') '注视警戒 Verb 必须明确为非暴力行为。'
Assert-True ($watchVerb.ai_IsWeapon -eq 'false') '注视警戒 Verb 不得声明为武器。'
Assert-True ($watchVerb.requireLineOfSight -eq 'true') '注视警戒首次选择和自动索敌必须沿用攻击式视线判断。'
Assert-True ($null -eq $watchComp.autoWatchRange) '不得保留与 Verb range 重复的自动警戒距离配置。'
Assert-True ($null -eq ($shieldHediff.comps.li | Where-Object { $_.Class -eq 'BDP.Content.LightSoul.HediffCompProperties_LightSoulDefenseWatch' })) '护盾宿主不得重复挂警戒；警戒只随独立防护状态存在。'

$watchJob = $jobDefXml.SelectSingleNode('/Defs/JobDef[defName="BDP_LightSoulDefenseWatch"]')
Assert-True ($null -ne $watchJob) '缺少光魂注视警戒 JobDef。'
Assert-True ($watchJob.driverClass -eq 'BDP.Content.LightSoul.JobDriver_LightSoulDefenseWatch') '注视警戒 JobDef 必须绑定对应驱动。'

$propertiesText = Get-Content -LiteralPath $propertiesPath -Raw -Encoding UTF8
$compText = Get-Content -LiteralPath $compPath -Raw -Encoding UTF8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding UTF8
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding UTF8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding UTF8
$waitPatchText = Get-Content -LiteralPath $waitPatchPath -Raw -Encoding UTF8
$verbPatchText = Get-Content -LiteralPath $verbPatchPath -Raw -Encoding UTF8
$rotationPatchText = Get-Content -LiteralPath $rotationPatchPath -Raw -Encoding UTF8
$tooltipPatchText = Get-Content -LiteralPath $tooltipPatchPath -Raw -Encoding UTF8
$keyedText = Get-Content -LiteralPath $keyedPath -Raw -Encoding UTF8
$jobLanguageText = Get-Content -LiteralPath $jobLanguagePath -Raw -Encoding UTF8

Assert-True ($propertiesText -match 'HediffCompProperties_VerbGiver') '注视警戒组件参数必须继承原版 VerbGiver。'
Assert-True ($compText -match 'Command_VerbTarget') '注视警戒按钮必须使用原版 Verb 目标命令。'
Assert-True ($compText -match 'verbTracker\?\.PrimaryVerb') '手动与自动入口必须读取组件持有的同一个 Verb。'
Assert-True ($compText -match 'CompPostPostAdd[\s\S]*CancelRangedAttackState') '进入防护形态时必须只取消远程攻击和瞄准。'
Assert-True ($compText -match 'CompPostPostRemoved[\s\S]*ClearWatchTarget') '退出防护形态时必须清除警戒状态。'

Assert-True ($verbText -match 'class Verb_LightSoulDefenseWatch\s*:\s*Verb') '注视警戒必须实现为正式 Verb。'
Assert-True ($verbText -match 'override void OrderForceTarget') '手动选定目标必须由 Verb 自己下达作业。'
Assert-True ($verbText -match 'job\.verbToUse\s*=\s*this') '警戒作业必须保存发起它的正式 Verb。'
Assert-True ($verbText -match 'EffectiveRange') '自动索敌必须读取正式 Verb 的有效射程。'
Assert-True ($verbText -match 'attackTargetsCache\.GetPotentialTargetsFor') '自动索敌必须从原版攻击目标缓存取得候选。'
Assert-True ($verbText -match 'CanHitTargetFrom|CanHitTarget') '自动索敌必须沿用正式 Verb 的射程和视线合法性判断。'
Assert-True ($verbText -match 'DistanceToSquared') '自动索敌必须按距离平方选择最近合法目标。'
Assert-True ($verbText -match 'thingIDNumber') '最近目标同距离时必须使用稳定目标 ID 消除候选顺序抖动。'
Assert-True ($verbText -match 'automaticWatchTarget[\s\S]*(same|Same|Thing|thingIDNumber)[\s\S]*automaticWatchTarget') '自动索敌必须比较当前锁定身份，最近目标不变时保持锁定。'
Assert-True ($verbText -notmatch 'BestShootTargetFromCurrentPosition|GetRandomShootingTargetByScore|TryRandomElementByWeight') '自动注视不得继续使用原版评分加权随机选择。'
Assert-True ($verbText -match 'protected override bool TryCastShot\(\)[\s\S]*return false') '注视警戒 Verb 的施放兜底必须永远不产生攻击。'

Assert-True ($jobDriverText -match 'bool\s+canWatchTarget\s*=\s*watchVerb\.CanHitTarget\(TargetA\)') '注视警戒 Job 必须用正式 Verb 的当前可命中结果决定朝向占用。'
Assert-True ($jobDriverText -match 'watchTarget\.handlingFacing\s*=\s*canWatchTarget') '目标不可注视时，警戒 Job 必须把朝向控制权交还原版。'
Assert-True ($jobDriverText -notmatch 'watchTarget\.handlingFacing\s*=\s*true') '注视警戒 Job 不得永久接管人物朝向。'
Assert-True ($jobDriverText -match 'if \(canWatchTarget\)[\s\S]*rotationTracker\.FaceTarget\(TargetA\)') '目标恢复射程和视线后，警戒 Job 必须自动恢复注视。'
Assert-True ($jobDriverText -notmatch '\|\| !watchVerb\.CanHitTarget\(TargetA\)') '目标暂时失去射程或视线时不得结束警戒 Job。'
Assert-True ($jobDriverText -match '!IsTargetStillUsable\(TargetA\)') '目标真正销毁或离图时仍必须结束警戒 Job。'
Assert-True ($jobDriverText -notmatch 'StartPath|TryStartAttack|TryStartCastOn|TryMeleeAttack') '注视警戒 Job 不得移动或攻击。'

Assert-True ($utilityText -notmatch 'equipment|Drop|TryDrop') '举盾切换不得操作装备或强制掉落武器。'
Assert-True ($verbPatchText -match 'HarmonyPatch\(typeof\(Pawn\), nameof\(Pawn\.TryGetAttackVerb\)\)') '防护形态下必须把正式注视警戒 Verb 暴露为当前有效 Verb。'
Assert-True ($waitPatchText -match 'CheckForAutoAttack' -and $waitPatchText -match 'RefreshAutomaticWatchTarget') '自动注视必须复用原版自动攻击检查时点。'
Assert-True ($rotationPatchText -match 'UpdateRotation' -and $rotationPatchText -match 'FaceTarget') '自动注视目标必须通过原版人物朝向器生效。'
Assert-True ($tooltipPatchText -match 'ShotCalculationTipString' -and $tooltipPatchText -match 'LightSoulDefenseWatchUtility\.ResolveVerb') '射击提示限制必须仅在 BDP 防护形态下生效。'

Assert-True ($keyedText -match '<BDP_Command_LightSoulDefenseWatch>注视警戒</BDP_Command_LightSoulDefenseWatch>') '语言包缺少“注视警戒”按钮名称。'
Assert-True ($keyedText -match '<BDP_Command_LightSoulDefenseWatch_Description>') '语言包缺少“注视警戒”按钮说明。'
Assert-True ($jobLanguageText -match '<BDP_LightSoulDefenseWatch\.reportString>') '语言包缺少注视警戒作业报告。'

$productionText = @(
    (Get-Content -LiteralPath $hediffPath -Raw -Encoding UTF8),
    (Get-Content -LiteralPath $jobDefPath -Raw -Encoding UTF8),
    $keyedText,
    $jobLanguageText,
    ((Get-ChildItem -LiteralPath $defenseRoot -Recurse -File | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
    }) -join "`n")
) -join "`n"
$oldIdentifiers = @(
    'LightSoulDefense' + 'Facing',
    'LightSoulDefense' + 'FaceTarget',
    'LightSoulDefenseAuto' + 'FacingService',
    'LightSoulDefenseSearch' + 'VerbScope'
)
foreach ($oldIdentifier in $oldIdentifiers) {
    Assert-True ($productionText -notmatch [regex]::Escape($oldIdentifier)) ('正式内容仍残留旧技术标识：' + $oldIdentifier)
}
Assert-True ($productionText -notmatch '>面向目标<') '正式玩家文本不得继续显示旧名称“面向目标”。'

Write-Output 'LightSoulDefenseWatchSmokeTests PASS'
