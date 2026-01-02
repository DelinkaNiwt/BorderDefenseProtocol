using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class ThingDef_Custom : ThingDef
{
	public List<PawnRenderNodeProperties> renderNodeProperties;

	public List<PawnRenderNodeProperties> RenderNodeProperties => renderNodeProperties;
}
