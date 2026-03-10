using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// 护盾特效播放器（静态工具类）
    /// 封装护盾抵挡特效的播放逻辑
    /// </summary>
    public static class ShieldEffectPlayer
    {
        /// <summary>
        /// 播放护盾抵挡特效
        /// </summary>
        /// <param name="position">特效位置（世界坐标）</param>
        /// <param name="map">地图</param>
        /// <param name="effectDef">特效Def（可选，默认使用Interceptor_BlockedProjectilePsychic）</param>
        /// <param name="scale">特效缩放比例（默认1.0）</param>
        public static void PlayBlockEffect(Vector3 position, Map map, EffecterDef effectDef = null, float scale = 1f)
        {
            // 地图检查
            if (map == null) return;

            // 如果使用默认特效（Interceptor_BlockedProjectilePsychic）且需要缩放，手动生成Fleck
            bool useDefaultEffect = effectDef == null || effectDef.defName == "Interceptor_BlockedProjectilePsychic";

            if (useDefaultEffect && scale != 1f)
            {
                // 手动生成虫洞特效的两个Fleck，应用缩放
                PlayScaledPsychicBlockEffect(position, map, scale);
            }
            else
            {
                // 使用标准Effecter系统（不支持缩放）
                EffecterDef def = effectDef;
                if (def == null)
                {
                    // 尝试从DefDatabase获取灵能护盾拦截特效
                    def = DefDatabase<EffecterDef>.GetNamedSilentFail("Interceptor_BlockedProjectilePsychic");
                    // 如果没有RoyaltyDLC，回退到Skip_Entry
                    if (def == null)
                    {
                        def = EffecterDefOf.Skip_Entry;
                    }
                }

                // 生成特效
                Effecter effecter = def.Spawn();
                effecter.Trigger(new TargetInfo(position.ToIntVec3(), map), TargetInfo.Invalid);
                effecter.Cleanup();
            }
        }

        /// <summary>
        /// 播放可缩放的灵能护盾拦截特效
        /// 多层叠加 + 短促有力的抖动效果，增强抵抗张力
        /// </summary>
        private static void PlayScaledPsychicBlockEffect(Vector3 position, Map map, float scale)
        {
            // 特效尺寸增大30%（0.7 * 1.3 = 0.91）
            scale *= 0.91f;

            // 获取自定义特效
            FleckDef hexShieldFleck = DefDatabase<FleckDef>.GetNamedSilentFail("BDP_HexShieldBlock");
            FleckDef shockwaveRingFleck = DefDatabase<FleckDef>.GetNamedSilentFail("BDP_ShockwaveRing");
            FleckDef explosionFlashFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ExplosionFlash");

            if (hexShieldFleck != null)
            {
                // === 多层叠加特效 ===

                // 1. 白色冲击闪光（瞬间爆发，制造硬感）
                if (explosionFlashFleck != null)
                {
                    float flashScale = scale * 1.0f; // 增大闪光尺寸
                    FleckMaker.Static(position, map, explosionFlashFleck, flashScale);
                }

                // 2. 冲击波环 - 多层扩散效果（快速生成不同大小的环模拟扩散）
                if (shockwaveRingFleck != null)
                {
                    // 第一层：小环（起始）
                    FleckMaker.Static(position, map, shockwaveRingFleck, scale * 1.0f);

                    // 第二层：中环（扩散中）
                    FleckMaker.Static(position, map, shockwaveRingFleck, scale * 1.3f);

                    // 第三层：大环（扩散末端）
                    FleckMaker.Static(position, map, shockwaveRingFleck, scale * 1.6f);
                }

                // 3. 正六边形护盾 - 短促有力的抖动效果（两次快速抖动）
                float baseScale = scale * 2f;

                // 第一次抖动：向外猛烈扩张（受到冲击）
                float shake1Scale = baseScale * 1.25f; // 扩张25%
                Vector3 shake1Offset = new Vector3(Rand.Range(-0.12f, 0.12f), 0f, Rand.Range(-0.12f, 0.12f)); // 更大的位置偏移
                FleckMaker.Static(position + shake1Offset, map, hexShieldFleck, shake1Scale);

                // 中间帧：正常大小（抵抗中）
                FleckMaker.Static(position, map, hexShieldFleck, baseScale);

                // 第二次抖动：向内收缩反弹（抵抗反作用力）
                float shake2Scale = baseScale * 0.85f; // 收缩15%
                Vector3 shake2Offset = new Vector3(Rand.Range(-0.08f, 0.08f), 0f, Rand.Range(-0.08f, 0.08f)); // 中等位置偏移
                FleckMaker.Static(position + shake2Offset, map, hexShieldFleck, shake2Scale);

                // 最终帧：稍微回弹（稳定）
                float finalScale = baseScale * 1.05f; // 轻微回弹
                FleckMaker.Static(position, map, hexShieldFleck, finalScale);
            }
            else
            {
                // 回退到原版虫洞特效
                FleckDef psycastSkipFleck = DefDatabase<FleckDef>.GetNamedSilentFail("PsycastSkipEffect");

                if (psycastSkipFleck == null || explosionFlashFleck == null)
                {
                    // 如果没有RoyaltyDLC，使用标准特效
                    Effecter effecter = EffecterDefOf.Skip_Entry.Spawn();
                    effecter.Trigger(new TargetInfo(position.ToIntVec3(), map), TargetInfo.Invalid);
                    effecter.Cleanup();
                    return;
                }

                // 生成虫洞特效
                float skipEffectScale = Rand.Range(0.5f, 0.75f) * scale;
                FleckMaker.Static(position, map, psycastSkipFleck, skipEffectScale);

                // 生成闪光特效
                float flashScale = 3.0f * scale;
                FleckMaker.Static(position, map, explosionFlashFleck, flashScale);
            }

            // 播放音效
            SoundDef blockSound = DefDatabase<SoundDef>.GetNamedSilentFail("Interceptor_BlockProjectile");
            if (blockSound != null)
            {
                blockSound.PlayOneShot(new TargetInfo(position.ToIntVec3(), map));
            }
        }
    }
}
