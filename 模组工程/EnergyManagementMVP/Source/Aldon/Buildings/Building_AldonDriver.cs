using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Aldon.Energy
{
    /// <summary>
    /// 驱动塔建筑 - 控制全局能量恢复信号
    /// 提供UI按钮来激活/停用能量系统
    /// </summary>
    public class Building_AldonDriver : Building
    {
        /// <summary>
        /// 获取建筑的UI按钮(Gizmos)
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 先返回基类的按钮
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            AldonSignalManager mgr = AldonSignalManager.Instance;

            if (!mgr.IsActive)
            {
                // 显示激活按钮
                yield return new Command_Action
                {
                    defaultLabel = "激活驱动塔",
                    defaultDesc = "启动全局能量恢复系统，允许所有授权殖民者恢复能量",
                    icon = TexCommand.Attack,  // 临时使用系统图标
                    action = () =>
                    {
                        AldonSignalManager.Instance.Activate();
                        Messages.Message("驱动塔已激活 - 能量恢复系统启动", MessageTypeDefOf.PositiveEvent);
                    }
                };
            }
            else
            {
                // 显示停用按钮
                yield return new Command_Action
                {
                    defaultLabel = "停用驱动塔",
                    defaultDesc = "关闭全局能量恢复系统，所有殖民者停止恢复能量",
                    icon = TexCommand.CannotShoot,  // 临时使用系统图标（Cancel图标）
                    action = () =>
                    {
                        AldonSignalManager.Instance.Deactivate();
                        Messages.Message("驱动塔已停用 - 能量恢复系统关闭", MessageTypeDefOf.NeutralEvent);
                    }
                };
            }
        }

        /// <summary>
        /// Tick驱动 - 建筑本体的逻辑
        /// </summary>
        protected override void Tick()
        {
            base.Tick();

            // 驱动塔本体的Tick逻辑（如电力检查等）可在此添加
            // MVP版本暂时不需要额外逻辑
        }

        /// <summary>
        /// 建筑被摧毁时 - 自动停用信号
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // 如果驱动塔被摧毁，自动停用信号
            if (AldonSignalManager.Instance.IsActive)
            {
                AldonSignalManager.Instance.Deactivate();
                Messages.Message("驱动塔被摧毁 - 能量恢复系统已关闭", MessageTypeDefOf.NegativeEvent);
            }

            base.Destroy(mode);
        }
    }
}
