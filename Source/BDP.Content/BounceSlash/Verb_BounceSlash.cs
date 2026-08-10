using BDP.Core.PathInput;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.BounceSlash
{
    /// <summary>
    /// 弹射砍击 Verb — 继承蚱蜢全部行为，仅覆写预览颜色（橙色）。
    /// 其余表现与蚱蜢完全一致。
    /// </summary>
    public class Verb_BounceSlash : Grasshopper.Verb_CastAbilityGrasshopper
    {
        /// <summary>
        /// 预览线换橙色，与蚱蜢白线区分。
        /// </summary>
        protected override void DrawPathPreview(LocalTargetInfo currentTarget)
        {
            PathPreviewData preview = PathInputHandler.BuildPreview(
                pathInputState, CasterPawn, currentTarget, isLastSegmentBlocked: false);

            PathInput.PathPreviewRenderer.DrawPreview(preview, pathInputState, SimpleColor.Orange);
        }
    }
}
