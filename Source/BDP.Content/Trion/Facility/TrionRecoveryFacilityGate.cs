using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// 玩家派系 Pawn 的 Trion 自然恢复门控：核心许可与实际睡眠共同决定倍率。
    /// 非玩家派系、无地图 Pawn 不受本建筑业务限制。
    /// </summary>
    public sealed class TrionRecoveryFacilityGate : ITrionRecoveryGate
    {
        /// <summary>
        /// 读取当前地图的共享许可与原版睡眠标记，不检查建筑或推进资源结算。
        /// </summary>
        /// <param name="pawn">准备进行自然恢复的 Pawn。</param>
        /// <returns>本次自然恢复相对于角色基准恢复率的倍率。</returns>
        public float GetRecoveryMultiplier(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Faction != Faction.OfPlayer)
            {
                return 1f;
            }

            TrionFacilityMapComponent component = TrionFacilityMapComponent.GetOrCreate(pawn.Map);
            bool hasPermit = component != null && component.HasActivePlayerTrionFacility();
            // 只认原版任务执行器的实际睡眠；躺下或昏迷本身不等于睡着。
            if (pawn.jobs?.curDriver?.asleep == true)
            {
                TrionRecoverySettingsDef settings = TrionRecoverySettingsDefOf.BDP_TrionRecoverySettings;
                if (settings != null)
                {
                    return hasPermit ? settings.sleepWithCoreMultiplier : settings.sleepWithoutCoreMultiplier;
                }
            }

            // 配置未加载时沿用原有核心许可规则，不在代码里补写睡眠数值。
            return hasPermit ? 1f : 0f;
        }
    }
}
