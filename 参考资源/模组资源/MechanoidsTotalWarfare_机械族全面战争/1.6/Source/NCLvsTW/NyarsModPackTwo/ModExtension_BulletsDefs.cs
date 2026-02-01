using System.Collections.Generic;
using Verse;

namespace NyarsModPackTwo;

public class ModExtension_BulletsDefs : DefModExtension
{
	public List<ThingDef> bullets = new List<ThingDef>();

	public IntRange castCount;
}
