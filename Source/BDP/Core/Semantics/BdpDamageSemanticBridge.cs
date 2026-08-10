using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace BDP.Core.Semantics
{
    /// <summary>
    /// 第一阶段攻击语义搬运工具。
    /// 它只负责把语义从一个宿主交给另一个宿主，不负责解释业务规则。
    /// </summary>
    public static class BdpDamageSemanticBridge
    {
        /// <summary>
        /// 爆炸实例没有可直接扩展的正式字段。
        /// 这里用弱表临时挂语义，爆炸对象销毁后记录也会一起释放。
        /// </summary>
        private static readonly ConditionalWeakTable<Explosion, SemanticContextHolder> explosionContexts = new ConditionalWeakTable<Explosion, SemanticContextHolder>();

        /// <summary>
        /// 从实现了语义承载接口的对象上读取当前语义。
        /// </summary>
        public static ISemanticContext GetContext(object carrier)
        {
            IBdpSemanticCarrier semanticCarrier = carrier as IBdpSemanticCarrier;
            return semanticCarrier != null ? semanticCarrier.SemanticContext : null;
        }

        /// <summary>
        /// 把语义直接写到实现了语义承载接口的宿主上。
        /// 目标不支持时，就什么都不做。
        /// </summary>
        public static void AssignContext(object carrier, ISemanticContext semanticContext)
        {
            IBdpSemanticCarrier semanticCarrier = carrier as IBdpSemanticCarrier;
            if (semanticCarrier == null)
            {
                return;
            }

            semanticCarrier.SemanticContext = semanticContext;
        }

        /// <summary>
        /// 从一个宿主把语义转交给另一个宿主。
        /// 第一阶段先服务 Verb 和 Projectile 这些直接攻击宿主。
        /// </summary>
        public static void TransferContext(object sourceCarrier, object targetCarrier)
        {
            AssignContext(targetCarrier, GetContext(sourceCarrier));
        }

        /// <summary>
        /// 把当前攻击语义挂到爆炸实例上。
        /// 这是爆炸链唯一需要跨边界保留语义的地方。
        /// </summary>
        public static void AssignExplosionContext(Explosion explosion, ISemanticContext semanticContext)
        {
            if (explosion == null || semanticContext == null)
            {
                return;
            }

            explosionContexts.Remove(explosion);
            explosionContexts.Add(explosion, new SemanticContextHolder { Context = semanticContext });
        }

        /// <summary>
        /// 读取某个爆炸实例上暂存的攻击语义。
        /// </summary>
        public static ISemanticContext GetExplosionContext(Explosion explosion)
        {
            if (explosion == null)
            {
                return null;
            }

            return explosionContexts.TryGetValue(explosion, out SemanticContextHolder holder) ? holder.Context : null;
        }

        /// <summary>
        /// 判断一份攻击语义是否可以拿来作为伤口来源名。
        /// 第一阶段规则很简单：只要有非空显示名就算有效。
        /// </summary>
        public static bool TryGetDisplayLabel(ISemanticContext semanticContext, out string displayLabel)
        {
            displayLabel = semanticContext != null ? semanticContext.DisplayLabel : null;
            return !string.IsNullOrEmpty(displayLabel);
        }

        /// <summary>
        /// 把一份攻击语义写进伤口 Hediff 的来源字段。
        /// 这里只整理界面括号里显示什么，不碰伤害数值和判定逻辑。
        /// </summary>
        public static bool TryApplyInjurySource(
            Hediff_Injury injury,
            ISemanticContext semanticContext,
            ThingDef fallbackSourceDef = null,
            string fallbackToolLabel = null,
            BodyPartGroupDef fallbackBodyPartGroup = null)
        {
            if (injury == null || !TryGetDisplayLabel(semanticContext, out string displayLabel))
            {
                return false;
            }

            injury.sourceLabel = displayLabel;
            if (!string.IsNullOrEmpty(fallbackToolLabel))
            {
                injury.sourceToolLabel = fallbackToolLabel;
                injury.sourceBodyPartGroup = null;
            }
            else
            {
                injury.sourceToolLabel = null;
                injury.sourceBodyPartGroup = fallbackBodyPartGroup;
            }

            injury.sourceDef = fallbackSourceDef ?? injury.sourceDef ?? semanticContext.Instigator?.def ?? ThingDefOf.Human;
            return true;
        }

        /// <summary>
        /// 只是给弱表包一层引用壳。
        /// </summary>
        private sealed class SemanticContextHolder
        {
            /// <summary>
            /// 当前暂存的攻击语义。
            /// </summary>
            public ISemanticContext Context;
        }
    }
}
