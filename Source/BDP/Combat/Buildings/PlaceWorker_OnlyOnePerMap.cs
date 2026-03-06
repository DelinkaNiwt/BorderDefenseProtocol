using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// PlaceWorker：限制建筑在地图内只能存在一座。
    /// 用于紧急脱离信标。
    /// </summary>
    public class PlaceWorker_OnlyOnePerMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            // 检查地图上是否已存在该建筑
            var existingBuildings = map.listerBuildings.AllBuildingsColonistOfDef(checkingDef as ThingDef);

            // 如果已存在建筑，且不是当前正在放置的建筑（thingToIgnore用于重新安装场景）
            if (existingBuildings.Count > 0)
            {
                foreach (var building in existingBuildings)
                {
                    if (building != thingToIgnore)
                    {
                        return new AcceptanceReport($"地图内已存在一座{checkingDef.label}");
                    }
                }
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
