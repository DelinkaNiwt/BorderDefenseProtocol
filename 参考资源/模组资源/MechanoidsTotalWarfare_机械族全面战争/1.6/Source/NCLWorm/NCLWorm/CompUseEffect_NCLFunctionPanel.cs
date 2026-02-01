using RimWorld;
using Verse;

namespace NCLWorm;

public class CompUseEffect_NCLFunctionPanel : CompUseEffect
{
	public CompProperties_Useable_NCLFunctionPanel Props => (CompProperties_Useable_NCLFunctionPanel)props;

	public override void DoEffect(Pawn usedBy)
	{
		base.DoEffect(usedBy);
		string text = "English";
		if (Prefs.LangFolderName.Contains("hinese"))
		{
			text = "ChineseSimplified";
		}
		Log.Warning(text);
		NCLCallDef nCLCallDef = DefDatabase<NCLCallDef>.GetNamed(text, errorOnFail: false);
		if (nCLCallDef == null)
		{
			nCLCallDef = Props.callDef;
		}
		Log.Warning(nCLCallDef.ToString());
		if (Current.Game.GetComponent<GameComp_NCLWorm>().firstCall)
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, nCLCallDef, nCLCallDef.FirstHello.FirstUseMess, useBaseFunction: false, nCLCallDef.FirstHello, DrawPic: false));
			Current.Game.GetComponent<GameComp_NCLWorm>().firstCall = false;
		}
		else if (Current.Game.GetComponent<GameComp_NCLWorm>().inWormWar)
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, nCLCallDef, nCLCallDef.WarHello.FirstUseMess, useBaseFunction: false, nCLCallDef.WarHello));
		}
		else if (Current.Game.GetComponent<GameComp_NCLWorm>().OutWar)
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, nCLCallDef, nCLCallDef.OutWarHello.FirstUseMess, useBaseFunction: false, nCLCallDef.OutWarHello));
		}
		else
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, nCLCallDef));
		}
	}
}
