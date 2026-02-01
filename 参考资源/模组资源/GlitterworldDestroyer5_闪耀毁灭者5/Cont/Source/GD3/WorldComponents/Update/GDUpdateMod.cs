using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class GDUpdateMod : Mod
	{
		public GDUpdateMod(ModContentPack mcp) : base(mcp)
		{
			settings = base.GetSettings<GDUpdateSettings>();
		}

		public static GDUpdateSettings settings;
	}
}