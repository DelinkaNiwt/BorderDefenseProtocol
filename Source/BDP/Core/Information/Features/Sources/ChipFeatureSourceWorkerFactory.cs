using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 根据来源配置的 WorkerClass 创建并缓存无状态 Worker。
    /// </summary>
    public static class ChipFeatureSourceWorkerFactory
    {
        /// <summary>按 Worker 类型缓存的实例。</summary>
        private static readonly Dictionary<Type, IChipFeatureSourceWorker> cache =
            new Dictionary<Type, IChipFeatureSourceWorker>();

        /// <summary>
        /// 尝试创建来源 Worker，并返回可定位的失败原因。
        /// </summary>
        public static bool TryCreate(
            ChipFeatureSourceConfig source,
            out IChipFeatureSourceWorker worker,
            out string error)
        {
            worker = null;
            error = null;
            if (source == null)
            {
                error = "来源配置为空。";
                return false;
            }

            Type workerType = source.WorkerClass;
            if (workerType == null)
            {
                error = "来源未声明 WorkerClass。";
                return false;
            }

            if (!typeof(IChipFeatureSourceWorker).IsAssignableFrom(workerType)
                || workerType.IsAbstract
                || workerType.IsInterface)
            {
                error = "WorkerClass 未实现 IChipFeatureSourceWorker 或不可实例化。";
                return false;
            }

            lock (cache)
            {
                if (cache.TryGetValue(workerType, out worker))
                {
                    return true;
                }

                try
                {
                    worker = (IChipFeatureSourceWorker)Activator.CreateInstance(workerType);
                    cache[workerType] = worker;
                    return true;
                }
                catch (Exception exception)
                {
                    worker = null;
                    error = "Worker 创建失败：" + exception.GetType().FullName;
                    return false;
                }
            }
        }
    }
}
