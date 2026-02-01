using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_Window : CompUseEffect
	{
		public Building Station
		{
			get
			{
				return this.parent as Building;
			}
		}

		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			CompPowerTrader comp = this.Station.TryGetComp<CompPowerTrader>();
			AcceptanceReport result;
			if (comp != null && !comp.PowerOn)
			{
				result = "StationWithoutPower".Translate();
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
			missionInfo = new MissionWindow("GD.StationTitle".Translate(), "GD.StationDescription".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary, Find.World.GetComponent<MissionComponent>().intelligenceAdvanced), this.parent.Map, user);
			Find.WindowStack.Add(missionInfo);
		}

		public MissionWindow missionInfo;

		public GraphicWindow graphicInfo;
	}
}
