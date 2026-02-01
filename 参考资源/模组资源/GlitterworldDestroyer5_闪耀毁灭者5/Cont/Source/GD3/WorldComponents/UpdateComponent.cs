using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	public class UpdateComponent : WorldComponent
	{
		public UpdateComponent(World world) : base(world)
		{
		}

		public override void FinalizeInit(bool fromLoad)
		{
			base.FinalizeInit(fromLoad);
			foreach (GDUpdate allDef in DefDatabase<GDUpdate>.AllDefs)
			{
				if (allDef.version > GDUpdateSettings.latestVersion)
				{
					ready = true;
					GDUpdateSettings.latestVersion = allDef.version;
					updateInfo = new UpdateWindow(allDef.label, allDef.description, allDef.preview);
				}
			}
		}

		public override void WorldComponentTick()
		{
			if ((Verse.Find.TickManager.TicksGame & 513) == 0 && ready)
			{
				if (updateInfo != null)
				{
					ready = false;
					Find.WindowStack.Add(updateInfo);
				}
			}
		}

		public bool ready = false;

		public UpdateWindow updateInfo;
	}
}
