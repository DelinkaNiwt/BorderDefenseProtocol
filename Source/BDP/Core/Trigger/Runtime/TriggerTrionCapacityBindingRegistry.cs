using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 触发体 Trion 容量绑定器注册表。
    /// 没有 Content 提供器时保持空操作，确保 Core 可独立运行。
    /// </summary>
    public static class TriggerTrionCapacityBindingRegistry
    {
        /// <summary>
        /// 当前已注册的触发体容量绑定器。
        /// </summary>
        private static readonly List<ITriggerTrionCapacityBinding> bindings =
            new List<ITriggerTrionCapacityBinding>();

        /// <summary>
        /// 注册一个触发体容量绑定器；同一实例不重复注册。
        /// </summary>
        public static void Register(ITriggerTrionCapacityBinding binding)
        {
            if (binding == null || bindings.Contains(binding))
            {
                return;
            }

            bindings.Add(binding);
        }

        /// <summary>
        /// 只清除指定绑定器，避免旧实例误删新实例。
        /// </summary>
        public static void Clear(ITriggerTrionCapacityBinding binding)
        {
            if (binding != null)
            {
                bindings.Remove(binding);
            }
        }

        /// <summary>
        /// 汇总当前 Pawn 的触发体容量贡献。
        /// </summary>
        public static float ResolveCapacityContribution(Pawn pawn)
        {
            if (pawn == null || bindings.Count == 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < bindings.Count; i++)
            {
                ITriggerTrionCapacityBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                try
                {
                    float value = binding.GetCapacityContribution(pawn);
                    if (value > 0f && !float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        total += value;
                    }
                }
                catch (Exception exception)
                {
                    Log.ErrorOnce(
                        "[BDP.Trion] 触发体容量贡献读取失败：" + exception,
                        17432121 + i);
                }
            }

            return total > 0f && !float.IsNaN(total) && !float.IsInfinity(total)
                ? total
                : 0f;
        }

        /// <summary>
        /// 广播触发体已装备事件。
        /// </summary>
        public static void NotifyEquipped(Pawn pawn, CompTriggerBody trigger)
        {
            Broadcast(pawn, trigger, (binding, owner, body) => binding.NotifyEquipped(owner, body));
        }

        /// <summary>
        /// 广播触发体已卸下事件。
        /// </summary>
        public static void NotifyUnequipped(Pawn pawn, CompTriggerBody trigger)
        {
            Broadcast(pawn, trigger, (binding, owner, body) => binding.NotifyUnequipped(owner, body));
        }

        /// <summary>
        /// 广播触发体即将因腺体资格失效而解除事件。
        /// </summary>
        public static void NotifyEligibilityLost(Pawn pawn, CompTriggerBody trigger)
        {
            Broadcast(pawn, trigger, (binding, owner, body) => binding.NotifyEligibilityLost(owner, body));
        }

        /// <summary>
        /// 广播触发体摧毁事件。
        /// </summary>
        public static void NotifyDestroyed(Pawn pawn, CompTriggerBody trigger)
        {
            Broadcast(pawn, trigger, (binding, owner, body) => binding.NotifyDestroyed(owner, body));
        }

        /// <summary>
        /// 只向声明处理当前触发体的绑定器广播事件。
        /// </summary>
        private static void Broadcast(
            Pawn pawn,
            CompTriggerBody trigger,
            Action<ITriggerTrionCapacityBinding, Pawn, CompTriggerBody> callback)
        {
            if (trigger == null || bindings.Count == 0 || callback == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                ITriggerTrionCapacityBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                try
                {
                    if (binding.Handles(trigger))
                    {
                        callback(binding, pawn, trigger);
                    }
                }
                catch (Exception exception)
                {
                    Log.ErrorOnce(
                        "[BDP.Trion] 触发体容量生命周期处理失败：" + exception,
                        17432141 + i);
                }
            }
        }
    }
}
