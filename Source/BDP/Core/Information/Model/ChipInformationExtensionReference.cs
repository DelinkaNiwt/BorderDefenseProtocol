namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息节点对外部信息的中性引用，不复制引用目标的业务内容。
    /// </summary>
    public sealed class ChipInformationExtensionReference
    {
        /// <summary>提供该引用的扩展命名空间稳定键。</summary>
        public string ProviderKey { get; private set; }

        /// <summary>提供方内引用目标类型的稳定键。</summary>
        public string ReferenceKindKey { get; private set; }

        /// <summary>引用目标的稳定键。</summary>
        public string ReferenceKey { get; private set; }

        /// <summary>引用所属形态或姿态上下文；为空表示芯片根节点。</summary>
        public string ContextKey { get; private set; }

        /// <summary>芯片在该关系中的客观角色；没有角色区分时为空。</summary>
        public string RelationRoleKey { get; private set; }

        /// <summary>创建一个指向外部信息目标的中性引用。</summary>
        public ChipInformationExtensionReference(
            string providerKey,
            string referenceKindKey,
            string referenceKey,
            string contextKey = null,
            string relationRoleKey = null)
        {
            ProviderKey = providerKey;
            ReferenceKindKey = referenceKindKey;
            ReferenceKey = referenceKey;
            ContextKey = contextKey;
            RelationRoleKey = relationRoleKey;
        }
    }
}
