# 编译真实喷射运行时，检查来源同步是否保留其他部位的待发爆发和计数。
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$runtimePath = Join-Path $sourceRoot 'BDP.Content/CombatBody/Wounds/Visuals/CombatBodyWoundSprayRuntime.cs'
$boundarySource = @'
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Verse
{
    public enum LookMode { Value }
    public static class Scribe_Collections
    {
        public static void Look<K,V>(ref Dictionary<K,V> values, string label, LookMode keys, LookMode vals) { }
    }
    public class Hediff { public int loadID; }
    public class HediffSet { public List<Hediff> hediffs = new List<Hediff>(); }
    public class Pawn_HealthTracker { public HediffSet hediffSet = new HediffSet(); }
    public class Pawn { public Pawn_HealthTracker health = new Pawn_HealthTracker(); }
}
namespace BDP.Content.CombatBody.Wounds.Visuals
{
    // 替身只记录喷射器接收到的动作，不重写真实运行时的增删与同步。
    internal sealed class CombatBodyWoundSprayEmitter
    {
        internal bool PendingBurst;
        internal int TickCount;
        internal float CutTilt;
        internal CombatBodyWoundSprayEmitter(Hediff wound, float? tilt) { CutTilt = tilt ?? 7f; }
        internal void NotifyBurst() { PendingBurst = true; }
        internal void Tick(Pawn pawn) { TickCount++; }
    }
}
namespace BDP.Tests
{
    using BDP.Content.CombatBody.Wounds.Visuals;
    public static class WoundSpraySyncBehavior
    {
        public static void Run()
        {
            var pawn = new Pawn();
            var first = new Hediff { loadID = 1 };
            var second = new Hediff { loadID = 2 };
            pawn.health.hediffSet.hediffs.Add(first);
            pawn.health.hediffSet.hediffs.Add(second);
            var runtime = new CombatBodyWoundSprayRuntime();
            runtime.NotifyWoundAdded(pawn, first);
            runtime.Tick(pawn);
            var field = typeof(CombatBodyWoundSprayRuntime).GetField("emitters", BindingFlags.Instance | BindingFlags.NonPublic);
            var emitters = (Dictionary<int, CombatBodyWoundSprayEmitter>)field.GetValue(runtime);
            var original = emitters[1];
            runtime.RebuildFromActiveDrains(pawn, new[] { 1, 2 });
            runtime.NotifyWoundAdded(pawn, second);
            if (!Object.ReferenceEquals(original, emitters[1]) || !emitters[1].PendingBurst || emitters[1].TickCount != 1)
                throw new Exception("新部位受伤不能重置已有部位的待发爆发与发射计数");
            if (!emitters[2].PendingBurst) throw new Exception("新受伤部位必须保留自己的爆发");
            runtime.RebuildFromActiveDrains(pawn, new[] { 2 });
            if (emitters.Count != 1 || emitters.ContainsKey(1) || !emitters[2].PendingBurst)
                throw new Exception("来源停止时只能删除失效部位，其他部位必须继续");
            runtime.RebuildFromActiveDrains(pawn, new[] { 1, 2 });
            if (emitters[1].PendingBurst) throw new Exception("读档或同步新增来源不能自行重播受伤爆发");
            runtime.RebuildFromActiveDrains(pawn, null);
            if (emitters.Count != 0) throw new Exception("空来源必须清理全部喷射器");
        }
    }
}
'@
Add-Type -TypeDefinition ($boundarySource + [Environment]::NewLine + (Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8).Replace('using System.Collections.Generic;', '').Replace('using Verse;', ''))
[BDP.Tests.WoundSpraySyncBehavior]::Run()
Write-Output 'CombatBodyWoundSpraySyncBehaviorTests PASS'
