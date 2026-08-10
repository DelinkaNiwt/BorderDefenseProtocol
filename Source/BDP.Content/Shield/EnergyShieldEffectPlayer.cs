using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 正式能量护盾命中特效播放器。
    /// 复用旧 BDP 六边形表现和 RimWorld 原版闪光、回退特效与音效。
    /// </summary>
    internal static class EnergyShieldEffectPlayer
    {
        /// <summary>
        /// 播放一次护盾抵挡表现。
        /// </summary>
        internal static void Play(
            Vector3 position,
            Map map,
            EffecterDef effectDef,
            float scale)
        {
            if (map == null)
            {
                return;
            }

            bool useScaledDefault = effectDef == null
                || effectDef.defName == "Interceptor_BlockedProjectilePsychic";
            if (useScaledDefault && scale != 1f)
            {
                PlayScaledBlockEffect(position, map, scale);
            }
            else
            {
                PlayStandardEffect(position, map, effectDef);
            }
        }

        /// <summary>
        /// 播放旧版同构的缩放六边形命中特效。
        /// </summary>
        private static void PlayScaledBlockEffect(Vector3 position, Map map, float scale)
        {
            float resolvedScale = scale * 0.91f;
            FleckDef shieldFleck =
                DefDatabase<FleckDef>.GetNamedSilentFail("BDP_Fleck_EnergyShieldBlock");
            FleckDef flashFleck =
                DefDatabase<FleckDef>.GetNamedSilentFail("ExplosionFlash");

            if (shieldFleck != null)
            {
                if (flashFleck != null)
                {
                    FleckMaker.Static(position, map, flashFleck, resolvedScale);
                }

                float baseScale = resolvedScale * 2f;
                Vector3 firstOffset = new Vector3(
                    Rand.Range(-0.12f, 0.12f),
                    0f,
                    Rand.Range(-0.12f, 0.12f));
                FleckMaker.Static(position + firstOffset, map, shieldFleck, baseScale * 1.25f);
                FleckMaker.Static(position, map, shieldFleck, baseScale);

                Vector3 secondOffset = new Vector3(
                    Rand.Range(-0.08f, 0.08f),
                    0f,
                    Rand.Range(-0.08f, 0.08f));
                FleckMaker.Static(position + secondOffset, map, shieldFleck, baseScale * 0.85f);
                FleckMaker.Static(position, map, shieldFleck, baseScale * 1.05f);
            }
            else
            {
                PlayFallbackEffect(position, map, resolvedScale, flashFleck);
            }

            PlayBlockSound(position, map);
        }

        /// <summary>
        /// 播放调用方显式指定的标准 Effecter，缺失时回退到原版跳跃入口特效。
        /// </summary>
        private static void PlayStandardEffect(
            Vector3 position,
            Map map,
            EffecterDef effectDef)
        {
            EffecterDef resolved = effectDef
                ?? DefDatabase<EffecterDef>.GetNamedSilentFail(
                    "Interceptor_BlockedProjectilePsychic")
                ?? EffecterDefOf.Skip_Entry;
            if (resolved == null)
            {
                return;
            }

            Effecter effecter = resolved.Spawn();
            effecter.Trigger(
                new TargetInfo(position.ToIntVec3(), map),
                TargetInfo.Invalid);
            effecter.Cleanup();
        }

        /// <summary>
        /// 在自定义六边形 Def 缺失时播放原版灵能跳跃粒子和闪光。
        /// </summary>
        private static void PlayFallbackEffect(
            Vector3 position,
            Map map,
            float scale,
            FleckDef flashFleck)
        {
            FleckDef skipFleck =
                DefDatabase<FleckDef>.GetNamedSilentFail("PsycastSkipEffect");
            if (skipFleck == null || flashFleck == null)
            {
                PlayStandardEffect(position, map, null);
                return;
            }

            FleckMaker.Static(
                position,
                map,
                skipFleck,
                Rand.Range(0.5f, 0.75f) * scale);
            FleckMaker.Static(position, map, flashFleck, 3f * scale);
        }

        /// <summary>
        /// 若原版拦截音效存在，则在命中位置播放一次。
        /// </summary>
        private static void PlayBlockSound(Vector3 position, Map map)
        {
            SoundDef sound =
                DefDatabase<SoundDef>.GetNamedSilentFail("Interceptor_BlockProjectile");
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(position.ToIntVec3(), map));
            }
        }
    }
}
