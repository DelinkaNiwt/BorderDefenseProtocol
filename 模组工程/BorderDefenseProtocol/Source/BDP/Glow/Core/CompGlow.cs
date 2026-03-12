using System;
using UnityEngine;
using Verse;

namespace BDP.Glow
{
    /// <summary>
    /// 发光效果渲染组件——管理Graphic生命周期和IGlowController实例。
    ///
    /// 生命周期：
    ///   PostSpawnSetup() → 初始化Graphic和Controller
    ///   CompTick()       → 调用controller.Tick()
    ///   PostDraw()       → 绘制发光层（见CompGlow.Rendering.cs）
    ///   PostExposeData() → 序列化controller状态
    ///
    /// 渲染逻辑已拆分到 CompGlow.Rendering.cs（partial class）。
    /// </summary>
    public partial class CompGlow : ThingComp
    {
        // ── 运行时状态 ──

        /// <summary>发光层Graphic实例（由graphicData初始化）。</summary>
        private Graphic glowGraphic;

        /// <summary>发光控制器实例（由controllerClass反射创建）。</summary>
        private IGlowController controller;

        /// <summary>初始化失败标志——防止错误在每帧重复传播。</summary>
        private bool initFailed = false;

        /// <summary>Tick计数器（用于tickInterval节流）。</summary>
        private int tickCounter = 0;

        // ── 属性 ──

        public CompProperties_BDPGlow Props => (CompProperties_BDPGlow)props;

        // ─────────────────────────────────────────────────────────────────────
        // 生命周期
        // ─────────────────────────────────────────────────────────────────────

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 初始化Graphic
            if (!TryInitGraphic())
                return;

            // 初始化Controller（读档路径：controller已由ExposeData恢复，只需重新Initialize）
            if (controller == null)
            {
                if (!TryCreateController())
                    return;
            }

            controller.Initialize(parent, Props.controllerParams);
        }

        public override void CompTick()
        {
            if (initFailed || controller == null) return;

            // tickInterval节流：0=每tick，>0=每N tick
            if (Props.tickInterval > 0)
            {
                tickCounter++;
                if (tickCounter < Props.tickInterval) return;
                tickCounter = 0;
            }

            controller.Tick();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // 序列化controller状态（有状态控制器如PulseGlow需要保存ticksAlive等）
            if (controller != null)
            {
                controller.ExposeData();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 初始化辅助方法
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 尝试初始化发光层Graphic。
        /// 失败时设置initFailed=true并记录错误日志。
        /// </summary>
        private bool TryInitGraphic()
        {
            try
            {
                if (Props.graphicData == null)
                {
                    Log.ErrorOnce($"[BDP.Glow] {parent.def.defName} 的 CompProperties_BDPGlow.graphicData 未配置。", parent.def.GetHashCode() ^ 0x47A1);
                    initFailed = true;
                    return false;
                }

                glowGraphic = Props.graphicData.GraphicColoredFor(parent);
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[BDP.Glow] 初始化Graphic失败（{parent.def.defName}）: {ex.Message}", parent.def.GetHashCode() ^ 0x47A2);
                initFailed = true;
                return false;
            }
        }

        /// <summary>
        /// 通过反射创建IGlowController实例。
        /// 失败时设置initFailed=true并记录错误日志。
        /// </summary>
        private bool TryCreateController()
        {
            try
            {
                Type controllerType = GenTypes.GetTypeInAnyAssembly(Props.controllerClass);
                if (controllerType == null)
                {
                    Log.ErrorOnce($"[BDP.Glow] 找不到控制器类型: {Props.controllerClass}", Props.controllerClass.GetHashCode() ^ 0x47A3);
                    initFailed = true;
                    return false;
                }

                controller = (IGlowController)Activator.CreateInstance(controllerType);
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[BDP.Glow] 创建控制器失败（{Props.controllerClass}）: {ex.Message}", Props.controllerClass.GetHashCode() ^ 0x47A4);
                initFailed = true;
                return false;
            }
        }
    }
}
