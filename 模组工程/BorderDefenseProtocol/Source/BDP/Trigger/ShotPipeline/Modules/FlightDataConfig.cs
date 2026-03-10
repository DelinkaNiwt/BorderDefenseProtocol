using System.Collections.Generic;
using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 飞行数据配置：存储手动引导路径，供射击时注入弹道。
    /// 由 FlightDataModule 产出，在 TryCastShot 中传递给 Bullet_BDP。
    /// </summary>
    public class FlightDataConfig
    {
        /// <summary>锚点路径（不含最终目标）</summary>
        public List<IntVec3> AnchorPath;

        /// <summary>最终目标</summary>
        public LocalTargetInfo FinalTarget;

        /// <summary>锚点散布半径</summary>
        public float AnchorSpread;

        /// <summary>是否有效（有锚点路径）</summary>
        public bool IsValid => AnchorPath != null && AnchorPath.Count > 0;
    }
}
