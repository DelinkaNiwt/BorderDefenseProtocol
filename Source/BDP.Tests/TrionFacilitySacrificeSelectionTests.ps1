$ErrorActionPreference = 'Stop'
# 编译真实窗口的选择记录、数量刷新与批次分配成员；不绘制界面或模拟原版搬运。
$taskFacilityRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'BDP.Content/Trion/Facility'
$taskWindowSource = Get-Content -Raw -LiteralPath (Join-Path $taskFacilityRoot 'Sacrifice/Dialog_TrionFacilitySacrifice.cs')
$taskLoadingSource = Get-Content -Raw -LiteralPath (Join-Path $taskFacilityRoot 'Sacrifice/CompTrionFacilitySacrifice.cs')
function Get-TaskWindowMember([string]$signature, [string]$source = $taskWindowSource) {
    $memberStart = $source.IndexOf($signature)
    if ($memberStart -lt 0) { throw "正式窗口成员缺失：$signature" }
    $braceStart = $source.IndexOf('{', $memberStart)
    $depth = 0
    for ($index = $braceStart; $index -lt $source.Length; $index++) {
        if ($source[$index] -eq '{') { $depth++ }
        if ($source[$index] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($memberStart, $index - $memberStart + 1) }
        }
    }
    throw "正式窗口成员不完整：$signature"
}
$taskFixture = @'
using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trion.Facility;
using BDP.Content.Trion.Facility.Sacrifice;
using RimWorld;
using Verse;
namespace BDP.Tests
{
    /// <summary>只提供真实已装批次成员需要的容器及保存字段，不模拟搬运系统。</summary>
    public sealed class SacrificeLoadedSubject
    {
        /// <summary>已装实物按不同于选择的顺序排列。</summary>
        public readonly List<Thing> innerContainer = new List<Thing>();
        /// <summary>正式保存的最后加入物品定义。</summary>
        private ThingDef lastSelectedDef;
        /// <summary>正式保存的最后加入物品固有市值。</summary>
        private float lastSelectedMarketValue;
        /// <summary>输入保存字段，不保留来源引用；真实成员定位已装实物。</summary>
        public SacrificeLoadedSubject(ThingDef definition, float value)
        {
            lastSelectedDef = definition;
            lastSelectedMarketValue = value;
        }
        __LOADED_MEMBER__
    }
    /// <summary>只提供真实窗口成员需要的字段及原版数量操作边界。</summary>
    public sealed class SacrificeSelectionSubject
    {
        /// <summary>本窗口核心。</summary>
        private readonly Building_TrionFacility facility = new Building_TrionFacility();
        /// <summary>真实分组后的窗口供物。</summary>
        private readonly List<TrionFacilitySacrificeSupplyGroup> supplies;
        /// <summary>真实成员维护的净增加段。</summary>
        private readonly List<KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>> selectionOrder
            = new List<KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>>();
        /// <summary>原生科技配置与真实供物分组，不复制数量或排序规则。</summary>
        public SacrificeSelectionSubject()
        {
            var settings = new TrionFacilitySacrificeSettingsDef { referenceMarketValue = 100 };
            settings.yieldTiers.Add(new TrionFacilitySacrificeYieldTier { techLevel = TechLevel.Neolithic, minYield = 1, maxYield = 2 });
            settings.yieldTiers.Add(new TrionFacilitySacrificeYieldTier { techLevel = TechLevel.Industrial, minYield = 10, maxYield = 20 });
            settings.yieldTiers.Add(new TrionFacilitySacrificeYieldTier { techLevel = TechLevel.Archotech, minYield = 10000, maxYield = 20000 });
            TrionFacilitySacrificeSettingsDefOf.BDP_TrionFacilitySacrificeSettings = settings;
            facility.comps.Add(new CompTrionFacilityResource { parent = facility,
                props = new CompProperties_TrionFacilityResource { maxResource = 1000 } });
            supplies = TrionFacilitySacrificeSupplyGroup.Create(new[] {
                new Thing { def = new ThingDef { techLevel = TechLevel.Industrial, healthAffectsPrice = false }, MarketValue = 32, stackCount = 9987 },
                new Thing { def = new ThingDef { techLevel = TechLevel.Neolithic }, MarketValue = 15, stackCount = 200 },
                new Thing { def = new ThingDef { techLevel = TechLevel.Archotech }, MarketValue = 100, stackCount = 3 }
            });
            RefreshSelectionLimits();
        }
        __RECORD_MEMBER__
        __BATCH_MEMBER__
        __LIMITS_MEMBER__
        /// <summary>原版数量控件以本行上限截断输入，再调用真实窗口变更记录。</summary>
        public void Select(int index, int count)
        {
            var group = supplies[index];
            int oldCount = group.CountToTransfer;
            group.ForceTo(Math.Min(Math.Max(0, count), group.GetMaximumToTransfer()));
            RecordSelectionChange(group, group.CountToTransfer - oldCount);
            RefreshSelectionLimits();
        }
        /// <summary>读取真实上限。</summary>
        public int Limit(int index) => supplies[index].GetMaximumToTransfer();
        /// <summary>读取真实数量。</summary>
        public int Count(int index) => supplies[index].CountToTransfer;
        /// <summary>读取真实批次分配后的最后一项。</summary>
        public bool LastIs(int index) => SelectedBatch().LastOrDefault()?.thing.def == supplies[index].Representative.def;
        /// <summary>最终核验使用真实共同收益及玩家末项。</summary>
        public bool Valid()
        {
            double total;
            float last;
            return TrionFacilitySacrificeUtility.TryGetBatchYield(SelectedBatch(), out total, out last)
                && TrionFacilitySacrificeUtility.CanAcceptBatch(total, last, 0, 1000);
        }
    }
}
'@
$taskFixture = $taskFixture.Replace('__RECORD_MEMBER__', (Get-TaskWindowMember 'private void RecordSelectionChange('))
$taskFixture = $taskFixture.Replace('__BATCH_MEMBER__', (Get-TaskWindowMember 'private List<ThingCountClass> SelectedBatch()'))
$taskFixture = $taskFixture.Replace('__LIMITS_MEMBER__', (Get-TaskWindowMember 'private void RefreshSelectionLimits()'))
$taskFixture = $taskFixture.Replace('__LOADED_MEMBER__', (Get-TaskWindowMember 'public List<ThingCountClass> LoadedBatch()' $taskLoadingSource))
$taskFixturePath = [IO.Path]::GetTempFileName() + '.cs'
$taskPaths = @(
    (Join-Path $PSScriptRoot 'TrionFacilityLifecycleTests.cs'),
    (Join-Path $taskFacilityRoot 'CompTrionFacilityResource.cs'),
    (Join-Path $taskFacilityRoot 'CompProperties_TrionFacilityResource.cs'),
    (Join-Path $taskFacilityRoot 'CompTrionFacilityControl.cs'),
    (Join-Path $taskFacilityRoot 'TrionFacilityDrainRateRegistry.cs'),
    (Join-Path $taskFacilityRoot 'ITrionFacilityDrainRateProvider.cs'),
    (Join-Path $taskFacilityRoot 'Sacrifice/TrionFacilitySacrificeSettingsDef.cs'),
    (Join-Path $taskFacilityRoot 'Sacrifice/TrionFacilitySacrificeSupplyGroup.cs'),
    (Join-Path $taskFacilityRoot 'Sacrifice/TrionFacilitySacrificeUtility.cs'),
    $taskFixturePath)
try {
    [IO.File]::WriteAllText($taskFixturePath, $taskFixture, [Text.UTF8Encoding]::new($false))
    Add-Type -Path $taskPaths
    $taskWindow = [BDP.Tests.SacrificeSelectionSubject]::new()
    $taskWindow.Select(0, [int]::MaxValue)
    if ($taskWindow.Count(0) -ne 81 -or $taskWindow.Limit(0) -ne 81 -or $taskWindow.Limit(1) -ne 0 -or
        $taskWindow.Limit(2) -ne 0 -or -not $taskWindow.LastIs(0) -or -not $taskWindow.Valid()) { throw '截图选满后的所有行必须停止增加。' }
    $taskWindow.Select(0, 80)
    if ($taskWindow.Limit(1) -ne 6) { throw '减回门槛以下后，果子只能按剩余容量增加六件。' }
    $taskWindow.Select(1, [int]::MaxValue)
    if ($taskWindow.Count(1) -ne 6 -or $taskWindow.Limit(0) -ne 80 -or -not $taskWindow.LastIs(1) -or
        -not $taskWindow.Valid()) { throw '最后操作果子时必须按果子的一件作为最后一件。' }
    $taskWindow.Select(1, 0)
    if (-not $taskWindow.LastIs(0) -or $taskWindow.Limit(0) -ne 81) { throw '清空末项后应恢复此前末项及可添数量。' }
    $taskWindow.Select(1, 2)
    $taskWindow.Select(0, 81)
    if (-not $taskWindow.LastIs(0)) { throw '再次加回零部件后，零部件应成为最后一项。' }
    $taskWindow.Select(0, 80)
    if (-not $taskWindow.LastIs(1)) { throw '撤回最近加的一件零部件后，最后一项应恢复为果子。' }
    $taskWindow = [BDP.Tests.SacrificeSelectionSubject]::new()
    $taskWindow.Select(2, 1)
    $taskWindow.Select(1, 3)
    if ($taskWindow.Count(1) -ne 0 -or -not $taskWindow.LastIs(2) -or -not $taskWindow.Valid()) { throw '先超凡溢出后不得插入小额供物。' }
    $taskWindow = [BDP.Tests.SacrificeSelectionSubject]::new()
    $taskWindow.Select(1, 3)
    $taskWindow.Select(2, [int]::MaxValue)
    if ($taskWindow.Count(2) -ne 1 -or -not $taskWindow.LastIs(2) -or -not $taskWindow.Valid()) { throw '先小额再超凡时只收一件最后的超凡供物。' }
    # 来源合堆销毁后仍按保存的价值特征找到真实末项，不取最高收益或容器末项。
    $taskLowDef = [Verse.ThingDef]::new()
    $taskHighDef = [Verse.ThingDef]::new()
    $taskOriginal = [Verse.Thing]::new()
    $taskOriginal.def = $taskLowDef
    $taskOriginal.MarketValue = 15
    $taskLoaded = [BDP.Tests.SacrificeLoadedSubject]::new($taskOriginal.def, $taskOriginal.MarketValue)
    $taskOriginal.Destroy([Verse.DestroyMode]::Vanish)
    $taskActualLow = [Verse.Thing]::new()
    $taskActualLow.def = $taskLowDef
    $taskActualLow.MarketValue = 15
    $taskActualHigh = [Verse.Thing]::new()
    $taskActualHigh.def = $taskHighDef
    $taskActualHigh.MarketValue = 100
    $taskLoaded.innerContainer.Add($taskActualLow)
    $taskLoaded.innerContainer.Add($taskActualHigh)
    $taskActualBatch = $taskLoaded.LoadedBatch()
    if ($taskActualBatch.Count -ne 2 -or $taskActualBatch[1].thing -ne $taskActualLow) { throw '搬运合堆和容器顺序不得改变玩家末项。' }
    $taskActualLow.MarketValue = 14
    if ($null -ne $taskLoaded.LoadedBatch()) { throw '找不到相同实际收益的末项时不得猜测其他物品。' }
    Write-Output '通过：玩家操作顺序、选满禁止追加、全部截断、减量恢复、来源销毁后匹配真实末项与价值变化拒绝。'
}
finally {
    if (Test-Path -LiteralPath $taskFixturePath) { Remove-Item -LiteralPath $taskFixturePath }
    $taskEmptyTempPath = $taskFixturePath.Substring(0, $taskFixturePath.Length - 3)
    if (Test-Path -LiteralPath $taskEmptyTempPath) { Remove-Item -LiteralPath $taskEmptyTempPath }
}
