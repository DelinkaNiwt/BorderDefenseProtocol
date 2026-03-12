using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 被动芯片效果基类——激活时不执行任何操作，效果由其他系统检测和触发。
    /// 适用于紧急脱离、蓑衣虫（雷达隐身）等被动触发的芯片。
    /// 机制：空壳标记，实际逻辑在外部系统中实现。
    /// </summary>
    public class PassiveChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            // 被动效果，激活时无需操作
            // 实际逻辑由外部系统检测芯片是否激活并执行相应行为
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            // 解除时无需操作
        }

        public void Tick(Pawn pawn, Thing triggerBody)
        {
            // 无需每tick逻辑
        }

        public bool CanActivate(Pawn pawn, Thing triggerBody)
        {
            // 总是可以激活
            return true;
        }
    }
}
