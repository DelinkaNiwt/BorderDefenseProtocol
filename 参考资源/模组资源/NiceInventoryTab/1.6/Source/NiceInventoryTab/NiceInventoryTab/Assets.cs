using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

[StaticConstructorOnStartup]
public static class Assets
{
	public struct IconColor
	{
		public Texture2D Icon;

		public Color Color;

		public IconColor(Texture2D icon, Color color)
		{
			Icon = icon;
			Color = color;
		}

		public IconColor(Color color, Texture2D icon)
		{
			Icon = icon;
			Color = color;
		}

		public (Texture2D icon, Color col) Deconstruct()
		{
			return (icon: Icon, col: Color);
		}
	}

	public static readonly Color ColorButtons;

	public static readonly Color ColorBG;

	public static readonly Color ColorBGL;

	public static readonly Color ColorBGD;

	public static readonly Color ColorStat;

	public static readonly Color ColorBGYellow;

	public static readonly Color ColorLightGray;

	public static readonly Color EnviromentPenaltyColor;

	public static readonly Color PenaltyColor;

	public static readonly Color BuffColor;

	public static readonly Color GearWarningColor;

	public static readonly Color HediffBuffColor;

	public static readonly List<Color> ColorPaletteRandom;

	public static readonly Texture2D Checkbox1Tex;

	public static readonly Texture2D Checkbox0Tex;

	public static readonly Texture2D VanilaListTex;

	public static readonly Texture2D PortraitTex;

	public static readonly Texture2D MedTex;

	public static readonly Texture2D AltTex;

	public static readonly Texture2D ProgressTex;

	public static readonly Texture2D HGradientTex;

	public static readonly Texture2D VGradientTex;

	public static readonly Texture2D SettingsTex;

	public static readonly Texture2D NoIconRecipeTex;

	public static readonly Texture2D DiagTiledTex;

	public static readonly Texture2D BuffTiledTex;

	public static readonly Texture2D ApparelSlotTex;

	public static readonly Texture2D BeltSlotTex;

	public static readonly Texture2D LockedTex;

	public static readonly Texture2D LockedButtonTex;

	public static readonly Texture2D SwapButtonTex;

	public static readonly Texture2D QualityTex;

	public static readonly Texture2D GlowQualityTex;

	public static readonly IconColor GlowTex;

	public static readonly IconColor ICDamageRanged;

	public static readonly IconColor ICDamageMelee;

	public static readonly IconColor ICDamageRangedBlue;

	public static readonly IconColor ICDamageRangedNeutral;

	public static readonly IconColor ICMass;

	public static readonly IconColor ICItemHP;

	public static readonly IconColor ICArmorSharp;

	public static readonly IconColor ICArmorBlunt;

	public static readonly IconColor ICArmorHeat;

	public static readonly IconColor ICArmorPen;

	public static readonly IconColor ICMoveSpeed;

	public static readonly IconColor ICWork;

	public static readonly IconColor ICRange;

	public static readonly IconColor ICReload;

	public static readonly IconColor ICStop;

	public static readonly IconColor ICFireRate;

	public static readonly IconColor ICShieldYes;

	public static readonly IconColor ICShieldNo;

	public static readonly IconColor ICMeleeAccuracy;

	public static readonly IconColor ICFlame;

	public static readonly IconColor ICEMP;

	public static readonly IconColor ICNerve;

	public static readonly IconColor ICBeam;

	public static readonly IconColor ICBoom;

	public static readonly IconColor ICSmoke;

	public static readonly IconColor ICToxic;

	public static readonly IconColor ICVacuum;

	public static readonly IconColor ICPsy;

	public static readonly IconColor ICPsyGain;

	public static readonly IconColor ICMedical;

	public static readonly IconColor ICSocial;

	public static readonly IconColor ICChat;

	public static readonly IconColor ICWarning;

	public static readonly IconColor ICWarningBG;

	public static readonly IconColor ICWarningBGDead;

	public static readonly IconColor ICWarningDead;

	public static readonly IconColor ICWarningBioCoded;

	public static readonly IconColor ICWarningBGBioCoded;

	public static readonly string Format_KG;

	public static readonly string Format_MoveSpeed;

	public static readonly string Format_Meters;

	public static readonly string Format_Seconds;

	public static readonly string Format_FireRate;

	public static readonly string Format_PD;

	public static DamageDef DamageDef_Nerve;

	public static DamageDef DamageDef_Beam;

	public static ThoughtDef AnyBodyPartCovered_Disapproved_Male;

	public static ThoughtDef AnyBodyPartCovered_Disapproved_Female;

	public static ApparelLayerDef ApparelLayerDefOf_Shield;

	public static StatDef MeleeWeapon_AverageArmorPenetration;

	public static JobDef NIT_MoveApparelToInventory;

	public static JobDef NIT_WearFromInventory;

	public static Color GoodOrBad(bool good)
	{
		if (!good)
		{
			return PenaltyColor;
		}
		return BuffColor;
	}

	public static Color GoodOrBadOrNeutral(float val, float greatergood)
	{
		if (val > greatergood)
		{
			return BuffColor;
		}
		if (val < greatergood)
		{
			return PenaltyColor;
		}
		return Color.white;
	}

	public static Color GoodOrBadEnv(bool b)
	{
		if (!b)
		{
			return EnviromentPenaltyColor;
		}
		return BuffColor;
	}

	public static Color fromHEX(int hex_no_alpha)
	{
		int num = (hex_no_alpha >> 16) & 0xFF;
		int num2 = (hex_no_alpha >> 8) & 0xFF;
		int num3 = hex_no_alpha & 0xFF;
		return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f);
	}

	public static Color fromHEX(int hex_no_alpha, float alpha)
	{
		int num = (hex_no_alpha >> 16) & 0xFF;
		int num2 = (hex_no_alpha >> 8) & 0xFF;
		int num3 = hex_no_alpha & 0xFF;
		return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f, alpha);
	}

	public static Color Darker(Color col, float v = 0.5f)
	{
		return Color.Lerp(col, Color.black, 0.5f);
	}

	public static Texture2D LoadTextureNoDuplicates(string relative)
	{
		return ContentFinder<Texture2D>.Get("NiceInventoryTab/" + relative);
	}

	public static Texture2D LoadTextureNoDuplicates2(string relative)
	{
		return ContentFinder<Texture2D>.Get("NiceBillTab/" + relative);
	}

	public static void DrawDiamond(Vector2 center, float diagonalSize, Color color)
	{
		float num = diagonalSize * 0.5f;
		float z = 45f;
		Vector2 vector = center + new Vector2((0f - num) * 0.5f, (0f - num) * 0.5f);
		Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, z), Vector3.one) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
		Rect rect = new Rect(vector.x, vector.y, num, num);
		GL.PushMatrix();
		GL.MultMatrix(m);
		GUI.DrawTexture(rect, (Texture)BaseContent.WhiteTex, (ScaleMode)0, true, 0f, color, 0f, 0f);
		GL.PopMatrix();
	}

	public static void DrawFlattenedDiamond(Vector2 center, float width, float height, Color color)
	{
		float num = height * 0.5f;
		float x = width / height;
		float z = 45f;
		Vector2 vector = center + new Vector2((0f - num) * 0.5f, (0f - num) * 0.5f);
		Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(x, 1f, 1f)) * Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, z), Vector3.one) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
		Rect rect = new Rect(vector.x, vector.y, num, num);
		GL.PushMatrix();
		GL.MultMatrix(m);
		GUI.DrawTexture(rect, (Texture)BaseContent.WhiteTex, (ScaleMode)0, true, 0f, color, 0f, 0f);
		GL.PopMatrix();
	}

	public static void DrawHighlightHorizontal(Rect r, float intencity = 0.1f)
	{
		GUI.color = new Color(1f, 1f, 1f, intencity);
		GUI.DrawTexture(r, (Texture)HGradientTex);
	}

	public static void DrawHighlightVertical(Rect r, float intencity = 0.1f)
	{
		GUI.color = new Color(1f, 1f, 1f, intencity);
		GUI.DrawTexture(r, (Texture)VGradientTex);
	}

	public static void DrawTilingTexture(Rect rect, Texture2D texture, float tileSize, Vector2 offset)
	{
		GUI.BeginGroup(rect);
		int num = Mathf.CeilToInt(rect.width / tileSize) + 1;
		int num2 = Mathf.CeilToInt(rect.height / tileSize) + 1;
		float num3 = offset.x % tileSize;
		float num4 = offset.y % tileSize;
		for (int i = -1; i < num; i++)
		{
			for (int j = -1; j < num2; j++)
			{
				GUI.DrawTexture(new Rect((float)i * tileSize + num3, (float)j * tileSize + num4, tileSize, tileSize), (Texture)texture);
			}
		}
		GUI.EndGroup();
	}

	public static void DrawCroppedText(Rect rect, TaggedString s)
	{
		GUI.BeginGroup(rect);
		Rect rect2 = rect.AtZero();
		rect2.width = 600f;
		Widgets.Label(rect2, s);
		GUI.EndGroup();
	}

	public static void LabelShadowed(Rect rect, string text, Color col)
	{
		GUI.color = Color.black;
		Widgets.Label(Utils.RectMove(rect, -1f, -1f), text);
		Widgets.Label(Utils.RectMove(rect, 1f, 1f), text);
		Widgets.Label(Utils.RectMove(rect, -1f, 1f), text);
		Widgets.Label(Utils.RectMove(rect, 1f, -1f), text);
		GUI.color = col;
		Widgets.Label(rect, text);
	}

	static Assets()
	{
		ColorButtons = fromHEX(7698554);
		ColorBG = fromHEX(1382685);
		ColorBGL = fromHEX(3093303);
		ColorBGD = fromHEX(921619);
		ColorStat = fromHEX(14935011);
		ColorBGYellow = fromHEX(4145199);
		ColorLightGray = fromHEX(13092807);
		EnviromentPenaltyColor = fromHEX(10066329);
		PenaltyColor = fromHEX(15291476);
		BuffColor = fromHEX(6153282);
		GearWarningColor = fromHEX(5583394);
		HediffBuffColor = fromHEX(16777215);
		ColorPaletteRandom = new List<Color>
		{
			fromHEX(4881497),
			fromHEX(8146431),
			fromHEX(16739179),
			fromHEX(5164484),
			fromHEX(16243823),
			fromHEX(12292046),
			fromHEX(8765922),
			fromHEX(15832202),
			fromHEX(7101671),
			fromHEX(16767293),
			fromHEX(5629605),
			fromHEX(16747136),
			fromHEX(7649791),
			fromHEX(10656766),
			fromHEX(16758892),
			fromHEX(7269251)
		};
		Checkbox1Tex = LoadTextureNoDuplicates("Checkbox_Checked");
		Checkbox0Tex = LoadTextureNoDuplicates("Checkbox_Empty");
		VanilaListTex = LoadTextureNoDuplicates("VanillaList");
		PortraitTex = LoadTextureNoDuplicates("Portrait");
		MedTex = LoadTextureNoDuplicates("Med");
		AltTex = LoadTextureNoDuplicates("Alt");
		ProgressTex = LoadTextureNoDuplicates("BGProgress");
		HGradientTex = LoadTextureNoDuplicates2("HGradient");
		VGradientTex = LoadTextureNoDuplicates2("VGradient");
		SettingsTex = LoadTextureNoDuplicates2("Settings");
		NoIconRecipeTex = LoadTextureNoDuplicates2("NoIconRecipe");
		DiagTiledTex = LoadTextureNoDuplicates("DiagTiled");
		BuffTiledTex = LoadTextureNoDuplicates("BuffTiled");
		ApparelSlotTex = LoadTextureNoDuplicates("ApparelSlot");
		BeltSlotTex = LoadTextureNoDuplicates("Belt");
		LockedTex = LoadTextureNoDuplicates2("Locked");
		LockedButtonTex = LoadTextureNoDuplicates("LockedButton");
		SwapButtonTex = LoadTextureNoDuplicates("SwapButton");
		QualityTex = LoadTextureNoDuplicates("Quality");
		GlowQualityTex = LoadTextureNoDuplicates("QualityGlow");
		GlowTex = new IconColor(LoadTextureNoDuplicates("Glow"), fromHEX(16777215, 0.2f));
		ICDamageRanged = new IconColor(LoadTextureNoDuplicates2("Damage"), fromHEX(16716891));
		ICDamageMelee = new IconColor(LoadTextureNoDuplicates("Melee"), fromHEX(16716891));
		ICDamageRangedBlue = new IconColor(LoadTextureNoDuplicates("Melee"), fromHEX(3516415));
		ICDamageRangedNeutral = new IconColor(LoadTextureNoDuplicates2("Damage"), Color.gray);
		ICMass = new IconColor(LoadTextureNoDuplicates2("Mass"), fromHEX(11382446));
		ICItemHP = new IconColor(LoadTextureNoDuplicates2("Ingot"), fromHEX(12102272));
		ICArmorSharp = new IconColor(fromHEX(16728371), LoadTextureNoDuplicates2("ArmorSharp"));
		ICArmorBlunt = new IconColor(fromHEX(5163505), LoadTextureNoDuplicates2("ArmorBlunt"));
		ICArmorHeat = new IconColor(fromHEX(15321172), LoadTextureNoDuplicates2("ArmorHeat"));
		ICArmorPen = new IconColor(fromHEX(16110974), LoadTextureNoDuplicates2("ArmorPen"));
		ICMoveSpeed = new IconColor(fromHEX(12904788), LoadTextureNoDuplicates2("Move"));
		ICWork = new IconColor(fromHEX(12904788), LoadTextureNoDuplicates2("Efficiency"));
		ICRange = new IconColor(fromHEX(3516415), LoadTextureNoDuplicates2("Range"));
		ICReload = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates2("Reload"));
		ICStop = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates2("StoppingPower"));
		ICFireRate = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates2("Ammo1"));
		ICShieldYes = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates("ShieldYes"));
		ICShieldNo = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates("ShieldNo"));
		ICMeleeAccuracy = new IconColor(fromHEX(5564868), LoadTextureNoDuplicates("MeleeAcc"));
		ICFlame = new IconColor(fromHEX(16765440), LoadTextureNoDuplicates("Fire"));
		ICEMP = new IconColor(fromHEX(6992127), LoadTextureNoDuplicates2("MechPower"));
		ICNerve = new IconColor(fromHEX(16716891), LoadTextureNoDuplicates2("MechPower"));
		ICBeam = new IconColor(fromHEX(2359147), LoadTextureNoDuplicates2("MechPower"));
		ICBoom = new IconColor(fromHEX(16740864), LoadTextureNoDuplicates("Boom"));
		ICSmoke = new IconColor(fromHEX(12171705), LoadTextureNoDuplicates("Boom"));
		ICToxic = new IconColor(fromHEX(8046440), LoadTextureNoDuplicates("Toxin"));
		ICVacuum = new IconColor(fromHEX(7173248), LoadTextureNoDuplicates("Vacuum"));
		ICPsy = new IconColor(fromHEX(12015337), LoadTextureNoDuplicates("Psy"));
		ICPsyGain = new IconColor(fromHEX(12015337), LoadTextureNoDuplicates("PsyGain"));
		ICMedical = new IconColor(fromHEX(16728385), LoadTextureNoDuplicates2("Medicine"));
		ICSocial = new IconColor(fromHEX(16776960), LoadTextureNoDuplicates("Social"));
		ICChat = new IconColor(fromHEX(16756224), LoadTextureNoDuplicates("Chat"));
		ICWarning = new IconColor(fromHEX(15224614), LoadTextureNoDuplicates("Warning"));
		ICWarningBG = new IconColor(fromHEX(15224614, 0.5f), LoadTextureNoDuplicates("WarningBG"));
		ICWarningBGDead = new IconColor(fromHEX(4342338, 0.5f), LoadTextureNoDuplicates("WarningBG"));
		ICWarningDead = new IconColor(fromHEX(4342338), LoadTextureNoDuplicates("WarningDead"));
		ICWarningBioCoded = new IconColor(fromHEX(5091915), LoadTextureNoDuplicates("WarningBioCoded"));
		ICWarningBGBioCoded = new IconColor(fromHEX(5091915, 0.5f), LoadTextureNoDuplicates("WarningBG"));
		Format_KG = "NIT_KG".Translate();
		Format_MoveSpeed = "NIT_MS".Translate();
		Format_Meters = "NIT_M".Translate();
		Format_Seconds = "NIT_Seconds".Translate();
		Format_FireRate = "NIT_RPM".Translate();
		Format_PD = "NIT_PerDay_Add".Translate();
		DamageDef_Nerve = null;
		DamageDef_Beam = null;
		AnyBodyPartCovered_Disapproved_Male = null;
		AnyBodyPartCovered_Disapproved_Female = null;
		ApparelLayerDefOf_Shield = null;
		MeleeWeapon_AverageArmorPenetration = null;
		NIT_MoveApparelToInventory = null;
		NIT_WearFromInventory = null;
		AnyBodyPartCovered_Disapproved_Male = DefDatabase<ThoughtDef>.AllDefs.FirstOrDefault((ThoughtDef x) => x.defName == "AnyBodyPartCovered_Disapproved_Male");
		AnyBodyPartCovered_Disapproved_Female = DefDatabase<ThoughtDef>.AllDefs.FirstOrDefault((ThoughtDef x) => x.defName == "AnyBodyPartCovered_Disapproved_Female");
		ApparelLayerDefOf_Shield = DefDatabase<ApparelLayerDef>.AllDefs.FirstOrDefault((ApparelLayerDef x) => x.defName == "Shield");
		MeleeWeapon_AverageArmorPenetration = DefDatabase<StatDef>.AllDefs.FirstOrDefault((StatDef x) => x.defName == "MeleeWeapon_AverageArmorPenetration");
		LockedButtonTex.filterMode = FilterMode.Point;
		BuffTiledTex.filterMode = FilterMode.Point;
		DamageDef_Nerve = DefDatabase<DamageDef>.AllDefs.FirstOrDefault((DamageDef x) => x.defName == "Nerve");
		DamageDef_Beam = DefDatabase<DamageDef>.AllDefs.FirstOrDefault((DamageDef x) => x.defName == "Beam");
		NIT_MoveApparelToInventory = DefDatabase<JobDef>.AllDefs.FirstOrDefault((JobDef x) => x.defName == "NIT_MoveApparelToInventory");
		NIT_WearFromInventory = DefDatabase<JobDef>.AllDefs.FirstOrDefault((JobDef x) => x.defName == "NIT_WearFromInventory");
	}
}
