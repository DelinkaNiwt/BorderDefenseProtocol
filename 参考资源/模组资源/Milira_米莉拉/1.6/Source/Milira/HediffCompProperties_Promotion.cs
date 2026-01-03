using System;
using Verse;

namespace Milira;

public class HediffCompProperties_Promotion : HediffCompProperties
{
	public PawnKindDef promotionPawnkind;

	public HediffDef hediffAddon;

	public FleckDef fleck;

	public SoundDef promotionSound;

	public Type lordJob;

	public bool shouldJoinParentLord;

	public HediffCompProperties_Promotion()
	{
		compClass = typeof(HediffComp_Promotion);
	}
}
