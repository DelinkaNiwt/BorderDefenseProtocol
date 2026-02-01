using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_Detection : CompUseEffect
	{
		public CompReceiverSelect CompSelect
		{
			get
			{
				return this.parent.TryGetComp<CompReceiverSelect>();
			}
		}

		public QuestScriptDef Quest
		{
			get
			{
				switch (CompSelect.Mark)
				{
					case 0:
						return GDDefOf.GD_Quest_Cluster_S;
					case 1:
						return GDDefOf.GD_Quest_Cluster_M;
					case 2:
						return GDDefOf.GD_Quest_Cluster_L;
					case 3:
						return GDDefOf.GD_Quest_Cluster_U;
					default:
						throw new NotImplementedException();
				}
			}
		}

		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			bool flag = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid) == null;
			bool flag2 = this.delayTicks > 0;
			AcceptanceReport result;
			if (flag)
			{
				result = "MechanoidNotExist".Translate();
			}
			else if (flag2)
			{
				int hour = Mathf.FloorToInt(this.delayTicks / 2500);
				result = "ReceiverDelaying".Translate(hour);
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
			Map map = this.parent.Map;
			QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(this.Quest, new IncidentParms
			{
				target = map,
				points = StorytellerUtility.DefaultThreatPointsNow(map)
			}.points));
			this.delayTicks = 60000 * GDSettings.DetectCooldown;
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
		}

		private int delayTicks = -1;

	}
}
