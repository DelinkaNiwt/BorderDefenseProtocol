using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>随黑触发器装备状态协调真实临时特性，不使用 Hediff 或人物 Comp。</summary>
    public static class BlackTriggerInheritedTraitService
    {
        /// <summary>装备后应用印记中由玩家选择的特性。</summary>
        public static void Apply(Pawn pawn, Thing source, BlackTriggerTraitOptionSnapshot option)
        {
            if (pawn?.story?.traits == null || source == null || option?.TraitDef == null) return;
            RemoveFromSource(pawn, source);
            List<Trait> existing = pawn.story.traits.allTraits;
            if (existing.Any(value => value != null && value.def == option.TraitDef)) return;
            Trait probe = new Trait(option.TraitDef, option.Degree);
            if (existing.Any(value => value != null && !(value is Trait_BlackTriggerInherited)
                && (option.TraitDef.ConflictsWith(value) || value.def.ConflictsWith(probe)))) return;
            Trait_BlackTriggerInherited inherited = new Trait_BlackTriggerInherited(option.TraitDef, option.Degree, source.ThingID);
            inherited.PreexistingAbilities = (option.TraitDef.DataAtDegree(option.Degree)?.abilities ?? new List<AbilityDef>())
                .Where(value => pawn.abilities?.GetAbility(value, includeTemporary: true) != null)
                .Distinct()
                .ToList();
            pawn.story.traits.GainTrait(inherited, suppressConflicts: false);
            pawn.story.traits.RecalculateSuppression();
        }

        /// <summary>只移除指定黑触发器实际添加的临时特性。</summary>
        public static void RemoveFromSource(Pawn pawn, Thing source)
        {
            if (source == null) return;
            RemoveFromSourceId(pawn, source.ThingID);
        }

        /// <summary>按来源 ThingID 移除 BDP 临时特性。</summary>
        public static void RemoveFromSourceId(Pawn pawn, string sourceThingId)
        {
            if (pawn?.story?.traits?.allTraits == null || sourceThingId.NullOrEmpty()) return;
            bool removed = false;
            foreach (Trait_BlackTriggerInherited trait in pawn.story.traits.allTraits
                .OfType<Trait_BlackTriggerInherited>()
                .Where(value => value.SourceThingId == sourceThingId)
                .ToList())
            {
                removed |= RemoveInheritedTrait(pawn, trait);
            }
            if (removed) pawn.story.traits.RecalculateSuppression();
        }

        /// <summary>人物死亡前移除全部 BDP 临时特性，避免进入尸体或复活数据。</summary>
        public static void RemoveAllTemporary(Pawn pawn)
        {
            if (pawn?.story?.traits?.allTraits == null) return;
            bool removed = false;
            foreach (Trait_BlackTriggerInherited trait in pawn.story.traits.allTraits.OfType<Trait_BlackTriggerInherited>().ToList())
            {
                removed |= RemoveInheritedTrait(pawn, trait);
            }
            if (removed) pawn.story.traits.RecalculateSuppression();
        }

        /// <summary>当前装备或其它特性变化后，恢复应存在并清理不应存在的临时特性。</summary>
        public static void Reconcile(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return;
            List<Thing> equippedBlackTriggers = pawn.equipment?.AllEquipmentListForReading?
                .Where(value => value?.TryGetComp<CompBlackTriggerImprint>() != null)
                .Cast<Thing>()
                .ToList() ?? new List<Thing>();
            HashSet<string> equippedIds = new HashSet<string>(equippedBlackTriggers.Select(value => value.ThingID));
            foreach (Trait_BlackTriggerInherited orphan in pawn.story.traits.allTraits
                .OfType<Trait_BlackTriggerInherited>()
                .Where(value => !equippedIds.Contains(value.SourceThingId))
                .ToList())
            {
                RemoveInheritedTrait(pawn, orphan);
            }
            for (int index = 0; index < equippedBlackTriggers.Count; index++)
            {
                Thing thing = equippedBlackTriggers[index];
                Apply(pawn, thing, thing.TryGetComp<CompBlackTriggerImprint>()?.InheritedTrait);
            }
        }

        /// <summary>读档后审计全部地图与世界人物。</summary>
        public static void AuditAllPawns()
        {
            HashSet<Pawn> pawns = new HashSet<Pawn>(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead);
            pawns.UnionWith(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead);
            foreach (Pawn pawn in pawns) Reconcile(pawn);
        }

        /// <summary>实际移除临时特性后恢复既有能力；返回是否确实移除了本次实例。</summary>
        private static bool RemoveInheritedTrait(Pawn pawn, Trait_BlackTriggerInherited trait)
        {
            if (pawn?.story?.traits?.allTraits == null || trait == null
                || !pawn.story.traits.allTraits.Contains(trait)) return false;
            List<AbilityDef> restore = trait.PreexistingAbilities?.ToList() ?? new List<AbilityDef>();
            pawn.story.traits.RemoveTrait(trait, unsuppressConflicts: true);
            if (pawn.story.traits.allTraits.Contains(trait)) return false;
            foreach (AbilityDef ability in restore)
            {
                if (ability != null && pawn.abilities?.GetAbility(ability, includeTemporary: true) == null)
                {
                    pawn.abilities.GainAbility(ability);
                }
            }
            return true;
        }
    }
}
