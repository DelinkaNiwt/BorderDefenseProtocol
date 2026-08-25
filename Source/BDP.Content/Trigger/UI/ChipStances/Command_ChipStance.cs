using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.UI.ChipStances
{
    /// <summary>
    /// 芯片姿态操作按钮。
    /// 它只在原版 Command_Action（动作按钮）上补充动态右键菜单读取口。
    /// </summary>
    public sealed class Command_ChipStance : Command_Action
    {
        /// <summary>
        /// 当前按钮右键菜单的动态构建函数。
        /// </summary>
        public Func<IEnumerable<FloatMenuOption>> RightClickOptionsGetter { get; set; }

        /// <summary>
        /// 读取当前可用的右键姿态选项。
        /// </summary>
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                return RightClickOptionsGetter != null
                    ? RightClickOptionsGetter()
                    : Array.Empty<FloatMenuOption>();
            }
        }
    }
}
