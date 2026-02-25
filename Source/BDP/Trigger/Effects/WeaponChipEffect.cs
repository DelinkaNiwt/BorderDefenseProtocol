using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 武器类芯片效果——通过CompTriggerBody的按侧Verb存储向触发体注入Verb/Tool配置。
    ///
    /// v2.0变更（T24）：
    ///   - Activate改为调用SetSideVerbs（按侧存储，支持双武器）
    ///   - Deactivate改为调用ClearSideVerbs
    ///   - 侧别通过CompTriggerBody.ActivatingSide临时上下文获取
    ///
    /// v5.0变更：RebuildVerbs搬迁至CompTriggerBody，本类只负责设置/清除Verb数据。
    /// </summary>
    public class WeaponChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            // 通过ActivatingSide获取当前操作的侧别
            var side = triggerComp.ActivatingSide ?? SlotSide.LeftHand;

            // 从ActivatingSlot读取WeaponChipConfig（T36：数据存在DefModExtension中）
            var cfg = GetConfig(triggerComp);

            // B2修复：近战芯片只有tools没有verbProperties时，从tools合成melee VerbProperties标记。
            // 原因：ComposeVerbs需要VerbProperties来识别此侧为近战，触发ComposeDualMelee路径。
            // 若verbProperties为null，ComposeVerbs回退到触发体默认Verbs，DualMelee永远不会被创建。
            var verbs = cfg?.verbProperties;
            if (verbs == null && cfg != null && cfg.tools != null && cfg.tools.Count > 0)
                verbs = SynthesizeMeleeVerbProps(cfg);

            triggerComp.SetSideVerbs(side, verbs, cfg?.tools);

            // v5.0：RebuildVerbs已搬迁至CompTriggerBody
            triggerComp.RebuildVerbs(pawn);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            var side = triggerComp.ActivatingSide ?? SlotSide.LeftHand;
            triggerComp.ClearSideVerbs(side);

            // v5.0：RebuildVerbs已搬迁至CompTriggerBody
            triggerComp.RebuildVerbs(pawn);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        /// <summary>
        /// B2修复：从WeaponChipConfig合成最小化的melee VerbProperties标记。
        /// 目的：让ComposeVerbs识别此侧为近战（IsMeleeAttack=true需要meleeDamageDef非null），
        /// 触发ComposeDualMelee路径，创建Verb_BDPMelee。
        /// DamageDef通过Tool.capacities → ManeuverDef.verb.meleeDamageDef查找。
        /// v6.0变更：接收完整WeaponChipConfig，设置burstShotCount和ticksBetweenBurstShots。
        /// </summary>
        private static List<VerbProperties> SynthesizeMeleeVerbProps(WeaponChipConfig cfg)
        {
            var result = new List<VerbProperties>();
            var tool = cfg.tools[0];

            // 从Tool的capacities查找对应的DamageDef（使用缓存避免线性搜索）
            DamageDef damageDef = null;
            if (tool.capacities != null && tool.capacities.Count > 0)
            {
                var maneuver = Verb_BDPMelee.GetManeuverForCapacity(tool.capacities[0]);
                damageDef = maneuver?.verb?.meleeDamageDef;
            }
            damageDef = damageDef ?? DamageDefOf.Blunt; // 兜底

            // 必须用Verb_BDPMelee而非Verb_MeleeAttackDamage！
            // 原因：标准Verb_MeleeAttackDamage.DamageInfosToApply用EquipmentSource.def（触发体）作为weapon，
            // 而Verb_BDPMelee.ApplyMeleeDamageToTarget用currentChipDef（芯片）作为weapon。
            // 若用标准类，hediff永远显示"触发体"。
            result.Add(new VerbProperties
            {
                verbClass = typeof(Verb_BDPMelee),
                meleeDamageDef = damageDef,
                meleeDamageBaseAmount = (int)tool.power,
                defaultCooldownTime = tool.cooldownTime,
                // v6.0：从WeaponChipConfig读取burst参数，供DualVerbCompositor和引擎burst机制使用
                burstShotCount = cfg.meleeBurstCount,
                ticksBetweenBurstShots = cfg.meleeBurstInterval,
            });
            return result;
        }

        /// <summary>
        /// 从CompTriggerBody读取WeaponChipConfig（委托给通用GetChipExtension）。
        /// </summary>
        private static WeaponChipConfig GetConfig(CompTriggerBody triggerComp)
        {
            return triggerComp?.GetChipExtension<WeaponChipConfig>();
        }
    }
}