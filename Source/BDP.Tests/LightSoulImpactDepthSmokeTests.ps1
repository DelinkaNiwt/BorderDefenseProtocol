$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# 当前主模组根目录。
$modRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

# 光魂抵挡前后景所需正式文件。
$impactDefsPath = Join-Path $modRoot '1.6\Content\Defs\Effects\LightSoulImpact.xml'
$impactLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ThingDef\LightSoulImpact.xml'
$lightSoulPath = Join-Path $modRoot '1.6\Content\Defs\HediffDef\LightSoul.xml'
$policyPath = Join-Path $modRoot 'Source\BDP.Content\Shield\EnergyShieldBlockPolicy.cs'
$shieldPath = Join-Path $modRoot 'Source\BDP.Content\Shield\HediffComp_EnergyShield.cs'
$propertiesPath = Join-Path $modRoot 'Source\BDP.Content\Shield\HediffCompProperties_EnergyShield.cs'
$effectPlayerPath = Join-Path $modRoot 'Source\BDP.Content\Shield\EnergyShieldEffectPlayer.cs'

# 断言一个条件必须成立。
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 按 defName 取得一个唯一 XML 定义。
function Get-DefByName {
    param([xml]$Document, [string]$DefName)
    return $Document.SelectSingleNode('/Defs/*[defName="' + $DefName + '"]')
}

foreach ($path in @(
    $impactDefsPath,
    $impactLanguagePath,
    $lightSoulPath,
    $policyPath,
    $shieldPath,
    $propertiesPath,
    $effectPlayerPath
)) {
    Assert-True (Test-Path -LiteralPath $path) ('光魂抵挡前后景缺少文件：' + $path)
}

[xml]$impactXml = Get-Content -Raw -Encoding UTF8 $impactDefsPath
[xml]$impactLanguageXml = Get-Content -Raw -Encoding UTF8 $impactLanguagePath
[xml]$lightSoulXml = Get-Content -Raw -Encoding UTF8 $lightSoulPath

# 后景白闪、火花和烟尘必须全部低于人物层。
$backgroundFlecks = @(
    'BDP_Fleck_LightSoulExplosionFlash_Back',
    'BDP_Fleck_LightSoulSparkFlash_Back',
    'BDP_Fleck_LightSoulAirPuff_Back',
    'BDP_Fleck_LightSoulMicroSparksFast_Back'
)
foreach ($defName in $backgroundFlecks) {
    $def = Get-DefByName $impactXml $defName
    Assert-True ($null -ne $def) ('缺少光魂后景 Fleck 定义：' + $defName)
    Assert-True ($def.altitudeLayer -eq 'Projectile') ($defName + ' 必须位于人物下方的 Projectile 层。')
}

# 前景飞散火花必须与其它前景粒子一起位于人物上方。
$foregroundMotes = @(
    'BDP_Mote_LightSoulSparkThrownFast_Front',
    'BDP_Mote_LightSoulLongSparkThrown_Front'
)
foreach ($defName in $foregroundMotes) {
    $def = Get-DefByName $impactXml $defName
    Assert-True ($null -ne $def) ('缺少光魂前景 Mote 定义：' + $defName)
    Assert-True ($def.altitudeLayer -eq 'MoteOverhead') ($defName + ' 必须位于人物上方的 MoteOverhead 层。')
    Assert-True ($null -ne $impactLanguageXml.LanguageData.SelectSingleNode($defName + '.label')) ($defName + ' 缺少简体中文语言包名称。')
}

# 两套组合特效必须存在，且后景不得引用原版的前景 Fleck。
$frontEffecter = Get-DefByName $impactXml 'BDP_Effecter_LightSoulDeflect_Front'
$backEffecter = Get-DefByName $impactXml 'BDP_Effecter_LightSoulDeflect_Back'
Assert-True ($null -ne $frontEffecter) '缺少光魂前景组合特效。'
Assert-True ($null -ne $backEffecter) '缺少光魂后景组合特效。'
$frontText = $frontEffecter.OuterXml
$backText = $backEffecter.OuterXml
Assert-True ($frontText -match 'BDP_Mote_LightSoulSparkThrownFast_Front') '前景组合特效必须使用前景快速火花。'
Assert-True ($frontText -match 'BDP_Mote_LightSoulLongSparkThrown_Front') '前景组合特效必须使用前景长火花。'
foreach ($defName in $backgroundFlecks | Where-Object { $_ -notmatch 'ExplosionFlash' }) {
    Assert-True ($backText -match [regex]::Escape($defName)) ('后景组合特效缺少子特效：' + $defName)
}
Assert-True ($backText -match '<moteDef>Mote_SparkThrownFast</moteDef>') '后景快速火花必须复用原版 Projectile 层定义。'
Assert-True ($backText -match '<moteDef>Mote_LongSparkThrown</moteDef>') '后景长火花必须复用原版 Projectile 层定义。'

# 两个光魂盾姿态必须显式启用同一套方向分层配置。
$hediffs = @($lightSoulXml.Defs.HediffDef)
Assert-True ($hediffs.Count -eq 2) '光魂必须保留灵活姿态和举盾姿态。'
foreach ($hediff in $hediffs) {
    $props = @($hediff.comps.li) | Where-Object {
        $_.Class -eq 'BDP.Content.Shield.HediffCompProperties_EnergyShield'
    } | Select-Object -First 1
    Assert-True ($null -ne $props) ($hediff.defName + ' 缺少能量护盾组件。')
    Assert-True ([string]$props.useDirectionalImpactDepth -eq 'true') ($hediff.defName + ' 必须启用方向分层。')
    Assert-True ($props.backgroundBlockFlashFleckDef -eq 'BDP_Fleck_LightSoulExplosionFlash_Back') ($hediff.defName + ' 后景白闪引用错误。')
    Assert-True ($props.foregroundDeflectEffectDef -eq 'BDP_Effecter_LightSoulDeflect_Front') ($hediff.defName + ' 前景火花引用错误。')
    Assert-True ($props.backgroundDeflectEffectDef -eq 'BDP_Effecter_LightSoulDeflect_Back') ($hediff.defName + ' 后景火花引用错误。')
}

# 运行时必须复用既有来源方向，并把同一个前后景结论交给白闪和组合火花。
$policyText = Get-Content -Raw -Encoding UTF8 $policyPath
$shieldText = Get-Content -Raw -Encoding UTF8 $shieldPath
$propertiesText = Get-Content -Raw -Encoding UTF8 $propertiesPath
$effectPlayerText = Get-Content -Raw -Encoding UTF8 $effectPlayerPath
Assert-True ($policyText -match 'ShouldRenderImpactBehindPawn') '缺少确定性的命中后景判定。'
Assert-True ($shieldText -match 'ShouldRenderImpactBehindPawn\(direction\)') '抵挡时序必须复用已经解析出的攻击来源方向。'
Assert-True ($propertiesText -match 'useDirectionalImpactDepth') '护盾配置缺少可选的方向分层开关。'
Assert-True ($effectPlayerText -match 'blockFlashFleckDef') '白闪播放器必须接受分层后的 Fleck 定义。'

# 装入真实程序集，验证北、南、东、西四个来源方向。
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll')) | Out-Null
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Content.dll'))
$policyType = $contentAssembly.GetType('BDP.Content.Shield.EnergyShieldBlockPolicy', $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$depthMethod = $policyType.GetMethod('ShouldRenderImpactBehindPawn', $flags)
Assert-True ($null -ne $depthMethod) '编译程序集缺少命中后景判定方法。'

$northBehind = [bool]$depthMethod.Invoke($null, @([UnityEngine.Vector3]::new(0, 0, 1)))
$southBehind = [bool]$depthMethod.Invoke($null, @([UnityEngine.Vector3]::new(0, 0, -1)))
$eastBehind = [bool]$depthMethod.Invoke($null, @([UnityEngine.Vector3]::new(1, 0, 0)))
$westBehind = [bool]$depthMethod.Invoke($null, @([UnityEngine.Vector3]::new(-1, 0, 0)))
Assert-True $northBehind '北半圆命中必须绘制在人物后方。'
Assert-True (-not $southBehind) '南半圆命中必须绘制在人物前方。'
Assert-True (-not $eastBehind) '正东命中必须稳定归入人物前方。'
Assert-True (-not $westBehind) '正西命中必须稳定归入人物前方。'

Write-Output 'LightSoulImpactDepthSmokeTests PASS'
