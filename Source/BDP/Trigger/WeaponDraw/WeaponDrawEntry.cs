using UnityEngine;
using Verse;

namespace BDP.Trigger.WeaponDraw
{
    /// <summary>
    /// 武器绘制描述符——单帧渲染所需的全部参数。
    ///
    /// 数据流：
    ///   WeaponDrawChipConfig（XML配置）
    ///     → CompTriggerBody.GetActiveWeaponDrawEntries()（聚合）
    ///       → Patch_Pawn_DrawAt_Weapon（消费绘制）
    /// </summary>
    public struct WeaponDrawEntry
    {
        /// <summary>要绘制的Graphic实例。</summary>
        public Graphic graphic;

        /// <summary>发光层Graphic（可选，null=不绘制发光层）。使用MoteGlow shader。</summary>
        public Graphic glowGraphic;

        /// <summary>相对于小人DrawPos的XZ偏移（世界空间）。</summary>
        public Vector3 drawOffset;

        /// <summary>
        /// 高度层控制：
        ///  > 0 → MoteOverhead层（小人前景，武器显示在小人前方）
        ///  = 0 → 与小人同层
        ///  &lt; 0 → Item层（小人背景，武器显示在小人后方）
        /// 具体映射见 Patch_Pawn_DrawAt_Weapon.DrawWeaponEntry()。
        /// </summary>
        public float altitudeOffset;

        /// <summary>绕Y轴旋转角度（度）。</summary>
        public float angle;

        /// <summary>
        /// 是否水平翻转贴图（左右镜像）。
        /// 实现方式：切换 plane10/plane10Flip mesh + angle 取反（避免负 scaleX 触发背面剔除）。
        /// </summary>
        public bool flipHorizontal;
    }

}
