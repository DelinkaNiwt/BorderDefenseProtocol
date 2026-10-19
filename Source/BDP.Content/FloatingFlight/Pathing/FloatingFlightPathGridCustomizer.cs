using Unity.Collections;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 给原版 MapGridRequest 提供浮空路径种类身份，同时保留其它模组的网格定制器。
    /// 原版缓存键不包含 Pawn 的 PathingContext，必须把 Hybrid 与 Always 分开。
    /// </summary>
    internal sealed class FloatingFlightPathGridCustomizer : PathRequest.IPathGridCustomizer
    {
        /// <summary>无其它定制器时复用的 Hybrid 缓存身份。</summary>
        private static readonly FloatingFlightPathGridCustomizer HybridIdentity =
            new FloatingFlightPathGridCustomizer(FloatingFlightPathKind.Hybrid, null);

        /// <summary>无其它定制器时复用的 Always 缓存身份。</summary>
        private static readonly FloatingFlightPathGridCustomizer AlwaysIdentity =
            new FloatingFlightPathGridCustomizer(FloatingFlightPathKind.Always, null);

        /// <summary>本次请求固定使用的浮空路径种类。</summary>
        private readonly FloatingFlightPathKind kind;

        /// <summary>原请求已有的网格偏移定制器。</summary>
        private readonly PathRequest.IPathGridCustomizer inner;

        /// <summary>建立一个带路径种类身份的定制器包装器。</summary>
        private FloatingFlightPathGridCustomizer(
            FloatingFlightPathKind kind,
            PathRequest.IPathGridCustomizer inner)
        {
            this.kind = kind;
            this.inner = inner;
        }

        /// <summary>为浮空请求附加缓存身份，不覆盖已有定制器。</summary>
        public static PathRequest.IPathGridCustomizer Wrap(
            FloatingFlightPathKind kind,
            PathRequest.IPathGridCustomizer inner)
        {
            if (kind != FloatingFlightPathKind.Hybrid
                && kind != FloatingFlightPathKind.Always)
            {
                return inner;
            }

            if (inner == null)
            {
                return kind == FloatingFlightPathKind.Always
                    ? AlwaysIdentity
                    : HybridIdentity;
            }

            return new FloatingFlightPathGridCustomizer(kind, inner);
        }

        /// <summary>读取包装器中固定的浮空路径种类。</summary>
        public static bool TryGetKind(
            PathRequest.IPathGridCustomizer customizer,
            out FloatingFlightPathKind kind)
        {
            FloatingFlightPathGridCustomizer typed =
                customizer as FloatingFlightPathGridCustomizer;
            if (typed == null)
            {
                kind = FloatingFlightPathKind.Vanilla;
                return false;
            }

            kind = typed.kind;
            return true;
        }

        /// <summary>保留其它模组提供的原版路径偏移网格。</summary>
        public NativeArray<ushort> GetOffsetGrid()
        {
            return inner == null
                ? default(NativeArray<ushort>)
                : inner.GetOffsetGrid();
        }
    }
}
