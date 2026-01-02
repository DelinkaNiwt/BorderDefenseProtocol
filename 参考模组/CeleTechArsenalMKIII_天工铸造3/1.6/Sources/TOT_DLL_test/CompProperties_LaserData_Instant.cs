using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_LaserData_Instant : CompProperties
{
	public FleckDef LaserLine_FleckDef;

	public FleckDef LaserLine_FleckDef2;

	public FleckDef MuzzleGlow;

	public FleckDef LaserFleck_End;

	public float LaserFleck_End_Scale_Base = 1f;

	public FleckDef LaserFleck_Spark;

	public float LaserFleck_Spark_Scale_Base = 1f;

	public float LaserFleck_Spark_Scale_Deviation = 0f;

	public int LaserFleck_Spark_Num = 1;

	public float LaserFleck_Spark_Spawn_Chance = 1f;

	public int Color_Red = 255;

	public int Color_Green = 255;

	public int Color_Blue = 255;

	public float Color_Alpha = 0.5f;

	public float StartPositionOffset_Range = 0f;

	public float turretyoffset = 0f;

	public bool useyoffset = false;

	public SoundDef SoundDef;

	public DamageDef DamageDef = DamageDefOf.Cut;

	public int DamageNum = 1;

	public float DamageArmorPenetration = 0f;

	public bool IfSecondDamage = false;

	public DamageDef DamageDef_B = DamageDefOf.Cut;

	public int DamageNum_B = 1;

	public float DamageArmorPenetration_B = 0f;

	public bool IfCanScatter = false;

	public int ScatterNum = 1;

	public float ScatterRadius = 1f;

	public DamageDef ScatterExplosionDef = DamageDefOf.Bomb;

	public int ScatterExplosionDamage = 1;

	public float ScatterExplosionRadius = 1f;

	public float ScatterExplosionArmorPenetration = 1f;

	public int ScatterTickMax = 1;

	public FleckDef LaserFleck_ScatterLaser;

	public bool IfCanDivide = false;

	public int DivideRadius = 1;

	public bool RandomRGB = false;

	public CompProperties_LaserData_Instant()
	{
		compClass = typeof(Comp_LaserData_Instant);
	}
}
