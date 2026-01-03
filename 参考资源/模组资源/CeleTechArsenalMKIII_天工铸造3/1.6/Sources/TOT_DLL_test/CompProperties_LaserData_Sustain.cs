using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_LaserData_Sustain : CompProperties
{
	public ThingDef LaserLine_MoteDef;

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

	public int ScatterRadius = 1;

	public DamageDef ScatterExplosionDef = DamageDefOf.Bomb;

	public int ScatterExplosionDamage = 1;

	public float ScatterExplosionRadius = 1f;

	public float ScatterExplosionArmorPenetration = 1f;

	public int ScatterTickMax = 1;

	public FleckDef LaserFleck_ScatterLaser;

	public CompProperties_LaserData_Sustain()
	{
		compClass = typeof(Comp_LaserData_Sustain);
	}
}
