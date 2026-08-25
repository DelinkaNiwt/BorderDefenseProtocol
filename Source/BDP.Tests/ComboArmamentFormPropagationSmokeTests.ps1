$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$modifierPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormComboExpressionModifier.cs"
$expressionServicePath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs"
$comboResolverPath = Join-Path $modRoot "Source\BDP\Core\Combos\Contract\ComboDefinitionContractResolver.cs"
$bootstrapPath = Join-Path $modRoot "Source\BDP.Content\ContentBootstrap.cs"
Assert-True (Test-Path -LiteralPath $modifierPath) "缺少组合结果武装构型修正提供器。"

$modifierText = Get-Utf8Text $modifierPath
$expressionServiceText = Get-Utf8Text $expressionServicePath
$comboResolverText = Get-Utf8Text $comboResolverPath
$bootstrapText = Get-Utf8Text $bootstrapPath

Assert-True ($modifierText -match "IComboExpressionVariantModifierProvider") "组合结果武装构型修正提供器必须实现 Core 中性接口。"
Assert-True ($modifierText -match "sourceVariantKey") "组合结果武装构型修正必须依据来源变体键查找构型。"
Assert-True ($modifierText -match "FindArmamentForm") "组合结果武装构型修正必须复用现有构型查找入口。"
Assert-True ($expressionServiceText -match "PrimaryVerb") "组合结果武装构型修正必须限制于主/副动作条目。"
Assert-True ($expressionServiceText -match "SecondaryVerb") "组合结果武装构型修正必须限制于主/副动作条目。"
Assert-True ($expressionServiceText -match "WeaponMode") "组合结果武装构型修正必须检查近战/远程模式。"
Assert-True ($expressionServiceText -match "VerbProps") "组合结果武装构型修正必须支持显式 VerbProps 字段。"
Assert-True ($expressionServiceText -match "RangedModules") "组合结果武装构型修正必须支持显式远程模块字段。"
Assert-True ($expressionServiceText -match "Execution") "组合结果武装构型修正必须支持显式执行字段。"
Assert-True ($expressionServiceText -match "ComboExpressionEntryConfig") "武装构型表达服务必须提供组合条目覆盖入口。"
Assert-True ($comboResolverText -match "entryConfig\.Execution(\s|\?|=|[,\)])") "组合执行节奏求值必须消费组合结果显式 Execution 字段。"
Assert-True ($bootstrapText -match "ChipArmamentFormComboExpressionModifier") "Content 启动入口必须注册组合结果武装构型修正提供器。"

$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))
$flags = [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static

$entryType = $coreAssembly.GetType("BDP.Core.Combos.ComboExpressionEntryConfig", $true)
$overlayType = $coreAssembly.GetType("BDP.Core.Combos.VerbPropsOverlay", $true)
$executionType = $coreAssembly.GetType("BDP.Core.Expressions.ChipAttackExecutionConfig", $true)
$formType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef", $true)
$overrideType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormOverrides", $true)
$serviceType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Resolution.ChipArmamentFormExpressionService", $true)
$cloneType = $coreAssembly.GetType("BDP.Core.Combos.ComboExpressionEntryCloneService", $true)

function Set-FieldValue
{
    param($Target, [string]$Name, $Value)
    $Target.GetType().GetField(
        $Name,
        [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance).SetValue($Target, $Value)
}

$entry = [Activator]::CreateInstance($entryType)
Set-FieldValue $entry "Kind" ([Enum]::Parse($entryType.GetField("Kind").FieldType, "PrimaryVerb"))
Set-FieldValue $entry "WeaponMode" ([Enum]::Parse($entryType.GetField("WeaponMode").FieldType, "Ranged"))
$overlay = [Activator]::CreateInstance($overlayType)
Set-FieldValue $overlay "range" ([Nullable[float]]30)
Set-FieldValue $entry "VerbProps" $overlay
$execution = [Activator]::CreateInstance($executionType)
Set-FieldValue $entry "Execution" $execution

$form = [Activator]::CreateInstance($formType)
$formOverrides = [Activator]::CreateInstance($overrideType)
Set-FieldValue $formOverrides "rangeMultiplier" ([Nullable[float]]1.2)
Set-FieldValue $formOverrides "hitCount" ([Nullable[int]]3)
Set-FieldValue $form "overrides" $formOverrides

$comboApplyMethod = @($serviceType.GetMethods($flags) |
    Where-Object {
        $_.Name -eq "ApplyArmamentFormOverrides" -and
        $_.GetParameters().Count -eq 2 -and
        $_.GetParameters()[0].ParameterType.FullName -eq "BDP.Core.Combos.ComboExpressionEntryConfig"
    }) | Select-Object -First 1
Assert-True ($null -ne $comboApplyMethod) "武装构型表达服务缺少组合条目覆盖方法。"
$comboApplyMethod.Invoke($null, @($entry, $form))
Assert-True ([Math]::Abs(([float]$entry.VerbProps.range) - 36.0) -lt 0.001) "组合结果显式射程必须只被构型倍率修正一次。"
Assert-True ([int]$entry.Execution.HitCount -eq 3) "组合结果显式执行字段必须接受构型修正。"

$cloneMethod = @($cloneType.GetMethods($flags) |
    Where-Object { $_.Name -eq "Clone" -and $_.GetParameters().Count -eq 1 }) | Select-Object -First 1
Assert-True ($null -ne $cloneMethod) "组合条目副本服务缺少单条复制方法。"
$original = [Activator]::CreateInstance($entryType)
Set-FieldValue $original "Kind" ([Enum]::Parse($entryType.GetField("Kind").FieldType, "PrimaryVerb"))
Set-FieldValue $original "WeaponMode" ([Enum]::Parse($entryType.GetField("WeaponMode").FieldType, "Ranged"))
$originalOverlay = [Activator]::CreateInstance($overlayType)
Set-FieldValue $originalOverlay "range" ([Nullable[float]]30)
Set-FieldValue $original "VerbProps" $originalOverlay
$cloned = $cloneMethod.Invoke($null, @($original))
$comboApplyMethod.Invoke($null, @($cloned, $form))
Assert-True ([Math]::Abs(([float]$original.VerbProps.range) - 30.0) -lt 0.001) "构型修正不得回写 ComboDef 原始条目。"

$inheritedOnly = [Activator]::CreateInstance($entryType)
Set-FieldValue $inheritedOnly "Kind" ([Enum]::Parse($entryType.GetField("Kind").FieldType, "PrimaryVerb"))
Set-FieldValue $inheritedOnly "WeaponMode" ([Enum]::Parse($entryType.GetField("WeaponMode").FieldType, "Ranged"))
$comboApplyMethod.Invoke($null, @($inheritedOnly, $form))
Assert-True ($null -eq $inheritedOnly.VerbProps) "未显式声明射程的组合结果不得凭空创建 VerbProps。"

Write-Host "PASS: 组合结果显式字段的武装构型传播边界完整。"
