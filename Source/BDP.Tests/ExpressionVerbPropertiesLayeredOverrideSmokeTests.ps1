# 普通表达条目 VerbProperties 分层绝对值与倍率优先级冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$entryPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Config\ChipExpressionEntryConfig.cs"
$overlayPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Config\VerbPropsOverlay.cs"
$servicePath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs"

$entryText = Get-Utf8Text $entryPath
$overlayText = Get-Utf8Text $overlayPath
$serviceText = Get-Utf8Text $servicePath

Assert-True ($entryText -match 'public\s+VerbPropsOverlay\s+VerbProps') "普通表达条目必须使用字段级 VerbProps 覆盖层。"
Assert-True ($overlayText -match 'public\s+float\?\s+rangeMultiplier') "表达条目必须支持射程倍率。"
Assert-True ($overlayText -match 'public\s+float\?\s+accuracyMultiplier') "表达条目必须支持精度倍率。"
Assert-True ($overlayText -match 'public\s+float\?\s+warmupMultiplier') "表达条目必须支持预热倍率。"
Assert-True ($overlayText -match 'public\s+float\?\s+cooldownMultiplier') "表达条目必须支持冷却倍率。"
Assert-True ($overlayText -match 'public\s+float\?\s+burstShotCountMultiplier') "表达条目必须支持连发数量倍率。"
Assert-True ($serviceText -match 'target\.range\s*=\s*target\.range\s*\?\?\s*ov\.range') "构型绝对值只能补表达条目缺省射程。"
Assert-True ($serviceText -match 'target\.rangeMultiplier\s*=\s*target\.rangeMultiplier\s*\?\?\s*ov\.rangeMultiplier') "表达条目倍率必须覆盖构型倍率。"

$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))
$flags = [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static
$instanceFlags = [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance

$entryType = $coreAssembly.GetType("BDP.Core.Expressions.ChipExpressionEntryConfig", $true)
$overlayType = $coreAssembly.GetType("BDP.Core.Expressions.VerbPropsOverlay", $true)
$verbType = $coreAssembly.GetType("BDP.Core.Verbs.BdpVerb_Shoot", $true)
$formType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef", $true)
$overrideType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormOverrides", $true)
$serviceType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Resolution.ChipArmamentFormExpressionService", $true)
$interpreterType = $coreAssembly.GetType("BDP.Core.Expressions.ChipExpressionContractInterpreter", $true)

function Set-FieldValue
{
    param($Target, [string]$Name, $Value)
    $Target.GetType().GetField($Name, $instanceFlags).SetValue($Target, $Value)
}

function Get-FieldValue
{
    param($Target, [string]$Name)
    return $Target.GetType().GetField($Name, $instanceFlags).GetValue($Target)
}

function New-Entry
{
    param([Nullable[float]]$Range, [Nullable[float]]$RangeMultiplier)

    $entry = [Activator]::CreateInstance($entryType)
    $overlay = [Activator]::CreateInstance($overlayType)
    Set-FieldValue $overlay "verbClass" $verbType
    if ($Range.HasValue) { Set-FieldValue $overlay "range" $Range }
    if ($RangeMultiplier.HasValue) { Set-FieldValue $overlay "rangeMultiplier" $RangeMultiplier }
    Set-FieldValue $entry "VerbProps" $overlay
    return $entry
}

function New-Form
{
    param([Nullable[float]]$Range, [Nullable[float]]$RangeMultiplier)

    $form = [Activator]::CreateInstance($formType)
    $overrides = [Activator]::CreateInstance($overrideType)
    if ($Range.HasValue) { Set-FieldValue $overrides "range" $Range }
    if ($RangeMultiplier.HasValue) { Set-FieldValue $overrides "rangeMultiplier" $RangeMultiplier }
    Set-FieldValue $form "overrides" $overrides
    return $form
}

$applyMethod = $serviceType.GetMethod("ApplyArmamentFormOverrides", $flags, $null, @($entryType, $formType), $null)
Assert-True ($null -ne $applyMethod) "缺少普通表达条目的武装构型覆盖入口。"

$entryExplicit = New-Entry ([Nullable[float]]30) ([Nullable[float]]1.1)
$applyMethod.Invoke($null, @($entryExplicit, (New-Form ([Nullable[float]]40) ([Nullable[float]]1.2)))
$explicitOverlay = Get-FieldValue $entryExplicit "VerbProps"
Assert-True ([Math]::Abs(([float](Get-FieldValue $explicitOverlay "range")) - 30.0) -lt 0.001) "表达条目显式绝对值必须覆盖构型绝对值。"
Assert-True ([Math]::Abs(([float](Get-FieldValue $explicitOverlay "rangeMultiplier")) - 1.1) -lt 0.001) "表达条目显式倍率必须覆盖构型倍率。"

$entryFormFallback = New-Entry $null $null
$applyMethod.Invoke($null, @($entryFormFallback, (New-Form ([Nullable[float]]40) ([Nullable[float]]1.2)))
$fallbackOverlay = Get-FieldValue $entryFormFallback "VerbProps"
Assert-True ([Math]::Abs(([float](Get-FieldValue $fallbackOverlay "range")) - 40.0) -lt 0.001) "表达条目缺省绝对值必须接受构型绝对值。"
Assert-True ([Math]::Abs(([float](Get-FieldValue $fallbackOverlay "rangeMultiplier")) - 1.2) -lt 0.001) "表达条目倍率缺省时必须接受构型倍率。"

$translateMethod = $interpreterType.GetMethod("TranslateEntry", $flags)
Assert-True ($null -ne $translateMethod) "缺少普通表达条目正规化入口。"
$translated = $translateMethod.Invoke($null, @($entryExplicit, $null, $null))
Assert-True ([Math]::Abs(([float]$translated.ResolvedVerbSpec.Range) - 33.0) -lt 0.001) "绝对值选择后必须只应用表达条目倍率。"

Write-Host "PASS: 普通表达条目的绝对值与倍率分层优先级完整。"
