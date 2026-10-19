using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 芯片许可接收器注册表。
    /// 注册表不包含任何具体芯片或许可业务名称。
    /// </summary>
    public static class ChipFunctionPermissionReceiverRegistry
    {
        /// <summary>当前有效的接收器实例。</summary>
        private static readonly List<IChipFunctionPermissionReceiver> Receivers =
            new List<IChipFunctionPermissionReceiver>();

        /// <summary>注册一个接收器；重复实例不会重复加入。</summary>
        public static void Register(IChipFunctionPermissionReceiver receiver)
        {
            if (receiver != null && !Receivers.Contains(receiver))
            {
                Receivers.Add(receiver);
            }
        }

        /// <summary>移除一个接收器实例。</summary>
        public static void Unregister(IChipFunctionPermissionReceiver receiver)
        {
            if (receiver != null)
            {
                Receivers.Remove(receiver);
            }
        }

        /// <summary>清空注册表，供重载和测试场景使用。</summary>
        public static void Clear()
        {
            Receivers.Clear();
        }

        /// <summary>按注册顺序通知全部接收器；单个业务异常不会阻断攻击主链。</summary>
        public static void Notify(MapPermissionContext context)
        {
            if (context == null || Receivers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < Receivers.Count; index++)
            {
                IChipFunctionPermissionReceiver receiver = Receivers[index];
                if (receiver == null)
                {
                    continue;
                }

                try
                {
                    receiver.Receive(context);
                }
                catch (Exception ex)
                {
                    BdpDiagnostics.Once(
                        "chip_permission.receiver_failed." + receiver.GetType().FullName,
                        "芯片许可接收器执行失败，已隔离该业务异常。receiver="
                        + receiver.GetType().FullName + "\n" + ex);
                }
            }
        }
    }
}
