namespace BDP.Trigger
{
    /// <summary>
    /// 拦截判定数据包——修饰CheckForFreeIntercept的行为。
    /// 模块可设置跳过拦截（穿透）或豁免特定目标。
    /// </summary>
    public struct InterceptContext
    {
        // ── 可修改 ──
        /// <summary>是否跳过拦截检查（穿透弹）。</summary>
        public bool SkipIntercept;

        public InterceptContext(bool skipIntercept)
        {
            SkipIntercept = skipIntercept;
        }
    }

    /// <summary>
    /// 拦截判定管线接口——修改拦截行为（穿透/豁免）。
    /// 执行顺序：管线第3阶段（引擎位置计算之后）。
    /// </summary>
    public interface IBDPInterceptModifier
    {
        /// <summary>修改拦截判定。</summary>
        void ModifyIntercept(Bullet_BDP host, ref InterceptContext ctx);
    }
}
