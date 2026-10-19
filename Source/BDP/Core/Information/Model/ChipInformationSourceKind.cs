namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息字段的通用来源类别。
    /// </summary>
    public enum ChipInformationSourceKind
    {
        /// <summary>原始声明定义。</summary>
        DeclaredDefinition,

        /// <summary>合并后的有效定义。</summary>
        EffectiveDefinition,

        /// <summary>激活后的正式运行结果。</summary>
        RuntimeResult,

        /// <summary>基础定义来源。</summary>
        BaseDefinition,

        /// <summary>武装或其它通用变体来源。</summary>
        Variant,

        /// <summary>通用派生来源。</summary>
        DerivedFact,

        /// <summary>画像来源。</summary>
        Profile,

        /// <summary>装载来源。</summary>
        Loadout,

        /// <summary>Trion 资源来源。</summary>
        Trion,

        /// <summary>激活条件来源。</summary>
        ActivationRequirement,

        /// <summary>表达来源。</summary>
        Expression,

        /// <summary>远程 Verb 来源。</summary>
        RangedVerb,

        /// <summary>近战 Tool 来源。</summary>
        MeleeTool,

        /// <summary>Ability 来源。</summary>
        Ability,

        /// <summary>Hediff 来源。</summary>
        Hediff,

        /// <summary>Passive 来源。</summary>
        Passive,

        /// <summary>形态来源。</summary>
        Mode,

        /// <summary>姿态来源。</summary>
        Stance,

        /// <summary>已注册扩展来源。</summary>
        Extension
    }
}
