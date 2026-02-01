using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

public class CharacterEditorCascet : Building_CryptosleepCasket
{
	private Pawn innerPawn;

	public CharacterEditorCascet()
	{
		innerPawn = null;
	}

	public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
	{
		bool flag = base.TryAcceptThing(thing, allowSpecialEffects);
		if (flag)
		{
			innerPawn = thing as Pawn;
			CEditor.API.Get<Dictionary<int, Building_CryptosleepCasket>>(EType.UIContainers)[0] = this;
			CEditor.API.StartEditor(innerPawn);
		}
		return flag;
	}
}
