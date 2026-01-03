using System;
using Verse;

namespace AlienRace;

[AttributeUsage(AttributeTargets.Field)]
public class LoadDefFromField : Attribute
{
	public string defName;

	public LoadDefFromField(string defName)
	{
		this.defName = defName;
	}

	public Def GetDef(Type defType)
	{
		return GenDefDatabase.GetDef(defType, defName);
	}
}
