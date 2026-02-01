using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class CompProperties_ServerUpperDrawer : CompProperties
	{
		public CompProperties_ServerUpperDrawer()
		{
			this.compClass = typeof(CompServerUpperDrawer);
		}

		public Vector3 drawOffset = new Vector3(0, 0, 0);
	}
}
