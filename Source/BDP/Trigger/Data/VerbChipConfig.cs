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
        // v9.0新增：功能域配置（分组）
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
        // 旧字段（阶段2将移除）
        // ═══════════════════════════════════════════════════════

        /// <summary>近战连击数（默认1=单次攻击）。</summary>
        public int meleeBurstCount = 1;

        /// <summary>近战连击间隔（ticks，默认12≈0.2秒）。仅meleeBurstCount>1时有效。</summary>
        public int meleeBurstInterval = 12;

        /// <summary>近战武器Tool配置（替代ThingDef.tools，避免IsWeapon=true）。</summary>
        public List<Tool> tools;

        /// <summary>每发射击Trion消耗（0=无消耗）。</summary>
        public float trionCostPerShot = 0f;

        /// <summary>齐射时每发子弹射出起点的随机偏移半径（格）。0=无偏移，0.3=轻微散布，0.6=明显散布。</summary>
        public float volleySpreadRadius = 0f;

        /// <summary>是否支持变化弹（引导飞行模式）。</summary>
        public bool supportsGuided = false;

        /// <summary>最大锚点数（不含最终目标）。仅supportsGuided=true时有效。</summary>
        public int maxAnchors = 3;

        /// <summary>
        /// 锚点散布基础半径（格）。每个锚点按递增系数偏移：
        /// actualAnchor[i] = anchor[i] + Random.insideUnitCircle * anchorSpread * (i / totalAnchors)
        /// 第一段偏移最小，最后一段偏移最大。齐射时每颗子弹独立计算。
        /// </summary>
        public float anchorSpread = 0.3f;

        /// <summary>
        /// 穿体穿透力初始值（0=不穿透）。
        /// 区别于护甲穿透（armorPenetration）——此值决定子弹能否穿过目标继续飞行。
        /// 每次穿透后由ImpactHandler递减，降至0时停止穿透。
        /// </summary>
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
