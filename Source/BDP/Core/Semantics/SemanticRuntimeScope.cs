using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

namespace BDP.Core.Semantics
{
    /// <summary>
    /// 很小的运行时语义作用域。
    /// 用于把“当前这一小段伤害调用链对应的攻击语义”临时压到栈里。
    /// </summary>
    public static class SemanticRuntimeScope
    {
        /// <summary>
        /// 每个线程独立维护自己的语义栈。
        /// 这样多层伤害调用不会互相串值。
        /// </summary>
        [ThreadStatic]
        private static Stack<ScopeFrame> stack;

        /// <summary>
        /// 当前线程分配到的下一个作用域编号。
        /// </summary>
        [ThreadStatic]
        private static long nextScopeId;

        /// <summary>
        /// 进入一段新的攻击语义作用域。
        /// 离开 using 块时会自动弹出。
        /// </summary>
        public static IDisposable Push(ISemanticContext semanticContext)
        {
            if (semanticContext == null)
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
                Value = semanticContext
            });
            return new PopScope(scopeId);
        }

        /// <summary>
        /// 读取当前最内层作用域里的攻击语义。
        /// </summary>
        public static ISemanticContext Current
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
        /// 单个语义作用域栈帧。
        /// </summary>
        private sealed class ScopeFrame
        {
            /// <summary>
            /// 本栈帧的线程内作用域编号。
            /// </summary>
            public long ScopeId;

            /// <summary>
            /// 本栈帧携带的语义上下文。
            /// </summary>
            public ISemanticContext Value;
        }

        /// <summary>
        /// 负责离开作用域时弹出自己压入的栈帧。
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
            /// 离开 using 作用域时只弹出自己对应的语义栈顶。
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
                    "semantic_runtime_scope.pop_mismatch",
                    "语义运行时作用域弹栈顺序不匹配，已保留当前栈顶，避免误清其它作用域。");
            }
        }

        /// <summary>
        /// 空作用域。
        /// 没有语义时也能统一用 `using` 写法。
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
