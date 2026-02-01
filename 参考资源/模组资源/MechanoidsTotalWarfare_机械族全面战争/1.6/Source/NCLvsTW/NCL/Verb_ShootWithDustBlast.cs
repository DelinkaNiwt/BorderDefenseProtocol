using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Verb_ShootWithDustBlast : Verb_Shoot
{
	private class MovingDustPuff
	{
		public Vector3 position;

		public Vector3 direction;

		public float distanceTraveled;

		public float size;

		public float rotation;

		public static readonly float MaxDistance = 8f;
	}

	private List<MovingDustPuff> movingPuffs = new List<MovingDustPuff>();

	private const float PuffSpeed = 0.4f;

	protected override bool TryCastShot()
	{
		bool shotFired = base.TryCastShot();
		if (shotFired && Caster != null && Caster.Map != null)
		{
			Vector3 shotDirection = GetShotDirection().normalized;
			Vector3 backblastDirection = -shotDirection;
			int puffCount = Rand.Range(0, 2);
			for (int i = 0; i < puffCount; i++)
			{
				Vector3 spreadDirection = backblastDirection.RotatedBy(Rand.Range(-45f, 45f)).normalized;
				Vector3 spawnPosition = Caster.DrawPos + spreadDirection * Rand.Range(2f, 3f);
				CreateMovingDustPuff(spawnPosition, Rand.Range(2.4f, 3.6f), spreadDirection, Rand.Range(0f, 360f));
			}
			CreateForwardFlares(shotDirection);
		}
		return shotFired;
	}

	private void CreateForwardFlares(Vector3 shotDirection)
	{
		if (Caster.Map != null)
		{
			Vector3 flareStartPos = Caster.DrawPos + shotDirection * 2f;
			flareStartPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			int flareCount = Rand.Range(3, 6);
			for (int i = 0; i < flareCount; i++)
			{
				Vector3 flareDirection = shotDirection.RotatedBy(Rand.Range(-5f, 5f));
				FleckCreationData flareData = FleckMaker.GetDataStatic(flareStartPos, Caster.Map, FleckDefOf.ShotFlash, Rand.Range(1.2f, 1.8f));
				flareData.velocity = flareDirection * Rand.Range(3f, 8.5f);
				flareData.rotationRate = Rand.Range(-180f, 180f);
				flareData.solidTimeOverride = 0.5f;
				Caster.Map.flecks.CreateFleck(flareData);
			}
			CreateForwardSparks(flareStartPos, shotDirection);
		}
	}

	private void CreateForwardSparks(Vector3 position, Vector3 direction)
	{
		if (Caster.Map != null)
		{
			position.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			int sparkCount = Rand.Range(4, 8);
			for (int i = 0; i < sparkCount; i++)
			{
				FleckCreationData sparkData = FleckMaker.GetDataStatic(position, Caster.Map, FleckDefOf.MicroSparks, Rand.Range(0.6f, 0.9f));
				Vector3 sparkDirection = direction.RotatedBy(Rand.Range(-10f, 10f));
				sparkData.velocity = sparkDirection * Rand.Range(2.5f, 4f);
				sparkData.rotationRate = Rand.Range(-120f, 120f);
				Caster.Map.flecks.CreateFleck(sparkData);
			}
		}
	}

	private Vector3 GetShotDirection()
	{
		if (currentTarget.IsValid)
		{
			return (currentTarget.CenterVector3 - Caster.DrawPos).normalized;
		}
		return Caster.Rotation.FacingCell.ToVector3();
	}

	private void CreateMovingDustPuff(Vector3 position, float size, Vector3 direction, float rotation)
	{
		Vector3 puffPos = position;
		puffPos.y = AltitudeLayer.MoteLow.AltitudeFor();
		FleckCreationData data = FleckMaker.GetDataStatic(puffPos, Caster.Map, FleckDefOf.DustPuff, size * Rand.Range(0.9f, 1.1f));
		data.velocity = direction * Rand.Range(0.8f, 3f);
		data.rotation = rotation;
		data.rotationRate = Rand.Range(-90f, 90f);
		data.solidTimeOverride = Mathf.Clamp(size * 0.3f, 0.2f, 0.6f);
		if (Caster.Map != null)
		{
			Caster.Map.flecks.CreateFleck(data);
		}
	}
}
