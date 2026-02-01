using RimWorld;
using UnityEngine;

namespace CharacterEditor;

public class Teleport_Character : MainTabWindow
{
	public override Vector2 InitialSize => new Vector2(1f, 1f);

	public Teleport_Character()
	{
		closeOnAccept = false;
		closeOnCancel = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		PlacingTool.BeginTeleportCustomPawn();
		Close();
	}
}
