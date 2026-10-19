using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using BDP.Core.UI.Charts;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using UnityEngine;
using Color = UnityEngine.Color;

/// <summary>直接执行实际图表几何源码，核查原生顶点、裁切命令、面积和状态恢复。</summary>
internal static class Program
{
    /// <summary>当前进程唯一的目标版本引用目录，由调用者显式传入。</summary>
    private static string gameDir;

    /// <summary>只影响此独立验证进程的原生库查找。</summary>
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string path);

    /// <summary>先连接本版本程序集解析，再进入需要游戏类型的方法。</summary>
    private static void Main(string[] args)
    {
        gameDir = Path.GetFullPath(args.Single());
        SetDllDirectory(gameDir);
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string path = Path.Combine(gameDir, name.Name + ".dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
        };
        Verify();
    }

    /// <summary>所有断言都针对实际原生输出；缺少新的圆弧入口时明确失败。</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Verify()
    {
        Require("DrawArc");
        Require("DrawSector");
        ImGui.CreateContext();
        try
        {
            var io = ImGui.GetIO();
            io.IniFilename = null;
            io.DisplaySize = new float2(1200, 1200);
            io.DeltaTime = 1f / 60f;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
            io.Fonts.AddFontDefault();
            VerifyColorAndCoordinates();
            VerifyBoundaryGeometry();
            VerifySectorsAndArcs();
            VerifyClipAndState();
            VerifyNonDrawingEvents();
            Console.WriteLine("PASS：原生颜色、坐标、凸边界、凹轮廓、扇区、圆弧虚线、裁切和状态恢复。");
        }
        finally { ImGui.DestroyContext(); }
    }

    /// <summary>透明度逐通道保留，偏移和界面缩放各应用一次。</summary>
    private static void VerifyColorAndCoordinates()
    {
        Verse.Prefs.UIScale = 1.5f;
        Verse.UI.Offset = new Vector2(40f, 30f);
        Color target = new Color(.18f, .55f, .56f, .18f);
        Snapshot result = Frame(_ => ChartGeometryRenderer.DrawLine(
            new Rect(0, 0, 100, 100), new Vector2(10, 20), new Vector2(30, 20), target, 2f));
        Color32 color = target;
        uint packed = new ImColor8(color.r, color.g, color.b, color.a).AsUint();
        Assert(result.Colors.All(value => value == packed), "颜色或透明度未按实际顶点保留");
        // 原生 AddLine（线条）在物理坐标上增加半像素，令描边落在像素中心。
        Near(result.Points.Min(point => point.X), 75.5f, .01f, "横向偏移/缩放");
        Near(result.Points.Max(point => point.X), 105.5f, .01f, "横向终点");
        Near(result.Points.Min(point => point.Y), 74f, .01f, "纵向不应再次翻转");
        Near(result.Points.Max(point => point.Y), 77f, .01f, "线宽缩放");
        Verse.Prefs.UIScale = 1f;
        Verse.UI.Offset = Vector2.zero;
    }

    /// <summary>超限填充按雷达凸边界裁切，厚线也不得穿出边界；凹轮廓保留扇心语义。</summary>
    private static void VerifyBoundaryGeometry()
    {
        Vector2[] diamond = { new Vector2(50, 0), new Vector2(100, 50), new Vector2(50, 100), new Vector2(0, 50), new Vector2(50, 0) };
        Vector2[] outside = { new Vector2(-20, -20), new Vector2(120, -20), new Vector2(120, 120), new Vector2(-20, 120) };
        Snapshot fill = Frame(_ => ChartGeometryRenderer.DrawFilledPolygon(
            new Rect(0, 0, 100, 100), new Vector2(50, 50), outside, Color.white, diamond));
        Assert(fill.Points.All(InsideDiamond), "超限雷达填充穿出多边形边界");
        Near(fill.Area, 5000f, .1f, "雷达超限填充面积");
        Snapshot line = Frame(_ => ChartGeometryRenderer.DrawPolyline(
            new Rect(0, 0, 100, 100), new[] { new Vector2(-20, 50), new Vector2(120, 50) },
            Color.white, 10f, false, 5f, 3f, diamond));
        Assert(line.Points.All(InsideDiamond), "粗线边缘穿出雷达边界");
        Near(line.Area, 950f, .1f, "裁切必须保留粗线宽度");
        Vector2[] star = Enumerable.Range(0, 10).Select(index =>
        {
            float angle = index * Mathf.PI / 5f;
            float radius = index % 2 == 0 ? 40f : 15f;
            return new Vector2(50 + Mathf.Cos(angle) * radius, 50 + Mathf.Sin(angle) * radius);
        }).ToArray();
        Snapshot concave = Frame(_ => ChartGeometryRenderer.DrawFilledPolygon(
            new Rect(0, 0, 100, 100), new Vector2(50, 50), star, new Color(.2f, .8f, .7f, .4f)));
        Near(concave.Area, PolygonArea(star), .1f, "凹雷达填充不得变成凸包或重复叠面");
    }

    /// <summary>零度、半圆、大扇区和整圆的实际三角形面积符合扇区；虚线保留空隙。</summary>
    private static void VerifySectorsAndArcs()
    {
        foreach (float degrees in new[] { 0f, 90f, 180f, 270f, 360f })
        {
            Snapshot sector = Frame(_ => Call("DrawSector", new Rect(0, 0, 300, 300),
                new Vector2(150, 150), 100f, -90f - degrees * .5f, -90f + degrees * .5f, Color.white));
            float expected = Mathf.PI * 10000f * degrees / 360f;
            Near(sector.Area, expected, Math.Max(1f, expected * .025f), "扇区面积 " + degrees);
        }
        Snapshot solid = Frame(_ => Arc(false));
        Snapshot dashed = Frame(_ => Arc(true));
        Assert(dashed.Area > solid.Area * .35f && dashed.Area < solid.Area * .65f, "虚线圆弧应保留连续弧长空隙");
    }

    /// <summary>绘制圆弧时全部参数采用公共界面单位。</summary>
    private static void Arc(bool dashed)
    {
        Call("DrawArc", new Rect(0, 0, 300, 300), new Vector2(150, 150),
            100f, -90f, 0f, Color.white, 2f, dashed, 5f, 5f);
    }

    /// <summary>矩形裁切继承父区域，提交后恢复原裁切和抗锯齿标记。</summary>
    private static void VerifyClipAndState()
    {
        Snapshot result = Frame(list =>
        {
            list.PushClipRect(new float2(20, 30), new float2(80, 90), true);
            ImDrawListFlags flags = list.Flags = ImDrawListFlags.AntiAliasedFill;
            float2 min = list.GetClipRectMin(), max = list.GetClipRectMax();
            ChartGeometryRenderer.DrawFilledPolygon(new Rect(0, 0, 50, 50), new Vector2(25, 25),
                new[] { Vector2.zero, new Vector2(50, 0), new Vector2(50, 50), new Vector2(0, 50) }, Color.white);
            Assert(list.Flags == flags, "未恢复原抗锯齿标记");
            Assert(list.GetClipRectMin().Equals(min) && list.GetClipRectMax().Equals(max), "未恢复父裁切");
            list.PopClipRect();
        });
        Assert(result.Clips.Any(clip => clip.X == 20f && clip.Y == 30f && clip.Z == 50f && clip.W == 50f),
            "原生命令必须同时受父区域和当前画布约束");
    }

    /// <summary>布局和输入阶段不得生成图形，全透明图形无需提交。</summary>
    private static void VerifyNonDrawingEvents()
    {
        Snapshot result = Frame(_ =>
        {
            Event.current.type = EventType.Layout;
            ChartGeometryRenderer.DrawCircle(new Rect(0, 0, 100, 100), new Vector2(50, 50), 10f, Color.white);
            Event.current.type = EventType.Repaint;
            ChartGeometryRenderer.DrawCircle(new Rect(0, 0, 100, 100), new Vector2(50, 50), 10f, Color.clear);
        });
        Assert(result.Points.Length == 0, "非绘制事件或全透明圆不应提交顶点");
    }

    /// <summary>新建独立原生帧并复制输出，不读取运行中的游戏状态。</summary>
    private static Snapshot Frame(Action<ImDrawListPtr> draw)
    {
        ImGui.NewFrame();
        Event.current = new Event { type = EventType.Repaint };
        var list = ImGui.GetBackgroundDrawList();
        list.Flags = ImDrawListFlags.None;
        draw(list);
        ImGui.Render();
        var result = new Snapshot
        {
            Points = Enumerable.Range(0, list.VtxBuffer.Count).Select(index => list.VtxBuffer[index].pos).ToArray(),
            Colors = Enumerable.Range(0, list.VtxBuffer.Count).Select(index => list.VtxBuffer[index].col).ToArray(),
            Indices = Enumerable.Range(0, list.IdxBuffer.Count).Select(index => (int)list.IdxBuffer[index]).ToArray(),
            Clips = Enumerable.Range(0, list.CmdBuffer.Count).Where(index => list.CmdBuffer[index].ElemCount > 0)
                .Select(index => list.CmdBuffer[index].ClipRect).ToArray()
        };
        return result;
    }

    /// <summary>读取真实几何入口；旧实现缺少入口时提供可定位的预期失败。</summary>
    private static MethodInfo Require(string name)
    {
        return typeof(ChartGeometryRenderer).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("缺少原生几何入口：" + name);
    }

    /// <summary>通过已解析成员执行几何，测试可先于新增入口编译并观察失败。</summary>
    private static void Call(string name, params object[] args) { Require(name).Invoke(null, args); }

    /// <summary>检查点是否落在已知菱形内部，包含浮点交点误差。</summary>
    private static bool InsideDiamond(float2 point) { return Math.Abs(point.X - 50f) + Math.Abs(point.Y - 50f) <= 50.001f; }

    /// <summary>用多边形面积公式提供独立几何预期。</summary>
    private static float PolygonArea(IReadOnlyList<Vector2> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % points.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return Math.Abs(area) * .5f;
    }

    /// <summary>比较带容差的原生几何结果。</summary>
    private static void Near(float actual, float expected, float tolerance, string label)
    {
        Assert(Math.Abs(actual - expected) <= tolerance, label + "：实际 " + actual + "，预期 " + expected);
    }

    /// <summary>失败必须终止验证进程。</summary>
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    /// <summary>保存一帧的实际顶点、颜色、索引和裁切命令。</summary>
    private sealed class Snapshot
    {
        /// <summary>实际物理像素顶点。</summary>
        internal float2[] Points;
        /// <summary>实际四通道颜色。</summary>
        internal uint[] Colors;
        /// <summary>三角形索引。</summary>
        internal int[] Indices;
        /// <summary>存在绘制内容的裁切命令。</summary>
        internal float4[] Clips;
        /// <summary>累加实际三角形面积，揭示漏填、凸包替代或重复叠面。</summary>
        internal float Area
        {
            get
            {
                float area = 0f;
                for (int i = 0; i < Indices.Length; i += 3)
                {
                    float2 a = Points[Indices[i]], b = Points[Indices[i + 1]], c = Points[Indices[i + 2]];
                    area += Math.Abs((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * .5f;
                }
                return area;
            }
        }
    }
}

namespace Verse
{
    /// <summary>只替代游戏窗口坐标环境；图元和几何输出均执行真实原生库。</summary>
    internal static class UI
    {
        /// <summary>模拟原版窗口和滚动产生的局部偏移。</summary>
        internal static Vector2 Offset;
        /// <summary>原版此转换返回尚未乘界面缩放的坐标。</summary>
        internal static Vector2 GUIToScreenPoint(Vector2 point) { return point + Offset; }
    }
    /// <summary>独立验证进程的缩放输入，不加载游戏存档或偏好设置。</summary>
    internal static class Prefs
    {
        /// <summary>由各用例设置的目标缩放。</summary>
        internal static float UIScale = 1f;
    }
    /// <summary>旧路径红灯验证的材质入口占位；新原生路径不得使用它。</summary>
    internal static class SolidColorMaterials
    {
        /// <summary>新路径回归若仍依赖旧材质，应立即失败。</summary>
        internal static Material SimpleSolidColorMaterial(Color color) { throw new InvalidOperationException("1.7 原生图形仍请求旧材质"); }
    }
}
