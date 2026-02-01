using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
    public class CompProperties_FloatingCannon : CompProperties
    {
		public ThingDef turretDef;

		public float angleOffset;

		public bool autoAttack = true;

		public List<PawnRenderNodeProperties> renderNodeProperties;

		public float offset;

		public float angle = 0;

		public int id;

		public CompProperties_FloatingCannon()
		{
			compClass = typeof(CompFloatingCannon);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			if (renderNodeProperties.NullOrEmpty())
			{
				yield break;
			}
			foreach (PawnRenderNodeProperties renderNodeProperty in renderNodeProperties)
			{
				if (!typeof(PawnRenderNode_FloatingCannon).IsAssignableFrom(renderNodeProperty.nodeClass))
				{
					yield return "contains nodeClass which is not PawnRenderNode_TurretGun or subclass thereof.";
				}
			}
		}
	}
}
