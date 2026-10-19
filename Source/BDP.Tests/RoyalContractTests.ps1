$ErrorActionPreference = 'Stop'
# 调用真实内容程序集；完整施放、存读档及地图死亡留游戏实测。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-contract-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" "/reference:$managed\Unity.Mathematics.dll" (Join-Path $PSScriptRoot 'RoyalContractTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '契约夹具编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '契约状态行为测试失败。' }
    [xml]$gene = Get-Content -LiteralPath (Join-Path $modRoot '1.6/Content/Defs/GeneDef/RoyalBloodline.xml') -Raw -Encoding utf8
    [xml]$ability = Get-Content -LiteralPath (Join-Path $modRoot '1.6/Content/Defs/AbilityDef/RoyalContract.xml') -Raw -Encoding utf8
    [xml]$contract = Get-Content -LiteralPath (Join-Path $modRoot '1.6/Content/Defs/HediffDef/RoyalContract.xml') -Raw -Encoding utf8
    [xml]$snapshot = Get-Content -LiteralPath (Join-Path $modRoot '1.6/Content/Defs/CombatBodyDef/RoyalContract.xml') -Raw -Encoding utf8
    if ($gene.Defs.GeneDef.abilities.li -ne $ability.Defs.AbilityDef.defName) { throw '皇室基因未接入正式契约能力。' }
    if ($contract.Defs.HediffDef.duplicationAllowed -ne 'false' -or $contract.Defs.HediffDef.everCurableByItem -ne 'false') { throw '契约不能被复制或普通治疗移除。' }
    if ($snapshot.SelectSingleNode('/Defs/BDP.Core.CombatBody.CombatBodySnapshotConfigDef/excludedHediffs/li').InnerText -ne $contract.Defs.HediffDef.defName) { throw '契约未排除战斗体快照回滚。' }
    if ($ability.Defs.AbilityDef.aiCanUse -ne 'false') { throw '契约应仅供玩家主动使用。' }
    # 原版语类接触流程：非正射程使选择校验可达，专用工作负责先走近再施放。
    if ($ability.Defs.AbilityDef.jobDef -ne 'CastAbilityOnThingMelee' -or [float]$ability.Defs.AbilityDef.verbProperties.range -ne -1) { throw '契约仍是远程施放配置，未接入原版走近施放工作。' }
    if ($ability.Defs.AbilityDef.verbProperties.verbClass -ne 'RimWorld.Verb_CastAbility' -or $ability.Defs.AbilityDef.verbProperties.requireLineOfSight -ne 'true') { throw '契约应复用原版接触能力流程，不另行绕过视线校验。' }
    Write-Output '通过：皇室能力关联、原版复制／治疗排除与战斗体快照配置。'
    Write-Output '通过：契约使用原版走近施放配置；完整寻路与施放仍需游戏实测。'
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
