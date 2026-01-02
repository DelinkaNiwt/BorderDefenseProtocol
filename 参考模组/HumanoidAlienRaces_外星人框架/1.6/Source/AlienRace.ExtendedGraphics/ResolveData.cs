using System.Collections.Generic;
using Verse;

namespace AlienRace.ExtendedGraphics;

public struct ResolveData
{
	public bool head;

	public BodyPartDef bodyPart;

	public string bodyPartLabel;

	public HediffDef hediff;

	public Dictionary<string, object> genericStorage;

	public ResolveData()
	{
		head = false;
		bodyPart = null;
		bodyPartLabel = null;
		hediff = null;
		genericStorage = new Dictionary<string, object>();
	}
}
