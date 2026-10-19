using System;
using System.Collections.Generic;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>逐伤口泄露的准入、实际损伤换算与已发布来源查询。</summary>
    internal static class CombatBodyWoundTrionDrainUtility
    {
        /// <summary>只换算本次实际新增损伤；断口使用部位上限，过量攻击不算真实损伤。</summary>
        internal static bool TryResolveWoundDrainPerSecond(Hediff wound, float effectiveDamage, out float drainPerSecond)
        {
            drainPerSecond = 0f;
            CombatBodyWoundPolicyDef policy = CombatBodyWoundPolicy.Resolve();
            if (!IsLiveWound(wound) || !CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(wound.pawn)
                || !policy.trionDrainEnabled || policy.trionDrainPerDamagePerSecond <= 0f) return false;
            float maximum = Math.Max(0f, wound.Part.def.GetMaxHealth(wound.pawn));
            float damage = wound is Hediff_MissingPart ? maximum : Math.Min(maximum, Math.Max(0f, effectiveDamage));
            drainPerSecond = damage * policy.trionDrainPerDamagePerSecond;
            return drainPerSecond > 0f;
        }

        /// <summary>显示只读取本伤口的已发布登记；到期、移除和断肢替代后不显示。</summary>
        internal static bool TryResolvePublishedDrainPerSecond(Hediff hediff, out float drainPerSecond)
        {
            drainPerSecond = 0f;
            if (!IsLiveWound(hediff)) return false;
            IReadOnlyDictionary<TrionDrainKey, float> snapshot = TrionSurfaceAccess.ResolveReader(hediff.pawn)?.GetDrainSnapshot();
            return snapshot != null && snapshot.TryGetValue(BuildDrainKey(hediff), out drainPerSecond) && drainPerSecond > 0f;
        }

        /// <summary>原版伤口编号是来源身份，部位标签只用于资源面板显示。</summary>
        internal static TrionDrainKey BuildDrainKey(Hediff wound)
        {
            return new TrionDrainKey("CombatBody", "Wound", wound.loadID, wound.Part.Label);
        }

        /// <summary>查询部位自己的真实断口，祖先缺失由有效性检查排除。</summary>
        internal static Hediff_MissingPart FindMissingWound(Pawn pawn, BodyPartRecord part)
        {
            List<Hediff> wounds = pawn?.health?.hediffSet?.hediffs;
            if (wounds == null || part == null) return null;
            for (int index = 0; index < wounds.Count; index++)
                if (wounds[index] is Hediff_MissingPart missing && missing.Part == part) return missing;
            return null;
        }

        /// <summary>只保留原版可流血类型与部位上的真实伤口，排除被替代的旧伤与下级记录。</summary>
        internal static bool IsLiveWound(Hediff wound)
        {
            // 战斗体会把BleedRate（生理流血率）压为零，准入须读原版定义系数。
            // 这里只判断伤口类型和部位；包扎、伤龄不替代既定的独立泄露期限。
            if (wound?.pawn?.health?.hediffSet == null || wound.Part == null
                || !CombatBodyWoundPolicy.IsSupportedWound(wound)
                || wound.def?.injuryProps == null || wound.def.injuryProps.bleedRate <= 0f
                || (wound.Part.def.bleedRate <= 0f
                    && CombatBodyMaterialAccess.Resolve(wound.pawn)?.RequiresBiologicalWoundPart != false)
                || !wound.pawn.health.hediffSet.hediffs.Contains(wound)
                || HasMissingAncestor(wound.pawn, wound.Part)) return false;
            return !(wound is Hediff_Injury)
                || (wound.Severity > 0f && FindMissingWound(wound.pawn, wound.Part) == null);
        }

        /// <summary>直接检查列表，避免断肢嵌套期间读取尚未刷新的部位缓存。</summary>
        private static bool HasMissingAncestor(Pawn pawn, BodyPartRecord part)
        {
            for (BodyPartRecord ancestor = part.parent; ancestor != null; ancestor = ancestor.parent)
                if (FindMissingWound(pawn, ancestor) != null) return true;
            return false;
        }
    }
}
