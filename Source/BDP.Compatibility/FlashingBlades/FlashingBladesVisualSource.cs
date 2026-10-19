using System;
using System.Reflection;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Visual;
using BDP.Core.Verbs;
using HarmonyLib;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>只读复用核心已发布视觉和绘制选择；不维护另一份功能/构型视觉规则。</summary>
    internal sealed class FlashingBladesVisualSource
    {
        /// <summary>已校验的成员访问器。</summary>
        private readonly FlashingBladesDiscovery targets;
        /// <summary>无状态的正式动作阶段解析器。</summary>
        private readonly object stageResolver;
        /// <summary>正式动作阶段解析入口。</summary>
        private readonly MethodInfo resolveStage;
        /// <summary>按实际攻击结果查找视觉归属，包含组合技归属。</summary>
        private readonly MethodInfo findEntry;
        /// <summary>正式双持/组合有效视觉选择。</summary>
        private readonly MethodInfo resolveEntry;
        /// <summary>正式绘制执行焦点判断。</summary>
        private readonly MethodInfo executionActive;
        /// <summary>单武器实际贴图条目选择。</summary>
        private readonly MethodInfo textureEntry;
        /// <summary>单武器实际贴图预设选择。</summary>
        private readonly MethodInfo texturePreset;

        /// <summary>缓存绘制请求类型，避免每次攻击查找。</summary>
        private static readonly Type RequestType = typeof(ExpressionVisualPresetDef).Assembly.GetType("BDP.Core.Trigger.Visual.VisualPoseRequest", true);
        /// <summary>核心绘制请求的已缓存主预设写入口。</summary>
        private static readonly PropertyInfo RequestPreset = RequestType.GetProperty("Preset");
        /// <summary>核心绘制请求的已缓存图像覆盖写入口。</summary>
        private static readonly PropertyInfo RequestOverlay = RequestType.GetProperty("GraphicOverridePreset");
        /// <summary>核心绘制请求的已缓存执行焦点写入口。</summary>
        private static readonly PropertyInfo RequestActive = RequestType.GetProperty("IsExecutionActive");
        /// <summary>核心绘制请求的已缓存来源芯片写入口。</summary>
        private static readonly PropertyInfo RequestChip = RequestType.GetProperty("SourceThing");
        /// <summary>与实际绘制共用主图像选择，包括阶段与局部覆盖。</summary>
        private static readonly MethodInfo MainGraphic = RequireMethod("BDP.Core.Trigger.Visual.VisualPoseResolver", "ResolveMainGraphic", typeof(Graphic), RequestType, typeof(WeaponVisualActionStage));

        /// <summary>一次性发现核心只读方法；失败时随适配器整体停用。</summary>
        internal FlashingBladesVisualSource(FlashingBladesDiscovery targets)
        {
            this.targets = targets;
            if (RequestPreset == null || RequestOverlay == null || RequestActive == null || RequestChip == null)
                throw new MissingMemberException(RequestType.FullName);
            Assembly core = typeof(ExpressionVisualPresetDef).Assembly;
            Type entry = core.GetType("BDP.Core.Expressions.VisualResidentEntry", true);
            Type projection = core.GetType("BDP.Core.Expressions.VisualExpressionProjection", true);
            Type state = core.GetType("BDP.Core.Trigger.Runtime.TriggerVisualRuntimeState", true);
            Type combat = core.GetType("BDP.Core.Trigger.Runtime.TriggerCombatProjectionState", true);
            Type snapshot = core.GetType("BDP.Core.Trigger.Visual.WeaponVisualStageSnapshot", true);
            Type resolver = core.GetType("BDP.Core.Trigger.Visual.WeaponVisualStageResolver", true);
            stageResolver = Activator.CreateInstance(resolver, true);
            resolveStage = RequireMethod(resolver.FullName, "Resolve", snapshot, typeof(Pawn), entry, combat, state);
            findEntry = RequireMethod("BDP.Core.Trigger.Visual.TriggerVisualEffectiveEntryResolver", "FindLaunchSourceEntry", entry, projection, typeof(string));
            resolveEntry = RequireMethod("BDP.Core.Trigger.Visual.TriggerVisualEffectiveEntryResolver", "TryResolve", typeof(bool), projection, entry, snapshot,
                entry.MakeByRefType(), typeof(ExpressionVisualPresetDef).MakeByRefType(), typeof(ExpressionVisualPresetDef).MakeByRefType());
            string draw = "BDP.Patches.Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual";
            executionActive = RequireMethod(draw, "ResolveExecutionActive", typeof(bool), projection, state, entry);
            textureEntry = RequireMethod(draw, "SelectTextureOnlyEntry", entry, projection);
            texturePreset = RequireMethod(draw, "ResolveTextureOnlyPreset", typeof(ExpressionVisualPresetDef), entry);
        }

        /// <summary>按完整签名及返回类型拒绝不匹配的核心接口。</summary>
        private static MethodInfo RequireMethod(string typeName, string name, Type returns, params Type[] parameters)
        {
            MethodInfo method = AccessTools.Method(typeof(ExpressionVisualPresetDef).Assembly.GetType(typeName, true), name, parameters);
            if (method == null || method.ReturnType != returns) throw new MissingMethodException(typeName, name);
            return method;
        }

        /// <summary>在攻击上下文建成时冻结有效主视觉尺寸；缺少视觉采用中性倍率。</summary>
        internal float Read(BdpVerb_MeleeAttackDamage verb, Thing chip)
        {
            CompTriggerBody trigger = verb.EquipmentSource != null ? verb.EquipmentSource.TryGetComp<CompTriggerBody>() : null;
            if (trigger == null) return 1f;
            object presentation = targets.Get(trigger, "PublishedPresentationProjection");
            object projection = presentation != null ? targets.Get(presentation, "VisualProjection") : null;
            return ReadProjection(projection, targets.Get(trigger, "PublishedCombatProjection"), targets.Get(trigger, "PublishedVisualRuntimeState"), verb.CasterPawn, verb.ResultId, chip);
        }

        /// <summary>主副手以结果归属分开；单武器复用实际绘制的主攻击贴图选择。</summary>
        internal float ReadProjection(object projection, object combat, object state, Pawn pawn, string resultId, Thing chip)
        {
            if (projection == null) return 1f;
            bool textureOnly = targets.Get(projection, "HostEquipmentRenderMode").ToString() == "ReplaceTextureOnly";
            object entry = textureOnly ? textureEntry.Invoke(null, new[] { projection }) : findEntry.Invoke(null, new object[] { projection, resultId });
            if (entry == null) return 1f;
            // 单武器可用同芯片主攻击图像；不能借另一芯片或另一只手的图像兜底。
            object source = targets.Get(entry, "SourceReference");
            if (textureOnly && (chip == null || source == null || !Equals(targets.Get(source, "ChipThingId"), chip.ThingID))) return 1f;
            object snapshot = resolveStage.Invoke(stageResolver, new[] { pawn, entry, combat, state });
            ExpressionVisualPresetDef preset, overlay = null;
            bool active = false;
            if (textureOnly) preset = (ExpressionVisualPresetDef)texturePreset.Invoke(null, new[] { entry });
            else
            {
                object[] args = { projection, entry, snapshot, null, null, null };
                if (!(bool)resolveEntry.Invoke(null, args)) return 1f;
                entry = args[3]; preset = (ExpressionVisualPresetDef)args[4]; overlay = (ExpressionVisualPresetDef)args[5];
                active = (bool)executionActive.Invoke(null, new[] { projection, state, entry });
            }
            return ResolveSize(preset, overlay, active, (WeaponVisualActionStage)targets.Get(snapshot, "Stage"), textureOnly, chip);
        }

        /// <summary>只读取实际主图像与绘制倍率，不需要绘制姿态样本，也不修改任何图像定义。</summary>
        internal static float ResolveSize(ExpressionVisualPresetDef preset, ExpressionVisualPresetDef overlay, bool active,
            WeaponVisualActionStage stage, bool textureOnly, Thing chip)
        {
            if (preset == null) return 1f;
            object request = Activator.CreateInstance(RequestType, true);
            RequestPreset.SetValue(request, preset, null);
            RequestOverlay.SetValue(request, overlay, null);
            RequestActive.SetValue(request, active, null);
            RequestChip.SetValue(request, chip, null);
            Graphic graphic = (Graphic)MainGraphic.Invoke(null, new[] { request, (object)stage });
            if (graphic == null) return 1f;
            float size = Math.Max(Math.Abs(graphic.drawSize.x), Math.Abs(graphic.drawSize.y)) * (textureOnly ? 1f : preset.ResolveDrawScale());
            return float.IsNaN(size) || float.IsInfinity(size) || size <= 0f ? 1f : size;
        }
    }
}
