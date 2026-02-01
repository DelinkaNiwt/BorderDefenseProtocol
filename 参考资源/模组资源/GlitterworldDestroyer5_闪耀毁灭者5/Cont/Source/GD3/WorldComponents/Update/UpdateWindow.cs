using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class UpdateWindow : Window
	{

		public UpdateWindow(string title, string description, string texPath)
		{
			this.title = title;
			this.description = description;
			this.texPath = texPath;
			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnAccept = false;
			closeOnCancel = false;
			soundAppear = SoundDefOf.CommsWindow_Open;
			soundClose = SoundDefOf.CommsWindow_Close;
		}

		public void SetTexture(string texPath)
		{
			if (PreviewTexture == null)
			{
				PreviewTexture = ContentFinder<Texture2D>.Get(texPath, reportFailure: false);
			}
		}

		public override void PreOpen()
		{
			base.PreOpen();
			SetTexture(texPath);
		}

		public override void DoWindowContents(Rect inRect)
		{
			float num = 0f;
			UnityEngine.Rect rect = Verse.GenUI.AtZero(inRect);
			if (title != null)
			{
				Verse.Text.Font = Verse.GameFont.Medium;
				UnityEngine.Rect rect2 = rect;
				rect2.height = 38f;
				rect.yMin += 53f;
				Verse.Widgets.DrawTitleBG(rect2);
				rect2.xMin += 9f;
				rect2.yMin += 5f;
				Verse.Widgets.Label(rect2, title);
				num += rect2.height + 15f;
			}
			float num2 = 0f;
			if (PreviewTexture != null)
			{
				UnityEngine.Rect position = rect;
				position.width = 580f;
				num2 = (position.height = position.width * (float)PreviewTexture.height / (float)PreviewTexture.width);
				position.y = num;
				UnityEngine.GUI.DrawTexture(position, PreviewTexture);
				num += position.height + 15f;
			}
			if (description != null)
			{
				Verse.Text.Font = Verse.GameFont.Small;
				UnityEngine.Rect outRect = rect;
				outRect.height = inRect.height - 45f - ((title != null) ? 41f : 0f) - ((PreviewTexture != null) ? (num2 + 15f) : 0f);
				outRect.y = num;
				float width = outRect.width - 16f;
				UnityEngine.Rect viewRect = new UnityEngine.Rect(0f, 0f, width, Verse.Text.CalcHeight(description, width));
				Verse.Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
				Verse.Widgets.Label(new UnityEngine.Rect(0f, 0f, viewRect.width, viewRect.height), description);
				Verse.Widgets.EndScrollView();
			}
			if (Verse.Widgets.ButtonText(new UnityEngine.Rect(0f, inRect.height - 35f, inRect.width - 5f, 35f), "Continue..."))
			{
				Close();
				GDUpdateMod.settings.Save();
			}
		}

		private readonly string texPath;

		private readonly string title;

		private readonly string description;

		private Vector2 scrollPosition = Vector2.zero;

		private Texture2D PreviewTexture;

		public override Vector2 InitialSize => new Vector2(620f, 500f);
	}
}
