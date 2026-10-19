using BDP.Core.CombatLog;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Semantics;
using Verse;
using RimWorld;

namespace BDP.Content.PlayerLog
{
    /// <summary>正式内容的原版日志条目工厂，不保存额外历史。</summary>
    internal sealed class PlayerBattleLogEntryFactory : IBattleLogEntryFactory
    {
        /// <summary>创建一次BDP开火条目。</summary>
        public BattleLogEntry_RangedFire CreateRangedFire(Thing initiator, Thing target, ThingDef weapon,
            ThingDef projectile, bool burst, ISemanticContext semanticContext)
        {
            return new BattleLogEntry_BdpRangedFire(initiator, target, weapon, projectile, burst, semanticContext);
        }
        /// <summary>创建可被原版伤害结果关联的BDP命中条目。</summary>
        public BattleLogEntry_RangedImpact CreateRangedImpact(Thing initiator, Thing recipient, Thing originalTarget,
            ThingDef weapon, ThingDef projectile, ThingDef cover, ISemanticContext semanticContext)
        {
            return new BattleLogEntry_BdpRangedImpact(initiator, recipient, originalTarget, weapon, projectile, cover, semanticContext);
        }
        /// <summary>沿用原版近战命中、闪避和招式选择。</summary>
        public BattleLogEntry_MeleeCombat CreateMelee(RulePackDef rules, bool alwaysShow, Pawn initiator, Thing target,
            ImplementOwnerTypeDef implementType, string toolLabel, ThingDef equipment, HediffDef hediff,
            LogEntryDef entryDef, ISemanticContext semanticContext)
        {
            return new BattleLogEntry_BdpMeleeCombat(rules, alwaysShow, initiator, target, implementType,
                toolLabel, equipment, hediff, entryDef, semanticContext);
        }

        /// <summary>使用原版爆炸条目的逐目标关联与伤害结果。</summary>
        public BattleLogEntry_ExplosionImpact CreateExplosionImpact(Thing initiator, Thing recipient, ThingDef weapon,
            ThingDef projectile, DamageDef damage, ISemanticContext semanticContext)
        {
            return new BattleLogEntry_BdpExplosionImpact(initiator, recipient, weapon, projectile, damage, semanticContext);
        }

        /// <summary>只补已确认分类，不读取当前人物状态推断结果。</summary>
        public void CompleteDamageResult(LogEntry_DamageResult entry, DamageResolution resolution, bool blockedByShield)
        {
            IPlayerBattleLogEntry bdpEntry = entry as IPlayerBattleLogEntry;
            if (bdpEntry == null) return;
            bdpEntry.Presentation.Complete(resolution, blockedByShield);
            bdpEntry.InvalidatePresentation();
        }

        /// <summary>业务完成点补一次必要直接结果，保持同一原版条目。</summary>
        internal static void AppendConfirmedResult(LogEntry_DamageResult entry, RulePackDef rules,
            HediffDef effect, BodyPartRecord part = null, bool important = false)
        {
            IPlayerBattleLogEntry bdpEntry = entry as IPlayerBattleLogEntry;
            if (bdpEntry == null || rules == null) return;
            bdpEntry.Presentation.AppendConfirmedResult(rules, effect, part, important);
            bdpEntry.InvalidatePresentation();
        }
    }
}
