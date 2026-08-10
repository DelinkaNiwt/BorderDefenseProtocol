using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程模块配置快照复制器。
    /// 它为模块配置提供统一、递归、协议级的深复制语义。
    /// </summary>
    internal static class RangedModuleConfigSnapshotCloner
    {
        /// <summary>
        /// 深复制指定配置根节点。
        /// </summary>
        internal static RangedModuleConfigNode Clone(RangedModuleConfigNode source)
        {
            return (RangedModuleConfigNode)CloneObject(
                source,
                new Dictionary<object, object>(ReferenceEqualityComparer.Instance));
        }

        /// <summary>
        /// 递归复制任意配置节点成员。
        /// </summary>
        private static object CloneObject(object source, IDictionary<object, object> visited)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();
            if (IsSharedReferenceType(type))
            {
                return source;
            }

            if (visited.TryGetValue(source, out object existing))
            {
                return existing;
            }

            if (type.IsArray)
            {
                Array sourceArray = (Array)source;
                Array clonedArray = Array.CreateInstance(type.GetElementType(), sourceArray.Length);
                visited[source] = clonedArray;
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    clonedArray.SetValue(CloneObject(sourceArray.GetValue(i), visited), i);
                }

                return clonedArray;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList sourceList = (IList)source;
                IList clonedList = (IList)Activator.CreateInstance(type);
                visited[source] = clonedList;
                for (int i = 0; i < sourceList.Count; i++)
                {
                    clonedList.Add(CloneObject(sourceList[i], visited));
                }

                return clonedList;
            }

            object clone = Activator.CreateInstance(type);
            visited[source] = clone;
            foreach (FieldInfo field in GetInstanceFields(type))
            {
                object fieldValue = field.GetValue(source);
                field.SetValue(clone, CloneObject(fieldValue, visited));
            }

            return clone;
        }

        /// <summary>
        /// 判断当前类型是否应按共享引用直接复用。
        /// </summary>
        private static bool IsSharedReferenceType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type.IsValueType
                || type == typeof(string)
                || type == typeof(decimal)
                || typeof(Def).IsAssignableFrom(type)
                || typeof(Type).IsAssignableFrom(type)
                || typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        /// <summary>
        /// 读取指定类型及其基类上的实例字段。
        /// </summary>
        private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(Flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (!field.IsStatic)
                    {
                        yield return field;
                    }
                }
            }
        }

        /// <summary>
        /// 以对象引用而不是值语义比较已访问节点。
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj != null
                    ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj)
                    : 0;
            }
        }
    }
}
