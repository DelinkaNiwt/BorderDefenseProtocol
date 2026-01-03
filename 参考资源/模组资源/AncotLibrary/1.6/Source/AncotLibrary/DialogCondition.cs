using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class DialogCondition
{
	public List<GameConditionDef> gameConditions;

	public FloatRange skyGlow;

	public FloatRange hourInterval;

	public FloatRange mapTemperature;

	public List<WeatherDef> weathers;

	public List<NeedProperty> initiatorNeeds;

	public List<NeedProperty> recipientNeeds;
}
