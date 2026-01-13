using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion 传送锚
    ///
    /// 职责：
    /// - 作为Bail Out的传送目标
    /// - 提供安全的脱离着陆点
    /// - 可被启用/禁用
    ///
    /// 工作流程：
    /// 1. 玩家构建 Building_BailOutAnchor
    /// 2. 锚点自动激活（IsActive = true）
    /// 3. 当Pawn执行Bail Out时，优先选择最近的有效锚点
    /// 4. 传送到锚点位置
    ///
    /// 特点：
    /// - 无需能源（被动设施）
    /// - 可通过Gizmo启用/禁用
    /// - 不占用宝贵的电力资源
    /// </summary>
    public class Building_BailOutAnchor : Building
    {
        // ============ 数据 ============
        private bool _isActive = true;  // 是否激活

        // ============ 属性 ============
        public bool IsActive => _isActive;

        // ============ 初始化 ============

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                Log.Message($"[Trion] 传送锚 已建造 @ {Position}");
            }
        }

        // ============ Gizmo ============

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            // 启用/禁用按钮
            string label = _isActive ? "禁用锚点" : "启用锚点";
            string desc = _isActive
                ? "禁用此传送锚，Bail Out 将不再传送到此处"
                : "启用此传送锚，Bail Out 可传送到此处";

            yield return new Command_Toggle
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = TexCommand.Install,
                isActive = () => _isActive,
                toggleAction = () =>
                {
                    _isActive = !_isActive;
                    Log.Message($"[Trion] 传送锚 {(_isActive ? "已启用" : "已禁用")}");
                }
            };

            // 信息按钮
            yield return new Command_Action
            {
                defaultLabel = "信息",
                defaultDesc = "查看此传送锚的状态",
                action = () =>
                {
                    string status = _isActive ? "激活中" : "已禁用";
                    Messages.Message($"[Trion] 传送锚状态: {status}", MessageTypeDefOf.NeutralEvent);
                }
            };
        }

        // ============ 序列化 ============

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _isActive, "isActive", true);
        }
    }
}
