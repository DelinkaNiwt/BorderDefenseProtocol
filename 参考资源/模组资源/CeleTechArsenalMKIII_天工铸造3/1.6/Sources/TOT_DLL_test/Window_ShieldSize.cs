using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Window_ShieldSize : Window
{
	private float windowWidth = 0f;

	private float windowHeight = 0f;

	private string[] options = new string[1] { "CloseButton".Translate() };

	private int selectedOption = -1;

	private readonly string title;

	private readonly string description;

	private Building_FRShield building_FRShield;

	private Vector2 scrollPosition = Vector2.zero;

	public override Vector2 InitialSize => new Vector2(windowWidth, windowHeight);

	public Window_ShieldSize(string title, string description, Building_FRShield building)
	{
		this.title = title;
		this.description = description;
		forcePause = true;
		absorbInputAroundWindow = true;
		closeOnAccept = false;
		closeOnCancel = false;
		soundAppear = SoundDefOf.CommsWindow_Open;
		soundClose = SoundDefOf.CommsWindow_Close;
		windowWidth = 320f;
		windowHeight = 190f;
		building_FRShield = building;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect rect = inRect.ContractedBy(10f);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect);
		Text.Font = GameFont.Medium;
		listing_Standard.Label((TaggedString)title, -1f, (string)null);
		Text.Font = GameFont.Small;
		listing_Standard.Label("CMC.ChangeSizeDescription".Translate(building_FRShield.compFullProjectileInterceptor.radius.ToString("F1")));
		building_FRShield.compFullProjectileInterceptor.radius = (int)listing_Standard.Slider(building_FRShield.compFullProjectileInterceptor.radius, 20f, 60f);
		listing_Standard.End();
		for (int i = 0; i < options.Length; i++)
		{
			Rect rect2 = new Rect(rect.x, inRect.height - 25f - (float)(options.Length + 1) * Text.LineHeight + (float)(i + 2) * Text.LineHeight, rect.width, Text.LineHeight);
			Widgets.DrawHighlightIfMouseover(rect2);
			Widgets.Label(rect2, options[i]);
			if (Widgets.ButtonInvisible(rect2))
			{
				selectedOption = i;
				if (selectedOption == 0)
				{
					Close();
				}
			}
		}
	}
}
