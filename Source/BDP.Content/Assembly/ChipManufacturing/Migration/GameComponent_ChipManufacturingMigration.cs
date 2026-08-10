using BDP.Content.Assembly.ChipManufacturing.Bill;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using RimWorld;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>读档结束后仅运行一次的芯片制造数据迁移入口。</summary>
    public sealed class GameComponent_ChipManufacturingMigration : GameComponent
    {
        /// <summary>防止同一游戏会话重复扫描。</summary>
        private bool completedThisSession;

        /// <summary>供原版 GameComponent 自动构造。</summary>
        public GameComponent_ChipManufacturingMigration(Game game)
        {
        }

        /// <summary>新游戏没有旧实体需要迁移。</summary>
        public override void StartedNewGame()
        {
            completedThisSession = true;
        }

        /// <summary>读档完成后排入主线程安全阶段。</summary>
        public override void LoadedGame()
        {
            LongEventHandler.ExecuteWhenFinished(RunOnce);
        }

        /// <summary>扫描实体与账单，并在末尾最多发送一封汇总信。</summary>
        private void RunOnce()
        {
            if (completedThisSession)
            {
                return;
            }

            completedThisSession = true;
            InvalidChipMigrationReport report = new InvalidChipMigrationReport();
            InvalidChipItemCollector collector = new InvalidChipItemCollector();
            new InvalidChipReplacementService().ReplaceCandidates(
                collector.Collect(),
                report);
            RemoveInvalidBills(report);

            if (report.HasAnythingToReport)
            {
                Find.LetterStack.ReceiveLetter(
                    "BDP_ChipMigration_LetterLabel".Translate(),
                    "BDP_ChipMigration_LetterBody".Translate(
                        report.ReplacedItemCount,
                        report.DeletedBillCount,
                        report.PreservedMissingSourceCount),
                    LetterDefOf.NeutralEvent);
            }
        }

        /// <summary>删除明确非法账单；来源缺失账单保留等待来源恢复。</summary>
        private static void RemoveInvalidBills(InvalidChipMigrationReport report)
        {
            ChipCombinationResolver resolver = new ChipCombinationResolver();
            foreach (Map map in Find.Maps)
            {
                foreach (VerseThing thing in map.listerThings.AllThings)
                {
                    Building_ChipFabricator fabricator = thing as Building_ChipFabricator;
                    if (fabricator == null)
                    {
                        continue;
                    }

                    BillStack stack = fabricator.BillStack;
                    for (int index = stack.Bills.Count - 1; index >= 0; index--)
                    {
                        Bill_ChipProduction bill = stack.Bills[index] as Bill_ChipProduction;
                        if (bill == null)
                        {
                            continue;
                        }

                        ChipCombinationResolution resolution =
                            resolver.Resolve(bill.CombinationRecord);
                        if (resolution.Status == ChipCombinationResolutionStatus.Invalid)
                        {
                            stack.Delete(bill);
                            report.RecordDeletedBill();
                        }
                        else if (resolution.Status == ChipCombinationResolutionStatus.MissingSource)
                        {
                            report.RecordPreservedMissingSource();
                        }
                    }
                }
            }
        }
    }
}
