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
            float firstSourceValue,
            float secondSourceValue)
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
            result.ResolvedValue = ResolveFloatByMode(mode.Value, firstSourceValue, secondSourceValue);
            return result;
        }

        /// <summary>
        /// 解析一个 int 字段。
        /// </summary>
        internal static ComboResolvedFieldValue<int> ResolveInt(
            int? explicitValue,
            ComboValueResolveMode? mode,
            int firstSourceValue,
            int secondSourceValue)
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
            result.ResolvedValue = ResolveIntByMode(mode.Value, firstSourceValue, secondSourceValue);
            return result;
        }

        /// <summary>
        /// 解析一个 string 字段。
        /// 只有显式值和单侧跟随模式是正式支持的。
        /// </summary>
        internal static string ResolveString(
            string explicitValue,
            ComboValueResolveMode? mode,
            string firstSourceValue,
            string secondSourceValue)
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
                case ComboValueResolveMode.FollowFirstSource:
                    return firstSourceValue;
                case ComboValueResolveMode.FollowSecondSource:
                    return secondSourceValue;
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
            IReadOnlyList<TValue> firstSourceValue,
            IReadOnlyList<TValue> secondSourceValue)
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
                case ComboValueResolveMode.FollowFirstSource:
                    return CloneList(firstSourceValue);
                case ComboValueResolveMode.FollowSecondSource:
                    return CloneList(secondSourceValue);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 按模式计算 float 值。
        /// </summary>
        private static float ResolveFloatByMode(ComboValueResolveMode mode, float firstSourceValue, float secondSourceValue)
        {
            switch (mode)
            {
                case ComboValueResolveMode.FollowFirstSource:
                    return firstSourceValue;
                case ComboValueResolveMode.FollowSecondSource:
                    return secondSourceValue;
                case ComboValueResolveMode.Sum:
                    return firstSourceValue + secondSourceValue;
                case ComboValueResolveMode.Max:
                    return firstSourceValue > secondSourceValue ? firstSourceValue : secondSourceValue;
                case ComboValueResolveMode.Min:
                    return firstSourceValue < secondSourceValue ? firstSourceValue : secondSourceValue;
                case ComboValueResolveMode.Average:
                default:
                    return (firstSourceValue + secondSourceValue) * 0.5f;
            }
        }

        /// <summary>
        /// 按模式计算 int 值。
        /// </summary>
        private static int ResolveIntByMode(ComboValueResolveMode mode, int firstSourceValue, int secondSourceValue)
        {
            switch (mode)
            {
                case ComboValueResolveMode.FollowFirstSource:
                    return firstSourceValue;
                case ComboValueResolveMode.FollowSecondSource:
                    return secondSourceValue;
                case ComboValueResolveMode.Sum:
                    return firstSourceValue + secondSourceValue;
                case ComboValueResolveMode.Max:
                    return firstSourceValue > secondSourceValue ? firstSourceValue : secondSourceValue;
                case ComboValueResolveMode.Min:
                    return firstSourceValue < secondSourceValue ? firstSourceValue : secondSourceValue;
                case ComboValueResolveMode.Average:
                default:
                    return (firstSourceValue + secondSourceValue) / 2;
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
