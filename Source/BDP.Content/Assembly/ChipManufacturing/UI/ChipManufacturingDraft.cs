using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using UnityEngine;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>玩家在一条分类/职业路径下尚未入队的编辑草稿。</summary>
    public sealed class ChipManufacturingDraft
    {
        /// <summary>当前草稿直接编辑的统一组合记录。</summary>
        public ChipCombinationRecord Record { get; }

        /// <summary>该路径独立保留的制造数量。</summary>
        public int Quantity { get; private set; } = 1;

        /// <summary>按草稿键建立空选择。</summary>
        public ChipManufacturingDraft(ChipManufacturingDraftKey key)
        {
            Record = new ChipCombinationRecord
            {
                CategoryDefName = key.CategoryDefName,
                ProfessionDefName = key.ProfessionDefName
            };
        }

        /// <summary>复用集中规则选择动作；单形态会直接替换旧动作。</summary>
        public bool TrySelectAction(
            ChipProfessionDef profession,
            ChipActionPresetDef action,
            out string failureCode)
        {
            return ChipCombinationSelectionRules.TrySelect(
                Record,
                profession,
                action,
                out failureCode);
        }

        /// <summary>取消指定形态；取消第一项时第二项自动前移。</summary>
        public void RemoveActionAt(int index)
        {
            ChipCombinationSelectionRules.RemoveAt(Record, index);
        }

        /// <summary>交换恰好两个动作的形态顺序。</summary>
        public bool SwapActions()
        {
            return ChipCombinationSelectionRules.Swap(Record);
        }

        /// <summary>选择或清空武装型；若新型只允许单动作则保留首个动作。</summary>
        public void SelectArmamentForm(ChipArmamentFormDef armamentForm)
        {
            Record.ArmamentFormDefName = armamentForm?.defName;
            int maximum = ChipCombinationSelectionRules.MaxActionCount(null, armamentForm);
            while (Record.OrderedActionPresetDefNames.Count > maximum)
            {
                Record.OrderedActionPresetDefNames.RemoveAt(
                    Record.OrderedActionPresetDefNames.Count - 1);
            }
        }

        /// <summary>把制造数量限制在玩家可操作的正整数范围。</summary>
        public void SetQuantity(int quantity)
        {
            Quantity = Mathf.Clamp(quantity, 1, 999);
        }

        /// <summary>从现有账单复制配置到当前草稿，不持有原账单记录引用。</summary>
        public void LoadFrom(ChipCombinationRecord source, int quantity)
        {
            if (source == null)
            {
                return;
            }

            ChipCombinationRecord copy = source.Clone();
            Record.CategoryDefName = copy.CategoryDefName;
            Record.ProfessionDefName = copy.ProfessionDefName;
            Record.ArmamentFormDefName = copy.ArmamentFormDefName;
            Record.LastResolvedLabel = copy.LastResolvedLabel;
            Record.OrderedActionPresetDefNames = copy.OrderedActionPresetDefNames;
            SetQuantity(Mathf.Max(1, quantity));
        }
    }
}
