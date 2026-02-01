using System.Collections.Generic;
using Verse;

namespace NyarsModPackOne;

public class ModExtension_EffecterOnMelee : DefModExtension
{
	public List<EffecterDef> effectersAtTarget = new List<EffecterDef>();

	public List<FleckDef> flecksAtTarget = new List<FleckDef>();

	public List<ThingDef> motesAtTarget = new List<ThingDef>();

	public List<EffecterDef> effectersAtCaster = new List<EffecterDef>();

	public List<FleckDef> flecksAtCaster = new List<FleckDef>();

	public List<ThingDef> motesAtCaster = new List<ThingDef>();

	public List<FleckDef> flecksLinkLine = new List<FleckDef>();

	public List<ThingDef> motesLinkLine = new List<ThingDef>();
}
