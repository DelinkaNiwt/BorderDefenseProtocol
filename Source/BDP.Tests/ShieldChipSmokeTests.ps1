$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# 当前主模组根目录。
$modRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

# 护盾业务所需文件路径。
$chipPath = Join-Path $modRoot '1.6\Content\Defs\Things\Items\Chips\Shield\ThingDefs_Chip_EnergyShield.xml'
$hediffPath = Join-Path $modRoot '1.6\Content\Defs\Health\Shield\HediffDefs_EnergyShield.xml'
$fleckPath = Join-Path $modRoot '1.6\Content\Defs\Effects\Shield\FleckDefs_EnergyShield.xml'
$shieldSourceRoot = Join-Path $modRoot 'Source\BDP.Content\Shield'
$texturePath = Join-Path $modRoot '1.6\Content\Textures\Effects\Shield\energy_shield_block.png'

# 断言一个条件必须成立。
function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 断言两个浮点值在允许误差内相等。
function Assert-Near {
    param(
        [Parameter(Mandatory = $true)]
        [single]$Actual,

        [Parameter(Mandatory = $true)]
        [single]$Expected,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ([Math]::Abs($Actual - $Expected) -ge 0.0001) {
        throw "$Message 实际值=$Actual，预期值=$Expected。"
    }
}

# 文件边界：护盾正式内容必须完整留在主模组 Content。
Assert-True (Test-Path -LiteralPath $chipPath) '缺少护盾芯片 XML。'
Assert-True (Test-Path -LiteralPath $hediffPath) '缺少护盾 Hediff XML。'
Assert-True (Test-Path -LiteralPath $fleckPath) '缺少护盾 Fleck XML。'
Assert-True (Test-Path -LiteralPath $texturePath) '缺少六边形护盾贴图。'
Assert-True (Test-Path -LiteralPath $shieldSourceRoot) '缺少护盾 C# 源码目录。'

# 芯片作者配置：验证槽位、启停时间、Trion 成本和 Hediff 聚合方式。
[xml]$chipXml = Get-Content -Raw -Encoding UTF8 $chipPath
$chip = $chipXml.Defs.ThingDef
$config = $chip.modExtensions.li
Assert-True ($chip.defName -eq 'BDP_Chip_EnergyShield') '护盾芯片 DefName 不正确。'
Assert-True ($config.Loadout.SlotRegion -eq 'MainSub') '护盾芯片必须属于主副槽区。'
Assert-True ([int]$config.Loadout.ActivationDelayTicks -eq 60) '护盾预热必须为 60 tick。'
Assert-True ([int]$config.Loadout.DeactivationDelayTicks -eq 30) '护盾关闭延迟必须为 30 tick。'
Assert-True ([float]$config.Trion.CapacityCost -eq 50) '护盾占用必须为 50 Trion。'
Assert-True ([float]$config.Trion.ActivationCost -eq 20) '护盾激活成本必须为 20 Trion。'
$entry = $config.Expression.Entries.li
Assert-True ($entry.Kind -eq 'Hediff') '护盾必须通过 Hediff 表达发布。'
Assert-True ($entry.HediffDefName -eq 'BDP_Hediff_EnergyShield') '护盾表达引用错误。'
Assert-True ($entry.HediffApplyModeKey -eq 'countToSeverity') '双护盾必须按数量聚合 Severity。'

# Hediff 作者配置：验证单枚与双枚护盾的实际定义数值。
[xml]$hediffXml = Get-Content -Raw -Encoding UTF8 $hediffPath
$hediff = $hediffXml.Defs.HediffDef
Assert-True ($hediff.hediffClass -eq 'BDP.Core.Expressions.BdpExpressionHostHediff') '护盾必须使用正式表达宿主。'
Assert-True ([float]$hediff.maxSeverity -eq 2) '护盾最大 Severity 必须为 2。'
$props = $hediff.comps.li
Assert-True ([float]$props.blockAngleRange -eq 180) '单护盾角度必须为 180°。'
Assert-Near ([float]$props.blockChance) 0.7 '单护盾抵挡率必须为 70%。'
Assert-Near ([float]$props.trionCostMultiplier) 0.7 '抵挡扣费倍率必须为 0.7。'
Assert-True ([float]$props.stackedBlockAngleRange -eq 360) '双护盾角度必须为 360°。'
Assert-Near ([float]$props.stackedBlockChance) 0.95 '双护盾抵挡率必须为 95%。'
Assert-True ([string]$props.showShieldBubble -eq 'false') '护盾激活期间不得显示常驻护盾球。'
Assert-True ($null -eq $props.SelectSingleNode('blockEffectDef')) '未声明 Royalty 依赖时不得硬引用 Royalty 护盾 Effecter。'

# 接入边界：只检查 Content 源码是否使用正式 Trion 接口和两个原版时序点。
$sourceFiles = Get-ChildItem -LiteralPath $shieldSourceRoot -Filter '*.cs'
$sourceText = ($sourceFiles | ForEach-Object { Get-Content -Raw -Encoding UTF8 $_.FullName }) -join "`n"
Assert-True ($sourceText -match 'TrionSurfaceAccess\.ResolveCommands') '护盾必须通过 Trion 正式请求口扣费。'
Assert-True ($sourceText -match 'Pawn.*PreApplyDamage') '缺少受伤前护盾补丁。'
Assert-True ($sourceText -match 'Pawn.*DrawAt') '缺少护盾绘制补丁。'

# 装入编译后的真实程序集，直接验证正式组件使用的确定性判定策略。
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\839005762\1.6\Assemblies\0Harmony.dll'
$mainAssemblyRoot = [IO.Path]::GetFullPath((Join-Path $modRoot '..\BorderDefenseProtocol\1.6\Assemblies'))
[Reflection.Assembly]::LoadFrom($harmonyPath) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $mainAssemblyRoot 'BDP.Core.dll')) | Out-Null
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $mainAssemblyRoot 'BDP.Content.dll'))
$policyType = $contentAssembly.GetType('BDP.Content.Shield.EnergyShieldBlockPolicy', $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$withinArc = $policyType.GetMethod('IsWithinArc', $flags)
$resolveChance = $policyType.GetMethod('ResolveBlockChance', $flags)
$calculateCost = $policyType.GetMethod('CalculateTrionCost', $flags)

Assert-True ($null -ne $withinArc) '缺少护盾角度判定方法。'
Assert-True ($null -ne $resolveChance) '缺少护盾概率解析方法。'
Assert-True ($null -ne $calculateCost) '缺少护盾扣费计算方法。'

# 正面测试：Pawn 朝 0°，伤害飞行角度 180°，反推来源为 0°。
$frontBlocked = [bool]$withinArc.Invoke($null, @([single]0, [single]180, $true, [single]180, [single]0))
Assert-True $frontBlocked '单护盾必须接受正面攻击。'

# 背面测试：Pawn 朝 0°，伤害飞行角度 0°，反推来源为 180°。
$backBlockedBySingle = [bool]$withinArc.Invoke($null, @([single]0, [single]0, $true, [single]180, [single]0))
Assert-True (-not $backBlockedBySingle) '单护盾必须拒绝背面攻击。'

# 双枚测试：关闭角度检查后，同一背面攻击必须进入抵挡流程。
$backBlockedByStack = [bool]$withinArc.Invoke($null, @([single]0, [single]0, $false, [single]360, [single]0))
Assert-True $backBlockedByStack '双护盾必须接受背面攻击。'

# Severity 和扣费公式测试。
$singleChance = [single]$resolveChance.Invoke($null, @([single]1, [single]0.7, [single]0.95))
$stackedChance = [single]$resolveChance.Invoke($null, @([single]2, [single]0.7, [single]0.95))
$trionCost = [single]$calculateCost.Invoke($null, @([single]20, [single]0.7))
Assert-Near $singleChance 0.7 'Severity 1 必须使用 70% 抵挡率。'
Assert-Near $stackedChance 0.95 'Severity 2 必须使用 95% 抵挡率。'
Assert-Near $trionCost 14 '20 点伤害必须消耗 14 Trion。'

Write-Output 'Shield chip smoke tests passed.'
