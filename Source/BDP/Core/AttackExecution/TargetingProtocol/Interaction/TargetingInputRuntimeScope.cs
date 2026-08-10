using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 很小的目标交互运行时输入作用域。
    /// 它用于把原版 Targeter 当前这一小段点击调用链对应的输入事实临时压入线程栈。
    /// </summary>
    public static class TargetingInputRuntimeScope
    {
        /// <summary>
        /// 每个线程独立维护自己的输入事实栈。
        /// 这样短暂输入桥不会和其它调用链串值。
        /// </summary>
        [ThreadStatic]
        private static Stack<ScopeFrame> stack;

        /// <summary>
        /// 当前线程分配到的下一个作用域编号。
        /// </summary>
        [ThreadStatic]
        private static long nextScopeId;

        /// <summary>
        /// 把当前输入事实压入线程作用域。
        /// 离开 using 块时自动弹出。
        /// </summary>
        public static IDisposable Push(TargetingInputRuntimeFacts facts)
        {
            if (facts == null)
            {
                return NoopScope.Instance;
            }

            if (stack == null)
            {
                stack = new Stack<ScopeFrame>();
            }

            long scopeId = ++nextScopeId;
            stack.Push(new ScopeFrame
            {
                ScopeId = scopeId,
                Value = facts
            });
            return new PopScope(scopeId);
        }

        /// <summary>
        /// 读取当前最内层输入事实。
        /// 没有时返回空值。
        /// </summary>
        public static TargetingInputRuntimeFacts Current
        {
            get
            {
                if (stack == null || stack.Count == 0)
                {
                    return null;
                }

                return stack.Peek().Value;
            }
        }

        /// <summary>
        /// 单个输入事实作用域栈帧。
        /// </summary>
        private sealed class ScopeFrame
        {
            /// <summary>
            /// 本栈帧的线程内作用域编号。
            /// </summary>
            public long ScopeId;

            /// <summary>
            /// 本栈帧携带的输入事实。
            /// </summary>
            public TargetingInputRuntimeFacts Value;
        }

        /// <summary>
        /// 离开 using 作用域时弹出自己压入的输入事实栈帧。
        /// </summary>
        private sealed class PopScope : IDisposable
        {
            /// <summary>
            /// 当前 PopScope 对应的作用域编号。
            /// </summary>
            private readonly long scopeId;

            /// <summary>
            /// 当前 PopScope 是否已经释放过。
            /// </summary>
            private bool disposed;

            /// <summary>
            /// 用指定作用域编号创建弹栈令牌。
            /// </summary>
            public PopScope(long scopeId)
            {
                this.scopeId = scopeId;
            }

            /// <summary>
            /// 只弹出自己对应的输入事实栈顶。
            /// </summary>
            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                if (stack == null || stack.Count == 0)
                {
                    return;
                }

                if (stack.Peek().ScopeId == scopeId)
                {
                    stack.Pop();
                    return;
                }

                BdpDiagnostics.Once(
                    "targeting_input_runtime_scope.pop_mismatch",
                    "目标输入运行时作用域弹栈顺序不匹配，已保留当前栈顶，避免误清其它作用域。");
            }
        }

        /// <summary>
        /// 空作用域。
        /// 没有输入事实时也能统一用 using 写法。
        /// </summary>
        private sealed class NoopScope : IDisposable
        {
            /// <summary>
            /// 空作用域单例。
            /// </summary>
            public static readonly NoopScope Instance = new NoopScope();

            /// <summary>
            /// 空作用域离开时不做任何事。
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
