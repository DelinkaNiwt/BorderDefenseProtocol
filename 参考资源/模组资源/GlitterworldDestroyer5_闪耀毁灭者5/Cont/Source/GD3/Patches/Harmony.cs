using System;
using HarmonyLib;
using Verse;

namespace GD3
{
	// Token: 0x0200001C RID: 28
	[StaticConstructorOnStartup]
	internal static class HarmonyInit
	{
		// Token: 0x0600005E RID: 94 RVA: 0x00003F25 File Offset: 0x00002125
		static HarmonyInit()
		{
			new Harmony("fxz.glitterworldDestroyer").PatchAll();
		}
	}
}
