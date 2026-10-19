$ErrorActionPreference = 'Stop'
# 仅编译正式存档成员，宿主复现真实程序集已确认的缺失节点行为；不模拟完整游戏读档。
$taskSourcePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'BDP.Content/Trion/Facility/Sacrifice/CompTrionFacilitySacrifice.cs'
$taskSource = Get-Content -Raw -LiteralPath $taskSourcePath
$taskMethodStart = $taskSource.IndexOf('public override void PostExposeData()')
if ($taskMethodStart -lt 0) { throw '正式组件缺少存档成员。' }
$taskBraceStart = $taskSource.IndexOf('{', $taskMethodStart)
$taskDepth = 0
$taskMethodEnd = -1
for ($taskIndex = $taskBraceStart; $taskIndex -lt $taskSource.Length; $taskIndex++) {
    if ($taskSource[$taskIndex] -eq '{') { $taskDepth++ }
    if ($taskSource[$taskIndex] -eq '}') {
        $taskDepth--
        if ($taskDepth -eq 0) { $taskMethodEnd = $taskIndex; break }
    }
}
if ($taskMethodEnd -lt 0) { throw '无法读取完整正式存档成员。' }
$taskMethod = $taskSource.Substring($taskMethodStart, $taskMethodEnd - $taskMethodStart + 1)
$taskHost = @'
using System;
using System.Collections.Generic;
using Verse;
namespace Verse
{
    /// <summary>隔离原版持有物；只观察是否保留已装实物。</summary>
    public sealed class Thing { }
    /// <summary>正式存档成员的物品定义引用。</summary>
    public sealed class ThingDef { }
    /// <summary>正式存档成员引用的人物宿主。</summary>
    public sealed class Pawn { }
    /// <summary>隔离原版拥有者关系和容器更新入口。</summary>
    public class ThingOwner
    {
        /// <summary>此容器的实际拥有者。</summary>
        public object Owner;
        /// <summary>保存的实物。</summary>
        public readonly List<Thing> Things = new List<Thing>();
        /// <summary>容器更新次数。</summary>
        public int Ticks;
        /// <summary>空引用会在原版相同调用处抛出异常。</summary>
        public void DoTick() { Ticks++; }
    }
    /// <summary>正式代码使用的原版泛型容器构造入口。</summary>
    public sealed class ThingOwner<T> : ThingOwner
    {
        /// <summary>构造时持有正确组件引用。</summary>
        public ThingOwner(object owner) { Owner = owner; }
    }
    /// <summary>只区分存档生命周期阶段。</summary>
    public enum LoadSaveMode { Inactive, LoadingVars, ResolvingCrossRefs, PostLoadInit, Saving }
    /// <summary>提供从真实程序集确认的读档边界输入。</summary>
    public static class Scribe
    {
        /// <summary>当前阶段。</summary>
        public static LoadSaveMode mode;
        /// <summary>缺失深存档节点返回空；已有节点返回真实容器引用。</summary>
        public static ThingOwner LoadedContainer;
        /// <summary>原版缺失装载编号时使用零。</summary>
        public static int LoadedGroup;
        /// <summary>原版缺失质量覆盖时使用零。</summary>
        public static float LoadedMass;
        /// <summary>隔离原版字段存取；保存实际成员写入的末项数据。</summary>
        public static readonly Dictionary<string, object> Fields = new Dictionary<string, object>();
    }
    /// <summary>本检查不解析人物交叉引用。</summary>
    public static class Scribe_References
    {
        /// <summary>保留正式成员调用接口。</summary>
        public static void Look<T>(ref T value, string key) { }
    }
    /// <summary>本检查保留定义引用，仅验证容器初始化。</summary>
    public static class Scribe_Defs
    {
        /// <summary>保留正式定义存档接口。</summary>
        public static void Look<T>(ref T value, string key)
        {
            if (Scribe.mode == LoadSaveMode.Saving) Scribe.Fields[key] = value;
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                value = Scribe.Fields.ContainsKey(key) ? (T)Scribe.Fields[key] : default(T);
        }
    }
    /// <summary>本检查不改变业务件数；仅验证原版容器初始化。</summary>
    public static class Scribe_Values
    {
        /// <summary>保留正式成员调用接口。</summary>
        public static void Look<T>(ref T value, string key, T fallback)
        {
            if (Scribe.mode == LoadSaveMode.Saving) Scribe.Fields[key] = value;
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                value = Scribe.Fields.ContainsKey(key) ? (T)Scribe.Fields[key] : fallback;
        }
    }
}
namespace BDP.Tests
{
    /// <summary>仅隔离原版父类的构造和缺失节点赋值，不复制BDP修复逻辑。</summary>
    public class TransporterHost
    {
        /// <summary>构造容器会在原版加载缺失节点时被覆盖为空。</summary>
        public ThingOwner innerContainer;
        /// <summary>原版未准备的构造默认值。</summary>
        public int groupID = -1;
        /// <summary>原版不覆盖质量的构造默认值。</summary>
        public float massCapacityOverride = -1f;
        /// <summary>复现原版正常构造。</summary>
        public TransporterHost() { innerContainer = new ThingOwner<Thing>(this); }
        /// <summary>复现原版LoadingVars赋值边界。</summary>
        public virtual void PostExposeData()
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars) return;
            innerContainer = Scribe.LoadedContainer;
            groupID = Scribe.LoadedGroup;
            massCapacityOverride = Scribe.LoadedMass;
        }
        /// <summary>与原版报错位置相同的更新调用。</summary>
        public void CompTick() { innerContainer.DoTick(); }
    }
    /// <summary>插入真实正式存档成员；其余宿主不实现业务规则。</summary>
    public sealed class SacrificeLoadSubject : TransporterHost
    {
        /// <summary>正式存档成员的选定人物字段。</summary>
        private Pawn sacrificer;
        /// <summary>正式存档成员的批次数量字段。</summary>
        private int requestedCount;
        /// <summary>正式组件保存的末项定义及固有市值。</summary>
        private ThingDef lastSelectedDef;
        /// <summary>正式组件保存的末项固有市值。</summary>
        private float lastSelectedMarketValue;
        /// <summary>指定保存输入，不复制正式序列化逻辑。</summary>
        public void SelectLast(ThingDef definition, float marketValue)
        { lastSelectedDef = definition; lastSelectedMarketValue = marketValue; }
        /// <summary>观察正式成员读取的末项。</summary>
        public ThingDef LastDefinition => lastSelectedDef;
        /// <summary>观察正式成员读取的末项价值。</summary>
        public float LastMarketValue => lastSelectedMarketValue;
        __SOURCE_METHOD__
    }
}
'@
Add-Type -TypeDefinition ($taskHost.Replace('__SOURCE_METHOD__', $taskMethod))

# 更新前旧存档没有容器节点：正式加载成员必须恢复可更新、空闲且拥有者正确的容器。
$taskMissing = [BDP.Tests.SacrificeLoadSubject]::new()
[Verse.Scribe]::LoadedContainer = $null
[Verse.Scribe]::LoadedGroup = 0
[Verse.Scribe]::LoadedMass = 0
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::LoadingVars
$taskMissing.PostExposeData()
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::ResolvingCrossRefs
$taskMissing.PostExposeData()
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::PostLoadInit
$taskMissing.PostExposeData()
$taskMissing.CompTick()
if ($taskMissing.innerContainer.Owner -ne $taskMissing -or $taskMissing.innerContainer.Things.Count -ne 0 -or
    $taskMissing.groupID -ne -1 -or $taskMissing.massCapacityOverride -ne -1) { throw '缺失容器必须恢复原版空闲初始状态。' }

# 已有合法容器：读档必须保留原容器、实物、装载编号及质量覆盖，不能把准备中的内容重置。
$taskExisting = [BDP.Tests.SacrificeLoadSubject]::new()
$taskSavedContainer = [Verse.ThingOwner[Verse.Thing]]::new($taskExisting)
$taskSavedThing = [Verse.Thing]::new()
$taskSavedContainer.Things.Add($taskSavedThing)
[Verse.Scribe]::LoadedContainer = $taskSavedContainer
[Verse.Scribe]::LoadedGroup = 42
[Verse.Scribe]::LoadedMass = 99
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::LoadingVars
$taskExisting.PostExposeData()
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::PostLoadInit
$taskExisting.PostExposeData()
$taskExisting.CompTick()
if ($taskExisting.innerContainer -ne $taskSavedContainer -or $taskExisting.innerContainer.Things[0] -ne $taskSavedThing -or
    $taskExisting.groupID -ne 42 -or $taskExisting.massCapacityOverride -ne 99) { throw '合法存档内容不得重置。' }
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::Saving
$taskLastDefinition = [Verse.ThingDef]::new()
$taskExisting.SelectLast($taskLastDefinition, 15.25)
$taskExisting.PostExposeData()
if ($taskExisting.innerContainer -ne $taskSavedContainer -or $taskExisting.groupID -ne 42) { throw '保存不得清除供物准备。' }
$taskRestored = [BDP.Tests.SacrificeLoadSubject]::new()
[Verse.Scribe]::mode = [Verse.LoadSaveMode]::LoadingVars
$taskRestored.PostExposeData()
if ($taskRestored.LastDefinition -ne $taskLastDefinition -or $taskRestored.LastMarketValue -ne 15.25) { throw '末项定义及固有市值必须在正式存档成员中保存、恢复。' }
Write-Output '通过：缺失节点恢复空闲容器，已有容器及准备保留，正式存档成员保存并恢复末项定义和市值。完整游戏读档仍需实测。'
