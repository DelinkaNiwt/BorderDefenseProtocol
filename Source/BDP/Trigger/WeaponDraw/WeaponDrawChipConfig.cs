using UnityEngine;
using Verse;

namespace BDP.Trigger.WeaponDraw
{
    /// <summary>
    /// 武器绘制芯片配置——DefModExtension，挂载在芯片ThingDef上。
    ///
    /// 参数语义（以"右手South"为绘制基准）：
    ///
    /// ── South / North ──
    ///   defaultOffset   : 右手South的XZ偏移（世界空间）。
    ///                     X分量决定左右手水平间距（右手South时X为负=屏幕左）。
    ///   southZAdjust    : South朝向整体Z微调（通常为负，贴近玩家视角）。
    ///   northZAdjust    : North朝向整体Z微调（通常为正，远离玩家视角）。
    ///
    /// ── East / West ──
    ///   sideBaseX       : 侧身时两手共同的X基准（朝东为正，朝西自动取反）。
    ///   sideDeltaX      : 前景/背景手相对基准的X微分（前景手靠近原点）。
    ///   sideDeltaZ      : 前景/背景手的Z微分（前景手为负=靠近玩家，背景手为正）。
    ///
    /// 推导规则见 CompTriggerBody.WeaponDraw.cs。
    /// </summary>
    public class WeaponDrawChipConfig : DefModExtension
    {
        // ── 必填 ──

        /// <summary>武器贴图配置（使用RimWorld标准GraphicData）。</summary>
        public GraphicData graphicData;

        /// <summary>发光层贴图配置（可选）。应使用MoteGlow shader。</summary>
        public GraphicData glowGraphicData;

        // ── South/North 参数 ──

        /// <summary>
        /// 右手South基准偏移（世界空间XZ）。
        /// X分量：右手South时为负（屏幕左），左手South自动取反为正（屏幕右）。
        /// Z分量：武器相对持握点的前后偏移。
        /// </summary>
        public Vector3 defaultOffset = Vector3.zero;

        /// <summary>默认旋转角度（度）。</summary>
        public float defaultAngle = 0f;

        /// <summary>默认高度偏移（正=前景，负=背景）。</summary>
        public float defaultAltitudeOffset = 0.1f;

        /// <summary>South朝向整体Z微调（叠加在defaultOffset.z上，通常为小负值）。</summary>
        public float southZAdjust = 0f;

        /// <summary>North朝向整体Z微调（叠加在defaultOffset.z上，通常为小正值）。</summary>
        public float northZAdjust = 0f;

        // ── 姿势参数 ──

        /// <summary>
        /// 左手武器额外角度偏移（度）。
        /// 使左右手始终保持角度差，待机时呈不对称交叉，攻击时呈扇形展开。
        /// 默认30°：右手对准目标，左手偏转30°。
        /// </summary>
        public float leftHandAngleOffset = 30f;

        // ── East/West 参数 ──

        /// <summary>
        /// 侧身时两手共同的X基准（朝东为正，朝西自动取反）。
        /// 代表角色侧身时手臂区域相对持握点的X偏移。
        /// </summary>
        public float sideBaseX = 0f;

        /// <summary>
        /// 前景/背景手相对sideBaseX的X微分。
        /// 前景手：sideBaseX - sideDeltaX（靠近原点）。
        /// 背景手：sideBaseX + sideDeltaX（远离原点）。
        /// </summary>
        public float sideDeltaX = 0f;

        /// <summary>
        /// 前景/背景手的Z微分（体现远近感）。
        /// 前景手：-sideDeltaZ（靠近玩家）。
        /// 背景手：+sideDeltaZ（远离玩家）。
        /// </summary>
        public float sideDeltaZ = 0f;

        // ── 枪口位置配置 ──

        /// <summary>
        /// 枪口相对于武器中心的偏移（武器局部坐标系）。
        /// 例如：(0, 0, 0.3) 表示枪口在武器前端0.3米处。
        /// X: 左右偏移，Y: 上下偏移，Z: 前后偏移（正值=向前）
        /// </summary>
        public Vector3 muzzleOffset = Vector3.zero;

        /// <summary>
        /// 左手枪口偏移覆盖值（可选）。
        /// 如果为null，左手枪口位置通过镜像右手自动推导。
        /// 用于非对称武器（如左右手持不同型号的枪）。
        /// </summary>
        public Vector3? leftMuzzleOffsetOverride = null;

        /// <summary>
        /// 是否为远程武器（枪械）。
        /// true: 子弹从枪口发射
        /// false: 子弹从小人中心发射（近战武器、未配置的武器）
        /// </summary>
        public bool isRangedWeapon = false;

        // ── 缓存 ──

        private Graphic cachedGraphic;
        private Graphic cachedGlowGraphic;

        public Graphic GetGraphic(Thing chip)
        {
            if (cachedGraphic == null && graphicData != null)
                cachedGraphic = graphicData.GraphicColoredFor(chip);
            return cachedGraphic;
        }

        /// <summary>获取（或懒初始化）发光层Graphic。null=无发光层配置。</summary>
        public Graphic GetGlowGraphic(Thing chip)
        {
            if (cachedGlowGraphic == null && glowGraphicData != null)
                cachedGlowGraphic = glowGraphicData.Graphic;  // 使用 Graphic 属性，读取 XML 中的 color 配置
            return cachedGlowGraphic;
        }
    }
}
