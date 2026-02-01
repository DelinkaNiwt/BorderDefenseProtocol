using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
    public class CompUseEffect_HelpCaller : CompUseEffect
    {
		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			this.delayTicksProp = 300;
		}
		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			Map map = p.Map;
			List<Thing> list = map.listerThings.ThingsOfDef(GDDefOf.Mech_BlackApocriton);
			bool flag = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid) == null;
			bool flag2 = p.Map.GameConditionManager.GetActiveCondition(GDDefOf.SolarFlare) != null;
			bool flag3 = list.Count != 0;
			AcceptanceReport result;
			if (flag)
			{
				result = "BlackMechanoidNotExist".Translate();
			}
			else if (flag2)
			{
				result = "BlackMechanoidSleeping".Translate();
			}
			else if (flag3)
			{
				result = "Incident_BlackApocritonExist".Translate();
			}
			else
			{
				result = base.CanBeUsedBy(p);
			}
			return result;
		}

		public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
			IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.Special, Find.CurrentMap);
			parms.faction = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);
			parms.points = 100000f;
			GDDefOf.GD3BlackPassedBy.Worker.TryExecute(parms);
			Messages.Message("CalledBlackMechanoid".Translate(user), user, MessageTypeDefOf.NeutralEvent, true);
			if (this.delayTicksProp <= 0)
			{
				this.DoDestroy();
				return;
			}
			this.delayTicks = this.delayTicksProp;
		}
		private void DoDestroy()
		{
			this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.delayTicks, "delayTicks", -1, false);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (this.delayTicks > 0)
			{
				this.delayTicks--;
			}
			if (this.delayTicks == 0)
			{
				this.DoDestroy();
				this.delayTicks = -1;
			}
		}
		private int delayTicks = -1;

		private int delayTicksProp = 0;
	}
}
