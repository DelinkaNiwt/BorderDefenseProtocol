using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class CompProperties_ExostriderTurret : CompProperties
    {
		public ThingDef turretDef;

		public Vector3 drawOffset;

		public bool autoAttack = true;

		public bool changeAngle = false;

		public int ID;

		public CompProperties_ExostriderTurret()
		{
			compClass = typeof(CompExostriderTurret);
		}
	}
}
