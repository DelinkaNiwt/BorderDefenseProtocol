using AncotLibrary;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public class ThingColumnWorker_WeaponOwner : ThingColumnWorker_Label
{
	protected override TextAnchor DefaultHeaderAlignment => TextAnchor.MiddleCenter;

	protected override TextAnchor LabelAlignment => TextAnchor.MiddleCenter;

	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		Pawn owner = WeaponOwner(thing);
		if (owner != null)
		{
			base.DoCell(rect, (Thing)owner, table);
		}
	}

	public Pawn WeaponOwner(Thing thing)
	{
		if (thing == null || thing.Destroyed)
		{
			return null;
		}
		if (thing.ParentHolder != null)
		{
			Thing obj = thing;
			if (thing.ParentHolder.ParentHolder is Pawn { Corpse: var corpse } pawn)
			{
				if (corpse != null)
				{
					obj = corpse;
				}
				else
				{
					obj = pawn;
				}
				return pawn;
			}
		}
		return null;
	}

	public override bool CanGroupWith(Thing thing, Thing other)
	{
		Pawn owner = WeaponOwner(thing);
		return owner != null && WeaponOwner(other) == owner;
	}
}
