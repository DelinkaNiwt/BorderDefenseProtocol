using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Semantics;
using Verse;
using RimWorld;

namespace BDP.Core.CombatLog
{
    /// <summary>原版日志条目的中性创建口，不解释具体内容或玩家文案。</summary>
    public interface IBattleLogEntryFactory
    {
        /// <summary>创建一次原版粒度的远程开火记录。</summary>
        BattleLogEntry_RangedFire CreateRangedFire(Thing initiator, Thing target, ThingDef weapon,
            ThingDef projectile, bool burst, ISemanticContext semanticContext);

        /// <summary>创建一次实际远程交互记录，保持原版伤害关联类型。</summary>
        BattleLogEntry_RangedImpact CreateRangedImpact(Thing initiator, Thing recipient, Thing originalTarget,
            ThingDef weapon, ThingDef projectile, ThingDef cover, ISemanticContext semanticContext);

        /// <summary>创建原版近战结果条目，原版招式规则与构造参数原样保留。</summary>
        BattleLogEntry_MeleeCombat CreateMelee(RulePackDef rules, bool alwaysShow, Pawn initiator, Thing target,
            ImplementOwnerTypeDef implementType, string toolLabel, ThingDef equipment, HediffDef hediff,
            LogEntryDef entryDef, ISemanticContext semanticContext);

        /// <summary>创建原版爆炸生产者的一次实际逐目标结果。</summary>
        BattleLogEntry_ExplosionImpact CreateExplosionImpact(Thing initiator, Thing recipient, ThingDef weapon,
            ThingDef projectile, DamageDef damage, ISemanticContext semanticContext);

        /// <summary>补充已经确认的本次承伤分类；不参与伤害裁决。</summary>
        void CompleteDamageResult(LogEntry_DamageResult entry, DamageResolution resolution, bool blockedByShield);
    }
}
