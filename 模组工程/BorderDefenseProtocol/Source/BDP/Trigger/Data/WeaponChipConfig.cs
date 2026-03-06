using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 武器芯片的DefModExtension配置。
    /// 原因（T36）：武器数据不能放ThingDef.Verbs/tools，否则IsWeapon=true。
    /// Fix-9：从Verb_BDPMelee.cs底部提取到独立文件，便于维护。
    /// v8.0变更：新增primaryVerbProps/secondaryVerbProps，废弃verbProperties/supportsVolley。
    /// </summary>
    public class WeaponChipConfig : DefModExtension
    {
        // ═══════════════════════════════════════════════════════
        // v8.0新增：显式攻击模式配置（推荐使用）
        // ═══════════════════════════════════════════════════════

        /// <summary>主攻击Verb配置（左键）。null时无左键攻击。</summary>
        public VerbProperties primaryVerbProps;

        /// <summary>副攻击Verb配置（右键，可选）。null时右键走默认行为（取消）。</summary>
        public VerbProperties secondaryVerbProps;

        // ═══════════════════════════════════════════════════════
        // 近战配置
        // ═══════════════════════════════════════════════════════

        /// <summary>近战连击数（默认1=单次攻击）。</summary>
        public int meleeBurstCount = 1;

        /// <summary>近战连击间隔（ticks，默认12≈0.2秒）。仅meleeBurstCount>1时有效。</summary>
        public int meleeBurstInterval = 12;

        /// <summary>近战武器Tool配置（替代ThingDef.tools，避免IsWeapon=true）。</summary>
        public List<Tool> tools;

        // ═══════════════════════════════════════════════════════
        // 通用配置
        // ═══════════════════════════════════════════════════════

        /// <summary>每发射击Trion消耗（0=无消耗）。</summary>
        public float trionCostPerShot = 0f;

        // ═══════════════════════════════════════════════════════
        // 废弃字段（保留用于向后兼容）
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [已废弃] 远程武器Verb配置列表。
        /// 请使用 primaryVerbProps/secondaryVerbProps 替代。
        /// 保留用于向后兼容：如果新字段为null，则回退到此字段。
        /// </summary>
        [System.Obsolete("Use primaryVerbProps/secondaryVerbProps instead")]
        public List<VerbProperties> verbProperties;

        /// <summary>
        /// [已废弃] 是否支持齐射模式（右键触发）。
        /// 请使用 secondaryVerbProps 显式配置齐射verb替代。
        /// 保留用于向后兼容：如果secondaryVerbProps为null且此字段为true，则自动创建齐射verb。
        /// </summary>
        [System.Obsolete("Use secondaryVerbProps with Verb_BDPVolley instead")]
        public bool supportsVolley = false;

        // ═══════════════════════════════════════════════════════
        // 特殊机制配置
        // ═══════════════════════════════════════════════════════

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

        /// <summary>获取主攻击verb的burstShotCount（优先使用primaryVerbProps，回退到verbProperties）。</summary>
        public int GetPrimaryBurstCount()
        {
            if (primaryVerbProps != null && primaryVerbProps.burstShotCount > 0)
                return primaryVerbProps.burstShotCount;
            return GetFirstBurstCount(); // 回退到旧逻辑
        }

        /// <summary>获取主攻击verb的投射物（优先使用primaryVerbProps，回退到verbProperties）。</summary>
        public ThingDef GetPrimaryProjectileDef()
        {
            if (primaryVerbProps != null && primaryVerbProps.defaultProjectile != null)
                return primaryVerbProps.defaultProjectile;
            return GetFirstProjectileDef(); // 回退到旧逻辑
        }

        /// <summary>[已废弃] 从verbProperties中读取第一个burstShotCount>0的值（默认1）。</summary>
        public int GetFirstBurstCount()
        {
            if (verbProperties == null) return 1;
            foreach (var vp in verbProperties)
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            return 1;
        }

        /// <summary>[已废弃] 从verbProperties中读取第一个非null的defaultProjectile。</summary>
        public ThingDef GetFirstProjectileDef()
        {
            if (verbProperties == null) return null;
            foreach (var vp in verbProperties)
                if (vp.defaultProjectile != null) return vp.defaultProjectile;
            return null;
        }
    }
}
