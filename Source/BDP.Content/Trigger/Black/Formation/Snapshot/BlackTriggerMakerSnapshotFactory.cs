using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在人物死亡前一次性冻结技能、记录、关系和特性资料。</summary>
    public static class BlackTriggerMakerSnapshotFactory
    {
        /// <summary>创建形成系统的唯一来源快照。</summary>
        public static BlackTriggerMakerSnapshot Capture(Pawn pawn, BlackTriggerFormationSource source, DamageInfo? dinfo, Hediff culprit)
        {
            if (pawn == null) return null;
            BlackTriggerMakerSnapshot snapshot = new BlackTriggerMakerSnapshot
            {
                Maker = pawn,
                MakerLoadId = pawn.GetUniqueLoadID(),
                MakerLabel = pawn.LabelNoCount,
                MakerWasColonistPlayerControlled = pawn.IsColonistPlayerControlled,
                Source = source,
                DamageDefName = dinfo.HasValue ? dinfo.Value.Def?.defName : null,
                CulpritHediffDefName = culprit?.def?.defName,
                MapLabel = pawn.MapHeld?.Parent?.LabelCap,
                StableSeed = StableSeed(pawn.GetUniqueLoadID(), Find.TickManager?.TicksGame ?? 0)
            };
            FillSkills(snapshot);
            FillRecords(snapshot);
            FillSourceTraits(snapshot);
            FillSourceAbilities(snapshot);
            FillHeirs(snapshot);
            FillTraitOptions(snapshot);
            snapshot.Facts = snapshot.Facts
                .OrderBy(value => value.Kind)
                .ThenBy(value => value.StableKey, StringComparer.Ordinal)
                .ToList();
            return snapshot;
        }

        /// <summary>按等级、当前经验和稳定名称选出最高三项有效技能。</summary>
        private static void FillSkills(BlackTriggerMakerSnapshot snapshot)
        {
            List<SkillRecord> skills = snapshot.Maker.skills?.skills;
            if (skills == null) return;
            foreach (SkillRecord skill in skills
                .Where(value => value != null && value.Level > 0 && !value.TotallyDisabled)
                .OrderByDescending(value => value.Level)
                .ThenByDescending(value => value.xpSinceLastLevel)
                .ThenBy(value => value.def?.defName ?? string.Empty, StringComparer.Ordinal)
                .Take(3))
            {
                snapshot.Facts.Add(new BlackTriggerFactSnapshot
                {
                    Kind = BlackTriggerFactKind.Skill,
                    DefName = skill.def.defName,
                    Label = skill.def.LabelCap,
                    Value = skill.Level,
                    Passion = skill.passion,
                    Rank = 0,
                    GroupKey = "BDP_BlackTrigger_FactGroup_Skill"
                });
            }
        }

        /// <summary>独立计算每项有效记录在当前殖民者群体中的前三名。</summary>
        private static void FillRecords(BlackTriggerMakerSnapshot snapshot)
        {
            List<Pawn> colonists = ResolvePlayerColonists();
            foreach (RecordDef record in BlackTriggerRecordCatalog.ResolveAvailable())
            {
                float value = snapshot.Maker.records?.GetValue(record) ?? 0f;
                if (value <= 0f) continue;
                int rank = 1 + colonists
                    .Where(pawn => pawn?.records != null)
                    .Select(pawn => pawn.records.GetValue(record))
                    .Where(current => current > value)
                    .Distinct()
                    .Count();
                if (rank > 3) continue;
                snapshot.Facts.Add(new BlackTriggerFactSnapshot
                {
                    Kind = BlackTriggerFactKind.Record,
                    DefName = record.defName,
                    Label = record.LabelCap,
                    Value = value,
                    Rank = rank,
                    GroupKey = BlackTriggerRecordCatalog.ResolveGroup(record)
                });
            }
        }

        /// <summary>收集跨地图、商队和世界旅行中的存活玩家派系殖民者传承人候选。</summary>
        private static void FillHeirs(BlackTriggerMakerSnapshot snapshot)
        {
            Pawn maker = snapshot.Maker;
            if (maker.relations == null) return;
            foreach (Pawn other in ResolvePlayerColonists())
            {
                if (other == maker || other.Dead || other.RaceProps?.Humanlike != true) continue;
                DirectPawnRelation familyRelation = maker.relations.DirectRelations?
                    .FirstOrDefault(value => value?.otherPawn == other &&
                        (value.def == PawnRelationDefOf.Parent || value.def == PawnRelationDefOf.Child || value.def == PawnRelationDefOf.Spouse));
                float opinion = maker.relations.OpinionOf(other);
                bool family = familyRelation != null;
                bool closeFriend = opinion >= 100f;
                if (!(family && opinion >= 0f) && !closeFriend) continue;
                snapshot.HeirCandidates.Add(new BlackTriggerHeirCandidateSnapshot
                {
                    Pawn = other,
                    LoadId = other.GetUniqueLoadID(),
                    Label = other.LabelNoCount,
                    RelationLabel = familyRelation?.def?.label ?? "BDP_BlackTrigger_Relation_CloseFriend".Translate().ToString(),
                    Opinion = opinion,
                    LocationLabel = other.MapHeld?.Parent?.LabelCap
                        ?? "BDP_BlackTrigger_Formation_HeirWorldLocation".Translate().ToString()
                });
            }
            snapshot.HeirCandidates = snapshot.HeirCandidates
                .GroupBy(value => value.LoadId)
                .Select(group => group.First())
                .OrderBy(value => value.Label ?? string.Empty, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>从人物实际拥有的自身特性中冻结完整来源列表。</summary>
        private static void FillSourceTraits(BlackTriggerMakerSnapshot snapshot)
        {
            List<Trait> traits = snapshot.Maker.story?.traits?.allTraits;
            if (traits == null) return;
            snapshot.SourceTraits = traits
                .Where(value => value?.def != null && !(value is Trait_BlackTriggerInherited))
                .GroupBy(value => value.def.defName + ":" + value.Degree, StringComparer.Ordinal)
                .Select(group => group.First())
                .Select(value => new BlackTriggerTraitSnapshot
                {
                    TraitDef = value.def,
                    Degree = value.Degree,
                    Label = value.LabelCap.ToString(),
                    WasSuppressed = value.Suppressed
                })
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>从人物当前 AbilityTracker 冻结实际拥有的能力。</summary>
        private static void FillSourceAbilities(BlackTriggerMakerSnapshot snapshot)
        {
            List<Ability> abilities = snapshot.Maker.abilities?.AllAbilitiesForReading;
            if (abilities == null) return;
            snapshot.SourceAbilities = abilities
                .Where(value => value?.def != null && !value.def.defName.NullOrEmpty())
                .GroupBy(value => value.def.defName + "@" + (value.def.modContentPack?.PackageId ?? string.Empty), StringComparer.Ordinal)
                .Select(group => group.First())
                .Select(value => new BlackTriggerAbilitySnapshot
                {
                    AbilityDefName = value.def.defName,
                    SourcePackageId = value.def.modContentPack?.PackageId,
                    Label = value.def.LabelCap
                })
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>从完整来源特性中稳定抽出最多三项继承选项。</summary>
        private static void FillTraitOptions(BlackTriggerMakerSnapshot snapshot)
        {
            snapshot.TraitOptions = (snapshot.SourceTraits ?? new List<BlackTriggerTraitSnapshot>())
                .OrderBy(value => BlackTriggerStableRandom.Hash(snapshot.StableSeed, value.StableKey))
                .ThenBy(value => value.TraitDef?.defName ?? string.Empty, StringComparer.Ordinal)
                .Take(3)
                .Select(value => new BlackTriggerTraitOptionSnapshot
                {
                    TraitDef = value.TraitDef,
                    Degree = value.Degree,
                    Label = value.Label,
                    WasSuppressed = value.WasSuppressed
                })
                .ToList();
        }

        /// <summary>返回地图、商队和世界旅行中的存活玩家派系殖民者。</summary>
        private static List<Pawn> ResolvePlayerColonists()
        {
            return ResolveAllLivingHumanlikes()
                .Where(pawn => pawn.IsColonist)
                .ToList();
        }

        /// <summary>汇总地图、WorldPawns、商队与旅行运输器中的存活人类。</summary>
        private static List<Pawn> ResolveAllLivingHumanlikes()
        {
            HashSet<Pawn> result = new HashSet<Pawn>();
            result.UnionWith(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead);
            result.UnionWith(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead);
            return result.Where(pawn => pawn != null && !pawn.Dead && pawn.RaceProps?.Humanlike == true).ToList();
        }

        /// <summary>由来源人物和事件时刻生成稳定种子。</summary>
        private static int StableSeed(string value, int tick)
        {
            return BlackTriggerStableRandom.Hash(tick, value);
        }
    }
}
