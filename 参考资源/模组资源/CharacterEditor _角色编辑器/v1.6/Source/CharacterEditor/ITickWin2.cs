using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class ITickWin2 : Window
{
	internal const string CO_TMPPLAYERHOMEMAPS = "tmpPlayerHomeMaps";

	private int timeOut = 200;

	private bool doClose = false;

	public override Vector2 InitialSize => new Vector2(1f, 1f);

	public override void DoWindowContents(Rect inRect)
	{
		if (doClose)
		{
			Close();
			doClose = false;
			return;
		}
		if (timeOut > 0)
		{
			timeOut--;
			return;
		}
		doClose = true;
		MapParent parent = Find.CurrentMap.Parent;
		parent.Abandon(wasGravshipLaunch: false);
		Current.Game.SetMemberValue("tmpPlayerHomeMaps", new List<Map>());
		PortraitsCache.Clear();
		PortraitsCache.PortraitsCacheUpdate();
		ScenarioLister.MarkDirty();
		LongEventHandler.ClearQueuedEvents();
		GC.Collect();
		CEditor.StartNewGame2();
		Close();
	}
}
