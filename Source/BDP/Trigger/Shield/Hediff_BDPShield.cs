using Verse;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// 护盾Hediff主类
    /// 所有逻辑在HediffComp_BDPShield中实现
    /// v2.0：重写Label属性，根据Severity动态显示名称
    /// </summary>
    public class Hediff_BDPShield : HediffWithComps
    {
        /// <summary>
        /// 重写Label属性，根据Severity返回不同名称
        /// Severity>=2：使用Props.stackedLabel（如果配置了）
        /// Severity=1：使用HediffDef的label
        /// </summary>
        public override string Label
        {
            get
            {
                // 获取护盾组件配置
                var shieldComp = this.TryGetComp<HediffComp_BDPShield>();
                if (shieldComp != null && Severity >= 2f)
                {
                    // Severity>=2时，使用stackedLabel（如果配置了）
                    if (!string.IsNullOrEmpty(shieldComp.Props.stackedLabel))
                    {
                        return shieldComp.Props.stackedLabel;
                    }
                }

                // 默认使用HediffDef的label
                return def.label ?? "护盾";
            }
        }

        /// <summary>
        /// 重写LabelInBrackets，显示激活芯片数量
        /// 例如："2芯片"
        /// </summary>
        public override string LabelInBrackets
        {
            get
            {
                int chipCount = (int)Severity;
                return $"{chipCount}芯片";
            }
        }
    }
}
