using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Semantics;
using Verse;
using RimWorld;
using BDP.Core.Verbs;

namespace BDP.Core.CombatLog
{
    /// <summary>单个正式日志提供器的薄接入口；不保存历史条目。</summary>
    public static class BattleLogEntryFactoryAccess
    {
        /// <summary>语义上下文中用于冻结来源定义类型的中性键。</summary>
        public const string SourceTypeKey = "source.definition.type";
        /// <summary>语义上下文中用于冻结来源定义名的中性键。</summary>
        public const string SourceDefKey = "source.definition.name";
        /// <summary>语义上下文中用于冻结声明条目标识的中性键。</summary>
        public const string SourceEntryKey = "source.entry";
        /// <summary>语义上下文中用于冻结来源变体标识的中性键。</summary>
        public const string SourceVariantKey = "source.variant";
        /// <summary>动态定义的显示语言键；避免依赖之后可能消失的芯片实例。</summary>
        public const string SourceLabelKey = "source.label.key";
        /// <summary>动态实例按原顺序提供的来源定义键，供Content还原当次条目的原始身份。</summary>
        public const string SourceOriginKeys = "source.origin.keys";
        /// <summary>当前唯一正式提供器。</summary>
        private static IBattleLogEntryFactory provider;

        /// <summary>启动时注册正式提供器，不建立多级优先级设施。</summary>
        public static void Register(IBattleLogEntryFactory value)
        {
            provider = value;
        }

        /// <summary>创建开火条目，未注册时保持原版创建方式。</summary>
        public static BattleLogEntry_RangedFire CreateRangedFire(Thing initiator, Thing target, ThingDef weapon,
            ThingDef projectile, bool burst, ISemanticContext semanticContext)
        {
            return provider != null
                ? provider.CreateRangedFire(initiator, target, weapon, projectile, burst, semanticContext)
                : new BattleLogEntry_RangedFire(initiator, target, weapon, projectile, burst);
        }

        /// <summary>创建命中条目，保留原版实际目标与原意图目标。</summary>
        public static BattleLogEntry_RangedImpact CreateRangedImpact(Thing initiator, Thing recipient,
            Thing originalTarget, ThingDef weapon, ThingDef projectile, ThingDef cover, ISemanticContext semanticContext)
        {
            return provider != null
                ? provider.CreateRangedImpact(initiator, recipient, originalTarget, weapon, projectile, cover, semanticContext)
                : new BattleLogEntry_RangedImpact(initiator, recipient, originalTarget, weapon, projectile, cover);
        }

        /// <summary>构造点准入仅限BDP近战；其它动作保持原版构造参数和类型。</summary>
        public static BattleLogEntry_MeleeCombat CreateMelee(RulePackDef rules, bool alwaysShow, Pawn initiator,
            Thing target, ImplementOwnerTypeDef implementType, string toolLabel, ThingDef equipment,
            HediffDef hediff, LogEntryDef entryDef, Verb_MeleeAttack verb)
        {
            BdpVerb_MeleeAttackDamage bdpVerb = verb as BdpVerb_MeleeAttackDamage;
            return bdpVerb != null && provider != null
                ? provider.CreateMelee(rules, alwaysShow, initiator, target, implementType, toolLabel,
                    equipment, hediff, entryDef, bdpVerb.SemanticContext)
                : new BattleLogEntry_MeleeCombat(rules, alwaysShow, initiator, target, implementType,
                    toolLabel, equipment, hediff, entryDef);
        }

        /// <summary>转交本次承伤结果，日志缺席不影响玩法。</summary>
        public static void CompleteDamageResult(LogEntry_DamageResult entry, DamageResolution resolution,
            bool blockedByShield = false)
        {
            if (entry != null)
            {
                provider?.CompleteDamageResult(entry, resolution, blockedByShield);
            }
        }

        /// <summary>从已有BDP范围交互上下文创建逐目标条目。</summary>
        public static BattleLogEntry_ExplosionImpact CreateExplosionImpact(Thing initiator, Thing recipient,
            ThingDef weapon, ThingDef projectile, DamageDef damage, ISemanticContext semanticContext)
        {
            return provider != null
                ? provider.CreateExplosionImpact(initiator, recipient, weapon, projectile, damage, semanticContext)
                : new BattleLogEntry_ExplosionImpact(initiator, recipient, weapon, projectile, damage);
        }

        /// <summary>原版爆炸构造点仅允许存在BDP交互上下文的爆炸进入扩展。</summary>
        public static BattleLogEntry_ExplosionImpact CreateExplosionImpactFromVanilla(Thing initiator,
            Thing recipient, ThingDef weapon, ThingDef projectile, DamageDef damage, Explosion explosion)
        {
            ExplosionImpactDispatchContext context = BdpDamageSemanticBridge.GetExplosionImpactContext(explosion);
            return context != null ? CreateExplosionImpact(initiator, recipient, weapon, projectile, damage, context.SemanticContext)
                : new BattleLogEntry_ExplosionImpact(initiator, recipient, weapon, projectile, damage);
        }
    }
}
