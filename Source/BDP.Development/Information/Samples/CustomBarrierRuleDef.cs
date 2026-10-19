using Verse;

namespace BDP.Development.Information.Samples
{
    /// <summary>Development 扩展示例中自定义护盾路线的权威业务规则。</summary>
    public sealed class CustomBarrierRuleDef : Def
    {
        /// <summary>该规则提供的玩家语义属性 DefName。</summary>
        public string PropertyDefName;

        /// <summary>该规则路线的反射比例业务值。</summary>
        public float ReflectionRatio;
    }
}
