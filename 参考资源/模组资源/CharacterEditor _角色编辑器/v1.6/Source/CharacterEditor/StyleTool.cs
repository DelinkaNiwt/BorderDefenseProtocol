using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class StyleTool
{
	internal const string CO_FACETATTOO = "faceTattoo";

	internal const string CO_BODYTATTOO = "bodyTattoo";

	internal static string selectedBeardModName;

	internal static HashSet<BeardDef> lOfBeardDefs;

	internal static bool isBeardConfigOpen;

	internal static string GetBeardDefName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.beardDef != null) ? p.style.beardDef.defName : "";
	}

	internal static string GetBeardName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.beardDef != null) ? p.style.beardDef.LabelCap.ToString() : "";
	}

	internal static HashSet<BeardDef> GetBeardList(string modname)
	{
		return (from b in DefTool.ListByMod<BeardDef>(modname)
			orderby !b.noGraphic
			select b).ToHashSet();
	}

	internal static bool SetBeard(this Pawn p, bool next, bool random)
	{
		if (!p.HasStyleTracker())
		{
			return false;
		}
		List<BeardDef> list = GetBeardList(null).ToList();
		if (list.EnumerableNullOrEmpty())
		{
			return false;
		}
		BeardDef beardDef = p.style.beardDef;
		int index = list.IndexOf(beardDef);
		index = list.NextOrPrevIndex(index, next, random);
		BeardDef b = list[index];
		p.SetBeard(b);
		return true;
	}

	internal static void SetBeard(this Pawn p, BeardDef b)
	{
		if (!p.HasStyleTracker() || b == null)
		{
			return;
		}
		try
		{
			if (b == BeardDefOf.NoBeard)
			{
				FixForCATMod_BeardsNotSettableToNoBeard(p, b);
			}
			else
			{
				p.style.beardDef = b;
			}
			p.SetDirty();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void FixForCATMod_BeardsNotSettableToNoBeard(Pawn p, BeardDef b)
	{
		BeardDefOf.NoBeard.noGraphic = false;
		BeardDefOf.NoBeard.texPath = "bclear";
		p.style.beardDef = b;
		CEditor.API.UpdateGraphics();
		BeardDefOf.NoBeard.noGraphic = true;
	}

	internal static string GetFaceTattooDefName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.FaceTattoo != null) ? p.style.FaceTattoo.defName : "";
	}

	internal static string GetFaceTattooName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.FaceTattoo != null) ? p.style.FaceTattoo.LabelCap.ToString() : "";
	}

	internal static HashSet<TattooDef> GetFaceTattooList(string modname)
	{
		return (from x in DefTool.ListByMod(modname, (TattooDef x) => x.tattooType == TattooType.Face)
			orderby !x.noGraphic
			select x).ToHashSet();
	}

	internal static bool SetFaceTattoo(this Pawn p, bool next, bool random)
	{
		if (!p.HasStyleTracker())
		{
			return false;
		}
		List<TattooDef> list = GetFaceTattooList(null).ToList();
		if (list.EnumerableNullOrEmpty())
		{
			return false;
		}
		TattooDef faceTattoo = p.style.FaceTattoo;
		int index = list.IndexOf(faceTattoo);
		index = list.NextOrPrevIndex(index, next, random);
		TattooDef t = list[index];
		p.SetFaceTattoo(t);
		return true;
	}

	internal static void SetFaceTattoo(this Pawn p, TattooDef t)
	{
		if (!p.HasStyleTracker() || t == null)
		{
			return;
		}
		try
		{
			p.style.SetMemberValue("faceTattoo", t);
			p.SetDirty();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static string GetBodyTattooDefName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.BodyTattoo != null) ? p.style.BodyTattoo.defName : "";
	}

	internal static string GetBodyTattooName(this Pawn p)
	{
		return (p.HasStyleTracker() && p.style.BodyTattoo != null) ? p.style.BodyTattoo.LabelCap.ToString() : "";
	}

	internal static HashSet<TattooDef> GetBodyTattooList(string modname)
	{
		return (from x in DefTool.ListByMod(modname, (TattooDef x) => x.tattooType == TattooType.Body)
			orderby !x.noGraphic
			select x).ToHashSet();
	}

	internal static bool SetBodyTattoo(this Pawn p, bool next, bool random)
	{
		if (!p.HasStyleTracker())
		{
			return false;
		}
		List<TattooDef> list = GetBodyTattooList(null).ToList();
		if (list.EnumerableNullOrEmpty())
		{
			return false;
		}
		TattooDef bodyTattoo = p.style.BodyTattoo;
		int index = list.IndexOf(bodyTattoo);
		index = list.NextOrPrevIndex(index, next, random);
		TattooDef t = list[index];
		p.SetBodyTattoo(t);
		return true;
	}

	internal static void SetBodyTattoo(this Pawn p, TattooDef t)
	{
		if (!p.HasStyleTracker() || t == null)
		{
			return;
		}
		try
		{
			p.style.SetMemberValue("bodyTattoo", t);
			p.SetDirty();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void AChooseBeardCustom()
	{
		SZWidgets.FloatMenuOnRect(GetBeardList(null), (BeardDef s) => s.LabelCap, ASetBeardCustom);
	}

	internal static void ASetBeardCustom(BeardDef beardDef)
	{
		CEditor.API.Pawn.SetBeard(beardDef);
		CEditor.API.UpdateGraphics();
	}

	internal static void ARandomBeard()
	{
		CEditor.API.Pawn.SetBeard(next: true, random: true);
		CEditor.API.UpdateGraphics();
	}

	internal static void ASelectedBeardModName(string val)
	{
		selectedBeardModName = val;
		lOfBeardDefs = GetBeardList(selectedBeardModName);
	}

	internal static void AConfigBeard()
	{
		isBeardConfigOpen = !isBeardConfigOpen;
	}
}
