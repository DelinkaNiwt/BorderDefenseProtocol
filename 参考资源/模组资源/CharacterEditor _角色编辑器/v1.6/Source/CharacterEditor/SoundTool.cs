using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal static class SoundTool
{
	internal static void PlayThis(SoundDef def)
	{
		if (def == null)
		{
			return;
		}
		TargetInfo targetInfo = (CEditor.InStartingScreen ? new TargetInfo(default(IntVec3), null, allowNullMap: true) : new TargetInfo(UI.MouseMapPosition().ToIntVec3(), Find.CurrentMap));
		if (!def.sustain && !def.subSounds.NullOrEmpty())
		{
			for (int i = 0; i < def.subSounds.Count; i++)
			{
				def.subSounds[i].TryPlay(targetInfo);
			}
			def.PlayOneShot(targetInfo);
		}
	}

	internal static void SetAndPlayPrev(ref SoundDef source, HashSet<SoundDef> l, Pawn p)
	{
		if (source != null)
		{
			source = l.GetPrev(source);
			source.PlayPawnSound(p);
		}
	}

	internal static void SetAndPlayNext(ref SoundDef source, HashSet<SoundDef> l, Pawn p)
	{
		source = l.GetNext(source);
		source.PlayPawnSound(p);
	}

	internal static void SetAndPlayPawnSound(ref SoundDef source, SoundDef value, Pawn p)
	{
		source = value;
		source.PlayPawnSound(p);
	}

	internal static SoundDef GetAndPlay(SoundDef value)
	{
		PlayThis(value);
		return value;
	}

	internal static SoundDef GetAndPlayPawnSoundCur(SoundDef value)
	{
		value.PlayPawnSound(CEditor.API.Pawn);
		return value;
	}

	internal static void PlayPawnSoundCur(SoundDef value)
	{
		value.PlayPawnSound(CEditor.API.Pawn);
	}

	internal static void PlayPawnSound(this SoundDef def, Pawn p)
	{
		LifeStageUtility.PlayNearestLifestageSound(p, (LifeStageAge ls) => def, (GeneDef g) => def, (MutantDef m) => def);
	}
}
