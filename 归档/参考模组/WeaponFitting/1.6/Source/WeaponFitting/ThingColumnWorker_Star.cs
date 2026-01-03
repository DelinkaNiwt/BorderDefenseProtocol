using AncotLibrary;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public class ThingColumnWorker_Star : ThingColumnWorker_Icon
{
	protected override Texture2D GetIconFor(Thing thing)
	{
		if (thing.IsWeaponStar())
		{
			return AncotLibraryIcon.StarOn;
		}
		return AncotLibraryIcon.StarOff;
	}

	protected override void ClickedIcon(Thing thing)
	{
		base.ClickedIcon(thing);
		if (thing.IsWeaponStar())
		{
			GameComponent_AncotLibrary.GC.StarWeapon.Remove(thing);
		}
		else
		{
			GameComponent_AncotLibrary.GC.StarWeapon.Add(thing);
		}
	}
}
