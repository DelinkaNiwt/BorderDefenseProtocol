using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class ModExtension_MechList : DefModExtension
{
	public List<ThingDef> mechs;

	public List<ThingDef> mechsRequireWeapon;

	public float maxHealthPercent = 0.8f;

	public float minHealthPercent = 0.5f;
}
