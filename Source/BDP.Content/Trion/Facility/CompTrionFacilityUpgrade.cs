using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施升级状态占位组件。
    /// 本阶段只保存等级并提供能力查询口，不产生升级入口。
    /// </summary>
    public sealed class CompTrionFacilityUpgrade : ThingComp
    {
        /// <summary>
        /// 当前升级等级。
        /// </summary>
        private int upgradeLevel;

        /// <summary>
        /// 对外暴露当前升级等级。
        /// </summary>
        public int UpgradeLevel
        {
            get { return Mathf.Max(0, upgradeLevel); }
        }

        /// <summary>
        /// 查询未来升级能力；当前没有任何能力解锁。
        /// </summary>
        /// <param name="capabilityId">未来能力标识。</param>
        /// <returns>当前阶段始终为 false。</returns>
        public bool HasCapability(string capabilityId)
        {
            return false;
        }

        /// <summary>
        /// 保存升级等级；升级条件和写入口留待后续设计。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref upgradeLevel, "bdpTrionFacilityUpgradeLevel", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                upgradeLevel = Mathf.Max(0, upgradeLevel);
            }
        }
    }
}
