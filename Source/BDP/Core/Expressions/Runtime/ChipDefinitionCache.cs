using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 芯片定义读取缓存。
    /// 它只缓存按 ThingDef 解释完成的静态定义结果，不缓存依赖当前 Trigger 真值的动态表达成立结果。
    /// </summary>
    internal sealed class ChipDefinitionCache
    {
        /// <summary>
        /// 当前已缓存的芯片定义读取结果。
        /// </summary>
        private readonly Dictionary<ThingDef, ChipDefinitionReadResult> cache =
            new Dictionary<ThingDef, ChipDefinitionReadResult>();

        /// <summary>
        /// 按 ThingDef 读取或创建芯片定义结果。
        /// </summary>
        internal ChipDefinitionReadResult GetOrAdd(ThingDef thingDef, Func<ThingDef, ChipDefinitionReadResult> factory)
        {
            if (thingDef == null)
            {
                return factory != null ? factory(null) : null;
            }

            ChipDefinitionReadResult cachedResult;
            if (cache.TryGetValue(thingDef, out cachedResult))
            {
                return cachedResult;
            }

            ChipDefinitionReadResult created = factory != null ? factory(thingDef) : null;
            cache[thingDef] = created;
            return created;
        }
    }
}
