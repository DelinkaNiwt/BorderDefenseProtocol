using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAbilityEffect_AoEFist : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	public static EffecterDef pulse;

	private new CompProperties_AoEFist Props => (CompProperties_AoEFist)props;

	private Pawn Pawn => parent.pawn;

	private bool Canusecell(IntVec3 c)
	{
		ShootLine resultingLine;
		return c.InBounds(Pawn.Map) && !(c == Pawn.Position) && c.InHorDistOf(Pawn.Position, Props.range) && parent.verb.TryFindShootLineFromTo(parent.pawn.Position, c, out resultingLine);
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		IntVec3 position = Pawn.Position;
		float angleFlat = (target.CenterVector3.ToIntVec3() - Pawn.Position).AngleFlat;
		if (Props.SpawnFleck != null)
		{
			for (int i = 0; i < Props.Fleck_Num; i++)
			{
				float scale = 15f + Rand.Range(-5f, 5f);
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(Pawn.DrawPos, Pawn.Map, Props.SpawnFleck, scale);
				float num = angleFlat + Rand.Range(-30f, 30f);
				if (num > 180f)
				{
					num = num - 180f + -180f;
				}
				if (num < -180f)
				{
					num = num + 180f + 180f;
				}
				dataStatic.rotation = 0f;
				dataStatic.rotation = num;
				dataStatic.velocityAngle = num;
				dataStatic.velocitySpeed = Rand.Range(70f, 75f);
				Pawn.Map.flecks.CreateFleck(dataStatic);
			}
		}
		DamageDef named = DefDatabase<DamageDef>.GetNamed("Bomb");
		GenExplosion.DoExplosion(position, parent.pawn.MapHeld, Props.range, named, Pawn, 8, -1f, null, null, null, null, null, 1f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, null, doVisualEffects: false, 1.6f, 0f, doSoundEffects: false, null, 1f, null, AffectedCells(target));
		base.Apply(target, dest);
	}

	private List<IntVec3> AffectedCells(LocalTargetInfo target)
	{
		tmpCells.Clear();
		Vector3 vector = Pawn.Position.ToVector3Shifted().Yto0();
		IntVec3 intVec = target.Cell.ClampInsideMap(Pawn.Map);
		if (Pawn.Position == intVec)
		{
			return tmpCells;
		}
		float lengthHorizontal = (intVec - Pawn.Position).LengthHorizontal;
		float num = (float)(intVec.x - Pawn.Position.x) / lengthHorizontal;
		float num2 = (float)(intVec.z - Pawn.Position.z) / lengthHorizontal;
		intVec.x = Mathf.RoundToInt((float)Pawn.Position.x + num * Props.range);
		intVec.z = Mathf.RoundToInt((float)Pawn.Position.z + num2 * Props.range);
		float target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
		float num3 = Props.lineWidthEnd / 2f;
		float num4 = Mathf.Sqrt(Mathf.Pow((intVec - Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
		float num5 = 57.29578f * Mathf.Asin(num3 / num4);
		int num6 = GenRadial.NumCellsInRadius(Props.range);
		for (int i = 0; i < num6; i++)
		{
			IntVec3 intVec2 = Pawn.Position + GenRadial.RadialPattern[i];
			if (Canusecell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), target2)) <= num5)
			{
				tmpCells.Add(intVec2);
			}
		}
		List<IntVec3> list = GenSight.BresenhamCellsBetween(Pawn.Position, intVec);
		for (int j = 0; j < list.Count; j++)
		{
			IntVec3 intVec3 = list[j];
			if (!tmpCells.Contains(intVec3) && Canusecell(intVec3))
			{
				tmpCells.Add(intVec3);
			}
		}
		return tmpCells;
	}

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		yield return new PreCastAction
		{
			action = delegate(LocalTargetInfo a, LocalTargetInfo b)
			{
				EffecterDef named = DefDatabase<EffecterDef>.GetNamed("CMC_PulseWave");
				parent.AddEffecterToMaintain(named.Spawn(parent.pawn.Position, a.Cell, parent.pawn.Map), Pawn.Position, a.Cell, 17, Pawn.MapHeld);
			},
			ticksAwayFromCast = 8
		};
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectedCells(target));
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Pawn.Faction != null)
		{
			foreach (IntVec3 item in AffectedCells(target))
			{
				List<Thing> thingList = item.GetThingList(Pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].Faction == Pawn.Faction)
					{
						return false;
					}
				}
			}
			return true;
		}
		return true;
	}
}
