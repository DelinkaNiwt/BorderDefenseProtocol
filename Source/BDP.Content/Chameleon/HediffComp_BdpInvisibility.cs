using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Content.Chameleon
{
    /// <summary>
    /// 变色龙隐身运行时组件。
    /// 复用原版隐身状态计算，只补上不依赖 DLC 的攻击目标缓存刷新和攻击后关断通知。
    /// </summary>
    public sealed class HediffComp_BdpInvisibility : HediffComp_Invisibility
    {
        /// <summary>
        /// 当前组件的强类型配置。
        /// </summary>
        public new HediffCompProperties_BdpInvisibility Props
        {
            get { return (HediffCompProperties_BdpInvisibility)props; }
        }

        /// <summary>
        /// 隐身状态加入 Pawn 后订阅中性攻击动作事件。
        /// </summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            AttackActionSuccessDispatcher.AttackActionSucceeded += OnAttackActionSucceeded;
            RefreshAttackTargetCache();
        }

        /// <summary>
        /// 隐身状态移除前解除事件订阅，并刷新攻击目标缓存。
        /// </summary>
        public override void CompPostPostRemoved()
        {
            AttackActionSuccessDispatcher.AttackActionSucceeded -= OnAttackActionSucceeded;
            base.CompPostPostRemoved();
            RefreshAttackTargetCache();
        }

        /// <summary>
        /// 当前 Pawn 完成任意攻击动作后，立刻请求关闭变色龙芯片。
        /// </summary>
        private void OnAttackActionSucceeded(AttackActionSuccess attack)
        {
            if (attack == null || attack.Pawn != Pawn)
            {
                return;
            }

            ChameleonAttackShutdownService.TryDeactivateImmediately(attack);
        }

        /// <summary>
        /// 复用原版隐身改变目标可攻击性的关键刷新动作，但不经过 DLC 检查。
        /// </summary>
        private void RefreshAttackTargetCache()
        {
            if (Pawn != null && Pawn.Spawned && Pawn.Map != null)
            {
                Pawn.Map.attackTargetsCache.UpdateTarget(Pawn);
            }
        }
    }
}
