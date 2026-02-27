using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 武器芯片的DefModExtension配置。
    /// 原因（T36）：武器数据不能放ThingDef.Verbs/tools，否则IsWeapon=true。
    /// Fix-9：从Verb_BDPMelee.cs底部提取到独立文件，便于维护。
    /// </summary>
    public class WeaponChipConfig : DefModExtension
    {
        /// <summary>近战连击数（默认1=单次攻击）。</summary>
        public int meleeBurstCount = 1;

        /// <summary>近战连击间隔（ticks，默认12≈0.2秒）。仅meleeBurstCount>1时有效。</summary>
        public int meleeBurstInterval = 12;

        /// <summary>远程武器Verb配置（替代ThingDef.Verbs，避免IsWeapon=true）。</summary>
        public List<VerbProperties> verbProperties;

        /// <summary>近战武器Tool配置（替代ThingDef.tools，避免IsWeapon=true）。</summary>
        public List<Tool> tools;

        /// <summary>每发射击Trion消耗（0=无消耗）。</summary>
        public float trionCostPerShot = 0f;

        /// <summary>
        /// 是否支持齐射模式（右键触发）。
        /// true时，右键Gizmo进入齐射瞄准：所有子弹在同一tick内一齐发射。
        /// 仅对远程芯片有效。
        /// </summary>
        public bool supportsVolley = false;

        /// <summary>
        /// 齐射时每发子弹射出起点的随机偏移半径（格）。
        /// 0=无偏移（所有子弹从同一点射出），0.3=轻微散布，0.6=明显散布。
        /// 仅supportsVolley=true时有效。
        /// </summary>
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

        /// <summary>从verbProperties中读取第一个burstShotCount>0的值（默认1）。</summary>
        public int GetFirstBurstCount()
        {
            if (verbProperties == null) return 1;
            foreach (var vp in verbProperties)
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            return 1;
        }

        /// <summary>从verbProperties中读取第一个非null的defaultProjectile。</summary>
        public ThingDef GetFirstProjectileDef()
        {
            if (verbProperties == null) return null;
            foreach (var vp in verbProperties)
                if (vp.defaultProjectile != null) return vp.defaultProjectile;
            return null;
        }
    }
}
