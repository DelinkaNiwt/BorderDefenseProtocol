using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Gizmo_TurretGunBuilding : Gizmo_ActionAndToggle
{
	public float range = 0f;

	public float minRange = 0f;

	public TargetingParameters targetingParams;

	private readonly List<CompTurretGun_Building> comps = new List<CompTurretGun_Building>();

	public LocalTargetInfo forcedTarget;

	public int cooldownTicksRemaining;

	public int cooldownTicksTotal;

	private int lastUpdate;

	private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color32(155, 215, 223, 64));

	private static readonly Texture2D haltIcon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");

	public void AddComp(CompTurretGun_Building comp)
	{
		if (comp != null && !comps.Contains(comp))
		{
			comps.Add(comp);
		}
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		bool flag = Mouse.IsOver(rect);
		if (parms.highLight)
		{
			Widgets.DrawStrongHighlight(rect.ExpandedBy(4f));
		}
		Material material = (parms.lowLight ? TexUI.GrayscaleGUI : null);
		GUI.color = (parms.lowLight ? Command.LowLightBgColor : (flag ? GenUI.MouseoverColor : Color.white));
		GenUI.DrawTextureWithMaterial(rect, Command.BGTex, material);
		GUI.color = Color.white;
		Rect outerRect = new Rect(rect.x + 5f, rect.y + 5f, 64f, 64f);
		Texture tex = icon ?? BaseContent.BadTex;
		Widgets.DrawTextureFitted(outerRect, tex, 0.85f);
		if (cooldownTicksRemaining > 0 && cooldownTicksTotal > 0)
		{
			float value = Mathf.InverseLerp(cooldownTicksTotal, 0f, cooldownTicksRemaining);
			Widgets.FillableBar(rect, Mathf.Clamp01(value), cooldownBarTex, null, doBorder: false);
			Text.Font = GameFont.Tiny;
			string text = cooldownTicksRemaining.ToStringTicksToPeriod();
			Vector2 vector = Text.CalcSize(text);
			Rect rect2 = new Rect(rect.center.x - vector.x / 2f, rect.y + 2f, vector.x, vector.y);
			Rect rect3 = rect2.ExpandedBy(8f, 0f);
			GUI.DrawTexture(rect3, (Texture)TexUI.GrayTextBG);
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rect2, text);
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
		}
		string text2 = defaultLabel?.CapitalizeFirst() ?? "";
		if (!text2.NullOrEmpty())
		{
			float num = Text.CalcHeight(text2, rect.width);
			Rect rect4 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
			GUI.DrawTexture(rect4, (Texture)TexUI.GrayTextBG);
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rect4, text2);
			Text.Anchor = TextAnchor.UpperLeft;
		}
		if (!defaultDesc.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, defaultDesc);
		}
		Rect rect5 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
		Texture2D texture2D = (comps.Any((CompTurretGun_Building c) => c.fireAtWill) ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
		GUI.DrawTexture(rect5, (Texture)texture2D);
		if (Widgets.ButtonInvisible(rect5))
		{
			foreach (CompTurretGun_Building comp in comps)
			{
				comp.fireAtWill = !comp.fireAtWill;
				(comp.fireAtWill ? turnOnSound : turnOffSound)?.PlayOneShotOnCamera();
			}
		}
		TooltipHandler.TipRegion(rect5, "Ancot.CommandToggleTurretDesc".Translate());
		Rect rect6 = new Rect(rect.xMax - 24f, rect.y + 24f, 24f, 24f);
		bool flag2 = forcedTarget.IsValid && (!forcedTarget.HasThing || (forcedTarget.Thing?.Spawned ?? false));
		GUI.color = (flag2 ? Color.white : Color.grey);
		GUI.DrawTexture(rect6, (Texture)haltIcon);
		TooltipHandler.TipRegion(rect6, flag2 ? "CommandStopForceAttackDesc".Translate() : "CommandStopAttackFailNotForceAttacking".Translate());
		if (flag2 && Widgets.ButtonInvisible(rect6))
		{
			foreach (CompTurretGun_Building comp2 in comps)
			{
				comp2.ResetCurrentTarget();
				comp2.forcedTarget = LocalTargetInfo.Invalid;
				comp2.isForcetargetDowned = false;
			}
			forcedTarget = LocalTargetInfo.Invalid;
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}
		GUI.color = Color.white;
		if (Widgets.ButtonInvisible(rect) && !rect5.Contains(Event.current.mousePosition) && !rect6.Contains(Event.current.mousePosition))
		{
			ProcessTargetingInput();
			return new GizmoResult(GizmoState.Interacted, Event.current);
		}
		return flag ? new GizmoResult(GizmoState.Mouseover, Event.current) : new GizmoResult(GizmoState.Clear, null);
	}

	private void ProcessTargetingInput()
	{
		SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
		Find.DesignatorManager.Deselect();
		Find.Targeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target)
		{
			foreach (CompTurretGun_Building comp in comps)
			{
				comp.currentTarget = target;
				comp.forcedTarget = target;
				if (target.Pawn != null && target.Pawn.Downed)
				{
					comp.isForcetargetDowned = true;
				}
			}
		}, null, null, null, null, null, playSoundOnAction: true, null, null);
	}

	public override void GizmoUpdateOnMouseover()
	{
		base.GizmoUpdateOnMouseover();
	}

	public override bool GroupsWith(Gizmo other)
	{
		if (other is Gizmo_TurretGunBuilding gizmo_TurretGunBuilding)
		{
			return gizmo_TurretGunBuilding.defaultLabel == defaultLabel && gizmo_TurretGunBuilding.icon == icon;
		}
		return false;
	}

	public override void MergeWith(Gizmo other)
	{
		base.MergeWith(other);
		if (!(other is Gizmo_TurretGunBuilding gizmo_TurretGunBuilding))
		{
			return;
		}
		foreach (CompTurretGun_Building comp in gizmo_TurretGunBuilding.comps)
		{
			AddComp(comp);
		}
	}
}
