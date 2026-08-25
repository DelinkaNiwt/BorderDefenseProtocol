namespace BDP.Core.Projectiles.Interaction
{
    /// <summary>
    /// 一枚投射物在飞行与伤害阶段冻结的交互策略。
    /// </summary>
    public sealed class ProjectileInteractionPolicy
    {
        /// <summary>
        /// 是否绕过原版 CompProjectileInterceptor 拦截器。
        /// </summary>
        public bool BypassProjectileInterceptors { get; set; }

        /// <summary>
        /// 是否绕过已注册的伤害前吸收型护盾。
        /// </summary>
        public bool BypassRegisteredDamageShields { get; set; }

        /// <summary>
        /// 复制当前交互策略。
        /// </summary>
        public ProjectileInteractionPolicy Clone()
        {
            return new ProjectileInteractionPolicy
            {
                BypassProjectileInterceptors = BypassProjectileInterceptors,
                BypassRegisteredDamageShields = BypassRegisteredDamageShields
            };
        }
    }

    /// <summary>
    /// 当前投射物进入伤害前护盾入口时可读取的冻结策略作用域。
    /// </summary>
    public static class ProjectileInteractionPolicyScope
    {
        /// <summary>
        /// 当前线程的策略。
        /// </summary>
        [System.ThreadStatic]
        private static ProjectileInteractionPolicy current;

        /// <summary>
        /// 读取当前策略。
        /// </summary>
        public static ProjectileInteractionPolicy Current
        {
            get { return current; }
        }

        /// <summary>
        /// 压入一份临时策略。
        /// </summary>
        public static System.IDisposable Push(ProjectileInteractionPolicy policy)
        {
            ProjectileInteractionPolicy previous = current;
            current = policy;
            return new Scope(previous);
        }

        /// <summary>
        /// 恢复上一层策略。
        /// </summary>
        private sealed class Scope : System.IDisposable
        {
            private readonly ProjectileInteractionPolicy previous;

            public Scope(ProjectileInteractionPolicy previous)
            {
                this.previous = previous;
            }

            public void Dispose()
            {
                current = previous;
            }
        }
    }
}
