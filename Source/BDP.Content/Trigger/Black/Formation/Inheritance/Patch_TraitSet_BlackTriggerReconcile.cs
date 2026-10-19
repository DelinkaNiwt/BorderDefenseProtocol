using HarmonyLib;
using RimWorld;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>外部移除冲突特性后，仅为当前装备黑触发器重新协调继承。</summary>
    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.RemoveTrait))]
    public static class Patch_TraitSet_BlackTriggerReconcile
    {
        /// <summary>忽略 BDP 自己的清理，避免递归。</summary>
        public static void Postfix(Trait trait)
        {
            if (trait == null || trait is Trait_BlackTriggerInherited) return;
            BlackTriggerInheritedTraitService.Reconcile(trait.pawn);
        }
    }
}
