using System;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace GD3
{
	public class CompProperties_SpawnThingOnDestroy : CompProperties
	{
		public CompProperties_SpawnThingOnDestroy()
		{
			this.compClass = typeof(CompSpawnThingOnDestroy);
		}

		public ThingDef thingDef = null;
	}
}