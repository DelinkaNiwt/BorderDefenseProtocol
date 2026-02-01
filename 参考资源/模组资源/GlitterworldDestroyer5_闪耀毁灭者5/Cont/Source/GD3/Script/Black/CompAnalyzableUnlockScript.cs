using System;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompAnalyzableUnlockScript : CompAnalyzable
	{
		public new CompProperties_CompAnalyzableUnlockScript Props => (CompProperties_CompAnalyzableUnlockScript)props;

		public override int AnalysisID => Props.analysisID;

		public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
		{
			AcceptanceReport result = base.CanInteract(activateBy, checkOptionalItems);
			if (!result.Accepted)
			{
				return result;
			}
			if (activateBy == null)
            {
				return false;
            }
			if (Find.World.GetComponent<MissionComponent>().blackMechDiscoverd)
			{
				return "GD.BlackMechAlreadyDiscovered".Translate();
			}
			if (activateBy.skills == null || activateBy.skills.GetSkill(SkillDefOf.Intellectual).PermanentlyDisabled || activateBy.skills.GetSkill(SkillDefOf.Intellectual).Level < 10)
            {
				return "GD.LevelNotEnoughToAnalyse".Translate();
            }
			return true;
		}

        public override void OnAnalyzed(Pawn pawn)
        {
            base.OnAnalyzed(pawn);
			Thing t = this.parent;
			Effecter effecter = GDDefOf.ApocrionAoeWarmup.SpawnAttached(t, t.MapHeld, 1f);
			effecter.Trigger(t, t, -1);
			effecter.Cleanup();
			GDDefOf.MechbandDishUsed.PlayOneShot(new TargetInfo(t.PositionHeld, t.MapHeld, false));
			GDDefOf.GD_Morse_IMHERE.PlayOneShotOnCamera();
			Find.World.GetComponent<MissionComponent>().blackMechDiscoverd = true;
        }
    }
}
