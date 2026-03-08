using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Verb芯片的DefModExtension配置。
    /// 原因（T36）：武器数据不能放ThingDef.Verbs/tools，否则IsWeapon=true。
    /// Fix-9：从Verb_BDPMelee.cs底部提取到独立文件，便于维护。
    /// v8.0变更：新增primaryVerbProps/secondaryVerbProps，废弃verbProperties/supportsVolley。
    /// v9.0变更：统一架构重构，所有芯片必须提供primaryVerbProps，配置按功能域分组。
    /// </summary>
    public class VerbChipConfig : DefModExtension
    {
        // ═══════════════════════════════════════════════════════
        // 核心Verb配置
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 主攻击Verb配置（左键）。
        /// 所有芯片（远程/近战）都必须提供此配置。
        /// null时芯片无法激活攻击。
        /// </summary>
        public VerbProperties primaryVerbProps;

        /// <summary>
        /// 副攻击Verb配置（右键，可选）。
        /// null时右键走默认行为（取消）。
        /// </summary>
        public VerbProperties secondaryVerbProps;

        // ═══════════════════════════════════════════════════════
        // 功能域配置（分组）
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 成本配置（Trion消耗等）。
        /// null时使用默认值（无消耗）。
        /// </summary>
        public CostConfig cost;

        /// <summary>
        /// 近战配置（tools等）。
        /// 仅近战芯片需要提供。
        /// null时表示非近战芯片。
        /// </summary>
        public MeleeConfig melee;

        /// <summary>
        /// 远程配置（齐射散布、引导飞行、穿透等）。
        /// 仅远程芯片需要提供。
        /// null时表示非远程芯片。
        /// </summary>
        public RangedConfig ranged;

        // ═══════════════════════════════════════════════════════
        // 旧字段（向后兼容，阶段3 XML迁移后将移除）
        // ═══════════════════════════════════════════════════════

        /// <summary>近战连击数（默认1=单次攻击）。已废弃，使用primaryVerbProps.burstShotCount。</summary>
        [Obsolete("Use primaryVerbProps.burstShotCount instead")]
        public int meleeBurstCount = 1;

        /// <summary>近战连击间隔（ticks，默认12≈0.2秒）。已废弃，使用primaryVerbProps.ticksBetweenBurstShots。</summary>
        [Obsolete("Use primaryVerbProps.ticksBetweenBurstShots instead")]
        public int meleeBurstInterval = 12;

        /// <summary>近战武器Tool配置。已废弃，使用melee.tools。</summary>
        [Obsolete("Use melee.tools instead")]
        public List<Tool> tools;

        /// <summary>每发射击Trion消耗。已废弃，使用cost.trionPerShot。</summary>
        [Obsolete("Use cost.trionPerShot instead")]
        public float trionCostPerShot = 0f;

        /// <summary>齐射散布半径。已废弃，使用ranged.volleySpreadRadius。</summary>
        [Obsolete("Use ranged.volleySpreadRadius instead")]
        public float volleySpreadRadius = 0f;

        /// <summary>是否支持变化弹。已废弃，使用ranged.guided != null判断。</summary>
        [Obsolete("Use ranged.guided != null instead")]
        public bool supportsGuided = false;

        /// <summary>最大锚点数。已废弃，使用ranged.guided.maxAnchors。</summary>
        [Obsolete("Use ranged.guided.maxAnchors instead")]
        public int maxAnchors = 3;

        /// <summary>锚点散布半径。已废弃，使用ranged.guided.anchorSpread。</summary>
        [Obsolete("Use ranged.guided.anchorSpread instead")]
        public float anchorSpread = 0.3f;

        /// <summary>穿体穿透力。已废弃，使用ranged.passthroughPower。</summary>
        [Obsolete("Use ranged.passthroughPower instead")]
        public float passthroughPower = 0f;

        // ═══════════════════════════════════════════════════════
        // 辅助方法
        // ═══════════════════════════════════════════════════════

        /// <summary>获取主攻击verb的burstShotCount。</summary>
        public int GetPrimaryBurstCount()
        {
            return primaryVerbProps?.burstShotCount ?? 1;
        }

        /// <summary>获取主攻击verb的投射物。</summary>
        public ThingDef GetPrimaryProjectileDef()
        {
            return primaryVerbProps?.defaultProjectile;
        }
    }
}
