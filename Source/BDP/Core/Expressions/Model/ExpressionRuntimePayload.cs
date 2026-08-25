using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达结果携带的运行参数包。
    /// 它把芯片本体、组合技来源和表达条目参数固定到正式结果上，供四类表达下游读取。
    /// </summary>
    internal sealed class ExpressionRuntimePayload
    {
        /// <summary>
        /// 当前结果的直接来源芯片。
        /// Combo 结果没有单一直接芯片时可为空。
        /// </summary>
        public ExpressionChipSnapshot SourceChip { get; set; }

        /// <summary>
        /// 当前结果关联的全部来源芯片快照。
        /// 单芯片结果通常只有一条，Combo 结果通常包含两侧来源。
        /// </summary>
        public IReadOnlyList<ExpressionChipSnapshot> SourceChips { get; set; }

        /// <summary>
        /// 当前结果若来自 Combo，这里记录 Combo 本体参数。
        /// </summary>
        public ExpressionComboSnapshot Combo { get; set; }

        /// <summary>
        /// 当前结果对应的表达条目参数。
        /// </summary>
        public ExpressionEntrySnapshot Entry { get; set; }

        /// <summary>
        /// 当前结果关联的来源引用集合。
        /// 它只保存可回溯坐标，不承载业务语义。
        /// </summary>
        public IReadOnlyList<ExpressionSourceReference> SourceReferences { get; set; }

        /// <summary>
        /// 克隆运行参数包，避免下游误改上游快照。
        /// </summary>
        public ExpressionRuntimePayload Clone()
        {
            return new ExpressionRuntimePayload
            {
                SourceChip = SourceChip != null ? SourceChip.Clone() : null,
                SourceChips = CloneChipSnapshots(SourceChips),
                Combo = Combo != null ? Combo.Clone() : null,
                Entry = Entry != null ? Entry.Clone() : null,
                SourceReferences = CloneSourceReferences(SourceReferences)
            };
        }

        /// <summary>
        /// 克隆芯片快照集合。
        /// </summary>
        private static IReadOnlyList<ExpressionChipSnapshot> CloneChipSnapshots(
            IReadOnlyList<ExpressionChipSnapshot> source)
        {
            List<ExpressionChipSnapshot> result = new List<ExpressionChipSnapshot>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    result.Add(source[i].Clone());
                }
            }

            return result;
        }

        /// <summary>
        /// 克隆来源引用集合。
        /// </summary>
        private static IReadOnlyList<ExpressionSourceReference> CloneSourceReferences(
            IReadOnlyList<ExpressionSourceReference> source)
        {
            List<ExpressionSourceReference> result = new List<ExpressionSourceReference>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ExpressionSourceReference reference = source[i];
                if (reference == null)
                {
                    continue;
                }

                result.Add(new ExpressionSourceReference
                {
                    ChipThingId = reference.ChipThingId,
                    ChipDefName = reference.ChipDefName,
                    Side = reference.Side,
                    SlotIndex = reference.SlotIndex
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 表达结果来源芯片的静态快照。
    /// </summary>
    internal sealed class ExpressionChipSnapshot
    {
        /// <summary>
        /// 来源芯片实例 ThingID。
        /// </summary>
        public string ChipThingId { get; set; }

        /// <summary>
        /// 来源芯片 DefName。
        /// </summary>
        public string ChipDefName { get; set; }

        /// <summary>
        /// 来源芯片所在侧别。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 来源芯片所在槽位序号。
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 芯片画像分类 DefName。
        /// 这里只保存已解析定义的稳定身份，不重新开放自由文本分类入口。
        /// </summary>
        public string ProfileCategoryDefName { get; set; }

        /// <summary>
        /// 芯片画像标签的稳定 DefName 快照。
        /// </summary>
        public IReadOnlyList<string> ProfileTagDefNames { get; set; }

        /// <summary>
        /// 芯片所属的装载槽位区域。
        /// </summary>
        public ChipSlotRegion? LoadoutSlotRegion { get; set; }

        /// <summary>
        /// 芯片对物理槽位的占用方式。
        /// </summary>
        public ChipSlotOccupancy? LoadoutSlotOccupancy { get; set; }

        /// <summary>
        /// 芯片本体级 Trion 参数。
        /// </summary>
        public ExpressionChipTrionSnapshot ChipTrion { get; set; }

        /// <summary>
        /// 芯片已声明块集合。
        /// </summary>
        public IReadOnlyList<ChipDefinitionDeclaredBlock> DeclaredBlocks { get; set; }

        /// <summary>
        /// 克隆芯片快照。
        /// </summary>
        public ExpressionChipSnapshot Clone()
        {
            return new ExpressionChipSnapshot
            {
                ChipThingId = ChipThingId,
                ChipDefName = ChipDefName,
                Side = Side,
                SlotIndex = SlotIndex,
                ProfileCategoryDefName = ProfileCategoryDefName,
                ProfileTagDefNames = ProfileTagDefNames != null
                    ? new List<string>(ProfileTagDefNames)
                    : new List<string>(),
                LoadoutSlotRegion = LoadoutSlotRegion,
                LoadoutSlotOccupancy = LoadoutSlotOccupancy,
                ChipTrion = ChipTrion != null ? ChipTrion.Clone() : null,
                DeclaredBlocks = CloneDeclaredBlocks(DeclaredBlocks)
            };
        }

        /// <summary>
        /// 克隆已声明块列表。
        /// </summary>
        private static IReadOnlyList<ChipDefinitionDeclaredBlock> CloneDeclaredBlocks(
            IReadOnlyList<ChipDefinitionDeclaredBlock> source)
        {
            return source != null
                ? new List<ChipDefinitionDeclaredBlock>(source)
                : new List<ChipDefinitionDeclaredBlock>();
        }
    }

    /// <summary>
    /// 芯片本体级 Trion 参数快照。
    /// </summary>
    internal sealed class ExpressionChipTrionSnapshot
    {
        /// <summary>
        /// 芯片常驻占用。
        /// </summary>
        public float CapacityCost { get; set; }

        /// <summary>
        /// 芯片激活成本。
        /// </summary>
        public float ActivationCost { get; set; }

        /// <summary>
        /// 克隆芯片本体 Trion 快照。
        /// </summary>
        public ExpressionChipTrionSnapshot Clone()
        {
            return new ExpressionChipTrionSnapshot
            {
                CapacityCost = CapacityCost,
                ActivationCost = ActivationCost
            };
        }
    }

    /// <summary>
    /// 表达条目级 Trion 参数快照。
    /// </summary>
    internal sealed class ExpressionSourceTrionSnapshot
    {
        /// <summary>
        /// 表达每次使用成本。
        /// </summary>
        public float UseCost { get; set; }

        /// <summary>
        /// 表达最低 Trion 要求。
        /// </summary>
        public float MinimumRequired { get; set; }

        /// <summary>
        /// 按最终有效来源数配置的整组表达每秒 Trion 总费用快照。
        /// </summary>
        public IReadOnlyList<ExpressionSustainCostBySourceCountConfig> SustainCostBySourceCount { get; set; }

        /// <summary>
        /// 克隆表达级 Trion 快照。
        /// </summary>
        public ExpressionSourceTrionSnapshot Clone()
        {
            return new ExpressionSourceTrionSnapshot
            {
                UseCost = UseCost,
                MinimumRequired = MinimumRequired,
                SustainCostBySourceCount = CloneSustainCostBySourceCount(SustainCostBySourceCount)
            };
        }

        /// <summary>
        /// 深复制持续费用档位，避免运行参数包共享可变 XML 配置对象。
        /// </summary>
        private static IReadOnlyList<ExpressionSustainCostBySourceCountConfig> CloneSustainCostBySourceCount(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> source)
        {
            List<ExpressionSustainCostBySourceCountConfig> result =
                new List<ExpressionSustainCostBySourceCountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ExpressionSustainCostBySourceCountConfig row = source[i];
                if (row == null)
                {
                    continue;
                }

                result.Add(new ExpressionSustainCostBySourceCountConfig
                {
                    SourceCount = row.SourceCount,
                    TotalPerSecond = row.TotalPerSecond
                });
            }

            return result;
        }
    }

    /// <summary>
    /// Combo 本体参数快照。
    /// </summary>
    internal sealed class ExpressionComboSnapshot
    {
        /// <summary>
        /// ComboDef 的 DefName。
        /// </summary>
        public string ComboDefName { get; set; }

        /// <summary>
        /// ComboDef 的显示标签。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Combo 声明的第一来源动作预设 DefName。
        /// </summary>
        public string FirstSourceActionDefName { get; set; }

        /// <summary>
        /// Combo 声明的第二来源动作预设 DefName。
        /// </summary>
        public string SecondSourceActionDefName { get; set; }

        /// <summary>
        /// Combo 本体级 Trion 参数。
        /// </summary>
        public ExpressionChipTrionSnapshot ChipTrion { get; set; }

        /// <summary>
        /// 克隆 Combo 快照。
        /// </summary>
        public ExpressionComboSnapshot Clone()
        {
            return new ExpressionComboSnapshot
            {
                ComboDefName = ComboDefName,
                Label = Label,
                FirstSourceActionDefName = FirstSourceActionDefName,
                SecondSourceActionDefName = SecondSourceActionDefName,
                ChipTrion = ChipTrion != null ? ChipTrion.Clone() : null
            };
        }
    }

    /// <summary>
    /// 表达条目运行参数快照。
    /// </summary>
    internal sealed class ExpressionEntrySnapshot
    {
        /// <summary>
        /// 表达条目稳定标识。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 表达结果类别。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 表达武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 表达显示标签。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 手动入口图标路径。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 单侧视觉图层局部覆盖预设 DefName。
        /// </summary>
        public string VisualGraphicOverrideDefName { get; set; }

        /// <summary>
        /// 复合视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 是否强制压制宿主装备贴图。
        /// </summary>
        public bool ForceSuppressHostEquipment { get; set; }

        /// <summary>
        /// 视觉优先级。
        /// </summary>
        public int VisualPriority { get; set; }

        /// <summary>
        /// 表达角色键。
        /// </summary>
        public string RoleKey { get; set; }

        /// <summary>
        /// Verb 主副身份。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; set; }

        /// <summary>
        /// 表达标签集合。
        /// </summary>
        public IReadOnlyList<string> Tags { get; set; }

        /// <summary>
        /// 表达级 Trion 参数。
        /// </summary>
        public ExpressionSourceTrionSnapshot ExpressionTrion { get; set; }

        /// <summary>
        /// 表达形态键。
        /// </summary>
        public string ModeKey { get; set; }

        /// <summary>
        /// AbilityDef 名称。
        /// </summary>
        public string AbilityDefName { get; set; }

        /// <summary>
        /// HediffDef 名称。
        /// </summary>
        public string HediffDefName { get; set; }

        /// <summary>
        /// Hediff 应用模式键。
        /// </summary>
        public string HediffApplyModeKey { get; set; }

        /// <summary>
        /// Passive 键。
        /// </summary>
        public string PassiveKey { get; set; }

        /// <summary>
        /// 克隆表达条目快照。
        /// </summary>
        public ExpressionEntrySnapshot Clone()
        {
            return new ExpressionEntrySnapshot
            {
                Id = Id,
                ResultKind = ResultKind,
                WeaponMode = WeaponMode,
                DisplayLabel = DisplayLabel,
                ManualEntryIconTexPath = ManualEntryIconTexPath,
                VisualPresetDefName = VisualPresetDefName,
                VisualGraphicOverrideDefName = VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = ForceSuppressHostEquipment,
                VisualPriority = VisualPriority,
                RoleKey = RoleKey,
                VerbAttackRole = VerbAttackRole,
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                ExpressionTrion = ExpressionTrion != null ? ExpressionTrion.Clone() : null,
                ModeKey = ModeKey,
                AbilityDefName = AbilityDefName,
                HediffDefName = HediffDefName,
                HediffApplyModeKey = HediffApplyModeKey,
                PassiveKey = PassiveKey
            };
        }
    }
}
