using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information.Vanilla;
using BDP.Support.Diagnostics;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Information
{
    /// <summary>
    /// Development 侧的原版武器统计投影诊断器。
    /// 它只验证正式表面能否驱动原版读取链，不参与正式玩家信息卡。
    /// </summary>
    public static class VanillaWeaponStatProjectionDiagnostic
    {
        /// <summary>从当前选中的芯片读取正规化契约并运行一次诊断。</summary>
        [DebugAction(
            "Mods",
            "BDP：验证原版武器信息投影",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing,
            displayPriority = -100)]
        public static void RunForSelectedThing()
        {
            Thing selectedThing = Find.Selector?.SingleSelectedThing;
            VanillaWeaponStatProjectionDiagnosticReport report = Capture(selectedThing);
            WriteReport(report);
        }

        /// <summary>捕获当前芯片的原版武器投影诊断结果。</summary>
        public static VanillaWeaponStatProjectionDiagnosticReport Capture(Thing sourceThing)
        {
            VanillaWeaponStatProjectionDiagnosticReport report =
                new VanillaWeaponStatProjectionDiagnosticReport
                {
                    SourceThingId = sourceThing?.ThingID,
                    Samples = new List<VanillaWeaponStatProjectionSample>()
                };

            if (sourceThing == null)
            {
                report.FailureReason = "没有选中的芯片 Thing。";
                return report;
            }

            ChipExpressionResolvedContract resolved =
                ChipExpressionContractSurfaceAccess.Resolve(sourceThing, null);
            report.ContractValid = resolved?.Validation != null && resolved.Validation.IsValid;
            if (!report.ContractValid || resolved.Contract?.Entries == null)
            {
                report.FailureReason = "芯片表达契约无效或没有条目。";
                return report;
            }

            for (int index = 0; index < resolved.Contract.Entries.Count; index++)
            {
                ChipExpressionEntryContract entry = resolved.Contract.Entries[index];
                if (entry == null
                    || (!ChipExpressionCapabilityResolver.HasExecutableRangedSurface(entry)
                        && !ChipExpressionCapabilityResolver.HasExecutableMeleeSurface(entry)))
                {
                    continue;
                }

                VanillaWeaponStatProjectionSample sample =
                    CaptureSurface(sourceThing, entry);
                if (sample != null)
                {
                    report.Samples.Add(sample);
                }
            }

            if (report.Samples.Count == 0)
            {
                report.FailureReason = ChipExpressionCapabilityResolver.IsAugmentationOnly(resolved.Contract)
                    ? "该芯片只有远程增强模块，没有独立可执行武器。"
                    : "契约中没有可执行的近战或远程表面。";
            }

            for (int index = 0; index < report.Samples.Count; index++)
            {
                VanillaWeaponStatProjectionSample sample = report.Samples[index];
                if (sample != null
                    && sample.HasVanillaSurface
                    && !sample.TemporaryDefRegistered)
                {
                    report.HasUnregisteredTemporarySurfaces = true;
                    break;
                }
            }

            return report;
        }

        /// <summary>通过正式 Core 表面工厂与原版读取适配器捕获一条样本。</summary>
        private static VanillaWeaponStatProjectionSample CaptureSurface(
            Thing sourceThing,
            ChipExpressionEntryContract entry)
        {
            VanillaWeaponStatProjectionSample sample = CreateSample(
                sourceThing,
                entry,
                ChipExpressionCapabilityResolver.ResolveWeaponMode(entry).ToString());
            VanillaWeaponStatSurface surface;
            string failureReason;
            if (!VanillaWeaponStatSurfaceFactory.TryCreate(
                entry,
                out surface,
                out failureReason))
            {
                sample.FailureReason = failureReason;
                return sample;
            }

            IReadOnlyList<StatDrawEntry> entries;
            if (!VanillaWeaponStatSurfaceReader.TryRead(
                surface,
                out entries,
                out failureReason))
            {
                sample.FailureReason = failureReason;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    StatDrawEntry entryResult = entries[index];
                    if (entryResult != null)
                    {
                        sample.VanillaEntries.Add(
                            entryResult.LabelCap + "=" + entryResult.ValueString);
                    }
                }
            }

            sample.HasVanillaSurface = sample.VanillaEntries.Count > 0;
            sample.TemporaryDefRegistered =
                DefDatabase<ThingDef>.GetNamedSilentFail(surface.Definition.defName) != null;
            if (sample.TemporaryDefRegistered)
            {
                sample.FailureReason = "临时 ThingDef 意外进入 DefDatabase。";
            }

            return sample;
        }

        /// <summary>创建一条诊断样本，保留契约中的关键输入值。</summary>
        private static VanillaWeaponStatProjectionSample CreateSample(
            Thing sourceThing,
            ChipExpressionEntryContract entry,
            string mode)
        {
            return new VanillaWeaponStatProjectionSample
            {
                SourceThingId = sourceThing?.ThingID,
                EntryId = entry.Id,
                Mode = mode,
                Range = entry.ResolvedVerbSpec != null
                    ? entry.ResolvedVerbSpec.Range
                    : 0f,
                WarmupTime = entry.ResolvedVerbSpec != null
                    ? entry.ResolvedVerbSpec.WarmupTime
                    : 0f,
                ProjectileDefName = entry.ResolvedVerbSpec?.ProjectileDef?.defName,
                VanillaEntries = new List<string>()
            };
        }

        /// <summary>把结构化诊断结果写入 Development 日志接收面。</summary>
        private static void WriteReport(VanillaWeaponStatProjectionDiagnosticReport report)
        {
            if (report == null)
            {
                return;
            }

            string sourceKey = string.IsNullOrWhiteSpace(report.SourceThingId)
                ? "none"
                : report.SourceThingId;
            if (!string.IsNullOrWhiteSpace(report.FailureReason))
            {
                BdpDiagnostics.Once(
                    "vanilla_weapon_projection.failure." + sourceKey,
                    "原版武器投影诊断未完成 source=" + sourceKey
                        + " reason=" + report.FailureReason);
            }

            if (report.Samples == null)
            {
                return;
            }

            for (int index = 0; index < report.Samples.Count; index++)
            {
                VanillaWeaponStatProjectionSample sample = report.Samples[index];
                if (sample == null)
                {
                    continue;
                }

                string sampleKey = sourceKey + "." + sample.EntryId + "." + sample.Mode;
                string entries = sample.VanillaEntries != null
                    ? string.Join(";", sample.VanillaEntries.ToArray())
                    : string.Empty;
                BdpDiagnostics.Once(
                    "vanilla_weapon_projection.sample." + sampleKey,
                    "原版武器投影样本 source=" + sourceKey
                        + " entry=" + sample.EntryId
                        + " mode=" + sample.Mode
                        + " range=" + sample.Range
                        + " warmup=" + sample.WarmupTime
                        + " projectile=" + sample.ProjectileDefName
                        + " vanillaEntries=" + entries
                        + " failure=" + sample.FailureReason);
            }
        }
    }

    /// <summary>一次原版武器投影诊断的结构化结果。</summary>
    public sealed class VanillaWeaponStatProjectionDiagnosticReport
    {
        /// <summary>诊断来源 Thing 的稳定标识。</summary>
        public string SourceThingId;

        /// <summary>表达契约是否通过正规化校验。</summary>
        public bool ContractValid;

        /// <summary>是否至少建立过一个未注册临时投影表面。</summary>
        public bool HasUnregisteredTemporarySurfaces;

        /// <summary>没有样本时的原因。</summary>
        public string FailureReason;

        /// <summary>远程/近战投影样本列表。</summary>
        public List<VanillaWeaponStatProjectionSample> Samples;
    }

    /// <summary>一条表达条目对应的原版武器投影样本。</summary>
    public sealed class VanillaWeaponStatProjectionSample
    {
        /// <summary>来源 Thing 的稳定标识。</summary>
        public string SourceThingId;

        /// <summary>表达条目标识。</summary>
        public string EntryId;

        /// <summary>投影模式键。</summary>
        public string Mode;

        /// <summary>契约中的射程值。</summary>
        public float Range;

        /// <summary>契约中的预热时长。</summary>
        public float WarmupTime;

        /// <summary>契约中的投射物定义名。</summary>
        public string ProjectileDefName;

        /// <summary>原版生成的统计条目文本。</summary>
        public List<string> VanillaEntries;

        /// <summary>原版是否生成了可观测统计表面。</summary>
        public bool HasVanillaSurface;

        /// <summary>临时定义是否意外进入 DefDatabase。</summary>
        public bool TemporaryDefRegistered;

        /// <summary>当前样本失败原因。</summary>
        public string FailureReason;
    }
}
