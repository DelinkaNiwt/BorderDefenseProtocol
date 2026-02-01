using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class NCLCallDef : Def
{
	[MustTranslate]
	public NCLCallTool FirstHello;

	public NCLCallTool WarHello;

	public NCLCallTool OutWarHello;

	[MustTranslate]
	public List<string> RandomHello;

	[MustTranslate]
	public List<NCLCallTool> NCLCallTools;
}
