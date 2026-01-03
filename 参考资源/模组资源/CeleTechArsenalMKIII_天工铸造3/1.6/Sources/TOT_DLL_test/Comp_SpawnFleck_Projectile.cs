using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Comp_SpawnFleck_Projectile : ThingComp
{
	public int Fleck_MakeFleckTick;

	public CompProperties_SpawnFleck_Projectile Props => props as CompProperties_SpawnFleck_Projectile;

	private Projectile Projectile => parent as Projectile;

	public override void CompTick()
	{
		base.CompTick();
		Tick_SpawnFleck();
	}

	public void Tick_SpawnFleck()
	{
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Props.Fleck_MakeFleckTickMax)
		{
			Fleck_MakeFleckTick = 0;
			Map map = Projectile.Map;
			int randomInRange = Props.Fleck_MakeFleckNum.RandomInRange;
			Vector3 drawPos = Projectile.DrawPos;
			for (int i = 0; i < randomInRange; i++)
			{
				float num = (drawPos - Projectile.intendedTarget.CenterVector3).AngleFlat();
				float velocityAngle = Props.Fleck_Angle.RandomInRange + num;
				float randomInRange2 = Props.Fleck_Scale.RandomInRange;
				float randomInRange3 = Props.Fleck_Speed.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, Props.FleckDef, randomInRange2);
				dataStatic.rotationRate = Props.Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange3;
				map.flecks.CreateFleck(dataStatic);
			}
		}
	}
}
