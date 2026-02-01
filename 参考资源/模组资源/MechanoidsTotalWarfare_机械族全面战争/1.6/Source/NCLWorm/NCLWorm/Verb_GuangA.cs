using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class Verb_GuangA : Verb
{
	private static readonly IntVec3[] cellOffsets = new IntVec3[9]
	{
		new IntVec3(1, 0, 1),
		new IntVec3(1, 0, -1),
		new IntVec3(1, 0, 0),
		new IntVec3(0, 0, 1),
		IntVec3.Zero,
		new IntVec3(0, 0, -1),
		new IntVec3(-1, 0, 1),
		new IntVec3(-1, 0, 0),
		new IntVec3(-1, 0, -1)
	};

	private static readonly IntVec3[] cellOffsetsMKII = new IntVec3[25]
	{
		new IntVec3(-2, 0, -2),
		new IntVec3(-2, 0, -1),
		new IntVec3(-2, 0, 0),
		new IntVec3(-2, 0, 1),
		new IntVec3(-2, 0, 2),
		new IntVec3(-1, 0, -2),
		new IntVec3(-1, 0, -1),
		new IntVec3(-1, 0, 0),
		new IntVec3(-1, 0, 1),
		new IntVec3(-1, 0, 2),
		new IntVec3(0, 0, -2),
		new IntVec3(0, 0, -1),
		IntVec3.Zero,
		new IntVec3(0, 0, 1),
		new IntVec3(0, 0, 2),
		new IntVec3(1, 0, -2),
		new IntVec3(1, 0, -1),
		new IntVec3(1, 0, 0),
		new IntVec3(1, 0, 1),
		new IntVec3(1, 0, 2),
		new IntVec3(2, 0, -2),
		new IntVec3(2, 0, -1),
		new IntVec3(2, 0, 0),
		new IntVec3(2, 0, 1),
		new IntVec3(2, 0, 2)
	};

	protected override int ShotsPerBurst => base.BurstShotCount;

	public override void WarmupComplete()
	{
		base.WarmupComplete();
	}

	protected override bool TryCastShot()
	{
		DamageOne();
		SelfConsume();
		return true;
	}

	public void DamageOne()
	{
		Vector3 vector = HeadOffsetAt(caster.DrawPos, caster.Rotation);
		vector = HeadOffsetAt(caster.DrawPos, caster.Rotation);
		Vector3 centerVector = currentTarget.CenterVector3;
		float angle = (vector - centerVector).AngleFlat();
		vector = AngleIncrement(vector, 2f, angle);
		List<IntVec3> list = new List<IntVec3>(250);
		for (int i = 0; i < 500; i += 2)
		{
			Vector3 vector2 = AngleIncrement(vector, i, angle);
			if (!vector2.InBounds(caster.Map))
			{
				break;
			}
			list.Add(vector2.ToIntVec3());
		}
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		foreach (IntVec3 item in list)
		{
			IntVec3[] array = cellOffsets;
			foreach (IntVec3 intVec in array)
			{
				IntVec3 intVec2 = item + intVec;
				if (intVec2.InBounds(caster.Map))
				{
					hashSet.Add(intVec2);
				}
			}
		}
		IntVec3 cell = currentTarget.Cell;
		Vector2 realPos = new Vector2(currentTarget.CenterVector3.x, currentTarget.CenterVector3.z);
		Mote_NCLWormLaser newThing = (Mote_NCLWormLaser)ThingMaker.MakeThing(NCLWormDefOf.Mote_NCLWormLaser);
		((Mote_NCLWormLaser)GenSpawn.Spawn(newThing, cell, caster.Map)).AbSpawn(caster, realPos, hashSet.ToList());
	}

	public Vector3 HeadOffsetAt(Vector3 BasePos, Rot4 rotation)
	{
		return rotation.AsInt switch
		{
			0 => BasePos + caster.def.race.headPosPerRotation[0], 
			1 => BasePos + caster.def.race.headPosPerRotation[1], 
			2 => BasePos + caster.def.race.headPosPerRotation[2], 
			3 => BasePos + caster.def.race.headPosPerRotation[3], 
			_ => BasePos, 
		};
	}

	public override void Notify_EquipmentLost()
	{
		base.Notify_EquipmentLost();
		if (state == VerbState.Bursting && burstShotsLeft < base.BurstShotCount)
		{
			SelfConsume();
		}
	}

	private void SelfConsume()
	{
		if (base.EquipmentSource != null && !base.EquipmentSource.Destroyed)
		{
			base.EquipmentSource.Destroy();
		}
	}

	public Vector3 AngleIncrement(Vector3 center, float range, float angle)
	{
		float f = angle * ((float)Math.PI / 180f);
		float x = center.x - range * Mathf.Sin(f);
		float z = center.z - range * Mathf.Cos(f);
		return new Vector3(x, center.y, z);
	}
}
