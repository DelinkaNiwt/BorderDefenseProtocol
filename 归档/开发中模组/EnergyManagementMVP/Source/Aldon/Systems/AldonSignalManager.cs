using Verse;
using System;
using System.Collections.Generic;

namespace Aldon.Energy
{
    /// <summary>
    /// 全局单例: 管理能量恢复信号
    /// 由驱动塔建筑控制激活/停用
    /// </summary>
    public class AldonSignalManager
    {
        // ===== 单例实现 =====
        private static AldonSignalManager instance;

        public static AldonSignalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AldonSignalManager();
                }
                return instance;
            }
        }

        // ===== 状态数据 =====
        private bool isActive = false;
        private List<Action> onActivated = new List<Action>();
        private List<Action> onDeactivated = new List<Action>();

        // ===== 公开属性 =====
        public bool IsActive => isActive;

        // ===== 控制方法 =====

        /// <summary>
        /// 激活全局信号 - 允许能量恢复
        /// </summary>
        public void Activate()
        {
            if (isActive) return;  // 防止重复激活

            isActive = true;
            Log.Message("[Aldon] 驱动塔已激活 - 能量恢复系统启动");

            // 广播激活回调
            foreach (var action in onActivated)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"[Aldon] 激活回调执行失败: {ex}");
                }
            }
        }

        /// <summary>
        /// 停用全局信号 - 停止能量恢复
        /// </summary>
        public void Deactivate()
        {
            if (!isActive) return;  // 防止重复停用

            isActive = false;
            Log.Message("[Aldon] 驱动塔已停用 - 能量恢复系统关闭");

            // 广播停用回调
            foreach (var action in onDeactivated)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"[Aldon] 停用回调执行失败: {ex}");
                }
            }
        }

        /// <summary>
        /// 获取当前信号状态
        /// </summary>
        public bool GetSignal() => isActive;

        // ===== 回调注册 =====

        /// <summary>
        /// 注册激活回调
        /// </summary>
        public void RegisterOnActivated(Action callback)
        {
            if (callback != null && !onActivated.Contains(callback))
            {
                onActivated.Add(callback);
            }
        }

        /// <summary>
        /// 注册停用回调
        /// </summary>
        public void RegisterOnDeactivated(Action callback)
        {
            if (callback != null && !onDeactivated.Contains(callback))
            {
                onDeactivated.Add(callback);
            }
        }

        /// <summary>
        /// 注销回调
        /// </summary>
        public void UnregisterCallback(Action callback)
        {
            onActivated.Remove(callback);
            onDeactivated.Remove(callback);
        }
    }
}
