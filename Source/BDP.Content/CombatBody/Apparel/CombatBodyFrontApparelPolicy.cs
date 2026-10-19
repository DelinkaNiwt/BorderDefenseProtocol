using System.Collections.Generic;
using System.Reflection;
using BDP.Core.CombatBody.External;
using RimWorld;
using Verse;

namespace BDP.Content.CombatBody.Apparel
{
    /// <summary>
    /// 战斗体前台衣物统一属性策略。
    /// 所有镜像与预设实例都在 Core 公共边界进入这里，不由具体预设配置。
    /// </summary>
    public sealed class CombatBodyFrontApparelPolicy : ICombatBodyFrontApparelProvider
    {
        /// <summary>
        /// 腰带层物品不加入镜像前台；其他衣物保持原有镜像行为。
        /// </summary>
        public bool CanMirror(RimWorld.Apparel apparel)
        {
            List<ApparelLayerDef> layers = apparel?.def?.apparel?.layers;
            if (layers == null)
            {
                return true;
            }

            for (int index = 0; index < layers.Count; index++)
            {
                if (layers[index] == ApparelLayerDefOf.Belt)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 准备战斗体前台衣物实例，统一移除品质并归一耐久。
        /// </summary>
        public void Prepare(RimWorld.Apparel apparel)
        {
            if (apparel == null)
            {
                return;
            }

            RemoveQualityComp(apparel);
            NormalizeHitPoints(apparel);
        }

        /// <summary>
        /// 将服装耐久归一为满值，避免旧实例导致原版锁定自动解除。
        /// </summary>
        private static void NormalizeHitPoints(RimWorld.Apparel apparel)
        {
            if (apparel.def != null && apparel.def.useHitPoints)
            {
                apparel.HitPoints = apparel.MaxHitPoints;
            }
        }

        /// <summary>
        /// 从实例组件集合及原版组件索引中移除品质组件。
        /// 原版读档会按 Def 重新初始化组件，因此该方法也会在读档重建后再次调用。
        /// </summary>
        private static void RemoveQualityComp(RimWorld.Apparel apparel)
        {
            CompQuality qualityComp = apparel.TryGetComp<CompQuality>();
            if (qualityComp == null)
            {
                return;
            }

            FieldInfo compsField = typeof(ThingWithComps).GetField(
                "comps",
                BindingFlags.NonPublic | BindingFlags.Instance);
            List<ThingComp> comps = compsField?.GetValue(apparel) as List<ThingComp>;
            comps?.Remove(qualityComp);

            apparel.compQuality = null;

            FieldInfo compsByTypeField = typeof(ThingWithComps).GetField(
                "compsByType",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<System.Type, ThingComp[]> compsByType =
                compsByTypeField?.GetValue(apparel) as Dictionary<System.Type, ThingComp[]>;
            compsByType?.Remove(typeof(CompQuality));
        }
    }
}
