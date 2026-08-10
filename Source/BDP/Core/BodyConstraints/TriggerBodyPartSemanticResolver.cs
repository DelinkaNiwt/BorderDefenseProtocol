using BDP.Core.Trigger;
using RimWorld;
using Verse;

namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Trigger 身体部位语义解析器。
    /// 它把 RimWorld 身体结构事实折算成 Trigger 可消费的最小身体约束语义。
    /// </summary>
    internal static class TriggerBodyPartSemanticResolver
    {
        /// <summary>
        /// 解析一个缺失身体部位对应的 Trigger 身体约束语义。
        /// </summary>
        public static TriggerBodyPartSemanticResult Resolve(BodyPartRecord part)
        {
            if (part == null || !IsManipulationLimbPart(part))
            {
                return new TriggerBodyPartSemanticResult(false, null);
            }

            TriggerSide resolvedSide;
            if (!TryResolveManipulationLimbSide(part, out resolvedSide))
            {
                return new TriggerBodyPartSemanticResult(true, null);
            }

            return new TriggerBodyPartSemanticResult(true, resolvedSide);
        }

        /// <summary>
        /// 判断当前部位本身是否是可操作肢体链上的正式节点。
        /// </summary>
        private static bool IsManipulationLimbPart(BodyPartRecord part)
        {
            return HasBodyPartTag(part, BodyPartTagDefOf.ManipulationLimbCore, "ManipulationLimbCore")
                || HasBodyPartTag(part, BodyPartTagDefOf.ManipulationLimbSegment, "ManipulationLimbSegment")
                || HasBodyPartTag(part, BodyPartTagDefOf.ManipulationLimbDigit, "ManipulationLimbDigit");
        }

        /// <summary>
        /// 判断指定部位是否拥有目标身体部位标签。
        /// </summary>
        private static bool HasBodyPartTag(BodyPartRecord part, BodyPartTagDef expectedTag, string expectedDefName)
        {
            if (part?.def?.tags == null)
            {
                return false;
            }

            for (int index = 0; index < part.def.tags.Count; index++)
            {
                BodyPartTagDef tag = part.def.tags[index];
                if (tag == expectedTag || tag?.defName == expectedDefName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从当前可操作肢体链解析 Trigger 侧别。
        /// </summary>
        private static bool TryResolveManipulationLimbSide(BodyPartRecord part, out TriggerSide side)
        {
            bool sawLeft = false;
            bool sawRight = false;
            BodyPartRecord current = part;

            while (current != null && IsManipulationLimbPart(current))
            {
                CollectSideHintsFromPartTree(current, ref sawLeft, ref sawRight);
                if (TryMapSideHints(sawLeft, sawRight, out side))
                {
                    return true;
                }

                current = current.parent;
            }

            side = TriggerSide.Special;
            return false;
        }

        /// <summary>
        /// 收集一个部位子树上的稳定侧别锚点。
        /// </summary>
        private static void CollectSideHintsFromPartTree(BodyPartRecord part, ref bool sawLeft, ref bool sawRight)
        {
            if (part == null)
            {
                return;
            }

            CollectSideHintsFromPart(part, ref sawLeft, ref sawRight);
            for (int index = 0; index < part.parts.Count; index++)
            {
                CollectSideHintsFromPartTree(part.parts[index], ref sawLeft, ref sawRight);
            }
        }

        /// <summary>
        /// 收集单个部位上的稳定侧别锚点。
        /// </summary>
        private static void CollectSideHintsFromPart(BodyPartRecord part, ref bool sawLeft, ref bool sawRight)
        {
            CollectSideHintsFromWoundAnchor(part, ref sawLeft, ref sawRight);
            CollectSideHintsFromGroups(part, ref sawLeft, ref sawRight);
        }

        /// <summary>
        /// 从伤口锚点收集侧别信息。
        /// </summary>
        private static void CollectSideHintsFromWoundAnchor(BodyPartRecord part, ref bool sawLeft, ref bool sawRight)
        {
            string woundAnchorTag = part.woundAnchorTag;
            if (string.IsNullOrEmpty(woundAnchorTag))
            {
                return;
            }

            if (woundAnchorTag.Contains("Left"))
            {
                sawLeft = true;
            }

            if (woundAnchorTag.Contains("Right"))
            {
                sawRight = true;
            }
        }

        /// <summary>
        /// 从身体部位组收集侧别信息。
        /// </summary>
        private static void CollectSideHintsFromGroups(BodyPartRecord part, ref bool sawLeft, ref bool sawRight)
        {
            if (part.groups == null)
            {
                return;
            }

            for (int index = 0; index < part.groups.Count; index++)
            {
                BodyPartGroupDef group = part.groups[index];
                if (group == BodyPartGroupDefOf.LeftHand || group?.defName == "LeftHand")
                {
                    sawLeft = true;
                }

                if (group == BodyPartGroupDefOf.RightHand || group?.defName == "RightHand")
                {
                    sawRight = true;
                }
            }
        }

        /// <summary>
        /// 把侧别锚点折算成 Trigger 侧别。
        /// </summary>
        private static bool TryMapSideHints(bool sawLeft, bool sawRight, out TriggerSide side)
        {
            if (sawLeft && !sawRight)
            {
                side = TriggerSide.Sub;
                return true;
            }

            if (sawRight && !sawLeft)
            {
                side = TriggerSide.Main;
                return true;
            }

            side = TriggerSide.Special;
            return false;
        }
    }
}

