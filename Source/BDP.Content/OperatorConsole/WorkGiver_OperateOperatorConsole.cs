using BDP.Content.OperatorConsole;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 操作员控制台的直接指派工作。
    /// 第一版不参与普通自动工作扫描，只响应玩家强制指派。
    /// </summary>
    public sealed class WorkGiver_OperateOperatorConsole : WorkGiver_Scanner
    {
        /// <summary>延迟取得控制台 ThingDef，等待 Def 数据库完成加载。</summary>
        private static ThingDef ConsoleDef
        {
            get { return DefDatabase<ThingDef>.GetNamed("BDP_OperatorConsole"); }
        }

        /// <summary>从建筑交互格进入工作。</summary>
        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.InteractionCell; }
        }

        /// <summary>只将操作员控制台作为潜在直接指派目标。</summary>
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(ConsoleDef); }
        }

        /// <summary>只接受玩家直接指派的强制工作，不进入自动扫描。</summary>
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!forced)
            {
                return false;
            }

            Building_OperatorConsole console = thing as Building_OperatorConsole;
            if (console == null || !console.HasPawnOperationPrerequisites(pawn))
            {
                return false;
            }

            if (!console.HasOperationalPrerequisites)
            {
                bool hasPower = console.HasPower;
                bool hasTrionPermission = console.HasTrionRelayCorePermission;
                string reasonKey = !hasPower && !hasTrionPermission
                    ? "BDP_OperatorConsole_CannotOperate_NoPowerAndTrion"
                    : (!hasPower
                        ? "BDP_OperatorConsole_CannotOperate_NoPower"
                        : "BDP_OperatorConsole_CannotOperate_NoTrionPermission");
                JobFailReason.Is(reasonKey.Translate());
                return false;
            }

            return pawn.CanReserveAndReach(
                console,
                PathEndMode.InteractionCell,
                Danger.Some,
                1,
                -1,
                null,
                true);
        }

        /// <summary>为直接指派创建控制台值守工作。</summary>
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return JobMaker.MakeJob(OperatorConsoleJobDefOf.BDP_OperateOperatorConsole, thing);
        }
    }
}
