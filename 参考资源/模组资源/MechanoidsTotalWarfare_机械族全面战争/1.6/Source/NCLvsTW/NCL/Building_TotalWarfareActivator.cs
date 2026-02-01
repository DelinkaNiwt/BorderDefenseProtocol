using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Building_TotalWarfareActivator : Building
{
	private bool _activated;

	public bool Activated
	{
		get
		{
			return _activated;
		}
		private set
		{
			_activated = value;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		Find.World.GetComponent<TipComponent>();
	}

	public void ActivateTotalWarfare()
	{
		if (!Activated)
		{
			TipComponent comp = Find.World.GetComponent<TipComponent>();
			if (comp != null)
			{
				Activated = true;
				comp.TWtriggered1 = true;
				Find.LetterStack.ReceiveLetter("NCL_TOTALWARFARE_MANUAL_TRIGGER_TITLE".Translate(), "NCL_TOTALWARFARE_MANUAL_TRIGGER_TEXT".Translate(), LetterDefOf.NeutralEvent);
				FleckMaker.ThrowExplosionCell(base.Position, base.Map, FleckDefOf.ExplosionFlash, Color.red);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref _activated, "activated", defaultValue: false);
	}
}
