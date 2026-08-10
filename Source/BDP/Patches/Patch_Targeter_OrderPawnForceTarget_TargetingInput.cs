using System;
using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace BDP.Patches
{
    /// <summary>
    /// 原版 Targeter 目标确认点击的中性输入桥。
    /// 它只负责把当前按钮与修饰键事实临时压入 BDP 输入作用域，不解释任何业务含义。
    /// </summary>
    [HarmonyPatch(typeof(Targeter), nameof(Targeter.OrderPawnForceTarget))]
    /// <summary>
    /// Targeter 到 BDP 目标交互输入帧的最小桥接补丁。
    /// </summary>
    public static class Patch_Targeter_OrderPawnForceTarget_TargetingInput
    {
        /// <summary>
        /// 在原版真正把目标回调给 targeting source 之前，
        /// 先把这一轮点击事件对应的中性输入事实压入运行时作用域。
        /// </summary>
        public static void Prefix(ref IDisposable __state)
        {
            __state = TargetingInputRuntimeScope.Push(TargetingInputRuntimeFacts.FromEvent(Event.current));
        }

        /// <summary>
        /// 当前这一轮目标确认调用结束或异常退出后，
        /// 立刻把临时输入事实作用域弹掉，避免串到后续无关调用。
        /// </summary>
        [HarmonyFinalizer]
        public static Exception Finalizer(IDisposable __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
