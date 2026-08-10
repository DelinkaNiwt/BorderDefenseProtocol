using RimWorld;
using Verse;

namespace BDP.Content.Trion.Talent.WorkGivers
{
    /// <summary>
    /// 让被选中且能够行走的受检者自行进入固定 Trion 天赋检测仪。
    /// 具体资格、预留和工作创建全部复用原版进入建筑实现。
    /// </summary>
    public sealed class WorkGiver_EnterTrionDetector : WorkGiver_EnterBuilding
    {
        /// <summary>固定检测仪定义；延迟查询以等待 Def 加载完成。</summary>
        private static ThingDef TrionDetectorDef
        {
            get { return DefDatabase<ThingDef>.GetNamed("BDP_TrionDetector"); }
        }

        /// <summary>只扫描固定 Trion 天赋检测仪。</summary>
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(TrionDetectorDef); }
        }
    }
}
