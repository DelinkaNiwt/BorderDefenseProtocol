using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>判定候选物品并把明确非法实体一对一替换为统一遗留物。</summary>
    public sealed class InvalidChipReplacementService
    {
        /// <summary>统一遗留物 DefName。</summary>
        private const string RemnantDefName = "BDP_InvalidChipRemnant";

        /// <summary>执行候选迁移；来源缺失保留，明确非法才替换。</summary>
        public void ReplaceCandidates(
            System.Collections.Generic.IEnumerable<InvalidChipCandidate> candidates,
            InvalidChipMigrationReport report)
        {
            ChipCombinationResolver resolver = new ChipCombinationResolver();
            InvalidChipPlacementService placement = new InvalidChipPlacementService();
            TriggerInvalidChipEvacuationAdapter triggerAdapter =
                new TriggerInvalidChipEvacuationAdapter();

            foreach (InvalidChipCandidate candidate in candidates)
            {
                // 旧持久化格式没有当前组合记录，不能把历史来源重新解释成合法成品。
                // 它必须直接进入非法物品替换路径，避免旧物品继续留在激活槽位中。
                if (!candidate.LegacyPersistenceDetected)
                {
                    ChipCombinationResolution resolution = resolver.Resolve(candidate.Record);
                    if (resolution.Status == ChipCombinationResolutionStatus.MissingSource)
                    {
                        report.RecordPreservedMissingSource();
                        continue;
                    }

                    if (resolution.Status != ChipCombinationResolutionStatus.Invalid)
                    {
                        continue;
                    }
                }

                VerseThing remnant = ThingMaker.MakeThing(ThingDef.Named(RemnantDefName));
                Pawn triggerOwner;
                bool replaced;
                if (triggerAdapter.TryFindLoadedChipOwner(candidate.Item, out triggerOwner))
                {
                    replaced = placement.ReplaceForTriggerOwner(
                        triggerOwner,
                        remnant,
                        delegate
                        {
                            Pawn ignoredOwner;
                            return triggerAdapter.TryDestroyLoadedChip(
                                candidate.Item,
                                out ignoredOwner);
                        });
                }
                else
                {
                    replaced = placement.Replace(candidate.Item, remnant);
                }
                if (replaced)
                {
                    report.RecordReplacedItem();
                }
                else if (!remnant.Destroyed)
                {
                    remnant.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
