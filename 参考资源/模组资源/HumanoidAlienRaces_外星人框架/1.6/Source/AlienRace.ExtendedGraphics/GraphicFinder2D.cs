using System;
using UnityEngine;
using Verse;

namespace AlienRace.ExtendedGraphics;

internal class GraphicFinder2D : IGraphicFinder<Texture2D>
{
	public Texture2D GetByPath(string basePath, int variant, string direction, bool reportFailure)
	{
		string path = basePath + ((variant == 0) ? "" : variant.ToString()) + "_" + direction;
		try
		{
			return ContentFinder<Texture2D>.Get(path, reportFailure);
		}
		catch (Exception innerException)
		{
			string message = $"Unable to load variant {variant.ToString()} for path '{basePath}' and direction {direction} (reportFailure: {reportFailure}). Tried {path}";
			throw new Exception(message, innerException);
		}
	}
}
