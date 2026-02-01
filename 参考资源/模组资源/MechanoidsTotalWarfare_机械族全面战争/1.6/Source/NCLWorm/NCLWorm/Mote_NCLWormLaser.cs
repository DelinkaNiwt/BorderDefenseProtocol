using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLWorm;

[StaticConstructorOnStartup]
public class Mote_NCLWormLaser : ThingWithComps
{
	public int lifeTick = 180;

	public Vector3 MinLaserPos_A_Start;

	public Vector3 MinLaserPos_A_End;

	public float MinLaserPos_A_Range = 0.5f;

	public float MinLaserPos_A_Range_Limit = 0.4f;

	public Vector3 MinLaserPos_B_Start;

	public Vector3 MinLaserPos_B_End;

	public float MinLaserPos_B_Range = -0.5f;

	public float MinLaserPos_B_Range_Limit = 0.4f;

	public Vector3 MinLaserPos_C_Start;

	public Vector3 MinLaserPos_C_End;

	public float MinLaserPos_C_Range = 0.2f;

	public float MinLaserPos_C_Range_Limit = 0.4f;

	public Vector3 MinLaserPos_D_Start;

	public Vector3 MinLaserPos_D_End;

	public float MinLaserPos_D_Range = -0.2f;

	public float MinLaserPos_D_Range_Limit = 0.4f;

	public Vector3 MinLaserPos_E_Start;

	public Vector3 MinLaserPos_E_End;

	public float MinLaserPos_E_Range = -0.1f;

	public float MinLaserPos_E_Range_Limit = 0.4f;

	public Vector3 MinLaserPos_F_Start;

	public Vector3 MinLaserPos_F_End;

	public float MinLaserPos_F_Range = 0.1f;

	public float MinLaserPos_F_Range_Limit = 0.4f;

	public Thing CCaster;

	public Vector2 RealPos;

	public float Angle;

	public float MinLaser_Rotate_Speed = 2f;

	private List<IntVec3> ListAllPos = new List<IntVec3>();

	private Color[] Rainbow = new Color[7]
	{
		Color.red,
		new Color(1f, 0.5f, 0f, 0.8f),
		Color.yellow,
		Color.green,
		Color.blue,
		new Color(0.29f, 0f, 0.51f, 0.8f),
		new Color(0.5f, 0f, 0.5f, 0.8f)
	};

	public bool MinLaserPos_A_UpOrDown = true;

	public bool MinLaserPos_B_UpOrDown = false;

	public bool MinLaserPos_C_UpOrDown = true;

	public bool MinLaserPos_D_UpOrDown = false;

	public bool MinLaserPos_E_UpOrDown = true;

	public bool MinLaserPos_F_UpOrDown = false;

	public Color DarkRed => new Color(((float)(75 + lifeTick) + Rand.Range(-80f, 80f)) / 255f, ((float)(lifeTick / 2 + 20) + Rand.Range(-80f, 80f)) / 255f, ((float)(lifeTick / 2 + 20) + Rand.Range(-80f, 80f)) / 255f);

	public Vector3 HeadOffsetAt(Thing caster, Vector3 BasePos, Rot4 rotation)
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

	public void AbSpawn(Thing baseThing, Vector2 RealPos, List<IntVec3> ListAllPos)
	{
		CCaster = baseThing;
		this.RealPos = RealPos;
		this.ListAllPos = ListAllPos;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref MinLaserPos_A_Range, "MinLaserPos_A_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_A_UpOrDown, "MinLaserPos_A_UpOrDown", defaultValue: false);
		Scribe_Values.Look(ref MinLaserPos_B_Range, "MinLaserPos_B_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_B_UpOrDown, "MinLaserPos_B_UpOrDown", defaultValue: false);
		Scribe_Values.Look(ref MinLaserPos_C_Range, "MinLaserPos_C_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_C_UpOrDown, "MinLaserPos_C_UpOrDown", defaultValue: false);
		Scribe_Values.Look(ref MinLaserPos_D_Range, "MinLaserPos_D_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_D_UpOrDown, "MinLaserPos_D_UpOrDown", defaultValue: false);
		Scribe_Values.Look(ref MinLaserPos_E_Range, "MinLaserPos_E_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_E_UpOrDown, "MinLaserPos_E_UpOrDown", defaultValue: false);
		Scribe_Values.Look(ref MinLaserPos_F_Range, "MinLaserPos_F_Range", 0f);
		Scribe_Values.Look(ref MinLaserPos_F_UpOrDown, "MinLaserPos_F_UpOrDown", defaultValue: false);
		Scribe_References.Look(ref CCaster, "CCaster");
		Scribe_Values.Look(ref RealPos, "RealPos");
		Scribe_Values.Look(ref Angle, "Angle", 0f);
		DeepProfiler.Start("Load All ListPos");
		Scribe_Collections.Look(ref ListAllPos, "ListAllPos", LookMode.Value);
		DeepProfiler.End();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Vector3 vector = HeadOffsetAt(CCaster, CCaster.DrawPos, CCaster.Rotation);
		Vector3 vector2 = new Vector3(RealPos.x, DrawPos.y, RealPos.y);
		float x = 2f;
		int num = 20;
		int num2 = 160;
		int num3 = 230;
		int num4 = 240;
		if (180 - lifeTick < num)
		{
			x = (float)(180 - lifeTick) / 10f;
		}
		if (180 - lifeTick <= num3 && 180 - lifeTick > num2)
		{
			x = 15f;
		}
		else if (180 - lifeTick <= num4 && 180 - lifeTick > num3)
		{
			x = 15f - (float)(180 - lifeTick - num3) * 1.5f;
		}
		float angle = (vector - vector2).AngleFlat();
		float a = 0.8f;
		if (180 - lifeTick >= 235)
		{
			a = 0.8f - (float)(180 - lifeTick - 235) / 62f;
		}
		Vector3 vector3 = vector;
		vector3.y = AltitudeLayer.PawnRope.AltitudeFor(3f);
		Vector3 vect = default(Vector3);
		for (int i = 0; i < 500; i += 50)
		{
			Vector3 vector4 = AngleIncrement(vector2, i, angle);
			if (!vector4.InBounds(base.Map))
			{
				break;
			}
			vect = vector4;
		}
		float lengthHorizontal = (vector3.ToIntVec3() - vect.ToIntVec3()).LengthHorizontal;
		float num5 = 2f;
		float z = lengthHorizontal * num5 + 100f;
		Color darkRed = DarkRed;
		darkRed.a = a;
		Material material = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get("UI/Misc/Laser"), ShaderDatabase.Transparent, darkRed);
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(vector3, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(x, 1f, z));
		Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		Vector3 vector5 = HeadOffsetAt(CCaster, CCaster.DrawPos, CCaster.Rotation);
		Vector3 vector6 = new Vector3(RealPos.x, DrawPos.y, RealPos.y);
		float angle2 = (vector5 - vector6).AngleFlat();
		vector5 = AngleIncrement(vector5, 2f, angle2);
		float num6 = 2f;
		float a2 = 0.8f;
		if (lifeTick <= 180)
		{
			if (lifeTick >= 20)
			{
				num6 = 12f - (float)(180 - lifeTick) * (11f / 160f);
				a2 = 0.3f + (float)(180 - lifeTick) * 0.003125f;
			}
			else if (lifeTick >= 15)
			{
				num6 = 1f;
				a2 = 0.8f;
			}
			else
			{
				float f = (float)(15 - lifeTick) / 15f;
				num6 = 1f + Mathf.Pow(f, 0.4f) * 5f;
				a2 = 0.8f - Mathf.Pow(f, 0.4f) * 0.7f;
			}
		}
		Vector3 pos = vector5;
		pos.y = AltitudeLayer.PawnUnused.AltitudeFor(3f);
		Color white = Color.white;
		white.a = a2;
		Material material2 = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get("UI/Misc/NCLRing"), ShaderDatabase.Transparent, white);
		Matrix4x4 matrix2 = default(Matrix4x4);
		matrix2.SetTRS(pos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(num6, 1f, num6));
		Graphics.DrawMesh(MeshPool.plane10, matrix2, material2, 0);
		Draw_MinLaserPos(MinLaserPos_A_UpOrDown, MinLaserPos_A_Start, MinLaserPos_A_End);
		Draw_MinLaserPos(MinLaserPos_B_UpOrDown, MinLaserPos_B_Start, MinLaserPos_B_End);
		Draw_MinLaserPos(MinLaserPos_C_UpOrDown, MinLaserPos_C_Start, MinLaserPos_C_End);
		Draw_MinLaserPos(MinLaserPos_D_UpOrDown, MinLaserPos_D_Start, MinLaserPos_D_End);
		Draw_MinLaserPos(MinLaserPos_E_UpOrDown, MinLaserPos_E_Start, MinLaserPos_E_End);
		Draw_MinLaserPos(MinLaserPos_F_UpOrDown, MinLaserPos_F_Start, MinLaserPos_F_End);
	}

	protected override void Tick()
	{
		base.Tick();
		lifeTick--;
		if (lifeTick >= 30 && lifeTick % 60 == 0)
		{
			NCLWormDefOf.NCLLaserWarmup.PlayOneShot(new TargetInfo(base.Position, base.Map));
		}
		if (lifeTick >= 30 && lifeTick % 5 == 0)
		{
			for (int i = 0; i < 8; i++)
			{
				Vector3 vector = HeadOffsetAt(CCaster, CCaster.DrawPos, CCaster.Rotation);
				Vector3 vector2 = new Vector3(RealPos.x, DrawPos.y, RealPos.y);
				float angle = (vector - vector2).AngleFlat();
				vector = AngleIncrement(vector, 2f, angle);
				FleckMaker.ThrowMicroSparks(vector, base.Map);
				FleckMaker.ThrowLightningGlow(vector, base.Map, 0.5f);
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, base.Map, NCLWormDefOf.Fleck_NCLStarFire, Rand.Range(0.3f, 0.5f));
				float num = (new Vector3(RealPos.x, DrawPos.y, RealPos.y) - vector).AngleFlat() + Rand.Range(-40f, 40f);
				if (num > 180f)
				{
					num -= 360f;
				}
				if (num < -180f)
				{
					num += 360f;
				}
				dataStatic.velocityAngle = num;
				dataStatic.velocitySpeed = Rand.Range(5f, 10f);
				base.Map.flecks.CreateFleck(dataStatic);
				if (lifeTick % 20 == 0)
				{
					NCLWormDefOf.MechBandElectricityArc.Spawn(ListAllPos.RandomElement(), base.Map);
				}
			}
		}
		if (lifeTick <= 20 && lifeTick % 5 == 0)
		{
			KeliKeli();
		}
		if (lifeTick <= 20 && lifeTick >= 10 && lifeTick % 10 == 0)
		{
			NCLWormDefOf.Explosion_MechBandShockwave.PlayOneShot(new TargetInfo(base.Position, base.Map));
			TakeDamage();
		}
		if (lifeTick == -40)
		{
			Destroy();
		}
		UpdateMinLaserPosition(ref MinLaserPos_A_UpOrDown, ref MinLaserPos_A_Range, MinLaserPos_A_Range_Limit, out MinLaserPos_A_Start, out MinLaserPos_A_End, 0f);
		UpdateMinLaserPosition(ref MinLaserPos_B_UpOrDown, ref MinLaserPos_B_Range, MinLaserPos_B_Range_Limit, out MinLaserPos_B_Start, out MinLaserPos_B_End, 180f);
		UpdateMinLaserPosition(ref MinLaserPos_C_UpOrDown, ref MinLaserPos_C_Range, MinLaserPos_C_Range_Limit, out MinLaserPos_C_Start, out MinLaserPos_C_End, 60f);
		UpdateMinLaserPosition(ref MinLaserPos_D_UpOrDown, ref MinLaserPos_D_Range, MinLaserPos_D_Range_Limit, out MinLaserPos_D_Start, out MinLaserPos_D_End, 240f);
		UpdateMinLaserPosition(ref MinLaserPos_E_UpOrDown, ref MinLaserPos_E_Range, MinLaserPos_E_Range_Limit, out MinLaserPos_E_Start, out MinLaserPos_E_End, 120f);
		UpdateMinLaserPosition(ref MinLaserPos_F_UpOrDown, ref MinLaserPos_F_Range, MinLaserPos_F_Range_Limit, out MinLaserPos_F_Start, out MinLaserPos_F_End, 300f);
	}

	public void KeliKeli()
	{
		Vector3 vector = HeadOffsetAt(CCaster, CCaster.DrawPos, CCaster.Rotation);
		float num = (DrawPos - vector).AngleFlat();
		int num2 = 0;
		if (lifeTick <= 20 && lifeTick > 0)
		{
			num2 = 20;
		}
		else if (lifeTick <= 0 && lifeTick >= -40)
		{
			num2 = (int)(20f * (1f - (float)Mathf.Abs(lifeTick) / 40f));
		}
		float num3 = (float)Mathf.Max(0, 60 - lifeTick) / 100f;
		var array = new[]
		{
			new
			{
				offset = 0.5f,
				useShifted = true
			},
			new
			{
				offset = 0.3f,
				useShifted = false
			}
		};
		var array2 = array;
		foreach (var anon in array2)
		{
			for (int j = 0; j <= num2; j++)
			{
				float num4 = Rand.Range(0f - anon.offset, anon.offset);
				Vector3 vector2 = (anon.useShifted ? ListAllPos.RandomElement().ToVector3Shifted() : ListAllPos.RandomElement().ToVector3());
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector2 + new Vector3(num4, 0f, num4), base.Map, NCLWormDefOf.Fleck_NCLStar, Rand.Range(1f, 1.5f));
				dataStatic.rotation = num + 90f;
				dataStatic.velocityAngle = num;
				dataStatic.velocitySpeed = Rand.Range(40f, 60f);
				dataStatic.instanceColor = Rainbow.RandomElement();
				dataStatic.def.solidTime = 0.6f - num3;
				base.Map.flecks.CreateFleck(dataStatic);
			}
		}
	}

	public void TakeDamage()
	{
		Map map = base.Map;
		foreach (IntVec3 listAllPo in ListAllPos)
		{
			List<Thing> thingList = listAllPo.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				DamageInfo dinfo = new DamageInfo(DefDatabase<DamageDef>.GetNamed("TW_HyperBeam_Damage", errorOnFail: false) ?? DamageDefOf.Vaporize, 100f, 5f, 0f, CCaster);
				thingList[i].TakeDamage(dinfo);
			}
			Vector3 loc = listAllPo.ToVector3Shifted();
			if (loc.ShouldSpawnMotesAt(map) && Rand.Chance(0.8f))
			{
				loc -= new Vector3(0.5f, 0f, 0.5f);
				loc += new Vector3(Rand.Value, 0f, Rand.Value);
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, NCLWormDefOf.NCL_Fleck_BurnerUsedEmber, Rand.Range(0.3f, 0.8f));
				dataStatic.rotation = Rand.Range(0f, 360f);
				dataStatic.rotationRate = Rand.Range(-12f, 12f);
				dataStatic.velocityAngle = Rand.Range(35, 45);
				dataStatic.velocitySpeed = 1.2f;
				map.flecks.CreateFleck(dataStatic);
			}
			FleckMaker.Static(listAllPo, map, NCLWormDefOf.Fleck_BeamBurn, 5f);
			if (listAllPo.Impassable(map))
			{
				GenExplosion.DoExplosion(listAllPo, map, 5.6f, DefDatabase<DamageDef>.GetNamed("TW_HyperBeam_Damage", errorOnFail: false) ?? DamageDefOf.Vaporize, this, 800, 5f);
				break;
			}
		}
	}

	private void UpdateMinLaserPosition(ref bool upOrDown, ref float range, float rangeLimit, out Vector3 startPos, out Vector3 endPos, float angleOffset)
	{
		if (upOrDown)
		{
			range -= 0.01f;
			if (range <= 0f - rangeLimit)
			{
				upOrDown = false;
			}
		}
		else
		{
			range += 0.01f;
			if (range >= rangeLimit)
			{
				upOrDown = true;
			}
		}
		Vector3 vector = HeadOffsetAt(CCaster, CCaster.DrawPos, CCaster.Rotation);
		Vector3 vector2 = new Vector3(RealPos.x, DrawPos.y, RealPos.y);
		float num = (vector - vector2).AngleFlat();
		float num2 = num - 90f;
		if (num2 < 0f)
		{
			num2 += 360f;
		}
		Vector3 center = vector;
		startPos = AngleIncrement(center, range, num2);
		float range2 = 12f - (180f - (float)lifeTick) / 15f;
		float num3 = angleOffset + (float)(180 - lifeTick) * MinLaser_Rotate_Speed;
		num3 %= 360f;
		if (num3 < 0f)
		{
			num3 += 360f;
		}
		endPos = AngleIncrement(vector2, range2, num3);
	}

	public void Draw_MinLaserPos(bool UpOrDown, Vector3 Start, Vector3 End)
	{
		float incOffset = 4f;
		if (!UpOrDown)
		{
			incOffset = 2f;
		}
		Vector3 vector = (Start + End) / 2f;
		vector.y = AltitudeLayer.PawnRope.AltitudeFor(incOffset);
		float x = 1f;
		int num = 20;
		int num2 = 160;
		int num3 = 170;
		int num4 = 180;
		if (180 - lifeTick < num)
		{
			x = (float)(180 - lifeTick) / 20f;
		}
		if (180 - lifeTick <= num3 && 180 - lifeTick > num2)
		{
			x = 5f;
		}
		else if (180 - lifeTick <= num4 && 180 - lifeTick > num3)
		{
			x = 5f - (float)(180 - Mathf.Max(lifeTick, 0) - num3) / 2f;
		}
		float angle = (Start - End).AngleFlat();
		float a = 0.8f;
		if (lifeTick <= 5)
		{
			a = (float)Mathf.Max(lifeTick, 0) * 0.2f;
		}
		Vector3 vector2 = Start;
		vector2.y = AltitudeLayer.PawnRope.AltitudeFor(3f);
		Vector3 vect = default(Vector3);
		for (int i = 0; i < 500; i += 50)
		{
			Vector3 vector3 = AngleIncrement(End, i, angle);
			if (!vector3.InBounds(base.Map))
			{
				break;
			}
			vect = vector3;
		}
		float lengthHorizontal = (vector2.ToIntVec3() - vect.ToIntVec3()).LengthHorizontal;
		float num5 = 2f;
		float z = lengthHorizontal * num5 + 100f;
		Color darkRed = DarkRed;
		darkRed.a = a;
		Material material = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get("UI/Misc/Laser"), ShaderDatabase.Transparent, darkRed);
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(vector2, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(x, 1f, z));
		Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
	}

	public Vector3 AngleIncrement(Vector3 center, float range, float angle)
	{
		float f = angle * ((float)Math.PI / 180f);
		float x = center.x - range * Mathf.Sin(f);
		float z = center.z - range * Mathf.Cos(f);
		return new Vector3(x, center.y, z);
	}
}
