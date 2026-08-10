using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击上下文冻结快照。
    /// 它只负责保存和读取节点，不恢复任何运行时会话。
    /// </summary>
    public sealed class AttackContextSnapshot : IExposable
    {
        /// <summary>
        /// 通过反射调用 Scribe_Deep.Look 的缓存入口。
        /// 节点具体类型由节点自己提供，主模组不把具体节点类型写死在这里。
        /// </summary>
        private static readonly MethodInfo DeepLookMethod = typeof(Scribe_Deep)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == nameof(Scribe_Deep.Look)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 3
                && method.GetParameters()[0].ParameterType.IsByRef);

        /// <summary>
        /// 当前快照里的节点条目列表。
        /// 这里使用列表而不是字典存档，避免 Verse 对泛型字典深度存档的额外不确定性。
        /// </summary>
        private List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 从运行态节点表创建冻结快照。
        /// 调用方传入的运行态节点会在这里被复制成独立快照条目。
        /// </summary>
        internal static AttackContextSnapshot Create(IReadOnlyDictionary<string, IAttackContextNode> nodes)
        {
            AttackContextSnapshot snapshot = new AttackContextSnapshot();
            if (nodes == null)
            {
                return snapshot;
            }

            foreach (KeyValuePair<string, IAttackContextNode> pair in nodes)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                snapshot.entries.Add(new Entry
                {
                    Key = pair.Key,
                    Node = pair.Value.Clone()
                });
            }

            return snapshot;
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
            Entry entry = FindEntry(key);
            if (entry == null)
            {
                return false;
            }

            node = entry.Node as T;
            return node != null;
        }

        /// <summary>
        /// 按键读取原始节点。
        /// 这条入口只服务主模组内部做中性桥接，不对业务层暴露额外协议。
        /// </summary>
        internal IAttackContextNode GetNode(string key)
        {
            return FindEntry(key)?.Node;
        }

        /// <summary>
        /// 读取当前快照全部条目。
        /// 这条入口只服务主模组内部恢复运行态上下文，不对业务层暴露额外协议。
        /// </summary>
        internal IEnumerable<Entry> GetEntries()
        {
            return entries ?? new List<Entry>();
        }

        /// <summary>
        /// 序列化当前攻击上下文快照。
        /// 快照只保存节点键、节点类型和节点数据，不保存运行时执行器。
        /// </summary>
        public void ExposeData()
        {
            List<Entry> exposedEntries = entries;
            Scribe_Collections.Look(ref exposedEntries, "entries", LookMode.Deep);
            entries = exposedEntries ?? new List<Entry>();
        }

        /// <summary>
        /// 查找指定键对应的快照条目。
        /// </summary>
        private Entry FindEntry(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || entries == null)
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry != null && entry.Key == key)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 快照中的单个节点条目。
        /// 它只记录节点键、节点类型和节点本体。
        /// </summary>
        public sealed class Entry : IExposable
        {
            /// <summary>
            /// 当前节点的具体类型名。
            /// 读档时用它定位具体节点类型。
            /// </summary>
            private string nodeType;

            /// <summary>
            /// 当前节点的字符串键。
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// 当前节点本体。
            /// 节点内部数据由节点自己的 ExposeData 负责。
            /// </summary>
            public IAttackContextNode Node { get; set; }

            /// <summary>
            /// 序列化当前节点条目。
            /// </summary>
            public void ExposeData()
            {
                string key = Key;
                Scribe_Values.Look(ref key, "key");
                Key = key;

                string nodeTypeName = !string.IsNullOrWhiteSpace(nodeType)
                    ? nodeType
                    : Node != null ? Node.GetType().AssemblyQualifiedName : null;
                Scribe_Values.Look(ref nodeTypeName, "nodeType");
                nodeType = nodeTypeName;

                ExposeNode();
            }

            /// <summary>
            /// 按运行时真实节点类型持久化节点本体。
            /// 类型无效时直接跳过，主模组不猜测替代含义。
            /// </summary>
            private void ExposeNode()
            {
                if (string.IsNullOrWhiteSpace(nodeType))
                {
                    return;
                }

                Type concreteType = Type.GetType(nodeType);
                if (concreteType == null || !typeof(IAttackContextNode).IsAssignableFrom(concreteType))
                {
                    return;
                }

                object nodeObject = Node;
                if (nodeObject == null && Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    try
                    {
                        nodeObject = Activator.CreateInstance(concreteType);
                    }
                    catch
                    {
                        return;
                    }
                }

                MethodInfo concreteLookMethod = DeepLookMethod.MakeGenericMethod(concreteType);
                object[] arguments = { nodeObject, "node", null };
                concreteLookMethod.Invoke(null, arguments);
                Node = arguments[0] as IAttackContextNode;
            }
        }
    }
}
