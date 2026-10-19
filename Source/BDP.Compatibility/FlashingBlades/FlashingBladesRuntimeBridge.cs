using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Verbs;
using RimWorld;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>一次挥击的冻结选择；不保存角色或芯片对象，不参与存档。</summary>
    internal sealed class FlashingBladesPlaybackSelection
    {
        /// <summary>配置项身份；找不到来源时为空。</summary>
        internal readonly string ConfigKey;
        /// <summary>本次实际自动分类。</summary>
        internal readonly object Category;
        /// <summary>本次手动纹理覆盖，自动时为未知枚举。</summary>
        internal readonly object Texture;
        /// <summary>正式攻击结果标识。</summary>
        internal readonly string ResultId;
        /// <summary>真实来源芯片标识。</summary>
        internal readonly string ChipId;
        /// <summary>来源形态键。</summary>
        internal readonly string Mode;
        /// <summary>来源姿态键。</summary>
        internal readonly string Stance;
        /// <summary>本次有效芯片主图像的最终绘制尺寸；无视觉时为 1。</summary>
        internal readonly float VisualSize;
        /// <summary>复制本次播放所需的最小事实。</summary>
        internal FlashingBladesPlaybackSelection(string key, object category, object texture, string result, string chip, string mode, string stance, float visualSize)
        { ConfigKey = key; Category = category; Texture = texture; ResultId = result; ChipId = chip; Mode = mode; Stance = stance; VisualSize = visualSize; }
    }

    /// <summary>沿用外部唯一播放链，只替换 BDP 的分类与手动纹理参数。</summary>
    internal static class FlashingBladesRuntimeBridge
    {
        /// <summary>当前外部接口。</summary>
        private static FlashingBladesDiscovery targets;
        /// <summary>共享只读定义目录。</summary>
        private static FlashingBladesCatalog catalog;
        /// <summary>手动图像的私有效果副本；只在正式生成入口使用。</summary>
        private static FlashingBladesTextureProfiles textureProfiles;
        /// <summary>核心正式视觉选择的只读桥。</summary>
        private static FlashingBladesVisualSource visualSource;
        /// <summary>适配器独立熔断处理。</summary>
        private static Action<Exception> fail;
        /// <summary>上下文释放即自动释放，不按角色或当前状态共用播放选择。</summary>
        private static ConditionalWeakTable<object, FlashingBladesPlaybackSelection> selections = new ConditionalWeakTable<object, FlashingBladesPlaybackSelection>();

        /// <summary>绑定或清除本适配器；禁用后不保留旧上下文。</summary>
        internal static void Bind(FlashingBladesDiscovery discovery, FlashingBladesCatalog entries, Action<Exception> failure)
        {
            targets = discovery; catalog = entries; fail = failure;
            textureProfiles = discovery != null ? new FlashingBladesTextureProfiles(discovery) : null;
            visualSource = discovery != null ? new FlashingBladesVisualSource(discovery) : null;
            selections = new ConditionalWeakTable<object, FlashingBladesPlaybackSelection>();
        }

        /// <summary>原版上下文建成后读取真实攻击来源；非 BDP 完整沿用原链。</summary>
        internal static void BuildPostfix(Verb_MeleeAttack verb, object __result)
        {
            BdpVerb_MeleeAttackDamage bdp = verb as BdpVerb_MeleeAttackDamage;
            if (targets == null || bdp == null || __result == null) return;
            try
            {
                if (!(bool)targets.Get(__result, "valid")) return;
                ExpressionPublishedResultSnapshot result;
                Thing chip = null;
                string mode = null, stance = null;
                ChipSourceReferenceSnapshot source = null;
                ITriggerLoadoutReader loadout = TriggerSurfaceAccess.ResolveLoadoutReader(bdp.CasterPawn);
                if (loadout != null && ExpressionSurfaceAccess.TryGetPublishedResult(bdp.CasterPawn, bdp.ResultId, out result) && result.SourceReference != null)
                {
                    ExpressionPublishedSourceReference reference = result.SourceReference;
                    ITriggerSlotState slot = loadout.GetAllSlots().FirstOrDefault(value => value.Side == reference.Side && value.Index == reference.SlotIndex
                        && value.LoadedChip != null && value.LoadedChip.ThingID == reference.ChipThingId);
                    chip = slot != null ? slot.LoadedChip : null;
                    if (chip != null)
                    {
                        source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
                        mode = loadout.GetChipModeKey(chip);
                        stance = loadout.GetChipStanceKey(chip);
                    }
                }
                string function = source != null && source.OrderedSourceKeys != null
                    ? (source.OrderedSourceKeys.Count == 1 ? source.OrderedSourceKeys[0]
                        : source.OrderedSourceKeys.FirstOrDefault(value => string.Equals(value, mode, StringComparison.OrdinalIgnoreCase))) : null;
                Capture(__result, function, source != null ? source.SourceVariantKey : null, mode, stance, bdp.ResultId, chip != null ? chip.ThingID : null, visualSource.Read(bdp, chip));
            }
            catch (Exception error) { fail(error); }
        }

        /// <summary>仅冻结本次选择；失配用当前真实工具，不读取触发体覆盖。</summary>
        internal static void Capture(object context, string function, string form, string mode, string stance, string result, string chip, float visualSize)
        {
            FlashingBladesEntry entry;
            object category;
            bool matched = catalog.TrySelectAttack(function, form, mode, stance, result, chip, out entry, out category);
            if (!matched) category = targets.ToolCategory((Tool)targets.Get(context, "tool"), (DamageDef)targets.Get(context, "damageDef"));
            object texture = targets.Unknown;
            if (matched) targets.TryOverride(entry.Key, out texture);
            selections.Remove(context);
            selections.Add(context, new FlashingBladesPlaybackSelection(matched ? entry.Key : null, category, texture, result, chip, mode, stance, visualSize));
        }

        /// <summary>读取冻结选择，也供可插拔开发诊断反射观察。</summary>
        internal static FlashingBladesPlaybackSelection ResolveSelection(object context)
        {
            FlashingBladesPlaybackSelection selection;
            return context != null && selections.TryGetValue(context, out selection) ? selection : null;
        }

        /// <summary>创建独立分类结果，不修改外部按装备定义缓存的结果对象。</summary>
        internal static bool ClassifyPrefix(object context, ref object __result)
        {
            FlashingBladesPlaybackSelection selection = ResolveSelection(context);
            if (targets == null || selection == null) return true;
            try
            {
                object result = Activator.CreateInstance(targets.ResultType);
                targets.Set(result, "category", selection.Category);
                targets.Set(result, "source", "BDP");
                targets.Set(result, "detail", selection.ConfigKey ?? "BDP.FB.unmatched");
                __result = result;
                return false;
            }
            catch (Exception error) { fail(error); return true; }
        }

        /// <summary>生成仍受原开关、阵营、概率、排除及去重控制；不另起播放事件。</summary>
        internal static void SpawnPrefix(object[] __args)
        {
            FlashingBladesPlaybackSelection selection = ResolveSelection(__args[1]);
            if (targets == null || selection == null) return;
            try
            {
                if (__args[0] != null && !Equals(selection.Texture, targets.Unknown))
                {
                    __args[0] = textureProfiles.Resolve(__args[0], selection.Texture);
                    // 图像已固定在私有定义上，绕过外部生成后立即还原共享图像的失效路径。
                    __args[2] = targets.Unknown;
                }
                else __args[2] = selection.Texture;
            }
            catch (Exception error) { fail(error); }
        }

        /// <summary>BDP 正式近战由自身执行距离；其它武器仍沿用外部扩距。</summary>
        internal static bool ReachPrefix(Verb verb, object[] __args, ref bool __result)
        {
            if (targets == null || !(verb is BdpVerb_MeleeAttackDamage)) return true;
            __args[1] = null;
            __args[2] = 0f;
            __result = false;
            return false;
        }

        /// <summary>仅 BDP 装备类刀光改读冻结尺寸；保留动态与全局开关，不继承触发体扩距倍率。</summary>
        internal static bool ScalePrefix(object __0, object __1, ref float __result)
        {
            FlashingBladesPlaybackSelection selection = ResolveSelection(__1);
            if (targets == null || selection == null || __0 == null) return true;
            try
            {
                if (!(bool)targets.Invoke("UsesSize", targets.Get(__0, "category"))) return true;
                object settings = targets.Get(targets.ForeignType("DGJY_Mod"), "Settings");
                bool dynamic = settings == null || (bool)targets.Get(settings, "dynamicWeaponScale");
                float global = settings != null ? (float)targets.Get(settings, "globalScaleMultiplier") : 1f;
                global = Math.Max(0.5f, Math.Min(1.5f, global));
                float size = (dynamic ? selection.VisualSize : 1f) * global;
                __result = float.IsNaN(size) || float.IsInfinity(size) || size <= 0f ? 1f : size;
                targets.Set(__1, "debugScaleFactor", __result);
                targets.Set(__1, "debugScaleSummary", "BDP_FB_VisualScaleDiagnostic".Translate(selection.VisualSize.ToString("0.##"), __result.ToString("0.##")).ToString());
                return false;
            }
            catch (Exception error) { fail(error); return true; }
        }
    }
}
