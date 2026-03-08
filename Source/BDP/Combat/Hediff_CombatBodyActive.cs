using BDP.Core;
using System.Collections.Generic;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体激活Hediff自定义类（v13.1重构：破裂检测轮询）
    ///
    /// 职责:
    /// - 承载战斗体激活状态
    /// - 通过HediffStage配置效果覆写(preventsDeath, totalBleedFactor等)
    /// - 轮询检测破裂条件（关键部位摧毁 → 倒地 → Trion耗尽）
    ///
    /// 重构说明:
    /// - 替代原HediffComp_CombatBodyDamageInterceptor
    /// - 不再拦截伤害,只覆写效果
    /// - 借鉴Biotech基因效果模式
    /// - v13.1: 从HediffComp_RuptureMonitor接管破裂检测职责
    /// </summary>
    public class Hediff_CombatBodyActive : HediffWithComps
    {
        /// <summary>
        /// 是否已触发破裂（防止重复触发）
        /// </summary>
        private bool ruptureTriggered = false;

        /// <summary>
        /// 激活时已知的缺失部位集合（用于区分真身已残缺 vs 战斗中新缺失）
        /// </summary>
        private HashSet<int> knownMissingParts = new HashSet<int>();

        /// <summary>
        /// 关键部位列表（从XML配置读取）
        /// </summary>
        private List<string> criticalParts;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            // 从HediffDef读取关键部位配置
            LoadCriticalPartsConfig();

            // 记录激活时已有的缺失部位
            RecordExistingMissingParts();
        }

        public override void Tick()
        {
            base.Tick();

            // 如果已触发破裂，停止检测
            if (ruptureTriggered) return;

            // 按正确顺序检测破裂条件（优先级：关键部位 > 倒地 > Trion耗尽）
            if (CheckCriticalPartDestruction())
            {
                return; // 已触发破裂
            }

            if (CheckPawnDowned())
            {
                return; // 已触发破裂
            }

            if (CheckTrionDepleted())
            {
                return; // 已触发破裂
            }
        }

        /// <summary>
        /// 从HediffDef的modExtensions中读取关键部位配置
        /// </summary>
        private void LoadCriticalPartsConfig()
        {
            var ext = def.GetModExtension<HediffExtension_RuptureConfig>();
            criticalParts = ext?.criticalParts ?? new List<string>();

            if (criticalParts.Count == 0)
            {
                Log.Warning($"[BDP] Hediff_CombatBodyActive: 未找到关键部位配置（缺少HediffExtension_RuptureConfig）");
            }
        }

        /// <summary>
        /// 记录激活时已有的缺失部位
        /// </summary>
        private void RecordExistingMissingParts()
        {
            knownMissingParts.Clear();

            if (pawn?.health?.hediffSet == null) return;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart missingPart && missingPart.Part != null)
                {
                    knownMissingParts.Add(missingPart.Part.Index);
                }
            }
        }

        /// <summary>
        /// 检查关键部位是否被摧毁（优先级1）
        /// </summary>
        private bool CheckCriticalPartDestruction()
        {
            if (criticalParts == null || criticalParts.Count == 0) return false;
            if (pawn?.health?.hediffSet == null) return false;

            var corePart = pawn.RaceProps?.body?.corePart;

            // 检查躯干HP（corePart原版不生成MissingPart，改用HP=0检测）
            if (corePart != null && criticalParts.Contains(corePart.def.defName))
            {
                if (pawn.health.hediffSet.GetPartHealth(corePart) <= 0f)
                {
                    TriggerRupture($"关键部位 {corePart.def.defName} HP归零");
                    return true;
                }
            }

            // 检查其他关键部位的新MissingPart
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart missingPart && missingPart.Part != null)
                {
                    // 检查是否是新的缺失部位（不在已知集合中）
                    if (!knownMissingParts.Contains(missingPart.Part.Index))
                    {
                        string partDefName = missingPart.Part.def.defName;
                        if (criticalParts.Contains(partDefName))
                        {
                            TriggerRupture($"关键部位 {partDefName} 被摧毁");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查Pawn是否倒地（优先级2）
        /// </summary>
        private bool CheckPawnDowned()
        {
            if (pawn?.Downed == true)
            {
                TriggerRupture("倒地");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查Trion是否耗尽（优先级3）
        /// </summary>
        private bool CheckTrionDepleted()
        {
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion != null && compTrion.Cur <= 0f)
            {
                TriggerRupture("Trion耗尽");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 触发破裂流程
        /// </summary>
        private void TriggerRupture(string reason)
        {
            if (ruptureTriggered) return;
            ruptureTriggered = true;

            Log.Message($"[BDP] Hediff_CombatBodyActive: Pawn {pawn} 触发破裂 (原因: {reason})");

            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime != null)
            {
                CombatBodyOrchestrator.TriggerCollapse(pawn, runtime, reason);
            }
            else
            {
                Log.Error($"[BDP] Hediff_CombatBodyActive: Pawn {pawn} 没有CombatBodyRuntime");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ruptureTriggered, "ruptureTriggered", false);
            Scribe_Collections.Look(ref knownMissingParts, "knownMissingParts", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (knownMissingParts == null)
                {
                    knownMissingParts = new HashSet<int>();
                }

                // 读档后重新加载配置
                LoadCriticalPartsConfig();
            }
        }
    }
}
