using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ThingColumnDef : Def
{
	public Type workerClass = typeof(ThingColumnWorker);

	public bool sortable;

	public bool ignoreWhenCalculatingOptimalTableSize;

	[NoTranslate]
	public string headerIcon;

	public Vector2 headerIconSize;

	[MustTranslate]
	public string headerTip;

	public bool headerAlwaysInteractable;

	public bool paintable;

	public bool groupable;

	public TrainableDef trainable;

	public int gap;

	public WorkTypeDef workType;

	public bool moveWorkTypeLabelDown;

	public bool showIcon;

	public bool useLabelShort;

	public int widthPriority;

	public int width = -1;

	[Unsaved(false)]
	private ThingColumnWorker workerInt;

	[Unsaved(false)]
	private Texture2D headerIconTex;

	private const int IconWidth = 26;

	private static readonly Vector2 IconSize = new Vector2(26f, 26f);

	public ThingColumnWorker Worker
	{
		get
		{
			if (workerInt == null)
			{
				workerInt = (ThingColumnWorker)Activator.CreateInstance(workerClass);
				workerInt.def = this;
			}
			return workerInt;
		}
	}

	public Texture2D HeaderIcon
	{
		get
		{
			if (headerIconTex == null && !headerIcon.NullOrEmpty())
			{
				headerIconTex = ContentFinder<Texture2D>.Get(headerIcon);
			}
			return headerIconTex;
		}
	}

	public Vector2 HeaderIconSize
	{
		get
		{
			if (headerIconSize != default(Vector2))
			{
				return headerIconSize;
			}
			if (HeaderIcon != null)
			{
				return IconSize;
			}
			return Vector2.zero;
		}
	}

	public bool HeaderInteractable => sortable || !headerTip.NullOrEmpty() || headerAlwaysInteractable;
}
