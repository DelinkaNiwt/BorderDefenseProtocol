using BDP.Trigger;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 紧急脱离芯片效果。
    /// 被动效果：战斗体破裂时触发传送。
    /// </summary>
    public class EmergencyEscapeChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            // 紧急脱离芯片是被动效果，激活时无需操作
            // 实际传送逻辑在CombatBodyOrchestrator.Deactivate中检查
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
