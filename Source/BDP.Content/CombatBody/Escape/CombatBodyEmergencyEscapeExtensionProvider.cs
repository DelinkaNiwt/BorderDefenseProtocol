using BDP.Core.CombatBody.External;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 将紧急脱离接入 Core 的中性崩解扩展生命周期。
    /// </summary>
    public sealed class CombatBodyEmergencyEscapeExtensionProvider : ICombatBodyCollapseExtensionProvider
    {
        /// <summary>
        /// 紧急脱离解析器。
        /// </summary>
        private readonly CombatBodyEmergencyEscapeResolver resolver = new CombatBodyEmergencyEscapeResolver();

        /// <summary>
        /// 紧急脱离执行服务。
        /// </summary>
        private readonly CombatBodyEmergencyEscapeService service = new CombatBodyEmergencyEscapeService();

        /// <summary>
        /// 崩解开始时缓存正式表达结果。
        /// </summary>
        public void Prepare(Pawn pawn)
        {
            CompCombatBodyEmergencyEscapeState state = ResolveState(pawn);
            state?.SetPreparedResolution(resolver.Resolve(pawn));
        }

        /// <summary>
        /// 崩解表现结束后消费缓存并执行紧急脱离。
        /// </summary>
        public void Execute(Pawn pawn)
        {
            CompCombatBodyEmergencyEscapeState state = ResolveState(pawn);
            if (state == null)
            {
                return;
            }

            service.ExecuteEmergencyEscapeIfAvailable(pawn, state.PreparedResolution);
        }

        /// <summary>
        /// 清理 Content 侧缓存。
        /// </summary>
        public void Clear(Pawn pawn)
        {
            ResolveState(pawn)?.Clear();
        }

        /// <summary>
        /// 获取当前 Pawn 的 Content 状态组件。
        /// </summary>
        private static CompCombatBodyEmergencyEscapeState ResolveState(Pawn pawn)
        {
            return pawn?.GetComp<CompCombatBodyEmergencyEscapeState>();
        }
    }
}
