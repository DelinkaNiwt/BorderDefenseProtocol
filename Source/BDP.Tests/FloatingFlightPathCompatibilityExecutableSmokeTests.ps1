$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function New-TypedList {
    param([Type]$ElementType)
    $openListType = [System.Collections.Generic.List``1]
    return ,([Activator]::CreateInstance($openListType.MakeGenericType([Type[]]@($ElementType))))
}

$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll'

[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll'))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll'))
[void][Reflection.Assembly]::LoadFrom($harmonyPath)
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Content.dll'))

$codeType = [HarmonyLib.CodeInstruction]
$list = New-TypedList $codeType
$dynamicMethod = [Reflection.Emit.DynamicMethod]::new(
    'BDP_FloatingFlightCompatibilityProbe',
    [void],
    [Type[]]@(),
    $contentAssembly.GetType('BDP.Content.FloatingFlight.Patches.Patch_PawnPathFollower_Steps_FloatingFlight', $true),
    $true)
$generator = $dynamicMethod.GetILGenerator()
$successLabel = $generator.DefineLabel()
$genGridType = $gameAssembly.GetType('Verse.GenGrid', $true)
$walkableBy = [HarmonyLib.AccessTools]::Method($genGridType, 'Walkable' + 'By')
[void]$list.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Call, $walkableBy))
[void]$list.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Brtrue, $successLabel))
[void]$list.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Ret))

$patchType = $contentAssembly.GetType(
    'BDP.Content.FloatingFlight.Patches.Patch_PawnPathFollower_Steps_FloatingFlight+SetupMoveIntoNextCellPatch',
    $true)
$transpiler = $patchType.GetMethod(
    'Transpiler',
    [Reflection.BindingFlags]'NonPublic,Static')
$result = $transpiler.Invoke($null, [object[]]@($list, $generator, $null))
$resultItems = @($result)

Assert-True (($resultItems | Where-Object { $_.operand -eq $walkableBy }).Count -eq 1) '原版 WalkableBy 调用必须原样保留。'
Assert-True (($resultItems | Where-Object {
            ($_.operand -is [Reflection.MethodInfo]) -and $_.operand.Name -eq 'CanBypassWalkableCheck'
        }).Count -eq 1) '浮空旁路必须插入一次。'
Assert-True ($resultItems.Count -gt $list.Count) '旁路必须通过新增指令实现，而不是替换原版指令。'

# 模拟第三方同签名函数：签名不能证明其语义，必须安全放弃该锚点。
$wrapper = [Reflection.Emit.DynamicMethod]::new(
    'UnknownModWalkabilityWrapper',
    [bool],
    [Type[]]@([Verse.IntVec3], [Verse.Map], [Verse.Pawn]),
    $contentAssembly.GetType('BDP.Content.FloatingFlight.Patches.Patch_PawnPathFollower_Steps_FloatingFlight', $true),
    $true)
$wrapperGenerator = $wrapper.GetILGenerator()
$wrapperGenerator.Emit([Reflection.Emit.OpCodes]::Ldc_I4_1)
$wrapperGenerator.Emit([Reflection.Emit.OpCodes]::Ret)
$wrapperList = New-TypedList $codeType
$wrapperLabel = $generator.DefineLabel()
[void]$wrapperList.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Call, $wrapper))
[void]$wrapperList.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Brtrue, $wrapperLabel))
[void]$wrapperList.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Ret))
$matcher = $contentAssembly.GetType(
    'BDP.Content.FloatingFlight.Patches.Patch_PawnPathFollower_Steps_FloatingFlight', $true).GetMethod(
    'FindWalkabilityCall', [Reflection.BindingFlags]'NonPublic,Static')
$wrapperIndex = $matcher.Invoke($null, [object[]]@($wrapperList, $walkableBy))
Assert-True ($wrapperIndex -eq -1) '未知第三方同签名函数不得成为可跳过的通行锚点。'
Assert-True ($wrapperList.Count -eq 3) '未知函数的原指令必须完整保留。'

Write-Output 'FloatingFlightPathCompatibilityExecutableSmokeTests PASS'
