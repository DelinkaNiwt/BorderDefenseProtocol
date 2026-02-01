using System;
using Verse;
using UnityEngine;

namespace GD3
{
	public class GDUpdateSettings : ModSettings
	{
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref GDUpdateSettings.latestVersion, "latestVersion", 0, true);
		}

		public void Save()
        {
			LoadSaveMode mode = Scribe.mode;
			Scribe.mode = LoadSaveMode.Inactive;
			Write();
			Scribe.mode = mode;
		}

		public static float latestVersion;
	}
}
