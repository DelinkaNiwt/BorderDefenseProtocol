using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击上下文运行态主干。
    /// 它只按字符串键保存节点，主模组不解释节点内容。
    /// </summary>
    public sealed class AttackContext
    {
        /// <summary>
        /// 当前运行态节点表。
        /// 键由调用方负责命名，值必须是可复制、可存读档的上下文节点。
        /// </summary>
        private readonly Dictionary<string, IAttackContextNode> nodes = new Dictionary<string, IAttackContextNode>();

        /// <summary>
        /// 从冻结快照恢复一份可继续传递的运行态上下文。
        /// 恢复结果会复制节点，避免和原快照共享同一引用。
        /// </summary>
        internal static AttackContext FromSnapshot(AttackContextSnapshot snapshot)
        {
            AttackContext attackContext = new AttackContext();
            if (snapshot == null)
            {
                return attackContext;
            }

            foreach (AttackContextSnapshot.Entry entry in snapshot.GetEntries())
            {
                if (entry?.Node == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                attackContext.Set(entry.Key, entry.Node.Clone());
            }

            return attackContext;
        }

        /// <summary>
        /// 按键读取指定类型节点。
        /// 找不到或类型不匹配时返回 null。
        /// </summary>
        public T Get<T>(string key)
            where T : class, IAttackContextNode
        {
            return TryGet(key, out T node) ? node : null;
        }

        /// <summary>
        /// 按键尝试读取指定类型节点。
        /// 返回 false 表示没有节点或节点类型不符合调用方要求。
        /// </summary>
        public bool TryGet<T>(string key, out T node)
            where T : class, IAttackContextNode
        {
            node = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!nodes.TryGetValue(key, out IAttackContextNode rawNode))
            {
                return false;
            }

            node = rawNode as T;
            return node != null;
        }

        /// <summary>
        /// 按键读取原始节点。
        /// 这条入口只服务主模组内部做中性桥接，不对业务层开放额外协议。
        /// </summary>
        internal IAttackContextNode GetNode(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            nodes.TryGetValue(key, out IAttackContextNode node);
            return node;
        }

        /// <summary>
        /// 按键读取或创建指定类型节点。
        /// 如果同键已有其它类型节点，则返回 null，避免静默覆盖调用方数据。
        /// </summary>
        public T GetOrCreate<T>(string key)
            where T : class, IAttackContextNode, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (nodes.TryGetValue(key, out IAttackContextNode rawNode))
            {
                return rawNode as T;
            }

            T created = new T();
            nodes[key] = created;
            return created;
        }

        /// <summary>
        /// 写入指定节点。
        /// 这条入口只服务主模组把已存在的中性节点收进统一上下文，不做业务解释。
        /// </summary>
        internal void Set(string key, IAttackContextNode node)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (node == null)
            {
                nodes.Remove(key);
                return;
            }

            nodes[key] = node;
        }

        /// <summary>
        /// 冻结当前运行态上下文。
        /// 每个节点通过自己的 Clone 方法复制，复制失败的节点不会被主模组特殊解释。
        /// </summary>
        public AttackContextSnapshot ToSnapshot()
        {
            return AttackContextSnapshot.Create(nodes);
        }
    }
}
