using Verse;

namespace Milira;

public static class MilianUtility
{
	public static bool IsMilian(Pawn pawn)
	{
		return pawn?.RaceProps?.body?.defName == "Milian_Body" && pawn != null && pawn.RaceProps?.IsMechanoid == true;
	}

	public static bool IsMilian(ThingDef race)
	{
		return race?.race?.body?.defName == "Milian_Body";
	}

	public static bool IsMilian_PawnClass(Pawn pawn)
	{
		return pawn.def.defName == "Milian_Mechanoid_PawnI" || pawn.def.defName == "Milian_Mechanoid_PawnII" || pawn.def.defName == "Milian_Mechanoid_PawnIII" || pawn.def.defName == "Milian_Mechanoid_PawnIV";
	}

	public static bool IsMilian_KnightClass(Pawn pawn)
	{
		return pawn.def.defName == "Milian_Mechanoid_KnightI" || pawn.def.defName == "Milian_Mechanoid_KnightII" || pawn.def.defName == "Milian_Mechanoid_KnightIII" || pawn.def.defName == "Milian_Mechanoid_KnightIV";
	}

	public static bool IsMilian_RookClass(Pawn pawn)
	{
		return pawn.def.defName == "Milian_Mechanoid_RookI" || pawn.def.defName == "Milian_Mechanoid_RookII" || pawn.def.defName == "Milian_Mechanoid_RookIII" || pawn.def.defName == "Milian_Mechanoid_RookIV";
	}

	public static bool IsMilian_BishopClass(Pawn pawn)
	{
		return pawn.def.defName == "Milian_Mechanoid_BishopI" || pawn.def.defName == "Milian_Mechanoid_BishopII" || pawn.def.defName == "Milian_Mechanoid_BishopIII" || pawn.def.defName == "Milian_Mechanoid_BishopIV";
	}
}
