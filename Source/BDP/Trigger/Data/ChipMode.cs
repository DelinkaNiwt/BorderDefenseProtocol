using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片形态定义。每个形态包含效果列表、属性修正。
    /// 属于 CompProperties_TriggerChip.modes 列表的元素，从XML加载，无需序列化。
    /// </summary>
    public class ChipMode
    {
        /// <summary>形态名称（UI显示）</summary>
        public string label;

        /// <summary>形态描述（可选）</summary>
        public string description;

        /// <summary>该形态的效果类型列表</summary>
        public List<Type> effectClasses;

        /// <summary>加算属性修正</summary>
        public List<StatModifier> statOffsets;

        /// <summary>乘算属性修正</summary>
        public List<StatModifier> statFactors;

        /// <summary>切换到此形态的Trion消耗（0=免费）</summary>
        public float switchCost;

        /// <summary>切换预热时间ticks（0=即时）</summary>
        public int switchWarmup;
    }
}
