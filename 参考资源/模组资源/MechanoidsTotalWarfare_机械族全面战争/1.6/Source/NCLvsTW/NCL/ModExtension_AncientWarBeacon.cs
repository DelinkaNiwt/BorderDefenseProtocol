using System.Collections.Generic;
using Verse;

namespace NCL;

public class ModExtension_AncientWarBeacon : DefModExtension
{
	public List<DragonGraveStageReq> stages;

	[MustTranslate]
	public string toggleGizmoLabel = "NCL_WARBEACON_TOGGLE_GIZMO_LABEL";

	[MustTranslate]
	public string toggleGizmoOffLabel = "NCL_WARBEACON_TOGGLE_GIZMO_OFF_LABEL";

	[NoTranslate]
	public string toggleGizmoIcon = "";

	[NoTranslate]
	public string toggleGizmoOffIcon = "";

	public ThingDef finalThing;

	public PawnKindDef finalPawnKind;
}
