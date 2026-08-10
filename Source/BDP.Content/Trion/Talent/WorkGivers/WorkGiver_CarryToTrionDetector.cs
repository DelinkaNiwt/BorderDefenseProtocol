using RimWorld;
using Verse;

namespace BDP.Content.Trion.Talent.WorkGivers
{
    /// <summary>
    /// 让殖民者把倒地角色或囚犯搬入固定 Trion 天赋检测仪。
    /// 具体资格、预留和工作创建全部复用原版搬运至建筑实现。
    /// </summary>
    public sealed class WorkGiver_CarryToTrionDetector : WorkGiver_CarryToBuilding
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
