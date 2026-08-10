using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 制造编辑器和解析器共用的动作选择规则。
    /// </summary>
    public static class ChipCombinationSelectionRules
    {
        /// <summary>枪手职业稳定 DefName。</summary>
        private const string GunnerProfessionDefName = "BDP_ChipProfession_Gunner";

        /// <summary>读取当前职业允许的最大动作数。</summary>
        public static int MaxActionCount(ChipProfessionDef profession)
        {
            return profession != null && profession.defName == GunnerProfessionDefName ? 2 : 1;
        }

        /// <summary>判断最终职业是否单向接纳动作的原生职业。</summary>
        public static bool CanUseAction(ChipProfessionDef profession, ChipActionPresetDef action)
        {
            if (action == null)
            {
                return false;
            }

            if (action.profession == null)
            {
                return profession == null;
            }

            return profession != null
                && profession.acceptedActionProfessions != null
                && profession.acceptedActionProfessions.Contains(action.profession);
        }

        /// <summary>判断动作预设自身是否已经包含多个形态。</summary>
        public static bool HasIntrinsicMultipleModes(ChipActionPresetDef action)
        {
            return action?.config?.Expression?.Modes != null
                && action.config.Expression.Modes.Count > 1;
        }

        /// <summary>
        /// 尝试选择动作；单选职业直接替换，枪手第三项不会自动顶替。
        /// </summary>
        public static bool TrySelect(
            ChipCombinationRecord record,
            ChipProfessionDef profession,
            ChipActionPresetDef action,
            out string failureCode)
        {
            failureCode = null;
            if (record == null || action == null || !CanUseAction(profession, action))
            {
                failureCode = "ProfessionMismatch";
                return false;
            }

            if (record.OrderedActionPresetDefNames == null)
            {
                record.OrderedActionPresetDefNames = new List<string>();
            }

            if (record.OrderedActionPresetDefNames.Contains(action.defName))
            {
                return true;
            }

            int maximum = MaxActionCount(profession);
            if (maximum == 1)
            {
                record.OrderedActionPresetDefNames.Clear();
                record.OrderedActionPresetDefNames.Add(action.defName);
                return true;
            }

            if (HasIntrinsicMultipleModes(action) && record.OrderedActionPresetDefNames.Count > 0)
            {
                failureCode = "IntrinsicMultiModeMustStandAlone";
                return false;
            }

            for (int index = 0; index < record.OrderedActionPresetDefNames.Count; index++)
            {
                ChipActionPresetDef selected =
                    ChipManufacturingDefLookup.FindAction(record.OrderedActionPresetDefNames[index]);
                if (HasIntrinsicMultipleModes(selected))
                {
                    failureCode = "IntrinsicMultiModeAlreadySelected";
                    return false;
                }
            }

            if (record.OrderedActionPresetDefNames.Count >= maximum)
            {
                failureCode = "ActionLimitReached";
                return false;
            }

            record.OrderedActionPresetDefNames.Add(action.defName);
            return true;
        }

        /// <summary>按索引取消动作；删除首项时后一项自然前移。</summary>
        public static void RemoveAt(ChipCombinationRecord record, int index)
        {
            if (record?.OrderedActionPresetDefNames == null
                || index < 0
                || index >= record.OrderedActionPresetDefNames.Count)
            {
                return;
            }

            record.OrderedActionPresetDefNames.RemoveAt(index);
        }

        /// <summary>交换恰好两个动作的形态顺序。</summary>
        public static bool Swap(ChipCombinationRecord record)
        {
            if (record?.OrderedActionPresetDefNames == null
                || record.OrderedActionPresetDefNames.Count != 2)
            {
                return false;
            }

            string first = record.OrderedActionPresetDefNames[0];
            record.OrderedActionPresetDefNames[0] = record.OrderedActionPresetDefNames[1];
            record.OrderedActionPresetDefNames[1] = first;
            return true;
        }
    }
}
