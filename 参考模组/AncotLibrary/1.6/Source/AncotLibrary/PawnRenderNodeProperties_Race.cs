using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_Race : PawnRenderNodeProperties
{
	public List<string> races;

	public PawnRenderNodeProperties_Race()
	{
		nodeClass = typeof(PawnRenderNode_Race);
		workerClass = typeof(PawnRenderNodeWorker_Race);
	}
}
