using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class GraphicWindow : Window
	{
		private float imageSize = 200f;
		public override Vector2 InitialSize => new Vector2(imageSize, imageSize);
		public GraphicWindow(string texPath, float drawSize, float drawOffset, float adjustx, float adjusty)
		{
			this.texPath = texPath;
			closeOnAccept = false;
			closeOnCancel = false;
			this.drawSize = drawSize;
			soundAppear = SoundDefOf.CommsWindow_Open;
			soundClose = SoundDefOf.CommsWindow_Close;
			this.adjustx = adjustx;
			this.adjusty = adjusty;
			this.drawOffset = drawOffset;
		}
		//UI.screenWidth / 2 - adjustx / 2 - imageSize, UI.screenHeight / 2 - adjusty / 2,

		public void SetTexture(string texPath)
		{
			if (PreviewTexture == null)
			{
				PreviewTexture = ContentFinder<Texture2D>.Get(texPath, reportFailure: false);
			}
			if (PhoneTexture == null)
			{
				PhoneTexture = ContentFinder<Texture2D>.Get("UI/GD_Phone", reportFailure: false);
			}
		}

		public override void PreOpen()
		{
			base.PreOpen();
			SetTexture(texPath);
		}

		public override void PostOpen()
		{
			base.PostOpen();
			this.windowRect = new Rect(UI.screenWidth / 2 - adjustx / 2 - imageSize, UI.screenHeight / 2 - adjusty / 2, imageSize, imageSize);
		}


		public override void DoWindowContents(Rect inRect)
		{
			Rect imageRect = new Rect((inRect.width - imageSize) / 2f, (inRect.height - imageSize) / 2f, imageSize, imageSize);
			Rect imageRectFix = new Rect((inRect.width - imageSize) / 2f, (inRect.height - imageSize - drawOffset) / 2f, imageSize, imageSize);
			Widgets.DrawTextureFitted(imageRect, PreviewTexture, drawSize);
			Widgets.DrawTextureFitted(imageRectFix, PhoneTexture, 1f);
		}

		private readonly string texPath;

		private Vector2 scrollPosition = Vector2.zero;

		private Texture2D PreviewTexture;

		private Texture2D PhoneTexture;

		private float adjustx;

		private float adjusty;

		private float drawSize;

		private float drawOffset;

		//public override Vector2 InitialSize => new Vector2(620f, 700f);
	}
}