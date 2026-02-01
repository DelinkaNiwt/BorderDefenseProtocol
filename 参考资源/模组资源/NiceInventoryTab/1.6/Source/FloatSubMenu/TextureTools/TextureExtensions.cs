using System;
using UnityEngine;
using Verse;

namespace TextureTools;

public static class TextureExtensions
{
	public static Texture2D Overlay(this Texture2D orig, Texture2D overlay, float alpha = 1f)
	{
		return MakeTexture(orig.width, orig.height, Draw);
		void Draw(RenderTexture render)
		{
			Graphics.Blit(orig, render);
			Material mat = MaterialPool.MatFrom(overlay, ShaderDatabase.MetaOverlay, new Color(1f, 1f, 1f, alpha));
			Graphics.Blit(overlay, render, mat);
		}
	}

	public static Texture2D Transform(this Texture2D orig, Rect pos, IntVec2 imageSize = default(IntVec2))
	{
		if (imageSize == default(IntVec2))
		{
			imageSize = new IntVec2(orig.width, orig.height);
		}
		return MakeTexture(imageSize.x, imageSize.z, Draw);
		void Draw(RenderTexture render)
		{
			Vector2 vector = new Vector2(orig.width, orig.height);
			Vector2 vector2 = imageSize.ToVector2() / vector / pos.size;
			Vector2 offset = new Vector2(0f - pos.x, 0f - pos.yMax) * vector2;
			Graphics.Blit(orig, render, vector2, offset);
		}
	}

	private static Texture2D MakeTexture(int width, int height, Action<RenderTexture> draw)
	{
		RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
		draw(temporary);
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(width, height);
		texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary(temporary);
		return texture2D;
	}
}
