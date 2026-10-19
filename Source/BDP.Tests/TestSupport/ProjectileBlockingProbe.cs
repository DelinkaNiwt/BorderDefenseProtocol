using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using RimWorld;
using UnityEngine;
using Verse;

/// <summary>只建立最小原版格网现场，所有阻挡扫描与路径裁短均调用正式宿主实现。</summary>
public static class ProjectileBlockingProbe
{
    /// <summary>为到达阶段提供固定的真实到达位置，不依赖游戏图形与倒计时初始化。</summary>
    public static Projectile CreateArrivalProjectile() { return new ArrivalProjectile(); }

    /// <summary>只替代现场位置，路线阶段和汇总逻辑仍使用正式实现。</summary>
    private sealed class ArrivalProjectile : Bullet
    {
        /// <summary>当前固定的锚点到达位置。</summary>
        public override Vector3 ExactPosition { get { return new Vector3(2.5f, 0f, 2.5f); } }
    }

    /// <summary>验证近墙、首续段起点差异、曲线和越顶规则。</summary>
    public static void Run()
    {
        Game saved = Current.Game;
        try
        {
            Game game = Blank<Game>();
            Map map = Blank<Map>();
            map.info = Blank<MapInfo>();
            map.info.Size = new IntVec3(32, 1, 32);
            map.cellIndices = new CellIndices(map);
            map.thingGrid = new ThingGrid(map);
            typeof(Game).GetField("maps", Hidden).SetValue(game, new List<Map> { map });
            Current.Game = game;
            Thing wall = AddWall(map, new IntVec3(4, 0, 2));
            BdpProjectile projectile = new BdpProjectile { def = Blank<ThingDef>() };
            projectile.def.projectile = new ProjectileProperties();
            SetMapIndex(projectile);
            MethodInfo binding = typeof(BdpProjectile).GetMethod("ResolveBindingFlightPathSnapshot", Hidden);
            Check(binding != null, "首段和续段没有共用阻挡准入。");
            ProjectileFlightPathSnapshot path = ProjectileFlightPathUtility.CreateLinear(
                new Vector3(2.5f, 0f, 2.5f), new Vector3(20.5f, 0f, 2.5f));
            Check(VerbUtility.InterceptChanceFactorFromDistance(path.Start, wall.Position) == 0f,
                "现场墙体不在原版沿途拦截跳过的5格范围内。");
            foreach (bool continuation in new[] { false, true })
            {
                ProjectileFlightPathSnapshot clipped = Bind(binding, projectile, path, continuation);
                Check(clipped.End.ToIntVec3() == wall.Position && clipped.ApproximateLength < 2f,
                    "近墙没有在首段/续段绑定前裁短，仍会先飞到墙后再补算。");
            }

            Thing second = AddWall(map, new IntVec3(7, 0, 2));
            ProjectileFlightPathSnapshot inside = ProjectileFlightPathUtility.CreateLinear(
                new Vector3(4.5f, 0f, 2.5f), path.End);
            Check(Bind(binding, projectile, inside, false).End.ToIntVec3() == wall.Position,
                "首发错误地采用了续段起点格例外。");
            Check(Bind(binding, projectile, inside, true).End.ToIntVec3() == second.Position,
                "续段跳过起点格后没有识别前方第二面墙。");

            Thing curveWall = AddWall(map, new IntVec3(7, 0, 7));
            ProjectileFlightPathSnapshot curve = ProjectileFlightPathUtility.CreateCubicBezier(
                new Vector3(2.5f, 0f, 2.5f), new Vector3(2.5f, 0f, 8.5f),
                new Vector3(12.5f, 0f, 8.5f), new Vector3(12.5f, 0f, 2.5f));
            ProjectileFlightPathSnapshot clippedCurve = Bind(binding, projectile, curve, false);
            Check(clippedCurve.Kind == ProjectileFlightPathKind.CubicBezier
                && clippedCurve.End.ToIntVec3() == curveWall.Position,
                "首段曲线没有沿真实曲线识别墙体或裁短时丢失曲线形状。");

            projectile.def.projectile.flyOverhead = true;
            Check((Bind(binding, projectile, path, false).End - path.End).sqrMagnitude < 0.000001f,
                "统一准入破坏了越顶飞行许可。");
        }
        finally { Current.Game = saved; }
    }

    /// <summary>测试夹具所用的非公开实例成员范围。</summary>
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>跳过依赖游戏图形初始化的构造，仅填充本测试所需的现场数据。</summary>
    private static T Blank<T>() { return (T)RuntimeHelpers.GetUninitializedObject(typeof(T)); }

    /// <summary>在真实格网中登记一个满格阻挡物，不触发地图生成和渲染。</summary>
    private static Thing AddWall(Map map, IntVec3 cell)
    {
        Thing wall = new Thing { def = Blank<ThingDef>(), Position = cell };
        wall.def.defName = "BlockingProbeWall";
        wall.def.fillPercent = 1f;
        SetMapIndex(wall);
        map.thingGrid.ThingsListAt(cell).Add(wall);
        return wall;
    }

    /// <summary>将夹具实体关联到当前测试地图，两版分别使用各自字段的实际类型。</summary>
    private static void SetMapIndex(Thing thing)
    {
        FieldInfo field = typeof(Thing).GetField("mapIndexOrState", Hidden);
        field.SetValue(thing, Convert.ChangeType(0, field.FieldType));
    }

    /// <summary>调用正式宿主的段绑定准入，不在夹具内重写扫描或裁短算法。</summary>
    private static ProjectileFlightPathSnapshot Bind(MethodInfo method, BdpProjectile projectile,
        ProjectileFlightPathSnapshot path, bool continuation)
    {
        return (ProjectileFlightPathSnapshot)method.Invoke(projectile, new object[] { path, continuation });
    }

    /// <summary>用具体行为错误终止检查。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
