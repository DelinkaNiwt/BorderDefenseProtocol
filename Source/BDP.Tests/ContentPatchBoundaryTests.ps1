$ErrorActionPreference = 'Stop'
# 将本次四处正式源码编入独立夹具，正式程序集保持不变。
$taskModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskManaged = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
$taskHarmony = 'C:/NiwtGames/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies'
$taskAssemblies = Join-Path $taskModRoot '1.6/Assemblies'
$taskOutput = Join-Path ([IO.Path]::GetTempPath()) ('bdp-content-boundary-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $taskReferences = @('mscorlib.dll', 'System.dll', 'System.Core.dll') | ForEach-Object { Join-Path 'C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8' $_ }
    $taskReferences += @((Join-Path $taskManaged 'Assembly-CSharp.dll'), (Join-Path $taskManaged 'UnityEngine.CoreModule.dll'), (Join-Path $taskManaged 'UnityEngine.IMGUIModule.dll'), (Join-Path $taskManaged 'netstandard.dll'), (Join-Path $taskHarmony '0Harmony.dll'), (Join-Path $taskAssemblies 'BDP.Core.dll'), (Join-Path $taskAssemblies 'BDP.Content.dll'))
    $taskSources = @('Source/BDP.Content/Trigger/Black/Formation/Inheritance/BlackTriggerInheritedTraitService.cs', 'Source/BDP.Content/CombatBody/Protection/Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs', 'Source/BDP.Content/Patches/Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.cs', 'Source/BDP.Content/Assembly/ChipManufacturing/Patches/Patch_WorkGiver_DoBill_ChipUnfinishedThing.cs', 'Source/BDP.Tests/ContentPatchBoundaryTests.cs') | ForEach-Object { Join-Path $taskModRoot $_ }
    $taskArguments = @('/nologo', '/noconfig', '/nostdlib+', '/langversion:7.3', '/nowarn:0436,1684', '/target:exe', "/out:$taskOutput") + @($taskReferences | ForEach-Object { "/reference:$_" }) + $taskSources
    & dotnet exec 'C:/Program Files/dotnet/sdk/10.0.100/Roslyn/bincore/csc.dll' @taskArguments
    if ($LASTEXITCODE -ne 0) { throw 'Content 边界行为夹具编译失败。' }
    & $taskOutput $taskManaged $taskHarmony $taskAssemblies
    if ($LASTEXITCODE -ne 0) { throw 'Content 边界行为验证失败。' }
} finally {
    Remove-Item -LiteralPath $taskOutput -ErrorAction SilentlyContinue
}
