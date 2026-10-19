using System.Collections.Generic;
using System.Linq;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using Verse;

namespace BDP.Content.Research
{
    /// <summary>将芯片制造定义接入原版解锁列表；分组、排版与信息卡仍由原版负责。</summary>
    public static class ResearchUnlockDisplay
    {
        /// <summary>
        /// 读取制造使用的同一份研究条件，不检查完成状态，以便预览尚未解锁的内容。
        /// 同一功能加入所有要求的研究后，原版会自动显示其共同解锁条件。
        /// </summary>
        public static List<Def> Build(
            ResearchProjectDef project,
            List<Def> original,
            IEnumerable<ChipManufacturingPresetDef> functions,
            IEnumerable<ChipArmamentFormDef> forms)
        {
            if (project?.tab?.defName != "BDP_ResearchTab")
            {
                return original;
            }

            // 不修改原版缓存；通用成品载体由具体功能和构型替代，避免重复与误导。
            var result = original == null ? new List<Def>() : new List<Def>(original);
            result.RemoveAll(def => def == null || def.defName == "BDP_Chip_Custom");
            foreach (ChipManufacturingPresetDef function in functions)
            {
                if (function.showInManufacturing
                    && function.researchPrerequisites?.Contains(project) == true)
                {
                    result.Add(function);
                }
            }
            foreach (ChipArmamentFormDef form in forms)
            {
                if (form.showInManufacturing && !form.implicitDefault
                    && form.researchPrerequisites?.Contains(project) == true)
                {
                    result.Add(form);
                }
            }
            return result.Distinct().OrderBy(def => def.label).ToList();
        }
    }
}
