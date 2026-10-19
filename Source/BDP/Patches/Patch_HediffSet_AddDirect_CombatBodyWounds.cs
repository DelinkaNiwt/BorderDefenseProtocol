using System;
using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>在原版添加、合并和断肢完成后，将真实新增损伤绑定到最终伤口。</summary>
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect))]
    public static class Patch_HediffSet_AddDirect_CombatBodyWounds
    {
        /// <summary>仅保存本次调用的受伤前事实，不持有长期状态。</summary>
        public sealed class AdditionState
        {
            /// <summary>原版添加前的部位生命值。</summary>
            internal float partHealth;
            /// <summary>可能被原版合并的既有伤口及其原严重度。</summary>
            internal readonly Dictionary<Hediff, float> severities = new Dictionary<Hediff, float>();
        }

        /// <summary>非 BDP 不介入；原版稍后才给伤口绑定人物，因此从健康集合读取宿主。</summary>
        public static void Prefix(Hediff hediff, Pawn ___pawn, out AdditionState __state)
        {
            __state = null;
            if (hediff?.Part == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff)
                || !CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(___pawn)) return;
            __state = new AdditionState { partHealth = ___pawn.health.hediffSet.GetPartHealth(hediff.Part) };
            if (!(hediff is Hediff_Injury)) return;
            foreach (Hediff old in ___pawn.health.hediffSet.hediffs)
                if (old is Hediff_Injury && old.Part == hediff.Part && old.def == hediff.def)
                    __state.severities.Add(old, old.Severity);
        }

        /// <summary>零实际损伤不登记、不续期；绝不把整部位总损伤重复记到每条伤口。</summary>
        public static void Postfix(Hediff hediff, Pawn ___pawn, AdditionState __state)
        {
            if (__state == null || hediff?.Part == null
                || !CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(___pawn)) return;
            Pawn pawn = ___pawn;
            float damage = Math.Max(0f, __state.partHealth - pawn.health.hediffSet.GetPartHealth(hediff.Part));
            if (damage <= 0f) return;
            CombatBodyWoundRuntime wounds = CombatBodyMaterialAccess.Wounds(pawn);
            if (wounds == null) return;

            // 断肢会移除传入伤口；只把实际断口交给登记层，其全额速率由部位上限换算。
            Hediff_MissingPart stump = CombatBodyWoundTrionDrainUtility.FindMissingWound(pawn, hediff.Part);
            if (stump != null)
            {
                wounds.NotifyWoundAddedOrChanged(pawn, stump, damage);
                return;
            }
            if (CombatBodyWoundTrionDrainUtility.IsLiveWound(hediff))
            {
                wounds.NotifyWoundAddedOrChanged(pawn, hediff, damage);
                return;
            }

            // 原版合并后临时对象不在列表中。比较最终严重度找真实接收者，不复写合并规则。
            float totalIncrease = 0f;
            foreach (KeyValuePair<Hediff, float> old in __state.severities)
                if (CombatBodyWoundTrionDrainUtility.IsLiveWound(old.Key))
                    totalIncrease += Math.Max(0f, old.Key.Severity - old.Value);
            if (totalIncrease <= 0f) return;
            foreach (KeyValuePair<Hediff, float> old in __state.severities)
            {
                if (!CombatBodyWoundTrionDrainUtility.IsLiveWound(old.Key)) continue;
                float increase = Math.Max(0f, old.Key.Severity - old.Value);
                if (increase > 0f) wounds.NotifyWoundAddedOrChanged(pawn, old.Key, damage * increase / totalIncrease);
            }
        }
    }
}
