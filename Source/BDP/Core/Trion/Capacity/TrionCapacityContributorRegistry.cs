using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trion.Capacity
{
    /// <summary>
    /// Trion 最大容量贡献提供器注册表。
    /// 没有业务程序集接入时返回零，保证 Core 保持原有容量行为。
    /// </summary>
    public static class TrionCapacityContributorRegistry
    {
        /// <summary>
        /// 当前已经注册的容量贡献提供器。
        /// </summary>
        private static readonly List<ITrionCapacityContributor> contributors =
            new List<ITrionCapacityContributor>();

        /// <summary>
        /// 注册一个容量贡献提供器；同一实例不会重复注册。
        /// </summary>
        /// <param name="contributor">待注册的容量贡献提供器。</param>
        public static void Register(ITrionCapacityContributor contributor)
        {
            if (contributor == null || contributors.Contains(contributor))
            {
                return;
            }

            contributors.Add(contributor);
        }

        /// <summary>
        /// 只清除指定实例，避免旧程序集清理时误删新提供器。
        /// </summary>
        /// <param name="contributor">准备清除的容量贡献提供器。</param>
        public static void Clear(ITrionCapacityContributor contributor)
        {
            if (contributor == null)
            {
                return;
            }

            contributors.Remove(contributor);
        }

        /// <summary>
        /// 汇总指定 Pawn 的额外 Trion 容量。
        /// 单个提供器异常时跳过该提供器，避免属性读取链整体中断。
        /// </summary>
        /// <param name="pawn">正在读取容量的 Pawn。</param>
        /// <returns>经过安全钳制的额外容量。</returns>
        public static float ResolveAdditionalCapacity(Pawn pawn)
        {
            if (pawn == null || contributors.Count == 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < contributors.Count; i++)
            {
                ITrionCapacityContributor contributor = contributors[i];
                if (contributor == null)
                {
                    continue;
                }

                try
                {
                    float value = contributor.GetAdditionalCapacity(pawn);
                    if (value > 0f && !float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        total += value;
                    }
                }
                catch (System.Exception exception)
                {
                    Log.ErrorOnce(
                        "[BDP.Trion] 容量贡献提供器读取失败：" + exception,
                        17432101 + i);
                }
            }

            if (float.IsNaN(total) || float.IsInfinity(total))
            {
                return 0f;
            }

            return total > 0f ? total : 0f;
        }
    }
}
