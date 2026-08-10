using System.Collections.Generic;
using System;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体快照策略。
    /// 它负责承接快照排除规则的正式读取口，不直接编排进入或退出流程。
    /// </summary>
    internal sealed class CombatBodySnapshotPolicy
    {
        /// <summary>
        /// 读取当前所有快照配置 Def 中声明的 Hediff 排除项。
        /// </summary>
        public HashSet<string> GetExcludedHediffDefNames()
        {
            HashSet<string> results = new HashSet<string>();
            List<CombatBodySnapshotConfigDef> allDefs = DefDatabase<CombatBodySnapshotConfigDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                CombatBodySnapshotConfigDef definition = allDefs[i];
                if (definition?.excludedHediffs == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.excludedHediffs.Count; j++)
                {
                    string defName = definition.excludedHediffs[j];
                    if (!string.IsNullOrEmpty(defName))
                    {
                        results.Add(defName);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 读取当前所有快照配置 Def 中声明的 Hediff 类排除项。
        /// </summary>
        public HashSet<string> GetExcludedHediffClassNames()
        {
            HashSet<string> results = new HashSet<string>();
            List<CombatBodySnapshotConfigDef> allDefs = DefDatabase<CombatBodySnapshotConfigDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                CombatBodySnapshotConfigDef definition = allDefs[i];
                if (definition?.excludedHediffClasses == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.excludedHediffClasses.Count; j++)
                {
                    string className = definition.excludedHediffClasses[j];
                    if (!string.IsNullOrEmpty(className))
                    {
                        results.Add(className);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 读取当前所有快照配置 Def 中声明的 Hediff 排除类型。
        /// </summary>
        public List<Type> GetExcludedHediffTypes()
        {
            List<Type> results = new List<Type>();
            HashSet<string> classNames = GetExcludedHediffClassNames();
            foreach (string className in classNames)
            {
                Type type = GenTypes.GetTypeInAnyAssembly(className);
                if (type != null)
                {
                    results.Add(type);
                }
            }

            return results;
        }

        /// <summary>
        /// 判断指定 `Hediff` 是否属于快照排除项。
        /// </summary>
        public bool IsExcluded(Hediff hediff)
        {
            if (hediff?.def == null)
            {
                return true;
            }

            HashSet<string> excludedHediffDefNames = GetExcludedHediffDefNames();
            if (excludedHediffDefNames.Contains(hediff.def.defName))
            {
                return true;
            }

            List<Type> excludedHediffTypes = GetExcludedHediffTypes();
            Type hediffType = hediff.GetType();
            for (int i = 0; i < excludedHediffTypes.Count; i++)
            {
                if (excludedHediffTypes[i].IsAssignableFrom(hediffType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
