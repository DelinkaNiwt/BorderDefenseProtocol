using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
[DefOf]
public class CMC_Def
{
	public static ThingDef CMCShieldGenerator;

	public static ThingDef CMC_Mote_ChipBoosted;

	public static ThingDef CMC_Mote_SwordShowerPawnBG;

	public static ThingDef CMC_Mote_SWTargetLocked;

	public static ThingDef CMC_Mote_MissileLocked;

	public static ThingDef CMC_SkillDummy;

	public static ThingDef CMC_FCradar;

	public static ThingDef CMC_CICAESA_Radar;

	public static ThingDef CMCML;

	public static ThingDef CMC_Svcannon;

	public static ThingDef CMC_HJX_ATGM;

	public static ThingDef CMC_RocketswarmLauncher;

	public static ThingDef CMC_SAML;

	public static FleckDef CMC_PulsingDistortionRing;

	public static FleckDef CMC_TeleportExit;

	public static FleckDef CMC_TeleportSpawn;

	public static EffecterDef CMC_TeleportEffector;

	public static EffecterDef CMC_Bomb;

	public static JobDef CMCTS_TradeWithShip;

	public static ThingDef CMC_TraderShuttle;

	public static ThingDef CMC_TraderShuttle_A;

	public static ThingDef CMC_TraderShuttle_S;

	public static ThingDef Bullet_CMC_Fragments;

	public static ThingDef CMC_LandPlatform;

	public static ResearchProjectDef CMC_FloatingGunIII;

	public static ResearchProjectDef CMC_FloatingGunIV;

	public static ResearchProjectDef CMC_FloatingGunV;

	public static ResearchProjectDef CMC_FloatingGunVI;

	static CMC_Def()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(CMC_Def));
	}
}
