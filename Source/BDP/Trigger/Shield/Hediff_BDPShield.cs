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
        /// 重写Label属性，优先使用当前stage的label
        /// 如果stage没有定义label，则回退到HediffDef的label
        /// </summary>
        public override string Label
        {
            get
            {
                // 优先使用当前stage的label
                if (CurStage?.label != null)
                {
                    return CurStage.label;
                }

                // 回退到HediffDef的label
                return def.label ?? "护盾";
            }
        }

        /// <summary>
        /// 重写LabelInBrackets，不显示额外信息
        /// </summary>
        public override string LabelInBrackets => null;
    }
}
