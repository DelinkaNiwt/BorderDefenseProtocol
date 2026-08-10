using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 限制同一地图内紧急脱离信标只能存在一座。
    /// 放置限制属于紧急脱离信标业务，随 Content 一起提供。
    /// </summary>
    public sealed class PlaceWorker_EmergencyEscapeBeaconOnlyOnePerMap : PlaceWorker
    {
        /// <summary>
        /// 判断当前位置是否允许放置。
        /// </summary>
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            ThingDef thingDef = checkingDef as ThingDef;
            if (thingDef == null || map == null)
            {
                return AcceptanceReport.WasAccepted;
            }

            foreach (Building building in map.listerBuildings.AllBuildingsColonistOfDef(thingDef))
            {
                if (building != thingToIgnore)
                {
                    return new AcceptanceReport(
                        "BDP_Message_EmergencyEscape_AlreadyExists".Translate(checkingDef.label));
                }
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
