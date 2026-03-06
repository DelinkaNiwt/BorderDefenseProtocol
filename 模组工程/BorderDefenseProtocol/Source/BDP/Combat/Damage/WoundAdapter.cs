using System.Collections.Generic;
using System.Linq;
using Verse;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 伤害类型映射工具。
    /// 负责：
    /// 1. 维护伤害类型 → HediffDef 映射表
    /// 2. 添加或合并伤口（同部位同类型伤口合并）
    /// </summary>
    public static class WoundAdapter
    {
        /// <summary>
        /// 伤害类型 → 伤口HediffDef 映射表。
        /// </summary>
        private static readonly Dictionary<string, HediffDef> damageToWoundMap = new Dictionary<string, HediffDef>();

        /// <summary>
        /// 静态构造函数：初始化映射表。
        /// </summary>
        static WoundAdapter()
        {
            // 枪击类型
            damageToWoundMap["Bullet"] = BDP_DefOf.BDP_CombatWound_Bullet;
            damageToWoundMap["Arrow"] = BDP_DefOf.BDP_CombatWound_Bullet;

            // 切口类型
            damageToWoundMap["Cut"] = BDP_DefOf.BDP_CombatWound_Cut;

            // 淤青类型
            damageToWoundMap["Blunt"] = BDP_DefOf.BDP_CombatWound_Blunt;
            damageToWoundMap["Bruise"] = BDP_DefOf.BDP_CombatWound_Blunt;

            // 灼伤类型
            damageToWoundMap["Burn"] = BDP_DefOf.BDP_CombatWound_Burn;
            damageToWoundMap["Flame"] = BDP_DefOf.BDP_CombatWound_Burn;

            // 刺伤类型
            damageToWoundMap["Stab"] = BDP_DefOf.BDP_CombatWound_Stab;

            // 裂纹类型
            damageToWoundMap["Crack"] = BDP_DefOf.BDP_CombatWound_Crack;

            // 抓伤类型
            damageToWoundMap["Scratch"] = BDP_DefOf.BDP_CombatWound_Scratch;

            // 咬伤类型
            damageToWoundMap["Bite"] = BDP_DefOf.BDP_CombatWound_Bite;

            // 撕裂类型
            damageToWoundMap["Shredded"] = BDP_DefOf.BDP_CombatWound_Shredded;

            // 挤压类型（映射到淤青）
            damageToWoundMap["Crush"] = BDP_DefOf.BDP_CombatWound_Blunt;
        }

        /// <summary>
        /// 获取对应的伤口HediffDef。
        /// </summary>
        /// <param name="damageDef">伤害类型</param>
        /// <returns>对应的伤口HediffDef，如果未找到则返回枪伤作为默认值</returns>
        public static HediffDef GetCombatWoundDef(DamageDef damageDef)
        {
            if (damageDef == null)
            {
                Log.Warning("[BDP] WoundAdapter.GetCombatWoundDef: damageDef为null，使用默认枪伤");
                return BDP_DefOf.BDP_CombatWound_Bullet;
            }

            // 尝试从映射表获取
            if (damageToWoundMap.TryGetValue(damageDef.defName, out HediffDef hediffDef))
            {
                return hediffDef;
            }

            // 未找到，使用默认值（枪伤）
            Log.Message($"[BDP] WoundAdapter: 未找到伤害类型 {damageDef.defName} 的映射，使用默认枪伤");
            return BDP_DefOf.BDP_CombatWound_Bullet;
        }

        /// <summary>
        /// 添加或合并伤口。
        /// 如果同部位已存在同类型伤口，则合并（severity累加，hitCount+1）。
        /// 否则创建新伤口。
        /// </summary>
        /// <param name="pawn">受伤Pawn</param>
        /// <param name="part">受伤部位</param>
        /// <param name="damageDef">伤害类型</param>
        /// <param name="severity">伤害严重度</param>
        /// <param name="dinfo">伤害信息（可选，用于记录武器信息）</param>
        public static void AddOrMergeWound(Pawn pawn, BodyPartRecord part, DamageDef damageDef, float severity, DamageInfo? dinfo = null)
        {
            if (pawn == null || part == null)
            {
                Log.Warning("[BDP] WoundAdapter.AddOrMergeWound: pawn或part为null");
                return;
            }

            // 获取对应的伤口HediffDef
            HediffDef hediffDef = GetCombatWoundDef(damageDef);

            // 查找同部位同类型的现有伤口
            var existingWound = pawn.health.hediffSet.hediffs
                .OfType<Hediff_CombatWound>()
                .FirstOrDefault(h => h.Part == part && h.def == hediffDef);

            if (existingWound != null)
            {
                // 合并：severity累加，hitCount+1
                float oldSeverity = existingWound.Severity;
                existingWound.Severity += severity;

                var comp = existingWound.TryGetComp<HediffComp_CombatWound>();
                if (comp != null)
                {
                    comp.hitCount++;
                    Log.Message($"[BDP] 伤口合并: {hediffDef.label} x{comp.hitCount}, 部位={part.Label}, severity={oldSeverity:F1}→{existingWound.Severity:F1}");
                }
            }
            else
            {
                // 新建伤口
                var hediff = (Hediff_CombatWound)HediffMaker.MakeHediff(hediffDef, pawn, part);
                hediff.Severity = severity;

                // 使用原版API设置武器信息
                if (dinfo.HasValue)
                {
                    hediff.sourceDef = dinfo.Value.Weapon;
                    hediff.sourceLabel = dinfo.Value.Weapon?.label ?? "";
                    hediff.sourceBodyPartGroup = dinfo.Value.WeaponBodyPartGroup;
                    hediff.sourceHediffDef = dinfo.Value.WeaponLinkedHediff;
                    hediff.sourceToolLabel = dinfo.Value.Tool?.labelNoLocation ?? dinfo.Value.Tool?.label;
                }

                pawn.health.AddHediff(hediff);

                Log.Message($"[BDP] 新建伤口: {hediffDef.label}, 部位={part.Label}, severity={severity:F1}");
            }
        }
    }
}
