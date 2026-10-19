using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.FloatingFlight.UI
{
    /// <summary>浮空模式按钮，复用原版动作按钮并提供右键模式菜单。</summary>
    public sealed class Command_FloatingFlightMode : Command_Action
    {
        /// <summary>当前 Pawn 右键菜单的动态构建函数。</summary>
        public Func<IEnumerable<FloatMenuOption>> RightClickOptionsGetter { get; set; }

        /// <summary>读取当前右键浮空模式选项。</summary>
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
