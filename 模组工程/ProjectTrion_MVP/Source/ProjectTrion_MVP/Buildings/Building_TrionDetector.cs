using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using ProjectTrion.Core;
using ProjectTrion.Components;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion天赋检测仪
    ///
    /// 功能：
    /// 1. 玩家与建筑交互，选择要扫描的Pawn
    /// 2. 首次扫描时：随机生成天赋等级，存储到Pawn.modData，重算Capacity
    /// 3. 再次扫描时：直接返回已有天赋，不再生成
    /// 4. 显示扫描结果（天赋等级、Capacity值）
    ///
    /// 这是MVP中天赋初始化的唯一触发点，避免了全局补丁
    /// </summary>
    public class Building_TrionDetector : Building
    {
        private Pawn _lastScannedPawn = null;
        private TalentGrade? _lastScannedTalent = null;

        /// <summary>
        /// 获取建筑的交互菜单（右键菜单）
        /// 允许玩家选择要扫描的Pawn
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            // 检查Map中是否有可扫描的Pawn（需要有Hediff_TrionGland）
            bool canUse = Map != null && HasScannablePawn();
            string disabledReason = "";

            if (Map == null)
                disabledReason = "无法访问地图";
            else if (!canUse)
                disabledReason = "地图上没有植入Trion腺体的Pawn";

            // 添加"扫描Pawn"按钮
            if (!canUse)
            {
                yield return new Command_Action
                {
                    defaultLabel = "扫描Pawn",
                    defaultDesc = $"选择一个Pawn进行天赋扫描（需要有Trion腺体）\n\n【无法使用】{disabledReason}",
                    icon = TexCommand.Attack,
                    action = () => Messages.Message($"无法扫描：{disabledReason}", MessageTypeDefOf.RejectInput, historical: false)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "扫描Pawn",
                    defaultDesc = "选择一个Pawn进行天赋扫描（需要有Trion腺体）",
                    icon = TexCommand.Attack,
                    action = delegate
                    {
                        OpenScanMenu();
                    }
                };
            }
        }

        /// <summary>
        /// 检查是否有可扫描的Pawn（有Hediff_TrionGland的Pawn）
        /// </summary>
        private bool HasScannablePawn()
        {
            if (Map == null)
                return false;

            HediffDef trionGlandDef = DefDatabase<HediffDef>.GetNamed("Hediff_TrionGland", false);
            if (trionGlandDef == null)
                return false;

            foreach (var pawn in Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.health.hediffSet.HasHediff(trionGlandDef))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 打开Pawn选择菜单
        /// </summary>
        private void OpenScanMenu()
        {
            if (Map == null)
                return;

            var options = new System.Collections.Generic.List<FloatMenuOption>();
            HediffDef trionGlandDef = DefDatabase<HediffDef>.GetNamed("Hediff_TrionGland", false);

            // 列出所有可扫描的Pawn（有Hediff_TrionGland的Pawn）
            foreach (var pawn in Map.mapPawns.AllPawnsSpawned)
            {
                if (trionGlandDef == null || !pawn.health.hediffSet.HasHediff(trionGlandDef))
                    continue; // 跳过没有Trion腺体的Pawn

                string label = $"扫描: {pawn.NameShortColored}";
                options.Add(new FloatMenuOption(label, delegate
                {
                    ScanPawn(pawn);
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("(没有可扫描的Pawn - 需要有Trion腺体)", null));
            }

            var floatMenu = new FloatMenu(options);
            Find.WindowStack.Add(floatMenu);
        }

        /// <summary>
        /// 扫描指定Pawn的天赋
        ///
        /// 流程：
        /// 1. 检查是否有CompTrion
        /// 2. 读取已有天赋或生成新天赋
        /// 3. 存储天赋到Pawn.modData
        /// 4. 重算Capacity
        /// 5. 显示结果
        /// </summary>
        private void ScanPawn(Pawn pawn)
        {
            if (pawn == null)
                return;

            var comp = pawn.GetComp<CompTrion>();
            if (comp == null)
            {
                Messages.Message($"扫描失败：{pawn.NameShortColored} 没有Trion腺体",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            try
            {
                TalentGrade? talent = GetOrGenerateTalent(pawn);

                if (talent == null)
                {
                    Log.Error($"[Trion] 无法生成天赋，扫描失败");
                    return;
                }

                // 重算Capacity
                RecalculateCapacityFromTalent(comp, talent.Value);

                // 保存扫描结果，用于UI显示
                _lastScannedPawn = pawn;
                _lastScannedTalent = talent;

                // 显示扫描结果信息
                string message = $"[Trion检测仪] {pawn.NameShortColored} 的扫描结果：\n" +
                    $"天赋等级: {FormatTalent(talent.Value)}\n" +
                    $"Trion容量: {comp.Capacity:F0}";

                Messages.Message(message, pawn, MessageTypeDefOf.PositiveEvent);

                Log.Message($"[Trion] {pawn.NameShortColored} 天赋: {talent}, Capacity: {comp.Capacity}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Trion] 扫描 {pawn.NameShortColored} 时发生错误: {ex}");
                Messages.Message($"扫描失败：{ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        /// <summary>
        /// 获取或生成天赋
        ///
        /// 从Hediff_TrionGland读取天赋。
        /// 首次调用：生成随机天赋，存储到Hediff_TrionGland.TakenTalent
        /// 再次调用：从Hediff_TrionGland读取已有天赋
        /// </summary>
        private TalentGrade? GetOrGenerateTalent(Pawn pawn)
        {
            // 查找Hediff_TrionGland
            var hediff = pawn.health.hediffSet.hediffs
                .OfType<ProjectTrion_MVP.Hediff_TrionGland>()
                .FirstOrDefault();

            if (hediff == null)
                return null;

            // 检查是否已有天赋记录
            if (hediff.TalentInitialized)
            {
                // 已初始化过，返回已保存的天赋
                Log.Message($"[Trion] {pawn.NameShortColored} 已有天赋: {hediff.TakenTalent}（非首次扫描）");
                return hediff.TakenTalent;
            }

            // 首次扫描：随机生成天赋并存储
            var randomTalent = GenerateRandomTalent();
            hediff.TakenTalent = randomTalent;
            hediff.TalentInitialized = true;

            Log.Message($"[Trion] {pawn.NameShortColored} 首次扫描，生成天赋: {randomTalent}");
            return randomTalent;
        }

        /// <summary>
        /// 随机生成天赋等级
        /// MVP中均匀分布在S-E之间
        /// </summary>
        private TalentGrade GenerateRandomTalent()
        {
            // MVP中均匀分布：S/A/B/C/D/E各1/6概率
            int randomValue = Rand.Range(1, 7); // 1-6
            return (TalentGrade)randomValue;
        }

        /// <summary>
        /// 根据天赋重算Capacity
        /// 通过CompTrion.TalentCapacityProvider委托查表
        /// </summary>
        private void RecalculateCapacityFromTalent(CompTrion comp, TalentGrade talent)
        {
            // 调用框架提供的委托来计算容量
            if (CompTrion.TalentCapacityProvider != null)
            {
                float newCapacity = CompTrion.TalentCapacityProvider(talent);
                comp.Capacity = newCapacity;
                Log.Message($"[Trion] Capacity已重算: {talent} → {newCapacity}");
            }
            else
            {
                Log.Warning("[Trion] TalentCapacityProvider未设置，Capacity保持不变");
            }
        }

        /// <summary>
        /// 将天赋等级格式化为可读字符串
        /// </summary>
        private string FormatTalent(TalentGrade talent)
        {
            return talent switch
            {
                TalentGrade.S => "S（精英）",
                TalentGrade.A => "A（高级）",
                TalentGrade.B => "B（中高）",
                TalentGrade.C => "C（普通）",
                TalentGrade.D => "D（中低）",
                TalentGrade.E => "E（新兵）",
                _ => "未知"
            };
        }
    }
}
