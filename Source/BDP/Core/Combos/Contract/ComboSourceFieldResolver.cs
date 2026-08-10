using System.Collections.Generic;

namespace BDP.Core.Combos
{
    /// <summary>
    /// Combo 来源字段统一求值器。
    /// 它只负责字段级取值与计算，不关心字段背后的业务含义。
    /// </summary>
    internal static class ComboSourceFieldResolver
    {
        /// <summary>
        /// 解析一个 float 字段。
        /// </summary>
        internal static ComboResolvedFieldValue<float> ResolveFloat(
            float? explicitValue,
            ComboValueResolveMode? mode,
            float chipAValue,
            float chipBValue)
        {
            ComboResolvedFieldValue<float> result = new ComboResolvedFieldValue<float>
            {
                HasExplicitValue = explicitValue.HasValue,
                ExplicitValue = explicitValue.HasValue ? explicitValue.Value : 0f,
                ResolveMode = mode
            };

            if (explicitValue.HasValue)
            {
                result.HasResolvedValue = true;
                result.ResolvedValue = explicitValue.Value;
                return result;
            }

            if (!mode.HasValue)
            {
                return result;
            }

            result.HasResolvedValue = true;
            result.ResolvedValue = ResolveFloatByMode(mode.Value, chipAValue, chipBValue);
            return result;
        }

        /// <summary>
        /// 解析一个 int 字段。
        /// </summary>
        internal static ComboResolvedFieldValue<int> ResolveInt(
            int? explicitValue,
            ComboValueResolveMode? mode,
            int chipAValue,
            int chipBValue)
        {
            ComboResolvedFieldValue<int> result = new ComboResolvedFieldValue<int>
            {
                HasExplicitValue = explicitValue.HasValue,
                ExplicitValue = explicitValue.HasValue ? explicitValue.Value : 0,
                ResolveMode = mode
            };

            if (explicitValue.HasValue)
            {
                result.HasResolvedValue = true;
                result.ResolvedValue = explicitValue.Value;
                return result;
            }

            if (!mode.HasValue)
            {
                return result;
            }

            result.HasResolvedValue = true;
            result.ResolvedValue = ResolveIntByMode(mode.Value, chipAValue, chipBValue);
            return result;
        }

        /// <summary>
        /// 解析一个 string 字段。
        /// 只有显式值和单侧跟随模式是正式支持的。
        /// </summary>
        internal static string ResolveString(
            string explicitValue,
            ComboValueResolveMode? mode,
            string chipAValue,
            string chipBValue)
        {
            if (explicitValue != null)
            {
                return explicitValue;
            }

            if (!mode.HasValue)
            {
                return null;
            }

            switch (mode.Value)
            {
                case ComboValueResolveMode.FollowChipMain:
                    return chipAValue;
                case ComboValueResolveMode.FollowChipSub:
                    return chipBValue;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 解析一个列表字段。
        /// 只有显式值和单侧跟随模式是正式支持的。
        /// </summary>
        internal static IReadOnlyList<TValue> ResolveList<TValue>(
            IReadOnlyList<TValue> explicitValue,
            ComboValueResolveMode? mode,
            IReadOnlyList<TValue> chipAValue,
            IReadOnlyList<TValue> chipBValue)
        {
            if (explicitValue != null)
            {
                return CloneList(explicitValue);
            }

            if (!mode.HasValue)
            {
                return null;
            }

            switch (mode.Value)
            {
                case ComboValueResolveMode.FollowChipMain:
                    return CloneList(chipAValue);
                case ComboValueResolveMode.FollowChipSub:
                    return CloneList(chipBValue);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 按模式计算 float 值。
        /// </summary>
        private static float ResolveFloatByMode(ComboValueResolveMode mode, float chipAValue, float chipBValue)
        {
            switch (mode)
            {
                case ComboValueResolveMode.FollowChipMain:
                    return chipAValue;
                case ComboValueResolveMode.FollowChipSub:
                    return chipBValue;
                case ComboValueResolveMode.Sum:
                    return chipAValue + chipBValue;
                case ComboValueResolveMode.Max:
                    return chipAValue > chipBValue ? chipAValue : chipBValue;
                case ComboValueResolveMode.Min:
                    return chipAValue < chipBValue ? chipAValue : chipBValue;
                case ComboValueResolveMode.Average:
                default:
                    return (chipAValue + chipBValue) * 0.5f;
            }
        }

        /// <summary>
        /// 按模式计算 int 值。
        /// </summary>
        private static int ResolveIntByMode(ComboValueResolveMode mode, int chipAValue, int chipBValue)
        {
            switch (mode)
            {
                case ComboValueResolveMode.FollowChipMain:
                    return chipAValue;
                case ComboValueResolveMode.FollowChipSub:
                    return chipBValue;
                case ComboValueResolveMode.Sum:
                    return chipAValue + chipBValue;
                case ComboValueResolveMode.Max:
                    return chipAValue > chipBValue ? chipAValue : chipBValue;
                case ComboValueResolveMode.Min:
                    return chipAValue < chipBValue ? chipAValue : chipBValue;
                case ComboValueResolveMode.Average:
                default:
                    return (chipAValue + chipBValue) / 2;
            }
        }

        /// <summary>
        /// 对来源列表做最小浅复制，避免回写来源集合。
        /// </summary>
        private static IReadOnlyList<TValue> CloneList<TValue>(IReadOnlyList<TValue> source)
        {
            List<TValue> result = new List<TValue>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                result.Add(source[i]);
            }

            return result;
        }
    }
}
