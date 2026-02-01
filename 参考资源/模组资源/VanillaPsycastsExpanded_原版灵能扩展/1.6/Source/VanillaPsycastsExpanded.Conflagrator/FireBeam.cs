using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Conflagrator;

public class FireBeam : PowerBeam
{
	public override void StartStrike()
	{
		base.StartStrike();
		base.Position.GetFirstThing<Mote>(base.Map).Destroy();
		Mote obj = (Mote)ThingMaker.MakeThing(VPE_DefOf.VPE_Mote_FireBeam);
		obj.exactPosition = base.Position.ToVector3Shifted();
		obj.Scale = 90f;
		obj.rotationRate = 1.2f;
		GenSpawn.Spawn(obj, base.Position, base.Map);
	}
}
