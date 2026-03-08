using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 射击模式核心数据层（v9.0 FireMode系统）。
    /// 三个属性共享固定预算 3.0：伤害(D) + 速度(S) + 连射数(C) = 3.0。
    /// 任一轴增加时，差值等量分配到其余未锁定轴。
    /// 配置存储在芯片Thing上，随芯片持久化（存档/读档/卸装均保留）。
    /// </summary>
    public class CompFireMode : ThingComp
    {
        // ── 预算常量 ──
        public const float BUDGET    = 3.0f;
        public const float MIN       = 0.1f;
        public const float MAX       = 2.8f;
        /// <summary>Speed 轴专用下限：0.1 时 destination 被拉远 10 倍，极易越界销毁。0.3 是安全阈值。</summary>
        public const float MIN_SPEED = 0.3f;

        // ── 三轴数值（各自范围 [MIN, MAX]，总和 = BUDGET） ──
        private float damage = 1f;
        private float speed  = 1f;
        private float burst  = 1f;

        /// <summary>-1=无锁，0=D，1=S，2=C</summary>
        private int locked = -1;

        // ── 公开只读属性 ──
        public float Damage => damage;
        public float Speed  => speed;
        public float Burst  => burst;
        public int   Locked => locked;

        // ── 预设枚举 ──
        public enum Preset { Balanced, HeavyDamage, Rapid, Sniper }

        /// <summary>
        /// 修改一轴数值，将差值等量分配到其余未锁定轴，最后归一化修正。
        /// </summary>
        public void SetValue(int axis, float newVal)
        {
            // Speed 轴（axis==1）使用专用下限，防止 destination 被拉远越界
            float minVal = (axis == 1) ? MIN_SPEED : MIN;
            newVal = Mathf.Clamp(newVal, minVal, MAX);
            float[] vals = { damage, speed, burst };
            float delta = newVal - vals[axis];
            if (Mathf.Abs(delta) < 0.0001f) return;

            // 统计可分配的未锁定轴（排除当前轴和锁定轴）
            int freeCount = 0;
            for (int i = 0; i < 3; i++)
                if (i != axis && locked != i) freeCount++;

            if (freeCount == 0) return; // 其余轴全锁定，无法调整

            float share = -delta / freeCount;
            vals[axis] = newVal;
            for (int i = 0; i < 3; i++)
            {
                if (i != axis && locked != i)
                    vals[i] = Mathf.Clamp(vals[i] + share, MIN, MAX);
            }

            // 归一化修正：确保总和精确等于 BUDGET
            Normalize(vals, axis);

            damage = vals[0];
            speed  = vals[1];
            burst  = vals[2];
        }

        /// <summary>切换锁定：locked==axis 则解锁，否则锁定该轴。</summary>
        public void SetLocked(int axis)
        {
            locked = (locked == axis) ? -1 : axis;
        }

        /// <summary>应用预设配置。</summary>
        public void ApplyPreset(Preset preset)
        {
            locked = -1; // 预设前解锁所有轴
            switch (preset)
            {
                case Preset.Balanced:
                    damage = 1f; speed = 1f; burst = 1f;
                    break;
                case Preset.HeavyDamage:
                    damage = 2f; speed = 0.5f; burst = 0.5f;
                    break;
                case Preset.Rapid:
                    damage = 0.5f; speed = 1.5f; burst = 1.0f;
                    break;
                case Preset.Sniper:
                    damage = 2f; speed = 0.8f; burst = 0.2f;
                    break;
            }
        }

        // ── 效果计算（基于倍率） ──

        /// <summary>有效连射数 = Max(1, Round(base * burst))</summary>
        public int GetEffectiveBurst(int baseBurst)
            => Mathf.Max(1, Mathf.RoundToInt(baseBurst * burst));

        /// <summary>有效伤害 = Max(1, Round(base * damage))</summary>
        public int GetEffectiveDamage(int baseDamage)
            => Mathf.Max(1, Mathf.RoundToInt(baseDamage * damage));

        /// <summary>有效飞行时间 = Max(1, Ceil(base / speed))</summary>
        public int GetEffectiveTicksToImpact(int baseTicks)
            => Mathf.Max(1, Mathf.CeilToInt(baseTicks / speed));

        // ── UI显示：实际值（从芯片def读取基础数据后乘以倍率） ──

        private WeaponChipConfig GetChipConfig()
            => parent?.def?.GetModExtension<WeaponChipConfig>();

        /// <summary>UI显示用：实际伤害值（整数）。无法读取时返回-1。</summary>
        public int GetDisplayDamage()
        {
            var projDef = GetChipConfig()?.GetFirstProjectileDef();
            if (projDef?.projectile == null) return -1;
            int baseDmg = projDef.projectile.GetDamageAmount(null);
            return GetEffectiveDamage(baseDmg);
        }

        /// <summary>UI显示用：实际速度（tiles/sec）。无法读取时返回-1。</summary>
        public float GetDisplaySpeed()
        {
            var projDef = GetChipConfig()?.GetFirstProjectileDef();
            if (projDef?.projectile == null) return -1f;
            return projDef.projectile.speed * speed;
        }

        /// <summary>UI显示用：实际连射数（整数）。无法读取时返回-1。</summary>
        public int GetDisplayBurst()
        {
            int baseBurst = GetChipConfig()?.GetFirstBurstCount() ?? -1;
            if (baseBurst < 0) return -1;
            return GetEffectiveBurst(baseBurst);
        }

        // ── 序列化 ──
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref damage, "fm_damage", 1f);
            Scribe_Values.Look(ref speed,  "fm_speed",  1f);
            Scribe_Values.Look(ref burst,  "fm_burst",  1f);
            Scribe_Values.Look(ref locked, "fm_locked", -1);
        }

        // ── 内部工具 ──

        /// <summary>
        /// 归一化：调整未锁定轴使总和精确等于 BUDGET。
        /// 优先调整非当前轴的未锁定轴，避免破坏用户刚设置的值。
        /// </summary>
        private static void Normalize(float[] vals, int skipAxis)
        {
            float sum = vals[0] + vals[1] + vals[2];
            float diff = BUDGET - sum;
            if (Mathf.Abs(diff) < 0.0001f) return;

            // 找一个可调整的轴（非skipAxis，且调整后在范围内）
            // 注意：Speed 轴（index 1）使用 MIN_SPEED 作为下限
            for (int i = 0; i < 3; i++)
            {
                if (i == skipAxis) continue;
                float minI    = (i == 1) ? MIN_SPEED : MIN;
                float adjusted = vals[i] + diff;
                if (adjusted >= minI && adjusted <= MAX)
                {
                    vals[i] = adjusted;
                    return;
                }
            }
            // 兜底：强制调整 skipAxis 自身
            float minSkip = (skipAxis == 1) ? MIN_SPEED : MIN;
            vals[skipAxis] = Mathf.Clamp(vals[skipAxis] + diff, minSkip, MAX);
        }
    }
}
