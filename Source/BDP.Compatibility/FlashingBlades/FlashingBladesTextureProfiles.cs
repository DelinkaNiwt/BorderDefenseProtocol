using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>仅供 BDP 手动刀光使用的私有效果副本；不注册或修改全局定义。</summary>
    internal sealed class FlashingBladesTextureProfiles
    {
        /// <summary>保留原定义全部运动、绘制与扩展字段，只复制需要写入的对象层级。</summary>
        private static readonly MethodInfo Copy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        /// <summary>已校验的外部接口。</summary>
        private readonly FlashingBladesDiscovery targets;
        /// <summary>按已加载效果配置与手动类别缓存，不按角色或攻击次数增长。</summary>
        private readonly Dictionary<Tuple<object, object>, object> profiles = new Dictionary<Tuple<object, object>, object>();

        /// <summary>每次适配器绑定建立独立缓存，停用后不影响已生成粒子的引用。</summary>
        internal FlashingBladesTextureProfiles(FlashingBladesDiscovery targets) { this.targets = targets; }

        /// <summary>只改图像，不替换自动效果的速度、命中闪光、配色或层数。</summary>
        internal object Resolve(object profile, object category)
        {
            Tuple<object, object> key = Tuple.Create(profile, category);
            object result;
            if (profiles.TryGetValue(key, out result)) return result;
            FleckDef texture = (FleckDef)targets.Invoke("TextureFleck", category);
            if (texture == null) return profile;
            result = Clone(profile);
            targets.Set(result, "effecterDef", CloneEffect((EffecterDef)targets.Get(profile, "effecterDef"), texture));
            targets.Set(result, "extraTrailEffecterDef", CloneEffect((EffecterDef)targets.Get(profile, "extraTrailEffecterDef"), texture));
            profiles.Add(key, result);
            return result;
        }

        /// <summary>主层与附加层均持有私有粒子定义；原版直到绘制时才读取其图像，不能生成后立即还原。</summary>
        private static EffecterDef CloneEffect(EffecterDef original, FleckDef texture)
        {
            if (original == null) return null;
            EffecterDef result = Clone(original);
            if (original.children == null) return result;
            result.children = new List<SubEffecterDef>();
            foreach (SubEffecterDef child in original.children)
            {
                if (child == null) { result.children.Add(null); continue; }
                SubEffecterDef copy = Clone(child);
                // 与外部手动覆盖范围一致：目标处的命中闪光不替换成刀光。
                if (child.fleckDef != null && child.fleckDef.defName != "ExplosionFlash")
                {
                    copy.fleckDef = Clone(child.fleckDef);
                    copy.fleckDef.graphicData = texture.graphicData;
                    copy.fleckDef.randomGraphics = texture.randomGraphics;
                }
                result.children.Add(copy);
            }
            return result;
        }

        /// <summary>浅复制后只改私有字段；其它字段继续共享原版只读配置。</summary>
        private static T Clone<T>(T original) where T : class { return (T)Copy.Invoke(original, null); }
    }
}
