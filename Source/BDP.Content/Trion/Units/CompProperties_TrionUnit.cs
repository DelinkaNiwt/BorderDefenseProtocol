using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trion.Units
{
    /// <summary>款型共用基底配置，外围许可和前置资格可以替换接入。</summary>
    public sealed class CompProperties_TrionUnit : CompProperties
    {
        /// <summary>当前默认业务消费的许可键，不在生命组件内检查核心建筑。</summary>
        public string operationPermissionKey = "BDP.TrionRelayCore.Active";
        /// <summary>可替换的运行条件实现；具体许可只是默认实现的一种来源。</summary>
        public Type operationConditionClass = typeof(TrionUnitOperationConditions);
        /// <summary>固定内部触发体，款型定义预载特殊槽芯片。</summary>
        public ThingDef internalTrigger;
        /// <summary>永久身体的健康页标记，不携带阻止死亡或人体转换行为。</summary>
        public HediffDef bodyMarker;
        /// <summary>无通知的归属或派系变化低频核对间隔，不扫描全地图建筑。</summary>
        public int conditionCheckInterval = 60;
        /// <summary>将定义绑定到唯一兵组件。</summary>
        public CompProperties_TrionUnit() { compClass = typeof(CompTrionUnit); }
        /// <summary>启动期报告无效接入，不能静默回退为无条件运行。</summary>
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (operationConditionClass == null || !typeof(ITrionUnitOperationCondition).IsAssignableFrom(operationConditionClass))
                yield return parentDef.defName + ": invalid operationConditionClass.";
            if (conditionCheckInterval < 1) yield return parentDef.defName + ": conditionCheckInterval must be positive.";
        }
    }
}
