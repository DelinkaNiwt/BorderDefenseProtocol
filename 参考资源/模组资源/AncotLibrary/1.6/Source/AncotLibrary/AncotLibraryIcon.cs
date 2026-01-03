using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class AncotLibraryIcon
{
	public static readonly Texture2D Illustration = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Illustration");

	public static readonly Texture2D SmallPoint = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/SmallPoint");

	public static readonly Texture2D Switch_ToggleOn = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Switch_ToggleOn");

	public static readonly Texture2D Switch_ToggleOff = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Switch_ToggleOff");

	public static readonly Texture2D Return = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Return");

	public static readonly Texture2D SpannerSmall = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Spanner");

	public static readonly Texture2D Hammer = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Hammer");

	public static readonly Texture2D Apparel = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Apparel");

	public static readonly Texture2D Drone = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Drone");

	public static readonly Texture2D Battery = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Battery");

	public static readonly Texture2D RadioButOff = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/RadioButOff");

	public static readonly Texture2D RadioButSemiOn = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/RadioButSemiOn");

	public static readonly Texture2D RadioButOn = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/RadioButOn");

	public static readonly Texture2D Dismiss_Transparent = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Dismiss_Transparent");

	public static readonly Texture2D Invoke = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Invoke");

	public static readonly Texture2D Info = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Info");

	public static readonly Texture2D Select_Off = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Select_Off");

	public static readonly Texture2D Select_On = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/Select_On");

	public static readonly Texture2D StarOn = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/StarOn");

	public static readonly Texture2D StarOff = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/StarOff");

	public static readonly Texture2D RotClockwise = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/RotClockwise");

	public static readonly Texture2D RotCounterclockwise = ContentFinder<Texture2D>.Get("AncotLibrary/Icons/RotCounterclockwise");

	public static readonly Texture2D SwitchA = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/SwitchA");

	public static readonly Texture2D SwitchPage = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/SwitchPage");

	public static readonly Texture2D ReloadAmmo = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/ReloadAmmo");

	public static readonly Texture2D Gun = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/Gun");

	public static readonly Texture2D Spanner = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/Spanner");

	public static readonly Texture2D Study = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/Study");

	public static readonly Texture2D AutoRepair_On = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/AutoRepair_On");

	public static readonly Texture2D AutoRepair_Off = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/AutoRepair_Off");

	public static readonly Texture2D Circle = ContentFinder<Texture2D>.Get("AncotLibrary/Tex/Circle");

	public static readonly Material MarkQuestion = MaterialPool.MatFrom("AncotLibrary/Gizmos/MarkQuestion", ShaderDatabase.Cutout, new Color(1f, 1f, 1f, 1f));

	public static readonly Texture2D VanillaAssignOwner = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner");

	public static readonly Texture2D VanillaHoldFire = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire");

	public static readonly Texture2D VanillaHalt = ContentFinder<Texture2D>.Get("UI/Commands/Halt");

	public static readonly Texture2D HealthBar_Health = ContentFinder<Texture2D>.Get("AncotLibrary/UI/HealthBar_Health");

	public static readonly Texture2D HealthBar_FrameLeft = ContentFinder<Texture2D>.Get("AncotLibrary/UI/HealthBar_FrameLeft");

	public static readonly Texture2D HealthBar_FrameRight = ContentFinder<Texture2D>.Get("AncotLibrary/UI/HealthBar_FrameRight");
}
